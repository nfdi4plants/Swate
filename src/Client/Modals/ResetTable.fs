module Modals.ResetTable

open Feliz
open Feliz.Bulma
open ExcelColors
open Model
open Messages
open Shared
open TermTypes


let Main (dispatch) (rmv: _ -> unit) =
    let reset = fun e -> Spreadsheet.Reset |> SpreadsheetMsg |> dispatch; rmv e

    Bulma.modal [
        Bulma.modal.isActive
        prop.children [
            Bulma.modalBackground [ prop.onClick rmv ]
            Bulma.modalCard [
                Bulma.modalCardHead [
                    Bulma.modalCardTitle "Attention!"
                    Bulma.delete [ prop.onClick rmv ]
                ]
                Bulma.modalCardBody [
                    Bulma.field.div [prop.innerHtml "Careful, this will delete <b>all</b> tables and <b>all</b> table history!"]
                    Bulma.field.div [prop.innerHtml "There is no option to recover any information deleted in this way."]
                    Bulma.field.div [prop.innerHtml "If you only want to delete one sheet, right-click the sheet at the bottom and select `delete`"]
                ]
                Bulma.modalCardFoot [
                    Bulma.buttons [
                        prop.className "grow justify-between"
                        prop.children [
                            Bulma.button.a [
                                prop.onClick rmv
                                Bulma.color.isInfo
                                prop.text "Back"
                            ]
                            Bulma.button.a [
                                prop.onClick reset
                                Bulma.color.isDanger
                                prop.text "Delete"
                            ]
                        ]
                    ]
                ]
            ]
        ]
    ]