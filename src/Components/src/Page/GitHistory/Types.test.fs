module internal Swate.Components.Page.GitHistory.SummaryTests

open Vitest
open Swate.Components.Page.GitHistory.Types

Vitest.test (
    "counts every file status once regardless of line counts and rename source",
    fun () ->
        let kinds = [|
            GitHistoryChangeKind.Added
            GitHistoryChangeKind.Deleted
            GitHistoryChangeKind.Modified
            GitHistoryChangeKind.Renamed
            GitHistoryChangeKind.Copied
            GitHistoryChangeKind.TypeChanged
            GitHistoryChangeKind.Modified
        |]

        let files =
            kinds
            |> Array.mapi (fun index kind -> {
                Path = $"file-{index}"
                PreviousPath = Some "original.txt"
                Kind = kind
                Insertions = if index % 2 = 0 then None else Some 100
                Deletions = if index % 2 = 0 then None else Some 0
            })

        Vitest.expect(GitHistoryChangeSummary.ofChanges files).toEqual {
            Added = 1
            Deleted = 1
            Modified = 2
            Renamed = 1
            Copied = 1
            TypeChanged = 1
        }
)

Vitest.test (
    "an empty changed-file list has six zero counts",
    fun () -> Vitest.expect(GitHistoryChangeSummary.ofChanges [||]).toEqual GitHistoryChangeSummary.empty
)
