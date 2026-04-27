module ElectronCore.GitServiceTests

open System
open Fable.Core
open Fable.Core.JS
open Fable.Core.JsInterop
open Main.Bindings.Path
open Main.Bindings.SimpleGit
open Swate.Electron.Shared.GitTypes
open Vitest

module GitService = Main.Git.GitService
module GitProvisioningService = Main.Git.GitProvisioningService
module GitAuthAdapter = Main.Git.GitAuthAdapter
module GitLfsAdapter = Main.Git.GitLfsAdapter
module GitLfsService = Main.Git.GitLfsService
module GitTokenProvider = Main.Git.GitTokenProvider

let private fsPromisesDynamic: obj = importAll "fs/promises"
let private osDynamic: obj = importAll "os"

type private TempRepositoryContext = {
    RootPath: string
    RepoPath: string
    Git: ISimpleGit
}

let private createSimpleGit (repoPath: string) =
    let options =
        SimpleGitOptions(baseDir = repoPath, binary = U3.Case1 "git", maxConcurrentProcesses = 1)

    SimpleGit.create options |> GitAuthAdapter.applyNonInteractiveEnv

let private createTempDirectoryAsync () : JS.Promise<string> =
    let prefix =
        join [|
            osDynamic?tmpdir () |> unbox<string>
            "swate-electron-git-tests-"
        |]

    fsPromisesDynamic?mkdtemp (prefix) |> unbox<JS.Promise<string>>

let private removeDirectoryAsync (path: string) : JS.Promise<unit> = promise {
    let! _ =
        fsPromisesDynamic?rm (path, createObj [ "recursive" ==> true; "force" ==> true ])
        |> unbox<JS.Promise<obj>>

    return ()
}

let private configureRepositoryAsync (git: ISimpleGit) : JS.Promise<unit> = promise {
    let! _ = git.raw [| "config"; "user.name"; "Swate Electron Tests" |]

    let! _ =
        git.raw [|
            "config"
            "user.email"
            "swate-electron-tests@example.org"
        |]

    let! _ = git.raw [| "config"; "core.autocrlf"; "false" |]
    return ()
}

let private ensureDirectoryAsync (path: string) : JS.Promise<unit> = promise {
    let! _ =
        fsPromisesDynamic?mkdir (path, createObj [ "recursive" ==> true ])
        |> unbox<JS.Promise<obj>>

    return ()
}

let private writeUtf8FileAsync (path: string) (content: string) : JS.Promise<unit> = promise {
    let! _ = fsPromisesDynamic?writeFile (path, content, "utf8") |> unbox<JS.Promise<obj>>
    return ()
}

let private expectOk<'T> (operationName: string) (result: GitService.GitResult<'T>) : 'T =
    match result with
    | Ok value -> value
    | Error failure -> failwith $"{operationName} failed ({failure.Kind}): {failure.Message}"

let private expectError<'T> (result: GitService.GitResult<'T>) : GitService.GitFailure =
    match result with
    | Ok _ -> failwith "Expected operation to fail."
    | Error failure -> failure

let private unwrapResultAsync
    (resultPromise: JS.Promise<GitService.GitResult<'T>>)
    (unwrap: GitService.GitResult<'T> -> 'U)
    : JS.Promise<'U> =
    promise {
        let! result = resultPromise
        return unwrap result
    }

let private expectOkResult<'T> (operationName: string) (result: Result<'T, exn>) =
    match result with
    | Ok value -> value
    | Error error -> failwith $"{operationName} failed: {error.Message}"

let private expectErrorResult<'T> (result: Result<'T, exn>) =
    match result with
    | Ok _ -> failwith "Expected operation to fail."
    | Error error -> error

let private gitLfsIntegrationTestOptions = TestOptions(timeout = 20000)

[<Emit("Buffer.from($0, 'utf8')")>]
let private utf8Buffer (text: string) : obj = jsNative

[<Emit("Buffer.byteLength($0, 'utf8')")>]
let private utf8ByteLength (text: string) : int = jsNative

