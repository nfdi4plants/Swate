module MainWindowView

open Feliz
open Feliz.Bulma
open Messages

open SpreadsheetView

let private initFunctionView dispatch =
    Html.div [
        prop.style [
            style.height.inheritFromParent
            style.width.inheritFromParent
            style.display.flex
            style.justifyContent.center
            style.alignItems.center
        ]
        prop.children [
            Html.div [
                prop.style [style.height.minContent; style.display.inheritFromParent; style.justifyContent.spaceBetween]
                prop.children [
                    let buttonStyle = prop.style [style.flexDirection.column; style.height.unset; style.width(length.px 140); style.margin(length.rem 1.5)]
                    Bulma.button.span [
                        Bulma.button.isLarge
                        buttonStyle
                        Bulma.color.isPrimary
                        prop.onClick(fun e -> SpreadsheetInterface.CreateAnnotationTable e.ctrlKey |> Messages.InterfaceMsg |> dispatch)
                        prop.children [
                            Html.div [
                                Html.i [prop.className "fas fa-plus"]
                                Html.i [prop.className "fas fa-table"]
                            ]
                            Html.div "New Table"
                        ]
                    ]
                    Bulma.button.span [
                        Bulma.button.isLarge
                        buttonStyle
                        Bulma.color.isInfo
                        prop.children [
                            Html.div [
                                Html.i [prop.className "fas fa-plus"]
                                Html.i [prop.className "fas fa-table"]
                            ]
                            Html.div "Import File"
                        ]
                    ]
                ]
            ]
        ]
    ]



let private spreadsheetSelectionFooter (model: Messages.Model) dispatch =
    Html.div [
        prop.style [
            style.position.sticky;
            style.bottom 0
            style.backgroundColor "whitesmoke"
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
    Html.div [
        prop.id "MainWindow"
        prop.style [
            style.display.flex
            style.flexDirection.column
            style.width (length.percent 100)
            style.height (length.percent 100)
        ]
        prop.children [
            let activeTableIsEmpty = Map.isEmpty state.ActiveTable
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
                        initFunctionView dispatch
                    | false ->
                        SpreadsheetView.Main model dispatch
                ]
            ]
            match activeTableIsEmpty with | true -> () | false -> spreadsheetSelectionFooter model dispatch
        ]
    ]