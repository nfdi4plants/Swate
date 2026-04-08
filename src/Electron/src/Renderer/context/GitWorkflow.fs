module Renderer.Context.GitWorkflow

open System
open Elmish
open Fable.Core

open Renderer.Types
open Swate.Components.GitSidebarTypes
open Swate.Electron.Shared
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
type GitPageChange =
    | NoChange
    | Set of PageState
    | Clear

type GitPullWorkflowResult = {
    Status: GitStatusDto option
    WarningMessage: string option
    PageChange: GitPageChange
}

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
    MergeResolutionPendingPath: string option
    InstallRetryState: GitInstallRetryState
    PageLoadRequestId: int
    CurrentArcPath: ArcRootPath
    ArcSessionId: int
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
        MergeResolutionPendingPath = None
        InstallRetryState = GitInstallRetryState.Idle
        PageLoadRequestId = 0
        CurrentArcPath = None
        ArcSessionId = 0
    }

type GitRefreshResult = {
    Status: Result<GitStatusDto, string>
    Branches: Result<GitBranchRefDto[], string>
    LfsSettings: Result<GitLfsSettingsDto, string>
}

type Reply<'T> = Result<'T, string> -> unit

type ConfirmMergeResolutionOutcome = {
    UpdatedStatus: GitStatusDto
    NextConflictedPath: string option
    PageChange: GitPageChange
}

type PreparedCommitOperation = {
    BusyOperation: GitBusyOperation
    NormalizedMessage: string
    PathsToCommit: string[]
    CurrentlyStagedPaths: string[]
}

type WriteRequest =
    | Fetch of Reply<unit>
    | Pull of Reply<unit>
    | Push of Reply<unit>
    | Sync of Reply<unit>
    | Clone of GitCloneRepositoryRequest * Reply<string>
    | CommitSelection of PreparedCommitOperation * Reply<unit>
    | CommitAll of PreparedCommitOperation * Reply<unit>
    | SaveLfsSettings of GitBusyOperation * GitLfsSettingsDto * Reply<unit>
    | CreateBranch of GitCreateBranchRequest * Reply<unit>
    | SwitchBranch of GitCheckoutBranchRequest * Reply<unit>

type WriteSuccess =
    | UnitSuccess of GitRefreshResult * GitPageChange * string option option * string option
    | CloneSuccess of string

type WriteAttemptOutcome =
    | Completed of WriteSuccess
    | RequiresLfsInstall of string

type private WriteOperationClassification =
    | WriteOperationReady of GitOperationResult
    | WriteOperationNeedsLfsInstall of string

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
    | SetMergeResolutionPendingPath of string option
    | SetInstallRetryState of GitInstallRetryState
    | StartRefreshRequest of requestId: int
    | FinishRefreshRequest of requestId: int * refreshResult: GitRefreshResult
    | StartPageLoad of requestId: int
    | FinishPageLoad of requestId: int * path: string * result: Result<unit, string>
    | ArcPathChanged of ArcRootPath
    | RefreshRequested of Reply<unit>
    | RefreshCompleted of requestId: int * reply: Reply<unit> * result: Result<GitRefreshResult, string>
    | SelectChangeRequested of GitSidebarChange * Reply<unit>
    | SelectChangeCompleted of requestId: int * path: string * reply: Reply<unit> * result: Result<GitPageChange, string>
    | ConfirmMergeResolutionRequested of GitConfirmMergeResolutionRequest * Reply<unit>
    | ConfirmMergeResolutionCompleted of sessionId: int * reply: Reply<unit> * result: Result<ConfirmMergeResolutionOutcome, string>
    | SaveLfsAutoTrackThresholdRequested of int * Reply<unit>
    | SaveDownloadLargeFilesRequested of bool * Reply<unit>
    | FetchRequested of Reply<unit>
    | PullRequested of Reply<unit>
    | PushRequested of Reply<unit>
    | SyncRequested of Reply<unit>
    | CloneRequested of GitCloneRepositoryRequest * Reply<string>
    | CommitSelectionRequested of GitSidebarCommitSelectionRequest * Reply<unit>
    | CommitAllRequested of string * Reply<unit>
    | CreateBranchRequested of GitSidebarCreateBranchRequest * Reply<unit>
    | SwitchBranchRequested of string * Reply<unit>
    | WriteRequested of WriteRequest
    | WriteCompleted of sessionId: int * WriteRequest * Result<WriteAttemptOutcome, string>
    | WriteInstallPromptAnswered of sessionId: int * WriteRequest * bool
    | WriteInstallCompleted of sessionId: int * WriteRequest * Result<GitOperationResult, string>

type GitDependencies = {
    getGitStatus: unit -> JS.Promise<Result<GitStatusDto, string>>
    getGitBranches: unit -> JS.Promise<Result<GitBranchRefDto[], string>>
    getGitLfsSettings: unit -> JS.Promise<Result<GitLfsSettingsDto, string>>
    loadDiffPage: string -> JS.Promise<Result<PageState, string>>
    loadMergeConflictPage: string -> JS.Promise<Result<PageState, string>>
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
    confirmGitMergeResolution:
        GitConfirmMergeResolutionRequest -> JS.Promise<Result<GitConfirmMergeResolutionResult, string>>
    confirmInstall: string -> bool
}

