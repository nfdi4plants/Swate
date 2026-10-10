module ElectronCore.GitHistoryTests

open System
open Fable.Core
open Main
open Main.Bindings.Path
open Swate.Electron.Shared.IPCTypes
open Swate.Electron.Shared.DTOs.GitHistoryDto
open Swate.Components.Page.GitHistory.Types
open Swate.Components.Page.GitComparison.GitPagedDiffTypes
open Vitest
open ElectronCore.TestHelpers

// Exercise the native runner against the existing checkout. This suite never
// initializes, stages, commits or otherwise writes a Git repository.
[<Emit("process.cwd()")>]
let private workingDirectory () : string = jsNative

[<Emit("process.env[$0]")>]
let private getEnvironment (name: string) : string option = jsNative

[<Emit("if ($1 == null) delete process.env[$0]; else process.env[$0] = $1")>]
let private setEnvironment (name: string) (value: string option) : unit = jsNative

[<Import("rmdirSync", "node:fs")>]
let private removeEmptyDirectory (path: string) : unit = jsNative

let private withEnvironment values body = promise {
    let previous = values |> Array.map (fun (key, _) -> key, getEnvironment key)

    try
        values |> Array.iter (fun (key, value) -> setEnvironment key value)
        return! body ()
    finally
        previous |> Array.iter (fun (key, value) -> setEnvironment key value)
}

let private git root args =
    execFile "git" (Array.append [| "--no-optional-locks"; "--no-replace-objects" |] args) root

let private repositoryRoot () =
    git (workingDirectory ()) [| "rev-parse"; "--show-toplevel" |] |> _.Trim()

let private lines (value: string) =
    value.Split([| '\r'; '\n' |], StringSplitOptions.RemoveEmptyEntries)

let private value =
    function
    | Ok result -> result
    | Error message -> failwith message

let private failure =
    function
    | Error message -> message
    | Ok _ -> failwith "Expected the history request to fail."

let private withApi root body = promise {
    let windowId = 941
    registerVault windowId root |> ignore

    try
        return! body (Main.IPC.GitHistory.api (ipcEvent windowId))
    finally
        ARC_VAULTS.Vaults.Remove windowId |> ignore
}

let private firstPage: GitHistoryRequest = {
    HeadRevision = None
    Skip = 0
    PageSize = 2
}

let private expectedSummary root revision parent =
    let revisions =
        match parent with
        | Some previous -> [| previous; revision |]
        | None -> [| revision |]

    let output =
        git
            root
            (Array.concat [|
                [|
                    "diff-tree"
                    "--no-commit-id"
                    "-r"
                    "--root"
                    "--no-ext-diff"
                    "--no-textconv"
                    "--no-color"
                    "-M"
                    "-C"
                    "--name-status"
                    "-z"
                |]
                revisions
                [| "--" |]
            |])

    let fields = output.Split('\000')
    let mutable index = 0
    let mutable summary = GitHistoryChangeSummary.empty

    while index < fields.Length && fields[index] <> "" do
        let status = fields[index][0]

        summary <-
            match status with
            | 'A' -> {
                summary with
                    Added = summary.Added + 1
              }
            | 'D' -> {
                summary with
                    Deleted = summary.Deleted + 1
              }
            | 'M' -> {
                summary with
                    Modified = summary.Modified + 1
              }
            | 'R' -> {
                summary with
                    Renamed = summary.Renamed + 1
              }
            | 'C' -> {
                summary with
                    Copied = summary.Copied + 1
              }
            | 'T' -> {
                summary with
                    TypeChanged = summary.TypeChanged + 1
              }
            | _ -> failwith $"Unexpected Git status: {fields[index]}"

        index <- index + (if status = 'R' || status = 'C' then 3 else 2)

    summary

