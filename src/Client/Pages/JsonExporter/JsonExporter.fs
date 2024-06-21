module JsonExporter.Core

open Fable.React
open Fable.React.Props
open Fable.Core.JsInterop
open Elmish

open Shared

open ExcelColors
open Model

open Messages

open Browser.Dom

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
//open Feliz.Bulma

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
//                                Bulma.button.a [
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
//                    Bulma.button.a [
//                        Bulma.color.isInfo
//                        Bulma.button.isFullWidth
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
//                                Bulma.button.a [
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
//                    Bulma.button.a [
//                        Bulma.color.isInfo
//                        Bulma.button.isFullWidth
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
//            Bulma.button.a [
//                Bulma.color.isInfo;
//                Bulma.button.isFullWidth
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
//                                Bulma.button.a [
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
//                    Bulma.button.a [
//                        let hasContent = model.JsonExporterModel.XLSXByteArray <> Array.empty
//                        Bulma.color.isInfo
//                        if hasContent then
//                            Bulma.button.isActive 
//                        else
//                            Bulma.color.isDanger
//                            prop.disabled true
//                        Bulma.button.isFullWidth
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

//let jsonExporterMainElement (model:Messages.Model) (dispatch: Messages.Msg -> unit) =
    
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

open Feliz
open Feliz.Bulma
open Shared.JsonExport

type private JsonExportState = {
    ExportFormat: JsonExportFormat
} with
    static member init() = {
        ExportFormat = ROCrate
    }

type FileExporter =

    
    static member private FileFormat(efm: JsonExportFormat, state: JsonExportState, setState) =
        let isSelected = efm = state.ExportFormat
        Html.option [
            prop.selected isSelected
            prop.text (string efm)
        ]

    [<ReactComponent>]
    static member JsonExport(model: Messages.Model, dispatch) =
        let state, setState = React.useState JsonExportState.init
        Html.div [
            Bulma.field.div [
                Bulma.field.hasAddons
                prop.children [
                    Bulma.control.p [
                        Html.span [
                            prop.className "select"
                            prop.children [
                                Html.select [
                                    prop.onChange (fun (e:Browser.Types.Event) ->
                                        let jef: JsonExportFormat = JsonExportFormat.fromString (e.target?value)
                                        { state with
                                            ExportFormat = jef }
                                        |> setState
                                    )
                                    prop.children [
                                        FileExporter.FileFormat(ROCrate, state, setState)
                                        FileExporter.FileFormat(ISA, state, setState)
                                        FileExporter.FileFormat(ARCtrl, state, setState)
                                        FileExporter.FileFormat(ARCtrlCompressed, state, setState)
                                    ]
                                ]
                            ]
                        ]
                    ]
                    Bulma.control.p [
                        Bulma.control.isExpanded
                        prop.children [
                            Bulma.button.button [
                                Bulma.button.isFullWidth
                                prop.text "Download"
                                prop.onClick (fun _ ->
                                    if model.SpreadsheetModel.ArcFile.IsSome then
                                        SpreadsheetInterface.ExportJson (model.SpreadsheetModel.ArcFile.Value, state.ExportFormat) |> InterfaceMsg |> dispatch
                                )
                            ]
                        ]
                    ]
                ]
            ]
        ]

    static member Main(model:Messages.Model, dispatch: Messages.Msg -> unit) =
        Html.div [
            pageHeader "File Export"

            Bulma.label "Export to Json"
            mainFunctionContainer [
                Bulma.field.div [
                    Bulma.content [
                        Bulma.help "Export Swate annotation tables to official JSON."
                        Html.ul [
                            Html.li [
                                Html.b "ARCtrl"
                                str ": A simple ARCtrl specific format."
                            ]
                            Html.li [
                                Html.b "ARCtrlCompressed"
                                str ": A compressed ARCtrl specific format."
                            ]
                            Html.li [
                                Html.b "ISA"
                                str ": ISA-JSON format ("
                                Html.a [
                                    prop.target.blank
                                    prop.href "https://isa-specs.readthedocs.io/en/latest/isajson.html#"
                                    prop.text "ISA-JSON"
                                ]
                                str ")."
                            ]
                            Html.li [
                                Html.b "ROCrate"
                                str ": ROCrate format ("
                                Html.a [
                                    prop.target.blank
                                    prop.href "https://www.researchobject.org/ro-crate/"
                                    prop.text "ROCrate"
                                ]
                                str ", "
                                Html.a [
                                    prop.target.blank
                                    prop.href "https://github.com/nfdi4plants/isa-ro-crate-profile/blob/main/profile/isa_ro_crate.md"
                                    prop.text "ISA-Profile"
                                ]
                                str ")."
                            ]
                        ]
                    ]
                ]
                FileExporter.JsonExport(model, dispatch)
            ]
        ]


