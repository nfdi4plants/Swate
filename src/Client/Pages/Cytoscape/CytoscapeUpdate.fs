namespace Cytoscape

open Elmish
open Messages

module Update =  

    let update (msg:Cytoscape.Msg) (currentState: Cytoscape.Model) (currentModel:Messages.Model) : Cytoscape.Model * Messages.Model  * Cmd<Messages.Msg> =
        match msg with
        //| Cytoscape.Msg.UpdateCyObject newCyObject ->
        //    let nextState = {
        //        currentState with
        //            CyObject = newCyObject
        //    }
        //    nextState, currentModel, Cmd.none
        | UpdateShowModal toggle ->
            let nextState = {
                currentState with
                    ShowModal = toggle
            }
            nextState, currentModel, Cmd.none
        | GetTermTree accession ->
            let cmd =
                Cmd.OfAsync.either
                    Api.api.getTreeByAccession
                    accession
                    (GetTermTreeResponse >> CytoscapeMsg)
                    (curry GenericError Cmd.none >> DevMsg)
            let nextState =
                { Cytoscape.Model.init(accession) with ShowModal = true }
            nextState, currentModel, cmd
        | GetTermTreeResponse tree ->
            let nextState =
                { currentState with
                    CyTermTree = Some tree
                }
            Graph.createCy nextState
            nextState, currentModel, Cmd.none
            