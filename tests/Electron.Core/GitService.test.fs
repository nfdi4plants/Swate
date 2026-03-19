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

    Vitest.test("rejects invalid diff pathspecs before invoking git", fun () -> promise {
        do!
            withTempRepository (fun context -> promise {
                let! failure =
                    unwrapResultAsync (GitService.getDiff context.RepoPath [| "../outside.txt" |]) expectError

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
)
