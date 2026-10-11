module internal Swate.Components.Page.GitHistory.Tests

open Vitest
open Swate.Components.Page.GitHistory.Types

module private Helpers =

    let revision = String.replicate 40 "a"

    let commit summary : GitHistoryCommit = {
        Revision = revision
        ParentRevisions = [||]
        Message = "Review metadata"
        AuthorName = "Maya Chen"
        AuthorEmail = "maya@example.org"
        AuthoredAt = "2026-10-10T12:00:00Z"
        CommittedAt = "2026-10-10T12:00:00Z"
        Summary = summary
    }

    let render summary changes =
        RTL.render (GitHistory.GitHistory([| commit summary |], changes, [||], ignore, (fun _ _ -> ()), ignore, ignore))
        |> ignore

    let badge label =
        RTL.screen.getByRole ("img", ByRoleOptions(name = Text label))

    let details files loading error : GitHistoryCommitChanges[] = [|
        {
            Revision = revision
            Files = files
            Loading = loading
            Error = error
        }
    |]

Vitest.afterEach (fun () -> RTL.cleanup ())

Vitest.test (
    "collapsed commits expose all six change badges before details are loaded",
    fun () ->
        Helpers.render
            (Some {
                Added = 1
                Deleted = 2
                Modified = 3
                Renamed = 4
                Copied = 5
                TypeChanged = 6
            })
            [||]

        let expected = [|
            "1 added file", "A 1", "swt:text-success"
            "2 deleted files", "D 2", "swt:text-error"
            "3 modified files", "M 3", "swt:text-warning"
            "4 renamed files", "R 4", "swt:text-info"
            "5 copied files", "C 5", "swt:text-secondary"
            "6 type changed files", "T 6", "swt:text-accent"
        |]

        for label, text, color in expected do
            let badge = Helpers.badge label
            Vitest.expect(badge.textContent).toBe text
            Vitest.expect(badge.getAttribute "title").toBe label
            Vitest.expect(badge.className.Contains color).toBe true

        let expand =
            RTL.screen.getByRole ("button", ByRoleOptions(name = Text "Expand saved version: Review metadata"))

        Vitest.expect(expand.getAttribute "aria-expanded").toBe "false"
        Vitest.expect(RTL.screen.getAllByRole("img").Count).toBe 6
)

Vitest.test (
    "zero categories are omitted and an empty summary has a neutral zero-files badge",
    fun () ->
        Helpers.render
            (Some {
                GitHistoryChangeSummary.empty with
                    Modified = 2
            })
            [||]

        Vitest.expect((Helpers.badge "2 modified files").textContent).toBe "M 2"
        Vitest.expect(RTL.screen.getAllByRole("img").Count).toBe 1

        RTL.cleanup ()
        Helpers.render (Some GitHistoryChangeSummary.empty) [||]
        Vitest.expect((Helpers.badge "0 changed files").textContent).toBe "0 files"
        Vitest.expect((Helpers.badge "0 changed files").className.Contains "swt:bg-base-200").toBe true
)

Vitest.test (
    "missing summaries use successfully loaded details as a fallback",
    fun () ->
        let file: GitHistoryFileChange = {
            Path = "renamed.md"
            PreviousPath = Some "original.md"
            Kind = GitHistoryChangeKind.Renamed
            Insertions = None
            Deletions = None
        }

        Helpers.render None (Helpers.details [| file |] false None)
        Vitest.expect((Helpers.badge "1 renamed file").textContent).toBe "R 1"
        Vitest.expect(RTL.screen.getAllByRole("img").Count).toBe 1

        RTL.cleanup ()
        Helpers.render None (Helpers.details [||] false None)
        Vitest.expect((Helpers.badge "0 changed files").textContent).toBe "0 files"
)

Vitest.test (
    "unknown, loading and failed summaries remain visibly unavailable",
    fun () ->
        for changes in
            [|
                [||]
                Helpers.details [||] true None
                Helpers.details [||] false (Some "Unavailable")
            |] do
            Helpers.render None changes
            Vitest.expect((Helpers.badge "File change summary unavailable").textContent).toBe "Counts unavailable"
            Vitest.expect(RTL.screen.getAllByRole("img").Count).toBe 1
            RTL.cleanup ()
)

Vitest.test (
    "available commit summaries take precedence even while details fail",
    fun () ->
        Helpers.render
            (Some {
                GitHistoryChangeSummary.empty with
                    Added = 7
            })
            (Helpers.details [||] false (Some "Unavailable"))

        Vitest.expect((Helpers.badge "7 added files").textContent).toBe "A 7"
)