let staleMergeConflictTokens = [|
    "not currently marked as conflicted"
    "changed on disk since it was opened"
|]

let staleArcSessionMessage = "Git operation was canceled because the active ARC changed."

let isStaleMergeConflictError (message: string) =
    let normalizedMessage = message.ToLowerInvariant()

    staleMergeConflictTokens
    |> Array.exists (fun token -> normalizedMessage.Contains(token))

let busyNoticeFromOperation =
    function
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

let currentRunStatus (model: GitState) =
    match model.CurrentProgress with
    | Some progress -> Some(GitSidebarRunStatus.Progress progress)
    | None -> model.BusyNotice |> Option.map GitSidebarRunStatus.Busy

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

let private applyRefreshResult (refreshResult: GitRefreshResult) (model: GitState) =
    let modelWithStatus =
        match refreshResult.Status with
        | Ok status -> applyStatus status model
        | Error _ -> {
            model with
                Status = GitState.Empty.Status
                ChangedFiles = [||]
                SelectedChangePath = None
          }

    let modelWithBranches =
        match refreshResult.Status, refreshResult.Branches with
        | Ok _, Ok branches -> {
            modelWithStatus with
                BranchOptions = mapBranches branches
          }
        | _ -> {
            modelWithStatus with
                BranchOptions = [||]
          }

    let modelWithSettings =
        match refreshResult.Status, refreshResult.LfsSettings with
        | Ok _, Ok settings -> {
            modelWithBranches with
                LfsAutoTrackThresholdMb = settings.AutoTrackThresholdMb
                DownloadLargeFiles = settings.DownloadLargeFiles
          }
        | _ -> {
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

let nextArcSessionId (model: GitState) = model.ArcSessionId + 1

let private distinctPaths (paths: string[]) =
    paths
    |> Array.map _.Trim()
    |> Array.filter (String.IsNullOrWhiteSpace >> not)
    |> Array.distinct

let prepareCommitSelection (state: GitState) (request: GitSidebarCommitSelectionRequest) = {
    BusyOperation = GitBusyOperation.CommittingSelectedChanges
    NormalizedMessage = request.Message.Trim()
    PathsToCommit = distinctPaths request.Paths
    CurrentlyStagedPaths =
        state.ChangedFiles
        |> Array.filter (fun change -> GitStatusCode.isStagedIndexStatus change.IndexStatus)
        |> Array.map _.Path
        |> Array.distinct
}

let prepareCommitAll (state: GitState) (message: string) = {
    BusyOperation = GitBusyOperation.CommittingAllChanges
    NormalizedMessage = message.Trim()
    PathsToCommit = state.ChangedFiles |> Array.map _.Path |> distinctPaths
    CurrentlyStagedPaths = [||]
}

let buildUpdatedLfsSettings (state: GitState) (thresholdMb: int option) (downloadLargeFiles: bool option) = {
    AutoTrackThresholdMb = thresholdMb |> Option.defaultValue state.LfsAutoTrackThresholdMb
    DownloadLargeFiles = downloadLargeFiles |> Option.defaultValue state.DownloadLargeFiles
}

let private effectCmd (effect: unit -> unit) : Cmd<'msg> = [ fun _dispatch -> effect () ]

let private ignoreReply (_: Result<'T, string>) = ()

let private resolveReplyCmd (reply: Reply<'T>) (result: Result<'T, string>) : Cmd<'msg> =
    effectCmd (fun () -> reply result)

let private applyPageChangeCmd (setPageState: PageState option -> unit) =
    function
    | GitPageChange.NoChange -> Cmd.none
    | GitPageChange.Set page -> effectCmd (fun () -> setPageState (Some page))
    | GitPageChange.Clear -> effectCmd (fun () -> setPageState None)

let private refreshAllAsync (deps: GitDependencies) = promise {
    let! statusResult = deps.getGitStatus ()
    let! branchResult = deps.getGitBranches ()
    let! lfsSettingsResult = deps.getGitLfsSettings ()

    return {
        Status = statusResult
        Branches = branchResult
        LfsSettings = lfsSettingsResult
    }
}

let private loadPageAsync (deps: GitDependencies) (path: string) (isConflicted: bool) = promise {
    let! result =
        if isConflicted then
            deps.loadMergeConflictPage path
        else
            deps.loadDiffPage path

    return result |> Result.map GitPageChange.Set
}

let private confirmMergeResolutionAsync (deps: GitDependencies) (request: GitConfirmMergeResolutionRequest) = promise {
    let! result = deps.confirmGitMergeResolution request

    match result with
    | Error message -> return Error message
    | Ok payload ->
        match payload.NextConflictedPath with
        | Some nextConflictedPath ->
            let! pageChangeResult = loadPageAsync deps nextConflictedPath true

            return
                pageChangeResult
                |> Result.map (fun pageChange -> {
                    UpdatedStatus = payload.UpdatedStatus
                    NextConflictedPath = payload.NextConflictedPath
                    PageChange = pageChange
                })
        | None ->
            return
                Ok {
                    UpdatedStatus = payload.UpdatedStatus
                    NextConflictedPath = None
                    PageChange = GitPageChange.Clear
                }
}

let private successfulNoopOperationResult = {
    Success = true
    Message = None
    FailureKind = None
    WarningMessage = None
    WarningKind = None
    Path = None
}

let private busyOperationForWriteRequest =
    function
    | Fetch _ -> GitBusyOperation.FetchingFromRemote
    | Pull _ -> GitBusyOperation.PullingFromRemote
    | Push _ -> GitBusyOperation.PushingToRemote
    | Sync _ -> GitBusyOperation.PushingToRemote
    | Clone _ -> GitBusyOperation.CloningRepository
    | CommitSelection(prepared, _)
    | CommitAll(prepared, _) -> prepared.BusyOperation
    | SaveLfsSettings(busyOperation, _, _) -> busyOperation
    | CreateBranch _ -> GitBusyOperation.CreatingBranch
    | SwitchBranch _ -> GitBusyOperation.SwitchingBranch

let private requiresArcForWriteRequest =
    function
    | Clone _ -> false
    | _ -> true

let private resolveWriteCmd request result =
    match request, result with
    | Fetch reply, result
    | Pull reply, result
    | Push reply, result
    | Sync reply, result
    | CommitSelection(_, reply), result
    | CommitAll(_, reply), result
    | SaveLfsSettings(_, _, reply), result
    | CreateBranch(_, reply), result
    | SwitchBranch(_, reply), result ->
        match result with
        | Ok _ -> resolveReplyCmd reply (Ok())
        | Error message -> resolveReplyCmd reply (Error message)
    | Clone(_, reply), Ok(CloneSuccess path) ->
        resolveReplyCmd reply (Ok path)
    | Clone(_, reply), Error message ->
        resolveReplyCmd reply (Error message)
    | Clone(_, reply), Ok _ ->
        resolveReplyCmd reply (Error "Clone request produced an invalid result.")

let private resolveStaleWriteCompletedCmd request result =
    match result with
    | Ok(Completed success) -> resolveWriteCmd request (Ok success)
    | Ok(RequiresLfsInstall _) -> resolveWriteCmd request (Error staleArcSessionMessage)
    | Error message -> resolveWriteCmd request (Error message)

let private writeErrorModel (message: string) (model: GitState) =
    {
        model with
            BusyOperation = None
            BusyNotice = None
            CurrentProgress = None
            ErrorNotice = Some message
            WarningNotice = None
    }

let private classifyWriteResult (busyOperation: GitBusyOperation) (result: Result<GitOperationResult, string>) =
    match result with
    | Error message -> Error message
    | Ok operationResult when operationResult.FailureKind = Some GitFailureKind.LfsInstallRequired ->
        Ok(
            WriteOperationNeedsLfsInstall(
                operationResult.Message
                |> Option.defaultValue "Git LFS is required for this operation. Install Git LFS now?"
            )
        )
    | Ok operationResult when not operationResult.Success ->
        Error(
            operationResult.Message
            |> Option.defaultValue (
                busyNoticeFromOperation busyOperation
                |> Option.defaultValue "Git operation failed."
            )
        )
    | Ok operationResult -> Ok(WriteOperationReady operationResult)

let private refreshAfterSuccess
    (deps: GitDependencies)
    (warningMessage: string option)
    (pageChange: GitPageChange)
    (selectedChangePathOverride: string option option)
    =
    promise {
        let! refreshResult = refreshAllAsync deps

        return
            match refreshResult.Status, refreshErrorMessage refreshResult with
            | Error message, _ -> Error message
            | Ok _, Some message -> Error message
            | Ok _, None ->
                Ok(Completed(UnitSuccess(refreshResult, pageChange, selectedChangePathOverride, warningMessage)))
    }

let private runSimpleWriteAttemptAsync
    (deps: GitDependencies)
    (busyOperation: GitBusyOperation)
    (operation: unit -> JS.Promise<Result<GitOperationResult, string>>)
    =
    promise {
        let! result = operation ()

        match classifyWriteResult busyOperation result with
        | Error message -> return Error message
        | Ok(WriteOperationNeedsLfsInstall promptMessage) -> return Ok(RequiresLfsInstall promptMessage)
        | Ok(WriteOperationReady operationResult) ->
            return! refreshAfterSuccess deps operationResult.WarningMessage GitPageChange.NoChange None
    }

let private runCloneAttemptAsync (deps: GitDependencies) (request: GitCloneRepositoryRequest) = promise {
    let! result = deps.gitCloneRepository request

    return
        match classifyWriteResult GitBusyOperation.CloningRepository result with
        | Error message -> Error message
        | Ok(WriteOperationNeedsLfsInstall promptMessage) -> Ok(RequiresLfsInstall promptMessage)
        | Ok(WriteOperationReady operationResult) ->
            Ok(Completed(CloneSuccess(operationResult.Path |> Option.defaultValue request.TargetPath)))
}

let private runPullAttemptAsync (deps: GitDependencies) = promise {
    let! pullResult = deps.gitPull { Remote = None; Branch = None }

    match classifyWriteResult GitBusyOperation.PullingFromRemote pullResult with
    | Error message -> return Error message
    | Ok(WriteOperationNeedsLfsInstall promptMessage) -> return Ok(RequiresLfsInstall promptMessage)
    | Ok(WriteOperationReady operationResult) ->
        let! refreshResult = refreshAllAsync deps

        match refreshResult.Status, refreshErrorMessage refreshResult with
        | Error message, _ -> return Error message
        | Ok _, Some message -> return Error message
        | Ok latestStatus, None when latestStatus.Conflicted.Length > 0 ->
            let firstConflictPath = latestStatus.Conflicted.[0]
            let! pageResult = loadPageAsync deps firstConflictPath true

            return
                pageResult
                |> Result.map (fun pageChange ->
                    Completed(UnitSuccess(refreshResult, pageChange, Some(Some firstConflictPath), operationResult.WarningMessage))
                )
        | Ok _, None ->
            return Ok(Completed(UnitSuccess(refreshResult, GitPageChange.NoChange, None, operationResult.WarningMessage)))
}

let private runSyncAttemptAsync (deps: GitDependencies) (state: GitState) = promise {
    if shouldPublishCurrentBranchFirst state then
        return!
            runSimpleWriteAttemptAsync deps GitBusyOperation.PushingToRemote (fun () ->
                deps.gitPush { Remote = None; Branch = None }
            )
    else
        let! pullResult = deps.gitPull { Remote = None; Branch = None }

        match classifyWriteResult GitBusyOperation.PullingFromRemote pullResult with
        | Error message -> return Error message
        | Ok(WriteOperationNeedsLfsInstall promptMessage) -> return Ok(RequiresLfsInstall promptMessage)
        | Ok(WriteOperationReady pullOperation) ->
            let! refreshResult = refreshAllAsync deps

            match refreshResult.Status, refreshErrorMessage refreshResult with
            | Error message, _ -> return Error message
            | Ok _, Some message -> return Error message
            | Ok latestStatus, None when
                pullOperation.WarningMessage.IsSome
                || latestStatus.Conflicted.Length > 0
                || latestStatus.IsMergeInProgress
                ->
                let selectedChangePathOverride =
                    latestStatus.Conflicted
                    |> Array.tryHead
                    |> Option.map Some

                let! pageChangeResult =
                    if latestStatus.Conflicted.Length > 0 then
                        loadPageAsync deps latestStatus.Conflicted.[0] true
                    else
                        promise { return Ok GitPageChange.NoChange }

                return
                    pageChangeResult
                    |> Result.map (fun pageChange ->
                        Completed(UnitSuccess(refreshResult, pageChange, selectedChangePathOverride, pullOperation.WarningMessage))
                    )
            | Ok _, None ->
                return!
                    runSimpleWriteAttemptAsync deps GitBusyOperation.PushingToRemote (fun () ->
                        deps.gitPush { Remote = None; Branch = None }
                    )
}

let private runCommitAttemptAsync
    (deps: GitDependencies)
    (prepared: PreparedCommitOperation)
    =
    promise {
        if String.IsNullOrWhiteSpace prepared.NormalizedMessage then
            return Error "Commit message must not be empty."
        elif prepared.PathsToCommit.Length = 0 then
            return Error "No changes available to commit."
        else
            let! unstageResult =
                if prepared.CurrentlyStagedPaths.Length = 0 then
                    promise { return Ok successfulNoopOperationResult }
                else
                    deps.gitUnstagePaths { Pathspecs = prepared.CurrentlyStagedPaths }

            match classifyWriteResult prepared.BusyOperation unstageResult with
            | Error message -> return Error message
            | Ok(WriteOperationNeedsLfsInstall promptMessage) -> return Ok(RequiresLfsInstall promptMessage)
            | Ok(WriteOperationReady _) ->
                let! stageResult = deps.gitStagePaths { Pathspecs = prepared.PathsToCommit }

                match classifyWriteResult prepared.BusyOperation stageResult with
                | Error message -> return Error message
                | Ok(WriteOperationNeedsLfsInstall promptMessage) -> return Ok(RequiresLfsInstall promptMessage)
                | Ok(WriteOperationReady _) ->
                    let! commitResult = deps.gitCommit { Message = prepared.NormalizedMessage }

                    match classifyWriteResult prepared.BusyOperation commitResult with
                    | Error message -> return Error message
                    | Ok(WriteOperationNeedsLfsInstall promptMessage) -> return Ok(RequiresLfsInstall promptMessage)
                    | Ok(WriteOperationReady operationResult) ->
                        return! refreshAfterSuccess deps operationResult.WarningMessage GitPageChange.NoChange None
    }

let private runSaveLfsSettingsAttemptAsync
    (deps: GitDependencies)
    (busyOperation: GitBusyOperation)
    (settings: GitLfsSettingsDto)
    =
    promise {
        let! result = deps.setGitLfsSettings settings

        match classifyWriteResult busyOperation result with
        | Error message -> return Error message
        | Ok(WriteOperationNeedsLfsInstall promptMessage) -> return Ok(RequiresLfsInstall promptMessage)
        | Ok(WriteOperationReady operationResult) ->
            return! refreshAfterSuccess deps operationResult.WarningMessage GitPageChange.NoChange None
    }

let private executeWriteAttempt (deps: GitDependencies) (state: GitState) (request: WriteRequest) = promise {
    match request with
    | Fetch _ ->
        return!
            runSimpleWriteAttemptAsync deps GitBusyOperation.FetchingFromRemote (fun () ->
                deps.gitFetch { Remote = None; Branch = None }
            )
    | Pull _ ->
        return! runPullAttemptAsync deps
    | Push _ ->
        return!
            runSimpleWriteAttemptAsync deps GitBusyOperation.PushingToRemote (fun () ->
                deps.gitPush { Remote = None; Branch = None }
            )
    | Sync _ ->
        return! runSyncAttemptAsync deps state
    | Clone(request, _) ->
        return! runCloneAttemptAsync deps request
    | CommitSelection(prepared, _) ->
        return! runCommitAttemptAsync deps prepared
    | CommitAll(prepared, _) ->
        return! runCommitAttemptAsync deps prepared
    | SaveLfsSettings(busyOperation, settings, _) ->
        return! runSaveLfsSettingsAttemptAsync deps busyOperation settings
    | CreateBranch(request, _) ->
        return! runSimpleWriteAttemptAsync deps GitBusyOperation.CreatingBranch (fun () -> deps.createBranch request)
    | SwitchBranch(request, _) ->
        return! runSimpleWriteAttemptAsync deps GitBusyOperation.SwitchingBranch (fun () -> deps.checkoutBranch request)
}

let transition (msg: Msg) (model: GitState) =
    match msg with
    | ResetWorkflow -> GitState.Empty
    | SetBusyOperation busyOperation -> {
        model with
            BusyOperation = busyOperation
            BusyNotice = busyOperation |> Option.bind busyNoticeFromOperation
      }
    | SetCurrentProgress currentProgress -> {
        model with
            CurrentProgress = currentProgress
      }
    | SetErrorNotice errorNotice -> { model with ErrorNotice = errorNotice }
    | SetWarningNotice warningNotice -> {
        model with
            WarningNotice = warningNotice
      }
    | SetSelectedChangePath selectedChangePath -> {
        model with
            SelectedChangePath = selectedChangePath
      }
    | SetStatus None -> {
        model with
            Status = GitState.Empty.Status
            ChangedFiles = [||]
            SelectedChangePath = None
      }
    | SetStatus(Some status) -> applyStatus status model
    | SetBranches None -> { model with BranchOptions = [||] }
    | SetBranches(Some branches) -> {
        model with
            BranchOptions = mapBranches branches
      }
    | SetLfsSettings None -> {
        model with
            LfsAutoTrackThresholdMb = GitState.Empty.LfsAutoTrackThresholdMb
            DownloadLargeFiles = GitState.Empty.DownloadLargeFiles
      }
    | SetLfsSettings(Some settings) -> {
        model with
            LfsAutoTrackThresholdMb = settings.AutoTrackThresholdMb
            DownloadLargeFiles = settings.DownloadLargeFiles
      }
    | SetMergeResolutionPendingPath mergeResolutionPendingPath -> {
        model with
            MergeResolutionPendingPath = mergeResolutionPendingPath
      }
    | SetInstallRetryState installRetryState -> {
        model with
            InstallRetryState = installRetryState
      }
    | StartRefreshRequest requestId -> {
        model with
            RefreshRequestId = requestId
            RefreshState = GitRefreshState.Loading
      }
    | FinishRefreshRequest(requestId, refreshResult) ->
        if requestId <> model.RefreshRequestId then
            model
        else
            applyRefreshResult refreshResult model
    | StartPageLoad requestId -> {
        model with
            PageLoadRequestId = requestId
      }
    | FinishPageLoad(requestId, path, result) ->
        if requestId <> model.PageLoadRequestId then
            model
        else
            match result with
            | Ok() -> {
                model with
                    SelectedChangePath = Some path
                    ErrorNotice = None
              }
            | Error message -> {
                model with
                    ErrorNotice = Some message
              }
    | ArcPathChanged _
    | RefreshRequested _
    | RefreshCompleted _
    | SelectChangeRequested _
    | SelectChangeCompleted _
    | ConfirmMergeResolutionRequested _
    | ConfirmMergeResolutionCompleted _
    | SaveLfsAutoTrackThresholdRequested _
    | SaveDownloadLargeFilesRequested _
    | FetchRequested _
    | PullRequested _
    | PushRequested _
    | SyncRequested _
    | CloneRequested _
    | CommitSelectionRequested _
    | CommitAllRequested _
    | CreateBranchRequested _
    | SwitchBranchRequested _
    | WriteRequested _
    | WriteCompleted _
    | WriteInstallPromptAnswered _
    | WriteInstallCompleted _ -> model

let init () : GitState * Cmd<Msg> = GitState.Empty, Cmd.none

let update
    (deps: GitDependencies)
    (setPageState: PageState option -> unit)
    (msg: Msg)
    (model: GitState)
    : GitState * Cmd<Msg> =
    match msg with
    | ArcPathChanged arcPath when arcPath = model.CurrentArcPath ->
        model, Cmd.none
    | ArcPathChanged arcPath ->
        let nextModel = {
            GitState.Empty with
                CurrentArcPath = arcPath
                ArcSessionId = nextArcSessionId model
        }

        let cmd =
            match arcPath with
            | Some _ ->
                Cmd.batch [
                    applyPageChangeCmd setPageState GitPageChange.Clear
                    Cmd.ofMsg (RefreshRequested ignoreReply)
                ]
            | None -> applyPageChangeCmd setPageState GitPageChange.Clear

        nextModel, cmd
    | RefreshRequested reply when model.CurrentArcPath.IsNone ->
        {
            GitState.Empty with
                CurrentArcPath = model.CurrentArcPath
                ArcSessionId = model.ArcSessionId
        },
        resolveReplyCmd reply (Ok())
    | RefreshRequested reply ->
        let requestId = nextRefreshRequestId model

        let nextModel =
            model
            |> transition (SetBusyOperation(Some GitBusyOperation.Refreshing))
            |> transition (SetErrorNotice None)
            |> transition (SetWarningNotice None)
            |> transition (StartRefreshRequest requestId)

        let cmd =
            Cmd.OfPromise.either
                refreshAllAsync
                deps
                (fun refreshResult -> RefreshCompleted(requestId, reply, Ok refreshResult))
                (fun err -> RefreshCompleted(requestId, reply, Error(string err)))

        nextModel, cmd
    | RefreshCompleted(requestId, reply, _) when requestId <> model.RefreshRequestId ->
        model, resolveReplyCmd reply (Ok())
    | RefreshCompleted(_, reply, Error message) ->
        let nextModel =
            { model with RefreshState = GitRefreshState.Idle }
            |> transition (SetBusyOperation None)
            |> transition (SetCurrentProgress None)
            |> transition (SetErrorNotice(Some message))
            |> transition (SetWarningNotice None)

        nextModel,
        Cmd.batch [
            applyPageChangeCmd setPageState GitPageChange.Clear
            resolveReplyCmd reply (Error message)
        ]
    | RefreshCompleted(requestId, reply, Ok refreshResult) ->
        let nextModel =
            model
            |> transition (FinishRefreshRequest(requestId, refreshResult))
            |> transition (SetBusyOperation None)
            |> transition (SetCurrentProgress None)

        let replyResult =
            match refreshResult.Status, refreshErrorMessage refreshResult with
            | Error message, _ -> Error message
            | Ok _, Some message -> Error message
            | Ok _, None -> Ok()

        let cmd =
            match replyResult with
            | Ok() -> resolveReplyCmd reply (Ok())
            | Error message ->
                Cmd.batch [
                    applyPageChangeCmd setPageState GitPageChange.Clear
                    resolveReplyCmd reply (Error message)
                ]

        nextModel, cmd
    | SelectChangeRequested(change, reply) when model.CurrentArcPath.IsNone ->
        model, resolveReplyCmd reply (Error "No ARC is loaded.")
    | SelectChangeRequested(change, reply) ->
        let requestId = nextPageLoadRequestId model

        let nextModel =
            model
            |> transition (SetErrorNotice None)
            |> transition (StartPageLoad requestId)

        let cmd =
            Cmd.OfPromise.either
                (fun (deps: GitDependencies, change: GitSidebarChange) ->
                    loadPageAsync deps change.Path change.IsConflicted
                )
                (deps, change)
                (fun result -> SelectChangeCompleted(requestId, change.Path, reply, result))
                (fun err -> SelectChangeCompleted(requestId, change.Path, reply, Error(string err)))

        nextModel, cmd
    | SelectChangeCompleted(requestId, _path, reply, _) when requestId <> model.PageLoadRequestId ->
        model, resolveReplyCmd reply (Ok())
    | SelectChangeCompleted(requestId, path, reply, Ok pageChange) ->
        let nextModel = model |> transition (FinishPageLoad(requestId, path, Ok()))

        nextModel,
        Cmd.batch [
            applyPageChangeCmd setPageState pageChange
            resolveReplyCmd reply (Ok())
        ]
    | SelectChangeCompleted(requestId, path, reply, Error message) ->
        let nextModel = model |> transition (FinishPageLoad(requestId, path, Error message))
        nextModel, resolveReplyCmd reply (Error message)
    | ConfirmMergeResolutionRequested(_, reply) when model.CurrentArcPath.IsNone ->
        model, resolveReplyCmd reply (Error "No ARC is loaded.")
    | ConfirmMergeResolutionRequested(request, reply) ->
        match model.BusyOperation with
        | Some(GitBusyOperation.ConfirmingMergeResolution _) ->
            model, resolveReplyCmd reply (Ok())
        | _ when model.MergeResolutionPendingPath = Some request.Path ->
            model, resolveReplyCmd reply (Ok())
        | _ ->
            let nextModel =
                model
                |> transition (SetBusyOperation(Some(GitBusyOperation.ConfirmingMergeResolution request.Path)))
                |> transition (SetMergeResolutionPendingPath(Some request.Path))
                |> transition (SetErrorNotice None)
                |> transition (SetWarningNotice None)

            let cmd =
                Cmd.OfPromise.either
                    (fun (deps, request) -> confirmMergeResolutionAsync deps request)
                    (deps, request)
                    (fun result -> ConfirmMergeResolutionCompleted(model.ArcSessionId, reply, result))
                    (fun err -> ConfirmMergeResolutionCompleted(model.ArcSessionId, reply, Error(string err)))

            nextModel, cmd
    | ConfirmMergeResolutionCompleted(sessionId, reply, result) when sessionId <> model.ArcSessionId ->
        model, resolveReplyCmd reply (Result.map ignore result)
    | ConfirmMergeResolutionCompleted(_, reply, Error message) ->
        let nextModel =
            model
            |> transition (SetBusyOperation None)
            |> transition (SetCurrentProgress None)
            |> transition (SetMergeResolutionPendingPath None)
            |> transition (SetErrorNotice(Some message))
            |> transition (SetWarningNotice None)

        if isStaleMergeConflictError message then
            let selectionClearedModel = nextModel |> transition (SetSelectedChangePath None)

            selectionClearedModel,
            Cmd.batch [
                applyPageChangeCmd setPageState GitPageChange.Clear
                Cmd.ofMsg (RefreshRequested ignoreReply)
                resolveReplyCmd reply (Error message)
            ]
        else
            nextModel, resolveReplyCmd reply (Error message)
    | ConfirmMergeResolutionCompleted(_, reply, Ok outcome) ->
        let nextModel =
            model
            |> transition (SetBusyOperation None)
            |> transition (SetCurrentProgress None)
            |> transition (SetStatus(Some outcome.UpdatedStatus))
            |> transition (SetMergeResolutionPendingPath None)
            |> transition (SetSelectedChangePath outcome.NextConflictedPath)
            |> transition (SetErrorNotice None)
            |> transition (SetWarningNotice None)

        nextModel,
        Cmd.batch [
            applyPageChangeCmd setPageState outcome.PageChange
            resolveReplyCmd reply (Ok())
        ]
    | SaveDownloadLargeFilesRequested(downloadLargeFiles, reply) when model.CurrentArcPath.IsNone ->
        let nextModel =
            model
            |> transition (SetLfsSettings(Some(buildUpdatedLfsSettings model None (Some downloadLargeFiles))))

        nextModel, resolveReplyCmd reply (Ok())
    | SaveDownloadLargeFilesRequested(downloadLargeFiles, reply) ->
        model,
        Cmd.ofMsg (
            WriteRequested(
                SaveLfsSettings(
                    GitBusyOperation.SavingGitLfsDownloadPreference,
                    buildUpdatedLfsSettings model None (Some downloadLargeFiles),
                    reply
                )
            )
        )
    | SaveLfsAutoTrackThresholdRequested(thresholdMb, reply) ->
        model,
        Cmd.ofMsg (
            WriteRequested(
                SaveLfsSettings(
                    GitBusyOperation.SavingGitLfsThreshold,
                    buildUpdatedLfsSettings model (Some thresholdMb) None,
                    reply
                )
            )
        )
    | FetchRequested reply -> model, Cmd.ofMsg (WriteRequested(Fetch reply))
    | PullRequested reply -> model, Cmd.ofMsg (WriteRequested(Pull reply))
    | PushRequested reply -> model, Cmd.ofMsg (WriteRequested(Push reply))
    | SyncRequested reply -> model, Cmd.ofMsg (WriteRequested(Sync reply))
    | CloneRequested(request, reply) -> model, Cmd.ofMsg (WriteRequested(Clone(request, reply)))
    | CommitSelectionRequested(request, reply) ->
        model, Cmd.ofMsg (WriteRequested(CommitSelection(prepareCommitSelection model request, reply)))
    | CommitAllRequested(message, reply) ->
        model, Cmd.ofMsg (WriteRequested(CommitAll(prepareCommitAll model message, reply)))
    | CreateBranchRequested(request, reply) ->
        model,
        Cmd.ofMsg (
            WriteRequested(
                CreateBranch(
                    {
                        Name = request.BranchName
                        StartPoint = request.StartPoint
                    },
                    reply
                )
            )
        )
    | SwitchBranchRequested(branchName, reply) ->
        let normalizedBranchName = branchName.Trim()

        if String.IsNullOrWhiteSpace normalizedBranchName then
            model, resolveReplyCmd reply (Error "Branch name must not be empty.")
        else
            model,
            Cmd.ofMsg (
                WriteRequested(
                    SwitchBranch(
                        { Name = normalizedBranchName },
                        reply
                    )
                )
            )
    | WriteRequested request when requiresArcForWriteRequest request && model.CurrentArcPath.IsNone ->
        model, resolveWriteCmd request (Error "No ARC is loaded.")
    | WriteRequested request ->
        let busyOperation = busyOperationForWriteRequest request

        let nextModel =
            model
            |> transition (SetBusyOperation(Some busyOperation))
            |> transition (SetErrorNotice None)
            |> transition (SetWarningNotice None)

        let cmd =
            Cmd.OfPromise.either
                (fun (deps, model, request) -> executeWriteAttempt deps model request)
                (deps, model, request)
                (fun result -> WriteCompleted(model.ArcSessionId, request, result))
                (fun err -> WriteCompleted(model.ArcSessionId, request, Error(string err)))

        nextModel, cmd
    | WriteCompleted(sessionId, request, result) when sessionId <> model.ArcSessionId ->
        model, resolveStaleWriteCompletedCmd request result
    | WriteCompleted(_, request, Error message) ->
        let nextModel = writeErrorModel message model

        nextModel, resolveWriteCmd request (Error message)
    | WriteCompleted(sessionId, request, Ok(RequiresLfsInstall promptMessage)) ->
        let busyOperation = busyOperationForWriteRequest request

        let nextModel =
            model
            |> transition (SetInstallRetryState(GitInstallRetryState.PromptingForInstall(promptMessage, busyOperation)))

        let cmd =
            Cmd.OfFunc.perform
                deps.confirmInstall
                promptMessage
                (fun shouldInstall -> WriteInstallPromptAnswered(sessionId, request, shouldInstall))

        nextModel, cmd
    | WriteInstallPromptAnswered(sessionId, request, _) when sessionId <> model.ArcSessionId ->
        model, resolveWriteCmd request (Error staleArcSessionMessage)
    | WriteInstallPromptAnswered(_, request, false) ->
        let message = "Git LFS installation is required to continue."

        let nextModel =
            model
            |> writeErrorModel message
            |> transition (SetInstallRetryState GitInstallRetryState.Idle)

        nextModel, resolveWriteCmd request (Error message)
    | WriteInstallPromptAnswered(sessionId, request, true) ->
        let busyOperation = busyOperationForWriteRequest request

        let nextModel =
            model
            |> transition (SetInstallRetryState(GitInstallRetryState.InstallingForRetry busyOperation))
            |> transition (SetBusyOperation(Some GitBusyOperation.InstallingGitLfs))

        let cmd =
            Cmd.OfPromise.either
                deps.installGitLfs
                ()
                (fun installResult -> WriteInstallCompleted(sessionId, request, installResult))
                (fun err -> WriteInstallCompleted(sessionId, request, Error(string err)))

        nextModel, cmd
    | WriteInstallCompleted(sessionId, request, _) when sessionId <> model.ArcSessionId ->
        model, resolveWriteCmd request (Error staleArcSessionMessage)
    | WriteInstallCompleted(_, request, Error message) ->
        let nextModel =
            model
            |> writeErrorModel message
            |> transition (SetInstallRetryState GitInstallRetryState.Idle)

        nextModel, resolveWriteCmd request (Error message)
    | WriteInstallCompleted(_, request, Ok operationResult) when not operationResult.Success ->
        let message = operationResult.Message |> Option.defaultValue "Git LFS installation failed."

        let nextModel =
            model
            |> writeErrorModel message
            |> transition (SetInstallRetryState GitInstallRetryState.Idle)

        nextModel, resolveWriteCmd request (Error message)
    | WriteInstallCompleted(sessionId, request, Ok _) ->
        let busyOperation = busyOperationForWriteRequest request

        let nextModel =
            model
            |> transition (SetInstallRetryState GitInstallRetryState.Idle)
            |> transition (SetBusyOperation(Some busyOperation))

        let cmd =
            Cmd.OfPromise.either
                (fun (deps, model, request) -> executeWriteAttempt deps model request)
                (deps, model, request)
                (fun result -> WriteCompleted(sessionId, request, result))
                (fun err -> WriteCompleted(sessionId, request, Error(string err)))

        nextModel, cmd
    | WriteCompleted(_, request, Ok(Completed success)) ->
        let baseModel, pageChange, warningMessage =
            match success with
            | UnitSuccess(refreshResult, pageChange, selectedChangePathOverride, warningMessage) ->
                let refreshedModel = applyRefreshResult refreshResult model

                let selectionAdjustedModel =
                    match selectedChangePathOverride with
                    | Some selectedChangePath -> refreshedModel |> transition (SetSelectedChangePath selectedChangePath)
                    | None -> refreshedModel

                selectionAdjustedModel, pageChange, warningMessage
            | CloneSuccess _ ->
                model, GitPageChange.NoChange, None

        let nextModel =
            baseModel
            |> transition (SetBusyOperation None)
            |> transition (SetCurrentProgress None)
            |> transition (SetErrorNotice None)
            |> transition (SetWarningNotice warningMessage)

        nextModel,
        Cmd.batch [
            applyPageChangeCmd setPageState pageChange
            resolveWriteCmd request (Ok success)
        ]
    | _ -> transition msg model, Cmd.none

let subscribe (_model: GitState) : Sub<Msg> = [
    [ "gitProgress" ],
    fun dispatch ->
        let dispose =
            Renderer.MainUpdateRendererBridge.subscribeGitProgressUpdate (fun progress ->
                dispatch (SetCurrentProgress(Some(mapProgress progress)))
            )

        { new System.IDisposable with
            member _.Dispose() = dispose ()
        }
]
