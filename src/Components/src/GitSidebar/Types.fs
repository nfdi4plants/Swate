namespace Swate.Components.GitSidebarTypes

open Fable.Core

[<StringEnum(CaseRules.None)>]
type GitSidebarBranchKind =
    | Local
    | Remote

type GitSidebarStatus = {
    CurrentBranch: string option
    TrackingBranch: string option
    Ahead: int
    Behind: int
    IsClean: bool
    IsMergeInProgress: bool
}

type GitSidebarChange = {
    Path: string
    OriginalPath: string option
    IndexStatus: string
    WorkingTreeStatus: string
    IsConflicted: bool
}

type GitSidebarBranchOption = {
    RefName: string
    DisplayLabel: string
    Kind: GitSidebarBranchKind
    IsCurrent: bool
    IsTracking: bool
}

type GitSidebarProgress = {
    Method: string option
    Stage: string option
    ProgressPercent: float option
}

type GitSidebarCreateBranchRequest = {
    BranchName: string
    StartPoint: string option
}
