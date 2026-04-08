module ElectronRenderer.GitWorkflowTests

open Browser.Dom
open Browser.Types
open Elmish
open Fable.Core
open Feliz
open Renderer.Context.GitWorkflow
open Renderer.Types
open Swate.Components.GitSidebarTypes
open Swate.Electron.Shared.GitTypes
open Vitest

[<Emit("new Promise((resolve, reject) => { $0(resolve, reject); })")>]
let private createPromise<'T> (executor: ('T -> unit) -> (obj -> unit) -> unit) : JS.Promise<'T> = jsNative

let private createDeferred<'T> () =
    let mutable resolveFn: ('T -> unit) option = None

    let deferredPromise =
        createPromise<'T> (fun resolve _reject -> resolveFn <- Some resolve)

    deferredPromise, (fun value -> resolveFn.Value value)

type private WorkflowStore(initialState: GitState) =
    let mutable state = initialState

    member _.Dispatch(msg: Msg) = state <- transition msg state

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

let private statusForBranch branchName = {
    cleanStatus with
        Current = Some branchName
        Tracking = Some $"origin/{branchName}"
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
    WarningMessage = None
    WarningKind = None
    Path = None
}

let private localBranch refName isCurrent isTracking = {
    RefName = refName
    DisplayLabel = refName
    Kind = GitBranchRefKind.Local
    IsCurrent = isCurrent
    IsTracking = isTracking
}

let private sidebarLocalBranch refName isCurrent isTracking : GitSidebarBranchOption = {
    RefName = refName
    DisplayLabel = refName
    Kind = GitSidebarBranchKind.Local
    IsCurrent = isCurrent
    IsTracking = isTracking
}

let private sidebarRemoteBranch refName isTracking : GitSidebarBranchOption = {
    RefName = refName
    DisplayLabel = refName
    Kind = GitSidebarBranchKind.Remote
    IsCurrent = false
    IsTracking = isTracking
}

let private lfsSettings thresholdMb downloadLargeFiles = {
    AutoTrackThresholdMb = thresholdMb
    DownloadLargeFiles = downloadLargeFiles
}

let private changedFile path indexStatus workingTreeStatus isConflicted = {
    Path = path
    OriginalPath = None
    IndexStatus = indexStatus
    WorkingTreeStatus = workingTreeStatus
    IsConflicted = isConflicted
}

let private unexpectedPromise<'T> (name: string) : JS.Promise<Result<'T, string>> = promise {
    return failwith $"Unexpected call: {name}"
}

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
    gitCloneRepository = fun _ -> unexpectedPromise "gitCloneRepository"
    createBranch = fun _ -> unexpectedPromise "createBranch"
    checkoutBranch = fun _ -> unexpectedPromise "checkoutBranch"
    gitStagePaths = fun _ -> unexpectedPromise "gitStagePaths"
    gitUnstagePaths = fun _ -> unexpectedPromise "gitUnstagePaths"
    gitCommit = fun _ -> unexpectedPromise "gitCommit"
    setGitLfsSettings = fun _ -> unexpectedPromise "setGitLfsSettings"
    confirmGitMergeResolution = fun _ -> unexpectedPromise "confirmGitMergeResolution"
    confirmInstall = fun message -> failwith $"Unexpected install prompt: {message}"
}

let private diffPage path : PageState =
    PageState.GitDiffPage {
        Path = path
        PreviousContent = "before"
        CurrentContent = "after"
        WordDiffText = "diff"
    }

let private mergeConflictPage path content : PageState =
    PageState.GitMergeConflictPage {
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
            container.remove ()
        )
}

Vitest.afterEach (fun () -> document.body.innerHTML <- "")

let private collectMessages (cmd: Cmd<Msg>) = promise {
    let messages = ResizeArray<Msg>()
    cmd |> List.iter (fun sub -> sub messages.Add)
    do! Promise.sleep 0
    return messages |> Seq.toArray
}

