module MainComponents.TableFooter

open Feliz
open Feliz.DaisyUI
open Fable.Core.JsInterop

open Messages

[<ReactComponent>]
let Main (dispatch: Messages.Msg -> unit) =
    let init_RowsToAdd = 1
    let state_rows, setState_rows = React.useState (init_RowsToAdd)

    Html.div [
        prop.id "ExpandTable"
        prop.className "flex flex-row justify-center grow-0 p-2"
        prop.title "Add Rows"
        prop.children [
            Daisy.join [
                Daisy.input [
                    prop.className "border-current"
                    join.item
                    input.bordered
                    prop.type'.number
                    prop.id "n_row_input"
                    prop.min init_RowsToAdd
                    prop.onChange (fun e -> setState_rows e)
                    prop.onKeyDown (key.enter, fun _ -> Spreadsheet.AddRows state_rows |> SpreadsheetMsg |> dispatch)
                    prop.defaultValue init_RowsToAdd
                    prop.style [ style.width (100) ]
                ]
                Daisy.button.a [
                    join.item
                    button.outline
                    prop.onClick (fun _ ->
                        let inp = Browser.Dom.document.getElementById "n_row_input"
                        inp?Value <- init_RowsToAdd
                        setState_rows init_RowsToAdd
                        Spreadsheet.AddRows state_rows |> SpreadsheetMsg |> dispatch)
                    prop.children [ Html.i [ prop.className "fa-solid fa-plus" ] ]
                ]
            ]
        ]
    ]