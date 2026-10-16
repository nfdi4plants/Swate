module Main.Auth.SecureAuthStore

open System
open Fable.Core
open Fable.Electron.Main
open Main.Bindings.Filesystem
open Main.Bindings.Path
open Thoth.Json.Core
open ARCitISA.Components.Composite.Authentication.Types

[<Literal>]
let private activeAccountFileName = "active-account.json"

/// Non-secret metadata stored in plaintext JSON alongside the encrypted blob.
type AuthMetadata = {
    LocalARCitISAAccountId: string
    Id: int
    Username: string
    Name: string
    Email: string
    CommitEmail: string option
    AvatarUrl: string
    TargetDataHub: string
    DateAdded: string
    TokenStatus: TokenStatus
    TokenExpiresOn: string option
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

let private encryptedFilePath (localARCitISAAccountId: string) =
    join [|
        getAuthDir ()
        $"{localARCitISAAccountId}-credentials.enc"
    |]

let private metaFilePath (localARCitISAAccountId: string) =
    join [| getAuthDir (); $"{localARCitISAAccountId}-meta.json" |]

let private activeAccountFilePath () =
    join [| getAuthDir (); activeAccountFileName |]

let private isSafeLocalARCitISAAccountId (localARCitISAAccountId: string) : bool =
    not (System.String.IsNullOrWhiteSpace localARCitISAAccountId)
    && localARCitISAAccountId.Length <= 256
    && localARCitISAAccountId
       |> Seq.forall (fun c -> System.Char.IsLetterOrDigit c || c = '_' || c = '-')

let private parseTokenStatus (raw: string) : TokenStatus option =
    match raw.Trim().ToLowerInvariant() with
    | "ok" -> Some TokenStatus.Ok
    | "expiring" -> Some TokenStatus.Expiring
    | "invalid" -> Some TokenStatus.Invalid
    | _ -> None

let private tokenStatusToString (status: TokenStatus) : string =
    match status with
    | TokenStatus.Ok -> "ok"
    | TokenStatus.Expiring -> "expiring"
    | TokenStatus.Invalid -> "invalid"

let private authMetadataDecoder (localARCitISAAccountId: string) : Decoder<AuthMetadata> =
    Decode.object (fun get ->
        let tokenStatusFromLegacyFlag =
            get.Optional.Field "tokenInvalid" Decode.bool
            |> Option.map (fun tokenInvalid -> if tokenInvalid then TokenStatus.Invalid else TokenStatus.Ok)

        {
            LocalARCitISAAccountId = localARCitISAAccountId
            Id = get.Required.Field "id" Decode.int
            // Optional: metadata written before commit identities were stored has no username.
            Username = get.Optional.Field "username" Decode.string |> Option.defaultValue ""
            Name = get.Required.Field "name" Decode.string
            Email = get.Required.Field "email" Decode.string
            CommitEmail =
                get.Optional.Field "commitEmail" Decode.string
                |> Option.filter (String.IsNullOrWhiteSpace >> not)
            AvatarUrl = get.Required.Field "avatarUrl" Decode.string
            TargetDataHub = get.Required.Field "targetDataHub" Decode.string
            DateAdded =
                get.Optional.Field "dateAdded" Decode.string
                |> Option.filter (String.IsNullOrWhiteSpace >> not)
                |> Option.defaultWith ARCitISA.Components.DateTimeExtensions.getUtcNowISO
            TokenStatus =
                get.Optional.Field "tokenStatus" Decode.string
                |> Option.bind parseTokenStatus
                |> Option.orElse tokenStatusFromLegacyFlag
                |> Option.defaultValue TokenStatus.Ok
            TokenExpiresOn =
                get.Optional.Field "tokenExpiresOn" Decode.string
                |> Option.filter (String.IsNullOrWhiteSpace >> not)
        }
    )

let private activeLocalARCitISAAccountIdDecoder: Decoder<string option> =
    Decode.object (fun get ->
        get.Optional.Field "localARCitISAAccountId" Decode.string
        |> Option.filter isSafeLocalARCitISAAccountId
    )

let private tryDecodeAuthMetadata (localARCitISAAccountId: string) (raw: string) : AuthMetadata option =
    try
        if not (isSafeLocalARCitISAAccountId localARCitISAAccountId) then
            None
        else
            ARCtrl.Json.Decode.fromJsonString (authMetadataDecoder localARCitISAAccountId) raw
            |> Some
            |> Option.filter (fun metadata -> metadata.Id > 0)
    with _ ->
        None

let private tryDecodeActiveLocalARCitISAAccountId (raw: string) : string option =
    try
        ARCtrl.Json.Decode.fromJsonString activeLocalARCitISAAccountIdDecoder raw
    with _ ->
        None

let private tryGetAccountPaths (localARCitISAAccountId: string) : (string * string) option =
    if isSafeLocalARCitISAAccountId localARCitISAAccountId then
        Some(encryptedFilePath localARCitISAAccountId, metaFilePath localARCitISAAccountId)
    else
        None

let private normalizeAccountIdentity (targetDataHub: string) (email: string) : string =
    let host = extractHost targetDataHub
    let normalizedEmail = email.Trim().ToLowerInvariant()
    $"{host}|{normalizedEmail}"

/// Generate a deterministic filesystem-safe local account key from host and email.
let generateLocalARCitISAAccountId (targetDataHub: string) (email: string) : string =
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
            Error "ARCitISA safe storage is not available on this system."
        elif not (isSafeLocalARCitISAAccountId credential.Metadata.LocalARCitISAAccountId) then
            Error "Invalid account identifier format."
        else
            let localARCitISAAccountId = credential.Metadata.LocalARCitISAAccountId
            let encrypted: Node.Buffer.Buffer = safeStorage.encryptString credential.Token
            let base64 = bufferToBase64 encrypted
            let tmpEnc = encryptedFilePath localARCitISAAccountId + ".tmp"
            writeFileSync tmpEnc base64 TextEncoding.Utf8
            renameSync tmpEnc (encryptedFilePath localARCitISAAccountId)

            let metaJson =
                JS.JSON.stringify {|
                    localARCitISAAccountId = credential.Metadata.LocalARCitISAAccountId
                    id = credential.Metadata.Id
                    username = credential.Metadata.Username
                    name = credential.Metadata.Name
                    email = credential.Metadata.Email
                    commitEmail = credential.Metadata.CommitEmail
                    avatarUrl = credential.Metadata.AvatarUrl
                    targetDataHub = credential.Metadata.TargetDataHub
                    dateAdded = credential.Metadata.DateAdded
                    tokenStatus = tokenStatusToString credential.Metadata.TokenStatus
                    tokenExpiresOn = credential.Metadata.TokenExpiresOn
                    tokenInvalid = credential.Metadata.TokenStatus = TokenStatus.Invalid
                |}

            let tmpMeta = metaFilePath localARCitISAAccountId + ".tmp"
            writeFileSync tmpMeta metaJson TextEncoding.Utf8
            renameSync tmpMeta (metaFilePath localARCitISAAccountId)
            Ok()
    with ex ->
        Error $"Failed to store auth credentials: {ex.Message}"

let tryLoad (localARCitISAAccountId: string) : StoredCredential option =
    try
        if not (isAvailable ()) then
            None
        else
            match tryGetAccountPaths localARCitISAAccountId with
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

                    tryDecodeAuthMetadata localARCitISAAccountId metaRaw
                    |> Option.map (fun metadata -> { Metadata = metadata; Token = token })
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
                    let localARCitISAAccountId = f.Replace("-meta.json", "")
                    tryLoad localARCitISAAccountId
                else
                    None
            )
            |> Array.toList
    with _ -> []

let remove (localARCitISAAccountId: string) : unit =
    try
        match tryGetAccountPaths localARCitISAAccountId with
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

let getActiveLocalARCitISAAccountId () : string option =
    try
        let path = activeAccountFilePath ()

        if existsSync path then
            let raw = readFileSync path TextEncoding.Utf8
            tryDecodeActiveLocalARCitISAAccountId raw
        else
            None
    with _ ->
        None

let setActiveLocalARCitISAAccountId (localARCitISAAccountId: string option) : unit =
    try
        let path = activeAccountFilePath ()

        match localARCitISAAccountId with
        | Some id when isSafeLocalARCitISAAccountId id ->
            let json = JS.JSON.stringify {| localARCitISAAccountId = id |}
            let tmp = path + ".tmp"
            writeFileSync tmp json TextEncoding.Utf8
            renameSync tmp path
        | Some _ -> ()
        | None ->
            if existsSync path then
                unlinkSync path
    with _ ->
        ()
