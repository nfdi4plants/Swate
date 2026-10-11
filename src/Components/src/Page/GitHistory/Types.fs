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

type GitHistoryChangeSummary = {
    Added: int
    Deleted: int
    Modified: int
    Renamed: int
    Copied: int
    TypeChanged: int
}

type GitHistoryCommit = {
    Revision: string
    ParentRevisions: string[]
    Message: string
    AuthorName: string
    AuthorEmail: string
    AuthoredAt: string
    CommittedAt: string
    Summary: GitHistoryChangeSummary option
}

type GitHistoryFileChange = {
    Path: string
    PreviousPath: string option
    Kind: GitHistoryChangeKind
    Insertions: int option
    Deletions: int option
}

module GitHistoryChangeSummary =

    let empty: GitHistoryChangeSummary = {
        Added = 0
        Deleted = 0
        Modified = 0
        Renamed = 0
        Copied = 0
        TypeChanged = 0
    }

    /// Counts file statuses independently of optional line statistics.
    let ofChanges (changes: GitHistoryFileChange[]) =
        changes
        |> Array.fold
            (fun summary change ->
                match change.Kind with
                | GitHistoryChangeKind.Added -> {
                    summary with
                        Added = summary.Added + 1
                  }
                | GitHistoryChangeKind.Deleted -> {
                    summary with
                        Deleted = summary.Deleted + 1
                  }
                | GitHistoryChangeKind.Modified -> {
                    summary with
                        Modified = summary.Modified + 1
                  }
                | GitHistoryChangeKind.Renamed -> {
                    summary with
                        Renamed = summary.Renamed + 1
                  }
                | GitHistoryChangeKind.Copied -> {
                    summary with
                        Copied = summary.Copied + 1
                  }
                | GitHistoryChangeKind.TypeChanged -> {
                    summary with
                        TypeChanged = summary.TypeChanged + 1
                  }
            )
            empty

/// The host keeps loaded file lists while browsing commits and their comparisons.
type GitHistoryCommitChanges = {
    Revision: string
    Files: GitHistoryFileChange[]
    Loading: bool
    Error: string option
}
