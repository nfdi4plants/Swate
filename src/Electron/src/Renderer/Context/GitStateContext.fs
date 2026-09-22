module Renderer.Context.GitStateContext

open Browser.Dom
open Fable.Core
open Feliz
open Feliz.UseElmish

open Renderer.Types
open Swate.Components.Page.GitSidebarTypes
open Swate.Components.Primitive.ErrorModal.Context
open Swate.Components.Primitive.ErrorModal.Types
open Swate.Electron.Shared.IPCTypes
open Swate.Electron.Shared.VersionControlTypes

open Renderer.Context.GitWorkflow

type GitStateController = {
    state: GitState
    refresh: unit -> unit
    initRepository: unit -> unit
    fetch: unit -> unit
    pull: unit -> unit
    push: unit -> unit
    cancelOperation: unit -> unit
    updateFromOnline: unit -> unit
    primarySaveSelection: GitSidebarCommitSelectionRequest -> unit
    primarySaveAll: string -> unit
    cloneRepository: CloneWorkspaceRequestDto -> JS.Promise<Result<string, string>>
    commitSelection: GitSidebarCommitSelectionRequest -> unit
    commitAll: string -> unit
    discardSelection: string[] -> unit
    confirmPendingRemoteAction: unit -> unit
    cancelPendingRemoteAction: unit -> unit
    submitPublishRename: string -> unit
    cancelPublishRename: unit -> unit
    saveLfsAutoTrackThreshold: int -> unit
    saveDownloadLargeFiles: bool -> unit
    createBranch: GitSidebarCreateBranchRequest -> unit
    switchBranch: string -> unit
    selectChange: GitSidebarChange -> JS.Promise<Result<unit, string>>
    confirmMergeResolution: GitMergeResolutionRequest -> unit
    pruneLfsCache: unit -> unit
    dedupLfsStorage: unit -> unit
}

