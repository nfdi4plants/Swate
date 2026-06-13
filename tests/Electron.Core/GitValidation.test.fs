module ElectronCore.GitValidationTests

open System
open System.Reflection
open System.Text.RegularExpressions
open Fable.Core
open Fable.Core.JS
open Fable.Core.JsInterop
open Fable.Electron
open Main.Bindings.Path
open Main.Bindings.SimpleGit
open Microsoft.FSharp.Reflection
open Swate.Components.Composite.Authentication.Types
open Swate.Electron.Shared.AuthTypes
open Swate.Electron.Shared.GitTypes
open Swate.Electron.Shared.IPCTypes
open Vitest

module GitService = Main.Git.GitService
module GitProvisioningService = Main.Git.GitProvisioningService
module GitAuthAdapter = Main.Git.GitAuthAdapter
module GitTokenProvider = Main.Git.GitTokenProvider
module AuthService = Main.Auth.AuthService

let private expectOk expected (result: Result<'T, exn>) =
    match result with
    | Ok actual -> Vitest.expect(actual).toEqual (expected)
    | Error error -> failwith $"Expected Ok but got Error: {error.Message}"

let private expectError (result: Result<'T, exn>) =
    match result with
    | Ok _ -> failwith "Expected Error but got Ok."
    | Error _ -> ()

let private flattenFunctionSignature (functionType: Type) =
    let rec collect (arguments: Type list) (current: Type) =
        if FSharpType.IsFunction current then
            let domainType, rangeType = FSharpType.GetFunctionElements current
            collect (domainType :: arguments) rangeType
        else
            List.rev arguments, current

    collect [] functionType

let private getRecordField (recordType: Type) (fieldName: string) : PropertyInfo =
    let field =
        FSharpType.GetRecordFields recordType
        |> Microsoft.FSharp.Collections.Array.tryFind (fun property -> property.Name = fieldName)

    match field with
    | Some property -> property
    | None -> failwith $"Expected field '{fieldName}' on record {recordType.FullName}."

let private extractSingleLineBlock (startPattern: string) (endPattern: string) (text: string) =
    let blockMatch =
        Regex.Match(text, $"{startPattern}(.*?){endPattern}", RegexOptions.Singleline ||| RegexOptions.Multiline)

    if not blockMatch.Success then
        failwith $"Could not extract source block between '{startPattern}' and '{endPattern}'."

    blockMatch.Groups.[1].Value

Vitest.describe (
    "GitService.classifyFailureKind",
    fun () ->
        let cases = [|
            "maps unauthorized message", "authentication failed for remote", GitFailureKind.Unauthorized
            "maps forbidden message", "403 Forbidden", GitFailureKind.Forbidden
            "maps network message", "could not resolve host github.com", GitFailureKind.Network
            "maps timeout message", "operation timed out", GitFailureKind.Timeout
            "maps canceled message", "AbortError: signal aborted", GitFailureKind.Canceled
            "maps duplicate DataHub project message",
            "GitLab request failed with HTTP 400: {\"message\":{\"name\":[\"has already been taken\"],\"path\":[\"has already been taken\"]}}",
            GitFailureKind.RemoteProjectAlreadyExists
            "falls back to unknown message", "something unexpected", GitFailureKind.Unknown
            "keeps unauthorized priority over network indicators",
            "connection refused, could not read username",
            GitFailureKind.Unauthorized
            "keeps timeout priority over authentication indicators",
            "timeout while authentication failed",
            GitFailureKind.Timeout
            "maps permission denied publickey to unauthorized",
            "Permission denied (publickey).",
            GitFailureKind.Unauthorized
            "keeps forbidden precedence when 403 and auth text are mixed",
            "403 Forbidden: could not read username",
            GitFailureKind.Forbidden
        |]

        for testName, message, expected in cases do
            Vitest.test (testName, fun () -> Vitest.expect(GitService.classifyFailureKind message).toEqual (expected))
)

Vitest.describe (
    "GitService.resolvePushTarget",
    fun () ->
        let cases = [|
            "pushes the current local branch and sets upstream when no upstream is configured",
            None,
            Some "feature/local-only",
            None,
            false,
            ("feature/local-only", "feature/local-only", true)

            "uses the current local branch without setting upstream when tracking already exists",
            None,
            Some "feature/tracked",
            Some "origin/feature/tracked",
            false,
            ("feature/tracked", "feature/tracked", false)

            "keeps an explicitly requested non-current branch as-is",
            Some "feature/other",
            Some "feature/current",
            None,
            false,
            ("feature/other", "feature/other", false)

            "falls back to HEAD for detached checkouts",
            None,
            Some "feature/detached",
            None,
            true,
            ("HEAD", "HEAD", false)
        |]

        for testName,
            requestedBranch,
            currentBranch,
            trackingBranch,
            isDetached,
            (expectedRefSpec, expectedPushBranch, expectedSetUpstream) in cases do
            Vitest.test (
                testName,
                fun () ->
                    let pushTarget =
                        GitService.resolvePushTarget requestedBranch currentBranch trackingBranch isDetached

                    Vitest.expect(pushTarget.RefSpec).toBe (expectedRefSpec)
                    Vitest.expect(pushTarget.PushBranch).toBe (expectedPushBranch)
                    Vitest.expect(pushTarget.SetUpstream).toBe (expectedSetUpstream)
            )
)

Vitest.describe (
    "GitService.ensureValidBranchLikeName",
    fun () ->
        let validCases = [|
            "accepts main", "main"
            "accepts nested branch", "feature/x"
        |]

        for testName, value in validCases do
            Vitest.test (testName, fun () -> GitService.ensureValidBranchLikeName "Branch name" value |> expectOk value)

        let invalidCases = [|
            "rejects empty branch", ""
            "rejects leading dash", "-flag"
            "rejects double-dot sequence", "a..b"
            "rejects tilde", "a~b"
            "rejects whitespace", "has space"
            "rejects trailing dot", "ok."
            "rejects @{ sequence", "a@{b"
            "rejects leading slash", "/feature"
            "rejects .lock suffix segment", "feature.lock"
            "rejects backslash character explicitly", "feature\\test"
            "rejects control characters", "feature\u0007test"
        |]

        for testName, value in invalidCases do
            Vitest.test (testName, fun () -> GitService.ensureValidBranchLikeName "Branch name" value |> expectError)
)

Vitest.describe (
    "GitService.ensureValidPathspec",
    fun () ->
        let validCases = [|
            "accepts relative path", "src/file.txt", "src/file.txt"
            "accepts unicode pathspec", "src/\u00FCber-\u03B2.txt", "src/\u00FCber-\u03B2.txt"
            "normalizes windows backslashes", "src\\file.txt", "src/file.txt"
        |]

        for testName, value, expected in validCases do
            Vitest.test (testName, fun () -> GitService.ensureValidPathspec value |> expectOk expected)

        Vitest.test (
            "accepts very long pathspec",
            fun () ->
                let longSegment = String.replicate 280 "a"
                let longPath = $"src/{longSegment}.txt"

                GitService.ensureValidPathspec longPath |> expectOk longPath
        )

        let invalidCases = [|
            "rejects empty pathspec", ""
            "rejects unix absolute path", "/etc/passwd"
            "rejects windows absolute path", "C:/Windows/file"
            "rejects traversal path", "../escape"
            "rejects dot segment path", "a/./b"
            "rejects pathspec with null character at index 0", "\u0000src/file.txt"
        |]

        for testName, value in invalidCases do
            Vitest.test (testName, fun () -> GitService.ensureValidPathspec value |> expectError)
)

Vitest.describe (
    "GitService.ensureAllowedRemoteUrl",
    fun () ->
        let validCases = [|
            "accepts https remote", "https://github.com/user/repo"
            "accepts ssh remote", "ssh://git@github.com/repo"
        |]

        for testName, value in validCases do
            Vitest.test (testName, fun () -> GitService.ensureAllowedRemoteUrl value |> expectOk value)

        let invalidCases = [|
            "rejects file protocol", "file:///local"
            "rejects ext protocol", "ext::ssh -o ProxyCommand"
            "rejects git protocol", "git://host/repo"
            "rejects empty URL", ""
            "rejects protocol override attempt", "https://github.com/repo -c protocol.file.allow=always"
        |]

        for testName, value in invalidCases do
            Vitest.test (testName, fun () -> GitService.ensureAllowedRemoteUrl value |> expectError)
)

Vitest.describe (
    "GitService.tryGetRepositoryWebUrlFromRemoteUrl",
    fun () ->
        let validCases = [|
            "converts https remote with .git suffix",
            "https://github.com/nfdi4plants/Swate.git",
            "https://github.com/nfdi4plants/Swate"
            "keeps https remote without .git suffix",
            "https://gitlab.example/group/project",
            "https://gitlab.example/group/project"
            "converts ssh remote to https browser URL",
            "ssh://git@gitlab.example/group/project.git",
            "https://gitlab.example/group/project"
            "removes credentials from https remote",
            "https://oauth2:secret@gitlab.example/group/project.git",
            "https://gitlab.example/group/project"
        |]

        for testName, remoteUrl, expectedWebUrl in validCases do
            Vitest.test (
                testName,
                fun () ->
                    GitService.tryGetRepositoryWebUrlFromRemoteUrl remoteUrl
                    |> expectOk expectedWebUrl
            )

        let invalidCases = [|
            "rejects empty remote", ""
            "rejects unsupported protocol", "git://github.com/nfdi4plants/Swate.git"
            "rejects SCP-style ssh remote", "git@github.com:nfdi4plants/Swate.git"
            "rejects remote without repository path", "https://github.com"
        |]

        for testName, remoteUrl in invalidCases do
            Vitest.test (testName, fun () -> GitService.tryGetRepositoryWebUrlFromRemoteUrl remoteUrl |> expectError)
)

Vitest.describe (
    "GitAuthAdapter.redactToken and redactArgs",
    fun () ->
        let tokenCases = [|
            "redacts bearer token", "Authorization: Bearer abc123", "Authorization: [REDACTED]"
            "redacts lowercase bearer token", "authorization:bearer XYZ", "authorization:[REDACTED]"
            "redacts bearer token without whitespace after the scheme",
            "Authorization:Bearerabc123",
            "Authorization:[REDACTED]"
            "redacts gitlab private-token header", "PRIVATE-TOKEN: abc123", "PRIVATE-TOKEN: [REDACTED]"
            "redacts x-access-token header", "X-Access-Token: abc123", "X-Access-Token: [REDACTED]"
            "keeps clean message unchanged", "clean message", "clean message"
            "keeps empty string unchanged", "", ""
            "redacts credential-bearing https URLs",
            "fatal: unable to access 'https://ghp_abc123@github.com/user/repo.git/'",
            "fatal: unable to access 'https://[REDACTED]@github.com/user/repo.git/'"
            "redacts multiple bearer tokens in one string",
            "Authorization: Bearer a and Authorization: Bearer b",
            "Authorization: [REDACTED] and Authorization: [REDACTED]"
            "redacts bearer token at start and end of string",
            "prefix Authorization: Bearer xyz789",
            "prefix Authorization: [REDACTED]"
            "redacts bearer tokens across multiple lines",
            "line1 Authorization: Bearer first\nline2 Authorization: Bearer second",
            "line1 Authorization: [REDACTED]\nline2 Authorization: [REDACTED]"
            "does not redact partial authorization field names",
            "Authorizatio: Bearer token123",
            "Authorizatio: Bearer token123"
        |]

        for testName, input, expected in tokenCases do
            Vitest.test (testName, fun () -> Vitest.expect(GitAuthAdapter.redactToken input).toBe (expected))

        Vitest.test (
            "keeps null unchanged",
            fun () ->
                let result: string = GitAuthAdapter.redactToken null
                Vitest.expect(isNull result).toBe (true)
        )

        Vitest.test (
            "redacts tokens in argument arrays",
            fun () ->
                let redacted =
                    GitAuthAdapter.redactArgs [|
                        "-c"
                        "http.extraHeader=Authorization: Bearer topsecret"
                    |]

                Vitest.expect(redacted.[1]).toBe ("http.extraHeader=Authorization: [REDACTED]")
        )

        Vitest.test (
            "redacts bearer token at the start of the string",
            fun () ->
                let redacted = GitAuthAdapter.redactToken "Authorization: Bearer abc123"
                Vitest.expect(redacted).toBe ("Authorization: [REDACTED]")
        )
)

Vitest.describe (
    "AuthStateDto helpers",
    fun () ->
        Vitest.test (
            "UsableActiveUser excludes invalidated active accounts",
            fun () ->
                let invalidActiveAccount: AccountSummary = {
                    User = {
                        Id = 1
                        LocalSwateAccountId = "acc-1"
                        Name = "Invalid User"
                        Email = "invalid@example.org"
                        AvatarUrl = "https://example.org/avatar.png"
                        TargetDataHub = "https://git.nfdi4plants.org/"
                    }
                    DateAdded = "2026-01-01T00:00:00.0000000Z"
                    TokenStatus = TokenStatus.Invalid
                    TokenExpiresOn = Some "2026-01-15"
                }

                let authState: AuthStateDto = {
                    ActiveAccount = Some invalidActiveAccount
                    StoredAccounts = [| invalidActiveAccount |]
                }

                Vitest.expect(authState.ActiveUser().IsSome).toBe (true)
                Vitest.expect(authState.UsableActiveUser()).toEqual (None)
        )
)

Vitest.describe (
    "GitTokenProvider.tryExtractHostFromRemoteUrl",
    fun () ->
        let validCases = [|
            "extracts github host from https URL", "https://github.com/user/repo.git", "github.com"
            "extracts gitlab host from ssh URL", "ssh://git@gitlab.com/repo", "gitlab.com"
        |]

        for testName, value, expected in validCases do
            Vitest.test (testName, fun () -> GitTokenProvider.tryExtractHostFromRemoteUrl value |> expectOk expected)

        let invalidCases = [|
            "rejects file URL", "file:///local"
            "rejects empty URL", ""
            "rejects http URL", "http://host/repo"
        |]

        for testName, value in invalidCases do
            Vitest.test (testName, fun () -> GitTokenProvider.tryExtractHostFromRemoteUrl value |> expectError)
)

Vitest.describe (
    "GitAuthAdapter.toConfigEntries and buildAuthArgs",
    fun () ->
        let configCases = [|
            "extracts one config entry after -c", [| "-c"; "key=val" |], [| "key=val" |]
            "ignores plain args without -c", [| "plain" |], [||]
            "extracts multiple config entries", [| "-c"; "a=1"; "-c"; "b=2" |], [| "a=1"; "b=2" |]
        |]

        for testName, args, expected in configCases do
            Vitest.test (testName, fun () -> Vitest.expect(GitAuthAdapter.toConfigEntries args).toEqual (expected))

        Vitest.test (
            "buildAuthArgs scopes clone auth header to the gitlab host when remote name is missing",
            fun () ->
                let repoUrl = "https://git.nfdi4plants.org/caroott/TestARCGit.git"

                let args =
                    GitAuthAdapter.buildAuthArgs "git.nfdi4plants.org" "abc123" None (Some repoUrl)

                Vitest
                    .expect(args)
                    .toEqual (
                        [|
                            "-c"
                            "http.https://git.nfdi4plants.org/.extraHeader=Authorization: Basic b2F1dGgyOmFiYzEyMw=="
                        |]
                    )
        )

        Vitest.test (
            "buildAuthArgs does not emit a global http.extraHeader",
            fun () ->
                let repoUrl = "https://git.nfdi4plants.org/caroott/TestARCGit.git"

                let args =
                    GitAuthAdapter.buildAuthArgs "git.nfdi4plants.org" "abc123" (Some "origin") (Some repoUrl)

                let containsEntry entry = args |> Seq.exists ((=) entry)

                Vitest.expect(containsEntry "http.extraHeader=Authorization: Basic b2F1dGgyOmFiYzEyMw==").toBe (false)
        )

        Vitest.test (
            "buildAuthArgs keeps scoped auth alongside authenticated remote and lfs urls",
            fun () ->
                let repoUrl = "https://git.nfdi4plants.org/caroott/TestARCGit.git"

                let args =
                    GitAuthAdapter.buildAuthArgs "git.nfdi4plants.org" "abc123" (Some "origin") (Some repoUrl)

                let containsEntry entry = args |> Seq.exists ((=) entry)

                Vitest
                    .expect(
                        containsEntry
                            "http.https://git.nfdi4plants.org/.extraHeader=Authorization: Basic b2F1dGgyOmFiYzEyMw=="
                    )
                    .toBe (true)

                Vitest
                    .expect(
                        containsEntry
                            "remote.origin.url=https://oauth2:abc123@git.nfdi4plants.org/caroott/TestARCGit.git"
                    )
                    .toBe (true)

                Vitest
                    .expect(
                        containsEntry
                            "remote.origin.lfsurl=https://oauth2:abc123@git.nfdi4plants.org/caroott/TestARCGit.git/info/lfs"
                    )
                    .toBe (true)
        )
)

Vitest.describe (
    "GitService.validateRemoteName",
    fun () ->
        let validCases = [|
            "accepts origin", "origin", "origin"
            "defaults empty to origin", "", "origin"
            "accepts upstream", "upstream", "upstream"
            "accepts mixed punctuation pattern", "a/b.c-d_e", "a/b.c-d_e"
        |]

        for testName, value, expected in validCases do
            Vitest.test (testName, fun () -> GitService.validateRemoteName value |> expectOk expected)

        Vitest.test ("rejects remote with spaces", fun () -> GitService.validateRemoteName "bad remote" |> expectError)
)

Vitest.describe (
    "AuthService revalidation helpers",
    fun () ->
        Vitest.test (
            "shouldSkipRevalidation is true while the cooldown window is still active",
            fun () ->
                let now = DateTime.UtcNow
                let lastStarted = now - TimeSpan.FromSeconds 5.0

                Vitest.expect(AuthService.shouldSkipRevalidation (Some lastStarted) now).toBe (true)
        )

        Vitest.test (
            "shouldSkipRevalidation is false once the cooldown window has elapsed",
            fun () ->
                let now = DateTime.UtcNow
                let lastStarted = now - TimeSpan.FromSeconds 45.0

                Vitest.expect(AuthService.shouldSkipRevalidation (Some lastStarted) now).toBe (false)
        )

        Vitest.test (
            "nextTokenStatusState marks unauthorized and forbidden failures as invalid",
            fun () ->
                Vitest
                    .expect(AuthService.nextTokenStatusState TokenStatus.Ok AuthFailureKind.Unauthorized)
                    .toEqual (TokenStatus.Invalid)

                Vitest
                    .expect(AuthService.nextTokenStatusState TokenStatus.Ok AuthFailureKind.Forbidden)
                    .toEqual (TokenStatus.Invalid)
        )

        Vitest.test (
            "nextTokenStatusState preserves the existing status for non-auth failures",
            fun () ->
                Vitest
                    .expect(AuthService.nextTokenStatusState TokenStatus.Expiring AuthFailureKind.Network)
                    .toEqual (TokenStatus.Expiring)

                Vitest
                    .expect(AuthService.nextTokenStatusState TokenStatus.Invalid AuthFailureKind.Network)
                    .toEqual (TokenStatus.Invalid)
        )
)

Vitest.describe (
    "GitProvisioningService validation helpers",
    fun () ->
        Vitest.describe (
            "target path normalization",
            fun () ->
                Vitest.test (
                    "rejects empty target path",
                    fun () ->
                        GitProvisioningService.validateAndNormalizeTargetPathWithResolver id ""
                        |> expectError
                )

                Vitest.test (
                    "rejects target path containing null byte",
                    fun () ->
                        GitProvisioningService.validateAndNormalizeTargetPathWithResolver id "repo\u0000name"
                        |> expectError
                )

                Vitest.test (
                    "normalizes target path via resolver",
                    fun () ->
                        let fakeResolver (value: string) = $"ABS::{value.Replace('\\', '/')}"

                        GitProvisioningService.validateAndNormalizeTargetPathWithResolver
                            fakeResolver
                            "  repo\\nested  "
                        |> expectOk "ABS::repo/nested"
                )
        )

        Vitest.describe (
            "clone target emptiness",
            fun () ->
                let cases = [|
                    "fails when existing directory is non-empty",
                    GitProvisioningService.classifyCloneTargetState true 1,
                    false
                    "allows existing empty directory target",
                    GitProvisioningService.classifyCloneTargetState true 0,
                    true
                    "allows missing directory target", GitProvisioningService.classifyCloneTargetState false 0, true
                |]

                for testName, state, shouldSucceed in cases do
                    Vitest.test (
                        testName,
                        fun () ->
                            let result = GitProvisioningService.ensureCloneTargetIsEmpty state

                            if shouldSucceed then
                                result |> expectOk ()
                            else
                                result |> expectError
                    )
        )

        Vitest.describe (
            "clone branch option assembly",
            fun () ->
                Vitest.test (
                    "returns empty options when branch is missing",
                    fun () ->
                        let options = GitProvisioningService.buildCloneBranchOptions None
                        Vitest.expect(options).toEqual ([||])
                )

                Vitest.test (
                    "includes --branch option pair when branch is provided",
                    fun () ->
                        let options =
                            GitProvisioningService.buildCloneBranchOptions (Some "feature/clone-flow")

                        Vitest.expect(options).toEqual ([| "--branch"; "feature/clone-flow" |])
                )
        )
)

Vitest.describe (
    "Git renderer workflow contracts",
    fun () ->
        Vitest.test (
            "IGitApi exposes installGitLfs as a typed no-payload endpoint",
            fun () ->
                let installField = getRecordField typeof<IGitApi> "installGitLfs"
                let argumentTypes, returnType = flattenFunctionSignature installField.PropertyType

                Vitest.expect(argumentTypes.Length).toBe (1)
                Vitest.expect(argumentTypes.[0]).toEqual (typeof<unit>)
                Vitest.expect(returnType.FullName.Contains("GitOperationResult")).toBe (true)
        )

)

Vitest.describe (
    "Git IPC provisioning contract reflection",
    fun () ->
        Vitest.test (
            "GitOperationResult exposes Path as string option",
            fun () ->
                let pathField = getRecordField typeof<GitOperationResult> "Path"
                Vitest.expect(pathField.PropertyType).toEqual (typeof<string option>)
        )

        Vitest.test (
            "GitOperationResult exposes warning fields for partial-success outcomes",
            fun () ->
                Vitest
                    .expect((getRecordField typeof<GitOperationResult> "WarningMessage").PropertyType.FullName)
                    .toBe (typeof<string option>.FullName)

                Vitest
                    .expect((getRecordField typeof<GitOperationResult> "WarningKind").PropertyType.FullName)
                    .toBe (typeof<GitFailureKind option>.FullName)
        )

        Vitest.test (
            "IGitApi.gitInitRepository uses target path string argument without IpcMainEvent",
            fun () ->
                let initField = getRecordField typeof<IGitApi> "gitInitRepository"
                let argumentTypes, _ = flattenFunctionSignature initField.PropertyType

                Vitest.expect(argumentTypes.Length).toBe (1)
                Vitest.expect(argumentTypes.[0]).toEqual (typeof<string>)
        )

        Vitest.test (
            "IGitApi.gitCloneRepository uses GitCloneRepositoryRequest argument without IpcMainEvent",
            fun () ->
                let cloneField = getRecordField typeof<IGitApi> "gitCloneRepository"
                let argumentTypes, _ = flattenFunctionSignature cloneField.PropertyType

                Vitest.expect(argumentTypes.Length).toBe (1)
                Vitest.expect(argumentTypes.[0].FullName).toBe (typeof<GitCloneRepositoryRequest>.FullName)
        )

        Vitest.test (
            "IGitApi.gitDiscardPaths uses GitPathspecRequest without IpcMainEvent",
            fun () ->
                let discardField = getRecordField typeof<IGitApi> "gitDiscardPaths"
                let argumentTypes, returnType = flattenFunctionSignature discardField.PropertyType

                Vitest.expect(argumentTypes.Length).toBe (1)
                Vitest.expect(argumentTypes.[0].FullName).toBe (typeof<GitPathspecRequest>.FullName)
                Vitest.expect(returnType.FullName.Contains("GitOperationResult")).toBe (true)
        )

        Vitest.test (
            "IGitApi.getOriginRepositoryWebUrl uses a typed no-payload endpoint",
            fun () ->
                let originRemoteField = getRecordField typeof<IGitApi> "getOriginRepositoryWebUrl"

                let argumentTypes, returnType =
                    flattenFunctionSignature originRemoteField.PropertyType

                let returnTypeName = returnType.FullName

                Vitest.expect(argumentTypes.Length).toBe (1)
                Vitest.expect(argumentTypes.[0]).toEqual (typeof<unit>)
                Vitest.expect(returnTypeName.Contains("FSharpResult")).toBe (true)
                Vitest.expect(returnTypeName.Contains("FSharpOption")).toBe (true)
                Vitest.expect(returnTypeName.Contains("System.String")).toBe (true)
        )

        Vitest.test (
            "GitCloneRepositoryRequest fields match expected schema",
            fun () ->
                let fields = FSharpType.GetRecordFields(typeof<GitCloneRepositoryRequest>)

                let fieldNames =
                    fields
                    |> Microsoft.FSharp.Collections.Array.map (fun field -> field.Name)
                    |> Microsoft.FSharp.Collections.Array.sort

                let expectedNames = [|
                    "Branch"
                    "DownloadLargeFiles"
                    "RemoteUrl"
                    "TargetPath"
                |]

                Vitest.expect(fieldNames).toEqual (expectedNames)

                Vitest
                    .expect((getRecordField typeof<GitCloneRepositoryRequest> "RemoteUrl").PropertyType)
                    .toEqual (typeof<string>)

                Vitest
                    .expect((getRecordField typeof<GitCloneRepositoryRequest> "TargetPath").PropertyType)
                    .toEqual (typeof<string>)

                Vitest
                    .expect((getRecordField typeof<GitCloneRepositoryRequest> "Branch").PropertyType)
                    .toEqual (typeof<string option>)

                Vitest
                    .expect((getRecordField typeof<GitCloneRepositoryRequest> "DownloadLargeFiles").PropertyType)
                    .toEqual (typeof<bool>)
        )

        Vitest.test (
            "GitConfirmMergeResolutionRequest exposes AutoCommit as a bool",
            fun () ->
                let fields = FSharpType.GetRecordFields(typeof<GitConfirmMergeResolutionRequest>)

                let fieldNames =
                    fields
                    |> Microsoft.FSharp.Collections.Array.map (fun field -> field.Name)
                    |> Microsoft.FSharp.Collections.Array.sort

                Vitest
                    .expect(fieldNames)
                    .toEqual (
                        [|
                            "AutoCommit"
                            "ExpectedConflictContent"
                            "Path"
                            "ResolvedContent"
                        |]
                    )

                Vitest
                    .expect((getRecordField typeof<GitConfirmMergeResolutionRequest> "AutoCommit").PropertyType)
                    .toEqual (typeof<bool>)
        )
)
