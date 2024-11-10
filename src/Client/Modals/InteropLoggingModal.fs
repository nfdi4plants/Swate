module Modals.InteropLoggingModal

open Fable.React
open Fable.React.Props
open ExcelColors
open Model
open Messages
open Shared
open Feliz
open Feliz.DaisyUI
open Components

let interopLoggingModal(model:DevState, dispatch) (rmv: _ -> unit) =
    let closeMsg =
        fun e ->
            rmv e
            UpdateDisplayLogList [] |> DevMsg |> dispatch
    let logs = model.DisplayLogList
    Daisy.modal.div [
        modal.active
        prop.children [
            Daisy.modalBackdrop [ prop.onClick closeMsg ]
            Daisy.alert [
                prop.style [style.width(length.percent 80); style.maxHeight (length.percent 80)]
                prop.children [
                    Components.DeleteButton(props = [prop.onClick closeMsg])
                    Daisy.table [
                        Html.tbody (logs |> List.map LogItem.toTableRow)
                    ]
                    Daisy.button.a [
                        button.warning
                        prop.className "justify-end"
                        prop.onClick closeMsg
                        prop.text "Continue"
                    ]
                ]
            ]
        ]
    ]