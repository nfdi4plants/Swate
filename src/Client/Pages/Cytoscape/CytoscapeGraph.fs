namespace Cytoscape

open Fable.Core.JsInterop

module Graph =
    let mutable cy : JS.Types.ICytoscape option = None

    let updateLayout() =
        let layout = cy.Value.layout({|
            directed = true
            name = "breadthfirst"
            padding = 20
        |})
        layout.run()

    let centerOn(accession:string) =
        let mainNode =
            let str = $"""[accession = "{accession}"]"""
            cy.Value?nodes(str)
        cy.Value.center(mainNode)
        

    let createClickEvent (ev) =
        cy.Value.bind "click" "node" ev

    let createCy (model: Cytoscape.Model)=
        let tree = model.CyTermTree.Value
        let element = Browser.Dom.document.getElementById "cy"
        let nodes =
            [|
                for node in tree.Nodes do
                    yield JS.Node.create(node.NodeId, ["accession", node.Term.Accession; "name", node.Term.Name; "ontology", node.Term.FK_Ontology])
            |]
        let rlts =
            [|
                for rlt in tree.Relationships do
                    yield JS.Edge.create(rlt.RelationshipId, rlt.StartNodeId, rlt.EndNodeId, ["type", rlt.Type])
            |]
        let cy_ele = Cytoscape.JS.cy({|
            container = element 
            elements = 
                [|
                    yield! nodes
                    yield! rlts
                |];
            style = seq [
                {|
                    selector = "node"
                    style = {|
                        label = "data(name)"
                    |} |> box
                |};
                {|
                    selector = $"""[accession = "{model.TargetAccession}"]"""
                    style = createObj [
                        "background-color" ==> "red"
                    ] |> box
                |};
                {|
                    selector = "edge"
                    style = createObj [
                        "width" ==> 3
                        "line-color" ==> "#ccc"
                        "target-arrow-color" ==> "black"
                        "target-arrow-shape" ==> "triangle"
                        "curve-style" ==> "bezier"
                        "label" ==> "data(type)"
                    ]
                |};
                {|
                    selector = $"""[type = "is_a"]"""
                    style = createObj [
                        "line-color" ==> "orange"
                    ] |> box
                |};
            ]
            wheelSensitivity = 0.5
        |})
        cy <- Some cy_ele
        centerOn(model.TargetAccession)
        createClickEvent(fun e -> Browser.Dom.console.log( e.target?position() ) )
        updateLayout()
        