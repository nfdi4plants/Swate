module Modals.Cytoscape

open Fable.React
open Fable.React.Props
open Fable.Core.JsInterop
open Cytoscape
open Feliz
open Feliz.Bulma
open Elmish
open Messages

let view (rmv: _ -> unit) =
    let closeMsg = rmv
    Bulma.modal [
        Bulma.modal.isActive
        prop.children [
            Bulma.modalBackground [prop.onClick closeMsg]
            Bulma.modalContent [
                prop.style [style.maxWidth (length.perc 80); style.maxHeight (length.perc 80); style.overflowX.auto]
                prop.children [
                    Bulma.box [
                        Html.div [prop.id "cy"]
                    ]
                ]
            ]
        ]
    ]