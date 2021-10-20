module JSONExporter

open Fable.React
open Fable.React.Props
open Fulma
open Fulma.Extensions.Wikiki
open Fable.FontAwesome
open Fable.Core.JsInterop
open Elmish

open Shared

open ExcelColors
open Model

open Shared.OfficeInteropTypes
open Validation
open Messages
open JSONExporter

open Browser.Dom
open Fable.Core.JsInterop


let download(filename, text) =
  let element = document.createElement("a");
  element.setAttribute("href", "data:text/plain;charset=utf-8," +  Fable.Core.JS.encodeURIComponent(text));
  element.setAttribute("download", filename);

  element?style?display <- "None";
  let _ = document.body.appendChild(element);

  element.click();

  document.body.removeChild(element);

let update (msg:Msg) (currentModel: Messages.Model) : Messages.Model * Cmd<Messages.Msg> =
    match msg with
    // Style
    | UpdateLoading isLoading ->
        let nextModel = {
            currentModel.JSONExporterModel with
                Loading = isLoading
        }
        currentModel.updateByJSONExporterModel nextModel, Cmd.none
    | UpdateShowTableExportTypeDropdown nextVal ->
        let nextModel = {
            currentModel.JSONExporterModel with
                ShowTableExportTypeDropdown = nextVal
                ShowWorkbookExportTypeDropdown = false
                ShowXLSXExportTypeDropdown = false
        }
        currentModel.updateByJSONExporterModel nextModel, Cmd.none
    | UpdateShowWorkbookExportTypeDropdown nextVal ->
        let nextModel = {
            currentModel.JSONExporterModel with
                ShowTableExportTypeDropdown = false
                ShowWorkbookExportTypeDropdown = nextVal
                ShowXLSXExportTypeDropdown = false
        }
        currentModel.updateByJSONExporterModel nextModel, Cmd.none
    | UpdateShowXLSXExportTypeDropdown nextVal ->
        let nextModel = {
            currentModel.JSONExporterModel with
                ShowTableExportTypeDropdown = false
                ShowWorkbookExportTypeDropdown = false
                ShowXLSXExportTypeDropdown = nextVal
        }
        currentModel.updateByJSONExporterModel nextModel, Cmd.none
    | UpdateTableJSONExportType nextType ->
        let nextModel = {
            currentModel.JSONExporterModel with
                TableJSONExportType             = nextType
                ShowTableExportTypeDropdown     = false
        }
        currentModel.updateByJSONExporterModel nextModel, Cmd.none
    | UpdateWorkbookJSONExportType nextType ->
        let nextModel = {
            currentModel.JSONExporterModel with
                WorkbookJSONExportType          = nextType
                ShowWorkbookExportTypeDropdown  = false
        }
        currentModel.updateByJSONExporterModel nextModel, Cmd.none
    | UpdateXLSXParsingExportType nextType ->
        let nextModel = {
            currentModel.JSONExporterModel with
                XLSXParsingExportType           = nextType
                ShowXLSXExportTypeDropdown      = false
        }
        currentModel.updateByJSONExporterModel nextModel, Cmd.none
    | CloseAllDropdowns ->
        let nextModel = {
            currentModel.JSONExporterModel with
                ShowTableExportTypeDropdown = false
                ShowWorkbookExportTypeDropdown = false
                ShowXLSXExportTypeDropdown = false
        }
        currentModel.updateByJSONExporterModel nextModel, Cmd.none
    //
    | ParseTableOfficeInteropRequest ->
        let nextModel = {
            currentModel.JSONExporterModel with
                Loading             = true
        }
        let cmd =
            Cmd.OfPromise.either
                OfficeInterop.getBuildingBlocksAndSheet
                ()
                (ParseTableServerRequest >> JSONExporterMsg)
                (curry GenericError (UpdateLoading false |> JSONExporterMsg |> Cmd.ofMsg) >> DevMsg)
        currentModel.updateByJSONExporterModel nextModel, cmd
    | ParseTableServerRequest (worksheetName, buildingBlocks) ->
        let nextModel = {
            currentModel.JSONExporterModel with
                CurrentExportType   = Some currentModel.JSONExporterModel.TableJSONExportType
                Loading             = true
        }
        let api =
            match currentModel.JSONExporterModel.TableJSONExportType with
            | JSONExportType.ProcessSeq ->
                Api.swateJsonAPIv1.parseAnnotationTableToProcessSeqJson
            | JSONExportType.Assay ->
                Api.swateJsonAPIv1.parseAnnotationTableToAssayJson
            | JSONExportType.Table ->
                Api.swateJsonAPIv1.parseAnnotationTableToTableJson
            | anythingElse -> failwith $"Cannot parse \"{anythingElse.ToString()}\" with this endpoint."
        let cmd =
            Cmd.OfAsync.either
                api
                (worksheetName, buildingBlocks)
                (ParseTableServerResponse >> JSONExporterMsg)
                (curry GenericError (UpdateLoading false |> JSONExporterMsg |> Cmd.ofMsg) >> DevMsg)

        currentModel.updateByJSONExporterModel nextModel, cmd
    //
    | ParseTablesOfficeInteropRequest ->
        let cmd =
            Cmd.OfPromise.either
                OfficeInterop.getBuildingBlocksAndSheets
                ()
                (ParseTablesServerRequest >> JSONExporterMsg)
                (curry GenericError (UpdateLoading false |> JSONExporterMsg |> Cmd.ofMsg) >> DevMsg)
        currentModel, cmd
    | ParseTablesServerRequest (worksheetBuildingBlocksTuple) ->
        let nextModel = {
            currentModel.JSONExporterModel with
                CurrentExportType   = Some currentModel.JSONExporterModel.WorkbookJSONExportType
                Loading             = true
        }
        let api =
            match currentModel.JSONExporterModel.WorkbookJSONExportType with
            | JSONExportType.ProcessSeq ->
                Api.swateJsonAPIv1.parseAnnotationTablesToProcessSeqJson
            | JSONExportType.Assay ->
                Api.swateJsonAPIv1.parseAnnotationTablesToAssayJson
            | JSONExportType.Table ->
                Api.swateJsonAPIv1.parseAnnotationTablesToTableJson
            | anythingElse -> failwith $"Cannot parse \"{anythingElse.ToString()}\" with this endpoint."
        let cmd =
            Cmd.OfAsync.either
                api
                worksheetBuildingBlocksTuple
                (ParseTableServerResponse >> JSONExporterMsg)
                (curry GenericError (UpdateLoading false |> JSONExporterMsg |> Cmd.ofMsg) >> DevMsg)

        currentModel.updateByJSONExporterModel nextModel, cmd
    //
    | ParseTableServerResponse parsedJson ->
        let n = System.DateTime.Now.ToUniversalTime().ToString("yyyymmdd_hhMMss")
        let jsonName = Option.bind (fun x -> Some <| "_" + x.ToString()) currentModel.JSONExporterModel.CurrentExportType |> Option.defaultValue ""
        let _ = download ($"{n}{jsonName}.json",parsedJson)
        let nextModel = {
            currentModel.JSONExporterModel with
                Loading             = false
                CurrentExportType   = None
        }
        currentModel.updateByJSONExporterModel nextModel, Cmd.none
    //
    | StoreXLSXByteArray byteArr ->
        let nextModel = {
            currentModel.JSONExporterModel with
                XLSXByteArray = byteArr
        }
        currentModel.updateByJSONExporterModel nextModel , Cmd.none
    | ParseXLSXToJsonRequest byteArr ->
        let nextModel = {
            currentModel.JSONExporterModel with
                CurrentExportType   = Some currentModel.JSONExporterModel.XLSXParsingExportType
                Loading             = true
        }
        let apif =
            match currentModel.JSONExporterModel.XLSXParsingExportType with
            | JSONExportType.ProcessSeq        -> Api.isaDotNetCommonApi.toProcessSeqJSON
            | JSONExportType.Assay             -> Api.isaDotNetCommonApi.toAssayJSON
            | JSONExportType.Table             -> Api.isaDotNetCommonApi.toTableJSON
            | JSONExportType.ProtocolTemplate  -> Api.isaDotNetCommonApi.toParsedSwateTemplate
        let cmd =
            Cmd.OfAsync.either
                apif
                byteArr
                (fun x -> x.ToString() |> (ParseXLSXToJsonResponse >> JSONExporterMsg))
                (curry GenericError (UpdateLoading false |> JSONExporterMsg |> Cmd.ofMsg) >> DevMsg)
        currentModel.updateByJSONExporterModel nextModel, cmd
    | ParseXLSXToJsonResponse jsonStr ->
        let n = System.DateTime.Now.ToUniversalTime().ToString("yyyymmdd_hhMMss")
        let jsonName = Option.bind (fun x -> Some <| "_" + x.ToString()) currentModel.JSONExporterModel.CurrentExportType |> Option.defaultValue ""
        let _ = download ($"{n}{jsonName}.json",jsonStr)
        let nextModel = {
            currentModel.JSONExporterModel with
                Loading = false
        }
        currentModel.updateByJSONExporterModel nextModel, Cmd.none

