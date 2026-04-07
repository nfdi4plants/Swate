module Renderer.Context.GitWorkflow

open System
open Elmish
open Fable.Core

open Renderer.Types
open Swate.Components.GitSidebarTypes
open Swate.Electron.Shared.GitTypes

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
    | CloningRepository
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
    RefreshRequestId: int
    BusyOperation: GitBusyOperation option
    BusyNotice: string option
    CurrentProgress: GitSidebarProgress option
    ErrorNotice: string option
    WarningNotice: string option
    SelectedChangePath: string option
    ActivePage: GitPage option
    MergeResolutionPendingPath: string option
    InstallRetryState: GitInstallRetryState
    PageLoadRequestId: int
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
        DownloadLargeFiles = false
        RefreshState = GitRefreshState.Idle
        RefreshRequestId = 0
        BusyOperation = None
        BusyNotice = None
        CurrentProgress = None
        ErrorNotice = None
        WarningNotice = None
        SelectedChangePath = None
        ActivePage = None
        MergeResolutionPendingPath = None
        InstallRetryState = GitInstallRetryState.Idle
        PageLoadRequestId = 0
    }

type GitRefreshResult = {
    Status: Result<GitStatusDto, string>
    Branches: Result<GitBranchRefDto[], string>
    LfsSettings: Result<GitLfsSettingsDto, string>
}

type Msg =
    | ResetWorkflow
    | SetBusyOperation of GitBusyOperation option
    | SetCurrentProgress of GitSidebarProgress option
    | SetErrorNotice of string option
    | SetWarningNotice of string option
    | SetSelectedChangePath of string option
    | SetStatus of GitStatusDto option
    | SetBranches of GitBranchRefDto[] option
    | SetLfsSettings of GitLfsSettingsDto option
    | SetActivePage of GitPage option
    | SetMergeResolutionPendingPath of string option
    | SetInstallRetryState of GitInstallRetryState
    | StartRefreshRequest of requestId: int
    | FinishRefreshRequest of requestId: int * refreshResult: GitRefreshResult
    | StartPageLoad of path: string * isConflicted: bool
    | FinishPageLoad of requestId: int * path: string * result: Result<GitPage, string>

type GitDependencies = {
    getGitStatus: unit -> JS.Promise<Result<GitStatusDto, string>>
    getGitBranches: unit -> JS.Promise<Result<GitBranchRefDto[], string>>
    getGitLfsSettings: unit -> JS.Promise<Result<GitLfsSettingsDto, string>>
    loadDiffPage: string -> JS.Promise<Result<GitPage, string>>
    loadMergeConflictPage: string -> JS.Promise<Result<GitPage, string>>
    installGitLfs: unit -> JS.Promise<Result<GitOperationResult, string>>
    gitFetch: GitRemoteOperationRequest -> JS.Promise<Result<GitOperationResult, string>>
    gitPull: GitRemoteOperationRequest -> JS.Promise<Result<GitOperationResult, string>>
    gitPush: GitRemoteOperationRequest -> JS.Promise<Result<GitOperationResult, string>>
    gitCloneRepository: GitCloneRepositoryRequest -> JS.Promise<Result<GitOperationResult, string>>
    createBranch: GitCreateBranchRequest -> JS.Promise<Result<GitOperationResult, string>>
    checkoutBranch: GitCheckoutBranchRequest -> JS.Promise<Result<GitOperationResult, string>>
    gitStagePaths: GitPathspecRequest -> JS.Promise<Result<GitOperationResult, string>>
    gitUnstagePaths: GitPathspecRequest -> JS.Promise<Result<GitOperationResult, string>>
    gitCommit: GitCommitRequest -> JS.Promise<Result<GitOperationResult, string>>
    setGitLfsSettings: GitLfsSettingsDto -> JS.Promise<Result<GitOperationResult, string>>
    confirmGitMergeResolution: GitConfirmMergeResolutionRequest -> JS.Promise<Result<GitConfirmMergeResolutionResult, string>>
    confirmInstall: string -> bool
}

let staleMergeConflictTokens =
    [|
        "not currently marked as conflicted"
        "changed on disk since it was opened"
    |]

