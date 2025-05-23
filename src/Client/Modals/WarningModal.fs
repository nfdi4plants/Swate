namespace Modals

open Fable.React
open Fable.React.Props
open Model
open Messages
open Swate.Components.Shared
open Feliz
open Feliz.DaisyUI
open Swate.Components

open Feliz
open Feliz.DaisyUI


type Warning =
    static member Main(warning: string, dispatch) =
        let closeMsg = Util.RMV_MODAL dispatch

        //Daisy.modal.div [
        Html.div [
            prop.className "swt:modal swt:modal-open"
            prop.children [
                //Daisy.modalBackdrop [ prop.onClick closeMsg ]
                Html.div [
                    prop.className "swt:modal swt:modal-backdrop"
                    prop.onClick closeMsg
                ]
                //Daisy.modalBox.div [
                Html.div [
                    prop.className "swt:modal-bodx"
                    prop.children [
                        //Daisy.alert [
                        Html.div [
                            prop.className "swt:alert swt:alert-warning"
                            prop.children [
                                Components.DeleteButton(props = [ prop.onClick closeMsg ])
                                Html.span warning
                                //Daisy.button.a [
                                Html.button [
                                    prop.className "swt:btn swt:btn-warning"
                                    prop.text "Continue"
                                    prop.onClick (fun e -> closeMsg e)
                                ]
                            ]
                        ]
                    ]
                ]
            ]
        ]