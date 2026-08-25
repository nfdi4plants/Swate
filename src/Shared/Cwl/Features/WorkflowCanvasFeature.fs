module Swate.Components.Shared.Cwl.Features.WorkflowCanvasFeature

open System
open Swate.Components.Shared.Cwl.Documents.Types

let private outputNameFromTargetNode (targetNodeId: string) (targetPortId: string) =
    if targetNodeId.StartsWith("output:", StringComparison.Ordinal) then
        targetNodeId.Substring("output:".Length)
    else
        targetPortId

let private sourceReferenceFromNode (sourceNodeId: string) (sourcePortId: string) =
    if sourceNodeId.StartsWith("step:", StringComparison.Ordinal) then
        let stepId = sourceNodeId.Substring("step:".Length)
        $"{stepId}/{sourcePortId}"
    elif sourceNodeId.StartsWith("in:", StringComparison.Ordinal) then
        sourcePortId
    else
        $"{sourceNodeId}/{sourcePortId}"

let connectOutputSource
    (sourceNodeId: string)
    (sourcePortId: string)
    (targetNodeId: string)
    (targetPortId: string)
    (model: WorkflowModel)
    =
    let outputName = outputNameFromTargetNode targetNodeId targetPortId
    let sourceReference = sourceReferenceFromNode sourceNodeId sourcePortId

    let updatedOutputs =
        match model.Outputs |> List.tryFindIndex (fun output -> output.Name = outputName) with
        | Some index ->
            model.Outputs
            |> List.mapi (fun currentIndex output ->
                if currentIndex = index then
                    {
                        output with
                            OutputSource = [ sourceReference ]
                    }
                else
                    output
            )
        | None ->
            model.Outputs
            @ [
                {
                    createOutput outputName with
                        OutputSource = [ sourceReference ]
                }
            ]

    { model with Outputs = updatedOutputs }

let disconnectEdge (edgeId: string) (model: WorkflowModel) =
    let prefix = "edge:"

    if edgeId.StartsWith(prefix, StringComparison.Ordinal) |> not then
        model
    else
        let body = edgeId.Substring(prefix.Length)
        let parts = body.Split([| "->" |], StringSplitOptions.None)

        if parts.Length <> 2 then
            model
        else
            let targetParts = parts.[1].Split('/')

            if targetParts.Length <> 2 then
                model
            else
                let targetNodeId = targetParts.[0]
                let targetPortId = targetParts.[1]
                let outputName = outputNameFromTargetNode targetNodeId targetPortId

                {
                    model with
                        Outputs = model.Outputs |> List.filter (fun output -> output.Name <> outputName)
                }
