namespace Cytoscape

open Elmish
open Messages

module Update =  

    let update (msg:Cytoscape.Msg) (currentState: Cytoscape.Model) (currentModel:Messages.Model) : Cytoscape.Model * Messages.Model  * Cmd<Messages.Msg> =
        match msg with
        | GetTermTree accession ->
            let cmd =
                Cmd.OfAsync.either
                    Api.api.getTreeByAccession
                    accession
                    (GetTermTreeResponse >> CytoscapeMsg)
                    (curry GenericError Cmd.none >> DevMsg)
            let nextState = Cytoscape.Model.init(accession)
            let batch = Cmd.batch [
                Cmd.ofSub (fun _ -> Modals.Controller.renderModal("CytoscapeView", Modals.Cytoscape.view))
                cmd
            ]
            nextState, currentModel, batch
        | GetTermTreeResponse tree ->
            let nextState =
                { currentState with
                    CyTermTree = Some tree
                }
            Graph.createCy nextState
            nextState, currentModel, Cmd.none
            