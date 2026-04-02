module ElectronCore.GitServiceTests

open System
open Fable.Core
open Fable.Core.JS
open Fable.Core.JsInterop
open Main.Bindings.Path
open Main.Bindings.SimpleGit
open Vitest

module GitService = Main.Git.GitService
module GitProvisioningService = Main.Git.GitProvisioningService
module GitAuthAdapter = Main.Git.GitAuthAdapter
module GitLfsService = Main.Git.GitLfsService

let private fsPromisesDynamic: obj = importAll "fs/promises"
let private osDynamic: obj = importAll "os"

type private TempRepositoryContext = {
    RootPath: string
    RepoPath: string
    Git: ISimpleGit
}

let private createSimpleGit (repoPath: string) =
    let options =
        SimpleGitOptions(
            baseDir = repoPath,
            binary = U3.Case1 "git",
            maxConcurrentProcesses = 1
        )

    SimpleGit.create options |> GitAuthAdapter.applyNonInteractiveEnv

let private createTempDirectoryAsync () : JS.Promise<string> =
    let prefix =
        join [|
            osDynamic?tmpdir() |> unbox<string>
            "swate-electron-git-tests-"
        |]

    fsPromisesDynamic?mkdtemp(prefix) |> unbox<JS.Promise<string>>

let private removeDirectoryAsync (path: string) : JS.Promise<unit> = promise {
    let! _ =
        fsPromisesDynamic?rm(path, createObj [ "recursive" ==> true; "force" ==> true ])
        |> unbox<JS.Promise<obj>>

    return ()
}

let private configureRepositoryAsync (git: ISimpleGit) : JS.Promise<unit> = promise {
    let! _ = git.raw [| "config"; "user.name"; "Swate Electron Tests" |]
    let! _ = git.raw [| "config"; "user.email"; "swate-electron-tests@example.org" |]
    let! _ = git.raw [| "config"; "core.autocrlf"; "false" |]
    return ()
}

let private ensureDirectoryAsync (path: string) : JS.Promise<unit> = promise {
    let! _ =
        fsPromisesDynamic?mkdir(path, createObj [ "recursive" ==> true ])
        |> unbox<JS.Promise<obj>>

    return ()
}

