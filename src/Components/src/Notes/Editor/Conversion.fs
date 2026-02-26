namespace Swate.Components.Notes.Editor

open System
open ARCtrl

[<RequireQualifiedAccess>]
module Conversion =

    let isSafePathSegment (value: string) =
        not (String.IsNullOrWhiteSpace value)
        && not (value.Contains "/")
        && not (value.Contains "\\")
        && not (value.Contains "..")

    let formatDateFolder (dateCreated: DateTime) =
        sprintf "%02d_%02d_%04d" dateCreated.Day dateCreated.Month dateCreated.Year

    let resolveProtocolName (draft: NotesDraft) =
        Validation.sanitizeProtocolName draft.Title

    let mkExistingTargetRelativePath (targetRef: ExistingTargetRef) (protocolName: string) =
        let folder =
            match targetRef.Kind with
            | NotesTargetKind.Study -> "studies"
            | NotesTargetKind.Assay -> "assays"

        if isSafePathSegment targetRef.Name && isSafePathSegment protocolName then
            Some $"{folder}/{targetRef.Name}/protocols/{protocolName}.md"
        else
            None

    let mkNewRootNoteRelativePath (dateCreated: DateTime) (protocolName: string) =
        let dateFolder = formatDateFolder dateCreated

        if isSafePathSegment dateFolder && isSafePathSegment protocolName then
            Some $"Notes/{dateFolder}/{protocolName}/{protocolName}.md"
        else
            None

    let private tryTagPart (value: string option) =
        match value with
        | Some v when not (String.IsNullOrWhiteSpace v) -> Some v
        | _ -> None

    let private tagToText (tag: OntologyAnnotation) =
        let parts =
            [
                tryTagPart tag.Name
                tryTagPart tag.TermSourceREF
                tryTagPart tag.TermAccessionNumber
            ]
            |> List.choose id

        if parts.IsEmpty then "Tag" else String.concat " | " parts

    let formatMarkdown (draft: NotesDraft) =
        let title = Validation.toOptionalString draft.Title |> Option.defaultValue "Untitled"

        let dateCreated =
            draft.DateCreated
            |> Option.map formatDateFolder
            |> Option.defaultValue ""

        let tagsText =
            draft.Tags
            |> Seq.map tagToText
            |> Seq.toList

        let tagsLine =
            if tagsText.IsEmpty then
                None
            else
                let joinedTags = String.concat ", " tagsText
                Some $"Tags: {joinedTags}"

        let body = Validation.toOptionalString draft.MainText |> Option.defaultValue ""

        let headerLines =
            [
                Some $"# {title}"
                Some ""
                Some $"Date Created: {dateCreated}"
                tagsLine
                Some ""
                Some "---"
                Some ""
            ]
            |> List.choose id

        let header = String.concat "\n" headerLines

        if String.IsNullOrWhiteSpace body then
            $"{header}\n"
        else
            $"{header}\n{body}\n"
