module Renderer.Context.GitWorkflow

open System
open Elmish
open Fable.Core

open Renderer.Types
open Swate.Components.Api.GitLabApi
open Swate.Components.Page.GitSidebarTypes
open Swate.Electron.Shared
open Swate.Electron.Shared.IPCTypes.MainToRendererIpc
open Swate.Electron.Shared.VersionControlTypes

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
    | CloningRepository of targetPath: string
    | CommittingSelectedChanges
    | CommittingAllChanges
    | DiscardingSelectedChanges
    | SavingGitLfsThreshold
    | SavingGitLfsDownloadPreference
    | CreatingBranch
    | SwitchingBranch
    | InstallingDependency of componentName: string
    | RenamingRepository
    | PruningGitLfsCache
    | DeduplicatingGitLfsStorage
    | ConfirmingMergeResolution of path: string
    | FinalizingMerge
    | AbandoningMerge
    | ClearingStaleLock
    | RestoringInterruptedPaths
    | RetryingMaterialization

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

/// Whether the user has accepted the update a synchronize flagged. An acceptance carries the
/// target revision and the workspace token the preview was computed for, so it never applies
/// to a state the user did not see.
[<RequireQualifiedAccess>]
type GitUpdateAcceptance =
    | RequirePreview
    | Accepted of observedTarget: string * workspaceVersion: string

/// What the confirmation dialog continues with when the user confirms.
[<RequireQualifiedAccess>]
type GitPendingRemoteAction =
    | None
    | UpdateFromOnline of GitUpdateAcceptance
    | PublishAfterUpdate of GitUpdateAcceptance
    | FinalizeMerge
    /// The dialog offers the recovery stored in PendingRecovery.
    | Recover

type GitPublishRenamePrompt = { CurrentName: string; Message: string }

/// A canceled update left paths that a killed fast-forward may have partially
/// rewritten. The user discards exactly those paths, keeps them, or retries the update.
type GitInterruptedUpdate = {
    AffectedPaths: string[]
    Instructions: string option
}

/// A recovery the user has to act on. Each case maps one structured recovery code of
/// the library to a dialog or notice of the sidebar.
[<RequireQualifiedAccess>]
type GitPendingRecovery =
    | RestoreInterruptedPaths of GitInterruptedUpdate
    | RetryMaterialization of message: string
    | RetryPublish of message: string
    | ClearStaleLock of instructions: string option
    | ClearCloneTarget of targetPath: string * instructions: string option

/// Remote provisioning that has created a project but not finished binding or
/// publishing. A retry resumes here and never creates a second project.
type GitProvisionedRemote = {
    RemoteUrl: string
    ProjectName: string
    IsBound: bool
}

type GitErrorNotification = { Title: string; Message: string }

type GitState = {
    Status: GitSidebarStatus
    ChangedFiles: GitSidebarChange[]
    BranchOptions: GitSidebarBranchOption[]
    /// The provider refs behind BranchOptions, kept so requests carry the opaque ref.
    Refs: LogicalRefDto[]
    OriginRemoteRepositoryWebUrl: string option
    PendingConfirmation: GitSidebarConfirmationDialog option
    PendingRemoteAction: GitPendingRemoteAction
    PendingPublishRename: GitPublishRenamePrompt option
    /// A primary save that still has to publish once the update or merge is done.
    PendingPostMergePush: bool
    PendingRecovery: GitPendingRecovery option
    ProvisionedRemote: GitProvisionedRemote option
    /// A publish that waits until the refresh of a renamed ARC root has loaded the
    /// workspace token.
    PendingPublishAfterRefresh: bool
    PendingPublishForPath: string option
    LfsAutoTrackThresholdMb: int
    DownloadLargeFiles: bool
    RepositoryAvailability: GitRepositoryAvailability
    RefreshState: GitRefreshState
    RefreshRequestId: int
    /// A refresh requested while a write is active runs after the write completes.
    RefreshPending: bool
    BusyOperation: GitBusyOperation option
    BusyNotice: string option
    /// The operation the renderer can cancel. The id is known before the call starts,
    /// the session id arrives with the started event.
    CurrentOperation: OperationKeyDto option
    CurrentProgress: GitSidebarProgress option
    ErrorNotice: string option
    WarningNotice: string option
    PendingRefreshWarningNotice: string option
    SelectedChangePath: string option
    MergeResolutionPendingPath: string option
    InstallRetryState: GitInstallRetryState
    PageLoadRequestId: int
    WriteRequestId: int
    CurrentArcPath: ArcRootPath
    ArcSessionId: int
    /// Opaque optimistic-concurrency token of the last refreshed status.
    WorkspaceVersion: string option
    ActiveConflict: ConflictSessionSummaryDto option
    Services: ServiceAvailabilityDto option
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
        Refs = [||]
        OriginRemoteRepositoryWebUrl = None
        PendingConfirmation = None
        PendingRemoteAction = GitPendingRemoteAction.None
        PendingPublishRename = None
        PendingPostMergePush = false
        PendingRecovery = None
        ProvisionedRemote = None
        PendingPublishAfterRefresh = false
        PendingPublishForPath = None
        LfsAutoTrackThresholdMb = 1
        DownloadLargeFiles = false
        RepositoryAvailability = GitRepositoryAvailability.Ready
        RefreshState = GitRefreshState.Idle
        RefreshRequestId = 0
        RefreshPending = false
        BusyOperation = None
        BusyNotice = None
        CurrentOperation = None
        CurrentProgress = None
        ErrorNotice = None
        WarningNotice = None
        PendingRefreshWarningNotice = None
        SelectedChangePath = None
        MergeResolutionPendingPath = None
        InstallRetryState = GitInstallRetryState.Idle
        PageLoadRequestId = 0
        WriteRequestId = 0
        CurrentArcPath = None
        ArcSessionId = 0
        WorkspaceVersion = None
        ActiveConflict = None
        Services = None
    }

type GitRefreshResult = {
    Session: Result<WorkspaceSessionInfoDto, OperationFailureDto>
    Status: Result<WorkspaceStatusDto, OperationFailureDto>
    Refs: Result<LogicalRefDto[], OperationFailureDto>
    LfsSettings: Result<StoragePolicySettingsDto, OperationFailureDto>
    OriginRemoteRepositoryWebUrl: string option
}

type Reply<'T> = Result<'T, string> -> unit

/// The conflict page the user reviewed: handle and workspace token captured with the
/// preview, so a confirmation is checked against exactly that state.
type GitMergeResolutionRequest = {
    Path: string
    Handle: ConflictSessionHandleDto
    WorkspaceVersion: string
    ResolvedContent: string
}

type ConfirmMergeResolutionOutcome = {
    UpdatedStatus: WorkspaceStatusDto
    NextConflictedPath: string option
    PageChange: GitPageChange
    /// True when the last item was resolved and the merge was finalized.
    Finalized: bool
}

[<RequireQualifiedAccess>]
type ConfirmMergeResolutionError =
    /// The handle or the workspace changed since the preview was shown.
    | Stale of message: string
    | Failed of message: string

type PreparedCommitOperation = {
    BusyOperation: GitBusyOperation
    NormalizedMessage: string
    PathsToCommit: string[]
}

type WriteRequest =
    | Fetch
    | Pull of GitUpdateAcceptance
    | Push of GitUpdateAcceptance
    | PrimarySave of PreparedCommitOperation
    | Clone of CloneWorkspaceRequestDto * Reply<string>
    | CommitSelection of PreparedCommitOperation
    | CommitAll of PreparedCommitOperation
    | DiscardSelection of string[]
    | SaveLfsSettings of GitBusyOperation * StoragePolicySettingsDto
    | PruneLfsCache
    | DedupLfsStorage
    | CreateBranch of GitSidebarCreateBranchRequest
    | SwitchBranch of string
    | FinalizeMerge
    | AbandonMerge
    | ClearStaleLock
    | RestoreInterruptedPaths of string[]
    | RetryMaterialization

/// SelectedChangePath uses None for no override and Some None to clear the selection.
/// Published says whether the synchronize published. It is None when the write was not a synchronize.
type WriteUnitSuccess = {
    Refresh: GitRefreshResult
    PageChange: GitPageChange
    SelectedChangePath: string option option
    Warning: string option
    Partial: OperationFailureDto option
    Published: bool option
}

type WriteSuccess =
    | UnitSuccess of WriteUnitSuccess
    | CloneSuccess of string

type WriteAttemptOutcome =
    | Completed of WriteSuccess
    | CompletedWithPendingRemoteConfirmation of WriteSuccess * GitSidebarConfirmationDialog * GitPendingRemoteAction
    | CompletedWithPendingRemoteFailure of WriteSuccess * string
    | RequiresRemoteProjectRename of string
    | RequiresDependencyInstall of componentName: string * promptMessage: string
    | RequiresRecovery of GitPendingRecovery * message: string
    /// The workspace token was stale while the workspace itself did not change. The
    /// write is retried once against a refreshed token before this reaches the user.
    | StaleWorkspaceVersion of message: string * targetMoved: bool
    | OperationCancelled of string
    /// The remote project exists now. Binding or publishing still has to be retried.
    | ProvisioningIncomplete of GitProvisionedRemote * message: string

[<RequireQualifiedAccess>]
type private RoutedFailure =
    | Cancelled of string
    | DependencyInstall of message: string
    | Recovery of GitPendingRecovery * message: string
    | RefreshAfterCancel of message: string
    | RefreshThenReport of message: string
    | StaleWorkspace of message: string * targetMoved: bool
    /// The workspace has a conflict session the user has to resolve first.
    | ConflictSession of OperationFailureDto
    | UpdateAcceptanceRequired of dialog: GitSidebarConfirmationDialog * observedTarget: string
    | Error of string

type Msg =
    | ResetWorkflow
    | SetCurrentProgress of GitSidebarProgress option
    | OperationStarted of OperationKeyDto
    | ArcPathChanged of ArcRootPath
    | GitRepositoryInitialized of arcPath: string
    | RefreshRequested
    | RefreshCompleted of requestId: int * result: Result<GitRefreshResult, string>
    | InitRepositoryRequested
    | InitRepositoryCompleted of sessionId: int * result: Result<unit, string>
    | SelectChangeRequested of GitSidebarChange * Reply<unit>
    | SelectChangeCompleted of
        requestId: int *
        path: string *
        reply: Reply<unit> *
        result: Result<GitPageChange, string>
    | ConfirmMergeResolutionRequested of GitMergeResolutionRequest
    | ConfirmMergeResolutionCompleted of
        sessionId: int *
        result: Result<ConfirmMergeResolutionOutcome, ConfirmMergeResolutionError>
    | SaveLfsAutoTrackThresholdRequested of int
    | SaveDownloadLargeFilesRequested of bool
    | FetchRequested
    | PullRequested
    | PushRequested
    | CancelCurrentOperationRequested
    | CancelCurrentOperationCompleted of sessionId: int * operationKey: OperationKeyDto * Result<bool, string>
    | UpdateFromOnlineRequested
    | CloneRequested of CloneWorkspaceRequestDto * Reply<string>
    | PrimarySaveSelectionRequested of GitSidebarCommitSelectionRequest
    | PrimarySaveAllRequested of string
    | CommitSelectionRequested of GitSidebarCommitSelectionRequest
    | CommitAllRequested of string
    | DiscardSelectionRequested of string[]
    | ConfirmPendingRemoteActionRequested
    | CancelPendingRemoteActionRequested
    | SubmitPublishRenameRequested of newName: string
    | CancelPublishRenameRequested
    | PublishRenameCompleted of sessionId: int * result: Result<string, string>
    | CreateBranchRequested of GitSidebarCreateBranchRequest
    | SwitchBranchRequested of string
    | SwitchBranchPreflightCompleted of
        sessionId: int *
        refName: string *
        Result<OperationResultDto<SwitchPreflightDto>, string>
    | PruneLfsCacheRequested
    | DedupLfsStorageRequested
    | ClearStaleLockRequested
    | RestoreInterruptedPathsRequested
    | RetryMaterializationRequested
    | DismissRecoveryRequested
    | WriteRequested of WriteRequest
    | WritePhaseChanged of sessionId: int * writeRequestId: int * GitBusyOperation
    | WriteCompleted of sessionId: int * writeRequestId: int * WriteRequest * Result<WriteAttemptOutcome, string>
    | WriteInstallPromptAnswered of sessionId: int * WriteRequest * componentName: string * bool
    | WriteInstallCompleted of sessionId: int * WriteRequest * Result<OperationResultDto<DependencyStatusDto>, string>

/// What the renderer needs from the main process. Every function returns the
/// structured result of the library, so the workflow routes on categories and codes.
/// Calls that can be canceled take the request with the operation id the workflow
/// allocated, so the cancel button knows the id before the call has started.
type GitDependencies = {
    getSessionInfo: OperationRequestDto -> JS.Promise<Result<OperationResultDto<WorkspaceSessionInfoDto>, string>>
    getStatus: OperationRequestDto -> JS.Promise<Result<OperationResultDto<WorkspaceStatusDto>, string>>
    listRefs: OperationRequestDto -> JS.Promise<Result<OperationResultDto<LogicalRefDto[]>, string>>
    getRepositoryWebUrl: OperationRequestDto -> JS.Promise<Result<OperationResultDto<string option>, string>>
    getStoragePolicySettings:
        OperationRequestDto -> JS.Promise<Result<OperationResultDto<StoragePolicySettingsDto>, string>>
    setStoragePolicySettings: StoragePolicySettingsRequestDto -> JS.Promise<Result<OperationResultDto<unit>, string>>
    loadDiffPage: GitSidebarChange -> JS.Promise<Result<PageState, string>>
    loadConflictPage: ConflictSessionSummaryDto -> string -> string -> JS.Promise<Result<PageState, string>>
    initializeWorkspace: InitializeWorkspaceRequestDto -> JS.Promise<Result<OperationResultDto<string>, string>>
    bindWorkspace: BindWorkspaceRequestDto -> JS.Promise<Result<OperationResultDto<WorkspaceSessionInfoDto>, string>>
    createRemoteProject: string -> JS.Promise<Result<ExploreProjectDto, GitLabError>>
    renameOpenArcRoot: string -> JS.Promise<Result<string, string>>
    checkDependencies: OperationRequestDto -> JS.Promise<Result<OperationResultDto<DependencyStatusDto[]>, string>>
    installDependency:
        InstallDependencyRequestDto -> JS.Promise<Result<OperationResultDto<DependencyStatusDto>, string>>
    refreshSynchronization:
        OperationRequestDto -> JS.Promise<Result<OperationResultDto<SynchronizationStateDto>, string>>
    synchronize: SynchronizeRequestDto -> JS.Promise<Result<OperationResultDto<SynchronizationStateDto>, string>>
    /// The write command replaces this with a dispatch of WritePhaseChanged, and nothing outside writeCmd reports phases.
    reportPhase: GitBusyOperation -> unit
    cancelOperation: OperationKeyDto -> JS.Promise<Result<bool, string>>
    cloneWorkspace: CloneWorkspaceRequestDto -> JS.Promise<Result<OperationResultDto<string>, string>>
    createRef: CreateRefRequestDto -> JS.Promise<Result<OperationResultDto<LogicalRefDto>, string>>
    switchRef: SwitchRefRequestDto -> JS.Promise<Result<OperationResultDto<WorkspaceStatusDto>, string>>
    preflightSwitchRef: SwitchRefRequestDto -> JS.Promise<Result<OperationResultDto<SwitchPreflightDto>, string>>
    createRevision: CreateRevisionRequestDto -> JS.Promise<Result<OperationResultDto<string>, string>>
    restorePaths: RestorePathsRequestDto -> JS.Promise<Result<OperationResultDto<unit>, string>>
    resolveConflict:
        ResolveConflictRequestDto -> JS.Promise<Result<OperationResultDto<ConflictResolutionOutcomeDto>, string>>
    finalizeConflict: FinalizeConflictRequestDto -> JS.Promise<Result<OperationResultDto<string option>, string>>
    cancelConflict: CancelConflictRequestDto -> JS.Promise<Result<OperationResultDto<unit>, string>>
    listObjects: OperationRequestDto -> JS.Promise<Result<OperationResultDto<ObjectStateDto[]>, string>>
    materializeObject: ObjectPathRequestDto -> JS.Promise<Result<OperationResultDto<unit>, string>>
    pruneStorage: OperationRequestDto -> JS.Promise<Result<OperationResultDto<string>, string>>
    deduplicateStorage: OperationRequestDto -> JS.Promise<Result<OperationResultDto<string>, string>>
    clearStaleLock: OperationRequestDto -> JS.Promise<Result<OperationResultDto<WorkspaceStatusDto>, string>>
    newOperationId: unit -> string
    confirmLfsPrune: string -> bool
    confirmInstall: string -> bool
    reportError: GitErrorNotification -> unit
}

