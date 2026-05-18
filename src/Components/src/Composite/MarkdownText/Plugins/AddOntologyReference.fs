namespace Swate.Components.Composite.MarkdownText.Plugins

open System
open Fable.Core.JsInterop
open Feliz
open ARCtrl

open Swate.Components.Composite.MarkdownText.JsBindings

[<RequireQualifiedAccess>]
module AddOntologyReference =

    [<Literal>]
    let keyCommand = "add-ontology-reference"

    let private parseSegments (input: string) =
        input.Split '|'
        |> Array.map (fun segment -> segment.Trim())
        |> Array.filter (String.IsNullOrWhiteSpace >> not)

    let private tryParseOntologyAnnotation (input: string) =
        let segments = parseSegments input

        match segments with
        | [| tan |] ->
            if String.IsNullOrWhiteSpace tan then
                Error "Ontology accession is required."
            else
                Ok(OntologyAnnotation.fromTermAnnotation tan)
        | [| name; tan |] ->
            if String.IsNullOrWhiteSpace tan then
                Error "Ontology accession is required."
            else
                Ok(OntologyAnnotation.fromTermAnnotation(tan, name = name))
        | [| name; tsr; tan |] ->
            if String.IsNullOrWhiteSpace tan then
                Error "Ontology accession is required."
            else
                Ok(OntologyAnnotation.create(name = name, tsr = tsr, tan = tan))
        | _ ->
            Error
                "Use one of: accession, name | accession, or name | source | accession."

    let private formatReference (oa: OntologyAnnotation) =
        let nameText = oa.NameText
        let shortAccession = oa.TermAccessionShort
        let referenceTarget = oa.TermAccessionAndOntobeeUrlIfShort

        let label =
            match String.IsNullOrWhiteSpace nameText, String.IsNullOrWhiteSpace shortAccession with
            | true, true -> "Ontology reference"
            | true, false -> shortAccession
            | false, true -> nameText
            | false, false -> $"{nameText} ({shortAccession})"

        if String.IsNullOrWhiteSpace referenceTarget then
            label
        else
            $"[{label}]({referenceTarget})"

    let private insertAtSelection (source: string) (startIndex: int) (endIndex: int) (content: string) =
        let boundedStart = min (max startIndex 0) source.Length
        let boundedEnd = min (max endIndex boundedStart) source.Length
        let nextValue = $"{source.Substring(0, boundedStart)}{content}{source.Substring boundedEnd}"
        let caretIndex = boundedStart + content.Length
        nextValue, (caretIndex, caretIndex)

    let command: ICommand =
        { new ICommand with
            member _.name = "Add Ontology"
            member _.keyCommand = keyCommand
            member _.icon =
                Html.span [
                    prop.className "swt:text-xs"
                    prop.text "Add Ontology"
                ]
            member _.buttonProps = createObj [ "aria-label" ==> "Add Ontology Reference" ]
            member _.execute _ _ = () }

    let private prompt: MarkdownPromptPlugin =
        {
            Title = "Add Ontology Reference"
            Description =
                Some "Use: accession OR name | accession OR name | source | accession."
            Placeholder = "instrument model | MS:1000031"
            SubmitButtonText = "Add"
            Validate =
                (fun input ->
                    match tryParseOntologyAnnotation input with
                    | Ok _ -> Ok()
                    | Error message -> Error message)
            Apply =
                (fun source selectionStart selectionEnd input ->
                    match tryParseOntologyAnnotation input with
                    | Ok oa ->
                        let reference = formatReference oa
                        insertAtSelection source selectionStart selectionEnd reference
                    | Error _ ->
                        source, (selectionStart, selectionEnd))
            InputMode = None
            Accept = None
            AllowMultiple = None
            ApplyFiles = None
        }

    let plugin: MarkdownToolbarPlugin =
        {
            Id = keyCommand
            Command = command
            Enabled = true
            Prompt = Some prompt
        }

