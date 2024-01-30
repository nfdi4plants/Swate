module MainComponents.AddRows

open Feliz
open Feliz.Bulma
open Fable.Core.JsInterop

open Messages

[<ReactComponent>]
let Main (dispatch: Messages.Msg -> unit) =
    let init_RowsToAdd = 1
    let state_rows, setState_rows = React.useState(init_RowsToAdd)
    Html.div [
        prop.id "ExpandTable"
        prop.style [
            style.flexGrow 1; style.justifyContent.center; style.display.inheritFromParent; style.padding(length.rem 1)
            style.position.sticky; style.left 0
        ]
        prop.children [
            Html.div [
                prop.style [style.height.maxContent; style.display.flex;]
                prop.children [
                    Bulma.input.number [
                        prop.id "n_row_input"
                        prop.min init_RowsToAdd
                        prop.onChange(fun e -> setState_rows e)
                        prop.defaultValue init_RowsToAdd
                        prop.style [style.width(50)]
                    ]
                    Bulma.button.a [
                        Bulma.button.isRounded
                        prop.title "Add rows"
                        prop.onClick(fun _ ->
                            let inp = Browser.Dom.document.getElementById "n_row_input"
                            inp?Value <- init_RowsToAdd
                            setState_rows init_RowsToAdd
                            Spreadsheet.AddRows state_rows |> SpreadsheetMsg |> dispatch
                        )
                        prop.children [
                            Bulma.icon [Html.i [prop.className "fa-solid fa-plus"]]
                        ]
                    ]
                ]
            ]
        ]
    ]



