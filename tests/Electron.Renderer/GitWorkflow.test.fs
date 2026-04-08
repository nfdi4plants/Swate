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

Vitest.describe("GitWorkflow write request flow", fun () ->
    Vitest.test("SaveDownloadLargeFilesRequested updates local state immediately when no ARC is loaded", fun () -> promise {
        let mutable replyResult = None
        let reply result = replyResult <- Some result

        let state = {
            GitState.Empty with
                CurrentArcPath = None
                LfsAutoTrackThresholdMb = 5
                DownloadLargeFiles = false
        }

        let nextState, cmd =
            update
                defaultDependencies
                ignore
                (SaveDownloadLargeFilesRequested(true, reply))
                state

        let! _ = collectMessages cmd

        Vitest.expect(nextState.DownloadLargeFiles).toBe(true)
        Vitest.expect(replyResult).toEqual(Some(Ok()))
    })

    Vitest.test("WriteRequested allows clone requests when no ARC is loaded", fun () -> promise {
        let mutable replyResult = None
        let reply result = replyResult <- Some result

        let request = {
            RemoteUrl = "https://example.org/repo.git"
            TargetPath = "C:/clone-target"
            Branch = None
            DownloadLargeFiles = false
        }

        let deps = {
            defaultDependencies with
                gitCloneRepository =
                    fun cloneRequest ->
                        Vitest.expect(cloneRequest).toEqual(request)

                        promise {
                            return
                                Ok {
                                    okOperationResult with
                                        Path = Some "C:/clone-target"
                                }
                        }
        }

        let stateAfterRequest, requestCmd = update deps ignore (WriteRequested(Clone(request, reply))) GitState.Empty
        let! requestMessages = collectMessages requestCmd

        let _, finishCmd =
            match requestMessages with
            | [| WriteCompleted(Clone _, Ok(Completed(CloneSuccess "C:/clone-target"))) |] ->
                update deps ignore requestMessages[0] stateAfterRequest
            | _ -> failwith "Expected the clone request to complete successfully."

        let! _ = collectMessages finishCmd

        Vitest.expect(stateAfterRequest.BusyOperation).toEqual(Some GitBusyOperation.CloningRepository)
        Vitest.expect(replyResult).toEqual(Some(Ok "C:/clone-target"))
    })

    Vitest.test("WriteCompleted applies the refreshed snapshot after push success", fun () -> promise {
        let mutable replyResult = None
        let reply result = replyResult <- Some result

        let deps = {
            defaultDependencies with
                gitPush = fun _ -> promise { return Ok okOperationResult }
                getGitStatus = fun () -> promise { return Ok(statusForBranch "feature/pushed") }
                getGitBranches = fun () -> promise { return Ok [| localBranch "feature/pushed" true true |] }
                getGitLfsSettings = fun () -> promise { return Ok(lfsSettings 9 true) }
        }

        let initialState = {
            GitState.Empty with
                CurrentArcPath = Some "C:/arc-a"
                Status = {
                    GitState.Empty.Status with
                        CurrentBranch = Some "feature/stale"
                }
                LfsAutoTrackThresholdMb = 1
                DownloadLargeFiles = false
        }

        let stateAfterRequest, requestCmd = update deps ignore (WriteRequested(Push reply)) initialState
        let! requestMessages = collectMessages requestCmd

        let nextState, finishCmd =
            match requestMessages with
            | [| WriteCompleted(Push _, Ok(Completed(UnitSuccess(_, GitPageChange.NoChange, None, None)))) |] ->
                update deps ignore requestMessages[0] stateAfterRequest
            | _ -> failwith "Expected the push request to complete with a refreshed snapshot."

        let! _ = collectMessages finishCmd

        Vitest.expect(nextState.Status.CurrentBranch).toEqual(Some "feature/pushed")
        Vitest.expect(nextState.BranchOptions |> Array.map _.RefName).toEqual([| "feature/pushed" |])
        Vitest.expect(nextState.LfsAutoTrackThresholdMb).toBe(9)
        Vitest.expect(nextState.DownloadLargeFiles).toBe(true)
        Vitest.expect(replyResult).toEqual(Some(Ok()))
    })

    Vitest.test("WriteRequested retries the original operation exactly once after Git LFS installation", fun () -> promise {
        let mutable promptCalls = 0
        let mutable installCalls = 0
        let mutable pushCalls = 0
        let mutable replyResult = None

        let deps = {
            defaultDependencies with
                confirmInstall =
                    fun _ ->
                        promptCalls <- promptCalls + 1
                        true
                installGitLfs =
                    fun () ->
                        installCalls <- installCalls + 1
                        promise { return Ok okOperationResult }
                gitPush =
                    fun _ -> promise {
                        pushCalls <- pushCalls + 1

                        if pushCalls = 1 then
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
                getGitStatus = fun () -> promise { return Ok cleanStatus }
                getGitBranches = fun () -> promise { return Ok [| localBranch "main" true true |] }
                getGitLfsSettings = fun () -> promise { return Ok(lfsSettings 5 false) }
        }

        let reply result = replyResult <- Some result

        let initialState = {
            GitState.Empty with
                CurrentArcPath = Some "C:/arc-a"
        }

        let stateAfterRequest, requestCmd = update deps ignore (WriteRequested(Push reply)) initialState
        let! requestMessages = collectMessages requestCmd

        let stateAfterWriteCompleted, promptCmd =
            match requestMessages with
            | [| WriteCompleted(Push _, Ok(RequiresLfsInstall _)) |] ->
                update deps ignore requestMessages[0] stateAfterRequest
            | _ -> failwith "Expected a Git LFS install prompt request."

        let! promptMessages = collectMessages promptCmd

        let stateAfterPromptAnswer, installCmd =
            match promptMessages with
            | [| WriteInstallPromptAnswered(Push _, true) |] ->
                update deps ignore promptMessages[0] stateAfterWriteCompleted
            | _ -> failwith "Expected an affirmative Git LFS install answer."

        let! installMessages = collectMessages installCmd

        let stateAfterInstall, retryCmd =
            match installMessages with
            | [| WriteInstallCompleted(Push _, Ok _) |] ->
                update deps ignore installMessages[0] stateAfterPromptAnswer
            | _ -> failwith "Expected a successful Git LFS install completion."

        let! retryMessages = collectMessages retryCmd

        let _, finishCmd =
            match retryMessages with
            | [| WriteCompleted(Push _, Ok(Completed(UnitSuccess(_, _, _, _)))) |] ->
                update deps ignore retryMessages[0] stateAfterInstall
            | _ -> failwith "Expected the retried push to succeed."

        let! _ = collectMessages finishCmd

        Vitest.expect(promptCalls).toBe(1)
        Vitest.expect(installCalls).toBe(1)
        Vitest.expect(pushCalls).toBe(2)
        Vitest.expect(replyResult).toEqual(Some(Ok()))
    })
)

Vitest.describe (
    "GitWorkflow renderer behavior",
    fun () ->
        Vitest.test (
            "ConfirmMergeResolutionRequested ignores a second request while another conflict is still confirming",
            fun () -> promise {
                let mutable replyResult = None

                let state = {
                    GitState.Empty with
                        CurrentArcPath = Some "C:/arc-a"
                        BusyOperation = Some(GitBusyOperation.ConfirmingMergeResolution "conflict-a.txt")
                        MergeResolutionPendingPath = Some "conflict-a.txt"
                }

                let request = {
                    Path = "conflict-b.txt"
                    ExpectedConflictContent = "<<<<<<< HEAD\nB\n=======\nC\n>>>>>>> branch\n"
                    ResolvedContent = "resolved"
                    AutoCommit = false
                }

                let nextState, cmd =
                    update
                        defaultDependencies
                        ignore
                        (ConfirmMergeResolutionRequested(request, fun result -> replyResult <- Some result))
                        state

                let! _ = collectMessages cmd

                Vitest.expect(nextState).toEqual(state)
                Vitest.expect(replyResult).toEqual(Some(Ok()))
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
