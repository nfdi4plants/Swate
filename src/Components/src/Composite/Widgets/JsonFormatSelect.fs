namespace Swate.Components.Composite.Widgets

open Fable.Core
open Feliz
open Swate.Components.Shared

module JsonWidgetLayout =

    let rootClass = "swt:join swt:w-fit swt:max-w-full"
    let actionClass = "swt:btn swt:btn-primary swt:join-item swt:w-32 swt:shrink-0"

    let fileInputClass =
        "swt:file-input swt:file-input-primary swt:join-item swt:w-32 swt:shrink-0"

[<Erase; Mangle(false)>]
type JsonFormatSelect =

    static member private Width(formats: JsonExportFormat list, selectedFormat: JsonExportFormat) =
        selectedFormat :: formats
        |> List.map (fun format -> format.AsStringRdbl.Length)
        |> List.max
        |> fun characters -> $"calc({characters}ch + 4rem)"

    [<ReactComponent>]
    static member JsonFormatSelect
        (
            formats: JsonExportFormat list,
            selectedFormat: JsonExportFormat,
            onFormatChange: JsonExportFormat -> unit,
            ?disabled: bool,
            ?testId: string
        ) =
        let disabled = defaultArg disabled false
        let selectWidth = JsonFormatSelect.Width(formats, selectedFormat)

        Html.select [
            if testId.IsSome then
                prop.testId testId.Value
            prop.className "swt:select swt:join-item swt:shrink-0"
            prop.style [
                style.custom ("width", selectWidth)
                style.custom ("min-width", selectWidth)
            ]
            prop.onChange (JsonExportFormat.fromString >> onFormatChange)
            prop.value (string selectedFormat)
            prop.disabled disabled
            prop.children [
                for format in formats do
                    Html.option [
                        prop.value (string format)
                        prop.text (format.AsStringRdbl)
                    ]
            ]
        ]
