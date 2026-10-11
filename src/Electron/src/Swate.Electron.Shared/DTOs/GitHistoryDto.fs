module Swate.Electron.Shared.DTOs.GitHistoryDto

open Swate.Components.Page.GitHistory.Types

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

/// Immutable source metadata for the shared worker-backed diff pipeline.
type GitHistoryDiffDto = {
    Revision: string
    ParentRevision: string option
    Path: string
    PreviousPath: string option
    Kind: GitHistoryChangeKind
}
