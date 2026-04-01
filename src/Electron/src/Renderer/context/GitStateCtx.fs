module Renderer.Context.GitStateCtx

open System
open Browser.Dom
open Elmish
open Fable.Core
open Fable.Electron.Remoting.Renderer
open Feliz
open Feliz.UseElmish

open Renderer.Types
open Swate.Components.GitSidebarTypes
open Swate.Electron.Shared
open Swate.Electron.Shared.GitTypes
open Swate.Electron.Shared.IPCTypes

[<RequireQualifiedAccess>]
type GitRefreshState =
    | Idle
    | Loading

[<RequireQualifiedAccess>]
type GitBusyOperation =
    | Refreshing
    | FetchingFromRemote
    | PullingFromRemote
    | PushingToRemote
    | CommittingSelectedChanges
    | CommittingAllChanges
    | SavingGitLfsThreshold
    | SavingGitLfsDownloadPreference
    | CreatingBranch
    | SwitchingBranch
    | InstallingGitLfs
    | ConfirmingMergeResolution of path: string

[<RequireQualifiedAccess>]
type GitInstallRetryState =
    | Idle
    | PromptingForInstall of promptMessage: string * retryOperation: GitBusyOperation
    | InstallingForRetry of retryOperation: GitBusyOperation

[<RequireQualifiedAccess>]
type GitPage =
    | Diff of GitDiffViewDataDto
    | MergeConflict of GitMergeConflictViewDataDto
    | Unsupported of GitUnsupportedPageData

type GitState = {
    Status: GitSidebarStatus
    ChangedFiles: GitSidebarChange[]
    BranchOptions: GitSidebarBranchOption[]
    LfsAutoTrackThresholdMb: int
    DownloadLargeFiles: bool
    RefreshState: GitRefreshState
    BusyOperation: GitBusyOperation option
    BusyNotice: string option
    CurrentProgress: GitSidebarProgress option
    ErrorNotice: string option
    SelectedChangePath: string option
    ActivePage: GitPage option
    MergeResolutionPendingPath: string option
    InstallRetryState: GitInstallRetryState
} with

    static member Empty = {
        Status = {
            CurrentBranch = None
            TrackingBranch = None
            Ahead = 0
            Behind = 0
            IsClean = true
            IsMergeInProgress = false
        }
        ChangedFiles = [||]
        BranchOptions = [||]
        LfsAutoTrackThresholdMb = 1
        DownloadLargeFiles = true
        RefreshState = GitRefreshState.Idle
        BusyOperation = None
        BusyNotice = None
        CurrentProgress = None
        ErrorNotice = None
        SelectedChangePath = None
        ActivePage = None
        MergeResolutionPendingPath = None
        InstallRetryState = GitInstallRetryState.Idle
    }

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

type private Msg =
    | ResetWorkflow
    | SetRefreshState of GitRefreshState
    | SetBusyOperation of GitBusyOperation option
    | SetCurrentProgress of GitSidebarProgress option
    | SetErrorNotice of string option
    | SetSelectedChangePath of string option
    | SetStatus of GitStatusDto option
    | SetBranches of GitBranchRefDto[] option
    | SetLfsSettings of GitLfsSettingsDto option
    | SetActivePage of GitPage option
    | SetMergeResolutionPendingPath of string option
    | SetInstallRetryState of GitInstallRetryState

let private staleMergeConflictTokens =
    [|
        "not currently marked as conflicted"
        "changed on disk since it was opened"
    |]

let private isStaleMergeConflictError (message: string) =
    let normalizedMessage = message.ToLowerInvariant()
    staleMergeConflictTokens |> Array.exists (fun token -> normalizedMessage.Contains(token))

