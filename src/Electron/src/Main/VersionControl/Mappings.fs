/// Structural mappings between the library's abstractions and the IPC DTOs. Nothing in
/// here reads a message text. Failures keep their category, code, state flags, paths,
/// recovery action and revision evidence, and results keep their three shapes.
module Main.VersionControl.Mappings

open Swate.Electron.Shared.VersionControlTypes
open VersionControlService.Abstractions

let failureCategory (category: FailureCategory) : FailureCategoryDto =
    match category with
    | FailureCategory.Validation -> FailureCategoryDto.Validation
    | FailureCategory.NotFound -> FailureCategoryDto.NotFound
    | FailureCategory.Concurrency -> FailureCategoryDto.Concurrency
    | FailureCategory.Authentication -> FailureCategoryDto.Authentication
    | FailureCategory.Authorization -> FailureCategoryDto.Authorization
    | FailureCategory.DependencyMissing -> FailureCategoryDto.DependencyMissing
    | FailureCategory.Network -> FailureCategoryDto.Network
    | FailureCategory.Timeout -> FailureCategoryDto.Timeout
    | FailureCategory.Canceled -> FailureCategoryDto.Canceled
    | FailureCategory.Conflict -> FailureCategoryDto.Conflict
    | FailureCategory.Unsupported -> FailureCategoryDto.Unsupported
    | FailureCategory.ProviderError -> FailureCategoryDto.ProviderError

let recoveryAction (action: RecoveryAction) : RecoveryActionDto = {
    Code = action.Code
    Instructions = action.Instructions
}

let failure (failure: OperationFailure) : OperationFailureDto = {
    Category = failureCategory failure.Category
    Code = failure.Code
    Message = failure.Message
    StateChanged = failure.StateChanged
    Retryable = failure.Retryable
    AffectedPaths = Array.copy failure.AffectedPaths
    RecoveryAction = failure.RecoveryAction |> Option.map recoveryAction
    Details = Array.copy failure.Details
    RevisionEvidence =
        failure.RevisionEvidence
        |> Array.map (fun (label, revision) -> {
            Label = label
            Revision = RevisionId.value revision
        })
}

let warning (warning: OperationWarning) : OperationWarningDto = {
    Code = warning.Code
    Message = warning.Message
}

let effect (effect: OperationEffect) : OperationEffectDto =
    match effect with
    | Performed -> OperationEffectDto.Performed
    | NoOp reason -> OperationEffectDto.NoOp reason

let publication (state: PublicationState) : PublicationStateDto =
    match state with
    | PublicationNotApplicable -> PublicationStateDto.NotApplicable
    | LocalOnly -> PublicationStateDto.LocalOnly
    | Published -> PublicationStateDto.Published

