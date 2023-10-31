module Modals.InteropLoggingModal

open Fable.React
open Fable.React.Props
open ExcelColors
open Model
open Messages
open Shared
open Feliz
open Feliz.Bulma

let interopLoggingModal(model:DevState, dispatch) (rmv: _ -> unit) =
    let closeMsg =
        fun e ->
            rmv e
            UpdateDisplayLogList [] |> DevMsg |> dispatch
    let logs = model.DisplayLogList
    Bulma.modal [
        Bulma.modal.isActive
        prop.children [
            Bulma.modalBackground [ prop.onClick closeMsg ]
            Bulma.notification [
                prop.style [style.width(length.percent 80); style.maxHeight (length.percent 80)]
                prop.children [
                    Bulma.delete [ prop.onClick closeMsg ]
                    Bulma.field.div [
                        Bulma.table [
                            Bulma.table.isFullWidth
                            prop.children [
                                Html.tbody (logs |> List.map LogItem.toTableRow)
                            ]
                        ]
                    ]
                    Bulma.field.div [
                        Bulma.button.a [
                            Bulma.color.isWarning
                            prop.style [style.float'.right]
                            prop.onClick closeMsg
                            prop.text "Continue"
                        ]
                    ]
                ]
            ]
        ]
    ]