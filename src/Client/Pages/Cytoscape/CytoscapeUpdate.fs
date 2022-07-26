namespace Cytoscape

open Elmish

[<AutoOpenAttribute>]
module Update =

    let update (msg:Cytoscape.Msg) (currentState: Cytoscape.Model) (currentModel:Messages.Model) : Cytoscape.Model * Messages.Model  * Cmd<Messages.Msg> =
        match msg with
        | Cytoscape.Msg.UpdateCyObject newCyObject ->
            let nextState = {
                currentState with
                    CyObject = newCyObject
            }
            nextState, currentModel, Cmd.none
        | UpdateShowModal toggle ->
            let nextState = {
                currentState with
                    ShowModal = toggle
            }
            nextState, currentModel, Cmd.none