let staleArcSessionMessage =
    "Git operation was canceled because the active ARC changed."

let busyNoticeFromOperation =
    function
    | GitBusyOperation.Refreshing -> Some "Refreshing Git state"
    | GitBusyOperation.InitializingRepository -> Some "Initializing repository"
    | GitBusyOperation.FetchingFromRemote -> Some "Fetching from remote"
    | GitBusyOperation.PullingFromRemote -> Some "Pulling from remote"
    | GitBusyOperation.PushingToRemote -> Some "Pushing to remote"
    | GitBusyOperation.CloningRepository _ -> Some "Cloning repository"
    | GitBusyOperation.CommittingSelectedChanges -> Some "Committing selected changes"
    | GitBusyOperation.CommittingAllChanges -> Some "Committing all changes"
    | GitBusyOperation.DiscardingSelectedChanges -> Some "Discarding selected changes"
    | GitBusyOperation.SavingGitLfsThreshold -> Some "Saving Git LFS threshold"
    | GitBusyOperation.SavingGitLfsDownloadPreference -> Some "Saving Git LFS download preference"
    | GitBusyOperation.CreatingBranch -> Some "Creating branch"
    | GitBusyOperation.SwitchingBranch -> Some "Switching branch"
    | GitBusyOperation.InstallingDependency componentName -> Some $"Installing {componentName}"
    | GitBusyOperation.RenamingRepository -> Some "Renaming ARC"
    | GitBusyOperation.PruningGitLfsCache -> Some "Cleaning Git LFS cache"
    | GitBusyOperation.DeduplicatingGitLfsStorage -> Some "Reducing Git LFS duplicate storage"
    | GitBusyOperation.ConfirmingMergeResolution _ -> Some "Confirming merge resolution"
    | GitBusyOperation.FinalizingMerge -> Some "Finalizing merge"
    | GitBusyOperation.AbandoningMerge -> Some "Abandoning merge"
    | GitBusyOperation.ClearingStaleLock -> Some "Clearing stale lock"
    | GitBusyOperation.RestoringInterruptedPaths -> Some "Restoring interrupted files"
    | GitBusyOperation.RetryingMaterialization -> Some "Downloading large files"

let currentRunStatus (model: GitState) =
    match model.CurrentProgress with
    | Some progress -> Some(GitSidebarRunStatus.Progress progress)
    | None -> model.BusyNotice |> Option.map GitSidebarRunStatus.Busy

let private countOrZero (value: int option) = value |> Option.defaultValue 0

let mapStatus (status: WorkspaceStatusDto) : GitSidebarStatus = {
    CurrentBranch = status.CurrentRef |> Option.map _.Name
    TrackingBranch = status.Synchronization |> Option.bind _.TargetRef |> Option.map _.Name
    Ahead = status.Synchronization |> Option.bind _.LocalRevisionCount |> countOrZero
    Behind = status.Synchronization |> Option.bind _.TargetRevisionCount |> countOrZero
    IsClean = status.Changes.Length = 0
    IsMergeInProgress = status.ActiveConflictSession.IsSome
}

/// Porcelain-style status letters the sidebar already renders.
let private changeCode (kind: FileChangeKindDto) =
    match kind with
    | FileChangeKindDto.Added -> "A"
    | FileChangeKindDto.Modified -> "M"
    | FileChangeKindDto.Deleted -> "D"
    | FileChangeKindDto.Renamed -> "R"
    | FileChangeKindDto.Conflicted -> "U"

let mapChanges (status: WorkspaceStatusDto) : GitSidebarChange[] =
    let conflictedPaths =
        status.ActiveConflictSession
        |> Option.map (fun session -> session.Items |> Array.map _.Path |> Set.ofArray)
        |> Option.defaultValue Set.empty

    status.Changes
    |> Array.map (fun change ->
        let isConflicted =
            change.Kind = FileChangeKindDto.Conflicted
            || conflictedPaths.Contains change.Path

        {
            Path = change.Path
            OriginalPath = change.OldPath
            IndexStatus = if isConflicted then "U" else changeCode change.Kind
            WorkingTreeStatus = if isConflicted then "U" else "."
            IsConflicted = isConflicted
        }
    )

let private mapRefKind (kind: RefKindDto) : GitSidebarBranchKind =
    match kind with
    | RefKindDto.Local -> GitSidebarBranchKind.Local
    | RefKindDto.Remote -> GitSidebarBranchKind.Remote

let mapBranches (targetRef: LogicalRefDto option) (refs: LogicalRefDto[]) : GitSidebarBranchOption[] =
    refs
    |> Array.map (fun reference -> {
        RefName = reference.Name
        DisplayLabel = reference.Name
        Kind = mapRefKind reference.Kind
        IsCurrent = reference.IsCurrent
        IsTracking =
            targetRef
            |> Option.exists (fun target -> target.ProviderRef = reference.ProviderRef)
    })

/// The opaque provider ref behind a branch option. The sidebar works with names, the
/// provider only accepts the ref it handed out.
let providerRefOf (model: GitState) (refName: string) =
    model.Refs
    |> Array.tryFind (fun reference -> String.Equals(reference.Name, refName, StringComparison.Ordinal))
    |> Option.map _.ProviderRef

/// Percentages come from the float counters. The phase code is the stage label.
let mapProgress (progress: VersionControlProgressDto) : GitSidebarProgress = {
    Method = None
    Stage = Some progress.PhaseCode
    ProgressPercent =
        match progress.Completed, progress.Total with
        | Some completed, Some total when total > 0.0 -> Some(Math.Min(100.0, completed / total * 100.0))
        | _ -> None
    Output = progress.DisplayMessage |> Option.map (fun message -> message + "\n")
}

let private appendProgressOutput current incoming =
    match current, incoming with
    | None, None -> None
    | Some output, None -> Some output
    | None, Some output -> Some output
    | Some currentOutput, Some incomingOutput -> Some(currentOutput + incomingOutput)

let private mergeProgressUpdate (model: GitState) (incoming: GitSidebarProgress) =
    let current =
        model.CurrentProgress
        |> Option.defaultValue {
            Method = None
            Stage = model.BusyNotice
            ProgressPercent = None
            Output = None
        }

    {
        Method = incoming.Method |> Option.orElse current.Method
        Stage = incoming.Stage |> Option.orElse current.Stage
        ProgressPercent = incoming.ProgressPercent |> Option.orElse current.ProgressPercent
        Output = appendProgressOutput current.Output incoming.Output
    }

/// The message shown for a structured failure: the library message plus its
/// recovery instructions when it has any.
let failureMessage (failure: OperationFailureDto) =
    match failure.RecoveryAction |> Option.bind _.Instructions with
    | Some instructions when not (String.IsNullOrWhiteSpace instructions) -> $"{failure.Message} {instructions}"
    | _ -> failure.Message

/// A partial success counts as a success here. The explorer actions have no warning
/// channel, so a partial that lands on one of these calls (a session-open failure the
/// host reports once) is dropped on this path.
let toUnitResult (result: Result<OperationResultDto<unit>, string>) : Result<unit, string> =
    match result with
    | Error message -> Error message
    | Ok(OperationResultDto.Succeeded _) -> Ok()
    | Ok(OperationResultDto.PartiallySucceeded _) -> Ok()
    | Ok(OperationResultDto.Failed failure) -> Error(failureMessage failure)

let recoveryCode (failure: OperationFailureDto) =
    failure.RecoveryAction |> Option.map _.Code

let isCanceled (failure: OperationFailureDto) =
    failure.Category = FailureCategoryDto.Canceled

let needsDependencyInstall (failure: OperationFailureDto) =
    failure.Category = FailureCategoryDto.DependencyMissing

/// Remote provisioning is offered only when the workspace has no publication target
/// at all. An unreachable target is an outage, never a missing project.
let needsPublishTarget (failure: OperationFailureDto) =
    failure.Code = VersionControlCodes.PublishTargetMissing

/// The pending recovery a completed write leaves behind, from the structured partial
/// failure the library attached to it.
let recoveryOfPartial (partial: OperationFailureDto option) : GitPendingRecovery option =
    partial
    |> Option.bind (fun failure ->
        match recoveryCode failure with
        | Some code when code = VersionControlCodes.Recovery.RetryMaterialization ->
            Some(GitPendingRecovery.RetryMaterialization(failureMessage failure))
        | Some code when code = VersionControlCodes.Recovery.RetryPublish ->
            Some(GitPendingRecovery.RetryPublish(failureMessage failure))
        | _ -> None
    )

/// The dialog the sidebar shows for a recovery. Confirm runs it, cancel keeps the
/// workspace as it is. Restoring interrupted files discards local changes on them.
let recoveryDialog (recovery: GitPendingRecovery) : GitSidebarConfirmationDialog =
    match recovery with
    | GitPendingRecovery.RestoreInterruptedPaths interrupted ->
        let separator = ", "
        let files = String.Join(separator, interrupted.AffectedPaths)

        let instructions =
            interrupted.Instructions
            |> Option.map (fun text -> $" {text}")
            |> Option.defaultValue ""

        {
            Title = "Update interrupted"
            Message =
                $"The update was canceled while these files may have been partially rewritten: {files}.{instructions} Restore discards the current content of exactly these files. Keep leaves them for review."
            ConfirmLabel = "Restore listed files"
            CancelLabel = "Keep files"
        }
    | GitPendingRecovery.RetryMaterialization message -> {
        Title = "Large files not downloaded"
        Message = $"{message} Download the missing large files now?"
        ConfirmLabel = "Download now"
        CancelLabel = "Later"
      }
    | GitPendingRecovery.RetryPublish message -> {
        Title = "Online sync pending"
        Message = message
        ConfirmLabel = "Publish now"
        CancelLabel = "Later"
      }
    | GitPendingRecovery.ClearStaleLock instructions ->
        let detail =
            instructions |> Option.map (fun text -> $" {text}") |> Option.defaultValue ""

        {
            Title = "Repository lock left behind"
            Message =
                $"A canceled operation left a lock in the repository.{detail} Swate can remove it now because no other operation of this ARC is running."
            ConfirmLabel = "Remove lock"
            CancelLabel = "Leave it"
        }
    | GitPendingRecovery.ClearCloneTarget(targetPath, instructions) ->
        let detail =
            instructions |> Option.map (fun text -> $" {text}") |> Option.defaultValue ""

        {
            Title = "Clone folder not cleaned up"
            Message = $"The canceled clone could not clean up '{targetPath}'.{detail} Clear the folder and clone again."
            ConfirmLabel = "OK"
            CancelLabel = "Dismiss"
        }

let private tryGetPathLeaf (pathValue: string) =
    pathValue
    |> Option.ofObj
    |> Option.map (fun value -> value.Trim().TrimEnd('/', '\\'))
    |> Option.filter (String.IsNullOrWhiteSpace >> not)
    |> Option.bind (fun trimmed ->
        trimmed.Split([| '/'; '\\' |], StringSplitOptions.RemoveEmptyEntries)
        |> Array.tryLast
    )
    |> Option.filter (String.IsNullOrWhiteSpace >> not)

let private publishRenamePrompt message model =
    let currentName =
        model.CurrentArcPath |> Option.bind tryGetPathLeaf |> Option.defaultValue "ARC"

    {
        CurrentName = currentName
        Message = message
    }

let private refreshFailure (refreshResult: GitRefreshResult) : OperationFailureDto option =
    match refreshResult.Session, refreshResult.Status, refreshResult.Refs, refreshResult.LfsSettings with
    | Error failure, _, _, _ -> Some failure
    | _, Error failure, _, _ -> Some failure
    | _, _, Error failure, _ -> Some failure
    | _, _, _, Error failure -> Some failure
    | Ok _, Ok _, Ok _, Ok _ -> None

let private refreshErrorMessage (refreshResult: GitRefreshResult) =
    refreshFailure refreshResult |> Option.map failureMessage

/// A workspace whose folder is not under version control at all. The sidebar then
/// offers to initialize one and shows no error.
let isMissingRepository (failure: OperationFailureDto) =
    failure.Code = VersionControlCodes.WorkspaceUnmanaged

let applyStatus (status: WorkspaceStatusDto) (model: GitState) =
    let mappedChanges = mapChanges status

    let nextSelectedPath =
        model.SelectedChangePath
        |> Option.filter (fun selectedPath -> mappedChanges |> Array.exists (fun change -> change.Path = selectedPath))

    {
        model with
            Status = mapStatus status
            ChangedFiles = mappedChanges
            SelectedChangePath = nextSelectedPath
            WorkspaceVersion = Some status.WorkspaceVersion
            ActiveConflict = status.ActiveConflictSession
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
                WorkspaceVersion = None
                ActiveConflict = None
          }

    let modelWithBranches =
        match refreshResult.Status, refreshResult.Refs with
        | Ok status, Ok refs -> {
            modelWithStatus with
                Refs = refs
                BranchOptions = mapBranches (status.Synchronization |> Option.bind _.TargetRef) refs
          }
        | _ -> {
            modelWithStatus with
                Refs = [||]
                BranchOptions = [||]
          }

    let modelWithSettings =
        match refreshResult.LfsSettings with
        | Ok settings -> {
            modelWithBranches with
                LfsAutoTrackThresholdMb =
                    settings.AutoPolicyThresholdMb
                    |> Option.defaultValue GitState.Empty.LfsAutoTrackThresholdMb
                DownloadLargeFiles = settings.MaterializeLargeObjects
          }
        | Error _ -> modelWithBranches

    {
        modelWithSettings with
            Services = refreshResult.Session |> Result.toOption |> Option.map _.Services
            OriginRemoteRepositoryWebUrl = refreshResult.OriginRemoteRepositoryWebUrl
            RepositoryAvailability = GitRepositoryAvailability.Ready
            RefreshState = GitRefreshState.Idle
            ErrorNotice = refreshErrorMessage refreshResult
            WarningNotice = model.PendingRefreshWarningNotice
            PendingRefreshWarningNotice = None
    }

let private applyWriteSuccessModel model success =
    match success with
    | UnitSuccess success ->
        let refreshedModel = applyRefreshResult success.Refresh model

        let selectionAdjustedModel =
            match success.SelectedChangePath with
            | Some selectedChangePath -> {
                refreshedModel with
                    SelectedChangePath = selectedChangePath
              }
            | None -> refreshedModel

        selectionAdjustedModel,
        success.PageChange,
        success.Warning,
        recoveryOfPartial success.Partial,
        success.Partial |> Option.map failureMessage,
        success.Published
    | CloneSuccess _ -> model, GitPageChange.NoChange, None, None, None, None

let nextRefreshRequestId (model: GitState) = model.RefreshRequestId + 1

let nextPageLoadRequestId (model: GitState) = model.PageLoadRequestId + 1

let nextArcSessionId (model: GitState) = model.ArcSessionId + 1

/// Paths are exact repository keys. Duplicates are dropped, nothing is trimmed.
let private distinctPaths (paths: string[]) =
    paths |> Array.filter (String.IsNullOrEmpty >> not) |> Array.distinct

let prepareCommitSelection (request: GitSidebarCommitSelectionRequest) = {
    BusyOperation = GitBusyOperation.CommittingSelectedChanges
    NormalizedMessage = request.Message.Trim()
    PathsToCommit = distinctPaths request.Paths
}

