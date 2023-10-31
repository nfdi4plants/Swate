module Modals.Loading

open Fable.React
open Fable.React.Props
open Feliz
open Feliz.Bulma

let loadingComponent =
    Html.i [
        prop.classes ["fa-solid"; "fa-spinner"; "fa-spin-pulse"; "fa-xl"]
    ]

let loadingModal =
    Bulma.modal [
        Bulma.modal.isActive
        prop.children [
            Bulma.modalBackground []
            Bulma.modalContent [
                prop.style [style.custom("width","auto")]
                prop.children [
                    Bulma.box loadingComponent
                ]
            ]
        ]
    ]