open Messages

let dropdownItem (exportType:JSONExportType) (model:Model) msg (isActive:bool) =
    Dropdown.Item.a [
        Dropdown.Item.Props [
            TabIndex 0
            OnClick (fun e ->
                e.stopPropagation()
                exportType |> msg
            )
            OnKeyDown (fun k -> if (int k.which) = 13 then exportType |> msg)
            Style [if isActive then BackgroundColor model.SiteStyleState.ColorMode.ControlForeground]
        ]

    ][
        Text.span [
            CustomClass (Tooltip.ClassName + " " + Tooltip.IsTooltipRight + " " + Tooltip.IsMultiline)
            Props [
                Tooltip.dataTooltip (exportType.toExplanation)
                Style [FontSize "1.1rem"; PaddingRight "10px"; TextAlign TextAlignOptions.Center; Color NFDIColors.Yellow.Darker20]
            ]
        ] [
            Fa.i [Fa.Solid.InfoCircle] []
        ]

        Text.span [] [str (exportType.ToString())]
    ]

let parseTableToISAJsonEle (model:Model) (dispatch:Messages.Msg -> unit) =
    mainFunctionContainer [
        Field.div [Field.HasAddons][
            Control.div [][
                Dropdown.dropdown [
                    Dropdown.IsActive model.JSONExporterModel.ShowTableExportTypeDropdown
                ][
                    Dropdown.trigger [][
                        Button.a [
                            Button.OnClick (fun e -> e.stopPropagation(); UpdateShowTableExportTypeDropdown (not model.JSONExporterModel.ShowTableExportTypeDropdown) |> JSONExporterMsg |> dispatch )
                        ][
                            span [Style [MarginRight "5px"]] [str (model.JSONExporterModel.TableJSONExportType.ToString())]
                            Fa.i [Fa.Solid.AngleDown] []
                        ]
                    ]
                    Dropdown.menu [][
                        Dropdown.content [][
                            let msg = (UpdateTableJSONExportType >> JSONExporterMsg >> dispatch)
                            dropdownItem JSONExportType.Assay model msg (model.JSONExporterModel.TableJSONExportType = JSONExportType.Assay)
                            dropdownItem JSONExportType.Table model msg (model.JSONExporterModel.TableJSONExportType = JSONExportType.Table)
                            dropdownItem JSONExportType.ProcessSeq model msg (model.JSONExporterModel.TableJSONExportType = JSONExportType.ProcessSeq)
                        ]
                    ]
                ]
            ]
            Control.div [Control.IsExpanded][
                Button.a [
                    Button.Color IsInfo
                    Button.IsFullWidth
                    Button.OnClick(fun e ->
                        JSONExporterMsg ParseTableOfficeInteropRequest |> dispatch
                    )
                ][
                    str "Download as isa json"
                ]
            ]
        ]
    ]