let isStaleMergeConflictError (message: string) =
    let normalizedMessage = message.ToLowerInvariant()
    staleMergeConflictTokens |> Array.exists (fun token -> normalizedMessage.Contains(token))

let busyNoticeFromOperation = function
    | GitBusyOperation.Refreshing -> Some "Refreshing Git state"
    | GitBusyOperation.FetchingFromRemote -> Some "Fetching from remote"
    | GitBusyOperation.PullingFromRemote -> Some "Pulling from remote"
    | GitBusyOperation.PushingToRemote -> Some "Pushing to remote"
    | GitBusyOperation.CloningRepository -> Some "Cloning repository"
    | GitBusyOperation.CommittingSelectedChanges -> Some "Committing selected changes"
    | GitBusyOperation.CommittingAllChanges -> Some "Committing all changes"
    | GitBusyOperation.SavingGitLfsThreshold -> Some "Saving Git LFS threshold"
    | GitBusyOperation.SavingGitLfsDownloadPreference -> Some "Saving Git LFS download preference"
    | GitBusyOperation.CreatingBranch -> Some "Creating branch"
    | GitBusyOperation.SwitchingBranch -> Some "Switching branch"
    | GitBusyOperation.InstallingGitLfs -> Some "Installing Git LFS"
    | GitBusyOperation.ConfirmingMergeResolution _ -> Some "Confirming merge resolution"

let mapStatus (status: GitStatusDto) : GitSidebarStatus = {
    CurrentBranch = status.Current
    TrackingBranch = status.Tracking
    Ahead = status.Ahead
    Behind = status.Behind
    IsClean = status.IsClean
    IsMergeInProgress = status.IsMergeInProgress
}

let mapChanges (status: GitStatusDto) : GitSidebarChange[] =
    let conflictedPaths = status.Conflicted |> Set.ofArray

    status.Files
    |> Array.map (fun file -> {
        Path = file.Path
        OriginalPath = file.OriginalPath
        IndexStatus = file.Index
        WorkingTreeStatus = file.WorkingDir
        IsConflicted = conflictedPaths.Contains file.Path
    })

let private mapBranchKind (kind: GitBranchRefKind) : GitSidebarBranchKind =
    match kind with
    | GitBranchRefKind.Local -> GitSidebarBranchKind.Local
    | GitBranchRefKind.Remote -> GitSidebarBranchKind.Remote

let mapBranches (branches: GitBranchRefDto[]) : GitSidebarBranchOption[] =
    branches
    |> Array.map (fun branch -> {
        RefName = branch.RefName
        DisplayLabel = branch.DisplayLabel
        Kind = mapBranchKind branch.Kind
        IsCurrent = branch.IsCurrent
        IsTracking = branch.IsTracking
    })

let mapProgress (progress: GitProgressDto) : GitSidebarProgress = {
    Method = progress.Method
    Stage = progress.Stage
    ProgressPercent = progress.Progress
}

let private hasMatchingOriginBranch (branchName: string) (model: GitState) =
    model.BranchOptions
    |> Array.exists (fun branch ->
        branch.Kind = GitSidebarBranchKind.Remote
        && String.Equals(branch.RefName, $"origin/{branchName}", StringComparison.Ordinal)
    )

let shouldPublishCurrentBranchFirst (model: GitState) =
    match model.Status.CurrentBranch, model.Status.TrackingBranch with
    | Some currentBranch, None -> not (hasMatchingOriginBranch currentBranch model)
    | _ -> false

let currentGitPagePath (page: GitPage option) =
    match page with
    | Some(GitPage.Diff data) -> Some data.Path
    | Some(GitPage.MergeConflict data) -> Some data.Path
    | Some(GitPage.Unsupported data) -> Some data.Path
    | None -> None

let private withActivePage (activePage: GitPage option) (model: GitState) =
    {
        model with
            ActivePage = activePage
    }

let private refreshErrorMessage (refreshResult: GitRefreshResult) =
    match refreshResult.Status, refreshResult.Branches, refreshResult.LfsSettings with
    | Ok _, Ok _, Ok _ -> None
    | Ok _, Error message, _ -> Some message
    | Ok _, _, Error message -> Some message
    | Error message, _, _ -> Some message

