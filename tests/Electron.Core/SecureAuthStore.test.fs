module ElectronCore.SecureAuthStoreTests

open System
open System.Text
open Fable.Core
open Fable.Core.JsInterop
open Main.Auth
open Main.Bindings.Path
open Swate.Components.Composite.Authentication.Types
open Vitest
open ElectronCore.TestHelpers

let private fsPromisesDynamic: obj = importAll "fs/promises"

let private isLowerHex (value: string) =
    value |> Seq.forall (fun c -> "0123456789abcdef".IndexOf(c) >= 0)

let private authDirectoryPath rootPath = join [| rootPath; "Settings"; "Auth" |]

let private writeUtf8FileAsync (path: string) (content: string) : JS.Promise<unit> = promise {
    let! _ = fsPromisesDynamic?writeFile (path, content, "utf8") |> unbox<JS.Promise<obj>>

    return ()
}

let private ensureAuthDirectoryAsync rootPath : JS.Promise<string> = promise {
    let authDirectory = authDirectoryPath rootPath

    let! _ =
        fsPromisesDynamic?mkdir (authDirectory, createObj [ "recursive" ==> true ])
        |> unbox<JS.Promise<obj>>

    return authDirectory
}

let private seedStoredCredentialAsync rootPath localSwateAccountId metadataJson (token: string) : JS.Promise<unit> = promise {
    let! authDirectory = ensureAuthDirectoryAsync rootPath
    let encryptedToken = token |> Encoding.UTF8.GetBytes |> Convert.ToBase64String

    do!
        writeUtf8FileAsync
            (join [|
                authDirectory
                $"{localSwateAccountId}-credentials.enc"
            |])
            encryptedToken

    do! writeUtf8FileAsync (join [| authDirectory; $"{localSwateAccountId}-meta.json" |]) metadataJson
}

let private seedActiveAccountAsync rootPath activeAccountJson : JS.Promise<unit> = promise {
    let! authDirectory = ensureAuthDirectoryAsync rootPath
    do! writeUtf8FileAsync (join [| authDirectory; "active-account.json" |]) activeAccountJson
}

let private withTempAuthStore (testBody: string -> JS.Promise<unit>) : JS.Promise<unit> = promise {
    let! rootPath = createTempDirectoryAsync "swate-auth-store-"
    Vitest.vi.stubEnv ("SWATE_TEST_USER_DATA", Some rootPath)

    try
        do! testBody rootPath
        do! removeDirectoryAsync rootPath
        Vitest.vi.unstubAllEnvs ()
    with error ->
        do! removeDirectoryAsync rootPath
        Vitest.vi.unstubAllEnvs ()
        return raise error
}

let private expectStoredCredential (credential: SecureAuthStore.StoredCredential option) =
    match credential with
    | Some value -> value
    | None -> failwith "Expected stored credential to load."

Vitest.describe (
    "SecureAuthStore.generateLocalSwateAccountId",
    fun () ->
        Vitest.test (
            "produces distinct hashes for emails that previously collided",
            fun () ->
                let targetDataHub = "https://datahub.example.org"
                let dotted = SecureAuthStore.generateLocalSwateAccountId targetDataHub "a.b@x"
                let underscored = SecureAuthStore.generateLocalSwateAccountId targetDataHub "a_b@x"
                Vitest.expect(dotted <> underscored).toBe (true)
        )

        Vitest.test (
            "normalizes host casing, trailing slash, and email whitespace",
            fun () ->
                let canonical =
                    SecureAuthStore.generateLocalSwateAccountId "https://datahub.example.org" "user@example.org"

                let normalizedVariant =
                    SecureAuthStore.generateLocalSwateAccountId "https://DATAHUB.example.org/" " User@Example.org "

                Vitest.expect(normalizedVariant).toBe (canonical)
        )

        Vitest.test (
            "returns a filesystem-safe hex hash",
            fun () ->
                let accountId =
                    SecureAuthStore.generateLocalSwateAccountId "https://datahub.example.org" "user@example.org"

                Vitest.expect(accountId.Length).toBe (64)
                Vitest.expect(isLowerHex accountId).toBe (true)
        )
)

