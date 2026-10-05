module ElectronCore.NoteSearchReaderTests

open Fable.Core
open Fable.Core.JsInterop
open Main.NoteSearchReader
open Vitest

let private fsPromisesDynamic: obj = importAll "fs/promises"
let private osDynamic: obj = importAll "os"
let private pathDynamic: obj = importAll "path"

let private noteMarkdown =
    """---
title: My note
date: 2026-04-27T00:00:00.0000000
tags:
  -
    annotationValue: Planning
    termSource: SWATE
    termAccession: SWATE:0001
  -
    annotationValue: Review
    termSource: SWATE
    termAccession: SWATE:0002
---

Body line 1
---
Body line 3
"""

let private markdownWithoutFrontmatter =
    """# Plain note

Date Created: 27_04_2026
Tags: Planning

---

Plain body
"""

let private noTagsMarkdown =
    """---
title: Untagged note
date: 2026-04-27T00:00:00.0000000
---

Body
"""

Vitest.describe (
    "NoteSearchReader.readNotes",
    fun () ->
        Vitest.test (
            "decodes YAML frontmatter into note metadata and preserves body text",
            fun () -> promise {
                let tmpDir = osDynamic?tmpdir () |> unbox<string>

                let! repoRoot =
                    fsPromisesDynamic?mkdtemp (pathDynamic?join (tmpDir, "swate-notes-"))
                    |> unbox<JS.Promise<string>>

                let notesDir =
                    pathDynamic?join (repoRoot, "notes", "2026-04-27", "my_note") |> unbox<string>

                let notePath = pathDynamic?join (notesDir, "my_note.md") |> unbox<string>

                let! _ =
                    fsPromisesDynamic?mkdir (notesDir, createObj [ "recursive" ==> true ])
                    |> unbox<JS.Promise<obj>>

                let! _ =
                    fsPromisesDynamic?writeFile (notePath, noteMarkdown, "utf8")
                    |> unbox<JS.Promise<unit>>

                let! notes = readNotes repoRoot

                Vitest.expect(notes.Length).toBe (1)
                let note = notes.[0]
                Vitest.expect(note.RelativePath).toBe ("notes/2026-04-27/my_note/my_note.md")
                Vitest.expect(note.Title).toBe ("My note")
                Vitest.expect(note.Date.Year).toBe (2026)
                Vitest.expect(note.Date.Month).toBe (4)
                Vitest.expect(note.Date.Day).toBe (27)
                Vitest.expect(note.Content).toBe ("Body line 1\n---\nBody line 3")

                match note.Tags with
                | Some tags ->
                    Vitest.expect(tags.Count).toBe (2)
                    Vitest.expect(tags.[0].Name).toEqual (Some "Planning")
                    Vitest.expect(tags.[0].TermSourceREF).toEqual (Some "SWATE")
                    Vitest.expect(tags.[0].TermAccessionNumber).toEqual (Some "SWATE:0001")
                | None -> failwith "Expected tags from YAML frontmatter."
            }
        )

        Vitest.test (
            "skips note files without YAML frontmatter",
            fun () -> promise {
                let tmpDir = osDynamic?tmpdir () |> unbox<string>

                let! repoRoot =
                    fsPromisesDynamic?mkdtemp (pathDynamic?join (tmpDir, "swate-notes-"))
                    |> unbox<JS.Promise<string>>

                let notesDir =
                    pathDynamic?join (repoRoot, "notes", "2026-04-27", "plain_note")
                    |> unbox<string>

                let notePath = pathDynamic?join (notesDir, "plain_note.md") |> unbox<string>

                let! _ =
                    fsPromisesDynamic?mkdir (notesDir, createObj [ "recursive" ==> true ])
                    |> unbox<JS.Promise<obj>>

                let! _ =
                    fsPromisesDynamic?writeFile (notePath, markdownWithoutFrontmatter, "utf8")
                    |> unbox<JS.Promise<unit>>

                let! notes = readNotes repoRoot

                Vitest.expect(notes.Length).toBe (0)
            }
        )

        Vitest.test (
            "keeps absent YAML tag data as None",
            fun () -> promise {
                let tmpDir = osDynamic?tmpdir () |> unbox<string>

                let! repoRoot =
                    fsPromisesDynamic?mkdtemp (pathDynamic?join (tmpDir, "swate-notes-"))
                    |> unbox<JS.Promise<string>>

                let notesDir =
                    pathDynamic?join (repoRoot, "notes", "2026-04-27", "untagged_note")
                    |> unbox<string>

                let notePath = pathDynamic?join (notesDir, "untagged_note.md") |> unbox<string>

                let! _ =
                    fsPromisesDynamic?mkdir (notesDir, createObj [ "recursive" ==> true ])
                    |> unbox<JS.Promise<obj>>

                let! _ =
                    fsPromisesDynamic?writeFile (notePath, noTagsMarkdown, "utf8")
                    |> unbox<JS.Promise<unit>>

                let! notes = readNotes repoRoot

                Vitest.expect(notes.Length).toBe (1)

                Vitest.expect(notes.[0].Tags.IsNone).toBe (true)
            }
        )

        Vitest.test (
            "discovers nested notes directly while ignoring non-note and out-of-scope files",
            fun () -> promise {
                let tmpDir = osDynamic?tmpdir () |> unbox<string>

                let! repoRoot =
                    fsPromisesDynamic?mkdtemp (pathDynamic?join (tmpDir, "swate-notes-discovery-"))
                    |> unbox<JS.Promise<string>>

                let nestedNotesDir =
                    pathDynamic?join (repoRoot, "notes", "2026-04-27", "nested") |> unbox<string>

                let outsideDir = pathDynamic?join (repoRoot, "studies") |> unbox<string>

                let! _ =
                    fsPromisesDynamic?mkdir (nestedNotesDir, createObj [ "recursive" ==> true ])
                    |> unbox<JS.Promise<obj>>

                let! _ =
                    fsPromisesDynamic?mkdir (outsideDir, createObj [ "recursive" ==> true ])
                    |> unbox<JS.Promise<obj>>

                let nestedNotePath = pathDynamic?join (nestedNotesDir, "nested.md") |> unbox<string>

                let ignoredTextPath =
                    pathDynamic?join (nestedNotesDir, "ignored.txt") |> unbox<string>

                let outsideNotePath = pathDynamic?join (outsideDir, "outside.md") |> unbox<string>

                let! _ =
                    fsPromisesDynamic?writeFile (nestedNotePath, noteMarkdown, "utf8")
                    |> unbox<JS.Promise<unit>>

                let! _ =
                    fsPromisesDynamic?writeFile (ignoredTextPath, noteMarkdown, "utf8")
                    |> unbox<JS.Promise<unit>>

                let! _ =
                    fsPromisesDynamic?writeFile (outsideNotePath, noteMarkdown, "utf8")
                    |> unbox<JS.Promise<unit>>

                let! notes = readNotes repoRoot

                Vitest.expect(notes.Length).toBe (1)
                Vitest.expect(notes.[0].RelativePath).toBe ("notes/2026-04-27/nested/nested.md")
            }
        )

        Vitest.test (
            "returns an empty result when the notes folder is missing",
            fun () -> promise {
                let tmpDir = osDynamic?tmpdir () |> unbox<string>

                let! repoRoot =
                    fsPromisesDynamic?mkdtemp (pathDynamic?join (tmpDir, "swate-no-notes-"))
                    |> unbox<JS.Promise<string>>

                let! notes = readNotes repoRoot
                Vitest.expect(notes).toEqual ([||])
            }
        )

        Vitest.test (
            "isolates malformed notes and returns valid notes sorted by descending date",
            fun () -> promise {
                let tmpDir = osDynamic?tmpdir () |> unbox<string>

                let! repoRoot =
                    fsPromisesDynamic?mkdtemp (pathDynamic?join (tmpDir, "swate-note-errors-"))
                    |> unbox<JS.Promise<string>>

                let notesDir = pathDynamic?join (repoRoot, "notes") |> unbox<string>

                let! _ =
                    fsPromisesDynamic?mkdir (notesDir, createObj [ "recursive" ==> true ])
                    |> unbox<JS.Promise<obj>>

                let olderMarkdown =
                    noteMarkdown.Replace("My note", "Older").Replace("2026-04-27", "2025-01-02")

                let newerMarkdown =
                    noteMarkdown.Replace("My note", "Newer").Replace("2026-04-27", "2027-03-04")

                let writes = [|
                    fsPromisesDynamic?writeFile (pathDynamic?join (notesDir, "older.md"), olderMarkdown, "utf8")
                    |> unbox<JS.Promise<unit>>
                    fsPromisesDynamic?writeFile (
                        pathDynamic?join (notesDir, "broken.md"),
                        markdownWithoutFrontmatter,
                        "utf8"
                    )
                    |> unbox<JS.Promise<unit>>
                    fsPromisesDynamic?writeFile (pathDynamic?join (notesDir, "newer.md"), newerMarkdown, "utf8")
                    |> unbox<JS.Promise<unit>>
                |]

                let! _ = Fable.Core.JS.Constructors.Promise.all writes
                let! notes = readNotes repoRoot

                Vitest.expect(notes.Length).toBe (2)
                Vitest.expect(notes |> Array.map _.Title).toEqual ([| "Newer"; "Older" |])
            }
        )
)
