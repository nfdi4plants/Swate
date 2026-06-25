module ElectronCore.NoteConversionTests

open System
open ARCtrl
open Swate.Components.Composite.Notes.Editor
open Swate.Components.Shared
open Vitest

let private mkTag name source accession =
    OntologyAnnotation.create (name = name, tsr = source, tan = accession)

let private expectSome message value =
    match value with
    | Some v -> v
    | None -> failwith message

Vitest.describe (
    "NoteConversion",
    fun () ->
        Vitest.test (
            "formatMarkdown writes YAML frontmatter and preserves body text",
            fun () ->
                let tag =
                    OntologyAnnotation.create (
                        name = "Planning",
                        tsr = "SWATE",
                        tan = "SWATE:0001",
                        comments = ResizeArray([ Comment(name = "description", value = "Planning tag") ])
                    )

                let draft = {
                    NotesDraft.init with
                        Title = " My note "
                        DateCreated = Some(DateTime(2026, 4, 27))
                        Tags = ResizeArray([ tag; mkTag "Review" "SWATE" "SWATE:0002" ])
                        MainText = "Body line 1\n---\nBody line 3"
                }

                let markdown = NoteConversion.formatMarkdown draft

                Vitest.expect(markdown.StartsWith("---\n")).toBe (true)
                Vitest.expect(markdown.Contains("# My note")).toBe (false)
                Vitest.expect(markdown.Contains("Date Created:")).toBe (false)
                Vitest.expect(markdown.Contains("tags:")).toBe (true)

                let frontmatter, body =
                    NoteConversion.tryDecodeMarkdownFrontmatter markdown
                    |> expectSome "Expected YAML frontmatter to decode."

                let tags =
                    frontmatter.Tags |> expectSome "Expected YAML frontmatter tags to decode."

                Vitest.expect(frontmatter.Title).toBe ("My note")
                Vitest.expect(frontmatter.Date).toEqual (DateTime(2026, 4, 27))
                Vitest.expect(tags.Count).toBe (2)
                Vitest.expect(tags.[0].Name).toEqual (Some "Planning")
                Vitest.expect(tags.[0].TermSourceREF).toEqual (Some "SWATE")
                Vitest.expect(tags.[0].TermAccessionNumber).toEqual (Some "SWATE:0001")
                Vitest.expect(tags.[0].Comments.Count).toBe (1)
                Vitest.expect(body.Trim()).toBe ("Body line 1\n---\nBody line 3")
        )

        Vitest.test (
            "frontmatter decoder returns None when tags field is absent",
            fun () ->
                let markdown =
                    """---
title: Untagged note
date: 2026-04-27T00:00:00.0000000
---

Body
"""

                let frontmatter, body =
                    NoteConversion.tryDecodeMarkdownFrontmatter markdown
                    |> expectSome "Expected YAML frontmatter to decode."

                Vitest.expect(frontmatter.Title).toBe ("Untagged note")
                Vitest.expect(frontmatter.Date).toEqual (DateTime(2026, 4, 27))
                Vitest.expect(frontmatter.Tags.IsNone).toBe (true)
                Vitest.expect(body.Trim()).toBe ("Body")
        )

        Vitest.test (
            "formatMarkdown omits tags for drafts without tags",
            fun () ->
                let draft = {
                    NotesDraft.init with
                        Title = "Untagged note"
                        DateCreated = Some(DateTime(2026, 4, 27))
                        MainText = "Body"
                }

                let markdown = NoteConversion.formatMarkdown draft

                Vitest.expect(markdown.Contains("tags:")).toBe (false)

                let frontmatter, _ =
                    NoteConversion.tryDecodeMarkdownFrontmatter markdown
                    |> expectSome "Expected YAML frontmatter to decode."

                Vitest.expect(frontmatter.Tags.IsNone).toBe (true)
        )

        Vitest.test (
            "formatMarkdown rejects drafts without titles",
            fun () ->
                let draft = {
                    NotesDraft.init with
                        Title = "   "
                        DateCreated = Some(DateTime(2026, 4, 27))
                }

                let mutable didThrow = false

                try
                    NoteConversion.formatMarkdown draft |> ignore
                with ex ->
                    didThrow <- true
                    Vitest.expect(ex.Message.Contains("Note title is required.")).toBe (true)

                Vitest.expect(didThrow).toBe (true)
        )

        Vitest.test (
            "note path helpers use dated note folders and protocol folders",
            fun () ->
                let studyTarget: ExistingTargetRef = {
                    Kind = NotesTargetKind.Study
                    Name = "StudyA"
                }

                let assayTarget: ExistingTargetRef = {
                    Kind = NotesTargetKind.Assay
                    Name = "AssayA"
                }

                Vitest
                    .expect(NoteConversion.mkExistingTargetRelativePath studyTarget "Sampling_protocol")
                    .toEqual (Some "studies/StudyA/protocols/Sampling_protocol/Sampling_protocol.md")

                Vitest
                    .expect(NoteConversion.mkExistingTargetRelativePath assayTarget "Extraction_protocol")
                    .toEqual (Some "assays/AssayA/protocols/Extraction_protocol/Extraction_protocol.md")

                Vitest
                    .expect(NoteConversion.mkNewRootNoteRelativePath (DateTime(2026, 6, 15)) "Sampling_protocol")
                    .toEqual (Some "notes/2026-06-15/Sampling_protocol/Sampling_protocol.md")
        )

        Vitest.test (
            "PayloadRequirements.tryResolve defaults missing dates and requires a protocol-safe title",
            fun () ->
                let validDraft = {
                    NotesDraft.init with
                        Title = " Sampling protocol "
                        DateCreated = Some(DateTime(2026, 6, 15))
                }

                Vitest.expect((NoteConversion.PayloadRequirements.tryResolve(validDraft)).IsSome).toBe (true)

                let missingDateDraft = {
                    validDraft with
                        DateCreated = None
                }

                let beforeResolve = DateTime.Today

                let missingDate, missingDateProtocolName =
                    NoteConversion.PayloadRequirements.tryResolve(missingDateDraft)
                    |> expectSome "Expected missing date to default."

                let afterResolve = DateTime.Today

                match
                    NoteConversion.PayloadRequirements.tryCreateNewRootNotePayload(
                        missingDate,
                        missingDateProtocolName,
                        missingDateDraft
                    )
                with
                | Error message -> failwith $"Expected payload, got error: {message}"
                | Ok payload ->
                    Vitest.expect(payload.DateCreated = beforeResolve || payload.DateCreated = afterResolve).toBe (true)

                    Vitest
                        .expect(payload.Intent.RelativePath)
                        .toBe (
                            $"notes/{NoteConversion.formatDateFolder payload.DateCreated}/Sampling_protocol/Sampling_protocol.md"
                        )

                    let frontmatter, _ =
                        NoteConversion.tryDecodeMarkdownFrontmatter payload.Intent.Content
                        |> expectSome "Expected YAML frontmatter to decode."

                    Vitest.expect(frontmatter.Date).toEqual (payload.DateCreated)

                let missingTitleDraft = {
                    validDraft with
                        Title = "   "
                }

                Vitest.expect((NoteConversion.PayloadRequirements.tryResolve(missingTitleDraft)).IsNone).toBe (true)

                let unsafeTitleDraft = {
                    validDraft with
                        Title = "!!!"
                }

                Vitest.expect((NoteConversion.PayloadRequirements.tryResolve(unsafeTitleDraft)).IsNone).toBe (true)
        )

        Vitest.test (
            "tryCreateNewRootNotePayload creates the same root note intent as the wizard",
            fun () ->
                let draft = {
                    NotesDraft.init with
                        Title = " Sampling protocol "
                        DateCreated = Some(DateTime(2026, 6, 15))
                        MainText = "Body"
                }

                let dateCreated, protocolName =
                    NoteConversion.PayloadRequirements.tryResolve(draft)
                    |> expectSome "Expected payload requirements to resolve."

                match NoteConversion.PayloadRequirements.tryCreateNewRootNotePayload(dateCreated, protocolName, draft) with
                | Error message -> failwith $"Expected payload, got error: {message}"
                | Ok payload ->
                    Vitest.expect(payload.Title).toBe ("Sampling protocol")

                    Vitest
                        .expect(payload.Intent.RelativePath)
                        .toBe ("notes/2026-06-15/Sampling_protocol/Sampling_protocol.md")

                    Vitest.expect(payload.Intent.Target).toEqual (NotesTarget.NewRootNote)
                    Vitest.expect(payload.Intent.Content.Contains("Body")).toBe (true)
        )

        Vitest.test (
            "hasUnsavedDraft detects user-entered note draft content",
            fun () ->
                Vitest.expect(State.hasUnsavedDraft NotesDraft.init).toBe (false)

                let editedDraft = {
                    NotesDraft.init with
                        MainText = "draft body"
                }

                Vitest.expect(State.hasUnsavedDraft editedDraft).toBe (true)
        )
)