let parseTablesToISAJsonEle (model:Model) (dispatch:Messages.Msg -> unit) =
    mainFunctionContainer [
        Field.div [Field.HasAddons][
            Control.div [][
                Dropdown.dropdown [
                    Dropdown.IsActive model.JSONExporterModel.ShowWorkbookExportTypeDropdown
                ][
                    Dropdown.trigger [][
                        Button.a [
                            Button.OnClick (fun e -> e.stopPropagation(); UpdateShowWorkbookExportTypeDropdown (not model.JSONExporterModel.ShowWorkbookExportTypeDropdown) |> JSONExporterMsg |> dispatch )
                        ][
                            span [Style [MarginRight "5px"]] [str (model.JSONExporterModel.WorkbookJSONExportType.ToString())]
                            Fa.i [Fa.Solid.AngleDown] []
                        ]
                    ]
                    Dropdown.menu [][
                        Dropdown.content [][
                            let msg = (UpdateWorkbookJSONExportType >> JSONExporterMsg >> dispatch)
                            dropdownItem JSONExportType.Assay model msg (model.JSONExporterModel.WorkbookJSONExportType = JSONExportType.Assay)
                            dropdownItem JSONExportType.Table model msg (model.JSONExporterModel.WorkbookJSONExportType = JSONExportType.Table)
                            dropdownItem JSONExportType.ProcessSeq model msg (model.JSONExporterModel.WorkbookJSONExportType = JSONExportType.ProcessSeq)
                        ]
                    ]
                ]
            ]
            Control.div [Control.IsExpanded][
                Button.a [
                    Button.Color IsInfo
                    Button.IsFullWidth
                    Button.OnClick(fun e ->
                        JSONExporterMsg ParseTablesOfficeInteropRequest |> dispatch
                    )
                ][
                    str "Download as isa json"
                ]
            ]
        ]
    ]

