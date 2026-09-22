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
open Swate.Components.Page.GitSidebarTypes
open Swate.Electron.Shared.VersionControlTypes
open Vitest

[<Import("renderToStaticMarkup", "react-dom/server")>]
let private renderToStaticMarkup (element: ReactElement) : string = jsNative

let private localBranch name current _ = {
    Name = name
    ProviderRef = $"git-local:{name}"
    Kind = RefKindDto.Local
    IsCurrent = current
}

let private remoteBranch name = {
    Name = $"origin/{name}"
    ProviderRef = $"git-remote:origin/{name}"
    Kind = RefKindDto.Remote
    IsCurrent = false
}

let private cleanStatus: WorkspaceStatusDto = {
    CurrentRef = Some(localBranch "main" true true)
    WorkspaceVersion = "v1"
    Changes = [||]
    ActiveConflictSession = None
    Synchronization =
        Some {
            BaseRevision = None
            WorkspaceRevision = Some "rev"
            TargetRevision = Some "target"
            TargetRef = Some(remoteBranch "main")
            LocalRevisionCount = Some 0
            TargetRevisionCount = Some 0
            RemoteChangedPaths = None
            Relationship = RevisionRelationshipDto.UpToDate
        }
}

let private statusForBranch name = {
    cleanStatus with
        CurrentRef = Some(localBranch name true true)
}

let private conflictedStatus paths = {
    cleanStatus with
        Changes =
            paths
            |> Array.map (fun path -> {
                Path = path
                OldPath = None
                Kind = FileChangeKindDto.Conflicted
            })
        ActiveConflictSession =
            Some {
                Handle = {
                    SessionId = "conflict-1"
                    Version = "1"
                }
                Items =
                    paths
                    |> Array.map (fun path -> {
                        Path = path
                        Candidates = [||]
                        CombinedPreview =
                            Some(ContentViewDto.Text "<<<<<<< HEAD\nA\n=======\nB\n>>>>>>> origin/main\n")
                        SupportsResolvedContent = true
                    })
            }
}

let private operationWithPublication publication value : OperationOutcomeDto<'T> = {
    Value = value
    Effect = OperationEffectDto.Performed
    Warnings = [||]
    AffectedPaths = [||]
    ResultingRevision = None
    ResultingWorkspaceVersion = None
    Publication = publication
}

let private operation value : OperationOutcomeDto<'T> =
    operationWithPublication PublicationStateDto.NotApplicable value

let private succeededWithWarnings warnings value : OperationResultDto<'T> =
    OperationResultDto.Succeeded {
        operation value with
            Warnings = warnings
    }

let private succeeded value : OperationResultDto<'T> =
    OperationResultDto.Succeeded(operation value)

let private succeededWithPublication publication value : OperationResultDto<'T> =
    OperationResultDto.Succeeded(operationWithPublication publication value)

let private makeFailure category code message recovery paths : OperationFailureDto = {
    Category = category
    Code = code
    Message = message
    StateChanged = false
    Retryable = false
    AffectedPaths = paths
    RecoveryAction = recovery
    Details = [||]
    RevisionEvidence = [||]
}

let private failed category code message : OperationResultDto<'T> =
    OperationResultDto.Failed(makeFailure category code message None [||])

let private lfsSettings n materialize : StoragePolicySettingsDto = {
    AutoPolicyThresholdMb = Some n
    MaterializeLargeObjects = materialize
}

let private sessionInfo: WorkspaceSessionInfoDto = {
    SessionId = "session-1"
    ProviderId = "git"
    WorkspaceRoot = "C:/arc"
    Location = None
    Services = {
        Synchronization = true
        TextDiff = true
        ConflictResolution = true
        ObjectMaterialization = true
        StoragePolicy = true
        Maintenance = true
        RepositoryBrowser = true
    }
}

let private refs = [|
    localBranch "main" true true
    localBranch "feature" false false
    remoteBranch "main"
|]

let private remoteProject: ExploreProjectDto = {
    id = 123
    name = "my-arc"
    path_with_namespace = "carol/my-arc"
    name_with_namespace = "carol / my-arc"
    description = None
    web_url = "https://gitlab.example/carol/my-arc"
    http_url_to_repo = "https://gitlab.example/carol/my-arc.git"
    ssh_url_to_repo = None
    avatar_url = None
    visibility = Some "private"
    star_count = 0
    created_at = None
    last_activity_at = None
    tag_list = [||]
    ``namespace`` = {
        id = 5
        name = "carol"
        kind = "user"
        full_path = "carol"
    }
}

let private changedFile path i w c : GitSidebarChange = {
    Path = path
    OriginalPath = None
    IndexStatus = i
    WorkingTreeStatus = w
    IsConflicted = c
}

let private sidebarLocalBranch name current tracking : GitSidebarBranchOption = {
    RefName = name
    DisplayLabel = name
    Kind = GitSidebarBranchKind.Local
    IsCurrent = current
    IsTracking = tracking
}

let private sidebarRemoteBranch name tracking : GitSidebarBranchOption = {
    RefName = name
    DisplayLabel = name
    Kind = GitSidebarBranchKind.Remote
    IsCurrent = false
    IsTracking = tracking
}

let private sidebarProgress stage percent = {
    Method = Some "git"
    Stage = Some stage
    ProgressPercent = Some percent
    Output = None
}

let private sidebarOutput output = {
    Method = None
    Stage = None
    ProgressPercent = None
    Output = Some output
}

[<Emit("new Event($0, { bubbles: true })")>]
let private createEvent (eventType: string) : Browser.Types.Event = jsNative

[<Emit("new MouseEvent($0, $1)")>]
let private createMouseEvent (eventType: string) (eventInit: obj) : Browser.Types.MouseEvent = jsNative

[<Emit("""
const setter = Object.getOwnPropertyDescriptor(HTMLTextAreaElement.prototype, "value").set;
setter.call($0, $1);
$0.dispatchEvent(new Event("input", { bubbles: true }));
""")>]
let private setTextAreaValue (element: HTMLTextAreaElement) (value: string) : unit = jsNative

[<Emit("""
const setter = Object.getOwnPropertyDescriptor(HTMLInputElement.prototype, "value").set;
setter.call($0, $1);
$0.dispatchEvent(new Event("input", { bubbles: true }));
""")>]
let private setInputValue (element: HTMLInputElement) (value: string) : unit = jsNative

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

[<Emit("""
globalThis.__swateOriginalFetch = globalThis.fetch;
globalThis.__swateGitLabCreateProjectFetches = [];
globalThis.fetch = async (url, options) => {
  const body = options && options.body ? JSON.parse(options.body) : null;
  globalThis.__swateGitLabCreateProjectFetches.push({ url, options, body });
  return {
    ok: false,
    status: 400,
    headers: { get: () => null },
    text: async () => JSON.stringify({ message: { name: ["has already been taken"], path: ["has already been taken"] } }),
    json: async () => ({ message: { name: ["has already been taken"], path: ["has already been taken"] } })
  };
};
""")>]
let private installGitLabCreateProjectFailureFetchSpy () : unit = jsNative

[<Emit("globalThis.__swateGitLabCreateProjectFetches[globalThis.__swateGitLabCreateProjectFetches.length - 1].body")>]
let private lastGitLabCreateProjectBody () : obj = jsNative

[<Emit("Object.prototype.hasOwnProperty.call($0, $1)")>]
let private hasOwnProperty (target: obj) (propertyName: string) : bool = jsNative

[<Emit("$0.firstElementChild")>]
let private firstElementChild (target: HTMLElement) : HTMLElement = jsNative

[<Emit("Array.from(document.body.querySelectorAll('button')).find((button) => button.textContent && button.textContent.includes($0))")>]
let private findBodyButtonContaining (text: string) : HTMLButtonElement option = jsNative

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

let private manyChangedFiles count = [|
    for i in 0 .. count - 1 -> changedFile (sprintf "src/file-%03i.txt" i) "M" " " false
|]

let private joinLines lines = String.concat "\n" lines + "\n"

let private buildAddedFileDiff path lines =
    [
        "new file mode 100644"
        "--- /dev/null"
        $"+++ b/{path}"
        yield! lines |> Array.map (fun line -> $"+{line}")
        "~"
        ""
    ]
    |> String.concat "\n"

let private buildSingleConflictDocument currentLines incomingLines =
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
    let rec loop i n =
        let j = haystack.IndexOf(needle, i, StringComparison.Ordinal)
        if j < 0 then n else loop (j + needle.Length) (n + 1)

    loop 0 0

let private unexpectedPromise<'T> name : JS.Promise<Result<'T, string>> = promise { return failwith name }
let private unexpectedGitLab<'T> name : JS.Promise<Result<'T, GitLabError>> = promise { return failwith name }

let private diffPage path =
    PageState.GitDiffPage {
        Path = path
        PreviousContent = "before"
        CurrentContent = "after"
        WordDiffText = "diff"
    }

let private defaultDependencies: GitDependencies = {
    getSessionInfo = fun _ -> promise { return Ok(succeeded sessionInfo) }
    getStatus = fun _ -> unexpectedPromise "getStatus"
    listRefs = fun _ -> unexpectedPromise "listRefs"
    getRepositoryWebUrl = fun _ -> promise { return Ok(succeeded None) }
    getStoragePolicySettings = fun _ -> unexpectedPromise "getStoragePolicySettings"
    setStoragePolicySettings = fun _ -> unexpectedPromise "setStoragePolicySettings"
    loadDiffPage = fun _ -> unexpectedPromise "loadDiffPage"
    loadConflictPage = fun _ _ _ -> unexpectedPromise "loadConflictPage"
    initializeWorkspace = fun _ -> unexpectedPromise "initializeWorkspace"
    bindWorkspace = fun _ -> unexpectedPromise "bindWorkspace"
    createRemoteProject = fun _ -> unexpectedGitLab "createRemoteProject"
    renameOpenArcRoot = fun _ -> unexpectedPromise "renameOpenArcRoot"
    checkDependencies = fun _ -> unexpectedPromise "checkDependencies"
    installDependency = fun _ -> unexpectedPromise "installDependency"
    refreshSynchronization = fun _ -> unexpectedPromise "refreshSynchronization"
    synchronize = fun _ -> unexpectedPromise "synchronize"
    reportPhase = ignore
    cancelOperation = fun _ -> unexpectedPromise "cancelOperation"
    cloneWorkspace = fun _ -> unexpectedPromise "cloneWorkspace"
    createRef = fun _ -> unexpectedPromise "createRef"
    preflightSwitchRef = fun _ -> unexpectedPromise "preflightSwitchRef"
    switchRef = fun _ -> unexpectedPromise "switchRef"
    createRevision = fun _ -> unexpectedPromise "createRevision"
    restorePaths = fun _ -> unexpectedPromise "restorePaths"
    resolveConflict = fun _ -> unexpectedPromise "resolveConflict"
    finalizeConflict = fun _ -> unexpectedPromise "finalizeConflict"
    cancelConflict = fun _ -> unexpectedPromise "cancelConflict"
    listObjects = fun _ -> unexpectedPromise "listObjects"
    materializeObject = fun _ -> unexpectedPromise "materializeObject"
    pruneStorage = fun _ -> unexpectedPromise "pruneStorage"
    deduplicateStorage = fun _ -> unexpectedPromise "deduplicateStorage"
    clearStaleLock = fun _ -> unexpectedPromise "clearStaleLock"
    newOperationId = fun () -> "op-1"
    confirmLfsPrune = fun _ -> false
    confirmInstall = fun _ -> false
    reportError = fun _ -> ()
}

let private renderToBody element = promise {
    let c = document.createElement ("div") :?> HTMLDivElement
    document.body.appendChild c |> ignore
    let r = ReactDOM.createRoot c
    r.render element
    do! Promise.sleep 0

    return
        c,
        (fun () ->
            r.unmount ()
            c.remove ()
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
    OnDiscardSelection = fun _ -> ()
    OnConfirmPendingRemoteAction = fun () -> ()
    OnCancelPendingRemoteAction = fun () -> ()
    OnSaveDownloadLargeFiles = fun _ -> ()
    OnSaveLfsAutoTrackThreshold = fun _ -> ()
    OnCreateBranch = fun _ -> ()
    OnSwitchBranch = fun _ -> ()
    OnSelectChange = fun _ -> promise { return Ok() }
    OnPruneLfsCache = fun () -> ()
    OnDedupLfsStorage = fun () -> ()
    OnCancelOperation = fun () -> ()
}

Vitest.afterEach (fun () -> document.body.innerHTML <- "")

let private collectMessages cmd = promise {
    let messages = ResizeArray<Msg>()
    cmd |> List.iter (fun sub -> sub messages.Add)
    do! Promise.sleep 0
    return messages |> Seq.toArray
}

/// A primary save reports its phases before it completes. Tests that only care about the
/// completion collect through this helper.
let private collectWriteMessages cmd = promise {
    let! messages = collectMessages cmd

    return
        messages
        |> Array.filter (fun message ->
            match message with
            | WritePhaseChanged _ -> false
            | _ -> true
        )
}

let private runningState = {
    GitState.Empty with
        CurrentArcPath = Some "C:/arc"
        ArcSessionId = 1
        WorkspaceVersion = Some "v1"
}

let private completeOne deps model cmd = promise {
    let! messages = collectMessages cmd

    match messages with
    | [| message |] -> return update deps ignore message model
    | _ -> return failwith "Expected one command message."
}

let private refreshed status = {
    Session = Ok sessionInfo
    Status = Ok status
    Refs = Ok refs
    LfsSettings = Ok(lfsSettings 5 true)
    OriginRemoteRepositoryWebUrl = None
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
                Vitest.expect(prepared.BusyOperation).toEqual (GitBusyOperation.CommittingAllChanges)
        )

        Vitest.test (
            "prepareCommitSelection keeps the selected paths exact and drops duplicates and empty entries",
            fun () ->
                let prepared =
                    prepareCommitSelection {
                        Message = " save "
                        Paths = [| " leading-space.txt"; "b.txt"; ""; "b.txt" |]
                    }

                Vitest.expect(prepared.NormalizedMessage).toBe ("save")
                Vitest.expect(prepared.PathsToCommit).toEqual ([| " leading-space.txt"; "b.txt" |])
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
                    .expect(buildUpdatedLfsSettings state (Some 4) None)
                    .toEqual (
                        {
                            AutoPolicyThresholdMb = Some 4
                            MaterializeLargeObjects = true
                        }
                    )

                Vitest
                    .expect(buildUpdatedLfsSettings state None (Some false))
                    .toEqual (
                        {
                            AutoPolicyThresholdMb = Some 7
                            MaterializeLargeObjects = false
                        }
                    )
        )

        Vitest.test (
            "GitLabApi.CreateProject lets GitLab generate the project path from the submitted name",
            fun () -> promise {
                installGitLabCreateProjectFetchSpy ()

                try
                    let! result = GitLabApi.CreateProject("https://gitlab.example/", "token-123", " My ARC Project ")

                    match result with
                    | Error err -> failwith err.GitLabErrorToString
                    | Ok project ->
                        Vitest.expect(project.name).toBe ("My ARC Project")
                        Vitest.expect(project.path_with_namespace).toBe ("carol/my-arc-project")

                    let body = lastGitLabCreateProjectBody ()

                    Vitest.expect(getProperty<string> body "name").toBe ("My ARC Project")
                    Vitest.expect(getProperty<bool> body "initialize_with_readme").toBe (false)
                    Vitest.expect(hasOwnProperty body "path").toBe (false)
                finally
                    cleanupGitLabCreateProjectFetchSpy ()
            }
        )

        Vitest.test (
            "GitLabApi.CreateProject includes GitLab's duplicate-name response in the error",
            fun () -> promise {
                installGitLabCreateProjectFailureFetchSpy ()

                try
                    let! result = GitLabApi.CreateProject("https://gitlab.example/", "token-123", "Existing ARC")

                    match result with
                    | Ok _ -> failwith "Expected duplicate project creation to fail."
                    | Error error ->
                        let message = error.GitLabErrorToString
                        Vitest.expect(message).toContain ("HTTP 400")
                        Vitest.expect(message).toContain ("has already been taken")
                finally
                    cleanupGitLabCreateProjectFetchSpy ()
            }
        )
)

