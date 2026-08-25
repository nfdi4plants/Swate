module Swate.Components.Shared.Cwl.Features.RequirementsFeature

open System
open Swate.Components.Shared.Cwl.Documents.Common
open Swate.Components.Shared.Cwl.Documents.Mutations
open Swate.Components.Shared.Cwl.Documents.Types

type RequirementProps = {
    SelectedRequirementId: RequirementNodeId option
    RequirementItems: RequirementNode list
    HintItems: RequirementNode list
}

let emptyProps = {
    SelectedRequirementId = None
    RequirementItems = []
    HintItems = []
}

let private updateExpressionTool requirementUpdate hintUpdate document =
    match document with
    | ExpressionToolDoc model ->
        ExpressionToolDoc {
            model with
                Requirements = requirementUpdate model.Requirements
                Hints = hintUpdate model.Hints
        }
    | _ -> document

let private toggleByKey key enabled items =
    let withoutKey = items |> List.filter (fun node -> node.Key <> key)

    if enabled then
        if withoutKey |> List.exists (fun node -> node.Key = key) then
            withoutKey
        else
            withoutKey @ [ createRequirementNode key ]
    else
        withoutKey

let private updateFieldById requirementNodeId fieldKey value items =
    items
    |> updateRequirementNode
        requirementNodeId
        (fun node ->
            let nextFields =
                if String.IsNullOrWhiteSpace value then
                    node.Fields |> Map.remove fieldKey
                else
                    node.Fields |> Map.add fieldKey value

            { node with Fields = nextFields }
        )

let setRequirementEnabled key enabled document =
    updateExpressionTool (toggleByKey key enabled) id document

let setHintEnabled key enabled document =
    updateExpressionTool id (toggleByKey key enabled) document

let setRequirementField requirementNodeId fieldKey value document =
    updateExpressionTool (updateFieldById requirementNodeId fieldKey value) id document

let setHintField requirementNodeId fieldKey value document =
    updateExpressionTool id (updateFieldById requirementNodeId fieldKey value) document
