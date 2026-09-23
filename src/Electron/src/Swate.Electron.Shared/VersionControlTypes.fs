/// Provider-neutral version control DTOs that cross the IPC boundary. They mirror the
/// library's abstractions with plain strings and serializable unions and carry no
/// provider-specific type. Every request names the operation it belongs to so the
/// renderer can cancel it before the first progress event arrives.
module Swate.Electron.Shared.VersionControlTypes

open Fable.Core

[<StringEnum(CaseRules.None)>]
type FailureCategoryDto =
    | Validation
    | NotFound
    | Concurrency
    | Authentication
    | Authorization
    | DependencyMissing
    | Network
    | Timeout
    | Canceled
    | Conflict
    | Unsupported
    | ProviderError

type RecoveryActionDto = {
    Code: string
    Instructions: string option
}

type RevisionEvidenceDto = { Label: string; Revision: string }

type OperationFailureDto = {
    Category: FailureCategoryDto
    Code: string
    Message: string
    StateChanged: bool
    Retryable: bool
    AffectedPaths: string[]
    RecoveryAction: RecoveryActionDto option
    Details: string[]
    RevisionEvidence: RevisionEvidenceDto[]
}

type OperationWarningDto = { Code: string; Message: string }

[<RequireQualifiedAccess>]
type OperationEffectDto =
    | Performed
    | NoOp of reason: string option

[<StringEnum(CaseRules.None)>]
type PublicationStateDto =
    | NotApplicable
    | LocalOnly
    | Published

type OperationOutcomeDto<'T> = {
    Value: 'T
    Effect: OperationEffectDto
    Warnings: OperationWarningDto[]
    AffectedPaths: string[]
    ResultingRevision: string option
    ResultingWorkspaceVersion: string option
    Publication: PublicationStateDto
}

[<RequireQualifiedAccess>]
type OperationResultDto<'T> =
    | Succeeded of OperationOutcomeDto<'T>
    | PartiallySucceeded of OperationOutcomeDto<'T> * OperationFailureDto
    | Failed of OperationFailureDto

type VersionControlProgressDto = {
    OperationId: string
    PhaseCode: string
    Item: string option
    Completed: float option
    Total: float option
    DisplayMessage: string option
}

/// Codes the library and the host report. The renderer routes on some of them,
/// and tests build failures with others.
/// Each group below names its producer.
module VersionControlCodes =

    // Produced by the library (failure codes of its providers).

    [<Literal>]
    let PublishTargetMissing = "publish_target_missing"

    [<Literal>]
    let TargetUnreachable = "target_unreachable"

    [<Literal>]
    let NetworkFailure = "network_failure"

    [<Literal>]
    let PreconditionFailed = "precondition_failed"

    [<Literal>]
    let ConflictsDetected = "conflicts_detected"

    [<Literal>]
    let ConflictSessionActive = "conflict_session_active"

    [<Literal>]
    let UpdateWouldOverwriteLocalChanges = "update_would_overwrite_local_changes"

    [<Literal>]
    let UpdateWouldCreateConflictSession = "update_would_create_conflict_session"

    [<Literal>]
    let AcceptanceTargetRequired = "acceptance_target_required"

    [<Literal>]
    let PreviewIndeterminate = "preview_indeterminate"

    [<Literal>]
    let TargetNotEmpty = "target_not_empty"

    [<Literal>]
    let OperationCanceled = "operation_canceled"

    [<Literal>]
    let BaseContentNotFound = "base_content_not_found"

    /// The library reports this warning when a picked file's content is not in the local cache.
    [<Literal>]
    let ObjectNotMaterialized = "object_not_materialized"

    [<Literal>]
    let InvalidLfsThreshold = "invalid_lfs_threshold"

    // Produced by the Swate main process (session host and IPC handler).
    [<Literal>]
    let ServiceUnavailable = "service_unavailable"

    [<Literal>]
    let SessionUnavailable = "session_unavailable"

    [<Literal>]
    let WorkspaceUnmanaged = "workspace_unmanaged"

    [<Literal>]
    let WorkspaceAmbiguous = "workspace_ambiguous"

    [<Literal>]
    let LocationUnsupported = "location_unsupported"

    [<Literal>]
    let UnexpectedException = "unexpected_exception"

    [<Literal>]
    let LockRemovalRefused = "lock_removal_refused"

    [<Literal>]
    let LockRemoved = "lock_removed"

    [<Literal>]
    let InvalidPath = "invalid_path"

    [<Literal>]
    let InvalidRef = "invalid_ref"

    [<Literal>]
    let InvalidRevision = "invalid_revision"

    [<Literal>]
    let BindingNotPersisted = "binding_not_persisted"

    /// The IPC call itself failed before a structured result existed.
    [<Literal>]
    let TransportError = "transport_error"

    // Produced by the main process when the DataHub ruleset refuses a storage policy change.
    // The renderer checks the same rules before calling.
    [<Literal>]
    let StoragePolicyBlocked = "storage_policy_blocked"

    /// Recovery action codes, all produced by the library.
    module Recovery =

        [<Literal>]
        let RefreshConflictSession = "refresh_conflict_session"

        [<Literal>]
        let ResolveConflictSession = "resolve_conflict_session"

        [<Literal>]
        let RetryMaterialization = "retry_materialization"

        [<Literal>]
        let RemoveIndexLock = "remove_index_lock"

        [<Literal>]
        let RestoreWorkspace = "restore_workspace"

        [<Literal>]
        let RefreshWorkspace = "refresh_workspace"

        [<Literal>]
        let InspectWorkspace = "inspect_workspace"

        [<Literal>]
        let ReopenWorkspace = "reopen_workspace"

        [<Literal>]
        let CheckDependencies = "check_dependencies"

        [<Literal>]
        let AbortMerge = "abort_merge"

        [<Literal>]
        let RemoveCloneTarget = "remove_clone_target"

        [<Literal>]
        let AcceptUpdateRisks = "accept_update_risks"

        [<Literal>]
        let ResolveLocalChanges = "resolve_local_changes"

        [<Literal>]
        let RetryPublish = "retry_publish"

