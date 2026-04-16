module Renderer.Context.GitStateContext

open Browser.Dom
open Fable.Core
open Feliz
open Feliz.UseElmish

open Renderer.Types
open Swate.Components.GitSidebarTypes
open Swate.Electron.Shared.GitTypes
open Swate.Electron.Shared.IPCTypes

open Renderer.Context.GitWorkflow

type GitStateController = {
    state: GitState
    refresh: unit -> unit
    initRepository: string option -> unit
    fetch: unit -> unit
    pull: unit -> unit
    push: unit -> unit
    cloneRepository: GitCloneRepositoryRequest -> JS.Promise<Result<string, string>>
    sync: unit -> unit
    commitSelection: GitSidebarCommitSelectionRequest -> unit
    commitAll: string -> unit
    saveLfsAutoTrackThreshold: int -> unit
    saveDownloadLargeFiles: bool -> unit
    createBranch: GitSidebarCreateBranchRequest -> unit
    switchBranch: string -> unit
    selectChange: GitSidebarChange -> JS.Promise<Result<unit, string>>
    confirmMergeResolution: GitConfirmMergeResolutionRequest -> unit
}

let private mapDiffPageResult (_requestedPath: string) =
    function
    | Ok(GitPageLoadResultDto.Loaded diffData) -> Ok(PageState.GitDiffPage diffData)
    | Ok(GitPageLoadResultDto.Unsupported unsupportedPage) -> Ok(PageState.GitUnsupportedPage unsupportedPage)
    | Error message -> Error message

let private mapMergeConflictPageResult (_requestedPath: string) =
    function
    | Ok(GitPageLoadResultDto.Loaded mergeData) -> Ok(PageState.GitMergeConflictPage mergeData)
    | Ok(GitPageLoadResultDto.Unsupported unsupportedPage) -> Ok(PageState.GitUnsupportedPage unsupportedPage)
    | Error message -> Error message

let private dependencies: GitDependencies = {
    getGitStatus = Renderer.GitApiClient.getGitStatus
    getGitBranches = Renderer.GitApiClient.getGitBranches
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
    createDataHubProject =
        fun projectName -> promise {
            let! result = Api.ipcGitLabApi.createProject (unbox null) projectName
            return result |> Result.mapError _.GitLabErrorToString
        }
    installGitLfs = Renderer.GitApiClient.installGitLfs
    gitFetch = Renderer.GitApiClient.gitFetch
    gitPull = Renderer.GitApiClient.gitPull
    gitPush = Renderer.GitApiClient.gitPush
    gitAddRemote = Renderer.GitApiClient.gitAddRemote
    gitCloneRepository = Renderer.GitApiClient.gitCloneRepository
    createBranch = Renderer.GitApiClient.createBranch
    checkoutBranch = Renderer.GitApiClient.checkoutBranch
    gitStagePaths = Renderer.GitApiClient.gitStagePaths
    gitUnstagePaths = Renderer.GitApiClient.gitUnstagePaths
    gitCommit = Renderer.GitApiClient.gitCommit
    setGitLfsSettings = Renderer.GitApiClient.setGitLfsSettings
    confirmGitMergeResolution = Renderer.GitApiClient.confirmGitMergeResolution
    confirmInstall = fun message -> window.confirm message
}

let GitStateCtx =
    React.createContext<GitStateController> (
        {
            state = GitState.Empty
            refresh = fun () -> ()
            initRepository = fun _ -> ()
            fetch = fun () -> ()
            pull = fun () -> ()
            push = fun () -> ()
            cloneRepository = fun _ -> promise { return Ok "" }
            sync = fun () -> ()
            commitSelection = fun _ -> ()
            commitAll = fun _ -> ()
            saveLfsAutoTrackThreshold = fun _ -> ()
            saveDownloadLargeFiles = fun _ -> ()
            createBranch = fun _ -> ()
            switchBranch = fun _ -> ()
            selectChange = fun _ -> promise { return Ok() }
            confirmMergeResolution = fun _ -> ()
        }
    )

[<Hook>]
let useGitState () = React.useContext GitStateCtx

[<ReactComponent>]
let GitStateCtxProvider (children: ReactElement) =

    let appStateCtx = Renderer.Context.AppStateContext.useAppState ()
    let pageStateCtx = Renderer.Context.PageStateContext.usePageState ()

    let gitState, dispatch =
        React.useElmish ((fun () -> init ()), update dependencies pageStateCtx.setState, subscribe, [||])

    let refresh () = dispatch RefreshRequested

    let initRepository remoteProjectName =
        dispatch (InitRepositoryRequested remoteProjectName)

    let fetch () = dispatch FetchRequested

    let pull () = dispatch PullRequested

    let push () = dispatch PushRequested

    let cloneRepository (request: GitCloneRepositoryRequest) =
        Promise.create (fun resolve _reject -> dispatch (CloneRequested(request, resolve)))

    let sync () = dispatch SyncRequested

    let commitSelection (request: GitSidebarCommitSelectionRequest) =
        dispatch (CommitSelectionRequested request)

    let commitAll (message: string) = dispatch (CommitAllRequested message)

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

    React.useEffect ((fun () -> dispatch (ArcPathChanged appStateCtx.state)), [| box appStateCtx.state |])

    let gitStateController: GitStateController =
        React.useMemo (
            (fun _ -> {
                state = gitState
                refresh = refresh
                initRepository = initRepository
                fetch = fetch
                pull = pull
                push = push
                cloneRepository = cloneRepository
                sync = sync
                commitSelection = commitSelection
                commitAll = commitAll
                saveLfsAutoTrackThreshold = saveLfsAutoTrackThreshold
                saveDownloadLargeFiles = saveDownloadLargeFiles
                createBranch = createBranchFrom
                switchBranch = switchBranchTo
                selectChange = selectChange
                confirmMergeResolution = confirmMergeResolutionAction
            }),
            [| box gitState |]
        )

    GitStateCtx.Provider(gitStateController, children)
