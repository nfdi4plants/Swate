module ElectronRenderer.GitWorkflowTests

open Browser.Dom
open Browser.Types
open Fable.Core
open Feliz
open Renderer.Context.GitWorkflow
open Swate.Electron.Shared.GitTypes
open Vitest

[<Emit("new Promise((resolve, reject) => { $0(resolve, reject); })")>]
let private createPromise<'T> (executor: ('T -> unit) -> (obj -> unit) -> unit) : JS.Promise<'T> = jsNative

let private createDeferred<'T> () =
    let mutable resolveFn: ('T -> unit) option = None

    let deferredPromise =
        createPromise<'T> (fun resolve _reject ->
            resolveFn <- Some resolve)

    deferredPromise, (fun value -> resolveFn.Value value)

type private WorkflowStore(initialState: GitState) =
    let mutable state = initialState

    member _.Dispatch(msg: Msg) =
        state <- transition msg state

    member _.GetState() = state

    member _.State = state

let private cleanStatus = {
    Current = Some "main"
    Tracking = Some "origin/main"
    Ahead = 0
    Behind = 0
    IsClean = true
    Conflicted = [||]
    IsMergeInProgress = false
    Files = [||]
}

let private conflictedStatus conflictedPaths = {
    Current = Some "main"
    Tracking = Some "origin/main"
    Ahead = 0
    Behind = 0
    IsClean = false
    Conflicted = conflictedPaths
    IsMergeInProgress = conflictedPaths.Length > 0
    Files =
        conflictedPaths
        |> Array.map (fun path -> {
            Path = path
            Index = "U"
            WorkingDir = "U"
            OriginalPath = None
        })
}

let private okOperationResult = {
    Success = true
    Message = None
    FailureKind = None
    Path = None
}

let private unexpectedPromise<'T> (name: string) : JS.Promise<Result<'T, string>> =
    promise { return failwith $"Unexpected call: {name}" }

let private defaultDependencies: GitDependencies = {
    getGitStatus = fun () -> unexpectedPromise "getGitStatus"
    getGitBranches = fun () -> unexpectedPromise "getGitBranches"
    getGitLfsSettings = fun () -> unexpectedPromise "getGitLfsSettings"
    loadDiffPage = fun path -> unexpectedPromise $"loadDiffPage:{path}"
    loadMergeConflictPage = fun path -> unexpectedPromise $"loadMergeConflictPage:{path}"
    installGitLfs = fun () -> unexpectedPromise "installGitLfs"
    gitFetch = fun _ -> unexpectedPromise "gitFetch"
    gitPull = fun _ -> unexpectedPromise "gitPull"
    gitPush = fun _ -> unexpectedPromise "gitPush"
    createBranch = fun _ -> unexpectedPromise "createBranch"
    checkoutBranch = fun _ -> unexpectedPromise "checkoutBranch"
    gitStagePaths = fun _ -> unexpectedPromise "gitStagePaths"
    gitUnstagePaths = fun _ -> unexpectedPromise "gitUnstagePaths"
    gitCommit = fun _ -> unexpectedPromise "gitCommit"
    setGitLfsSettings = fun _ -> unexpectedPromise "setGitLfsSettings"
    confirmGitMergeResolution = fun _ -> unexpectedPromise "confirmGitMergeResolution"
    confirmInstall = fun message -> failwith $"Unexpected install prompt: {message}"
}

let private diffPage path : GitPage =
    GitPage.Diff {
        Path = path
        PreviousContent = "before"
        CurrentContent = "after"
        WordDiffText = "diff"
    }

let private mergeConflictPage path content : GitPage =
    GitPage.MergeConflict {
        Path = path
        MergeConflictContent = content
    }

let private renderToBody (element: ReactElement) = promise {
    let container = document.createElement ("div") :?> HTMLDivElement
    document.body.appendChild container |> ignore
    let root = ReactDOM.createRoot container
    root.render element
    do! Promise.sleep 0
    return
        container,
        (fun () ->
            root.unmount ()
            container.remove ())
}

Vitest.afterEach(fun () ->
    document.body.innerHTML <- "")

