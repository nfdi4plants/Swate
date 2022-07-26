module Cytoscape.View

open Fable.React
open Fable.React.Props
open Fulma
open Fable.FontAwesome
open Fable.Core.JsInterop
open Cytoscape

open Elmish
open Messages

let mutable cy : JS.Types.ICytoscape option = None

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
                        let element = Browser.Dom.document.getElementById "cy"
                        let cy_ele = Cytoscape.JS.cy({|
                            container = element 
                            elements = seq [
                                JS.createNode "a"
                                JS.createNode "b"
                                JS.createNode "c"
                                JS.createEdge "ab" "a" "b"
                                JS.createEdge "ac" "a" "c"
                            ];
                            style = seq [
                                {|
                                    selector = "node"
                                    style = {|
                                        label = "data(id)"
                                    |} |> box
                                |};
                                {|
                                    selector = "edge"
                                    style = createObj [
                                        "width" ==> 3
                                        "line-color" ==> "blue"
                                    ]
                                |}
                            ]
                        |})
                        cy_ele.center()
                        cy_ele.bind "click" "node" (fun e -> Browser.Dom.console.log(e.target?id()))
                        UpdateCyObject (Some cy_ele) |> CytoscapeMsg |> dispatch
                        cy <- Some cy_ele
                    )
                ] [
                    str "Add Cy"
                ]
                Button.a [
                    Button.OnClick (fun e ->
                        if model.CytoscapeModel.CyObject.IsSome then
                            let nextCy = model.CytoscapeModel.CyObject.Value
                            nextCy.add <| JS.createNode "d"
                            nextCy.add <| JS.createEdge "da" "d" "a"
                            nextCy.add <| JS.createEdge "db" "d" "b"
                            nextCy.add <| JS.createEdge "dc" "d" "c"
                            let layout = nextCy.layout({|name = "breadthfirst"|})
                            layout.run()
                    )
                ] [
                    str "Add More info to cy"
                ]
            ]
        ]
    ]