Vitest.describe("GitWorkflow request preparation", fun () ->
    Vitest.test("prepareCommitAll snapshots distinct changed paths from the current model", fun () ->
        let state = {
            GitState.Empty with
                ChangedFiles = [|
                    changedFile "a.txt" "M" " " false
                    changedFile "b.txt" "M" " " false
                    changedFile "a.txt" "M" " " false
                |]
        }

        let prepared = prepareCommitAll state "  save everything  "

        Vitest.expect(prepared.NormalizedMessage).toBe("save everything")
        Vitest.expect(prepared.PathsToCommit).toEqual([| "a.txt"; "b.txt" |])
        Vitest.expect(prepared.CurrentlyStagedPaths).toEqual([||])
        Vitest.expect(prepared.BusyOperation).toEqual(GitBusyOperation.CommittingAllChanges)
    )

    Vitest.test("prepareCommitSelection snapshots already staged paths before rewriting the stage", fun () ->
        let state = {
            GitState.Empty with
                ChangedFiles = [|
                    changedFile "staged.txt" "M" " " false
                    changedFile "unstaged.txt" "." "M" false
                |]
        }

        let prepared =
            prepareCommitSelection
                state
                {
                    Message = "  save selected  "
                    Paths = [| "unstaged.txt"; "staged.txt"; "unstaged.txt" |]
                }

        Vitest.expect(prepared.NormalizedMessage).toBe("save selected")
        Vitest.expect(prepared.PathsToCommit).toEqual([| "unstaged.txt"; "staged.txt" |])
        Vitest.expect(prepared.CurrentlyStagedPaths).toEqual([| "staged.txt" |])
        Vitest.expect(prepared.BusyOperation).toEqual(GitBusyOperation.CommittingSelectedChanges)
    )

    Vitest.test("buildUpdatedLfsSettings keeps untouched values from the current model snapshot", fun () ->
        let state = {
            GitState.Empty with
                LfsAutoTrackThresholdMb = 7
                DownloadLargeFiles = true
        }

        Vitest.expect(buildUpdatedLfsSettings state (Some 12) None).toEqual({
            AutoTrackThresholdMb = 12
            DownloadLargeFiles = true
        })

        Vitest.expect(buildUpdatedLfsSettings state None (Some false)).toEqual({
            AutoTrackThresholdMb = 7
            DownloadLargeFiles = false
        })
    )
)

