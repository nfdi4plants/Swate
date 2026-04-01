module Renderer.Context.GitStateCtx

open Browser.Dom
open Fable.Core
open Fable.Electron.Remoting.Renderer
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

let private mapDiffPageResult (_requestedPath: string) = function
    | Ok(GitPageLoadResultDto.Loaded diffData) -> Ok(GitPage.Diff diffData)
    | Ok(GitPageLoadResultDto.Unsupported unsupportedPage) -> Ok(GitPage.Unsupported unsupportedPage)
    | Error message -> Error message

let private mapMergeConflictPageResult (_requestedPath: string) = function
    | Ok(GitPageLoadResultDto.Loaded mergeData) -> Ok(GitPage.MergeConflict mergeData)
    | Ok(GitPageLoadResultDto.Unsupported unsupportedPage) -> Ok(GitPage.Unsupported unsupportedPage)
    | Error message -> Error message

let private dependencies: GitDependencies = {
    getGitStatus = Renderer.GitApiClient.getGitStatus
    getGitBranches = Renderer.GitApiClient.getGitBranches
    getGitLfsSettings = Renderer.GitApiClient.getGitLfsSettings
    loadDiffPage =
        fun requestedPath ->
            promise {
                let! result = Renderer.GitApiClient.getGitDiffViewData requestedPath
                return mapDiffPageResult requestedPath result
            }
    loadMergeConflictPage =
        fun requestedPath ->
            promise {
                let! result = Renderer.GitApiClient.getGitMergeConflictViewData requestedPath
                return mapMergeConflictPageResult requestedPath result
            }
    installGitLfs = Renderer.GitApiClient.installGitLfs
    gitFetch = Renderer.GitApiClient.gitFetch
    gitPull = Renderer.GitApiClient.gitPull
    gitPush = Renderer.GitApiClient.gitPush
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
    let gitState, elmishDispatch = React.useElmish ((fun () -> init appStateCtx.state), update, [| box appStateCtx.state |])
    let gitStateRef = React.useRef gitState

    gitStateRef.current <- gitState

    let dispatch msg =
        let nextState = transition msg gitStateRef.current
        gitStateRef.current <- nextState
        elmishDispatch msg

    let getState () = gitStateRef.current
    let isArcLoaded () = appStateCtx.state.IsSome

    let refresh () = promise {
        dispatch (SetBusyOperation(Some GitBusyOperation.Refreshing))
        let! result = refreshAll dependencies isArcLoaded getState dispatch
        dispatch (SetBusyOperation None)
        return result |> Result.map ignore
    }

    let fetch () =
        runWriteOperation
            dependencies
            isArcLoaded
            getState
            dispatch
            GitBusyOperation.FetchingFromRemote
            (fun () ->
                dependencies.gitFetch {
                    Remote = None
                    Branch = None
                })

    let pull () = promise {
        let! result = runPullWorkflow dependencies isArcLoaded getState dispatch
        return result |> Result.map ignore
    }

    let push () =
        runPushWorkflow dependencies isArcLoaded getState dispatch

    let sync () = promise {
        return! runSyncWorkflow dependencies isArcLoaded getState dispatch
    }

    let commitSelection (request: GitSidebarCommitSelectionRequest) =
        runCommitOperation
            dependencies
            isArcLoaded
            getState
            dispatch
            GitBusyOperation.CommittingSelectedChanges
            request.Paths
            true
            request.Message

    let commitAll (message: string) =
        let state = getState ()

        let allChangedPaths =
            state.ChangedFiles
            |> Array.map _.Path
            |> Array.distinct

        runCommitOperation
            dependencies
            isArcLoaded
            getState
            dispatch
            GitBusyOperation.CommittingAllChanges
            allChangedPaths
            false
            message

    let saveLfsAutoTrackThreshold (thresholdMb: int) = promise {
        let! result =
            runWriteOperation
                dependencies
                isArcLoaded
                getState
                dispatch
                GitBusyOperation.SavingGitLfsThreshold
                (fun () ->
                    let state = getState ()

                    dependencies.setGitLfsSettings {
                        AutoTrackThresholdMb = thresholdMb
                        DownloadLargeFiles = state.DownloadLargeFiles
                    })

        match result with
        | Ok () ->
            let state = getState ()

            dispatch (SetLfsSettings(Some {
                AutoTrackThresholdMb = thresholdMb
                DownloadLargeFiles = state.DownloadLargeFiles
            }))

            return Ok()
        | Error message ->
            return Error message
    }

    let saveDownloadLargeFiles (downloadLargeFiles: bool) = promise {
        let! result =
            runWriteOperation
                dependencies
                isArcLoaded
                getState
                dispatch
                GitBusyOperation.SavingGitLfsDownloadPreference
                (fun () ->
                    let state = getState ()

                    dependencies.setGitLfsSettings {
                        AutoTrackThresholdMb = state.LfsAutoTrackThresholdMb
                        DownloadLargeFiles = downloadLargeFiles
                    })

        match result with
        | Ok () ->
            let state = getState ()

            dispatch (SetLfsSettings(Some {
                AutoTrackThresholdMb = state.LfsAutoTrackThresholdMb
                DownloadLargeFiles = downloadLargeFiles
            }))

            return Ok()
        | Error message ->
            return Error message
    }

    let createBranchFrom (request: GitSidebarCreateBranchRequest) =
        runWriteOperation
            dependencies
            isArcLoaded
            getState
            dispatch
            GitBusyOperation.CreatingBranch
            (fun () ->
                dependencies.createBranch {
                    Name = request.BranchName
                    StartPoint = request.StartPoint
                })

    let switchBranchTo (branchName: string) =
        let normalizedBranchName = branchName.Trim()

        if System.String.IsNullOrWhiteSpace normalizedBranchName then
            promise { return Error "Branch name must not be empty." }
        else
            runWriteOperation
                dependencies
                isArcLoaded
                getState
                dispatch
                GitBusyOperation.SwitchingBranch
                (fun () ->
                    dependencies.checkoutBranch {
                        Name = normalizedBranchName
                    })

    let selectChange (change: GitSidebarChange) =
        loadPage dependencies getState dispatch change.Path change.IsConflicted

    let confirmMergeResolutionAction request =
        Renderer.Context.GitWorkflow.confirmMergeResolution dependencies isArcLoaded getState dispatch request

    let ipcHandler: IMainUpdateRendererApi = {
        IMainUpdateRendererApi.empty with
            gitProgressUpdate =
                fun progress ->
                    dispatch (SetCurrentProgress(Some(mapProgress progress)))
    }

    React.useEffectOnce (fun _ -> Remoting.init |> Remoting.buildHandler ipcHandler)

    React.useEffect (
        (fun () ->
            match gitState.ActivePage with
            | Some(GitPage.Diff diffData) ->
                pageStateCtx.setState (Some(PageState.GitDiffPage diffData))
            | Some(GitPage.MergeConflict mergeData) ->
                pageStateCtx.setState (Some(PageState.GitMergeConflictPage mergeData))
            | Some(GitPage.Unsupported unsupportedPage) ->
                pageStateCtx.setState (Some(PageState.GitUnsupportedPage unsupportedPage))
            | None ->
                pageStateCtx.setState None),
        [| box gitState.ActivePage |]
    )

    React.useEffect (
        (fun () ->
            if appStateCtx.state.IsSome then
                refreshAll dependencies isArcLoaded getState dispatch |> Promise.start
            else
                dispatch ResetWorkflow),
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
