module GitValidationTests

open System
open Expecto
open Fable.Core
open Main.Git
open Main.Bindings.SimpleGit

let private expectOk expected (result: Result<'a, exn>) message =
    match result with
    | Ok actual -> Expect.equal actual expected message
    | Error error -> failtest $"Expected Ok but got Error: {error.Message}"

let private expectError (result: Result<'a, exn>) message =
    match result with
    | Ok value -> failtest $"Expected Error but got Ok: {value}"
    | Error _ -> ()

let classifyFailureKindTests =
    testList "classifyFailureKind" [
        testCase "maps unauthorized message" <| fun _ ->
            Expect.equal
                (GitService.classifyFailureKind "authentication failed for remote")
                GitService.GitFailureKind.Unauthorized
                "Authentication failures should classify as unauthorized."

        testCase "maps forbidden message" <| fun _ ->
            Expect.equal (GitService.classifyFailureKind "403 Forbidden") GitService.GitFailureKind.Forbidden "403 should classify as forbidden."

        testCase "maps network message" <| fun _ ->
            Expect.equal
                (GitService.classifyFailureKind "could not resolve host github.com")
                GitService.GitFailureKind.Network
                "Host resolution failures should classify as network."

        testCase "maps timeout message" <| fun _ ->
            Expect.equal (GitService.classifyFailureKind "operation timed out") GitService.GitFailureKind.Timeout "Timeout text should classify as timeout."

        testCase "maps canceled message" <| fun _ ->
            Expect.equal
                (GitService.classifyFailureKind "AbortError: signal aborted")
                GitService.GitFailureKind.Canceled
                "Abort text should classify as canceled."

        testCase "falls back to unknown message" <| fun _ ->
            Expect.equal (GitService.classifyFailureKind "something unexpected") GitService.GitFailureKind.Unknown "Unmapped text should classify as unknown."

        testCase "keeps unauthorized priority over network indicators" <| fun _ ->
            Expect.equal
                (GitService.classifyFailureKind "connection refused, could not read username")
                GitService.GitFailureKind.Unauthorized
                "Authentication indicators should win over network indicators."

        testCase "keeps timeout priority over authentication indicators" <| fun _ ->
            Expect.equal
                (GitService.classifyFailureKind "timeout while authentication failed")
                GitService.GitFailureKind.Timeout
                "Timeout should win when timeout and authentication indicators are both present."

        testCase "maps permission denied publickey to unauthorized" <| fun _ ->
            Expect.equal
                (GitService.classifyFailureKind "Permission denied (publickey).")
                GitService.GitFailureKind.Unauthorized
                "SSH authentication failures should classify as unauthorized."

        testCase "keeps forbidden precedence when 403 and auth text are mixed" <| fun _ ->
            Expect.equal
                (GitService.classifyFailureKind "403 Forbidden: could not read username")
                GitService.GitFailureKind.Forbidden
                "403 should classify as forbidden even when auth text is present."
    ]

let branchValidationTests =
    testList "ensureValidBranchLikeName" [
        testCase "accepts main" <| fun _ ->
            GitService.ensureValidBranchLikeName "Branch name" "main"
            |> expectOk "main" <| "main should be valid."

        testCase "accepts nested branch" <| fun _ ->
            GitService.ensureValidBranchLikeName "Branch name" "feature/x"
            |> expectOk "feature/x" <| "feature/x should be valid."

        testCase "rejects empty branch" <| fun _ ->
            GitService.ensureValidBranchLikeName "Branch name" ""
            |> expectError <| "Empty branch should be rejected."

        testCase "rejects leading dash" <| fun _ ->
            GitService.ensureValidBranchLikeName "Branch name" "-flag"
            |> expectError <| "Leading dash should be rejected."

        testCase "rejects double-dot sequence" <| fun _ ->
            GitService.ensureValidBranchLikeName "Branch name" "a..b"
            |> expectError <| "Double-dot should be rejected."

        testCase "rejects tilde" <| fun _ ->
            GitService.ensureValidBranchLikeName "Branch name" "a~b"
            |> expectError <| "Tilde should be rejected."

        testCase "rejects whitespace" <| fun _ ->
            GitService.ensureValidBranchLikeName "Branch name" "has space"
            |> expectError <| "Whitespace should be rejected."

        testCase "rejects trailing dot" <| fun _ ->
            GitService.ensureValidBranchLikeName "Branch name" "ok."
            |> expectError <| "Trailing dot should be rejected."

        testCase "rejects @{ sequence" <| fun _ ->
            GitService.ensureValidBranchLikeName "Branch name" "a@{b"
            |> expectError <| "@{ should be rejected."

        testCase "rejects leading slash" <| fun _ ->
            GitService.ensureValidBranchLikeName "Branch name" "/feature"
            |> expectError <| "Leading slash should be rejected."

        testCase "rejects .lock suffix segment" <| fun _ ->
            GitService.ensureValidBranchLikeName "Branch name" "feature.lock"
            |> expectError <| ".lock branch suffix should be rejected."

        testCase "rejects backslash character explicitly" <| fun _ ->
            GitService.ensureValidBranchLikeName "Branch name" "feature\\test"
            |> expectError <| "Backslash should be rejected."

        testCase "rejects control characters" <| fun _ ->
            GitService.ensureValidBranchLikeName "Branch name" "feature\u0007test"
            |> expectError <| "ASCII control characters should be rejected."
    ]

let pathspecValidationTests =
    testList "ensureValidPathspec" [
        testCase "accepts relative path" <| fun _ ->
            GitService.ensureValidPathspec "src/file.txt"
            |> expectOk "src/file.txt" <| "Relative pathspec should be accepted."

        testCase "rejects empty pathspec" <| fun _ ->
            GitService.ensureValidPathspec ""
            |> expectError <| "Empty pathspec should be rejected."

        testCase "rejects unix absolute path" <| fun _ ->
            GitService.ensureValidPathspec "/etc/passwd"
            |> expectError <| "Unix absolute path should be rejected."

        testCase "rejects windows absolute path" <| fun _ ->
            GitService.ensureValidPathspec "C:/Windows/file"
            |> expectError <| "Windows absolute path should be rejected."

        testCase "rejects traversal path" <| fun _ ->
            GitService.ensureValidPathspec "../escape"
            |> expectError <| "Traversal segments should be rejected."

        testCase "rejects dot segment path" <| fun _ ->
            GitService.ensureValidPathspec "a/./b"
            |> expectError <| "Dot segments should be rejected."

        testCase "accepts unicode pathspec" <| fun _ ->
            GitService.ensureValidPathspec "src/\u00FCber-\u03B2.txt"
            |> expectOk "src/\u00FCber-\u03B2.txt" <| "Unicode pathspec should be accepted."

        testCase "accepts very long pathspec" <| fun _ ->
            let longSegment = String.replicate 280 "a"
            let longPath = $"src/{longSegment}.txt"

            GitService.ensureValidPathspec longPath
            |> expectOk longPath <| "Long pathspec should be accepted when otherwise valid."

        testCase "normalizes windows backslashes" <| fun _ ->
            GitService.ensureValidPathspec "src\\file.txt"
            |> expectOk "src/file.txt" <| "Backslashes should be normalized to forward slashes."

        testCase "rejects pathspec with null character at index 0" <| fun _ ->
            GitService.ensureValidPathspec "\u0000src/file.txt"
            |> expectError <| "Null bytes should be rejected regardless of position."
    ]

let remoteUrlValidationTests =
    testList "ensureAllowedRemoteUrl" [
        testCase "accepts https remote" <| fun _ ->
            GitService.ensureAllowedRemoteUrl "https://github.com/user/repo"
            |> expectOk "https://github.com/user/repo" <| "HTTPS remote should be allowed."

        testCase "accepts ssh remote" <| fun _ ->
            GitService.ensureAllowedRemoteUrl "ssh://git@github.com/repo"
            |> expectOk "ssh://git@github.com/repo" <| "SSH remote should be allowed."

        testCase "rejects file protocol" <| fun _ ->
            GitService.ensureAllowedRemoteUrl "file:///local"
            |> expectError <| "file:// should be blocked."

        testCase "rejects ext protocol" <| fun _ ->
            GitService.ensureAllowedRemoteUrl "ext::ssh -o ProxyCommand"
            |> expectError <| "ext:: should be blocked."

        testCase "rejects git protocol" <| fun _ ->
            GitService.ensureAllowedRemoteUrl "git://host/repo"
            |> expectError <| "git:// should be blocked."

        testCase "rejects empty URL" <| fun _ ->
            GitService.ensureAllowedRemoteUrl ""
            |> expectError <| "Empty URL should be blocked."

        testCase "rejects protocol override attempt" <| fun _ ->
            GitService.ensureAllowedRemoteUrl "https://github.com/repo -c protocol.file.allow=always"
            |> expectError <| "Protocol override attempt should be blocked."
    ]

let redactionTests =
    testList "redactToken and redactArgs" [
        testCase "redacts bearer token" <| fun _ ->
            Expect.equal
                (GitAuthAdapter.redactToken "Authorization: Bearer abc123")
                "Authorization: Bearer [REDACTED]"
                "Bearer token should be redacted."

        testCase "redacts lowercase bearer token" <| fun _ ->
            Expect.equal
                (GitAuthAdapter.redactToken "authorization:bearer XYZ")
                "authorization:bearer [REDACTED]"
                "Bearer token redaction should be case-insensitive."

        testCase "keeps clean message unchanged" <| fun _ ->
            Expect.equal (GitAuthAdapter.redactToken "clean message") "clean message" "Clean messages should be unchanged."

        testCase "keeps empty string unchanged" <| fun _ ->
            Expect.equal (GitAuthAdapter.redactToken "") "" "Empty text should be unchanged."

        testCase "keeps null unchanged" <| fun _ ->
            let result = GitAuthAdapter.redactToken null
            Expect.isTrue (obj.ReferenceEquals(result, null)) "Null input should remain null."

        testCase "redacts tokens in argument arrays" <| fun _ ->
            let redacted =
                GitAuthAdapter.redactArgs [|
                    "-c"
                    "http.extraHeader=Authorization: Bearer topsecret"
                |]

            Expect.equal redacted.[1] "http.extraHeader=Authorization: Bearer [REDACTED]" "Args should be redacted."

        testCase "redacts credential-bearing https URLs" <| fun _ ->
            let redacted = GitAuthAdapter.redactToken "fatal: unable to access 'https://ghp_abc123@github.com/user/repo.git/'"
            Expect.equal redacted "fatal: unable to access 'https://[REDACTED]@github.com/user/repo.git/'" "Credential values in URLs should be redacted."

        testCase "redacts multiple bearer tokens in one string" <| fun _ ->
            let redacted =
                GitAuthAdapter.redactToken "Authorization: Bearer a and Authorization: Bearer b"

            Expect.equal
                redacted
                "Authorization: Bearer [REDACTED] and Authorization: Bearer [REDACTED]"
                "All bearer tokens should be redacted."

        testCase "redacts bearer token at start and end of string" <| fun _ ->
            let startRedacted = GitAuthAdapter.redactToken "Authorization: Bearer abc123"
            let endRedacted = GitAuthAdapter.redactToken "prefix Authorization: Bearer xyz789"

            Expect.equal startRedacted "Authorization: Bearer [REDACTED]" "Token at start should be redacted."
            Expect.equal endRedacted "prefix Authorization: Bearer [REDACTED]" "Token at end should be redacted."

        testCase "redacts bearer tokens across multiple lines" <| fun _ ->
            let input =
                "line1 Authorization: Bearer first\nline2 Authorization: Bearer second"

            let expected =
                "line1 Authorization: Bearer [REDACTED]\nline2 Authorization: Bearer [REDACTED]"

            let redacted = GitAuthAdapter.redactToken input
            Expect.equal redacted expected "Tokens should be redacted on each line."

        testCase "does not redact partial authorization field names" <| fun _ ->
            let input = "Authorizatio: Bearer token123"
            let redacted = GitAuthAdapter.redactToken input
            Expect.equal redacted input "Only valid Authorization field names should be redacted."
    ]

let hostExtractionTests =
    testList "tryExtractHostFromRemoteUrl" [
        testCase "extracts github host from https URL" <| fun _ ->
            GitTokenProvider.tryExtractHostFromRemoteUrl "https://github.com/user/repo.git"
            |> expectOk "github.com" <| "Host should be extracted from https URL."

        testCase "extracts gitlab host from ssh URL" <| fun _ ->
            GitTokenProvider.tryExtractHostFromRemoteUrl "ssh://git@gitlab.com/repo"
            |> expectOk "gitlab.com" <| "Host should be extracted from ssh URL."

        testCase "rejects file URL" <| fun _ ->
            GitTokenProvider.tryExtractHostFromRemoteUrl "file:///local"
            |> expectError <| "file:// URL should be rejected."

        testCase "rejects empty URL" <| fun _ ->
            GitTokenProvider.tryExtractHostFromRemoteUrl ""
            |> expectError <| "Empty URL should be rejected."

        testCase "rejects http URL" <| fun _ ->
            GitTokenProvider.tryExtractHostFromRemoteUrl "http://host/repo"
            |> expectError <| "Only https:// and ssh:// URLs should be accepted."
    ]

let authConfigTests =
    testList "toConfigEntries and buildAuthArgs" [
        testCase "extracts one config entry after -c" <| fun _ ->
            let extracted = GitAuthAdapter.toConfigEntries [| "-c"; "key=val" |]
            Expect.equal extracted [| "key=val" |] "Single -c entry should be extracted."

        testCase "ignores plain args without -c" <| fun _ ->
            let extracted = GitAuthAdapter.toConfigEntries [| "plain" |]
            Expect.equal extracted [||] "Non -c args should not produce config entries."

        testCase "extracts multiple config entries" <| fun _ ->
            let extracted = GitAuthAdapter.toConfigEntries [| "-c"; "a=1"; "-c"; "b=2" |]
            Expect.equal extracted [| "a=1"; "b=2" |] "All -c config values should be extracted."

        testCase "buildAuthArgs returns -c header tuple" <| fun _ ->
            let args = GitAuthAdapter.buildAuthArgs "github.com" "abc123"
            Expect.equal args.[0] "-c" "First argument should be -c."
            Expect.equal args.[1] "http.extraHeader=Authorization: Bearer abc123" "Second argument should be bearer header config."
    ]

let remoteNameValidationTests =
    testList "validateRemoteName" [
        testCase "accepts origin" <| fun _ ->
            GitService.validateRemoteName "origin"
            |> expectOk "origin" <| "origin should be valid."

        testCase "defaults empty to origin" <| fun _ ->
            GitService.validateRemoteName ""
            |> expectOk "origin" <| "Empty value should default to origin."

        testCase "accepts upstream" <| fun _ ->
            GitService.validateRemoteName "upstream"
            |> expectOk "upstream" <| "upstream should be valid."

        testCase "rejects remote with spaces" <| fun _ ->
            GitService.validateRemoteName "bad remote"
            |> expectError <| "Remote names with spaces should be rejected."

        testCase "accepts mixed punctuation pattern" <| fun _ ->
            GitService.validateRemoteName "a/b.c-d_e"
            |> expectOk "a/b.c-d_e" <| "Allowed punctuation should be accepted."
    ]

let bindingSurfaceParityTests =
    testList "SimpleGit binding surface parity" [
        testCase "ISimpleGit contains expected upstream method names" <| fun _ ->
            let actualMethodNames =
                typeof<ISimpleGit>.GetMethods()
                |> Array.map _.Name
                |> Set.ofArray

            let expectedMethodNames =
                [
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
                ]
                |> Set.ofList

            let missing =
                expectedMethodNames
                |> Set.filter (fun name -> not (Set.contains name actualMethodNames))
                |> Set.toList

            Expect.equal missing [] "Expected no missing method names from ISimpleGit."

        testCase "SimpleGitOptions exposes required option properties including unsafe" <| fun _ ->
            let optionPropertyNames =
                typeof<SimpleGitOptions>.GetProperties()
                |> Array.map _.Name
                |> Set.ofArray

            let expectedOptionNames =
                [
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
                ]
                |> Set.ofList

            let missing =
                expectedOptionNames
                |> Set.filter (fun name -> not (Set.contains name optionPropertyNames))
                |> Set.toList

            Expect.equal missing [] "Expected SimpleGitOptions to expose all required properties."

        testCase "SimpleGitOptions abort uses AbortSignal type" <| fun _ ->
            let abortProperty = typeof<SimpleGitOptions>.GetProperty("abort")
            Expect.equal abortProperty.PropertyType typeof<IAbortSignal option> "Abort option should be typed as AbortSignal."

        testCase "SimpleGitOptions binary and errors plugin options are typed" <| fun _ ->
            let binaryProperty = typeof<SimpleGitOptions>.GetProperty("binary")
            let errorsProperty = typeof<SimpleGitOptions>.GetProperty("errors")

            Expect.equal binaryProperty.PropertyType typeof<SimpleGitBinary option> "Binary option should support string and tuple forms."
            Expect.equal errorsProperty.PropertyType typeof<SimpleGitErrorsHandler option> "Errors plugin callback should be exposed."

        testCase "SimpleGitOptions completion and spawn options are typed" <| fun _ ->
            let completionProperty = typeof<SimpleGitOptions>.GetProperty("completion")
            let spawnOptionsProperty = typeof<SimpleGitOptions>.GetProperty("spawnOptions")

            Expect.equal
                completionProperty.PropertyType
                typeof<SimpleGitCompletionOptions option>
                "Completion plugin options should be exposed on SimpleGitOptions."

            Expect.equal
                spawnOptionsProperty.PropertyType
                typeof<SimpleGitSpawnOptions option>
                "Spawn options should be exposed on SimpleGitOptions."

        testCase "SimpleGitProgressEvent uses required non-optional fields" <| fun _ ->
            let progressEventType = typeof<SimpleGitProgressEvent>
            let methodProperty = progressEventType.GetProperty("method")
            let stageProperty = progressEventType.GetProperty("stage")
            let progressProperty = progressEventType.GetProperty("progress")
            let processedProperty = progressEventType.GetProperty("processed")
            let totalProperty = progressEventType.GetProperty("total")

            Expect.equal methodProperty.PropertyType typeof<string> "method should be a required string."
            Expect.equal stageProperty.PropertyType typeof<string> "stage should be a required string."
            Expect.equal progressProperty.PropertyType typeof<float> "progress should be a required float."
            Expect.equal processedProperty.PropertyType typeof<float> "processed should be a required float."
            Expect.equal totalProperty.PropertyType typeof<float> "total should be a required float."

        testCase "DiffResult includes the name-status file variant" <| fun _ ->
            let filesProperty = typeof<DiffResult>.GetProperty("files")

            Expect.equal
                filesProperty.PropertyType
                typeof<U3<DiffResultTextFile, DiffResultBinaryFile, DiffResultNameStatusFile>[]>
                "DiffResult.files should include text, binary, and name-status file variants."

        testCase "PullResult includes required summary and created/deleted fields" <| fun _ ->
            let pullResultType = typeof<PullResult>
            let summaryProperty = pullResultType.GetProperty("summary")
            let createdProperty = pullResultType.GetProperty("created")
            let deletedProperty = pullResultType.GetProperty("deleted")
            let remoteMessagesProperty = pullResultType.GetProperty("remoteMessages")

            Expect.equal summaryProperty.PropertyType typeof<PullDetailSummary> "Pull summary should be required."
            Expect.equal createdProperty.PropertyType typeof<string[]> "PullResult.created should be available."
            Expect.equal deletedProperty.PropertyType typeof<string[]> "PullResult.deleted should be available."
            Expect.equal remoteMessagesProperty.PropertyType typeof<RemoteMessages> "PullResult.remoteMessages should be typed as RemoteMessages."

        testCase "DiffResultNameStatusFile optionality matches upstream" <| fun _ ->
            let nameStatusType = typeof<DiffResultNameStatusFile>
            let statusProperty = nameStatusType.GetProperty("status")
            let similarityProperty = nameStatusType.GetProperty("similarity")

            Expect.equal statusProperty.PropertyType typeof<string option> "DiffResultNameStatusFile.status should be optional."
            Expect.equal similarityProperty.PropertyType typeof<float> "DiffResultNameStatusFile.similarity should be required."

        testCase "High-impact result fields are non-optional" <| fun _ ->
            let commitResultType = typeof<CommitResult>
            let commitBranchProperty = commitResultType.GetProperty("branch")
            let commitProperty = commitResultType.GetProperty("commit")
            let commitSummaryProperty = commitResultType.GetProperty("summary")
            let versionType = typeof<VersionResult>
            let majorProperty = versionType.GetProperty("major")
            let minorProperty = versionType.GetProperty("minor")
            let patchProperty = versionType.GetProperty("patch")
            let agentProperty = versionType.GetProperty("agent")
            let installedProperty = versionType.GetProperty("installed")
            let countObjectsProperties =
                typeof<CountObjectsResult>.GetProperties()
                |> Array.filter (fun p -> p.Name <> "Type")

            Expect.equal commitBranchProperty.PropertyType typeof<string> "Commit branch should be required."
            Expect.equal commitProperty.PropertyType typeof<string> "Commit hash should be required."
            Expect.equal commitSummaryProperty.PropertyType typeof<CommitResultSummary> "Commit summary should be required."
            Expect.equal majorProperty.PropertyType typeof<int> "VersionResult.major should be required."
            Expect.equal minorProperty.PropertyType typeof<int> "VersionResult.minor should be required."
            Expect.equal patchProperty.PropertyType typeof<U2<int, string>> "VersionResult.patch should be number|string."
            Expect.equal agentProperty.PropertyType typeof<string> "VersionResult.agent should be required."
            Expect.equal installedProperty.PropertyType typeof<bool> "VersionResult.installed should be required."

            for property in countObjectsProperties do
                Expect.equal property.PropertyType typeof<int> $"CountObjects field '{property.Name}' should be required int."

        testCase "Branch summary and branch delete result shapes are strongly typed" <| fun _ ->
            let branchSummaryType = typeof<BranchSummary>
            let currentProperty = branchSummaryType.GetProperty("current")
            let branchesProperty = branchSummaryType.GetProperty("branches")

            let deleteSuccessType = typeof<BranchSingleDeleteSuccess>
            let deleteFailureType = typeof<BranchSingleDeleteFailure>
            let deleteBatchType = typeof<BranchMultiDeleteResult>

            Expect.equal currentProperty.PropertyType typeof<string> "BranchSummary.current should be required."
            Expect.equal
                branchesProperty.PropertyType
                typeof<System.Collections.Generic.Dictionary<string, BranchSummaryBranch>>
                "BranchSummary.branches should be typed as a dictionary."

            Expect.equal (deleteSuccessType.GetProperty("branch").PropertyType) typeof<string> "Delete success branch should be required."
            Expect.equal (deleteSuccessType.GetProperty("hash").PropertyType) typeof<string> "Delete success hash should be required."
            Expect.equal (deleteFailureType.GetProperty("branch").PropertyType) typeof<string> "Delete failure branch should be required."
            Expect.equal (deleteFailureType.GetProperty("hash").PropertyType) typeof<obj> "Delete failure hash should represent null payload."
            Expect.equal (deleteBatchType.GetProperty("all").PropertyType) typeof<BranchSingleDeleteResult[]> "Delete batch all should be required."
            Expect.equal (deleteBatchType.GetProperty("errors").PropertyType) typeof<BranchSingleDeleteResult[]> "Delete batch errors should be required."
            Expect.equal
                (deleteBatchType.GetProperty("branches").PropertyType)
                typeof<System.Collections.Generic.Dictionary<string, BranchSingleDeleteResult>>
                "Delete batch branches should be typed."

        testCase "Fetch, push, grep and log response models are typed without obj option placeholders" <| fun _ ->
            let fetchType = typeof<FetchResult>
            let pushType = typeof<PushResult>
            let grepType = typeof<GrepResult>
            let logType = typeof<LogResult>
            let cleanType = typeof<CleanSummary>
            let authorType = typeof<CommitResultAuthor>
            let mergeConflictType = typeof<MergeConflict>
            let pullType = typeof<PullResult>

            Expect.equal (cleanType.GetProperty("paths").PropertyType) typeof<string[]> "CleanSummary.paths should be required."
            Expect.equal (cleanType.GetProperty("files").PropertyType) typeof<string[]> "CleanSummary.files should be required."
            Expect.equal (cleanType.GetProperty("folders").PropertyType) typeof<string[]> "CleanSummary.folders should be required."
            Expect.equal (authorType.GetProperty("email").PropertyType) typeof<string> "CommitResultAuthor.email should be required."
            Expect.equal (authorType.GetProperty("name").PropertyType) typeof<string> "CommitResultAuthor.name should be required."
            Expect.equal (mergeConflictType.GetProperty("reason").PropertyType) typeof<string> "MergeConflict.reason should be required."

            Expect.equal (fetchType.GetProperty("branches").PropertyType) typeof<FetchResultBranch[]> "Fetch branches should be typed arrays."
            Expect.equal (fetchType.GetProperty("tags").PropertyType) typeof<FetchResultBranch[]> "Fetch tags should be typed arrays."
            Expect.equal (fetchType.GetProperty("updated").PropertyType) typeof<FetchResultUpdate[]> "Fetch updated should be typed arrays."
            Expect.equal (fetchType.GetProperty("deleted").PropertyType) typeof<FetchResultDeleted[]> "Fetch deleted should be typed arrays."

            Expect.equal (pushType.GetProperty("pushed").PropertyType) typeof<PushResultPushedItem[]> "Push pushed should be typed."
            Expect.equal (pushType.GetProperty("ref").PropertyType) typeof<PushResultRef option> "Push ref should be typed."
            Expect.equal (pushType.GetProperty("branch").PropertyType) typeof<PushResultBranch option> "Push branch should be typed."
            Expect.equal (pushType.GetProperty("update").PropertyType) typeof<PushResultBranchUpdate option> "Push update should be typed."
            Expect.equal (pushType.GetProperty("remoteMessages").PropertyType) typeof<PushResultRemoteMessages> "Push remote messages should be typed."

            Expect.equal
                (grepType.GetProperty("paths").PropertyType)
                typeof<System.Collections.Generic.HashSet<string>>
                "Grep paths should be typed as a set-like collection."
            Expect.equal
                (grepType.GetProperty("results").PropertyType)
                typeof<System.Collections.Generic.Dictionary<string, GrepResultLine[]>>
                "Grep results should be typed."

            Expect.equal (logType.GetProperty("all").PropertyType) typeof<LogResultLine[]> "Log all entries should be typed and required."
            Expect.equal (logType.GetProperty("latest").PropertyType) typeof<LogResultLine option> "Log latest should be typed."
            Expect.equal (pullType.GetProperty("insertions").PropertyType) typeof<PullDetailFileChanges> "Pull insertions should be typed."
            Expect.equal (pullType.GetProperty("deletions").PropertyType) typeof<PullDetailFileChanges> "Pull deletions should be typed."

        testCase "Residual required fields stay non-optional and typed" <| fun _ ->
            let initType = typeof<InitResult>
            let configGetType = typeof<ConfigGetResult>
            let configListType = typeof<ConfigListSummary>
            let mergeType = typeof<MergeResult>
            let remoteWithoutRefsType = typeof<RemoteWithoutRefs>
            let remoteWithRefsType = typeof<RemoteWithRefs>
            let moveType = typeof<MoveResult>

            Expect.equal (initType.GetProperty("path").PropertyType) typeof<string> "InitResult.path should be required."
            Expect.equal (initType.GetProperty("gitDir").PropertyType) typeof<string> "InitResult.gitDir should be required."

            Expect.equal (configGetType.GetProperty("key").PropertyType) typeof<string> "ConfigGetResult.key should be required."
            Expect.equal (configGetType.GetProperty("values").PropertyType) typeof<string[]> "ConfigGetResult.values should be required."
            Expect.equal (configGetType.GetProperty("paths").PropertyType) typeof<string[]> "ConfigGetResult.paths should be required."
            Expect.equal (configGetType.GetProperty("scopes").PropertyType) typeof<ConfigScopes> "ConfigGetResult.scopes should be typed."

            Expect.equal (configListType.GetProperty("all").PropertyType) typeof<ConfigValues> "ConfigListSummary.all should be typed."
            Expect.equal (configListType.GetProperty("files").PropertyType) typeof<string[]> "ConfigListSummary.files should be required."
            Expect.equal (configListType.GetProperty("values").PropertyType) typeof<ConfigFileValues> "ConfigListSummary.values should be typed."

            Expect.equal (mergeType.GetProperty("conflicts").PropertyType) typeof<MergeConflict[]> "MergeResult.conflicts should be required."
            Expect.equal (mergeType.GetProperty("merges").PropertyType) typeof<string[]> "MergeResult.merges should be required."
            Expect.equal (mergeType.GetProperty("result").PropertyType) typeof<string> "MergeResult.result should be required."

            Expect.equal (remoteWithoutRefsType.GetProperty("name").PropertyType) typeof<string> "RemoteWithoutRefs.name should be required."
            Expect.equal (remoteWithRefsType.GetProperty("name").PropertyType) typeof<string> "RemoteWithRefs.name should be required."
            Expect.equal (remoteWithRefsType.GetProperty("refs").PropertyType) typeof<RemoteRefs> "RemoteWithRefs.refs should be typed."
            Expect.equal (moveType.GetProperty("moves").PropertyType) typeof<MoveResultItem[]> "MoveResult.moves should be typed."

        testCase "clearQueue is marked deprecated in binding" <| fun _ ->
            let clearQueueMethod = typeof<ISimpleGit>.GetMethod("clearQueue")
            let obsoleteAttributes = clearQueueMethod.GetCustomAttributes(typeof<ObsoleteAttribute>, true)
            Expect.isGreaterThan obsoleteAttributes.Length 0 "clearQueue should be marked obsolete in the binding."

        testCase "SimpleGit factory exposes create overloads" <| fun _ ->
            let createMethods =
                typeof<SimpleGit>.GetMethods()
                |> Array.filter (fun mi -> mi.Name = "create")

            Expect.isGreaterThanOrEqual createMethods.Length 4 "Expected at least four create overloads."
    ]

let tests =
    testList "GitValidation" [
        classifyFailureKindTests
        branchValidationTests
        pathspecValidationTests
        remoteUrlValidationTests
        redactionTests
        hostExtractionTests
        authConfigTests
        remoteNameValidationTests
        bindingSurfaceParityTests
    ]