Vitest.describe("GitWorkflow update command flow", fun () ->
    Vitest.test("ArcPathChanged clears page state and schedules a refresh when switching repositories", fun () -> promise {
        let clearedPages = ResizeArray<PageState option>()
        let setPageState pageState = clearedPages.Add pageState

        let state = {
            GitState.Empty with
                CurrentArcPath = Some "C:/arc-a"
                SelectedChangePath = Some "tracked.txt"
                BusyOperation = Some GitBusyOperation.Refreshing
        }

        let nextState, cmd = update defaultDependencies setPageState (ArcPathChanged(Some "C:/arc-b")) state
        let! messages = collectMessages cmd

        Vitest.expect(nextState).toEqual({
            GitState.Empty with
                CurrentArcPath = Some "C:/arc-b"
        })
        Vitest.expect(clearedPages |> Seq.toArray).toEqual([| None |])

        match messages with
        | [| RefreshRequested _ |] -> ()
        | _ -> failwith $"Unexpected follow-up message count: {messages.Length}"
    })

    Vitest.test("RefreshCompleted ignores stale responses and still resolves the older caller", fun () -> promise {
        let mutable replyResult = None
        let reply result = replyResult <- Some result

        let state = {
            GitState.Empty with
                CurrentArcPath = Some "C:/arc-a"
                RefreshRequestId = 2
                Status = {
                    GitState.Empty.Status with
                        CurrentBranch = Some "feature/live"
                }
        }

        let staleRefresh = {
            Status = Ok(statusForBranch "feature/stale")
            Branches = Ok [| localBranch "feature/stale" true true |]
            LfsSettings = Ok(lfsSettings 9 false)
        }

        let nextState, cmd = update defaultDependencies ignore (RefreshCompleted(1, reply, Ok staleRefresh)) state
        let! _ = collectMessages cmd

        Vitest.expect(nextState.Status.CurrentBranch).toEqual(Some "feature/live")
        Vitest.expect(replyResult).toEqual(Some(Ok()))
    })

    Vitest.test("RefreshCompleted ignores stale failures and keeps the current model untouched", fun () -> promise {
        let mutable replyResult = None
        let reply result = replyResult <- Some result

        let state = {
            GitState.Empty with
                CurrentArcPath = Some "C:/arc-a"
                RefreshRequestId = 2
                ErrorNotice = Some "keep current error"
                Status = {
                    GitState.Empty.Status with
                        CurrentBranch = Some "feature/live"
                }
        }

        let nextState, cmd = update defaultDependencies ignore (RefreshCompleted(1, reply, Error "older refresh failed")) state
        let! _ = collectMessages cmd

        Vitest.expect(nextState.Status.CurrentBranch).toEqual(Some "feature/live")
        Vitest.expect(nextState.ErrorNotice).toEqual(Some "keep current error")
        Vitest.expect(replyResult).toEqual(Some(Ok()))
    })

    Vitest.test("SelectChangeCompleted leaves the current selection untouched when an older request finishes late", fun () -> promise {
        let mutable replyResult = None
        let reply result = replyResult <- Some result

        let state = {
            GitState.Empty with
                CurrentArcPath = Some "C:/arc-a"
                PageLoadRequestId = 2
                SelectedChangePath = Some "B.txt"
        }

        let nextState, cmd =
            update
                defaultDependencies
                ignore
                (SelectChangeCompleted(1, "A.txt", reply, Ok(GitPageChange.Set(diffPage "A.txt"))))
                state

        let! _ = collectMessages cmd

        Vitest.expect(nextState.SelectedChangePath).toEqual(Some "B.txt")
        Vitest.expect(replyResult).toEqual(Some(Ok()))
    })

    Vitest.test("SelectChangeCompleted ignores stale failures and keeps the current selection/error untouched", fun () -> promise {
        let mutable replyResult = None
        let reply result = replyResult <- Some result

        let state = {
            GitState.Empty with
                CurrentArcPath = Some "C:/arc-a"
                PageLoadRequestId = 2
                SelectedChangePath = Some "B.txt"
                ErrorNotice = None
        }

        let nextState, cmd =
            update
                defaultDependencies
                ignore
                (SelectChangeCompleted(1, "A.txt", reply, Error "older load failed"))
                state

        let! _ = collectMessages cmd

        Vitest.expect(nextState.SelectedChangePath).toEqual(Some "B.txt")
        Vitest.expect(nextState.ErrorNotice).toEqual(None)
        Vitest.expect(replyResult).toEqual(Some(Ok()))
    })

    Vitest.test("ConfirmMergeResolutionCompleted refreshes git state after stale merge-conflict errors", fun () -> promise {
        let clearedPages = ResizeArray<PageState option>()
        let setPageState pageState = clearedPages.Add pageState
        let mutable replyResult = None
        let reply result = replyResult <- Some result

        let state = {
            GitState.Empty with
                CurrentArcPath = Some "C:/arc-a"
                SelectedChangePath = Some "conflict-a.txt"
                MergeResolutionPendingPath = Some "conflict-a.txt"
                BusyOperation = Some(GitBusyOperation.ConfirmingMergeResolution "conflict-a.txt")
        }

        let nextState, cmd =
            update
                defaultDependencies
                setPageState
                (ConfirmMergeResolutionCompleted(reply, Error "File is not currently marked as conflicted."))
                state

        let! messages = collectMessages cmd

        Vitest.expect(nextState.SelectedChangePath).toEqual(None)
        Vitest.expect(nextState.MergeResolutionPendingPath).toEqual(None)
        Vitest.expect(clearedPages |> Seq.toArray).toEqual([| None |])

        match messages with
        | [| RefreshRequested _ |] -> ()
        | _ -> failwith $"Unexpected follow-up message count: {messages.Length}"

        Vitest.expect(replyResult).toEqual(Some(Error "File is not currently marked as conflicted."))
    })
)

