module Modals.ErrorModal

open Fable.React
open Fable.React.Props
open ExcelColors
open Model
open Messages
open Feliz
open Feliz.Bulma

///<summary>This modal is used to display errors from for example api communication</summary>
let errorModal(error: exn) (rmv: _ -> unit) =
    let closeMsg = rmv
    Bulma.modal [
        Bulma.modal.isActive
        prop.children [
            Bulma.modalBackground [ prop.onClick closeMsg ]
            Bulma.notification [
                Bulma.color.isDanger
                prop.style [style.width(length.percent 90); style.maxHeight (length.percent 80)]
                prop.children [
                    Bulma.delete [prop.onClick closeMsg]
                    Html.span (error.GetPropagatedError())
                ]
            ]
        ]
    ]
