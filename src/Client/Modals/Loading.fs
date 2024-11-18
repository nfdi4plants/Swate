namespace Components.Modals

open Fable.React
open Fable.React.Props
open Feliz
open Feliz.DaisyUI

type Loading =
    static member Component =
        Daisy.loading [
            loading.ring
            loading.lg
        ]

    static member Modal(?rmv: Browser.Types.MouseEvent -> unit) =
        Daisy.modal.div [
            modal.open'
            prop.children [
                Daisy.modalBackdrop [if rmv.IsSome then prop.onClick rmv.Value]
                Daisy.modalBox.div [
                    prop.className "size-auto flex min-w-0"
                    prop.children [
                        Loading.Component
                    ]
                ]
            ]
        ]