let private runSimpleGitResult
    (operation: ISimpleGit -> JS.Promise<'T>)
    (git: ISimpleGit)
    : JS.Promise<Result<'T, exn>> =
    promise {
        try
            let! result = operation git
            return Ok result
        with error ->
            return Error error
    }

let private createRawOnlyGit (rawHandler: string[] -> JS.Promise<string>) : ISimpleGit =
    createObj [
        "raw" ==> (fun (command: obj) ->
            match command with
            | :? (string array) as commands -> rawHandler commands
            | :? string as singleCommand -> rawHandler [| singleCommand |]
            | _ -> failwith "Unexpected raw command payload.")
    ]
    |> unbox<ISimpleGit>

let private runSimpleGitRawWithFakeGit
    (fakeGit: ISimpleGit)
    (operation: ISimpleGit -> JS.Promise<string>)
    (_: ISimpleGit)
    : JS.Promise<Result<string, exn>> =
    promise {
        try
            let! result = operation fakeGit
            return Ok result
        with error ->
            return Error error
    }

let private splitNonEmptyLines (text: string) =
    text.Split([| '\r'; '\n' |], StringSplitOptions.RemoveEmptyEntries)

let private testRemoteHost = "git.local.test"
let private testRemoteToken = "swate-test-token"
let private testRemoteUrl = $"https://{testRemoteHost}/origin.git"
let private testAuthenticatedRemoteUrl = $"https://oauth2:{testRemoteToken}@{testRemoteHost}/origin.git"

let private toFileRemoteUrl (path: string) =
    let normalized = path.Replace("\\", "/")

    if normalized.StartsWith("/", StringComparison.Ordinal) then
        $"file://{normalized}"
    else
        $"file:///{normalized}"

let private configureLocalRemoteRewrite (git: ISimpleGit) (remotePath: string) = promise {
    let localRemoteUrl = toFileRemoteUrl remotePath
    let! _ = git.raw [| "config"; "--add"; $"url.{localRemoteUrl}.insteadOf"; testRemoteUrl |]
    let! _ = git.raw [| "config"; "--add"; $"url.{localRemoteUrl}.insteadOf"; testAuthenticatedRemoteUrl |]
    return ()
}

let private withTestTokenProvider (body: unit -> JS.Promise<'T>) = promise {
    GitTokenProvider.setTokenProvider {
        TryGetAccessToken =
            fun host -> promise {
                return
                    if String.Equals(host, testRemoteHost, StringComparison.OrdinalIgnoreCase) then
                        Some testRemoteToken
                    else
                        None
            }
    }

    try
        return! body ()
    finally
        GitTokenProvider.setTokenProvider GitTokenProvider.defaultTokenProvider
}

let private withTempRepository (testBody: TempRepositoryContext -> JS.Promise<unit>) : JS.Promise<unit> = promise {
    let! rootPath = createTempDirectoryAsync ()

    try
        let repoPath = join [| rootPath; "repo" |]
        let! initResult = GitProvisioningService.initRepository repoPath
        let normalizedRepoPath = expectOk "git init" initResult
        let git = createSimpleGit normalizedRepoPath
        do! configureRepositoryAsync git

        do!
            testBody {
                RootPath = rootPath
                RepoPath = normalizedRepoPath
                Git = git
            }

        do! removeDirectoryAsync rootPath
    with error ->
        do! removeDirectoryAsync rootPath
        return raise error
}

let private commitWorkflowFilePath = "notes/workflow.txt"
let private featureBranchName = "feature/local-workflow"
let private initialCommitMessage = "test: initial commit"
let private secondCommitMessage = "test: second commit"

Vitest.describe (
    "GitService local repository workflow",
    fun () ->
        Vitest.test (
            "initializes, commits, diffs, recommits, and switches branches locally",
            fun () -> promise {
                do!
                    withTempRepository (fun context -> promise {
                        let notesDirectory = join [| context.RepoPath; "notes" |]
                        let filePath = join [| context.RepoPath; "notes"; "workflow.txt" |]

                        do! ensureDirectoryAsync notesDirectory
                        do! writeUtf8FileAsync filePath "alpha\nbeta\ngamma\n"

                        let! stageResult = GitService.stagePaths context.RepoPath [| commitWorkflowFilePath |]
                        expectOk "git add" stageResult |> ignore

                        let! stagedStatus =
                            unwrapResultAsync
                                (GitService.getStatus context.RepoPath)
                                (expectOk "git status after staging")

                        let stagedFile =
                            stagedStatus.Files
                            |> Microsoft.FSharp.Collections.Array.find (fun file -> file.Path = commitWorkflowFilePath)

                        Vitest.expect(stagedStatus.IsClean).toBe (false)
                        Vitest.expect(stagedFile.Index).toBe ("A")

                        let! firstCommitHash =
                            unwrapResultAsync
                                (GitService.commit context.RepoPath initialCommitMessage)
                                (expectOk "first commit")

                        Vitest.expect(firstCommitHash.Length).toBeGreaterThan (6)

                        let! cleanStatus =
                            unwrapResultAsync
                                (GitService.getStatus context.RepoPath)
                                (expectOk "git status after first commit")

                        let initialBranch =
                            cleanStatus.Current
                            |> Option.defaultWith (fun () ->
                                failwith "Expected initialized repository to have a current branch."
                            )

                        Vitest.expect(cleanStatus.IsClean).toBe (true)

                        do! writeUtf8FileAsync filePath "alpha\nbeta updated\ndelta\n"

                        let! modifiedStatus =
                            unwrapResultAsync
                                (GitService.getStatus context.RepoPath)
                                (expectOk "git status after file change")

                        let modifiedFile =
                            modifiedStatus.Files
                            |> Microsoft.FSharp.Collections.Array.find (fun file -> file.Path = commitWorkflowFilePath)

                        Vitest.expect(modifiedStatus.IsClean).toBe (false)
                        Vitest.expect(modifiedFile.WorkingDir).toBe ("M")

                        let! diffSummary =
                            unwrapResultAsync
                                (GitService.getDiffSummary context.RepoPath)
                                (expectOk "git diff summary")

                        Vitest.expect(diffSummary.Changed).toBe (1)
                        Vitest.expect(diffSummary.Insertions).toBe (2)
                        Vitest.expect(diffSummary.Deletions).toBe (2)

                        let! diffText =
                            unwrapResultAsync
                                (GitService.getDiff context.RepoPath [| commitWorkflowFilePath |])
                                (expectOk "git diff")

                        Vitest.expect(diffText.Contains("diff --git")).toBe (true)
                        Vitest.expect(diffText.Contains("@@")).toBe (true)
                        Vitest.expect(diffText.Contains("-beta")).toBe (true)
                        Vitest.expect(diffText.Contains("+beta updated")).toBe (true)
                        Vitest.expect(diffText.Contains("-gamma")).toBe (true)
                        Vitest.expect(diffText.Contains("+delta")).toBe (true)

                        let! wordDiffText =
                            unwrapResultAsync
                                (GitService.getWordDiff context.RepoPath [| commitWorkflowFilePath |])
                                (expectOk "git word diff")

                        Vitest.expect(wordDiffText.Contains("diff --git")).toBe (true)
                        Vitest.expect(wordDiffText.Contains("@@ -2,2 +2,2 @@")).toBe (true)
                        Vitest.expect(wordDiffText.Contains("-beta")).toBe (false)
                        Vitest.expect(wordDiffText.Contains("-gamma")).toBe (true)
                        Vitest.expect(wordDiffText.Contains("+updated")).toBe (true)
                        Vitest.expect(wordDiffText.Contains("+delta")).toBe (true)
                        Vitest.expect(wordDiffText.Contains("~")).toBe (true)

                        let! secondStageResult = GitService.stagePaths context.RepoPath [| commitWorkflowFilePath |]
                        expectOk "git add after edit" secondStageResult |> ignore

                        let! secondCommitHash =
                            unwrapResultAsync
                                (GitService.commit context.RepoPath secondCommitMessage)
                                (expectOk "second commit")

                        Vitest.expect(secondCommitHash.Length).toBeGreaterThan (6)

                        let! logOutput = context.Git.raw [| "log"; "-2"; "--pretty=%s" |]
                        let logMessages = splitNonEmptyLines logOutput
                        Vitest.expect(logMessages.Length).toBe (2)
                        Vitest.expect(logMessages.[0]).toBe (secondCommitMessage)
                        Vitest.expect(logMessages.[1]).toBe (initialCommitMessage)

                        let! createBranchResult = GitService.createBranch context.RepoPath featureBranchName None
                        expectOk "create branch" createBranchResult |> ignore

                        let! createdBranchStatus =
                            unwrapResultAsync
                                (GitService.getStatus context.RepoPath)
                                (expectOk "git status after create branch")

                        Vitest.expect(createdBranchStatus.Current |> Option.defaultValue "").toBe (featureBranchName)

                        let! checkoutBaseBranchResult = GitService.checkoutBranch context.RepoPath { Name = initialBranch; StartPoint = None }
                        expectOk "checkout initial branch" checkoutBaseBranchResult |> ignore

                        let! baseBranchStatus =
                            unwrapResultAsync
                                (GitService.getStatus context.RepoPath)
                                (expectOk "git status after checkout")

                        Vitest.expect(baseBranchStatus.Current |> Option.defaultValue "").toBe (initialBranch)

                        let! checkoutFeatureBranchResult = GitService.checkoutBranch context.RepoPath { Name = featureBranchName; StartPoint = None }
                        expectOk "checkout feature branch" checkoutFeatureBranchResult |> ignore

                        let! featureBranchStatus =
                            unwrapResultAsync
                                (GitService.getStatus context.RepoPath)
                                (expectOk "git status on feature branch")

                        Vitest.expect(featureBranchStatus.Current |> Option.defaultValue "").toBe (featureBranchName)
                    })
            }
        )

        Vitest.test (
            "unstagePaths keeps working tree changes but removes staged state",
            fun () -> promise {
                do!
                    withTempRepository (fun context -> promise {
                        let filePath = join [| context.RepoPath; "tracked.txt" |]

                        do! writeUtf8FileAsync filePath "first\nsecond\n"

                        let! initialStageResult = GitService.stagePaths context.RepoPath [| "tracked.txt" |]
                        expectOk "stage tracked file" initialStageResult |> ignore

                        let! initialCommitResult = GitService.commit context.RepoPath "test: track file"
                        expectOk "commit tracked file" initialCommitResult |> ignore

                        do! writeUtf8FileAsync filePath "first\nsecond updated\nthird\n"

                        let! modifiedStageResult = GitService.stagePaths context.RepoPath [| "tracked.txt" |]
                        expectOk "stage modified file" modifiedStageResult |> ignore

                        let! unstageResult = GitService.unstagePaths context.RepoPath [| "tracked.txt" |]
                        expectOk "unstage modified file" unstageResult |> ignore

                        let! status =
                            unwrapResultAsync
                                (GitService.getStatus context.RepoPath)
                                (expectOk "git status after unstage")

                        let fileStatus =
                            status.Files
                            |> Microsoft.FSharp.Collections.Array.find (fun file -> file.Path = "tracked.txt")

                        Vitest.expect(status.IsClean).toBe (false)
                        Vitest.expect(fileStatus.WorkingDir).toBe ("M")
                        Vitest.expect(fileStatus.Index = "M").toBe (false)
                    })
            }
        )

        Vitest.test (
            "addRemote stores the requested origin URL in the local repository config",
            fun () -> promise {
                do!
                    withTempRepository (fun context -> promise {
                        let remoteUrl = "https://git.nfdi4plants.org/caroott/arc-a.git"

                        let! _ =
                            unwrapResultAsync
                                (GitService.addRemote context.RepoPath "origin" remoteUrl)
                                (expectOk "add origin remote")

                        let! configuredRemoteUrl = context.Git.raw [| "remote"; "get-url"; "origin" |]
                        Vitest.expect(configuredRemoteUrl.Trim()).toBe (remoteUrl)
                    })
            }
        )

        Vitest.test (
            "previewPull reports SafeToPull when the fetched upstream can be merged without conflicts",
            fun () -> promise {
                do!
                    withTempRepository (fun context -> promise {
                        let remotePath = join [| context.RootPath; "origin.git" |]
                        let clonePath = join [| context.RootPath; "incoming" |]
                        let repoFilePath = join [| context.RepoPath; "workflow.txt" |]

                        do! writeUtf8FileAsync repoFilePath "base\n"
                        let! stageBase = GitService.stagePaths context.RepoPath [| "workflow.txt" |]
                        expectOk "stage base file" stageBase |> ignore

                        let! commitBase = GitService.commit context.RepoPath "test: base commit"
                        expectOk "commit base" commitBase |> ignore

                        let! baseStatus =
                            unwrapResultAsync
                                (GitService.getStatus context.RepoPath)
                                (expectOk "git status after base commit")

                        let baseBranch =
                            baseStatus.Current
                            |> Option.defaultWith (fun () -> failwith "Expected current branch after base commit.")

                        let! _ = context.Git.raw [| "init"; "--bare"; remotePath |]
                        do! configureLocalRemoteRewrite context.Git remotePath
                        let! _ = context.Git.raw [| "remote"; "add"; "origin"; testRemoteUrl |]
                        let! _ = context.Git.raw [| "push"; "-u"; "origin"; baseBranch |]

                        let! _ = context.Git.raw [| "clone"; remotePath; clonePath |]
                        let cloneGit = createSimpleGit clonePath
                        do! configureRepositoryAsync cloneGit

                        let cloneFilePath = join [| clonePath; "workflow.txt" |]
                        do! writeUtf8FileAsync cloneFilePath "remote only change\n"
                        let! _ = cloneGit.raw [| "add"; "workflow.txt" |]
                        let! _ = cloneGit.raw [| "commit"; "-m"; "test: remote only change" |]
                        let! _ = cloneGit.raw [| "push"; "origin"; baseBranch |]

                        let! preview =
                            withTestTokenProvider (fun () ->
                                unwrapResultAsync
                                    (GitService.previewPull context.RepoPath None None None)
                                    (expectOk "preview pull")
                            )

                        Vitest.expect(preview.Status).toEqual (GitPullPreflightStatus.SafeToPull)
                    })
            })

        Vitest.test (
            "previewPull reports WouldRequireMergeResolution when local and fetched upstream change the same lines differently",
            fun () -> promise {
                do!
                    withTempRepository (fun context -> promise {
                        let remotePath = join [| context.RootPath; "origin.git" |]
                        let clonePath = join [| context.RootPath; "incoming" |]
                        let repoFilePath = join [| context.RepoPath; "conflict.txt" |]

                        do! writeUtf8FileAsync repoFilePath "base\n"
                        let! stageBase = GitService.stagePaths context.RepoPath [| "conflict.txt" |]
                        expectOk "stage base file" stageBase |> ignore
                        let! commitBase = GitService.commit context.RepoPath "test: base commit"
                        expectOk "commit base" commitBase |> ignore

                        let! baseStatus =
                            unwrapResultAsync
                                (GitService.getStatus context.RepoPath)
                                (expectOk "git status after base commit")

                        let baseBranch =
                            baseStatus.Current
                            |> Option.defaultWith (fun () -> failwith "Expected current branch after base commit.")

                        let! _ = context.Git.raw [| "init"; "--bare"; remotePath |]
                        do! configureLocalRemoteRewrite context.Git remotePath
                        let! _ = context.Git.raw [| "remote"; "add"; "origin"; testRemoteUrl |]
                        let! _ = context.Git.raw [| "push"; "-u"; "origin"; baseBranch |]

                        let! _ = context.Git.raw [| "clone"; remotePath; clonePath |]
                        let cloneGit = createSimpleGit clonePath
                        do! configureRepositoryAsync cloneGit

                        do! writeUtf8FileAsync repoFilePath "local change\n"
                        let! localStage = GitService.stagePaths context.RepoPath [| "conflict.txt" |]
                        expectOk "stage local change" localStage |> ignore
                        let! commitLocal = GitService.commit context.RepoPath "test: local change"
                        expectOk "commit local" commitLocal |> ignore

                        let cloneFilePath = join [| clonePath; "conflict.txt" |]
                        do! writeUtf8FileAsync cloneFilePath "remote change\n"
                        let! _ = cloneGit.raw [| "add"; "conflict.txt" |]
                        let! _ = cloneGit.raw [| "commit"; "-m"; "test: remote change" |]
                        let! _ = cloneGit.raw [| "push"; "origin"; baseBranch |]

                        let! preview =
                            withTestTokenProvider (fun () ->
                                unwrapResultAsync
                                    (GitService.previewPull context.RepoPath None None None)
                                    (expectOk "preview pull")
                            )

                        Vitest.expect(preview.Status).toEqual (GitPullPreflightStatus.WouldRequireMergeResolution)
                    })
            })

        Vitest.test (
            "previewPull returns Indeterminate when HEAD is detached",
            fun () -> promise {
                do!
                    withTempRepository (fun context -> promise {
                        let filePath = join [| context.RepoPath; "detached.txt" |]

                        do! writeUtf8FileAsync filePath "base\n"
                        let! stageBase = GitService.stagePaths context.RepoPath [| "detached.txt" |]
                        expectOk "stage detached file" stageBase |> ignore

                        let! commitBase = GitService.commit context.RepoPath "test: detached base"
                        expectOk "commit detached base" commitBase |> ignore

                        let! _ = context.Git.raw [| "checkout"; "--detach"; "HEAD" |]

                        let! preview =
                            unwrapResultAsync
                                (GitService.previewPull context.RepoPath None None None)
                                (expectOk "preview pull on detached head")

                        Vitest.expect(preview.Status).toEqual (GitPullPreflightStatus.Indeterminate)
                    })
            })

        Vitest.test (
            "previewPull returns Indeterminate when no upstream tracking branch can be resolved",
            fun () -> promise {
                do!
                    withTempRepository (fun context -> promise {
                        let filePath = join [| context.RepoPath; "no-upstream.txt" |]

                        do! writeUtf8FileAsync filePath "base\n"
                        let! stageBase = GitService.stagePaths context.RepoPath [| "no-upstream.txt" |]
                        expectOk "stage no-upstream file" stageBase |> ignore

                        let! commitBase = GitService.commit context.RepoPath "test: no upstream base"
                        expectOk "commit no-upstream base" commitBase |> ignore

                        let! _ = GitService.createBranch context.RepoPath "feature/no-upstream" None
                        let! status =
                            unwrapResultAsync
                                (GitService.getStatus context.RepoPath)
                                (expectOk "git status on branch without upstream")

                        Vitest.expect(status.Tracking.IsNone).toBe (true)

                        let! preview =
                            unwrapResultAsync
                                (GitService.previewPull context.RepoPath None None None)
                                (expectOk "preview pull without upstream")

                        Vitest.expect(preview.Status).toEqual (GitPullPreflightStatus.Indeterminate)
                    })
            })

        Vitest.test (
            "commit keeps the staged version when the working tree changes again before commit",
            fun () -> promise {
                do!
                    withTempRepository (fun context -> promise {
                        let filePath = join [| context.RepoPath; "tracked.txt" |]

                        do! writeUtf8FileAsync filePath "zero\n"
                        let! initialStageResult = GitService.stagePaths context.RepoPath [| "tracked.txt" |]
                        expectOk "stage initial tracked file" initialStageResult |> ignore
                        let! initialCommitResult = GitService.commit context.RepoPath "test: initial tracked file"
                        expectOk "commit initial tracked file" initialCommitResult |> ignore

                        do! writeUtf8FileAsync filePath "version A\n"
                        let! stageResult = GitService.stagePaths context.RepoPath [| "tracked.txt" |]
                        expectOk "stage version A" stageResult |> ignore

                        do! writeUtf8FileAsync filePath "version B\n"

                        let! commitResult = GitService.commit context.RepoPath "test: commit staged version"
                        expectOk "commit staged version" commitResult |> ignore

                        let! headContent = context.Git.raw [| "show"; "HEAD:tracked.txt" |]
                        Vitest.expect(headContent).toBe ("version A\n")

                        let! workingTreeStatus =
                            unwrapResultAsync
                                (GitService.getStatus context.RepoPath)
                                (expectOk "git status after divergent commit")

                        let trackedFileStatus =
                            workingTreeStatus.Files
                            |> Microsoft.FSharp.Collections.Array.find (fun file -> file.Path = "tracked.txt")

                        Vitest.expect(trackedFileStatus.WorkingDir).toBe ("M")
                    })
            }
        )

        Vitest.test (
            "planOutboundPush ignores remote-only ls-remote tips that are missing locally",
            fun () -> promise {
                let candidateBlobId = "1111111111111111111111111111111111111111"
                let remoteBranchTip = "9999999999999999999999999999999999999999"
                let remoteOnlyTip = "3c1332ae987944dcdf3f68977206561b4cb1a2dc"
                let lfsObjectId = "abababababababababababababababababababababababababababababababab"
                let pointerContent =
                    $"version https://git-lfs.github.com/spec/v1\noid sha256:{lfsObjectId}\nsize 18\n"

                let pointerSize = utf8ByteLength pointerContent
                let mutable batchCheckStdin = None
                let mutable revListStdin = None
                let mutable batchCheckStdin = None
                let optimizedRevListArgs =
                    [|
                        "rev-list"
                        "--objects"
                        "--no-object-names"
                        "--stdin"
                        "--ignore-missing"
                        "--filter=blob:limit=1024"
                        "--filter=object:type=blob"
                        "--filter-provided-objects"
                    |]
                let catFileBatchCheckArgs = [| "cat-file"; "--batch-check" |]
                let catFileBatchArgs = [| "cat-file"; "--batch"; "--buffer" |]

                let fakeGit =
                    createRawOnlyGit (fun command -> promise {
                        let commandText = String.concat " " command

                        match command with
                        | [| "push"; "--porcelain"; "--dry-run"; "origin"; "refs/heads/main" |] ->
                            return "To origin\n* [new branch] refs/heads/main -> refs/heads/main\nDone\n"
                        | [| "ls-remote"; "--refs"; "origin" |] ->
                            return
                                $"{remoteBranchTip}\trefs/heads/main\n{remoteOnlyTip}\trefs/merge-requests/1/merge\n"
                        | _ ->
                            return failwith $"Unexpected raw command: {commandText}"
                    })

                let fakeSpawnedGit : GitLfsAdapter.GitSpawnRequest -> JS.Promise<GitLfsAdapter.GitSpawnResult> =
                    fun request -> promise {
                        let commandText = String.concat " " request.Arguments

                        match request.Arguments with
                        | args when args = catFileBatchCheckArgs ->
                            batchCheckStdin <- request.StandardInput

                            let stdoutText = $"{remoteBranchTip} commit 123\n{remoteOnlyTip} missing\n"

                            return {
                                ExitCode = 0
                                StdoutBuffer = utf8Buffer stdoutText
                                StdoutText = stdoutText
                                StderrText = ""
                                TimedOut = false
                            }
                        | args when args = optimizedRevListArgs ->
                            revListStdin <- request.StandardInput

                            return {
                                ExitCode = 0
                                StdoutBuffer = utf8Buffer $"{candidateBlobId}\n"
                                StdoutText = $"{candidateBlobId}\n"
                                StderrText = ""
                                TimedOut = false
                            }
                        | args when args = catFileBatchArgs ->
                            let stdoutText = $"{candidateBlobId} blob {pointerSize}\n{pointerContent}"

                            return {
                                ExitCode = 0
                                StdoutBuffer = utf8Buffer stdoutText
                                StdoutText = stdoutText
                                StderrText = ""
                                TimedOut = false
                            }
                        | _ ->
                            return failwith $"Unexpected spawned command: {commandText}"
                    }

                let! plan =
                    GitLfsService.planOutboundPush
                        (runSimpleGitRawWithFakeGit fakeGit)
                        fakeSpawnedGit
                        id
                        (fun _ -> promise { return Error(exn "status should not be called") })
                        "C:/repo"
                        "origin"
                        (Some "refs/heads/main")
                        fakeGit

                let actualPlan = expectOkResult "plan outbound push with remote-only tips" plan

                Vitest.expect(batchCheckStdin).toEqual (Some $"{remoteBranchTip}\n{remoteOnlyTip}\n")
                Vitest.expect(revListStdin).toEqual (Some $"refs/heads/main\n--not\n{remoteBranchTip}\n")
                Vitest.expect(actualPlan).toEqual (GitLfsService.OutboundPushPlan.UploadLfsObjects [| lfsObjectId |])
            }
        )

        Vitest.test (
            "planOutboundPush returns exact LFS object IDs from batch pointer blobs",
            fun () -> promise {
                let candidateBlobId = "1111111111111111111111111111111111111111"
                let remoteTip = "9999999999999999999999999999999999999999"
                let lfsObjectId = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"
                let pointerContent =
                    $"version https://git-lfs.github.com/spec/v1\noid sha256:{lfsObjectId}\nsize 18\n"

                let pointerSize = utf8ByteLength pointerContent
                let mutable batchCheckStdin = None
                let mutable revListStdin = None
                let optimizedRevListArgs =
                    [|
                        "rev-list"
                        "--objects"
                        "--no-object-names"
                        "--stdin"
                        "--ignore-missing"
                        "--filter=blob:limit=1024"
                        "--filter=object:type=blob"
                        "--filter-provided-objects"
                    |]
                let catFileBatchCheckArgs = [| "cat-file"; "--batch-check" |]
                let catFileBatchArgs = [| "cat-file"; "--batch"; "--buffer" |]

                let fakeGit =
                    createRawOnlyGit (fun command -> promise {
                        let commandText = String.concat " " command

                        match command with
                        | [| "push"; "--porcelain"; "--dry-run"; "origin"; "refs/heads/main" |] ->
                            return "To origin\n* [new branch] refs/heads/main -> refs/heads/main\nDone\n"
                        | [| "ls-remote"; "--refs"; "origin" |] ->
                            return $"{remoteTip}\trefs/heads/main\n"
                        | _ ->
                            return failwith $"Unexpected raw command: {commandText}"
                    })

                let fakeSpawnedGit : GitLfsAdapter.GitSpawnRequest -> JS.Promise<GitLfsAdapter.GitSpawnResult> =
                    fun (request: GitLfsAdapter.GitSpawnRequest) -> promise {
                        let commandText = String.concat " " request.Arguments

                        match request.Arguments with
                        | args when args = catFileBatchCheckArgs ->
                            batchCheckStdin <- request.StandardInput

                            let stdoutText = $"{remoteTip} commit 123\n"

                            return {
                                ExitCode = 0
                                StdoutBuffer = utf8Buffer stdoutText
                                StdoutText = stdoutText
                                StderrText = ""
                                TimedOut = false
                            }
                        | args when args = optimizedRevListArgs ->
                            revListStdin <- request.StandardInput

                            return {
                                ExitCode = 0
                                StdoutBuffer = utf8Buffer $"{candidateBlobId}\n"
                                StdoutText = $"{candidateBlobId}\n"
                                StderrText = ""
                                TimedOut = false
                            }
                        | args when args = catFileBatchArgs ->
                            let stdoutText = $"{candidateBlobId} blob {pointerSize}\n{pointerContent}"

                            return {
                                ExitCode = 0
                                StdoutBuffer = utf8Buffer stdoutText
                                StdoutText = stdoutText
                                StderrText = ""
                                TimedOut = false
                            }
                        | _ ->
                            return failwith $"Unexpected spawned command: {commandText}"
                    }

                let! plan =
                    GitLfsService.planOutboundPush
                        (runSimpleGitRawWithFakeGit fakeGit)
                        fakeSpawnedGit
                        id
                        (fun _ -> promise { return Error(exn "status should not be called") })
                        "C:/repo"
                        "origin"
                        (Some "refs/heads/main")
                        fakeGit

                let actualPlan = expectOkResult "plan outbound push exact OIDs" plan

                Vitest.expect(batchCheckStdin).toEqual (Some $"{remoteTip}\n")
                Vitest.expect(revListStdin).toEqual (Some $"refs/heads/main\n--not\n{remoteTip}\n")
                Vitest.expect(actualPlan).toEqual (GitLfsService.OutboundPushPlan.UploadLfsObjects [| lfsObjectId |])
            }
        )

        Vitest.test (
            "planOutboundPush skips LFS upload when outbound commits do not contain LFS pointers",
            fun () -> promise {
                do!
                    withTempRepository (fun context -> promise {
                        let remotePath = join [| context.RootPath; "remote.git" |]
                        let filePath = join [| context.RepoPath; "plain.txt" |]

                        let! _ = context.Git.raw [| "init"; "--bare"; remotePath |]
                        let! _ = context.Git.raw [| "remote"; "add"; "origin"; remotePath |]

                        do! writeUtf8FileAsync filePath "plain text\n"

                        let! stageResult = GitService.stagePaths context.RepoPath [| "plain.txt" |]
                        expectOk "stage plain text file" stageResult |> ignore

                        let! commitResult = GitService.commit context.RepoPath "test: plain text commit"
                        expectOk "commit plain text file" commitResult |> ignore

                        let! plan =
                            GitLfsService.planOutboundPush
                                runSimpleGitResult
                                GitLfsAdapter.runGitCaptured
                                id
                                (fun git -> runSimpleGitResult (fun currentGit -> currentGit.status ()) git)
                                context.RepoPath
                                "origin"
                                None
                                context.Git

                        let actualPlan = expectOkResult "plan outbound push without lfs" plan
                        Vitest.expect(actualPlan).toEqual (GitLfsService.OutboundPushPlan.SkipLfsUpload)
                    })
            }
        )

        Vitest.test (
            "planOutboundPush requires LFS upload when outbound commits contain staged LFS pointers",
            gitLfsIntegrationTestOptions,
            fun () -> promise {
                do!
                    withTempRepository (fun context -> promise {
                        let remotePath = join [| context.RootPath; "remote-lfs.git" |]
                        let filePath = join [| context.RepoPath; "artifact.bin" |]

                        let! _ = context.Git.raw [| "init"; "--bare"; remotePath |]
                        let! _ = context.Git.raw [| "remote"; "add"; "origin"; remotePath |]
                        let! _ = context.Git.raw [| "lfs"; "install"; "--local" |]
                        let! _ = context.Git.raw [| "lfs"; "track"; "*.bin" |]

                        do! writeUtf8FileAsync filePath "binary-ish content\n"

                        let! stageResult =
                            GitService.stagePaths context.RepoPath [| ".gitattributes"; "artifact.bin" |]

                        expectOk "stage lfs-managed file" stageResult |> ignore

                        let! commitResult = GitService.commit context.RepoPath "test: lfs pointer commit"
                        expectOk "commit lfs-managed file" commitResult |> ignore

                        let! plan =
                            GitLfsService.planOutboundPush
                                runSimpleGitResult
                                GitLfsAdapter.runGitCaptured
                                id
                                (fun git -> runSimpleGitResult (fun currentGit -> currentGit.status ()) git)
                                context.RepoPath
                                "origin"
                                None
                                context.Git

                        let actualPlan = expectOkResult "plan outbound push with lfs" plan

                        match actualPlan with
                        | GitLfsService.OutboundPushPlan.UploadLfsObjects lfsObjectIds ->
                            Vitest.expect(lfsObjectIds.Length).toBeGreaterThan (0)
                        | GitLfsService.OutboundPushPlan.SkipLfsUpload ->
                            failwith "Expected outbound LFS upload to be required."
                    })
            }
        )

        Vitest.test (
            "planOutboundPush skips LFS upload when the remote is already up to date but local origin refs are stale",
            gitLfsIntegrationTestOptions,
            fun () -> promise {
                do!
                    withTempRepository (fun context -> promise {
                        let remotePath = join [| context.RootPath; "remote-stale-lfs.git" |]
                        let filePath = join [| context.RepoPath; "artifact.bin" |]
                        let branchName = "feature/stale-lfs-plan"

                        let! _ = context.Git.raw [| "init"; "--bare"; remotePath |]
                        let! _ = context.Git.raw [| "remote"; "add"; "origin"; remotePath |]
                        let! _ = context.Git.raw [| "lfs"; "install"; "--local" |]
                        let! _ = context.Git.raw [| "lfs"; "track"; "*.bin" |]

                        do! writeUtf8FileAsync filePath "binary-ish content\n"

                        let! stageResult =
                            GitService.stagePaths context.RepoPath [| ".gitattributes"; "artifact.bin" |]

                        expectOk "stage lfs-managed file for stale remote ref test" stageResult
                        |> ignore

                        let! commitResult =
                            GitService.commit context.RepoPath "test: lfs pointer commit for stale remote ref test"

                        expectOk "commit lfs-managed file for stale remote ref test" commitResult
                        |> ignore

                        let! _ = context.Git.raw [| "branch"; "-M"; branchName |]
                        let! _ = context.Git.raw [| "push"; "--set-upstream"; "origin"; branchName |]

                        let! _ =
                            context.Git.raw [|
                                "update-ref"
                                "-d"
                                $"refs/remotes/origin/{branchName}"
                            |]

                        let! plan =
                            GitLfsService.planOutboundPush
                                runSimpleGitResult
                                GitLfsAdapter.runGitCaptured
                                id
                                (fun git -> runSimpleGitResult (fun currentGit -> currentGit.status ()) git)
                                context.RepoPath
                                "origin"
                                None
                                context.Git

                        let actualPlan =
                            expectOkResult "plan outbound push with stale local remote refs" plan

                        Vitest.expect(actualPlan).toEqual (GitLfsService.OutboundPushPlan.SkipLfsUpload)
                    })
            }
        )

        Vitest.test (
            "uploadObjects sends exact object IDs over stdin",
            fun () -> promise {
                let lfsObjectId = "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb"

                let commandAuth =
                    GitAuthAdapter.createCommandAuthentication
                        "git.nfdi4plants.org"
                        "abc123"
                        (Some "origin")
                        (Some "https://git.nfdi4plants.org/caroott/TestARCGit.git")

                let mutable recordedArgs = [||]
                let mutable recordedStdin = None

                let fakeSpawnedGit : GitLfsAdapter.GitSpawnRequest -> JS.Promise<GitLfsAdapter.GitSpawnResult> =
                    fun (request: GitLfsAdapter.GitSpawnRequest) -> promise {
                        recordedArgs <- request.Arguments
                        recordedStdin <- request.StandardInput

                        return {
                            ExitCode = 0
                            StdoutBuffer = utf8Buffer ""
                            StdoutText = ""
                            StderrText = ""
                            TimedOut = false
                        }
                    }

                let! result =
                    GitLfsService.uploadObjects
                        fakeSpawnedGit
                        commandAuth
                        "C:/repo"
                        "origin"
                        "refs/heads/main"
                        [| lfsObjectId |]

                expectOkResult "upload exact object ids" result |> ignore

                let expectedArgs =
                    [|
                        yield! commandAuth.ConfigArgs
                        yield "lfs"
                        yield "push"
                        yield "--object-id"
                        yield "origin"
                        yield "--stdin"
                    |]

                Vitest.expect(recordedArgs).toEqual (
                    expectedArgs
                )
                Vitest.expect(recordedStdin).toEqual (Some $"{lfsObjectId}\n")
            }
        )

        Vitest.test (
            "executePushWorkflow skips the LFS upload step when the plan does not require it",
            fun () -> promise {
                let events = ResizeArray<string>()

                let pushTarget: GitService.GitPushTarget = {
                    RefSpec = "feature/skip-lfs"
                    PushBranch = "feature/skip-lfs"
                    SetUpstream = false
                }

                let! result =
                    GitService.executePushWorkflow
                        pushTarget
                        (fun () ->
                            events.Add "plan"
                            promise { return Ok GitLfsService.OutboundPushPlan.SkipLfsUpload }
                        )
                        (fun objectIds ->
                            let joinedObjectIds = String.concat "," objectIds
                            events.Add $"upload:{joinedObjectIds}"
                            promise { return Ok() }
                        )
                        (fun skipLfsHook _ ->
                            events.Add $"push:skip={skipLfsHook}"
                            promise { return Ok() }
                        )
                        (fun _ -> promise { return None })

                expectOk "execute push workflow without lfs upload" result |> ignore
                Vitest.expect(events.ToArray()).toEqual ([| "plan"; "push:skip=false" |])
            }
        )

        Vitest.test (
            "executePushWorkflow enables GIT_LFS_SKIP_PUSH only after successful exact upload",
            fun () -> promise {
                let events = ResizeArray<string>()

                let pushTarget: GitService.GitPushTarget = {
                    RefSpec = "feature/upload-lfs"
                    PushBranch = "feature/upload-lfs"
                    SetUpstream = true
                }

                let! result =
                    GitService.executePushWorkflow
                        pushTarget
                        (fun () -> promise {
                            return
                                Ok(
                                    GitLfsService.OutboundPushPlan.UploadLfsObjects [|
                                        "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb"
                                    |]
                                )
                        })
                        (fun objectIds ->
                            let joinedObjectIds = String.concat "," objectIds
                            events.Add($"upload:{joinedObjectIds}")
                            promise { return Ok() }
                        )
                        (fun skipLfsHook _ ->
                            events.Add($"push:skip={skipLfsHook}")
                            promise { return Ok() }
                        )
                        (fun _ -> promise { return None })

                expectOk "execute push workflow with exact upload" result |> ignore

                Vitest.expect(events.ToArray()).toEqual (
                    [|
                        "upload:bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb"
                        "push:skip=true"
                    |]
                )
            }
        )

        Vitest.test (
            "executePushWorkflow aborts and appends diagnostics when exact upload fails",
            fun () -> promise {
                let uploadFailure: GitService.GitFailure = {
                    Kind = GitFailureKind.Network
                    Message = "lfs upload failed"
                }

                let pushTarget: GitService.GitPushTarget = {
                    RefSpec = "feature/diagnostics"
                    PushBranch = "feature/diagnostics"
                    SetUpstream = false
                }

                let! result =
                    GitService.executePushWorkflow
                        pushTarget
                        (fun () -> promise {
                            return
                                Ok(
                                    GitLfsService.OutboundPushPlan.UploadLfsObjects [|
                                        "cccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccc"
                                    |]
                                )
                        })
                        (fun _ -> promise { return Error uploadFailure })
                        (fun _ _ -> promise { return Ok() })
                        (fun _ -> promise { return Some "Git LFS Env:\n[REDACTED]" })

                let failure = expectError result
                Vitest.expect(failure.Kind).toEqual (GitFailureKind.Network)
                Vitest.expect(failure.Message.Contains("lfs upload failed")).toBe (true)
                Vitest.expect(failure.Message.Contains("LFS diagnostics")).toBe (true)
            }
        )

        Vitest.test (
            "planOutboundPush falls back to remote-truth per-object probes when filter planning is unavailable",
            fun () -> promise {
                let candidateBlobId = "3333333333333333333333333333333333333333"
                let remoteTip = "7777777777777777777777777777777777777777"
                let lfsObjectId = "dddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddd"

                let fakeGit =
                    createRawOnlyGit (fun command -> promise {
                        let commandText = String.concat " " command

                        match command with
                        | [| "push"; "--porcelain"; "--dry-run"; "origin"; "refs/heads/main" |] ->
                            return "To origin\n* [new branch] refs/heads/main -> refs/heads/main\nDone\n"
                        | [| "ls-remote"; "--refs"; "origin" |] ->
                            return $"{remoteTip}\trefs/heads/main\n"
                        | [| "cat-file"; "-t"; currentObjectId |] when currentObjectId = candidateBlobId ->
                            return "blob\n"
                        | [| "cat-file"; "-s"; currentObjectId |] when currentObjectId = candidateBlobId ->
                            return "129\n"
                        | [| "cat-file"; "-p"; currentObjectId |] when currentObjectId = candidateBlobId ->
                            return $"version https://git-lfs.github.com/spec/v1\noid sha256:{lfsObjectId}\nsize 18\n"
                        | [| "for-each-ref"; "--format=%(refname)"; "refs/remotes/origin" |] ->
                            return failwith "Legacy local remote-ref fallback must not run in this test."
                        | _ ->
                            return failwith $"Unexpected raw command: {commandText}"
                    })

                let seenRevListModes = ResizeArray<string>()
                let batchCheckArgs = [| "cat-file"; "--batch-check" |]
                let optimizedRevListArgs =
                    [|
                        "rev-list"
                        "--objects"
                        "--no-object-names"
                        "--stdin"
                        "--ignore-missing"
                        "--filter=blob:limit=1024"
                        "--filter=object:type=blob"
                        "--filter-provided-objects"
                    |]
                let fallbackRevListArgs =
                    [| "rev-list"; "--objects"; "--no-object-names"; "--stdin"; "--ignore-missing" |]

                let fakeSpawnedGit : GitLfsAdapter.GitSpawnRequest -> JS.Promise<GitLfsAdapter.GitSpawnResult> =
                    fun (request: GitLfsAdapter.GitSpawnRequest) -> promise {
                        let commandText = String.concat " " request.Arguments

                        match request.Arguments with
                        | args when args = batchCheckArgs ->
                            return {
                                ExitCode = 0
                                StdoutBuffer = utf8Buffer $"{remoteTip} commit 123\n"
                                StdoutText = $"{remoteTip} commit 123\n"
                                StderrText = ""
                                TimedOut = false
                            }
                        | args when args = optimizedRevListArgs ->
                            seenRevListModes.Add "optimized"

                            return {
                                ExitCode = 129
                                StdoutBuffer = utf8Buffer ""
                                StdoutText = ""
                                StderrText = "fatal: unknown option `--filter-provided-objects`"
                                TimedOut = false
                            }
                        | args when args = fallbackRevListArgs ->
                            seenRevListModes.Add "fallback"

                            return {
                                ExitCode = 0
                                StdoutBuffer = utf8Buffer $"{candidateBlobId}\n"
                                StdoutText = $"{candidateBlobId}\n"
                                StderrText = ""
                                TimedOut = false
                            }
                        | _ ->
                            return failwith $"Unexpected spawned command: {commandText}"
                    }

                let! plan =
                    GitLfsService.planOutboundPush
                        (runSimpleGitRawWithFakeGit fakeGit)
                        fakeSpawnedGit
                        id
                        (fun _ -> promise { return Error(exn "status should not be called") })
                        "C:/repo"
                        "origin"
                        (Some "refs/heads/main")
                        fakeGit

                let actualPlan = expectOkResult "plan outbound push fallback" plan

                Vitest.expect(seenRevListModes.ToArray()).toEqual ([| "optimized"; "fallback" |])
                Vitest.expect(actualPlan).toEqual (GitLfsService.OutboundPushPlan.UploadLfsObjects [| lfsObjectId |])
            }
        )

        Vitest.test (
            "uploadObjects falls back to ref-spec upload when object-id mode is unavailable",
            fun () -> promise {
                let lfsObjectId = "eeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeee"

                let commandAuth: GitAuthAdapter.GitCommandAuthentication = {
                    ConfigArgs = [| "-c"; "http.extraHeader=Authorization: Basic TEST" |]
                    Environment = createObj []
                }

                let calls = ResizeArray<string>()
                let objectIdUploadArgs =
                    [|
                        "-c"
                        "http.extraHeader=Authorization: Basic TEST"
                        "lfs"
                        "push"
                        "--object-id"
                        "origin"
                        "--stdin"
                    |]
                let refSpecUploadArgs =
                    [|
                        "-c"
                        "http.extraHeader=Authorization: Basic TEST"
                        "lfs"
                        "push"
                        "origin"
                        "refs/heads/main"
                    |]

                let fakeSpawnedGit : GitLfsAdapter.GitSpawnRequest -> JS.Promise<GitLfsAdapter.GitSpawnResult> =
                    fun (request: GitLfsAdapter.GitSpawnRequest) -> promise {
                        let commandText = String.concat " " request.Arguments
                        calls.Add commandText

                        match request.Arguments with
                        | args when args = objectIdUploadArgs ->
                            return {
                                ExitCode = 2
                                StdoutBuffer = utf8Buffer ""
                                StdoutText = ""
                                StderrText = "unknown flag: --object-id"
                                TimedOut = false
                            }
                        | args when args = refSpecUploadArgs ->
                            return {
                                ExitCode = 0
                                StdoutBuffer = utf8Buffer ""
                                StdoutText = ""
                                StderrText = ""
                                TimedOut = false
                            }
                        | _ ->
                            return failwith $"Unexpected spawned command: {commandText}"
                    }

                let! result =
                    GitLfsService.uploadObjects
                        fakeSpawnedGit
                        commandAuth
                        "C:/repo"
                        "origin"
                        "refs/heads/main"
                        [| lfsObjectId |]

                expectOkResult "upload fallback" result |> ignore
                Vitest.expect(calls.ToArray()).toEqual (
                    [|
                        "-c http.extraHeader=Authorization: Basic TEST lfs push --object-id origin --stdin"
                        "-c http.extraHeader=Authorization: Basic TEST lfs push origin refs/heads/main"
                    |]
                )
            }
        )

        Vitest.test (
            "planOutboundPush returns a timeout failure when optimized planner probing fails",
            fun () -> promise {
                let remoteTip = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"
                let batchCheckArgs = [| "cat-file"; "--batch-check" |]
                let optimizedRevListArgs =
                    [|
                        "rev-list"
                        "--objects"
                        "--no-object-names"
                        "--stdin"
                        "--ignore-missing"
                        "--filter=blob:limit=1024"
                        "--filter=object:type=blob"
                        "--filter-provided-objects"
                    |]

                let fakeGit =
                    createRawOnlyGit (fun command -> promise {
                        let commandText = String.concat " " command

                        match command with
                        | [| "push"; "--porcelain"; "--dry-run"; "origin"; "refs/heads/main" |] ->
                            return "To origin\n* [new branch] refs/heads/main -> refs/heads/main\nDone\n"
                        | [| "ls-remote"; "--refs"; "origin" |] ->
                            return $"{remoteTip}\trefs/heads/main\n"
                        | [| "for-each-ref"; "--format=%(refname)"; "refs/remotes/origin" |] ->
                            return failwith "Legacy local remote-ref fallback must not run for planner timeouts."
                        | _ ->
                            return failwith $"Unexpected raw command: {commandText}"
                    })

                let fakeSpawnedGit : GitLfsAdapter.GitSpawnRequest -> JS.Promise<GitLfsAdapter.GitSpawnResult> =
                    fun request -> promise {
                        let commandText = String.concat " " request.Arguments

                        if request.Arguments = batchCheckArgs then
                            return {
                                ExitCode = 0
                                StdoutBuffer = utf8Buffer $"{remoteTip} commit 123\n"
                                StdoutText = $"{remoteTip} commit 123\n"
                                StderrText = ""
                                TimedOut = false
                            }
                        elif request.Arguments = optimizedRevListArgs then
                            return {
                                ExitCode = -1
                                StdoutBuffer = utf8Buffer ""
                                StdoutText = ""
                                StderrText = ""
                                TimedOut = true
                            }
                        else
                            return failwith $"Unexpected spawned command: {commandText}"
                    }

                let! plan =
                    GitLfsService.planOutboundPush
                        (runSimpleGitRawWithFakeGit fakeGit)
                        fakeSpawnedGit
                        id
                        (fun _ -> promise { return Error(exn "status should not be called") })
                        "C:/repo"
                        "origin"
                        (Some "refs/heads/main")
                        fakeGit

                let failure = expectErrorResult plan
                Vitest.expect(failure.Message.Contains("timed out")).toBe (true)
            }
        )

        Vitest.test (
            "planOutboundPush returns a timeout failure when remote-truth fallback probing fails",
            fun () -> promise {
                let remoteTip = "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb"
                let batchCheckArgs = [| "cat-file"; "--batch-check" |]
                let optimizedRevListArgs =
                    [|
                        "rev-list"
                        "--objects"
                        "--no-object-names"
                        "--stdin"
                        "--ignore-missing"
                        "--filter=blob:limit=1024"
                        "--filter=object:type=blob"
                        "--filter-provided-objects"
                    |]
                let fallbackRevListArgs =
                    [| "rev-list"; "--objects"; "--no-object-names"; "--stdin"; "--ignore-missing" |]

                let fakeGit =
                    createRawOnlyGit (fun command -> promise {
                        let commandText = String.concat " " command

                        match command with
                        | [| "push"; "--porcelain"; "--dry-run"; "origin"; "refs/heads/main" |] ->
                            return "To origin\n* [new branch] refs/heads/main -> refs/heads/main\nDone\n"
                        | [| "ls-remote"; "--refs"; "origin" |] ->
                            return $"{remoteTip}\trefs/heads/main\n"
                        | [| "for-each-ref"; "--format=%(refname)"; "refs/remotes/origin" |] ->
                            return failwith "Legacy local remote-ref fallback must not run for planner timeouts."
                        | _ ->
                            return failwith $"Unexpected raw command: {commandText}"
                    })

                let fakeSpawnedGit : GitLfsAdapter.GitSpawnRequest -> JS.Promise<GitLfsAdapter.GitSpawnResult> =
                    fun request -> promise {
                        let commandText = String.concat " " request.Arguments

                        match request.Arguments with
                        | args when args = batchCheckArgs ->
                            return {
                                ExitCode = 0
                                StdoutBuffer = utf8Buffer $"{remoteTip} commit 123\n"
                                StdoutText = $"{remoteTip} commit 123\n"
                                StderrText = ""
                                TimedOut = false
                            }
                        | args when args = optimizedRevListArgs ->
                            return {
                                ExitCode = 129
                                StdoutBuffer = utf8Buffer ""
                                StdoutText = ""
                                StderrText = "fatal: unknown option `--filter-provided-objects`"
                                TimedOut = false
                            }
                        | args when args = fallbackRevListArgs ->
                            return {
                                ExitCode = -1
                                StdoutBuffer = utf8Buffer ""
                                StdoutText = ""
                                StderrText = ""
                                TimedOut = true
                            }
                        | _ ->
                            return failwith $"Unexpected spawned command: {commandText}"
                    }

                let! plan =
                    GitLfsService.planOutboundPush
                        (runSimpleGitRawWithFakeGit fakeGit)
                        fakeSpawnedGit
                        id
                        (fun _ -> promise { return Error(exn "status should not be called") })
                        "C:/repo"
                        "origin"
                        (Some "refs/heads/main")
                        fakeGit

                let failure = expectErrorResult plan
                Vitest.expect(failure.Message.Contains("timed out")).toBe (true)
            }
        )

        Vitest.test (
            "uploadObjects preserves timeout failures from spawned git",
            fun () -> promise {
                let commandAuth: GitAuthAdapter.GitCommandAuthentication = {
                    ConfigArgs = [| "-c"; "http.extraHeader=Authorization: Basic TEST" |]
                    Environment = createObj []
                }

                let fakeSpawnedGit : GitLfsAdapter.GitSpawnRequest -> JS.Promise<GitLfsAdapter.GitSpawnResult> =
                    fun _ -> promise {
                        return {
                            ExitCode = -1
                            StdoutBuffer = utf8Buffer ""
                            StdoutText = ""
                            StderrText = ""
                            TimedOut = true
                        }
                    }

                let! result =
                    GitLfsService.uploadObjects
                        fakeSpawnedGit
                        commandAuth
                        "C:/repo"
                        "origin"
                        "refs/heads/main"
                        [| "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" |]

                let failure = expectErrorResult result
                Vitest.expect(failure.Message.Contains("timed out")).toBe (true)
            }
        )

        Vitest.test (
            "rejects invalid diff pathspecs before invoking git",
            fun () -> promise {
                do!
                    withTempRepository (fun context -> promise {
                        let! failure =
                            unwrapResultAsync (GitService.getDiff context.RepoPath [| "../outside.txt" |]) expectError

                        Vitest.expect(failure.Message.Contains("traversal")).toBe (true)
                    })
            }
        )

        Vitest.test (
            "rejects invalid word diff pathspecs before invoking git",
            fun () -> promise {
                do!
                    withTempRepository (fun context -> promise {
                        let! failure =
                            unwrapResultAsync
                                (GitService.getWordDiff context.RepoPath [| "../outside.txt" |])
                                expectError

                        Vitest.expect(failure.Message.Contains("traversal")).toBe (true)
                    })
            }
        )

        Vitest.test (
            "fails when initializing a repository twice at the same target path",
            fun () -> promise {
                let! rootPath = createTempDirectoryAsync ()

                try
                    let repoPath = join [| rootPath; "repo" |]

                    let! firstInitResult = GitProvisioningService.initRepository repoPath
                    let normalizedRepoPath = expectOk "first init" firstInitResult
                    Vitest.expect(normalizedRepoPath.Length).toBeGreaterThan (0)

                    let! secondInitResult = GitProvisioningService.initRepository repoPath
                    let failure = expectError secondInitResult

                    Vitest.expect(failure.Message.Contains("already a git repository")).toBe (true)
                    do! removeDirectoryAsync rootPath
                with error ->
                    do! removeDirectoryAsync rootPath
                    return raise error
            }
        )

        Vitest.test (
            "fails when checking out a branch that does not exist locally",
            fun () -> promise {
                do!
                    withTempRepository (fun context -> promise {
                        let filePath = join [| context.RepoPath; "tracked.txt" |]

                        do! writeUtf8FileAsync filePath "content\n"

                        let! stageResult = GitService.stagePaths context.RepoPath [| "tracked.txt" |]
                        expectOk "stage tracked file" stageResult |> ignore

                        let! commitResult = GitService.commit context.RepoPath "test: base commit"
                        expectOk "commit tracked file" commitResult |> ignore

                        let! failure =
                            unwrapResultAsync
                                (GitService.checkoutBranch context.RepoPath { Name = "missing/local-branch"; StartPoint = None })
                                expectError

                        Vitest.expect(failure.Message.Contains("does not exist in the local repository")).toBe (true)
                    })
            }
        )

        Vitest.test (
            "checkoutBranch switches tracking from origin/main to the matching remote branch",
            fun () -> promise {
                do!
                    withTempRepository (fun context -> promise {
                        let filePath = join [| context.RepoPath; "tracked.txt" |]
                        let remotePath = join [| context.RootPath; "remote.git" |]

                        do! writeUtf8FileAsync filePath "content\n"

                        let! stageResult = GitService.stagePaths context.RepoPath [| "tracked.txt" |]
                        expectOk "stage tracked file" stageResult |> ignore

                        let! commitResult = GitService.commit context.RepoPath "test: base commit"
                        expectOk "commit tracked file" commitResult |> ignore

                        let! baseStatus =
                            unwrapResultAsync
                                (GitService.getStatus context.RepoPath)
                                (expectOk "git status after base commit")

                        let initialBranch =
                            baseStatus.Current
                            |> Option.defaultWith (fun () -> failwith "Expected current branch after base commit.")

                        let! _ = context.Git.raw [| "init"; "--bare"; remotePath |]
                        let! _ = context.Git.raw [| "remote"; "add"; "origin"; remotePath |]
                        let! _ = context.Git.raw [| "push"; "-u"; "origin"; initialBranch |]

                        let! createBranchResult = GitService.createBranch context.RepoPath featureBranchName None
                        expectOk "create feature branch" createBranchResult |> ignore

                        let! _ = context.Git.raw [| "push"; "-u"; "origin"; featureBranchName |]

                        let! _ =
                            context.Git.raw [|
                                "branch"
                                $"--set-upstream-to=origin/{initialBranch}"
                                featureBranchName
                            |]

                        let! poisonedStatus =
                            unwrapResultAsync
                                (GitService.getStatus context.RepoPath)
                                (expectOk "git status after poisoning tracking")

                        Vitest.expect(poisonedStatus.Tracking).toEqual (Some $"origin/{initialBranch}")

                        let! checkoutBaseResult = GitService.checkoutBranch context.RepoPath { Name = initialBranch; StartPoint = None }
                        expectOk "checkout base branch" checkoutBaseResult |> ignore

                        let! checkoutFeatureResult = GitService.checkoutBranch context.RepoPath { Name = featureBranchName; StartPoint = None }
                        expectOk "checkout feature branch" checkoutFeatureResult |> ignore

                        let! featureStatus =
                            unwrapResultAsync
                                (GitService.getStatus context.RepoPath)
                                (expectOk "git status after checkout feature branch")

                        Vitest.expect(featureStatus.Current).toEqual (Some featureBranchName)
                        Vitest.expect(featureStatus.Tracking).toEqual (Some $"origin/{featureBranchName}")

                        let! branchOptions =
                            unwrapResultAsync
                                (GitService.getBranches context.RepoPath)
                                (expectOk "git branches after checkout feature branch")

                        let localFeatureBranch =
                            branchOptions
                            |> Microsoft.FSharp.Collections.Array.find (fun branch ->
                                branch.RefName = featureBranchName
                            )

                        let remoteFeatureBranch =
                            branchOptions
                            |> Microsoft.FSharp.Collections.Array.find (fun branch ->
                                branch.RefName = $"origin/{featureBranchName}"
                            )

                        Vitest.expect(localFeatureBranch.Kind).toEqual (GitBranchRefKind.Local)
                        Vitest.expect(localFeatureBranch.IsCurrent).toBe (true)
                        Vitest.expect(localFeatureBranch.IsTracking).toBe (true)
                        Vitest.expect(remoteFeatureBranch.Kind).toEqual (GitBranchRefKind.Remote)
                        Vitest.expect(remoteFeatureBranch.IsTracking).toBe (true)
                    })
            }
        )

        Vitest.test (
            "createBranch clears inherited origin/main tracking when the new remote branch does not exist yet",
            fun () -> promise {
                do!
                    withTempRepository (fun context -> promise {
                        let filePath = join [| context.RepoPath; "tracked.txt" |]
                        let remotePath = join [| context.RootPath; "remote.git" |]

                        do! writeUtf8FileAsync filePath "content\n"

                        let! stageResult = GitService.stagePaths context.RepoPath [| "tracked.txt" |]
                        expectOk "stage tracked file" stageResult |> ignore

                        let! commitResult = GitService.commit context.RepoPath "test: base commit"
                        expectOk "commit tracked file" commitResult |> ignore

                        let! baseStatus =
                            unwrapResultAsync
                                (GitService.getStatus context.RepoPath)
                                (expectOk "git status after base commit")

                        let initialBranch =
                            baseStatus.Current
                            |> Option.defaultWith (fun () -> failwith "Expected current branch after base commit.")

                        let! _ = context.Git.raw [| "init"; "--bare"; remotePath |]
                        let! _ = context.Git.raw [| "remote"; "add"; "origin"; remotePath |]
                        let! _ = context.Git.raw [| "push"; "-u"; "origin"; initialBranch |]

                        let! createBranchResult =
                            GitService.createBranch context.RepoPath featureBranchName (Some $"origin/{initialBranch}")

                        expectOk "create feature branch from origin/main" createBranchResult |> ignore

                        let! featureStatus =
                            unwrapResultAsync
                                (GitService.getStatus context.RepoPath)
                                (expectOk "git status after branch creation")

                        Vitest.expect(featureStatus.Current).toEqual (Some featureBranchName)
                        Vitest.expect(featureStatus.Tracking |> Option.isNone).toBe (true)
                    })
            }
        )

        Vitest.test (
            "confirmMergeResolution leaves the merge uncommitted when auto-commit is disabled",
            fun () -> promise {
                do!
                    withTempRepository (fun context -> promise {
                        let filePath = join [| context.RepoPath; "conflict.txt" |]
                        let featureBranch = "feature/merge-resolution"

                        do! writeUtf8FileAsync filePath "base\n"

                        let! initialStageResult = GitService.stagePaths context.RepoPath [| "conflict.txt" |]
                        expectOk "stage base file" initialStageResult |> ignore

                        let! initialCommitResult = GitService.commit context.RepoPath "test: base commit"
                        expectOk "commit base file" initialCommitResult |> ignore

                        let! baseStatus =
                            unwrapResultAsync
                                (GitService.getStatus context.RepoPath)
                                (expectOk "git status after base commit")

                        let baseBranch =
                            baseStatus.Current
                            |> Option.defaultWith (fun () -> failwith "Expected current branch after base commit.")

                        let! createBranchResult = GitService.createBranch context.RepoPath featureBranch None
                        expectOk "create merge branch" createBranchResult |> ignore

                        do! writeUtf8FileAsync filePath "feature change\n"

                        let! featureStageResult = GitService.stagePaths context.RepoPath [| "conflict.txt" |]
                        expectOk "stage feature change" featureStageResult |> ignore

                        let! featureCommitResult = GitService.commit context.RepoPath "test: feature change"
                        expectOk "commit feature change" featureCommitResult |> ignore

                        let! checkoutBaseResult = GitService.checkoutBranch context.RepoPath { Name = baseBranch; StartPoint = None }
                        expectOk "checkout base branch" checkoutBaseResult |> ignore

                        do! writeUtf8FileAsync filePath "main change\n"

                        let! mainStageResult = GitService.stagePaths context.RepoPath [| "conflict.txt" |]
                        expectOk "stage main change" mainStageResult |> ignore

                        let! mainCommitResult = GitService.commit context.RepoPath "test: main change"
                        expectOk "commit main change" mainCommitResult |> ignore

                        try
                            let! _ = context.Git.raw [| "merge"; featureBranch |]
                            ()
                        with _ ->
                            ()

                        let! mergeStatus =
                            unwrapResultAsync
                                (GitService.getStatus context.RepoPath)
                                (expectOk "git status during merge")

                        Vitest.expect(mergeStatus.IsMergeInProgress).toBe (true)
                        Vitest.expect(mergeStatus.Conflicted).toEqual ([| "conflict.txt" |])

                        let! mergeView =
                            unwrapResultAsync
                                (GitService.getMergeConflictViewData context.RepoPath "conflict.txt")
                                (expectOk "load merge conflict view")

                        let! resolutionResult =
                            unwrapResultAsync
                                (GitService.confirmMergeResolution
                                    context.RepoPath
                                    "conflict.txt"
                                    mergeView.MergeConflictContent
                                    "resolved content\n"
                                    false)
                                (expectOk "confirm merge resolution")

                        Vitest.expect(resolutionResult.RemainingConflictedPaths).toEqual ([||])
                        Vitest.expect(resolutionResult.NextConflictedPath).toEqual (None)
                        Vitest.expect(resolutionResult.UpdatedStatus.IsMergeInProgress).toBe (true)
                        Vitest.expect(resolutionResult.UpdatedStatus.Conflicted).toEqual ([||])

                        let! refreshedStatus =
                            unwrapResultAsync
                                (GitService.getStatus context.RepoPath)
                                (expectOk "git status after merge resolution")

                        Vitest.expect(refreshedStatus.IsMergeInProgress).toBe (true)
                        Vitest.expect(refreshedStatus.Conflicted).toEqual ([||])

                        let! latestCommitMessage = context.Git.raw [| "log"; "-1"; "--pretty=%s" |]
                        Vitest.expect(latestCommitMessage.Trim()).toBe ("test: main change")

                        let! mergeHead = context.Git.raw [| "rev-parse"; "--verify"; "MERGE_HEAD" |]
                        Vitest.expect(String.IsNullOrWhiteSpace(mergeHead)).toBe (false)
                    })
            }
        )

        Vitest.test (
            "confirmMergeResolution commits the merge when auto-commit is enabled and the last conflict is resolved",
            fun () -> promise {
                do!
                    withTempRepository (fun context -> promise {
                        let filePath = join [| context.RepoPath; "conflict.txt" |]
                        let featureBranch = "feature/merge-resolution-autocommit"

                        do! writeUtf8FileAsync filePath "base\n"

                        let! initialStageResult = GitService.stagePaths context.RepoPath [| "conflict.txt" |]
                        expectOk "stage base file" initialStageResult |> ignore

                        let! initialCommitResult = GitService.commit context.RepoPath "test: base commit"
                        expectOk "commit base file" initialCommitResult |> ignore

                        let! baseStatus =
                            unwrapResultAsync
                                (GitService.getStatus context.RepoPath)
                                (expectOk "git status after base commit")

                        let baseBranch =
                            baseStatus.Current
                            |> Option.defaultWith (fun () -> failwith "Expected current branch after base commit.")

                        let! createBranchResult = GitService.createBranch context.RepoPath featureBranch None
                        expectOk "create merge branch" createBranchResult |> ignore

                        do! writeUtf8FileAsync filePath "feature change\n"

                        let! featureStageResult = GitService.stagePaths context.RepoPath [| "conflict.txt" |]
                        expectOk "stage feature change" featureStageResult |> ignore

                        let! featureCommitResult = GitService.commit context.RepoPath "test: feature change"
                        expectOk "commit feature change" featureCommitResult |> ignore

                        let! checkoutBaseResult = GitService.checkoutBranch context.RepoPath { Name = baseBranch; StartPoint = None }
                        expectOk "checkout base branch" checkoutBaseResult |> ignore

                        do! writeUtf8FileAsync filePath "main change\n"

                        let! mainStageResult = GitService.stagePaths context.RepoPath [| "conflict.txt" |]
                        expectOk "stage main change" mainStageResult |> ignore

                        let! mainCommitResult = GitService.commit context.RepoPath "test: main change"
                        expectOk "commit main change" mainCommitResult |> ignore

                        try
                            let! _ = context.Git.raw [| "merge"; featureBranch |]
                            ()
                        with _ ->
                            ()

                        let! mergeView =
                            unwrapResultAsync
                                (GitService.getMergeConflictViewData context.RepoPath "conflict.txt")
                                (expectOk "load merge conflict view")

                        let! resolutionResult =
                            unwrapResultAsync
                                (GitService.confirmMergeResolution
                                    context.RepoPath
                                    "conflict.txt"
                                    mergeView.MergeConflictContent
                                    "resolved content\n"
                                    true)
                                (expectOk "confirm merge resolution with auto-commit")

                        Vitest.expect(resolutionResult.RemainingConflictedPaths).toEqual ([||])
                        Vitest.expect(resolutionResult.NextConflictedPath).toEqual (None)
                        Vitest.expect(resolutionResult.UpdatedStatus.IsMergeInProgress).toBe (false)
                        Vitest.expect(resolutionResult.UpdatedStatus.Conflicted).toEqual ([||])

                        let! refreshedStatus =
                            unwrapResultAsync
                                (GitService.getStatus context.RepoPath)
                                (expectOk "git status after auto-commit")

                        Vitest.expect(refreshedStatus.IsMergeInProgress).toBe (false)
                        Vitest.expect(refreshedStatus.Conflicted).toEqual ([||])

                        let! latestCommitMessage = context.Git.raw [| "log"; "-1"; "--pretty=%s" |]
                        Vitest.expect(latestCommitMessage.Trim()).toBe ("Resolve merge conflicts")

                        let! mergeHeadResult =
                            runSimpleGitResult
                                (fun git -> git.raw [| "rev-parse"; "--verify"; "MERGE_HEAD" |])
                                context.Git

                        match mergeHeadResult with
                        | Ok _ -> failwith "Expected MERGE_HEAD to be cleared after the merge commit."
                        | Error _ -> ()
                    })
            }
        )

        Vitest.test (
            "getDiffViewData reports explicitly unsupported paths through the unsupported-content sentinel",
            fun () -> promise {
                do!
                    withTempRepository (fun context -> promise {
                        let filePath = join [| context.RepoPath; "image.png" |]

                        do! writeUtf8FileAsync filePath "not really an image"

                        let! failure =
                            unwrapResultAsync (GitService.getDiffViewData context.RepoPath "image.png") expectError

                        let unsupported = GitService.tryGetUnsupportedGitContent "image.png" failure

                        Vitest.expect(unsupported.IsSome).toBe (true)
                        Vitest.expect(unsupported |> Option.map _.Path).toEqual (Some "image.png")
                    })
            }
        )
)
