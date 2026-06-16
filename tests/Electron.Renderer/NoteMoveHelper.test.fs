module ElectronRenderer.NoteMoveHelperTests

open System
open Renderer.Components.Helper.NotePathHelper
open Renderer.Components.MainContent.NoteMoveHelper
open Swate.Components.Composite.Notes.Editor
open Swate.Components.Shared
open Vitest

let private target kind name : ExistingTargetRef = { Kind = kind; Name = name }

let private markdown title date =
    let draft: NotesDraft = {
        NotesDraft.init with
            Title = title
            DateCreated = Some date
            MainText = "Protocol body"
    }

    NoteConversion.formatMarkdown draft

let private expectResolvedPaths result =
    match result with
    | Ok paths -> paths
    | Error errorMessage -> failwith errorMessage

Vitest.describe (
    "NoteMoveHelper",
    fun () ->
        Vitest.test (
            "resolves selected note paths for an existing study",
            fun () ->
                let content = markdown "Sampling protocol" (DateTime(2026, 6, 15))

                let sourcePath, targetPath =
                    tryResolveMoveToExistingTargetPaths
                        (Some "notes/2026-06-15/untitled-note/untitled-note.md")
                        content
                        (target NotesTargetKind.Study "StudyA")
                    |> expectResolvedPaths

                Vitest.expect(sourcePath).toBe ("notes/2026-06-15/untitled-note/untitled-note.md")
                Vitest.expect(targetPath).toBe ("studies/StudyA/protocols/Sampling_protocol/Sampling_protocol.md")

                match tryGetStructuredNoteFolderMove sourcePath targetPath with
                | None -> failwith "Expected new note folder structure to move as a folder."
                | Some(sourceFolderPath, targetFolderPath) ->
                    Vitest.expect(sourceFolderPath).toBe ("notes/2026-06-15/untitled-note")
                    Vitest.expect(targetFolderPath).toBe ("studies/StudyA/protocols/Sampling_protocol")
        )

        Vitest.test (
            "resolves selected note paths for an existing assay",
            fun () ->
                let content = markdown "Extraction protocol" (DateTime(2026, 6, 15))

                let _, targetPath =
                    tryResolveMoveToExistingTargetPaths
                        (Some "notes/2026-06-15/untitled-note/untitled-note.md")
                        content
                        (target NotesTargetKind.Assay "AssayA")
                    |> expectResolvedPaths

                Vitest
                    .expect(targetPath)
                    .toBe ("assays/AssayA/protocols/Extraction_protocol/Extraction_protocol.md")
        )

        Vitest.test (
            "uses the structured note folder as conflict path",
            fun () ->
                let conflictPath =
                    noteTargetConflictPath "studies/StudyA/protocols/Sampling_protocol/Sampling_protocol.md"

                Vitest.expect(conflictPath).toBe ("studies/StudyA/protocols/Sampling_protocol")
        )

        Vitest.test (
            "uses non-structured markdown path as conflict path",
            fun () ->
                let conflictPath = noteTargetConflictPath "notes/README.md"

                Vitest.expect(conflictPath).toBe ("notes/README.md")
        )

        Vitest.test (
            "uses non-markdown path as conflict path",
            fun () ->
                let conflictPath = noteTargetConflictPath "notes/2026-06-15/attachments"

                Vitest.expect(conflictPath).toBe ("notes/2026-06-15/attachments")
        )

        Vitest.test (
            "rejects notes that are already in the selected target",
            fun () ->
                let content = markdown "Sampling protocol" (DateTime(2026, 6, 15))

                let result =
                    tryResolveMoveToExistingTargetPaths
                        (Some "studies/StudyA/protocols/Sampling_protocol/Sampling_protocol.md")
                        content
                        (target NotesTargetKind.Study "StudyA")

                match result with
                | Ok _ -> failwith "Expected path resolution to fail for an unchanged target."
                | Error errorMessage -> Vitest.expect(errorMessage.Contains "already").toBe (true)
        )

        Vitest.test (
            "requires note frontmatter",
            fun () ->
                let result =
                    tryResolveMoveToExistingTargetPaths
                        (Some "notes/2026-06-15/plain/plain.md")
                        "# Plain markdown"
                        (target NotesTargetKind.Assay "AssayA")

                match result with
                | Ok _ -> failwith "Expected path resolution to fail without frontmatter."
                | Error errorMessage -> Vitest.expect(errorMessage.Contains "frontmatter").toBe (true)
        )

        Vitest.test (
            "rejects missing selection",
            fun () ->
                let content = markdown "Sampling protocol" (DateTime(2026, 6, 15))

                let result =
                    tryResolveMoveToExistingTargetPaths None content (target NotesTargetKind.Study "StudyA")

                match result with
                | Ok _ -> failwith "Expected path resolution to fail without a selected note."
                | Error errorMessage -> Vitest.expect(errorMessage.Contains "selected").toBe (true)
        )

        Vitest.test (
            "rejects invalid source paths",
            fun () ->
                let content = markdown "Sampling protocol" (DateTime(2026, 6, 15))

                let result =
                    tryResolveMoveToExistingTargetPaths
                        (Some "attachments/protocol.md")
                        content
                        (target NotesTargetKind.Study "StudyA")

                match result with
                | Ok _ -> failwith "Expected path resolution to fail for a non-note source path."
                | Error errorMessage -> Vitest.expect(errorMessage.Contains "Only markdown").toBe (true)
        )

        Vitest.test (
            "rejects invalid protocol titles",
            fun () ->
                let content = markdown "///" (DateTime(2026, 6, 15))

                let result =
                    tryResolveMoveToExistingTargetPaths
                        (Some "notes/2026-06-15/plain/plain.md")
                        content
                        (target NotesTargetKind.Assay "AssayA")

                match result with
                | Ok _ -> failwith "Expected path resolution to fail for an unsafe protocol title."
                | Error errorMessage -> Vitest.expect(errorMessage.Contains "Title").toBe (true)
        )
)
