module ElectronCore.SecureAuthStoreTests

open Main.Auth
open Vitest

let private isLowerHex (value: string) =
    value |> Seq.forall (fun c -> "0123456789abcdef".IndexOf(c) >= 0)

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