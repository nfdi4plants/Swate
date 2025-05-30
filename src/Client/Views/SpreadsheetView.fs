module SpreadsheetView

open Feliz
open Feliz.DaisyUI
open Messages
open Swate.Components.Shared
open MainComponents
open Swate.Components.Shared
open Fable.Core.JsInterop
open Model
open ARCtrl

let private WidgetOrderContainer bringWidgetToFront (widget) =
    Html.div [ prop.onClick bringWidgetToFront; prop.children [ widget ] ]

let private ModalDisplay (widgets: Widget list, displayWidget: Widget -> ReactElement) =

    match widgets.Length with
    | 0 -> Html.none
    | _ ->
        Html.div [
            for widget in widgets do
                displayWidget widget
        ]

open Swate.Components.Shared
open FileImport

[<ReactComponent>]
let Main (model: Model, dispatch) =
    let widgets, setWidgets = React.useState ([])

    let rmvWidget (widget: Widget) =
        widgets |> List.except [ widget ] |> setWidgets

    let bringWidgetToFront (widget: Widget) =
        let newList =
            widgets |> List.except [ widget ] |> (fun x -> widget :: x |> List.rev)

        setWidgets newList

    let displayWidget (widget: Widget) =
        let rmv (widget: Widget) = fun _ -> rmvWidget widget
        let bringWidgetToFront = fun _ -> bringWidgetToFront widget

        match widget with
        | Widget._BuildingBlock -> Widget.BuildingBlock(model, dispatch, rmv widget)
        | Widget._Template -> Widget.Templates(model, dispatch, rmv widget)
        | Widget._FilePicker -> Widget.FilePicker(model, dispatch, rmv widget)
        | Widget._DataAnnotator -> Widget.DataAnnotator(model, dispatch, rmv widget)
        |> WidgetOrderContainer bringWidgetToFront

    let addWidget (widget: Widget) =
        widget :: widgets |> List.rev |> setWidgets

    let state = model.SpreadsheetModel

    Html.div [
        prop.id "MainWindow"
        prop.className "swt:@container/main swt:min-w-[400px] swt:flex swt:flex-col swt:h-screen"
        prop.children [
            MainComponents.Navbar.Main(model, dispatch, widgets, setWidgets)
            ModalDisplay(widgets, displayWidget)
            Html.div [
                prop.id "TableContainer"
                prop.className "swt:flex swt:grow swt:flex-col swt:h-full swt:overflow-y-hidden"
                prop.children [
                    //
                    match state.ArcFile with
                    | None -> MainComponents.NoFileElement.Main {| dispatch = dispatch |}
                    | Some(ArcFiles.Assay _)
                    | Some(ArcFiles.Study _)
                    | Some(ArcFiles.Investigation _)
                    | Some(ArcFiles.Template _) ->
                        match model.SpreadsheetModel.ActiveView with
                        | Spreadsheet.ActiveView.Table _ ->
                            match model.SpreadsheetModel.ActiveTable.ColumnCount with
                            | 0 ->
                                let openBuildingBlockWidget = fun () -> addWidget Widget._BuildingBlock
                                let openTemplateWidget = fun () -> addWidget Widget._Template
                                MainComponents.EmptyTableElement.Main(openBuildingBlockWidget, openTemplateWidget)
                            | _ ->
                                MainComponents.SpreadsheetView.ArcTable.Main(model, dispatch)
                                MainComponents.TableFooter.Main dispatch
                        | Spreadsheet.ActiveView.Metadata ->
                            Html.section [
                                prop.className "swt:overflow-y-auto swt:h-full"
                                prop.children [
                                    Html.div [
                                        prop.children [
                                            match model.SpreadsheetModel.ArcFile with
                                            | Some(ArcFiles.Assay assay) ->
                                                let setAssay assay =
                                                    assay
                                                    |> Assay
                                                    |> Spreadsheet.UpdateArcFile
                                                    |> SpreadsheetMsg
                                                    |> dispatch

                                                let setAssayDataMap assay dataMap =
                                                    dataMap
                                                    |> SpreadsheetInterface.UpdateDatamap
                                                    |> InterfaceMsg
                                                    |> dispatch

                                                Components.Metadata.Assay.Main(assay, setAssay, setAssayDataMap, model)
                                            | Some(ArcFiles.Study(study, assays)) ->
                                                let setStudy (study, assays) =
                                                    (study, assays)
                                                    |> Study
                                                    |> Spreadsheet.UpdateArcFile
                                                    |> SpreadsheetMsg
                                                    |> dispatch

                                                let setStudyDataMap study dataMap =
                                                    dataMap
                                                    |> SpreadsheetInterface.UpdateDatamap
                                                    |> InterfaceMsg
                                                    |> dispatch

                                                Components.Metadata.Study.Main(
                                                    study,
                                                    assays,
                                                    setStudy,
                                                    setStudyDataMap,
                                                    model
                                                )
                                            | Some(ArcFiles.Investigation investigation) ->
                                                let setInvesigation investigation =
                                                    investigation
                                                    |> Investigation
                                                    |> Spreadsheet.UpdateArcFile
                                                    |> SpreadsheetMsg
                                                    |> dispatch

                                                Components.Metadata.Investigation.Main(
                                                    investigation,
                                                    setInvesigation,
                                                    model
                                                )
                                            | Some(ArcFiles.Template template) ->
                                                let setTemplate template =
                                                    template
                                                    |> ArcFiles.Template
                                                    |> Spreadsheet.UpdateArcFile
                                                    |> SpreadsheetMsg
                                                    |> dispatch

                                                Components.Metadata.Template.Main(template, setTemplate)
                                            | None -> Html.none
                                        ]
                                    ]
                                ]
                            ]
                        | Spreadsheet.ActiveView.DataMap ->
                            MainComponents.SpreadsheetView.DataMap.Main(model, dispatch)
                            MainComponents.TableFooter.Main dispatch
                ]
            ]
            if state.ArcFile.IsSome then
                MainComponents.FooterTabs.SpreadsheetSelectionFooter model dispatch
        ]
    ]