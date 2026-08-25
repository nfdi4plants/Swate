module Swate.Components.Shared.Cwl.Documents.Workflow

open System
open Swate.Components.Shared.Cwl.Documents.Common
open Swate.Components.Shared.Cwl.Documents.Types

let tryFindStep (stepId: StepId) (model: WorkflowModel) =
    model.Steps |> List.tryFind (fun step -> step.Id = stepId)

let renameStep (stepId: StepId) (name: string) (model: WorkflowModel) =
    let trimmed = name.Trim()

    if String.IsNullOrWhiteSpace trimmed then
        model
    else
        {
            model with
                Steps =
                    model.Steps
                    |> List.map (fun step ->
                        if step.Id = stepId then
                            { step with Name = trimmed }
                        else
                            step
                    )
        }

let moveStepDown (stepId: StepId) (model: WorkflowModel) =
    let steps = model.Steps |> List.toArray

    match steps |> Array.tryFindIndex (fun step -> step.Id = stepId) with
    | Some index when index >= 0 && index < steps.Length - 1 ->
        let next = steps.[index + 1]
        steps.[index + 1] <- steps.[index]
        steps.[index] <- next

        {
            model with
                Steps = steps |> Array.toList
        }
    | _ -> model

let moveStepUp (stepId: StepId) (model: WorkflowModel) =
    let steps = model.Steps |> List.toArray

    match steps |> Array.tryFindIndex (fun step -> step.Id = stepId) with
    | Some index when index > 0 ->
        let previous = steps.[index - 1]
        steps.[index - 1] <- steps.[index]
        steps.[index] <- previous

        {
            model with
                Steps = steps |> Array.toList
        }
    | _ -> model
