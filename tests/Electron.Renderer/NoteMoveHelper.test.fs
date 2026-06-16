module ElectronRenderer.NoteMoveHelperTests

open System
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

let private expectReadyPlan result =
    match result with
    | Ok(ExistingTargetNoteMovePlanResult.Ready plan) -> plan
    | Ok(ExistingTargetNoteMovePlanResult.TargetConflict plan) ->
        failwith $"Expected a ready move plan, but got a target conflict for '{plan.TargetPath}'."
    | Error errorMessage -> failwith errorMessage

let private expectConflictPlan result =
    match result with
    | Ok(ExistingTargetNoteMovePlanResult.TargetConflict plan) -> plan
    | Ok(ExistingTargetNoteMovePlanResult.Ready plan) ->
        failwith $"Expected a target conflict, but got a ready move plan for '{plan.TargetPath}'."
    | Error errorMessage -> failwith errorMessage

Vitest.describe (
    "NoteMoveHelper",
    fun () ->
        Vitest.test (
            "builds a move plan from a selected note to an existing study",
            fun () ->
                let content = markdown "Sampling protocol" (DateTime(2026, 6, 15))

                let plan =
                    tryBuildMoveToExistingTargetPlan
                        (Some "notes/2026-06-15/untitled-note/untitled-note.md")
                        content
                        (target NotesTargetKind.Study "StudyA")
                        []
                    |> expectReadyPlan

                Vitest.expect(plan.SourcePath).toBe ("notes/2026-06-15/untitled-note/untitled-note.md")
                Vitest.expect(plan.TargetPath).toBe ("studies/StudyA/protocols/Sampling_protocol/Sampling_protocol.md")
                Vitest.expect(movePlanConflictPath plan).toBe ("studies/StudyA/protocols/Sampling_protocol")

                match plan.FolderMove with
                | None -> failwith "Expected new note folder structure to move as a folder."
                | Some folderMove ->
                    Vitest.expect(folderMove.SourceFolderPath).toBe ("notes/2026-06-15/untitled-note")
                    Vitest.expect(folderMove.TargetFolderPath).toBe ("studies/StudyA/protocols/Sampling_protocol")
        )

        Vitest.test (
            "builds a move plan from a selected note to an existing assay",
            fun () ->
                let content = markdown "Extraction protocol" (DateTime(2026, 6, 15))

                let plan =
                    tryBuildMoveToExistingTargetPlan
                        (Some "notes/2026-06-15/untitled-note/untitled-note.md")
                        content
                        (target NotesTargetKind.Assay "AssayA")
                        []
                    |> expectReadyPlan

                Vitest
                    .expect(plan.TargetPath)
                    .toBe ("assays/AssayA/protocols/Extraction_protocol/Extraction_protocol.md")
        )

        Vitest.test (
            "reports a conflict when the target note already exists",
            fun () ->
                let content = markdown "Sampling protocol" (DateTime(2026, 6, 15))

                let plan =
                    tryBuildMoveToExistingTargetPlan
                        (Some "notes/2026-06-15/untitled-note/untitled-note.md")
                        content
                        (target NotesTargetKind.Study "StudyA")
                        [
                            "studies/StudyA/protocols/Sampling_protocol/Sampling_protocol.md"
                        ]
                    |> expectConflictPlan

                Vitest.expect(plan.TargetPath).toBe ("studies/StudyA/protocols/Sampling_protocol/Sampling_protocol.md")
        )

        Vitest.test (
            "reports a conflict when the target note folder already exists",
            fun () ->
                let content = markdown "Sampling protocol" (DateTime(2026, 6, 15))

                let plan =
                    tryBuildMoveToExistingTargetPlan
                        (Some "notes/2026-06-15/untitled-note/untitled-note.md")
                        content
                        (target NotesTargetKind.Study "StudyA")
                        [ "studies/StudyA/protocols/Sampling_protocol" ]
                    |> expectConflictPlan

                Vitest.expect(plan.TargetPath).toBe ("studies/StudyA/protocols/Sampling_protocol/Sampling_protocol.md")
        )

        Vitest.test (
            "finds target conflicts in snapshots with normalized paths",
            fun () ->
                let exists =
                    PathHelpers.pathExistsInSnapshot
                        [
                            "studies\\StudyA\\protocols\\Sampling_protocol\\Sampling_protocol.md"
                        ]
                        "studies/studya/protocols/sampling_protocol/sampling_protocol.md"

                Vitest.expect(exists).toBe (true)
        )

        Vitest.test (
            "rejects notes that are already in the selected target",
            fun () ->
                let content = markdown "Sampling protocol" (DateTime(2026, 6, 15))

                let result =
                    tryBuildMoveToExistingTargetPlan
                        (Some "studies/StudyA/protocols/Sampling_protocol/Sampling_protocol.md")
                        content
                        (target NotesTargetKind.Study "StudyA")
                        []

                match result with
                | Ok _ -> failwith "Expected move plan creation to fail for an unchanged target."
                | Error errorMessage -> Vitest.expect(errorMessage.Contains "already").toBe (true)
        )

        Vitest.test (
            "requires note frontmatter",
            fun () ->
                let result =
                    tryBuildMoveToExistingTargetPlan
                        (Some "notes/2026-06-15/plain/plain.md")
                        "# Plain markdown"
                        (target NotesTargetKind.Assay "AssayA")
                        []

                match result with
                | Ok _ -> failwith "Expected move plan creation to fail without frontmatter."
                | Error errorMessage -> Vitest.expect(errorMessage.Contains "frontmatter").toBe (true)
        )
)