let applyStatus (status: GitStatusDto) (model: GitState) =
    let mappedChanges = mapChanges status

    let nextSelectedPath =
        model.SelectedChangePath
        |> Option.filter (fun selectedPath -> mappedChanges |> Array.exists (fun change -> change.Path = selectedPath))

    {
        model with
            Status = mapStatus status
            ChangedFiles = mappedChanges
            SelectedChangePath = nextSelectedPath
    }
    |> withActivePage model.ActivePage

let private applyRefreshResult (refreshResult: GitRefreshResult) (model: GitState) =
    let modelWithStatus =
        match refreshResult.Status with
        | Ok status -> applyStatus status model
        | Error _ ->
            {
                model with
                    Status = GitState.Empty.Status
                    ChangedFiles = [||]
                    SelectedChangePath = None
            }
            |> withActivePage None

    let modelWithBranches =
        match refreshResult.Status, refreshResult.Branches with
        | Ok _, Ok branches ->
            { modelWithStatus with BranchOptions = mapBranches branches }
        | _ ->
            { modelWithStatus with BranchOptions = [||] }

    let modelWithSettings =
        match refreshResult.Status, refreshResult.LfsSettings with
        | Ok _, Ok settings ->
            {
                modelWithBranches with
                    LfsAutoTrackThresholdMb = settings.AutoTrackThresholdMb
                    DownloadLargeFiles = settings.DownloadLargeFiles
            }
        | _ ->
            {
                modelWithBranches with
                    LfsAutoTrackThresholdMb = GitState.Empty.LfsAutoTrackThresholdMb
                    DownloadLargeFiles = GitState.Empty.DownloadLargeFiles
            }

    {
        modelWithSettings with
            RefreshState = GitRefreshState.Idle
            ErrorNotice = refreshErrorMessage refreshResult
    }

let nextRefreshRequestId (model: GitState) = model.RefreshRequestId + 1

let nextPageLoadRequestId (model: GitState) = model.PageLoadRequestId + 1

let init (_appState: Swate.Electron.Shared.ArcRootPath) : GitState * Cmd<Msg> =
    GitState.Empty, Cmd.none

let transition (msg: Msg) (model: GitState) =
    match msg with
    | ResetWorkflow ->
        GitState.Empty
    | SetBusyOperation busyOperation ->
        {
            model with
                BusyOperation = busyOperation
                BusyNotice = busyOperation |> Option.bind busyNoticeFromOperation
        }
    | SetCurrentProgress currentProgress ->
        { model with CurrentProgress = currentProgress }
    | SetErrorNotice errorNotice ->
        { model with ErrorNotice = errorNotice }
    | SetWarningNotice warningNotice ->
        { model with WarningNotice = warningNotice }
    | SetSelectedChangePath selectedChangePath ->
        { model with SelectedChangePath = selectedChangePath }
    | SetStatus None ->
        {
            model with
                Status = GitState.Empty.Status
                ChangedFiles = [||]
                SelectedChangePath = None
        }
    | SetStatus(Some status) ->
        applyStatus status model
    | SetBranches None ->
        { model with BranchOptions = [||] }
    | SetBranches(Some branches) ->
        { model with BranchOptions = mapBranches branches }
    | SetLfsSettings None ->
        {
            model with
                LfsAutoTrackThresholdMb = GitState.Empty.LfsAutoTrackThresholdMb
                DownloadLargeFiles = GitState.Empty.DownloadLargeFiles
        }
    | SetLfsSettings(Some settings) ->
        {
            model with
                LfsAutoTrackThresholdMb = settings.AutoTrackThresholdMb
                DownloadLargeFiles = settings.DownloadLargeFiles
        }
    | SetActivePage activePage ->
        model |> withActivePage activePage
    | SetMergeResolutionPendingPath mergeResolutionPendingPath ->
        { model with MergeResolutionPendingPath = mergeResolutionPendingPath }
    | SetInstallRetryState installRetryState ->
        { model with InstallRetryState = installRetryState }
    | StartRefreshRequest requestId ->
        {
            model with
                RefreshRequestId = requestId
                RefreshState = GitRefreshState.Loading
        }
    | FinishRefreshRequest(requestId, refreshResult) ->
        if requestId <> model.RefreshRequestId then
            model
        else
            applyRefreshResult refreshResult model
    | StartPageLoad _ ->
        {
            model with
                PageLoadRequestId = nextPageLoadRequestId model
        }
    | FinishPageLoad(requestId, path, result) ->
        if requestId <> model.PageLoadRequestId then
            model
        else
            match result with
            | Ok page ->
                {
                    model with
                        SelectedChangePath = Some path
                        ErrorNotice = None
                }
                |> withActivePage (Some page)
            | Error message ->
                { model with ErrorNotice = Some message }