module private Helper =

    let private pathRequest (path: string) : ObjectPathRequestDto = {
        OperationId = Renderer.VersionControlApiClient.newOperationId ()
        Path = path
        RefreshTree = None
    }

    let private unsupportedPage (path: string) (reason: string option) =
        Ok(PageState.GitUnsupportedPage { Path = path; Reason = reason })

    /// The current content comes from the vault file itself, the rest from the provider.
    let loadDiffPage (change: GitSidebarChange) : JS.Promise<Result<PageState, string>> =
        GitDiffPageLoader.load
            (fun path -> Renderer.VersionControlApiClient.getBaseContent (pathRequest path))
            (fun path -> Renderer.VersionControlApiClient.getWordDiff (pathRequest path))
            (fun path -> promise {
                let! file = Api.ipcArcVaultApi.openFile path
                return file |> Result.map _.content |> Result.mapError _.Message
            })
            change

    /// The conflict page carries the handle and the workspace token the preview was
    /// taken with, so the confirmation is checked against exactly that state.
    let loadConflictPage
        (conflict: ConflictSessionSummaryDto)
        (workspaceVersion: string)
        (requestedPath: string)
        : JS.Promise<Result<PageState, string>> =
        promise {
            match conflict.Items |> Array.tryFind (fun item -> item.Path = requestedPath) with
            | None -> return Error $"'{requestedPath}' is not part of the open conflict session anymore."
            | Some item ->
                match item.CombinedPreview with
                | Some(ContentViewDto.Text content) when item.SupportsResolvedContent ->
                    return
                        Ok(
                            PageState.GitMergeConflictPage {
                                Path = requestedPath
                                ConflictContent = content
                                Handle = conflict.Handle
                                WorkspaceVersion = workspaceVersion
                            }
                        )
                | Some(ContentViewDto.Unsupported reason) -> return unsupportedPage requestedPath reason
                | _ ->
                    return unsupportedPage requestedPath (Some "The provider offers no text preview for this conflict.")
        }

    let dependencies (reportError: GitErrorNotification -> unit) : GitDependencies = {
        getSessionInfo = Renderer.VersionControlApiClient.getSessionInfo
        getStatus = Renderer.VersionControlApiClient.getStatus
        listRefs = Renderer.VersionControlApiClient.listRefs
        getRepositoryWebUrl = Renderer.VersionControlApiClient.getRepositoryWebUrl
        getStoragePolicySettings = Renderer.VersionControlApiClient.getStoragePolicySettings
        setStoragePolicySettings = Renderer.VersionControlApiClient.setStoragePolicySettings
        loadDiffPage = loadDiffPage
        loadConflictPage = loadConflictPage
        initializeWorkspace = Renderer.VersionControlApiClient.initializeWorkspace
        bindWorkspace = Renderer.VersionControlApiClient.bindWorkspace
        createRemoteProject = Api.ipcGitLabApi.createProject
        renameOpenArcRoot =
            fun newName -> promise {
                let! result = Api.ipcArcVaultApi.renameOpenArcRoot newName
                return result |> Result.mapError _.Message
            }
        checkDependencies = Renderer.VersionControlApiClient.checkDependencies
        installDependency = Renderer.VersionControlApiClient.installDependency
        refreshSynchronization = Renderer.VersionControlApiClient.refreshSynchronization
        synchronize = Renderer.VersionControlApiClient.synchronize
        // The write command replaces this with a dispatch of WritePhaseChanged, and nothing outside writeCmd reports phases.
        reportPhase = ignore
        cancelOperation = Renderer.VersionControlApiClient.cancelOperation
        cloneWorkspace = Renderer.VersionControlApiClient.cloneWorkspace
        createRef = Renderer.VersionControlApiClient.createRef
        preflightSwitchRef = Renderer.VersionControlApiClient.preflightSwitchRef
        switchRef = Renderer.VersionControlApiClient.switchRef
        createRevision = Renderer.VersionControlApiClient.createRevision
        restorePaths = Renderer.VersionControlApiClient.restorePaths
        resolveConflict = Renderer.VersionControlApiClient.resolveConflict
        finalizeConflict = Renderer.VersionControlApiClient.finalizeConflict
        cancelConflict = Renderer.VersionControlApiClient.cancelConflict
        listObjects = Renderer.VersionControlApiClient.listObjects
        materializeObject = Renderer.VersionControlApiClient.materializeObject
        pruneStorage = Renderer.VersionControlApiClient.pruneStorage
        deduplicateStorage = Renderer.VersionControlApiClient.deduplicateStorage
        clearStaleLock = Renderer.VersionControlApiClient.clearStaleLock
        newOperationId = Renderer.VersionControlApiClient.newOperationId
        confirmLfsPrune = fun message -> window.confirm message
        confirmInstall = fun message -> window.confirm message
        reportError = reportError
    }

let GitStateCtx =
    React.createContext<GitStateController> (
        {
            state = GitState.Empty
            refresh = fun () -> ()
            initRepository = fun () -> ()
            fetch = fun () -> ()
            pull = fun () -> ()
            push = fun () -> ()
            cancelOperation = fun () -> ()
            updateFromOnline = fun () -> ()
            primarySaveSelection = fun _ -> ()
            primarySaveAll = fun _ -> ()
            cloneRepository = fun _ -> promise { return Ok "" }
            commitSelection = fun _ -> ()
            commitAll = fun _ -> ()
            discardSelection = fun _ -> ()
            confirmPendingRemoteAction = fun () -> ()
            cancelPendingRemoteAction = fun () -> ()
            submitPublishRename = fun _ -> ()
            cancelPublishRename = fun () -> ()
            saveLfsAutoTrackThreshold = fun _ -> ()
            saveDownloadLargeFiles = fun _ -> ()
            createBranch = fun _ -> ()
            switchBranch = fun _ -> ()
            selectChange = fun _ -> promise { return Ok() }
            confirmMergeResolution = fun _ -> ()
            pruneLfsCache = fun () -> ()
            dedupLfsStorage = fun () -> ()
        }
    )

[<Hook>]
let useGitStateCtx () = React.useContext GitStateCtx

