module JsonExporter.Core

open Fable.React
open Fable.React.Props
open Fulma
open Fable.FontAwesome
open Fable.Core.JsInterop
open Elmish

open Shared

open ExcelColors
open Model

open Messages
open JsonExporter.State

open Browser.Dom

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
            currentModel.JsonExporterModel with
                Loading = isLoading
        }
        currentModel.updateByJsonExporterModel nextModel, Cmd.none
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

open Messages

let dropdownItem (exportType:JsonExportType) (model:Model) msg (isActive:bool) =
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

    ] [
        Text.span [
            CustomClass "has-tooltip-right has-tooltip-multiline"
            Props [
                Props.Custom ("data-tooltip", exportType.toExplanation)
                Style [FontSize "1.1rem"; PaddingRight "10px"; TextAlign TextAlignOptions.Center; Color NFDIColors.Yellow.Darker20]
            ]
        ] [
            Fa.i [Fa.Solid.InfoCircle] []
        ]

        Text.span [] [str (exportType.ToString())]
    ]

let parseTableToISAJsonEle (model:Model) (dispatch:Messages.Msg -> unit) =
    mainFunctionContainer [
        Field.div [Field.HasAddons] [
            Control.div [] [
                Dropdown.dropdown [
                    Dropdown.IsActive model.JsonExporterModel.ShowTableExportTypeDropdown
                ] [
                    Dropdown.trigger [] [
                        Button.a [
                            Button.OnClick (fun e -> e.stopPropagation(); UpdateShowTableExportTypeDropdown (not model.JsonExporterModel.ShowTableExportTypeDropdown) |> JsonExporterMsg |> dispatch )
                        ] [
                            span [Style [MarginRight "5px"]] [str (model.JsonExporterModel.TableJsonExportType.ToString())]
                            Fa.i [Fa.Solid.AngleDown] []
                        ]
                    ]
                    Dropdown.menu [] [
                        Dropdown.content [] [
                            let msg = (UpdateTableJsonExportType >> JsonExporterMsg >> dispatch)
                            dropdownItem JsonExportType.Assay model msg (model.JsonExporterModel.TableJsonExportType = JsonExportType.Assay)
                            dropdownItem JsonExportType.ProcessSeq model msg (model.JsonExporterModel.TableJsonExportType = JsonExportType.ProcessSeq)
                        ]
                    ]
                ]
            ]
            Control.div [Control.IsExpanded] [
                Button.a [
                    Button.Color IsInfo
                    Button.IsFullWidth
                    Button.OnClick(fun _ ->
                        InterfaceMsg SpreadsheetInterface.ExportJsonTable |> dispatch
                    )
                ] [
                    str "Download as isa json"
                ]
            ]
        ]
    ]

let parseTablesToISAJsonEle (model:Model) (dispatch:Messages.Msg -> unit) =
    mainFunctionContainer [
        Field.div [Field.HasAddons] [
            Control.div [] [
                Dropdown.dropdown [
                    Dropdown.IsActive model.JsonExporterModel.ShowWorkbookExportTypeDropdown
                ] [
                    Dropdown.trigger [] [
                        Button.a [
                            Button.OnClick (fun e -> e.stopPropagation(); UpdateShowWorkbookExportTypeDropdown (not model.JsonExporterModel.ShowWorkbookExportTypeDropdown) |> JsonExporterMsg |> dispatch )
                        ] [
                            span [Style [MarginRight "5px"]] [str (model.JsonExporterModel.WorkbookJsonExportType.ToString())]
                            Fa.i [Fa.Solid.AngleDown] []
                        ]
                    ]
                    Dropdown.menu [] [
                        Dropdown.content [] [
                            let msg = (UpdateWorkbookJsonExportType >> JsonExporterMsg >> dispatch)
                            dropdownItem JsonExportType.Assay model msg (model.JsonExporterModel.WorkbookJsonExportType = JsonExportType.Assay)
                            dropdownItem JsonExportType.ProcessSeq model msg (model.JsonExporterModel.WorkbookJsonExportType = JsonExportType.ProcessSeq)
                        ]
                    ]
                ]
            ]
            Control.div [Control.IsExpanded] [
                Button.a [
                    Button.Color IsInfo
                    Button.IsFullWidth
                    Button.OnClick(fun _ ->
                        InterfaceMsg SpreadsheetInterface.ExportJsonTables |> dispatch
                    )
                ] [
                    str "Download as isa json"
                ]
            ]
        ]
    ]

// SND ELEMENT

