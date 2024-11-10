module Modals.ErrorModal

open Fable.React
open Fable.React.Props
open ExcelColors
open Model
open Messages
open Feliz
open Feliz.DaisyUI
open Components

///<summary>This modal is used to display errors from for example api communication</summary>
let errorModal(error: exn) (rmv: _ -> unit) =
    let closeMsg = rmv
    Daisy.modal.div [
        modal.active
        prop.children [
            Daisy.modalBackdrop [ prop.onClick closeMsg ]
            Daisy.alert [
                alert.error
                prop.style [style.width(length.percent 90); style.maxHeight (length.percent 80); style.overflow.auto]
                prop.children [
                    Components.DeleteButton(props = [prop.onClick closeMsg])
                    Html.span (error.GetPropagatedError())
                ]
            ]
        ]
    ]