Vitest.describe (
    "GitWorkflow LFS storage maintenance",
    fun () ->
        Vitest.test (
            "PruneLfsCacheRequested asks for confirmation before running prune",
            fun () -> promise {
                let mutable confirmedMessage = None

                let deps = {
                    defaultDependencies with
                        confirmLfsPrune =
                            fun message ->
                                confirmedMessage <- Some message
                                true
                        pruneStorage = fun _ -> promise { return Ok(succeeded "pruned") }
                }

                let model = {
                    GitState.Empty with
                        CurrentArcPath = Some "C:/arc"
                        ArcSessionId = 1
                }

                let nextModel, cmd = update deps ignore PruneLfsCacheRequested model
                let! _ = collectMessages cmd

                Vitest.expect(confirmedMessage.IsSome).toBe (true)
                Vitest.expect(nextModel.BusyOperation).toBe (None)
            }
        )

        Vitest.test (
            "DedupLfsStorageRequested starts dedup write operation",
            fun () -> promise {
                let model = {
                    GitState.Empty with
                        CurrentArcPath = Some "C:/arc"
                        ArcSessionId = 1
                }

                let nextModel, _ =
                    update defaultDependencies ignore (WriteRequested DedupLfsStorage) model

                Vitest.expect(nextModel.BusyOperation).toEqual (Some GitBusyOperation.DeduplicatingGitLfsStorage)
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

                let state = {
                    GitState.Empty with
                        CurrentArcPath = Some "C:/arc-a"
                        SelectedChangePath = Some "tracked.txt"
                        BusyOperation = Some GitBusyOperation.Refreshing
                }

                let nextState, cmd =
                    update defaultDependencies clearedPages.Add (ArcPathChanged(Some "C:/arc-b")) state

                let! messages = collectMessages cmd

                Vitest
                    .expect(nextState)
                    .toEqual (
                        {
                            GitState.Empty with
                                CurrentArcPath = Some "C:/arc-b"
                                ArcSessionId = 1
                                RefreshRequestId = 1
                                PageLoadRequestId = 1
                        }
                    )

                Vitest.expect(clearedPages |> Seq.toArray).toEqual ([| None |])
                Vitest.expect(messages).toEqual ([| RefreshRequested |])
            }
        )

        Vitest.test (
            "RefreshCompleted reports current failures through the Git error modal",
            fun () -> promise {
                let reportedErrors = ResizeArray<GitErrorNotification>()

                let deps = {
                    defaultDependencies with
                        reportError = reportedErrors.Add
                }

                let state = {
                    GitState.Empty with
                        CurrentArcPath = Some "C:/arc"
                        RefreshRequestId = 2
                        RefreshState = GitRefreshState.Loading
                }

                let nextState, cmd =
                    update deps ignore (RefreshCompleted(2, Error "remote status failed")) state

                let! _ = collectMessages cmd

                Vitest.expect(nextState.ErrorNotice).toEqual (Some "remote status failed")
                Vitest.expect(reportedErrors.Count).toBe (1)
                Vitest.expect(reportedErrors[0].Title).toBe ("Could not refresh Git state")
                Vitest.expect(reportedErrors[0].Message).toBe ("remote status failed")
            }
        )

        Vitest.test (
            "A failed settings read keeps the current threshold and download preference",
            fun () -> promise {
                let settingsFailure =
                    makeFailure ProviderError "settings_read_failed" "The settings read failed." None [||]

                let statusFailure =
                    makeFailure ProviderError "status_read_failed" "The status read failed." None [||]

                let state = {
                    GitState.Empty with
                        CurrentArcPath = Some "C:/arc"
                        RefreshRequestId = 1
                        RefreshState = GitRefreshState.Loading
                        LfsAutoTrackThresholdMb = 42
                        DownloadLargeFiles = true
                }

                let settingsFailedRefresh = {
                    Session = Ok sessionInfo
                    Status = Ok cleanStatus
                    Refs = Ok refs
                    LfsSettings = Error settingsFailure
                    OriginRemoteRepositoryWebUrl = None
                }

                let firstState, firstCmd =
                    update defaultDependencies ignore (RefreshCompleted(1, Ok settingsFailedRefresh)) state

                let! _ = collectMessages firstCmd

                Vitest.expect(firstState.LfsAutoTrackThresholdMb).toBe (42)
                Vitest.expect(firstState.DownloadLargeFiles).toBe (true)
                Vitest.expect(firstState.ErrorNotice).toEqual (Some "The settings read failed.")

                let statusFailedRefresh = {
                    settingsFailedRefresh with
                        Status = Error statusFailure
                        LfsSettings = Ok(lfsSettings 9 false)
                }

                let secondState, secondCmd =
                    update defaultDependencies ignore (RefreshCompleted(1, Ok statusFailedRefresh)) state

                let! _ = collectMessages secondCmd

                Vitest.expect(secondState.LfsAutoTrackThresholdMb).toBe (9)
                Vitest.expect(secondState.DownloadLargeFiles).toBe (false)
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
                    Session = Ok sessionInfo
                    Status = Ok(statusForBranch "feature/stale")
                    Refs = Ok [| localBranch "feature/stale" true true |]
                    LfsSettings = Ok(lfsSettings 9 false)
                    OriginRemoteRepositoryWebUrl = Some "https://example.org/feature/stale"
                }

                let nextState, cmd =
                    update defaultDependencies ignore (RefreshCompleted(1, Ok staleRefresh)) state

                let! messages = collectMessages cmd

                Vitest.expect(nextState.Status.CurrentBranch).toEqual (Some "feature/live")
                Vitest.expect(messages).toEqual ([||])
            }
        )

        Vitest.test (
            "RefreshCompleted does not report stale failures through the Git error modal",
            fun () -> promise {
                let reportedErrors = ResizeArray<GitErrorNotification>()

                let deps = {
                    defaultDependencies with
                        reportError = reportedErrors.Add
                }

                let state = {
                    GitState.Empty with
                        CurrentArcPath = Some "C:/arc-a"
                        RefreshRequestId = 2
                        ErrorNotice = Some "keep current error"
                }

                let nextState, cmd =
                    update deps ignore (RefreshCompleted(1, Error "older refresh failed")) state

                let! messages = collectMessages cmd

                Vitest.expect(messages).toEqual ([||])
                Vitest.expect(nextState.ErrorNotice).toEqual (Some "keep current error")
                Vitest.expect(reportedErrors.Count).toBe (0)
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
            "RefreshRequested stores origin repository URL from refresh metadata",
            fun () -> promise {
                let deps = {
                    defaultDependencies with
                        getStatus = fun _ -> promise { return Ok(succeeded cleanStatus) }
                        listRefs = fun _ -> promise { return Ok(succeeded [| localBranch "main" true true |]) }
                        getStoragePolicySettings = fun _ -> promise { return Ok(succeeded (lfsSettings 3 true)) }
                        getRepositoryWebUrl =
                            fun _ -> promise { return Ok(succeeded (Some "https://github.com/nfdi4plants/Swate")) }
                }

                let state = {
                    GitState.Empty with
                        CurrentArcPath = Some "C:/arc-a"
                }

                let requestingState, requestCmd = update deps ignore RefreshRequested state
                let! requestMessages = collectMessages requestCmd

                let nextState, nextCmd =
                    match requestMessages with
                    | [| (RefreshCompleted _ as message) |] -> update deps ignore message requestingState
                    | _ -> failwith "Expected refresh completion message."

                let! nextMessages = collectMessages nextCmd

                Vitest
                    .expect(nextState.OriginRemoteRepositoryWebUrl)
                    .toEqual (Some "https://github.com/nfdi4plants/Swate")

                Vitest.expect(nextMessages).toEqual ([||])
            }
        )

        Vitest.test (
            "The refresh reads the settings for a session without a storage policy service",
            fun () -> promise {
                let sessionWithoutStoragePolicy = {
                    sessionInfo with
                        Services = {
                            sessionInfo.Services with
                                StoragePolicy = false
                        }
                }

                let deps = {
                    defaultDependencies with
                        getSessionInfo = fun _ -> promise { return Ok(succeeded sessionWithoutStoragePolicy) }
                        getStatus = fun _ -> promise { return Ok(succeeded cleanStatus) }
                        listRefs = fun _ -> promise { return Ok(succeeded [| localBranch "main" true true |]) }
                        getStoragePolicySettings = fun _ -> promise { return Ok(succeeded (lfsSettings 3 true)) }
                }

                let state = {
                    GitState.Empty with
                        CurrentArcPath = Some "C:/arc-a"
                }

                let requestingState, requestCmd = update deps ignore RefreshRequested state
                let! requestMessages = collectMessages requestCmd

                let nextState, nextCmd =
                    match requestMessages with
                    | [| (RefreshCompleted _ as message) |] -> update deps ignore message requestingState
                    | _ -> failwith "Expected refresh completion message."

                let! nextMessages = collectMessages nextCmd

                Vitest.expect(nextState.LfsAutoTrackThresholdMb).toBe (3)
                Vitest.expect(nextState.DownloadLargeFiles).toBe (true)
                Vitest.expect(nextMessages).toEqual ([||])
            }
        )

        Vitest.test (
            "RefreshRequested keeps refresh successful when origin repository URL lookup fails",
            fun () -> promise {
                let deps = {
                    defaultDependencies with
                        getStatus = fun _ -> promise { return Ok(succeeded cleanStatus) }
                        listRefs = fun _ -> promise { return Ok(succeeded [| localBranch "main" true true |]) }
                        getStoragePolicySettings = fun _ -> promise { return Ok(succeeded (lfsSettings 2 false)) }
                        getRepositoryWebUrl = fun _ -> promise { return Error "origin lookup failed" }
                }

                let state = {
                    GitState.Empty with
                        CurrentArcPath = Some "C:/arc-a"
                }

                let requestingState, requestCmd = update deps ignore RefreshRequested state
                let! requestMessages = collectMessages requestCmd

                let nextState, nextCmd =
                    match requestMessages with
                    | [| (RefreshCompleted _ as message) |] -> update deps ignore message requestingState
                    | _ -> failwith "Expected refresh completion message."

                let! nextMessages = collectMessages nextCmd

                Vitest.expect(nextState.ErrorNotice).toEqual (None)
                Vitest.expect(nextState.OriginRemoteRepositoryWebUrl).toEqual (None)
                Vitest.expect(nextMessages).toEqual ([||])
            }
        )

        Vitest.test (
            "RefreshCompleted records missing-repository state instead of leaving the sidebar in generic error mode",
            fun () -> promise {
                let clearedPages = ResizeArray<PageState option>()

                let missing =
                    makeFailure
                        NotFound
                        VersionControlCodes.WorkspaceUnmanaged
                        "The selected ARC path is not a git repository."
                        None
                        [||]

                let refreshResult = {
                    Session = Ok sessionInfo
                    Status = Error missing
                    Refs = Error missing
                    LfsSettings = Error missing
                    OriginRemoteRepositoryWebUrl = None
                }

                let state = {
                    GitState.Empty with
                        CurrentArcPath = Some "C:/arc-a"
                        RefreshRequestId = 1
                        RefreshState = GitRefreshState.Loading
                        BusyOperation = Some GitBusyOperation.Refreshing
                }

                let nextState, cmd =
                    update defaultDependencies clearedPages.Add (RefreshCompleted(1, Ok refreshResult)) state

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
                        clearedPages.Add
                        (ConfirmMergeResolutionCompleted(
                            state.ArcSessionId,
                            Error(ConfirmMergeResolutionError.Stale "stale conflict handle")
                        ))
                        state

                let! messages = collectMessages cmd

                Vitest.expect(nextState.SelectedChangePath).toEqual (None)
                Vitest.expect(nextState.MergeResolutionPendingPath).toEqual (None)
                Vitest.expect(clearedPages |> Seq.toArray).toEqual ([| None |])
                Vitest.expect(messages).toEqual ([| RefreshRequested |])
            }
        )

        Vitest.test (
            "ConfirmMergeResolutionCompleted ignores late results from the previous ARC",
            fun () -> promise {
                let pageStates = ResizeArray<PageState option>()
                let conflict = (conflictedStatus [| "conflict-a.txt" |]).ActiveConflictSession.Value

                let refreshedConflict =
                    (conflictedStatus [| "conflict-b.txt" |]).ActiveConflictSession.Value

                let deps = {
                    defaultDependencies with
                        resolveConflict =
                            fun _ -> promise {
                                return
                                    Ok(
                                        succeeded {
                                            RefreshedHandle = refreshedConflict.Handle
                                            RemainingItems = refreshedConflict.Items
                                        }
                                    )
                            }
                        getStatus = fun _ -> promise { return Ok(succeeded (conflictedStatus [| "conflict-b.txt" |])) }
                        loadConflictPage = fun _ _ path -> promise { return Ok(diffPage path) }
                }

                let request = {
                    Path = "conflict-a.txt"
                    Handle = conflict.Handle
                    WorkspaceVersion = "v1"
                    ResolvedContent = "resolved"
                }

                let initialState = {
                    GitState.Empty with
                        CurrentArcPath = Some "C:/arc-a"
                        WorkspaceVersion = Some "v1"
                        SelectedChangePath = Some "conflict-a.txt"
                }

                let stateAfterRequest, requestCmd =
                    update deps pageStates.Add (ConfirmMergeResolutionRequested request) initialState

                let switchedState, switchCmd =
                    update deps pageStates.Add (ArcPathChanged(Some "C:/arc-b")) stateAfterRequest

                let! _ = collectMessages switchCmd
                let! completionMessages = collectMessages requestCmd

                let nextState, finishCmd =
                    match completionMessages with
                    | [| ConfirmMergeResolutionCompleted _ |] ->
                        update deps pageStates.Add completionMessages[0] switchedState
                    | _ -> failwith "Expected a merge-resolution completion message."

                let! _ = collectMessages finishCmd

                Vitest
                    .expect(nextState)
                    .toEqual (
                        {
                            GitState.Empty with
                                CurrentArcPath = Some "C:/arc-b"
                                ArcSessionId = 1
                                RefreshRequestId = 1
                                PageLoadRequestId = 1
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
            "Primary save creates the revision with exactly the selected paths and never stages separately",
            fun () -> promise {
                let mutable captured = None

                let deps = {
                    defaultDependencies with
                        createRevision =
                            fun request ->
                                captured <- Some request
                                promise { return Ok(failed ProviderError "git_failure" "stop") }
                }

                let model, command =
                    update
                        deps
                        ignore
                        (PrimarySaveSelectionRequested {
                            Message = "save"
                            Paths = [| " leading-space.txt"; "b.txt"; "b.txt" |]
                        })
                        runningState

                let! messages = collectMessages command
                let _, writeCmd = update deps ignore messages[0] model
                let! _ = collectMessages writeCmd
                Vitest.expect(captured.Value.Paths).toEqual ([| " leading-space.txt"; "b.txt" |])
                Vitest.expect(captured.Value.ExpectedWorkspaceVersion).toBe ("v1")
            }
        )

        Vitest.test (
            "Synchronize never provisions when the target is unreachable",
            fun () -> promise {
                let mutable projectCalls = 0

                let deps = {
                    defaultDependencies with
                        synchronize =
                            fun request ->
                                if request.PublishLocalRevisions then
                                    promise {
                                        return
                                            Ok(
                                                failed
                                                    Network
                                                    VersionControlCodes.TargetUnreachable
                                                    "target unreachable"
                                            )
                                    }
                                else
                                    unexpectedPromise "unexpected pull"
                        createRemoteProject =
                            fun _ ->
                                projectCalls <- projectCalls + 1
                                unexpectedGitLab "unexpected"
                }

                let model, command =
                    update deps ignore (WriteRequested(Push GitUpdateAcceptance.RequirePreview)) runningState

                let! messages = collectMessages command
                let next, finish = update deps ignore messages[0] model
                let! _ = collectMessages finish
                Vitest.expect(projectCalls).toBe (0)
                Vitest.expect(next.ErrorNotice).toEqual (Some "target unreachable")
            }
        )

        Vitest.test (
            "A duplicate remote project name opens the rename prompt",
            fun () -> promise {
                let deps = {
                    defaultDependencies with
                        synchronize =
                            fun request ->
                                if request.PublishLocalRevisions then
                                    promise {
                                        return Ok(failed Validation VersionControlCodes.PublishTargetMissing "missing")
                                    }
                                else
                                    unexpectedPromise "unexpected pull"
                        createRemoteProject = fun _ -> promise { return Error(GitLabError.HttpError 400) }
                }

                let model, command =
                    update deps ignore (WriteRequested(Push GitUpdateAcceptance.RequirePreview)) {
                        runningState with
                            CurrentArcPath = Some "C:/work/Existing ARC"
                    }

                let! messages = collectMessages command
                let next, finish = update deps ignore messages[0] model
                let! _ = collectMessages finish
                Vitest.expect(next.PendingPublishRename |> Option.map _.CurrentName).toEqual (Some "Existing ARC")
            }
        )

        Vitest.test (
            "Cancel sends the key reported by the started event",
            fun () -> promise {
                let mutable captured = None

                let deps = {
                    defaultDependencies with
                        cancelOperation =
                            fun key ->
                                captured <- Some key
                                promise { return Ok true }
                }

                let model, _ =
                    update deps ignore (WriteRequested(Push GitUpdateAcceptance.RequirePreview)) runningState

                let model, _ =
                    update
                        deps
                        ignore
                        (OperationStarted {
                            SessionId = "s-1"
                            OperationId = "op-1"
                        })
                        model

                let _, command = update deps ignore CancelCurrentOperationRequested model
                let! _ = collectMessages command

                Vitest
                    .expect(captured)
                    .toEqual (
                        Some {
                            SessionId = "s-1"
                            OperationId = "op-1"
                        }
                    )
            }
        )

        Vitest.test (
            "A started operation the workflow did not allocate does not become the cancel target",
            fun () -> promise {
                let requested, _ =
                    update defaultDependencies ignore (WriteRequested Fetch) runningState

                let afterUnrelated, _ =
                    update
                        defaultDependencies
                        ignore
                        (OperationStarted {
                            SessionId = "s"
                            OperationId = "explorer-materialize"
                        })
                        requested

                let afterOwned, _ =
                    update
                        defaultDependencies
                        ignore
                        (OperationStarted {
                            SessionId = "s"
                            OperationId = "op-1/2"
                        })
                        afterUnrelated

                Vitest.expect(afterUnrelated.CurrentOperation).toEqual (Some { SessionId = ""; OperationId = "op-1" })

                Vitest
                    .expect(afterOwned.CurrentOperation)
                    .toEqual (
                        Some {
                            SessionId = "s"
                            OperationId = "op-1/2"
                        }
                    )
            }
        )

        Vitest.test (
            "Storage settings are sent as one record with the threshold as Some",
            fun () -> promise {
                let mutable captured = None

                let deps = {
                    defaultDependencies with
                        setStoragePolicySettings =
                            fun request ->
                                captured <- Some request
                                promise { return Ok(succeeded ()) }
                }

                let model, command =
                    update deps ignore (SaveLfsAutoTrackThresholdRequested 4) runningState

                let! messages = collectMessages command
                let model, write = update deps ignore messages[0] model
                let! _ = collectMessages write

                Vitest
                    .expect(captured.Value.Settings)
                    .toEqual (
                        {
                            AutoPolicyThresholdMb = Some 4
                            MaterializeLargeObjects = false
                        }
                    )
            }
        )

        Vitest.test (
            "Discard sends restorePaths with the exact paths and the current workspace version",
            fun () -> promise {
                let mutable captured = None
                let pageStates = ResizeArray<PageState option>()

                let deps = {
                    defaultDependencies with
                        restorePaths =
                            fun request ->
                                captured <- Some request
                                promise { return Ok(succeeded ()) }
                        getStatus = fun _ -> promise { return Ok(succeeded cleanStatus) }
                        listRefs = fun _ -> promise { return Ok(succeeded refs) }
                        getStoragePolicySettings = fun _ -> promise { return Ok(succeeded (lfsSettings 5 true)) }
                }

                let model, command =
                    update deps pageStates.Add (DiscardSelectionRequested [| " leading.txt"; "b.txt"; "b.txt" |]) {
                        runningState with
                            SelectedChangePath = Some "b.txt"
                    }

                let! messages = collectMessages command
                let requested, write = update deps pageStates.Add messages[0] model
                let! completion = collectMessages write

                let finalState, finishCmd =
                    match completion with
                    | [| WriteCompleted(_, _, DiscardSelection _, Ok(Completed _)) |] ->
                        update deps pageStates.Add completion[0] requested
                    | _ -> failwith "Expected discard to complete successfully."

                let! _ = collectMessages finishCmd
                Vitest.expect(captured.Value.Paths).toEqual ([| " leading.txt"; "b.txt" |])
                Vitest.expect(captured.Value.ExpectedWorkspaceVersion).toBe ("v1")
                Vitest.expect(pageStates |> Seq.toArray).toEqual ([| None |])
                Vitest.expect(finalState.SelectedChangePath).toEqual (None)
            }
        )

        Vitest.test (
            "Switching to a branch sends its opaque provider ref",
            fun () -> promise {
                let mutable captured = None

                let deps = {
                    defaultDependencies with
                        preflightSwitchRef =
                            fun _ -> promise { return Ok(succeeded { PathsAtRisk = [||]; IsSafe = true }) }
                        switchRef =
                            fun request ->
                                captured <- Some request
                                promise { return Ok(succeeded cleanStatus) }
                }

                let model, command =
                    update deps ignore (SwitchBranchRequested "feature") {
                        runningState with
                            Refs = [| localBranch "feature" false false |]
                    }

                let! messages = collectMessages command
                let preflightState, writeRequest = update deps ignore messages[0] model
                let! writeMessages = collectMessages writeRequest
                let model, write = update deps ignore writeMessages[0] preflightState
                let! _ = collectMessages write
                Vitest.expect(captured.Value.TargetRef).toBe ("git-local:feature")
            }
        )

        Vitest.test (
            "An unsafe branch switch names the paths at risk and does not write",
            fun () -> promise {
                let reportedErrors = ResizeArray<GitErrorNotification>()

                let deps = {
                    defaultDependencies with
                        preflightSwitchRef =
                            fun _ -> promise {
                                return
                                    Ok(
                                        succeeded {
                                            PathsAtRisk = [| "data.txt"; "metadata.tsv" |]
                                            IsSafe = false
                                        }
                                    )
                            }
                        reportError = reportedErrors.Add
                }

                let model, command =
                    update deps ignore (SwitchBranchRequested "feature") {
                        runningState with
                            Refs = [| localBranch "feature" false false |]
                    }

                let! messages = collectMessages command
                let nextState, finishCmd = update deps ignore messages[0] model
                let! finishMessages = collectMessages finishCmd

                Vitest.expect(nextState.BusyOperation).toEqual (None)
                Vitest.expect(nextState.ErrorNotice.Value).toContain ("data.txt")
                Vitest.expect(nextState.ErrorNotice.Value).toContain ("metadata.tsv")
                Vitest.expect(nextState.ErrorNotice.Value).toContain ("Save or discard")
                Vitest.expect(finishMessages).toEqual ([||])
                Vitest.expect(reportedErrors.Count).toBe (1)
            }
        )

        Vitest.test (
            "A partial branch preflight does not switch",
            fun () -> promise {
                let reportedErrors = ResizeArray<GitErrorNotification>()

                let deps = {
                    defaultDependencies with
                        reportError = reportedErrors.Add
                }

                let failure =
                    makeFailure ProviderError "preflight_incomplete" "The branch preflight did not finish." None [||]

                let model = {
                    runningState with
                        BusyOperation = Some GitBusyOperation.SwitchingBranch
                        BusyNotice = Some "Switching branch"
                        CurrentOperation =
                            Some {
                                SessionId = ""
                                OperationId = "switch-op"
                            }
                }

                let nextState, command =
                    update
                        deps
                        ignore
                        (SwitchBranchPreflightCompleted(
                            model.ArcSessionId,
                            "feature",
                            Ok(
                                OperationResultDto.PartiallySucceeded(
                                    operation { PathsAtRisk = [||]; IsSafe = true },
                                    failure
                                )
                            )
                        ))
                        model

                let! messages = collectMessages command

                Vitest.expect(nextState.BusyOperation).toEqual (None)
                Vitest.expect(nextState.ErrorNotice).toEqual (Some failure.Message)
                Vitest.expect(messages).toEqual ([||])
                Vitest.expect(reportedErrors.Count).toBe (1)
                Vitest.expect(reportedErrors[0].Title).toBe ("Could not switch branch")
            }
        )

        Vitest.test (
            "A stale branch preflight reply is ignored",
            fun () -> promise {
                let state = {
                    runningState with
                        BusyOperation = Some GitBusyOperation.SwitchingBranch
                        CurrentOperation =
                            Some {
                                SessionId = ""
                                OperationId = "switch-op"
                            }
                        Refs = [| localBranch "feature" false false |]
                }

                let nextState, cmd =
                    update
                        defaultDependencies
                        ignore
                        (SwitchBranchPreflightCompleted(
                            state.ArcSessionId - 1,
                            "feature",
                            Ok(succeeded { PathsAtRisk = [||]; IsSafe = true })
                        ))
                        state

                let! messages = collectMessages cmd

                Vitest.expect(nextState).toEqual (state)
                Vitest.expect(messages).toEqual ([||])
            }
        )

        Vitest.test (
            "WriteCompleted reports write failures through the Git error modal",
            fun () -> promise {
                let reportedErrors = ResizeArray<GitErrorNotification>()

                let deps = {
                    defaultDependencies with
                        reportError = reportedErrors.Add
                }

                let state = {
                    GitState.Empty with
                        CurrentArcPath = Some "C:/arc"
                        WriteRequestId = 3
                        BusyOperation = Some GitBusyOperation.PushingToRemote
                }

                let nextState, cmd =
                    update
                        deps
                        ignore
                        (WriteCompleted(
                            state.ArcSessionId,
                            3,
                            Push GitUpdateAcceptance.RequirePreview,
                            Error "remote rejected the push"
                        ))
                        state

                let! _ = collectMessages cmd

                Vitest.expect(nextState.ErrorNotice).toEqual (Some "remote rejected the push")
                Vitest.expect(reportedErrors.Count).toBe (1)
                Vitest.expect(reportedErrors[0].Title).toBe ("Could not push changes")
                Vitest.expect(reportedErrors[0].Message).toBe ("remote rejected the push")
            }
        )

        Vitest.test (
            "WriteCompleted with a cancellation failure shows a warning notice instead of an error",
            fun () -> promise {
                let canceled =
                    makeFailure Canceled VersionControlCodes.OperationCanceled "The pull was canceled." None [||]

                let deps = {
                    defaultDependencies with
                        synchronize =
                            fun request ->
                                if request.PublishLocalRevisions then
                                    unexpectedPromise "unexpected push"
                                else
                                    promise { return Ok(OperationResultDto.Failed canceled) }
                }

                let state = { runningState with WriteRequestId = 0 }

                let stateAfterRequest, requestCmd =
                    update deps ignore (WriteRequested(Pull GitUpdateAcceptance.RequirePreview)) state

                let! completionMessages = collectMessages requestCmd

                let stateAfterWrite, finishCmd =
                    match completionMessages with
                    | [| WriteCompleted(_, _, Pull _, Ok(OperationCancelled _)) |] ->
                        update deps ignore completionMessages[0] stateAfterRequest
                    | _ -> failwith "Expected a canceled pull completion."

                let! finishMessages = collectMessages finishCmd

                Vitest.expect(stateAfterWrite.BusyOperation).toEqual (None)
                Vitest.expect(stateAfterWrite.WarningNotice).toEqual (Some "Git operation cancelled.")
                Vitest.expect(stateAfterWrite.ErrorNotice).toEqual (None)
                Vitest.expect(finishMessages).toEqual ([||])
            }
        )

        Vitest.test (
            "WriteCompleted surfaces a genuine pull failure whose message ends in git's 'Aborting'",
            fun () -> promise {
                let overwrittenByMergeError =
                    "error: Your local changes would be overwritten by merge.\nAborting"

                let reportedErrors = ResizeArray<GitErrorNotification>()

                let deps = {
                    defaultDependencies with
                        reportError = reportedErrors.Add
                }

                let state = {
                    runningState with
                        WriteRequestId = 1
                        BusyOperation = Some GitBusyOperation.PullingFromRemote
                }

                let nextState, cmd =
                    update
                        deps
                        ignore
                        (WriteCompleted(
                            state.ArcSessionId,
                            1,
                            Pull GitUpdateAcceptance.RequirePreview,
                            Error overwrittenByMergeError
                        ))
                        state

                let! _ = collectMessages cmd

                Vitest.expect(nextState.ErrorNotice).toEqual (Some overwrittenByMergeError)
                Vitest.expect(nextState.WarningNotice).toEqual (None)
                Vitest.expect(reportedErrors.Count).toBe (1)
            }
        )

        Vitest.test (
            "WriteCompleted keeps the pull-completed context when LFS hydration is cancelled",
            fun () -> promise {
                let failure =
                    makeFailure
                        Canceled
                        VersionControlCodes.OperationCanceled
                        "Large-file hydration was canceled."
                        (Some {
                            Code = VersionControlCodes.Recovery.RetryMaterialization
                            Instructions = None
                        })
                        [||]

                let deps = {
                    defaultDependencies with
                        synchronize =
                            fun request ->
                                if request.PublishLocalRevisions then
                                    unexpectedPromise "unexpected push"
                                else
                                    promise {
                                        return
                                            Ok(
                                                OperationResultDto.PartiallySucceeded(
                                                    operation cleanStatus.Synchronization.Value,
                                                    failure
                                                )
                                            )
                                    }
                        getStatus = fun _ -> promise { return Ok(succeeded (statusForBranch "main")) }
                        listRefs = fun _ -> promise { return Ok(succeeded [| localBranch "main" true true |]) }
                        getStoragePolicySettings = fun _ -> promise { return Ok(succeeded (lfsSettings 9 true)) }
                }

                let stateAfterRequest, requestCmd =
                    update deps ignore (WriteRequested(Pull GitUpdateAcceptance.RequirePreview)) runningState

                let! completionMessages = collectMessages requestCmd

                let nextState, finishCmd =
                    match completionMessages with
                    | [| WriteCompleted(_, _, Pull _, Ok(Completed _)) |] ->
                        update deps ignore completionMessages[0] stateAfterRequest
                    | _ -> failwith "Expected the canceled hydration to complete with refreshed state."

                let! _ = collectMessages finishCmd

                Vitest.expect(nextState.ErrorNotice).toEqual (None)
                Vitest.expect(nextState.WarningNotice).toEqual (Some(failureMessage failure))

                Vitest
                    .expect(
                        nextState.PendingRecovery
                        |> Option.map (fun recovery ->
                            match recovery with
                            | GitPendingRecovery.RetryMaterialization _ -> true
                            | _ -> false
                        )
                    )
                    .toEqual (Some true)
            }
        )

        Vitest.test (
            "SetCurrentProgress ignores late Git progress after the busy operation has finished",
            fun () -> promise {
                let nextState, cmd =
                    update
                        defaultDependencies
                        ignore
                        (SetCurrentProgress(Some(sidebarProgress "Receiving objects" 72.)))
                        GitState.Empty

                let! messages = collectMessages cmd

                Vitest.expect(nextState.CurrentProgress).toEqual (None)
                Vitest.expect(currentRunStatus nextState).toEqual (None)
                Vitest.expect(messages).toEqual ([||])
            }
        )

        Vitest.test (
            "SetCurrentProgress appends Git output to the active progress notice",
            fun () -> promise {
                let current = sidebarProgress "Receiving objects" 72.

                let state = {
                    GitState.Empty with
                        BusyOperation = Some GitBusyOperation.PullingFromRemote
                        CurrentProgress = Some current
                }

                let nextState, cmd =
                    update
                        defaultDependencies
                        ignore
                        (SetCurrentProgress(Some(sidebarOutput "remote: counting objects\n")))
                        state

                let! messages = collectMessages cmd

                let expectedProgress = {
                    current with
                        Output = Some "remote: counting objects\n"
                }

                Vitest.expect(nextState.CurrentProgress).toEqual (Some expectedProgress)
                Vitest.expect(currentRunStatus nextState).toEqual (Some(GitSidebarRunStatus.Progress expectedProgress))
                Vitest.expect(messages).toEqual ([||])
            }
        )

        Vitest.test (
            "WriteRequested clears stale transfer progress before showing the new operation notice",
            fun () -> promise {
                let state = {
                    runningState with
                        BusyOperation = None
                        CurrentProgress = Some(sidebarProgress "Receiving objects" 72.)
                }

                let nextState, _ =
                    update defaultDependencies ignore (WriteRequested(Push GitUpdateAcceptance.RequirePreview)) state

                Vitest.expect(nextState.CurrentProgress).toEqual (None)
                Vitest.expect(currentRunStatus nextState).toEqual (Some(GitSidebarRunStatus.Busy "Pushing to remote"))
            }
        )

        Vitest.test (
            "SaveDownloadLargeFilesRequested updates local state immediately when no ARC is loaded",
            fun () -> promise {
                let nextState, cmd =
                    update defaultDependencies ignore (SaveDownloadLargeFilesRequested true) GitState.Empty

                let! messages = collectMessages cmd

                Vitest.expect(nextState.DownloadLargeFiles).toBe (true)
                Vitest.expect(messages).toEqual ([||])
            }
        )

        Vitest.test (
            "InitRepositoryCompleted clears the missing-repository state and schedules a refresh",
            fun () -> promise {
                let deps = {
                    defaultDependencies with
                        initializeWorkspace = fun request -> promise { return Ok(succeeded request.TargetPath) }
                }

                let state = {
                    GitState.Empty with
                        CurrentArcPath = Some "C:/arc"
                        RepositoryAvailability = GitRepositoryAvailability.MissingRepository
                }

                let stateAfterRequest, requestCmd = update deps ignore InitRepositoryRequested state
                let! completionMessages = collectMessages requestCmd

                let nextState, finishCmd =
                    match completionMessages with
                    | [| InitRepositoryCompleted(_, Ok _) |] ->
                        update deps ignore completionMessages[0] stateAfterRequest
                    | _ -> failwith "Expected repository initialization to complete."

                let! messages = collectMessages finishCmd

                Vitest.expect(nextState.RepositoryAvailability).toEqual (GitRepositoryAvailability.Ready)
                Vitest.expect(nextState.BusyOperation).toEqual (None)
                Vitest.expect(messages).toEqual ([| RefreshRequested |])
            }
        )

        Vitest.test (
            "WriteRequested allows clone requests when no ARC is loaded",
            fun () -> promise {
                let mutable replyResult = None
                let reply result = replyResult <- Some result

                let cloneRequest = {
                    OperationId = "clone-op"
                    ProviderLocation = "https://gitlab.example/carol/my-arc.git"
                    DisplayName = Some "my-arc"
                    TargetPath = "C:/clone-target"
                    TargetRef = None
                    MaterializeAllObjects = true
                }

                let deps = {
                    defaultDependencies with
                        cloneWorkspace = fun _ -> promise { return Ok(succeeded "C:/clone-target") }
                }

                let stateAfterRequest, requestCmd =
                    update deps ignore (WriteRequested(Clone(cloneRequest, reply))) GitState.Empty

                let! completionMessages = collectMessages requestCmd

                let nextState, finishCmd =
                    match completionMessages with
                    | [| WriteCompleted(_, _, Clone _, Ok(Completed(CloneSuccess root))) |] ->
                        Vitest.expect(root).toBe ("C:/clone-target")
                        update deps ignore completionMessages[0] stateAfterRequest
                    | _ -> failwith "Expected clone to return the workspace root."

                let! _ = collectMessages finishCmd

                Vitest
                    .expect(stateAfterRequest.BusyOperation)
                    .toEqual (Some(GitBusyOperation.CloningRepository "C:/clone-target"))

                Vitest.expect(nextState.BusyOperation).toEqual (None)
                Vitest.expect(replyResult).toEqual (Some(Ok "C:/clone-target"))
            }
        )

        Vitest.test (
            "A clone runs under the operation id allocated for cancellation",
            fun () -> promise {
                let mutable capturedRequest = None
                let mutable canceledKey = None
                let reply (_: Result<string, string>) = ()

                let cloneRequest = {
                    OperationId = "caller-op"
                    ProviderLocation = "https://gitlab.example/carol/my-arc.git"
                    DisplayName = Some "my-arc"
                    TargetPath = "C:/clone-target"
                    TargetRef = None
                    MaterializeAllObjects = true
                }

                let deps = {
                    defaultDependencies with
                        newOperationId = fun () -> "allocated-op"
                        cloneWorkspace =
                            fun request ->
                                capturedRequest <- Some request
                                promise { return Ok(succeeded "C:/clone-target") }
                        cancelOperation =
                            fun key ->
                                canceledKey <- Some key
                                promise { return Ok true }
                }

                let stateAfterRequest, requestCmd =
                    update deps ignore (WriteRequested(Clone(cloneRequest, reply))) GitState.Empty

                let _, cancelCmd =
                    update deps ignore CancelCurrentOperationRequested stateAfterRequest

                let! _ = collectMessages cancelCmd
                let! completionMessages = collectMessages requestCmd

                match completionMessages with
                | [| WriteCompleted(_, _, Clone _, Ok(Completed(CloneSuccess root))) |] ->
                    Vitest.expect(root).toBe ("C:/clone-target")
                | _ -> failwith "Expected clone to complete successfully."

                Vitest.expect(capturedRequest.Value.OperationId).toBe ("allocated-op")
                Vitest.expect(capturedRequest.Value.OperationId).not.toBe (cloneRequest.OperationId)
                Vitest.expect(canceledKey.Value.OperationId).toBe (capturedRequest.Value.OperationId)
                Vitest.expect(canceledKey.Value.OperationId).not.toBe (cloneRequest.OperationId)
            }
        )

        Vitest.test (
            "clone progress updates the current run status while the start-page clone is busy",
            fun () -> promise {
                let reply (_: Result<string, string>) = ()

                let cloneRequest = {
                    OperationId = "clone-op"
                    ProviderLocation = "https://gitlab.example/carol/my-arc.git"
                    DisplayName = Some "my-arc"
                    TargetPath = "C:/clone-target"
                    TargetRef = None
                    MaterializeAllObjects = true
                }

                let stateAfterRequest, _ =
                    update defaultDependencies ignore (WriteRequested(Clone(cloneRequest, reply))) GitState.Empty

                let progress = sidebarProgress "Receiving objects" 72.

                let nextState, cmd =
                    update defaultDependencies ignore (SetCurrentProgress(Some progress)) stateAfterRequest

                let! messages = collectMessages cmd

                Vitest.expect(nextState.CurrentProgress).toEqual (Some progress)
                Vitest.expect(currentRunStatus nextState).toEqual (Some(GitSidebarRunStatus.Progress progress))
                Vitest.expect(messages).toEqual ([||])
            }
        )

        Vitest.test (
            "WriteCompleted applies the refreshed snapshot after push success",
            fun () -> promise {
                let deps = {
                    defaultDependencies with
                        synchronize =
                            fun request ->
                                if request.PublishLocalRevisions then
                                    promise { return Ok(succeeded cleanStatus.Synchronization.Value) }
                                else
                                    unexpectedPromise "unexpected pull"
                        getStatus = fun _ -> promise { return Ok(succeeded (statusForBranch "feature/pushed")) }
                        listRefs =
                            fun _ -> promise { return Ok(succeeded [| localBranch "feature/pushed" true true |]) }
                        getStoragePolicySettings = fun _ -> promise { return Ok(succeeded (lfsSettings 9 true)) }
                }

                let stateAfterRequest, requestCmd =
                    update deps ignore (WriteRequested(Push GitUpdateAcceptance.RequirePreview)) runningState

                let! completionMessages = collectMessages requestCmd

                let nextState, finishCmd =
                    match completionMessages with
                    | [| WriteCompleted(_, _, Push _, Ok(Completed _)) |] ->
                        update deps ignore completionMessages[0] stateAfterRequest
                    | _ -> failwith "Expected push to complete successfully."

                let! _ = collectMessages finishCmd

                Vitest.expect(nextState.Status.CurrentBranch).toEqual (Some "feature/pushed")

                Vitest
                    .expect(
                        nextState.BranchOptions
                        |> Array.exists (fun branch -> branch.RefName = "feature/pushed")
                    )
                    .toBe (true)

                Vitest.expect(nextState.LfsAutoTrackThresholdMb).toBe (9)
                Vitest.expect(nextState.DownloadLargeFiles).toBe (true)
            }
        )

        Vitest.test (
            "SubmitPublishRenameRequested defers push until the renamed ARC path is refreshed",
            fun () -> promise {
                let renamedNames = ResizeArray<string>()

                let deps = {
                    defaultDependencies with
                        renameOpenArcRoot =
                            fun newName ->
                                renamedNames.Add newName
                                promise { return Ok "C:/work/Renamed ARC" }
                        synchronize =
                            fun request ->
                                if request.PublishLocalRevisions then
                                    promise { return Ok(succeeded cleanStatus.Synchronization.Value) }
                                else
                                    unexpectedPromise "unexpected pull"
                        getStatus = fun _ -> promise { return Ok(succeeded (statusForBranch "main")) }
                        listRefs = fun _ -> promise { return Ok(succeeded [| localBranch "main" true true |]) }
                        getStoragePolicySettings = fun _ -> promise { return Ok(succeeded (lfsSettings 5 true)) }
                }

                let initialState = {
                    runningState with
                        CurrentArcPath = Some "C:/work/Existing ARC"
                        PendingPublishRename =
                            Some {
                                CurrentName = "Existing ARC"
                                Message = "A DataHub repository named 'Existing ARC' already exists."
                            }
                }

                let stateAfterSubmit, renameCmd =
                    update deps ignore (SubmitPublishRenameRequested " Renamed ARC ") initialState

                let! renameMessages = collectMessages renameCmd

                let stateAfterRename, completionCmd =
                    match renameMessages with
                    | [| PublishRenameCompleted(_, Ok "C:/work/Renamed ARC") |] ->
                        update deps ignore renameMessages[0] stateAfterSubmit
                    | _ -> failwith "Expected rename completion to be dispatched."

                let! completionMessages = collectMessages completionCmd

                let stateAfterPathChange, pathChangeCmd =
                    update deps ignore (ArcPathChanged(Some "C:/work/Renamed ARC")) stateAfterRename

                let! pathChangeMessages = collectMessages pathChangeCmd

                let refreshingState, refreshCmd =
                    match pathChangeMessages with
                    | [| RefreshRequested |] -> update deps ignore RefreshRequested stateAfterPathChange
                    | _ -> failwith "Expected the path change to start a refresh."

                let! refreshMessages = collectMessages refreshCmd

                let refreshedState, afterRefreshCmd =
                    match refreshMessages with
                    | [| (RefreshCompleted _ as message) |] -> update deps ignore message refreshingState
                    | _ -> failwith "Expected the refresh to complete."

                let! afterRefreshMessages = collectMessages afterRefreshCmd

                Vitest.expect(renamedNames |> Seq.toArray).toEqual ([| "Renamed ARC" |])
                Vitest.expect(stateAfterSubmit.BusyOperation).toEqual (Some GitBusyOperation.RenamingRepository)
                Vitest.expect(stateAfterRename.PendingPublishRename).toEqual (None)
                Vitest.expect(stateAfterRename.PendingPublishForPath).toEqual (Some "C:/work/Renamed ARC")
                Vitest.expect(completionMessages).toEqual ([||])
                Vitest.expect(stateAfterPathChange.PendingPublishAfterRefresh).toBe (true)
                Vitest.expect(stateAfterPathChange.PendingPublishForPath).toEqual (None)
                Vitest.expect(refreshedState.PendingPublishAfterRefresh).toBe (false)

                Vitest
                    .expect(afterRefreshMessages)
                    .toEqual (
                        [|
                            WriteRequested(Push GitUpdateAcceptance.RequirePreview)
                        |]
                    )

                let stateAfterPush, pushCmd =
                    match afterRefreshMessages with
                    | [| WriteRequested(Push GitUpdateAcceptance.RequirePreview) |] ->
                        update deps ignore afterRefreshMessages[0] refreshedState
                    | _ -> failwith "Expected push to start after the refresh."

                let! pushMessages = collectMessages pushCmd

                let finalState, finishCmd =
                    match pushMessages with
                    | [| WriteCompleted(_, _, Push _, Ok(Completed _)) |] ->
                        update deps ignore pushMessages[0] stateAfterPush
                    | _ -> failwith "Expected retried push to complete successfully."

                let! _ = collectMessages finishCmd

                Vitest.expect(finalState.ErrorNotice).toEqual (None)
            }
        )

        Vitest.test (
            "PublishRenameCompleted before the path change defers the publish until the refresh of the new path",
            fun () -> promise {
                let deps = {
                    defaultDependencies with
                        getStatus = fun _ -> promise { return Ok(succeeded (statusForBranch "main")) }
                        listRefs = fun _ -> promise { return Ok(succeeded [| localBranch "main" true true |]) }
                        getStoragePolicySettings = fun _ -> promise { return Ok(succeeded (lfsSettings 5 true)) }
                }

                let state = {
                    GitState.Empty with
                        CurrentArcPath = Some "C:/work/Existing ARC"
                        ArcSessionId = 7
                        BusyOperation = Some GitBusyOperation.RenamingRepository
                        BusyNotice = Some "Renaming ARC"
                        PendingPublishRename =
                            Some {
                                CurrentName = "Existing ARC"
                                Message = "A DataHub repository named 'Existing ARC' already exists."
                            }
                }

                let afterRename, renameCmd =
                    update deps ignore (PublishRenameCompleted(7, Ok "C:/work/Renamed ARC")) state

                let! renameMessages = collectMessages renameCmd

                Vitest.expect(renameMessages).toEqual ([||])
                Vitest.expect(afterRename.PendingPublishForPath).toEqual (Some "C:/work/Renamed ARC")

                let afterPathChange, pathChangeCmd =
                    update deps ignore (ArcPathChanged(Some "C:/work/Renamed ARC")) afterRename

                let! pathChangeMessages = collectMessages pathChangeCmd

                Vitest.expect(afterPathChange.PendingPublishAfterRefresh).toBe (true)
                Vitest.expect(afterPathChange.PendingPublishForPath).toEqual (None)
                Vitest.expect(pathChangeMessages).toEqual ([| RefreshRequested |])

                let refreshingState, refreshCmd =
                    update deps ignore pathChangeMessages[0] afterPathChange

                let! refreshMessages = collectMessages refreshCmd

                let refreshedState, afterRefreshCmd =
                    match refreshMessages with
                    | [| (RefreshCompleted _ as message) |] -> update deps ignore message refreshingState
                    | _ -> failwith "Expected the refresh to complete."

                let! afterRefreshMessages = collectMessages afterRefreshCmd

                Vitest.expect(refreshedState.PendingPublishAfterRefresh).toBe (false)

                Vitest
                    .expect(afterRefreshMessages)
                    .toEqual (
                        [|
                            WriteRequested(Push GitUpdateAcceptance.RequirePreview)
                        |]
                    )
            }
        )

        Vitest.test (
            "PublishRenameCompleted still retries push when path-change update arrives first",
            fun () -> promise {
                let deps = {
                    defaultDependencies with
                        getStatus = fun _ -> promise { return Ok(succeeded (statusForBranch "main")) }
                        listRefs = fun _ -> promise { return Ok(succeeded [| localBranch "main" true true |]) }
                        getStoragePolicySettings = fun _ -> promise { return Ok(succeeded (lfsSettings 5 true)) }
                }

                let state = {
                    GitState.Empty with
                        CurrentArcPath = Some "C:/work/Existing ARC"
                        ArcSessionId = 7
                        BusyOperation = Some GitBusyOperation.RenamingRepository
                        BusyNotice = Some "Renaming ARC"
                        PendingPublishRename =
                            Some {
                                CurrentName = "Existing ARC"
                                Message = "A DataHub repository named 'Existing ARC' already exists."
                            }
                }

                let stateAfterPathChange, pathChangeCmd =
                    update deps ignore (ArcPathChanged(Some "C:/work/Renamed ARC")) state

                let! pathChangeMessages = collectMessages pathChangeCmd

                let stateAfterRenameCompletion, retryCmd =
                    update deps ignore (PublishRenameCompleted(7, Ok "C:/work/Renamed ARC")) stateAfterPathChange

                let! retryMessages = collectMessages retryCmd

                // The refresh started by the path change loads the workspace token, and only
                // then does the publish run.
                let refreshingState, refreshCmd =
                    match pathChangeMessages with
                    | [| RefreshRequested |] -> update deps ignore RefreshRequested stateAfterRenameCompletion
                    | _ -> failwith "Expected the path change to start a refresh."

                let! refreshMessages = collectMessages refreshCmd

                let refreshedState, afterRefreshCmd =
                    match refreshMessages with
                    | [| (RefreshCompleted _ as message) |] -> update deps ignore message refreshingState
                    | _ -> failwith "Expected the refresh to complete."

                let! afterRefreshMessages = collectMessages afterRefreshCmd

                Vitest.expect(stateAfterPathChange.ArcSessionId).not.toBe (7)
                Vitest.expect(stateAfterRenameCompletion.CurrentArcPath).toEqual (Some "C:/work/Renamed ARC")
                Vitest.expect(stateAfterRenameCompletion.ErrorNotice).toEqual (None)
                Vitest.expect(retryMessages.Length).toBe (1)

                Vitest
                    .expect(
                        retryMessages
                        |> Array.exists (
                            function
                            | RefreshRequested -> true
                            | _ -> false
                        )
                    )
                    .toBe (true)

                Vitest.expect(stateAfterRenameCompletion.PendingPublishAfterRefresh).toBe (true)
                Vitest.expect(refreshedState.PendingPublishAfterRefresh).toBe (false)

                Vitest
                    .expect(afterRefreshMessages)
                    .toEqual (
                        [|
                            WriteRequested(Push GitUpdateAcceptance.RequirePreview)
                        |]
                    )
            }
        )

        Vitest.test (
            "WriteCompleted ignores late results from the previous ARC",
            fun () -> promise {
                let oldSessionState = {
                    GitState.Empty with
                        CurrentArcPath = Some "C:/arc-a"
                        WorkspaceVersion = Some "v1"
                }

                let stateAfterRequest, _ =
                    update
                        defaultDependencies
                        ignore
                        (WriteRequested(Push GitUpdateAcceptance.RequirePreview))
                        oldSessionState

                let switchedState, switchCmd =
                    update defaultDependencies ignore (ArcPathChanged(Some "C:/arc-b")) stateAfterRequest

                let! _ = collectMessages switchCmd

                let staleRefresh = {
                    Session = Ok sessionInfo
                    Status = Ok(statusForBranch "feature/old-arc")
                    Refs = Ok [| localBranch "feature/old-arc" true true |]
                    LfsSettings = Ok(lfsSettings 13 true)
                    OriginRemoteRepositoryWebUrl = None
                }

                let nextState, finishCmd =
                    update
                        defaultDependencies
                        ignore
                        (WriteCompleted(
                            oldSessionState.ArcSessionId,
                            stateAfterRequest.WriteRequestId,
                            Push GitUpdateAcceptance.RequirePreview,
                            Ok(
                                Completed(
                                    UnitSuccess {
                                        Refresh = staleRefresh
                                        PageChange = GitPageChange.NoChange
                                        SelectedChangePath = None
                                        Warning = None
                                        Partial = None
                                        Published = None
                                    }
                                )
                            )
                        ))
                        switchedState

                let! _ = collectMessages finishCmd

                Vitest
                    .expect(nextState)
                    .toEqual (
                        {
                            GitState.Empty with
                                CurrentArcPath = Some "C:/arc-b"
                                ArcSessionId = 1
                                RefreshRequestId = 1
                                PageLoadRequestId = 1
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
                        (WriteInstallPromptAnswered(
                            state.ArcSessionId,
                            Push GitUpdateAcceptance.RequirePreview,
                            "git-lfs-configuration",
                            false
                        ))
                        state

                let! _ = collectMessages cmd

                Vitest.expect(nextState.BusyOperation).toEqual (None)
                Vitest.expect(nextState.CurrentProgress).toEqual (None)

                Vitest
                    .expect(nextState.ErrorNotice)
                    .toEqual (Some "The version control dependency 'git-lfs-configuration' is required to continue.")

                Vitest.expect(currentRunStatus nextState).toEqual (None)
            }
        )

        Vitest.test (
            "WriteInstallCompleted clears progress when the installer reports failure",
            fun () -> promise {
                let state = {
                    GitState.Empty with
                        CurrentArcPath = Some "C:/arc-a"
                        BusyOperation = Some(GitBusyOperation.InstallingDependency "git-lfs-configuration")
                        CurrentProgress = Some(sidebarProgress "installing" 75.)
                        InstallRetryState = GitInstallRetryState.InstallingForRetry GitBusyOperation.PushingToRemote
                }

                let failure =
                    makeFailure ProviderError "dependency_install_failed" "Git LFS installation failed." None [||]

                let nextState, cmd =
                    update
                        defaultDependencies
                        ignore
                        (WriteInstallCompleted(
                            state.ArcSessionId,
                            Push GitUpdateAcceptance.RequirePreview,
                            Ok(OperationResultDto.Failed failure)
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
            "A stale-version completion of a superseded write is ignored",
            fun () -> promise {
                let model = {
                    runningState with
                        WriteRequestId = 2
                        BusyOperation = Some GitBusyOperation.DiscardingSelectedChanges
                        ErrorNotice = Some "keep this notice"
                }

                let nextState, cmd =
                    update
                        defaultDependencies
                        ignore
                        (WriteCompleted(
                            model.ArcSessionId,
                            model.WriteRequestId - 1,
                            DiscardSelection [| "a.txt" |],
                            Ok(StaleWorkspaceVersion("old", false))
                        ))
                        model

                let! messages = collectMessages cmd

                Vitest.expect(nextState.BusyOperation).toEqual (model.BusyOperation)
                Vitest.expect(nextState.ErrorNotice).toEqual (model.ErrorNotice)
                Vitest.expect(messages).toEqual ([||])
            }
        )

        Vitest.test (
            "WriteCompleted ignores stale non-clone writes without follow-up callback work",
            fun () -> promise {
                let state = {
                    GitState.Empty with
                        CurrentArcPath = Some "C:/arc-b"
                        ArcSessionId = 2
                        WriteRequestId = 1
                }

                let nextState, cmd =
                    update defaultDependencies ignore (WriteCompleted(1, 1, Fetch, Error "stale fetch failed")) state

                let! messages = collectMessages cmd

                Vitest.expect(nextState).toEqual (state)
                Vitest.expect(messages).toEqual ([||])
            }
        )

        Vitest.test (
            "Primary save commits locally and synchronizes once",
            fun () -> promise {
                let mutable revisionCalls = 0
                let synchronizeRequests = ResizeArray<SynchronizeRequestDto>()
                let sync = cleanStatus.Synchronization.Value

                let deps = {
                    defaultDependencies with
                        createRevision =
                            fun _ ->
                                revisionCalls <- revisionCalls + 1
                                promise { return Ok(succeeded "revision-1") }
                        synchronize =
                            fun request ->
                                synchronizeRequests.Add request
                                promise { return Ok(succeededWithPublication PublicationStateDto.Published sync) }
                        getStatus = fun _ -> promise { return Ok(succeeded cleanStatus) }
                        listRefs = fun _ -> promise { return Ok(succeeded refs) }
                        getStoragePolicySettings = fun _ -> promise { return Ok(succeeded (lfsSettings 5 true)) }
                }

                let state = {
                    runningState with
                        ChangedFiles = [| changedFile "README.md" "M" " " false |]
                }

                let stateAfterRequest, requestCmd =
                    update deps ignore (PrimarySaveAllRequested "Add polish") state

                let! requestMessages = collectMessages requestCmd

                let stateAfterWrite, finishCmd =
                    match requestMessages with
                    | [| WriteRequested(PrimarySave _) |] -> update deps ignore requestMessages[0] stateAfterRequest
                    | _ -> failwith "Expected the primary save request to enqueue a write request."

                let! completionMessages = collectWriteMessages finishCmd

                let finalState, finalCmd =
                    match completionMessages with
                    | [| WriteCompleted(_, _, PrimarySave _, Ok(Completed _)) |] ->
                        update deps ignore completionMessages[0] stateAfterWrite
                    | _ -> failwith "Expected the primary save flow to complete successfully."

                let! _ = collectMessages finalCmd

                Vitest.expect(revisionCalls).toBe (1)
                Vitest.expect(synchronizeRequests.Count).toBe (1)
                Vitest.expect(synchronizeRequests[0].ExpectedWorkspaceVersion).toBe ("v1")
                Vitest.expect(synchronizeRequests[0].ExpectedTargetRevision).toEqual (None)
                Vitest.expect(synchronizeRequests[0].AcceptUpdateRisks).toBe (false)
                Vitest.expect(synchronizeRequests[0].PublishLocalRevisions).toBe (true)
                Vitest.expect(finalState.Status.IsClean).toBe (true)
                Vitest.expect(finalState.ErrorNotice).toEqual (None)
                Vitest.expect(finalState.WarningNotice).toEqual (None)
            }
        )

        Vitest.test (
            "A primary save exposes the cancellable synchronize phase",
            fun () -> promise {
                let sync = cleanStatus.Synchronization.Value
                let mutable releaseSynchronize = None
                let mutable synchronizeRequest = None

                let deps = {
                    defaultDependencies with
                        createRevision = fun _ -> promise { return Ok(succeeded "revision-1") }
                        synchronize =
                            fun request ->
                                synchronizeRequest <- Some request
                                Promise.create (fun resolve _reject -> releaseSynchronize <- Some resolve)
                        getStatus = fun _ -> promise { return Ok(succeeded cleanStatus) }
                        listRefs = fun _ -> promise { return Ok(succeeded refs) }
                        getStoragePolicySettings = fun _ -> promise { return Ok(succeeded (lfsSettings 5 true)) }
                }

                let state = {
                    runningState with
                        ChangedFiles = [| changedFile "README.md" "M" " " false |]
                }

                let stateAfterRequest, requestCmd =
                    update deps ignore (PrimarySaveAllRequested "save") state

                let! requestMessages = collectMessages requestCmd

                let stateAfterWrite, writeCmd =
                    update deps ignore requestMessages[0] stateAfterRequest

                let pendingMessages = ResizeArray<Msg>()
                writeCmd |> List.iter (fun effect -> effect pendingMessages.Add)
                do! Promise.sleep 0

                let phaseState, _ = update deps ignore pendingMessages[0] stateAfterWrite

                Vitest
                    .expect(pendingMessages[0])
                    .toEqual (
                        WritePhaseChanged(
                            stateAfterWrite.ArcSessionId,
                            stateAfterWrite.WriteRequestId,
                            GitBusyOperation.PushingToRemote
                        )
                    )

                Vitest.expect(phaseState.BusyOperation).toEqual (Some GitBusyOperation.PushingToRemote)
                Vitest.expect(phaseState.CurrentOperation).toEqual (stateAfterWrite.CurrentOperation)
                Vitest.expect(synchronizeRequest.IsSome).toBe (true)

                let synchronizeKey = {
                    SessionId = "synchronize-session"
                    OperationId = synchronizeRequest.Value.OperationId
                }

                let startedState, _ =
                    update deps ignore (OperationStarted synchronizeKey) phaseState

                Vitest.expect(startedState.CurrentOperation).toEqual (Some synchronizeKey)

                releaseSynchronize.Value(Ok(succeededWithPublication PublicationStateDto.Published sync))
                do! Promise.sleep 0
                do! Promise.sleep 0

                Vitest
                    .expect(pendingMessages[1])
                    .toEqual (
                        WritePhaseChanged(
                            stateAfterWrite.ArcSessionId,
                            stateAfterWrite.WriteRequestId,
                            GitBusyOperation.Refreshing
                        )
                    )

                let mutable finalState = startedState

                for index in 1 .. pendingMessages.Count - 1 do
                    let nextState, _ = update deps ignore pendingMessages[index] finalState
                    finalState <- nextState

                Vitest.expect(finalState.BusyOperation).toEqual (None)
                Vitest.expect(finalState.ErrorNotice).toEqual (None)
                Vitest.expect(finalState.WarningNotice).toEqual (None)
            }
        )

        Vitest.test (
            "A phase update from an older write is ignored",
            fun () -> promise {
                let state = {
                    runningState with
                        WriteRequestId = 4
                        BusyOperation = Some GitBusyOperation.CommittingAllChanges
                        BusyNotice = Some "Committing all changes"
                }

                let nextState, cmd =
                    update
                        defaultDependencies
                        ignore
                        (WritePhaseChanged(state.ArcSessionId, 3, GitBusyOperation.PushingToRemote))
                        state

                let! messages = collectMessages cmd

                Vitest.expect(nextState).toEqual (state)
                Vitest.expect(messages).toEqual ([||])
            }
        )

        Vitest.test (
            "Primary save synchronizes once when no upstream is configured yet",
            fun () -> promise {
                let synchronizeRequests = ResizeArray<SynchronizeRequestDto>()

                let noUpstreamStatus = {
                    statusForBranch "feature/new-branch" with
                        Synchronization =
                            Some {
                                cleanStatus.Synchronization.Value with
                                    TargetRef = None
                                    TargetRevision = None
                                    Relationship = RevisionRelationshipDto.NoTarget
                            }
                }

                let sync = cleanStatus.Synchronization.Value

                let deps = {
                    defaultDependencies with
                        createRevision = fun _ -> promise { return Ok(succeeded "revision-1") }
                        synchronize =
                            fun request ->
                                synchronizeRequests.Add request

                                promise { return Ok(succeededWithPublication PublicationStateDto.Published sync) }
                        getStatus = fun _ -> promise { return Ok(succeeded noUpstreamStatus) }
                        listRefs =
                            fun _ -> promise { return Ok(succeeded [| localBranch "feature/new-branch" true false |]) }
                        getStoragePolicySettings = fun _ -> promise { return Ok(succeeded (lfsSettings 5 true)) }
                }

                let state = {
                    runningState with
                        ChangedFiles = [| changedFile "README.md" "M" " " false |]
                }

                let stateAfterRequest, requestCmd =
                    update deps ignore (PrimarySaveAllRequested "Publish branch") state

                let! requestMessages = collectMessages requestCmd

                let stateAfterWrite, finishCmd =
                    match requestMessages with
                    | [| WriteRequested(PrimarySave _) |] -> update deps ignore requestMessages[0] stateAfterRequest
                    | _ -> failwith "Expected the primary save request to enqueue a write request."

                let! completionMessages = collectWriteMessages finishCmd

                let finalState, finalCmd =
                    match completionMessages with
                    | [| WriteCompleted(_, _, PrimarySave _, Ok(Completed _)) |] ->
                        update deps ignore completionMessages[0] stateAfterWrite
                    | _ -> failwith "Expected the primary save flow to publish the branch and finish."

                let! _ = collectMessages finalCmd

                Vitest.expect(synchronizeRequests.Count).toBe (1)
                Vitest.expect(synchronizeRequests[0].PublishLocalRevisions).toBe (true)
                Vitest.expect(finalState.ErrorNotice).toEqual (None)
                Vitest.expect(finalState.WarningNotice).toEqual (None)
            }
        )

        Vitest.test (
            "A partial save offers materialization recovery and resumes the pending publish",
            fun () -> promise {
                let recovery =
                    makeFailure
                        Canceled
                        VersionControlCodes.OperationCanceled
                        "Large-file hydration was canceled."
                        (Some {
                            Code = VersionControlCodes.Recovery.RetryMaterialization
                            Instructions = None
                        })
                        [||]

                let partialOutcome =
                    operationWithPublication PublicationStateDto.LocalOnly cleanStatus.Synchronization.Value

                let mutable materializeCalls = 0

                let deps = {
                    defaultDependencies with
                        createRevision = fun _ -> promise { return Ok(succeeded "revision-1") }
                        synchronize =
                            fun _ -> promise {
                                return Ok(OperationResultDto.PartiallySucceeded(partialOutcome, recovery))
                            }
                        getStatus = fun _ -> promise { return Ok(succeeded cleanStatus) }
                        listRefs = fun _ -> promise { return Ok(succeeded refs) }
                        getStoragePolicySettings = fun _ -> promise { return Ok(succeeded (lfsSettings 5 true)) }
                        listObjects =
                            fun _ -> promise {
                                return
                                    Ok(
                                        succeeded [|
                                            {
                                                Path = "large.bin"
                                                IsMaterialized = false
                                                IsLocallyAvailable = false
                                                SizeBytes = Some 100.
                                                ObjectId = Some "object-1"
                                            }
                                        |]
                                    )
                            }
                        materializeObject =
                            fun _ ->
                                materializeCalls <- materializeCalls + 1
                                promise { return Ok(succeeded ()) }
                }

                let saveState = {
                    runningState with
                        ChangedFiles = [| changedFile "README.md" "M" " " false |]
                }

                let stateAfterRequest, requestCmd =
                    update deps ignore (PrimarySaveAllRequested "Save") saveState

                let! requestMessages = collectMessages requestCmd

                let stateAfterWrite, writeCmd =
                    match requestMessages with
                    | [| WriteRequested(PrimarySave _) |] -> update deps ignore requestMessages[0] stateAfterRequest
                    | _ -> failwith "Expected the primary save request."

                let! completionMessages = collectWriteMessages writeCmd

                let recoveryState, completionCmd =
                    match completionMessages with
                    | [| WriteCompleted(_, _, PrimarySave _, Ok(Completed _)) |] ->
                        update deps ignore completionMessages[0] stateAfterWrite
                    | _ -> failwith "Expected the partial save completion."

                let! _ = collectMessages completionCmd

                Vitest
                    .expect(recoveryState.WarningNotice)
                    .toEqual (Some "Changes were saved locally. Online sync is still pending.")

                Vitest.expect(recoveryState.PendingRecovery.IsSome).toBe (true)
                Vitest.expect(recoveryState.PendingPostMergePush).toBe (true)

                let confirmedState, confirmCmd =
                    update deps ignore ConfirmPendingRemoteActionRequested recoveryState

                let! confirmMessages = collectMessages confirmCmd

                let retryState, retryRequestCmd =
                    match confirmMessages with
                    | [| RetryMaterializationRequested |] -> update deps ignore confirmMessages[0] confirmedState
                    | _ -> failwith "Expected materialization recovery to be confirmed."

                let! retryRequestMessages = collectMessages retryRequestCmd

                let retryRunningState, retryCmd =
                    match retryRequestMessages with
                    | [| WriteRequested RetryMaterialization |] -> update deps ignore retryRequestMessages[0] retryState
                    | _ -> failwith "Expected materialization retry to start."

                let! retryCompletionMessages = collectMessages retryCmd

                let finalState, finishCmd =
                    match retryCompletionMessages with
                    | [| WriteCompleted(_, _, RetryMaterialization, Ok(Completed _)) |] ->
                        update deps ignore retryCompletionMessages[0] retryRunningState
                    | _ -> failwith "Expected materialization retry to complete."

                let! finishMessages = collectMessages finishCmd

                Vitest.expect(materializeCalls).toBe (1)
                Vitest.expect(finalState.PendingPostMergePush).toBe (false)

                Vitest
                    .expect(
                        finishMessages
                        |> Array.exists (
                            function
                            | WriteRequested(Push GitUpdateAcceptance.RequirePreview) -> true
                            | _ -> false
                        )
                    )
                    .toBe (true)
            }
        )

        Vitest.test (
            "Dismissing materialization recovery resumes the pending publish",
            fun () -> promise {
                let recovery = GitPendingRecovery.RetryMaterialization "Retry materialization."

                let state = {
                    runningState with
                        PendingRecovery = Some recovery
                        PendingConfirmation =
                            Some {
                                Title = "Retry materialization"
                                Message = "Retry materialization."
                                ConfirmLabel = "Retry"
                                CancelLabel = "Dismiss"
                            }
                        PendingRemoteAction = GitPendingRemoteAction.Recover
                        PendingPostMergePush = true
                }

                let nextState, cmd =
                    update defaultDependencies ignore DismissRecoveryRequested state

                let! messages = collectMessages cmd

                Vitest.expect(nextState.PendingRecovery).toEqual (None)
                Vitest.expect(nextState.PendingPostMergePush).toBe (false)

                Vitest
                    .expect(messages)
                    .toEqual (
                        [|
                            WriteRequested(Push GitUpdateAcceptance.RequirePreview)
                        |]
                    )
            }
        )

        Vitest.test (
            "Local-only save never requests synchronization",
            fun () -> promise {
                let mutable synchronizeCalled = false

                let localOnly = {
                    sessionInfo with
                        Services = {
                            sessionInfo.Services with
                                Synchronization = false
                        }
                }

                let deps = {
                    defaultDependencies with
                        createRevision = fun _ -> promise { return Ok(succeeded "revision-1") }
                        getSessionInfo = fun _ -> promise { return Ok(succeeded localOnly) }
                        synchronize =
                            fun _ ->
                                synchronizeCalled <- true
                                unexpectedPromise "unexpected synchronize"
                        getStatus = fun _ -> promise { return Ok(succeeded cleanStatus) }
                        listRefs = fun _ -> promise { return Ok(succeeded refs) }
                        getStoragePolicySettings = fun _ -> promise { return Ok(succeeded (lfsSettings 5 true)) }
                }

                let state = {
                    runningState with
                        ChangedFiles = [| changedFile "README.md" "M" " " false |]
                }

                let stateAfterRequest, requestCmd =
                    update deps ignore (CommitAllRequested "Local only") state

                let! requestMessages = collectMessages requestCmd

                let stateAfterWrite, finishCmd =
                    match requestMessages with
                    | [| WriteRequested(CommitAll _) |] -> update deps ignore requestMessages[0] stateAfterRequest
                    | _ -> failwith "Expected the local commit request."

                let! completionMessages = collectMessages finishCmd

                let finalState, finalCmd =
                    match completionMessages with
                    | [| WriteCompleted(_, _, CommitAll _, Ok(Completed _)) |] ->
                        update deps ignore completionMessages[0] stateAfterWrite
                    | _ -> failwith "Expected the local commit to complete."

                let! _ = collectMessages finalCmd

                Vitest.expect(synchronizeCalled).toBe (false)
                Vitest.expect(finalState.ErrorNotice).toEqual (None)
            }
        )

        Vitest.test (
            "Primary save keeps a warning and pending confirmation when synchronize needs merge resolution",
            fun () -> promise {
                let sync = cleanStatus.Synchronization.Value

                let failure = {
                    makeFailure
                        Conflict
                        VersionControlCodes.UpdateWouldCreateConflictSession
                        "Updating from the target needs conflict resolution."
                        (Some {
                            Code = VersionControlCodes.Recovery.AcceptUpdateRisks
                            Instructions = None
                        })
                        [| "isa.study.xlsx" |] with
                        RevisionEvidence = [|
                            {
                                Label = "observed_target"
                                Revision = "target-1"
                            }
                        |]
                }

                let synchronizeRequests = ResizeArray<SynchronizeRequestDto>()

                let deps = {
                    defaultDependencies with
                        createRevision = fun _ -> promise { return Ok(succeeded "revision-1") }
                        synchronize =
                            fun request ->
                                synchronizeRequests.Add request

                                if synchronizeRequests.Count = 1 then
                                    promise { return Ok(OperationResultDto.Failed failure) }
                                else
                                    promise { return Ok(succeeded sync) }
                        getStatus = fun _ -> promise { return Ok(succeeded cleanStatus) }
                        listRefs = fun _ -> promise { return Ok(succeeded refs) }
                        getStoragePolicySettings = fun _ -> promise { return Ok(succeeded (lfsSettings 5 true)) }
                }

                let state = {
                    runningState with
                        ChangedFiles = [| changedFile "README.md" "M" " " false |]
                }

                let stateAfterRequest, requestCmd =
                    update deps ignore (PrimarySaveAllRequested "Add polish") state

                let! requestMessages = collectMessages requestCmd

                let stateAfterWrite, finishCmd =
                    match requestMessages with
                    | [| WriteRequested(PrimarySave _) |] -> update deps ignore requestMessages[0] stateAfterRequest
                    | _ -> failwith "Expected the primary save request."

                let! completionMessages = collectWriteMessages finishCmd

                let nextState, completionCmd =
                    match completionMessages with
                    | [| WriteCompleted(_,
                                        _,
                                        PrimarySave _,
                                        Ok(CompletedWithPendingRemoteConfirmation(_, _, pendingAction))) |] ->
                        Vitest
                            .expect(pendingAction)
                            .toEqual (
                                GitPendingRemoteAction.PublishAfterUpdate(
                                    GitUpdateAcceptance.Accepted(
                                        "target-1",
                                        synchronizeRequests[0].ExpectedWorkspaceVersion
                                    )
                                )
                            )

                        update deps ignore completionMessages[0] stateAfterWrite
                    | _ -> failwith "Expected the primary save flow to request remote confirmation."

                let! _ = collectMessages completionCmd

                let stateAfterCancel, cancelCmd =
                    update deps ignore CancelPendingRemoteActionRequested nextState

                let! cancelMessages = collectMessages cancelCmd

                Vitest.expect(nextState.PendingConfirmation.IsSome).toBe (true)

                Vitest
                    .expect(nextState.PendingRemoteAction)
                    .toEqual (
                        GitPendingRemoteAction.PublishAfterUpdate(
                            GitUpdateAcceptance.Accepted("target-1", synchronizeRequests[0].ExpectedWorkspaceVersion)
                        )
                    )

                Vitest.expect(nextState.PendingConfirmation.Value.Title).toBe ("Merge resolution required")
                Vitest.expect(nextState.PendingConfirmation.Value.Message).toContain ("isa.study.xlsx")
                Vitest.expect(nextState.WarningNotice |> Option.defaultValue "").toContain ("saved locally")
                Vitest.expect(cancelMessages).toEqual ([||])
                Vitest.expect(stateAfterCancel.PendingConfirmation).toEqual (None)
                Vitest.expect(stateAfterCancel.WarningNotice |> Option.defaultValue "").toContain ("saved locally")

                let confirming, confirmCmd =
                    update deps ignore ConfirmPendingRemoteActionRequested nextState

                let! confirmMessages = collectMessages confirmCmd

                let duringWrite, secondWriteCmd =
                    match confirmMessages with
                    | [| WriteRequested(Push(GitUpdateAcceptance.Accepted(target, version))) |] ->
                        Vitest.expect(target).toBe ("target-1")
                        Vitest.expect(version).toBe (synchronizeRequests[0].ExpectedWorkspaceVersion)
                        update deps ignore confirmMessages[0] confirming
                    | _ -> failwith "Expected the accepted publish request."

                Vitest.expect(duringWrite.PendingPostMergePush).toBe (true)
                let! _ = collectMessages secondWriteCmd
                Vitest.expect(synchronizeRequests.Count).toBe (2)
                Vitest.expect(synchronizeRequests[1].AcceptUpdateRisks).toBe (true)
                Vitest.expect(synchronizeRequests[1].ExpectedTargetRevision).toEqual (Some "target-1")

                Vitest
                    .expect(synchronizeRequests[1].ExpectedWorkspaceVersion)
                    .toBe (synchronizeRequests[0].ExpectedWorkspaceVersion)

                Vitest.expect(synchronizeRequests[1].PublishLocalRevisions).toBe (true)
            }
        )

        Vitest.test (
            "The save's acceptance dialog is built over the refreshed snapshot",
            fun () -> promise {
                let failure = {
                    makeFailure
                        Conflict
                        VersionControlCodes.UpdateWouldCreateConflictSession
                        "Updating from the target needs conflict resolution."
                        (Some {
                            Code = VersionControlCodes.Recovery.AcceptUpdateRisks
                            Instructions = None
                        })
                        [| "isa.study.xlsx" |] with
                        RevisionEvidence = [|
                            {
                                Label = "observed_target"
                                Revision = "target-1"
                            }
                        |]
                }

                let runCase (confirmationStatus: Result<OperationResultDto<WorkspaceStatusDto>, string>) = promise {
                    let mutable getStatusCalls = 0
                    let postCommitStatus = statusForBranch "post-commit"

                    let deps = {
                        defaultDependencies with
                            createRevision = fun _ -> promise { return Ok(succeeded "revision-1") }
                            synchronize = fun _ -> promise { return Ok(OperationResultDto.Failed failure) }
                            getStatus =
                                fun _ ->
                                    getStatusCalls <- getStatusCalls + 1

                                    promise {
                                        if getStatusCalls = 1 then
                                            return Ok(succeeded postCommitStatus)
                                        else
                                            return confirmationStatus
                                    }
                            listRefs = fun _ -> promise { return Ok(succeeded refs) }
                            getStoragePolicySettings = fun _ -> promise { return Ok(succeeded (lfsSettings 5 true)) }
                    }

                    let state = {
                        runningState with
                            ChangedFiles = [| changedFile "README.md" "M" " " false |]
                    }

                    let stateAfterRequest, requestCmd =
                        update deps ignore (PrimarySaveAllRequested "Add polish") state

                    let! requestMessages = collectMessages requestCmd

                    let stateAfterWrite, finishCmd =
                        match requestMessages with
                        | [| WriteRequested(PrimarySave _) |] -> update deps ignore requestMessages[0] stateAfterRequest
                        | _ -> failwith "Expected the primary save request."

                    let! completionMessages = collectWriteMessages finishCmd

                    let finalState, completionCmd =
                        match completionMessages with
                        | [| WriteCompleted(_, _, PrimarySave _, Ok(CompletedWithPendingRemoteConfirmation(_, _, _))) |] ->
                            update deps ignore completionMessages[0] stateAfterWrite
                        | _ -> failwith "Expected the primary save flow to request remote confirmation."

                    let! _ = collectMessages completionCmd

                    Vitest.expect(getStatusCalls).toBe (2)
                    return finalState
                }

                let! refreshedState = runCase (Ok(succeeded (statusForBranch "refreshed")))

                Vitest.expect(refreshedState.Status.CurrentBranch).toEqual (Some "refreshed")

                let! failedRefreshState = runCase (Error "confirmation refresh failed")

                Vitest.expect(failedRefreshState.Status.CurrentBranch).toEqual (Some "post-commit")
                Vitest.expect(failedRefreshState.PendingConfirmation.IsSome).toBe (true)
                Vitest.expect(failedRefreshState.ErrorNotice).toEqual (None)
            }
        )

        Vitest.test (
            "A recovery attached to a pending confirmation becomes a warning and is not parked",
            fun () -> promise {
                let partial =
                    makeFailure
                        Canceled
                        VersionControlCodes.OperationCanceled
                        "Large-file hydration was canceled."
                        (Some {
                            Code = VersionControlCodes.Recovery.RetryMaterialization
                            Instructions = None
                        })
                        [||]

                let prepared = {
                    BusyOperation = GitBusyOperation.CommittingAllChanges
                    NormalizedMessage = "Save"
                    PathsToCommit = [| "README.md" |]
                }

                let dialog = {
                    Title = "Merge confirmation"
                    Message = "Confirm the merge."
                    ConfirmLabel = "Confirm"
                    CancelLabel = "Cancel"
                }

                let state = {
                    runningState with
                        BusyOperation = Some prepared.BusyOperation
                        WriteRequestId = 1
                }

                let nextState, cmd =
                    update
                        defaultDependencies
                        ignore
                        (WriteCompleted(
                            state.ArcSessionId,
                            state.WriteRequestId,
                            PrimarySave prepared,
                            Ok(
                                CompletedWithPendingRemoteConfirmation(
                                    UnitSuccess {
                                        Refresh = refreshed cleanStatus
                                        PageChange = GitPageChange.NoChange
                                        SelectedChangePath = None
                                        Warning = None
                                        Partial = Some partial
                                        Published = None
                                    },
                                    dialog,
                                    GitPendingRemoteAction.FinalizeMerge
                                )
                            )
                        ))
                        state

                let! _ = collectMessages cmd

                Vitest.expect(nextState.PendingRecovery).toEqual (None)
                Vitest.expect(nextState.PendingConfirmation).toEqual (Some dialog)
                Vitest.expect(nextState.WarningNotice).toEqual (Some(failureMessage partial))
            }
        )

        Vitest.test (
            "Primary save preserves the local commit warning when synchronize fails after commit",
            fun () -> promise {
                let deps = {
                    defaultDependencies with
                        createRevision = fun _ -> promise { return Ok(succeeded "revision-1") }
                        synchronize = fun _ -> promise { return Error "Network unavailable during synchronize." }
                        getStatus = fun _ -> promise { return Ok(succeeded cleanStatus) }
                        listRefs = fun _ -> promise { return Ok(succeeded refs) }
                        getStoragePolicySettings = fun _ -> promise { return Ok(succeeded (lfsSettings 5 true)) }
                }

                let state = {
                    runningState with
                        ChangedFiles = [| changedFile "README.md" "M" " " false |]
                }

                let stateAfterRequest, requestCmd =
                    update deps ignore (PrimarySaveAllRequested "Save locally first") state

                let! requestMessages = collectMessages requestCmd

                let stateAfterWrite, finishCmd =
                    match requestMessages with
                    | [| WriteRequested(PrimarySave _) |] -> update deps ignore requestMessages[0] stateAfterRequest
                    | _ -> failwith "Expected the primary save request."

                let! completionMessages = collectWriteMessages finishCmd

                let nextState, completionCmd =
                    match completionMessages with
                    | [| WriteCompleted(_, _, PrimarySave _, Ok(CompletedWithPendingRemoteFailure(_, message))) |] ->
                        Vitest.expect(message).toBe ("Network unavailable during synchronize.")
                        update deps ignore completionMessages[0] stateAfterWrite
                    | _ -> failwith "Expected a saved-locally outcome after preflight failed."

                let! finishMessages = collectMessages completionCmd

                Vitest.expect(finishMessages).toEqual ([||])
                Vitest.expect(nextState.WarningNotice |> Option.defaultValue "").toContain ("saved locally")
                Vitest.expect(nextState.ErrorNotice).toEqual (Some "Network unavailable during synchronize.")
                Vitest.expect(nextState.Status.IsClean).toBe (true)
            }
        )

        Vitest.test (
            "A primary save whose synchronize is canceled with a refresh recovery keeps the saved-locally notice",
            fun () -> promise {
                let canceled =
                    makeFailure
                        Canceled
                        VersionControlCodes.OperationCanceled
                        "Publish was canceled."
                        (Some {
                            Code = VersionControlCodes.Recovery.RefreshWorkspace
                            Instructions = None
                        })
                        [||]

                let deps = {
                    defaultDependencies with
                        createRevision = fun _ -> promise { return Ok(succeeded "revision-1") }
                        synchronize =
                            fun request ->
                                if request.PublishLocalRevisions then
                                    promise { return Ok(OperationResultDto.Failed canceled) }
                                else
                                    unexpectedPromise "unexpected pull"
                        getStatus = fun _ -> promise { return Ok(succeeded cleanStatus) }
                        listRefs = fun _ -> promise { return Ok(succeeded refs) }
                        getStoragePolicySettings = fun _ -> promise { return Ok(succeeded (lfsSettings 5 true)) }
                }

                let state = {
                    runningState with
                        ChangedFiles = [| changedFile "README.md" "M" " " false |]
                }

                let stateAfterRequest, requestCmd =
                    update deps ignore (PrimarySaveAllRequested "Save locally first") state

                let! requestMessages = collectMessages requestCmd

                let stateAfterWrite, finishCmd =
                    match requestMessages with
                    | [| WriteRequested(PrimarySave _) |] -> update deps ignore requestMessages[0] stateAfterRequest
                    | _ -> failwith "Expected the primary save request."

                let! completionMessages = collectWriteMessages finishCmd

                let nextState, completionCmd =
                    match completionMessages with
                    | [| WriteCompleted(_,
                                        _,
                                        PrimarySave _,
                                        Ok(CompletedWithPendingRemoteFailure(UnitSuccess success, message))) |] when
                        success.Warning.IsSome && success.Partial.IsNone
                        ->
                        let warning = success.Warning.Value
                        Vitest.expect(warning).toBe ("Changes were saved locally. Online sync is still pending.")
                        Vitest.expect(message).toBe (failureMessage canceled)
                        update deps ignore completionMessages[0] stateAfterWrite
                    | _ -> failwith "Expected a saved-locally outcome after publish was canceled."

                let! finishMessages = collectMessages completionCmd

                Vitest.expect(finishMessages).toEqual ([||])

                Vitest
                    .expect(nextState.WarningNotice)
                    .toEqual (Some "Changes were saved locally. Online sync is still pending.")
            }
        )

        Vitest.test (
            "UpdateFromOnlineRequested opens a confirmation dialog when synchronize predicts merge resolution",
            fun () -> promise {
                let failure = {
                    makeFailure
                        Conflict
                        VersionControlCodes.UpdateWouldCreateConflictSession
                        "Updating from the target needs conflict resolution."
                        (Some {
                            Code = VersionControlCodes.Recovery.AcceptUpdateRisks
                            Instructions = None
                        })
                        [| "isa.study.xlsx" |] with
                        RevisionEvidence = [|
                            {
                                Label = "observed_target"
                                Revision = "target-1"
                            }
                        |]
                }

                let synchronizeRequests = ResizeArray<SynchronizeRequestDto>()

                let deps = {
                    defaultDependencies with
                        synchronize =
                            fun request ->
                                synchronizeRequests.Add request

                                if synchronizeRequests.Count = 1 then
                                    promise { return Ok(OperationResultDto.Failed failure) }
                                else
                                    promise { return Ok(succeeded cleanStatus.Synchronization.Value) }
                        getStatus = fun _ -> promise { return Ok(succeeded cleanStatus) }
                        listRefs = fun _ -> promise { return Ok(succeeded refs) }
                        getStoragePolicySettings = fun _ -> promise { return Ok(succeeded (lfsSettings 5 true)) }
                }

                let state = {
                    runningState with
                        BusyOperation = None
                }

                let stateAfterRequest, requestCmd =
                    update deps ignore UpdateFromOnlineRequested state

                let! writeMessages = collectMessages requestCmd

                let writeState, writeCmd =
                    match writeMessages with
                    | [| WriteRequested(Pull GitUpdateAcceptance.RequirePreview) |] ->
                        update deps ignore writeMessages[0] stateAfterRequest
                    | _ -> failwith "Expected the update write request."

                let! completionMessages = collectMessages writeCmd

                let nextState, finishCmd =
                    match completionMessages with
                    | [| WriteCompleted(_, _, Pull _, Ok(CompletedWithPendingRemoteConfirmation(_, dialog, action))) |] ->
                        Vitest.expect(dialog.Title).toBe ("Merge resolution required")

                        Vitest
                            .expect(action)
                            .toEqual (
                                GitPendingRemoteAction.UpdateFromOnline(GitUpdateAcceptance.Accepted("target-1", "v1"))
                            )

                        update deps ignore completionMessages[0] writeState
                    | _ -> failwith "Expected update confirmation to complete."

                let! messages = collectMessages finishCmd

                Vitest.expect(messages).toEqual ([||])
                Vitest.expect(nextState.PendingConfirmation.Value.Title).toBe ("Merge resolution required")
                Vitest.expect(nextState.PendingConfirmation.Value.Message).toContain ("isa.study.xlsx")

                Vitest
                    .expect(nextState.PendingRemoteAction)
                    .toEqual (GitPendingRemoteAction.UpdateFromOnline(GitUpdateAcceptance.Accepted("target-1", "v1")))

                let confirmed, confirmCmd =
                    update deps ignore ConfirmPendingRemoteActionRequested nextState

                let! confirmMessages = collectMessages confirmCmd

                let _, secondWriteCmd =
                    match confirmMessages with
                    | [| WriteRequested(Pull(GitUpdateAcceptance.Accepted(target, version))) |] ->
                        Vitest.expect(target).toBe ("target-1")
                        Vitest.expect(version).toBe ("v1")
                        update deps ignore confirmMessages[0] confirmed
                    | _ -> failwith "Expected the accepted pull request."

                let! _ = collectMessages secondWriteCmd
                Vitest.expect(synchronizeRequests.Count).toBe (2)
                Vitest.expect(synchronizeRequests[1].AcceptUpdateRisks).toBe (true)
                Vitest.expect(synchronizeRequests[1].ExpectedTargetRevision).toEqual (Some "target-1")
                Vitest.expect(synchronizeRequests[1].ExpectedWorkspaceVersion).toBe ("v1")
                Vitest.expect(synchronizeRequests[1].PublishLocalRevisions).toBe (false)
            }
        )

        Vitest.test (
            "A decision failure without an observed target is reported as an error",
            fun () -> promise {
                let failure =
                    makeFailure
                        Conflict
                        VersionControlCodes.UpdateWouldCreateConflictSession
                        "Updating from the target would overwrite local changes."
                        (Some {
                            Code = VersionControlCodes.Recovery.AcceptUpdateRisks
                            Instructions = None
                        })
                        [| "isa.study.xlsx" |]

                let reportedErrors = ResizeArray<GitErrorNotification>()

                let deps = {
                    defaultDependencies with
                        synchronize = fun _ -> promise { return Ok(OperationResultDto.Failed failure) }
                        reportError = reportedErrors.Add
                }

                let requested, requestCmd =
                    update deps ignore (WriteRequested(Pull GitUpdateAcceptance.RequirePreview)) runningState

                let! completionMessages = collectMessages requestCmd

                let finalState, finishCmd =
                    match completionMessages with
                    | [| WriteCompleted(_, _, Pull _, Error message) |] ->
                        Vitest.expect(message).toBe (failureMessage failure)
                        update deps ignore completionMessages[0] requested
                    | _ -> failwith "Expected the decision failure to be reported as an error."

                let! _ = collectMessages finishCmd
                Vitest.expect(finalState.ErrorNotice).toEqual (Some(failureMessage failure))
                Vitest.expect(finalState.PendingConfirmation).toEqual (None)
                Vitest.expect(finalState.PendingRemoteAction).toEqual (GitPendingRemoteAction.None)
                Vitest.expect(reportedErrors.Count).toBe (1)
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
            "A synchronize that cannot preview the update is reported as an error",
            fun () -> promise {
                let failure = {
                    makeFailure
                        ProviderError
                        VersionControlCodes.PreviewIndeterminate
                        "Git pull synchronization could not be classified safely."
                        None
                        [||] with
                        Retryable = true
                        RevisionEvidence = [|
                            {
                                Label = "observed_target"
                                Revision = "target-1"
                            }
                        |]
                }

                let reportedErrors = ResizeArray<GitErrorNotification>()

                let deps = {
                    defaultDependencies with
                        synchronize = fun _ -> promise { return Ok(OperationResultDto.Failed failure) }
                        reportError = reportedErrors.Add
                }

                let state = {
                    runningState with
                        BusyOperation = None
                }

                let stateAfterRequest, requestCmd =
                    update deps ignore UpdateFromOnlineRequested state

                let! writeMessages = collectMessages requestCmd

                let writeState, writeCmd =
                    match writeMessages with
                    | [| WriteRequested(Pull GitUpdateAcceptance.RequirePreview) |] ->
                        update deps ignore writeMessages[0] stateAfterRequest
                    | _ -> failwith "Expected the update write request."

                let! completionMessages = collectMessages writeCmd

                let nextState, finishCmd =
                    match completionMessages with
                    | [| WriteCompleted(_, _, Pull _, Error message) |] ->
                        Vitest.expect(message).toBe (failureMessage failure)
                        update deps ignore completionMessages[0] writeState
                    | _ -> failwith "Expected the indeterminate update to be reported as an error."

                let! messages = collectMessages finishCmd

                Vitest.expect(messages).toEqual ([||])
                Vitest.expect(nextState.ErrorNotice).toEqual (Some(failureMessage failure))
                Vitest.expect(nextState.PendingConfirmation).toEqual (None)
                Vitest.expect(nextState.PendingRemoteAction).toEqual (GitPendingRemoteAction.None)
                Vitest.expect(reportedErrors.Count).toBe (1)
            }
        )

        Vitest.test (
            "An update over local changes is reported with the paths and without a dialog",
            fun () -> promise {
                let failure = {
                    makeFailure
                        Conflict
                        VersionControlCodes.UpdateWouldOverwriteLocalChanges
                        "The library message is replaced by the renderer."
                        (Some {
                            Code = VersionControlCodes.Recovery.ResolveLocalChanges
                            Instructions = None
                        })
                        [| "assays/a/isa.assay.xlsx"; "runs/r/out.csv" |] with
                        RevisionEvidence = [|
                            {
                                Label = "observed_target"
                                Revision = "target-1"
                            }
                        |]
                }

                let synchronizeRequests = ResizeArray<SynchronizeRequestDto>()

                let deps = {
                    defaultDependencies with
                        synchronize =
                            fun request ->
                                synchronizeRequests.Add request
                                promise { return Ok(OperationResultDto.Failed failure) }
                }

                let requested, requestCmd =
                    update deps ignore (WriteRequested(Pull GitUpdateAcceptance.RequirePreview)) runningState

                let! completionMessages = collectMessages requestCmd

                let finalState, finishCmd =
                    match completionMessages with
                    | [| WriteCompleted(_, _, Pull _, Error _) |] -> update deps ignore completionMessages[0] requested
                    | _ -> failwith "Expected the local-change overwrite to be reported as an error."

                let! _ = collectMessages finishCmd
                let errorNotice = finalState.ErrorNotice |> Option.defaultValue ""

                Vitest.expect(errorNotice).toContain ("assays/a/isa.assay.xlsx")
                Vitest.expect(errorNotice).toContain ("runs/r/out.csv")
                Vitest.expect(errorNotice).toContain ("Save or discard")
                Vitest.expect(finalState.PendingConfirmation).toEqual (None)
                Vitest.expect(finalState.PendingRemoteAction).toEqual (GitPendingRemoteAction.None)
                Vitest.expect(synchronizeRequests.Count).toBe (1)
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
                        ActiveConflict = None
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
                                Finalized = true
                            }
                        ))
                        state

                let! messages = collectMessages cmd

                Vitest.expect(nextState.PendingPostMergePush).toBe (false)

                Vitest
                    .expect(messages)
                    .toEqual (
                        [|
                            WriteRequested(Push GitUpdateAcceptance.RequirePreview)
                        |]
                    )
            }
        )

        Vitest.test (
            "ArcPathChanged schedules a bare RefreshRequested when switching repositories",
            fun () -> promise {
                let nextState, cmd =
                    update defaultDependencies ignore (ArcPathChanged(Some "C:/arc-b")) GitState.Empty

                let! messages = collectMessages cmd

                Vitest.expect(nextState.CurrentArcPath).toEqual (Some "C:/arc-b")
                Vitest.expect(messages).toEqual ([| RefreshRequested |])
            }
        )

        Vitest.test (
            "Primary save provisions the remote project when the workspace has no target",
            fun () -> promise {
                let calls = ResizeArray<string>()
                let mutable boundLocation = None

                let noTargetStatus = {
                    cleanStatus with
                        Synchronization =
                            Some {
                                cleanStatus.Synchronization.Value with
                                    TargetRef = None
                                    TargetRevision = None
                                    Relationship = RevisionRelationshipDto.NoTarget
                            }
                }

                let sync = cleanStatus.Synchronization.Value
                let mutable synchronizeCalls = 0

                let deps = {
                    defaultDependencies with
                        createRevision =
                            fun _ ->
                                calls.Add "createRevision"
                                promise { return Ok(succeeded "revision-1") }
                        synchronize =
                            fun _ ->
                                calls.Add "synchronize"
                                synchronizeCalls <- synchronizeCalls + 1

                                if synchronizeCalls = 1 then
                                    promise {
                                        return
                                            Ok(
                                                failed
                                                    Validation
                                                    VersionControlCodes.PublishTargetMissing
                                                    "publish target missing"
                                            )
                                    }
                                else
                                    promise { return Ok(succeededWithPublication PublicationStateDto.Published sync) }
                        createRemoteProject =
                            fun _ ->
                                calls.Add "createRemoteProject"
                                promise { return Ok remoteProject }
                        bindWorkspace =
                            fun request ->
                                calls.Add "bindWorkspace"
                                boundLocation <- Some request.ProviderLocation
                                promise { return Ok(succeeded sessionInfo) }
                        getStatus =
                            fun _ ->
                                calls.Add "getStatus"
                                promise { return Ok(succeeded noTargetStatus) }
                        listRefs =
                            fun _ ->
                                calls.Add "listRefs"
                                promise { return Ok(succeeded refs) }
                        getStoragePolicySettings =
                            fun _ ->
                                calls.Add "getStoragePolicySettings"
                                promise { return Ok(succeeded (lfsSettings 5 true)) }
                }

                let state = {
                    runningState with
                        ChangedFiles = [| changedFile "README.md" "M" " " false |]
                }

                let stateAfterRequest, requestCmd =
                    update deps ignore (PrimarySaveAllRequested "Publish") state

                let! requestMessages = collectMessages requestCmd

                let stateAfterWrite, finishCmd =
                    match requestMessages with
                    | [| WriteRequested(PrimarySave _) |] -> update deps ignore requestMessages[0] stateAfterRequest
                    | _ -> failwith "Expected the primary save request."

                let! completionMessages = collectWriteMessages finishCmd

                let finalState, finalCmd =
                    match completionMessages with
                    | [| WriteCompleted(_, _, PrimarySave _, Ok(Completed _)) |] ->
                        update deps ignore completionMessages[0] stateAfterWrite
                    | _ -> failwith "Expected remote provisioning to complete the primary save."

                let! _ = collectMessages finalCmd

                Vitest
                    .expect(calls |> Seq.take 9 |> Seq.toArray)
                    .toEqual (
                        [|
                            "createRevision"
                            "getStatus"
                            "listRefs"
                            "getStoragePolicySettings"
                            "synchronize"
                            "createRemoteProject"
                            "bindWorkspace"
                            "getStatus"
                            "synchronize"
                        |]
                    )

                Vitest.expect(boundLocation).toEqual (Some "https://gitlab.example/carol/my-arc.git")
                Vitest.expect(finalState.ErrorNotice).toEqual (None)
                Vitest.expect(finalState.WarningNotice).toEqual (None)
            }
        )

        Vitest.test (
            "A provisioned bound remote threads acceptance through the second synchronize",
            fun () -> promise {
                let synchronizeRequests = ResizeArray<SynchronizeRequestDto>()

                let decisionFailure = {
                    makeFailure
                        Conflict
                        VersionControlCodes.UpdateWouldCreateConflictSession
                        "The update needs merge resolution."
                        (Some {
                            Code = VersionControlCodes.Recovery.AcceptUpdateRisks
                            Instructions = None
                        })
                        [| "conflict.txt" |] with
                        RevisionEvidence = [|
                            {
                                Label = "observed_target"
                                Revision = "target-1"
                            }
                        |]
                }

                let decidingStatus = {
                    cleanStatus with
                        WorkspaceVersion = "v2"
                }

                let deps = {
                    defaultDependencies with
                        synchronize =
                            fun request ->
                                synchronizeRequests.Add request

                                if synchronizeRequests.Count = 1 then
                                    promise { return Ok(OperationResultDto.Failed decisionFailure) }
                                else
                                    promise {
                                        return
                                            Ok(
                                                succeededWithPublication
                                                    PublicationStateDto.Published
                                                    cleanStatus.Synchronization.Value
                                            )
                                    }
                        getStatus = fun _ -> promise { return Ok(succeeded decidingStatus) }
                        listRefs = fun _ -> promise { return Ok(succeeded refs) }
                        getStoragePolicySettings = fun _ -> promise { return Ok(succeeded (lfsSettings 5 true)) }
                }

                let state = {
                    runningState with
                        ProvisionedRemote =
                            Some {
                                RemoteUrl = "https://gitlab.example/carol/my-arc.git"
                                ProjectName = "my-arc"
                                IsBound = true
                            }
                }

                let requested, writeCmd =
                    update deps ignore (WriteRequested(Push GitUpdateAcceptance.RequirePreview)) state

                let! completionMessages = collectMessages writeCmd

                let pendingState, pendingCmd =
                    match completionMessages with
                    | [| WriteCompleted(_, _, Push _, Ok(CompletedWithPendingRemoteConfirmation(_, _, action))) |] ->
                        Vitest
                            .expect(action)
                            .toEqual (
                                GitPendingRemoteAction.PublishAfterUpdate(
                                    GitUpdateAcceptance.Accepted("target-1", "v2")
                                )
                            )

                        update deps ignore completionMessages[0] requested
                    | _ -> failwith "Expected the bound remote to request acceptance."

                let! _ = collectMessages pendingCmd

                let confirmedState, confirmCmd =
                    update deps ignore ConfirmPendingRemoteActionRequested pendingState

                let! confirmMessages = collectMessages confirmCmd

                let secondWriteState, secondWriteCmd =
                    match confirmMessages with
                    | [| WriteRequested(Push(GitUpdateAcceptance.Accepted(target, workspaceVersion))) |] ->
                        Vitest.expect(target).toBe ("target-1")
                        Vitest.expect(workspaceVersion).toBe ("v2")
                        update deps ignore confirmMessages[0] confirmedState
                    | _ -> failwith "Expected the accepted publish request."

                let! secondCompletionMessages = collectMessages secondWriteCmd

                let _, finishCmd =
                    match secondCompletionMessages with
                    | [| WriteCompleted(_, _, Push _, Ok(Completed _)) |] ->
                        update deps ignore secondCompletionMessages[0] secondWriteState
                    | _ -> failwith "Expected the accepted publish to complete."

                let! _ = collectMessages finishCmd
                Vitest.expect(synchronizeRequests.Count).toBe (2)
                Vitest.expect(synchronizeRequests[1].AcceptUpdateRisks).toBe (true)
                Vitest.expect(synchronizeRequests[1].ExpectedTargetRevision).toEqual (Some "target-1")
            }
        )

        Vitest.test (
            "Acceptance after provisioning uses the token from the deciding synchronize",
            fun () -> promise {
                let synchronizeRequests = ResizeArray<SynchronizeRequestDto>()

                let missingTarget =
                    makeFailure Validation VersionControlCodes.PublishTargetMissing "publish target missing" None [||]

                let decisionFailure = {
                    makeFailure
                        Conflict
                        VersionControlCodes.UpdateWouldCreateConflictSession
                        "The update needs merge resolution."
                        (Some {
                            Code = VersionControlCodes.Recovery.AcceptUpdateRisks
                            Instructions = None
                        })
                        [| "conflict.txt" |] with
                        RevisionEvidence = [|
                            {
                                Label = "observed_target"
                                Revision = "target-2"
                            }
                        |]
                }

                let decidingStatus = {
                    cleanStatus with
                        WorkspaceVersion = "v2"
                }

                let deps = {
                    defaultDependencies with
                        synchronize =
                            fun request ->
                                synchronizeRequests.Add request

                                if synchronizeRequests.Count = 1 then
                                    promise { return Ok(OperationResultDto.Failed missingTarget) }
                                else
                                    promise { return Ok(OperationResultDto.Failed decisionFailure) }
                        createRemoteProject = fun _ -> promise { return Ok remoteProject }
                        bindWorkspace = fun _ -> promise { return Ok(succeeded sessionInfo) }
                        getStatus = fun _ -> promise { return Ok(succeeded decidingStatus) }
                        listRefs = fun _ -> promise { return Ok(succeeded refs) }
                        getStoragePolicySettings = fun _ -> promise { return Ok(succeeded (lfsSettings 5 true)) }
                }

                let requested, writeCmd =
                    update deps ignore (WriteRequested(Push GitUpdateAcceptance.RequirePreview)) runningState

                let! completionMessages = collectMessages writeCmd

                let nextState, finishCmd =
                    match completionMessages with
                    | [| WriteCompleted(_, _, Push _, Ok(CompletedWithPendingRemoteConfirmation(_, _, action))) |] ->
                        let state, command = update deps ignore completionMessages[0] requested

                        Vitest
                            .expect(action)
                            .toEqual (
                                GitPendingRemoteAction.PublishAfterUpdate(
                                    GitUpdateAcceptance.Accepted("target-2", "v2")
                                )
                            )

                        state, command
                    | _ -> failwith "Expected acceptance after provisioning."

                let! _ = collectMessages finishCmd
                Vitest.expect(nextState.PendingConfirmation.IsSome).toBe (true)
                Vitest.expect(synchronizeRequests.Count).toBe (2)
            }
        )

        Vitest.test (
            "A push whose synchronize reports a missing target after an applied update provisions the project",
            fun () -> promise {
                let sync = cleanStatus.Synchronization.Value
                let synchronizeRequests = ResizeArray<SynchronizeRequestDto>()
                let mutable createProjectCalls = 0
                let mutable bindCalls = 0
                let mutable statusCalls = 0

                let partialOutcome = {
                    operation sync with
                        Publication = PublicationStateDto.LocalOnly
                }

                let missingTarget = {
                    makeFailure
                        Validation
                        VersionControlCodes.PublishTargetMissing
                        "publish target missing after the update"
                        None
                        [||] with
                        StateChanged = true
                }

                let deps = {
                    defaultDependencies with
                        synchronize =
                            fun request ->
                                synchronizeRequests.Add request

                                if synchronizeRequests.Count = 1 then
                                    promise {
                                        return Ok(OperationResultDto.PartiallySucceeded(partialOutcome, missingTarget))
                                    }
                                else
                                    promise { return Ok(succeeded sync) }
                        createRemoteProject =
                            fun _ ->
                                createProjectCalls <- createProjectCalls + 1
                                promise { return Ok remoteProject }
                        bindWorkspace =
                            fun _ ->
                                bindCalls <- bindCalls + 1
                                promise { return Ok(succeeded sessionInfo) }
                        getStatus =
                            fun _ ->
                                statusCalls <- statusCalls + 1
                                promise { return Ok(succeeded cleanStatus) }
                        listRefs = fun _ -> promise { return Ok(succeeded refs) }
                        getStoragePolicySettings = fun _ -> promise { return Ok(succeeded (lfsSettings 5 true)) }
                }

                let requested, writeCmd =
                    update deps ignore (WriteRequested(Push GitUpdateAcceptance.RequirePreview)) runningState

                let! completionMessages = collectMessages writeCmd

                let finalState, finishCmd =
                    match completionMessages with
                    | [| WriteCompleted(_, _, Push _, Ok(Completed _)) |] ->
                        update deps ignore completionMessages[0] requested
                    | _ -> failwith "Expected provisioning after the applied update."

                let! _ = collectMessages finishCmd

                Vitest.expect(createProjectCalls).toBe (1)
                Vitest.expect(bindCalls).toBe (1)
                Vitest.expect(statusCalls).toBeGreaterThan (0)
                Vitest.expect(synchronizeRequests.Count).toBe (2)
                Vitest.expect(synchronizeRequests[0].PublishLocalRevisions).toBe (true)
                Vitest.expect(synchronizeRequests[1].PublishLocalRevisions).toBe (true)
                Vitest.expect(finalState.ErrorNotice).toEqual (None)
            }
        )

        Vitest.test (
            "A canceled update with restore_workspace offers to restore exactly the affected paths",
            fun () -> promise {
                let affectedPaths = [| "a.txt"; "b.txt" |]

                let canceled = {
                    makeFailure
                        Canceled
                        VersionControlCodes.OperationCanceled
                        "Update was canceled."
                        (Some {
                            Code = VersionControlCodes.Recovery.RestoreWorkspace
                            Instructions = Some "Restore the interrupted files."
                        })
                        affectedPaths with
                        StateChanged = true
                }

                let mutable restoredPaths = None

                let deps = {
                    defaultDependencies with
                        synchronize = fun _ -> promise { return Ok(OperationResultDto.Failed canceled) }
                        restorePaths =
                            fun request ->
                                restoredPaths <- Some request.Paths
                                promise { return Ok(succeeded ()) }
                        getStatus = fun _ -> promise { return Ok(succeeded cleanStatus) }
                        listRefs = fun _ -> promise { return Ok(succeeded refs) }
                        getStoragePolicySettings = fun _ -> promise { return Ok(succeeded (lfsSettings 5 true)) }
                }

                let stateAfterRequest, requestCmd =
                    update deps ignore (WriteRequested(Pull GitUpdateAcceptance.RequirePreview)) runningState

                let! completionMessages = collectMessages requestCmd

                let recoveryState, recoveryCmd =
                    match completionMessages with
                    | [| WriteCompleted(_,
                                        _,
                                        Pull _,
                                        Ok(RequiresRecovery(GitPendingRecovery.RestoreInterruptedPaths _, _))) |] ->
                        update deps ignore completionMessages[0] stateAfterRequest
                    | _ -> failwith "Expected restore recovery after cancellation."

                let! _ = collectMessages recoveryCmd

                let confirmedState, confirmCmd =
                    update deps ignore ConfirmPendingRemoteActionRequested recoveryState

                let! confirmMessages = collectMessages confirmCmd

                let restoreState, restoreCmd =
                    match confirmMessages with
                    | [| RestoreInterruptedPathsRequested |] -> update deps ignore confirmMessages[0] confirmedState
                    | _ -> failwith "Expected restore confirmation to dispatch the restore request."

                let! restoreMessages = collectMessages restoreCmd

                let _, restoreExecutionCmd =
                    match restoreMessages with
                    | [| WriteRequested(RestoreInterruptedPaths _) |] ->
                        update deps ignore restoreMessages[0] restoreState
                    | _ -> failwith "Expected the restore write request."

                let! _ = collectMessages restoreExecutionCmd

                Vitest.expect(recoveryState.PendingRecovery.IsSome).toBe (true)
                Vitest.expect(recoveryState.PendingConfirmation.IsSome).toBe (true)
                Vitest.expect(restoredPaths).toEqual (Some affectedPaths)
            }
        )

        Vitest.test (
            "Clearing a stale lock reports the removed lock files",
            fun () -> promise {
                let lockA = "C:/arc/.git/index.lock"
                let lockB = "C:/arc/.git/HEAD.lock"
                let unrelated = "This warning must stay hidden."

                let warnings = [|
                    {
                        Code = VersionControlCodes.LockRemoved
                        Message = lockA
                    }
                    {
                        Code = VersionControlCodes.LockRemoved
                        Message = lockB
                    }
                    {
                        Code = "other_warning"
                        Message = unrelated
                    }
                |]

                let refreshDependencies = {
                    defaultDependencies with
                        getStatus = fun _ -> promise { return Ok(succeeded cleanStatus) }
                        listRefs = fun _ -> promise { return Ok(succeeded refs) }
                        getStoragePolicySettings = fun _ -> promise { return Ok(succeeded (lfsSettings 5 true)) }
                }

                let runClear result = promise {
                    let deps = {
                        refreshDependencies with
                            clearStaleLock = fun _ -> promise { return result }
                    }

                    let requested, requestCmd =
                        update deps ignore (WriteRequested ClearStaleLock) runningState

                    let! completion = collectMessages requestCmd

                    match completion with
                    | [| WriteCompleted(_, _, ClearStaleLock, Ok(Completed _)) |] ->
                        let finalState, finishCmd = update deps ignore completion[0] requested
                        let! _ = collectMessages finishCmd
                        return finalState
                    | _ -> return failwith "Expected stale-lock clearing to complete."
                }

                let! pluralState = runClear (Ok(succeededWithWarnings warnings cleanStatus))
                let pluralNotice = pluralState.WarningNotice |> Option.defaultValue ""

                Vitest.expect(pluralNotice).toBe ($"Removed stale lock files: {lockA}, {lockB}.")
                Vitest.expect(pluralNotice).not.toContain (unrelated)
                Vitest.expect(pluralNotice).toContain (lockA)
                Vitest.expect(pluralNotice).toContain (lockB)

                let! singularState =
                    runClear (
                        Ok(
                            succeededWithWarnings
                                [|
                                    {
                                        Code = VersionControlCodes.LockRemoved
                                        Message = lockA
                                    }
                                |]
                                cleanStatus
                        )
                    )

                Vitest.expect(singularState.WarningNotice).toEqual (Some $"Removed stale lock file: {lockA}.")

                let! emptyState = runClear (Ok(succeeded cleanStatus))
                Vitest.expect(emptyState.WarningNotice).toEqual (None)

                let partialFailure =
                    makeFailure ProviderError "partial_lock_cleanup" "The remaining lock cleanup needs attention." None [||]

                let! partialState =
                    runClear (
                        Ok(
                            OperationResultDto.PartiallySucceeded(
                                operationWithPublication PublicationStateDto.NotApplicable cleanStatus
                                |> fun outcome -> {
                                    outcome with
                                        Warnings = [|
                                            {
                                                Code = VersionControlCodes.LockRemoved
                                                Message = lockA
                                            }
                                        |]
                                }
                                , partialFailure
                            )
                        )
                    )

                Vitest
                    .expect(partialState.WarningNotice)
                    .toEqual (Some $"Removed stale lock file: {lockA}. {partialFailure.Message}")
            }
        )

        Vitest.test (
            "A canceled update with remove_index_lock offers to clear the lock, and a cleared lock with an open merge abandons it",
            fun () -> promise {
                let canceled =
                    makeFailure
                        Canceled
                        VersionControlCodes.OperationCanceled
                        "Update was canceled."
                        (Some {
                            Code = VersionControlCodes.Recovery.RemoveIndexLock
                            Instructions = None
                        })
                        [||]

                let conflicted = conflictedStatus [| "conflict.txt" |]

                let deps = {
                    defaultDependencies with
                        synchronize = fun _ -> promise { return Ok(OperationResultDto.Failed canceled) }
                        clearStaleLock = fun _ -> promise { return Ok(succeeded conflicted) }
                        getStatus = fun _ -> promise { return Ok(succeeded conflicted) }
                        listRefs = fun _ -> promise { return Ok(succeeded refs) }
                        getStoragePolicySettings = fun _ -> promise { return Ok(succeeded (lfsSettings 5 true)) }
                }

                let stateAfterRequest, requestCmd =
                    update deps ignore (WriteRequested(Pull GitUpdateAcceptance.RequirePreview)) runningState

                let! completionMessages = collectMessages requestCmd

                let recoveryState, recoveryCmd =
                    match completionMessages with
                    | [| WriteCompleted(_, _, Pull _, Ok(RequiresRecovery(GitPendingRecovery.ClearStaleLock _, _))) |] ->
                        update deps ignore completionMessages[0] stateAfterRequest
                    | _ -> failwith "Expected stale-lock recovery after cancellation."

                let! _ = collectMessages recoveryCmd

                let confirmedState, confirmCmd =
                    update deps ignore ConfirmPendingRemoteActionRequested recoveryState

                let! confirmMessages = collectMessages confirmCmd

                let requestedState, requestedCmd =
                    match confirmMessages with
                    | [| ClearStaleLockRequested |] -> update deps ignore confirmMessages[0] confirmedState
                    | _ -> failwith "Expected lock clearing to be dispatched."

                let! requestedMessages = collectMessages requestedCmd

                let clearState, clearCmd =
                    match requestedMessages with
                    | [| WriteRequested ClearStaleLock |] -> update deps ignore requestedMessages[0] requestedState
                    | _ -> failwith "Expected the stale-lock write to be requested."

                let! clearMessages = collectMessages clearCmd

                let finalState, finalCmd =
                    match clearMessages with
                    | [| WriteCompleted(_, _, ClearStaleLock, Ok(Completed _)) |] ->
                        update deps ignore clearMessages[0] clearState
                    | _ -> failwith "Expected stale-lock clearing to complete."

                let! finalMessages = collectMessages finalCmd

                Vitest.expect(finalState.ActiveConflict.IsSome).toBe (true)
                Vitest.expect(finalMessages).toEqual ([| WriteRequested AbandonMerge |])
            }
        )

        Vitest.test (
            "A canceled fetch that left an index lock offers the clear lock dialog",
            fun () -> promise {
                let canceled =
                    makeFailure
                        Canceled
                        VersionControlCodes.OperationCanceled
                        "Fetch was canceled."
                        (Some {
                            Code = VersionControlCodes.Recovery.RemoveIndexLock
                            Instructions = None
                        })
                        [||]

                let deps = {
                    defaultDependencies with
                        refreshSynchronization = fun _ -> promise { return Ok(OperationResultDto.Failed canceled) }
                }

                let stateAfterRequest, requestCmd =
                    update deps ignore (WriteRequested Fetch) runningState

                let! completionMessages = collectMessages requestCmd

                let nextState, finishCmd =
                    match completionMessages with
                    | [| WriteCompleted(_, _, Fetch, Ok(RequiresRecovery(GitPendingRecovery.ClearStaleLock _, _))) |] ->
                        update deps ignore completionMessages[0] stateAfterRequest
                    | _ -> failwith "Expected clear-lock recovery after the canceled fetch."

                let! _ = collectMessages finishCmd

                Vitest.expect(nextState.PendingRecovery).toEqual (Some(GitPendingRecovery.ClearStaleLock None))
                Vitest.expect(nextState.PendingConfirmation.IsSome).toBe (true)
            }
        )

        Vitest.test (
            "A canceled synchronize with a refresh recovery refreshes and keeps the warning",
            fun () -> promise {
                let canceled =
                    makeFailure
                        Canceled
                        VersionControlCodes.OperationCanceled
                        "Publish was canceled after changing the workspace."
                        (Some {
                            Code = VersionControlCodes.Recovery.RefreshWorkspace
                            Instructions = None
                        })
                        [||]

                let deps = {
                    defaultDependencies with
                        synchronize = fun _ -> promise { return Ok(OperationResultDto.Failed canceled) }
                        getStatus = fun _ -> promise { return Ok(succeeded cleanStatus) }
                        listRefs = fun _ -> promise { return Ok(succeeded refs) }
                        getStoragePolicySettings = fun _ -> promise { return Ok(succeeded (lfsSettings 5 true)) }
                }

                let stateAfterRequest, requestCmd =
                    update deps ignore (WriteRequested(Push GitUpdateAcceptance.RequirePreview)) runningState

                let! completionMessages = collectMessages requestCmd

                let nextState, finishCmd =
                    match completionMessages with
                    | [| WriteCompleted(_, _, Push _, Ok(Completed _)) |] ->
                        update deps ignore completionMessages[0] stateAfterRequest
                    | _ -> failwith "Expected refresh recovery after the canceled publish."

                let! _ = collectMessages finishCmd

                Vitest.expect(nextState.WarningNotice).toEqual (Some(failureMessage canceled))
                Vitest.expect(nextState.ErrorNotice).toEqual (None)
            }
        )

        Vitest.test (
            "A partial synchronize with retry_publish offers to publish now",
            fun () -> promise {
                let sync = cleanStatus.Synchronization.Value

                let retryFailure = {
                    makeFailure
                        Canceled
                        VersionControlCodes.OperationCanceled
                        "The update was applied. Synchronize again to publish."
                        (Some {
                            Code = VersionControlCodes.Recovery.RetryPublish
                            Instructions = Some "Publish the applied update now."
                        })
                        [||] with
                        StateChanged = true
                }

                let deps = {
                    defaultDependencies with
                        synchronize =
                            fun _ -> promise {
                                return
                                    Ok(
                                        OperationResultDto.PartiallySucceeded(
                                            {
                                                operation sync with
                                                    Publication = PublicationStateDto.LocalOnly
                                            },
                                            retryFailure
                                        )
                                    )
                            }
                        getStatus = fun _ -> promise { return Ok(succeeded cleanStatus) }
                        listRefs = fun _ -> promise { return Ok(succeeded refs) }
                        getStoragePolicySettings = fun _ -> promise { return Ok(succeeded (lfsSettings 5 true)) }
                }

                let requested, writeCmd =
                    update deps ignore (WriteRequested(Push GitUpdateAcceptance.RequirePreview)) runningState

                let! completionMessages = collectMessages writeCmd

                let recoveryState, recoveryCmd =
                    match completionMessages with
                    | [| WriteCompleted(_, _, Push _, Ok(Completed _)) |] ->
                        update deps ignore completionMessages[0] requested
                    | _ -> failwith "Expected the partial synchronize completion."

                let! _ = collectMessages recoveryCmd

                Vitest
                    .expect(recoveryState.PendingRecovery)
                    .toEqual (Some(GitPendingRecovery.RetryPublish(failureMessage retryFailure)))

                Vitest
                    .expect(recoveryState.PendingConfirmation |> Option.map _.Title)
                    .toEqual (Some "Online sync pending")

                let _, confirmCmd =
                    update deps ignore ConfirmPendingRemoteActionRequested recoveryState

                let! confirmMessages = collectMessages confirmCmd

                Vitest
                    .expect(confirmMessages)
                    .toEqual (
                        [|
                            WriteRequested(Push GitUpdateAcceptance.RequirePreview)
                        |]
                    )
            }
        )

        Vitest.test (
            "Later on the publish offer does not publish",
            fun () -> promise {
                let sync = cleanStatus.Synchronization.Value

                let retryFailure = {
                    makeFailure
                        Canceled
                        VersionControlCodes.OperationCanceled
                        "The update was applied. Synchronize again to publish."
                        (Some {
                            Code = VersionControlCodes.Recovery.RetryPublish
                            Instructions = Some "Publish the applied update now."
                        })
                        [||] with
                        StateChanged = true
                }

                let deps = {
                    defaultDependencies with
                        synchronize =
                            fun _ -> promise {
                                return
                                    Ok(
                                        OperationResultDto.PartiallySucceeded(
                                            {
                                                operation sync with
                                                    Publication = PublicationStateDto.LocalOnly
                                            },
                                            retryFailure
                                        )
                                    )
                            }
                        getStatus = fun _ -> promise { return Ok(succeeded cleanStatus) }
                        listRefs = fun _ -> promise { return Ok(succeeded refs) }
                        getStoragePolicySettings = fun _ -> promise { return Ok(succeeded (lfsSettings 5 true)) }
                }

                let requested, writeCmd =
                    update deps ignore (WriteRequested(Push GitUpdateAcceptance.RequirePreview)) runningState

                let! completionMessages = collectMessages writeCmd

                let recoveryState, recoveryCmd =
                    match completionMessages with
                    | [| WriteCompleted(_, _, Push _, Ok(Completed _)) |] ->
                        update deps ignore completionMessages[0] requested
                    | _ -> failwith "Expected the partial synchronize completion."

                let! _ = collectMessages recoveryCmd

                let cancelingState, cancelCmd =
                    update deps ignore CancelPendingRemoteActionRequested recoveryState

                let! cancelMessages = collectMessages cancelCmd

                let dismissedState, dismissCmd =
                    match cancelMessages with
                    | [| DismissRecoveryRequested |] -> update deps ignore cancelMessages[0] cancelingState
                    | _ -> failwith "Expected the recovery dismissal."

                let! dismissMessages = collectMessages dismissCmd

                let dispatchedMessages = Array.append cancelMessages dismissMessages

                Vitest
                    .expect(
                        dispatchedMessages
                        |> Array.exists (
                            function
                            | WriteRequested(Push _) -> true
                            | _ -> false
                        )
                    )
                    .toBe (false)

                Vitest.expect(dismissedState.PendingPostMergePush).toBe (false)
                Vitest.expect(dismissedState.PendingRecovery).toEqual (None)
            }
        )

        Vitest.test (
            "A partial without a mapped recovery leaves no pending publish",
            fun () -> promise {
                let sync = cleanStatus.Synchronization.Value

                let partialFailure = {
                    makeFailure
                        Concurrency
                        VersionControlCodes.PreconditionFailed
                        "The workspace changed while synchronizing."
                        None
                        [||] with
                        StateChanged = true
                }

                let deps = {
                    defaultDependencies with
                        createRevision = fun _ -> promise { return Ok(succeeded "revision-1") }
                        synchronize =
                            fun _ -> promise {
                                return
                                    Ok(
                                        OperationResultDto.PartiallySucceeded(
                                            {
                                                operation sync with
                                                    Publication = PublicationStateDto.LocalOnly
                                            },
                                            partialFailure
                                        )
                                    )
                            }
                        getStatus = fun _ -> promise { return Ok(succeeded cleanStatus) }
                        listRefs = fun _ -> promise { return Ok(succeeded refs) }
                        getStoragePolicySettings = fun _ -> promise { return Ok(succeeded (lfsSettings 5 true)) }
                }

                let saveState = {
                    runningState with
                        ChangedFiles = [| changedFile "README.md" "M" " " false |]
                }

                let stateAfterRequest, requestCmd =
                    update deps ignore (PrimarySaveAllRequested "Save") saveState

                let! requestMessages = collectMessages requestCmd

                let stateAfterWrite, writeCmd =
                    match requestMessages with
                    | [| WriteRequested(PrimarySave _) |] -> update deps ignore requestMessages[0] stateAfterRequest
                    | _ -> failwith "Expected the primary save request."

                let! completionMessages = collectWriteMessages writeCmd

                let finalState, finishCmd =
                    match completionMessages with
                    | [| WriteCompleted(_, _, PrimarySave _, Ok(Completed _)) |] ->
                        update deps ignore completionMessages[0] stateAfterWrite
                    | _ -> failwith "Expected the partial save completion."

                let! _ = collectMessages finishCmd

                Vitest.expect(finalState.PendingPostMergePush).toBe (false)
                Vitest.expect(finalState.PendingRecovery).toEqual (None)

                Vitest
                    .expect(finalState.WarningNotice |> Option.defaultValue "")
                    .toContain (failureMessage partialFailure)
            }
        )

        Vitest.test (
            "Publish now clears the parked recovery",
            fun () -> promise {
                let sync = cleanStatus.Synchronization.Value

                let retryFailure = {
                    makeFailure
                        Canceled
                        VersionControlCodes.OperationCanceled
                        "The update was applied. Synchronize again to publish."
                        (Some {
                            Code = VersionControlCodes.Recovery.RetryPublish
                            Instructions = Some "Publish the applied update now."
                        })
                        [||] with
                        StateChanged = true
                }

                let deps = {
                    defaultDependencies with
                        synchronize =
                            fun _ -> promise {
                                return
                                    Ok(
                                        OperationResultDto.PartiallySucceeded(
                                            {
                                                operation sync with
                                                    Publication = PublicationStateDto.LocalOnly
                                            },
                                            retryFailure
                                        )
                                    )
                            }
                        getStatus = fun _ -> promise { return Ok(succeeded cleanStatus) }
                        listRefs = fun _ -> promise { return Ok(succeeded refs) }
                        getStoragePolicySettings = fun _ -> promise { return Ok(succeeded (lfsSettings 5 true)) }
                }

                let requested, writeCmd =
                    update deps ignore (WriteRequested(Push GitUpdateAcceptance.RequirePreview)) runningState

                let! completionMessages = collectMessages writeCmd

                let recoveryState, recoveryCmd =
                    match completionMessages with
                    | [| WriteCompleted(_, _, Push _, Ok(Completed _)) |] ->
                        update deps ignore completionMessages[0] requested
                    | _ -> failwith "Expected the partial synchronize completion."

                let! _ = collectMessages recoveryCmd

                let confirmedState, confirmCmd =
                    update deps ignore ConfirmPendingRemoteActionRequested recoveryState

                Vitest.expect(confirmedState.PendingRecovery).toEqual (None)

                let! confirmMessages = collectMessages confirmCmd

                Vitest
                    .expect(confirmMessages)
                    .toEqual (
                        [|
                            WriteRequested(Push GitUpdateAcceptance.RequirePreview)
                        |]
                    )
            }
        )

        Vitest.test (
            "A canceled fetch that needs inspection reports it after a refresh",
            fun () -> promise {
                let canceled =
                    makeFailure
                        Canceled
                        VersionControlCodes.OperationCanceled
                        "Fetch was canceled while inspecting the workspace."
                        (Some {
                            Code = VersionControlCodes.Recovery.InspectWorkspace
                            Instructions = None
                        })
                        [||]

                let deps = {
                    defaultDependencies with
                        refreshSynchronization = fun _ -> promise { return Ok(OperationResultDto.Failed canceled) }
                        getStatus = fun _ -> promise { return Ok(succeeded cleanStatus) }
                        listRefs = fun _ -> promise { return Ok(succeeded refs) }
                        getStoragePolicySettings = fun _ -> promise { return Ok(succeeded (lfsSettings 5 true)) }
                }

                let stateAfterRequest, requestCmd =
                    update deps ignore (WriteRequested Fetch) runningState

                let! completionMessages = collectMessages requestCmd

                let nextState, finishCmd =
                    match completionMessages with
                    | [| WriteCompleted(_, _, Fetch, Ok(CompletedWithPendingRemoteFailure(_, message))) |] ->
                        Vitest.expect(message).toBe (failureMessage canceled)
                        update deps ignore completionMessages[0] stateAfterRequest
                    | _ -> failwith "Expected inspection failure after the canceled fetch."

                let! _ = collectMessages finishCmd

                Vitest.expect(nextState.ErrorNotice).toEqual (Some(failureMessage canceled))
                Vitest.expect(nextState.PendingConfirmation).toEqual (None)
            }
        )

        Vitest.test (
            "A canceled update with refresh_workspace refreshes and reports the notice",
            fun () -> promise {
                let canceled =
                    makeFailure
                        Canceled
                        VersionControlCodes.OperationCanceled
                        "Update was canceled after changing the workspace."
                        (Some {
                            Code = VersionControlCodes.Recovery.RefreshWorkspace
                            Instructions = None
                        })
                        [||]

                let deps = {
                    defaultDependencies with
                        synchronize = fun _ -> promise { return Ok(OperationResultDto.Failed canceled) }
                        getStatus = fun _ -> promise { return Ok(succeeded (statusForBranch "main")) }
                        listRefs = fun _ -> promise { return Ok(succeeded [| localBranch "main" true true |]) }
                        getStoragePolicySettings = fun _ -> promise { return Ok(succeeded (lfsSettings 5 true)) }
                }

                let stateAfterRequest, requestCmd =
                    update deps ignore (WriteRequested(Pull GitUpdateAcceptance.RequirePreview)) runningState

                let! completionMessages = collectMessages requestCmd

                let nextState, finishCmd =
                    match completionMessages with
                    | [| WriteCompleted(_, _, Pull _, Ok(Completed _)) |] ->
                        update deps ignore completionMessages[0] stateAfterRequest
                    | _ -> failwith "Expected refresh recovery to complete with refreshed state."

                let! _ = collectMessages finishCmd

                Vitest.expect(nextState.ErrorNotice).toEqual (None)
                Vitest.expect(nextState.WarningNotice).toEqual (Some(failureMessage canceled))
                Vitest.expect(nextState.Status.CurrentBranch).toEqual (Some "main")
            }
        )

        Vitest.test (
            "A partial update with retry_materialization keeps the pulled result and offers the download",
            fun () -> promise {
                let failure =
                    makeFailure
                        Canceled
                        VersionControlCodes.OperationCanceled
                        "Large-file hydration was canceled."
                        (Some {
                            Code = VersionControlCodes.Recovery.RetryMaterialization
                            Instructions = None
                        })
                        [||]

                let mutable materializedPaths = ResizeArray<string>()
                let mutable refreshTreeValues = ResizeArray<bool option>()

                let deps = {
                    defaultDependencies with
                        synchronize =
                            fun _ -> promise {
                                return
                                    Ok(
                                        OperationResultDto.PartiallySucceeded(
                                            operation cleanStatus.Synchronization.Value,
                                            failure
                                        )
                                    )
                            }
                        listObjects =
                            fun _ -> promise {
                                return
                                    Ok(
                                        succeeded [|
                                            {
                                                Path = "large.bin"
                                                IsMaterialized = false
                                                IsLocallyAvailable = false
                                                SizeBytes = Some 100.
                                                ObjectId = Some "object-1"
                                            }
                                            {
                                                Path = "small.txt"
                                                IsMaterialized = false
                                                IsLocallyAvailable = false
                                                SizeBytes = None
                                                ObjectId = None
                                            }
                                        |]
                                    )
                            }
                        materializeObject =
                            fun request ->
                                materializedPaths.Add request.Path
                                refreshTreeValues.Add request.RefreshTree
                                promise { return Ok(succeeded ()) }
                        getStatus = fun _ -> promise { return Ok(succeeded cleanStatus) }
                        listRefs = fun _ -> promise { return Ok(succeeded refs) }
                        getStoragePolicySettings = fun _ -> promise { return Ok(succeeded (lfsSettings 5 true)) }
                }

                let stateAfterRequest, requestCmd =
                    update deps ignore (WriteRequested(Pull GitUpdateAcceptance.RequirePreview)) runningState

                let! completionMessages = collectMessages requestCmd

                let recoveryState, finishCmd =
                    match completionMessages with
                    | [| WriteCompleted(_, _, Pull _, Ok(Completed _)) |] ->
                        update deps ignore completionMessages[0] stateAfterRequest
                    | _ -> failwith "Expected partial update completion."

                let! _ = collectMessages finishCmd

                let confirmedState, confirmCmd =
                    update deps ignore ConfirmPendingRemoteActionRequested recoveryState

                let! confirmMessages = collectMessages confirmCmd

                let retryState, retryCmd =
                    match confirmMessages with
                    | [| RetryMaterializationRequested |] -> update deps ignore confirmMessages[0] confirmedState
                    | _ -> failwith "Expected materialization retry to be dispatched."

                let! retryMessages = collectMessages retryCmd

                let! _ =
                    match retryMessages with
                    | [| WriteRequested RetryMaterialization |] -> promise { return () }
                    | _ -> promise { return () }

                let _, retryCompletionCmd = update deps ignore retryMessages[0] retryState
                let! _ = collectMessages retryCompletionCmd

                Vitest
                    .expect(
                        recoveryState.PendingRecovery
                        |> Option.map (fun value ->
                            match value with
                            | GitPendingRecovery.RetryMaterialization _ -> true
                            | _ -> false
                        )
                    )
                    .toEqual (Some true)

                Vitest.expect(recoveryState.Status.CurrentBranch).toEqual (Some "main")
                Vitest.expect(materializedPaths |> Seq.toArray).toEqual ([| "large.bin"; "small.txt" |])
                Vitest.expect(refreshTreeValues |> Seq.toArray).toEqual ([| Some false; Some true |])
            }
        )

        Vitest.test (
            "An update conflict opens the first conflicted item",
            fun () -> promise {
                let conflictStatus = conflictedStatus [| "a.txt"; "b.txt" |]
                let conflict = conflictStatus.ActiveConflictSession.Value

                let failure =
                    makeFailure
                        Conflict
                        VersionControlCodes.ConflictsDetected
                        "Conflicts detected."
                        (Some {
                            Code = VersionControlCodes.Recovery.ResolveConflictSession
                            Instructions = None
                        })
                        [||]

                let pageStates = ResizeArray<PageState option>()
                let mutable loaded = None

                let deps = {
                    defaultDependencies with
                        synchronize =
                            fun _ -> promise {
                                return
                                    Ok(
                                        OperationResultDto.PartiallySucceeded(
                                            operation cleanStatus.Synchronization.Value,
                                            failure
                                        )
                                    )
                            }
                        getStatus = fun _ -> promise { return Ok(succeeded conflictStatus) }
                        listRefs = fun _ -> promise { return Ok(succeeded refs) }
                        getStoragePolicySettings = fun _ -> promise { return Ok(succeeded (lfsSettings 5 true)) }
                        loadConflictPage =
                            fun session version path ->
                                loaded <- Some(session, version, path)
                                promise { return Ok(diffPage path) }
                }

                let stateAfterRequest, requestCmd =
                    update deps pageStates.Add (WriteRequested(Pull GitUpdateAcceptance.RequirePreview)) runningState

                let! completionMessages = collectMessages requestCmd

                let nextState, finishCmd =
                    match completionMessages with
                    | [| WriteCompleted(_, _, Pull _, Ok(Completed _)) |] ->
                        update deps pageStates.Add completionMessages[0] stateAfterRequest
                    | _ -> failwith "Expected conflict update completion."

                let! _ = collectMessages finishCmd

                Vitest.expect(nextState.SelectedChangePath).toEqual (Some "a.txt")
                Vitest.expect(pageStates.Count).toBe (1)

                Vitest
                    .expect(loaded |> Option.map (fun (_, version, path) -> version, path))
                    .toEqual (Some("v1", "a.txt"))

                Vitest.expect(nextState.ActiveConflict).toEqual (Some conflict)
            }
        )

        Vitest.test (
            "Confirming a resolution finalizes when nothing remains and resumes the pending publish",
            fun () -> promise {
                let conflict = (conflictedStatus [| "conflict.txt" |]).ActiveConflictSession.Value

                let refreshedHandle = {
                    SessionId = "conflict-2"
                    Version = "2"
                }

                let mutable resolveRequest = None
                let mutable finalizeRequest = None
                let mutable statusCalls = 0

                let deps = {
                    defaultDependencies with
                        resolveConflict =
                            fun request ->
                                resolveRequest <- Some request

                                promise {
                                    return
                                        Ok(
                                            succeeded {
                                                RefreshedHandle = refreshedHandle
                                                RemainingItems = [||]
                                            }
                                        )
                                }
                        getStatus =
                            fun _ ->
                                statusCalls <- statusCalls + 1
                                promise { return Ok(succeeded cleanStatus) }
                        finalizeConflict =
                            fun request ->
                                finalizeRequest <- Some request
                                promise { return Ok(succeeded (Some "rev")) }
                }

                let request = {
                    Path = "conflict.txt"
                    Handle = conflict.Handle
                    WorkspaceVersion = "v1"
                    ResolvedContent = "resolved"
                }

                let state = {
                    runningState with
                        ActiveConflict = Some conflict
                        PendingPostMergePush = true
                        SelectedChangePath = Some "conflict.txt"
                }

                let stateAfterRequest, requestCmd =
                    update deps ignore (ConfirmMergeResolutionRequested request) state

                let! completionMessages = collectMessages requestCmd

                let nextState, finishCmd =
                    match completionMessages with
                    | [| ConfirmMergeResolutionCompleted(_, Ok outcome) |] ->
                        Vitest.expect(outcome.Finalized).toBe (true)
                        update deps ignore completionMessages[0] stateAfterRequest
                    | _ -> failwith "Expected merge resolution completion."

                let! messages = collectMessages finishCmd

                Vitest.expect(statusCalls).toBe (2)
                Vitest.expect(resolveRequest.Value.Handle).toEqual (request.Handle)
                Vitest.expect(resolveRequest.Value.ExpectedWorkspaceVersion).toBe ("v1")
                Vitest.expect(finalizeRequest.Value.Handle).toEqual (refreshedHandle)
                Vitest.expect(finalizeRequest.Value.ExpectedWorkspaceVersion).toBe (cleanStatus.WorkspaceVersion)
                Vitest.expect(nextState.PendingPostMergePush).toBe (false)

                Vitest
                    .expect(messages)
                    .toEqual (
                        [|
                            WriteRequested(Push GitUpdateAcceptance.RequirePreview)
                        |]
                    )
            }
        )

        Vitest.test (
            "A stale conflict handle reloads the session instead of resolving",
            fun () -> promise {
                let conflict = (conflictedStatus [| "conflict.txt" |]).ActiveConflictSession.Value

                let failure =
                    makeFailure
                        Concurrency
                        VersionControlCodes.PreconditionFailed
                        "The conflict session is stale."
                        (Some {
                            Code = VersionControlCodes.Recovery.RefreshConflictSession
                            Instructions = None
                        })
                        [||]

                let pageStates = ResizeArray<PageState option>()

                let deps = {
                    defaultDependencies with
                        resolveConflict = fun _ -> promise { return Ok(OperationResultDto.Failed failure) }
                }

                let request = {
                    Path = "conflict.txt"
                    Handle = conflict.Handle
                    WorkspaceVersion = "v1"
                    ResolvedContent = "resolved"
                }

                let state = {
                    runningState with
                        SelectedChangePath = Some "conflict.txt"
                }

                let stateAfterRequest, requestCmd =
                    update deps pageStates.Add (ConfirmMergeResolutionRequested request) state

                let! completionMessages = collectMessages requestCmd

                let nextState, finishCmd =
                    match completionMessages with
                    | [| ConfirmMergeResolutionCompleted(_, Error(ConfirmMergeResolutionError.Stale _)) |] ->
                        update deps pageStates.Add completionMessages[0] stateAfterRequest
                    | _ -> failwith "Expected stale conflict completion."

                let! messages = collectMessages finishCmd

                Vitest.expect(nextState.SelectedChangePath).toEqual (None)
                Vitest.expect(pageStates |> Seq.toArray).toEqual ([| None |])
                Vitest.expect(messages).toEqual ([| RefreshRequested |])
            }
        )

        Vitest.test (
            "A missing dependency asks to configure it and retries once",
            fun () -> promise {
                let mutable promptCalls = 0
                let mutable installCalls = 0
                let mutable publishCalls = 0
                let mutable installedComponent = None
                let sync = cleanStatus.Synchronization.Value

                let dependencies = [|
                    {
                        Component = "git"
                        Installed = true
                        Version = Some "2.0"
                        Compatible = true
                        Remediation = None
                    }
                    {
                        Component = "git-lfs"
                        Installed = true
                        Version = Some "3.0"
                        Compatible = true
                        Remediation = None
                    }
                    {
                        Component = "git-lfs-configuration"
                        Installed = false
                        Version = None
                        Compatible = false
                        Remediation = Some "run git lfs install"
                    }
                |]

                let deps = {
                    defaultDependencies with
                        synchronize =
                            fun _ ->
                                publishCalls <- publishCalls + 1

                                if publishCalls = 1 then
                                    promise {
                                        return
                                            Ok(
                                                failed
                                                    DependencyMissing
                                                    "lfs_operation_failed"
                                                    "Git LFS is not configured."
                                            )
                                    }
                                else
                                    promise { return Ok(succeeded sync) }
                        checkDependencies = fun _ -> promise { return Ok(succeeded dependencies) }
                        confirmInstall =
                            fun _ ->
                                promptCalls <- promptCalls + 1
                                true
                        installDependency =
                            fun request ->
                                installedComponent <- Some request.Component
                                installCalls <- installCalls + 1
                                promise { return Ok(succeeded dependencies[2]) }
                        getStatus = fun _ -> promise { return Ok(succeeded cleanStatus) }
                        listRefs = fun _ -> promise { return Ok(succeeded refs) }
                        getStoragePolicySettings = fun _ -> promise { return Ok(succeeded (lfsSettings 5 true)) }
                }

                let stateAfterRequest, requestCmd =
                    update deps ignore (WriteRequested(Push GitUpdateAcceptance.RequirePreview)) runningState

                let! completionMessages = collectMessages requestCmd

                let stateAfterPrompt, promptCmd =
                    match completionMessages with
                    | [| WriteCompleted(_, _, Push _, Ok(RequiresDependencyInstall("git-lfs-configuration", _))) |] ->
                        update deps ignore completionMessages[0] stateAfterRequest
                    | _ -> failwith "Expected the missing dependency prompt."

                let! promptMessages = collectMessages promptCmd

                let stateAfterAnswer, installCmd =
                    match promptMessages with
                    | [| WriteInstallPromptAnswered(_, Push _, "git-lfs-configuration", true) |] ->
                        update deps ignore promptMessages[0] stateAfterPrompt
                    | _ -> failwith "Expected an affirmative dependency-install answer."

                let! installMessages = collectMessages installCmd

                let stateAfterInstall, retryCmd =
                    match installMessages with
                    | [| WriteInstallCompleted(_, Push _, Ok _) |] ->
                        update deps ignore installMessages[0] stateAfterAnswer
                    | _ -> failwith "Expected dependency installation to complete."

                let! retryMessages = collectMessages retryCmd

                let finalState, finishCmd =
                    match retryMessages with
                    | [| WriteCompleted(_, _, Push _, Ok(Completed _)) |] ->
                        update deps ignore retryMessages[0] stateAfterInstall
                    | _ -> failwith "Expected the retried publish to complete."

                let! _ = collectMessages finishCmd

                Vitest.expect(promptCalls).toBe (1)
                Vitest.expect(installCalls).toBe (1)
                Vitest.expect(publishCalls).toBe (2)
                Vitest.expect(installedComponent).toEqual (Some "git-lfs-configuration")
                Vitest.expect(finalState.ErrorNotice).toEqual (None)
            }
        )

        Vitest.test (
            "A stale workspace token refreshes and retries the write once",
            fun () -> promise {
                let requests = ResizeArray<CreateRevisionRequestDto>()

                let deps = {
                    defaultDependencies with
                        createRevision =
                            fun request ->
                                requests.Add request

                                if requests.Count = 1 then
                                    promise {
                                        return
                                            Ok(
                                                failed
                                                    Concurrency
                                                    VersionControlCodes.PreconditionFailed
                                                    "workspace version is stale"
                                            )
                                    }
                                else
                                    promise { return Ok(succeeded "revision-2") }
                        getStatus =
                            fun _ -> promise {
                                return
                                    Ok(
                                        succeeded {
                                            cleanStatus with
                                                WorkspaceVersion = "v2"
                                        }
                                    )
                            }
                        listRefs = fun _ -> promise { return Ok(succeeded refs) }
                        getStoragePolicySettings = fun _ -> promise { return Ok(succeeded (lfsSettings 5 true)) }
                }

                let state = {
                    runningState with
                        ChangedFiles = [| changedFile "a.txt" "M" " " false |]
                }

                let model, command = update deps ignore (CommitAllRequested "save") state
                let! messages = collectMessages command
                let requested, writeCmd = update deps ignore messages[0] model
                let! completion = collectMessages writeCmd

                let finalState, _ =
                    match completion with
                    | [| WriteCompleted(_, _, CommitAll _, Ok(Completed _)) |] ->
                        update deps ignore completion[0] requested
                    | _ -> failwith "Expected the retried commit to complete."

                Vitest.expect(requests.Count).toBe (2)
                Vitest.expect(requests[0].ExpectedWorkspaceVersion).toBe ("v1")
                Vitest.expect(requests[1].ExpectedWorkspaceVersion).toBe ("v2")
                Vitest.expect(finalState.ErrorNotice).toEqual (None)
            }
        )

        Vitest.test (
            "A workspace token that is stale twice reports the failure",
            fun () -> promise {
                let reportedErrors = ResizeArray<GitErrorNotification>()
                let mutable calls = 0

                let deps = {
                    defaultDependencies with
                        reportError = reportedErrors.Add
                        createRevision =
                            fun _ ->
                                calls <- calls + 1

                                promise {
                                    return Ok(failed Concurrency VersionControlCodes.PreconditionFailed "still stale")
                                }
                        getStatus = fun _ -> promise { return Ok(succeeded cleanStatus) }
                        listRefs = fun _ -> promise { return Ok(succeeded refs) }
                        getStoragePolicySettings = fun _ -> promise { return Ok(succeeded (lfsSettings 5 true)) }
                }

                let state = {
                    runningState with
                        ChangedFiles = [| changedFile "a.txt" "M" " " false |]
                }

                let model, command = update deps ignore (CommitAllRequested "save") state
                let! messages = collectMessages command
                let requested, writeCmd = update deps ignore messages[0] model
                let! completion = collectMessages writeCmd
                let finalState, finishCmd = update deps ignore completion[0] requested
                let! _ = collectMessages finishCmd

                Vitest.expect(calls).toBe (2)
                Vitest.expect(finalState.BusyOperation).toEqual (None)
                Vitest.expect(finalState.ErrorNotice).toEqual (Some "still stale")
                Vitest.expect(reportedErrors.Count).toBe (1)
            }
        )

        Vitest.test (
            "A precondition failure outside the concurrency category is an error, not a retry",
            fun () -> promise {
                let mutable calls = 0

                let deps = {
                    defaultDependencies with
                        createRevision =
                            fun _ ->
                                calls <- calls + 1
                                promise { return Ok(failed Validation VersionControlCodes.PreconditionFailed "bad") }
                        reportError = fun _ -> ()
                }

                let state = {
                    runningState with
                        ChangedFiles = [| changedFile "a.txt" "M" " " false |]
                }

                let model, command = update deps ignore (CommitAllRequested "save") state
                let! messages = collectMessages command

                let requested, writeCmd =
                    match messages with
                    | [| WriteRequested(CommitAll _) |] -> update deps ignore messages[0] model
                    | _ -> failwith "Expected the commit-all write request."

                let! completion = collectMessages writeCmd

                let finalState, finishCmd =
                    match completion with
                    | [| WriteCompleted(_, _, CommitAll _, Error "bad") |] -> update deps ignore completion[0] requested
                    | _ -> failwith "Expected the validation failure."

                let! _ = collectMessages finishCmd

                Vitest.expect(calls).toBe (1)
                Vitest.expect(finalState.ErrorNotice).toEqual (Some "bad")
            }
        )

        Vitest.test (
            "A stale discard is not repeated and reports the refreshed state",
            fun () -> promise {
                let restoreRequests = ResizeArray<RestorePathsRequestDto>()
                let reportedErrors = ResizeArray<GitErrorNotification>()

                let deps = {
                    defaultDependencies with
                        restorePaths =
                            fun request ->
                                restoreRequests.Add request

                                promise {
                                    return
                                        Ok(
                                            failed
                                                Concurrency
                                                VersionControlCodes.PreconditionFailed
                                                "workspace version is stale"
                                        )
                                }
                        getStatus =
                            fun _ -> promise {
                                return
                                    Ok(
                                        succeeded {
                                            cleanStatus with
                                                WorkspaceVersion = "v2"
                                        }
                                    )
                            }
                        listRefs = fun _ -> promise { return Ok(succeeded refs) }
                        getStoragePolicySettings = fun _ -> promise { return Ok(succeeded (lfsSettings 5 true)) }
                        reportError = reportedErrors.Add
                }

                let state = {
                    runningState with
                        ChangedFiles = [| changedFile "a.txt" "M" " " false |]
                }

                let model, command =
                    update deps ignore (DiscardSelectionRequested [| "a.txt" |]) state

                let! messages = collectMessages command

                let requested, writeCmd =
                    match messages with
                    | [| WriteRequested(DiscardSelection [| "a.txt" |]) |] -> update deps ignore messages[0] model
                    | _ -> failwith "Expected the discard write request."

                let! completion = collectMessages writeCmd
                let requestedWithPending = { requested with RefreshPending = true }

                let finalState, finishCmd, completedMessage =
                    match completion with
                    | [| WriteCompleted(_, _, DiscardSelection _, Ok(StaleWorkspaceVersion(message, _))) |] ->
                        let nextState, command = update deps ignore completion[0] requestedWithPending
                        nextState, command, message
                    | _ -> failwith "Expected the stale discard completion."

                let! finishMessages = collectMessages finishCmd

                let refreshCount =
                    finishMessages
                    |> Array.filter (
                        function
                        | RefreshRequested -> true
                        | _ -> false
                    )
                    |> Array.length

                Vitest.expect(restoreRequests.Count).toBe (1)

                Vitest
                    .expect(completedMessage.StartsWith("The workspace changed since it was last refreshed"))
                    .toBe (true)

                Vitest.expect(finalState.BusyOperation).toEqual (None)
                Vitest.expect(finalState.ErrorNotice).toEqual (Some completedMessage)
                Vitest.expect(finalState.RefreshPending).toBe (false)
                Vitest.expect(reportedErrors.Count).toBe (1)
                Vitest.expect(refreshCount).toBe (1)
            }
        )

        Vitest.test (
            "A stale update is not repeated without a new synchronize decision",
            fun () -> promise {
                let synchronizeRequests = ResizeArray<SynchronizeRequestDto>()
                let reportedErrors = ResizeArray<GitErrorNotification>()

                let deps = {
                    defaultDependencies with
                        synchronize =
                            fun request ->
                                synchronizeRequests.Add request

                                promise {
                                    return
                                        Ok(
                                            failed
                                                Concurrency
                                                VersionControlCodes.PreconditionFailed
                                                "workspace version is stale"
                                        )
                                }
                        getStatus =
                            fun _ -> promise {
                                return
                                    Ok(
                                        succeeded {
                                            cleanStatus with
                                                WorkspaceVersion = "v2"
                                        }
                                    )
                            }
                        listRefs = fun _ -> promise { return Ok(succeeded refs) }
                        getStoragePolicySettings = fun _ -> promise { return Ok(succeeded (lfsSettings 5 true)) }
                        reportError = reportedErrors.Add
                }

                let stateAfterRequest, requestCmd =
                    update deps ignore (WriteRequested(Pull GitUpdateAcceptance.RequirePreview)) runningState

                let! completionMessages = collectMessages requestCmd

                let finalState, finishCmd, completedMessage =
                    match completionMessages with
                    | [| WriteCompleted(_, _, Pull _, Ok(StaleWorkspaceVersion(message, _))) |] ->
                        let nextState, command = update deps ignore completionMessages[0] stateAfterRequest
                        nextState, command, message
                    | _ -> failwith "Expected the stale update completion."

                let! finishMessages = collectMessages finishCmd

                Vitest.expect(synchronizeRequests.Count).toBe (1)

                Vitest
                    .expect(completedMessage.StartsWith("The workspace changed since it was last refreshed"))
                    .toBe (true)

                Vitest.expect(finalState.BusyOperation).toEqual (None)
                Vitest.expect(finalState.ErrorNotice).toEqual (Some completedMessage)
                Vitest.expect(reportedErrors.Count).toBe (1)

                Vitest
                    .expect(
                        finishMessages
                        |> Array.exists (
                            function
                            | RefreshRequested -> true
                            | _ -> false
                        )
                    )
                    .toBe (true)
            }
        )

        Vitest.test (
            "An accepted pull is not replayed when the target moved",
            fun () -> promise {
                let synchronizeRequests = ResizeArray<SynchronizeRequestDto>()
                let reportedErrors = ResizeArray<GitErrorNotification>()

                let deps = {
                    defaultDependencies with
                        synchronize =
                            fun request ->
                                synchronizeRequests.Add request

                                promise {
                                    return
                                        Ok(
                                            failed
                                                Concurrency
                                                VersionControlCodes.PreconditionFailed
                                                "The target moved after the update was accepted."
                                        )
                                }
                        getStatus =
                            fun _ -> promise {
                                return
                                    Ok(
                                        succeeded {
                                            cleanStatus with
                                                WorkspaceVersion = "v2"
                                        }
                                    )
                            }
                        listRefs = fun _ -> promise { return Ok(succeeded refs) }
                        getStoragePolicySettings = fun _ -> promise { return Ok(succeeded (lfsSettings 5 true)) }
                        reportError = reportedErrors.Add
                }

                let stateAfterRequest, requestCmd =
                    update
                        deps
                        ignore
                        (WriteRequested(Pull(GitUpdateAcceptance.Accepted("target-1", "v1"))))
                        runningState

                let! completionMessages = collectMessages requestCmd

                let finalState, finishCmd =
                    match completionMessages with
                    | [| WriteCompleted(_, _, Pull _, Ok(StaleWorkspaceVersion(message, _))) |] ->
                        let nextState, command = update deps ignore completionMessages[0] stateAfterRequest
                        nextState, command
                    | _ -> failwith "Expected the accepted pull to be rejected as stale."

                let! finishMessages = collectMessages finishCmd

                Vitest.expect(synchronizeRequests.Count).toBe (1)
                Vitest.expect(finalState.ErrorNotice.IsSome).toBe (true)
                Vitest.expect(reportedErrors.Count).toBe (1)

                let refreshCount =
                    finishMessages
                    |> Array.filter (
                        function
                        | RefreshRequested -> true
                        | _ -> false
                    )
                    |> Array.length

                Vitest.expect(refreshCount).toBe (1)

                Vitest
                    .expect(
                        finishMessages
                        |> Array.exists (
                            function
                            | WriteRequested(Pull _) -> true
                            | _ -> false
                        )
                    )
                    .toBe (false)
            }
        )

        Vitest.test (
            "An accepted push is not replayed when the target moved",
            fun () -> promise {
                let synchronizeRequests = ResizeArray<SynchronizeRequestDto>()
                let reportedErrors = ResizeArray<GitErrorNotification>()

                let staleFailure = {
                    makeFailure
                        Concurrency
                        VersionControlCodes.PreconditionFailed
                        "The target moved after the preview."
                        None
                        [||] with
                        RevisionEvidence = [|
                            {
                                Label = "expected_target"
                                Revision = "target-1"
                            }
                            {
                                Label = "observed_target"
                                Revision = "target-2"
                            }
                        |]
                }

                let deps = {
                    defaultDependencies with
                        synchronize =
                            fun request ->
                                synchronizeRequests.Add request
                                promise { return Ok(OperationResultDto.Failed staleFailure) }
                        getStatus =
                            fun _ -> promise {
                                return
                                    Ok(
                                        succeeded {
                                            cleanStatus with
                                                WorkspaceVersion = "v2"
                                        }
                                    )
                            }
                        listRefs = fun _ -> promise { return Ok(succeeded refs) }
                        getStoragePolicySettings = fun _ -> promise { return Ok(succeeded (lfsSettings 5 true)) }
                        reportError = reportedErrors.Add
                }

                let stateAfterRequest, requestCmd =
                    update
                        deps
                        ignore
                        (WriteRequested(Push(GitUpdateAcceptance.Accepted("target-1", "v1"))))
                        runningState

                let! completionMessages = collectMessages requestCmd

                let finalState, finishCmd, message =
                    match completionMessages with
                    | [| WriteCompleted(_, _, Push _, Ok(StaleWorkspaceVersion(message, _))) |] ->
                        let state, command = update deps ignore completionMessages[0] stateAfterRequest
                        state, command, message
                    | _ -> failwith "Expected the accepted push to be rejected as stale."

                let! finishMessages = collectMessages finishCmd

                let expectedMessage =
                    "The online copy changed since the preview. Review the current changes and try again."

                Vitest.expect(synchronizeRequests.Count).toBe (1)
                Vitest.expect(message).toBe (expectedMessage)
                Vitest.expect(finalState.ErrorNotice).toEqual (Some expectedMessage)
                Vitest.expect(reportedErrors.Count).toBe (1)

                Vitest
                    .expect(
                        finishMessages
                        |> Array.exists (
                            function
                            | RefreshRequested -> true
                            | _ -> false
                        )
                    )
                    .toBe (true)
            }
        )

        Vitest.test (
            "A stale finalize of a merge is not repeated",
            fun () -> promise {
                let mutable calls = 0
                let conflict = (conflictedStatus [| "conflict.txt" |]).ActiveConflictSession.Value

                let deps = {
                    defaultDependencies with
                        finalizeConflict =
                            fun _ ->
                                calls <- calls + 1

                                promise {
                                    return
                                        Ok(
                                            failed
                                                Concurrency
                                                VersionControlCodes.PreconditionFailed
                                                "workspace version is stale"
                                        )
                                }
                }

                let state = {
                    runningState with
                        ActiveConflict = Some conflict
                }

                let requested, writeCmd = update deps ignore (WriteRequested FinalizeMerge) state

                let! completion = collectMessages writeCmd

                let finalState, finishCmd =
                    match completion with
                    | [| WriteCompleted(_, _, FinalizeMerge, Ok(StaleWorkspaceVersion(_, _))) |] ->
                        update deps ignore completion[0] requested
                    | _ -> failwith "Expected the stale finalize completion."

                let! _ = collectMessages finishCmd

                Vitest.expect(calls).toBe (1)
                Vitest.expect(finalState.BusyOperation).toEqual (None)
            }
        )

        Vitest.test (
            "A refresh of the previous ARC is ignored after the path changed",
            fun () -> promise {
                let state = {
                    runningState with
                        CurrentArcPath = Some "C:/arc-a"
                        RefreshRequestId = 1
                }

                let afterPathChange, pathCmd =
                    update defaultDependencies ignore (ArcPathChanged(Some "C:/arc-b")) state

                let! pathMessages = collectMessages pathCmd

                let hasRefresh =
                    pathMessages
                    |> Array.exists (
                        function
                        | RefreshRequested -> true
                        | _ -> false
                    )

                if not hasRefresh then
                    failwith "Expected the path change to request a refresh."

                let refreshing, _ =
                    update defaultDependencies ignore RefreshRequested afterPathChange

                let finalState, _ =
                    update defaultDependencies ignore (RefreshCompleted(1, Error "old ARC failure")) refreshing

                Vitest.expect(finalState.ErrorNotice).toEqual (None)
                Vitest.expect(finalState.CurrentArcPath).toEqual (Some "C:/arc-b")
            }
        )

        Vitest.test (
            "A rename reply that arrives after the refresh finished still publishes",
            fun () -> promise {
                let state = {
                    runningState with
                        CurrentArcPath = Some "C:/renamed-arc"
                        ArcSessionId = runningState.ArcSessionId + 1
                        RefreshState = GitRefreshState.Idle
                }

                let nextState, cmd =
                    update
                        defaultDependencies
                        ignore
                        (PublishRenameCompleted(runningState.ArcSessionId, Ok "C:/renamed-arc"))
                        state

                let! messages = collectMessages cmd

                Vitest.expect(nextState.PendingPublishAfterRefresh).toBe (true)

                Vitest
                    .expect(
                        messages
                        |> Array.exists (
                            function
                            | RefreshRequested -> true
                            | _ -> false
                        )
                    )
                    .toBe (true)
            }
        )

        Vitest.test (
            "A rename reply for the current path under another spelling asks for a refresh",
            fun () -> promise {
                let model = {
                    runningState with
                        CurrentArcPath = Some "C:/arc"
                        BusyOperation = Some GitBusyOperation.RenamingRepository
                }

                let nextState, command =
                    update
                        defaultDependencies
                        ignore
                        (PublishRenameCompleted(model.ArcSessionId - 1, Ok "C:\\arc"))
                        model

                let! messages = collectMessages command

                Vitest.expect(nextState.PendingPublishAfterRefresh).toBe (true)
                Vitest.expect(messages).toEqual ([| RefreshRequested |])
            }
        )

        Vitest.test (
            "A stale rename reply does not clear a running write",
            fun () -> promise {
                let model = {
                    runningState with
                        ArcSessionId = runningState.ArcSessionId + 1
                        BusyOperation = Some GitBusyOperation.PushingToRemote
                        BusyNotice = Some "Pushing to remote"
                        CurrentOperation =
                            Some {
                                SessionId = "s-2"
                                OperationId = "write-op"
                            }
                }

                let nextState, command =
                    update
                        defaultDependencies
                        ignore
                        (PublishRenameCompleted(model.ArcSessionId - 1, Ok "C:\\arc"))
                        model

                let! messages = collectMessages command

                Vitest.expect(nextState.BusyOperation).toEqual (model.BusyOperation)
                Vitest.expect(nextState.BusyNotice).toEqual (model.BusyNotice)
                Vitest.expect(nextState.CurrentOperation).toEqual (model.CurrentOperation)
                Vitest.expect(nextState.PendingPublishAfterRefresh).toBe (true)
                Vitest.expect(messages).toEqual ([| RefreshRequested |])
            }
        )

        Vitest.test (
            "A dependency install becomes the cancel target while it runs",
            fun () -> promise {
                let requests = ResizeArray<InstallDependencyRequestDto>()

                let deps = {
                    defaultDependencies with
                        installDependency =
                            fun request ->
                                requests.Add request

                                promise {
                                    return
                                        Ok(
                                            succeeded {
                                                Component = "git-lfs-configuration"
                                                Installed = true
                                                Version = None
                                                Compatible = true
                                                Remediation = None
                                            }
                                        )
                                }
                }

                let nextState, cmd =
                    update
                        deps
                        ignore
                        (WriteInstallPromptAnswered(
                            runningState.ArcSessionId,
                            Push GitUpdateAcceptance.RequirePreview,
                            "git-lfs-configuration",
                            true
                        ))
                        runningState

                let! _ = collectMessages cmd

                Vitest.expect(requests.Count).toBe (1)

                Vitest
                    .expect(nextState.CurrentOperation |> Option.map _.OperationId)
                    .toEqual (Some requests[0].OperationId)
            }
        )

        Vitest.test (
            "A stale abandon of a merge is not repeated",
            fun () -> promise {
                let mutable calls = 0
                let conflict = (conflictedStatus [| "conflict.txt" |]).ActiveConflictSession.Value

                let deps = {
                    defaultDependencies with
                        cancelConflict =
                            fun _ ->
                                calls <- calls + 1

                                promise {
                                    return
                                        Ok(
                                            failed
                                                Concurrency
                                                VersionControlCodes.PreconditionFailed
                                                "workspace version is stale"
                                        )
                                }
                }

                let state = {
                    runningState with
                        ActiveConflict = Some conflict
                }

                let requested, writeCmd = update deps ignore (WriteRequested AbandonMerge) state

                let! completion = collectMessages writeCmd

                let finalState, finishCmd =
                    match completion with
                    | [| WriteCompleted(_, _, AbandonMerge, Ok(StaleWorkspaceVersion(_, _))) |] ->
                        update deps ignore completion[0] requested
                    | _ -> failwith "Expected the stale abandon completion."

                let! _ = collectMessages finishCmd

                Vitest.expect(calls).toBe (1)
                Vitest.expect(finalState.BusyOperation).toEqual (None)
            }
        )

        Vitest.test (
            "A stale restore of interrupted paths is not repeated",
            fun () -> promise {
                let affectedPaths = [| "a.txt" |]
                let mutable restoreCalls = 0

                let canceled =
                    makeFailure
                        Canceled
                        VersionControlCodes.OperationCanceled
                        "Update was canceled."
                        (Some {
                            Code = VersionControlCodes.Recovery.RestoreWorkspace
                            Instructions = Some "Restore the interrupted files."
                        })
                        affectedPaths

                let deps = {
                    defaultDependencies with
                        synchronize = fun _ -> promise { return Ok(OperationResultDto.Failed canceled) }
                        restorePaths =
                            fun _ ->
                                restoreCalls <- restoreCalls + 1

                                promise {
                                    return
                                        Ok(
                                            failed
                                                Concurrency
                                                VersionControlCodes.PreconditionFailed
                                                "workspace version is stale"
                                        )
                                }
                        getStatus = fun _ -> promise { return Ok(succeeded cleanStatus) }
                        listRefs = fun _ -> promise { return Ok(succeeded refs) }
                        getStoragePolicySettings = fun _ -> promise { return Ok(succeeded (lfsSettings 5 true)) }
                }

                let stateAfterRequest, requestCmd =
                    update deps ignore (WriteRequested(Pull GitUpdateAcceptance.RequirePreview)) runningState

                let! completionMessages = collectMessages requestCmd

                let recoveryState, recoveryCmd =
                    match completionMessages with
                    | [| WriteCompleted(_,
                                        _,
                                        Pull _,
                                        Ok(RequiresRecovery(GitPendingRecovery.RestoreInterruptedPaths _, _))) |] ->
                        update deps ignore completionMessages[0] stateAfterRequest
                    | _ -> failwith "Expected the restore recovery."

                let! _ = collectMessages recoveryCmd

                let confirmedState, confirmCmd =
                    update deps ignore ConfirmPendingRemoteActionRequested recoveryState

                let! confirmMessages = collectMessages confirmCmd

                let restoreState, restoreCmd =
                    match confirmMessages with
                    | [| RestoreInterruptedPathsRequested |] -> update deps ignore confirmMessages[0] confirmedState
                    | _ -> failwith "Expected the restore confirmation."

                let! restoreMessages = collectMessages restoreCmd

                let requestedState, writeCmd =
                    match restoreMessages with
                    | [| WriteRequested(RestoreInterruptedPaths [| "a.txt" |]) |] ->
                        update deps ignore restoreMessages[0] restoreState
                    | _ -> failwith "Expected the restore write request."

                let! completion = collectMessages writeCmd

                let finalState, finishCmd =
                    match completion with
                    | [| WriteCompleted(_, _, RestoreInterruptedPaths _, Ok(StaleWorkspaceVersion(_, _))) |] ->
                        update deps ignore completion[0] requestedState
                    | _ -> failwith "Expected the stale restore completion."

                let! _ = collectMessages finishCmd

                Vitest.expect(restoreCalls).toBe (1)
                Vitest.expect(finalState.BusyOperation).toEqual (None)
            }
        )

        Vitest.test (
            "A stale token does not replay a branch switch",
            fun () -> promise {
                let requests = ResizeArray<SwitchRefRequestDto>()
                let mutable preflightCalls = 0

                let deps = {
                    defaultDependencies with
                        preflightSwitchRef =
                            fun _ ->
                                preflightCalls <- preflightCalls + 1
                                promise { return Ok(succeeded { PathsAtRisk = [||]; IsSafe = true }) }
                        switchRef =
                            fun request ->
                                requests.Add request

                                if requests.Count = 1 then
                                    promise {
                                        return
                                            Ok(
                                                failed
                                                    Concurrency
                                                    VersionControlCodes.PreconditionFailed
                                                    "workspace version is stale"
                                            )
                                    }
                                else
                                    promise { return Ok(succeeded cleanStatus) }
                        getStatus =
                            fun _ -> promise {
                                return
                                    Ok(
                                        succeeded {
                                            cleanStatus with
                                                WorkspaceVersion = "v2"
                                        }
                                    )
                            }
                        listRefs = fun _ -> promise { return Ok(succeeded refs) }
                        getStoragePolicySettings = fun _ -> promise { return Ok(succeeded (lfsSettings 5 true)) }
                }

                let state = {
                    runningState with
                        Refs = [| localBranch "feature" false false |]
                }

                let model, command = update deps ignore (SwitchBranchRequested "feature") state
                let! messages = collectMessages command
                let preflightState, writeRequest = update deps ignore messages[0] model
                let! writeMessages = collectMessages writeRequest
                let requested, writeCmd = update deps ignore writeMessages[0] preflightState
                let! completion = collectMessages writeCmd

                let finalState, finishCmd =
                    match completion with
                    | [| WriteCompleted(_, _, SwitchBranch "feature", Ok(StaleWorkspaceVersion(message, false))) |] ->
                        Vitest
                            .expect(message)
                            .toBe (
                                "The workspace changed since it was last refreshed, so the action was not repeated. Review the current changes and try again."
                            )

                        update deps ignore completion[0] requested
                    | _ -> failwith "Expected the stale branch switch completion."

                let! _ = collectMessages finishCmd

                Vitest.expect(requests.Count).toBe (1)
                Vitest.expect(preflightCalls).toBe (1)

                Vitest
                    .expect(finalState.ErrorNotice)
                    .toEqual (
                        Some
                            "The workspace changed since it was last refreshed, so the action was not repeated. Review the current changes and try again."
                    )
            }
        )

        Vitest.test (
            "A conflict session reported by a commit opens the first conflicted item",
            fun () -> promise {
                let conflicted = conflictedStatus [| "conflict.txt" |]

                let deps = {
                    defaultDependencies with
                        createRevision =
                            fun _ -> promise {
                                return
                                    Ok(
                                        failed
                                            Conflict
                                            VersionControlCodes.ConflictSessionActive
                                            "a conflict session is open"
                                    )
                            }
                        getStatus = fun _ -> promise { return Ok(succeeded conflicted) }
                        listRefs = fun _ -> promise { return Ok(succeeded refs) }
                        getStoragePolicySettings = fun _ -> promise { return Ok(succeeded (lfsSettings 5 true)) }
                        loadConflictPage = fun _ _ path -> promise { return Ok(diffPage path) }
                }

                let state = {
                    runningState with
                        ChangedFiles = [| changedFile "a.txt" "M" " " false |]
                }

                let model, command = update deps ignore (CommitAllRequested "save") state
                let! messages = collectMessages command
                let requested, writeCmd = update deps ignore messages[0] model
                let! completion = collectMessages writeCmd

                let finalState, _ =
                    match completion with
                    | [| WriteCompleted(_, _, CommitAll _, Ok(Completed(UnitSuccess success))) |] ->
                        match success.SelectedChangePath, success.PageChange with
                        | Some(Some "conflict.txt"), GitPageChange.Set _ -> update deps ignore completion[0] requested
                        | _ -> failwith "Expected the conflict page of the first conflicted item."
                    | _ -> failwith "Expected the conflict page of the first conflicted item."

                Vitest.expect(finalState.ActiveConflict.IsSome).toBe (true)
                Vitest.expect(finalState.SelectedChangePath).toEqual (Some "conflict.txt")
                Vitest.expect(finalState.ErrorNotice).toEqual (None)
            }
        )

        Vitest.test (
            "A primary save whose synchronize opens a conflict session keeps the publish for after the merge",
            fun () -> promise {
                let conflicted = conflictedStatus [| "conflict.txt" |]
                let sync = cleanStatus.Synchronization.Value
                let mutable synchronized = false
                let mutable mergeFinalized = false
                let conflict = conflicted.ActiveConflictSession.Value

                let failure =
                    makeFailure
                        Conflict
                        VersionControlCodes.ConflictsDetected
                        "conflicts detected"
                        (Some {
                            Code = VersionControlCodes.Recovery.ResolveConflictSession
                            Instructions = None
                        })
                        [| "conflict.txt" |]

                let deps = {
                    defaultDependencies with
                        createRevision = fun _ -> promise { return Ok(succeeded "revision-1") }
                        synchronize =
                            fun _ ->
                                synchronized <- true
                                promise { return Ok(OperationResultDto.PartiallySucceeded(operation sync, failure)) }
                        getStatus =
                            fun _ -> promise {
                                return
                                    Ok(
                                        succeeded (
                                            if synchronized && not mergeFinalized then
                                                conflicted
                                            else
                                                statusForBranch "main"
                                        )
                                    )
                            }
                        listRefs = fun _ -> promise { return Ok(succeeded [| localBranch "main" true true |]) }
                        getStoragePolicySettings = fun _ -> promise { return Ok(succeeded (lfsSettings 5 true)) }
                        loadConflictPage = fun _ _ path -> promise { return Ok(diffPage path) }
                        resolveConflict =
                            fun _ -> promise {
                                return
                                    Ok(
                                        succeeded {
                                            RefreshedHandle = {
                                                SessionId = "conflict-2"
                                                Version = "2"
                                            }
                                            RemainingItems = [||]
                                        }
                                    )
                            }
                        finalizeConflict =
                            fun _ ->
                                mergeFinalized <- true
                                promise { return Ok(succeeded (Some "rev")) }
                }

                let state = {
                    runningState with
                        ChangedFiles = [| changedFile "a.txt" "M" " " false |]
                }

                let model, command = update deps ignore (PrimarySaveAllRequested "save") state
                let! messages = collectMessages command
                let requested, writeCmd = update deps ignore messages[0] model
                let! completion = collectWriteMessages writeCmd

                let finalState, finishCmd =
                    match completion with
                    | [| WriteCompleted(_, _, PrimarySave _, Ok(Completed(UnitSuccess success))) |] ->
                        match success.PageChange with
                        | GitPageChange.Set _ -> update deps ignore completion[0] requested
                        | _ -> failwith "Expected the conflict page after the update inside the save."
                    | _ -> failwith "Expected the conflict page after the update inside the save."

                let! finishMessages = collectMessages finishCmd

                Vitest.expect(finalState.ActiveConflict.IsSome).toBe (true)
                Vitest.expect(finalState.PendingPostMergePush).toBe (true)

                Vitest
                    .expect(finalState.WarningNotice)
                    .toEqual (Some "Changes were saved locally. Online sync is still pending.")

                Vitest.expect(finishMessages).toEqual ([||])

                let mergeRequest = {
                    Path = "conflict.txt"
                    Handle = conflict.Handle
                    WorkspaceVersion = "v1"
                    ResolvedContent = "resolved"
                }

                let mergeState, mergeCmd =
                    update deps ignore (ConfirmMergeResolutionRequested mergeRequest) finalState

                let! mergeCompletions = collectMessages mergeCmd

                let finalizedState, finalizeCmd =
                    match mergeCompletions with
                    | [| ConfirmMergeResolutionCompleted(_, Ok outcome) |] ->
                        Vitest.expect(outcome.Finalized).toBe (true)
                        update deps ignore mergeCompletions[0] mergeState
                    | _ -> failwith "Expected merge resolution to complete."

                let! finalMessages = collectMessages finalizeCmd
                Vitest.expect(finalizedState.PendingPostMergePush).toBe (false)

                Vitest
                    .expect(finalMessages)
                    .toEqual (
                        [|
                            WriteRequested(Push GitUpdateAcceptance.RequirePreview)
                        |]
                    )
            }
        )

        Vitest.test (
            "Cancel before the started event uses the id allocated at request time",
            fun () -> promise {
                let publishIds = ResizeArray<string>()
                let cancelKeys = ResizeArray<OperationKeyDto>()
                let mutable ids = 0

                let deps = {
                    defaultDependencies with
                        newOperationId =
                            fun () ->
                                ids <- ids + 1
                                $"op-{ids}"
                        synchronize =
                            fun request ->
                                publishIds.Add request.OperationId
                                promise { return Ok(succeeded cleanStatus.Synchronization.Value) }
                        cancelOperation =
                            fun key ->
                                cancelKeys.Add key
                                promise { return Ok true }
                        getStatus = fun _ -> promise { return Ok(succeeded cleanStatus) }
                        listRefs = fun _ -> promise { return Ok(succeeded refs) }
                        getStoragePolicySettings = fun _ -> promise { return Ok(succeeded (lfsSettings 5 true)) }
                }

                let requested, writeCmd =
                    update deps ignore (WriteRequested(Push GitUpdateAcceptance.RequirePreview)) runningState

                let canceling, cancelCmd =
                    update deps ignore CancelCurrentOperationRequested requested

                let! cancelMessages = collectMessages cancelCmd
                let! _ = collectMessages writeCmd

                Vitest.expect(requested.CurrentOperation).toEqual (Some { SessionId = ""; OperationId = "op-1" })
                Vitest.expect(canceling.WarningNotice).toEqual (None)
                Vitest.expect(cancelKeys |> Seq.toArray).toEqual ([| { SessionId = ""; OperationId = "op-1" } |])
                Vitest.expect(publishIds |> Seq.toArray).toEqual ([| "op-1" |])

                Vitest
                    .expect(cancelMessages)
                    .toEqual (
                        [|
                            CancelCurrentOperationCompleted(1, { SessionId = ""; OperationId = "op-1" }, Ok true)
                        |]
                    )
            }
        )

        Vitest.test (
            "A refused cancel leaves the current notices unchanged",
            fun () -> promise {
                let reportedErrors = ResizeArray<GitErrorNotification>()

                let deps = {
                    defaultDependencies with
                        reportError = reportedErrors.Add
                }

                let state = {
                    runningState with
                        BusyOperation = Some GitBusyOperation.PushingToRemote
                        CurrentOperation = Some { SessionId = ""; OperationId = "op-1" }
                        WarningNotice = Some "existing warning"
                        ErrorNotice = Some "existing error"
                }

                let nextState, cmd =
                    update
                        deps
                        ignore
                        (CancelCurrentOperationCompleted(state.ArcSessionId, state.CurrentOperation.Value, Ok false))
                        state

                let! _ = collectMessages cmd

                Vitest.expect(nextState.WarningNotice).toEqual (Some "existing warning")
                Vitest.expect(nextState.ErrorNotice).toEqual (Some "existing error")
                Vitest.expect(reportedErrors.Count).toBe (0)
            }
        )

        Vitest.test (
            "A late cancel reply from another operation is ignored",
            fun () -> promise {
                let currentKey = {
                    SessionId = "s-1"
                    OperationId = "op-1/1"
                }

                let state = {
                    runningState with
                        CurrentOperation = Some currentKey
                        ErrorNotice = Some "current error"
                        WarningNotice = Some "current warning"
                }

                let nextState, cmd =
                    update
                        defaultDependencies
                        ignore
                        (CancelCurrentOperationCompleted(
                            state.ArcSessionId,
                            {
                                SessionId = "s-1"
                                OperationId = "op-2/1"
                            },
                            Error "late failure"
                        ))
                        state

                let! messages = collectMessages cmd

                Vitest.expect(nextState).toEqual (state)
                Vitest.expect(messages).toEqual ([||])
            }
        )

        Vitest.test (
            "A cancel error reply survives the started event",
            fun () -> promise {
                let reportedErrors = ResizeArray<GitErrorNotification>()

                let deps = {
                    defaultDependencies with
                        reportError = reportedErrors.Add
                }

                let model = {
                    runningState with
                        CurrentOperation =
                            Some {
                                SessionId = "s-1"
                                OperationId = "op-1/1"
                            }
                }

                let nextState, command =
                    update
                        deps
                        ignore
                        (CancelCurrentOperationCompleted(
                            model.ArcSessionId,
                            { SessionId = ""; OperationId = "op-1" },
                            Error "boom"
                        ))
                        model

                let! _ = collectMessages command

                Vitest.expect(nextState.ErrorNotice).toEqual (Some "boom")
                Vitest.expect(reportedErrors.Count).toBe (1)
            }
        )

        Vitest.test (
            "Primary save without an online service stops after the local commit",
            fun () -> promise {
                let localOnly = {
                    sessionInfo with
                        Services = {
                            sessionInfo.Services with
                                Synchronization = false
                        }
                }

                let deps = {
                    defaultDependencies with
                        getSessionInfo = fun _ -> promise { return Ok(succeeded localOnly) }
                        createRevision = fun _ -> promise { return Ok(succeeded "revision-1") }
                        getStatus = fun _ -> promise { return Ok(succeeded cleanStatus) }
                        listRefs = fun _ -> promise { return Ok(succeeded refs) }
                        getStoragePolicySettings = fun _ -> promise { return Ok(succeeded (lfsSettings 5 true)) }
                }

                let state = {
                    runningState with
                        ChangedFiles = [| changedFile "a.txt" "M" " " false |]
                }

                let model, command = update deps ignore (PrimarySaveAllRequested "save") state
                let! messages = collectMessages command
                let requested, writeCmd = update deps ignore messages[0] model
                let! completion = collectMessages writeCmd

                let finalState, _ =
                    match completion with
                    | [| WriteCompleted(_, _, PrimarySave _, Ok(Completed _)) |] ->
                        update deps ignore completion[0] requested
                    | _ -> failwith "Expected the local save to complete."

                Vitest.expect(finalState.ErrorNotice).toEqual (None)

                Vitest
                    .expect(finalState.WarningNotice)
                    .toEqual (
                        Some
                            "Changes were saved locally. This workspace provider does not support online synchronization."
                    )
            }
        )

        Vitest.test (
            "A synchronize whose bind fails resumes without creating a second project",
            fun () -> promise {
                let sync = cleanStatus.Synchronization.Value
                let mutable projectCalls = 0
                let mutable bindCalls = 0
                let mutable publishCalls = 0

                let deps = {
                    defaultDependencies with
                        synchronize =
                            fun _ ->
                                publishCalls <- publishCalls + 1

                                if publishCalls = 1 then
                                    promise {
                                        return
                                            Ok(
                                                failed
                                                    Validation
                                                    VersionControlCodes.PublishTargetMissing
                                                    "publish target missing"
                                            )
                                    }
                                else
                                    promise { return Ok(succeeded sync) }
                        createRemoteProject =
                            fun _ ->
                                projectCalls <- projectCalls + 1
                                promise { return Ok remoteProject }
                        bindWorkspace =
                            fun _ ->
                                bindCalls <- bindCalls + 1

                                if bindCalls = 1 then
                                    promise { return Ok(failed ProviderError "bind_failed" "bind failed") }
                                else
                                    promise { return Ok(succeeded sessionInfo) }
                        getStatus = fun _ -> promise { return Ok(succeeded cleanStatus) }
                        listRefs = fun _ -> promise { return Ok(succeeded refs) }
                        getStoragePolicySettings = fun _ -> promise { return Ok(succeeded (lfsSettings 5 true)) }
                }

                let requested, writeCmd =
                    update deps ignore (WriteRequested(Push GitUpdateAcceptance.RequirePreview)) runningState

                let! completion = collectMessages writeCmd

                let incomplete, incompleteCmd =
                    match completion with
                    | [| WriteCompleted(_, _, Push _, Ok(ProvisioningIncomplete _)) |] ->
                        update deps ignore completion[0] requested
                    | _ -> failwith "Expected the failed bind to leave the provisioning incomplete."

                let! incompleteMessages = collectMessages incompleteCmd

                let refreshing, refreshCmd =
                    match incompleteMessages with
                    | [| RefreshRequested |] -> update deps ignore RefreshRequested incomplete
                    | _ -> failwith "Expected a refresh after the incomplete provisioning."

                let! refreshMessages = collectMessages refreshCmd
                let refreshed, _ = update deps ignore refreshMessages[0] refreshing

                let retried, retryCmd =
                    update deps ignore (WriteRequested(Push GitUpdateAcceptance.RequirePreview)) refreshed

                let! retryCompletion = collectMessages retryCmd

                let finalState, _ =
                    match retryCompletion with
                    | [| WriteCompleted(_, _, Push _, Ok(Completed _)) |] ->
                        update deps ignore retryCompletion[0] retried
                    | _ -> failwith "Expected the retried publish to complete."

                Vitest
                    .expect(incomplete.ProvisionedRemote)
                    .toEqual (
                        Some {
                            RemoteUrl = remoteProject.http_url_to_repo
                            ProjectName = "arc"
                            IsBound = false
                        }
                    )

                Vitest.expect(incomplete.ErrorNotice).toEqual (Some "bind failed")
                Vitest.expect(refreshed.WarningNotice).toEqual (Some "bind failed")
                Vitest.expect(projectCalls).toBe (1)
                Vitest.expect(bindCalls).toBe (2)
                Vitest.expect(publishCalls).toBe (2)
                Vitest.expect(finalState.ProvisionedRemote).toEqual (None)
                Vitest.expect(finalState.ErrorNotice).toEqual (None)
            }
        )

        Vitest.test (
            "A recovery offer does not start a refresh, dismissing it does",
            fun () -> promise {
                let canceled =
                    makeFailure
                        Canceled
                        VersionControlCodes.OperationCanceled
                        "Update was canceled."
                        (Some {
                            Code = VersionControlCodes.Recovery.RestoreWorkspace
                            Instructions = None
                        })
                        [| "a.txt" |]

                let deps = {
                    defaultDependencies with
                        synchronize = fun _ -> promise { return Ok(OperationResultDto.Failed canceled) }
                }

                let requested, writeCmd =
                    update deps ignore (WriteRequested(Pull GitUpdateAcceptance.RequirePreview)) runningState

                let! completion = collectMessages writeCmd

                let offered, offerCmd =
                    match completion with
                    | [| WriteCompleted(_,
                                        _,
                                        Pull _,
                                        Ok(RequiresRecovery(GitPendingRecovery.RestoreInterruptedPaths _, _))) |] ->
                        update deps ignore completion[0] requested
                    | _ -> failwith "Expected the restore offer."

                let! offerMessages = collectMessages offerCmd

                let canceling, cancelCmd =
                    update deps ignore CancelPendingRemoteActionRequested offered

                let! cancelMessages = collectMessages cancelCmd

                let dismissed, dismissCmd =
                    match cancelMessages with
                    | [| DismissRecoveryRequested |] -> update deps ignore DismissRecoveryRequested canceling
                    | _ -> failwith "Expected the dismissal."

                let! dismissMessages = collectMessages dismissCmd

                Vitest.expect(offered.BusyOperation).toEqual (None)
                Vitest.expect(offered.PendingConfirmation.IsSome).toBe (true)
                Vitest.expect(offerMessages).toEqual ([||])
                Vitest.expect(dismissed.PendingRecovery).toEqual (None)
                Vitest.expect(dismissMessages).toEqual ([| RefreshRequested |])
            }
        )

        Vitest.test (
            "A refresh requested during a write does not swallow a confirmed recovery",
            fun () -> promise {
                let canceled =
                    makeFailure
                        Canceled
                        VersionControlCodes.OperationCanceled
                        "restore"
                        (Some {
                            Code = VersionControlCodes.Recovery.RestoreWorkspace
                            Instructions = None
                        })
                        [| "a.txt" |]

                let mutable restoredPaths = None

                let deps = {
                    defaultDependencies with
                        refreshSynchronization = fun _ -> promise { return Ok(OperationResultDto.Failed canceled) }
                        restorePaths =
                            fun request ->
                                restoredPaths <- Some request.Paths
                                promise { return Ok(succeeded ()) }
                        getStatus = fun _ -> promise { return Ok(succeeded cleanStatus) }
                        listRefs = fun _ -> promise { return Ok(succeeded refs) }
                        getStoragePolicySettings = fun _ -> promise { return Ok(succeeded (lfsSettings 5 true)) }
                }

                let stateAfterRequest, requestCmd =
                    update deps ignore (WriteRequested Fetch) runningState

                let pendingState, pendingCmd = update deps ignore RefreshRequested stateAfterRequest
                let! pendingMessages = collectMessages pendingCmd
                let! completionMessages = collectMessages requestCmd

                let recoveryState, recoveryCmd =
                    match completionMessages with
                    | [| WriteCompleted(_,
                                        _,
                                        Fetch,
                                        Ok(RequiresRecovery(GitPendingRecovery.RestoreInterruptedPaths {
                                                                                                           AffectedPaths = [| "a.txt" |]
                                                                                                           Instructions = None
                                                                                                       },
                                                            "restore"))) |] ->
                        update deps ignore completionMessages[0] pendingState
                    | _ -> failwith "Expected restore recovery after the canceled fetch."

                let! recoveryMessages = collectMessages recoveryCmd

                let confirmedState, confirmCmd =
                    update deps ignore ConfirmPendingRemoteActionRequested recoveryState

                let! confirmMessages = collectMessages confirmCmd

                let intermediateState, intermediateCmd =
                    match confirmMessages with
                    | [| RestoreInterruptedPathsRequested |] -> update deps ignore confirmMessages[0] confirmedState
                    | _ -> failwith "Expected the confirmed recovery to dispatch the restore request."

                let! intermediateMessages = collectMessages intermediateCmd

                let restoreRequestedState, restoreWriteCmd =
                    match intermediateMessages with
                    | [| WriteRequested(RestoreInterruptedPaths [| "a.txt" |]); RefreshRequested |] ->
                        update deps ignore intermediateMessages[0] intermediateState
                    | _ -> failwith "Expected the restore write and its queued refresh."

                let heldState, heldRefreshCmd =
                    update deps ignore intermediateMessages[1] restoreRequestedState

                let! heldRefreshMessages = collectMessages heldRefreshCmd
                let! _ = collectMessages restoreWriteCmd

                Vitest.expect(pendingMessages).toEqual ([||])
                Vitest.expect(recoveryMessages).toEqual ([||])
                Vitest.expect(recoveryState.RefreshPending).toBe (true)
                Vitest.expect(recoveryState.PendingConfirmation.IsSome).toBe (true)
                Vitest.expect(heldRefreshMessages).toEqual ([||])
                Vitest.expect(heldState.BusyOperation).toEqual (Some GitBusyOperation.RestoringInterruptedPaths)
                Vitest.expect(heldState.RefreshPending).toBe (true)
                Vitest.expect(restoredPaths).toEqual (Some [| "a.txt" |])
            }
        )

        Vitest.test (
            "Dismissing a recovery offer with a pending refresh refreshes once",
            fun () -> promise {
                let canceled =
                    makeFailure
                        Canceled
                        VersionControlCodes.OperationCanceled
                        "restore"
                        (Some {
                            Code = VersionControlCodes.Recovery.RestoreWorkspace
                            Instructions = None
                        })
                        [| "a.txt" |]

                let deps = {
                    defaultDependencies with
                        refreshSynchronization = fun _ -> promise { return Ok(OperationResultDto.Failed canceled) }
                }

                let stateAfterRequest, requestCmd =
                    update deps ignore (WriteRequested Fetch) runningState

                let pendingState, pendingCmd = update deps ignore RefreshRequested stateAfterRequest
                let! pendingMessages = collectMessages pendingCmd
                let! completionMessages = collectMessages requestCmd

                let recoveryState, recoveryCmd =
                    match completionMessages with
                    | [| WriteCompleted(_,
                                        _,
                                        Fetch,
                                        Ok(RequiresRecovery(GitPendingRecovery.RestoreInterruptedPaths _, "restore"))) |] ->
                        update deps ignore completionMessages[0] pendingState
                    | _ -> failwith "Expected restore recovery after the canceled fetch."

                let! recoveryMessages = collectMessages recoveryCmd

                let cancelingState, cancelCmd =
                    update deps ignore CancelPendingRemoteActionRequested recoveryState

                let! cancelMessages = collectMessages cancelCmd

                let dismissedState, dismissCmd =
                    match cancelMessages with
                    | [| DismissRecoveryRequested |] -> update deps ignore cancelMessages[0] cancelingState
                    | _ -> failwith "Expected dismissal to dispatch DismissRecoveryRequested."

                let! dismissMessages = collectMessages dismissCmd

                let refreshCount =
                    Array.append cancelMessages dismissMessages
                    |> Array.filter (
                        function
                        | RefreshRequested -> true
                        | _ -> false
                    )
                    |> Array.length

                Vitest.expect(pendingMessages).toEqual ([||])
                Vitest.expect(recoveryMessages).toEqual ([||])
                Vitest.expect(dismissedState.RefreshPending).toBe (false)
                Vitest.expect(refreshCount).toBe (1)
            }
        )

        Vitest.test (
            "A refresh requested during the save's refresh phase waits for the write",
            fun () -> promise {
                let writeKey = {
                    SessionId = "save-session"
                    OperationId = "save-op"
                }

                let savingState = {
                    runningState with
                        BusyOperation = Some GitBusyOperation.Refreshing
                        BusyNotice = Some "Refreshing"
                        CurrentOperation = Some writeKey
                }

                let pendingState, pendingCmd =
                    update defaultDependencies ignore RefreshRequested savingState

                let! pendingMessages = collectMessages pendingCmd

                Vitest.expect(pendingState.RefreshPending).toBe (true)
                Vitest.expect(pendingMessages).toEqual ([||])

                let deps = {
                    defaultDependencies with
                        getStatus = fun _ -> promise { return Ok(succeeded cleanStatus) }
                        listRefs = fun _ -> promise { return Ok(succeeded refs) }
                        getStoragePolicySettings = fun _ -> promise { return Ok(succeeded (lfsSettings 5 true)) }
                }

                let standaloneState = {
                    runningState with
                        BusyOperation = Some GitBusyOperation.Refreshing
                        BusyNotice = Some "Refreshing"
                        CurrentOperation = None
                }

                let _, standaloneCmd = update deps ignore RefreshRequested standaloneState
                let! standaloneMessages = collectMessages standaloneCmd

                match standaloneMessages with
                | [| RefreshCompleted(_, Ok _) |] -> ()
                | _ -> failwith "Expected a standalone refresh to produce RefreshCompleted."
            }
        )

        Vitest.test (
            "A refresh requested during a write runs when the write completes",
            fun () -> promise {
                let deps = {
                    defaultDependencies with
                        refreshSynchronization =
                            fun _ -> promise { return Ok(succeeded cleanStatus.Synchronization.Value) }
                        getStatus = fun _ -> promise { return Ok(succeeded cleanStatus) }
                        listRefs = fun _ -> promise { return Ok(succeeded refs) }
                        getStoragePolicySettings = fun _ -> promise { return Ok(succeeded (lfsSettings 5 true)) }
                }

                let stateAfterRequest, requestCmd =
                    update deps ignore (WriteRequested Fetch) runningState

                let pendingState, pendingCmd = update deps ignore RefreshRequested stateAfterRequest
                let! pendingMessages = collectMessages pendingCmd
                let! completionMessages = collectMessages requestCmd

                let finalState, finishCmd =
                    match completionMessages with
                    | [| WriteCompleted(_, _, Fetch, Ok(Completed _)) |] ->
                        update deps ignore completionMessages[0] pendingState
                    | _ -> failwith "Expected the fetch to complete successfully."

                let! finishMessages = collectMessages finishCmd

                Vitest.expect(pendingMessages).toEqual ([||])
                Vitest.expect(pendingState.RefreshPending).toBe (true)
                Vitest.expect(finalState.RefreshPending).toBe (false)

                Vitest
                    .expect(
                        finishMessages
                        |> Array.exists (
                            function
                            | RefreshRequested -> true
                            | _ -> false
                        )
                    )
                    .toBe (true)
            }
        )

        Vitest.test (
            "A refresh requested during a write waits until the recovery offer is resolved",
            fun () -> promise {
                let deps = {
                    defaultDependencies with
                        refreshSynchronization =
                            fun _ -> promise {
                                return
                                    Ok(
                                        OperationResultDto.Failed(
                                            makeFailure
                                                Canceled
                                                VersionControlCodes.OperationCanceled
                                                "lock"
                                                (Some {
                                                    Code = VersionControlCodes.Recovery.RemoveIndexLock
                                                    Instructions = None
                                                })
                                                [||]
                                        )
                                    )
                            }
                }

                let stateAfterRequest, requestCmd =
                    update deps ignore (WriteRequested Fetch) runningState

                let pendingState, pendingCmd = update deps ignore RefreshRequested stateAfterRequest
                let! pendingMessages = collectMessages pendingCmd
                let! completionMessages = collectMessages requestCmd

                let recoveryState, finishCmd =
                    match completionMessages with
                    | [| WriteCompleted(_,
                                        _,
                                        Fetch,
                                        Ok(RequiresRecovery(GitPendingRecovery.ClearStaleLock None, "lock"))) |] ->
                        update deps ignore completionMessages[0] pendingState
                    | _ -> failwith "Expected the fetch to offer lock recovery."

                let! finishMessages = collectMessages finishCmd

                let cancelingState, cancelCmd =
                    update deps ignore CancelPendingRemoteActionRequested recoveryState

                let! cancelMessages = collectMessages cancelCmd

                let dismissedState, dismissCmd =
                    match cancelMessages with
                    | [| DismissRecoveryRequested |] -> update deps ignore cancelMessages[0] cancelingState
                    | _ -> failwith "Expected recovery dismissal."

                let! dismissMessages = collectMessages dismissCmd

                let refreshCount =
                    Array.append cancelMessages dismissMessages
                    |> Array.filter (
                        function
                        | RefreshRequested -> true
                        | _ -> false
                    )
                    |> Array.length

                Vitest.expect(pendingMessages).toEqual ([||])
                Vitest.expect(pendingState.RefreshPending).toBe (true)
                Vitest.expect(finishMessages).toEqual ([||])
                Vitest.expect(recoveryState.RefreshPending).toBe (true)
                Vitest.expect(recoveryState.PendingConfirmation.IsSome).toBe (true)
                Vitest.expect(dismissedState.RefreshPending).toBe (false)

                Vitest.expect(refreshCount).toBe (1)
            }
        )
)

Vitest.describe (
    "GitWorkflow renderer behavior",
    fun () ->
        let _ =
            Vitest.test (
                "applyStatus keeps the selected path clear when the viewed file disappears from changed files",
                fun () ->
                    let model = {
                        runningState with
                            SelectedChangePath = Some "x"
                            ChangedFiles = [| changedFile "x" "M" " " false |]
                    }

                    Vitest.expect((applyStatus cleanStatus model).SelectedChangePath).toEqual (None)
            )

        let _ =
            Vitest.test (
                "GitState.Empty defaults DownloadLargeFiles to false until settings load",
                fun () -> Vitest.expect(GitState.Empty.DownloadLargeFiles).toBe (false)
            )

        Vitest.test (
            "ConfirmMergeResolutionRequested ignores a second request while another conflict is still confirming",
            fun () -> promise {
                let request = {
                    Path = "conflict.txt"
                    Handle = {
                        SessionId = "conflict-1"
                        Version = "1"
                    }
                    WorkspaceVersion = "v1"
                    ResolvedContent = "resolved"
                }

                let state = {
                    runningState with
                        BusyOperation = Some(GitBusyOperation.ConfirmingMergeResolution "other.txt")
                        MergeResolutionPendingPath = Some "other.txt"
                }

                let nextState, cmd =
                    update defaultDependencies ignore (ConfirmMergeResolutionRequested request) state

                let! messages = collectMessages cmd

                Vitest.expect(nextState).toEqual (state)
                Vitest.expect(messages).toEqual ([||])
            }
        )
)

Vitest.describe (
    "GitWorkflow component behavior",
    fun () ->
        Vitest.test (
            "GitMergeConflictViewer disables Confirm Merge while confirmation is blocked",
            fun () -> promise {
                let mutable confirmCalls = 0

                let! container, cleanup =
                    renderToBody (
                        Swate.Components.Page.GitMergeConflictViewer.Viewer(
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
                let lines = [|
                    for index in 0..599 -> $"Generated renderer diff line {index + 1}"
                |]

                let markup =
                    renderToStaticMarkup (
                        Html.div [
                            prop.style [ style.width 960; style.height 480 ]
                            prop.children [
                                Swate.Components.Page.GitDiffViewer.Viewer(
                                    wordDiffText = buildAddedFileDiff "notes/renderer-large.txt" lines,
                                    previousContent = "",
                                    currentContent = joinLines lines,
                                    testIdPrefix = "renderer-large-diff"
                                )
                            ]
                        ]
                    )

                Vitest
                    .expect(markup.Contains("data-testid=\"renderer-large-diff-comparison-scroll-virtual-content\""))
                    .toBe (true)

                Vitest
                    .expect(markup.Contains("data-testid=\"renderer-large-diff-comparison-scroll-row-0\""))
                    .toBe (true)

                Vitest
                    .expect(markup.Contains("data-testid=\"renderer-large-diff-comparison-scroll-row-599\""))
                    .toBe (false)

                Vitest
                    .expect(countOccurrences "data-testid=\"renderer-large-diff-comparison-scroll-row-" markup)
                    .toBeLessThan (120)
        )

        Vitest.test (
            "GitDiffViewer renders synthetic new-file diff metadata without blanking the content pane",
            fun () ->
                let markup =
                    renderToStaticMarkup (
                        Swate.Components.Page.GitDiffViewer.Viewer(
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
                let currentLines = [|
                    for index in 0..239 -> $"Current renderer conflict line {index + 1}"
                |]

                let incomingLines = [|
                    for index in 0..239 -> $"Incoming renderer conflict line {index + 1}"
                |]

                let markup =
                    renderToStaticMarkup (
                        Html.div [
                            prop.style [ style.width 960; style.height 520 ]
                            prop.children [
                                Swate.Components.Page.GitMergeConflictViewer.Viewer(
                                    mergeConflictContent = buildSingleConflictDocument currentLines incomingLines,
                                    testIdPrefix = "renderer-large-merge"
                                )
                            ]
                        ]
                    )

                Vitest
                    .expect(markup.Contains("data-testid=\"renderer-large-merge-conflict-1-scroll-virtual-content\""))
                    .toBe (true)

                Vitest
                    .expect(markup.Contains("data-testid=\"renderer-large-merge-conflict-1-scroll-row-0\""))
                    .toBe (true)

                Vitest
                    .expect(markup.Contains("data-testid=\"renderer-large-merge-conflict-1-scroll-row-239\""))
                    .toBe (false)
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
                        Swate.Components.Page.GitSidebar.Main(
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
                                OnDiscardSelection = fun _ -> ()
                                OnConfirmPendingRemoteAction = fun () -> ()
                                OnCancelPendingRemoteAction = fun () -> ()
                                OnSaveDownloadLargeFiles = fun _ -> ()
                                OnSaveLfsAutoTrackThreshold = fun _ -> ()
                                OnCreateBranch = fun _ -> ()
                                OnSwitchBranch = fun _ -> ()
                                OnSelectChange = fun _ -> promise { return Ok() }
                                OnPruneLfsCache = fun () -> ()
                                OnDedupLfsStorage = fun () -> ()
                                OnCancelOperation = fun () -> ()
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
                        Renderer.Components.LeftSidebar.Git.GitSidebarEmptyState.Main(
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
                        Swate.Components.Page.GitSidebar.Main(
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
                                OnDiscardSelection = fun _ -> ()
                                OnConfirmPendingRemoteAction = fun () -> ()
                                OnCancelPendingRemoteAction = fun () -> ()
                                OnSaveDownloadLargeFiles = fun _ -> ()
                                OnSaveLfsAutoTrackThreshold = fun _ -> ()
                                OnCreateBranch = fun _ -> ()
                                OnSwitchBranch = fun _ -> ()
                                OnSelectChange = fun _ -> promise { return Ok() }
                                OnPruneLfsCache = fun () -> ()
                                OnDedupLfsStorage = fun () -> ()
                                OnCancelOperation = fun () -> ()
                            },
                            downloadLargeFiles = true,
                            lfsAutoTrackThresholdMb = 5,
                            remoteActionsEnabled = false,
                            remoteActionsWarning = "Sign in to a DataHub account to use fetch, pull, push, or update."
                        )
                    )

                let updateButton =
                    container.querySelector ("[data-testid='GitSidebarUpdateArcButton']") :?> HTMLButtonElement

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
                        Swate.Components.Page.GitSidebar.Main(
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
                                OnDiscardSelection = fun _ -> ()
                                OnConfirmPendingRemoteAction = fun () -> ()
                                OnCancelPendingRemoteAction = fun () -> ()
                                OnSaveDownloadLargeFiles = fun _ -> ()
                                OnSaveLfsAutoTrackThreshold = fun _ -> ()
                                OnCreateBranch = fun _ -> ()
                                OnSwitchBranch = fun _ -> ()
                                OnSelectChange = fun _ -> promise { return Ok() }
                                OnPruneLfsCache = fun () -> ()
                                OnDedupLfsStorage = fun () -> ()
                                OnCancelOperation = fun () -> ()
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
                        Swate.Components.Page.GitSidebar.Main(
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
                                OnDiscardSelection = fun _ -> ()
                                OnConfirmPendingRemoteAction = fun () -> ()
                                OnCancelPendingRemoteAction = fun () -> ()
                                OnSaveDownloadLargeFiles = fun _ -> ()
                                OnSaveLfsAutoTrackThreshold = fun _ -> ()
                                OnCreateBranch = fun _ -> ()
                                OnSwitchBranch = fun _ -> ()
                                OnSelectChange = fun _ -> promise { return Ok() }
                                OnPruneLfsCache = fun () -> ()
                                OnDedupLfsStorage = fun () -> ()
                                OnCancelOperation = fun () -> ()
                            },
                            downloadLargeFiles = true,
                            lfsAutoTrackThresholdMb = 5
                        )
                    )

                Vitest.expect(container.textContent.Contains("studies/s-study-01/protocol.md")).toBe (true)
                Vitest.expect(container.querySelector ("[data-testid='GitSidebarChangeStatusIcon-3']")).not.toBeNull ()

                cleanup ()
            }
        )

        Vitest.test (
            "GitSidebar renders small changed-file sets through the virtualized list path",
            fun () -> promise {
                let! container, cleanup =
                    renderToBody (
                        Html.div [
                            prop.style [ style.width 340; style.height 760 ]
                            prop.children [
                                Swate.Components.Page.GitSidebar.Main(
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
                                        OnDiscardSelection = fun _ -> ()
                                        OnConfirmPendingRemoteAction = fun () -> ()
                                        OnCancelPendingRemoteAction = fun () -> ()
                                        OnSaveDownloadLargeFiles = fun _ -> ()
                                        OnSaveLfsAutoTrackThreshold = fun _ -> ()
                                        OnCreateBranch = fun _ -> ()
                                        OnSwitchBranch = fun _ -> ()
                                        OnSelectChange = fun _ -> promise { return Ok() }
                                        OnPruneLfsCache = fun () -> ()
                                        OnDedupLfsStorage = fun () -> ()
                                        OnCancelOperation = fun () -> ()
                                    },
                                    downloadLargeFiles = true,
                                    lfsAutoTrackThresholdMb = 5
                                )
                            ]
                        ]
                    )

                Vitest
                    .expect(container.querySelector ("[data-testid='GitSidebarChangedFilesScrollContainer']"))
                    .not.toBeNull ()

                Vitest
                    .expect(container.querySelector ("[data-testid='GitSidebarChangedFilesVirtualContent']"))
                    .not.toBeNull ()

                Vitest.expect(container.querySelectorAll("[data-testid^='GitSidebarChangeRow-']").length).toBe (3)

                Vitest
                    .expect(
                        container
                            .querySelector("[data-testid='GitSidebarChangedFilesScrollContainer']")
                            .getAttribute ("role")
                    )
                    .toBe ("region")

                Vitest
                    .expect(
                        container
                            .querySelector("[data-testid='GitSidebarChangedFilesScrollContainer']")
                            .getAttribute ("aria-label")
                    )
                    .toBe ("Changed files")

                Vitest
                    .expect(
                        container
                            .querySelector("[data-testid='GitSidebarChangedFilesVirtualContent']")
                            .getAttribute ("role")
                    )
                    .toBe ("list")

                Vitest.expect(container.querySelectorAll("[role='listitem']").length).toBe (3)

                Vitest
                    .expect(container.querySelector ("[role='listitem'] [data-testid='GitSidebarChangeRow-0']"))
                    .not.toBeNull ()

                cleanup ()
            }
        )

        Vitest.test (
            "GitSidebar virtualizes long changed-file lists instead of mounting every item at once",
            fun () -> promise {
                let! container, cleanup =
                    renderToBody (
                        Html.div [
                            prop.style [ style.width 340; style.height 760 ]
                            prop.children [
                                Swate.Components.Page.GitSidebar.Main(
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
                                        OnDiscardSelection = fun _ -> ()
                                        OnConfirmPendingRemoteAction = fun () -> ()
                                        OnCancelPendingRemoteAction = fun () -> ()
                                        OnSaveDownloadLargeFiles = fun _ -> ()
                                        OnSaveLfsAutoTrackThreshold = fun _ -> ()
                                        OnCreateBranch = fun _ -> ()
                                        OnSwitchBranch = fun _ -> ()
                                        OnSelectChange = fun _ -> promise { return Ok() }
                                        OnPruneLfsCache = fun () -> ()
                                        OnDedupLfsStorage = fun () -> ()
                                        OnCancelOperation = fun () -> ()
                                    },
                                    downloadLargeFiles = true,
                                    lfsAutoTrackThresholdMb = 5
                                )
                            ]
                        ]
                    )

                Vitest.expect(container.textContent.Contains("400 files")).toBe (true)

                Vitest
                    .expect(container.querySelector ("[data-testid='GitSidebarChangedFilesVirtualContent']"))
                    .not.toBeNull ()

                Vitest.expect(container.querySelector ("[data-testid='GitSidebarChangeRow-0']")).not.toBeNull ()
                Vitest.expect(container.querySelector ("[data-testid='GitSidebarChangeRow-399']")).toBeNull ()

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
                            prop.style [ style.width 340; style.height 760; style.overflow.hidden ]
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
                                                Swate.Components.Page.GitSidebar.Main(
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
                                                        OnDiscardSelection = fun _ -> ()
                                                        OnConfirmPendingRemoteAction = fun () -> ()
                                                        OnCancelPendingRemoteAction = fun () -> ()
                                                        OnSaveDownloadLargeFiles = fun _ -> ()
                                                        OnSaveLfsAutoTrackThreshold = fun _ -> ()
                                                        OnCreateBranch = fun _ -> ()
                                                        OnSwitchBranch = fun _ -> ()
                                                        OnSelectChange = fun _ -> promise { return Ok() }
                                                        OnPruneLfsCache = fun () -> ()
                                                        OnDedupLfsStorage = fun () -> ()
                                                        OnCancelOperation = fun () -> ()
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
                let boundedHost =
                    container.querySelector ("[data-testid='GitSidebarBoundedHost']") :?> HTMLElement

                for expectedClass in boundedWrapperClasses do
                    Vitest.expect(boundedHost.classList.contains expectedClass).toBe (true)

                Vitest.expect(boundedHost.classList.contains "swt:w-full").toBe (false)

                let renderedRows =
                    container.querySelectorAll ("[data-testid^='GitSidebarChangeRow-']")

                Vitest.expect(renderedRows.length).toBeLessThan (200)
                Vitest.expect(container.querySelector ("[data-testid='GitSidebarChangeRow-0']")).not.toBeNull ()
                Vitest.expect(container.querySelector ("[data-testid='GitSidebarChangeRow-199']")).toBeNull ()

                cleanup ()
            }
        )

        Vitest.test (
            "GitSidebar hides the inline git return text and exposes it through a popover trigger",
            fun () -> promise {
                let! container, cleanup =
                    renderToBody (
                        Swate.Components.Page.GitSidebar.Main(
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
                                OnDiscardSelection = fun _ -> ()
                                OnConfirmPendingRemoteAction = fun () -> ()
                                OnCancelPendingRemoteAction = fun () -> ()
                                OnSaveDownloadLargeFiles = fun _ -> ()
                                OnSaveLfsAutoTrackThreshold = fun _ -> ()
                                OnCreateBranch = fun _ -> ()
                                OnSwitchBranch = fun _ -> ()
                                OnSelectChange = fun _ -> promise { return Ok() }
                                OnPruneLfsCache = fun () -> ()
                                OnDedupLfsStorage = fun () -> ()
                                OnCancelOperation = fun () -> ()
                            },
                            downloadLargeFiles = true,
                            lfsAutoTrackThresholdMb = 5
                        )
                    )

                Vitest.expect(container.textContent.Contains("git: D.")).toBe (false)
                Vitest.expect(container.textContent.Contains("Deleted")).toBe (false)
                Vitest.expect(container.querySelector ("[data-testid='GitSidebarChangeStatusIcon-0']")).not.toBeNull ()

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
                                Swate.Components.Page.GitSidebar.Main(
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
                                        OnDiscardSelection = fun _ -> ()
                                        OnConfirmPendingRemoteAction = fun () -> ()
                                        OnCancelPendingRemoteAction = fun () -> ()
                                        OnSaveDownloadLargeFiles = fun _ -> ()
                                        OnSaveLfsAutoTrackThreshold = fun _ -> ()
                                        OnCreateBranch = fun _ -> ()
                                        OnSwitchBranch = fun _ -> ()
                                        OnSelectChange = fun _ -> promise { return Ok() }
                                        OnPruneLfsCache = fun () -> ()
                                        OnDedupLfsStorage = fun () -> ()
                                        OnCancelOperation = fun () -> ()
                                    },
                                    downloadLargeFiles = true,
                                    lfsAutoTrackThresholdMb = 5
                                )
                            ]
                        ]
                    )

                let statusSlot =
                    container.querySelector ("[data-testid='GitSidebarChangeStatusSlot-0']") :?> HTMLElement

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
                        Swate.Components.Page.GitSidebar.Main(
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
                                OnDiscardSelection = fun _ -> ()
                                OnConfirmPendingRemoteAction = fun () -> ()
                                OnCancelPendingRemoteAction = fun () -> ()
                                OnSaveDownloadLargeFiles = fun _ -> ()
                                OnSaveLfsAutoTrackThreshold = fun _ -> ()
                                OnCreateBranch = fun _ -> ()
                                OnSwitchBranch = fun _ -> ()
                                OnSelectChange = fun _ -> promise { return Ok() }
                                OnPruneLfsCache = fun () -> ()
                                OnDedupLfsStorage = fun () -> ()
                                OnCancelOperation = fun () -> ()
                            },
                            downloadLargeFiles = true,
                            lfsAutoTrackThresholdMb = 5
                        )
                    )

                let firstRow =
                    container.querySelector ("[data-testid='GitSidebarChangeRow-0']") :?> HTMLElement

                let thirdRow =
                    container.querySelector ("[data-testid='GitSidebarChangeRow-2']") :?> HTMLElement

                firstRow.click ()
                do! Promise.sleep 0

                let shiftClick =
                    createMouseEvent "click" (createObj [ "bubbles" ==> true; "shiftKey" ==> true ])

                thirdRow.dispatchEvent shiftClick |> ignore
                do! Promise.sleep 0

                Vitest.expect(container.querySelectorAll("[data-testid='GitSidebarPrimarySaveButton']").length).toBe (1)
                Vitest.expect(container.textContent.Contains("Save Selected Changes")).toBe (true)

                Vitest
                    .expect(container.querySelectorAll("[data-testid^='GitSidebarCommitSelectionCheckbox-']").length)
                    .toBe (0)

                cleanup ()
            }
        )

        Vitest.test (
            "GitSidebar hover discard button discards the currently marked files",
            fun () -> promise {
                let mutable discardedPaths: string[] option = None

                let! container, cleanup =
                    renderToBody (
                        Swate.Components.Page.GitSidebar.Main(
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
                                noopCallbacks with
                                    OnDiscardSelection = fun paths -> discardedPaths <- Some paths
                            },
                            downloadLargeFiles = true,
                            lfsAutoTrackThresholdMb = 5
                        )
                    )

                let row0 =
                    container.querySelector ("[data-testid='GitSidebarChangeRow-0']") :?> HTMLElement

                let row2 =
                    container.querySelector ("[data-testid='GitSidebarChangeRow-2']") :?> HTMLElement

                row0.click ()
                do! Promise.sleep 0

                let ctrlClick =
                    createMouseEvent "click" (createObj [ "bubbles" ==> true; "ctrlKey" ==> true ])

                row2.dispatchEvent ctrlClick |> ignore
                do! Promise.sleep 0

                let hoverEvent = createMouseEvent "mouseenter" (createObj [ "bubbles" ==> true ])

                row0.dispatchEvent hoverEvent |> ignore
                do! Promise.sleep 0

                let discardButton =
                    container.querySelector ("[data-testid='GitSidebarDiscardChangeButton-0']") :?> HTMLElement

                discardButton.click ()
                do! Promise.sleep 0

                Vitest.expect(discardedPaths).toEqual (Some [| "src/file-000.txt"; "src/file-002.txt" |])

                cleanup ()
            }
        )

        Vitest.test (
            "GitSidebar plain click clears previous marks and ctrl-click toggles a row on and back off",
            fun () -> promise {
                let mutable capturedSelection: GitSidebarCommitSelectionRequest option = None

                let! container, cleanup =
                    renderToBody (
                        Swate.Components.Page.GitSidebar.Main(
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
                                OnPrimarySaveSelection = fun request -> capturedSelection <- Some request
                                OnPrimarySaveAll = fun _ -> ()
                                OnCommitSelection = fun _ -> ()
                                OnCommitAll = fun _ -> ()
                                OnDiscardSelection = fun _ -> ()
                                OnConfirmPendingRemoteAction = fun () -> ()
                                OnCancelPendingRemoteAction = fun () -> ()
                                OnSaveDownloadLargeFiles = fun _ -> ()
                                OnSaveLfsAutoTrackThreshold = fun _ -> ()
                                OnCreateBranch = fun _ -> ()
                                OnSwitchBranch = fun _ -> ()
                                OnSelectChange = fun _ -> promise { return Ok() }
                                OnPruneLfsCache = fun () -> ()
                                OnDedupLfsStorage = fun () -> ()
                                OnCancelOperation = fun () -> ()
                            },
                            downloadLargeFiles = true,
                            lfsAutoTrackThresholdMb = 5
                        )
                    )

                let row0 =
                    container.querySelector ("[data-testid='GitSidebarChangeRow-0']") :?> HTMLElement

                let row1 =
                    container.querySelector ("[data-testid='GitSidebarChangeRow-1']") :?> HTMLElement

                let row2 =
                    container.querySelector ("[data-testid='GitSidebarChangeRow-2']") :?> HTMLElement

                let messageInput =
                    container.querySelector ("[data-testid='GitSidebarCommitMessageInput']") :?> HTMLTextAreaElement

                row0.click ()
                do! Promise.sleep 0

                let ctrlClick =
                    createMouseEvent "click" (createObj [ "bubbles" ==> true; "ctrlKey" ==> true ])

                row2.dispatchEvent ctrlClick |> ignore
                do! Promise.sleep 0
                row2.dispatchEvent ctrlClick |> ignore
                do! Promise.sleep 0
                row1.click ()
                do! Promise.sleep 0

                setTextAreaValue messageInput "save one file"
                do! Promise.sleep 0

                let saveButton =
                    container.querySelector ("[data-testid='GitSidebarPrimarySaveButton']") :?> HTMLButtonElement

                Vitest.expect(saveButton.disabled).toBe (false)
                Vitest.expect(container.textContent.Contains("Save Selected Changes")).toBe (true)
                saveButton.click ()
                do! Promise.sleep 0

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
                        Swate.Components.Page.GitSidebar.Main(
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
                                OnPrimarySaveSelection = fun request -> capturedSelection <- Some request
                                OnPrimarySaveAll = fun _ -> ()
                                OnCommitSelection = fun _ -> ()
                                OnCommitAll = fun _ -> ()
                                OnDiscardSelection = fun _ -> ()
                                OnConfirmPendingRemoteAction = fun () -> ()
                                OnCancelPendingRemoteAction = fun () -> ()
                                OnSaveDownloadLargeFiles = fun _ -> ()
                                OnSaveLfsAutoTrackThreshold = fun _ -> ()
                                OnCreateBranch = fun _ -> ()
                                OnSwitchBranch = fun _ -> ()
                                OnSelectChange = fun _ -> promise { return Ok() }
                                OnPruneLfsCache = fun () -> ()
                                OnDedupLfsStorage = fun () -> ()
                                OnCancelOperation = fun () -> ()
                            },
                            downloadLargeFiles = true,
                            lfsAutoTrackThresholdMb = 5
                        )
                    )

                let row0 =
                    container.querySelector ("[data-testid='GitSidebarChangeRow-0']") :?> HTMLElement

                let row2 =
                    container.querySelector ("[data-testid='GitSidebarChangeRow-2']") :?> HTMLElement

                let messageInput =
                    container.querySelector ("[data-testid='GitSidebarCommitMessageInput']") :?> HTMLTextAreaElement

                row0.click ()
                do! Promise.sleep 0

                let ctrlShiftClick =
                    createMouseEvent
                        "click"
                        (createObj [
                            "bubbles" ==> true
                            "ctrlKey" ==> true
                            "shiftKey" ==> true
                        ])

                row2.dispatchEvent ctrlShiftClick |> ignore
                do! Promise.sleep 0

                setTextAreaValue messageInput "save range"
                do! Promise.sleep 0

                let saveButton =
                    container.querySelector ("[data-testid='GitSidebarPrimarySaveButton']") :?> HTMLButtonElement

                Vitest.expect(saveButton.disabled).toBe (false)
                Vitest.expect(container.textContent.Contains("Save Selected Changes")).toBe (true)
                saveButton.click ()
                do! Promise.sleep 0

                Vitest
                    .expect(capturedSelection |> Option.map _.Paths)
                    .toEqual (
                        Some [|
                            "src/file-000.txt"
                            "src/file-001.txt"
                            "src/file-002.txt"
                        |]
                    )

                cleanup ()
            }
        )

        Vitest.test (
            "GitSidebar keeps marked rows selected even when diff opening fails",
            fun () -> promise {
                let! container, cleanup =
                    renderToBody (
                        Swate.Components.Page.GitSidebar.Main(
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
                                OnDiscardSelection = fun _ -> ()
                                OnConfirmPendingRemoteAction = fun () -> ()
                                OnCancelPendingRemoteAction = fun () -> ()
                                OnSaveDownloadLargeFiles = fun _ -> ()
                                OnSaveLfsAutoTrackThreshold = fun _ -> ()
                                OnCreateBranch = fun _ -> ()
                                OnSwitchBranch = fun _ -> ()
                                OnSelectChange = fun _ -> promise { return Error "Diff failed to load." }
                                OnPruneLfsCache = fun () -> ()
                                OnDedupLfsStorage = fun () -> ()
                                OnCancelOperation = fun () -> ()
                            },
                            downloadLargeFiles = true,
                            lfsAutoTrackThresholdMb = 5
                        )
                    )

                let firstRow =
                    container.querySelector ("[data-testid='GitSidebarChangeRow-0']") :?> HTMLElement

                firstRow.click ()
                do! Promise.sleep 0

                Vitest.expect(container.textContent.Contains("Save Selected Changes")).toBe (true)
                Vitest.expect(container.querySelector ("[data-testid='GitSidebarErrorNotice']")).not.toBeNull ()

                cleanup ()
            }
        )

        Vitest.test (
            "GitSidebar publish rename prompt submits the edited repository name",
            fun () -> promise {
                let mutable submittedName: string option = None
                let mutable cancelCalls = 0

                let! container, cleanup =
                    renderToBody (
                        Swate.Components.Page.GitSidebar.Main(
                            status = {
                                CurrentBranch = Some "main"
                                TrackingBranch = None
                                Ahead = 1
                                Behind = 0
                                IsClean = true
                                IsMergeInProgress = false
                            },
                            changedFiles = [||],
                            branchOptions = [| sidebarLocalBranch "main" true false |],
                            callbacks = noopCallbacks,
                            downloadLargeFiles = true,
                            lfsAutoTrackThresholdMb = 5,
                            publishRenamePrompt = {
                                CurrentName = "Existing ARC"
                                Message = "A DataHub repository named 'Existing ARC' already exists."
                            },
                            onSubmitPublishRename = (fun name -> submittedName <- Some name),
                            onCancelPublishRename = (fun () -> cancelCalls <- cancelCalls + 1)
                        )
                    )

                let input =
                    document.body.querySelector ("[data-testid='GitSidebarPublishRenameInput']") :?> HTMLInputElement

                Vitest.expect(input.value).toBe ("Existing ARC")

                setInputValue input "Renamed ARC"
                do! Promise.sleep 0

                let submitButton =
                    document.body.querySelector ("[data-testid='GitSidebarPublishRenameSubmit']") :?> HTMLButtonElement

                submitButton.click ()
                do! Promise.sleep 0

                Vitest.expect(submittedName).toEqual (Some "Renamed ARC")
                Vitest.expect(cancelCalls).toBe (0)

                cleanup ()
            }
        )

        Vitest.test (
            "GitSidebar Ctrl+click on a marked row deselects it and rows suppress native text selection",
            fun () -> promise {
                let! container, cleanup =
                    renderToBody (
                        Swate.Components.Page.GitSidebar.Main(
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
                                OnDiscardSelection = fun _ -> ()
                                OnConfirmPendingRemoteAction = fun () -> ()
                                OnCancelPendingRemoteAction = fun () -> ()
                                OnSaveDownloadLargeFiles = fun _ -> ()
                                OnSaveLfsAutoTrackThreshold = fun _ -> ()
                                OnCreateBranch = fun _ -> ()
                                OnSwitchBranch = fun _ -> ()
                                OnSelectChange = fun _ -> promise { return Ok() }
                                OnPruneLfsCache = fun () -> ()
                                OnDedupLfsStorage = fun () -> ()
                                OnCancelOperation = fun () -> ()
                            },
                            downloadLargeFiles = true,
                            lfsAutoTrackThresholdMb = 5
                        )
                    )

                let firstRow =
                    container.querySelector ("[data-testid='GitSidebarChangeRow-0']") :?> HTMLElement

                firstRow.click ()
                do! Promise.sleep 0

                Vitest.expect(container.textContent.Contains("Save Selected Changes")).toBe (true)

                let ctrlClick =
                    createMouseEvent "click" (createObj [ "bubbles" ==> true; "ctrlKey" ==> true ])

                firstRow.dispatchEvent ctrlClick |> ignore
                do! Promise.sleep 0

                Vitest.expect(container.textContent.Contains("Save All Changes")).toBe (true)

                let rowClass = firstRow.className
                Vitest.expect(rowClass.Contains("swt:select-none")).toBe (true)

                cleanup ()
            }
        )

        Vitest.test (
            "GitSidebar status icon click does not open the changed file row",
            fun () -> promise {
                let mutable selectCalls = 0

                let! container, cleanup =
                    renderToBody (
                        Swate.Components.Page.GitSidebar.Main(
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
                            callbacks = {
                                noopCallbacks with
                                    OnSelectChange =
                                        fun _ ->
                                            selectCalls <- selectCalls + 1
                                            promise { return Ok() }
                            },
                            downloadLargeFiles = true,
                            lfsAutoTrackThresholdMb = 5
                        )
                    )

                let statusIcon =
                    container.querySelector ("[data-testid='GitSidebarChangeStatusIcon-0']") :?> HTMLElement

                let clickEvent = createMouseEvent "click" (createObj [ "bubbles" ==> true ])

                statusIcon.dispatchEvent clickEvent |> ignore
                do! Promise.sleep 0

                Vitest.expect(selectCalls).toBe (0)

                cleanup ()
            }
        )

        Vitest.test (
            "GitSidebar renders the save section without the old outer card wrapper",
            fun () -> promise {
                let! container, cleanup =
                    renderToBody (
                        Swate.Components.Page.GitSidebar.Main(
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

                let saveSection =
                    container.querySelector ("[data-testid='GitSidebarCommitSection']") :?> HTMLElement

                let legacyCard = firstElementChild saveSection
                Vitest.expect(legacyCard.className.Contains("swt:rounded-box")).toBe (false)
                Vitest.expect(legacyCard.className.Contains("swt:border")).toBe (false)

                cleanup ()
            }
        )
)

Vitest.describe (
    "GitDiffPageLoader",
    fun () ->
        let change path index : GitSidebarChange = {
            Path = path
            OriginalPath = None
            IndexStatus = index
            WorkingTreeStatus = "."
            IsConflicted = false
        }

        Vitest.test (
            "An added file shows an empty previous side",
            fun () -> promise {
                let path = "new.txt"

                let getBaseContent =
                    fun _ -> promise { return Ok(failed NotFound VersionControlCodes.BaseContentNotFound "absent") }

                let getWordDiff = fun _ -> promise { return Ok(succeeded (ContentViewDto.Text "")) }

                let readCurrentContent = fun _ -> promise { return Ok "new content" }

                let! result = GitDiffPageLoader.load getBaseContent getWordDiff readCurrentContent (change path "A")

                match result with
                | Ok(PageState.GitDiffPage page) ->
                    Vitest.expect(page.PreviousContent).toBe ("")
                    Vitest.expect(page.CurrentContent).toBe ("new content")
                | _ -> failwith "Expected an added-file diff page."
            }
        )

        Vitest.test (
            "A deleted file shows an empty current side without reading the file",
            fun () -> promise {
                let path = "deleted.txt"
                let mutable currentRead = false

                let getBaseContent =
                    fun _ -> promise { return Ok(succeeded (ContentViewDto.Text "old")) }

                let getWordDiff =
                    fun _ -> promise { return Ok(succeeded (ContentViewDto.Text "-old")) }

                let readCurrentContent =
                    fun _ ->
                        currentRead <- true
                        promise { return Ok "must not be read" }

                let! result = GitDiffPageLoader.load getBaseContent getWordDiff readCurrentContent (change path "D")

                match result with
                | Ok(PageState.GitDiffPage page) ->
                    Vitest.expect(page.PreviousContent).toBe ("old")
                    Vitest.expect(page.CurrentContent).toBe ("")
                    Vitest.expect(currentRead).toBe (false)
                | _ -> failwith "Expected a deleted-file diff page."
            }
        )

        Vitest.test (
            "A change with neither side is an error",
            fun () -> promise {
                let path = "empty.txt"

                let getBaseContent =
                    fun _ -> promise { return Ok(failed NotFound VersionControlCodes.BaseContentNotFound "absent") }

                let getWordDiff = fun _ -> promise { return Ok(succeeded (ContentViewDto.Text "")) }

                let readCurrentContent = fun _ -> promise { return Ok "must not be read" }

                let! result = GitDiffPageLoader.load getBaseContent getWordDiff readCurrentContent (change path "D")

                match result with
                | Error message -> Vitest.expect(message.Contains(path)).toBe (true)
                | _ -> failwith "Expected a missing-content error."
            }
        )

        Vitest.test (
            "A failed read of the current file is an error",
            fun () -> promise {
                let path = "changed.txt"

                let getBaseContent =
                    fun _ -> promise { return Ok(succeeded (ContentViewDto.Text "old")) }

                let getWordDiff = fun _ -> promise { return Ok(succeeded (ContentViewDto.Text "")) }

                let readCurrentContent = fun _ -> promise { return Error "EACCES" }

                let! result = GitDiffPageLoader.load getBaseContent getWordDiff readCurrentContent (change path "M")

                match result with
                | Error message -> Vitest.expect(message.Contains("EACCES")).toBe (true)
                | _ -> failwith "Expected a current-content read error."
            }
        )

        Vitest.test (
            "A base failure other than not found is an error",
            fun () -> promise {
                let path = "failed-base.txt"

                let getBaseContent =
                    fun _ -> promise { return Ok(failed ProviderError "git_failure" "boom") }

                let getWordDiff = fun _ -> promise { return Ok(succeeded (ContentViewDto.Text "")) }

                let readCurrentContent = fun _ -> promise { return Ok "x" }

                let! result = GitDiffPageLoader.load getBaseContent getWordDiff readCurrentContent (change path "M")

                match result with
                | Error message -> Vitest.expect(message.Contains("boom")).toBe (true)
                | _ -> failwith "Expected a base-content error."
            }
        )

        Vitest.test (
            "An unsupported word diff opens the unsupported page",
            fun () -> promise {
                let path = "binary.dat"

                let getBaseContent =
                    fun _ -> promise { return Ok(succeeded (ContentViewDto.Text "a")) }

                let getWordDiff =
                    fun _ -> promise { return Ok(failed Unsupported "binary" "binary content") }

                let readCurrentContent = fun _ -> promise { return Ok "b" }

                let! result = GitDiffPageLoader.load getBaseContent getWordDiff readCurrentContent (change path "M")

                match result with
                | Ok(PageState.GitUnsupportedPage _) -> ()
                | _ -> failwith "Expected an unsupported diff page."
            }
        )
)
