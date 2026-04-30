module NotesConversionTests

#if FABLE_COMPILER
open Fable.Mocha
#else
open Expecto
#endif

open System
open ARCtrl
open Swate.Components.Notes.Editor

let private mkTag name source accession =
    OntologyAnnotation.create(name = name, tsr = source, tan = accession)

let private expectSome message value =
    match value with
    | Some v -> v
    | None -> failwith message

let tests =
    testList "Notes conversion" [
        testCase "formatMarkdown writes YAML frontmatter and preserves body text" <| fun _ ->
            let tag =
                OntologyAnnotation.create(
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

            Expect.isTrue (markdown.StartsWith("---\n")) "Markdown should start with YAML frontmatter."
            Expect.isFalse (markdown.Contains("# My note")) "Markdown title should not be written."
            Expect.isFalse (markdown.Contains("Date Created:")) "Markdown date line should not be written."
            Expect.isTrue (markdown.Contains("tags:")) "Tag metadata should be encoded in YAML."

            let frontmatter, body =
                NoteConversion.tryDecodeMarkdownFrontmatter markdown
                |> expectSome "Expected YAML frontmatter to decode."

            let tags =
                frontmatter.Tags
                |> expectSome "Expected YAML frontmatter tags to decode."

            Expect.equal frontmatter.Title "My note" "Title should be trimmed before encoding."
            Expect.equal frontmatter.Date (DateTime(2026, 4, 27)) "Date should round-trip as DateTime."
            Expect.equal tags.Count 2 "All tags should round-trip."
            Expect.equal tags.[0].Name (Some "Planning") "Tag name should round-trip."
            Expect.equal tags.[0].TermSourceREF (Some "SWATE") "Tag source should round-trip."
            Expect.equal tags.[0].TermAccessionNumber (Some "SWATE:0001") "Tag accession should round-trip."
            Expect.equal tags.[0].Comments.Count 1 "Tag comments should round-trip."
            Expect.equal (body.Trim()) "Body line 1\n---\nBody line 3" "Body should remain plain text."

        testCase "frontmatter decoder returns None when tags field is absent" <| fun _ ->
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

            Expect.equal frontmatter.Title "Untagged note" "Title should decode."
            Expect.equal frontmatter.Date (DateTime(2026, 4, 27)) "Date should decode."
            Expect.isTrue frontmatter.Tags.IsNone "Absent YAML tags should stay None."
            Expect.equal (body.Trim()) "Body" "Body should decode."

        testCase "formatMarkdown omits tags for drafts without tags" <| fun _ ->
            let draft = {
                NotesDraft.init with
                    Title = "Untagged note"
                    DateCreated = Some(DateTime(2026, 4, 27))
                    MainText = "Body"
            }

            let markdown = NoteConversion.formatMarkdown draft

            Expect.isFalse (markdown.Contains("tags:")) "Empty draft tags should not write a YAML tags field."

            let frontmatter, _ =
                NoteConversion.tryDecodeMarkdownFrontmatter markdown
                |> expectSome "Expected YAML frontmatter to decode."

            Expect.isTrue frontmatter.Tags.IsNone "Omitted YAML tags should decode as None."
    ]
