module Modals.WarningModal

open Fable.React
open Fable.React.Props
open ExcelColors
open Model
open Messages
open Shared
open Feliz
open Feliz.DaisyUI
open Components


let warningModal (warning:{|NextMsg:Msg; ModalMessage: string|}, model, dispatch) (rmv: _ -> unit) =
    let msg = fun _ -> warning.NextMsg |> dispatch
    let closeMsg = rmv
    let message = warning.ModalMessage
    Daisy.modal.div [
        modal.active
        prop.children [
            Daisy.modalBackdrop [prop.onClick closeMsg]
            Daisy.modalBox.div [
                Daisy.alert [
                    alert.warning
                    prop.children [
                        Components.DeleteButton(props = [prop.onClick closeMsg])
                        Html.span message
                        Daisy.button.a [
                            button.warning
                            prop.onClick (fun e ->
                                closeMsg e
                                msg e
                            )
                            prop.text "Continue"
                        ]
                    ]
                ]
            ]
        ]
    ]

open Feliz
open Feliz.DaisyUI
open ExcelColors

let warningModalSimple (warning: string) (rmv: _ -> unit) =
    let closeMsg = rmv
    Daisy.modal.div [
        modal.active
        prop.children [
            Daisy.modalBackdrop [prop.onClick closeMsg]
            Daisy.modalBox.div [
                Daisy.alert [
                    alert.warning
                    prop.children [
                        Components.DeleteButton(props = [prop.onClick closeMsg])
                        Html.span warning
                        Daisy.button.a [
                            button.warning
                            prop.onClick (fun e ->
                                closeMsg e
                            )
                            prop.text "Continue"
                        ]
                    ]
                ]
            ]
        ]
    ]