[<ReactComponent>]
let GitStateCtxProvider (children: ReactElement) =

    let appStateCtx = Renderer.Context.AppStateContext.useAppStateCtx ()
    let pageStateCtx = Renderer.Context.PageStateContext.usePageStateCtx ()
    let errorModalCtx = useErrorModalCtx ()
    let errorModalCtxRef = React.useRef errorModalCtx
    errorModalCtxRef.current <- errorModalCtx

    let reportGitError =
        React.useCallback (
            (fun (notification: GitErrorNotification) ->
                errorModalCtxRef.current.enqueue (
                    ErrorModalRequest.create (notification.Message, title = notification.Title)
                )
            ),
            [||]
        )

    let dependencies =
        React.useMemo ((fun _ -> Helper.dependencies reportGitError), [||])

    let gitState, dispatch =
        React.useElmish ((fun () -> init ()), update dependencies pageStateCtx.setState, subscribe, [||])

    let refresh () = dispatch RefreshRequested

    let initRepository () = dispatch InitRepositoryRequested

    let fetch () = dispatch FetchRequested

    let pull () = dispatch PullRequested

    let push () = dispatch PushRequested

    let cancelOperation () =
        dispatch CancelCurrentOperationRequested

    let updateFromOnline () = dispatch UpdateFromOnlineRequested

    let primarySaveSelection (request: GitSidebarCommitSelectionRequest) =
        dispatch (PrimarySaveSelectionRequested request)

    let primarySaveAll (message: string) =
        dispatch (PrimarySaveAllRequested message)

    let cloneRepository (request: CloneWorkspaceRequestDto) =
        Promise.create (fun resolve _reject -> dispatch (CloneRequested(request, resolve)))

    let commitSelection (request: GitSidebarCommitSelectionRequest) =
        dispatch (CommitSelectionRequested request)

    let commitAll (message: string) = dispatch (CommitAllRequested message)

    let discardSelection (paths: string[]) =
        dispatch (DiscardSelectionRequested paths)

    let confirmPendingRemoteAction () =
        dispatch ConfirmPendingRemoteActionRequested

    let cancelPendingRemoteAction () =
        dispatch CancelPendingRemoteActionRequested

    let submitPublishRename newName =
        dispatch (SubmitPublishRenameRequested newName)

    let cancelPublishRename () = dispatch CancelPublishRenameRequested

    let saveLfsAutoTrackThreshold (thresholdMb: int) =
        dispatch (SaveLfsAutoTrackThresholdRequested thresholdMb)

    let saveDownloadLargeFiles (downloadLargeFiles: bool) =
        dispatch (SaveDownloadLargeFilesRequested downloadLargeFiles)

    let createBranchFrom (request: GitSidebarCreateBranchRequest) =
        dispatch (CreateBranchRequested request)

    let switchBranchTo (branchName: string) =
        dispatch (SwitchBranchRequested branchName)

    let selectChange (change: GitSidebarChange) =
        Promise.create (fun resolve _reject -> dispatch (SelectChangeRequested(change, resolve)))

    let confirmMergeResolutionAction (request: GitMergeResolutionRequest) =
        dispatch (ConfirmMergeResolutionRequested request)

    let pruneLfsCache () = dispatch PruneLfsCacheRequested

    let dedupLfsStorage () = dispatch DedupLfsStorageRequested

    React.useEffect ((fun () -> dispatch (ArcPathChanged appStateCtx)), [| box appStateCtx |])

    let gitStateController: GitStateController =
        React.useMemo (
            (fun _ -> {
                state = gitState
                refresh = refresh
                initRepository = initRepository
                fetch = fetch
                pull = pull
                push = push
                cancelOperation = cancelOperation
                updateFromOnline = updateFromOnline
                primarySaveSelection = primarySaveSelection
                primarySaveAll = primarySaveAll
                cloneRepository = cloneRepository
                commitSelection = commitSelection
                commitAll = commitAll
                discardSelection = discardSelection
                confirmPendingRemoteAction = confirmPendingRemoteAction
                cancelPendingRemoteAction = cancelPendingRemoteAction
                submitPublishRename = submitPublishRename
                cancelPublishRename = cancelPublishRename
                saveLfsAutoTrackThreshold = saveLfsAutoTrackThreshold
                saveDownloadLargeFiles = saveDownloadLargeFiles
                createBranch = createBranchFrom
                switchBranch = switchBranchTo
                selectChange = selectChange
                confirmMergeResolution = confirmMergeResolutionAction
                pruneLfsCache = pruneLfsCache
                dedupLfsStorage = dedupLfsStorage
            }),
            [| box gitState |]
        )

    GitStateCtx.Provider(gitStateController, children)