let prepareCommitAll (state: GitState) (message: string) = {
    BusyOperation = GitBusyOperation.CommittingAllChanges
    NormalizedMessage = message.Trim()
    PathsToCommit = state.ChangedFiles |> Array.map _.Path |> distinctPaths
}

let buildUpdatedLfsSettings
    (state: GitState)
    (thresholdMb: int option)
    (downloadLargeFiles: bool option)
    : StoragePolicySettingsDto =
    {
        AutoPolicyThresholdMb = Some(thresholdMb |> Option.defaultValue state.LfsAutoTrackThresholdMb)
        MaterializeLargeObjects = downloadLargeFiles |> Option.defaultValue state.DownloadLargeFiles
    }

/// Resolves a caller-provided reply callback synchronously when the command runs.
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

let private reportErrorCmd (deps: GitDependencies) (title: string) (message: string) : Cmd<Msg> = [
    fun _dispatch -> deps.reportError { Title = title; Message = message }
]

let private titleForWriteRequest =
    function
    | Fetch -> "Could not fetch changes"
    | Pull _ -> "Could not pull changes"
    | Push _ -> "Could not push changes"
    | PrimarySave _ -> "Could not save changes"
    | Clone _ -> "Could not clone repository"
    | CommitSelection _
    | CommitAll _ -> "Could not commit changes"
    | DiscardSelection _ -> "Could not discard changes"
    | SaveLfsSettings _ -> "Could not save Git LFS settings"
    | PruneLfsCache -> "Could not clean Git LFS cache"
    | DedupLfsStorage -> "Could not reduce Git LFS storage"
    | CreateBranch _ -> "Could not create branch"
    | SwitchBranch _ -> "Could not switch branch"
    | FinalizeMerge -> "Could not finalize merge"
    | AbandonMerge -> "Could not abandon merge"
    | ClearStaleLock -> "Could not clear the stale lock"
    | RestoreInterruptedPaths _ -> "Could not restore interrupted files"
    | RetryMaterialization -> "Could not download large files"

let private reportWriteErrorCmd deps request message =
    reportErrorCmd deps (titleForWriteRequest request) message

let private withBusyOperation busyOperation model = {
    model with
        BusyOperation = busyOperation
        BusyNotice = busyOperation |> Option.bind busyNoticeFromOperation
        CurrentProgress = None
        CurrentOperation = None
}

let private startRefreshRequest requestId model = {
    model with
        RefreshRequestId = requestId
        RefreshState = GitRefreshState.Loading
}

/// A transport failure (the IPC call itself failed) as a structured failure, so the
/// flows below match on one shape.
let private transportFailure (message: string) : OperationFailureDto = {
    Category = FailureCategoryDto.ProviderError
    Code = VersionControlCodes.TransportError
    Message = message
    StateChanged = false
    Retryable = true
    AffectedPaths = [||]
    RecoveryAction = None
    Details = [||]
    RevisionEvidence = [||]
}

/// Flattens the transport error and the structured result: Ok carries the outcome and
/// the partial failure when there is one, Error carries the failure.
let private toResult (call: JS.Promise<Result<OperationResultDto<'T>, string>>) = promise {
    let! result = call

    return
        match result with
        | Error message -> Error(transportFailure message)
        | Ok(OperationResultDto.Succeeded outcome) -> Ok(outcome, None)
        | Ok(OperationResultDto.PartiallySucceeded(outcome, failure)) -> Ok(outcome, Some failure)
        | Ok(OperationResultDto.Failed failure) -> Error failure
}

let private request (deps: GitDependencies) : OperationRequestDto = { OperationId = deps.newOperationId () }

let private refreshAllAsync (deps: GitDependencies) = promise {
    let! sessionResult = toResult (deps.getSessionInfo (request deps))

    match sessionResult with
    | Error failure ->
        return {
            Session = Error failure
            Status = Error failure
            Refs = Error failure
            LfsSettings = Error failure
            OriginRemoteRepositoryWebUrl = None
        }
    | Ok(sessionOutcome, _) ->
        let session = sessionOutcome.Value
        // The session info runs first because it decides whether the repository URL call is
        // needed. The other four calls are independent and run together. and! awaits them
        // through Promise.all, so a rejected call never stays unobserved.
        let! statusResult = toResult (deps.getStatus (request deps))
        and! refsResult = toResult (deps.listRefs (request deps))

        and! settingsResult = promise {
            let! result = toResult (deps.getStoragePolicySettings (request deps))
            return result |> Result.map (fun (outcome, _) -> outcome.Value)
        }

        and! webUrl =
            if session.Services.RepositoryBrowser then
                promise {
                    let! result = toResult (deps.getRepositoryWebUrl (request deps))

                    return
                        match result with
                        | Ok(outcome, _) -> outcome.Value
                        | Error _ -> None
                }
            else
                promise { return None }

        return {
            Session = Ok session
            Status = statusResult |> Result.map (fun (outcome, _) -> outcome.Value)
            Refs = refsResult |> Result.map (fun (outcome, _) -> outcome.Value)
            LfsSettings = settingsResult
            OriginRemoteRepositoryWebUrl = webUrl
        }
}

let private versionOf (refreshResult: GitRefreshResult) =
    refreshResult.Status |> Result.toOption |> Option.map _.WorkspaceVersion

let private runInitRepositoryAsync (deps: GitDependencies) (arcPath: string) = promise {
    let! initResult =
        toResult (
            deps.initializeWorkspace {
                OperationId = deps.newOperationId ()
                TargetPath = arcPath
            }
        )

    match initResult with
    | Error failure -> return Error(failureMessage failure)
    | Ok _ -> return Ok()
}

/// Builds the diff page from the provider's base content and word diff plus the
/// current file. Exposed with its readers as parameters so the tests can drive it.
module GitDiffPageLoader =

    let private unsupportedPage (path: string) (reason: string option) =
        Ok(PageState.GitUnsupportedPage { Path = path; Reason = reason })

    let private contentOf (result: Result<OperationResultDto<ContentViewDto>, string>) =
        match result with
        | Error message -> Error message
        | Ok(OperationResultDto.Failed failure) when failure.Category = FailureCategoryDto.Unsupported ->
            Ok(ContentViewDto.Unsupported(Some failure.Message))
        | Ok(OperationResultDto.Failed failure) -> Error(failureMessage failure)
        | Ok result ->
            OperationResultDto.tryValue result
            |> Option.defaultValue (ContentViewDto.Unsupported None)
            |> Ok

    /// An added file has no committed base and a deleted file has no current content.
    /// Either side is shown as empty text. A change with neither side is an error.
    let load
        (getBaseContent: string -> JS.Promise<Result<OperationResultDto<ContentViewDto>, string>>)
        (getWordDiff: string -> JS.Promise<Result<OperationResultDto<ContentViewDto>, string>>)
        (readCurrentContent: string -> JS.Promise<Result<string, string>>)
        (change: GitSidebarChange)
        : JS.Promise<Result<PageState, string>> =
        promise {
            let requestedPath = change.Path
            let isDeleted = change.IndexStatus = "D" || change.WorkingTreeStatus = "D"
            let! baseContent = getBaseContent requestedPath
            let! wordDiff = getWordDiff requestedPath

            let! currentContent =
                if isDeleted then
                    promise { return Ok None }
                else
                    promise {
                        let! content = readCurrentContent requestedPath
                        return content |> Result.map Some
                    }

            let baseView =
                match baseContent with
                | Ok(OperationResultDto.Failed failure) when failure.Code = VersionControlCodes.BaseContentNotFound ->
                    Ok None
                | other -> contentOf other |> Result.map Some

            match baseView, contentOf wordDiff with
            | Error message, _
            | _, Error message -> return Error message
            | Ok(Some(ContentViewDto.Unsupported reason)), _
            | _, Ok(ContentViewDto.Unsupported reason) -> return unsupportedPage requestedPath reason
            | Ok previous, Ok(ContentViewDto.Text wordDiffText) ->
                let previousText =
                    match previous with
                    | Some(ContentViewDto.Text text) -> Some text
                    | _ -> None

                match currentContent with
                | Error message -> return Error $"Could not read the current content of '{requestedPath}': {message}"
                | Ok None when previousText.IsNone ->
                    return Error $"'{requestedPath}' has no content on either side of the diff."
                | Ok current ->
                    return
                        Ok(
                            PageState.GitDiffPage {
                                Path = requestedPath
                                PreviousContent = previousText |> Option.defaultValue ""
                                CurrentContent = current |> Option.defaultValue ""
                                WordDiffText = wordDiffText
                            }
                        )
        }

let private loadPageAsync
    (deps: GitDependencies)
    (activeConflict: ConflictSessionSummaryDto option)
    (workspaceVersion: string option)
    (change: GitSidebarChange)
    =
    promise {
        let path = change.Path

        let! result =
            match change.IsConflicted, activeConflict, workspaceVersion with
            | true, Some conflict, Some version -> deps.loadConflictPage conflict version path
            | true, _, _ -> promise {
                // The status says the path conflicts but the session is not in the model: reload it
                // together with a fresh token, so the page carries the state the user reviews.
                let! status = toResult (deps.getStatus (request deps))

                match status with
                | Ok(outcome, _) ->
                    match outcome.Value.ActiveConflictSession with
                    | Some conflict -> return! deps.loadConflictPage conflict outcome.Value.WorkspaceVersion path
                    | None -> return! deps.loadDiffPage change
                | Error failure -> return Error(failureMessage failure)
              }
            | false, _, _ -> deps.loadDiffPage change

        return result |> Result.map GitPageChange.Set
    }

let private conflictSessionConfirmationDialog (overlappingPaths: string[]) : GitSidebarConfirmationDialog =
    let overlapping =
        if overlappingPaths.Length > 0 then
            let joined = String.Join(", ", overlappingPaths)
            $" Changed both locally and online: {joined}."
        else
            ""

    {
        Title = "Merge resolution required"
        Message = $"Updating from online will require merge resolution.{overlapping} Continue?"
        ConfirmLabel = "Open Merge Resolution"
        CancelLabel = "Cancel"
    }

let private localChangesOverwriteMessage (affectedPaths: string[]) =
    if affectedPaths.Length > 0 then
        let joined = String.Join(", ", affectedPaths)
        $"Updating from online would change files you also changed locally: {joined}. Save or discard those changes, then try again."
    else
        "Updating from online would change files you also changed locally. Save or discard your local changes, then try again."

let private observedTargetOf (failure: OperationFailureDto) =
    failure.RevisionEvidence
    |> Array.tryFind (fun evidence -> evidence.Label = "observed_target")
    |> Option.map _.Revision

let private hasRevisionEvidence label (failure: OperationFailureDto) =
    failure.RevisionEvidence
    |> Array.exists (fun evidence -> evidence.Label = label)

let private isConflictPartial (failure: OperationFailureDto) =
    failure.Category = FailureCategoryDto.Conflict
    || failure.Code = VersionControlCodes.ConflictsDetected
    || failure.Code = VersionControlCodes.ConflictSessionActive

/// The stale message for a pinned acceptance whose target moved. The write is never
/// replayed, so the text names the online copy instead of the workspace token.
let private movedTargetMessage =
    "The online copy changed since the preview. Review the current changes and try again."

/// Routes a structured failure. Categories and codes decide, never the message.
let private routeFailure (targetPath: string option) (failure: OperationFailureDto) : RoutedFailure =
    if isCanceled failure then
        match recoveryCode failure with
        | Some code when code = VersionControlCodes.Recovery.RestoreWorkspace ->
            RoutedFailure.Recovery(
                GitPendingRecovery.RestoreInterruptedPaths {
                    AffectedPaths = failure.AffectedPaths
                    Instructions = failure.RecoveryAction |> Option.bind _.Instructions
                },
                failureMessage failure
            )
        | Some code when code = VersionControlCodes.Recovery.RemoveCloneTarget ->
            RoutedFailure.Recovery(
                GitPendingRecovery.ClearCloneTarget(
                    targetPath |> Option.defaultValue "",
                    failure.RecoveryAction |> Option.bind _.Instructions
                ),
                failureMessage failure
            )
        | Some code when code = VersionControlCodes.Recovery.RemoveIndexLock ->
            RoutedFailure.Recovery(
                GitPendingRecovery.ClearStaleLock(failure.RecoveryAction |> Option.bind _.Instructions),
                failureMessage failure
            )
        | Some code when code = VersionControlCodes.Recovery.RefreshWorkspace ->
            RoutedFailure.RefreshAfterCancel(failureMessage failure)
        | Some code when
            code = VersionControlCodes.Recovery.InspectWorkspace
            || code = VersionControlCodes.Recovery.AbortMerge
            ->
            RoutedFailure.RefreshThenReport(failureMessage failure)
        | _ -> RoutedFailure.Cancelled(failureMessage failure)
    elif failure.Code = VersionControlCodes.UpdateWouldOverwriteLocalChanges then
        RoutedFailure.Error(localChangesOverwriteMessage failure.AffectedPaths)
    else
        match observedTargetOf failure with
        | Some target when failure.Code = VersionControlCodes.UpdateWouldCreateConflictSession ->
            RoutedFailure.UpdateAcceptanceRequired(conflictSessionConfirmationDialog failure.AffectedPaths, target)
        | _ when needsDependencyInstall failure -> RoutedFailure.DependencyInstall(failureMessage failure)
        | _ when
            failure.Code = VersionControlCodes.PreconditionFailed
            && failure.Category = FailureCategoryDto.Concurrency
            && not failure.StateChanged
            ->
            let targetMoved = hasRevisionEvidence "expected_target" failure

            let message =
                if targetMoved then
                    movedTargetMessage
                else
                    failureMessage failure

            RoutedFailure.StaleWorkspace(message, targetMoved)
        | _ when
            failure.Code = VersionControlCodes.ConflictsDetected
            || failure.Code = VersionControlCodes.ConflictSessionActive
            ->
            RoutedFailure.ConflictSession failure
        | _ when failure.StateChanged -> RoutedFailure.RefreshThenReport(failureMessage failure)
        | _ -> RoutedFailure.Error(failureMessage failure)

/// A dependency failure names no component. The dependency report does: the first
/// component that is missing or incompatible decides whether an installation can be
/// offered (only the provider's configuration component is installable).
let private resolveDependencyComponentAsync (deps: GitDependencies) (message: string) = promise {
    let! report = toResult (deps.checkDependencies (request deps))

    let missing =
        match report with
        | Ok(outcome, _) ->
            outcome.Value
            |> Array.tryFind (fun status -> not status.Installed || not status.Compatible)
        | Error _ -> None

    return
        match missing with
        | Some status ->
            let remediation =
                status.Remediation
                |> Option.filter (String.IsNullOrWhiteSpace >> not)
                |> Option.map (fun text -> $" {text}")
                |> Option.defaultValue ""

            Ok(RequiresDependencyInstall(status.Component, $"{message}{remediation} Install or configure it now?"))
        | None -> Error message
}

