namespace Pages

open Feliz
open Fable.Core
open Fable.React
open Fable.React.Props
//open Fable.Core.JS
open Fable.Core.JsInterop

open Model
open Messages
open Browser.Types
open SpreadsheetInterface
open Messages
open Elmish

open Feliz
open Feliz.DaisyUI
open Swate.Components.Shared
open ARCtrl
open Fable.Core.JsInterop

open Modals
open Model.ModalState

type private TemplateFromFileState = {
    /// User select type to upload
    FileType: ArcFilesDiscriminate
    /// User selects json type to upload
    JsonFormat: JsonExportFormat
    Loading: bool
} with

    static member init() = {
        FileType = ArcFilesDiscriminate.Assay
        JsonFormat = JsonExportFormat.ROCrate
        Loading = false
    }

module private Helper =

    let upload (state: TemplateFromFileState) setState (dispatch: Msg -> unit) (ev: File list) =
        let fileList = ev //: FileList = ev.target?files

        if fileList.Length > 0 then
            let file = fileList.Item 0 |> fun f -> f.slice ()

            let reader = Browser.Dom.FileReader.Create()

            reader.onload <-
                fun evt ->
                    let (r: string) = evt.target?result

                    async {
                        setState { state with Loading = true }

                        let p = promise {
                            return Spreadsheet.IO.Json.readFromJsonMap.[(state.FileType, state.JsonFormat)] r
                        }

                        do!
                            Promise.either
                                (fun af ->
                                    TableModals.SelectiveFileImport af
                                    |> Model.ModalState.ModalTypes.TableModal
                                    |> Some
                                    |> Messages.UpdateModal
                                    |> dispatch

                                    setState { state with Loading = false }
                                )
                                (fun e -> GenericError(Cmd.none, e) |> DevMsg |> dispatch)
                                p
                            |> Async.AwaitPromise
                    }
                    |> Async.StartImmediate

            reader.onerror <- fun evt -> curry GenericError Cmd.none (exn evt.Value) |> DevMsg |> dispatch

            reader.readAsText (file)
        else
            ()

type JsonImport =

    static member private FileUploadButton
        (state: TemplateFromFileState, setState: TemplateFromFileState -> unit, dispatch)
        =
        Html.input [
            prop.type'.file
            prop.className "swt:file-input swt:file-input-neutral swt:w-full"
            prop.onChange (fun (ev: File list) -> Helper.upload state setState dispatch ev)
            prop.onClick (fun e -> e.target?value <- null)
        ]

    [<ReactComponent>]
    static member JsonUploadComponent(model: Model, dispatch) =
        let state, setState = React.useState (TemplateFromFileState.init)

        let keysToString (arcfile: ArcFilesDiscriminate, jsontype: JsonExportFormat) = sprintf "%A@%A" arcfile jsontype

        let tryKeysFromString (s: string) =
            let parts = s.Split '@'

            if parts.Length = 2 then
                let arcFile = parts.[0] |> ArcFilesDiscriminate.fromString
                let jsontype = parts.[1] |> JsonExportFormat.fromString

                Some(arcFile, jsontype)
            else
                None

        React.fragment [
            Html.select [
                prop.className "swt:select swt:w-full"
                prop.value (sprintf "%A@%A" state.FileType state.JsonFormat)
                prop.onChange (fun (ev: string) ->
                    match tryKeysFromString ev with
                    | Some(arcfile, jsontype) ->
                        setState {
                            state with
                                FileType = arcfile
                                JsonFormat = jsontype
                        }
                    | None -> ()
                )
                prop.children [
                    for (arcfile, jsontype) in Spreadsheet.IO.Json.readFromJsonMap.Keys |> Seq.sortBy fst do
                        Html.option [
                            prop.value (sprintf "%A@%A" arcfile jsontype)
                            prop.text (sprintf "%A - %A" arcfile jsontype)
                        ]
                ]
            ]

            Html.div [ JsonImport.FileUploadButton(state, setState, dispatch) ]
        ]

    [<ReactComponent>]
    static member Main(model, dispatch) =
        SidebarComponents.SidebarLayout.Container [
            SidebarComponents.SidebarLayout.Header "Json Import"

            // Box 2
            SidebarComponents.SidebarLayout.Description(
                Html.p [
                    Html.b "Import JSON files."
                    Html.text " You can use \"Json Export\" to create these files from existing Swate tables. "
                ]
            )

            SidebarComponents.SidebarLayout.LogicContainer [ JsonImport.JsonUploadComponent(model, dispatch) ]
        ]