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
    AccountId: string
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

let private encryptedFilePath (accountId: string) =
    join [| getAuthDir (); $"{accountId}-credentials.enc" |]

let private metaFilePath (accountId: string) =
    join [| getAuthDir (); $"{accountId}-meta.json" |]

let private activeAccountFilePath () =
    join [| getAuthDir (); activeAccountFileName |]

let private isSafeAccountId (accountId: string) : bool =
    not (System.String.IsNullOrWhiteSpace accountId)
    && accountId.Length <= 256
    && accountId
       |> Seq.forall (fun c -> System.Char.IsLetterOrDigit c || c = '_' || c = '-')

let private tryGetAccountPaths (accountId: string) : (string * string) option =
    if isSafeAccountId accountId then
        Some(encryptedFilePath accountId, metaFilePath accountId)
    else
        None

let private normalizeAccountIdentity (targetDataHub: string) (email: string) : string =
    let host = extractHost targetDataHub
    let normalizedEmail = email.Trim().ToLowerInvariant()
    $"{host}|{normalizedEmail}"

/// Generate a deterministic filesystem-safe account ID from host and email.
let generateAccountId (targetDataHub: string) (email: string) : string =
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
        elif not (isSafeAccountId credential.Metadata.AccountId) then
            Error "Invalid account identifier format."
        else
            let accountId = credential.Metadata.AccountId
            let encrypted: Node.Buffer.Buffer = safeStorage.encryptString credential.Token
            let base64 = bufferToBase64 encrypted
            let tmpEnc = encryptedFilePath accountId + ".tmp"
            writeFileSync tmpEnc base64 TextEncoding.Utf8
            renameSync tmpEnc (encryptedFilePath accountId)

            let metaJson =
                JS.JSON.stringify {|
                    accountId = credential.Metadata.AccountId
                    name = credential.Metadata.Name
                    email = credential.Metadata.Email
                    avatarUrl = credential.Metadata.AvatarUrl
                    targetDataHub = credential.Metadata.TargetDataHub
                    dateAdded = credential.Metadata.DateAdded
                    tokenInvalid = credential.Metadata.TokenInvalid
                |}

            let tmpMeta = metaFilePath accountId + ".tmp"
            writeFileSync tmpMeta metaJson TextEncoding.Utf8
            renameSync tmpMeta (metaFilePath accountId)
            Ok()
    with ex ->
        Error $"Failed to store auth credentials: {ex.Message}"

let tryLoad (accountId: string) : StoredCredential option =
    try
        if not (isAvailable ()) then
            None
        else
            match tryGetAccountPaths accountId with
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
                    let storedAccountId: string = meta?accountId

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

                    if not (isSafeAccountId storedAccountId) then
                        None
                    else
                        Some {
                            Metadata = {
                                AccountId = storedAccountId
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
                    let accountId = f.Replace("-meta.json", "")
                    tryLoad accountId
                else
                    None
            )
            |> Array.toList
    with _ -> []

let remove (accountId: string) : unit =
    try
        match tryGetAccountPaths accountId with
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

let getActiveAccountId () : string option =
    try
        let path = activeAccountFilePath ()

        if existsSync path then
            let raw = readFileSync path TextEncoding.Utf8
            let parsed: obj = JS.JSON.parse raw
            let id: string = parsed?accountId

            if not (isSafeAccountId id) then None else Some id
        else
            None
    with _ ->
        None

let setActiveAccountId (accountId: string option) : unit =
    try
        let path = activeAccountFilePath ()

        match accountId with
        | Some id when isSafeAccountId id ->
            let json = JS.JSON.stringify {| accountId = id |}
            let tmp = path + ".tmp"
            writeFileSync tmp json TextEncoding.Utf8
            renameSync tmp path
        | Some _ -> ()
        | None ->
            if existsSync path then
                unlinkSync path
    with _ ->
        ()