let private busyNoticeFromOperation = function
    | GitBusyOperation.Refreshing -> Some "Refreshing Git state"
    | GitBusyOperation.FetchingFromRemote -> Some "Fetching from remote"
    | GitBusyOperation.PullingFromRemote -> Some "Pulling from remote"
    | GitBusyOperation.PushingToRemote -> Some "Pushing to remote"
    | GitBusyOperation.CommittingSelectedChanges -> Some "Committing selected changes"
    | GitBusyOperation.CommittingAllChanges -> Some "Committing all changes"
    | GitBusyOperation.SavingGitLfsThreshold -> Some "Saving Git LFS threshold"
    | GitBusyOperation.SavingGitLfsDownloadPreference -> Some "Saving Git LFS download preference"
    | GitBusyOperation.CreatingBranch -> Some "Creating branch"
    | GitBusyOperation.SwitchingBranch -> Some "Switching branch"
    | GitBusyOperation.InstallingGitLfs -> Some "Installing Git LFS"
    | GitBusyOperation.ConfirmingMergeResolution _ -> Some "Confirming merge resolution"

let private mapStatus (status: GitStatusDto) : GitSidebarStatus = {
    CurrentBranch = status.Current
    TrackingBranch = status.Tracking
    Ahead = status.Ahead
    Behind = status.Behind
    IsClean = status.IsClean
    IsMergeInProgress = status.IsMergeInProgress
}

let private mapChanges (status: GitStatusDto) : GitSidebarChange[] =
    let conflictedPaths = status.Conflicted |> Set.ofArray

    status.Files
    |> Array.map (fun file -> {
        Path = file.Path
        OriginalPath = file.OriginalPath
        IndexStatus = file.Index
        WorkingTreeStatus = file.WorkingDir
        IsConflicted = conflictedPaths.Contains file.Path
    })

let private mapBranchKind (kind: Swate.Electron.Shared.GitTypes.GitBranchRefKind) : GitSidebarBranchKind =
    match kind with
    | Swate.Electron.Shared.GitTypes.GitBranchRefKind.Local -> GitSidebarBranchKind.Local
    | Swate.Electron.Shared.GitTypes.GitBranchRefKind.Remote -> GitSidebarBranchKind.Remote

let private mapBranches (branches: GitBranchRefDto[]) : GitSidebarBranchOption[] =
    branches
    |> Array.map (fun branch -> {
        RefName = branch.RefName
        DisplayLabel = branch.DisplayLabel
        Kind = mapBranchKind branch.Kind
        IsCurrent = branch.IsCurrent
        IsTracking = branch.IsTracking
    })

let private mapProgress (progress: GitProgressDto) : GitSidebarProgress = {
    Method = progress.Method
    Stage = progress.Stage
    ProgressPercent = progress.Progress
}

let private currentGitPagePath (page: GitPage option) =
    match page with
    | Some(GitPage.Diff data) -> Some data.Path
    | Some(GitPage.MergeConflict data) -> Some data.Path
    | Some(GitPage.Unsupported data) -> Some data.Path
    | None -> None

let private applyStatus (status: GitStatusDto) (model: GitState) =
    let mappedChanges = mapChanges status

    let nextSelectedPath =
        model.SelectedChangePath
        |> Option.filter (fun selectedPath -> mappedChanges |> Array.exists (fun change -> change.Path = selectedPath))

    let nextActivePage =
        match currentGitPagePath model.ActivePage with
        | Some pagePath when nextSelectedPath = Some pagePath -> model.ActivePage
        | Some _ -> None
        | None -> model.ActivePage

    {
        model with
            Status = mapStatus status
            ChangedFiles = mappedChanges
            SelectedChangePath = nextSelectedPath
            ActivePage = nextActivePage
    }

let private init (_appState: ArcRootPath) : GitState * Cmd<Msg> =
    GitState.Empty, Cmd.none

