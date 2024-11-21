namespace Protocol

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
open Shared
open ARCtrl
open Fable.Core.JsInterop

type private TemplateFromFileState = {
    /// User select type to upload
    FileType: ArcFilesDiscriminate
    /// User selects json type to upload
    JsonFormat: JsonExportFormat
    UploadedFile: ArcFiles option
    Loading: bool
} with
    static member init () =
        {
            FileType = ArcFilesDiscriminate.Assay
            JsonFormat = JsonExportFormat.ROCrate
            UploadedFile = None
            Loading = false
        }

module private Helper =

    let upload (state: TemplateFromFileState) setState (dispatch: Msg -> unit) (ev: File list) =
        let fileList = ev //: FileList = ev.target?files

        if fileList.Length > 0 then
            let file = fileList.Item 0 |> fun f -> f.slice()

            let reader = Browser.Dom.FileReader.Create()

            reader.onload <- fun evt ->
                let (r: string) = evt.target?result
                async {
                    setState {state with Loading = true}
                    let p = Spreadsheet.IO.Json.readFromJson state.FileType state.JsonFormat r
                    do!
                        Promise.either
                            (fun af -> setState {state with UploadedFile = Some af; Loading = false})
                            (fun e -> GenericError (Cmd.none,e) |> DevMsg |> dispatch)
                            p
                        |> Async.AwaitPromise
                } |> Async.StartImmediate

            reader.onerror <- fun evt ->
                curry GenericError Cmd.none (exn evt.Value) |> DevMsg |> dispatch

            reader.readAsText(file)
        else
            ()

type TemplateFromFile =

    static member private FileUploadButton (state:TemplateFromFileState, setState: TemplateFromFileState -> unit, dispatch) =
        Daisy.file [
            file.bordered
            prop.className "w-full"
            prop.onChange (fun (ev: File list) ->
                Helper.upload state setState dispatch ev
            )
            prop.onClick(fun e ->
                log e
                e.target?value <- null
            )
        ]

    static member private SelectorButton<'a when 'a : equality> (targetselector: 'a, selector: 'a, setSelector: 'a -> unit, ?isDisabled) =
        Daisy.button.button [
            join.item
            if isDisabled.IsSome then
                prop.disabled isDisabled.Value
            prop.style [style.flexGrow 1]
            if (targetselector = selector) then
                button.primary
            prop.onClick (fun _ -> setSelector targetselector)
            prop.text (string targetselector)
        ]

    [<ReactComponent>]
    static member Main(model: Model, dispatch) =
        let state, setState = React.useState(TemplateFromFileState.init)
        let setJsonFormat = fun x -> setState { state with JsonFormat = x }
        let setFileType = fun x -> setState { state with FileType = x }
        let fileTypeDisabled (ft: ArcFilesDiscriminate) =
            match state.JsonFormat, ft with
            // isa and rocrate do not support template
            | JsonExportFormat.ROCrate, ArcFilesDiscriminate.Template
            | JsonExportFormat.ISA, ArcFilesDiscriminate.Template -> true
            | _ -> false
        let jsonFormatDisabled (jf: JsonExportFormat) =
            match state.FileType ,jf with
            // template does not support isa and rocrate
            | ArcFilesDiscriminate.Template, JsonExportFormat.ROCrate
            | ArcFilesDiscriminate.Template, JsonExportFormat.ISA -> true
            | _ -> false
        SidebarComponents.SidebarLayout.LogicContainer [
            // modal!
            match state.UploadedFile with
            | Some af ->
                Modals.SelectiveImportModal.Main af model.SpreadsheetModel dispatch (fun _ -> TemplateFromFileState.init() |> setState)
            | None -> Html.none
            Html.div [
                Daisy.join [
                    prop.className "w-full"
                    prop.children [
                        JsonExportFormat.ROCrate |> fun jef -> TemplateFromFile.SelectorButton<JsonExportFormat> (jef, state.JsonFormat, setJsonFormat, jsonFormatDisabled jef)
                        JsonExportFormat.ISA |> fun jef -> TemplateFromFile.SelectorButton<JsonExportFormat> (jef, state.JsonFormat, setJsonFormat, jsonFormatDisabled jef)
                        JsonExportFormat.ARCtrl |> fun jef -> TemplateFromFile.SelectorButton<JsonExportFormat> (jef, state.JsonFormat, setJsonFormat, jsonFormatDisabled jef)
                        JsonExportFormat.ARCtrlCompressed |> fun jef -> TemplateFromFile.SelectorButton<JsonExportFormat> (jef, state.JsonFormat, setJsonFormat, jsonFormatDisabled jef)
                    ]
                ]
            ]

            Html.div [
                Daisy.join [
                    prop.className "w-full"
                    prop.children [
                        ArcFilesDiscriminate.Assay |> fun ft -> TemplateFromFile.SelectorButton<ArcFilesDiscriminate> (ft, state.FileType, setFileType, fileTypeDisabled ft)
                        ArcFilesDiscriminate.Study |> fun ft -> TemplateFromFile.SelectorButton<ArcFilesDiscriminate> (ft, state.FileType, setFileType, fileTypeDisabled ft)
                        ArcFilesDiscriminate.Investigation |> fun ft -> TemplateFromFile.SelectorButton<ArcFilesDiscriminate> (ft, state.FileType, setFileType, fileTypeDisabled ft)
                        ArcFilesDiscriminate.Template |> fun ft -> TemplateFromFile.SelectorButton<ArcFilesDiscriminate> (ft, state.FileType, setFileType, fileTypeDisabled ft)
                    ]
                ]
            ]

            Html.div [
                TemplateFromFile.FileUploadButton(state, setState, dispatch)
            ]
        ]