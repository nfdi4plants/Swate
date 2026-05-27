module Main.Auth.SecureAuthStore

open System
open Fable.Core
open Fable.Core.JsInterop
open Fable.Electron.Main
open Main.Bindings.Filesystem
open Main.Bindings.Path

[<Literal>]
let private activeAccountFileName = "active-account.json"

/// Non-secret metadata stored in plaintext JSON alongside the encrypted blob.
type AuthMetadata = {
    LocalSwateAccountId: string
    Id: int
    Name: string
    Email: string
    AvatarUrl: string
    TargetDataHub: string
    DateAdded: string
    TokenInvalid: bool
}

/// In-memory representation of a full auth credential set.
type StoredCredential = {
    Metadata: AuthMetadata
    Token: string
}

// ── JS interop helpers for Buffer ────────────────────────────────────

[<Emit("$0.toString('base64')")>]
let private bufferToBase64 (buf: Node.Buffer.Buffer) : string = jsNative

[<Emit("Buffer.from($0, 'base64')")>]
let private bufferFromBase64 (base64: string) : Node.Buffer.Buffer = jsNative

type private CryptoHash =
    abstract update: data: string * inputEncoding: string -> CryptoHash
    abstract digest: encoding: string -> string

[<Import("createHash", "crypto")>]
let private createHash (algorithm: string) : CryptoHash = jsNative

let private sha256Hex (input: string) : string =
    let hash = createHash "sha256"
    hash.update(input, "utf8").digest ("hex")

// ── helpers ──────────────────────────────────────────────────────────

/// Extract host from a normalized base URL for token-provider matching.
let internal extractHost (baseUrl: string) : string =
    let mutable uri = Unchecked.defaultof<Uri>

    if Uri.TryCreate(baseUrl, UriKind.Absolute, &uri) then
        uri.Host.Trim().ToLowerInvariant()
    else
        baseUrl.Trim().ToLowerInvariant()

let private getAuthDir () =
    let settingsRoot = Main.SettingsStore.getSettingsRootPath ()
    let dir = join [| settingsRoot; "Auth" |]
    mkdirSync dir (MkdirOptions(recursive = true))
    dir

let private encryptedFilePath (localSwateAccountId: string) =
    join [| getAuthDir (); $"{localSwateAccountId}-credentials.enc" |]

let private metaFilePath (localSwateAccountId: string) =
    join [| getAuthDir (); $"{localSwateAccountId}-meta.json" |]

let private activeAccountFilePath () =
    join [| getAuthDir (); activeAccountFileName |]

let private isSafeLocalSwateAccountId (localSwateAccountId: string) : bool =
    not (System.String.IsNullOrWhiteSpace localSwateAccountId)
    && localSwateAccountId.Length <= 256
    && localSwateAccountId
       |> Seq.forall (fun c -> System.Char.IsLetterOrDigit c || c = '_' || c = '-')

let private tryGetAccountPaths (localSwateAccountId: string) : (string * string) option =
    if isSafeLocalSwateAccountId localSwateAccountId then
        Some(encryptedFilePath localSwateAccountId, metaFilePath localSwateAccountId)
    else
        None

let private normalizeAccountIdentity (targetDataHub: string) (email: string) : string =
    let host = extractHost targetDataHub
    let normalizedEmail = email.Trim().ToLowerInvariant()
    $"{host}|{normalizedEmail}"

/// Generate a deterministic filesystem-safe local account key from host and email.
let generateLocalSwateAccountId (targetDataHub: string) (email: string) : string =
    normalizeAccountIdentity targetDataHub email |> sha256Hex

// ── public API ───────────────────────────────────────────────────────

let isAvailable () : bool =
    try
        safeStorage.isEncryptionAvailable ()
    with _ ->
        false

let store (credential: StoredCredential) : Result<unit, string> =
    try
        if not (isAvailable ()) then
            Error "Electron safe storage is not available on this system."
        elif not (isSafeLocalSwateAccountId credential.Metadata.LocalSwateAccountId) then
            Error "Invalid account identifier format."
        else
            let localSwateAccountId = credential.Metadata.LocalSwateAccountId
            let encrypted: Node.Buffer.Buffer = safeStorage.encryptString credential.Token
            let base64 = bufferToBase64 encrypted
            let tmpEnc = encryptedFilePath localSwateAccountId + ".tmp"
            writeFileSync tmpEnc base64 TextEncoding.Utf8
            renameSync tmpEnc (encryptedFilePath localSwateAccountId)

            let metaJson =
                JS.JSON.stringify {|
                    localSwateAccountId = credential.Metadata.LocalSwateAccountId
                    id = credential.Metadata.Id
                    name = credential.Metadata.Name
                    email = credential.Metadata.Email
                    avatarUrl = credential.Metadata.AvatarUrl
                    targetDataHub = credential.Metadata.TargetDataHub
                    dateAdded = credential.Metadata.DateAdded
                    tokenInvalid = credential.Metadata.TokenInvalid
                |}

            let tmpMeta = metaFilePath localSwateAccountId + ".tmp"
            writeFileSync tmpMeta metaJson TextEncoding.Utf8
            renameSync tmpMeta (metaFilePath localSwateAccountId)
            Ok()
    with ex ->
        Error $"Failed to store auth credentials: {ex.Message}"

