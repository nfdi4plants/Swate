module MainWindowView

open Feliz
open Feliz.Bulma
open Messages

let private spreadsheetSelectionFooter (model: Messages.Model) dispatch =
    Html.div [
        prop.style [
            style.position.sticky;
            style.bottom 0
        ]
        prop.children [
            Html.div [
                prop.children [
                    Bulma.tabs [
                        prop.style [style.overflowY.visible]
                        Bulma.tabs.isBoxed
                        prop.children [
                            Html.ul [
                                yield Bulma.tab  [
                                    prop.style [style.width (length.px 20)]
                                ]
                                for KeyValue (index,table) in model.SpreadsheetModel.Tables do
                                    yield
                                        MainComponents.FooterTabs.Main {| i = index; table = table; model = model; dispatch = dispatch |}
                                yield
                                    MainComponents.FooterTabs.MainPlus {| dispatch = dispatch |}
                            ]
                        ]
                    ]
                ]
            ]
        ]
    ]

[<ReactComponent>]
let Main (model: Messages.Model) dispatch =
    let state = model.SpreadsheetModel
    let activeTableIsEmpty = Map.isEmpty state.ActiveTable
    let init_RowsToAdd = 1
    let state_rows, setState_rows = React.useState(init_RowsToAdd)
    Html.div [
        prop.id "MainWindow"
        prop.style [
            style.display.flex
            style.flexDirection.column
            style.width (length.percent 100)
            style.height (length.percent 100)
        ]
        prop.children [
            MainComponents.Navbar.Main model dispatch
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
                    match activeTableIsEmpty with
                    | true ->
                        MainComponents.NoTablesElement.Main dispatch
                    | false ->
                        MainComponents.SpreadsheetView.Main model dispatch
                        MainComponents.AddRows.Main init_RowsToAdd state_rows setState_rows dispatch
                ]
            ]
            match activeTableIsEmpty with | true -> Html.none | false -> spreadsheetSelectionFooter model dispatch
        ]
    ]