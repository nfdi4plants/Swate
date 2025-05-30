module MainComponents.TableFooter

open Feliz
open Feliz.DaisyUI
open Fable.Core.JsInterop

open Messages

[<ReactComponent>]
let Main (dispatch: Messages.Msg -> unit) =
    let init_RowsToAdd = 1
    let state_rows, setState_rows = React.useState (init_RowsToAdd)
    let inputRef = React.useInputRef()

    Html.div [
        prop.id "ExpandTable"
        prop.className "swt:flex swt:flex-row swt:justify-center swt:grow-0 swt:p-2"
        prop.title "Add Rows"
        prop.children [
            //Daisy.join [
            Html.div [
                prop.className "swt:join"
                prop.children [
                    //Daisy.input [
                    Html.input [
                        prop.className "swt:input swt:join-item swt:border-current"
                        prop.type'.number
                        prop.ref inputRef
                        prop.min init_RowsToAdd
                        prop.onChange (fun e -> setState_rows e)
                        prop.onKeyDown (key.enter, fun _ -> Spreadsheet.AddRows state_rows |> SpreadsheetMsg |> dispatch)
                        prop.defaultValue init_RowsToAdd
                        prop.style [ style.width (100) ]
                    ]
                    //Daisy.button.a [
                    Html.button [
                        prop.className "swt:btn swt:btn-outline swt:join-item"
                        prop.onClick (fun _ ->
                            inputRef.current.Value.value <- unbox init_RowsToAdd
                            setState_rows init_RowsToAdd
                            Spreadsheet.AddRows state_rows |> SpreadsheetMsg |> dispatch)
                        prop.children [ Html.i [ prop.className "fa-solid fa-plus" ] ]
                    ]
                ]
            ]
        ]
    ]