let update (msg: Msg) (model: GitState) : GitState * Cmd<Msg> =
    transition msg model, Cmd.none

let refreshAll
    (deps: GitDependencies)
    (isArcLoaded: unit -> bool)
    (getState: unit -> GitState)
    (dispatch: Msg -> unit)
    =
    promise {
        if not (isArcLoaded ()) then
            dispatch ResetWorkflow
            return Ok None
        else
            let requestId = nextRefreshRequestId (getState ())
            dispatch (StartRefreshRequest requestId)

            let! statusResult = deps.getGitStatus ()
            let! branchResult = deps.getGitBranches ()
            let! lfsSettingsResult = deps.getGitLfsSettings ()

            let refreshResult = {
                Status = statusResult
                Branches = branchResult
                LfsSettings = lfsSettingsResult
            }

            dispatch (FinishRefreshRequest(requestId, refreshResult))

            return
                if (getState ()).RefreshRequestId <> requestId then
                    Ok None
                else
                    match statusResult, refreshErrorMessage refreshResult with
                    | Ok status, None -> Ok(Some status)
                    | Ok _, Some message -> Error message
                    | Error message, _ -> Error message
    }

let loadPage
    (deps: GitDependencies)
    (getState: unit -> GitState)
    (dispatch: Msg -> unit)
    (path: string)
    (isConflicted: bool)
    =
    promise {
        let requestId = nextPageLoadRequestId (getState ())
        dispatch (StartPageLoad(path, isConflicted))

        let! result =
            if isConflicted then
                deps.loadMergeConflictPage path
            else
                deps.loadDiffPage path

        dispatch (FinishPageLoad(requestId, path, result))

        if (getState ()).PageLoadRequestId <> requestId then
            return Ok()
        else
            return result |> Result.map ignore
    }