[<StringEnum(CaseRules.None)>]
type RefKindDto =
    | Local
    | Remote

type LogicalRefDto = {
    Name: string
    ProviderRef: string
    Kind: RefKindDto
    IsCurrent: bool
}

[<StringEnum(CaseRules.None)>]
type FileChangeKindDto =
    | Added
    | Modified
    | Deleted
    | Renamed
    | Conflicted

type FileChangeDto = {
    Path: string
    OldPath: string option
    Kind: FileChangeKindDto
}

[<StringEnum(CaseRules.None)>]
type RevisionRelationshipDto =
    | UpToDate
    | LocalAhead
    | TargetAhead
    | Diverged
    | NoTarget
    | Unknown

type SynchronizationStateDto = {
    BaseRevision: string option
    WorkspaceRevision: string option
    TargetRevision: string option
    TargetRef: LogicalRefDto option
    LocalRevisionCount: int option
    TargetRevisionCount: int option
    RemoteChangedPaths: string[] option
    Relationship: RevisionRelationshipDto
}

type ConflictSessionHandleDto = { SessionId: string; Version: string }

[<RequireQualifiedAccess>]
type ContentViewDto =
    | Text of content: string
    | Unsupported of reason: string option

type ConflictCandidateObjectDto = {
    SizeBytes: float option
    ObjectId: string option
    IsLocallyAvailable: bool
}

type ConflictCandidateDto = {
    CandidateId: string
    Label: string
    Revision: string option
    Preview: ContentViewDto option
    Object: ConflictCandidateObjectDto option
}

type ConflictItemDto = {
    Path: string
    Candidates: ConflictCandidateDto[]
    CombinedPreview: ContentViewDto option
    SupportsResolvedContent: bool
}

type ConflictSessionSummaryDto = {
    Handle: ConflictSessionHandleDto
    Items: ConflictItemDto[]
}

type ConflictResolutionOutcomeDto = {
    RefreshedHandle: ConflictSessionHandleDto
    RemainingItems: ConflictItemDto[]
}

type WorkspaceStatusDto = {
    CurrentRef: LogicalRefDto option
    WorkspaceVersion: string
    Changes: FileChangeDto[]
    ActiveConflictSession: ConflictSessionSummaryDto option
    Synchronization: SynchronizationStateDto option
}

type SwitchPreflightDto = { PathsAtRisk: string[]; IsSafe: bool }

/// Materialization state of one large object whose content may not be downloaded yet.
type ObjectStateDto = {
    Path: string
    IsMaterialized: bool
    IsLocallyAvailable: bool
    SizeBytes: float option
    ObjectId: string option
}

type StoragePolicySettingsDto = {
    AutoPolicyThresholdMb: int option
    MaterializeLargeObjects: bool
}

