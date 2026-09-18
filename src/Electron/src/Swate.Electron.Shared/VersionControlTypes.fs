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

/// Identity of one running operation. The renderer chooses the operation id, the main
/// process reports the session id it ran under.
type OperationKeyDto = {
    SessionId: string
    OperationId: string
}

type VersionControlProgressDto = {
    SessionId: string
    OperationId: string
    PhaseCode: string
    Item: string option
    Completed: float option
    Total: float option
    DisplayMessage: string option
}

/// Stable codes the renderer routes on, listed here so nothing spells them inline.
/// IdentityMissing through OperationCanceled and the Recovery codes are produced by the
/// library. ServiceUnavailable through LockRemovalRefused are produced by the Swate
/// main process (session host and IPC handler), StoragePolicyBlocked by the renderer
/// when the DataHub ruleset refuses a storage policy change.
module VersionControlCodes =

    [<Literal>]
    let IdentityMissing = "identity_missing"

    [<Literal>]
    let PublishTargetMissing = "publish_target_missing"

    [<Literal>]
    let TargetUnreachable = "target_unreachable"

    [<Literal>]
    let ConfiguredTargetInvalid = "configured_target_invalid"

    [<Literal>]
    let PreconditionFailed = "precondition_failed"

    [<Literal>]
    let ConflictsDetected = "conflicts_detected"

    [<Literal>]
    let ConflictSessionActive = "conflict_session_active"

    [<Literal>]
    let TargetNotEmpty = "target_not_empty"

    [<Literal>]
    let OperationCanceled = "operation_canceled"

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
    let StoragePolicyBlocked = "storage_policy_blocked"

    [<Literal>]
    let LockRemovalRefused = "lock_removal_refused"

    module Recovery =

        [<Literal>]
        let RefreshConflictSession = "refresh_conflict_session"

        [<Literal>]
        let ResolveConflictSession = "resolve_conflict_session"

        [<Literal>]
        let RetryMaterialization = "retry_materialization"

        [<Literal>]
        let ReconcileMaterialization = "reconcile_materialization"

        [<Literal>]
        let ReconcileIndex = "reconcile_index"

        [<Literal>]
        let RemoveIndexLock = "remove_index_lock"

        [<Literal>]
        let RestoreWorkspace = "restore_workspace"

        [<Literal>]
        let RefreshWorkspace = "refresh_workspace"

        [<Literal>]
        let InspectWorkspace = "inspect_workspace"

        [<Literal>]
        let AbortMerge = "abort_merge"

        [<Literal>]
        let RemoveCloneTarget = "remove_clone_target"

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

type ConflictCandidateDto = {
    CandidateId: string
    Label: string
    Revision: string option
    Preview: ContentViewDto option
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

type DiffEntryDto = {
    Path: string
    OldPath: string option
    Kind: FileChangeKindDto
    LineInsertions: int option
    LineDeletions: int option
}

type DiffSummaryDto = { Entries: DiffEntryDto[] }

type UpdatePreviewDto = {
    ChangedPaths: string[]
    OverlappingPaths: string[]
    HasDataLossRisk: bool
    WouldCreateConflictSession: bool
}

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

/// Which optional services the open session offers. The renderer enables matching
/// controls from this record and never calls an absent service.
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

/// A request without payload. Every call carries an operation id so it can be canceled.
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

type ObjectPathRequestDto = { OperationId: string; Path: string }

type UpdateRequestDto = {
    OperationId: string
    ExpectedWorkspaceVersion: string
}

type PublishRequestDto = {
    OperationId: string
    ExpectedWorkspaceVersion: string
    ExpectedTargetRevision: string option
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

    let map (mapping: 'T -> 'U) (result: OperationResultDto<'T>) : OperationResultDto<'U> =
        let mapOutcome (outcome: OperationOutcomeDto<'T>) : OperationOutcomeDto<'U> = {
            Value = mapping outcome.Value
            Effect = outcome.Effect
            Warnings = outcome.Warnings
            AffectedPaths = outcome.AffectedPaths
            ResultingRevision = outcome.ResultingRevision
            ResultingWorkspaceVersion = outcome.ResultingWorkspaceVersion
            Publication = outcome.Publication
        }

        match result with
        | OperationResultDto.Succeeded outcome -> OperationResultDto.Succeeded(mapOutcome outcome)
        | OperationResultDto.PartiallySucceeded(outcome, failure) ->
            OperationResultDto.PartiallySucceeded(mapOutcome outcome, failure)
        | OperationResultDto.Failed failure -> OperationResultDto.Failed failure

    /// The failure of a failed or partially successful result.
    let tryFailure (result: OperationResultDto<'T>) : OperationFailureDto option =
        match result with
        | OperationResultDto.Succeeded _ -> None
        | OperationResultDto.PartiallySucceeded(_, failure)
        | OperationResultDto.Failed failure -> Some failure

    /// The value of a successful or partially successful result.
    let tryValue (result: OperationResultDto<'T>) : 'T option =
        match result with
        | OperationResultDto.Succeeded outcome
        | OperationResultDto.PartiallySucceeded(outcome, _) -> Some outcome.Value
        | OperationResultDto.Failed _ -> None

    /// True for a canceled failure and for a partial success whose failure is the
    /// cancellation, such as a clone whose large-object download was canceled.
    let isCanceled (result: OperationResultDto<'T>) =
        tryFailure result
        |> Option.exists (fun failure -> failure.Category = FailureCategoryDto.Canceled)

    let recoveryCode (failure: OperationFailureDto) =
        failure.RecoveryAction |> Option.map _.Code
