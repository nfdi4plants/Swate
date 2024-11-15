module JsonExporter.Core

open System
open Fable.Core.JsInterop
open Elmish

open Shared

open ExcelColors
open Model

open Messages

open Browser.Dom

open Feliz
open Feliz.DaisyUI

open ExcelJS.Fable
open GlobalBindings

open ARCtrl
open ARCtrl.Spreadsheet

let download(filename, text) =
  let element = document.createElement("a");
  element.setAttribute("href", "data:text/plain;charset=utf-8," +  Fable.Core.JS.encodeURIComponent(text));
  element.setAttribute("download", filename);

  element?style?display <- "None";
  let _ = document.body.appendChild(element);

  element.click();

  document.body.removeChild(element) |> ignore
  ()

//open Messages
//open Feliz
//open Feliz.DaisyUI

//let dropdownItem (exportType:JsonExportType) (model:Model) msg (isActive:bool) =
//    Bulma.dropdownItem.a [
//        prop.tabIndex 0
//        prop.onClick (fun e ->
//                e.stopPropagation()
//                exportType |> msg
//            )
//        prop.onKeyDown (fun k -> if (int k.which) = 13 then exportType |> msg)
//        prop.children [
//            Html.span [
//                prop.className "has-tooltip-right has-tooltip-multiline"
//                prop.custom ("data-tooltip", exportType.toExplanation)
//                prop.style [style.fontSize(length.rem 1.1); style.paddingRight 10; style.textAlign.center; style.color NFDIColors.Yellow.Darker20]
//                Html.i [prop.className "fa-solid fa-circle-info"] |> prop.children
//            ]

//            Html.span (exportType.ToString())
//        ]
//    ]

//let parseTableToISAJsonEle (model:Model) (dispatch:Messages.Msg -> unit) =
//    mainFunctionContainer [
//        Bulma.field.div [
//            Bulma.field.hasAddons
//            prop.children [
//                Bulma.control.div [
//                    Bulma.dropdown [
//                        if model.JsonExporterModel.ShowTableExportTypeDropdown then Bulma.dropdown.isActive
//                        prop.children [
//                            Bulma.dropdownTrigger [
//                                Daisy.button.a [
//                                    prop.onClick(fun e -> e.stopPropagation(); UpdateShowTableExportTypeDropdown (not model.JsonExporterModel.ShowTableExportTypeDropdown) |> JsonExporterMsg |> dispatch )
//                                    prop.children [
//                                        span [Style [MarginRight "5px"]] [str (model.JsonExporterModel.TableJsonExportType.ToString())]
//                                        Html.i [prop.className "fa-solid fa-angle-down"]
//                                    ]
//                                ]
//                            ]
//                            Bulma.dropdownMenu [
//                                Bulma.dropdownContent [
//                                    let msg = (UpdateTableJsonExportType >> JsonExporterMsg >> dispatch)
//                                    dropdownItem JsonExportType.Assay model msg (model.JsonExporterModel.TableJsonExportType = JsonExportType.Assay)
//                                    dropdownItem JsonExportType.ProcessSeq model msg (model.JsonExporterModel.TableJsonExportType = JsonExportType.ProcessSeq)
//                                ]
//                            ]
//                        ]
//                    ]
//                ]
//                Bulma.control.div [
//                    Bulma.control.isExpanded
//                    Daisy.button.a [
//                        button.info
//                        button.block
//                        prop.onClick(fun _ ->
//                            InterfaceMsg SpreadsheetInterface.ExportJsonTable |> dispatch
//                        )
//                        prop.text "Download as isa json"
//                    ] |> prop.children
//                ]
//            ]
//        ]
//    ]

//let parseTablesToISAJsonEle (model:Model) (dispatch:Messages.Msg -> unit) =
//    mainFunctionContainer [
//        Bulma.field.div [
//            Bulma.field.hasAddons
//            prop.children [
//                Bulma.control.div [
//                    Bulma.dropdown [
//                        if model.JsonExporterModel.ShowWorkbookExportTypeDropdown then Bulma.dropdown.isActive
//                        prop.children [
//                            Bulma.dropdownTrigger [
//                                Daisy.button.a [
//                                    prop.onClick (fun e -> e.stopPropagation(); UpdateShowWorkbookExportTypeDropdown (not model.JsonExporterModel.ShowWorkbookExportTypeDropdown) |> JsonExporterMsg |> dispatch )
//                                    prop.children [
//                                        span [Style [MarginRight "5px"]] [str (model.JsonExporterModel.WorkbookJsonExportType.ToString())]
//                                        Html.i [prop.className "fa-solid fa-angle-down"]
//                                    ]
//                                ]
//                            ]
//                            Bulma.dropdownMenu [
//                                Bulma.dropdownContent [
//                                    let msg = (UpdateWorkbookJsonExportType >> JsonExporterMsg >> dispatch)
//                                    dropdownItem JsonExportType.Assay model msg (model.JsonExporterModel.WorkbookJsonExportType = JsonExportType.Assay)
//                                    dropdownItem JsonExportType.ProcessSeq model msg (model.JsonExporterModel.WorkbookJsonExportType = JsonExportType.ProcessSeq)
//                                ]
//                            ]
//                        ]
//                    ]
//                ]
//                Bulma.control.div [
//                    Bulma.control.isExpanded
//                    Daisy.button.a [
//                        button.info
//                        button.block
//                        prop.onClick(fun _ ->
//                            InterfaceMsg SpreadsheetInterface.ExportJsonTables |> dispatch
//                        )
//                        prop.text "Download as isa json"
//                    ]
//                    |> prop.children
//                ]
//            ]
//        ]
//    ]

