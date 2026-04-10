module Renderer.Context.GitWorkflow

open System
open Elmish
open Fable.Core

open Renderer.Types
open Swate.Components.Api.GitLabApi
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
    | InitializingRepository
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
type GitRepositoryAvailability =
    | Ready
    | MissingRepository

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

type InitRepositoryOutcome = { WarningMessage: string option }

type GitState = {
    Status: GitSidebarStatus
    ChangedFiles: GitSidebarChange[]
    BranchOptions: GitSidebarBranchOption[]
    LfsAutoTrackThresholdMb: int
    DownloadLargeFiles: bool
    RepositoryAvailability: GitRepositoryAvailability
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
        RepositoryAvailability = GitRepositoryAvailability.Ready
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
    | Fetch
    | Pull
    | Push
    | Sync
    | Clone of GitCloneRepositoryRequest * Reply<string>
    | CommitSelection of PreparedCommitOperation
    | CommitAll of PreparedCommitOperation
    | SaveLfsSettings of GitBusyOperation * GitLfsSettingsDto
    | CreateBranch of GitCreateBranchRequest
    | SwitchBranch of GitCheckoutBranchRequest

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
    | SetCurrentProgress of GitSidebarProgress option
    | ArcPathChanged of ArcRootPath
    | RefreshRequested
    | RefreshCompleted of requestId: int * result: Result<GitRefreshResult, string>
    | InitRepositoryRequested of remoteProjectName: string option
    | InitRepositoryCompleted of sessionId: int * result: Result<InitRepositoryOutcome, string>
    | SelectChangeRequested of GitSidebarChange * Reply<unit>
    | SelectChangeCompleted of
        requestId: int *
        path: string *
        reply: Reply<unit> *
        result: Result<GitPageChange, string>
    | ConfirmMergeResolutionRequested of GitConfirmMergeResolutionRequest
    | ConfirmMergeResolutionCompleted of sessionId: int * result: Result<ConfirmMergeResolutionOutcome, string>
    | SaveLfsAutoTrackThresholdRequested of int
    | SaveDownloadLargeFilesRequested of bool
    | FetchRequested
    | PullRequested
    | PushRequested
    | SyncRequested
    | CloneRequested of GitCloneRepositoryRequest * Reply<string>
    | CommitSelectionRequested of GitSidebarCommitSelectionRequest
    | CommitAllRequested of string
    | CreateBranchRequested of GitSidebarCreateBranchRequest
    | SwitchBranchRequested of string
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
    initGitRepository: string -> JS.Promise<Result<string, string>>
    createDataHubProject: string -> JS.Promise<Result<ExploreProjectDto, string>>
    installGitLfs: unit -> JS.Promise<Result<GitOperationResult, string>>
    gitFetch: GitRemoteOperationRequest -> JS.Promise<Result<GitOperationResult, string>>
    gitPull: GitRemoteOperationRequest -> JS.Promise<Result<GitOperationResult, string>>
    gitPush: GitRemoteOperationRequest -> JS.Promise<Result<GitOperationResult, string>>
    gitAddRemote: GitRemoteConfigRequest -> JS.Promise<Result<GitOperationResult, string>>
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

let staleArcSessionMessage =
    "Git operation was canceled because the active ARC changed."

let private isMissingRepositoryMessage (message: string) =
    message.ToLowerInvariant().Contains("not a git repository")

let isStaleMergeConflictError (message: string) =
    let normalizedMessage = message.ToLowerInvariant()

    staleMergeConflictTokens
    |> Array.exists (fun token -> normalizedMessage.Contains(token))

let busyNoticeFromOperation =
    function
    | GitBusyOperation.Refreshing -> Some "Refreshing Git state"
    | GitBusyOperation.InitializingRepository -> Some "Initializing repository"
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
            RepositoryAvailability = GitRepositoryAvailability.Ready
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

/// Resolves a caller-provided reply callback synchronously when the command runs.
/// No IPC or async work is needed — the reply executes in the same dispatch cycle.
let private resolveReplyCmd (reply: Reply<'T>) (result: Result<'T, string>) : Cmd<'msg> = [
    fun _dispatch -> reply result
]

