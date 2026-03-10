module Swate.Electron.Shared.GitTypes


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
    | Unknown

type GitFileStatusDto = {
    Path: string
    Index: string
    WorkingDir: string
    OriginalPath: string option
}

type GitStatusDto = {
    Current: string option
    Tracking: string option
    Ahead: int
    Behind: int
    IsClean: bool
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
}

type GitPathspecRequest = { Pathspecs: string[] }

type GitCommitRequest = { Message: string }

type GitCreateBranchRequest = {
    Name: string
    StartPoint: string option
}

type GitCheckoutBranchRequest = { Name: string }