namespace Swate.Components.MarkdownText.Plugins

open System
open Fable.Core.JsInterop
open Feliz
open Swate.Components.MarkdownText.JsBindings

[<RequireQualifiedAccess>]
module AddStep =

    [<Literal>]
    let keyCommand = "add-step"

    let private normalizeStepText (stepText: string) =
        let normalized = stepText.Replace("\r\n", "\n").Replace("\r", "\n").Trim()

        normalized.Split '\n'
        |> Array.map (fun line -> line.Trim())
        |> Array.filter (String.IsNullOrWhiteSpace >> not)
        |> String.concat " "

    let private createListItem (stepText: string) =
        let content =
            normalizeStepText stepText
            |> fun normalized ->
                if String.IsNullOrWhiteSpace normalized then "Step" else normalized

        $"- [ ] {content}"

    let insertAtSelection (source: string) (startIndex: int) (endIndex: int) (stepText: string) =
        let boundedStart = min (max startIndex 0) source.Length
        let boundedEnd = min (max endIndex boundedStart) source.Length

        let requiresLeadingNewLine =
            boundedStart > 0
            && source.[boundedStart - 1] <> '\n'

        let requiresTrailingNewLine =
            boundedEnd < source.Length
            && source.[boundedEnd] <> '\n'

        let prefix = if requiresLeadingNewLine then "\n" else ""
        let suffix = if requiresTrailingNewLine then "\n" else ""
        let insertion = $"{prefix}{createListItem stepText}{suffix}"
        let nextValue = $"{source.Substring(0, boundedStart)}{insertion}{source.Substring boundedEnd}"
        let caretIndex = boundedStart + insertion.Length

        nextValue, caretIndex

    let command: ICommand =
        { new ICommand with
            member _.name = "Add Step"
            member _.keyCommand = keyCommand
            member _.icon =
                Html.span [
                    prop.className "swt:text-xs"
                    prop.text "Add Step"
                ]
            member _.buttonProps = createObj [ "aria-label" ==> "Add Step" ]
            member _.execute _ _ = () }

    let plugin: MarkdownToolbarPlugin =
        {
            Id = keyCommand
            Command = command
            Enabled = true
        }
