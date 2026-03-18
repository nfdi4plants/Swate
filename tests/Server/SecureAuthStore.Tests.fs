module SecureAuthStoreTests

open Expecto
open Main.Auth

let tests =
    testList "SecureAuthStore" [
        testCase "generateAccountId produces distinct hashes for emails that previously collided" <| fun _ ->
            let targetDataHub = "https://datahub.example.org"
            let dotted = SecureAuthStore.generateAccountId targetDataHub "a.b@x"
            let underscored = SecureAuthStore.generateAccountId targetDataHub "a_b@x"

            Expect.notEqual
                dotted
                underscored
                "Different emails must not collapse to the same account ID."

        testCase "generateAccountId normalizes host casing, trailing slash, and email whitespace" <| fun _ ->
            let canonical =
                SecureAuthStore.generateAccountId "https://datahub.example.org" "user@example.org"

            let normalizedVariant =
                SecureAuthStore.generateAccountId "https://DATAHUB.example.org/" " User@Example.org "

            Expect.equal
                normalizedVariant
                canonical
                "Equivalent host and email values should produce the same account ID."

        testCase "generateAccountId returns a filesystem-safe hex hash" <| fun _ ->
            let accountId =
                SecureAuthStore.generateAccountId "https://datahub.example.org" "user@example.org"

            Expect.equal accountId.Length 64 "SHA-256 hex digests should be 64 characters long."
            Expect.isTrue (accountId |> Seq.forall System.Char.IsAsciiHexDigit) "Account ID should contain only hex digits."
    ]
