module ElectronRenderer.NoteSearchInteropTests

open System
open ARCtrl
open Fable.Core.JsInterop
open Renderer.Components.MainContent.NoteSearchInterop
open Swate.Components.NoteTypes
open Vitest

let private buildNote (tags: ResizeArray<OntologyAnnotation> option) =
    {
        RelativePath = "notes/test-note.md"
        Title = "Test note"
        Date = DateTime(2026, 4, 27)
        Tags = tags
        Content = "Body"
    }

let private asOntologyAnnotation (value: obj) : OntologyAnnotation = unbox value

let private createPascalCaseTag name source accession =
    createObj [
        "Name" ==> name
        "TermSourceREF" ==> source
        "TermAccessionNumber" ==> accession
    ]

let private createUnderscoreTag name source accession =
    createObj [
        "_name" ==> name
        "_termSourceREF" ==> source
        "_termAccessionNumber" ==> accession
    ]

let private expectTags (note: Note) =
    match note.Tags with
    | Some tags -> tags
    | None -> failwith "Expected note tags to be present."

let private expectTag
    (tag: OntologyAnnotation)
    (expectedName: string)
    (expectedSource: string option)
    (expectedAccession: string option)
    =
    Vitest.expect(tag.NameText).toBe(expectedName)
    Vitest.expect(tag.TermSourceREF).toEqual(expectedSource)
    Vitest.expect(tag.TermAccessionNumber).toEqual(expectedAccession)

Vitest.describe (
    "NoteSearchInterop.rehydrateNote",
    fun () ->
        Vitest.test (
            "rehydrates tags from raw PascalCase JS objects",
            fun () ->
                let rawTag =
                    createPascalCaseTag "Planning" "SWATE" "SWATE:0001"
                    |> asOntologyAnnotation

                let note = buildNote (Some(ResizeArray [ rawTag ]))
                let hydrated = rehydrateNote note
                let tags = expectTags hydrated

                Vitest.expect(tags.Count).toBe(1)
                expectTag tags.[0] "Planning" (Some "SWATE") (Some "SWATE:0001")
        )

        Vitest.test (
            "rehydrates tags from raw underscore JS objects",
            fun () ->
                let rawTag =
                    createUnderscoreTag "Analysis" "MS" "MS:1000121"
                    |> asOntologyAnnotation

                let note = buildNote (Some(ResizeArray [ rawTag ]))
                let hydrated = rehydrateNote note
                let tags = expectTags hydrated

                Vitest.expect(tags.Count).toBe(1)
                expectTag tags.[0] "Analysis" (Some "MS") (Some "MS:1000121")
        )

        Vitest.test (
            "keeps semantic values when tags are already OntologyAnnotation instances",
            fun () ->
                let existing = OntologyAnnotation(?name = Some "Existing")
                let note = buildNote (Some(ResizeArray [ existing ]))
                let hydrated = rehydrateNote note
                let tags = expectTags hydrated

                Vitest.expect(tags.Count).toBe(1)
                expectTag tags.[0] "Existing" None None
        )

        Vitest.test (
            "keeps already-hydrated OntologyAnnotation tags in mixed arrays when other tags are rehydrated",
            fun () ->
                let validPascal =
                    createPascalCaseTag "Planning" "SRC1" "ACC1"
                    |> asOntologyAnnotation

                let validUnderscore =
                    createUnderscoreTag "Execution" "SRC2" "ACC2"
                    |> asOntologyAnnotation

                let alreadyHydrated = OntologyAnnotation(?name = Some "Review")

                let note =
                    buildNote (Some(ResizeArray [ validPascal; validUnderscore; alreadyHydrated ]))

                let hydrated = rehydrateNote note
                let tags = expectTags hydrated

                Vitest.expect(tags.Count).toBe(3)
                expectTag tags.[0] "Planning" (Some "SRC1") (Some "ACC1")
                expectTag tags.[1] "Execution" (Some "SRC2") (Some "ACC2")
                expectTag tags.[2] "Review" None None
        )

        Vitest.test (
            "preserves Some [] for empty tags arrays",
            fun () ->
                let note = buildNote (Some(ResizeArray []))
                let hydrated = rehydrateNote note

                Vitest.expect(hydrated.Tags.IsSome).toBe(true)
                Vitest.expect(expectTags hydrated |> _.Count).toBe(0)
        )

        Vitest.test (
            "drops malformed entries in mixed tag payloads while keeping valid tags in order",
            fun () ->
                let validPascal =
                    createPascalCaseTag "Planning" "SRC1" "ACC1"
                    |> asOntologyAnnotation

                let malformed =
                    createObj [
                        "Name" ==> 42
                        "TermSourceREF" ==> null
                        "TermAccessionNumber" ==> false
                    ]
                    |> asOntologyAnnotation

                let validUnderscore =
                    createUnderscoreTag "Execution" "SRC2" "ACC2"
                    |> asOntologyAnnotation

                let nullTag: OntologyAnnotation = unbox null
                let validOntologyAnnotation = OntologyAnnotation("Review", "SRC3", "ACC3")

                let note =
                    buildNote (
                        Some(
                            ResizeArray [
                                validPascal
                                malformed
                                validUnderscore
                                nullTag
                                validOntologyAnnotation
                            ]
                        )
                    )

                let hydrated = rehydrateNote note
                let tags = expectTags hydrated

                Vitest.expect(tags.Count).toBe(3)
                expectTag tags.[0] "Planning" (Some "SRC1") (Some "ACC1")
                expectTag tags.[1] "Execution" (Some "SRC2") (Some "ACC2")
                expectTag tags.[2] "Review" (Some "SRC3") (Some "ACC3")
        )
)