/// After an update or a conflict failure: opens the first conflicted item, asks to
/// finalize an emptied conflict session, or reports the refreshed state. The pending
/// warning is the caller's (a primary save keeps its saved-locally notice).
let private completeAfterUpdateAsync
    (deps: GitDependencies)
    (partial: OperationFailureDto option)
    (pendingWarning: string option)
    =
    promise {
        let! refreshResult = refreshAllAsync deps

        match refreshResult.Status, refreshErrorMessage refreshResult with
        | Error failure, _ -> return Error(failureMessage failure)
        | Ok _, Some message -> return Error message
        | Ok latestStatus, None ->
            let isConflictOutcome = partial |> Option.exists isConflictPartial

            let partialToKeep = if isConflictOutcome then None else partial

            match latestStatus.ActiveConflictSession with
            | Some conflict when conflict.Items.Length > 0 ->
                let firstConflictPath = conflict.Items.[0].Path
                let! pageResult = deps.loadConflictPage conflict latestStatus.WorkspaceVersion firstConflictPath

                return
                    pageResult
                    |> Result.map (fun page ->
                        Completed(
                            UnitSuccess {
                                Refresh = refreshResult
                                PageChange = GitPageChange.Set page
                                SelectedChangePath = Some(Some firstConflictPath)
                                Warning = pendingWarning
                                Partial = partialToKeep
                                Published = None
                            }
                        )
                    )
            | Some _ ->
                return
                    Ok(
                        CompletedWithPendingRemoteConfirmation(
                            UnitSuccess {
                                Refresh = refreshResult
                                PageChange = GitPageChange.NoChange
                                SelectedChangePath = None
                                Warning = pendingWarning
                                Partial = partialToKeep
                                Published = None
                            },
                            {
                                Title = "All conflicts resolved"
                                Message =
                                    "Every conflicted file has been resolved. Finalize the merge to keep the result, or abandon it."
                                ConfirmLabel = "Finalize merge"
                                CancelLabel = "Abandon merge"
                            },
                            GitPendingRemoteAction.FinalizeMerge
                        )
                    )
            | None when isConflictOutcome ->
                return
                    Ok(
                        CompletedWithPendingRemoteFailure(
                            UnitSuccess {
                                Refresh = refreshResult
                                PageChange = GitPageChange.NoChange
                                SelectedChangePath = None
                                Warning = None
                                Partial = None
                                Published = None
                            },
                            partial
                            |> Option.map failureMessage
                            |> Option.defaultValue "The update reported conflicts."
                        )
                    )
            | None ->
                return
                    Ok(
                        Completed(
                            UnitSuccess {
                                Refresh = refreshResult
                                PageChange = GitPageChange.NoChange
                                SelectedChangePath = None
                                Warning = partial |> Option.map failureMessage
                                Partial = partial
                                Published = None
                            }
                        )
                    )
    }

let private routedToOutcome (deps: GitDependencies) (routed: RoutedFailure) = promise {
    match routed with
    | RoutedFailure.Cancelled message -> return Ok(OperationCancelled message)
    | RoutedFailure.DependencyInstall message -> return! resolveDependencyComponentAsync deps message
    | RoutedFailure.Recovery(recovery, message) -> return Ok(RequiresRecovery(recovery, message))
    | RoutedFailure.RefreshAfterCancel message ->
        let! refreshResult = refreshAllAsync deps

        match refreshResult.Status with
        | Ok _ ->
            return
                Ok(
                    Completed(
                        UnitSuccess {
                            Refresh = refreshResult
                            PageChange = GitPageChange.NoChange
                            SelectedChangePath = None
                            Warning = Some message
                            Partial = None
                            Published = None
                        }
                    )
                )
        | Error refreshFailure -> return Error(failureMessage refreshFailure)
    | RoutedFailure.RefreshThenReport message ->
        let! refreshResult = refreshAllAsync deps

        match refreshResult.Status with
        | Ok _ ->
            return
                Ok(
                    CompletedWithPendingRemoteFailure(
                        UnitSuccess {
                            Refresh = refreshResult
                            PageChange = GitPageChange.NoChange
                            SelectedChangePath = None
                            Warning = None
                            Partial = None
                            Published = None
                        },
                        message
                    )
                )
        | Error refreshFailure ->
            let refreshMessage = failureMessage refreshFailure

            let combined =
                $"{message} Refreshing the workspace afterwards failed: {refreshMessage}"

            return Error combined
    | RoutedFailure.StaleWorkspace(message, targetMoved) -> return Ok(StaleWorkspaceVersion(message, targetMoved))
    | RoutedFailure.ConflictSession failure -> return! completeAfterUpdateAsync deps (Some failure) None
    | RoutedFailure.UpdateAcceptanceRequired(_, _) ->
        return Error "The synchronization needs a decision that this write cannot offer."
    | RoutedFailure.Error message -> return Error message
}

let private refreshAfterSuccess
    (deps: GitDependencies)
    (partial: OperationFailureDto option)
    (pageChange: GitPageChange)
    (selectedChangePathOverride: string option option)
    (warningMessage: string option)
    =
    promise {
        let! refreshResult = refreshAllAsync deps

        let combinedWarningMessage =
            match warningMessage, partial |> Option.map failureMessage with
            | Some warning, Some partialMessage -> Some $"{warning} {partialMessage}"
            | Some warning, None -> Some warning
            | None, Some partialMessage -> Some partialMessage
            | None, None -> None

        return
            match refreshResult.Status, refreshErrorMessage refreshResult with
            | Error failure, _ -> Error(failureMessage failure)
            | Ok _, Some message -> Error message
            | Ok _, None ->
                Ok(
                    Completed(
                        UnitSuccess {
                            Refresh = refreshResult
                            PageChange = pageChange
                            SelectedChangePath = selectedChangePathOverride
                            Warning = combinedWarningMessage
                            Partial = partial
                            Published = None
                        }
                    )
                )
    }

let private completeAfterSynchronizeAsync
    (deps: GitDependencies)
    (partial: OperationFailureDto option)
    (pendingWarning: string option)
    (published: bool option)
    =
    promise {
        if partial |> Option.exists isConflictPartial then
            return! completeAfterUpdateAsync deps partial pendingWarning
        else
            let! result = refreshAfterSuccess deps partial GitPageChange.NoChange None None

            return
                result
                |> Result.map (fun outcome ->
                    match outcome with
                    | Completed(UnitSuccess success) ->
                        let finalWarning =
                            match recoveryOfPartial success.Partial with
                            | None ->
                                match pendingWarning, success.Warning with
                                | Some pending, Some partialMessage -> Some $"{pending} {partialMessage}"
                                | Some pending, None -> Some pending
                                | None, Some partialMessage -> Some partialMessage
                                | None, None -> None
                            | Some _ -> pendingWarning |> Option.orElse success.Warning

                        Completed(
                            UnitSuccess {
                                Refresh = success.Refresh
                                PageChange = success.PageChange
                                SelectedChangePath = success.SelectedChangePath
                                Warning = finalWarning
                                Partial = success.Partial
                                Published = published
                            }
                        )
                    | other -> other
                )
    }

let private acceptanceConfirmation
    (deps: GitDependencies)
    (dialog: GitSidebarConfirmationDialog)
    (action: GitPendingRemoteAction)
    (pendingWarning: string option)
    =
    promise {
        let! refreshResult = refreshAllAsync deps

        match refreshResult.Status, refreshErrorMessage refreshResult with
        | Error failure, _ -> return Error(failureMessage failure)
        | Ok _, Some message -> return Error message
        | Ok _, None ->
            return
                Ok(
                    CompletedWithPendingRemoteConfirmation(
                        UnitSuccess {
                            Refresh = refreshResult
                            PageChange = GitPageChange.NoChange
                            SelectedChangePath = None
                            Warning = pendingWarning
                            Partial = None
                            Published = None
                        },
                        dialog,
                        action
                    )
                )
    }

