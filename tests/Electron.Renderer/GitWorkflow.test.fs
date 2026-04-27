module ElectronRenderer.GitWorkflowTests

open System
open Browser.Dom
open Browser.Types
open Elmish
open Fable.Core
open Fable.Core.JsInterop
open Feliz
open Renderer.Context.GitWorkflow
open Renderer.Types
open Swate.Components.Api.GitLabApi
open Swate.Components.GitSidebarTypes
open Swate.Electron.Shared.GitTypes
open Vitest

[<Import("renderToStaticMarkup", "react-dom/server")>]
let private renderToStaticMarkup (element: ReactElement) : string = jsNative

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

let private sidebarProgress stage percent = {
    Method = Some "git"
    Stage = Some stage
    ProgressPercent = Some percent
}

let private changedFile path indexStatus workingTreeStatus isConflicted = {
    Path = path
    OriginalPath = None
    IndexStatus = indexStatus
    WorkingTreeStatus = workingTreeStatus
    IsConflicted = isConflicted
}

[<Emit("new Event($0)")>]
let private createEvent (eventType: string) : Browser.Types.Event = jsNative

[<Emit("new MouseEvent($0, $1)")>]
let private createMouseEvent (eventType: string) (eventInit: obj) : Browser.Types.MouseEvent = jsNative

[<Emit("""
globalThis.__swateOriginalFetch = globalThis.fetch;
globalThis.__swateGitLabCreateProjectFetches = [];
globalThis.fetch = async (url, options) => {
  const body = options && options.body ? JSON.parse(options.body) : null;
  globalThis.__swateGitLabCreateProjectFetches.push({ url, options, body });
  return {
    ok: true,
    status: 201,
    headers: { get: () => null },
    json: async () => ({
      id: 123,
      name: body.name,
      path_with_namespace: "carol/my-arc-project",
      name_with_namespace: "carol / " + body.name,
      description: null,
      web_url: "https://gitlab.example/carol/my-arc-project",
      http_url_to_repo: "https://gitlab.example/carol/my-arc-project.git",
      ssh_url_to_repo: null,
      avatar_url: null,
      visibility: "private",
      star_count: 0,
      created_at: "2026-04-13T00:00:00Z",
      last_activity_at: "2026-04-13T00:00:00Z",
      topics: [],
      tag_list: [],
      namespace: {
        id: 5,
        name: "carol",
        kind: "user",
        full_path: "carol"
      }
    })
  };
};
""")>]
let private installGitLabCreateProjectFetchSpy () : unit = jsNative

[<Emit("globalThis.__swateGitLabCreateProjectFetches[globalThis.__swateGitLabCreateProjectFetches.length - 1].body")>]
let private lastGitLabCreateProjectBody () : obj = jsNative

[<Emit("Object.prototype.hasOwnProperty.call($0, $1)")>]
let private hasOwnProperty (target: obj) (propertyName: string) : bool = jsNative

[<Emit("$0.firstElementChild")>]
let private firstElementChild (target: HTMLElement) : HTMLElement = jsNative

[<Emit("$0[$1]")>]
let private getProperty<'T> (target: obj) (propertyName: string) : 'T = jsNative

[<Emit("""
if (globalThis.__swateOriginalFetch === undefined) {
  delete globalThis.fetch;
} else {
  globalThis.fetch = globalThis.__swateOriginalFetch;
}
delete globalThis.__swateOriginalFetch;
delete globalThis.__swateGitLabCreateProjectFetches;
""")>]
let private cleanupGitLabCreateProjectFetchSpy () : unit = jsNative

let private manyChangedFiles count =
    [|
        for index in 0 .. count - 1 do
            changedFile (sprintf "src/file-%03i.txt" index) "M" " " false
    |]

let private joinLines (lines: string array) =
    String.concat "\n" lines + "\n"

let private buildAddedFileDiff (path: string) (lines: string array) =
    [
        "new file mode 100644"
        "--- /dev/null"
        $"+++ b/{path}"
        $"@@ -0,0 +1,{lines.Length} @@"
        yield! lines |> Array.map (fun line -> $"+{line}")
        "~"
        ""
    ]
    |> String.concat "\n"

let private buildSingleConflictDocument (currentLines: string array) (incomingLines: string array) =
    [
        "<<<<<<< HEAD"
        yield! currentLines
        "======="
        yield! incomingLines
        ">>>>>>> origin/main"
        ""
    ]
    |> String.concat "\n"

let private countOccurrences (needle: string) (haystack: string) =
    let rec loop startIndex count =
        let nextIndex = haystack.IndexOf(needle, startIndex, StringComparison.Ordinal)

        if nextIndex < 0 then
            count
        else
            loop (nextIndex + needle.Length) (count + 1)

    loop 0 0

let private unexpectedPromise<'T> (name: string) : JS.Promise<Result<'T, string>> = promise {
    return failwith $"Unexpected call: {name}"
}

