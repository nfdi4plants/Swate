module Swate.Electron.Shared.DTOs.GitHistoryDto

open Swate.Components.Page.GitHistory.Types
open Swate.Components.Page.GitComparison.GitPagedDiffTypes

type GitHistoryRequest = {
    /// Subsequent pages stay pinned to the first page's HEAD.
    HeadRevision: string option
    Skip: int
    PageSize: int
}

type GitHistoryPageDto = {
    BranchName: string option
    HeadRevision: string option
    Commits: GitHistoryCommit[]
    NextSkip: int option
}

type GitHistoryRevisionRequest = { Revision: string }

type GitHistoryDiffRequest = { Revision: string; Path: string }

/// A committed patch, rendered by the same paged viewer as workspace changes.
type GitHistoryDiffDto = {
    Revision: string
    ParentRevision: string option
    Path: string
    PreviousPath: string option
    Kind: GitHistoryChangeKind
    Parts: PagedPart[]
    BlockedReason: string option
}
