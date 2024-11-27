namespace Modals

open Fable.React
open Fable.React.Props
open Model
open Messages
open Shared
open Feliz
open Feliz.DaisyUI
open Components

open Feliz
open Feliz.DaisyUI


type Warning =
    static member Main (warning: string, dispatch) =
        let closeMsg = Util.RMV_MODAL dispatch
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