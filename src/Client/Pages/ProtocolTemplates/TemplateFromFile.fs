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
open Feliz.Bulma
open Shared
open ARCtrl

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
    
    let upload (uploadId: string) (state: TemplateFromFileState) setState dispatch (ev: File list) =
        let fileList = ev //: FileList = ev.target?files

        if fileList.Length > 0 then
            let file = fileList.Item 0 |> fun f -> f.slice()

            let reader = Browser.Dom.FileReader.Create()

            reader.onload <- fun evt ->
                let (r: string) = evt.target?result
                async {
                    setState {state with Loading = true}
                    let! af = Spreadsheet.IO.Json.readFromJson state.FileType state.JsonFormat r |> Async.AwaitPromise
                    setState {state with UploadedFile = Some af; Loading = false}
                } |> Async.StartImmediate
                                   
            reader.onerror <- fun evt ->
                curry GenericLog Cmd.none ("Error", evt.Value) |> DevMsg |> dispatch

            reader.readAsText(file)
        else
            ()
        let picker = Browser.Dom.document.getElementById(uploadId)
        // https://stackoverflow.com/questions/3528359/html-input-type-file-file-selection-event/3528376
        picker?value <- null

type TemplateFromFile =

    static member private fileUploadButton (state:TemplateFromFileState, setState: TemplateFromFileState -> unit, dispatch)  =
        let uploadId = "UploadFiles_ElementId"
        Bulma.label [
            Bulma.fileInput [
                prop.id uploadId
                prop.type' "file";
                prop.style [style.display.none]
                prop.onChange (fun (ev: File list) ->
                    Helper.upload uploadId state setState dispatch ev
                )
            ]
            Bulma.button.a [
                Bulma.color.isInfo;
                Bulma.button.isFullWidth
                prop.onClick(fun e ->
                    e.preventDefault()
                    let getUploadElement = Browser.Dom.document.getElementById uploadId
                    getUploadElement.click()
                )
                prop.text "Upload protocol"
            ]
        ]

    static member private SelectorButton<'a when 'a : equality> (targetselector: 'a, selector: 'a, setSelector: 'a -> unit, ?isDisabled) =
        Bulma.button.button [
            if isDisabled.IsSome then
                prop.disabled isDisabled.Value
            prop.style [style.flexGrow 1]
            if (targetselector = selector) then
                color.isSuccess
                button.isSelected
            prop.onClick (fun _ -> setSelector targetselector)
            prop.text (string targetselector)
        ]

    [<ReactComponent>]
    static member Main(model: Messages.Model, dispatch) =
        let state, setState = React.useState(TemplateFromFileState.init)
        let af = React.useRef (
            let a = ArcAssay.init("My Assay")
            let t1 = a.InitTable("Template Table 1")
            t1.AddColumns([|
                CompositeColumn.create(CompositeHeader.Input IOType.Source, [| for i in 1 .. 5 do sprintf "Source _ %i" i |> CompositeCell.FreeText |])
                CompositeColumn.create(CompositeHeader.Output IOType.Sample, [| for i in 1 .. 5 do sprintf "Sample _ %i" i |> CompositeCell.FreeText |])
                CompositeColumn.create(CompositeHeader.Component (OntologyAnnotation("instrument model", "MS", "MS:19283")), [| for i in 1 .. 5 do OntologyAnnotation("SCIEX instrument model", "MS", "MS:21387189237") |> CompositeCell.Term |])
                CompositeColumn.create(CompositeHeader.Factor (OntologyAnnotation("temperature", "UO", "UO:21387")), [| for i in 1 .. 5 do CompositeCell.createUnitized("", OntologyAnnotation("degree celcius", "UO", "UO:21387189237")) |])
            |])
            let t2 = a.InitTable("Template Table 2")
            t2.AddColumns([|
                CompositeColumn.create(CompositeHeader.Input IOType.Source, [| for i in 1 .. 5 do sprintf "Source2 _ %i" i |> CompositeCell.FreeText |])
                CompositeColumn.create(CompositeHeader.Output IOType.Sample, [| for i in 1 .. 5 do sprintf "Sample2 _ %i" i |> CompositeCell.FreeText |])
                CompositeColumn.create(CompositeHeader.Component (OntologyAnnotation("instrument", "MS", "MS:19283")), [| for i in 1 .. 5 do OntologyAnnotation("SCIEX instrument model", "MS", "MS:21387189237") |> CompositeCell.Term |])
                CompositeColumn.create(CompositeHeader.Factor (OntologyAnnotation("temperature", "UO", "UO:21387")), [| for i in 1 .. 5 do CompositeCell.createUnitized("", OntologyAnnotation("degree celcius", "UO", "UO:21387189237")) |])
            |])
            let af = ArcFiles.Assay a
            af
        )
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
        mainFunctionContainer [
            // modal!
            match state.UploadedFile with
            | Some af ->
                Modals.SelectiveImportModal.Main af (fun _ -> TemplateFromFileState.init() |> setState)
            | None -> Html.none
            Modals.SelectiveImportModal.Main af.current (fun _ -> TemplateFromFileState.init() |> setState)
            Bulma.field.div [
                Bulma.help [
                    b [] [str "Import JSON files."]
                    str " You can use \"Json Export\" to create these files from existing Swate tables. "
                ]
            ]
            Bulma.field.div [
                Bulma.buttons [
                    buttons.hasAddons
                    prop.children [
                        JsonExportFormat.ROCrate |> fun jef -> TemplateFromFile.SelectorButton<JsonExportFormat> (jef, state.JsonFormat, setJsonFormat, jsonFormatDisabled jef)
                        JsonExportFormat.ISA |> fun jef -> TemplateFromFile.SelectorButton<JsonExportFormat> (jef, state.JsonFormat, setJsonFormat, jsonFormatDisabled jef)
                        JsonExportFormat.ARCtrl |> fun jef -> TemplateFromFile.SelectorButton<JsonExportFormat> (jef, state.JsonFormat, setJsonFormat, jsonFormatDisabled jef)
                        JsonExportFormat.ARCtrlCompressed |> fun jef -> TemplateFromFile.SelectorButton<JsonExportFormat> (jef, state.JsonFormat, setJsonFormat, jsonFormatDisabled jef)
                    ]
                ]
            ]

            Bulma.field.div [
                Bulma.buttons [
                    buttons.hasAddons
                    prop.children [
                        ArcFilesDiscriminate.Assay |> fun ft -> TemplateFromFile.SelectorButton<ArcFilesDiscriminate> (ft, state.FileType, setFileType, fileTypeDisabled ft)
                        ArcFilesDiscriminate.Study |> fun ft -> TemplateFromFile.SelectorButton<ArcFilesDiscriminate> (ft, state.FileType, setFileType, fileTypeDisabled ft)
                        ArcFilesDiscriminate.Investigation |> fun ft -> TemplateFromFile.SelectorButton<ArcFilesDiscriminate> (ft, state.FileType, setFileType, fileTypeDisabled ft)
                        ArcFilesDiscriminate.Template |> fun ft -> TemplateFromFile.SelectorButton<ArcFilesDiscriminate> (ft, state.FileType, setFileType, fileTypeDisabled ft)
                    ]
                ]
            ]

            Bulma.field.div [
                TemplateFromFile.fileUploadButton(state, setState, dispatch)
            ]
        ]