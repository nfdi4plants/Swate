module Renderer.Context.GitStateContext

open Browser.Dom
open Fable.Core
open Feliz
open Feliz.UseElmish

open Renderer.Types
open Swate.Components.Page.GitSidebarTypes
open Swate.Components.Primitive.ErrorModal.Context
open Swate.Components.Primitive.ErrorModal.Types
open Swate.Electron.Shared.GitTypes
open Swate.Electron.Shared.IPCTypes

open Renderer.Context.GitWorkflow

type GitStateController = {
    state: GitState
    refresh: unit -> unit
    initRepository: unit -> unit
    fetch: unit -> unit
    pull: unit -> unit
    push: unit -> unit
    updateFromOnline: unit -> unit
    primarySaveSelection: GitSidebarCommitSelectionRequest -> unit
    primarySaveAll: string -> unit
    cloneRepository: GitCloneRepositoryRequest -> JS.Promise<Result<string, string>>
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
    confirmMergeResolution: GitConfirmMergeResolutionRequest -> unit
    pruneLfsCache: unit -> unit
    dedupLfsStorage: unit -> unit
}

module private Helper =

    let mapDiffPageResult (_requestedPath: string) =
        function
        | Ok(GitPageLoadResultDto.Loaded diffData) -> Ok(PageState.GitDiffPage diffData)
        | Ok(GitPageLoadResultDto.Unsupported unsupportedPage) -> Ok(PageState.GitUnsupportedPage unsupportedPage)
        | Error message -> Error message

    let mapMergeConflictPageResult (_requestedPath: string) =
        function
        | Ok(GitPageLoadResultDto.Loaded mergeData) -> Ok(PageState.GitMergeConflictPage mergeData)
        | Ok(GitPageLoadResultDto.Unsupported unsupportedPage) -> Ok(PageState.GitUnsupportedPage unsupportedPage)
        | Error message -> Error message

    let dependencies (reportError: GitErrorNotification -> unit) : GitDependencies = {
        getGitStatus = Renderer.GitApiClient.getGitStatus
        getGitBranches = Renderer.GitApiClient.getGitBranches
        getOriginRemoteRepositoryWebUrl = Renderer.GitApiClient.getOriginRepositoryWebUrl
        getGitLfsSettings = Renderer.GitApiClient.getGitLfsSettings
        loadDiffPage =
            fun requestedPath -> promise {
                let! result = Renderer.GitApiClient.getGitDiffViewData requestedPath
                return mapDiffPageResult requestedPath result
            }
        loadMergeConflictPage =
            fun requestedPath -> promise {
                let! result = Renderer.GitApiClient.getGitMergeConflictViewData requestedPath
                return mapMergeConflictPageResult requestedPath result
            }
        initGitRepository = Renderer.GitApiClient.gitInitRepository
        renameOpenArcRoot =
            fun newName -> promise {
                let! result = Api.ipcArcVaultApi.renameOpenArcRoot newName
                return result |> Result.mapError _.Message
            }
        installGitLfs = Renderer.GitApiClient.installGitLfs
        previewGitPull = Renderer.GitApiClient.previewGitPull
        gitFetch = Renderer.GitApiClient.gitFetch
        gitPull = Renderer.GitApiClient.gitPull
        gitPush = Renderer.GitApiClient.gitPush
        gitCloneRepository = Renderer.GitApiClient.gitCloneRepository
        createBranch = Renderer.GitApiClient.createBranch
        checkoutBranch = Renderer.GitApiClient.checkoutBranch
        gitStagePaths = Renderer.GitApiClient.gitStagePaths
        gitUnstagePaths = Renderer.GitApiClient.gitUnstagePaths
        gitDiscardPaths = Renderer.GitApiClient.gitDiscardPaths
        gitCommit = Renderer.GitApiClient.gitCommit
        setGitLfsSettings = Renderer.GitApiClient.setGitLfsSettings
        gitLfsPrune = Renderer.GitApiClient.gitLfsPrune
        gitLfsDedup = Renderer.GitApiClient.gitLfsDedup
        confirmGitMergeResolution = Renderer.GitApiClient.confirmGitMergeResolution
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

    let updateFromOnline () = dispatch UpdateFromOnlineRequested

    let primarySaveSelection (request: GitSidebarCommitSelectionRequest) =
        dispatch (PrimarySaveSelectionRequested request)

    let primarySaveAll (message: string) =
        dispatch (PrimarySaveAllRequested message)

    let cloneRepository (request: GitCloneRepositoryRequest) =
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

    let confirmMergeResolutionAction request =
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
