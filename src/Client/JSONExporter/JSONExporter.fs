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
    | UpdateShowExportTypeDropdown nextVal ->
        let nextModel = {
            currentModel.JSONExporterModel with
                ShowExportTypeDropdown = nextVal
        }
        currentModel.updateByJSONExporterModel nextModel, Cmd.none
    | UpdateJSONExportType nextType ->
        let nextModel = {
            currentModel.JSONExporterModel with
                JSONExportType          = nextType
                ShowExportTypeDropdown  = false
        }
        currentModel.updateByJSONExporterModel nextModel, Cmd.none
    | UpdateJSONExportMode nextMode ->
        let nextModel = {
            currentModel.JSONExporterModel with
                JSONExportMode = nextMode
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
                ActiveWorksheetName = worksheetName
        }
        let cmd =
            Cmd.OfAsync.either
                Api.expertAPIv1.parseAnnotationTableToISAJson
                (currentModel.JSONExporterModel.JSONExportType, worksheetName, buildingBlocks)
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
        let cmd =
            Cmd.OfAsync.either
                Api.expertAPIv1.parseAnnotationTablesToISAJson
                (currentModel.JSONExporterModel.JSONExportType, worksheetBuildingBlocksTuple)
                (ParseTableServerResponse >> JSONExporterMsg)
                (curry GenericError (UpdateLoading false |> JSONExporterMsg |> Cmd.ofMsg) >> Dev)

        currentModel, cmd
    //
    | ParseTableServerResponse parsedJson ->
        let n = System.DateTime.Now.ToUniversalTime().ToString("yyyymmdd_hhMMss")
        let _ = download ($"{n}_isa.json",parsedJson)
        let nextModel = {
            currentModel.JSONExporterModel with
                ExportJsonString = parsedJson
                Loading = false
        }
        currentModel.updateByJSONExporterModel nextModel, Cmd.none

open Messages

let dropdownItem (exportType:JSONExportType) (model:Model) dispatch =
    Dropdown.Item.a [
        Dropdown.Item.Props [
            TabIndex 0
            OnClick (fun e ->
                e.stopPropagation()
                UpdateJSONExportType exportType  |> JSONExporterMsg |> dispatch
            )
            OnKeyDown (fun k -> if (int k.which) = 13 then UpdateJSONExportType exportType  |> JSONExporterMsg |> dispatch)
            colorControl model.SiteStyleState.ColorMode
        ]

    ][
        Text.span [
            CustomClass (Tooltip.ClassName + " " + Tooltip.IsTooltipRight + " " + Tooltip.IsMultiline)
            Props [
                Tooltip.dataTooltip ("Lorem ipsum dolor sit amet, consetetur sadipscing elitr, sed diam nonumy eirmod tempor invidunt ut labore et dolore magna aliquyam erat, sed diam voluptua. At vero eos et accusam et justo duo dolores et ea rebum. Stet clita kasd gubergren, no sea takimata sanctus est Lorem ipsum dolor sit amet. Lorem ipsum dolor sit amet, consetetur sadipscing elitr, sed diam nonumy eirmod tempor invidunt ut labore et dolore magna aliquyam erat, sed diam voluptua. At vero eos et accusam et justo duo dolores et ea rebum. Stet clita kasd gubergren, no sea takimata sanctus est Lorem ipsum dolor sit amet.")
                Style [PaddingRight "10px"]
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
                    Dropdown.IsActive model.JSONExporterModel.ShowExportTypeDropdown
                ][
                    Dropdown.trigger [][
                        Button.a [
                            Button.OnClick (fun e -> e.stopPropagation(); UpdateShowExportTypeDropdown (not model.JSONExporterModel.ShowExportTypeDropdown) |> JSONExporterMsg |> dispatch )
                        ][
                            span [Style [MarginRight "5px"]] [str (model.JSONExporterModel.JSONExportType.ToString())]
                            Fa.i [Fa.Solid.AngleDown] []
                        ]
                    ]
                    Dropdown.menu [][
                        Dropdown.content [][
                            dropdownItem JSONExportType.Assay model dispatch
                            dropdownItem JSONExportType.RowMajor model dispatch
                            dropdownItem JSONExportType.ProcessSeq model dispatch
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
            //Control.div [][
            //    Dropdown.dropdown [
            //        Dropdown.IsActive model.JSONExporterModel.ShowExportTypeDropdown
            //    ][
            //        Dropdown.trigger [][
            //            Button.a [
            //                Button.OnClick (fun e -> e.stopPropagation(); UpdateShowExportTypeDropdown (not model.JSONExporterModel.ShowExportTypeDropdown) |> JSONExporterMsg |> dispatch )
            //            ][
            //                span [Style [MarginRight "5px"]] [str (model.JSONExporterModel.JSONExportType.ToString())]
            //                Fa.i [Fa.Solid.AngleDown] []
            //            ]
            //        ]
            //        Dropdown.menu [][
            //            Dropdown.content [][
            //                dropdownItem JSONExportType.Assay model dispatch
            //                dropdownItem JSONExportType.RowMajor model dispatch
            //                dropdownItem JSONExportType.ProcessSeq model dispatch
            //            ]
            //        ]
            //    ]
            //]
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
        OnClick     (fun e -> UpdateShowExportTypeDropdown false |> JSONExporterMsg |> dispatch)
        Style [Height "100vh"]
    ] [

        Label.label [Label.Size Size.IsLarge; Label.Props [Style [Color model.SiteStyleState.ColorMode.Accent]]][ str "JSON Exporter"]

        Label.label [Label.Props [Style [Color model.SiteStyleState.ColorMode.Accent]]] [str "Export active table"]

        parseTableToISAJsonEle model dispatch

        Label.label [Label.Props [Style [Color model.SiteStyleState.ColorMode.Accent]]] [str "Export workbook"]
        
        parseTablesToISAJsonEle model dispatch

    ]