let private defaultDependencies: GitDependencies = {
    getGitStatus = fun () -> unexpectedPromise "getGitStatus"
    getGitBranches = fun () -> unexpectedPromise "getGitBranches"
    getGitLfsSettings = fun () -> unexpectedPromise "getGitLfsSettings"
    loadDiffPage = fun path -> unexpectedPromise $"loadDiffPage:{path}"
    loadMergeConflictPage = fun path -> unexpectedPromise $"loadMergeConflictPage:{path}"
    initGitRepository = fun path -> unexpectedPromise $"initGitRepository:{path}"
    createDataHubProject = fun name -> unexpectedPromise $"createDataHubProject:{name}"
    installGitLfs = fun () -> unexpectedPromise "installGitLfs"
    previewGitPull = fun _ -> unexpectedPromise "previewGitPull"
    gitFetch = fun _ -> unexpectedPromise "gitFetch"
    gitPull = fun _ -> unexpectedPromise "gitPull"
    gitPush = fun _ -> unexpectedPromise "gitPush"
    gitAddRemote = fun _ -> unexpectedPromise "gitAddRemote"
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

let private noopCallbacks: GitSidebarCallbacks = {
    OnRefresh = fun () -> ()
    OnFetch = fun () -> ()
    OnPull = fun () -> ()
    OnPush = fun () -> ()
    OnUpdateFromOnline = fun () -> ()
    OnPrimarySaveSelection = fun _ -> ()
    OnPrimarySaveAll = fun _ -> ()
    OnCommitSelection = fun _ -> ()
    OnCommitAll = fun _ -> ()
    OnConfirmPendingRemoteAction = fun () -> ()
    OnCancelPendingRemoteAction = fun () -> ()
    OnSaveDownloadLargeFiles = fun _ -> ()
    OnSaveLfsAutoTrackThreshold = fun _ -> ()
    OnCreateBranch = fun _ -> ()
    OnSwitchBranch = fun _ -> ()
    OnSelectChange = fun _ -> promise { return Ok() }
}

Vitest.afterEach (fun () -> document.body.innerHTML <- "")

let private collectMessages (cmd: Cmd<Msg>) = promise {
    let messages = ResizeArray<Msg>()
    cmd |> List.iter (fun sub -> sub messages.Add)
    do! Promise.sleep 0
    return messages |> Seq.toArray
}

Vitest.describe (
    "GitWorkflow request preparation",
    fun () ->
        Vitest.test (
            "prepareCommitAll snapshots distinct changed paths from the current model",
            fun () ->
                let state = {
                    GitState.Empty with
                        ChangedFiles = [|
                            changedFile "a.txt" "M" " " false
                            changedFile "b.txt" "M" " " false
                            changedFile "a.txt" "M" " " false
                        |]
                }

                let prepared = prepareCommitAll state "  save everything  "

                Vitest.expect(prepared.NormalizedMessage).toBe ("save everything")
                Vitest.expect(prepared.PathsToCommit).toEqual ([| "a.txt"; "b.txt" |])
                Vitest.expect(prepared.CurrentlyStagedPaths).toEqual ([||])
                Vitest.expect(prepared.BusyOperation).toEqual (GitBusyOperation.CommittingAllChanges)
        )

        Vitest.test (
            "prepareCommitSelection snapshots already staged paths before rewriting the stage",
            fun () ->
                let state = {
                    GitState.Empty with
                        ChangedFiles = [|
                            changedFile "staged.txt" "M" " " false
                            changedFile "unstaged.txt" "." "M" false
                        |]
                }

                let prepared =
                    prepareCommitSelection state {
                        Message = "  save selected  "
                        Paths = [| "unstaged.txt"; "staged.txt"; "unstaged.txt" |]
                    }

                Vitest.expect(prepared.NormalizedMessage).toBe ("save selected")
                Vitest.expect(prepared.PathsToCommit).toEqual ([| "unstaged.txt"; "staged.txt" |])
                Vitest.expect(prepared.CurrentlyStagedPaths).toEqual ([| "staged.txt" |])
                Vitest.expect(prepared.BusyOperation).toEqual (GitBusyOperation.CommittingSelectedChanges)
        )

        Vitest.test (
            "buildUpdatedLfsSettings keeps untouched values from the current model snapshot",
            fun () ->
                let state = {
                    GitState.Empty with
                        LfsAutoTrackThresholdMb = 7
                        DownloadLargeFiles = true
                }

                Vitest
                    .expect(buildUpdatedLfsSettings state (Some 12) None)
                    .toEqual (
                        {
                            AutoTrackThresholdMb = 12
                            DownloadLargeFiles = true
                        }
                    )

                Vitest
                    .expect(buildUpdatedLfsSettings state None (Some false))
                    .toEqual (
                        {
                            AutoTrackThresholdMb = 7
                            DownloadLargeFiles = false
                        }
                    )
        )

        Vitest.test (
            "GitLabApi.CreateProject lets GitLab generate the project path from the submitted name",
            fun () -> promise {
                installGitLabCreateProjectFetchSpy ()

                try
                    let! result =
                        GitLabApi.CreateProject(
                            "https://gitlab.example/",
                            "token-123",
                            " My ARC Project "
                        )

                    match result with
                    | Error err -> failwith err.GitLabErrorToString
                    | Ok project ->
                        Vitest.expect(project.name).toBe("My ARC Project")
                        Vitest.expect(project.path_with_namespace).toBe("carol/my-arc-project")

                    let body = lastGitLabCreateProjectBody ()

                    Vitest.expect(getProperty<string> body "name").toBe("My ARC Project")
                    Vitest.expect(getProperty<bool> body "initialize_with_readme").toBe(false)
                    Vitest.expect(hasOwnProperty body "path").toBe(false)
                finally
                    cleanupGitLabCreateProjectFetchSpy ()
            }
        )
)
Vitest.describe (
    "GitWorkflow update command flow",
    fun () ->
        Vitest.test (
            "ArcPathChanged clears page state and schedules a refresh when switching repositories",
            fun () -> promise {
                let clearedPages = ResizeArray<PageState option>()
                let setPageState pageState = clearedPages.Add pageState

                let state = {
                    GitState.Empty with
                        CurrentArcPath = Some "C:/arc-a"
                        SelectedChangePath = Some "tracked.txt"
                        BusyOperation = Some GitBusyOperation.Refreshing
                }

                let nextState, cmd =
                    update defaultDependencies setPageState (ArcPathChanged(Some "C:/arc-b")) state

                let! messages = collectMessages cmd

                Vitest
                    .expect(nextState)
                    .toEqual (
                        {
                            GitState.Empty with
                                CurrentArcPath = Some "C:/arc-b"
                                ArcSessionId = 1
                        }
                    )

                Vitest.expect(clearedPages |> Seq.toArray).toEqual ([| None |])

                match messages with
                | [| RefreshRequested |] -> ()
                | _ -> failwith $"Unexpected follow-up message count: {messages.Length}"
            }
        )

        Vitest.test (
            "RefreshCompleted ignores stale responses without emitting follow-up callback work",
            fun () -> promise {
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

                let nextState, cmd =
                    update defaultDependencies ignore (RefreshCompleted(1, Ok staleRefresh)) state

                let! messages = collectMessages cmd

                Vitest.expect(nextState.Status.CurrentBranch).toEqual (Some "feature/live")
                Vitest.expect(messages).toEqual ([||])
            }
        )

        Vitest.test (
            "RefreshCompleted ignores stale failures and keeps the current model untouched",
            fun () -> promise {
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

                let nextState, cmd =
                    update defaultDependencies ignore (RefreshCompleted(1, Error "older refresh failed")) state

                let! messages = collectMessages cmd

                Vitest.expect(nextState.Status.CurrentBranch).toEqual (Some "feature/live")
                Vitest.expect(nextState.ErrorNotice).toEqual (Some "keep current error")
                Vitest.expect(messages).toEqual ([||])
            }
        )

        Vitest.test (
            "RefreshCompleted records missing-repository state instead of leaving the sidebar in generic error mode",
            fun () -> promise {
                let clearedPages = ResizeArray<PageState option>()
                let setPageState pageState = clearedPages.Add pageState

                let state = {
                    GitState.Empty with
                        CurrentArcPath = Some "C:/arc-a"
                        RefreshRequestId = 1
                        RefreshState = GitRefreshState.Loading
                        BusyOperation = Some GitBusyOperation.Refreshing
                }

                let nextState, cmd =
                    update
                        defaultDependencies
                        setPageState
                        (RefreshCompleted(1, Error "git status failed (Unknown): The selected ARC path is not a git repository."))
                        state

                let! messages = collectMessages cmd

                Vitest.expect(nextState.RepositoryAvailability).toEqual (GitRepositoryAvailability.MissingRepository)
                Vitest.expect(nextState.ErrorNotice).toEqual (None)
                Vitest.expect(nextState.ChangedFiles).toEqual ([||])
                Vitest.expect(clearedPages |> Seq.toArray).toEqual ([| None |])
                Vitest.expect(messages).toEqual ([||])
            }
        )

        Vitest.test (
            "SelectChangeCompleted leaves the current selection untouched when an older request finishes late",
            fun () -> promise {
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

                Vitest.expect(nextState.SelectedChangePath).toEqual (Some "B.txt")
                Vitest.expect(replyResult).toEqual (Some(Ok()))
            }
        )

        Vitest.test (
            "SelectChangeCompleted ignores stale failures and keeps the current selection/error untouched",
            fun () -> promise {
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

                Vitest.expect(nextState.SelectedChangePath).toEqual (Some "B.txt")
                Vitest.expect(nextState.ErrorNotice).toEqual (None)
                Vitest.expect(replyResult).toEqual (Some(Ok()))
            }
        )

        Vitest.test (
            "ConfirmMergeResolutionCompleted refreshes git state after stale merge-conflict errors",
            fun () -> promise {
                let clearedPages = ResizeArray<PageState option>()
                let setPageState pageState = clearedPages.Add pageState

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
                        (ConfirmMergeResolutionCompleted(
                            state.ArcSessionId,
                            Error "File is not currently marked as conflicted."
                        ))
                        state

                let! messages = collectMessages cmd

                Vitest.expect(nextState.SelectedChangePath).toEqual (None)
                Vitest.expect(nextState.MergeResolutionPendingPath).toEqual (None)
                Vitest.expect(clearedPages |> Seq.toArray).toEqual ([| None |])

                match messages with
                | [| RefreshRequested |] -> ()
                | _ -> failwith $"Unexpected follow-up message count: {messages.Length}"
            }
        )

        Vitest.test (
            "ConfirmMergeResolutionCompleted ignores late results from the previous ARC",
            fun () -> promise {
                let pageStates = ResizeArray<PageState option>()
                let setPageState pageState = pageStates.Add pageState

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
                        loadMergeConflictPage = fun path -> promise { return Ok(diffPage path) }
                }

                let request = {
                    Path = "conflict-a.txt"
                    ExpectedConflictContent = "<<<<<<< HEAD\nA\n=======\nB\n>>>>>>> branch\n"
                    ResolvedContent = "resolved"
                    AutoCommit = false
                }

                let initialState = {
                    GitState.Empty with
                        CurrentArcPath = Some "C:/arc-a"
                        SelectedChangePath = Some "conflict-a.txt"
                }

                let stateAfterRequest, requestCmd =
                    update deps setPageState (ConfirmMergeResolutionRequested request) initialState

                let switchedState, switchCmd =
                    update deps setPageState (ArcPathChanged(Some "C:/arc-b")) stateAfterRequest

                let! _ = collectMessages switchCmd
                let! completionMessages = collectMessages requestCmd

                let nextState, finishCmd =
                    match completionMessages with
                    | [| ConfirmMergeResolutionCompleted _ |] ->
                        update deps setPageState completionMessages[0] switchedState
                    | _ -> failwith "Expected a merge-resolution completion message."

                let! _ = collectMessages finishCmd

                Vitest
                    .expect(nextState)
                    .toEqual (
                        {
                            GitState.Empty with
                                CurrentArcPath = Some "C:/arc-b"
                                ArcSessionId = 1
                        }
                    )

                Vitest.expect(pageStates |> Seq.toArray).toEqual ([| None |])
            }
        )
)

