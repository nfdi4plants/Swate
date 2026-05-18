namespace Swate.Components.Page.GitSidebarTypes

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

[<RequireQualifiedAccess>]
type GitSidebarRunStatus =
    | Idle
    | Busy of notice: string
    | Progress of GitSidebarProgress

type GitSidebarCreateBranchRequest = {
    BranchName: string
    StartPoint: string option
}

type GitSidebarCommitSelectionRequest = { Message: string; Paths: string[] }

type GitSidebarConfirmationDialog = {
    Title: string
    Message: string
    ConfirmLabel: string
    CancelLabel: string
}

type GitSidebarCallbacks = {
    OnRefresh: unit -> unit
    OnFetch: unit -> unit
    OnPull: unit -> unit
    OnPush: unit -> unit
    OnUpdateFromOnline: unit -> unit
    OnPrimarySaveSelection: GitSidebarCommitSelectionRequest -> unit
    OnPrimarySaveAll: string -> unit
    OnCommitSelection: GitSidebarCommitSelectionRequest -> unit
    OnCommitAll: string -> unit
    OnDiscardSelection: string[] -> unit
    OnConfirmPendingRemoteAction: unit -> unit
    OnCancelPendingRemoteAction: unit -> unit
    OnSaveDownloadLargeFiles: bool -> unit
    OnSaveLfsAutoTrackThreshold: int -> unit
    OnCreateBranch: GitSidebarCreateBranchRequest -> unit
    OnSwitchBranch: string -> unit
    OnSelectChange: GitSidebarChange -> JS.Promise<Result<unit, string>>
    OnPruneLfsCache: unit -> unit
    OnDedupLfsStorage: unit -> unit
}

[<RequireQualifiedAccess>]
module GitStatusCode =

    let normalize (code: string) =
        let trimmed = code |> Option.ofObj |> Option.defaultValue String.Empty |> _.Trim()

        if String.IsNullOrWhiteSpace trimmed then "." else trimmed

    let isStagedIndexStatus (code: string) =
        let normalized = normalize code
        normalized <> "." && normalized <> "?"