let private writeUtf8FileAsync (path: string) (content: string) : JS.Promise<unit> = promise {
    let! _ = fsPromisesDynamic?writeFile(path, content, "utf8") |> unbox<JS.Promise<obj>>
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

let private splitNonEmptyLines (text: string) =
    text.Split([| '\r'; '\n' |], StringSplitOptions.RemoveEmptyEntries)

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

Vitest.describe("GitService local repository workflow", fun () ->
    Vitest.test("initializes, commits, diffs, recommits, and switches branches locally", fun () -> promise {
        do!
            withTempRepository (fun context -> promise {
                let notesDirectory = join [| context.RepoPath; "notes" |]
                let filePath = join [| context.RepoPath; "notes"; "workflow.txt" |]

                do! ensureDirectoryAsync notesDirectory
                do! writeUtf8FileAsync filePath "alpha\nbeta\ngamma\n"

                let! stageResult = GitService.stagePaths context.RepoPath [| commitWorkflowFilePath |]
                expectOk "git add" stageResult |> ignore

                let! stagedStatus =
                    unwrapResultAsync (GitService.getStatus context.RepoPath) (expectOk "git status after staging")

                let stagedFile =
                    stagedStatus.Files
                    |> Microsoft.FSharp.Collections.Array.find (fun file -> file.Path = commitWorkflowFilePath)

                Vitest.expect(stagedStatus.IsClean).toBe(false)
                Vitest.expect(stagedFile.Index).toBe("A")

                let! firstCommitHash =
                    unwrapResultAsync (GitService.commit context.RepoPath initialCommitMessage) (expectOk "first commit")

                Vitest.expect(firstCommitHash.Length).toBeGreaterThan(6)

                let! cleanStatus =
                    unwrapResultAsync (GitService.getStatus context.RepoPath) (expectOk "git status after first commit")

                let initialBranch =
                    cleanStatus.Current
                    |> Option.defaultWith (fun () -> failwith "Expected initialized repository to have a current branch.")

                Vitest.expect(cleanStatus.IsClean).toBe(true)

                do! writeUtf8FileAsync filePath "alpha\nbeta updated\ndelta\n"

                let! modifiedStatus =
                    unwrapResultAsync (GitService.getStatus context.RepoPath) (expectOk "git status after file change")

                let modifiedFile =
                    modifiedStatus.Files
                    |> Microsoft.FSharp.Collections.Array.find (fun file -> file.Path = commitWorkflowFilePath)

                Vitest.expect(modifiedStatus.IsClean).toBe(false)
                Vitest.expect(modifiedFile.WorkingDir).toBe("M")

                let! diffSummary =
                    unwrapResultAsync (GitService.getDiffSummary context.RepoPath) (expectOk "git diff summary")

                Vitest.expect(diffSummary.Changed).toBe(1)
                Vitest.expect(diffSummary.Insertions).toBe(2)
                Vitest.expect(diffSummary.Deletions).toBe(2)

                let! diffText =
                    unwrapResultAsync (GitService.getDiff context.RepoPath [| commitWorkflowFilePath |]) (expectOk "git diff")

                Vitest.expect(diffText.Contains("diff --git")).toBe(true)
                Vitest.expect(diffText.Contains("@@")).toBe(true)
                Vitest.expect(diffText.Contains("-beta")).toBe(true)
                Vitest.expect(diffText.Contains("+beta updated")).toBe(true)
                Vitest.expect(diffText.Contains("-gamma")).toBe(true)
                Vitest.expect(diffText.Contains("+delta")).toBe(true)

                let! wordDiffText =
                    unwrapResultAsync (GitService.getWordDiff context.RepoPath [| commitWorkflowFilePath |]) (expectOk "git word diff")

                Vitest.expect(wordDiffText.Contains("diff --git")).toBe(true)
                Vitest.expect(wordDiffText.Contains("@@ -2,2 +2,2 @@")).toBe(true)
                Vitest.expect(wordDiffText.Contains("-beta")).toBe(false)
                Vitest.expect(wordDiffText.Contains("-gamma")).toBe(true)
                Vitest.expect(wordDiffText.Contains("+updated")).toBe(true)
                Vitest.expect(wordDiffText.Contains("+delta")).toBe(true)
                Vitest.expect(wordDiffText.Contains("~")).toBe(true)

                let! secondStageResult = GitService.stagePaths context.RepoPath [| commitWorkflowFilePath |]
                expectOk "git add after edit" secondStageResult |> ignore

                let! secondCommitHash =
                    unwrapResultAsync (GitService.commit context.RepoPath secondCommitMessage) (expectOk "second commit")

                Vitest.expect(secondCommitHash.Length).toBeGreaterThan(6)

                let! logOutput = context.Git.raw [| "log"; "-2"; "--pretty=%s" |]
                let logMessages = splitNonEmptyLines logOutput
                Vitest.expect(logMessages.Length).toBe(2)
                Vitest.expect(logMessages.[0]).toBe(secondCommitMessage)
                Vitest.expect(logMessages.[1]).toBe(initialCommitMessage)

                let! createBranchResult = GitService.createBranch context.RepoPath featureBranchName None
                expectOk "create branch" createBranchResult |> ignore

                let! createdBranchStatus =
                    unwrapResultAsync (GitService.getStatus context.RepoPath) (expectOk "git status after create branch")

                Vitest.expect(createdBranchStatus.Current |> Option.defaultValue "").toBe(featureBranchName)

                let! checkoutBaseBranchResult = GitService.checkoutBranch context.RepoPath initialBranch
                expectOk "checkout initial branch" checkoutBaseBranchResult |> ignore

                let! baseBranchStatus =
                    unwrapResultAsync (GitService.getStatus context.RepoPath) (expectOk "git status after checkout")

                Vitest.expect(baseBranchStatus.Current |> Option.defaultValue "").toBe(initialBranch)

                let! checkoutFeatureBranchResult = GitService.checkoutBranch context.RepoPath featureBranchName
                expectOk "checkout feature branch" checkoutFeatureBranchResult |> ignore

                let! featureBranchStatus =
                    unwrapResultAsync (GitService.getStatus context.RepoPath) (expectOk "git status on feature branch")

                Vitest.expect(featureBranchStatus.Current |> Option.defaultValue "").toBe(featureBranchName)
            })
    })

    Vitest.test("unstagePaths keeps working tree changes but removes staged state", fun () -> promise {
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
                    unwrapResultAsync (GitService.getStatus context.RepoPath) (expectOk "git status after unstage")

                let fileStatus =
                    status.Files
                    |> Microsoft.FSharp.Collections.Array.find (fun file -> file.Path = "tracked.txt")

                Vitest.expect(status.IsClean).toBe(false)
                Vitest.expect(fileStatus.WorkingDir).toBe("M")
                Vitest.expect(fileStatus.Index = "M").toBe(false)
            })
    })

    Vitest.test("commit keeps the staged version when the working tree changes again before commit", fun () -> promise {
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
                Vitest.expect(headContent).toBe("version A\n")

                let! workingTreeStatus =
                    unwrapResultAsync (GitService.getStatus context.RepoPath) (expectOk "git status after divergent commit")

                let trackedFileStatus =
                    workingTreeStatus.Files
                    |> Microsoft.FSharp.Collections.Array.find (fun file -> file.Path = "tracked.txt")

                Vitest.expect(trackedFileStatus.WorkingDir).toBe("M")
            })
    })

    Vitest.test("planOutboundPush skips LFS upload when outbound commits do not contain LFS pointers", fun () -> promise {
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
                        (fun git -> runSimpleGitResult (fun currentGit -> currentGit.status ()) git)
                        "origin"
                        None
                        context.Git

                let actualPlan = expectOkResult "plan outbound push without lfs" plan
                Vitest.expect(actualPlan).toEqual(GitLfsService.OutboundPushPlan.SkipLfsUpload)
            })
    })

    Vitest.test("planOutboundPush requires LFS upload when outbound commits contain staged LFS pointers", fun () -> promise {
        do!
            withTempRepository (fun context -> promise {
                let remotePath = join [| context.RootPath; "remote-lfs.git" |]
                let filePath = join [| context.RepoPath; "artifact.bin" |]

                let! _ = context.Git.raw [| "init"; "--bare"; remotePath |]
                let! _ = context.Git.raw [| "remote"; "add"; "origin"; remotePath |]
                let! _ = context.Git.raw [| "lfs"; "install"; "--local" |]
                let! _ = context.Git.raw [| "lfs"; "track"; "*.bin" |]

                do! writeUtf8FileAsync filePath "binary-ish content\n"

                let! stageResult = GitService.stagePaths context.RepoPath [| ".gitattributes"; "artifact.bin" |]
                expectOk "stage lfs-managed file" stageResult |> ignore

                let! commitResult = GitService.commit context.RepoPath "test: lfs pointer commit"
                expectOk "commit lfs-managed file" commitResult |> ignore

                let! plan =
                    GitLfsService.planOutboundPush
                        runSimpleGitResult
                        (fun git -> runSimpleGitResult (fun currentGit -> currentGit.status ()) git)
                        "origin"
                        None
                        context.Git

                let actualPlan = expectOkResult "plan outbound push with lfs" plan

                match actualPlan with
                | GitLfsService.OutboundPushPlan.UploadLfsObjects refSpec ->
                    Vitest.expect(String.IsNullOrWhiteSpace refSpec).toBe(false)
                | GitLfsService.OutboundPushPlan.SkipLfsUpload ->
                    failwith "Expected outbound LFS upload to be required."
            })
    })

    Vitest.test("planOutboundPush skips LFS upload when the remote is already up to date but local origin refs are stale", fun () -> promise {
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

                let! stageResult = GitService.stagePaths context.RepoPath [| ".gitattributes"; "artifact.bin" |]
                expectOk "stage lfs-managed file for stale remote ref test" stageResult |> ignore

                let! commitResult = GitService.commit context.RepoPath "test: lfs pointer commit for stale remote ref test"
                expectOk "commit lfs-managed file for stale remote ref test" commitResult |> ignore

                let! _ = context.Git.raw [| "branch"; "-M"; branchName |]
                let! _ = context.Git.raw [| "push"; "--set-upstream"; "origin"; branchName |]
                let! _ = context.Git.raw [| "update-ref"; "-d"; $"refs/remotes/origin/{branchName}" |]

                let! plan =
                    GitLfsService.planOutboundPush
                        runSimpleGitResult
                        (fun git -> runSimpleGitResult (fun currentGit -> currentGit.status ()) git)
                        "origin"
                        None
                        context.Git

                let actualPlan = expectOkResult "plan outbound push with stale local remote refs" plan
                Vitest.expect(actualPlan).toEqual(GitLfsService.OutboundPushPlan.SkipLfsUpload)
            })
    })

    Vitest.test("executePushWorkflow skips the LFS upload step when the plan does not require it", fun () -> promise {
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
                    promise { return Ok GitLfsService.OutboundPushPlan.SkipLfsUpload })
                (fun refSpec ->
                    events.Add $"upload:{refSpec}"
                    promise { return Ok() })
                (fun _ ->
                    events.Add "push"
                    promise { return Ok() })
                (fun _ -> promise { return None })

        expectOk "execute push workflow without lfs upload" result |> ignore
        Vitest.expect(events.ToArray()).toEqual([| "plan"; "push" |])
    })

    Vitest.test("executePushWorkflow uploads LFS objects before pushing when the plan requires it", fun () -> promise {
        let events = ResizeArray<string>()

        let pushTarget: GitService.GitPushTarget = {
            RefSpec = "feature/upload-lfs"
            PushBranch = "feature/upload-lfs"
            SetUpstream = true
        }

        let! result =
            GitService.executePushWorkflow
                pushTarget
                (fun () ->
                    events.Add "plan"
                    promise { return Ok(GitLfsService.OutboundPushPlan.UploadLfsObjects "refs/heads/feature/upload-lfs") })
                (fun refSpec ->
                    events.Add $"upload:{refSpec}"
                    promise { return Ok() })
                (fun _ ->
                    events.Add "push"
                    promise { return Ok() })
                (fun _ -> promise { return None })

        expectOk "execute push workflow with lfs upload" result |> ignore

        Vitest.expect(events.ToArray()).toEqual(
            [|
                "plan"
                "upload:refs/heads/feature/upload-lfs"
                "push"
            |]
        )
    })

    Vitest.test("executePushWorkflow appends diagnostics when the LFS upload step fails", fun () -> promise {
        let uploadFailure: GitService.GitFailure = {
            Kind = GitService.GitFailureKind.Network
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
                (fun () ->
                    promise { return Ok(GitLfsService.OutboundPushPlan.UploadLfsObjects "refs/heads/feature/diagnostics") })
                (fun _ -> promise { return Error uploadFailure })
                (fun _ -> promise { return Ok() })
                (fun _ -> promise { return Some "Git LFS Env:\n[REDACTED]" })

        let failure = expectError result
        Vitest.expect(failure.Kind).toEqual(GitService.GitFailureKind.Network)
        Vitest.expect(failure.Message.Contains("lfs upload failed")).toBe(true)
        Vitest.expect(failure.Message.Contains("LFS diagnostics")).toBe(true)
    })

    Vitest.test("rejects invalid diff pathspecs before invoking git", fun () -> promise {
        do!
            withTempRepository (fun context -> promise {
                let! failure =
                    unwrapResultAsync (GitService.getDiff context.RepoPath [| "../outside.txt" |]) expectError

                Vitest.expect(failure.Message.Contains("traversal")).toBe(true)
            })
    })

    Vitest.test("rejects invalid word diff pathspecs before invoking git", fun () -> promise {
        do!
            withTempRepository (fun context -> promise {
                let! failure =
                    unwrapResultAsync (GitService.getWordDiff context.RepoPath [| "../outside.txt" |]) expectError

                Vitest.expect(failure.Message.Contains("traversal")).toBe(true)
            })
    })

    Vitest.test("fails when initializing a repository twice at the same target path", fun () -> promise {
        let! rootPath = createTempDirectoryAsync ()

        try
            let repoPath = join [| rootPath; "repo" |]

            let! firstInitResult = GitProvisioningService.initRepository repoPath
            let normalizedRepoPath = expectOk "first init" firstInitResult
            Vitest.expect(normalizedRepoPath.Length).toBeGreaterThan(0)

            let! secondInitResult = GitProvisioningService.initRepository repoPath
            let failure = expectError secondInitResult

            Vitest.expect(failure.Message.Contains("already a git repository")).toBe(true)
            do! removeDirectoryAsync rootPath
        with error ->
            do! removeDirectoryAsync rootPath
            return raise error
    })

    Vitest.test("fails when checking out a branch that does not exist locally", fun () -> promise {
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
                        (GitService.checkoutBranch context.RepoPath "missing/local-branch")
                        expectError

                Vitest.expect(failure.Message.Contains("does not exist in the local repository")).toBe(true)
            })
    })

    Vitest.test("checkoutBranch switches tracking from origin/main to the matching remote branch", fun () -> promise {
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
                    unwrapResultAsync (GitService.getStatus context.RepoPath) (expectOk "git status after base commit")

                let initialBranch =
                    baseStatus.Current
                    |> Option.defaultWith (fun () -> failwith "Expected current branch after base commit.")

                let! _ = context.Git.raw [| "init"; "--bare"; remotePath |]
                let! _ = context.Git.raw [| "remote"; "add"; "origin"; remotePath |]
                let! _ = context.Git.raw [| "push"; "-u"; "origin"; initialBranch |]

                let! createBranchResult = GitService.createBranch context.RepoPath featureBranchName None
                expectOk "create feature branch" createBranchResult |> ignore

                let! _ = context.Git.raw [| "push"; "-u"; "origin"; featureBranchName |]
                let! _ = context.Git.raw [| "branch"; $"--set-upstream-to=origin/{initialBranch}"; featureBranchName |]

                let! poisonedStatus =
                    unwrapResultAsync (GitService.getStatus context.RepoPath) (expectOk "git status after poisoning tracking")

                Vitest.expect(poisonedStatus.Tracking).toEqual(Some $"origin/{initialBranch}")

                let! checkoutBaseResult = GitService.checkoutBranch context.RepoPath initialBranch
                expectOk "checkout base branch" checkoutBaseResult |> ignore

                let! checkoutFeatureResult = GitService.checkoutBranch context.RepoPath featureBranchName
                expectOk "checkout feature branch" checkoutFeatureResult |> ignore

                let! featureStatus =
                    unwrapResultAsync (GitService.getStatus context.RepoPath) (expectOk "git status after checkout feature branch")

                Vitest.expect(featureStatus.Current).toEqual(Some featureBranchName)
                Vitest.expect(featureStatus.Tracking).toEqual(Some $"origin/{featureBranchName}")

                let! branchOptions =
                    unwrapResultAsync (GitService.getBranches context.RepoPath) (expectOk "git branches after checkout feature branch")

                let localFeatureBranch =
                    branchOptions
                    |> Microsoft.FSharp.Collections.Array.find (fun branch -> branch.RefName = featureBranchName)

                let remoteFeatureBranch =
                    branchOptions
                    |> Microsoft.FSharp.Collections.Array.find (fun branch -> branch.RefName = $"origin/{featureBranchName}")

                Vitest.expect(localFeatureBranch.Kind).toEqual(GitService.GitBranchRefKind.Local)
                Vitest.expect(localFeatureBranch.IsCurrent).toBe(true)
                Vitest.expect(localFeatureBranch.IsTracking).toBe(true)
                Vitest.expect(remoteFeatureBranch.Kind).toEqual(GitService.GitBranchRefKind.Remote)
                Vitest.expect(remoteFeatureBranch.IsTracking).toBe(true)
            })
    })

    Vitest.test("createBranch clears inherited origin/main tracking when the new remote branch does not exist yet", fun () -> promise {
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
                    unwrapResultAsync (GitService.getStatus context.RepoPath) (expectOk "git status after base commit")

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
                    unwrapResultAsync (GitService.getStatus context.RepoPath) (expectOk "git status after branch creation")

                Vitest.expect(featureStatus.Current).toEqual(Some featureBranchName)
                Vitest.expect(featureStatus.Tracking |> Option.isNone).toBe(true)
            })
    })
)