let promptForGitLfsInstall
    (deps: GitDependencies)
    (dispatch: Msg -> unit)
    (busyOperation: GitBusyOperation)
    (message: string)
    =
    promise {
        dispatch (SetInstallRetryState(GitInstallRetryState.PromptingForInstall(message, busyOperation)))

        let shouldInstall = deps.confirmInstall message

        if not shouldInstall then
            dispatch (SetInstallRetryState GitInstallRetryState.Idle)
            return Ok false
        else
            dispatch (SetInstallRetryState(GitInstallRetryState.InstallingForRetry busyOperation))
            dispatch (SetBusyOperation(Some GitBusyOperation.InstallingGitLfs))
            dispatch (SetErrorNotice None)

            let! installResult = deps.installGitLfs ()

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
    (deps: GitDependencies)
    (dispatch: Msg -> unit)
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

            let! promptResult = promptForGitLfsInstall deps dispatch busyOperation promptMessage

            match promptResult with
            | Error message ->
                return
                    Ok {
                        Success = false
                        Message = Some message
                        FailureKind = Some GitFailureKind.LfsInstallRequired
                        WarningMessage = None
                        WarningKind = None
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
                return! runGitOperationWithLfsInstallRetry deps dispatch busyOperation false operation
        | _ ->
            return result
    }

let runWriteOperation
    (deps: GitDependencies)
    (isArcLoaded: unit -> bool)
    (getState: unit -> GitState)
    (dispatch: Msg -> unit)
    (busyOperation: GitBusyOperation)
    (operation: unit -> JS.Promise<Result<GitOperationResult, string>>)
    =
    promise {
        if not (isArcLoaded ()) then
            return Error "No ARC is loaded."
        else
            dispatch (SetBusyOperation(Some busyOperation))
            dispatch (SetErrorNotice None)
            dispatch (SetWarningNotice None)

            let! result = runGitOperationWithLfsInstallRetry deps dispatch busyOperation true operation

            match result with
            | Error message ->
                dispatch (SetBusyOperation None)
                dispatch (SetCurrentProgress None)
                dispatch (SetErrorNotice(Some message))
                dispatch (SetWarningNotice None)
                return Error message
            | Ok operationResult when not operationResult.Success ->
                let message =
                    operationResult.Message
                    |> Option.defaultValue ((busyNoticeFromOperation busyOperation) |> Option.defaultValue "Git operation failed.")

                dispatch (SetBusyOperation None)
                dispatch (SetCurrentProgress None)
                dispatch (SetErrorNotice(Some message))
                dispatch (SetWarningNotice None)
                return Error message
            | Ok operationResult ->
                let! refreshResult = refreshAll deps isArcLoaded getState dispatch

                dispatch (SetBusyOperation None)
                dispatch (SetCurrentProgress None)

                match refreshResult with
                | Ok(Some _) ->
                    dispatch (SetErrorNotice None)
                    dispatch (SetWarningNotice operationResult.WarningMessage)
                    return Ok()
                | Ok None ->
                    dispatch (SetWarningNotice operationResult.WarningMessage)
                    return Ok()
                | Error message ->
                    dispatch (SetErrorNotice(Some message))
                    dispatch (SetWarningNotice None)
                    return Error message
    }

let runPushWorkflow
    (deps: GitDependencies)
    (isArcLoaded: unit -> bool)
    (getState: unit -> GitState)
    (dispatch: Msg -> unit)
    =
    runWriteOperation
        deps
        isArcLoaded
        getState
        dispatch
        GitBusyOperation.PushingToRemote
        (fun () ->
            deps.gitPush {
                Remote = None
                Branch = None
            })

let runPullWorkflow
    (deps: GitDependencies)
    (isArcLoaded: unit -> bool)
    (getState: unit -> GitState)
    (dispatch: Msg -> unit)
    =
    promise {
        if not (isArcLoaded ()) then
            return Error "No ARC is loaded."
        else
            dispatch (SetBusyOperation(Some GitBusyOperation.PullingFromRemote))
            dispatch (SetErrorNotice None)
            dispatch (SetWarningNotice None)

            let! result =
                runGitOperationWithLfsInstallRetry
                    deps
                    dispatch
                    GitBusyOperation.PullingFromRemote
                    true
                    (fun () ->
                        deps.gitPull {
                            Remote = None
                            Branch = None
                        })

            match result with
            | Error message ->
                dispatch (SetBusyOperation None)
                dispatch (SetCurrentProgress None)
                dispatch (SetErrorNotice(Some message))
                dispatch (SetWarningNotice None)
                return Error message
            | Ok operationResult when not operationResult.Success ->
                let message = operationResult.Message |> Option.defaultValue "Pull failed."
                dispatch (SetBusyOperation None)
                dispatch (SetCurrentProgress None)
                dispatch (SetErrorNotice(Some message))
                dispatch (SetWarningNotice None)
                return Error message
            | Ok operationResult ->
                let! refreshResult = refreshAll deps isArcLoaded getState dispatch

                dispatch (SetBusyOperation None)
                dispatch (SetCurrentProgress None)

                match refreshResult with
                | Error message ->
                    dispatch (SetErrorNotice(Some message))
                    dispatch (SetWarningNotice None)
                    return Error message
                | Ok(Some latestStatus) when latestStatus.Conflicted.Length > 0 ->
                    let firstConflictPath = latestStatus.Conflicted.[0]
                    let! openResult = loadPage deps getState dispatch firstConflictPath true

                    match openResult with
                    | Ok () ->
                        dispatch (SetErrorNotice None)
                        dispatch (SetWarningNotice operationResult.WarningMessage)
                        return Ok(Some latestStatus)
                    | Error message ->
                        dispatch (SetErrorNotice(Some message))
                        dispatch (SetWarningNotice None)
                        return Error message
                | Ok(Some latestStatus) ->
                    dispatch (SetErrorNotice None)
                    dispatch (SetWarningNotice operationResult.WarningMessage)
                    return Ok(Some latestStatus)
                | Ok None ->
                    dispatch (SetWarningNotice operationResult.WarningMessage)
                    return Ok None
    }

let runSyncWorkflow
    (deps: GitDependencies)
    (isArcLoaded: unit -> bool)
    (getState: unit -> GitState)
    (dispatch: Msg -> unit)
    =
    promise {
        if shouldPublishCurrentBranchFirst (getState ()) then
            return! runPushWorkflow deps isArcLoaded getState dispatch
        else
            let! pullResult = runPullWorkflow deps isArcLoaded getState dispatch

            match pullResult with
            | Error message ->
                return Error message
            | Ok None ->
                return Ok()
            | Ok(Some _) when (getState ()).WarningNotice.IsSome ->
                return Ok()
            | Ok(Some latestStatus) when latestStatus.Conflicted.Length > 0 || latestStatus.IsMergeInProgress ->
                return Ok()
            | Ok(Some _) ->
                return! runPushWorkflow deps isArcLoaded getState dispatch
    }

let runCloneWorkflow (deps: GitDependencies) (dispatch: Msg -> unit) (request: GitCloneRepositoryRequest) =
    promise {
        dispatch (SetBusyOperation(Some GitBusyOperation.CloningRepository))
        dispatch (SetErrorNotice None)
        dispatch (SetWarningNotice None)

        let! result =
            runGitOperationWithLfsInstallRetry
                deps
                dispatch
                GitBusyOperation.CloningRepository
                true
                (fun () -> deps.gitCloneRepository request)

        dispatch (SetBusyOperation None)
        dispatch (SetCurrentProgress None)

        match result with
        | Error message ->
            dispatch (SetErrorNotice(Some message))
            dispatch (SetWarningNotice None)
            return Error message
        | Ok operationResult when not operationResult.Success ->
            let message = operationResult.Message |> Option.defaultValue "Clone failed."
            dispatch (SetErrorNotice(Some message))
            dispatch (SetWarningNotice None)
            return Error message
        | Ok operationResult ->
            dispatch (SetErrorNotice None)
            dispatch (SetWarningNotice operationResult.WarningMessage)
            return Ok(operationResult.Path |> Option.defaultValue request.TargetPath)
    }

let runCommitOperation
    (deps: GitDependencies)
    (isArcLoaded: unit -> bool)
    (getState: unit -> GitState)
    (dispatch: Msg -> unit)
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
        elif not (isArcLoaded ()) then
            return Error "No ARC is loaded."
        else
            let normalizedPaths = pathsToCommit |> Array.distinct

            let currentlyStagedPaths =
                if clearExistingStage then
                    let state = getState ()

                    state.ChangedFiles
                    |> Array.filter (fun change -> GitStatusCode.isStagedIndexStatus change.IndexStatus)
                    |> Array.map _.Path
                    |> Array.distinct
                else
                    [||]

            dispatch (SetBusyOperation(Some busyOperation))
            dispatch (SetErrorNotice None)
            dispatch (SetWarningNotice None)

            let! prepareStageResult =
                if currentlyStagedPaths.Length = 0 then
                    promise { return Ok() }
                else
                    promise {
                        let! result =
                            deps.gitUnstagePaths {
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
                dispatch (SetWarningNotice None)
                return Error message
            | Ok () ->
                let! stageResult =
                    runGitOperationWithLfsInstallRetry
                        deps
                        dispatch
                        busyOperation
                        true
                        (fun () ->
                            deps.gitStagePaths {
                                Pathspecs = normalizedPaths
                            })

                match stageResult with
                | Error message ->
                    dispatch (SetBusyOperation None)
                    dispatch (SetCurrentProgress None)
                    dispatch (SetErrorNotice(Some message))
                    dispatch (SetWarningNotice None)
                    return Error message
                | Ok operationResult when not operationResult.Success ->
                    let message =
                        operationResult.Message
                        |> Option.defaultValue "Staging changes before commit failed."

                    dispatch (SetBusyOperation None)
                    dispatch (SetCurrentProgress None)
                    dispatch (SetErrorNotice(Some message))
                    dispatch (SetWarningNotice None)
                    return Error message
                | Ok _ ->
                    let! commitResult =
                        runGitOperationWithLfsInstallRetry
                            deps
                            dispatch
                            busyOperation
                            true
                            (fun () ->
                                deps.gitCommit {
                                    Message = normalizedMessage
                                })

                    match commitResult with
                    | Error message ->
                        dispatch (SetBusyOperation None)
                        dispatch (SetCurrentProgress None)
                        dispatch (SetErrorNotice(Some message))
                        dispatch (SetWarningNotice None)
                        return Error message
                    | Ok operationResult when not operationResult.Success ->
                        let message =
                            operationResult.Message
                            |> Option.defaultValue "Commit failed."

                        dispatch (SetBusyOperation None)
                        dispatch (SetCurrentProgress None)
                        dispatch (SetErrorNotice(Some message))
                        dispatch (SetWarningNotice None)
                        return Error message
                    | Ok _ ->
                        let! refreshResult = refreshAll deps isArcLoaded getState dispatch

                        dispatch (SetBusyOperation None)
                        dispatch (SetCurrentProgress None)

                        match refreshResult with
                        | Ok(Some _) ->
                            dispatch (SetErrorNotice None)
                            dispatch (SetWarningNotice None)
                            return Ok()
                        | Ok None ->
                            dispatch (SetWarningNotice None)
                            return Ok()
                        | Error message ->
                            dispatch (SetErrorNotice(Some message))
                            dispatch (SetWarningNotice None)
                            return Error message
    }

let confirmMergeResolution
    (deps: GitDependencies)
    (isArcLoaded: unit -> bool)
    (getState: unit -> GitState)
    (dispatch: Msg -> unit)
    (request: GitConfirmMergeResolutionRequest)
    =
    promise {
        if not (isArcLoaded ()) then
            return Error "No ARC is loaded."
        else
            let currentState = getState ()

            match currentState.BusyOperation with
            | Some(GitBusyOperation.ConfirmingMergeResolution _) ->
                return Ok()
            | _ when currentState.MergeResolutionPendingPath = Some request.Path ->
                return Ok()
            | _ ->
                dispatch (SetBusyOperation(Some(GitBusyOperation.ConfirmingMergeResolution request.Path)))
                dispatch (SetMergeResolutionPendingPath(Some request.Path))
                dispatch (SetErrorNotice None)
                dispatch (SetWarningNotice None)

                let! result = deps.confirmGitMergeResolution request

                dispatch (SetBusyOperation None)
                dispatch (SetCurrentProgress None)

                match result with
                | Error message ->
                    dispatch (SetMergeResolutionPendingPath None)

                    if isStaleMergeConflictError message then
                        let! _ = refreshAll deps isArcLoaded getState dispatch
                        dispatch (SetSelectedChangePath None)
                        dispatch (SetActivePage None)

                    dispatch (SetErrorNotice(Some message))
                    dispatch (SetWarningNotice None)
                    return Error message
                | Ok payload ->
                    dispatch (SetStatus(Some payload.UpdatedStatus))
                    dispatch (SetMergeResolutionPendingPath None)

                    match payload.NextConflictedPath with
                    | Some nextConflictPath ->
                        let! openResult = loadPage deps getState dispatch nextConflictPath true

                        match openResult with
                        | Ok () ->
                            dispatch (SetErrorNotice None)
                            dispatch (SetWarningNotice None)
                            return Ok()
                        | Error message ->
                            dispatch (SetErrorNotice(Some message))
                            dispatch (SetWarningNotice None)
                            return Error message
                    | None ->
                        dispatch (SetSelectedChangePath None)
                        dispatch (SetActivePage None)
                        dispatch (SetErrorNotice None)
                        dispatch (SetWarningNotice None)
                        return Ok()
    }
