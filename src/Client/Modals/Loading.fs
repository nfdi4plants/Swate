namespace Modals

open Fable.React
open Fable.React.Props
open Feliz
open Feliz.DaisyUI

type Loading =
    static member Component = //Daisy.loading [ loading.ring; loading.lg ]
        Html.span [ prop.className "swt:loading swt:loading-ring swt:loading-lg" ]

    static member Modal(rmv: _ -> unit) =
        //Daisy.modal.div [
        Html.div [
            prop.className "swt:modal swt:modal-open"
            prop.children [
                //Daisy.modalBackdrop [ prop.onClick rmv ]
                Html.div [ prop.className "swt:modal-backdrop"; prop.onClick rmv ]
                //Daisy.modalBox.div [
                Html.div [
                    prop.className "swt:modal-box swt:size-auto swt:flex swt:min-w-0"
                    prop.children [ Loading.Component ]
                ]
            ]
        ]