/// Runs one library call and hands the outcome and its partial failure on. Failures are
/// routed structurally.
let private runTrackedWriteAsync
    (deps: GitDependencies)
    (call: unit -> JS.Promise<Result<OperationResultDto<'T>, string>>)
    (onSuccess: OperationOutcomeDto<'T> -> OperationFailureDto option -> JS.Promise<Result<WriteAttemptOutcome, string>>)
    : JS.Promise<Result<WriteAttemptOutcome, string>> =
    promise {
        let! result = toResult (call ())

        match result with
        | Ok(outcome, partial) -> return! onSuccess outcome partial
        | Error failure -> return! routedToOutcome deps (routeFailure None failure)
    }

let private simpleWriteAsync
    (deps: GitDependencies)
    (call: unit -> JS.Promise<Result<OperationResultDto<'T>, string>>)
    =
    runTrackedWriteAsync deps call (fun _ partial -> refreshAfterSuccess deps partial GitPageChange.NoChange None None)

let private requireWorkspaceVersion (state: GitState) =
    match state.WorkspaceVersion with
    | Some version -> Ok version
    | None -> Error "The workspace state is not loaded yet. Refresh and try again."

let private requireSynchronization (state: GitState) =
    match state.Services with
    | Some services when not services.Synchronization ->
        Error "This workspace provider does not support online synchronization."
    | _ -> Ok()

let private synchronizeRequest
    (deps: GitDependencies)
    (version: string)
    (acceptance: GitUpdateAcceptance)
    (publish: bool)
    : SynchronizeRequestDto =
    match acceptance with
    | GitUpdateAcceptance.RequirePreview -> {
        OperationId = deps.newOperationId ()
        ExpectedWorkspaceVersion = version
        ExpectedTargetRevision = None
        AcceptUpdateRisks = false
        PublishLocalRevisions = publish
      }
    | GitUpdateAcceptance.Accepted(target, acceptedVersion) -> {
        OperationId = deps.newOperationId ()
        ExpectedWorkspaceVersion = acceptedVersion
        ExpectedTargetRevision = Some target
        AcceptUpdateRisks = true
        PublishLocalRevisions = publish
      }

let private runCloneAttemptAsync (deps: GitDependencies) (cloneRequest: CloneWorkspaceRequestDto) = promise {
    let! result =
        toResult (
            deps.cloneWorkspace {
                cloneRequest with
                    OperationId = deps.newOperationId ()
            }
        )

    match result with
    | Ok(outcome, _) -> return Ok(Completed(CloneSuccess outcome.Value))
    | Error failure ->
        let routed = routeFailure (Some cloneRequest.TargetPath) failure

        let routed =
            match routed with
            | RoutedFailure.RefreshThenReport message when not (isCanceled failure) -> RoutedFailure.Error message
            | _ -> routed

        return! routedToOutcome deps routed
}

/// After an update: opens the first conflicted item, asks to finalize an emptied
/// conflict session, or reports the refreshed state. A conflict outcome without a
/// session stops here with its failure so nothing publishes on top of it.
let private runPullAttemptAsync (deps: GitDependencies) (state: GitState) (acceptance: GitUpdateAcceptance) = promise {
    match requireSynchronization state, requireWorkspaceVersion state with
    | Error message, _
    | _, Error message -> return Error message
    | Ok(), Ok version ->
        let! result = toResult (deps.synchronize (synchronizeRequest deps version acceptance false))

        match result with
        | Ok(outcome, partial) ->
            return!
                completeAfterSynchronizeAsync
                    deps
                    partial
                    None
                    (Some(outcome.Publication = PublicationStateDto.Published))
        | Error failure ->
            match routeFailure None failure with
            | RoutedFailure.UpdateAcceptanceRequired(dialog, target) ->
                return!
                    acceptanceConfirmation
                        deps
                        dialog
                        (GitPendingRemoteAction.UpdateFromOnline(GitUpdateAcceptance.Accepted(target, version)))
                        None
            | routed -> return! routedToOutcome deps routed
}

let private runCommitAttemptAsync (deps: GitDependencies) (state: GitState) (prepared: PreparedCommitOperation) = promise {
    if String.IsNullOrWhiteSpace prepared.NormalizedMessage then
        return Error "Commit message must not be empty."
    elif prepared.PathsToCommit.Length = 0 then
        return Error "No changes available to commit."
    else
        match requireWorkspaceVersion state with
        | Error message -> return Error message
        | Ok version ->
            // One request with the exact selected paths. The library stages and commits
            // them as one revision, so nothing is staged or unstaged separately here.
            return!
                simpleWriteAsync
                    deps
                    (fun () ->
                        deps.createRevision {
                            OperationId = deps.newOperationId ()
                            Message = prepared.NormalizedMessage
                            Paths = prepared.PathsToCommit
                            ExpectedWorkspaceVersion = version
                        }
                    )
}

let private runDiscardAttemptAsync (deps: GitDependencies) (state: GitState) (paths: string[]) = promise {
    let pathsToDiscard = distinctPaths paths

    if pathsToDiscard.Length = 0 then
        return Error "No selected changes to discard."
    else
        match requireWorkspaceVersion state with
        | Error message -> return Error message
        | Ok version ->
            return!
                runTrackedWriteAsync
                    deps
                    (fun () ->
                        deps.restorePaths {
                            OperationId = deps.newOperationId ()
                            Paths = pathsToDiscard
                            ExpectedWorkspaceVersion = version
                        }
                    )
                    (fun _ partial -> refreshAfterSuccess deps partial GitPageChange.Clear (Some None) None)
}

let private pendingPrimarySaveWarning =
    "Changes were saved locally. Online sync is still pending."

/// The saved-locally outcome after a remote step failed. The snapshot is refreshed
/// first, because the failed step may have changed the workspace.
let private pendingPrimarySaveRemoteFailureAsync (deps: GitDependencies) (message: string) = promise {
    let! refreshResult = refreshAllAsync deps

    match refreshResult.Status with
    | Ok _ ->
        return
            Ok(
                CompletedWithPendingRemoteFailure(
                    UnitSuccess {
                        Refresh = refreshResult
                        PageChange = GitPageChange.NoChange
                        SelectedChangePath = None
                        Warning = Some pendingPrimarySaveWarning
                        Partial = None
                        Published = None
                    },
                    message
                )
            )
    | Error failure -> return Error(failureMessage failure)
}

/// A duplicate or otherwise refused project name comes back as a rename prompt. Every
/// other GitLab failure is an error of its own.
let private isProjectNameRefused (error: GitLabError) =
    match error with
    | GitLabError.HttpError 400
    | GitLabError.HttpError 409
    | GitLabError.InvalidRequest _ -> true
    | _ -> false

[<RequireQualifiedAccess>]
type private PublishFailure =
    | Routed of RoutedFailure
    | AcceptanceRequired of dialog: GitSidebarConfirmationDialog * observedTarget: string * workspaceVersion: string
    | ProjectNameRefused of string
    | ProvisioningIncomplete of GitProvisionedRemote * string

/// Synchronizes the workspace. Without a publication target the repository is created on the
/// DataHub of the signed-in account (or the one created earlier is reused), the workspace is
/// bound to it and the synchronize runs once more. An unreachable target is an outage and
/// never triggers provisioning.
let private runPublishAsync
    (deps: GitDependencies)
    (model: GitState)
    (version: string)
    (acceptance: GitUpdateAcceptance)
    : JS.Promise<Result<OperationOutcomeDto<SynchronizationStateDto> * OperationFailureDto option, PublishFailure>> =
    promise {
        let publishOnce version acceptance =
            toResult (deps.synchronize (synchronizeRequest deps version acceptance true))

        let projectName =
            model.CurrentArcPath |> Option.bind tryGetPathLeaf |> Option.defaultValue "ARC"

        let bindAndPublish (acceptance: GitUpdateAcceptance) (provisioned: GitProvisionedRemote) = promise {
            let! bound =
                if provisioned.IsBound then
                    promise { return Ok() }
                else
                    promise {
                        let! result =
                            toResult (
                                deps.bindWorkspace {
                                    OperationId = deps.newOperationId ()
                                    ProviderLocation = provisioned.RemoteUrl
                                    DisplayName = Some provisioned.ProjectName
                                }
                            )

                        return result |> Result.map ignore
                    }

            match bound with
            | Error failure -> return Error(PublishFailure.ProvisioningIncomplete(provisioned, failureMessage failure))
            | Ok() ->
                let boundRemote = { provisioned with IsBound = true }
                // The session was reopened by the main process, so the token is read again.
                let! statusResult = toResult (deps.getStatus (request deps))

                match statusResult with
                | Error failure ->
                    return Error(PublishFailure.ProvisioningIncomplete(boundRemote, failureMessage failure))
                | Ok(statusOutcome, _) ->
                    let! published = publishOnce statusOutcome.Value.WorkspaceVersion acceptance

                    match published with
                    | Ok(outcome, partial) -> return Ok(outcome, partial)
                    | Error failure when isCanceled failure ->
                        return Error(PublishFailure.ProvisioningIncomplete(boundRemote, failureMessage failure))
                    | Error failure ->
                        match routeFailure None failure with
                        | RoutedFailure.UpdateAcceptanceRequired(dialog, target) ->
                            return
                                Error(
                                    PublishFailure.AcceptanceRequired(
                                        dialog,
                                        target,
                                        statusOutcome.Value.WorkspaceVersion
                                    )
                                )
                        | routed -> return Error(PublishFailure.Routed routed)
        }

        let provisionAndPublish acceptance = promise {
            match model.ProvisionedRemote with
            | Some provisioned -> return! bindAndPublish acceptance provisioned
            | None ->
                let! created = deps.createRemoteProject projectName

                match created with
                | Error error when isProjectNameRefused error ->
                    return Error(PublishFailure.ProjectNameRefused error.GitLabErrorToString)
                | Error error -> return Error(PublishFailure.Routed(RoutedFailure.Error error.GitLabErrorToString))
                | Ok project ->
                    return!
                        bindAndPublish acceptance {
                            RemoteUrl = project.http_url_to_repo
                            ProjectName = projectName
                            IsBound = false
                        }
        }

        match model.ProvisionedRemote with
        | Some _ -> return! provisionAndPublish acceptance
        | None ->
            let! first = publishOnce version acceptance

            match first with
            | Ok(_, Some partial) when needsPublishTarget partial -> return! provisionAndPublish acceptance
            | Ok(outcome, partial) -> return Ok(outcome, partial)
            | Error failure when needsPublishTarget failure -> return! provisionAndPublish acceptance
            | Error failure ->
                match routeFailure None failure with
                | RoutedFailure.UpdateAcceptanceRequired(dialog, target) ->
                    return Error(PublishFailure.AcceptanceRequired(dialog, target, version))
                | routed -> return Error(PublishFailure.Routed routed)
    }

let private publishFailureToOutcome
    (onRouted: RoutedFailure -> JS.Promise<Result<WriteAttemptOutcome, string>>)
    (failure: PublishFailure)
    =
    promise {
        match failure with
        | PublishFailure.Routed routed -> return! onRouted routed
        | PublishFailure.AcceptanceRequired _ ->
            return Error "The synchronization needs a decision that this write cannot offer."
        | PublishFailure.ProjectNameRefused message -> return Ok(RequiresRemoteProjectRename message)
        | PublishFailure.ProvisioningIncomplete(provisioned, message) ->
            return Ok(ProvisioningIncomplete(provisioned, message))
    }

let private runPushAttemptAsync (deps: GitDependencies) (state: GitState) (acceptance: GitUpdateAcceptance) = promise {
    match requireSynchronization state, requireWorkspaceVersion state with
    | Error message, _
    | _, Error message -> return Error message
    | Ok(), Ok version ->
        let! result = runPublishAsync deps state version acceptance

        match result with
        | Ok(outcome, partial) ->
            return!
                completeAfterSynchronizeAsync
                    deps
                    partial
                    None
                    (Some(outcome.Publication = PublicationStateDto.Published))
        | Error(PublishFailure.AcceptanceRequired(dialog, target, workspaceVersion)) ->
            return!
                acceptanceConfirmation
                    deps
                    dialog
                    (GitPendingRemoteAction.PublishAfterUpdate(GitUpdateAcceptance.Accepted(target, workspaceVersion)))
                    None
        | Error failure -> return! publishFailureToOutcome (routedToOutcome deps) failure
}

let private runFetchAttemptAsync (deps: GitDependencies) (state: GitState) = promise {
    match requireSynchronization state with
    | Error message -> return Error message
    | Ok() -> return! simpleWriteAsync deps (fun () -> deps.refreshSynchronization (request deps))
}

/// Primary save commits the exact selection, then synchronizes the workspace. A partial
/// commit that needs reconciliation stops here with its recovery. A no-op commit still
/// continues with synchronization because the workspace may be ahead of the target.
let private runPrimarySaveAttemptAsync (deps: GitDependencies) (state: GitState) (prepared: PreparedCommitOperation) = promise {
    let! commitAttempt = runCommitAttemptAsync deps state prepared

    match commitAttempt with
    | Error message -> return Error message
    | Ok(Completed(UnitSuccess success)) when success.Partial.IsSome ->
        // The revision exists but the library asks for reconciliation first.
        let partial = success.Partial.Value

        return
            Ok(
                CompletedWithPendingRemoteFailure(
                    UnitSuccess {
                        Refresh = success.Refresh
                        PageChange = success.PageChange
                        SelectedChangePath = success.SelectedChangePath
                        Warning = Some pendingPrimarySaveWarning
                        Partial = Some partial
                        Published = None
                    },
                    failureMessage partial
                )
            )
    | Ok(Completed(UnitSuccess success)) when
        success.Partial.IsNone
        && (requireSynchronization (applyRefreshResult success.Refresh state)).IsError
        ->
        // The revision exists. Without an online service the local save is the whole outcome.
        return
            Ok(
                Completed(
                    UnitSuccess {
                        Refresh = success.Refresh
                        PageChange = success.PageChange
                        SelectedChangePath = success.SelectedChangePath
                        Warning =
                            Some
                                "Changes were saved locally. This workspace provider does not support online synchronization."
                        Partial = None
                        Published = None
                    }
                )
            )
    | Ok(Completed(UnitSuccess success)) ->
        let refreshedState = applyRefreshResult success.Refresh state

        let runPushAfterLocalCommit (version: string) = promise {
            deps.reportPhase GitBusyOperation.PushingToRemote
            let! pushResult = runPublishAsync deps refreshedState version GitUpdateAcceptance.RequirePreview

            let followsRefresh =
                match pushResult with
                | Ok _
                | Error(PublishFailure.AcceptanceRequired _) -> true
                | Error(PublishFailure.Routed(RoutedFailure.Recovery _)) -> false
                | Error(PublishFailure.Routed _) -> true
                | Error(PublishFailure.ProjectNameRefused _)
                | Error(PublishFailure.ProvisioningIncomplete _) -> false

            if followsRefresh then
                deps.reportPhase GitBusyOperation.Refreshing

            match pushResult with
            | Ok(outcome, partial) ->
                return!
                    completeAfterSynchronizeAsync
                        deps
                        partial
                        (if partial.IsSome && outcome.Publication <> PublicationStateDto.Published then
                             Some pendingPrimarySaveWarning
                         else
                             None)
                        (Some(outcome.Publication = PublicationStateDto.Published))
            | Error(PublishFailure.AcceptanceRequired(dialog, target, workspaceVersion)) ->
                let! confirmationRefreshResult = refreshAllAsync deps

                let confirmationRefreshSnapshot =
                    match confirmationRefreshResult.Status, refreshErrorMessage confirmationRefreshResult with
                    | Ok _, None -> confirmationRefreshResult
                    | _ -> success.Refresh

                return
                    Ok(
                        CompletedWithPendingRemoteConfirmation(
                            UnitSuccess {
                                Refresh = confirmationRefreshSnapshot
                                PageChange = success.PageChange
                                SelectedChangePath = success.SelectedChangePath
                                Warning = Some pendingPrimarySaveWarning
                                Partial = None
                                Published = None
                            },
                            dialog,
                            GitPendingRemoteAction.PublishAfterUpdate(
                                GitUpdateAcceptance.Accepted(target, workspaceVersion)
                            )
                        )
                    )
            | Error failure ->
                return!
                    publishFailureToOutcome
                        (fun routed -> promise {
                            match routed with
                            | RoutedFailure.UpdateAcceptanceRequired _ ->
                                // The wrapper turns every decision into PublishFailure.AcceptanceRequired.
                                return!
                                    pendingPrimarySaveRemoteFailureAsync
                                        deps
                                        "The synchronization needs a decision that this write cannot offer."
                            | RoutedFailure.Recovery(recovery, message) ->
                                return Ok(RequiresRecovery(recovery, message))
                            | RoutedFailure.ConflictSession failure ->
                                return! completeAfterUpdateAsync deps (Some failure) (Some pendingPrimarySaveWarning)
                            | RoutedFailure.Cancelled message
                            | RoutedFailure.DependencyInstall message
                            | RoutedFailure.StaleWorkspace(message, _)
                            | RoutedFailure.Error message
                            | RoutedFailure.RefreshAfterCancel message
                            | RoutedFailure.RefreshThenReport message ->
                                // The local commit already succeeded, so the saved-locally outcome stays.
                                return! pendingPrimarySaveRemoteFailureAsync deps message
                        })
                        failure
        }

        match versionOf success.Refresh with
        | None ->
            return! pendingPrimarySaveRemoteFailureAsync deps "The workspace state could not be read after saving."
        | Some version -> return! runPushAfterLocalCommit version
    | Ok(Completed(CloneSuccess _)) -> return Error "Primary save produced an invalid result."
    | Ok other -> return Ok other
}

let private runSaveLfsSettingsAttemptAsync (deps: GitDependencies) (settings: StoragePolicySettingsDto) =
    simpleWriteAsync
        deps
        (fun () ->
            deps.setStoragePolicySettings {
                OperationId = deps.newOperationId ()
                Settings = settings
            }
        )

let private runCreateBranchAttemptAsync
    (deps: GitDependencies)
    (state: GitState)
    (branchRequest: GitSidebarCreateBranchRequest)
    =
    promise {
        match requireWorkspaceVersion state with
        | Error message -> return Error message
        | Ok version ->
            let baseRef =
                match branchRequest.StartPoint with
                | None -> Ok None
                | Some startPoint ->
                    match providerRefOf state startPoint with
                    | Some providerRef -> Ok(Some providerRef)
                    | None -> Error $"Branch '{startPoint}' is not known. Refresh and try again."

            match baseRef with
            | Error message -> return Error message
            | Ok baseRef ->
                return!
                    runTrackedWriteAsync
                        deps
                        (fun () ->
                            deps.createRef {
                                OperationId = deps.newOperationId ()
                                Name = branchRequest.BranchName
                                BaseRef = baseRef
                                SwitchTo = true
                                ExpectedWorkspaceVersion = version
                            }
                        )
                        (fun _ partial -> refreshAfterSuccess deps partial GitPageChange.Clear (Some None) None)
    }

let private runSwitchBranchAttemptAsync (deps: GitDependencies) (state: GitState) (refName: string) = promise {
    match requireWorkspaceVersion state, providerRefOf state refName with
    | Error message, _ -> return Error message
    | Ok _, None -> return Error $"Branch '{refName}' is not known. Refresh and try again."
    | Ok version, Some providerRef ->
        return!
            runTrackedWriteAsync
                deps
                (fun () ->
                    deps.switchRef {
                        OperationId = deps.newOperationId ()
                        TargetRef = providerRef
                        ExpectedWorkspaceVersion = version
                    }
                )
                (fun _ partial -> refreshAfterSuccess deps partial GitPageChange.Clear (Some None) None)
}

let private runFinalizeMergeAttemptAsync (deps: GitDependencies) (state: GitState) = promise {
    match requireWorkspaceVersion state, state.ActiveConflict with
    | Error message, _ -> return Error message
    | Ok _, None -> return Error "There is no merge to finalize."
    | Ok version, Some conflict ->
        return!
            runTrackedWriteAsync
                deps
                (fun () ->
                    deps.finalizeConflict {
                        OperationId = deps.newOperationId ()
                        Handle = conflict.Handle
                        ExpectedWorkspaceVersion = version
                        Message = None
                    }
                )
                (fun _ partial -> refreshAfterSuccess deps partial GitPageChange.Clear (Some None) None)
}

let private runAbandonMergeAttemptAsync (deps: GitDependencies) (state: GitState) = promise {
    match requireWorkspaceVersion state, state.ActiveConflict with
    | Error message, _ -> return Error message
    | Ok _, None -> return Error "There is no merge to abandon."
    | Ok version, Some conflict ->
        return!
            runTrackedWriteAsync
                deps
                (fun () ->
                    deps.cancelConflict {
                        OperationId = deps.newOperationId ()
                        Handle = conflict.Handle
                        ExpectedWorkspaceVersion = version
                    }
                )
                (fun _ partial -> refreshAfterSuccess deps partial GitPageChange.Clear (Some None) None)
}

/// Removes a stale lock and refreshes. The refreshed status decides what follows: an
/// active conflict session is abandoned through the normal path.
let private runClearStaleLockAttemptAsync (deps: GitDependencies) =
    runTrackedWriteAsync
        deps
        (fun () -> deps.clearStaleLock (request deps))
        (fun outcome partial ->
            let removedLockPaths =
                outcome.Warnings
                |> Array.choose (fun warning ->
                    if warning.Code = VersionControlCodes.LockRemoved then
                        Some warning.Message
                    else
                        None
                )

            let warningMessage =
                match removedLockPaths with
                | [||] -> None
                | [| path |] -> Some $"Removed stale lock file: {path}."
                | paths ->
                    let joined = String.concat ", " paths
                    Some $"Removed stale lock files: {joined}."

            refreshAfterSuccess deps partial GitPageChange.NoChange None warningMessage
        )

let private runRestoreInterruptedPathsAttemptAsync (deps: GitDependencies) (state: GitState) (paths: string[]) = promise {
    match requireWorkspaceVersion state with
    | Error message -> return Error message
    | Ok version ->
        return!
            runTrackedWriteAsync
                deps
                (fun () ->
                    deps.restorePaths {
                        OperationId = deps.newOperationId ()
                        Paths = paths
                        ExpectedWorkspaceVersion = version
                    }
                )
                (fun _ partial -> refreshAfterSuccess deps partial GitPageChange.Clear (Some None) None)
}

/// Downloads the large objects an interrupted update left behind, one per object,
/// stopping at the first failure so its message is reported.
let private runRetryMaterializationAttemptAsync (deps: GitDependencies) = promise {
    let! objects = toResult (deps.listObjects (request deps))

    match objects with
    | Error failure -> return! routedToOutcome deps (routeFailure None failure)
    | Ok(outcome, _) ->
        let pending =
            outcome.Value
            |> Array.filter (fun objectState -> not objectState.IsMaterialized)

        let mutable failure: OperationFailureDto option = None

        for index, objectState in pending |> Array.indexed do
            if failure.IsNone then
                let! materialized =
                    toResult (
                        deps.materializeObject {
                            OperationId = deps.newOperationId ()
                            Path = objectState.Path
                            RefreshTree = Some(index = pending.Length - 1)
                        }
                    )

                match materialized with
                | Ok _ -> ()
                | Error error -> failure <- Some error

        match failure with
        | Some error -> return! routedToOutcome deps (routeFailure None error)
        | None -> return! refreshAfterSuccess deps None GitPageChange.NoChange None None
}

let private executeWriteAttemptOnce (deps: GitDependencies) (state: GitState) (writeRequest: WriteRequest) = promise {
    match writeRequest with
    | Fetch -> return! runFetchAttemptAsync deps state
    | Pull acceptance -> return! runPullAttemptAsync deps state acceptance
    | Push acceptance -> return! runPushAttemptAsync deps state acceptance
    | PrimarySave prepared -> return! runPrimarySaveAttemptAsync deps state prepared
    | Clone(cloneRequest, _) -> return! runCloneAttemptAsync deps cloneRequest
    | CommitSelection prepared -> return! runCommitAttemptAsync deps state prepared
    | CommitAll prepared -> return! runCommitAttemptAsync deps state prepared
    | DiscardSelection paths -> return! runDiscardAttemptAsync deps state paths
    | SaveLfsSettings(_, settings) -> return! runSaveLfsSettingsAttemptAsync deps settings
    | PruneLfsCache -> return! simpleWriteAsync deps (fun () -> deps.pruneStorage (request deps))
    | DedupLfsStorage -> return! simpleWriteAsync deps (fun () -> deps.deduplicateStorage (request deps))
    | CreateBranch branchRequest -> return! runCreateBranchAttemptAsync deps state branchRequest
    | SwitchBranch refName -> return! runSwitchBranchAttemptAsync deps state refName
    | FinalizeMerge -> return! runFinalizeMergeAttemptAsync deps state
    | AbandonMerge -> return! runAbandonMergeAttemptAsync deps state
    | ClearStaleLock -> return! runClearStaleLockAttemptAsync deps
    | RestoreInterruptedPaths paths -> return! runRestoreInterruptedPathsAttemptAsync deps state paths
    | RetryMaterialization -> return! runRetryMaterializationAttemptAsync deps
}

/// Writes that change the working tree from a state the user reviewed are not replayed
/// against a workspace that moved in between. A discard or a restore would drop content
/// the user has not seen. An update would run without its preview. An accepted synchronize is
/// pinned to the target and the token the user saw, so a stale token means the decision no
/// longer applies and a fresh preview is needed. Finalizing or
/// abandoning a merge would act on whichever conflict session the refresh found, which
/// is not the one the user reviewed.
/// A stale branch switch must run its preflight again before retrying the switch.
let private replaysAfterStaleToken =
    function
    | DiscardSelection _
    | RestoreInterruptedPaths _
    | Pull _
    | Push(GitUpdateAcceptance.Accepted _)
    | SwitchBranch _
    | FinalizeMerge
    | AbandonMerge -> false
    | _ -> true

let private staleWithoutReplayMessage =
    "The workspace changed since it was last refreshed, so the action was not repeated. Review the current changes and try again."

/// A stale workspace token that left the workspace unchanged is the normal outcome
/// when the workspace moved between the refresh and the click. The state is refreshed
/// and the write runs once more against the current token, unless replaying it could
/// discard content the user has not reviewed.
let private executeWriteAttempt (deps: GitDependencies) (state: GitState) (writeRequest: WriteRequest) = promise {
    let! first = executeWriteAttemptOnce deps state writeRequest

    match first with
    | Ok(StaleWorkspaceVersion(message, targetMoved)) when not (replaysAfterStaleToken writeRequest) ->
        let reported = if targetMoved then message else staleWithoutReplayMessage

        return Ok(StaleWorkspaceVersion(reported, targetMoved))
    | Ok(StaleWorkspaceVersion(_, _)) ->
        let! refreshResult = refreshAllAsync deps

        match refreshResult.Status, refreshErrorMessage refreshResult with
        | Error failure, _ -> return Error(failureMessage failure)
        | Ok _, Some message -> return Error message
        | Ok _, None ->
            let! second = executeWriteAttemptOnce deps (applyRefreshResult refreshResult state) writeRequest

            match second with
            | Ok(StaleWorkspaceVersion(message, _)) -> return Error message
            | other -> return other
    | other -> return other
}

/// Resolves one conflicted file with the content the user reviewed, against the handle
/// and token captured with the page. When nothing remains, the merge is finalized.
let private confirmMergeResolutionAsync (deps: GitDependencies) (resolution: GitMergeResolutionRequest) = promise {
    let! resolved =
        toResult (
            deps.resolveConflict {
                OperationId = deps.newOperationId ()
                Handle = resolution.Handle
                ExpectedWorkspaceVersion = resolution.WorkspaceVersion
                Path = resolution.Path
                Resolution = ConflictResolutionDto.SupplyResolvedContent resolution.ResolvedContent
            }
        )

    let staleOrFailed (failure: OperationFailureDto) =
        if
            failure.Category = FailureCategoryDto.Concurrency
            || recoveryCode failure = Some VersionControlCodes.Recovery.RefreshConflictSession
        then
            ConfirmMergeResolutionError.Stale(failureMessage failure)
        else
            ConfirmMergeResolutionError.Failed(failureMessage failure)

    match resolved with
    | Error failure -> return Error(staleOrFailed failure)
    | Ok(outcome, _) ->
        let! statusResult = toResult (deps.getStatus (request deps))

        match statusResult with
        | Error failure -> return Error(ConfirmMergeResolutionError.Failed(failureMessage failure))
        | Ok(statusOutcome, _) ->
            let status = statusOutcome.Value

            if outcome.Value.RemainingItems.Length = 0 then
                let! finalized =
                    toResult (
                        deps.finalizeConflict {
                            OperationId = deps.newOperationId ()
                            Handle = outcome.Value.RefreshedHandle
                            ExpectedWorkspaceVersion = status.WorkspaceVersion
                            Message = None
                        }
                    )

                match finalized with
                | Error failure -> return Error(staleOrFailed failure)
                | Ok _ ->
                    let! afterFinalize = toResult (deps.getStatus (request deps))

                    match afterFinalize with
                    | Error failure -> return Error(ConfirmMergeResolutionError.Failed(failureMessage failure))
                    | Ok(finalStatus, _) ->
                        return
                            Ok {
                                UpdatedStatus = finalStatus.Value
                                NextConflictedPath = None
                                PageChange = GitPageChange.Clear
                                Finalized = true
                            }
            else
                let nextPath = outcome.Value.RemainingItems.[0].Path

                let refreshedSession: ConflictSessionSummaryDto = {
                    Handle = outcome.Value.RefreshedHandle
                    Items = outcome.Value.RemainingItems
                }

                let! pageResult = deps.loadConflictPage refreshedSession status.WorkspaceVersion nextPath

                return
                    pageResult
                    |> Result.map (fun page -> {
                        UpdatedStatus = {
                            status with
                                ActiveConflictSession = Some refreshedSession
                        }
                        NextConflictedPath = Some nextPath
                        PageChange = GitPageChange.Set page
                        Finalized = false
                    })
                    |> Result.mapError ConfirmMergeResolutionError.Failed
}

let init () : GitState * Cmd<Msg> = GitState.Empty, Cmd.none

let private missingRepositoryModel (model: GitState) = {
    GitState.Empty with
        CurrentArcPath = model.CurrentArcPath
        ArcSessionId = model.ArcSessionId
        RefreshRequestId = model.RefreshRequestId
        PageLoadRequestId = model.PageLoadRequestId
        WriteRequestId = model.WriteRequestId
        RepositoryAvailability = GitRepositoryAvailability.MissingRepository
}

let private clearBusy (model: GitState) = {
    model with
        BusyOperation = None
        BusyNotice = None
        CurrentProgress = None
        CurrentOperation = None
}

let private writeErrorModel (message: string) (model: GitState) = {
    clearBusy model with
        ErrorNotice = Some message
        WarningNotice = None
}

let private busyOperationForWriteRequest =
    function
    | Fetch -> GitBusyOperation.FetchingFromRemote
    | Pull _ -> GitBusyOperation.PullingFromRemote
    | Push _ -> GitBusyOperation.PushingToRemote
    | PrimarySave prepared -> prepared.BusyOperation
    | Clone(cloneRequest, _) -> GitBusyOperation.CloningRepository cloneRequest.TargetPath
    | CommitSelection prepared -> prepared.BusyOperation
    | CommitAll prepared -> prepared.BusyOperation
    | DiscardSelection _ -> GitBusyOperation.DiscardingSelectedChanges
    | SaveLfsSettings(busyOperation, _) -> busyOperation
    | PruneLfsCache -> GitBusyOperation.PruningGitLfsCache
    | DedupLfsStorage -> GitBusyOperation.DeduplicatingGitLfsStorage
    | CreateBranch _ -> GitBusyOperation.CreatingBranch
    | SwitchBranch _ -> GitBusyOperation.SwitchingBranch
    | FinalizeMerge -> GitBusyOperation.FinalizingMerge
    | AbandonMerge -> GitBusyOperation.AbandoningMerge
    | ClearStaleLock -> GitBusyOperation.ClearingStaleLock
    | RestoreInterruptedPaths _ -> GitBusyOperation.RestoringInterruptedPaths
    | RetryMaterialization -> GitBusyOperation.RetryingMaterialization

let private requiresArcForWriteRequest =
    function
    | Clone _ -> false
    | _ -> true

let private resolveCloneReplyCmd writeRequest result =
    match writeRequest, result with
    | Clone(_, reply), Ok(CloneSuccess path) -> resolveReplyCmd reply (Ok path)
    | Clone(_, reply), Error message -> resolveReplyCmd reply (Error message)
    | Clone(_, reply), Ok _ -> resolveReplyCmd reply (Error "Clone request produced an invalid result.")
    | _ -> Cmd.none

let private resolveStaleWriteCompletedCmd writeRequest result =
    match result with
    | Ok(Completed success) -> resolveCloneReplyCmd writeRequest (Ok success)
    | Ok(CompletedWithPendingRemoteConfirmation(success, _, _)) -> resolveCloneReplyCmd writeRequest (Ok success)
    | Ok(CompletedWithPendingRemoteFailure(success, _)) -> resolveCloneReplyCmd writeRequest (Ok success)
    | Ok(RequiresDependencyInstall _) -> resolveCloneReplyCmd writeRequest (Error staleArcSessionMessage)
    | Ok(RequiresRemoteProjectRename _) -> resolveCloneReplyCmd writeRequest (Error staleArcSessionMessage)
    | Ok(RequiresRecovery(_, message)) -> resolveCloneReplyCmd writeRequest (Error message)
    | Ok(ProvisioningIncomplete(_, message)) -> resolveCloneReplyCmd writeRequest (Error message)
    | Ok(OperationCancelled message) -> resolveCloneReplyCmd writeRequest (Error message)
    | Ok(StaleWorkspaceVersion(message, _)) -> resolveCloneReplyCmd writeRequest (Error message)
    | Error message -> resolveCloneReplyCmd writeRequest (Error message)

/// The first library call of a write uses the id allocated when the write was
/// requested, so a cancel that lands before the started event still reaches it.
let private withFirstOperationId (deps: GitDependencies) (operationId: string) =
    let handedOut = ref false
    let nextSuffix = ref 1

    {
        deps with
            newOperationId =
                fun () ->
                    if handedOut.Value then
                        let suffix = nextSuffix.Value
                        nextSuffix.Value <- suffix + 1
                        $"{operationId}/{suffix}"
                    else
                        handedOut.Value <- true
                        operationId
    }

let private operationRootId (operationId: string) =
    let separatorIndex = operationId.IndexOf "/"

    if separatorIndex < 0 then
        operationId
    else
        operationId.Substring(0, separatorIndex)

let private writeCmd
    (deps: GitDependencies)
    (model: GitState)
    (writeRequest: WriteRequest)
    (sessionId: int)
    (writeRequestId: int)
    (operationId: string)
    =
    Cmd.ofEffect (fun dispatch ->
        promise {
            try
                let writeDeps = withFirstOperationId deps operationId

                let writeDeps = {
                    writeDeps with
                        reportPhase = fun phase -> dispatch (WritePhaseChanged(sessionId, writeRequestId, phase))
                }

                let! result = executeWriteAttempt writeDeps model writeRequest

                dispatch (WriteCompleted(sessionId, writeRequestId, writeRequest, result))
            with err ->
                dispatch (WriteCompleted(sessionId, writeRequestId, writeRequest, Error(string err)))
        }
        |> Promise.start
    )

/// Whether the post-merge publish should run now: the primary save asked for it and the
/// workspace has no open conflict session anymore.
let private shouldRunPostMergePush (model: GitState) =
    model.PendingPostMergePush && model.ActiveConflict.IsNone

let private updateCore
    (deps: GitDependencies)
    (setPageState: PageState option -> unit)
    (msg: Msg)
    (model: GitState)
    : GitState * Cmd<Msg> =
    match msg with
    | ResetWorkflow -> GitState.Empty, Cmd.none
    | SetCurrentProgress(Some progress) when model.BusyOperation.IsSome ->
        {
            model with
                CurrentProgress = Some(mergeProgressUpdate model progress)
        },
        Cmd.none
    | SetCurrentProgress _ -> { model with CurrentProgress = None }, Cmd.none
    | OperationStarted key when model.BusyOperation.IsSome ->
        match model.CurrentOperation with
        | Some currentOperation ->
            let rootOperationId = operationRootId currentOperation.OperationId

            let belongsToWrite =
                key.OperationId = rootOperationId
                || key.OperationId.StartsWith(rootOperationId + "/", StringComparison.Ordinal)

            if belongsToWrite then
                {
                    model with
                        CurrentOperation = Some key
                },
                Cmd.none
            else
                model, Cmd.none
        | None -> model, Cmd.none
    | OperationStarted _ -> model, Cmd.none
    | ArcPathChanged arcPath when arcPath = model.CurrentArcPath -> model, Cmd.none
    | ArcPathChanged arcPath ->
        let pendingPublishAfterRefresh =
            match model.PendingPublishForPath, arcPath with
            | Some pendingPath, Some nextPath -> Swate.Components.Shared.PathHelpers.pathsEqual pendingPath nextPath
            | _ -> false

        let nextModel = {
            GitState.Empty with
                CurrentArcPath = arcPath
                ArcSessionId = nextArcSessionId model
                // The request counters keep counting across ARCs. Resetting them let a
                // refresh or a page load of the previous ARC match an id of the new one.
                RefreshRequestId = nextRefreshRequestId model
                PageLoadRequestId = nextPageLoadRequestId model
                PendingPublishAfterRefresh = pendingPublishAfterRefresh
                PendingPublishForPath = None
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
    | GitRepositoryInitialized arcPath ->
        match model.CurrentArcPath with
        | Some currentArcPath when Swate.Components.Shared.PathHelpers.pathsEqual currentArcPath arcPath ->
            model, Cmd.ofMsg RefreshRequested
        | _ -> model, Cmd.none
    | RefreshRequested when model.CurrentArcPath.IsNone ->
        {
            GitState.Empty with
                CurrentArcPath = model.CurrentArcPath
                ArcSessionId = model.ArcSessionId
        },
        Cmd.none
    | RefreshRequested when
        model.BusyOperation.IsSome
        && not (
            model.BusyOperation = Some GitBusyOperation.Refreshing
            && model.CurrentOperation.IsNone
        )
        ->
        // A write may report Refreshing while it still owns CurrentOperation. Queue the
        // refresh until the write completes so it does not clear the write state.
        { model with RefreshPending = true }, Cmd.none
    | RefreshRequested ->
        let requestId = nextRefreshRequestId model

        let nextModel =
            model
            |> withBusyOperation (Some GitBusyOperation.Refreshing)
            |> startRefreshRequest requestId
            |> fun state -> {
                state with
                    ErrorNotice = None
                    WarningNotice =
                        match state.PendingRefreshWarningNotice with
                        | Some _ -> state.WarningNotice
                        | None -> None
            }

        let cmd =
            Cmd.OfPromise.either
                refreshAllAsync
                deps
                (fun refreshResult -> RefreshCompleted(requestId, Ok refreshResult))
                (fun err -> RefreshCompleted(requestId, Error(string err)))

        nextModel, cmd
    | RefreshCompleted(requestId, _) when requestId <> model.RefreshRequestId -> model, Cmd.none
    | RefreshCompleted(_, Error message) ->
        let nextModel = {
            clearBusy model with
                RefreshState = GitRefreshState.Idle
                ErrorNotice = Some message
                WarningNotice = None
                PendingRefreshWarningNotice = None
        }

        nextModel,
        Cmd.batch [
            applyPageChangeCmd setPageState GitPageChange.Clear
            reportErrorCmd deps "Could not refresh Git state" message
        ]
    | RefreshCompleted(_, Ok refreshResult) when refreshFailure refreshResult |> Option.exists isMissingRepository ->
        missingRepositoryModel model, applyPageChangeCmd setPageState GitPageChange.Clear
    | RefreshCompleted(_, Ok refreshResult) ->
        let nextModel = model |> applyRefreshResult refreshResult |> clearBusy

        let cmd =
            match refreshErrorMessage refreshResult with
            | Some message ->
                Cmd.batch [
                    applyPageChangeCmd setPageState GitPageChange.Clear
                    reportErrorCmd deps "Could not refresh Git state" message
                ]
            | None when nextModel.PendingPublishAfterRefresh ->
                Cmd.ofMsg (WriteRequested(Push GitUpdateAcceptance.RequirePreview))
            | None -> Cmd.none

        {
            nextModel with
                PendingPublishAfterRefresh = false
        },
        cmd
    | InitRepositoryRequested when model.CurrentArcPath.IsNone -> model, Cmd.none
    | InitRepositoryRequested ->
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
                (fun (deps, arcPath) -> runInitRepositoryAsync deps arcPath)
                (deps, Option.get model.CurrentArcPath)
                (fun result -> InitRepositoryCompleted(model.ArcSessionId, result))
                (fun err -> InitRepositoryCompleted(model.ArcSessionId, Error(string err)))

        nextModel, cmd
    | InitRepositoryCompleted(sessionId, _) when sessionId <> model.ArcSessionId -> model, Cmd.none
    | InitRepositoryCompleted(_, Error message) ->
        let nextModel = {
            clearBusy model with
                ErrorNotice = Some message
                WarningNotice = None
                PendingRefreshWarningNotice = None
        }

        nextModel, reportErrorCmd deps "Could not initialize Git repository" message
    | InitRepositoryCompleted(_, Ok()) ->
        let nextModel = {
            clearBusy model with
                RepositoryAvailability = GitRepositoryAvailability.Ready
                ErrorNotice = None
                WarningNotice = None
                PendingRefreshWarningNotice = None
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
                    loadPageAsync deps model.ActiveConflict model.WorkspaceVersion change
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

        nextModel,
        Cmd.batch [
            resolveReplyCmd reply (Error message)
            reportErrorCmd deps "Could not open Git change" message
        ]
    | ConfirmMergeResolutionRequested _ when model.CurrentArcPath.IsNone -> model, Cmd.none
    | ConfirmMergeResolutionRequested resolution ->
        match model.BusyOperation with
        | Some _ -> model, Cmd.none
        | None when model.MergeResolutionPendingPath = Some resolution.Path -> model, Cmd.none
        | None ->
            let nextModel =
                model
                |> withBusyOperation (Some(GitBusyOperation.ConfirmingMergeResolution resolution.Path))
                |> fun state -> {
                    state with
                        MergeResolutionPendingPath = Some resolution.Path
                        ErrorNotice = None
                        WarningNotice = None
                }

            let cmd =
                Cmd.OfPromise.either
                    (fun (deps, resolution) -> confirmMergeResolutionAsync deps resolution)
                    (deps, resolution)
                    (fun result -> ConfirmMergeResolutionCompleted(model.ArcSessionId, result))
                    (fun err ->
                        ConfirmMergeResolutionCompleted(
                            model.ArcSessionId,
                            Error(ConfirmMergeResolutionError.Failed(string err))
                        )
                    )

            nextModel, cmd
    | ConfirmMergeResolutionCompleted(sessionId, _) when sessionId <> model.ArcSessionId -> model, Cmd.none
    | ConfirmMergeResolutionCompleted(_, Error(ConfirmMergeResolutionError.Stale message)) ->
        // The handle or the workspace changed underneath the resolution. Reload both
        // and let the user redo the choice deliberately.
        let nextModel = {
            clearBusy model with
                MergeResolutionPendingPath = None
                SelectedChangePath = None
                ErrorNotice = Some message
                WarningNotice = None
        }

        nextModel,
        Cmd.batch [
            applyPageChangeCmd setPageState GitPageChange.Clear
            Cmd.ofMsg RefreshRequested
            reportErrorCmd deps "Could not confirm merge resolution" message
        ]
    | ConfirmMergeResolutionCompleted(_, Error(ConfirmMergeResolutionError.Failed message)) ->
        let nextModel = {
            clearBusy model with
                MergeResolutionPendingPath = None
                ErrorNotice = Some message
                WarningNotice = None
        }

        // Resolve may have written the file before failing, so the state is reloaded.
        nextModel,
        Cmd.batch [
            Cmd.ofMsg RefreshRequested
            reportErrorCmd deps "Could not confirm merge resolution" message
        ]
    | ConfirmMergeResolutionCompleted(_, Ok outcome) ->
        let nextModel =
            model
            |> applyStatus outcome.UpdatedStatus
            |> clearBusy
            |> fun state -> {
                state with
                    MergeResolutionPendingPath = None
                    SelectedChangePath = outcome.NextConflictedPath
                    ErrorNotice = None
            }

        if outcome.Finalized && shouldRunPostMergePush nextModel then
            {
                nextModel with
                    PendingPostMergePush = false
            },
            Cmd.batch [
                applyPageChangeCmd setPageState outcome.PageChange
                Cmd.ofMsg (WriteRequested(Push GitUpdateAcceptance.RequirePreview))
            ]
        else
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
    | PullRequested -> model, Cmd.ofMsg (WriteRequested(Pull GitUpdateAcceptance.RequirePreview))
    | PushRequested -> model, Cmd.ofMsg (WriteRequested(Push GitUpdateAcceptance.RequirePreview))
    | CancelCurrentOperationRequested ->
        match model.CurrentOperation with
        | None -> model, Cmd.none
        | Some key ->
            let cmd =
                Cmd.OfPromise.either
                    deps.cancelOperation
                    key
                    (fun result -> CancelCurrentOperationCompleted(model.ArcSessionId, key, result))
                    (fun err -> CancelCurrentOperationCompleted(model.ArcSessionId, key, Error(string err)))

            model, cmd
    | CancelCurrentOperationCompleted(sessionId, key, _) when
        sessionId <> model.ArcSessionId
        || (model.CurrentOperation
            |> Option.map (fun current -> operationRootId current.OperationId))
           <> Some(operationRootId key.OperationId)
        ->
        model, Cmd.none
    | CancelCurrentOperationCompleted(_, _, Error message) ->
        {
            model with
                ErrorNotice = Some message
                WarningNotice = None
        },
        reportErrorCmd deps "Could not cancel Git operation" message
    | CancelCurrentOperationCompleted(_, _, Ok true) -> model, Cmd.none
    | CancelCurrentOperationCompleted(_, _, Ok false) -> model, Cmd.none
    | UpdateFromOnlineRequested when model.CurrentArcPath.IsNone || model.BusyOperation.IsSome -> model, Cmd.none
    | UpdateFromOnlineRequested -> model, Cmd.ofMsg (WriteRequested(Pull GitUpdateAcceptance.RequirePreview))
    | CloneRequested(cloneRequest, reply) -> model, Cmd.ofMsg (WriteRequested(Clone(cloneRequest, reply)))
    | PrimarySaveSelectionRequested selection ->
        model, Cmd.ofMsg (WriteRequested(PrimarySave(prepareCommitSelection selection)))
    | PrimarySaveAllRequested message -> model, Cmd.ofMsg (WriteRequested(PrimarySave(prepareCommitAll model message)))
    | CommitSelectionRequested selection ->
        model, Cmd.ofMsg (WriteRequested(CommitSelection(prepareCommitSelection selection)))
    | CommitAllRequested message -> model, Cmd.ofMsg (WriteRequested(CommitAll(prepareCommitAll model message)))
    | DiscardSelectionRequested paths -> model, Cmd.ofMsg (WriteRequested(DiscardSelection paths))
    | ConfirmPendingRemoteActionRequested ->
        match model.PendingRemoteAction with
        | GitPendingRemoteAction.UpdateFromOnline acceptance ->
            {
                model with
                    PendingConfirmation = None
                    PendingRemoteAction = GitPendingRemoteAction.None
            },
            Cmd.ofMsg (WriteRequested(Pull acceptance))
        | GitPendingRemoteAction.PublishAfterUpdate acceptance ->
            {
                model with
                    PendingConfirmation = None
                    PendingRemoteAction = GitPendingRemoteAction.None
                    PendingPostMergePush = true
            },
            Cmd.ofMsg (WriteRequested(Push acceptance))
        | GitPendingRemoteAction.FinalizeMerge ->
            {
                model with
                    PendingConfirmation = None
                    PendingRemoteAction = GitPendingRemoteAction.None
            },
            Cmd.ofMsg (WriteRequested FinalizeMerge)
        | GitPendingRemoteAction.Recover ->
            let followUp =
                match model.PendingRecovery with
                | Some(GitPendingRecovery.RestoreInterruptedPaths _) -> Cmd.ofMsg RestoreInterruptedPathsRequested
                | Some(GitPendingRecovery.RetryMaterialization _) -> Cmd.ofMsg RetryMaterializationRequested
                | Some(GitPendingRecovery.RetryPublish _) ->
                    Cmd.ofMsg (WriteRequested(Push GitUpdateAcceptance.RequirePreview))
                | Some(GitPendingRecovery.ClearStaleLock _) -> Cmd.ofMsg ClearStaleLockRequested
                | Some(GitPendingRecovery.ClearCloneTarget _)
                | None -> Cmd.ofMsg DismissRecoveryRequested

            {
                model with
                    PendingConfirmation = None
                    PendingRemoteAction = GitPendingRemoteAction.None
                    PendingRecovery =
                        match model.PendingRecovery with
                        | Some(GitPendingRecovery.RetryPublish _) -> None
                        | _ -> model.PendingRecovery
            },
            followUp
        | GitPendingRemoteAction.None -> model, Cmd.none
    | CancelPendingRemoteActionRequested ->
        let cmd =
            match model.PendingRemoteAction with
            | GitPendingRemoteAction.FinalizeMerge -> Cmd.ofMsg (WriteRequested AbandonMerge)
            | GitPendingRemoteAction.Recover -> Cmd.ofMsg DismissRecoveryRequested
            | _ -> Cmd.none

        {
            model with
                PendingConfirmation = None
                PendingRemoteAction = GitPendingRemoteAction.None
                PendingPostMergePush =
                    match model.PendingRemoteAction with
                    | GitPendingRemoteAction.Recover -> model.PendingPostMergePush
                    | _ -> false
        },
        cmd
    | CancelPublishRenameRequested ->
        {
            clearBusy model with
                PendingPublishRename = None
                ErrorNotice = None
        },
        Cmd.none
    | SubmitPublishRenameRequested _ when model.PendingPublishRename.IsNone -> model, Cmd.none
    | SubmitPublishRenameRequested newName ->
        let normalizedName =
            newName
            |> Option.ofObj
            |> Option.map _.Trim()
            |> Option.defaultValue String.Empty

        if String.IsNullOrWhiteSpace normalizedName then
            {
                model with
                    ErrorNotice = Some "ARC folder name must not be empty."
            },
            reportErrorCmd deps "Could not rename ARC" "ARC folder name must not be empty."
        else
            let nextModel =
                model
                |> withBusyOperation (Some GitBusyOperation.RenamingRepository)
                |> fun state -> { state with ErrorNotice = None }

            let cmd =
                Cmd.OfPromise.either
                    deps.renameOpenArcRoot
                    normalizedName
                    (fun result -> PublishRenameCompleted(model.ArcSessionId, result))
                    (fun err -> PublishRenameCompleted(model.ArcSessionId, Error(string err)))

            nextModel, cmd
    | PublishRenameCompleted(sessionId, Ok renamedPath) when
        sessionId = model.ArcSessionId
        && not (
            model.CurrentArcPath
            |> Option.exists (fun currentPath -> Swate.Components.Shared.PathHelpers.pathsEqual currentPath renamedPath)
        )
        ->
        {
            clearBusy model with
                PendingPublishRename = None
                PendingPublishForPath = Some renamedPath
                ErrorNotice = None
        },
        Cmd.none
    | PublishRenameCompleted(sessionId, Ok renamedPath) when
        sessionId <> model.ArcSessionId
        && (model.CurrentArcPath
            |> Option.exists (fun currentPath -> Swate.Components.Shared.PathHelpers.pathsEqual currentPath renamedPath))
        ->
        // The path change already reset the model. Its refresh may still be running or
        // may have finished before this reply, so a refresh is asked for either way and
        // the publish runs when it has loaded the workspace token.
        {
            model with
                PendingPublishRename = None
                ErrorNotice = None
                PendingPublishAfterRefresh = true
        },
        Cmd.ofMsg RefreshRequested
    | PublishRenameCompleted(sessionId, _) when sessionId <> model.ArcSessionId -> model, Cmd.none
    | PublishRenameCompleted(_, Error message) ->
        {
            clearBusy model with
                ErrorNotice = Some message
        },
        reportErrorCmd deps "Could not rename ARC" message
    | PublishRenameCompleted(_, Ok _) ->
        {
            clearBusy model with
                PendingPublishRename = None
                ErrorNotice = None
        },
        Cmd.ofMsg (WriteRequested(Push GitUpdateAcceptance.RequirePreview))
    | CreateBranchRequested branchRequest -> model, Cmd.ofMsg (WriteRequested(CreateBranch branchRequest))
    | SwitchBranchRequested _ when model.CurrentArcPath.IsNone || model.BusyOperation.IsSome -> model, Cmd.none
    | SwitchBranchRequested branchName ->
        let normalizedBranchName = branchName.Trim()

        if String.IsNullOrWhiteSpace normalizedBranchName then
            model, Cmd.none
        else
            match requireWorkspaceVersion model, providerRefOf model normalizedBranchName with
            | Error message, _ ->
                {
                    model with
                        ErrorNotice = Some message
                },
                reportErrorCmd deps "Could not switch branch" message
            | Ok _, None ->
                let message =
                    $"Branch '{normalizedBranchName}' is not known. Refresh and try again."

                {
                    model with
                        ErrorNotice = Some message
                },
                reportErrorCmd deps "Could not switch branch" message
            | Ok version, Some providerRef ->
                let operationId = deps.newOperationId ()

                let nextModel =
                    model
                    |> withBusyOperation (Some GitBusyOperation.SwitchingBranch)
                    |> fun state -> {
                        state with
                            ErrorNotice = None
                            WarningNotice = None
                            CurrentOperation =
                                Some {
                                    SessionId = ""
                                    OperationId = operationId
                                }
                    }

                let cmd =
                    Cmd.OfPromise.either
                        deps.preflightSwitchRef
                        {
                            OperationId = operationId
                            TargetRef = providerRef
                            ExpectedWorkspaceVersion = version
                        }
                        (fun result -> SwitchBranchPreflightCompleted(model.ArcSessionId, normalizedBranchName, result))
                        (fun err ->
                            SwitchBranchPreflightCompleted(model.ArcSessionId, normalizedBranchName, Error(string err))
                        )

                nextModel, cmd
    | SwitchBranchPreflightCompleted(sessionId, _, _) when sessionId <> model.ArcSessionId -> model, Cmd.none
    | SwitchBranchPreflightCompleted(_, _, Ok(OperationResultDto.PartiallySucceeded(_, failure))) ->
        let message = failureMessage failure

        {
            clearBusy model with
                ErrorNotice = Some message
                WarningNotice = None
        },
        reportErrorCmd deps "Could not switch branch" message
    | SwitchBranchPreflightCompleted(_, refName, Ok(OperationResultDto.Succeeded outcome)) when outcome.Value.IsSafe ->
        clearBusy model, Cmd.ofMsg (WriteRequested(SwitchBranch refName))
    | SwitchBranchPreflightCompleted(_, _, Ok(OperationResultDto.Succeeded outcome)) ->
        let paths = String.Join(", ", outcome.Value.PathsAtRisk)

        let message =
            if outcome.Value.PathsAtRisk.Length > 0 then
                $"Switching branches would overwrite local changes in: {paths}. Save or discard those files, then switch."
            else
                "Switching branches would overwrite local changes. Save or discard those files, then switch."

        {
            clearBusy model with
                ErrorNotice = Some message
                WarningNotice = None
        },
        reportErrorCmd deps "Could not switch branch" message
    | SwitchBranchPreflightCompleted(_, _, Ok(OperationResultDto.Failed failure)) when isCanceled failure ->
        {
            clearBusy model with
                ErrorNotice = None
                WarningNotice = Some "Git operation cancelled."
        },
        Cmd.none
    | SwitchBranchPreflightCompleted(_, _, Ok(OperationResultDto.Failed failure)) ->
        let message = failureMessage failure

        {
            clearBusy model with
                ErrorNotice = Some message
                WarningNotice = None
        },
        reportErrorCmd deps "Could not switch branch" message
    | SwitchBranchPreflightCompleted(_, _, Error message) ->
        {
            clearBusy model with
                ErrorNotice = Some message
                WarningNotice = None
        },
        reportErrorCmd deps "Could not switch branch" message
    | PruneLfsCacheRequested ->
        let message =
            "This cleans hidden Git LFS cache files for the current ARC. Files that are still needed can be downloaded again from the remote. Continue?"

        if deps.confirmLfsPrune message then
            model, Cmd.ofMsg (WriteRequested PruneLfsCache)
        else
            model, Cmd.none
    | DedupLfsStorageRequested -> model, Cmd.ofMsg (WriteRequested DedupLfsStorage)
    | ClearStaleLockRequested -> { model with PendingRecovery = None }, Cmd.ofMsg (WriteRequested ClearStaleLock)
    | RestoreInterruptedPathsRequested ->
        match model.PendingRecovery with
        | Some(GitPendingRecovery.RestoreInterruptedPaths interrupted) when interrupted.AffectedPaths.Length > 0 ->
            { model with PendingRecovery = None },
            Cmd.ofMsg (WriteRequested(RestoreInterruptedPaths interrupted.AffectedPaths))
        | _ -> { model with PendingRecovery = None }, Cmd.none
    | RetryMaterializationRequested ->
        { model with PendingRecovery = None }, Cmd.ofMsg (WriteRequested RetryMaterialization)
    | DismissRecoveryRequested ->
        // The offer is gone, so the sidebar catches up with what the canceled step left.
        let followUp =
            if shouldRunPostMergePush model then
                Cmd.ofMsg (WriteRequested(Push GitUpdateAcceptance.RequirePreview))
            elif model.CurrentArcPath.IsSome then
                Cmd.ofMsg RefreshRequested
            else
                Cmd.none

        {
            model with
                PendingRecovery = None
                RefreshPending = false
                PendingPostMergePush =
                    if shouldRunPostMergePush model then
                        false
                    else
                        model.PendingPostMergePush
        },
        followUp
    | WriteRequested writeRequest when requiresArcForWriteRequest writeRequest && model.CurrentArcPath.IsNone ->
        model, Cmd.none
    | WriteRequested writeRequest when model.BusyOperation.IsSome ->
        // One mutation at a time. The sidebar disables its buttons while busy, so this
        // only catches a request that raced the completion.
        model, resolveCloneReplyCmd writeRequest (Error "Another Git operation is still running.")
    | WriteRequested writeRequest ->
        let writeRequestId = model.WriteRequestId + 1
        let operationId = deps.newOperationId ()

        let nextModel =
            model
            |> withBusyOperation (Some(busyOperationForWriteRequest writeRequest))
            |> fun state -> {
                state with
                    ErrorNotice = None
                    WarningNotice = None
                    WriteRequestId = writeRequestId
                    CurrentOperation =
                        Some {
                            SessionId = ""
                            OperationId = operationId
                        }
            }

        nextModel, writeCmd deps model writeRequest model.ArcSessionId writeRequestId operationId
    | WritePhaseChanged(sessionId, writeRequestId, _) when
        sessionId <> model.ArcSessionId || writeRequestId <> model.WriteRequestId
        ->
        model, Cmd.none
    | WritePhaseChanged(_, _, phase) ->
        {
            model with
                BusyOperation = Some phase
                BusyNotice = busyNoticeFromOperation phase
                CurrentProgress = None
        },
        Cmd.none
    | WriteCompleted(sessionId, writeRequestId, writeRequest, result) when
        sessionId <> model.ArcSessionId || writeRequestId <> model.WriteRequestId
        ->
        model, resolveStaleWriteCompletedCmd writeRequest result
    | WriteCompleted(_, _, writeRequest, Ok(StaleWorkspaceVersion(message, _))) ->
        // Reached when the retry after a refresh was stale again, or when the write is
        // one that is never replayed. The refresh shows the state the user has to review.
        let nextModel = {
            writeErrorModel message model with
                PendingPostMergePush = false
                RefreshPending = false
        }

        nextModel,
        Cmd.batch [
            resolveCloneReplyCmd writeRequest (Error message)
            reportWriteErrorCmd deps writeRequest message
            Cmd.ofMsg RefreshRequested
        ]
    | WriteCompleted(_, _, writeRequest, Ok(OperationCancelled message)) ->
        let nextModel = {
            clearBusy model with
                ErrorNotice = None
                WarningNotice = Some "Git operation cancelled."
                PendingPostMergePush = false
        }

        nextModel, resolveCloneReplyCmd writeRequest (Error message)
    | WriteCompleted(_, _, writeRequest, Error message) ->
        let nextModel = {
            writeErrorModel message model with
                PendingPostMergePush = false
        }

        nextModel,
        Cmd.batch [
            resolveCloneReplyCmd writeRequest (Error message)
            reportWriteErrorCmd deps writeRequest message
        ]
    | WriteCompleted(_, _, _, Ok(RequiresRemoteProjectRename message)) ->
        let nextModel = {
            clearBusy model with
                ErrorNotice = None
                PendingPublishRename = Some(publishRenamePrompt message model)
        }

        nextModel, Cmd.none
    | WriteCompleted(_, _, writeRequest, Ok(ProvisioningIncomplete(provisioned, message))) ->
        // The remote project exists. The next publish resumes with binding or publishing.
        // The refresh would clear the notice, so it is carried across as a warning.
        let nextModel = {
            clearBusy model with
                ErrorNotice = Some message
                PendingRefreshWarningNotice = Some message
                ProvisionedRemote = Some provisioned
        }

        nextModel,
        Cmd.batch [
            Cmd.ofMsg RefreshRequested
            reportWriteErrorCmd deps writeRequest message
        ]
    | WriteCompleted(_, _, writeRequest, Ok(RequiresRecovery(recovery, message))) ->
        // The recovery offer stays open. A refresh requested meanwhile runs after the offer is resolved.
        let nextModel = {
            clearBusy model with
                ErrorNotice = None
                WarningNotice = Some message
                PendingRecovery = Some recovery
                PendingConfirmation = Some(recoveryDialog recovery)
                PendingRemoteAction = GitPendingRemoteAction.Recover
        }

        nextModel, resolveCloneReplyCmd writeRequest (Error message)
    | WriteCompleted(sessionId, _, writeRequest, Ok(RequiresDependencyInstall(componentName, promptMessage))) ->
        let nextModel = {
            model with
                InstallRetryState =
                    GitInstallRetryState.PromptingForInstall(promptMessage, busyOperationForWriteRequest writeRequest)
        }

        let cmd =
            Cmd.OfFunc.either
                deps.confirmInstall
                promptMessage
                (fun shouldInstall -> WriteInstallPromptAnswered(sessionId, writeRequest, componentName, shouldInstall))
                (fun _ -> WriteInstallPromptAnswered(sessionId, writeRequest, componentName, false))

        nextModel, cmd
    | WriteInstallPromptAnswered(sessionId, writeRequest, _, _) when sessionId <> model.ArcSessionId ->
        model, resolveCloneReplyCmd writeRequest (Error staleArcSessionMessage)
    | WriteInstallPromptAnswered(_, writeRequest, componentName, false) ->
        let message =
            $"The version control dependency '{componentName}' is required to continue."

        let nextModel =
            model
            |> writeErrorModel message
            |> fun state -> {
                state with
                    InstallRetryState = GitInstallRetryState.Idle
            }

        nextModel,
        Cmd.batch [
            resolveCloneReplyCmd writeRequest (Error message)
            reportErrorCmd deps "Version control dependency required" message
        ]
    | WriteInstallPromptAnswered(sessionId, writeRequest, componentName, true) ->
        let busyOperation = busyOperationForWriteRequest writeRequest

        let installing = GitBusyOperation.InstallingDependency componentName

        // The install is the operation that runs now, so the cancel button targets it
        // instead of the write that asked for the dependency.
        let installOperationId = deps.newOperationId ()

        let nextModel = {
            model with
                InstallRetryState = GitInstallRetryState.InstallingForRetry busyOperation
                BusyOperation = Some installing
                BusyNotice = busyNoticeFromOperation installing
                CurrentOperation =
                    Some {
                        SessionId = ""
                        OperationId = installOperationId
                    }
        }

        let cmd =
            Cmd.OfPromise.either
                deps.installDependency
                {
                    OperationId = installOperationId
                    Component = componentName
                }
                (fun installResult -> WriteInstallCompleted(sessionId, writeRequest, installResult))
                (fun err -> WriteInstallCompleted(sessionId, writeRequest, Error(string err)))

        nextModel, cmd
    | WriteInstallCompleted(sessionId, writeRequest, _) when sessionId <> model.ArcSessionId ->
        model, resolveCloneReplyCmd writeRequest (Error staleArcSessionMessage)
    | WriteInstallCompleted(_, writeRequest, Error message) ->
        let nextModel =
            model
            |> writeErrorModel message
            |> fun state -> {
                state with
                    InstallRetryState = GitInstallRetryState.Idle
            }

        nextModel,
        Cmd.batch [
            resolveCloneReplyCmd writeRequest (Error message)
            reportErrorCmd deps "Could not install the dependency" message
        ]
    | WriteInstallCompleted(_, writeRequest, Ok(OperationResultDto.Failed failure)) ->
        let message = failureMessage failure

        let nextModel =
            model
            |> writeErrorModel message
            |> fun state -> {
                state with
                    InstallRetryState = GitInstallRetryState.Idle
            }

        nextModel,
        Cmd.batch [
            resolveCloneReplyCmd writeRequest (Error message)
            reportErrorCmd deps "Could not install the dependency" message
        ]
    | WriteInstallCompleted(sessionId, writeRequest, Ok _) ->
        // The dependency is configured. The failed stage runs again with the same
        // workspace token, because installing a dependency does not change the workspace.
        let busyOperation = busyOperationForWriteRequest writeRequest
        let operationId = deps.newOperationId ()

        let nextModel = {
            model with
                InstallRetryState = GitInstallRetryState.Idle
                BusyOperation = Some busyOperation
                BusyNotice = busyNoticeFromOperation busyOperation
                CurrentOperation =
                    Some {
                        SessionId = ""
                        OperationId = operationId
                    }
        }

        nextModel, writeCmd deps model writeRequest sessionId model.WriteRequestId operationId
    | WriteCompleted(_,
                     _,
                     writeRequest,
                     Ok(CompletedWithPendingRemoteConfirmation(success, dialog, pendingRemoteAction))) ->
        let baseModel, pageChange, warningMessage, _, recoveryMessage, _ =
            applyWriteSuccessModel model success

        // The confirmation dialog owns the pending action, so a recovery offer cannot be shown at the same time.
        // Surface its message as a warning until the dialog is resolved.
        let nextModel = {
            clearBusy baseModel with
                ErrorNotice = None
                WarningNotice = warningMessage |> Option.orElse recoveryMessage
                PendingConfirmation = Some dialog
                PendingRemoteAction = pendingRemoteAction
                PendingRecovery = None
                // PrimarySave with FinalizeMerge is the "All conflicts resolved" question after
                // a save whose update opened a conflict session. The publish resumes after finalization.
                // A save acceptance arrives as PublishAfterUpdate, so it falls through to `_ -> false`
                // because its confirm branch sets the flag. Merge previews no longer exist.
                PendingPostMergePush =
                    match writeRequest, pendingRemoteAction with
                    | PrimarySave _, GitPendingRemoteAction.FinalizeMerge -> true
                    | _, GitPendingRemoteAction.FinalizeMerge -> model.PendingPostMergePush
                    | _ -> false
        }

        nextModel, applyPageChangeCmd setPageState pageChange
    | WriteCompleted(_, _, writeRequest, Ok(CompletedWithPendingRemoteFailure(success, message))) ->
        let baseModel, pageChange, warningMessage, recovery, _, _ =
            applyWriteSuccessModel model success

        let nextModel = {
            clearBusy baseModel with
                ErrorNotice = Some message
                WarningNotice = warningMessage
                PendingConfirmation = None
                PendingRemoteAction = GitPendingRemoteAction.None
                PendingPostMergePush = false
                PendingRecovery = recovery
        }

        let report =
            match writeRequest with
            | PrimarySave _ -> reportErrorCmd deps "Could not push saved changes" message
            | _ -> reportWriteErrorCmd deps writeRequest message

        nextModel,
        Cmd.batch [
            applyPageChangeCmd setPageState pageChange
            resolveCloneReplyCmd writeRequest (Error message)
            report
        ]
    | WriteCompleted(_, _, writeRequest, Ok(Completed success)) ->
        let baseModel, pageChange, warningMessage, recovery, partial, published =
            applyWriteSuccessModel model success

        let nextModel = {
            clearBusy baseModel with
                ErrorNotice = None
                WarningNotice = warningMessage
                PendingPublishRename = None
                PendingRecovery = recovery
                PendingConfirmation = recovery |> Option.map recoveryDialog
                PendingRemoteAction =
                    match recovery with
                    | Some _ -> GitPendingRemoteAction.Recover
                    | None -> GitPendingRemoteAction.None
                ProvisionedRemote =
                    match writeRequest with
                    | Push _
                    | PrimarySave _ -> None
                    | _ -> model.ProvisionedRemote
        }

        let followUp =
            match writeRequest with
            // A cleared lock revealed an open merge: the abandon path runs it.
            | ClearStaleLock when nextModel.ActiveConflict.IsSome -> Cmd.ofMsg (WriteRequested AbandonMerge)
            | FinalizeMerge when shouldRunPostMergePush nextModel ->
                Cmd.ofMsg (WriteRequested(Push GitUpdateAcceptance.RequirePreview))
            | RetryMaterialization when shouldRunPostMergePush nextModel ->
                Cmd.ofMsg (WriteRequested(Push GitUpdateAcceptance.RequirePreview))
            | _ -> Cmd.none

        let nextModel =
            match writeRequest with
            | FinalizeMerge when shouldRunPostMergePush nextModel -> {
                nextModel with
                    PendingPostMergePush = false
              }
            | RetryMaterialization when shouldRunPostMergePush nextModel -> {
                nextModel with
                    PendingPostMergePush = false
              }
            | AbandonMerge -> {
                nextModel with
                    PendingPostMergePush = false
              }
            // Only a materialization recovery leaves the publish undone with nothing else
            // offering it. The publish resumes after that recovery. The retry_publish partial
            // is its own offer, and an unmapped partial has no path that could resume.
            | (PrimarySave _ | Push _) when published = Some false && partial.IsSome ->
                let resumesAfterMaterialization =
                    match recovery with
                    | Some(GitPendingRecovery.RetryMaterialization _) -> true
                    | _ -> false

                {
                    nextModel with
                        PendingPostMergePush = resumesAfterMaterialization
                }
            // The update inside the save opened a conflict session. The publish resumes
            // once the merge is finalized.
            | PrimarySave _ when nextModel.ActiveConflict.IsSome -> {
                nextModel with
                    PendingPostMergePush = true
              }
            // The push finished without a conflict session, so no publish is pending.
            | Push _ when nextModel.ActiveConflict.IsNone -> {
                nextModel with
                    PendingPostMergePush = false
              }
            | _ -> nextModel

        nextModel,
        Cmd.batch [
            applyPageChangeCmd setPageState pageChange
            resolveCloneReplyCmd writeRequest (Ok success)
            followUp
        ]

/// The wrapper holds a refresh until the write finishes and any open confirmation is resolved.
/// It also waits for the pending recovery to be handled.
let update
    (deps: GitDependencies)
    (setPageState: PageState option -> unit)
    (msg: Msg)
    (model: GitState)
    : GitState * Cmd<Msg> =
    let next, cmd = updateCore deps setPageState msg model

    if
        next.RefreshPending
        && next.BusyOperation.IsNone
        && next.PendingConfirmation.IsNone
        && next.PendingRecovery.IsNone
    then
        { next with RefreshPending = false }, Cmd.batch [ cmd; Cmd.ofMsg RefreshRequested ]
    else
        next, cmd

let subscribe (_model: GitState) : Sub<Msg> = [
    [ "versionControlProgress" ],
    fun dispatch ->
        let dispose =
            Renderer.IpcReceiver.subscribeProxyReceiver<IVersionControlRendererApi> {
                versionControlProgress = fun progress -> dispatch (SetCurrentProgress(Some(mapProgress progress)))
                versionControlOperationStarted = fun key -> dispatch (OperationStarted key)
            }

        { new System.IDisposable with
            member _.Dispose() = dispose ()
        }
    [ "gitRepositoryInitialized" ],
    fun dispatch ->
        let dispose =
            Renderer.IpcReceiver.subscribeProxyReceiver<IGitRepositoryRendererApi> {
                gitRepositoryInitialized = fun arcPath -> dispatch (GitRepositoryInitialized arcPath)
            }

        { new System.IDisposable with
            member _.Dispose() = dispose ()
        }
]