let fileUploadButton (model:Model) dispatch id =
    Label.label [Label.Props [Style [FontWeight "normal";MarginBottom "0.5rem"]]] [
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
                            
                        StoreXLSXByteArray byteArr |> JsonExporterMsg |> dispatch
                                   
                    reader.onerror <- fun evt ->
                        curry GenericLog Cmd.none ("Error", evt.Value) |> DevMsg |> dispatch

                    reader.readAsArrayBuffer(blobs |> List.head)

                    let picker = Browser.Dom.document.getElementById(id)
                    // https://stackoverflow.com/questions/3528359/html-input-type-file-file-selection-event/3528376
                    picker?value <- null
                )
            ]
        ]
        Button.a [Button.Color Color.IsInfo; Button.IsFullWidth] [
            str "Upload Excel file"
        ]
    ]


let xlsxUploadAndParsingMainElement (model:Model) (dispatch: Msg -> unit) =
    let inputId = "xlsxConverter_uploadButton"
    mainFunctionContainer [
        // Upload xlsx file to byte []
        fileUploadButton model dispatch inputId
        // Request parsing
        Field.div [Field.HasAddons] [
            Control.div [] [
                Dropdown.dropdown [
                    Dropdown.IsActive model.JsonExporterModel.ShowXLSXExportTypeDropdown
                ] [
                    Dropdown.trigger [] [
                        Button.a [
                            Button.OnClick (fun e -> e.stopPropagation(); UpdateShowXLSXExportTypeDropdown (not model.JsonExporterModel.ShowXLSXExportTypeDropdown) |> JsonExporterMsg |> dispatch )
                        ] [
                            span [Style [MarginRight "5px"]] [str (model.JsonExporterModel.XLSXParsingExportType.ToString())]
                            Fa.i [Fa.Solid.AngleDown] []
                        ]
                    ]
                    Dropdown.menu [] [
                        Dropdown.content [] [
                            let msg = (UpdateXLSXParsingExportType >> JsonExporterMsg >> dispatch)
                            dropdownItem JsonExportType.Assay model msg (model.JsonExporterModel.XLSXParsingExportType = JsonExportType.Assay)
                            dropdownItem JsonExportType.ProcessSeq model msg (model.JsonExporterModel.XLSXParsingExportType = JsonExportType.ProcessSeq)
                            dropdownItem JsonExportType.ProtocolTemplate model msg (model.JsonExporterModel.XLSXParsingExportType = JsonExportType.ProtocolTemplate)
                        ]
                    ]
                ]
            ]
            Control.div [Control.IsExpanded] [
                Button.a [
                    let hasContent = model.JsonExporterModel.XLSXByteArray <> Array.empty
                    Button.Color IsInfo
                    if hasContent then
                        Button.IsActive true
                    else
                        Button.Color Color.IsDanger
                        Button.Props [Disabled true]
                    Button.IsFullWidth
                    Button.OnClick(fun _ ->
                        if hasContent then
                            ParseXLSXToJsonRequest model.JsonExporterModel.XLSXByteArray |> JsonExporterMsg |> dispatch
                    )
                ] [
                    str "Download as isa json"
                ]
            ]
        ]
    ]


let jsonExporterMainElement (model:Messages.Model) (dispatch: Messages.Msg -> unit) =
    
    Content.content [ Content.Props [
        OnSubmit    (fun e -> e.preventDefault())
        OnKeyDown   (fun k -> if (int k.which) = 13 then k.preventDefault())
        OnClick     (fun e -> CloseAllDropdowns |> JsonExporterMsg |> dispatch)
        Style [MinHeight "100vh"]
    ]] [

        Label.label [Label.Size Size.IsLarge; Label.Props [Style [Color model.SiteStyleState.ColorMode.Accent]]] [ str "Json Exporter"]

        Help.help [] [
            str "Export swate annotation tables to "
            a [Href @"https://en.wikipedia.org/wiki/JSON"] [str "JSON"]
            str " format. Official ISA-JSON types can be found "
            a [Href @"https://isa-specs.readthedocs.io/en/latest/isajson.html#"] [str "here"]
            str "."
        ]

        Label.label [Label.Props [Style [Color model.SiteStyleState.ColorMode.Accent]]] [str "Export active table"]

        parseTableToISAJsonEle model dispatch

        Label.label [Label.Props [Style [Color model.SiteStyleState.ColorMode.Accent]]] [str "Export workbook"]
        
        parseTablesToISAJsonEle model dispatch

        Label.label [Label.Props [Style [Color model.SiteStyleState.ColorMode.Accent]]] [str "Export Swate conform xlsx file."]

        xlsxUploadAndParsingMainElement model dispatch
    ]