let outcome (mapValue: 'T -> 'U) (outcome: OperationOutcome<'T>) : OperationOutcomeDto<'U> = {
    Value = mapValue outcome.Value
    Effect = effect outcome.Effect
    Warnings = outcome.Warnings |> Array.map warning
    AffectedPaths = Array.copy outcome.AffectedPaths
    ResultingRevision = outcome.ResultingRevision |> Option.map RevisionId.value
    ResultingWorkspaceVersion = outcome.ResultingWorkspaceVersion
    Publication = publication outcome.Publication
}

let result (mapValue: 'T -> 'U) (result: OperationResult<'T>) : OperationResultDto<'U> =
    match result with
    | Succeeded value -> OperationResultDto.Succeeded(outcome mapValue value)
    | PartiallySucceeded(value, error) -> OperationResultDto.PartiallySucceeded(outcome mapValue value, failure error)
    | Failed error -> OperationResultDto.Failed(failure error)

let progress (sessionId: string) (operationId: string) (progress: OperationProgress) : VersionControlProgressDto = {
    SessionId = sessionId
    OperationId = operationId
    PhaseCode = progress.PhaseCode
    Item = progress.Item
    Completed = progress.Completed
    Total = progress.Total
    DisplayMessage = progress.DisplayMessage
}

let logicalRef (reference: LogicalRef) : LogicalRefDto = {
    Name = reference.Name
    ProviderRef = ProviderRef.value reference.ProviderRef
    Kind =
        match reference.Kind with
        | LocalRef -> RefKindDto.Local
        | RemoteRef -> RefKindDto.Remote
    IsCurrent = reference.IsCurrent
}

let fileChangeKind (kind: FileChangeKind) : FileChangeKindDto =
    match kind with
    | AddedChange -> FileChangeKindDto.Added
    | ModifiedChange -> FileChangeKindDto.Modified
    | DeletedChange -> FileChangeKindDto.Deleted
    | RenamedChange -> FileChangeKindDto.Renamed
    | ConflictedChange -> FileChangeKindDto.Conflicted

let fileChange (change: FileChange) : FileChangeDto = {
    Path = RepositoryPath.value change.Path
    OldPath = change.OldPath |> Option.map RepositoryPath.value
    Kind = fileChangeKind change.Kind
}

let relationship (relationship: RevisionRelationship) : RevisionRelationshipDto =
    match relationship with
    | UpToDate -> RevisionRelationshipDto.UpToDate
    | LocalAhead -> RevisionRelationshipDto.LocalAhead
    | TargetAhead -> RevisionRelationshipDto.TargetAhead
    | Diverged -> RevisionRelationshipDto.Diverged
    | NoTarget -> RevisionRelationshipDto.NoTarget
    | UnknownRelationship -> RevisionRelationshipDto.Unknown

let synchronizationState (state: SynchronizationState) : SynchronizationStateDto = {
    BaseRevision = state.BaseRevision |> Option.map RevisionId.value
    WorkspaceRevision = state.WorkspaceRevision |> Option.map RevisionId.value
    TargetRevision = state.TargetRevision |> Option.map RevisionId.value
    TargetRef = state.TargetRef |> Option.map logicalRef
    LocalRevisionCount = state.LocalRevisionCount
    TargetRevisionCount = state.TargetRevisionCount
    RemoteChangedPaths = state.RemoteChangedPaths |> Option.map (Array.map RepositoryPath.value)
    Relationship = relationship state.Relationship
}

let conflictHandle (handle: ConflictSessionHandle) : ConflictSessionHandleDto = {
    SessionId = handle.SessionId
    Version = handle.Version
}

let conflictPreview (preview: ConflictPreview) : ContentViewDto =
    match preview with
    | TextPreview content -> ContentViewDto.Text content
    | UnsupportedPreview reason -> ContentViewDto.Unsupported reason

let contentView (view: ContentView) : ContentViewDto =
    match view with
    | TextContent content -> ContentViewDto.Text content
    | UnsupportedContent reason -> ContentViewDto.Unsupported reason

let conflictItem (item: ConflictItem) : ConflictItemDto = {
    Path = RepositoryPath.value item.Path
    Candidates =
        item.Candidates
        |> Array.map (fun candidate -> {
            CandidateId = candidate.CandidateId
            Label = candidate.Label
            Revision = candidate.Revision |> Option.map RevisionId.value
            Preview = candidate.Preview |> Option.map conflictPreview
        })
    CombinedPreview = item.CombinedPreview |> Option.map conflictPreview
    SupportsResolvedContent = item.SupportsResolvedContent
}

let conflictSession (summary: ConflictSessionSummary) : ConflictSessionSummaryDto = {
    Handle = conflictHandle summary.Handle
    Items = summary.Items |> Array.map conflictItem
}

let conflictOutcome (outcome: ConflictResolutionOutcome) : ConflictResolutionOutcomeDto = {
    RefreshedHandle = conflictHandle outcome.RefreshedHandle
    RemainingItems = outcome.RemainingItems |> Array.map conflictItem
}

let workspaceStatus (status: WorkspaceStatus) : WorkspaceStatusDto = {
    CurrentRef = status.CurrentRef |> Option.map logicalRef
    WorkspaceVersion = status.WorkspaceVersion
    Changes = status.Changes |> Array.map fileChange
    ActiveConflictSession = status.ActiveConflictSession |> Option.map conflictSession
    Synchronization = status.Synchronization |> Option.map synchronizationState
}

let switchPreflight (preflight: SwitchPreflight) : SwitchPreflightDto = {
    PathsAtRisk = preflight.PathsAtRisk |> Array.map RepositoryPath.value
    IsSafe = preflight.IsSafe
}

let diffSummary (summary: DiffSummary) : DiffSummaryDto = {
    Entries =
        summary.Entries
        |> Array.map (fun entry -> {
            Path = RepositoryPath.value entry.Path
            OldPath = entry.OldPath |> Option.map RepositoryPath.value
            Kind = fileChangeKind entry.Kind
            LineInsertions = entry.LineInsertions
            LineDeletions = entry.LineDeletions
        })
}

let objectState (state: ObjectState) : ObjectStateDto = {
    Path = RepositoryPath.value state.Path
    IsMaterialized = state.IsMaterialized
    IsLocallyAvailable = state.IsLocallyAvailable
    SizeBytes = state.SizeBytes
    ObjectId = state.ObjectId
}

let dependencyStatus (status: DependencyStatus) : DependencyStatusDto = {
    Component = status.Component
    Installed = status.Installed
    Version = status.Version
    Compatible = status.Compatible
    Remediation = status.Remediation
}

let repositoryLocation (location: RepositoryLocation) : RepositoryLocationDto = {
    ProviderId = ProviderId.value location.ProviderId
    DisplayName = location.DisplayName
    ProviderLocation = location.ProviderLocation
    ConnectionProfileId = location.ConnectionProfileId
}

let serviceAvailability (session: WorkspaceSession) : ServiceAvailabilityDto = {
    Synchronization = session.Synchronization.IsSome
    TextDiff = session.TextDiff.IsSome
    ConflictResolution = session.ConflictResolution.IsSome
    ObjectMaterialization = session.ObjectMaterialization.IsSome
    StoragePolicy = session.StoragePolicy.IsSome
    Maintenance = session.Maintenance.IsSome
    RepositoryBrowser = session.RepositoryBrowser.IsSome
}

let sessionInfo (sessionId: string) (session: WorkspaceSession) : WorkspaceSessionInfoDto = {
    SessionId = sessionId
    ProviderId = ProviderId.value session.Descriptor.ProviderId
    WorkspaceRoot = session.Descriptor.WorkspaceRoot
    Location = session.Descriptor.Location |> Option.map repositoryLocation
    Services = serviceAvailability session
}

let conflictHandleFromDto (handle: ConflictSessionHandleDto) : ConflictSessionHandle = {
    SessionId = handle.SessionId
    Version = handle.Version
}

let conflictResolutionFromDto (resolution: ConflictResolutionDto) : ConflictResolution =
    match resolution with
    | ConflictResolutionDto.PickCandidate candidateId -> PickCandidate candidateId
    | ConflictResolutionDto.SupplyResolvedContent content -> SupplyResolvedContent content

/// A validation failure for a path the renderer sent that the library would reject.
let invalidPathFailure (path: string) (message: string) : OperationFailure = {
    OperationFailure.create Validation VersionControlCodes.InvalidPath message with
        AffectedPaths = [| path |]
}

let tryRepositoryPath (path: string) : Result<RepositoryPath, OperationFailure> =
    RepositoryPath.tryCreate path |> Result.mapError (invalidPathFailure path)

/// Converts every path or fails on the first invalid one without touching the provider.
let tryRepositoryPaths (paths: string[]) : Result<RepositoryPath[], OperationFailure> =
    paths
    |> Array.fold
        (fun state path ->
            match state with
            | Error error -> Error error
            | Ok converted ->
                match tryRepositoryPath path with
                | Ok repositoryPath -> Ok(Array.append converted [| repositoryPath |])
                | Error error -> Error error
        )
        (Ok [||])

let tryProviderRef (value: string) : Result<ProviderRef, OperationFailure> =
    ProviderRef.tryCreate value
    |> Result.mapError (OperationFailure.create Validation VersionControlCodes.InvalidRef)

let tryRevisionId (value: string) : Result<RevisionId, OperationFailure> =
    RevisionId.tryCreate value
    |> Result.mapError (OperationFailure.create Validation VersionControlCodes.InvalidRevision)
