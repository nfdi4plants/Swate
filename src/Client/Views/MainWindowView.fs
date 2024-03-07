module MainWindowView

open Feliz
open Feliz.Bulma
open Messages
open Shared
open MainComponents
open Shared
open Fable.Core.JsInterop

let private WidgetOrderContainer bringWidgetToFront (widget) =
    Html.div [
        prop.onClick bringWidgetToFront
        prop.children [
            widget
        ]
    ]

let private ModalDisplay (widgets: Widget list, rmvWidget: Widget -> unit, bringWidgetToFront: Widget -> unit, model, dispatch) = 
    let rmv (widget: Widget) = fun _ -> rmvWidget widget
    let displayWidget (widget: Widget) =
        let bringWidgetToFront = fun _ -> bringWidgetToFront widget
        match widget with
        | Widget._BuildingBlock -> Widget.BuildingBlock (model, dispatch, rmv widget) 
        | Widget._Template -> Widget.Templates (model, dispatch, rmv widget)
        | Widget._FilePicker -> Widget.FilePicker (model, dispatch, rmv widget)
        |> WidgetOrderContainer bringWidgetToFront
    match widgets.Length with
    | 0 -> 
        Html.none
    | _ ->
        Html.div [
            for widget in widgets do displayWidget widget
        ]

let private SpreadsheetSelectionFooter (model: Messages.Model) dispatch =
    Html.div [
        prop.style [
            style.position.sticky;
            style.bottom 0
        ]
        prop.children [
            Html.div [
                prop.children [
                    Bulma.tabs [
                        Bulma.tabs.isBoxed
                        prop.children [
                            Html.ul [
                                Bulma.tab  [
                                    prop.style [style.width (length.px 20)]
                                ]
                                
                                MainComponents.FooterTabs.MainMetadata (model, dispatch)
                                for index in 0 .. (model.SpreadsheetModel.Tables.TableCount-1) do
                                    MainComponents.FooterTabs.Main (index, model.SpreadsheetModel.Tables, model, dispatch)
                                if model.SpreadsheetModel.CanHaveTables() then 
                                    MainComponents.FooterTabs.MainPlus (model, dispatch)
                                if model.SpreadsheetModel.TableViewIsActive() then
                                    MainComponents.FooterTabs.ToggleSidebar(model, dispatch)
                            ]
                        ]
                    ]
                ]
            ]
        ]
    ]

open Shared

[<ReactComponent>]
let Main (model: Messages.Model, dispatch) =
    let widgets, setWidgets = React.useState([])
    let rmvWidget (widget: Widget) = widgets |> List.except [widget] |> setWidgets
    let bringWidgetToFront (widget: Widget) = 
        let newList = widgets |> List.except [widget] |> fun x -> widget::x |> List.rev
        setWidgets newList
    let state = model.SpreadsheetModel
    Html.div [
        prop.id "MainWindow"
        prop.style [
            style.display.flex
            style.flexDirection.column
            style.width (length.percent 100)
            style.height (length.percent 100)
        ]
        prop.children [
            MainComponents.Navbar.Main (model, dispatch, widgets, setWidgets)
            ModalDisplay (widgets, rmvWidget, bringWidgetToFront, model, dispatch)
            Html.div [
                prop.id "TableContainer"
                prop.style [
                    style.width.inheritFromParent
                    style.height.inheritFromParent
                    style.overflowX.auto
                    style.display.flex
                    style.flexDirection.column
                ]
                prop.children [
                    //
                    match state.ArcFile with
                    | None ->
                        MainComponents.NoTablesElement.Main {|dispatch = dispatch|}
                    | Some (ArcFiles.Assay _) 
                    | Some (ArcFiles.Study _)
                    | Some (ArcFiles.Investigation _) 
                    | Some (ArcFiles.Template _) ->
                        XlsxFileView.Main (model , dispatch)
                    if state.Tables.TableCount > 0 && state.ActiveTable.ColumnCount > 0 && state.ActiveView <> Spreadsheet.ActiveView.Metadata then
                        MainComponents.AddRows.Main dispatch
                ]
            ]
            if state.ArcFile.IsSome then 
                SpreadsheetSelectionFooter model dispatch
        ]
    ]