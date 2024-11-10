module Modals.ResetTable

open Feliz
open Feliz.DaisyUI
open ExcelColors
open Model
open Messages
open Shared
open Components


let Main (dispatch) (rmv: _ -> unit) =
    let reset = fun e -> Spreadsheet.Reset |> SpreadsheetMsg |> dispatch; rmv e

    Daisy.modal.div [
        modal.active
        prop.children [
            Daisy.modalBackdrop [ prop.onClick rmv ]
            Daisy.modalBox.div [
                Daisy.cardActions [
                    prop.className "justify-end"
                    prop.children [
                        Components.Components.DeleteButton(props=[prop.onClick rmv])
                    ]
                ]
                Daisy.cardTitle "Attention!"
                Html.div [
                    Html.p [prop.innerHtml "Careful, this will delete <b>all</b> tables and <b>all</b> table history!"]
                    Html.p [prop.innerHtml "There is no option to recover any information deleted in this way."]
                    Html.p [prop.innerHtml "If you only want to delete one sheet, right-click the sheet at the bottom and select `delete`"]
                ]
                Daisy.cardActions [
                    prop.className "justify-end"
                    prop.children [
                        Daisy.button.a [
                            prop.onClick rmv
                            button.info
                            prop.text "Back"
                        ]
                        Daisy.button.a [
                            prop.onClick reset
                            button.error
                            prop.text "Delete"
                        ]
                    ]
                ]
            ]
        ]
    ]