let private update (msg: Msg) (model: GitState) : GitState * Cmd<Msg> =
    match msg with
    | ResetWorkflow ->
        GitState.Empty, Cmd.none
    | SetRefreshState refreshState ->
        { model with RefreshState = refreshState }, Cmd.none
    | SetBusyOperation busyOperation ->
        {
            model with
                BusyOperation = busyOperation
                BusyNotice = busyOperation |> Option.bind busyNoticeFromOperation
        },
        Cmd.none
    | SetCurrentProgress currentProgress ->
        { model with CurrentProgress = currentProgress }, Cmd.none
    | SetErrorNotice errorNotice ->
        { model with ErrorNotice = errorNotice }, Cmd.none
    | SetSelectedChangePath selectedChangePath ->
        { model with SelectedChangePath = selectedChangePath }, Cmd.none
    | SetStatus None ->
        {
            model with
                Status = GitState.Empty.Status
                ChangedFiles = [||]
                SelectedChangePath = None
        },
        Cmd.none
    | SetStatus(Some status) ->
        applyStatus status model, Cmd.none
    | SetBranches None ->
        { model with BranchOptions = [||] }, Cmd.none
    | SetBranches(Some branches) ->
        { model with BranchOptions = mapBranches branches }, Cmd.none
    | SetLfsSettings None ->
        {
            model with
                LfsAutoTrackThresholdMb = GitState.Empty.LfsAutoTrackThresholdMb
                DownloadLargeFiles = GitState.Empty.DownloadLargeFiles
        },
        Cmd.none
    | SetLfsSettings(Some settings) ->
        {
            model with
                LfsAutoTrackThresholdMb = settings.AutoTrackThresholdMb
                DownloadLargeFiles = settings.DownloadLargeFiles
        },
        Cmd.none
    | SetActivePage activePage ->
        let nextMergePendingPath =
            match model.MergeResolutionPendingPath, currentGitPagePath activePage with
            | Some pendingPath, Some activePath when String.Equals(pendingPath, activePath, StringComparison.Ordinal) ->
                Some pendingPath
            | Some _, _ ->
                None
            | None, _ ->
                None

        {
            model with
                ActivePage = activePage
                MergeResolutionPendingPath = nextMergePendingPath
        },
        Cmd.none
    | SetMergeResolutionPendingPath mergeResolutionPendingPath ->
        { model with MergeResolutionPendingPath = mergeResolutionPendingPath }, Cmd.none
    | SetInstallRetryState installRetryState ->
        { model with InstallRetryState = installRetryState }, Cmd.none

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
    let gitState, dispatch = React.useElmish ((fun () -> init appStateCtx.state), update, [| box appStateCtx.state |])

    let refreshAll () = promise {
        if appStateCtx.state.IsNone then
            dispatch ResetWorkflow
            return Ok None
        else
            dispatch (SetRefreshState GitRefreshState.Loading)

            let! statusResult = Renderer.GitApiClient.getGitStatus ()
            let! branchResult = Renderer.GitApiClient.getGitBranches ()
            let! lfsSettingsResult = Renderer.GitApiClient.getGitLfsSettings ()

            dispatch (SetRefreshState GitRefreshState.Idle)

            match statusResult with
            | Ok status -> dispatch (SetStatus(Some status))
            | Error _ -> dispatch (SetStatus None)

            match statusResult, branchResult with
            | Ok _, Ok branches -> dispatch (SetBranches(Some branches))
            | _ -> dispatch (SetBranches None)

            match statusResult, lfsSettingsResult with
            | Ok _, Ok lfsSettings -> dispatch (SetLfsSettings(Some lfsSettings))
            | _ -> dispatch (SetLfsSettings None)

            let errorMessage =
                match statusResult, branchResult, lfsSettingsResult with
                | Ok _, Ok _, Ok _ -> None
                | Ok _, Error message, _ -> Some message
                | Ok _, _, Error message -> Some message
                | Error message, _, _ -> Some message

            dispatch (SetErrorNotice errorMessage)

            return
                match statusResult, errorMessage with
                | Ok status, None -> Ok(Some status)
                | Ok _, Some message -> Error message
                | Error message, _ -> Error message
    }

    let openDiffPage path = promise {
        let! result = Renderer.GitApiClient.getGitDiffViewData path

        match result with
        | Ok(Renderer.GitApiClient.Loaded diffData) ->
            dispatch (SetSelectedChangePath(Some path))
            dispatch (SetErrorNotice None)
            dispatch (SetActivePage(Some(GitPage.Diff diffData)))
            return Ok()
        | Ok(Renderer.GitApiClient.Unsupported unsupportedPage) ->
            dispatch (SetSelectedChangePath(Some path))
            dispatch (SetErrorNotice None)
            dispatch (SetActivePage(Some(GitPage.Unsupported unsupportedPage)))
            return Ok()
        | Error message ->
            dispatch (SetErrorNotice(Some message))
            return Error message
    }

    let openMergeConflictPage path = promise {
        let! result = Renderer.GitApiClient.getGitMergeConflictViewData path

        match result with
        | Ok(Renderer.GitApiClient.Loaded mergeData) ->
            dispatch (SetSelectedChangePath(Some path))
            dispatch (SetErrorNotice None)
            dispatch (SetActivePage(Some(GitPage.MergeConflict mergeData)))
            return Ok()
        | Ok(Renderer.GitApiClient.Unsupported unsupportedPage) ->
            dispatch (SetSelectedChangePath(Some path))
            dispatch (SetErrorNotice None)
            dispatch (SetActivePage(Some(GitPage.Unsupported unsupportedPage)))
            return Ok()
        | Error message ->
            dispatch (SetErrorNotice(Some message))
            return Error message
    }

    let openFirstConflictIfNeeded (status: GitStatusDto) = promise {
        match status.Conflicted |> Array.tryHead with
        | Some firstConflictPath -> return! openMergeConflictPage firstConflictPath
        | None -> return Ok()
    }

    let promptForGitLfsInstall (busyOperation: GitBusyOperation) (message: string) = promise {
        dispatch (SetInstallRetryState(GitInstallRetryState.PromptingForInstall(message, busyOperation)))

        let shouldInstall = window.confirm message

        if not shouldInstall then
            dispatch (SetInstallRetryState GitInstallRetryState.Idle)
            return Ok false
        else
            dispatch (SetInstallRetryState(GitInstallRetryState.InstallingForRetry busyOperation))
            dispatch (SetBusyOperation(Some GitBusyOperation.InstallingGitLfs))
            dispatch (SetErrorNotice None)

            let! installResult = Renderer.GitApiClient.installGitLfs ()

            dispatch (SetBusyOperation None)
            dispatch (SetInstallRetryState GitInstallRetryState.Idle)

            match installResult with
            | Error message ->
                return Error message
            | Ok operationResult when not operationResult.Success ->
                let message =
                    operationResult.Message
                    |> Option.defaultValue "Git LFS installation failed."

                return Error message
            | Ok _ ->
                return Ok true
    }

    let rec runGitOperationWithLfsInstallRetry
        (busyOperation: GitBusyOperation)
        (allowRetry: bool)
        (operation: unit -> JS.Promise<Result<GitOperationResult, string>>)
        =
        promise {
            let! result = operation ()

            match result with
            | Ok operationResult when allowRetry && operationResult.FailureKind = Some GitFailureKind.LfsInstallRequired ->
                let promptMessage =
                    operationResult.Message
                    |> Option.defaultValue "Git LFS is required for this operation. Install Git LFS now?"

                let! promptResult = promptForGitLfsInstall busyOperation promptMessage

                match promptResult with
                | Error message ->
                    return
                        Ok {
                            Success = false
                            Message = Some message
                            FailureKind = Some GitFailureKind.LfsInstallRequired
                            Path = None
                        }
                | Ok false ->
                    return
                        Ok {
                            operationResult with
                                Message = Some "Git LFS installation is required to continue."
                        }
                | Ok true ->
                    dispatch (SetBusyOperation(Some busyOperation))
                    return! runGitOperationWithLfsInstallRetry busyOperation false operation
            | _ ->
                return result
        }

    let runWriteOperation
        (busyOperation: GitBusyOperation)
        (operation: unit -> JS.Promise<Result<GitOperationResult, string>>)
        =
        promise {
            if appStateCtx.state.IsNone then
                return Error "No ARC is loaded."
            else
                dispatch (SetBusyOperation(Some busyOperation))
                dispatch (SetErrorNotice None)

                let! result = runGitOperationWithLfsInstallRetry busyOperation true operation

                match result with
                | Error message ->
                    dispatch (SetBusyOperation None)
                    dispatch (SetCurrentProgress None)
                    dispatch (SetErrorNotice(Some message))
                    return Error message
                | Ok operationResult when not operationResult.Success ->
                    let message =
                        operationResult.Message
                        |> Option.defaultValue ((busyNoticeFromOperation busyOperation) |> Option.defaultValue "Git operation failed.")

                    dispatch (SetBusyOperation None)
                    dispatch (SetCurrentProgress None)
                    dispatch (SetErrorNotice(Some message))
                    return Error message
                | Ok _ ->
                    let! refreshResult = refreshAll ()

                    dispatch (SetBusyOperation None)
                    dispatch (SetCurrentProgress None)

                    match refreshResult with
                    | Ok _ ->
                        dispatch (SetErrorNotice None)
                        return Ok()
                    | Error message ->
                        dispatch (SetErrorNotice(Some message))
                        return Error message
        }

    let refresh () = promise {
        dispatch (SetBusyOperation(Some GitBusyOperation.Refreshing))
        let! result = refreshAll ()
        dispatch (SetBusyOperation None)
        return result |> Result.map ignore
    }

    let fetch () =
        runWriteOperation GitBusyOperation.FetchingFromRemote (fun () ->
            Renderer.GitApiClient.gitFetch {
                Remote = None
                Branch = None
            })

    let runPullWorkflow () = promise {
        if appStateCtx.state.IsNone then
            return Error "No ARC is loaded."
        else
            dispatch (SetBusyOperation(Some GitBusyOperation.PullingFromRemote))
            dispatch (SetErrorNotice None)

            let! result =
                runGitOperationWithLfsInstallRetry
                    GitBusyOperation.PullingFromRemote
                    true
                    (fun () ->
                        Renderer.GitApiClient.gitPull {
                            Remote = None
                            Branch = None
                        })

            match result with
            | Error message ->
                dispatch (SetBusyOperation None)
                dispatch (SetCurrentProgress None)
                dispatch (SetErrorNotice(Some message))
                return Error message
            | Ok operationResult when not operationResult.Success ->
                let message = operationResult.Message |> Option.defaultValue "Pull failed."
                dispatch (SetBusyOperation None)
                dispatch (SetCurrentProgress None)
                dispatch (SetErrorNotice(Some message))
                return Error message
            | Ok _ ->
                let! refreshResult = refreshAll ()

                dispatch (SetBusyOperation None)
                dispatch (SetCurrentProgress None)

                match refreshResult with
                | Error message ->
                    dispatch (SetErrorNotice(Some message))
                    return Error message
                | Ok(Some latestStatus) when latestStatus.Conflicted.Length > 0 ->
                    let! openResult = openFirstConflictIfNeeded latestStatus

                    match openResult with
                    | Ok () ->
                        dispatch (SetErrorNotice None)
                        return Ok(Some latestStatus)
                    | Error message ->
                        dispatch (SetErrorNotice(Some message))
                        return Error message
                | Ok latestStatusOption ->
                    dispatch (SetErrorNotice None)
                    return Ok latestStatusOption
    }

    let pull () = promise {
        let! result = runPullWorkflow ()
        return result |> Result.map ignore
    }

    let push () =
        runWriteOperation GitBusyOperation.PushingToRemote (fun () ->
            Renderer.GitApiClient.gitPush {
                Remote = None
                Branch = None
            })

    let sync () = promise {
        let! pullResult = runPullWorkflow ()

        match pullResult with
        | Error message ->
            return Error message
        | Ok(Some latestStatus) when latestStatus.Conflicted.Length > 0 || latestStatus.IsMergeInProgress ->
            return Ok()
        | Ok _ ->
            return! push ()
    }

    let runCommitOperation
        (busyOperation: GitBusyOperation)
        (pathsToCommit: string[])
        (clearExistingStage: bool)
        (message: string)
        =
        promise {
            let normalizedMessage = message.Trim()

            if String.IsNullOrWhiteSpace normalizedMessage then
                return Error "Commit message must not be empty."
            elif pathsToCommit.Length = 0 then
                return Error "No changes available to commit."
            elif appStateCtx.state.IsNone then
                return Error "No ARC is loaded."
            else
                let normalizedPaths = pathsToCommit |> Array.distinct

                let currentlyStagedPaths =
                    if clearExistingStage then
                        gitState.ChangedFiles
                        |> Array.filter (fun change -> GitStatusCode.isStagedIndexStatus change.IndexStatus)
                        |> Array.map _.Path
                        |> Array.distinct
                    else
                        [||]

                dispatch (SetBusyOperation(Some busyOperation))
                dispatch (SetErrorNotice None)

                let! prepareStageResult =
                    if currentlyStagedPaths.Length = 0 then
                        promise { return Ok() }
                    else
                        promise {
                            let! result =
                                Renderer.GitApiClient.gitUnstagePaths {
                                    Pathspecs = currentlyStagedPaths
                                }

                            return
                                match result with
                                | Error message -> Error message
                                | Ok operationResult when not operationResult.Success ->
                                    Error(
                                        operationResult.Message
                                        |> Option.defaultValue "Preparing the selected commit failed."
                                    )
                                | Ok _ -> Ok()
                        }

                match prepareStageResult with
                | Error message ->
                    dispatch (SetBusyOperation None)
                    dispatch (SetCurrentProgress None)
                    dispatch (SetErrorNotice(Some message))
                    return Error message
                | Ok () ->
                    let! stageResult =
                        runGitOperationWithLfsInstallRetry
                            busyOperation
                            true
                            (fun () ->
                                Renderer.GitApiClient.gitStagePaths {
                                    Pathspecs = normalizedPaths
                                })

                    match stageResult with
                    | Error message ->
                        dispatch (SetBusyOperation None)
                        dispatch (SetCurrentProgress None)
                        dispatch (SetErrorNotice(Some message))
                        return Error message
                    | Ok operationResult when not operationResult.Success ->
                        let message =
                            operationResult.Message
                            |> Option.defaultValue "Staging changes before commit failed."

                        dispatch (SetBusyOperation None)
                        dispatch (SetCurrentProgress None)
                        dispatch (SetErrorNotice(Some message))
                        return Error message
                    | Ok _ ->
                        let! commitResult =
                            runGitOperationWithLfsInstallRetry
                                busyOperation
                                true
                                (fun () ->
                                    Renderer.GitApiClient.gitCommit {
                                        Message = normalizedMessage
                                    })

                        match commitResult with
                        | Error message ->
                            dispatch (SetBusyOperation None)
                            dispatch (SetCurrentProgress None)
                            dispatch (SetErrorNotice(Some message))
                            return Error message
                        | Ok operationResult when not operationResult.Success ->
                            let message =
                                operationResult.Message
                                |> Option.defaultValue "Commit failed."

                            dispatch (SetBusyOperation None)
                            dispatch (SetCurrentProgress None)
                            dispatch (SetErrorNotice(Some message))
                            return Error message
                        | Ok _ ->
                            let! refreshResult = refreshAll ()

                            dispatch (SetBusyOperation None)
                            dispatch (SetCurrentProgress None)

                            match refreshResult with
                            | Ok _ ->
                                dispatch (SetErrorNotice None)
                                return Ok()
                            | Error message ->
                                dispatch (SetErrorNotice(Some message))
                                return Error message
        }

    let commitSelection (request: GitSidebarCommitSelectionRequest) =
        runCommitOperation GitBusyOperation.CommittingSelectedChanges request.Paths true request.Message

    let commitAll (message: string) =
        let allChangedPaths =
            gitState.ChangedFiles
            |> Array.map _.Path
            |> Array.distinct

        runCommitOperation GitBusyOperation.CommittingAllChanges allChangedPaths false message

    let saveLfsAutoTrackThreshold (thresholdMb: int) = promise {
        let! result =
            runWriteOperation
                GitBusyOperation.SavingGitLfsThreshold
                (fun () ->
                    Renderer.GitApiClient.setGitLfsSettings {
                        AutoTrackThresholdMb = thresholdMb
                        DownloadLargeFiles = gitState.DownloadLargeFiles
                    })

        match result with
        | Ok () ->
            dispatch (SetLfsSettings(Some {
                AutoTrackThresholdMb = thresholdMb
                DownloadLargeFiles = gitState.DownloadLargeFiles
            }))
            return Ok()
        | Error message ->
            return Error message
    }

    let saveDownloadLargeFiles (downloadLargeFiles: bool) = promise {
        let! result =
            runWriteOperation
                GitBusyOperation.SavingGitLfsDownloadPreference
                (fun () ->
                    Renderer.GitApiClient.setGitLfsSettings {
                        AutoTrackThresholdMb = gitState.LfsAutoTrackThresholdMb
                        DownloadLargeFiles = downloadLargeFiles
                    })

        match result with
        | Ok () ->
            dispatch (SetLfsSettings(Some {
                AutoTrackThresholdMb = gitState.LfsAutoTrackThresholdMb
                DownloadLargeFiles = downloadLargeFiles
            }))
            return Ok()
        | Error message ->
            return Error message
    }

    let createBranchFrom (request: GitSidebarCreateBranchRequest) =
        runWriteOperation GitBusyOperation.CreatingBranch (fun () ->
            Renderer.GitApiClient.createBranch {
                Name = request.BranchName
                StartPoint = request.StartPoint
            })

    let switchBranchTo (branchName: string) =
        let normalizedBranchName = branchName.Trim()

        if String.IsNullOrWhiteSpace normalizedBranchName then
            promise { return Error "Branch name must not be empty." }
        else
            runWriteOperation GitBusyOperation.SwitchingBranch (fun () ->
                Renderer.GitApiClient.checkoutBranch {
                    Name = normalizedBranchName
                })

    let selectChange (change: GitSidebarChange) =
        if change.IsConflicted then
            openMergeConflictPage change.Path
        else
            openDiffPage change.Path

    let confirmMergeResolution (request: GitConfirmMergeResolutionRequest) = promise {
        if appStateCtx.state.IsNone then
            return Error "No ARC is loaded."
        else
            dispatch (SetBusyOperation(Some(GitBusyOperation.ConfirmingMergeResolution request.Path)))
            dispatch (SetMergeResolutionPendingPath(Some request.Path))
            dispatch (SetErrorNotice None)

            let! result = Renderer.GitApiClient.confirmGitMergeResolution request

            dispatch (SetBusyOperation None)
            dispatch (SetCurrentProgress None)

            match result with
            | Error message ->
                dispatch (SetMergeResolutionPendingPath None)

                if isStaleMergeConflictError message then
                    let! _ = refreshAll ()
                    dispatch (SetSelectedChangePath None)
                    dispatch (SetActivePage None)

                dispatch (SetErrorNotice(Some message))
                return Error message
            | Ok payload ->
                dispatch (SetStatus(Some payload.UpdatedStatus))
                dispatch (SetMergeResolutionPendingPath None)

                match payload.NextConflictedPath with
                | Some nextConflictPath ->
                    let! openResult = openMergeConflictPage nextConflictPath

                    match openResult with
                    | Ok () ->
                        dispatch (SetErrorNotice None)
                        return Ok()
                    | Error message ->
                        dispatch (SetErrorNotice(Some message))
                        return Error message
                | None ->
                    dispatch (SetSelectedChangePath None)
                    dispatch (SetActivePage None)
                    dispatch (SetErrorNotice None)
                    return Ok()
    }

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
                refreshAll () |> Promise.start
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
                confirmMergeResolution = confirmMergeResolution
            }),
            [| box gitState |]
        )

    GitStateCtx.Provider(gitStateController, children)
