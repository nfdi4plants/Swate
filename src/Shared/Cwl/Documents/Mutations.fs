module Swate.Components.Shared.Cwl.Documents.Mutations

open Swate.Components.Shared.Cwl.Documents.Common
open Swate.Components.Shared.Cwl.Documents.Types

let private append item items = items @ [ item ]

let addInput (input: InputModel) (inputs: InputModel list) = append input inputs

let updateInput (inputId: InputId) update (inputs: InputModel list) =
    inputs
    |> List.map (fun (input: InputModel) -> if input.Id = inputId then update input else input)

let removeInput (inputId: InputId) (inputs: InputModel list) =
    inputs |> List.filter (fun (input: InputModel) -> input.Id <> inputId)

let addOutput (output: OutputModel) (outputs: OutputModel list) = append output outputs

let updateOutput (outputId: OutputId) update (outputs: OutputModel list) =
    outputs
    |> List.map (fun (output: OutputModel) -> if output.Id = outputId then update output else output)

let removeOutput (outputId: OutputId) (outputs: OutputModel list) =
    outputs |> List.filter (fun (output: OutputModel) -> output.Id <> outputId)

let addRequirementNode (node: RequirementNode) (nodes: RequirementNode list) = append node nodes

let updateRequirementNode (requirementNodeId: RequirementNodeId) update (nodes: RequirementNode list) =
    nodes
    |> List.map (fun (node: RequirementNode) -> if node.Id = requirementNodeId then update node else node)

let removeRequirementNode (requirementNodeId: RequirementNodeId) (nodes: RequirementNode list) =
    nodes
    |> List.filter (fun (node: RequirementNode) -> node.Id <> requirementNodeId)

let addWorkflowStep (step: WorkflowStepModel) (model: WorkflowModel) = {
    model with
        Steps = append step model.Steps
}

let updateWorkflowStep (stepId: StepId) update (model: WorkflowModel) = {
    model with
        Steps =
            model.Steps
            |> List.map (fun (step: WorkflowStepModel) -> if step.Id = stepId then update step else step)
}

let removeWorkflowStep (stepId: StepId) (model: WorkflowModel) = {
    model with
        Steps = model.Steps |> List.filter (fun (step: WorkflowStepModel) -> step.Id <> stepId)
}

let addStepInput (stepId: StepId) (stepInput: StepInputModel) (model: WorkflowModel) =
    updateWorkflowStep
        stepId
        (fun step -> {
            step with
                Inputs = append stepInput step.Inputs
        })
        model

let updateStepInput (stepId: StepId) (stepInputId: StepInputId) update (model: WorkflowModel) =
    updateWorkflowStep
        stepId
        (fun step -> {
            step with
                Inputs =
                    step.Inputs
                    |> List.map (fun (input: StepInputModel) -> if input.Id = stepInputId then update input else input)
        })
        model

let removeStepInput (stepId: StepId) (stepInputId: StepInputId) (model: WorkflowModel) =
    updateWorkflowStep
        stepId
        (fun step -> {
            step with
                Inputs =
                    step.Inputs
                    |> List.filter (fun (input: StepInputModel) -> input.Id <> stepInputId)
        })
        model

let addStepOutput (stepId: StepId) (stepOutput: StepOutputModel) (model: WorkflowModel) =
    updateWorkflowStep
        stepId
        (fun step -> {
            step with
                Outputs = append stepOutput step.Outputs
        })
        model

let updateStepOutput (stepId: StepId) (stepOutputId: StepOutputId) update (model: WorkflowModel) =
    updateWorkflowStep
        stepId
        (fun step -> {
            step with
                Outputs =
                    step.Outputs
                    |> List.map (fun (output: StepOutputModel) ->
                        if output.Id = stepOutputId then update output else output
                    )
        })
        model

let removeStepOutput (stepId: StepId) (stepOutputId: StepOutputId) (model: WorkflowModel) =
    updateWorkflowStep
        stepId
        (fun step -> {
            step with
                Outputs =
                    step.Outputs
                    |> List.filter (fun (output: StepOutputModel) -> output.Id <> stepOutputId)
        })
        model

let setCommandLineToolInputs (inputs: InputModel list) (model: CommandLineToolModel) : CommandLineToolModel = {
    model with
        Inputs = inputs
}

let setCommandLineToolOutputs (outputs: OutputModel list) (model: CommandLineToolModel) : CommandLineToolModel = {
    model with
        Outputs = outputs
}

let setWorkflowInputs (inputs: InputModel list) (model: WorkflowModel) : WorkflowModel = { model with Inputs = inputs }

let setWorkflowOutputs (outputs: OutputModel list) (model: WorkflowModel) : WorkflowModel = {
    model with
        Outputs = outputs
}

let setExpressionToolInputs (inputs: InputModel list) (model: ExpressionToolModel) : ExpressionToolModel = {
    model with
        Inputs = inputs
}

let setExpressionToolOutputs (outputs: OutputModel list) (model: ExpressionToolModel) : ExpressionToolModel = {
    model with
        Outputs = outputs
}

let setOperationInputs (inputs: InputModel list) (model: OperationModel) : OperationModel = {
    model with
        Inputs = inputs
}

let setOperationOutputs (outputs: OutputModel list) (model: OperationModel) : OperationModel = {
    model with
        Outputs = outputs
}

let updateDocumentInputs (update: InputModel list -> InputModel list) (document: EditorDocument) =
    match document with
    | CommandLineToolDoc model ->
        CommandLineToolDoc {
            model with
                Inputs = update model.Inputs
        }
    | WorkflowDoc model ->
        WorkflowDoc {
            model with
                Inputs = update model.Inputs
        }
    | ExpressionToolDoc model ->
        ExpressionToolDoc {
            model with
                Inputs = update model.Inputs
        }
    | OperationDoc model ->
        OperationDoc {
            model with
                Inputs = update model.Inputs
        }

let updateDocumentOutputs (update: OutputModel list -> OutputModel list) (document: EditorDocument) =
    match document with
    | CommandLineToolDoc model ->
        CommandLineToolDoc {
            model with
                Outputs = update model.Outputs
        }
    | WorkflowDoc model ->
        WorkflowDoc {
            model with
                Outputs = update model.Outputs
        }
    | ExpressionToolDoc model ->
        ExpressionToolDoc {
            model with
                Outputs = update model.Outputs
        }
    | OperationDoc model ->
        OperationDoc {
            model with
                Outputs = update model.Outputs
        }