let tryLoad (localSwateAccountId: string) : StoredCredential option =
    try
        if not (isAvailable ()) then
            None
        else
            match tryGetAccountPaths localSwateAccountId with
            | None -> None
            | Some(enc, metaPath) ->
                if not (existsSync enc) then
                    None
                elif not (existsSync metaPath) then
                    None
                else
                    let base64 = readFileSync enc TextEncoding.Utf8
                    let buffer = bufferFromBase64 base64
                    let token = safeStorage.decryptString buffer
                    let metaRaw = readFileSync metaPath TextEncoding.Utf8
                    let meta: obj = JS.JSON.parse metaRaw
                    let name: string = meta?name
                    let email: string = meta?email
                    let avatarUrl: string = meta?avatarUrl
                    let targetDataHub: string = meta?targetDataHub
                    let storedLocalSwateAccountId: string = meta?localSwateAccountId
                    let id: int =
                        try
                            let value: int = meta?id
                            value
                        with _ ->
                            -1

                    let dateAdded: string =
                        try
                            let value: string = meta?dateAdded

                            if String.IsNullOrWhiteSpace value then
                                Swate.Components.DateTimeExtensions.getUtcNowISO ()
                            else
                                value
                        with _ ->
                            Swate.Components.DateTimeExtensions.getUtcNowISO ()

                    let tokenInvalid: bool =
                        try
                            let value: bool = meta?tokenInvalid
                            value
                        with _ ->
                            false

                    if not (isSafeLocalSwateAccountId storedLocalSwateAccountId) || id <= 0 then
                        None
                    else
                        Some {
                            Metadata = {
                                LocalSwateAccountId = storedLocalSwateAccountId
                                Id = id
                                Name = name
                                Email = email
                                AvatarUrl = avatarUrl
                                TargetDataHub = targetDataHub
                                DateAdded = dateAdded
                                TokenInvalid = tokenInvalid
                            }
                            Token = token
                        }
    with _ ->
        None

/// Load all stored accounts by scanning the Auth directory for meta files.
let loadAll () : StoredCredential list =
    try
        if not (isAvailable ()) then
            []
        else
            let dir = getAuthDir ()
            let files = readdirSync dir

            files
            |> Array.choose (fun f ->
                if f.EndsWith("-meta.json") then
                    let localSwateAccountId = f.Replace("-meta.json", "")
                    tryLoad localSwateAccountId
                else
                    None
            )
            |> Array.toList
    with _ -> []

let remove (localSwateAccountId: string) : unit =
    try
        match tryGetAccountPaths localSwateAccountId with
        | None -> ()
        | Some(enc, metaPath) ->
            if existsSync enc then
                unlinkSync enc

            if existsSync metaPath then
                unlinkSync metaPath
    with _ ->
        ()

let clearAll () : unit =
    try
        let dir = getAuthDir ()
        let files = readdirSync dir

        for f in files do
            let fullPath = join [| dir; f |]
            unlinkSync fullPath
    with _ ->
        ()

let getActiveLocalSwateAccountId () : string option =
    try
        let path = activeAccountFilePath ()

        if existsSync path then
            let raw = readFileSync path TextEncoding.Utf8
            let parsed: obj = JS.JSON.parse raw
            let id: string = parsed?localSwateAccountId

            if not (isSafeLocalSwateAccountId id) then None else Some id
        else
            None
    with _ ->
        None

let setActiveLocalSwateAccountId (localSwateAccountId: string option) : unit =
    try
        let path = activeAccountFilePath ()

        match localSwateAccountId with
        | Some id when isSafeLocalSwateAccountId id ->
            let json = JS.JSON.stringify {| localSwateAccountId = id |}
            let tmp = path + ".tmp"
            writeFileSync tmp json TextEncoding.Utf8
            renameSync tmp path
        | Some _ -> ()
        | None ->
            if existsSync path then
                unlinkSync path
    with _ ->
        ()