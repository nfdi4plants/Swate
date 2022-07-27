module Cytoscape.View

open Fable.React
open Fable.React.Props
open Fulma
open Fable.FontAwesome
open Fable.Core.JsInterop
open Cytoscape

open Elmish
open Messages

let view (model:Model) (dispatch:Messages.Msg -> unit) =
    let closeMsg = (fun e -> UpdateShowModal false |> CytoscapeMsg |> dispatch)
    Modal.modal [ Modal.IsActive true ] [
        Modal.background [Props [ OnClick closeMsg ]] [ ]
        Modal.content [Props [Style [MaxWidth "80%"; MaxHeight "80%"; OverflowX OverflowOptions.Auto]]] [
            Box.box' [] [
                div [Id "cy"] []
            ]
            Box.box' [] [
                Button.a [
                    Button.OnClick (fun e ->
                        ()
                    )
                ] [
                    str "Add Cy"
                ]
                Button.a [
                    Button.OnClick (fun e ->
                        ()
                    )
                ] [
                    str "Update layout"
                ]
            ]
        ]
    ]