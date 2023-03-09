module Modals.ErrorModal

open Fable.React
open Fable.React.Props
open Fulma
open Fulma.Extensions.Wikiki
open Fable.FontAwesome
open ExcelColors
open Model
open Messages

///<summary>This modal is used to display errors from for example api communication</summary>
let errorModal(error: exn) (rmv: _ -> unit) =
    let closeMsg = rmv
    Modal.modal [ Modal.IsActive true ] [
        Modal.background [
            Props [ OnClick closeMsg ]
        ] [ ]
        Notification.notification [
            Notification.Color IsDanger
            Notification.Props [Style [MaxWidth "80%"; MaxHeight "80%"; OverflowX OverflowOptions.Auto]]
        ] [
            Notification.delete [Props [OnClick closeMsg]] []
            str (error.GetPropagatedError())
        ]
    ]