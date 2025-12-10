module Modals.Cytoscape

open Fable.React
open Fable.React.Props
open Fable.Core.JsInterop
open Cytoscape
open Feliz
open Elmish
open Messages

// let view (rmv: _ -> unit) =
//     let closeMsg = rmv
//     Daisy.modal.div [
//         modal.active
//         prop.children [
//             Daisy.modalBackdrop [prop.onClick closeMsg]
//             Bulma.modalContent [
//                 prop.style [style.maxWidth (length.perc 80); style.maxHeight (length.perc 80); style.overflowX.auto]
//                 prop.children [
//                     Bulma.box [
//                         Html.div [prop.id "cy"]
//                     ]
//                 ]
//             ]
//         ]
//     ]
let placeholder = "placeholder"