Vitest.describe (
    "Read-only Git history",
    fun () ->
        Vitest.test (
            "loads real commits and a pinned second page without changing repository state",
            fun () -> promise {
                let root = repositoryRoot ()

                let expected =
                    git root [|
                        "log"
                        "--topo-order"
                        "--format=%H"
                        "--max-count=4"
                        "HEAD"
                        "--"
                    |]
                    |> lines

                let before = git root [| "status"; "--porcelain=v1"; "-z" |]
                let indexPath = git root [| "rev-parse"; "--git-path"; "index" |] |> _.Trim()
                let! indexBefore = Main.Bindings.Filesystem.readFileBase64Async (resolve [| root; indexPath |])

                do!
                    withApi
                        root
                        (fun api -> promise {
                            let! response = api.listHistory firstPage
                            let page = value response
                            Vitest.expect(page.Commits |> Array.map _.Revision).toEqual (expected |> Array.truncate 2)
                            Vitest.expect(page.HeadRevision).toEqual (Some expected[0])
                            Vitest.expect(page.NextSkip).toEqual (if expected.Length > 2 then Some 2 else None)

                            Vitest
                                .expect(
                                    page.Commits
                                    |> Array.forall (fun commit -> commit.Message <> "" && commit.CommittedAt <> "")
                                )
                                .toBe
                                true

                            let! next =
                                api.listHistory {
                                    firstPage with
                                        HeadRevision = page.HeadRevision
                                        Skip = 2
                                }

                            let nextPage = value next
                            Vitest.expect(nextPage.Commits |> Array.map _.Revision).toEqual expected[2..3]

                            // Summaries are present on every page before expansion and
                            // match the independently loaded first-parent file details.
                            for commit in Array.append page.Commits nextPage.Commits do
                                let! changed = api.listChanges { Revision = commit.Revision }

                                Vitest
                                    .expect(commit.Summary)
                                    .toEqual (Some(GitHistoryChangeSummary.ofChanges (value changed)))
                        })

                let! indexAfter = Main.Bindings.Filesystem.readFileBase64Async (resolve [| root; indexPath |])
                Vitest.expect(indexAfter).toEqual indexBefore
                Vitest.expect(git root [| "rev-parse"; "HEAD" |] |> _.Trim()).toBe expected[0]
                Vitest.expect(git root [| "status"; "--porcelain=v1"; "-z" |]).toEqual before
            }
        )

        Vitest.test (
            "summarizes root commits against an empty tree and available merges against their first parent",
            fun () -> promise {
                let root = repositoryRoot ()

                let rootRevision =
                    git root [|
                        "log"
                        "--max-parents=0"
                        "--max-count=1"
                        "--format=%H"
                        "HEAD"
                        "--"
                    |]
                    |> lines
                    |> Array.head

                let mergeRevision =
                    git root [|
                        "log"
                        "--min-parents=2"
                        "--max-count=1"
                        "--format=%H"
                        "HEAD"
                        "--"
                    |]
                    |> lines
                    |> Array.tryHead

                let revisions = Array.append [| rootRevision |] (mergeRevision |> Option.toArray)

                do!
                    withApi
                        root
                        (fun api -> promise {
                            for revision in revisions do
                                let parents = git root [| "rev-parse"; revision + "^@" |] |> lines
                                let parent = Array.tryHead parents
                                let expected = expectedSummary root revision parent

                                let! response =
                                    api.listHistory {
                                        firstPage with
                                            HeadRevision = Some revision
                                            PageSize = 1
                                    }

                                let commit = (value response).Commits |> Array.head
                                Vitest.expect(commit.Revision).toBe revision
                                Vitest.expect(commit.ParentRevisions).toEqual parents
                                Vitest.expect(commit.Summary).toEqual (Some expected)

                                let! response = api.listChanges { Revision = revision }
                                Vitest.expect(GitHistoryChangeSummary.ofChanges (value response)).toEqual expected

                                if revision = rootRevision then
                                    Vitest.expect(parents.Length).toBe 0

                                    Vitest
                                        .expect(
                                            expected.Modified + expected.Deleted + expected.Renamed + expected.Copied
                                        )
                                        .toBe
                                        0
                                else
                                    Vitest.expect(parents.Length > 1).toBe true
                        })
            }
        )

        Vitest.test (
            "opens a changed text file as a committed diff with zero-based viewer coordinates",
            fun () -> promise {
                let root = repositoryRoot ()

                do!
                    withApi
                        root
                        (fun api -> promise {
                            let! response = api.listHistory { firstPage with PageSize = 20 }
                            let commits = (value response).Commits
                            let mutable selected: (string * GitHistoryFileChange) option = None

                            for commit in commits do
                                if selected.IsNone then
                                    let! response = api.listChanges { Revision = commit.Revision }

                                    selected <-
                                        value response
                                        |> Array.tryFind (fun change ->
                                            (change.Path.EndsWith(".fs")
                                             || change.Path.EndsWith(".md")
                                             || change.Path.EndsWith(".tsx"))
                                            && (change.Insertions |> Option.defaultValue 0)
                                               + (change.Deletions |> Option.defaultValue 0) > 0
                                        )
                                        |> Option.map (fun change -> commit.Revision, change)

                            let revision, change =
                                selected
                                |> Option.defaultWith (fun () ->
                                    failwith
                                        "The checkout needs a recent text change for the read-only integration fixture."
                                )

                            let parents = git root [| "rev-parse"; revision + "^@" |] |> lines

                            let! response =
                                api.openDiff {
                                    Revision = revision
                                    Path = change.Path
                                }

                            let diff = value response
                            Vitest.expect(diff.Revision).toBe revision
                            Vitest.expect(diff.Path).toBe change.Path
                            Vitest.expect(diff.ParentRevision).toEqual (Array.tryHead parents)
                            Vitest.expect(diff.Kind).toEqual change.Kind
                            Vitest.expect(diff.BlockedReason).toEqual None
                            Vitest.expect(diff.Parts.Length > 0).toBe true

                            for part in diff.Parts do
                                match part with
                                | HunkRows(_, previous, current, _, _, rows) ->
                                    Vitest.expect(rows.Length > 0).toBe true
                                    let previousNumbers = rows |> Array.choose _.Previous |> Array.map _.Number
                                    let currentNumbers = rows |> Array.choose _.Current |> Array.map _.Number

                                    if previousNumbers.Length > 0 then
                                        Vitest.expect(previousNumbers[0]).toBe previous.Start

                                    if currentNumbers.Length > 0 then
                                        Vitest.expect(currentNumbers[0]).toBe current.Start
                                | _ -> failwith "Expected committed patch hunks."
                        })
            }
        )

        Vitest.test (
            "ignores inherited Git directory and global configuration overrides",
            fun () -> promise {
                let root = repositoryRoot ()
                let expected = git root [| "rev-parse"; "HEAD" |] |> _.Trim()

                do!
                    withEnvironment
                        [|
                            "GIT_DIR", Some(resolve [| root; "missing-git-directory" |])
                            "GIT_WORK_TREE", Some(resolve [| root; "missing-work-tree" |])
                            "GIT_CONFIG_GLOBAL", Some(resolve [| root; "missing-git-config" |])
                        |]
                        (fun () ->
                            withApi
                                root
                                (fun api -> promise {
                                    let! response = api.listHistory firstPage
                                    Vitest.expect((value response).HeadRevision).toEqual (Some expected)
                                })
                        )
            }
        )

        Vitest.test (
            "distinguishes a non-repository folder from missing Git",
            fun () -> promise {
                let root = repositoryRoot ()
                let! plainFolder = createTempDirectoryAsync "swate-history-plain-"

                try
                    do!
                        withApi
                            plainFolder
                            (fun api -> promise {
                                let! response = api.listHistory firstPage
                                Vitest.expect(failure response).toBe "This ARC folder is not a Git repository."
                            })

                    do!
                        withEnvironment
                            [| "PATH", Some plainFolder |]
                            (fun () ->
                                withApi
                                    root
                                    (fun api -> promise {
                                        let! response = api.listHistory firstPage

                                        Vitest.expect(failure response).toBe
                                            "Git is unavailable. Install Git and reopen Swate to view history."
                                    })
                            )
                finally
                    // The fixture contains no repository or files.
                    removeEmptyDirectory plainFolder
            }
        )

        Vitest.test (
            "validates revision and changed-file requests before opening a diff",
            fun () -> promise {
                let root = repositoryRoot ()
                let head = git root [| "rev-parse"; "HEAD" |] |> _.Trim()

                do!
                    withApi
                        root
                        (fun api -> promise {
                            let! revision = api.listChanges { Revision = "--all" }
                            Vitest.expect(failure revision).toBe "Select a valid full commit revision."

                            let! path =
                                api.openDiff {
                                    Revision = head
                                    Path = "../outside.txt"
                                }

                            Vitest.expect(failure path).toBe "Select a valid repository-relative changed path."

                            let! unchanged =
                                api.openDiff {
                                    Revision = head
                                    Path = "missing-history-file.txt"
                                }

                            Vitest.expect(failure unchanged).toBe
                                "This path is not a changed file in the selected commit."
                        })
            }
        )
)
