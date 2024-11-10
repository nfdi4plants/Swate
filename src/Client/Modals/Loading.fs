module Modals.Loading

open Fable.React
open Fable.React.Props
open Feliz
open Feliz.DaisyUI

let loadingComponent =
    Html.i [
        prop.classes ["fa-solid"; "fa-spinner"; "fa-spin-pulse"; "fa-xl"]
    ]

let loadingModal =
    Daisy.modal.div [
        prop.className "modal-open"
        prop.children [
            Daisy.modalBackdrop []
            Daisy.modalBox.div [
                prop.style [style.custom("width","auto")]
                prop.children [
                    loadingComponent
                ]
            ]
        ]
    ]