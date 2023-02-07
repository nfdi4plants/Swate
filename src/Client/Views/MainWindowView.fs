module MainWindowView

open Feliz
open Feliz.Bulma

open SpreadsheetView

let private initFunctionView =
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

[<ReactComponent>]
let Main (model: Messages.Model) dispatch =
    let state = model.SpreadsheetModel
    Html.div [
        prop.style [
            style.width (length.percent 100)
            style.height (length.percent 100)
            style.overflowX.scroll
        ]
        prop.children [
            //
            match Map.isEmpty state.ActiveTable with
            | true ->
                initFunctionView
            | false ->
                SpreadsheetView.Main model dispatch
        ]
    ]