//// SND ELEMENT
//open Browser.Types

//let fileUploadButton (model:Model) dispatch (id: string) =
//    Bulma.label [
//        prop.className "mb-2 has-text-weight-normal"
//        prop.children [
//            Bulma.fileInput [
//                prop.id id
//                prop.type' "file";
//                prop.style [style.display.none]
//                prop.onChange (fun (ev: File list) ->
//                    let files = ev//: Browser.Types.FileList = ev.target?files

//                    let blobs =
//                        files
//                        |> List.map (fun f -> f.slice() )

//                    let reader = Browser.Dom.FileReader.Create()

//                    reader.onload <- fun evt ->
//                        let byteArr =
//                            let arraybuffer : Fable.Core.JS.ArrayBuffer = evt.target?result
//                            let uintArr = Fable.Core.JS.Constructors.Uint8Array.Create arraybuffer
//                            uintArr.ToString().Split([|","|], System.StringSplitOptions.RemoveEmptyEntries)
//                            |> Array.map (fun byteStr -> byte byteStr)

//                        StoreXLSXByteArray byteArr |> JsonExporterMsg |> dispatch

//                    reader.onerror <- fun evt ->
//                        curry GenericLog Cmd.none ("Error", evt.Value) |> DevMsg |> dispatch

//                    reader.readAsArrayBuffer(blobs |> List.head)

//                    let picker = Browser.Dom.document.getElementById(id)
//                    // https://stackoverflow.com/questions/3528359/html-input-type-file-file-selection-event/3528376
//                    picker?value <- null
//                )
//            ]
//            Daisy.button.a [
//                button.info;
//                button.block
//                prop.text "Upload Excel file"
//            ]
//        ]
//    ]


//let xlsxUploadAndParsingMainElement (model:Model) (dispatch: Msg -> unit) =
//    let inputId = "xlsxConverter_uploadButton"
//    mainFunctionContainer [
//        // Upload xlsx file to byte []
//        fileUploadButton model dispatch inputId
//        // Request parsing
//        Bulma.field.div [
//            Bulma.field.hasAddons
//            prop.children [
//                Bulma.control.div [
//                    Bulma.dropdown [
//                        if model.JsonExporterModel.ShowXLSXExportTypeDropdown then Bulma.dropdown.isActive
//                        prop.children [
//                            Bulma.dropdownTrigger [
//                                Daisy.button.a [
//                                    prop.onClick (fun e -> e.stopPropagation(); UpdateShowXLSXExportTypeDropdown (not model.JsonExporterModel.ShowXLSXExportTypeDropdown) |> JsonExporterMsg |> dispatch )
//                                    prop.children [
//                                        span [Style [MarginRight "5px"]] [str (model.JsonExporterModel.XLSXParsingExportType.ToString())]
//                                        Html.i [prop.className "fa-solid fa-angle-down"]
//                                    ]
//                                ]
//                            ]
//                            Bulma.dropdownMenu [
//                                Bulma.dropdownContent [
//                                    let msg = (UpdateXLSXParsingExportType >> JsonExporterMsg >> dispatch)
//                                    dropdownItem JsonExportType.Assay model msg (model.JsonExporterModel.XLSXParsingExportType = JsonExportType.Assay)
//                                    dropdownItem JsonExportType.ProcessSeq model msg (model.JsonExporterModel.XLSXParsingExportType = JsonExportType.ProcessSeq)
//                                    dropdownItem JsonExportType.ProtocolTemplate model msg (model.JsonExporterModel.XLSXParsingExportType = JsonExportType.ProtocolTemplate)
//                                ]
//                            ]
//                        ]
//                    ]
//                ]
//                Bulma.control.div [
//                    Bulma.control.isExpanded
//                    Daisy.button.a [
//                        let hasContent = model.JsonExporterModel.XLSXByteArray <> Array.empty
//                        button.info
//                        if hasContent then
//                            Daisy.button.isActive
//                        else
//                            button.error
//                            prop.disabled true
//                        button.block
//                        prop.onClick(fun _ ->
//                            if hasContent then
//                                ParseXLSXToJsonRequest model.JsonExporterModel.XLSXByteArray |> JsonExporterMsg |> dispatch
//                        )
//                        prop.text "Download as isa json"
//                    ]
//                    |> prop.children
//                ]
//            ]
//        ]
//    ]

