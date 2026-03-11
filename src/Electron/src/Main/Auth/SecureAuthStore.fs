module Main.Auth.SecureAuthStore

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

// ── helpers ──────────────────────────────────────────────────────────

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

/// Generate a filesystem-safe account ID from host and email.
let generateAccountId (targetDataHub: string) (email: string) : string =
    let combined = $"{targetDataHub.ToLowerInvariant()}_{email.ToLowerInvariant()}"

    combined
    |> String.collect (fun c -> if System.Char.IsLetterOrDigit c then string c else "_")

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
        elif not (existsSync (encryptedFilePath accountId)) then
            None
        elif not (existsSync (metaFilePath accountId)) then
            None
        else
            let base64 = readFileSync (encryptedFilePath accountId) TextEncoding.Utf8
            let buffer = bufferFromBase64 base64
            let token = safeStorage.decryptString buffer
            let metaRaw = readFileSync (metaFilePath accountId) TextEncoding.Utf8
            let meta: obj = JS.JSON.parse metaRaw
            let name: string = meta?name
            let email: string = meta?email
            let avatarUrl: string = meta?avatarUrl
            let targetDataHub: string = meta?targetDataHub
            let storedAccountId: string = meta?accountId

            Some {
                Metadata = {
                    AccountId = storedAccountId
                    Name = name
                    Email = email
                    AvatarUrl = avatarUrl
                    TargetDataHub = targetDataHub
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
        let enc = encryptedFilePath accountId
        let meta = metaFilePath accountId

        if existsSync enc then
            unlinkSync enc

        if existsSync meta then
            unlinkSync meta
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

            if System.String.IsNullOrWhiteSpace id then
                None
            else
                Some id
        else
            None
    with _ ->
        None

let setActiveAccountId (accountId: string option) : unit =
    try
        let path = activeAccountFilePath ()

        match accountId with
        | Some id ->
            let json = JS.JSON.stringify {| accountId = id |}
            let tmp = path + ".tmp"
            writeFileSync tmp json TextEncoding.Utf8
            renameSync tmp path
        | None ->
            if existsSync path then
                unlinkSync path
    with _ ->
        ()