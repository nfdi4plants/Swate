namespace Swate.Components.GitSidebarTypes

open System
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

type GitSidebarCommitSelectionRequest = {
    Message: string
    Paths: string[]
}

[<RequireQualifiedAccess>]
module GitStatusCode =

    let normalize (code: string) =
        let trimmed =
            code
            |> Option.ofObj
            |> Option.defaultValue String.Empty
            |> _.Trim()

        if String.IsNullOrWhiteSpace trimmed then
            "."
        else
            trimmed

    let isStagedIndexStatus (code: string) =
        let normalized = normalize code
        normalized <> "." && normalized <> "?"