type DependencyStatusDto = {
    Component: string
    Installed: bool
    Version: string option
    Compatible: bool
    Remediation: string option
}

type RepositoryLocationDto = {
    ProviderId: string
    DisplayName: string option
    ProviderLocation: string
    ConnectionProfileId: string option
}

/// Which optional services the open session offers. The renderer reads Synchronization and
/// RepositoryBrowser. A call to an absent service answers service_unavailable.
type ServiceAvailabilityDto = {
    Synchronization: bool
    TextDiff: bool
    ConflictResolution: bool
    ObjectMaterialization: bool
    StoragePolicy: bool
    Maintenance: bool
    RepositoryBrowser: bool
}

type WorkspaceSessionInfoDto = {
    SessionId: string
    ProviderId: string
    WorkspaceRoot: string
    Location: RepositoryLocationDto option
    Services: ServiceAvailabilityDto
}

/// A request without payload. Every call carries an operation id so it can be canceled, and the
/// same record names a running operation in cancelOperation and versionControlOperationStarted.
type OperationRequestDto = { OperationId: string }

type CloneWorkspaceRequestDto = {
    OperationId: string
    /// Nonsecret repository location text (a clone URL for Git repositories).
    ProviderLocation: string
    DisplayName: string option
    TargetPath: string
    TargetRef: string option
    MaterializeAllObjects: bool
}

type InitializeWorkspaceRequestDto = {
    OperationId: string
    TargetPath: string
}

type BindWorkspaceRequestDto = {
    OperationId: string
    ProviderLocation: string
    DisplayName: string option
}

type CreateRefRequestDto = {
    OperationId: string
    Name: string
    BaseRef: string option
    SwitchTo: bool
    ExpectedWorkspaceVersion: string
}

type SwitchRefRequestDto = {
    OperationId: string
    TargetRef: string
    ExpectedWorkspaceVersion: string
}

type CreateRevisionRequestDto = {
    OperationId: string
    Message: string
    Paths: string[]
    ExpectedWorkspaceVersion: string
}

type RestorePathsRequestDto = {
    OperationId: string
    Paths: string[]
    ExpectedWorkspaceVersion: string
}

type ObjectPathRequestDto = {
    OperationId: string
    Path: string
    /// How the file tree is refreshed after a materialization or a dematerialization. None
    /// refreshes when the result changed state. Some true always refreshes. Some false skips
    /// the refresh when the result is Succeeded and keeps it for a partial or failed result,
    /// because earlier objects of a loop may have changed files on disk. Diff and content
    /// reads ignore the field.
    RefreshTree: bool option
}

/// One synchronization: refresh, update when the target is ahead, publish unless
/// PublishLocalRevisions is false. ExpectedTargetRevision pins an accepted update to the
/// target revision the user saw.
type SynchronizeRequestDto = {
    OperationId: string
    ExpectedWorkspaceVersion: string
    ExpectedTargetRevision: string option
    AcceptUpdateRisks: bool
    PublishLocalRevisions: bool
}

[<RequireQualifiedAccess>]
type ConflictResolutionDto =
    | PickCandidate of candidateId: string
    | SupplyResolvedContent of content: string

type ResolveConflictRequestDto = {
    OperationId: string
    Handle: ConflictSessionHandleDto
    ExpectedWorkspaceVersion: string
    Path: string
    Resolution: ConflictResolutionDto
}

type FinalizeConflictRequestDto = {
    OperationId: string
    Handle: ConflictSessionHandleDto
    ExpectedWorkspaceVersion: string
    Message: string option
}

type CancelConflictRequestDto = {
    OperationId: string
    Handle: ConflictSessionHandleDto
    ExpectedWorkspaceVersion: string
}

type PathStoragePolicyRequestDto = {
    OperationId: string
    Path: string
    UseLargeObjectStorage: bool
}

type StoragePolicySettingsRequestDto = {
    OperationId: string
    Settings: StoragePolicySettingsDto
}

type InstallDependencyRequestDto = {
    OperationId: string
    Component: string
}

module OperationResultDto =

    /// The value of a successful or partially successful result.
    let tryValue (result: OperationResultDto<'T>) : 'T option =
        match result with
        | OperationResultDto.Succeeded outcome
        | OperationResultDto.PartiallySucceeded(outcome, _) -> Some outcome.Value
        | OperationResultDto.Failed _ -> None
