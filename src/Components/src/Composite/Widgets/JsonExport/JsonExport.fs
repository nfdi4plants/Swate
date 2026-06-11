namespace Swate.Components.Composite.Widgets.JsonExport

open Fable.Core
open Fable.Core.JsInterop
open Feliz
open Swate.Components
open Swate.Components.Shared


module private JsonExportHelper =

    type JsonExportState = {
        ExportFormat: JsonExportFormat
    } with

        static member init() = {
            ExportFormat = JsonExportFormat.ROCrate
        }

    let downloadJson (arcfile: ArcFiles, jef: JsonExportFormat) =
        let jsonExport = Json.Export.parseToJsonString (arcfile, jef)
        Swate.Components.Util.Download.downloadFromString (jsonExport)

open JsonExportHelper

[<Erase; Mangle(false)>]
type JsonExport =

    [<ReactComponent>]
    static member private FileFormat(efm: JsonExportFormat, state: JsonExportState, setState) =
        Html.option [ prop.text (efm.AsStringRdbl) ]

    [<ReactComponent(true)>]
    static member JsonExport(arcFile: ArcFiles) =
        let state, setState = React.useState JsonExportState.init

        let keys = Json.Generic.readFromJsonMap |> Map.keys |> Seq.toList

        Html.div [
            prop.className "swt:join"
            prop.children [
                Html.select [
                    prop.className "swt:select swt:join-item swt:min-w-fit"
                    prop.onChange (fun (e: Browser.Types.Event) ->
                        let jef: JsonExportFormat = JsonExportFormat.fromString (e.target?value)
                        { state with ExportFormat = jef } |> setState
                    )
                    prop.defaultValue (string state.ExportFormat)
                    prop.children [
                        for af, jf in keys do
                            if af = arcFile.RelatedArcFilesDiscriminate then
                                JsonExport.FileFormat(jf, state, setState)
                    ]
                ]
                Html.button [
                    prop.className "swt:btn swt:btn-primary swt:grow swt:join-item"
                    prop.text "Download"
                    prop.onClick (fun _ ->
                        downloadJson (arcFile, state.ExportFormat)
                    )
                ]
            ]
        ]
