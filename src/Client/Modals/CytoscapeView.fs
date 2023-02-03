module Modals.Cytoscape

open Fable.React
open Fable.React.Props
open Fulma
open Fable.FontAwesome
open Fable.Core.JsInterop
open Cytoscape

open Elmish
open Messages

let view (rmv: _ -> unit) =
    let closeMsg = rmv
    Modal.modal [ Modal.IsActive true ] [
        Modal.background [Props [ OnClick closeMsg ]] [ ]
        Modal.content [Props [Style [MaxWidth "80%"; MaxHeight "80%"; OverflowX OverflowOptions.Auto]]] [
            Box.box' [] [
                div [Id "cy"] []
            ]
        ]
    ]