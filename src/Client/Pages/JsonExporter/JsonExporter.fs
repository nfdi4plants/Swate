module JsonExporter.Core

open Fable.React
open Fable.React.Props
open Fable.Core.JsInterop
open Elmish

open Shared

open ExcelColors
open Model

open Messages
open JsonExporter

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

let update (msg:JsonExporter.Msg) (currentModel: Messages.Model) : Messages.Model * Cmd<Messages.Msg> =
    match msg with
    // Style
    | UpdateLoading isLoading ->
        let nextModel = { currentModel with Messages.Model.JsonExporterModel.Loading = isLoading }
        nextModel, Cmd.none
    | UpdateShowTableExportTypeDropdown nextVal ->
        let nextModel = {
            currentModel.JsonExporterModel with
                ShowTableExportTypeDropdown = nextVal
                ShowWorkbookExportTypeDropdown = false
                ShowXLSXExportTypeDropdown = false
        }
        currentModel.updateByJsonExporterModel nextModel, Cmd.none
    | UpdateShowWorkbookExportTypeDropdown nextVal ->
        let nextModel = {
            currentModel.JsonExporterModel with
                ShowTableExportTypeDropdown = false
                ShowWorkbookExportTypeDropdown = nextVal
                ShowXLSXExportTypeDropdown = false
        }
        currentModel.updateByJsonExporterModel nextModel, Cmd.none
    | UpdateShowXLSXExportTypeDropdown nextVal ->
        let nextModel = {
            currentModel.JsonExporterModel with
                ShowTableExportTypeDropdown = false
                ShowWorkbookExportTypeDropdown = false
                ShowXLSXExportTypeDropdown = nextVal
        }
        currentModel.updateByJsonExporterModel nextModel, Cmd.none
    | UpdateTableJsonExportType nextType ->
        let nextModel = {
            currentModel.JsonExporterModel with
                TableJsonExportType             = nextType
                ShowTableExportTypeDropdown     = false
        }
        currentModel.updateByJsonExporterModel nextModel, Cmd.none
    | UpdateWorkbookJsonExportType nextType ->
        let nextModel = {
            currentModel.JsonExporterModel with
                WorkbookJsonExportType          = nextType
                ShowWorkbookExportTypeDropdown  = false
        }
        currentModel.updateByJsonExporterModel nextModel, Cmd.none
    | UpdateXLSXParsingExportType nextType ->
        let nextModel = {
            currentModel.JsonExporterModel with
                XLSXParsingExportType           = nextType
                ShowXLSXExportTypeDropdown      = false
        }
        currentModel.updateByJsonExporterModel nextModel, Cmd.none
    | CloseAllDropdowns ->
        let nextModel = {
            currentModel.JsonExporterModel with
                ShowTableExportTypeDropdown = false
                ShowWorkbookExportTypeDropdown = false
                ShowXLSXExportTypeDropdown = false
        }
        currentModel.updateByJsonExporterModel nextModel, Cmd.none
    //
    | ParseTableOfficeInteropRequest ->
        let nextModel = {
            currentModel.JsonExporterModel with
                Loading             = true
        }
        let cmd =
            Cmd.OfPromise.either
                OfficeInterop.Core.getBuildingBlocksAndSheet
                ()
                (ParseTableServerRequest >> JsonExporterMsg)
                (curry GenericError (UpdateLoading false |> JsonExporterMsg |> Cmd.ofMsg) >> DevMsg)
        currentModel.updateByJsonExporterModel nextModel, cmd
    | ParseTableServerRequest (worksheetName, buildingBlocks) ->
        let nextModel = {
            currentModel.JsonExporterModel with
                CurrentExportType   = Some currentModel.JsonExporterModel.TableJsonExportType
                Loading             = true
        }
        let api =
            match currentModel.JsonExporterModel.TableJsonExportType with
            | JsonExportType.Assay ->
                Api.swateJsonAPIv1.parseAnnotationTableToAssayJson
            | JsonExportType.ProcessSeq ->
                Api.swateJsonAPIv1.parseAnnotationTableToProcessSeqJson
            | anythingElse -> failwith $"Cannot parse \"{anythingElse.ToString()}\" with this endpoint."
        let cmd =
            Cmd.OfAsync.either
                api
                (worksheetName, buildingBlocks)
                (ParseTableServerResponse >> JsonExporterMsg)
                (curry GenericError (UpdateLoading false |> JsonExporterMsg |> Cmd.ofMsg) >> DevMsg)
        currentModel.updateByJsonExporterModel nextModel, cmd
    //
    | ParseTablesOfficeInteropRequest ->
        let cmd =
            Cmd.OfPromise.either
                OfficeInterop.Core.getBuildingBlocksAndSheets
                ()
                (ParseTablesServerRequest >> JsonExporterMsg)
                (curry GenericError (UpdateLoading false |> JsonExporterMsg |> Cmd.ofMsg) >> DevMsg)
        currentModel, cmd
    | ParseTablesServerRequest (worksheetBuildingBlocksTuple) ->
        let nextModel = {
            currentModel.JsonExporterModel with
                CurrentExportType   = Some currentModel.JsonExporterModel.WorkbookJsonExportType
                Loading             = true
        }
        let api =
            match currentModel.JsonExporterModel.WorkbookJsonExportType with
            | JsonExportType.ProcessSeq ->
                Api.swateJsonAPIv1.parseAnnotationTablesToProcessSeqJson
            | JsonExportType.Assay ->
                Api.swateJsonAPIv1.parseAnnotationTablesToAssayJson
            | anythingElse -> failwith $"Cannot parse \"{anythingElse.ToString()}\" with this endpoint."
        let cmd =
            Cmd.OfAsync.either
                api
                worksheetBuildingBlocksTuple
                (ParseTableServerResponse >> JsonExporterMsg)
                (curry GenericError (UpdateLoading false |> JsonExporterMsg |> Cmd.ofMsg) >> DevMsg)

        currentModel.updateByJsonExporterModel nextModel, cmd
    //
    | ParseTableServerResponse parsedJson ->
        let n = System.DateTime.Now.ToUniversalTime().ToString("yyyyMMdd_hhmmss")
        let jsonName = Option.bind (fun x -> Some <| "_" + x.ToString()) currentModel.JsonExporterModel.CurrentExportType |> Option.defaultValue ""
        let _ = download ($"{n}{jsonName}.json",parsedJson)
        let nextModel = {
            currentModel.JsonExporterModel with
                Loading             = false
                CurrentExportType   = None
        }
        currentModel.updateByJsonExporterModel nextModel, Cmd.none
    //
    | StoreXLSXByteArray byteArr ->
        let nextModel = {
            currentModel.JsonExporterModel with
                XLSXByteArray = byteArr
        }
        currentModel.updateByJsonExporterModel nextModel , Cmd.none
    | ParseXLSXToJsonRequest byteArr ->
        let nextModel = {
            currentModel.JsonExporterModel with
                CurrentExportType   = Some currentModel.JsonExporterModel.XLSXParsingExportType
                Loading             = true
        }
        let apif =
            match currentModel.JsonExporterModel.XLSXParsingExportType with
            | JsonExportType.ProcessSeq        -> Api.isaDotNetCommonApi.toProcessSeqJsonStr
            | JsonExportType.Assay             -> Api.isaDotNetCommonApi.toAssayJsonStr
            | JsonExportType.ProtocolTemplate  -> Api.isaDotNetCommonApi.toSwateTemplateJsonStr
        let cmd =
            Cmd.OfAsync.either
                apif
                byteArr
                (ParseXLSXToJsonResponse >> JsonExporterMsg)
                (curry GenericError (UpdateLoading false |> JsonExporterMsg |> Cmd.ofMsg) >> DevMsg)
        currentModel.updateByJsonExporterModel nextModel, cmd
    | ParseXLSXToJsonResponse jsonStr ->
        let n = System.DateTime.Now.ToUniversalTime().ToString("yyyyMMdd_hhmmss")
        let jsonName = Option.bind (fun x -> Some <| "_" + x.ToString()) currentModel.JsonExporterModel.CurrentExportType |> Option.defaultValue ""
        let _ = download ($"{n}{jsonName}.json",jsonStr)
        let nextModel = {
            currentModel.JsonExporterModel with
                Loading = false
        }

        currentModel.updateByJsonExporterModel nextModel, Cmd.none

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