Vitest.describe (
    "GitWorkflow write request flow",
    fun () ->
        Vitest.test (
            "SaveDownloadLargeFilesRequested updates local state immediately when no ARC is loaded",
            fun () -> promise {
                let state = {
                    GitState.Empty with
                        CurrentArcPath = None
                        LfsAutoTrackThresholdMb = 5
                        DownloadLargeFiles = false
                }

                let nextState, cmd =
                    update defaultDependencies ignore (SaveDownloadLargeFilesRequested true) state

                let! _ = collectMessages cmd

                Vitest.expect(nextState.DownloadLargeFiles).toBe (true)
            }
        )

        Vitest.test (
            "InitRepositoryCompleted clears the missing-repository state and schedules a refresh",
            fun () -> promise {
                let deps = {
                    defaultDependencies with
                        initGitRepository = fun path -> promise { return Ok path }
                }

                let state = {
                    GitState.Empty with
                        CurrentArcPath = Some "C:/arc-a"
                        RepositoryAvailability = GitRepositoryAvailability.MissingRepository
                }

                let requestedState, requestCmd =
                    update deps ignore (InitRepositoryRequested None) state

                let! requestMessages = collectMessages requestCmd

                let nextState, finishCmd =
                    match requestMessages with
                    | [| InitRepositoryCompleted(sessionId, Ok _) |] when sessionId = state.ArcSessionId ->
                        update deps ignore requestMessages[0] requestedState
                    | _ -> failwith "Expected init-repository completion."

                let! finishMessages = collectMessages finishCmd

                Vitest.expect(nextState.RepositoryAvailability).toEqual (GitRepositoryAvailability.Ready)
                Vitest.expect(nextState.BusyOperation).toEqual (None)

                match finishMessages with
                | [| RefreshRequested |] -> ()
                | _ -> failwith "Expected init-repository success to trigger RefreshRequested."
            }
        )

        Vitest.test (
            "InitRepositoryRequested can create a DataHub project and add origin before refreshing",
            fun () -> promise {
                let mutable addedRemote = None

                let deps = {
                    defaultDependencies with
                        initGitRepository = fun path -> promise { return Ok path }
                        createDataHubProject =
                            fun projectName -> promise {
                                return
                                    Ok {
                                        id = 42
                                        name = projectName
                                        path_with_namespace = $"caroott/{projectName}"
                                        name_with_namespace = $"caroott / {projectName}"
                                        description = None
                                        web_url = $"https://git.nfdi4plants.org/caroott/{projectName}"
                                        http_url_to_repo = $"https://git.nfdi4plants.org/caroott/{projectName}.git"
                                        ssh_url_to_repo = None
                                        avatar_url = None
                                        visibility = Some "private"
                                        star_count = 0
                                        created_at = None
                                        last_activity_at = None
                                        tag_list = [||]
                                        ``namespace`` = {
                                            id = 1
                                            name = "caroott"
                                            kind = "user"
                                            full_path = "caroott"
                                        }
                                    }
                            }
                        gitAddRemote =
                            fun request -> promise {
                                addedRemote <- Some request
                                return Ok okOperationResult
                            }
                }

                let state = {
                    GitState.Empty with
                        CurrentArcPath = Some "C:/arc-a"
                        RepositoryAvailability = GitRepositoryAvailability.MissingRepository
                }

                let requestedState, requestCmd =
                    update deps ignore (InitRepositoryRequested(Some "arc-a")) state

                let! requestMessages = collectMessages requestCmd

                let nextState, finishCmd =
                    match requestMessages with
                    | [| InitRepositoryCompleted(sessionId, Ok _) |] when sessionId = state.ArcSessionId ->
                        update deps ignore requestMessages[0] requestedState
                    | _ -> failwith "Expected a successful init-and-remote bootstrap completion."

                let! finishMessages = collectMessages finishCmd

                Vitest
                    .expect(addedRemote)
                    .toEqual (
                        Some {
                            RemoteName = "origin"
                            RemoteUrl = "https://git.nfdi4plants.org/caroott/arc-a.git"
                        }
                    )

                Vitest.expect(nextState.RepositoryAvailability).toEqual (GitRepositoryAvailability.Ready)

                match finishMessages with
                | [| RefreshRequested |] -> ()
                | _ -> failwith "Expected remote bootstrap success to trigger RefreshRequested."
            }
        )

        Vitest.test (
            "InitRepositoryRequested recovers to a ready local repository when DataHub bootstrap fails after git init",
            fun () -> promise {
                let warningMessage = "DataHub project creation failed."

                let deps = {
                    defaultDependencies with
                        initGitRepository = fun path -> promise { return Ok path }
                        createDataHubProject =
                            fun _ -> promise { return Error warningMessage }
                        getGitStatus = fun () -> promise { return Ok cleanStatus }
                        getGitBranches = fun () -> promise { return Ok [| localBranch "main" true true |] }
                        getGitLfsSettings = fun () -> promise { return Ok(lfsSettings 1 false) }
                }

                let state = {
                    GitState.Empty with
                        CurrentArcPath = Some "C:/arc-a"
                        RepositoryAvailability = GitRepositoryAvailability.MissingRepository
                }

                let requestedState, requestCmd =
                    update deps ignore (InitRepositoryRequested(Some "arc-a")) state

                let! requestMessages = collectMessages requestCmd

                let nextState, finishCmd =
                    match requestMessages with
                    | [| (InitRepositoryCompleted _ as message) |] -> update deps ignore message requestedState
                    | _ -> failwith "Expected init-repository completion."

                let! finishMessages = collectMessages finishCmd

                Vitest.expect(nextState.RepositoryAvailability).toEqual (GitRepositoryAvailability.Ready)
                Vitest.expect(nextState.BusyOperation).toEqual (None)

                let afterRefreshRequestedState, refreshRequestCmd =
                    match finishMessages with
                    | [| RefreshRequested |] -> update deps ignore RefreshRequested nextState
                    | _ -> failwith "Expected partial init success to trigger RefreshRequested."

                let! refreshMessages = collectMessages refreshRequestCmd

                let finalState, finalCmd =
                    match refreshMessages with
                    | [| RefreshCompleted(_, Ok refreshResult) |] ->
                        update deps ignore refreshMessages[0] afterRefreshRequestedState
                    | _ -> failwith "Expected queued refresh completion."

                let! finalMessages = collectMessages finalCmd

                Vitest.expect(finalState.RepositoryAvailability).toEqual (GitRepositoryAvailability.Ready)
                Vitest.expect(finalState.WarningNotice).toEqual (Some warningMessage)
                Vitest.expect(finalMessages).toEqual ([||])

                match finishMessages with
                | [| RefreshRequested |] -> ()
                | _ -> failwith "Expected partial init success to trigger RefreshRequested."
            }
        )

        Vitest.test (
            "WriteRequested allows clone requests when no ARC is loaded",
            fun () -> promise {
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
                                Vitest.expect(cloneRequest).toEqual (request)

                                promise {
                                    return
                                        Ok {
                                            okOperationResult with
                                                Path = Some "C:/clone-target"
                                        }
                                }
                }

                let stateAfterRequest, requestCmd =
                    update deps ignore (WriteRequested(Clone(request, reply))) GitState.Empty

                let! requestMessages = collectMessages requestCmd

                let _, finishCmd =
                    match requestMessages with
                    | [| WriteCompleted(_, Clone _, Ok(Completed(CloneSuccess "C:/clone-target"))) |] ->
                        update deps ignore requestMessages[0] stateAfterRequest
                    | _ -> failwith "Expected the clone request to complete successfully."

                let! _ = collectMessages finishCmd

                Vitest.expect(stateAfterRequest.BusyOperation).toEqual (Some GitBusyOperation.CloningRepository)
                Vitest.expect(replyResult).toEqual (Some(Ok "C:/clone-target"))
            }
        )

        Vitest.test (
            "WriteCompleted applies the refreshed snapshot after push success",
            fun () -> promise {
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

                let stateAfterRequest, requestCmd =
                    update deps ignore (WriteRequested Push) initialState

                let! requestMessages = collectMessages requestCmd

                let nextState, finishCmd =
                    match requestMessages with
                    | [| WriteCompleted(_, Push, Ok(Completed(UnitSuccess(_, GitPageChange.NoChange, None, None)))) |] ->
                        update deps ignore requestMessages[0] stateAfterRequest
                    | _ -> failwith "Expected the push request to complete with a refreshed snapshot."

                let! _ = collectMessages finishCmd

                Vitest.expect(nextState.Status.CurrentBranch).toEqual (Some "feature/pushed")
                Vitest.expect(nextState.BranchOptions |> Array.map _.RefName).toEqual ([| "feature/pushed" |])
                Vitest.expect(nextState.LfsAutoTrackThresholdMb).toBe (9)
                Vitest.expect(nextState.DownloadLargeFiles).toBe (true)
            }
        )

        Vitest.test (
            "WriteRequested retries the original operation exactly once after Git LFS installation",
            fun () -> promise {
                let mutable promptCalls = 0
                let mutable installCalls = 0
                let mutable pushCalls = 0

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

                let initialState = {
                    GitState.Empty with
                        CurrentArcPath = Some "C:/arc-a"
                }

                let stateAfterRequest, requestCmd =
                    update deps ignore (WriteRequested Push) initialState

                let! requestMessages = collectMessages requestCmd

                let stateAfterWriteCompleted, promptCmd =
                    match requestMessages with
                    | [| WriteCompleted(_, Push, Ok(RequiresLfsInstall _)) |] ->
                        update deps ignore requestMessages[0] stateAfterRequest
                    | _ -> failwith "Expected a Git LFS install prompt request."

                let! promptMessages = collectMessages promptCmd

                let stateAfterPromptAnswer, installCmd =
                    match promptMessages with
                    | [| WriteInstallPromptAnswered(_, Push, true) |] ->
                        update deps ignore promptMessages[0] stateAfterWriteCompleted
                    | _ -> failwith "Expected an affirmative Git LFS install answer."

                let! installMessages = collectMessages installCmd

                let stateAfterInstall, retryCmd =
                    match installMessages with
                    | [| WriteInstallCompleted(_, Push, Ok _) |] ->
                        update deps ignore installMessages[0] stateAfterPromptAnswer
                    | _ -> failwith "Expected a successful Git LFS install completion."

                let! retryMessages = collectMessages retryCmd

                let _, finishCmd =
                    match retryMessages with
                    | [| WriteCompleted(_, Push, Ok(Completed(UnitSuccess(_, _, _, _)))) |] ->
                        update deps ignore retryMessages[0] stateAfterInstall
                    | _ -> failwith "Expected the retried push to succeed."

                let! _ = collectMessages finishCmd

                Vitest.expect(promptCalls).toBe (1)
                Vitest.expect(installCalls).toBe (1)
                Vitest.expect(pushCalls).toBe (2)
            }
        )

        Vitest.test (
            "WriteCompleted ignores late results from the previous ARC",
            fun () -> promise {
                let deps = {
                    defaultDependencies with
                        gitPush = fun _ -> promise { return Ok okOperationResult }
                        getGitStatus = fun () -> promise { return Ok(statusForBranch "feature/old-arc") }
                        getGitBranches = fun () -> promise { return Ok [| localBranch "feature/old-arc" true true |] }
                        getGitLfsSettings = fun () -> promise { return Ok(lfsSettings 13 true) }
                }

                let initialState = {
                    GitState.Empty with
                        CurrentArcPath = Some "C:/arc-a"
                        Status = {
                            GitState.Empty.Status with
                                CurrentBranch = Some "feature/a"
                        }
                }

                let stateAfterRequest, requestCmd =
                    update deps ignore (WriteRequested Push) initialState

                let switchedState, switchCmd =
                    update deps ignore (ArcPathChanged(Some "C:/arc-b")) stateAfterRequest

                let! _ = collectMessages switchCmd
                let! requestMessages = collectMessages requestCmd

                let nextState, finishCmd =
                    match requestMessages with
                    | [| WriteCompleted(_, Push, Ok(Completed(UnitSuccess(_, _, _, _)))) |] ->
                        update deps ignore requestMessages[0] switchedState
                    | _ -> failwith "Expected the push request to complete successfully."

                let! _ = collectMessages finishCmd

                Vitest
                    .expect(nextState)
                    .toEqual (
                        {
                            GitState.Empty with
                                CurrentArcPath = Some "C:/arc-b"
                                ArcSessionId = 1
                        }
                    )
            }
        )

        Vitest.test (
            "WriteInstallPromptAnswered(false) clears progress and records the cancellation error",
            fun () -> promise {
                let state = {
                    GitState.Empty with
                        CurrentArcPath = Some "C:/arc-a"
                        BusyOperation = Some GitBusyOperation.PushingToRemote
                        CurrentProgress = Some(sidebarProgress "pushing" 50.)
                        InstallRetryState =
                            GitInstallRetryState.PromptingForInstall(
                                "Install Git LFS now?",
                                GitBusyOperation.PushingToRemote
                            )
                }

                let nextState, cmd =
                    update
                        defaultDependencies
                        ignore
                        (WriteInstallPromptAnswered(state.ArcSessionId, Push, false))
                        state

                let! _ = collectMessages cmd

                Vitest.expect(nextState.BusyOperation).toEqual (None)
                Vitest.expect(nextState.CurrentProgress).toEqual (None)
                Vitest.expect(nextState.ErrorNotice).toEqual (Some "Git LFS installation is required to continue.")
                Vitest.expect(currentRunStatus nextState).toEqual (None)
            }
        )

        Vitest.test (
            "WriteInstallCompleted clears progress when the installer reports failure",
            fun () -> promise {
                let state = {
                    GitState.Empty with
                        CurrentArcPath = Some "C:/arc-a"
                        BusyOperation = Some GitBusyOperation.InstallingGitLfs
                        CurrentProgress = Some(sidebarProgress "installing" 75.)
                        InstallRetryState = GitInstallRetryState.InstallingForRetry GitBusyOperation.PushingToRemote
                }

                let nextState, cmd =
                    update
                        defaultDependencies
                        ignore
                        (WriteInstallCompleted(
                            state.ArcSessionId,
                            Push,
                            Ok {
                                okOperationResult with
                                    Success = false
                                    Message = Some "Git LFS installation failed."
                            }
                        ))
                        state

                let! _ = collectMessages cmd

                Vitest.expect(nextState.BusyOperation).toEqual (None)
                Vitest.expect(nextState.CurrentProgress).toEqual (None)
                Vitest.expect(nextState.ErrorNotice).toEqual (Some "Git LFS installation failed.")
                Vitest.expect(currentRunStatus nextState).toEqual (None)
            }
        )

        Vitest.test (
            "WriteCompleted ignores stale non-clone writes without follow-up callback work",
            fun () -> promise {
                let state = {
                    GitState.Empty with
                        ArcSessionId = 2
                        BusyOperation = Some GitBusyOperation.FetchingFromRemote
                        BusyNotice = Some "Checking for remote changes..."
                }

                let nextState, cmd =
                    update
                        defaultDependencies
                        ignore
                        (WriteCompleted(1, WriteRequest.Fetch, Error "stale fetch failed"))
                        state

                let! messages = collectMessages cmd

                Vitest.expect(nextState).toEqual (state)
                Vitest.expect(messages).toEqual ([||])
            }
        )

        Vitest.test (
            "Primary save commits locally, preflights pull, pulls, and pushes when the preflight is safe",
            fun () -> promise {
                let mutable stageCalls = 0
                let mutable commitCalls = 0
                let mutable previewCalls = 0
                let mutable pullCalls = 0
                let mutable pushCalls = 0

                let deps = {
                    defaultDependencies with
                        getGitStatus = fun () -> promise { return Ok cleanStatus }
                        getGitBranches = fun () -> promise { return Ok [| localBranch "main" true true |] }
                        getGitLfsSettings = fun () -> promise { return Ok(lfsSettings 5 true) }
                        gitStagePaths =
                            fun _ -> promise {
                                stageCalls <- stageCalls + 1
                                return Ok okOperationResult
                            }
                        gitCommit =
                            fun _ -> promise {
                                commitCalls <- commitCalls + 1
                                return Ok okOperationResult
                            }
                        previewGitPull =
                            fun _ -> promise {
                                previewCalls <- previewCalls + 1
                                return Ok { Status = GitPullPreflightStatus.SafeToPull; Message = None }
                            }
                        gitPull =
                            fun _ -> promise {
                                pullCalls <- pullCalls + 1
                                return Ok okOperationResult
                            }
                        gitPush =
                            fun _ -> promise {
                                pushCalls <- pushCalls + 1
                                return Ok okOperationResult
                            }
                }

                let initialState = {
                    GitState.Empty with
                        CurrentArcPath = Some "C:/arc"
                        ChangedFiles = [| changedFile "README.md" "M" " " false |]
                }

                let stateAfterRequest, requestCmd =
                    update deps ignore (PrimarySaveAllRequested "Add polish") initialState

                let! requestMessages = collectMessages requestCmd

                let _, finishCmd =
                    match requestMessages with
                    | [| WriteCompleted(_, PrimarySave _, Ok(Completed(UnitSuccess(_, _, _, _)))) |] ->
                        update deps ignore requestMessages[0] stateAfterRequest
                    | _ -> failwith "Expected the primary save flow to finish as one completed write request."

                let! _ = collectMessages finishCmd

                Vitest.expect(stageCalls).toBe (1)
                Vitest.expect(commitCalls).toBe (1)
                Vitest.expect(previewCalls).toBe (1)
                Vitest.expect(pullCalls).toBe (1)
                Vitest.expect(pushCalls).toBe (1)
            }
        )

        Vitest.test (
            "Primary save publishes the branch first when no upstream is configured yet",
            fun () -> promise {
                let mutable previewCalls = 0
                let mutable pullCalls = 0
                let mutable pushCalls = 0

                let deps = {
                    defaultDependencies with
                        getGitStatus = fun () -> promise { return Ok(statusForBranch "feature/new-branch") }
                        getGitBranches = fun () -> promise { return Ok [| localBranch "feature/new-branch" true false |] }
                        getGitLfsSettings = fun () -> promise { return Ok(lfsSettings 5 true) }
                        gitStagePaths = fun _ -> promise { return Ok okOperationResult }
                        gitCommit = fun _ -> promise { return Ok okOperationResult }
                        previewGitPull =
                            fun _ -> promise {
                                previewCalls <- previewCalls + 1
                                return Ok { Status = GitPullPreflightStatus.SafeToPull; Message = None }
                            }
                        gitPull =
                            fun _ -> promise {
                                pullCalls <- pullCalls + 1
                                return Ok okOperationResult
                            }
                        gitPush =
                            fun _ -> promise {
                                pushCalls <- pushCalls + 1
                                return Ok okOperationResult
                            }
                }

                let initialState = {
                    GitState.Empty with
                        CurrentArcPath = Some "C:/arc"
                        Status = {
                            GitState.Empty.Status with
                                CurrentBranch = Some "feature/new-branch"
                                TrackingBranch = None
                                IsClean = false
                        }
                        BranchOptions = [| sidebarLocalBranch "feature/new-branch" true false |]
                        ChangedFiles = [| changedFile "README.md" "M" " " false |]
                }

                let stateAfterRequest, requestCmd =
                    update deps ignore (PrimarySaveAllRequested "Publish branch") initialState

                let! requestMessages = collectMessages requestCmd

                let _, finishCmd =
                    match requestMessages with
                    | [| WriteCompleted(_, PrimarySave _, Ok(Completed(UnitSuccess(_, _, _, _)))) |] ->
                        update deps ignore requestMessages[0] stateAfterRequest
                    | _ -> failwith "Expected the primary save flow to publish the branch and finish."

                let! _ = collectMessages finishCmd

                Vitest.expect(pushCalls).toBe (1)
                Vitest.expect(previewCalls).toBe (0)
                Vitest.expect(pullCalls).toBe (0)
            }
        )

        Vitest.test (
            "Local-only save keeps the old add-and-commit behavior and never requests pull preflight",
            fun () -> promise {
                let mutable previewCalled = false

                let deps = {
                    defaultDependencies with
                        gitStagePaths = fun _ -> promise { return Ok okOperationResult }
                        gitCommit = fun _ -> promise { return Ok okOperationResult }
                        previewGitPull =
                            fun _ -> promise {
                                previewCalled <- true
                                return Ok { Status = GitPullPreflightStatus.SafeToPull; Message = None }
                            }
                        getGitStatus = fun () -> promise { return Ok cleanStatus }
                        getGitBranches = fun () -> promise { return Ok [| localBranch "main" true true |] }
                        getGitLfsSettings = fun () -> promise { return Ok(lfsSettings 5 true) }
                }

                let state = {
                    GitState.Empty with
                        CurrentArcPath = Some "C:/arc"
                        ChangedFiles = [| changedFile "README.md" "M" " " false |]
                }

                let _, cmd = update deps ignore (CommitAllRequested "Local only") state
                let! _ = collectMessages cmd

                Vitest.expect(previewCalled).toBe (false)
            }
        )

        Vitest.test (
            "Primary save keeps a warning and pending confirmation when preflight says online sync still needs merge resolution",
            fun () -> promise {
                let deps = {
                    defaultDependencies with
                        getGitStatus = fun () -> promise { return Ok cleanStatus }
                        getGitBranches = fun () -> promise { return Ok [| localBranch "main" true true |] }
                        getGitLfsSettings = fun () -> promise { return Ok(lfsSettings 5 true) }
                        gitStagePaths = fun _ -> promise { return Ok okOperationResult }
                        gitCommit = fun _ -> promise { return Ok okOperationResult }
                        previewGitPull =
                            fun _ -> promise {
                                return
                                    Ok {
                                        Status = GitPullPreflightStatus.WouldRequireMergeResolution
                                        Message = Some "Pulling would require merge resolution."
                                    }
                            }
                }

                let initialState = {
                    GitState.Empty with
                        CurrentArcPath = Some "C:/arc"
                        ChangedFiles = [| changedFile "README.md" "M" " " false |]
                }

                let stateAfterRequest, requestCmd =
                    update deps ignore (PrimarySaveAllRequested "Add polish") initialState

                let! requestMessages = collectMessages requestCmd

                let nextState, finishCmd =
                    match requestMessages with
                    | [| WriteCompleted(_, PrimarySave _, Ok(CompletedWithPendingRemoteConfirmation(UnitSuccess(_, _, _, Some warningText), _, GitPendingRemoteAction.CompletePrimarySavePush))) |] ->
                        let updatedState, cmd = update deps ignore requestMessages[0] stateAfterRequest
                        Vitest.expect(warningText).toContain ("saved locally")
                        updatedState, cmd
                    | _ -> failwith "Expected the primary save flow to request remote confirmation."

                let! _ = collectMessages finishCmd

                Vitest.expect(nextState.PendingConfirmation.IsSome).toBe (true)
                Vitest.expect(nextState.PendingRemoteAction).toEqual (GitPendingRemoteAction.CompletePrimarySavePush)
                Vitest.expect(nextState.WarningNotice |> Option.defaultValue "").toContain ("saved locally")

                let stateAfterCancel, cancelCmd =
                    update deps ignore CancelPendingRemoteActionRequested nextState

                let! cancelMessages = collectMessages cancelCmd

                Vitest.expect(cancelMessages).toEqual ([||])
                Vitest.expect(stateAfterCancel.PendingConfirmation).toEqual (None)
                Vitest.expect(stateAfterCancel.WarningNotice |> Option.defaultValue "").toContain ("saved locally")
            }
        )

        Vitest.test (
            "UpdateFromOnlineRequested opens a confirmation dialog instead of pulling when preflight predicts merge resolution",
            fun () -> promise {
                let deps = {
                    defaultDependencies with
                        previewGitPull =
                            fun _ -> promise {
                                return
                                    Ok {
                                        Status = GitPullPreflightStatus.WouldRequireMergeResolution
                                        Message = Some "Pulling would require merge resolution."
                                    }
                            }
                }

                let state = {
                    GitState.Empty with
                        CurrentArcPath = Some "C:/arc"
                }

                let nextState, cmd = update deps ignore UpdateFromOnlineRequested state
                let! messages = collectMessages cmd

                Vitest.expect(messages).toEqual ([||])
                Vitest.expect(nextState.PendingConfirmation.IsSome).toBe (true)
                Vitest.expect(nextState.PendingRemoteAction).toEqual (GitPendingRemoteAction.UpdateFromOnline)
            }
        )

        Vitest.test (
            "UpdateFromOnlineRequested does nothing when no ARC is loaded",
            fun () -> promise {
                let nextState, cmd =
                    update defaultDependencies ignore UpdateFromOnlineRequested GitState.Empty

                let! messages = collectMessages cmd

                Vitest.expect(messages).toEqual ([||])
                Vitest.expect(nextState).toEqual (GitState.Empty)
            }
        )

        Vitest.test (
            "UpdateFromOnlineRequested shows the indeterminate confirmation wording when preflight cannot classify safely",
            fun () -> promise {
                let deps = {
                    defaultDependencies with
                        previewGitPull =
                            fun _ -> promise {
                                return
                                    Ok {
                                        Status = GitPullPreflightStatus.Indeterminate
                                        Message = Some "Git pull preflight could not be classified safely."
                                    }
                            }
                }

                let state = {
                    GitState.Empty with
                        CurrentArcPath = Some "C:/arc"
                }

                let nextState, cmd = update deps ignore UpdateFromOnlineRequested state
                let! messages = collectMessages cmd

                Vitest.expect(messages).toEqual ([||])
                Vitest.expect(nextState.PendingConfirmation |> Option.map _.Title).toEqual (Some "Update could not be previewed")
                Vitest
                    .expect(nextState.PendingConfirmation |> Option.map _.Message |> Option.defaultValue "")
                    .toContain ("could not determine safely")
            }
        )

        Vitest.test (
            "UpdatePreflightCompleted ignores results from the previous ARC session",
            fun () -> promise {
                let state = {
                    GitState.Empty with
                        CurrentArcPath = Some "C:/arc-b"
                        ArcSessionId = 4
                }

                let nextState, cmd =
                    update
                        defaultDependencies
                        ignore
                        (UpdatePreflightCompleted(
                            3,
                            Ok {
                                Status = GitPullPreflightStatus.WouldRequireMergeResolution
                                Message = Some "stale"
                            }
                        ))
                        state

                let! messages = collectMessages cmd

                Vitest.expect(messages).toEqual ([||])
                Vitest.expect(nextState.PendingConfirmation).toEqual (None)
            }
        )

        Vitest.test (
            "ConfirmMergeResolutionCompleted dispatches Push when the pending primary-save push can resume",
            fun () -> promise {
                let state = {
                    GitState.Empty with
                        CurrentArcPath = Some "C:/arc"
                        ArcSessionId = 5
                        PendingPostMergePush = true
                        BusyOperation = Some(GitBusyOperation.ConfirmingMergeResolution "conflict.txt")
                        MergeResolutionPendingPath = Some "conflict.txt"
                        SelectedChangePath = Some "conflict.txt"
                }

                let nextState, cmd =
                    update
                        defaultDependencies
                        ignore
                        (ConfirmMergeResolutionCompleted(
                            5,
                            Ok {
                                UpdatedStatus = cleanStatus
                                NextConflictedPath = None
                                PageChange = GitPageChange.Clear
                            }
                        ))
                        state

                let! messages = collectMessages cmd

                Vitest.expect(nextState.PendingPostMergePush).toBe (false)
                Vitest.expect(messages).toEqual ([| WriteRequested Push |])
            }
        )

        Vitest.test (
            "ArcPathChanged schedules a bare RefreshRequested when switching repositories",
            fun () -> promise {
                let nextState, cmd =
                    update defaultDependencies ignore (ArcPathChanged(Some "C:/arc-b")) GitState.Empty

                let! messages = collectMessages cmd

                Vitest.expect(nextState.CurrentArcPath).toEqual (Some "C:/arc-b")

                match messages with
                | [| RefreshRequested |] -> ()
                | _ -> failwith "Expected ArcPathChanged to dispatch RefreshRequested without a reply payload."
            }
        )
)