// SND ELEMENT

let fileUploadButton (model:Model) dispatch id =
    Label.label [Label.Props [Style [FontWeight "normal";MarginBottom "0.5rem"]]][
        Input.input [
            Input.Props [
                Id id
                Props.Type "file"; Style [Display DisplayOptions.None]
                OnChange (fun ev ->
                    let files : Browser.Types.FileList = ev.target?files

                    let blobs =
                        [ for i=0 to (files.length - 1) do yield files.item i ]
                        |> List.map (fun f -> f.slice() )

                    let reader = Browser.Dom.FileReader.Create()

                    reader.onload <- fun evt ->
                        let byteArr =
                            let arraybuffer : Fable.Core.JS.ArrayBuffer = evt.target?result
                            let uintArr = Fable.Core.JS.Constructors.Uint8Array.Create arraybuffer
                            uintArr.ToString().Split([|","|], System.StringSplitOptions.RemoveEmptyEntries)
                            |> Array.map (fun byteStr -> byte byteStr)
                            
                        StoreXLSXByteArray byteArr |> JSONExporterMsg |> dispatch
                                   
                    reader.onerror <- fun evt ->
                        curry GenericLog Cmd.none ("Error", evt.Value) |> DevMsg |> dispatch

                    reader.readAsArrayBuffer(blobs |> List.head)

                    let picker = Browser.Dom.document.getElementById(id)
                    // https://stackoverflow.com/questions/3528359/html-input-type-file-file-selection-event/3528376
                    picker?value <- null
                )
            ]
        ]
        Button.a [Button.Color Color.IsInfo; Button.IsFullWidth][
            str "Upload Excel file"
        ]
    ]


