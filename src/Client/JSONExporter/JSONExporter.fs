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
open Fable.Core.JS
open Fable.Core.JsInterop


let download(filename, text) =
  let element = document.createElement("a");
  element.setAttribute("href", "data:text/plain;charset=utf-8," +  encodeURIComponent(text));
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
        }
        currentModel.updateByJSONExporterModel nextModel, Cmd.none
    | UpdateShowWorkbookExportTypeDropdown nextVal ->
        let nextModel = {
            currentModel.JSONExporterModel with
                ShowTableExportTypeDropdown = false
                ShowWorkbookExportTypeDropdown = nextVal
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
    //
    | ParseTableOfficeInteropRequest ->
        let cmd =
            Cmd.OfPromise.either
                OfficeInterop.getBuildingBlocksAndSheet
                ()
                (ParseTableServerRequest >> JSONExporterMsg)
                (curry GenericError (UpdateLoading false |> JSONExporterMsg |> Cmd.ofMsg) >> Dev)
        currentModel, cmd
    | ParseTableServerRequest (worksheetName, buildingBlocks) ->
        let nextModel = {
            currentModel.JSONExporterModel with
                CurrentExportType   = Some currentModel.JSONExporterModel.TableJSONExportType
        }
        let cmd =
            Cmd.OfAsync.either
                Api.expertAPIv1.parseAnnotationTableToISAJson
                (currentModel.JSONExporterModel.TableJSONExportType, worksheetName, buildingBlocks)
                (ParseTableServerResponse >> JSONExporterMsg)
                (curry GenericError (UpdateLoading false |> JSONExporterMsg |> Cmd.ofMsg) >> Dev)

        currentModel.updateByJSONExporterModel nextModel, cmd
    //
    | ParseTablesOfficeInteropRequest ->
        let cmd =
            Cmd.OfPromise.either
                OfficeInterop.getBuildingBlocksAndSheets
                ()
                (ParseTablesServerRequest >> JSONExporterMsg)
                (curry GenericError (UpdateLoading false |> JSONExporterMsg |> Cmd.ofMsg) >> Dev)
        currentModel, cmd
    | ParseTablesServerRequest (worksheetBuildingBlocksTuple) ->
        let nextModel = {
            currentModel.JSONExporterModel with
                CurrentExportType   = Some currentModel.JSONExporterModel.WorkbookJSONExportType
        }
        let cmd =
            Cmd.OfAsync.either
                Api.expertAPIv1.parseAnnotationTablesToISAJson
                (currentModel.JSONExporterModel.WorkbookJSONExportType, worksheetBuildingBlocksTuple)
                (ParseTableServerResponse >> JSONExporterMsg)
                (curry GenericError (UpdateLoading false |> JSONExporterMsg |> Cmd.ofMsg) >> Dev)

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
            //colorControl model.SiteStyleState.ColorMode
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

let jsonExporterMainElement (model:Messages.Model) (dispatch: Messages.Msg -> unit) =
    form [
        OnSubmit    (fun e -> e.preventDefault())
        OnKeyDown   (fun k -> if (int k.which) = 13 then k.preventDefault())
        OnClick     (fun e ->
            UpdateShowTableExportTypeDropdown false |> JSONExporterMsg |> dispatch
            UpdateShowWorkbookExportTypeDropdown false |> JSONExporterMsg |> dispatch
        )
        Style [Height "100vh"]
    ] [

        Label.label [Label.Size Size.IsLarge; Label.Props [Style [Color model.SiteStyleState.ColorMode.Accent]]][ str "JSON Exporter"]

        Label.label [Label.Props [Style [Color model.SiteStyleState.ColorMode.Accent]]] [str "Export active table"]

        parseTableToISAJsonEle model dispatch

        Label.label [Label.Props [Style [Color model.SiteStyleState.ColorMode.Accent]]] [str "Export workbook"]
        
        parseTablesToISAJsonEle model dispatch

    ]