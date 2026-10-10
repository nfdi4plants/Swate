module Swate.Components.Page.GitHistory.Types

open Fable.Core

[<StringEnum; RequireQualifiedAccess>]
type GitHistoryChangeKind =
    | Added
    | Modified
    | Deleted
    | Renamed
    | Copied
    | TypeChanged

type GitHistoryCommit = {
    Revision: string
    ParentRevisions: string[]
    Message: string
    AuthorName: string
    AuthorEmail: string
    AuthoredAt: string
    CommittedAt: string
}

type GitHistoryFileChange = {
    Path: string
    PreviousPath: string option
    Kind: GitHistoryChangeKind
    Insertions: int option
    Deletions: int option
}

/// The host keeps loaded file lists while browsing commits and their comparisons.
type GitHistoryCommitChanges = {
    Revision: string
    Files: GitHistoryFileChange[]
    Loading: bool
    Error: string option
}