let xlsxUploadAndParsingMainElement (model:Model) (dispatch: Msg -> unit) =
    let inputId = "xlsxConverter_uploadButton"
    mainFunctionContainer [
        /// Upload xlsx file to byte []
        fileUploadButton model dispatch inputId
        /// Request parsing
        Field.div [Field.HasAddons][
            Control.div [][
                Dropdown.dropdown [
                    Dropdown.IsActive model.JSONExporterModel.ShowXLSXExportTypeDropdown
                ][
                    Dropdown.trigger [][
                        Button.a [
                            Button.OnClick (fun e -> e.stopPropagation(); UpdateShowXLSXExportTypeDropdown (not model.JSONExporterModel.ShowXLSXExportTypeDropdown) |> JSONExporterMsg |> dispatch )
                        ][
                            span [Style [MarginRight "5px"]] [str (model.JSONExporterModel.XLSXParsingExportType.ToString())]
                            Fa.i [Fa.Solid.AngleDown] []
                        ]
                    ]
                    Dropdown.menu [][
                        Dropdown.content [][
                            let msg = (UpdateXLSXParsingExportType >> JSONExporterMsg >> dispatch)
                            dropdownItem JSONExportType.Assay model msg (model.JSONExporterModel.XLSXParsingExportType = JSONExportType.Assay)
                            dropdownItem JSONExportType.Table model msg (model.JSONExporterModel.XLSXParsingExportType = JSONExportType.Table)
                            dropdownItem JSONExportType.ProcessSeq model msg (model.JSONExporterModel.XLSXParsingExportType = JSONExportType.ProcessSeq)
                            dropdownItem JSONExportType.ProtocolTemplate model msg (model.JSONExporterModel.XLSXParsingExportType = JSONExportType.ProtocolTemplate)
                        ]
                    ]
                ]
            ]
            Control.div [Control.IsExpanded][
                Button.a [
                    let hasContent = model.JSONExporterModel.XLSXByteArray <> Array.empty
                    Button.Color IsInfo
                    if hasContent then
                        Button.IsActive true
                    else
                        Button.Color Color.IsDanger
                        Button.Props [Disabled true]
                    Button.IsFullWidth
                    Button.OnClick(fun e ->
                        ParseXLSXToJsonRequest model.JSONExporterModel.XLSXByteArray |> JSONExporterMsg |> dispatch
                    )
                ][
                    str "Download as isa json"
                ]
            ]
        ]
    ]


let jsonExporterMainElement (model:Messages.Model) (dispatch: Messages.Msg -> unit) =
    
    Content.content [ Content.Props [
        OnSubmit    (fun e -> e.preventDefault())
        OnKeyDown   (fun k -> if (int k.which) = 13 then k.preventDefault())
        OnClick     (fun e -> CloseAllDropdowns |> JSONExporterMsg |> dispatch)
        Style [Height "100vh"]
    ]] [

        Label.label [Label.Size Size.IsLarge; Label.Props [Style [Color model.SiteStyleState.ColorMode.Accent]]][ str "JSON Exporter"]

        Help.help [][
            str "Export swate annotation tables to "
            a [Href @"https://en.wikipedia.org/wiki/JSON"][str "JSON"]
            str " format. Official ISA-JSON types can be found "
            a [Href @"https://isa-specs.readthedocs.io/en/latest/isajson.html#"][str "here"]
            str "."
        ]

        Label.label [Label.Props [Style [Color model.SiteStyleState.ColorMode.Accent]]] [str "Export active table"]

        parseTableToISAJsonEle model dispatch

        Label.label [Label.Props [Style [Color model.SiteStyleState.ColorMode.Accent]]] [str "Export workbook"]
        
        parseTablesToISAJsonEle model dispatch

        Label.label [Label.Props [Style [Color model.SiteStyleState.ColorMode.Accent]]] [str "Export Swate conform xlsx file."]

        xlsxUploadAndParsingMainElement model dispatch
    ]