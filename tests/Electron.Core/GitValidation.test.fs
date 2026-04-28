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
open Swate.Components.Authentication.Types
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

let private fsPromisesDynamic: obj = importAll "fs/promises"

let private simpleGitSourcePath =
    join [|
        ".."
        ".."
        "src"
        "Electron"
        "src"
        "Main"
        "Bindings"
        "SimpleGit.fs"
    |]

let private gitApiClientSourcePath =
    join [|
        ".."
        ".."
        "src"
        "Electron"
        "src"
        "Renderer"
        "GitApiClient.fs"
    |]

let private gitStateCtxSourcePath =
    join [|
        ".."
        ".."
        "src"
        "Electron"
        "src"
        "Renderer"
        "Context"
        "GitStateContext.fs"
    |]

let private gitProvisioningServiceSourcePath =
    join [|
        ".."
        ".."
        "src"
        "Electron"
        "src"
        "Main"
        "Git"
        "GitProvisioningService.fs"
    |]

let private gitMergeConflictTargetSourcePath =
    join [|
        ".."
        ".."
        "src"
        "Electron"
        "src"
        "Renderer"
        "Components"
        "MainContent"
        "GitMergeConflictTarget.fs"
    |]

let mutable private simpleGitSourceText: string option = None
let mutable private gitApiClientSourceText: string option = None
let mutable private gitStateCtxSourceText: string option = None
let mutable private gitProvisioningServiceSourceText: string option = None
let mutable private gitMergeConflictTargetSourceText: string option = None

let private readUtf8FileAsync (path: string) : JS.Promise<string> = promise {
    let! text = fsPromisesDynamic?readFile (path, "utf8") |> unbox<JS.Promise<string>>
    return text
}

let private getSimpleGitSource () =
    simpleGitSourceText
    |> Option.defaultWith (fun () -> failwith "Expected SimpleGit source text to be loaded before running tests.")

let private getGitApiClientSource () =
    gitApiClientSourceText
    |> Option.defaultWith (fun () -> failwith "Expected GitApiClient source text to be loaded before running tests.")

let private getGitStateCtxSource () =
    gitStateCtxSourceText
    |> Option.defaultWith (fun () -> failwith "Expected GitStateCtx source text to be loaded before running tests.")

let private getGitProvisioningServiceSource () =
    gitProvisioningServiceSourceText
    |> Option.defaultWith (fun () ->
        failwith "Expected GitProvisioningService source text to be loaded before running tests."
    )

let private getGitMergeConflictTargetSource () =
    gitMergeConflictTargetSourceText
    |> Option.defaultWith (fun () ->
        failwith "Expected GitMergeConflictTarget source text to be loaded before running tests."
    )

let private extractSingleLineBlock (startPattern: string) (endPattern: string) (text: string) =
    let blockMatch =
        Regex.Match(text, $"{startPattern}(.*?){endPattern}", RegexOptions.Singleline ||| RegexOptions.Multiline)

    if not blockMatch.Success then
        failwith $"Could not extract source block between '{startPattern}' and '{endPattern}'."

    blockMatch.Groups.[1].Value

let private expectSourceContains (sourceText: string) (snippet: string) =
    Vitest.expect(sourceText.Contains(snippet), $"Expected source to contain: {snippet}").toBe (true)

let private expectSourceNotContains (sourceText: string) (snippet: string) =
    Vitest.expect(sourceText.Contains(snippet), $"Expected source not to contain: {snippet}").toBe (false)

Vitest.beforeAll (fun () -> promise {
    let! sourceText = readUtf8FileAsync simpleGitSourcePath
    let! gitApiClientText = readUtf8FileAsync gitApiClientSourcePath
    let! gitStateCtxText = readUtf8FileAsync gitStateCtxSourcePath
    let! gitProvisioningServiceText = readUtf8FileAsync gitProvisioningServiceSourcePath
    let! gitMergeConflictTargetText = readUtf8FileAsync gitMergeConflictTargetSourcePath
    simpleGitSourceText <- Some sourceText
    gitApiClientSourceText <- Some gitApiClientText
    gitStateCtxSourceText <- Some gitStateCtxText
    gitProvisioningServiceSourceText <- Some gitProvisioningServiceText
    gitMergeConflictTargetSourceText <- Some gitMergeConflictTargetText
})