Vitest.describe (
    "GitWorkflow renderer behavior",
    fun () ->
        Vitest.test (
            "ConfirmMergeResolutionRequested ignores a second request while another conflict is still confirming",
            fun () -> promise {
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
                    update defaultDependencies ignore (ConfirmMergeResolutionRequested request) state

                let! _ = collectMessages cmd

                Vitest.expect(nextState).toEqual (state)
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
            "GitDiffViewer virtualizes large added-file diffs instead of mounting every rendered row",
            fun () ->
                let lines =
                    [| for index in 0 .. 599 -> $"Generated renderer diff line {index + 1}" |]

                let markup =
                    renderToStaticMarkup (
                        Html.div [
                            prop.style [
                                style.width 960
                                style.height 480
                            ]
                            prop.children [
                                Swate.Components.GitDiffViewer.Viewer(
                                    wordDiffText = buildAddedFileDiff "notes/renderer-large.txt" lines,
                                    previousContent = "",
                                    currentContent = joinLines lines,
                                    testIdPrefix = "renderer-large-diff"
                                )
                            ]
                        ]
                    )

                Vitest.expect(markup.Contains("data-testid=\"renderer-large-diff-comparison-scroll-virtual-content\"")).toBe (true)
                Vitest.expect(markup.Contains("data-testid=\"renderer-large-diff-comparison-scroll-row-0\"")).toBe (true)
                Vitest.expect(markup.Contains("data-testid=\"renderer-large-diff-comparison-scroll-row-599\"")).toBe (false)
                Vitest.expect(countOccurrences "data-testid=\"renderer-large-diff-comparison-scroll-row-" markup).toBeLessThan (120)
        )

        Vitest.test (
            "GitDiffViewer renders synthetic new-file diff metadata without blanking the content pane",
            fun () ->
                let markup =
                    renderToStaticMarkup (
                        Swate.Components.GitDiffViewer.Viewer(
                            wordDiffText = "new file mode 100644\n--- /dev/null\n+++ b/notes/draft.txt\n",
                            previousContent = "",
                            currentContent = "Draft line\n",
                            testIdPrefix = "renderer-synthetic-diff"
                        )
                    )

                Vitest.expect(markup.Contains("Draft line")).toBe (true)
                Vitest.expect(markup.Contains("Changed")).toBe (true)
        )

        Vitest.test (
            "GitMergeConflictViewer virtualizes long conflict blocks instead of mounting every rendered row",
            fun () ->
                let currentLines =
                    [| for index in 0 .. 239 -> $"Current renderer conflict line {index + 1}" |]

                let incomingLines =
                    [| for index in 0 .. 239 -> $"Incoming renderer conflict line {index + 1}" |]

                let markup =
                    renderToStaticMarkup (
                        Html.div [
                            prop.style [
                                style.width 960
                                style.height 520
                            ]
                            prop.children [
                                Swate.Components.GitMergeConflictViewer.Viewer(
                                    mergeConflictContent = buildSingleConflictDocument currentLines incomingLines,
                                    testIdPrefix = "renderer-large-merge"
                                )
                            ]
                        ]
                    )

                Vitest.expect(markup.Contains("data-testid=\"renderer-large-merge-conflict-1-scroll-virtual-content\"")).toBe (true)
                Vitest.expect(markup.Contains("data-testid=\"renderer-large-merge-conflict-1-scroll-row-0\"")).toBe (true)
                Vitest.expect(markup.Contains("data-testid=\"renderer-large-merge-conflict-1-scroll-row-239\"")).toBe (false)
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
                                OnRefresh = fun () -> ()
                                OnFetch = fun () -> ()
                                OnPull = fun () -> ()
                                OnPush = fun () -> ()
                                OnUpdateFromOnline = fun () -> ()
                                OnPrimarySaveSelection = fun _ -> ()
                                OnPrimarySaveAll = fun _ -> ()
                                OnCommitSelection = fun _ -> ()
                                OnCommitAll = fun _ -> ()
                                OnConfirmPendingRemoteAction = fun () -> ()
                                OnCancelPendingRemoteAction = fun () -> ()
                                OnSaveDownloadLargeFiles = fun _ -> ()
                                OnSaveLfsAutoTrackThreshold = fun _ -> ()
                                OnCreateBranch = fun _ -> ()
                                OnSwitchBranch = fun _ -> ()
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

        Vitest.test (
            "GitSidebarEmptyState renders the bootstrap call-to-action",
            fun () -> promise {
                let! container, cleanup =
                    renderToBody (
                        Renderer.Components.LeftSidebar.GitSidebarEmptyState.Main(
                            title = "Initialize Git for this ARC",
                            description = "The selected ARC folder is not a Git repository yet.",
                            primaryAction = {
                                Label = "Initialize Repository"
                                IconClassName = "swt:fluent--branch-fork-24-regular"
                                Disabled = false
                                OnClick = ignore
                            },
                            ?infoText = Some "Remote actions remain disabled until you sign in."
                        )
                    )

                Vitest.expect(container.textContent.Contains("Initialize Git for this ARC")).toBe (true)
                Vitest.expect(container.textContent.Contains("Initialize Repository")).toBe (true)
                Vitest.expect(container.textContent.Contains("Remote actions remain disabled")).toBe (true)

                cleanup ()
            }
        )

        Vitest.test (
            "GitSidebar disables remote actions and shows the requested warning when auth is unavailable",
            fun () -> promise {
                let! container, cleanup =
                    renderToBody (
                        Swate.Components.GitSidebar.Main(
                            status = {
                                CurrentBranch = Some "main"
                                TrackingBranch = Some "origin/main"
                                Ahead = 0
                                Behind = 0
                                IsClean = true
                                IsMergeInProgress = false
                            },
                            changedFiles = [||],
                            branchOptions = [| sidebarLocalBranch "main" true true |],
                            callbacks = {
                                OnRefresh = fun () -> ()
                                OnFetch = fun () -> ()
                                OnPull = fun () -> ()
                                OnPush = fun () -> ()
                                OnUpdateFromOnline = fun () -> ()
                                OnPrimarySaveSelection = fun _ -> ()
                                OnPrimarySaveAll = fun _ -> ()
                                OnCommitSelection = fun _ -> ()
                                OnCommitAll = fun _ -> ()
                                OnConfirmPendingRemoteAction = fun () -> ()
                                OnCancelPendingRemoteAction = fun () -> ()
                                OnSaveDownloadLargeFiles = fun _ -> ()
                                OnSaveLfsAutoTrackThreshold = fun _ -> ()
                                OnCreateBranch = fun _ -> ()
                                OnSwitchBranch = fun _ -> ()
                                OnSelectChange = fun _ -> promise { return Ok() }
                            },
                            downloadLargeFiles = true,
                            lfsAutoTrackThresholdMb = 5,
                            remoteActionsEnabled = false,
                            remoteActionsWarning =
                                "Sign in to a DataHub account to use fetch, pull, push, or update."
                        )
                    )

                let updateButton =
                    container.querySelector ("[data-testid='GitSidebarUpdateArcButton']")
                    :?> HTMLButtonElement

                Vitest.expect(updateButton.disabled).toBe (true)
                Vitest.expect(container.textContent.Contains("Sign in to a DataHub account")).toBe (true)

                cleanup ()
            }
        )

        Vitest.test (
            "GitSidebar accepts unit callbacks for observable actions while keeping async file selection",
            fun () -> promise {
                let! container, cleanup =
                    renderToBody (
                        Swate.Components.GitSidebar.Main(
                            status = {
                                CurrentBranch = Some "main"
                                TrackingBranch = Some "origin/main"
                                Ahead = 0
                                Behind = 0
                                IsClean = true
                                IsMergeInProgress = false
                            },
                            changedFiles = [||],
                            branchOptions = [| sidebarLocalBranch "main" true true |],
                            callbacks = {
                                OnRefresh = fun () -> ()
                                OnFetch = fun () -> ()
                                OnPull = fun () -> ()
                                OnPush = fun () -> ()
                                OnUpdateFromOnline = fun () -> ()
                                OnPrimarySaveSelection = fun _ -> ()
                                OnPrimarySaveAll = fun _ -> ()
                                OnCommitSelection = fun _ -> ()
                                OnCommitAll = fun _ -> ()
                                OnConfirmPendingRemoteAction = fun () -> ()
                                OnCancelPendingRemoteAction = fun () -> ()
                                OnSaveDownloadLargeFiles = fun _ -> ()
                                OnSaveLfsAutoTrackThreshold = fun _ -> ()
                                OnCreateBranch = fun _ -> ()
                                OnSwitchBranch = fun _ -> ()
                                OnSelectChange = fun _ -> promise { return Ok() }
                            },
                            downloadLargeFiles = true,
                            lfsAutoTrackThresholdMb = 5
                        )
                    )

                Vitest.expect(container.querySelector ("[data-testid='GitSidebar']")).not.toBeNull ()
                cleanup ()
            }
        )

        Vitest.test (
            "GitSidebar renders conflicted change rows alongside other changed files",
            fun () -> promise {
                let! container, cleanup =
                    renderToBody (
                        Swate.Components.GitSidebar.Main(
                            status = {
                                CurrentBranch = Some "main"
                                TrackingBranch = Some "origin/main"
                                Ahead = 0
                                Behind = 0
                                IsClean = false
                                IsMergeInProgress = true
                            },
                            changedFiles = [|
                                changedFile "README.md" "M" " " false
                                changedFile "assays/isa.assay.xlsx" "?" "?" false
                                changedFile "notes/protocol.md" "R" "M" false
                                changedFile "studies/s-study-01/protocol.md" "U" "U" true
                            |],
                            branchOptions = [| sidebarLocalBranch "main" true true |],
                            callbacks = {
                                OnRefresh = fun () -> ()
                                OnFetch = fun () -> ()
                                OnPull = fun () -> ()
                                OnPush = fun () -> ()
                                OnUpdateFromOnline = fun () -> ()
                                OnPrimarySaveSelection = fun _ -> ()
                                OnPrimarySaveAll = fun _ -> ()
                                OnCommitSelection = fun _ -> ()
                                OnCommitAll = fun _ -> ()
                                OnConfirmPendingRemoteAction = fun () -> ()
                                OnCancelPendingRemoteAction = fun () -> ()
                                OnSaveDownloadLargeFiles = fun _ -> ()
                                OnSaveLfsAutoTrackThreshold = fun _ -> ()
                                OnCreateBranch = fun _ -> ()
                                OnSwitchBranch = fun _ -> ()
                                OnSelectChange = fun _ -> promise { return Ok() }
                            },
                            downloadLargeFiles = true,
                            lfsAutoTrackThresholdMb = 5
                        )
                    )

                Vitest.expect(container.textContent.Contains("studies/s-study-01/protocol.md")).toBe (true)
                Vitest.expect(container.querySelector("[data-testid='GitSidebarChangeStatusButton-3']")).not.toBeNull ()

                cleanup ()
            }
        )

        Vitest.test (
            "GitSidebar renders small changed-file sets through the virtualized list path",
            fun () -> promise {
                let! container, cleanup =
                    renderToBody (
                        Html.div [
                            prop.style [
                                style.width 340
                                style.height 760
                            ]
                            prop.children [
                                Swate.Components.GitSidebar.Main(
                                    status = {
                                        CurrentBranch = Some "main"
                                        TrackingBranch = Some "origin/main"
                                        Ahead = 0
                                        Behind = 0
                                        IsClean = false
                                        IsMergeInProgress = false
                                    },
                                    changedFiles = manyChangedFiles 3,
                                    branchOptions = [| sidebarLocalBranch "main" true true |],
                                    callbacks = {
                                        OnRefresh = fun () -> ()
                                        OnFetch = fun () -> ()
                                        OnPull = fun () -> ()
                                        OnPush = fun () -> ()
                                        OnUpdateFromOnline = fun () -> ()
                                        OnPrimarySaveSelection = fun _ -> ()
                                        OnPrimarySaveAll = fun _ -> ()
                                        OnCommitSelection = fun _ -> ()
                                        OnCommitAll = fun _ -> ()
                                        OnConfirmPendingRemoteAction = fun () -> ()
                                        OnCancelPendingRemoteAction = fun () -> ()
                                        OnSaveDownloadLargeFiles = fun _ -> ()
                                        OnSaveLfsAutoTrackThreshold = fun _ -> ()
                                        OnCreateBranch = fun _ -> ()
                                        OnSwitchBranch = fun _ -> ()
                                        OnSelectChange = fun _ -> promise { return Ok() }
                                    },
                                    downloadLargeFiles = true,
                                    lfsAutoTrackThresholdMb = 5
                                )
                            ]
                        ]
                    )

                Vitest.expect(container.querySelector("[data-testid='GitSidebarChangedFilesScrollContainer']")).not.toBeNull ()
                Vitest.expect(container.querySelector("[data-testid='GitSidebarChangedFilesVirtualContent']")).not.toBeNull ()
                Vitest.expect(container.querySelectorAll("[data-testid^='GitSidebarChangeRow-']").length).toBe(3)
                Vitest
                    .expect(
                        container
                            .querySelector("[data-testid='GitSidebarChangedFilesScrollContainer']")
                            .getAttribute("role")
                    )
                    .toBe("region")
                Vitest
                    .expect(
                        container
                            .querySelector("[data-testid='GitSidebarChangedFilesScrollContainer']")
                            .getAttribute("aria-label")
                    )
                    .toBe("Changed files")
                Vitest
                    .expect(
                        container
                            .querySelector("[data-testid='GitSidebarChangedFilesVirtualContent']")
                            .getAttribute("role")
                    )
                    .toBe("list")
                Vitest.expect(container.querySelectorAll("[role='listitem']").length).toBe(3)
                Vitest
                    .expect(container.querySelector("[role='listitem'] [data-testid='GitSidebarChangeRow-0']"))
                    .not
                    .toBeNull()

                cleanup ()
            }
        )

        Vitest.test (
            "GitSidebar virtualizes long changed-file lists instead of mounting every item at once",
            fun () -> promise {
                let! container, cleanup =
                    renderToBody (
                        Html.div [
                            prop.style [
                                style.width 340
                                style.height 760
                            ]
                            prop.children [
                                Swate.Components.GitSidebar.Main(
                                    status = {
                                        CurrentBranch = Some "main"
                                        TrackingBranch = Some "origin/main"
                                        Ahead = 0
                                        Behind = 0
                                        IsClean = false
                                        IsMergeInProgress = false
                                    },
                                    changedFiles = manyChangedFiles 400,
                                    branchOptions = [| sidebarLocalBranch "main" true true |],
                                    callbacks = {
                                        OnRefresh = fun () -> ()
                                        OnFetch = fun () -> ()
                                        OnPull = fun () -> ()
                                        OnPush = fun () -> ()
                                        OnUpdateFromOnline = fun () -> ()
                                        OnPrimarySaveSelection = fun _ -> ()
                                        OnPrimarySaveAll = fun _ -> ()
                                        OnCommitSelection = fun _ -> ()
                                        OnCommitAll = fun _ -> ()
                                        OnConfirmPendingRemoteAction = fun () -> ()
                                        OnCancelPendingRemoteAction = fun () -> ()
                                        OnSaveDownloadLargeFiles = fun _ -> ()
                                        OnSaveLfsAutoTrackThreshold = fun _ -> ()
                                        OnCreateBranch = fun _ -> ()
                                        OnSwitchBranch = fun _ -> ()
                                        OnSelectChange = fun _ -> promise { return Ok() }
                                    },
                                    downloadLargeFiles = true,
                                    lfsAutoTrackThresholdMb = 5
                                )
                            ]
                        ]
                    )

                Vitest.expect(container.textContent.Contains("400 files")).toBe (true)
                Vitest.expect(container.querySelector("[data-testid='GitSidebarChangedFilesVirtualContent']")).not.toBeNull ()
                Vitest.expect(container.querySelector("[data-testid='GitSidebarChangeRow-0']")).not.toBeNull ()
                Vitest.expect(container.querySelector("[data-testid='GitSidebarChangeRow-399']")).toBeNull ()

                cleanup ()
            }
        )

        Vitest.test (
            "GitSidebar virtualizes changed files when nested inside an outer scrollable wrapper",
            fun () -> promise {
                // Reproduce the production layout: fixed-height sidebar panel -> outer scroll wrapper
                // -> Git host wrapper -> GitSidebar. The virtualizer only works when the host wrapper
                // gives GitSidebar a bounded height, so this test keeps the working height contract visible.
                let boundedWrapperClasses = [
                    "swt:box-border"
                    "swt:flex"
                    "swt:h-full"
                    "swt:min-h-0"
                    "swt:min-w-0"
                    "swt:max-w-full"
                    "swt:flex-col"
                    "swt:overflow-hidden"
                    "swt:p-4"
                ]

                let! container, cleanup =
                    renderToBody (
                        Html.div [
                            prop.style [
                                style.width 340
                                style.height 760
                                style.overflow.hidden
                            ]
                            prop.children [
                                Html.div [
                                    prop.style [
                                        style.width (length.percent 100)
                                        style.height (length.percent 100)
                                        style.overflowY.auto
                                    ]
                                    prop.children [
                                        Html.div [
                                            prop.testId "GitSidebarBoundedHost"
                                            prop.className boundedWrapperClasses
                                            prop.style [ style.height (length.percent 100) ]
                                            prop.children [
                                                Swate.Components.GitSidebar.Main(
                                                    status = {
                                                        CurrentBranch = Some "main"
                                                        TrackingBranch = Some "origin/main"
                                                        Ahead = 0
                                                        Behind = 0
                                                        IsClean = false
                                                        IsMergeInProgress = false
                                                    },
                                                    changedFiles = manyChangedFiles 200,
                                                    branchOptions = [| sidebarLocalBranch "main" true true |],
                                                    callbacks = {
                                                        OnRefresh = fun () -> ()
                                                        OnFetch = fun () -> ()
                                                        OnPull = fun () -> ()
                                                        OnPush = fun () -> ()
                                                        OnUpdateFromOnline = fun () -> ()
                                                        OnPrimarySaveSelection = fun _ -> ()
                                                        OnPrimarySaveAll = fun _ -> ()
                                                        OnCommitSelection = fun _ -> ()
                                                        OnCommitAll = fun _ -> ()
                                                        OnConfirmPendingRemoteAction = fun () -> ()
                                                        OnCancelPendingRemoteAction = fun () -> ()
                                                        OnSaveDownloadLargeFiles = fun _ -> ()
                                                        OnSaveLfsAutoTrackThreshold = fun _ -> ()
                                                        OnCreateBranch = fun _ -> ()
                                                        OnSwitchBranch = fun _ -> ()
                                                        OnSelectChange = fun _ -> promise { return Ok() }
                                                    },
                                                    downloadLargeFiles = true,
                                                    lfsAutoTrackThresholdMb = 5
                                                )
                                            ]
                                        ]
                                    ]
                                ]
                            ]
                        ]
                    )

                // With 200 items, only a subset should be rendered (visible + overscan).
                // If virtualization is broken, all 200 items would be in the DOM.
                let boundedHost = container.querySelector("[data-testid='GitSidebarBoundedHost']") :?> HTMLElement

                for expectedClass in boundedWrapperClasses do
                    Vitest.expect(boundedHost.classList.contains expectedClass).toBe (true)

                Vitest.expect(boundedHost.classList.contains "swt:w-full").toBe (false)

                let renderedRows = container.querySelectorAll("[data-testid^='GitSidebarChangeRow-']")
                Vitest.expect(renderedRows.length).toBeLessThan(200)
                Vitest.expect(container.querySelector("[data-testid='GitSidebarChangeRow-0']")).not.toBeNull ()
                Vitest.expect(container.querySelector("[data-testid='GitSidebarChangeRow-199']")).toBeNull ()

                cleanup ()
            }
        )

        Vitest.test (
            "GitSidebar hides the inline git return text and exposes it through a popover trigger",
            fun () -> promise {
                let! container, cleanup =
                    renderToBody (
                        Swate.Components.GitSidebar.Main(
                            status = {
                                CurrentBranch = Some "main"
                                TrackingBranch = Some "origin/main"
                                Ahead = 0
                                Behind = 0
                                IsClean = false
                                IsMergeInProgress = false
                            },
                            changedFiles = [| changedFile "obsolete.md" "D" " " false |],
                            branchOptions = [| sidebarLocalBranch "main" true true |],
                            callbacks = {
                                OnRefresh = fun () -> ()
                                OnFetch = fun () -> ()
                                OnPull = fun () -> ()
                                OnPush = fun () -> ()
                                OnUpdateFromOnline = fun () -> ()
                                OnPrimarySaveSelection = fun _ -> ()
                                OnPrimarySaveAll = fun _ -> ()
                                OnCommitSelection = fun _ -> ()
                                OnCommitAll = fun _ -> ()
                                OnConfirmPendingRemoteAction = fun () -> ()
                                OnCancelPendingRemoteAction = fun () -> ()
                                OnSaveDownloadLargeFiles = fun _ -> ()
                                OnSaveLfsAutoTrackThreshold = fun _ -> ()
                                OnCreateBranch = fun _ -> ()
                                OnSwitchBranch = fun _ -> ()
                                OnSelectChange = fun _ -> promise { return Ok() }
                            },
                            downloadLargeFiles = true,
                            lfsAutoTrackThresholdMb = 5
                        )
                    )

                Vitest.expect(container.textContent.Contains("git: D.")).toBe (false)
                Vitest.expect(container.textContent.Contains("Deleted")).toBe (false)
                Vitest.expect(container.querySelector("[data-testid='GitSidebarChangeStatusButton-0']")).not.toBeNull ()

                cleanup ()
            }
        )

        Vitest.test (
            "GitSidebar keeps the change-status icon in a fixed right-edge slot",
            fun () -> promise {
                let! container, cleanup =
                    renderToBody (
                        Html.div [
                            prop.style [ style.width 340; style.height 760 ]
                            prop.children [
                                Swate.Components.GitSidebar.Main(
                                    status = {
                                        CurrentBranch = Some "main"
                                        TrackingBranch = Some "origin/main"
                                        Ahead = 0
                                        Behind = 0
                                        IsClean = false
                                        IsMergeInProgress = false
                                    },
                                    changedFiles = [|
                                        changedFile
                                            "src/very/long/path/that/wraps/in/the/sidebar/and/needs/a/fixed/status/icon.txt"
                                            "M"
                                            " "
                                            false
                                    |],
                                    branchOptions = [| sidebarLocalBranch "main" true true |],
                                    callbacks = {
                                        OnRefresh = fun () -> ()
                                        OnFetch = fun () -> ()
                                        OnPull = fun () -> ()
                                        OnPush = fun () -> ()
                                        OnUpdateFromOnline = fun () -> ()
                                        OnPrimarySaveSelection = fun _ -> ()
                                        OnPrimarySaveAll = fun _ -> ()
                                        OnCommitSelection = fun _ -> ()
                                        OnCommitAll = fun _ -> ()
                                        OnConfirmPendingRemoteAction = fun () -> ()
                                        OnCancelPendingRemoteAction = fun () -> ()
                                        OnSaveDownloadLargeFiles = fun _ -> ()
                                        OnSaveLfsAutoTrackThreshold = fun _ -> ()
                                        OnCreateBranch = fun _ -> ()
                                        OnSwitchBranch = fun _ -> ()
                                        OnSelectChange = fun _ -> promise { return Ok() }
                                    },
                                    downloadLargeFiles = true,
                                    lfsAutoTrackThresholdMb = 5
                                )
                            ]
                        ]
                    )

                let statusSlot = container.querySelector("[data-testid='GitSidebarChangeStatusSlot-0']") :?> HTMLElement
                Vitest.expect(statusSlot.className.Contains("swt:ml-auto")).toBe (true)
                Vitest.expect(statusSlot.className.Contains("swt:shrink-0")).toBe (true)

                cleanup ()
            }
        )

        Vitest.test (
            "GitSidebar marks rows with Windows Explorer click semantics and shows one primary save button",
            fun () -> promise {
                let! container, cleanup =
                    renderToBody (
                        Swate.Components.GitSidebar.Main(
                            status = {
                                CurrentBranch = Some "main"
                                TrackingBranch = Some "origin/main"
                                Ahead = 0
                                Behind = 0
                                IsClean = false
                                IsMergeInProgress = false
                            },
                            changedFiles = manyChangedFiles 3,
                            branchOptions = [| sidebarLocalBranch "main" true true |],
                            callbacks = {
                                OnRefresh = fun () -> ()
                                OnFetch = fun () -> ()
                                OnPull = fun () -> ()
                                OnPush = fun () -> ()
                                OnUpdateFromOnline = fun () -> ()
                                OnPrimarySaveSelection = fun _ -> ()
                                OnPrimarySaveAll = fun _ -> ()
                                OnCommitSelection = fun _ -> ()
                                OnCommitAll = fun _ -> ()
                                OnConfirmPendingRemoteAction = fun () -> ()
                                OnCancelPendingRemoteAction = fun () -> ()
                                OnSaveDownloadLargeFiles = fun _ -> ()
                                OnSaveLfsAutoTrackThreshold = fun _ -> ()
                                OnCreateBranch = fun _ -> ()
                                OnSwitchBranch = fun _ -> ()
                                OnSelectChange = fun _ -> promise { return Ok() }
                            },
                            downloadLargeFiles = true,
                            lfsAutoTrackThresholdMb = 5
                        )
                    )

                let firstRow = container.querySelector("[data-testid='GitSidebarChangeRow-0']") :?> HTMLButtonElement
                let thirdRow = container.querySelector("[data-testid='GitSidebarChangeRow-2']") :?> HTMLButtonElement

                firstRow.click ()

                let shiftClick =
                    createMouseEvent "click" (createObj [ "bubbles" ==> true; "shiftKey" ==> true ])

                thirdRow.dispatchEvent shiftClick |> ignore

                Vitest.expect(container.querySelectorAll("[data-testid='GitSidebarPrimarySaveButton']").length).toBe (1)
                Vitest.expect(container.textContent.Contains("Save Selected Changes")).toBe (true)
                Vitest.expect(container.querySelectorAll("[data-testid^='GitSidebarCommitSelectionCheckbox-']").length).toBe (0)

                cleanup ()
            }
        )

        Vitest.test (
            "GitSidebar plain click clears previous marks and ctrl-click toggles a row on and back off",
            fun () -> promise {
                let mutable capturedSelection: GitSidebarCommitSelectionRequest option = None

                let! container, cleanup =
                    renderToBody (
                        Swate.Components.GitSidebar.Main(
                            status = {
                                CurrentBranch = Some "main"
                                TrackingBranch = Some "origin/main"
                                Ahead = 0
                                Behind = 0
                                IsClean = false
                                IsMergeInProgress = false
                            },
                            changedFiles = manyChangedFiles 3,
                            branchOptions = [| sidebarLocalBranch "main" true true |],
                            callbacks = {
                                OnRefresh = fun () -> ()
                                OnFetch = fun () -> ()
                                OnPull = fun () -> ()
                                OnPush = fun () -> ()
                                OnUpdateFromOnline = fun () -> ()
                                OnPrimarySaveSelection = fun _ -> ()
                                OnPrimarySaveAll = fun _ -> ()
                                OnCommitSelection = fun request -> capturedSelection <- Some request
                                OnCommitAll = fun _ -> ()
                                OnConfirmPendingRemoteAction = fun () -> ()
                                OnCancelPendingRemoteAction = fun () -> ()
                                OnSaveDownloadLargeFiles = fun _ -> ()
                                OnSaveLfsAutoTrackThreshold = fun _ -> ()
                                OnCreateBranch = fun _ -> ()
                                OnSwitchBranch = fun _ -> ()
                                OnSelectChange = fun _ -> promise { return Ok() }
                            },
                            downloadLargeFiles = true,
                            lfsAutoTrackThresholdMb = 5
                        )
                    )

                let row0 = container.querySelector("[data-testid='GitSidebarChangeRow-0']") :?> HTMLButtonElement
                let row1 = container.querySelector("[data-testid='GitSidebarChangeRow-1']") :?> HTMLButtonElement
                let row2 = container.querySelector("[data-testid='GitSidebarChangeRow-2']") :?> HTMLButtonElement
                let messageInput = container.querySelector("[data-testid='GitSidebarCommitMessageInput']") :?> HTMLTextAreaElement
                let saveButton = container.querySelector("[data-testid='GitSidebarPrimarySaveButton']") :?> HTMLButtonElement

                row0.click ()

                let ctrlClick =
                    createMouseEvent "click" (createObj [ "bubbles" ==> true; "ctrlKey" ==> true ])

                row2.dispatchEvent ctrlClick |> ignore
                row2.dispatchEvent ctrlClick |> ignore
                row1.click ()

                messageInput.value <- "save one file"
                messageInput.dispatchEvent (createEvent "input") |> ignore
                saveButton.click ()

                Vitest.expect(capturedSelection |> Option.map _.Paths).toEqual (Some [| "src/file-001.txt" |])

                cleanup ()
            }
        )

        Vitest.test (
            "GitSidebar ctrl-shift-click adds the anchor range to the existing marked set",
            fun () -> promise {
                let mutable capturedSelection: GitSidebarCommitSelectionRequest option = None

                let! container, cleanup =
                    renderToBody (
                        Swate.Components.GitSidebar.Main(
                            status = {
                                CurrentBranch = Some "main"
                                TrackingBranch = Some "origin/main"
                                Ahead = 0
                                Behind = 0
                                IsClean = false
                                IsMergeInProgress = false
                            },
                            changedFiles = manyChangedFiles 3,
                            branchOptions = [| sidebarLocalBranch "main" true true |],
                            callbacks = {
                                OnRefresh = fun () -> ()
                                OnFetch = fun () -> ()
                                OnPull = fun () -> ()
                                OnPush = fun () -> ()
                                OnUpdateFromOnline = fun () -> ()
                                OnPrimarySaveSelection = fun _ -> ()
                                OnPrimarySaveAll = fun _ -> ()
                                OnCommitSelection = fun request -> capturedSelection <- Some request
                                OnCommitAll = fun _ -> ()
                                OnConfirmPendingRemoteAction = fun () -> ()
                                OnCancelPendingRemoteAction = fun () -> ()
                                OnSaveDownloadLargeFiles = fun _ -> ()
                                OnSaveLfsAutoTrackThreshold = fun _ -> ()
                                OnCreateBranch = fun _ -> ()
                                OnSwitchBranch = fun _ -> ()
                                OnSelectChange = fun _ -> promise { return Ok() }
                            },
                            downloadLargeFiles = true,
                            lfsAutoTrackThresholdMb = 5
                        )
                    )

                let row0 = container.querySelector("[data-testid='GitSidebarChangeRow-0']") :?> HTMLButtonElement
                let row2 = container.querySelector("[data-testid='GitSidebarChangeRow-2']") :?> HTMLButtonElement
                let messageInput = container.querySelector("[data-testid='GitSidebarCommitMessageInput']") :?> HTMLTextAreaElement
                let saveButton = container.querySelector("[data-testid='GitSidebarPrimarySaveButton']") :?> HTMLButtonElement

                row0.click ()

                let ctrlShiftClick =
                    createMouseEvent
                        "click"
                        (createObj [ "bubbles" ==> true; "ctrlKey" ==> true; "shiftKey" ==> true ])

                row2.dispatchEvent ctrlShiftClick |> ignore

                messageInput.value <- "save range"
                messageInput.dispatchEvent (createEvent "input") |> ignore
                saveButton.click ()

                Vitest
                    .expect(capturedSelection |> Option.map _.Paths)
                    .toEqual (Some [| "src/file-000.txt"; "src/file-001.txt"; "src/file-002.txt" |])

                cleanup ()
            }
        )

        Vitest.test (
            "GitSidebar keeps marked rows selected even when diff opening fails",
            fun () -> promise {
                let! container, cleanup =
                    renderToBody (
                        Swate.Components.GitSidebar.Main(
                            status = {
                                CurrentBranch = Some "main"
                                TrackingBranch = Some "origin/main"
                                Ahead = 0
                                Behind = 0
                                IsClean = false
                                IsMergeInProgress = false
                            },
                            changedFiles = [|
                                changedFile "README.md" "M" " " false
                                changedFile "docs/guide.md" "M" " " false
                            |],
                            branchOptions = [| sidebarLocalBranch "main" true true |],
                            callbacks = {
                                OnRefresh = fun () -> ()
                                OnFetch = fun () -> ()
                                OnPull = fun () -> ()
                                OnPush = fun () -> ()
                                OnUpdateFromOnline = fun () -> ()
                                OnPrimarySaveSelection = fun _ -> ()
                                OnPrimarySaveAll = fun _ -> ()
                                OnCommitSelection = fun _ -> ()
                                OnCommitAll = fun _ -> ()
                                OnConfirmPendingRemoteAction = fun () -> ()
                                OnCancelPendingRemoteAction = fun () -> ()
                                OnSaveDownloadLargeFiles = fun _ -> ()
                                OnSaveLfsAutoTrackThreshold = fun _ -> ()
                                OnCreateBranch = fun _ -> ()
                                OnSwitchBranch = fun _ -> ()
                                OnSelectChange = fun _ -> promise { return Error "Diff failed to load." }
                            },
                            downloadLargeFiles = true,
                            lfsAutoTrackThresholdMb = 5
                        )
                    )

                let firstRow = container.querySelector("[data-testid='GitSidebarChangeRow-0']") :?> HTMLButtonElement
                firstRow.click ()
                do! Promise.sleep 0

                Vitest.expect(container.textContent.Contains("Save Selected Changes")).toBe (true)
                Vitest.expect(container.querySelector("[data-testid='GitSidebarErrorNotice']")).not.toBeNull ()

                cleanup ()
            }
        )

        Vitest.test (
            "GitSidebar renders the save section without the old outer card wrapper",
            fun () -> promise {
                let! container, cleanup =
                    renderToBody (
                        Swate.Components.GitSidebar.Main(
                            status = {
                                CurrentBranch = Some "main"
                                TrackingBranch = Some "origin/main"
                                Ahead = 0
                                Behind = 0
                                IsClean = false
                                IsMergeInProgress = false
                            },
                            changedFiles = [| changedFile "README.md" "M" " " false |],
                            branchOptions = [| sidebarLocalBranch "main" true true |],
                            callbacks = noopCallbacks,
                            downloadLargeFiles = true,
                            lfsAutoTrackThresholdMb = 5
                        )
                    )

                let saveSection = container.querySelector("[data-testid='GitSidebarCommitSection']") :?> HTMLElement
                let legacyCard = firstElementChild saveSection
                Vitest.expect(legacyCard.className.Contains("swt:rounded-box")).toBe (false)
                Vitest.expect(legacyCard.className.Contains("swt:border")).toBe (false)

                cleanup ()
            }
        )
)
