namespace Swate.Components.Composite.MarkdownText.Plugins

open System
open Fable.Core.JsInterop
open Feliz
open Swate.Components.Composite.MarkdownText.JsBindings

[<RequireQualifiedAccess>]
module AddImage =

    [<Literal>]
    let keyCommand = "add-image"

    let private toMarkdownImageLink ((file: MarkdownPromptFile), (path: string)) =
        let fallbackPath = if String.IsNullOrWhiteSpace path then file.Name else path
        let normalizedPath = fallbackPath.Replace("\\", "/")
        let altText = if String.IsNullOrWhiteSpace file.Name then "image" else file.Name

        $"![{altText}]({normalizedPath})"

    let private insertAtSelection (source: string) (startIndex: int) (endIndex: int) (links: string list) =
        let boundedStart = min (max startIndex 0) source.Length
        let boundedEnd = min (max endIndex boundedStart) source.Length
        let content = links |> String.concat "\n"

        let requiresLeadingNewLine = boundedStart > 0 && source.[boundedStart - 1] <> '\n'

        let requiresTrailingNewLine =
            boundedEnd < source.Length && source.[boundedEnd] <> '\n'

        let prefix = if requiresLeadingNewLine then "\n" else ""
        let suffix = if requiresTrailingNewLine then "\n" else ""
        let insertion = $"{prefix}{content}{suffix}"

        let nextValue =
            $"{source.Substring(0, boundedStart)}{insertion}{source.Substring boundedEnd}"

        let caretIndex = boundedStart + insertion.Length
        nextValue, (caretIndex, caretIndex)

    let command: ICommand =
        { new ICommand with
            member _.name = "Add Image"
            member _.keyCommand = keyCommand
            member _.icon = Html.span [ prop.className "swt:text-xs"; prop.text "Add Image" ]
            member _.buttonProps = createObj [ "aria-label" ==> "Add Image" ]
            member _.execute _ _ = ()
        }

    let private prompt: MarkdownPromptPlugin = {
        Title = "Add Image"
        Description = Some "Drop image files or choose them to insert markdown image links."
        Placeholder = "Choose image files"
        SubmitButtonText = "Insert"
        // Not used in File mode; required by MarkdownPromptPlugin shape.
        Validate = (fun _ -> Ok())
        // Not used in File mode; required by MarkdownPromptPlugin shape.
        Apply = (fun source selectionStart selectionEnd _ -> source, (selectionStart, selectionEnd))
        InputMode = Some MarkdownPromptInputMode.File
        Accept = Some "image/*"
        AllowMultiple = Some true
        ApplyFiles =
            Some(
                fun source selectionStart selectionEnd filesWithPaths ->
                    let links = filesWithPaths |> List.map toMarkdownImageLink

                    if List.isEmpty links then
                        source, (selectionStart, selectionEnd)
                    else
                        insertAtSelection source selectionStart selectionEnd links
            )
    }

    let plugin: MarkdownToolbarPlugin = {
        Id = keyCommand
        Command = command
        Enabled = true
        Prompt = Some prompt
    }

