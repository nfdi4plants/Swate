module Renderer.Context.GitStateCtx

open Browser.Dom
open Fable.Core
open Feliz
open Feliz.UseElmish

open Renderer.Types
open Swate.Components.GitSidebarTypes
open Swate.Electron.Shared
open Swate.Electron.Shared.GitTypes
open Swate.Electron.Shared.IPCTypes

open Renderer.Context.GitWorkflow

type GitStateController = {
    state: GitState
    refresh: unit -> JS.Promise<Result<unit, string>>
    fetch: unit -> JS.Promise<Result<unit, string>>
    pull: unit -> JS.Promise<Result<unit, string>>
    push: unit -> JS.Promise<Result<unit, string>>
    cloneRepository: GitCloneRepositoryRequest -> JS.Promise<Result<string, string>>
    sync: unit -> JS.Promise<Result<unit, string>>
    commitSelection: GitSidebarCommitSelectionRequest -> JS.Promise<Result<unit, string>>
    commitAll: string -> JS.Promise<Result<unit, string>>
    saveLfsAutoTrackThreshold: int -> JS.Promise<Result<unit, string>>
    saveDownloadLargeFiles: bool -> JS.Promise<Result<unit, string>>
    createBranch: GitSidebarCreateBranchRequest -> JS.Promise<Result<unit, string>>
    switchBranch: string -> JS.Promise<Result<unit, string>>
    selectChange: GitSidebarChange -> JS.Promise<Result<unit, string>>
    confirmMergeResolution: GitConfirmMergeResolutionRequest -> JS.Promise<Result<unit, string>>
}

let private dispatchPromise
    (dispatch: Msg -> unit)
    (buildMsg: (Result<'T, string> -> unit) -> Msg)
    : JS.Promise<Result<'T, string>> =
    Promise.create (fun resolve _reject -> dispatch (buildMsg resolve))

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
    installGitLfs = Renderer.GitApiClient.installGitLfs
    gitFetch = Renderer.GitApiClient.gitFetch
    gitPull = Renderer.GitApiClient.gitPull
    gitPush = Renderer.GitApiClient.gitPush
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
            refresh = fun () -> promise { return Ok() }
            fetch = fun () -> promise { return Ok() }
            pull = fun () -> promise { return Ok() }
            push = fun () -> promise { return Ok() }
            cloneRepository = fun _ -> promise { return Ok "" }
            sync = fun () -> promise { return Ok() }
            commitSelection = fun _ -> promise { return Ok() }
            commitAll = fun _ -> promise { return Ok() }
            saveLfsAutoTrackThreshold = fun _ -> promise { return Ok() }
            saveDownloadLargeFiles = fun _ -> promise { return Ok() }
            createBranch = fun _ -> promise { return Ok() }
            switchBranch = fun _ -> promise { return Ok() }
            selectChange = fun _ -> promise { return Ok() }
            confirmMergeResolution = fun _ -> promise { return Ok() }
        }
    )

[<Hook>]
let useGitState () = React.useContext GitStateCtx

[<ReactComponent>]
let GitStateCtxProvider (children: ReactElement) =

    let appStateCtx = Renderer.Context.AppStateCtx.useAppState ()
    let pageStateCtx = Renderer.Context.PageStateCtx.usePageState ()
    let gitState, dispatch =
        React.useElmish (
            (fun () -> init ()),
            update dependencies pageStateCtx.setState,
            subscribe,
            [||]
        )

    let refresh () =
        dispatchPromise dispatch RefreshRequested

    let fetch () =
        dispatchPromise dispatch FetchRequested

    let pull () = promise {
        return! dispatchPromise dispatch PullRequested
    }

    let push () =
        dispatchPromise dispatch PushRequested

    let cloneRepository (request: GitCloneRepositoryRequest) =
        dispatchPromise dispatch (fun reply -> CloneRequested(request, reply))

    let sync () =
        dispatchPromise dispatch SyncRequested

    let commitSelection (request: GitSidebarCommitSelectionRequest) =
        dispatchPromise dispatch (fun reply -> CommitSelectionRequested(request, reply))

    let commitAll (message: string) =
        dispatchPromise dispatch (fun reply -> CommitAllRequested(message, reply))

    let saveLfsAutoTrackThreshold (thresholdMb: int) =
        dispatchPromise dispatch (fun reply -> SaveLfsAutoTrackThresholdRequested(thresholdMb, reply))

    let saveDownloadLargeFiles (downloadLargeFiles: bool) =
        dispatchPromise dispatch (fun reply -> SaveDownloadLargeFilesRequested(downloadLargeFiles, reply))

    let createBranchFrom (request: GitSidebarCreateBranchRequest) =
        dispatchPromise dispatch (fun reply -> CreateBranchRequested(request, reply))

    let switchBranchTo (branchName: string) =
        dispatchPromise dispatch (fun reply -> SwitchBranchRequested(branchName, reply))

    let selectChange (change: GitSidebarChange) =
        dispatchPromise dispatch (fun reply -> SelectChangeRequested(change, reply))

    let confirmMergeResolutionAction request =
        dispatchPromise dispatch (fun reply -> ConfirmMergeResolutionRequested(request, reply))

    React.useEffect (
        (fun () -> dispatch (ArcPathChanged appStateCtx.state)),
        [| box appStateCtx.state |]
    )

    let gitStateController: GitStateController =
        React.useMemo (
            (fun _ -> {
                state = gitState
                refresh = refresh
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
