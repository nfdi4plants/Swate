module Swate.Components.Shared.Cwl.Features.WorkflowCanvasFeature

open System
open Swate.Components.Shared.Cwl.Documents.Mutations
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

let connectStepInputSource
    (sourceNodeId: string)
    (sourcePortId: string)
    (targetNodeId: string)
    (targetPortId: string)
    (model: WorkflowModel)
    =
    let sourceReference = sourceReferenceFromNode sourceNodeId sourcePortId

    if targetNodeId.StartsWith("step:", StringComparison.Ordinal) |> not then
        model
    else
        let targetStepName = targetNodeId.Substring("step:".Length)

        model.Steps
        |> List.tryFind (fun step -> step.Name = targetStepName)
        |> Option.bind (fun step ->
            step.Inputs
            |> List.tryFind (fun input -> input.Name = targetPortId)
            |> Option.map (fun input -> step.Id, input.Id)
        )
        |> Option.map (fun (stepId, stepInputId) ->
            updateWorkflowStep
                stepId
                (fun step -> {
                    step with
                        Inputs =
                            step.Inputs
                            |> List.map (fun input ->
                                if input.Id = stepInputId then
                                    {
                                        input with
                                            Sources = [ sourceReference ]
                                    }
                                else
                                    input
                            )
                })
                model
        )
        |> Option.defaultValue model

let private trySplitEndpoint (endpoint: string) =
    let separatorIndex = endpoint.LastIndexOf("/", StringComparison.Ordinal)

    if separatorIndex <= 0 || separatorIndex >= endpoint.Length - 1 then
        None
    else
        Some(endpoint.Substring(0, separatorIndex), endpoint.Substring(separatorIndex + 1))

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
            match trySplitEndpoint parts.[0], trySplitEndpoint parts.[1] with
            | Some(sourceNodeId, sourcePortId), Some(targetNodeId, targetPortId) ->
                let sourceReference = sourceReferenceFromNode sourceNodeId sourcePortId

                if targetNodeId.StartsWith("step:", StringComparison.Ordinal) then
                    let targetStepName = targetNodeId.Substring("step:".Length)

                    {
                        model with
                            Steps =
                                model.Steps
                                |> List.map (fun step ->
                                    if step.Name <> targetStepName then
                                        step
                                    else
                                        {
                                            step with
                                                Inputs =
                                                    step.Inputs
                                                    |> List.map (fun input ->
                                                        if input.Name = targetPortId then
                                                            {
                                                                input with
                                                                    Sources =
                                                                        input.Sources
                                                                        |> List.filter ((<>) sourceReference)
                                                            }
                                                        else
                                                            input
                                                    )
                                        }
                                )
                    }
                else
                    let outputName = outputNameFromTargetNode targetNodeId targetPortId

                    {
                        model with
                            Outputs =
                                model.Outputs
                                |> List.choose (fun output ->
                                    if output.Name <> outputName then
                                        Some output
                                    else
                                        let remainingSources =
                                            output.OutputSource |> List.filter ((<>) sourceReference)

                                        if remainingSources.IsEmpty then
                                            None
                                        else
                                            Some {
                                                output with
                                                    OutputSource = remainingSources
                                            }
                                )
                    }
            | _ -> model
