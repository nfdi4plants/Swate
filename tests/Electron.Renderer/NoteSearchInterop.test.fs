module ElectronRenderer.NoteSearchInteropTests

open System
open ARCtrl
open ARCtrl.Json
open Swate.Electron.Shared.DTOs.NoteSearchDto
open Vitest

let private buildNoteDto (tags: string[] option) : NoteSearchDto = {
    RelativePath = "notes/test-note.md"
    Title = "Test note"
    Date = DateTime(2026, 4, 27)
    Tags = tags
    Content = "Body"
}

let private serializeTag (tag: OntologyAnnotation) =
    OntologyAnnotation.toJsonString 0 tag

let private expectTag (tag: OntologyAnnotation) (expectedName: string) (expectedSource: string option) (expectedAccession: string option) =
    Vitest.expect(tag.NameText).toBe(expectedName)
    Vitest.expect(tag.TermSourceREF).toEqual(expectedSource)
    Vitest.expect(tag.TermAccessionNumber).toEqual(expectedAccession)

let private expectTagsCount (count: int) (note: Swate.Components.NoteTypes.Note) =
    match note.Tags with
    | Some tags -> Vitest.expect(tags.Count).toBe(count)
    | None -> failwith "Expected tags to be present."

Vitest.describe("NoteSearchInterop.toDomainNote", fun () ->
    Vitest.test("deserializes valid serialized ontology tags", fun () ->
        let dto =
            buildNoteDto (
                Some
                    [|
                        OntologyAnnotation.create(name = "Planning", tsr = "SWATE", tan = "SWATE:0001") |> serializeTag
                        OntologyAnnotation.create(name = "Execution", tsr = "MS", tan = "MS:1000121") |> serializeTag
                    |]
            )

        let note = NoteSearchNoteDto.toNote dto

        match note.Tags with
        | Some tags ->
            Vitest.expect(tags.Count).toBe(2)
            expectTag tags.[0] "Planning" (Some "SWATE") (Some "SWATE:0001")
            expectTag tags.[1] "Execution" (Some "MS") (Some "MS:1000121")
        | None -> failwith "Expected note tags to be present.")

    Vitest.test("drops undecodable serialized tags while preserving decodable order", fun () ->
        let dto =
            buildNoteDto (
                Some
                    [|
                        OntologyAnnotation.create(name = "Planning", tsr = "SRC1", tan = "ACC1") |> serializeTag
                        "{ \"annotationValue\": 42 }"
                        ""
                        "not-json"
                        OntologyAnnotation.create(name = "Review", tsr = "SRC2", tan = "ACC2") |> serializeTag
                    |]
            )

        let note = NoteSearchNoteDto.toNote dto

        match note.Tags with
        | Some tags ->
            Vitest.expect(tags.Count).toBe(3)
            expectTag tags.[0] "Planning" (Some "SRC1") (Some "ACC1")
            expectTag tags.[1] "42" None None
            expectTag tags.[2] "Review" (Some "SRC2") (Some "ACC2")
        | None -> failwith "Expected note tags to be present.")

    Vitest.test("preserves None for missing tags", fun () ->
        let dto = buildNoteDto None
        let note = NoteSearchNoteDto.toNote dto
        Vitest.expect(note.Tags.IsNone).toBe(true))

    Vitest.test("preserves Some [] when all serialized tags are invalid", fun () ->
        let dto = buildNoteDto (Some [| ""; "   "; "invalid-json" |])
        let note = NoteSearchNoteDto.toNote dto
        expectTagsCount 0 note)

    Vitest.test("preserves Some [] for empty tag arrays", fun () ->
        let dto = buildNoteDto (Some [||])
        let note = NoteSearchNoteDto.toNote dto
        expectTagsCount 0 note)
)
