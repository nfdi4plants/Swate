namespace Modals

open Fable.React
open Fable.React.Props
open Model
open Messages
open Feliz
open Feliz.DaisyUI
open Swate.Components

type Error =

    ///<summary>This modal is used to display errors from for example api communication</summary>
    static member Main(error: exn, dispatch) =
        let closeMsg = Util.RMV_MODAL dispatch

        //Daisy.modal.div [
        Html.div [
            prop.className "swt:modal swt:modal-open"
            prop.children [
                //Daisy.modalBackdrop [ prop.onClick closeMsg ]
                Html.div [
                    prop.className "swt:modal-backdrop"
                    prop.onClick closeMsg
                ]
                //Daisy.modalBox.div [
                Html.div [
                    prop.className "swt:modal-box swt:!p-0"
                    prop.style [
                        style.width (length.percent 90)
                        style.maxHeight (length.percent 80)
                        style.overflow.auto
                    ]
                    prop.children [
                        //Daisy.alert [
                        Html.div [
                            prop.className "swt:alert swt:alert-error swt:size-full"
                            prop.children [
                                Components.DeleteButton(props = [ prop.onClick closeMsg ])
                                Html.span (error.GetPropagatedError())
                            ]
                        ]
                    ]
                ]
            ]
        ]