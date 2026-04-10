module Swate.Electron.Shared.GitTypes

open Fable.Core

[<Literal>]
let GitLfsSkipSmudgeEnvKey = "GIT_LFS_SKIP_SMUDGE"

// GIT LFS Types
type GitLfsCommand =
    | Pull
    | Fetch
    | Install
    | Track
    | Untrack
    | Status

type GitLfsRequest = {
    RequestId: string
    RepoPath: string
    Command: GitLfsCommand
    FilePath: string option
    TimeoutMs: int option
}

type GitLfsResult = {
    Success: bool
    Output: string
    Error: string
}

[<RequireQualifiedAccess>]
type GitFailureKind =
    | Unauthorized
    | Forbidden
    | Network
    | Timeout
    | Canceled
    | LfsInstallRequired
    | Unknown

[<StringEnum(CaseRules.None)>]
type GitBranchRefKind =
    | Local
    | Remote

type GitFileStatusDto = {
    Path: string
    Index: string
    WorkingDir: string
    OriginalPath: string option
}

type GitBranchRefDto = {
    RefName: string
    DisplayLabel: string
    Kind: GitBranchRefKind
    IsCurrent: bool
    IsTracking: bool
}

type GitDiffViewDataDto = {
    Path: string
    PreviousContent: string
    CurrentContent: string
    WordDiffText: string
}

type GitMergeConflictViewDataDto = {
    Path: string
    MergeConflictContent: string
}

type GitUnsupportedContentDto = {
    Path: string
    Reason: string option
}

[<RequireQualifiedAccess>]
type GitPageLoadResultDto<'T> =
    | Loaded of 'T
    | Unsupported of GitUnsupportedContentDto

type GitStatusDto = {
    Current: string option
    Tracking: string option
    Ahead: int
    Behind: int
    IsClean: bool
    Conflicted: string[]
    IsMergeInProgress: bool
    Files: GitFileStatusDto[]
}

type GitDiffSummaryDto = {
    Changed: int
    Insertions: int
    Deletions: int
}

type GitOperationResult = {
    Success: bool
    Message: string option
    FailureKind: GitFailureKind option
    WarningMessage: string option
    WarningKind: GitFailureKind option
    Path: string option
}

type GitProgressDto = {
    Method: string option
    Stage: string option
    Progress: float option
    Processed: float option
    Total: float option
}

type GitRemoteOperationRequest = {
    Remote: string option
    Branch: string option
}

type GitCloneRepositoryRequest = {
    RemoteUrl: string
    TargetPath: string
    Branch: string option
    DownloadLargeFiles: bool
}

type GitPathspecRequest = { Pathspecs: string[] }

type GitCommitRequest = { Message: string }

type GitLfsSettingsDto = {
    AutoTrackThresholdMb: int
    DownloadLargeFiles: bool
}

type GitCreateBranchRequest = {
    Name: string
    StartPoint: string option
}

type GitCheckoutBranchRequest = { Name: string }

type GitConfirmMergeResolutionRequest = {
    Path: string
    ExpectedConflictContent: string
    ResolvedContent: string
    AutoCommit: bool
}

type GitConfirmMergeResolutionResult = {
    UpdatedStatus: GitStatusDto
    RemainingConflictedPaths: string[]
    NextConflictedPath: string option
}
