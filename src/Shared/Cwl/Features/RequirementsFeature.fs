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

let private updateDocumentRequirements requirementUpdate hintUpdate document =
    match document with
    | CommandLineToolDoc model ->
        CommandLineToolDoc {
            model with
                Requirements = requirementUpdate model.Requirements
                Hints = hintUpdate model.Hints
        }
    | WorkflowDoc model ->
        WorkflowDoc {
            model with
                Requirements = requirementUpdate model.Requirements
                Hints = hintUpdate model.Hints
        }
    | ExpressionToolDoc model ->
        ExpressionToolDoc {
            model with
                Requirements = requirementUpdate model.Requirements
                Hints = hintUpdate model.Hints
        }
    | OperationDoc model ->
        OperationDoc {
            model with
                Requirements = requirementUpdate model.Requirements
                Hints = hintUpdate model.Hints
        }

let private toggleByKey key enabled requirementNodeId items =
    let withoutKey = items |> List.filter (fun node -> node.Key <> key)

    if enabled then
        if withoutKey |> List.exists (fun node -> node.Key = key) then
            withoutKey
        else
            withoutKey
            @ [
                {
                    Id = requirementNodeId |> Option.defaultValue (newRequirementNodeId ())
                    Key = key
                    Fields = emptyStringMap
                }
            ]
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
    updateDocumentRequirements (toggleByKey key enabled None) id document

let setRequirementEnabledWithId requirementNodeId key enabled document =
    updateDocumentRequirements (toggleByKey key enabled (Some requirementNodeId)) id document

let setHintEnabled key enabled document =
    updateDocumentRequirements id (toggleByKey key enabled None) document

let setHintEnabledWithId requirementNodeId key enabled document =
    updateDocumentRequirements id (toggleByKey key enabled (Some requirementNodeId)) document

let setRequirementField requirementNodeId fieldKey value document =
    updateDocumentRequirements (updateFieldById requirementNodeId fieldKey value) id document

let setHintField requirementNodeId fieldKey value document =
    updateDocumentRequirements id (updateFieldById requirementNodeId fieldKey value) document