//let jsonExporterMainElement (model:Model) (dispatch: Messages.Msg -> unit) =

    //Bulma.content [

    //    prop.onSubmit    (fun e -> e.preventDefault())
    //    prop.onKeyDown   (fun k -> if (int k.which) = 13 then k.preventDefault())
    //    prop.onClick     (fun e -> CloseAllDropdowns |> JsonExporterMsg |> dispatch)
    //    prop.style [style.minHeight(length.vh 100)]
    //    prop.children [
    //        Bulma.label "Json Exporter"

    //        Bulma.help [
    //            str "Export swate annotation tables to "
    //            a [Href @"https://en.wikipedia.org/wiki/JSON"] [str "JSON"]
    //            str " format. Official ISA-JSON types can be found "
    //            a [Href @"https://isa-specs.readthedocs.io/en/latest/isajson.html#"] [str "here"]
    //            str "."
    //        ]

    //        Bulma.label "Export active table"

    //        parseTableToISAJsonEle model dispatch

    //        Bulma.label "Export workbook"

    //        parseTablesToISAJsonEle model dispatch

    //        Bulma.label "Export Swate conform xlsx file."

    //        xlsxUploadAndParsingMainElement model dispatch
    //    ]
    //]

type private JsonExportState = {
    ExportFormat: JsonExportFormat
} with
    static member init() = {
        ExportFormat = JsonExportFormat.ROCrate
    }

type FileExporter =

    static member private FileFormat(efm: JsonExportFormat, state: JsonExportState, setState) =
        Html.option [
            prop.text (string efm)
        ]

    [<ReactComponent>]
    static member JsonExport(model: Model, dispatch) =
        let state, setState = React.useState JsonExportState.init
        Html.div [
            Daisy.join [
                prop.children [
                    Daisy.select [
                        join.item
                        select.bordered
                        prop.onChange (fun (e:Browser.Types.Event) ->
                            let jef: JsonExportFormat = JsonExportFormat.fromString (e.target?value)
                            { state with
                                ExportFormat = jef }
                            |> setState
                        )
                        prop.defaultValue(string state.ExportFormat)
                        prop.children [
                            FileExporter.FileFormat(JsonExportFormat.ROCrate, state, setState)
                            FileExporter.FileFormat(JsonExportFormat.ISA, state, setState)
                            FileExporter.FileFormat(JsonExportFormat.ARCtrl, state, setState)
                            FileExporter.FileFormat(JsonExportFormat.ARCtrlCompressed, state, setState)
                        ]
                    ]
                    Daisy.button.button [
                        join.item
                        button.block
                        button.primary
                        prop.text "Download"
                        prop.onClick (fun _ ->
                            let host = model.PersistentStorageState.Host
                            match host with
                            | Some Swatehost.Excel ->
                                promise {
                                    let! result = OfficeInterop.Core.Main.tryParseToArcFile()
                                    match result with
                                    | Result.Ok arcFile -> SpreadsheetInterface.ExportJson (arcFile, state.ExportFormat) |> InterfaceMsg |> dispatch
                                    | Result.Error msgs -> OfficeInterop.SendErrorsToFront msgs |> OfficeInteropMsg |> dispatch
                                } |> ignore
                            | Some Swatehost.Browser | Some Swatehost.ARCitect ->
                                if model.SpreadsheetModel.ArcFile.IsSome then
                                    SpreadsheetInterface.ExportJson (model.SpreadsheetModel.ArcFile.Value, state.ExportFormat) |> InterfaceMsg |> dispatch
                            | _ -> failwith "not implemented"
                        )
                    ]
                ]
            ]
        ]

    static member Main(model:Model, dispatch: Messages.Msg -> unit) =
        SidebarComponents.SidebarLayout.Container [
            SidebarComponents.SidebarLayout.Header "File Export"

            SidebarComponents.SidebarLayout.Description "Export to Json"
            SidebarComponents.SidebarLayout.LogicContainer [
                Html.div [
                    prop.className "prose-sm -mt-4"
                    prop.children [
                        Html.h3 "Export Swate annotation tables to official JSON."
                        Html.ul [
                            Html.li [
                                Html.b "ARCtrl"
                                Html.text ": A simple ARCtrl specific format."
                            ]
                            Html.li [
                                Html.b "ARCtrlCompressed"
                                Html.text ": A compressed ARCtrl specific format."
                            ]
                            Html.li [
                                Html.b "ISA"
                                Html.text ": ISA-JSON format ("
                                Html.a [
                                    prop.target.blank
                                    prop.href "https://isa-specs.readthedocs.io/en/latest/isajson.html#"
                                    prop.text "ISA-JSON"
                                ]
                                Html.text ")."
                            ]
                            Html.li [
                                Html.b "ROCrate"
                                Html.text ": ROCrate format ("
                                Html.a [
                                    prop.target.blank
                                    prop.href "https://www.researchobject.org/ro-crate/"
                                    prop.text "ROCrate"
                                ]
                                Html.text ", "
                                Html.a [
                                    prop.target.blank
                                    prop.href "https://github.com/nfdi4plants/isa-ro-crate-profile/blob/main/profile/isa_ro_crate.md"
                                    prop.text "ISA-Profile"
                                ]
                                Html.text ")."
                            ]
                        ]
                    ]
                ]
                FileExporter.JsonExport(model, dispatch)
            ]
        ]