Vitest.describe (
    "GitService.classifyFailureKind",
    fun () ->
        let cases = [|
            "maps unauthorized message", "authentication failed for remote", GitFailureKind.Unauthorized
            "maps forbidden message", "403 Forbidden", GitFailureKind.Forbidden
            "maps network message", "could not resolve host github.com", GitFailureKind.Network
            "maps timeout message", "operation timed out", GitFailureKind.Timeout
            "maps canceled message", "AbortError: signal aborted", GitFailureKind.Canceled
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
                        AccountId = "acc-1"
                        Name = "Invalid User"
                        Email = "invalid@example.org"
                        AvatarUrl = "https://example.org/avatar.png"
                        TargetDataHub = "https://git.nfdi4plants.org/"
                    }
                    DateAdded = "2026-01-01T00:00:00.0000000Z"
                    TokenInvalid = true
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
            "nextTokenInvalidState marks unauthorized and forbidden failures as invalid",
            fun () ->
                Vitest.expect(AuthService.nextTokenInvalidState false AuthFailureKind.Unauthorized).toBe (true)
                Vitest.expect(AuthService.nextTokenInvalidState false AuthFailureKind.Forbidden).toBe (true)
        )

        Vitest.test (
            "nextTokenInvalidState preserves the existing invalid flag for non-auth failures",
            fun () ->
                Vitest.expect(AuthService.nextTokenInvalidState false AuthFailureKind.Network).toBe (false)
                Vitest.expect(AuthService.nextTokenInvalidState true AuthFailureKind.Network).toBe (true)
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
            "clone auth fallback removal",
            fun () ->
                Vitest.test (
                    "removes the unauthenticated clone retry helpers from provisioning",
                    fun () ->
                        let sourceText = getGitProvisioningServiceSource ()

                        expectSourceNotContains
                            sourceText
                            "let shouldRetryWithoutAuth (failure: GitService.GitFailure) ="

                        expectSourceNotContains
                            sourceText
                            "let validateCloneRetryCleanupEntries (entries: string[]) : Result<string option, exn> ="

                        expectSourceNotContains sourceText "let private cleanupCloneTargetForRetry"
                )

                Vitest.test (
                    "does not retry authenticated clone failures without auth",
                    fun () ->
                        let sourceText = getGitProvisioningServiceSource ()
                        expectSourceNotContains sourceText "| Error failure when shouldRetryWithoutAuth failure ->"
                        expectSourceNotContains sourceText "cleanupCloneTargetForRetry normalizedTargetPath"

                        expectSourceContains
                            sourceText
                            "return! hydrateIfRequested (Some token) authenticatedCloneResult"
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

        Vitest.test (
            "GitApiClient calls no-payload endpoints without unbox null",
            fun () ->
                let sourceText = getGitApiClientSource ()
                expectSourceContains sourceText "gitApi.getGitStatus ()"
                expectSourceContains sourceText "gitApi.getGitBranches ()"
                expectSourceContains sourceText "gitApi.getGitLfsSettings ()"
                expectSourceContains sourceText "gitApi.installGitLfs ()"
                expectSourceNotContains sourceText "unbox null"
        )

        Vitest.test (
            "GitApiClient no longer depends on unsupported-content message text",
            fun () ->
                let sourceText = getGitApiClientSource ()
                Vitest.expect(sourceText.Contains("Unsupported git content for '")).toBe (false)
        )

        Vitest.test (
            "GitStateCtx uses the typed git client and no longer uses dynamic IPC dispatch",
            fun () ->
                let sourceText = getGitStateCtxSource ()
                expectSourceContains sourceText "React.useElmish"
                Vitest.expect(sourceText.Contains("ipcGitApiDynamic")).toBe (false)
                Vitest.expect(sourceText.Contains("invokeGitApiWithoutPayload")).toBe (false)
                Vitest.expect(sourceText.Contains("invokeGitApiWithPayload")).toBe (false)
                Vitest.expect(sourceText.Contains("Api.ipcArcVaultApi.runGitLfs")).toBe (false)
        )

        Vitest.test (
            "GitMergeConflictTarget does not keep a sticky local merge-confirm latch",
            fun () ->
                let sourceText = getGitMergeConflictTargetSource ()
                Vitest.expect(sourceText.Contains("React.useState")).toBe (false)
                Vitest.expect(sourceText.Contains("React.useRef")).toBe (false)
                expectSourceContains sourceText "gitStateCtx.state.MergeResolutionPendingPath = Some mergeData.Path"
        )

        Vitest.test (
            "GitMergeConflictTarget opts into auto-commit after the final conflict is resolved",
            fun () ->
                let sourceText = getGitMergeConflictTargetSource ()
                expectSourceContains sourceText "AutoCommit = true"
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

Vitest.describe (
    "SimpleGit binding surface parity",
    fun () ->
        Vitest.test (
            "ISimpleGit contains expected upstream method names",
            fun () ->
                let simpleGitSource = getSimpleGitSource ()

                let interfaceBlock =
                    extractSingleLineBlock "type ISimpleGit =" "\\[<Erase>\\]\\s*type SimpleGit =" simpleGitSource

                let actualMethodNames =
                    Regex.Matches(interfaceBlock, "abstract member\\s+([A-Za-z0-9_]+):")
                    |> Seq.cast<Match>
                    |> Seq.map (fun entry -> entry.Groups.[1].Value)
                    |> Set.ofSeq

                let expectedMethodNames =
                    [|
                        "add"
                        "cwd"
                        "hashObject"
                        "init"
                        "merge"
                        "mergeFromTo"
                        "outputHandler"
                        "push"
                        "stash"
                        "status"
                        "addAnnotatedTag"
                        "addConfig"
                        "applyPatch"
                        "listConfig"
                        "addRemote"
                        "addTag"
                        "binaryCatFile"
                        "branch"
                        "branchLocal"
                        "catFile"
                        "checkIgnore"
                        "checkIsRepo"
                        "checkout"
                        "checkoutBranch"
                        "checkoutLatestTag"
                        "checkoutLocalBranch"
                        "clean"
                        "clearQueue"
                        "clone"
                        "commit"
                        "countObjects"
                        "customBinary"
                        "deleteLocalBranch"
                        "deleteLocalBranches"
                        "diff"
                        "diffSummary"
                        "env"
                        "exec"
                        "fetch"
                        "firstCommit"
                        "getConfig"
                        "getRemotes"
                        "grep"
                        "listRemote"
                        "log"
                        "mirror"
                        "mv"
                        "pull"
                        "pushTags"
                        "raw"
                        "rebase"
                        "remote"
                        "removeRemote"
                        "reset"
                        "revert"
                        "revparse"
                        "rm"
                        "rmKeepLocal"
                        "show"
                        "showBuffer"
                        "silent"
                        "stashList"
                        "subModule"
                        "submoduleAdd"
                        "submoduleInit"
                        "submoduleUpdate"
                        "tag"
                        "tags"
                        "updateServerInfo"
                        "version"
                    |]
                    |> Set.ofArray

                let missing =
                    Set.difference expectedMethodNames actualMethodNames
                    |> Set.toArray
                    |> Microsoft.FSharp.Collections.Array.sort

                Vitest.expect(missing).toEqual ([||])
        )

        Vitest.test (
            "SimpleGitOptions exposes required option properties including unsafe",
            fun () ->
                let simpleGitSource = getSimpleGitSource ()

                let optionsBlock =
                    extractSingleLineBlock "type SimpleGitOptions" "type ISimpleGit =" simpleGitSource

                let optionPropertyNames =
                    Regex.Matches(optionsBlock, "member val\\s+(``unsafe``|[A-Za-z0-9_]+):")
                    |> Seq.cast<Match>
                    |> Seq.map (fun entry -> entry.Groups.[1].Value.Replace("``", ""))
                    |> Set.ofSeq

                let expectedOptionNames =
                    [|
                        "baseDir"
                        "binary"
                        "maxConcurrentProcesses"
                        "trimmed"
                        "config"
                        "abort"
                        "progress"
                        "errors"
                        "completion"
                        "timeout"
                        "spawnOptions"
                        "unsafe"
                    |]
                    |> Set.ofArray

                let missing =
                    Set.difference expectedOptionNames optionPropertyNames
                    |> Set.toArray
                    |> Microsoft.FSharp.Collections.Array.sort

                Vitest.expect(missing).toEqual ([||])
        )

        Vitest.test (
            "SimpleGitOptions abort uses AbortSignal type",
            fun () ->
                let optionsBlock =
                    extractSingleLineBlock "type SimpleGitOptions" "type ISimpleGit =" (getSimpleGitSource ())

                expectSourceContains optionsBlock "member val abort: IAbortSignal option"
        )

        Vitest.test (
            "SimpleGitOptions binary and errors plugin options are typed",
            fun () ->
                let optionsBlock =
                    extractSingleLineBlock "type SimpleGitOptions" "type ISimpleGit =" (getSimpleGitSource ())

                expectSourceContains optionsBlock "member val binary: SimpleGitBinary option"
                expectSourceContains optionsBlock "member val errors: SimpleGitErrorsHandler option"
        )

        Vitest.test (
            "SimpleGitOptions completion and spawn options are typed",
            fun () ->
                let optionsBlock =
                    extractSingleLineBlock "type SimpleGitOptions" "type ISimpleGit =" (getSimpleGitSource ())

                expectSourceContains optionsBlock "member val completion: SimpleGitCompletionOptions option"
                expectSourceContains optionsBlock "member val spawnOptions: SimpleGitSpawnOptions option"
        )

        Vitest.test (
            "SimpleGitProgressEvent uses required non-optional fields",
            fun () ->
                let progressBlock =
                    extractSingleLineBlock
                        "type SimpleGitProgressEvent ="
                        "type SimpleGitProgressHandler"
                        (getSimpleGitSource ())

                for snippet in
                    [|
                        "abstract member method: string"
                        "abstract member stage: string"
                        "abstract member progress: float"
                        "abstract member processed: float"
                        "abstract member total: float"
                    |] do
                    expectSourceContains progressBlock snippet
        )

        Vitest.test (
            "DiffResult includes the name-status file variant",
            fun () ->
                expectSourceContains
                    (getSimpleGitSource ())
                    "abstract member files: U3<DiffResultTextFile, DiffResultBinaryFile, DiffResultNameStatusFile>[]"
        )

        Vitest.test (
            "PullResult includes required summary and created/deleted fields",
            fun () ->
                let simpleGitSource = getSimpleGitSource ()

                for snippet in
                    [|
                        "abstract member summary: PullDetailSummary"
                        "abstract member created: string[]"
                        "abstract member deleted: string[]"
                        "abstract member remoteMessages: RemoteMessages"
                    |] do
                    expectSourceContains simpleGitSource snippet
        )

        Vitest.test (
            "DiffResultNameStatusFile optionality matches upstream",
            fun () ->
                let simpleGitSource = getSimpleGitSource ()
                expectSourceContains simpleGitSource "abstract member status: string option"
                expectSourceContains simpleGitSource "abstract member similarity: float"
        )

        Vitest.test (
            "High-impact result fields are non-optional",
            fun () ->
                let simpleGitSource = getSimpleGitSource ()

                for snippet in
                    [|
                        "abstract member branch: string"
                        "abstract member commit: string"
                        "abstract member summary: CommitResultSummary"
                        "abstract member major: int"
                        "abstract member minor: int"
                        "abstract member patch: U2<int, string>"
                        "abstract member agent: string"
                        "abstract member installed: bool"
                        "abstract member count: int"
                        "abstract member size: int"
                        "abstract member inPack: int"
                        "abstract member packs: int"
                        "abstract member sizePack: int"
                        "abstract member prunePackable: int"
                        "abstract member garbage: int"
                        "abstract member sizeGarbage: int"
                    |] do
                    expectSourceContains simpleGitSource snippet
        )

        Vitest.test (
            "Branch summary and branch delete result shapes are strongly typed",
            fun () ->
                let simpleGitSource = getSimpleGitSource ()

                for snippet in
                    [|
                        "abstract member current: string"
                        "abstract member branches: System.Collections.Generic.Dictionary<string, BranchSummaryBranch>"
                        "abstract member branch: string"
                        "abstract member hash: string"
                        "abstract member hash: obj"
                        "abstract member all: BranchSingleDeleteResult[]"
                        "abstract member errors: BranchSingleDeleteResult[]"
                        "abstract member branches: System.Collections.Generic.Dictionary<string, BranchSingleDeleteResult>"
                    |] do
                    expectSourceContains simpleGitSource snippet
        )

        Vitest.test (
            "Fetch, push, grep and log response models are typed without obj option placeholders",
            fun () ->
                let simpleGitSource = getSimpleGitSource ()

                for snippet in
                    [|
                        "abstract member paths: string[]"
                        "abstract member files: string[]"
                        "abstract member folders: string[]"
                        "abstract member email: string"
                        "abstract member name: string"
                        "abstract member reason: string"
                        "abstract member branches: FetchResultBranch[]"
                        "abstract member tags: FetchResultBranch[]"
                        "abstract member updated: FetchResultUpdate[]"
                        "abstract member deleted: FetchResultDeleted[]"
                        "abstract member pushed: PushResultPushedItem[]"
                        "abstract member ref: PushResultRef option"
                        "abstract member branch: PushResultBranch option"
                        "abstract member update: PushResultBranchUpdate option"
                        "abstract member remoteMessages: PushResultRemoteMessages"
                        "abstract member paths: System.Collections.Generic.HashSet<string>"
                        "abstract member results: System.Collections.Generic.Dictionary<string, GrepResultLine[]>"
                        "abstract member all: LogResultLine[]"
                        "abstract member latest: LogResultLine option"
                        "abstract member insertions: PullDetailFileChanges"
                        "abstract member deletions: PullDetailFileChanges"
                    |] do
                    expectSourceContains simpleGitSource snippet
        )

        Vitest.test (
            "Residual required fields stay non-optional and typed",
            fun () ->
                let simpleGitSource = getSimpleGitSource ()

                for snippet in
                    [|
                        "abstract member path: string"
                        "abstract member gitDir: string"
                        "abstract member key: string"
                        "abstract member values: string[]"
                        "abstract member paths: string[]"
                        "abstract member scopes: ConfigScopes"
                        "abstract member all: ConfigValues"
                        "abstract member files: string[]"
                        "abstract member values: ConfigFileValues"
                        "abstract member conflicts: MergeConflict[]"
                        "abstract member merges: string[]"
                        "abstract member result: string"
                        "abstract member name: string"
                        "abstract member refs: RemoteRefs"
                        "abstract member moves: MoveResultItem[]"
                    |] do
                    expectSourceContains simpleGitSource snippet
        )

        Vitest.test (
            "clearQueue is marked deprecated in binding",
            fun () ->
                let simpleGitSource = getSimpleGitSource ()

                expectSourceContains
                    simpleGitSource
                    "[<System.Obsolete(\"Deprecated upstream. Removed in v2; prefer abort-plugin configuration for pending task cancellation.\")>]"

                expectSourceContains simpleGitSource "abstract member clearQueue: unit -> ISimpleGit"
        )

        Vitest.test (
            "SimpleGit factory exposes create overloads",
            fun () ->
                let createMethodCount =
                    Regex.Matches(getSimpleGitSource (), "static member create\\b").Count

                Vitest.expect(createMethodCount).toBeGreaterThanOrEqual (4)
        )
)