let private applyPageChangeCmd (setPageState: PageState option -> unit) =
    function
    | GitPageChange.NoChange -> Cmd.none
    | GitPageChange.Set page -> [
        fun _dispatch -> setPageState (Some page)
      ]
    | GitPageChange.Clear -> [
        fun _dispatch -> setPageState None
      ]

let private withBusyOperation busyOperation model = {
    model with
        BusyOperation = busyOperation
        BusyNotice = busyOperation |> Option.bind busyNoticeFromOperation
}

let private startRefreshRequest requestId model = {
    model with
        RefreshRequestId = requestId
        RefreshState = GitRefreshState.Loading
}

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

let private runInitRepositoryAsync
    (deps: GitDependencies)
    (arcPath: string)
    (remoteProjectName: string option)
    =
    promise {
        let! initResult = deps.initGitRepository arcPath

        match initResult with
        | Error message -> return Error message
        | Ok _ ->
            match remoteProjectName |> Option.map _.Trim() |> Option.filter (String.IsNullOrWhiteSpace >> not) with
            | None -> return Ok { WarningMessage = None }
            | Some projectName ->
                let! projectResult = deps.createDataHubProject projectName

                match projectResult with
                | Error message ->
                    return
                        Ok {
                            WarningMessage = Some message
                        }
                | Ok project ->
                    let! addRemoteResult =
                        deps.gitAddRemote {
                            RemoteName = "origin"
                            RemoteUrl = project.http_url_to_repo
                        }

                    match addRemoteResult with
                    | Error message ->
                        return
                            Ok {
                                WarningMessage = Some message
                            }
                    | Ok operationResult when operationResult.Success ->
                        return Ok { WarningMessage = None }
                    | Ok operationResult ->
                        return
                            Ok {
                                WarningMessage =
                                    Some(operationResult.Message |> Option.defaultValue "Adding origin remote failed.")
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
    | Fetch -> GitBusyOperation.FetchingFromRemote
    | Pull -> GitBusyOperation.PullingFromRemote
    | Push -> GitBusyOperation.PushingToRemote
    | Sync -> GitBusyOperation.PushingToRemote
    | Clone _ -> GitBusyOperation.CloningRepository
    | CommitSelection prepared -> prepared.BusyOperation
    | CommitAll prepared -> prepared.BusyOperation
    | SaveLfsSettings(busyOperation, _) -> busyOperation
    | CreateBranch _ -> GitBusyOperation.CreatingBranch
    | SwitchBranch _ -> GitBusyOperation.SwitchingBranch

let private requiresArcForWriteRequest =
    function
    | Clone _ -> false
    | _ -> true

let private resolveCloneReplyCmd request result =
    match request, result with
    | Clone(_, reply), Ok(CloneSuccess path) -> resolveReplyCmd reply (Ok path)
    | Clone(_, reply), Error message -> resolveReplyCmd reply (Error message)
    | Clone(_, reply), Ok _ -> resolveReplyCmd reply (Error "Clone request produced an invalid result.")
    | _ -> Cmd.none

let private resolveStaleWriteCompletedCmd request result =
    match result with
    | Ok(Completed success) -> resolveCloneReplyCmd request (Ok success)
    | Ok(RequiresLfsInstall _) -> resolveCloneReplyCmd request (Error staleArcSessionMessage)
    | Error message -> resolveCloneReplyCmd request (Error message)

let private writeErrorModel (message: string) (model: GitState) = {
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
                    Completed(
                        UnitSuccess(
                            refreshResult,
                            pageChange,
                            Some(Some firstConflictPath),
                            operationResult.WarningMessage
                        )
                    )
                )
        | Ok _, None ->
            return
                Ok(Completed(UnitSuccess(refreshResult, GitPageChange.NoChange, None, operationResult.WarningMessage)))
}

let private runSyncAttemptAsync (deps: GitDependencies) (state: GitState) = promise {
    if shouldPublishCurrentBranchFirst state then
        return!
            runSimpleWriteAttemptAsync
                deps
                GitBusyOperation.PushingToRemote
                (fun () -> deps.gitPush { Remote = None; Branch = None })
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
                    latestStatus.Conflicted |> Array.tryHead |> Option.map Some

                let! pageChangeResult =
                    if latestStatus.Conflicted.Length > 0 then
                        loadPageAsync deps latestStatus.Conflicted.[0] true
                    else
                        promise { return Ok GitPageChange.NoChange }

                return
                    pageChangeResult
                    |> Result.map (fun pageChange ->
                        Completed(
                            UnitSuccess(
                                refreshResult,
                                pageChange,
                                selectedChangePathOverride,
                                pullOperation.WarningMessage
                            )
                        )
                    )
            | Ok _, None ->
                return!
                    runSimpleWriteAttemptAsync
                        deps
                        GitBusyOperation.PushingToRemote
                        (fun () -> deps.gitPush { Remote = None; Branch = None })
}

let private runCommitAttemptAsync (deps: GitDependencies) (prepared: PreparedCommitOperation) = promise {
    if String.IsNullOrWhiteSpace prepared.NormalizedMessage then
        return Error "Commit message must not be empty."
    elif prepared.PathsToCommit.Length = 0 then
        return Error "No changes available to commit."
    else
        let! unstageResult =
            if prepared.CurrentlyStagedPaths.Length = 0 then
                promise { return Ok successfulNoopOperationResult }
            else
                deps.gitUnstagePaths {
                    Pathspecs = prepared.CurrentlyStagedPaths
                }

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
    | Fetch ->
        return!
            runSimpleWriteAttemptAsync
                deps
                GitBusyOperation.FetchingFromRemote
                (fun () -> deps.gitFetch { Remote = None; Branch = None })
    | Pull -> return! runPullAttemptAsync deps
    | Push ->
        return!
            runSimpleWriteAttemptAsync
                deps
                GitBusyOperation.PushingToRemote
                (fun () -> deps.gitPush { Remote = None; Branch = None })
    | Sync -> return! runSyncAttemptAsync deps state
    | Clone(request, _) -> return! runCloneAttemptAsync deps request
    | CommitSelection prepared -> return! runCommitAttemptAsync deps prepared
    | CommitAll prepared -> return! runCommitAttemptAsync deps prepared
    | SaveLfsSettings(busyOperation, settings) -> return! runSaveLfsSettingsAttemptAsync deps busyOperation settings
    | CreateBranch request ->
        return! runSimpleWriteAttemptAsync deps GitBusyOperation.CreatingBranch (fun () -> deps.createBranch request)
    | SwitchBranch request ->
        return! runSimpleWriteAttemptAsync deps GitBusyOperation.SwitchingBranch (fun () -> deps.checkoutBranch request)
}

let init () : GitState * Cmd<Msg> = GitState.Empty, Cmd.none

let private missingRepositoryModel (model: GitState) = {
    model with
        Status = GitState.Empty.Status
        ChangedFiles = [||]
        BranchOptions = [||]
        LfsAutoTrackThresholdMb = GitState.Empty.LfsAutoTrackThresholdMb
        DownloadLargeFiles = GitState.Empty.DownloadLargeFiles
        RepositoryAvailability = GitRepositoryAvailability.MissingRepository
        RefreshState = GitRefreshState.Idle
        BusyOperation = None
        BusyNotice = None
        CurrentProgress = None
        ErrorNotice = None
        WarningNotice = None
        SelectedChangePath = None
        MergeResolutionPendingPath = None
        InstallRetryState = GitInstallRetryState.Idle
}

let update
    (deps: GitDependencies)
    (setPageState: PageState option -> unit)
    (msg: Msg)
    (model: GitState)
    : GitState * Cmd<Msg> =
    match msg with
    | ResetWorkflow -> GitState.Empty, Cmd.none
    | SetCurrentProgress currentProgress ->
        {
            model with
                CurrentProgress = currentProgress
        },
        Cmd.none
    | ArcPathChanged arcPath when arcPath = model.CurrentArcPath -> model, Cmd.none
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
                    Cmd.ofMsg RefreshRequested
                ]
            | None -> applyPageChangeCmd setPageState GitPageChange.Clear

        nextModel, cmd
    | RefreshRequested when model.CurrentArcPath.IsNone ->
        {
            GitState.Empty with
                CurrentArcPath = model.CurrentArcPath
                ArcSessionId = model.ArcSessionId
        },
        Cmd.none
    | RefreshRequested ->
        let requestId = nextRefreshRequestId model

        let nextModel =
            model
            |> withBusyOperation (Some GitBusyOperation.Refreshing)
            |> startRefreshRequest requestId
            |> fun state -> {
                state with
                    ErrorNotice = None
                    WarningNotice = None
            }

        let cmd =
            Cmd.OfPromise.either
                refreshAllAsync
                deps
                (fun refreshResult -> RefreshCompleted(requestId, Ok refreshResult))
                (fun err -> RefreshCompleted(requestId, Error(string err)))

        nextModel, cmd
    | RefreshCompleted(requestId, _) when requestId <> model.RefreshRequestId -> model, Cmd.none
    | RefreshCompleted(_, Error message) when isMissingRepositoryMessage message ->
        missingRepositoryModel model, Cmd.none
    | RefreshCompleted(_, Error message) ->
        let nextModel = {
            model with
                RefreshState = GitRefreshState.Idle
                BusyOperation = None
                BusyNotice = None
                CurrentProgress = None
                ErrorNotice = Some message
                WarningNotice = None
        }

        nextModel, applyPageChangeCmd setPageState GitPageChange.Clear
    | RefreshCompleted(_, Ok refreshResult)
        when refreshErrorMessage refreshResult |> Option.exists isMissingRepositoryMessage ->
        missingRepositoryModel model, Cmd.none
    | RefreshCompleted(requestId, Ok refreshResult) ->
        let nextModel =
            model
            |> applyRefreshResult refreshResult
            |> withBusyOperation None
            |> fun state -> { state with CurrentProgress = None }

        let hasError =
            match refreshResult.Status, refreshErrorMessage refreshResult with
            | Error _, _
            | Ok _, Some _ -> true
            | Ok _, None -> false

        let cmd =
            if hasError then
                applyPageChangeCmd setPageState GitPageChange.Clear
            else
                Cmd.none

        nextModel, cmd
    | InitRepositoryRequested _ when model.CurrentArcPath.IsNone -> model, Cmd.none
    | InitRepositoryRequested remoteProjectName ->
        let nextModel =
            model
            |> withBusyOperation (Some GitBusyOperation.InitializingRepository)
            |> fun state -> {
                state with
                    ErrorNotice = None
                    WarningNotice = None
            }

        let cmd =
            Cmd.OfPromise.either
                (fun (deps, arcPath, remoteProjectName) -> runInitRepositoryAsync deps arcPath remoteProjectName)
                (deps, Option.get model.CurrentArcPath, remoteProjectName)
                (fun result -> InitRepositoryCompleted(model.ArcSessionId, result))
                (fun err -> InitRepositoryCompleted(model.ArcSessionId, Error(string err)))

        nextModel, cmd
    | InitRepositoryCompleted(sessionId, _) when sessionId <> model.ArcSessionId -> model, Cmd.none
    | InitRepositoryCompleted(_, Error message) ->
        let nextModel = {
            model with
                BusyOperation = None
                BusyNotice = None
                CurrentProgress = None
                ErrorNotice = Some message
                WarningNotice = None
        }

        nextModel, Cmd.none
    | InitRepositoryCompleted(_, Ok outcome) ->
        let nextModel = {
            model with
                RepositoryAvailability = GitRepositoryAvailability.Ready
                BusyOperation = None
                BusyNotice = None
                CurrentProgress = None
                ErrorNotice = None
                WarningNotice = outcome.WarningMessage
        }

        nextModel, Cmd.ofMsg RefreshRequested
    | SelectChangeRequested(_, reply) when model.CurrentArcPath.IsNone ->
        model, resolveReplyCmd reply (Error "No ARC is loaded.")
    | SelectChangeRequested(change, reply) ->
        let requestId = nextPageLoadRequestId model

        let nextModel = {
            model with
                ErrorNotice = None
                PageLoadRequestId = requestId
        }

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
    | SelectChangeCompleted(_, path, reply, Ok pageChange) ->
        let nextModel = {
            model with
                SelectedChangePath = Some path
                ErrorNotice = None
        }

        nextModel,
        Cmd.batch [
            applyPageChangeCmd setPageState pageChange
            resolveReplyCmd reply (Ok())
        ]
    | SelectChangeCompleted(_, _path, reply, Error message) ->
        let nextModel = {
            model with
                ErrorNotice = Some message
        }

        nextModel, resolveReplyCmd reply (Error message)
    | ConfirmMergeResolutionRequested _ when model.CurrentArcPath.IsNone -> model, Cmd.none
    | ConfirmMergeResolutionRequested request ->
        match model.BusyOperation with
        | Some(GitBusyOperation.ConfirmingMergeResolution _) -> model, Cmd.none
        | _ when model.MergeResolutionPendingPath = Some request.Path -> model, Cmd.none
        | _ ->
            let nextModel =
                model
                |> withBusyOperation (Some(GitBusyOperation.ConfirmingMergeResolution request.Path))
                |> fun state -> {
                    state with
                        MergeResolutionPendingPath = Some request.Path
                        ErrorNotice = None
                        WarningNotice = None
                }

            let cmd =
                Cmd.OfPromise.either
                    (fun (deps, request) -> confirmMergeResolutionAsync deps request)
                    (deps, request)
                    (fun result -> ConfirmMergeResolutionCompleted(model.ArcSessionId, result))
                    (fun err -> ConfirmMergeResolutionCompleted(model.ArcSessionId, Error(string err)))

            nextModel, cmd
    | ConfirmMergeResolutionCompleted(sessionId, _) when sessionId <> model.ArcSessionId -> model, Cmd.none
    | ConfirmMergeResolutionCompleted(_, Error message) ->
        let nextModel = {
            model with
                BusyOperation = None
                BusyNotice = None
                CurrentProgress = None
                MergeResolutionPendingPath = None
                ErrorNotice = Some message
                WarningNotice = None
        }

        if isStaleMergeConflictError message then
            let selectionClearedModel = {
                nextModel with
                    SelectedChangePath = None
            }

            selectionClearedModel,
            Cmd.batch [
                applyPageChangeCmd setPageState GitPageChange.Clear
                Cmd.ofMsg RefreshRequested
            ]
        else
            nextModel, Cmd.none
    | ConfirmMergeResolutionCompleted(_, Ok outcome) ->
        let nextModel =
            model
            |> applyStatus outcome.UpdatedStatus
            |> withBusyOperation None
            |> fun state -> {
                state with
                    CurrentProgress = None
                    MergeResolutionPendingPath = None
                    SelectedChangePath = outcome.NextConflictedPath
                    ErrorNotice = None
                    WarningNotice = None
            }

        nextModel, applyPageChangeCmd setPageState outcome.PageChange
    | SaveDownloadLargeFilesRequested downloadLargeFiles when model.CurrentArcPath.IsNone ->
        {
            model with
                DownloadLargeFiles = downloadLargeFiles
        },
        Cmd.none
    | SaveDownloadLargeFilesRequested downloadLargeFiles ->
        model,
        Cmd.ofMsg (
            WriteRequested(
                SaveLfsSettings(
                    GitBusyOperation.SavingGitLfsDownloadPreference,
                    buildUpdatedLfsSettings model None (Some downloadLargeFiles)
                )
            )
        )
    | SaveLfsAutoTrackThresholdRequested thresholdMb ->
        model,
        Cmd.ofMsg (
            WriteRequested(
                SaveLfsSettings(
                    GitBusyOperation.SavingGitLfsThreshold,
                    buildUpdatedLfsSettings model (Some thresholdMb) None
                )
            )
        )
    | FetchRequested -> model, Cmd.ofMsg (WriteRequested Fetch)
    | PullRequested -> model, Cmd.ofMsg (WriteRequested Pull)
    | PushRequested -> model, Cmd.ofMsg (WriteRequested Push)
    | SyncRequested -> model, Cmd.ofMsg (WriteRequested Sync)
    | CloneRequested(request, reply) -> model, Cmd.ofMsg (WriteRequested(Clone(request, reply)))
    | CommitSelectionRequested request ->
        model, Cmd.ofMsg (WriteRequested(CommitSelection(prepareCommitSelection model request)))
    | CommitAllRequested message -> model, Cmd.ofMsg (WriteRequested(CommitAll(prepareCommitAll model message)))
    | CreateBranchRequested request ->
        model,
        Cmd.ofMsg (
            WriteRequested(
                CreateBranch {
                    Name = request.BranchName
                    StartPoint = request.StartPoint
                }
            )
        )
    | SwitchBranchRequested branchName ->
        let normalizedBranchName = branchName.Trim()

        if String.IsNullOrWhiteSpace normalizedBranchName then
            model, Cmd.none
        else
            model, Cmd.ofMsg (WriteRequested(SwitchBranch { Name = normalizedBranchName }))
    | WriteRequested request when requiresArcForWriteRequest request && model.CurrentArcPath.IsNone -> model, Cmd.none
    | WriteRequested request ->
        let nextModel =
            model
            |> withBusyOperation (Some(busyOperationForWriteRequest request))
            |> fun state -> {
                state with
                    ErrorNotice = None
                    WarningNotice = None
            }

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
        nextModel, resolveCloneReplyCmd request (Error message)
    | WriteCompleted(sessionId, request, Ok(RequiresLfsInstall promptMessage)) ->
        let nextModel = {
            model with
                InstallRetryState =
                    GitInstallRetryState.PromptingForInstall(promptMessage, busyOperationForWriteRequest request)
        }

        let cmd =
            Cmd.OfFunc.perform
                deps.confirmInstall
                promptMessage
                (fun shouldInstall -> WriteInstallPromptAnswered(sessionId, request, shouldInstall))

        nextModel, cmd
    | WriteInstallPromptAnswered(sessionId, request, _) when sessionId <> model.ArcSessionId ->
        model, resolveCloneReplyCmd request (Error staleArcSessionMessage)
    | WriteInstallPromptAnswered(_, request, false) ->
        let message = "Git LFS installation is required to continue."

        let nextModel =
            model
            |> writeErrorModel message
            |> fun state -> {
                state with
                    InstallRetryState = GitInstallRetryState.Idle
            }

        nextModel, resolveCloneReplyCmd request (Error message)
    | WriteInstallPromptAnswered(sessionId, request, true) ->
        let busyOperation = busyOperationForWriteRequest request

        let nextModel = {
            model with
                InstallRetryState = GitInstallRetryState.InstallingForRetry busyOperation
                BusyOperation = Some GitBusyOperation.InstallingGitLfs
                BusyNotice = busyNoticeFromOperation GitBusyOperation.InstallingGitLfs
        }

        let cmd =
            Cmd.OfPromise.either
                deps.installGitLfs
                ()
                (fun installResult -> WriteInstallCompleted(sessionId, request, installResult))
                (fun err -> WriteInstallCompleted(sessionId, request, Error(string err)))

        nextModel, cmd
    | WriteInstallCompleted(sessionId, request, _) when sessionId <> model.ArcSessionId ->
        model, resolveCloneReplyCmd request (Error staleArcSessionMessage)
    | WriteInstallCompleted(_, request, Error message) ->
        let nextModel =
            model
            |> writeErrorModel message
            |> fun state -> {
                state with
                    InstallRetryState = GitInstallRetryState.Idle
            }

        nextModel, resolveCloneReplyCmd request (Error message)
    | WriteInstallCompleted(_, request, Ok operationResult) when not operationResult.Success ->
        let message =
            operationResult.Message |> Option.defaultValue "Git LFS installation failed."

        let nextModel =
            model
            |> writeErrorModel message
            |> fun state -> {
                state with
                    InstallRetryState = GitInstallRetryState.Idle
            }

        nextModel, resolveCloneReplyCmd request (Error message)
    | WriteInstallCompleted(sessionId, request, Ok _) ->
        let busyOperation = busyOperationForWriteRequest request

        let nextModel = {
            model with
                InstallRetryState = GitInstallRetryState.Idle
                BusyOperation = Some busyOperation
                BusyNotice = busyNoticeFromOperation busyOperation
        }

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
                    | Some selectedChangePath -> {
                        refreshedModel with
                            SelectedChangePath = selectedChangePath
                      }
                    | None -> refreshedModel

                selectionAdjustedModel, pageChange, warningMessage
            | CloneSuccess _ -> model, GitPageChange.NoChange, None

        let nextModel = {
            baseModel with
                BusyOperation = None
                BusyNotice = None
                CurrentProgress = None
                ErrorNotice = None
                WarningNotice = warningMessage
        }

        nextModel,
        Cmd.batch [
            applyPageChangeCmd setPageState pageChange
            resolveCloneReplyCmd request (Ok success)
        ]

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
