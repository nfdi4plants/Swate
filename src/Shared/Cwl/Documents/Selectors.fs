module Swate.Components.Shared.Cwl.Documents.Selectors

open Swate.Components.Shared.Cwl.Documents.Common
open Swate.Components.Shared.Cwl.Documents.Types

type RequirementLookup = { Node: RequirementNode; IsHint: bool }

let private tryFindInputInList (inputId: InputId) (inputs: InputModel list) =
    inputs |> List.tryFind (fun (input: InputModel) -> input.Id = inputId)

let private tryFindOutputInList (outputId: OutputId) (outputs: OutputModel list) =
    outputs |> List.tryFind (fun (output: OutputModel) -> output.Id = outputId)

let private tryFindStepInList (stepId: StepId) (steps: WorkflowStepModel list) =
    steps |> List.tryFind (fun (step: WorkflowStepModel) -> step.Id = stepId)

let private tryFindStepInputInList (stepInputId: StepInputId) (stepInputs: StepInputModel list) =
    stepInputs
    |> List.tryFind (fun (stepInput: StepInputModel) -> stepInput.Id = stepInputId)

let private tryFindStepOutputInList (stepOutputId: StepOutputId) (stepOutputs: StepOutputModel list) =
    stepOutputs
    |> List.tryFind (fun (stepOutput: StepOutputModel) -> stepOutput.Id = stepOutputId)

let private tryFindRequirementInList (requirementNodeId: RequirementNodeId) (nodes: RequirementNode list) =
    nodes
    |> List.tryFind (fun (node: RequirementNode) -> node.Id = requirementNodeId)

let tryFindInput (inputId: InputId) (document: EditorDocument) =
    match document with
    | CommandLineToolDoc model -> tryFindInputInList inputId model.Inputs
    | WorkflowDoc model -> tryFindInputInList inputId model.Inputs
    | ExpressionToolDoc model -> tryFindInputInList inputId model.Inputs
    | OperationDoc model -> tryFindInputInList inputId model.Inputs

let tryFindOutput (outputId: OutputId) (document: EditorDocument) =
    match document with
    | CommandLineToolDoc model -> tryFindOutputInList outputId model.Outputs
    | WorkflowDoc model -> tryFindOutputInList outputId model.Outputs
    | ExpressionToolDoc model -> tryFindOutputInList outputId model.Outputs
    | OperationDoc model -> tryFindOutputInList outputId model.Outputs

let tryFindWorkflowStep (stepId: StepId) (document: EditorDocument) =
    match document with
    | WorkflowDoc model -> tryFindStepInList stepId model.Steps
    | _ -> None

let tryFindStepInput (stepId: StepId) (stepInputId: StepInputId) (document: EditorDocument) =
    tryFindWorkflowStep stepId document
    |> Option.bind (fun step -> tryFindStepInputInList stepInputId step.Inputs)

let tryFindStepOutput (stepId: StepId) (stepOutputId: StepOutputId) (document: EditorDocument) =
    tryFindWorkflowStep stepId document
    |> Option.bind (fun step -> tryFindStepOutputInList stepOutputId step.Outputs)

let tryFindRequirementNode (requirementNodeId: RequirementNodeId) (document: EditorDocument) =
    let lookup requirements hints =
        tryFindRequirementInList requirementNodeId requirements
        |> Option.map (fun node -> { Node = node; IsHint = false })
        |> Option.orElseWith (fun () ->
            tryFindRequirementInList requirementNodeId hints
            |> Option.map (fun node -> { Node = node; IsHint = true })
        )

    match document with
    | CommandLineToolDoc model -> lookup model.Requirements model.Hints
    | WorkflowDoc model -> lookup model.Requirements model.Hints
    | ExpressionToolDoc model -> lookup model.Requirements model.Hints
    | OperationDoc model -> lookup model.Requirements model.Hints
