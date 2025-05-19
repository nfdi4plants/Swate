namespace Modals

open Fable.React
open Fable.React.Props
open Feliz
open Feliz.DaisyUI

type Loading =
    static member Component = Daisy.loading [ loading.ring; loading.lg ]

    static member Modal(rmv: _ -> unit) =
        Daisy.modal.div [
            modal.open'
            prop.children [
                Daisy.modalBackdrop [ prop.onClick rmv ]
                Daisy.modalBox.div [ prop.className "size-auto flex min-w-0"; prop.children [ Loading.Component ] ]
            ]
        ]

    static member Modal(dispatch: Messages.Msg -> unit) =
        let rmv = Util.RMV_MODAL dispatch
        Loading.Modal(rmv = rmv)