Vitest.describe("GitWorkflow renderer behavior", fun () ->
    Vitest.test("confirmMergeResolution ignores a duplicate same-path request while the first call is in flight", fun () -> promise {
        let store = WorkflowStore GitState.Empty
        let confirmDeferred, resolveConfirm = createDeferred<Result<GitConfirmMergeResolutionResult, string>> ()
        let mutable confirmCallCount = 0

        let deps = {
            defaultDependencies with
                confirmGitMergeResolution =
                    fun _ ->
                        confirmCallCount <- confirmCallCount + 1
                        confirmDeferred
        }

        let request = {
            Path = "conflict-a.txt"
            ExpectedConflictContent = "<<<<<<< HEAD\nA\n=======\nB\n>>>>>>> branch\n"
            ResolvedContent = "resolved"
        }

        let firstResultPromise =
            confirmMergeResolution deps (fun () -> true) store.GetState store.Dispatch request

        let secondResultPromise =
            confirmMergeResolution deps (fun () -> true) store.GetState store.Dispatch request

        Vitest.expect(confirmCallCount).toBe(1)
        Vitest.expect(store.State.MergeResolutionPendingPath).toEqual(Some request.Path)

        let payload = {
            UpdatedStatus = cleanStatus
            RemainingConflictedPaths = [||]
            NextConflictedPath = None
        }

        resolveConfirm (Ok payload)

        let! firstResult = firstResultPromise
        let! secondResult = secondResultPromise

        Vitest.expect(firstResult).toEqual(Ok())
        Vitest.expect(secondResult).toEqual(Ok())
        Vitest.expect(confirmCallCount).toBe(1)
        Vitest.expect(store.State.MergeResolutionPendingPath).toEqual(None)
    })

    Vitest.test("loadPage ignores stale responses from older requests", fun () -> promise {
        let store = WorkflowStore GitState.Empty
        let deferredA, resolveA = createDeferred<Result<GitPage, string>> ()
        let deferredB, resolveB = createDeferred<Result<GitPage, string>> ()

        let deps = {
            defaultDependencies with
                loadDiffPage =
                    fun path ->
                        match path with
                        | "A.txt" -> deferredA
                        | "B.txt" -> deferredB
                        | _ -> unexpectedPromise $"loadDiffPage:{path}"
        }

        let loadA = loadPage deps store.GetState store.Dispatch "A.txt" false
        let loadB = loadPage deps store.GetState store.Dispatch "B.txt" false

        let pageA = diffPage "A.txt"
        let pageB = diffPage "B.txt"

        resolveB (Ok pageB)
        let! resultB = loadB

        resolveA (Ok pageA)
        let! resultA = loadA

        Vitest.expect(resultB).toEqual(Ok())
        Vitest.expect(resultA).toEqual(Ok())
        Vitest.expect(store.State.SelectedChangePath).toEqual(Some "B.txt")
        Vitest.expect(store.State.ActivePage).toEqual(Some pageB)
    })

    Vitest.test("confirmMergeResolution opens the next conflicted file and clears pending state", fun () -> promise {
        let initialState = {
            GitState.Empty with
                SelectedChangePath = Some "conflict-a.txt"
                ActivePage = Some(mergeConflictPage "conflict-a.txt" "<<<<<<< HEAD\nA\n=======\nB\n>>>>>>> branch\n")
        }

        let store = WorkflowStore initialState
        let nextPage = mergeConflictPage "conflict-b.txt" "<<<<<<< HEAD\nB\n=======\nC\n>>>>>>> branch\n"

        let deps = {
            defaultDependencies with
                confirmGitMergeResolution =
                    fun _ ->
                        promise {
                            return
                                Ok {
                                    UpdatedStatus = conflictedStatus [| "conflict-b.txt" |]
                                    RemainingConflictedPaths = [| "conflict-b.txt" |]
                                    NextConflictedPath = Some "conflict-b.txt"
                                }
                        }
                loadMergeConflictPage =
                    fun path ->
                        match path with
                        | "conflict-b.txt" -> promise { return Ok nextPage }
                        | _ -> unexpectedPromise $"loadMergeConflictPage:{path}"
        }

        let! result =
            confirmMergeResolution
                deps
                (fun () -> true)
                store.GetState
                store.Dispatch
                {
                    Path = "conflict-a.txt"
                    ExpectedConflictContent = "<<<<<<< HEAD\nA\n=======\nB\n>>>>>>> branch\n"
                    ResolvedContent = "resolved"
                }

        Vitest.expect(result).toEqual(Ok())
        Vitest.expect(store.State.MergeResolutionPendingPath).toEqual(None)
        Vitest.expect(store.State.SelectedChangePath).toEqual(Some "conflict-b.txt")
        Vitest.expect(store.State.ActivePage).toEqual(Some nextPage)
    })

    Vitest.test("runPullWorkflow preserves pull failure and does not start conflict navigation", fun () -> promise {
        let store = WorkflowStore GitState.Empty
        let mutable statusCalls = 0
        let mutable mergePageCalls = 0

        let deps = {
            defaultDependencies with
                gitPull =
                    fun _ ->
                        promise {
                            return
                                Ok {
                                    okOperationResult with
                                        Success = false
                                        Message = Some "Pull failed."
                                }
                        }
                getGitStatus =
                    fun () ->
                        statusCalls <- statusCalls + 1
                        promise { return Ok(conflictedStatus [| "conflict-a.txt" |]) }
                loadMergeConflictPage =
                    fun path ->
                        mergePageCalls <- mergePageCalls + 1
                        unexpectedPromise $"loadMergeConflictPage:{path}"
        }

        let! result = runPullWorkflow deps (fun () -> true) store.GetState store.Dispatch

        Vitest.expect(result).toEqual(Error "Pull failed.")
        Vitest.expect(statusCalls).toBe(0)
        Vitest.expect(mergePageCalls).toBe(0)
        Vitest.expect(store.State.ErrorNotice).toEqual(Some "Pull failed.")
        Vitest.expect(store.State.ActivePage).toEqual(None)
    })

    Vitest.test("runGitOperationWithLfsInstallRetry retries the original operation exactly once after install", fun () -> promise {
        let store = WorkflowStore GitState.Empty
        let mutable operationCalls = 0
        let mutable installCalls = 0
        let mutable promptCalls = 0

        let deps = {
            defaultDependencies with
                installGitLfs =
                    fun () ->
                        installCalls <- installCalls + 1
                        promise { return Ok okOperationResult }
                confirmInstall =
                    fun _ ->
                        promptCalls <- promptCalls + 1
                        true
        }

        let operation () =
            operationCalls <- operationCalls + 1

            promise {
                if operationCalls = 1 then
                    return
                        Ok {
                            okOperationResult with
                                Success = false
                                Message = Some "Install Git LFS now?"
                                FailureKind = Some GitFailureKind.LfsInstallRequired
                        }
                else
                    return Ok okOperationResult
            }

        let! result =
            runGitOperationWithLfsInstallRetry
                deps
                store.Dispatch
                GitBusyOperation.PushingToRemote
                true
                operation

        Vitest.expect(result).toEqual(Ok okOperationResult)
        Vitest.expect(operationCalls).toBe(2)
        Vitest.expect(installCalls).toBe(1)
        Vitest.expect(promptCalls).toBe(1)
        Vitest.expect(store.State.InstallRetryState).toEqual(GitInstallRetryState.Idle)
    })

    Vitest.test("GitMergeConflictViewer disables Confirm Merge while confirmation is blocked", fun () -> promise {
        let mutable confirmCalls = 0

        let! container, cleanup =
            renderToBody (
                Swate.Components.GitMergeConflictViewer.Viewer(
                    mergeConflictContent = "<<<<<<< HEAD\nA\n=======\nB\n>>>>>>> branch\n",
                    defaultResolvedContent = "resolved",
                    onConfirmMerge = (fun _ -> confirmCalls <- confirmCalls + 1),
                    confirmDisabled = true,
                    testIdPrefix = "renderer-workflow-test"
                )
            )

        let confirmButton =
            container.querySelector ("[data-testid='renderer-workflow-test-confirm-merge']")
            :?> HTMLButtonElement

        Vitest.expect(confirmButton.disabled).toBe(true)
        confirmButton.click ()
        Vitest.expect(confirmCalls).toBe(0)

        cleanup ()
    })
)
