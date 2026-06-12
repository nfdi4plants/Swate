namespace Swate.Components.Composite.Widgets.JsonExport

open Fable.Core
open Feliz
open Swate.Components
open Swate.Components.Composite.Widgets
open Swate.Components.Shared


module private JsonExportHelper =

    let defaultFormat (arcFile: ArcFiles) =
        Json.Generic.tryGetDefaultExportFormat arcFile.RelatedArcFilesDiscriminate
        |> Option.defaultValue JsonExportFormat.ARCtrl

    let downloadJson (arcfile: ArcFiles, jef: JsonExportFormat) =
        let jsonExport = Json.Export.parseToJsonString (arcfile, jef)
        Swate.Components.Util.Download.downloadFromString (jsonExport)

    let defaultExportJson (arcfile: ArcFiles, jef: JsonExportFormat) =
        promise {
            try
                downloadJson (arcfile, jef)
                return Ok()
            with exn ->
                return Error exn
        }

open JsonExportHelper

[<Erase; Mangle(false)>]
type JsonExport =

    [<ReactComponent(true)>]
    static member JsonExport
        (
            arcFile: ArcFiles,
            ?onExportJson: ArcFiles * JsonExportFormat -> JS.Promise<Result<unit, exn>>,
            ?onError: exn -> unit
        ) =
        let exportFormat, setExportFormat = React.useState (fun () -> defaultFormat arcFile)
        let isExporting, setIsExporting = React.useState false

        let onError = defaultArg onError (fun exn -> Browser.Dom.console.error exn)

        let exportJson =
            defaultArg onExportJson (fun (arcFile, jsonFormat) -> defaultExportJson (arcFile, jsonFormat))

        let supportedFormats =
            React.useMemo (
                (fun () -> Json.Generic.supportedExportFormats arcFile.RelatedArcFilesDiscriminate),
                [| box arcFile.RelatedArcFilesDiscriminate |]
            )

        React.useEffect (
            (fun () ->
                if not (supportedFormats |> List.contains exportFormat) then
                    Json.Generic.tryGetDefaultExportFormat arcFile.RelatedArcFilesDiscriminate
                    |> Option.defaultValue JsonExportFormat.ARCtrl
                    |> setExportFormat
            ),
            [| box supportedFormats; box exportFormat |]
        )

        Html.div [
            prop.className JsonWidgetLayout.rootClass
            prop.children [
                JsonFormatSelect.JsonFormatSelect(
                    supportedFormats,
                    exportFormat,
                    setExportFormat,
                    disabled = isExporting
                )
                Html.button [
                    prop.className JsonWidgetLayout.actionClass
                    prop.disabled (isExporting || supportedFormats.IsEmpty)
                    prop.text (
                        if isExporting then
                            "Exporting..."
                        else
                            "Download"
                    )
                    prop.onClick (fun _ ->
                        if not isExporting then
                            setIsExporting true

                            exportJson (arcFile, exportFormat)
                            |> Promise.catch (fun exn -> Error exn)
                            |> Promise.map (fun result ->
                                setIsExporting false

                                match result with
                                | Ok() -> ()
                                | Error exn -> onError exn
                            )
                            |> Promise.start
                    )
                ]
            ]
        ]
