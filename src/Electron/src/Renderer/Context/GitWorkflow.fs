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
    | InstallingGitLfs
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

/// What the confirmation dialog continues with when the user confirms.
[<RequireQualifiedAccess>]
type GitPendingRemoteAction =
    | None
    | UpdateFromOnline
    | CompletePrimarySavePush
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
    | ClearStaleLock of instructions: string option
    | ClearCloneTarget of targetPath: string * instructions: string option

/// Remote provisioning that has created a project but not finished binding or
/// publishing. A retry resumes here and never creates a second project.
type GitProvisionedRemote = {
    RemoteUrl: string
    ProjectName: string
    IsBound: bool
}

type InitRepositoryOutcome = { WarningMessage: string option }

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
    LfsAutoTrackThresholdMb: int
    DownloadLargeFiles: bool
    RepositoryAvailability: GitRepositoryAvailability
    RefreshState: GitRefreshState
    RefreshRequestId: int
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
    TargetRevision: string option
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
        LfsAutoTrackThresholdMb = 1
        DownloadLargeFiles = false
        RepositoryAvailability = GitRepositoryAvailability.Ready
        RefreshState = GitRefreshState.Idle
        RefreshRequestId = 0
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
        TargetRevision = None
    }

type GitRefreshResult = {
    Session: Result<WorkspaceSessionInfoDto, OperationFailureDto>
    Status: Result<WorkspaceStatusDto, OperationFailureDto>
    Refs: Result<LogicalRefDto[], OperationFailureDto>
    LfsSettings: Result<StoragePolicySettingsDto, OperationFailureDto> option
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
    | Pull
    | Push
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

/// A completed write: the refreshed snapshot, the page to show, an optional override
/// of the selected path, a warning, and the structured partial failure when the
/// library reported one, so its recovery code survives to the sidebar.
type WriteSuccess =
    | UnitSuccess of
        GitRefreshResult *
        GitPageChange *
        string option option *
        string option *
        OperationFailureDto option
    | CloneSuccess of string

type WriteAttemptOutcome =
    | Completed of WriteSuccess
    | CompletedWithPendingRemoteConfirmation of WriteSuccess * GitSidebarConfirmationDialog * GitPendingRemoteAction
    | CompletedWithPendingRemoteFailure of WriteSuccess * string
    | RequiresRemoteProjectRename of string
    | RequiresDependencyInstall of componentName: string * promptMessage: string
    | RequiresRecovery of GitPendingRecovery * message: string
    | OperationCancelled of string
    /// The remote project exists now. Binding or publishing still has to be retried.
    | ProvisioningIncomplete of GitProvisionedRemote * message: string

[<RequireQualifiedAccess>]
type private RoutedFailure =
    | Cancelled of string
    | DependencyInstall of componentName: string * message: string
    | Recovery of GitPendingRecovery * message: string
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
    | InitRepositoryCompleted of sessionId: int * result: Result<InitRepositoryOutcome, string>
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
    | CancelCurrentOperationCompleted of Result<bool, string>
    | UpdateFromOnlineRequested
    | UpdatePreflightCompleted of sessionId: int * Result<OperationResultDto<UpdatePreviewDto>, string>
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
    | PruneLfsCacheRequested
    | DedupLfsStorageRequested
    | FinalizeMergeRequested
    | AbandonMergeRequested
    | ClearStaleLockRequested
    | RestoreInterruptedPathsRequested
    | RetryMaterializationRequested
    | DismissRecoveryRequested
    | WriteRequested of WriteRequest
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
    loadDiffPage: string -> JS.Promise<Result<PageState, string>>
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
    previewUpdate: OperationRequestDto -> JS.Promise<Result<OperationResultDto<UpdatePreviewDto>, string>>
    update: UpdateRequestDto -> JS.Promise<Result<OperationResultDto<SynchronizationStateDto>, string>>
    publish: PublishRequestDto -> JS.Promise<Result<OperationResultDto<SynchronizationStateDto>, string>>
    cancelOperation: OperationKeyDto -> JS.Promise<Result<bool, string>>
    cloneWorkspace: CloneWorkspaceRequestDto -> JS.Promise<Result<OperationResultDto<string>, string>>
    createRef: CreateRefRequestDto -> JS.Promise<Result<OperationResultDto<LogicalRefDto>, string>>
    switchRef: SwitchRefRequestDto -> JS.Promise<Result<OperationResultDto<WorkspaceStatusDto>, string>>
    createRevision: CreateRevisionRequestDto -> JS.Promise<Result<OperationResultDto<string>, string>>
    restorePaths: RestorePathsRequestDto -> JS.Promise<Result<OperationResultDto<unit>, string>>
    getActiveConflictSession:
        OperationRequestDto -> JS.Promise<Result<OperationResultDto<ConflictSessionSummaryDto option>, string>>
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
    | GitBusyOperation.InstallingGitLfs -> Some "Installing Git LFS"
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

let hasNoTarget (synchronization: SynchronizationStateDto) =
    synchronization.Relationship = RevisionRelationshipDto.NoTarget
    || synchronization.TargetRef.IsNone

let shouldPublishCurrentBranchFirst (model: GitState) =
    match model.Status.CurrentBranch, model.Status.TrackingBranch with
    | Some _, None -> true
    | _ -> false

/// The pending recovery a completed write leaves behind, from the structured partial
/// failure the library attached to it.
let recoveryOfPartial (partial: OperationFailureDto option) : GitPendingRecovery option =
    partial
    |> Option.bind (fun failure ->
        match recoveryCode failure with
        | Some code when code = VersionControlCodes.Recovery.RetryMaterialization ->
            Some(GitPendingRecovery.RetryMaterialization(failureMessage failure))
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
    match refreshResult.Session, refreshResult.Status, refreshResult.Refs with
    | Error failure, _, _ -> Some failure
    | _, Error failure, _ -> Some failure
    | _, _, Error failure -> Some failure
    | Ok _, Ok _, Ok _ ->
        match refreshResult.LfsSettings with
        | Some(Error failure) -> Some failure
        | _ -> None

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
            TargetRevision = status.Synchronization |> Option.bind _.TargetRevision
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
                TargetRevision = None
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
        match refreshResult.Status, refreshResult.LfsSettings with
        | Ok _, Some(Ok settings) -> {
            modelWithBranches with
                LfsAutoTrackThresholdMb =
                    settings.AutoPolicyThresholdMb
                    |> Option.defaultValue GitState.Empty.LfsAutoTrackThresholdMb
                DownloadLargeFiles = settings.MaterializeLargeObjects
          }
        | Ok _, None -> modelWithBranches
        | _ -> {
            modelWithBranches with
                LfsAutoTrackThresholdMb = GitState.Empty.LfsAutoTrackThresholdMb
                DownloadLargeFiles = GitState.Empty.DownloadLargeFiles
          }

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
    | UnitSuccess(refreshResult, pageChange, selectedChangePathOverride, warningMessage, partial) ->
        let refreshedModel = applyRefreshResult refreshResult model

        let selectionAdjustedModel =
            match selectedChangePathOverride with
            | Some selectedChangePath -> {
                refreshedModel with
                    SelectedChangePath = selectedChangePath
              }
            | None -> refreshedModel

        selectionAdjustedModel, pageChange, warningMessage, recoveryOfPartial partial
    | CloneSuccess _ -> model, GitPageChange.NoChange, None, None

let nextRefreshRequestId (model: GitState) = model.RefreshRequestId + 1

let nextPageLoadRequestId (model: GitState) = model.PageLoadRequestId + 1

let nextArcSessionId (model: GitState) = model.ArcSessionId + 1

/// Paths are exact repository keys. Duplicates are dropped, nothing is trimmed.
let private distinctPaths (paths: string[]) =
    paths |> Array.filter (fun path -> path.Length > 0) |> Array.distinct

let prepareCommitSelection (state: GitState) (request: GitSidebarCommitSelectionRequest) = {
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
    | Pull -> "Could not pull changes"
    | Push -> "Could not push changes"
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
    Code = "transport_error"
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
            LfsSettings = None
            OriginRemoteRepositoryWebUrl = None
        }
    | Ok(sessionOutcome, _) ->
        let session = sessionOutcome.Value
        let! statusResult = toResult (deps.getStatus (request deps))
        let! refsResult = toResult (deps.listRefs (request deps))

        let! settingsResult =
            if session.Services.StoragePolicy then
                promise {
                    let! result = toResult (deps.getStoragePolicySettings (request deps))
                    return Some(result |> Result.map (fun (outcome, _) -> outcome.Value))
                }
            else
                promise { return None }

        let! webUrl =
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

let private targetRevisionOf (refreshResult: GitRefreshResult) =
    refreshResult.Status
    |> Result.toOption
    |> Option.bind _.Synchronization
    |> Option.bind _.TargetRevision

let private synchronizationOf (refreshResult: GitRefreshResult) =
    refreshResult.Status |> Result.toOption |> Option.bind _.Synchronization

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
    | Ok _ -> return Ok { WarningMessage = None }
}

let private loadPageAsync
    (deps: GitDependencies)
    (activeConflict: ConflictSessionSummaryDto option)
    (workspaceVersion: string option)
    (path: string)
    (isConflicted: bool)
    =
    promise {
        let! result =
            match isConflicted, activeConflict, workspaceVersion with
            | true, Some conflict, Some version -> deps.loadConflictPage conflict version path
            | true, _, _ -> promise {
                // The status says the path conflicts but the session is not in the model: reload it
                // together with a fresh token, so the page carries the state the user reviews.
                let! status = toResult (deps.getStatus (request deps))

                match status with
                | Ok(outcome, _) ->
                    match outcome.Value.ActiveConflictSession with
                    | Some conflict -> return! deps.loadConflictPage conflict outcome.Value.WorkspaceVersion path
                    | None -> return! deps.loadDiffPage path
                | Error failure -> return Error(failureMessage failure)
              }
            | false, _, _ -> deps.loadDiffPage path

        return result |> Result.map GitPageChange.Set
    }

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
        | _ -> RoutedFailure.Cancelled(failureMessage failure)
    elif needsDependencyInstall failure then
        RoutedFailure.DependencyInstall("", failureMessage failure)
    else
        RoutedFailure.Error(failureMessage failure)

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

let private routedToOutcome (deps: GitDependencies) (routed: RoutedFailure) = promise {
    match routed with
    | RoutedFailure.Cancelled message -> return Ok(OperationCancelled message)
    | RoutedFailure.DependencyInstall(_, message) -> return! resolveDependencyComponentAsync deps message
    | RoutedFailure.Recovery(recovery, message) -> return Ok(RequiresRecovery(recovery, message))
    | RoutedFailure.Error message -> return Error message
}

let private refreshAfterSuccess
    (deps: GitDependencies)
    (partial: OperationFailureDto option)
    (pageChange: GitPageChange)
    (selectedChangePathOverride: string option option)
    =
    promise {
        let! refreshResult = refreshAllAsync deps

        return
            match refreshResult.Status, refreshErrorMessage refreshResult with
            | Error failure, _ -> Error(failureMessage failure)
            | Ok _, Some message -> Error message
            | Ok _, None ->
                Ok(
                    Completed(
                        UnitSuccess(
                            refreshResult,
                            pageChange,
                            selectedChangePathOverride,
                            partial |> Option.map failureMessage,
                            partial
                        )
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
    runTrackedWriteAsync deps call (fun _ partial -> refreshAfterSuccess deps partial GitPageChange.NoChange None)

let private requireWorkspaceVersion (state: GitState) =
    match state.WorkspaceVersion with
    | Some version -> Ok version
    | None -> Error "The workspace state is not loaded yet. Refresh and try again."

let private requireSynchronization (state: GitState) =
    match state.Services with
    | Some services when not services.Synchronization ->
        Error "This workspace provider does not support online synchronization."
    | _ -> Ok()

let private runCloneAttemptAsync (deps: GitDependencies) (cloneRequest: CloneWorkspaceRequestDto) = promise {
    let! result = toResult (deps.cloneWorkspace cloneRequest)

    match result with
    | Ok(outcome, _) -> return Ok(Completed(CloneSuccess outcome.Value))
    | Error failure -> return! routedToOutcome deps (routeFailure (Some cloneRequest.TargetPath) failure)
}

/// After an update: opens the first conflicted item, asks to finalize an emptied
/// conflict session, or reports the refreshed state. A conflict outcome without a
/// session stops here with its failure so nothing publishes on top of it.
let private completeAfterUpdateAsync (deps: GitDependencies) (partial: OperationFailureDto option) = promise {
    let! refreshResult = refreshAllAsync deps

    match refreshResult.Status, refreshErrorMessage refreshResult with
    | Error failure, _ -> return Error(failureMessage failure)
    | Ok _, Some message -> return Error message
    | Ok latestStatus, None ->
        let isConflictOutcome =
            partial
            |> Option.exists (fun failure -> failure.Category = FailureCategoryDto.Conflict)

        let partialToKeep = if isConflictOutcome then None else partial

        match latestStatus.ActiveConflictSession with
        | Some conflict when conflict.Items.Length > 0 ->
            let firstConflictPath = conflict.Items.[0].Path
            let! pageResult = deps.loadConflictPage conflict latestStatus.WorkspaceVersion firstConflictPath

            return
                pageResult
                |> Result.map (fun page ->
                    Completed(
                        UnitSuccess(
                            refreshResult,
                            GitPageChange.Set page,
                            Some(Some firstConflictPath),
                            None,
                            partialToKeep
                        )
                    )
                )
        | Some _ ->
            return
                Ok(
                    CompletedWithPendingRemoteConfirmation(
                        UnitSuccess(refreshResult, GitPageChange.NoChange, None, None, partialToKeep),
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
                        UnitSuccess(refreshResult, GitPageChange.NoChange, None, None, None),
                        partial
                        |> Option.map failureMessage
                        |> Option.defaultValue "The update reported conflicts."
                    )
                )
        | None ->
            return
                Ok(
                    Completed(
                        UnitSuccess(
                            refreshResult,
                            GitPageChange.NoChange,
                            None,
                            partial |> Option.map failureMessage,
                            partial
                        )
                    )
                )
}

let private runPullAttemptAsync (deps: GitDependencies) (state: GitState) = promise {
    match requireSynchronization state, requireWorkspaceVersion state with
    | Error message, _
    | _, Error message -> return Error message
    | Ok(), Ok version ->
        let! result =
            toResult (
                deps.update {
                    OperationId = deps.newOperationId ()
                    ExpectedWorkspaceVersion = version
                }
            )

        match result with
        | Ok(_, partial) -> return! completeAfterUpdateAsync deps partial
        | Error failure ->
            match recoveryCode failure with
            | Some code when isCanceled failure && code = VersionControlCodes.Recovery.RefreshWorkspace ->
                // The update finished before the cancellation landed.
                let! refreshResult = refreshAllAsync deps

                match refreshResult.Status with
                | Ok _ ->
                    return
                        Ok(
                            Completed(
                                UnitSuccess(
                                    refreshResult,
                                    GitPageChange.NoChange,
                                    None,
                                    Some(failureMessage failure),
                                    None
                                )
                            )
                        )
                | Error refreshFailure -> return Error(failureMessage refreshFailure)
            | Some code when isCanceled failure && code = VersionControlCodes.Recovery.RemoveIndexLock ->
                return
                    Ok(
                        RequiresRecovery(
                            GitPendingRecovery.ClearStaleLock(failure.RecoveryAction |> Option.bind _.Instructions),
                            failureMessage failure
                        )
                    )
            | Some code when
                isCanceled failure
                && (code = VersionControlCodes.Recovery.InspectWorkspace
                    || code = VersionControlCodes.Recovery.AbortMerge)
                ->
                // Refresh first so the sidebar shows what the killed process left, then
                // surface the instructions.
                let! refreshResult = refreshAllAsync deps

                match refreshResult.Status with
                | Ok _ ->
                    return
                        Ok(
                            CompletedWithPendingRemoteFailure(
                                UnitSuccess(refreshResult, GitPageChange.NoChange, None, None, None),
                                failureMessage failure
                            )
                        )
                | Error refreshFailure -> return Error(failureMessage refreshFailure)
            | _ -> return! routedToOutcome deps (routeFailure None failure)
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
                    (fun _ partial -> refreshAfterSuccess deps partial GitPageChange.Clear (Some None))
}

let private pendingPrimarySaveWarning =
    "Changes were saved locally. Online sync is still pending."

let private indeterminateUpdateMessage (message: string option) =
    let fallback =
        "Swate could not determine safely whether updating will require merge resolution. Continue anyway?"

    match
        message
        |> Option.map _.Trim()
        |> Option.filter (String.IsNullOrWhiteSpace >> not)
    with
    | Some diagnostic -> $"{fallback} {diagnostic}"
    | None -> fallback

let private mergeConfirmationDialog (preview: UpdatePreviewDto) : GitSidebarConfirmationDialog =
    let overlapping =
        if preview.OverlappingPaths.Length > 0 then
            let separator = ", "
            let joined = String.Join(separator, preview.OverlappingPaths)
            $" Changed both locally and online: {joined}."
        else
            ""

    let message =
        if preview.WouldCreateConflictSession then
            $"Updating from online will require merge resolution.{overlapping} Continue?"
        else
            $"Updating from online would overwrite local changes.{overlapping} Continue?"

    {
        Title = "Merge resolution required"
        Message = message
        ConfirmLabel = "Open Merge Resolution"
        CancelLabel = "Cancel"
    }

let private indeterminateConfirmationDialog (message: string option) : GitSidebarConfirmationDialog = {
    Title = "Update could not be previewed"
    Message = indeterminateUpdateMessage message
    ConfirmLabel = "Continue"
    CancelLabel = "Cancel"
}

let private previewNeedsConfirmation (preview: UpdatePreviewDto) =
    preview.WouldCreateConflictSession || preview.HasDataLossRisk

/// The saved-locally outcome after a remote step failed. The snapshot is refreshed
/// first, because the failed step may have changed the workspace.
let private pendingPrimarySaveRemoteFailureAsync (deps: GitDependencies) (message: string) = promise {
    let! refreshResult = refreshAllAsync deps

    match refreshResult.Status with
    | Ok _ ->
        return
            Ok(
                CompletedWithPendingRemoteFailure(
                    UnitSuccess(refreshResult, GitPageChange.NoChange, None, Some pendingPrimarySaveWarning, None),
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
    | ProjectNameRefused of string
    | ProvisioningIncomplete of GitProvisionedRemote * string

/// Publishes the current refs. Without a configured target the repository is created
/// on the DataHub of the signed-in account (or the one created earlier is reused), the
/// workspace is bound to it and the publish runs once more. An unreachable target is
/// an outage and never triggers provisioning.
let private runPublishAsync
    (deps: GitDependencies)
    (model: GitState)
    (version: string)
    (targetRevision: string option)
    : JS.Promise<Result<OperationOutcomeDto<SynchronizationStateDto> * OperationFailureDto option, PublishFailure>> =
    promise {
        let publishOnce (version: string) (targetRevision: string option) =
            toResult (
                deps.publish {
                    OperationId = deps.newOperationId ()
                    ExpectedWorkspaceVersion = version
                    ExpectedTargetRevision = targetRevision
                }
            )

        let projectName =
            model.CurrentArcPath |> Option.bind tryGetPathLeaf |> Option.defaultValue "ARC"

        let bindAndPublish (provisioned: GitProvisionedRemote) = promise {
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
                    let! published =
                        publishOnce
                            statusOutcome.Value.WorkspaceVersion
                            (statusOutcome.Value.Synchronization |> Option.bind _.TargetRevision)

                    match published with
                    | Ok(outcome, partial) -> return Ok(outcome, partial)
                    | Error failure when isCanceled failure ->
                        return Error(PublishFailure.ProvisioningIncomplete(boundRemote, failureMessage failure))
                    | Error failure -> return Error(PublishFailure.Routed(routeFailure None failure))
        }

        let provisionAndPublish () = promise {
            match model.ProvisionedRemote with
            | Some provisioned -> return! bindAndPublish provisioned
            | None ->
                let! created = deps.createRemoteProject projectName

                match created with
                | Error error when isProjectNameRefused error ->
                    return Error(PublishFailure.ProjectNameRefused error.GitLabErrorToString)
                | Error error -> return Error(PublishFailure.Routed(RoutedFailure.Error error.GitLabErrorToString))
                | Ok project ->
                    return!
                        bindAndPublish {
                            RemoteUrl = project.http_url_to_repo
                            ProjectName = projectName
                            IsBound = false
                        }
        }

        match model.ProvisionedRemote with
        | Some _ -> return! provisionAndPublish ()
        | None ->
            let! first = publishOnce version targetRevision

            match first with
            | Ok(outcome, partial) -> return Ok(outcome, partial)
            | Error failure when needsPublishTarget failure -> return! provisionAndPublish ()
            | Error failure -> return Error(PublishFailure.Routed(routeFailure None failure))
    }

let private publishFailureToOutcome (deps: GitDependencies) (failure: PublishFailure) = promise {
    match failure with
    | PublishFailure.Routed routed -> return! routedToOutcome deps routed
    | PublishFailure.ProjectNameRefused message -> return Ok(RequiresRemoteProjectRename message)
    | PublishFailure.ProvisioningIncomplete(provisioned, message) ->
        return Ok(ProvisioningIncomplete(provisioned, message))
}

let private runPushAttemptAsync (deps: GitDependencies) (state: GitState) = promise {
    match requireSynchronization state, requireWorkspaceVersion state with
    | Error message, _
    | _, Error message -> return Error message
    | Ok(), Ok version ->
        let! result = runPublishAsync deps state version state.TargetRevision

        match result with
        | Ok(_, partial) -> return! refreshAfterSuccess deps partial GitPageChange.NoChange None
        | Error failure -> return! publishFailureToOutcome deps failure
}

let private runFetchAttemptAsync (deps: GitDependencies) (state: GitState) = promise {
    match requireSynchronization state with
    | Error message -> return Error message
    | Ok() -> return! simpleWriteAsync deps (fun () -> deps.refreshSynchronization (request deps))
}

/// Primary save: commit the exact selection, then bring the branch online. A partial
/// commit that needs reconciliation stops here with its recovery. A no-op commit still
/// continues with the online steps, because the workspace may be ahead of the target.
let private runPrimarySaveAttemptAsync (deps: GitDependencies) (state: GitState) (prepared: PreparedCommitOperation) = promise {
    match requireSynchronization state with
    | Error message -> return Error message
    | Ok() ->
        let! commitAttempt = runCommitAttemptAsync deps state prepared

        match commitAttempt with
        | Error message -> return Error message
        | Ok(Completed(UnitSuccess(refreshResult, pageChange, selectedChangePathOverride, _, Some partial))) ->
            // The revision exists but the library asks for reconciliation first.
            return
                Ok(
                    CompletedWithPendingRemoteFailure(
                        UnitSuccess(
                            refreshResult,
                            pageChange,
                            selectedChangePathOverride,
                            Some pendingPrimarySaveWarning,
                            Some partial
                        ),
                        failureMessage partial
                    )
                )
        | Ok(Completed(UnitSuccess(refreshResult, pageChange, selectedChangePathOverride, _, None))) ->
            let refreshedState = applyRefreshResult refreshResult state

            let runPushAfterLocalCommit (version: string) (targetRevision: string option) = promise {
                let! pushResult = runPublishAsync deps refreshedState version targetRevision

                match pushResult with
                | Ok(_, partial) -> return! refreshAfterSuccess deps partial GitPageChange.NoChange None
                | Error(PublishFailure.ProjectNameRefused message) -> return Ok(RequiresRemoteProjectRename message)
                | Error(PublishFailure.ProvisioningIncomplete(provisioned, message)) ->
                    return Ok(ProvisioningIncomplete(provisioned, message))
                | Error(PublishFailure.Routed(RoutedFailure.Recovery(recovery, message))) ->
                    return Ok(RequiresRecovery(recovery, message))
                | Error(PublishFailure.Routed(RoutedFailure.Cancelled message))
                | Error(PublishFailure.Routed(RoutedFailure.DependencyInstall(_, message)))
                | Error(PublishFailure.Routed(RoutedFailure.Error message)) ->
                    // The local commit already succeeded, so the saved-locally outcome stays.
                    return! pendingPrimarySaveRemoteFailureAsync deps message
            }

            match versionOf refreshResult, synchronizationOf refreshResult with
            | None, _ ->
                return! pendingPrimarySaveRemoteFailureAsync deps "The workspace state could not be read after saving."
            | Some version, synchronization ->
                let noTarget = synchronization |> Option.map hasNoTarget |> Option.defaultValue true

                if noTarget || shouldPublishCurrentBranchFirst refreshedState then
                    return! runPushAfterLocalCommit version (targetRevisionOf refreshResult)
                else
                    let! previewResult = toResult (deps.previewUpdate (request deps))

                    match previewResult with
                    | Error failure when failure.Category = FailureCategoryDto.Unsupported ->
                        return
                            Ok(
                                CompletedWithPendingRemoteConfirmation(
                                    UnitSuccess(
                                        refreshResult,
                                        pageChange,
                                        selectedChangePathOverride,
                                        Some pendingPrimarySaveWarning,
                                        None
                                    ),
                                    indeterminateConfirmationDialog (Some failure.Message),
                                    GitPendingRemoteAction.CompletePrimarySavePush
                                )
                            )
                    | Error failure -> return! pendingPrimarySaveRemoteFailureAsync deps (failureMessage failure)
                    | Ok(previewOutcome, _) when previewNeedsConfirmation previewOutcome.Value ->
                        return
                            Ok(
                                CompletedWithPendingRemoteConfirmation(
                                    UnitSuccess(
                                        refreshResult,
                                        pageChange,
                                        selectedChangePathOverride,
                                        Some pendingPrimarySaveWarning,
                                        None
                                    ),
                                    mergeConfirmationDialog previewOutcome.Value,
                                    GitPendingRemoteAction.CompletePrimarySavePush
                                )
                            )
                    | Ok _ ->
                        let! updateResult =
                            toResult (
                                deps.update {
                                    OperationId = deps.newOperationId ()
                                    ExpectedWorkspaceVersion = version
                                }
                            )

                        match updateResult with
                        | Error failure when
                            isCanceled failure
                            && recoveryCode failure = Some VersionControlCodes.Recovery.RemoveIndexLock
                            ->
                            return
                                Ok(
                                    RequiresRecovery(
                                        GitPendingRecovery.ClearStaleLock(
                                            failure.RecoveryAction |> Option.bind _.Instructions
                                        ),
                                        failureMessage failure
                                    )
                                )
                        | Error failure ->
                            match routeFailure None failure with
                            | RoutedFailure.Recovery(recovery, message) ->
                                return Ok(RequiresRecovery(recovery, message))
                            | RoutedFailure.Cancelled message
                            | RoutedFailure.DependencyInstall(_, message)
                            | RoutedFailure.Error message -> return! pendingPrimarySaveRemoteFailureAsync deps message
                        | Ok(_, partial) ->
                            let! afterUpdate = completeAfterUpdateAsync deps partial

                            match afterUpdate with
                            | Ok(Completed(UnitSuccess(updatedResult, GitPageChange.NoChange, None, _, updatePartial))) when
                                (updatedResult.Status |> Result.toOption |> Option.bind _.ActiveConflictSession).IsNone
                                ->
                                match versionOf updatedResult with
                                | Some updatedVersion ->
                                    let! pushed =
                                        runPushAfterLocalCommit updatedVersion (targetRevisionOf updatedResult)

                                    match pushed, updatePartial with
                                    | Ok(Completed(UnitSuccess(finalResult, page, selection, warning, None))), Some kept ->
                                        // A materialization retry offered by the update survives a later publish.
                                        return
                                            Ok(Completed(UnitSuccess(finalResult, page, selection, warning, Some kept)))
                                    | other, _ -> return other
                                | None ->
                                    return!
                                        pendingPrimarySaveRemoteFailureAsync
                                            deps
                                            "The workspace state could not be read after updating."
                            | Ok(CompletedWithPendingRemoteConfirmation(success, dialog, action)) ->
                                // A conflict session waits for the user. The publish resumes after the merge.
                                return Ok(CompletedWithPendingRemoteConfirmation(success, dialog, action))
                            | other -> return other
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
                        (fun _ partial -> refreshAfterSuccess deps partial GitPageChange.Clear (Some None))
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
                (fun _ partial -> refreshAfterSuccess deps partial GitPageChange.Clear (Some None))
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
                (fun _ partial -> refreshAfterSuccess deps partial GitPageChange.Clear (Some None))
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
                (fun _ partial -> refreshAfterSuccess deps partial GitPageChange.Clear (Some None))
}

/// Removes a stale lock and refreshes. The refreshed status decides what follows: an
/// active conflict session is abandoned through the normal path.
let private runClearStaleLockAttemptAsync (deps: GitDependencies) =
    runTrackedWriteAsync
        deps
        (fun () -> deps.clearStaleLock (request deps))
        (fun _ partial -> refreshAfterSuccess deps partial GitPageChange.NoChange None)

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
                (fun _ partial -> refreshAfterSuccess deps partial GitPageChange.Clear (Some None))
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

        for objectState in pending do
            if failure.IsNone then
                let! materialized =
                    toResult (
                        deps.materializeObject {
                            OperationId = deps.newOperationId ()
                            Path = objectState.Path
                        }
                    )

                match materialized with
                | Ok _ -> ()
                | Error error -> failure <- Some error

        match failure with
        | Some error -> return! routedToOutcome deps (routeFailure None error)
        | None -> return! refreshAfterSuccess deps None GitPageChange.NoChange None
}

let private executeWriteAttempt (deps: GitDependencies) (state: GitState) (writeRequest: WriteRequest) = promise {
    match writeRequest with
    | Fetch -> return! runFetchAttemptAsync deps state
    | Pull -> return! runPullAttemptAsync deps state
    | Push -> return! runPushAttemptAsync deps state
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
    | Pull -> GitBusyOperation.PullingFromRemote
    | Push -> GitBusyOperation.PushingToRemote
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
    | Error message -> resolveCloneReplyCmd writeRequest (Error message)

let private writeCmd
    (deps: GitDependencies)
    (model: GitState)
    (writeRequest: WriteRequest)
    (sessionId: int)
    (writeRequestId: int)
    =
    Cmd.OfPromise.either
        (fun (deps, model, writeRequest) -> executeWriteAttempt deps model writeRequest)
        (deps, model, writeRequest)
        (fun result -> WriteCompleted(sessionId, writeRequestId, writeRequest, result))
        (fun err -> WriteCompleted(sessionId, writeRequestId, writeRequest, Error(string err)))

/// Whether the post-merge publish should run now: the primary save asked for it and the
/// workspace has no open conflict session anymore.
let private shouldRunPostMergePush (model: GitState) =
    model.PendingPostMergePush && model.ActiveConflict.IsNone

let update
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
        {
            model with
                CurrentOperation = Some key
        },
        Cmd.none
    | OperationStarted _ -> model, Cmd.none
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
            | None -> Cmd.none

        nextModel, cmd
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
    | InitRepositoryCompleted(_, Ok outcome) ->
        let nextModel = {
            clearBusy model with
                RepositoryAvailability = GitRepositoryAvailability.Ready
                ErrorNotice = None
                WarningNotice = outcome.WarningMessage
                PendingRefreshWarningNotice = outcome.WarningMessage
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
                    loadPageAsync deps model.ActiveConflict model.WorkspaceVersion change.Path change.IsConflicted
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
                Cmd.ofMsg (WriteRequested Push)
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
    | PullRequested -> model, Cmd.ofMsg (WriteRequested Pull)
    | PushRequested -> model, Cmd.ofMsg (WriteRequested Push)
    | CancelCurrentOperationRequested ->
        match model.CurrentOperation with
        | None -> model, Cmd.none
        | Some key ->
            let nextModel = {
                model with
                    WarningNotice = Some "Canceling operation..."
            }

            let cmd =
                Cmd.OfPromise.either
                    deps.cancelOperation
                    key
                    CancelCurrentOperationCompleted
                    (fun err -> CancelCurrentOperationCompleted(Error(string err)))

            nextModel, cmd
    | CancelCurrentOperationCompleted(Error message) ->
        {
            model with
                ErrorNotice = Some message
                WarningNotice = None
        },
        reportErrorCmd deps "Could not cancel Git operation" message
    | CancelCurrentOperationCompleted(Ok _) -> model, Cmd.none
    | UpdateFromOnlineRequested when model.CurrentArcPath.IsNone || model.BusyOperation.IsSome -> model, Cmd.none
    | UpdateFromOnlineRequested ->
        let operationId = deps.newOperationId ()

        let nextModel =
            model
            |> withBusyOperation (Some GitBusyOperation.FetchingFromRemote)
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
                deps.previewUpdate
                { OperationId = operationId }
                (fun result -> UpdatePreflightCompleted(model.ArcSessionId, result))
                (fun err -> UpdatePreflightCompleted(model.ArcSessionId, Error(string err)))

        nextModel, cmd
    | UpdatePreflightCompleted(sessionId, _) when sessionId <> model.ArcSessionId -> model, Cmd.none
    | UpdatePreflightCompleted(_, Ok(OperationResultDto.Succeeded outcome))
    | UpdatePreflightCompleted(_, Ok(OperationResultDto.PartiallySucceeded(outcome, _))) ->
        if previewNeedsConfirmation outcome.Value then
            {
                clearBusy model with
                    PendingConfirmation = Some(mergeConfirmationDialog outcome.Value)
                    PendingRemoteAction = GitPendingRemoteAction.UpdateFromOnline
            },
            Cmd.none
        else
            clearBusy model, Cmd.ofMsg (WriteRequested Pull)
    | UpdatePreflightCompleted(_, Ok(OperationResultDto.Failed failure)) when isCanceled failure ->
        {
            clearBusy model with
                WarningNotice = Some "Git operation cancelled."
        },
        Cmd.none
    | UpdatePreflightCompleted(_, Ok(OperationResultDto.Failed failure)) when
        failure.Category = FailureCategoryDto.Unsupported
        ->
        {
            clearBusy model with
                PendingConfirmation = Some(indeterminateConfirmationDialog (Some failure.Message))
                PendingRemoteAction = GitPendingRemoteAction.UpdateFromOnline
        },
        Cmd.none
    | UpdatePreflightCompleted(_, Ok(OperationResultDto.Failed failure)) ->
        let message = failureMessage failure

        {
            clearBusy model with
                ErrorNotice = Some message
        },
        reportErrorCmd deps "Could not preview update from online" message
    | UpdatePreflightCompleted(_, Error message) ->
        {
            clearBusy model with
                ErrorNotice = Some message
        },
        reportErrorCmd deps "Could not preview update from online" message
    | CloneRequested(cloneRequest, reply) -> model, Cmd.ofMsg (WriteRequested(Clone(cloneRequest, reply)))
    | PrimarySaveSelectionRequested selection ->
        model, Cmd.ofMsg (WriteRequested(PrimarySave(prepareCommitSelection model selection)))
    | PrimarySaveAllRequested message -> model, Cmd.ofMsg (WriteRequested(PrimarySave(prepareCommitAll model message)))
    | CommitSelectionRequested selection ->
        model, Cmd.ofMsg (WriteRequested(CommitSelection(prepareCommitSelection model selection)))
    | CommitAllRequested message -> model, Cmd.ofMsg (WriteRequested(CommitAll(prepareCommitAll model message)))
    | DiscardSelectionRequested paths -> model, Cmd.ofMsg (WriteRequested(DiscardSelection paths))
    | ConfirmPendingRemoteActionRequested ->
        match model.PendingRemoteAction with
        | GitPendingRemoteAction.UpdateFromOnline ->
            {
                model with
                    PendingConfirmation = None
                    PendingRemoteAction = GitPendingRemoteAction.None
            },
            Cmd.ofMsg (WriteRequested Pull)
        | GitPendingRemoteAction.CompletePrimarySavePush ->
            {
                model with
                    PendingConfirmation = None
                    PendingRemoteAction = GitPendingRemoteAction.None
                    PendingPostMergePush = true
            },
            Cmd.ofMsg (WriteRequested Pull)
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
                | Some(GitPendingRecovery.ClearStaleLock _) -> Cmd.ofMsg ClearStaleLockRequested
                | Some(GitPendingRecovery.ClearCloneTarget _)
                | None -> Cmd.ofMsg DismissRecoveryRequested

            {
                model with
                    PendingConfirmation = None
                    PendingRemoteAction = GitPendingRemoteAction.None
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
        sessionId <> model.ArcSessionId && model.CurrentArcPath = Some renamedPath
        ->
        {
            clearBusy model with
                PendingPublishRename = None
                ErrorNotice = None
        },
        Cmd.ofMsg (WriteRequested Push)
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
        Cmd.ofMsg (WriteRequested Push)
    | CreateBranchRequested branchRequest -> model, Cmd.ofMsg (WriteRequested(CreateBranch branchRequest))
    | SwitchBranchRequested branchName ->
        let normalizedBranchName = branchName.Trim()

        if String.IsNullOrWhiteSpace normalizedBranchName then
            model, Cmd.none
        else
            model, Cmd.ofMsg (WriteRequested(SwitchBranch normalizedBranchName))
    | PruneLfsCacheRequested ->
        let message =
            "This cleans hidden Git LFS cache files for the current ARC. Files that are still needed can be downloaded again from the remote. Continue?"

        if deps.confirmLfsPrune message then
            model, Cmd.ofMsg (WriteRequested PruneLfsCache)
        else
            model, Cmd.none
    | DedupLfsStorageRequested -> model, Cmd.ofMsg (WriteRequested DedupLfsStorage)
    | FinalizeMergeRequested -> model, Cmd.ofMsg (WriteRequested FinalizeMerge)
    | AbandonMergeRequested -> model, Cmd.ofMsg (WriteRequested AbandonMerge)
    | ClearStaleLockRequested -> { model with PendingRecovery = None }, Cmd.ofMsg (WriteRequested ClearStaleLock)
    | RestoreInterruptedPathsRequested ->
        match model.PendingRecovery with
        | Some(GitPendingRecovery.RestoreInterruptedPaths interrupted) when interrupted.AffectedPaths.Length > 0 ->
            { model with PendingRecovery = None },
            Cmd.ofMsg (WriteRequested(RestoreInterruptedPaths interrupted.AffectedPaths))
        | _ -> { model with PendingRecovery = None }, Cmd.none
    | RetryMaterializationRequested ->
        { model with PendingRecovery = None }, Cmd.ofMsg (WriteRequested RetryMaterialization)
    | DismissRecoveryRequested -> { model with PendingRecovery = None }, Cmd.none
    | WriteRequested writeRequest when requiresArcForWriteRequest writeRequest && model.CurrentArcPath.IsNone ->
        model, Cmd.none
    | WriteRequested writeRequest when model.BusyOperation.IsSome ->
        // One mutation at a time. The sidebar disables its buttons while busy, so this
        // only catches a request that raced the completion.
        model, resolveCloneReplyCmd writeRequest (Error "Another Git operation is still running.")
    | WriteRequested writeRequest ->
        let writeRequestId = model.WriteRequestId + 1

        let nextModel =
            model
            |> withBusyOperation (Some(busyOperationForWriteRequest writeRequest))
            |> fun state -> {
                state with
                    ErrorNotice = None
                    WarningNotice = None
                    WriteRequestId = writeRequestId
            }

        nextModel, writeCmd deps model writeRequest model.ArcSessionId writeRequestId
    | WriteCompleted(sessionId, writeRequestId, writeRequest, result) when
        sessionId <> model.ArcSessionId || writeRequestId <> model.WriteRequestId
        ->
        model, resolveStaleWriteCompletedCmd writeRequest result
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
        let nextModel = {
            clearBusy model with
                ErrorNotice = Some message
                ProvisionedRemote = Some provisioned
        }

        nextModel,
        Cmd.batch [
            Cmd.ofMsg RefreshRequested
            reportWriteErrorCmd deps writeRequest message
        ]
    | WriteCompleted(_, _, writeRequest, Ok(RequiresRecovery(recovery, message))) ->
        // The workspace may have changed, so the sidebar refreshes and keeps the offer.
        let nextModel = {
            clearBusy model with
                ErrorNotice = None
                WarningNotice = Some message
                PendingRecovery = Some recovery
                PendingConfirmation = Some(recoveryDialog recovery)
                PendingRemoteAction = GitPendingRemoteAction.Recover
        }

        nextModel,
        Cmd.batch [
            resolveCloneReplyCmd writeRequest (Error message)
            match writeRequest with
            | Clone _ -> Cmd.none
            | _ -> Cmd.ofMsg RefreshRequested
        ]
    | WriteCompleted(sessionId, _, writeRequest, Ok(RequiresDependencyInstall(componentName, promptMessage))) ->
        let nextModel = {
            model with
                InstallRetryState =
                    GitInstallRetryState.PromptingForInstall(promptMessage, busyOperationForWriteRequest writeRequest)
        }

        let cmd =
            Cmd.OfFunc.perform
                deps.confirmInstall
                promptMessage
                (fun shouldInstall -> WriteInstallPromptAnswered(sessionId, writeRequest, componentName, shouldInstall))

        nextModel, cmd
    | WriteInstallPromptAnswered(sessionId, writeRequest, _, _) when sessionId <> model.ArcSessionId ->
        model, resolveCloneReplyCmd writeRequest (Error staleArcSessionMessage)
    | WriteInstallPromptAnswered(_, writeRequest, _, false) ->
        let message = "Git LFS installation is required to continue."

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
            reportErrorCmd deps "Git LFS installation required" message
        ]
    | WriteInstallPromptAnswered(sessionId, writeRequest, componentName, true) ->
        let busyOperation = busyOperationForWriteRequest writeRequest

        let nextModel = {
            model with
                InstallRetryState = GitInstallRetryState.InstallingForRetry busyOperation
                BusyOperation = Some GitBusyOperation.InstallingGitLfs
                BusyNotice = busyNoticeFromOperation GitBusyOperation.InstallingGitLfs
        }

        let cmd =
            Cmd.OfPromise.either
                deps.installDependency
                {
                    OperationId = deps.newOperationId ()
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
            reportErrorCmd deps "Could not install Git LFS" message
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
            reportErrorCmd deps "Could not install Git LFS" message
        ]
    | WriteInstallCompleted(sessionId, writeRequest, Ok _) ->
        // The dependency is configured. The failed stage runs again with a fresh token.
        let busyOperation = busyOperationForWriteRequest writeRequest

        let nextModel = {
            model with
                InstallRetryState = GitInstallRetryState.Idle
                BusyOperation = Some busyOperation
                BusyNotice = busyNoticeFromOperation busyOperation
        }

        nextModel, writeCmd deps model writeRequest sessionId model.WriteRequestId
    | WriteCompleted(_,
                     _,
                     PrimarySave _,
                     Ok(CompletedWithPendingRemoteConfirmation(success, dialog, pendingRemoteAction)))
    | WriteCompleted(_, _, Pull, Ok(CompletedWithPendingRemoteConfirmation(success, dialog, pendingRemoteAction))) ->
        let baseModel, pageChange, warningMessage, recovery =
            applyWriteSuccessModel model success

        let nextModel = {
            clearBusy baseModel with
                ErrorNotice = None
                WarningNotice = warningMessage
                PendingConfirmation = Some dialog
                PendingRemoteAction = pendingRemoteAction
                PendingRecovery = recovery
                // A finalize question keeps the pending publish. A merge preview starts one.
                PendingPostMergePush =
                    match pendingRemoteAction with
                    | GitPendingRemoteAction.FinalizeMerge -> model.PendingPostMergePush
                    | _ -> false
        }

        nextModel, applyPageChangeCmd setPageState pageChange
    | WriteCompleted(_, _, PrimarySave _, Ok(CompletedWithPendingRemoteFailure(success, message)))
    | WriteCompleted(_, _, Pull, Ok(CompletedWithPendingRemoteFailure(success, message))) ->
        let baseModel, pageChange, warningMessage, recovery =
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

        nextModel,
        Cmd.batch [
            applyPageChangeCmd setPageState pageChange
            reportErrorCmd deps "Could not complete online synchronization" message
        ]
    | WriteCompleted(_, _, writeRequest, Ok(CompletedWithPendingRemoteConfirmation(_, _, _))) ->
        let message = "Git operation produced an invalid pending remote confirmation."
        let nextModel = writeErrorModel message model

        nextModel,
        Cmd.batch [
            resolveCloneReplyCmd writeRequest (Error message)
            reportWriteErrorCmd deps writeRequest message
        ]
    | WriteCompleted(_, _, writeRequest, Ok(CompletedWithPendingRemoteFailure(_, _))) ->
        let message = "Git operation produced an invalid pending remote failure."
        let nextModel = writeErrorModel message model

        nextModel,
        Cmd.batch [
            resolveCloneReplyCmd writeRequest (Error message)
            reportWriteErrorCmd deps writeRequest message
        ]
    | WriteCompleted(_, _, writeRequest, Ok(Completed success)) ->
        let baseModel, pageChange, warningMessage, recovery =
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
                    | Push
                    | PrimarySave _ -> None
                    | _ -> model.ProvisionedRemote
        }

        let followUp =
            match writeRequest with
            // A cleared lock revealed an open merge: the abandon path runs it.
            | ClearStaleLock when nextModel.ActiveConflict.IsSome -> Cmd.ofMsg (WriteRequested AbandonMerge)
            // The confirmed update finished without a conflict session: the pending publish runs.
            | Pull when shouldRunPostMergePush nextModel -> Cmd.ofMsg (WriteRequested Push)
            | FinalizeMerge when shouldRunPostMergePush nextModel -> Cmd.ofMsg (WriteRequested Push)
            | _ -> Cmd.none

        let nextModel =
            match writeRequest with
            | Pull
            | FinalizeMerge when shouldRunPostMergePush nextModel -> {
                nextModel with
                    PendingPostMergePush = false
              }
            | AbandonMerge -> {
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