module FileExporterAux =

    open ARCtrl.ISA
    open ARCtrl.ISA.Json

    [<RequireQualifiedAccess>]
    type ISA =
    | Assay
    | Study
    | Investigation
    
        static member initFromArcfile(arcfile: ArcFiles) =
            match arcfile with
            | ArcFiles.Assay _ -> Some Assay
            | ArcFiles.Study _ -> Some Study
            | ArcFiles.Investigation _ -> Some Investigation
            | ArcFiles.Template _ -> None

    let toISAJsonString (arcfile: ArcFiles option) =
        let timed = fun s -> System.DateTime.Now.ToString("yyyyMMdd_hhmm_") + s
        match arcfile with
        | None | Some (ArcFiles.Template _) -> None
        | Some (ArcFiles.Assay a) -> (timed "assay.json",ArcAssay.toJsonString a) |> Some
        | Some (ArcFiles.Study (s,as')) -> (timed "study.json", ArcStudy.toJsonString s (ResizeArray as')) |> Some
        | Some (ArcFiles.Investigation i) -> (timed "investigation.json", ArcInvestigation.toJsonString i) |> Some

open FileExporterAux

type FileExporter =

    [<ReactComponent>]
    static member JsonExport(model: Messages.Model, dispatch) =
        let isa, setIsa = React.useState(model.SpreadsheetModel.ArcFile |> Option.bind ISA.initFromArcfile)
        Html.div [
            Bulma.field.div [
                Bulma.field.hasAddons
                prop.children [
                    Bulma.control.p [
                        //Html.span [
                        //    prop.className "select"
                        //    prop.children [
                        //        Html.select [
                        //            Html.option "Test"
                        //            Html.option "Test2"
                        //            Html.option "Test3"
                        //        ]
                        //    ]
                        //]
                        Bulma.button.a [
                            prop.text "ISA"
                            Bulma.button.isStatic
                        ]
                    ]
                    Bulma.control.p [
                        Bulma.control.isExpanded
                        prop.children [
                            Bulma.button.button [
                                Bulma.button.isFullWidth
                                prop.text "Download"
                                if isa.IsNone then 
                                    prop.disabled true
                                prop.onClick (fun _ -> 
                                    let r = toISAJsonString model.SpreadsheetModel.ArcFile
                                    r |> Option.iter (fun r -> download r)
                                )
                            ]
                        ]
                    ]
                ]
            ]
            if isa.IsNone then
                Bulma.help [
                    Bulma.color.isDanger
                    prop.text "Unable to convert Template to ISA-JSON"
                ]
        ]

    static member Main(model:Messages.Model, dispatch: Messages.Msg -> unit) =
        Html.div [
            pageHeader "File Export"

            Bulma.label "Export to Json"
            mainFunctionContainer [
                Bulma.field.div [Bulma.help [
                    str "Export Swate annotation tables to official ISA-JSON ("
                    a [Href @"https://isa-specs.readthedocs.io/en/latest/isajson.html#"; Target "_Blank"] [str "more"]
                    str ")."
                ]]
                FileExporter.JsonExport(model, dispatch)
            ]
        ]