Vitest.describe (
    "GitWorkflow renderer behavior",
    fun () ->
        Vitest.test (
            "confirmMergeResolution ignores a duplicate same-path request while the first call is in flight",
            fun () -> promise {
                let store = WorkflowStore GitState.Empty

                let confirmDeferred, resolveConfirm =
                    createDeferred<Result<GitConfirmMergeResolutionResult, string>> ()

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
                    AutoCommit = false
                }

                let firstResultPromise =
                    confirmMergeResolution deps (fun () -> true) store.GetState store.Dispatch request

                let secondResultPromise =
                    confirmMergeResolution deps (fun () -> true) store.GetState store.Dispatch request

                Vitest.expect(confirmCallCount).toBe (1)
                Vitest.expect(store.State.MergeResolutionPendingPath).toEqual (Some request.Path)

                let payload = {
                    UpdatedStatus = cleanStatus
                    RemainingConflictedPaths = [||]
                    NextConflictedPath = None
                }

                resolveConfirm (Ok payload)

                let! firstResult = firstResultPromise
                let! secondResult = secondResultPromise

                Vitest.expect(firstResult).toEqual (Ok GitPageChange.Clear)
                Vitest.expect(secondResult).toEqual (Ok GitPageChange.NoChange)
                Vitest.expect(confirmCallCount).toBe (1)
                Vitest.expect(store.State.MergeResolutionPendingPath).toEqual (None)
            }
        )

        Vitest.test (
            "confirmMergeResolution ignores a second request while another conflict is still confirming",
            fun () -> promise {
                let store =
                    WorkflowStore {
                        GitState.Empty with
                            BusyOperation = Some(GitBusyOperation.ConfirmingMergeResolution "conflict-a.txt")
                            MergeResolutionPendingPath = Some "conflict-a.txt"
                    }

                let mutable confirmCallCount = 0

                let deps = {
                    defaultDependencies with
                        confirmGitMergeResolution =
                            fun _ ->
                                confirmCallCount <- confirmCallCount + 1
                                unexpectedPromise "confirmGitMergeResolution"
                }

                let! result =
                    confirmMergeResolution deps (fun () -> true) store.GetState store.Dispatch {
                        Path = "conflict-b.txt"
                        ExpectedConflictContent = "<<<<<<< HEAD\nB\n=======\nC\n>>>>>>> branch\n"
                        ResolvedContent = "resolved"
                        AutoCommit = false
                    }

                Vitest.expect(result).toEqual (Ok GitPageChange.NoChange)
                Vitest.expect(confirmCallCount).toBe (0)
                Vitest.expect(store.State.MergeResolutionPendingPath).toEqual (Some "conflict-a.txt")
            }
        )

        Vitest.test (
            "loadPage ignores stale responses from older requests",
            fun () -> promise {
                let store = WorkflowStore GitState.Empty
                let deferredA, resolveA = createDeferred<Result<PageState, string>> ()
                let deferredB, resolveB = createDeferred<Result<PageState, string>> ()

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

                Vitest.expect(resultB).toEqual (Ok(GitPageChange.Set pageB))
                Vitest.expect(resultA).toEqual (Ok GitPageChange.NoChange)
                Vitest.expect(store.State.SelectedChangePath).toEqual (Some "B.txt")
            }
        )

        Vitest.test (
            "refreshAll ignores stale responses from older requests",
            fun () -> promise {
                let store = WorkflowStore GitState.Empty

                let statusDeferredA, resolveStatusA =
                    createDeferred<Result<GitStatusDto, string>> ()

                let statusDeferredB, resolveStatusB =
                    createDeferred<Result<GitStatusDto, string>> ()

                let mutable statusCalls = 0
                let branchProfiles = ResizeArray<string>()
                let lfsProfiles = ResizeArray<string>()

                let deps = {
                    defaultDependencies with
                        getGitStatus =
                            fun () ->
                                statusCalls <- statusCalls + 1

                                match statusCalls with
                                | 1 -> statusDeferredA
                                | 2 -> statusDeferredB
                                | _ -> unexpectedPromise "getGitStatus"
                        getGitBranches =
                            fun () ->
                                let profile = branchProfiles.[0]
                                branchProfiles.RemoveAt 0

                                match profile with
                                | "A" -> promise { return Ok [| localBranch "feature/a" true true |] }
                                | "B" -> promise { return Ok [| localBranch "feature/b" true true |] }
                                | _ -> unexpectedPromise $"getGitBranches:{profile}"
                        getGitLfsSettings =
                            fun () ->
                                let profile = lfsProfiles.[0]
                                lfsProfiles.RemoveAt 0

                                match profile with
                                | "A" -> promise { return Ok(lfsSettings 3 true) }
                                | "B" -> promise { return Ok(lfsSettings 9 false) }
                                | _ -> unexpectedPromise $"getGitLfsSettings:{profile}"
                }

                let refreshA = refreshAll deps (fun () -> true) store.GetState store.Dispatch
                let refreshB = refreshAll deps (fun () -> true) store.GetState store.Dispatch

                branchProfiles.Add "B"
                lfsProfiles.Add "B"
                resolveStatusB (Ok(statusForBranch "feature/b"))
                let! resultB = refreshB

                branchProfiles.Add "A"
                lfsProfiles.Add "A"
                resolveStatusA (Ok(statusForBranch "feature/a"))
                let! resultA = refreshA

                Vitest.expect(resultB).toEqual (Ok(Some(statusForBranch "feature/b")))
                Vitest.expect(resultA).toEqual (Ok None)
                Vitest.expect(store.State.Status.CurrentBranch).toEqual (Some "feature/b")
                Vitest.expect(store.State.BranchOptions |> Array.map _.RefName).toEqual ([| "feature/b" |])
                Vitest.expect(store.State.LfsAutoTrackThresholdMb).toBe (9)
                Vitest.expect(store.State.DownloadLargeFiles).toBe (false)
            }
        )

        Vitest.test (
            "applyStatus keeps the selected path clear when the viewed file disappears from changed files",
            fun () ->
                let model = {
                    GitState.Empty with
                        SelectedChangePath = Some "tracked.txt"
                        ChangedFiles = [|
                            {
                                Path = "tracked.txt"
                                OriginalPath = None
                                IndexStatus = "M"
                                WorkingTreeStatus = " "
                                IsConflicted = false
                            }
                        |]
                }

                let nextModel = applyStatus cleanStatus model

                Vitest.expect(nextModel.SelectedChangePath).toEqual (None)
        )

        Vitest.test (
            "confirmMergeResolution opens the next conflicted file and clears pending state",
            fun () -> promise {
                let initialState = {
                    GitState.Empty with
                        SelectedChangePath = Some "conflict-a.txt"
                }

                let store = WorkflowStore initialState

                let nextPage =
                    mergeConflictPage "conflict-b.txt" "<<<<<<< HEAD\nB\n=======\nC\n>>>>>>> branch\n"

                let deps = {
                    defaultDependencies with
                        confirmGitMergeResolution =
                            fun _ -> promise {
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
                    confirmMergeResolution deps (fun () -> true) store.GetState store.Dispatch {
                        Path = "conflict-a.txt"
                        ExpectedConflictContent = "<<<<<<< HEAD\nA\n=======\nB\n>>>>>>> branch\n"
                        ResolvedContent = "resolved"
                        AutoCommit = false
                    }

                Vitest.expect(result).toEqual (Ok(GitPageChange.Set nextPage))
                Vitest.expect(store.State.MergeResolutionPendingPath).toEqual (None)
                Vitest.expect(store.State.SelectedChangePath).toEqual (Some "conflict-b.txt")
            }
        )

        Vitest.test (
            "runPullWorkflow preserves pull failure and does not start conflict navigation",
            fun () -> promise {
                let store = WorkflowStore GitState.Empty
                let mutable statusCalls = 0
                let mutable mergePageCalls = 0

                let deps = {
                    defaultDependencies with
                        gitPull =
                            fun _ -> promise {
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

                Vitest.expect(result).toEqual (Error "Pull failed.")
                Vitest.expect(statusCalls).toBe (0)
                Vitest.expect(mergePageCalls).toBe (0)
                Vitest.expect(store.State.ErrorNotice).toEqual (Some "Pull failed.")
            }
        )

        Vitest.test (
            "runPullWorkflow fails when LFS hydration fails after pull",
            fun () -> promise {
                let store = WorkflowStore GitState.Empty

                let warningMessage =
                    "Git pull completed, but Git LFS download failed: hydration failed."

                let deps = {
                    defaultDependencies with
                        gitPull =
                            fun _ -> promise {
                                return
                                    Ok {
                                        okOperationResult with
                                            Success = false
                                            Message = Some warningMessage
                                            FailureKind = Some GitFailureKind.Network
                                    }
                            }
                }

                let! result = runPullWorkflow deps (fun () -> true) store.GetState store.Dispatch

                Vitest.expect(result).toEqual (Error warningMessage)
                Vitest.expect(store.State.ErrorNotice).toEqual (Some warningMessage)
                Vitest.expect(store.State.WarningNotice).toEqual (None)
            }
        )

        Vitest.test (
            "GitState.Empty defaults DownloadLargeFiles to false until settings load",
            fun () -> Vitest.expect(GitState.Empty.DownloadLargeFiles).toBe (false)
        )

        Vitest.test (
            "shouldPublishCurrentBranchFirst is true when the current branch has no upstream or matching remote",
            fun () ->
                let state = {
                    GitState.Empty with
                        Status = {
                            GitState.Empty.Status with
                                CurrentBranch = Some "feature/local-only"
                                TrackingBranch = None
                        }
                        BranchOptions = [| sidebarLocalBranch "feature/local-only" true false |]
                }

                Vitest.expect(shouldPublishCurrentBranchFirst state).toBe (true)
        )

        Vitest.test (
            "runSyncWorkflow publishes a new local-only branch before attempting pull",
            fun () -> promise {
                let store =
                    WorkflowStore {
                        GitState.Empty with
                            Status = {
                                GitState.Empty.Status with
                                    CurrentBranch = Some "feature/local-only"
                                    TrackingBranch = None
                            }
                            BranchOptions = [| sidebarLocalBranch "feature/local-only" true false |]
                    }

                let mutable pullCalls = 0
                let mutable pushCalls = 0

                let deps = {
                    defaultDependencies with
                        gitPull =
                            fun _ ->
                                pullCalls <- pullCalls + 1
                                unexpectedPromise "gitPull"
                        gitPush =
                            fun _ ->
                                pushCalls <- pushCalls + 1
                                promise { return Ok okOperationResult }
                        getGitStatus =
                            fun () -> promise {
                                return
                                    Ok {
                                        statusForBranch "feature/local-only" with
                                            Tracking = Some "origin/feature/local-only"
                                    }
                            }
                        getGitBranches =
                            fun () -> promise {
                                return
                                    Ok [|
                                        localBranch "feature/local-only" true true
                                        {
                                            RefName = "origin/feature/local-only"
                                            DisplayLabel = "origin/feature/local-only"
                                            Kind = GitBranchRefKind.Remote
                                            IsCurrent = false
                                            IsTracking = true
                                        }
                                    |]
                            }
                        getGitLfsSettings = fun () -> promise { return Ok(lfsSettings 5 true) }
                }

                let! result = runSyncWorkflow deps (fun () -> true) store.GetState store.Dispatch

                Vitest.expect(result).toEqual (Ok GitPageChange.NoChange)
                Vitest.expect(pullCalls).toBe (0)
                Vitest.expect(pushCalls).toBe (1)
                Vitest.expect(store.State.Status.TrackingBranch).toEqual (Some "origin/feature/local-only")
            }
        )

        Vitest.test (
            "runSyncWorkflow stops when pull fails after LFS hydration failure",
            fun () -> promise {
                let store = WorkflowStore GitState.Empty
                let mutable pushCalls = 0

                let hydrationFailureMessage =
                    "Git pull completed, but Git LFS download failed: hydration failed."

                let deps = {
                    defaultDependencies with
                        gitPull =
                            fun _ -> promise {
                                return
                                    Ok {
                                        okOperationResult with
                                            Success = false
                                            Message = Some hydrationFailureMessage
                                            FailureKind = Some GitFailureKind.Network
                                    }
                            }
                        gitPush =
                            fun _ ->
                                pushCalls <- pushCalls + 1
                                promise { return Ok okOperationResult }
                        getGitStatus = fun () -> promise { return Ok cleanStatus }
                        getGitBranches = fun () -> promise { return Ok [| localBranch "main" true true |] }
                        getGitLfsSettings = fun () -> promise { return Ok(lfsSettings 5 false) }
                }

                let! result = runSyncWorkflow deps (fun () -> true) store.GetState store.Dispatch

                Vitest.expect(result).toEqual (Error hydrationFailureMessage)
                Vitest.expect(pushCalls).toBe (0)
                Vitest.expect(store.State.ErrorNotice).toEqual (Some hydrationFailureMessage)
                Vitest.expect(store.State.WarningNotice).toEqual (None)
            }
        )

        Vitest.test (
            "runGitOperationWithLfsInstallRetry retries the original operation exactly once after install",
            fun () -> promise {
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

                Vitest.expect(result).toEqual (Ok okOperationResult)
                Vitest.expect(operationCalls).toBe (2)
                Vitest.expect(installCalls).toBe (1)
                Vitest.expect(promptCalls).toBe (1)
                Vitest.expect(store.State.InstallRetryState).toEqual (GitInstallRetryState.Idle)
            }
        )

        Vitest.test (
            "GitMergeConflictViewer disables Confirm Merge while confirmation is blocked",
            fun () -> promise {
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

                Vitest.expect(confirmButton.disabled).toBe (true)
                confirmButton.click ()
                Vitest.expect(confirmCalls).toBe (0)

                cleanup ()
            }
        )

        Vitest.test (
            "GitSidebar shows when the current local branch has no upstream yet",
            fun () -> promise {
                let status: GitSidebarStatus = {
                    CurrentBranch = Some "feature/local-only"
                    TrackingBranch = None
                    Ahead = 0
                    Behind = 0
                    IsClean = true
                    IsMergeInProgress = false
                }

                let! container, cleanup =
                    renderToBody (
                        Swate.Components.GitSidebar.Main(
                            status = status,
                            changedFiles = [||],
                            branchOptions = [| sidebarLocalBranch "feature/local-only" true false |],
                            callbacks = {
                                OnRefresh = fun () -> promise { return Ok() }
                                OnFetch = fun () -> promise { return Ok() }
                                OnPull = fun () -> promise { return Ok() }
                                OnPush = fun () -> promise { return Ok() }
                                OnSync = fun () -> promise { return Ok() }
                                OnCommitSelection = fun _ -> promise { return Ok() }
                                OnCommitAll = fun _ -> promise { return Ok() }
                                OnSaveDownloadLargeFiles = fun _ -> promise { return Ok() }
                                OnSaveLfsAutoTrackThreshold = fun _ -> promise { return Ok() }
                                OnCreateBranch = fun _ -> promise { return Ok() }
                                OnSwitchBranch = fun _ -> promise { return Ok() }
                                OnSelectChange = fun _ -> promise { return Ok() }
                            },
                            downloadLargeFiles = true,
                            lfsAutoTrackThresholdMb = 5
                        )
                    )

                Vitest
                    .expect(
                        container.textContent.Contains(
                            "No upstream configured yet. Push will publish and track origin/feature/local-only."
                        )
                    )
                    .toBe (true)

                cleanup ()
            }
        )
)
