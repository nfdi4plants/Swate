module Swate.Components.Shared.Cwl.Documents.Workflow

open System
open Swate.Components.Shared.Cwl.Documents.Common
open Swate.Components.Shared.Cwl.Documents.Types

module Mutations = Swate.Components.Shared.Cwl.Documents.Mutations

let private nextName (prefix: string) (existing: seq<string>) =
    let existingSet = existing |> Set.ofSeq
    let mutable index = 1
    let mutable candidate = sprintf "%s_%d" prefix index

    while existingSet.Contains candidate do
        index <- index + 1
        candidate <- sprintf "%s_%d" prefix index

    candidate

let private nonEmptyTrimmed (value: string) =
    let trimmed = value.Trim()

    if String.IsNullOrWhiteSpace trimmed then
        None
    else
        Some trimmed

let private parseSourceText (text: string) =
    text.Split([| ',' |], StringSplitOptions.RemoveEmptyEntries)
    |> Array.map (fun item -> item.Trim())
    |> Array.filter (String.IsNullOrWhiteSpace >> not)
    |> Array.toList

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

let addStep (model: WorkflowModel) =
    let step =
        createWorkflowStep (nextName "step" (model.Steps |> List.map (fun step -> step.Name))) (ExternalRun "tool.cwl")

    {
        model with
            Steps = model.Steps @ [ step ]
    },
    step.Id

let removeStep (stepId: StepId) (model: WorkflowModel) =
    Mutations.removeWorkflowStep stepId model

let addStepInput (stepId: StepId) (model: WorkflowModel) =
    match tryFindStep stepId model with
    | Some step ->
        let input =
            createStepInput (nextName "in" (step.Inputs |> List.map (fun input -> input.Name)))

        Mutations.addStepInput stepId input model, input.Id
    | None -> model, newStepInputId ()

let renameStepInput (stepId: StepId) (stepInputId: StepInputId) (name: string) (model: WorkflowModel) =
    match nonEmptyTrimmed name with
    | Some trimmed -> Mutations.updateStepInput stepId stepInputId (fun input -> { input with Name = trimmed }) model
    | None -> model

let setStepInputSourceText (stepId: StepId) (stepInputId: StepInputId) (sourceText: string) (model: WorkflowModel) =
    Mutations.updateStepInput
        stepId
        stepInputId
        (fun input -> {
            input with
                Sources = parseSourceText sourceText
        })
        model

let removeStepInput (stepId: StepId) (stepInputId: StepInputId) (model: WorkflowModel) =
    Mutations.removeStepInput stepId stepInputId model

let addStepOutput (stepId: StepId) (model: WorkflowModel) =
    match tryFindStep stepId model with
    | Some step ->
        let output =
            createStepOutput (nextName "out" (step.Outputs |> List.map (fun output -> output.Name)))

        Mutations.addStepOutput stepId output model, output.Id
    | None -> model, newStepOutputId ()

let renameStepOutput (stepId: StepId) (stepOutputId: StepOutputId) (name: string) (model: WorkflowModel) =
    match nonEmptyTrimmed name with
    | Some trimmed ->
        Mutations.updateStepOutput stepId stepOutputId (fun output -> { output with Name = trimmed }) model
    | None -> model

let removeStepOutput (stepId: StepId) (stepOutputId: StepOutputId) (model: WorkflowModel) =
    Mutations.removeStepOutput stepId stepOutputId model

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