Vitest.describe (
    "SecureAuthStore persisted behavior",
    fun () ->
        Vitest.test (
            "stores and restores current credentials",
            fun () ->
                withTempAuthStore (fun _ -> promise {
                    let credential: SecureAuthStore.StoredCredential = {
                        Metadata = {
                            LocalSwateAccountId = "current-key"
                            Id = 42
                            Name = "Current User"
                            Email = "current@example.org"
                            AvatarUrl = "https://example.org/avatar.png"
                            TargetDataHub = "https://datahub.example.org"
                            DateAdded = "2026-06-09T07:00:00.000Z"
                            TokenStatus = TokenStatus.Invalid
                            TokenExpiresOn = Some "2026-06-30"
                        }
                        Token = "current-token"
                    }

                    match SecureAuthStore.store credential with
                    | Error message -> failwith message
                    | Ok() -> ()

                    let loaded = SecureAuthStore.tryLoad "current-key" |> expectStoredCredential
                    Vitest.expect(loaded).toEqual (credential)
                })
        )

        Vitest.test (
            "rejects credentials without a positive numeric id",
            fun () ->
                withTempAuthStore (fun rootPath -> promise {
                    let metadataWithoutId =
                        """{"name":"User","email":"user@example.org","avatarUrl":"https://example.org/avatar.png","targetDataHub":"https://datahub.example.org"}"""

                    do! seedStoredCredentialAsync rootPath "missing-id" metadataWithoutId "token"
                    Vitest.expect(SecureAuthStore.tryLoad "missing-id").toBe (None)

                    let metadataWithInvalidId =
                        """{"id":0,"name":"User","email":"user@example.org","avatarUrl":"https://example.org/avatar.png","targetDataHub":"https://datahub.example.org"}"""

                    do! seedStoredCredentialAsync rootPath "invalid-id" metadataWithInvalidId "token"
                    Vitest.expect(SecureAuthStore.tryLoad "invalid-id").toBe (None)
                })
        )

        Vitest.test (
            "uses the validated filename-derived key instead of metadata account fields",
            fun () ->
                withTempAuthStore (fun rootPath -> promise {
                    let metadataJson =
                        """{"localSwateAccountId":"stored-key","id":42,"name":"Current User","email":"current@example.org","avatarUrl":"https://example.org/avatar.png","targetDataHub":"https://datahub.example.org"}"""

                    do! seedStoredCredentialAsync rootPath "filename-key" metadataJson "token"

                    let loaded = SecureAuthStore.tryLoad "filename-key" |> expectStoredCredential
                    Vitest.expect(loaded.Metadata.LocalSwateAccountId).toBe ("filename-key")
                    Vitest.expect(loaded.Metadata.Id).toBe (42)
                })
        )

        Vitest.test (
            "rejects unsafe account keys and malformed stored metadata",
            fun () ->
                withTempAuthStore (fun rootPath -> promise {
                    do! seedStoredCredentialAsync rootPath "valid-key" """{"name":""" "token"

                    Vitest.expect(SecureAuthStore.tryLoad "../unsafe").toBe (None)
                    Vitest.expect(SecureAuthStore.tryLoad "valid-key").toBe (None)
                })
        )

        Vitest.test (
            "restores the current active account selection",
            fun () ->
                withTempAuthStore (fun _ -> promise {
                    SecureAuthStore.setActiveLocalSwateAccountId (Some "current-account")
                    Vitest.expect(SecureAuthStore.getActiveLocalSwateAccountId ()).toBe (Some "current-account")
                })
        )

        Vitest.test (
            "rejects unsafe and malformed active account selections",
            fun () ->
                withTempAuthStore (fun rootPath -> promise {
                    do! seedActiveAccountAsync rootPath """{"localSwateAccountId":"../unsafe"}"""
                    Vitest.expect(SecureAuthStore.getActiveLocalSwateAccountId ()).toBe (None)

                    do! seedActiveAccountAsync rootPath """{"accountId":"unsupported-account"}"""
                    Vitest.expect(SecureAuthStore.getActiveLocalSwateAccountId ()).toBe (None)

                    do! seedActiveAccountAsync rootPath """{"localSwateAccountId":"""
                    Vitest.expect(SecureAuthStore.getActiveLocalSwateAccountId ()).toBe (None)
                })
        )
)
