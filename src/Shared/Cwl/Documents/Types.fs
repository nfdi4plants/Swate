module Swate.Components.Shared.Cwl.Documents.Types

open Swate.Components.Shared.Cwl.Documents.Common

type RequirementNode = {
    Id: RequirementNodeId
    Key: string
    Fields: StringMap
}

type InputBindingModel = {
    Prefix: string option
    Position: int option
}

type OutputBindingModel = { Glob: string option }

type InputModel = {
    Id: InputId
    Name: string
    CwlType: string option
    Optional: bool
    InputBinding: InputBindingModel option
    Metadata: MetadataMap
}

type OutputModel = {
    Id: OutputId
    Name: string
    CwlType: string option
    OutputBinding: OutputBindingModel option
    OutputSource: string list
    Metadata: MetadataMap
}

type StepInputModel = {
    Id: StepInputId
    Name: string
    Sources: string list
    Metadata: MetadataMap
}

type StepOutputModel = {
    Id: StepOutputId
    Name: string
    Metadata: MetadataMap
}

type CommandLineToolModel = {
    CwlVersion: string
    Intent: string list
    BaseCommand: string list
    Inputs: InputModel list
    Outputs: OutputModel list
    Requirements: RequirementNode list
    Hints: RequirementNode list
    Metadata: MetadataMap
}

type WorkflowModel = {
    CwlVersion: string
    Intent: string list
    Inputs: InputModel list
    Outputs: OutputModel list
    Steps: WorkflowStepModel list
    Requirements: RequirementNode list
    Hints: RequirementNode list
    Metadata: MetadataMap
}

and ExpressionToolModel = {
    CwlVersion: string
    Intent: string list
    Expression: string
    Inputs: InputModel list
    Outputs: OutputModel list
    Requirements: RequirementNode list
    Hints: RequirementNode list
    Metadata: MetadataMap
}

and OperationModel = {
    CwlVersion: string
    Intent: string list
    Inputs: InputModel list
    Outputs: OutputModel list
    Requirements: RequirementNode list
    Hints: RequirementNode list
    Metadata: MetadataMap
}

and WorkflowRunModel =
    | ExternalRun of RelativePath: string
    | InlineCommandLineTool of CommandLineToolModel
    | InlineWorkflow of WorkflowModel
    | InlineExpressionTool of ExpressionToolModel
    | InlineOperation of OperationModel

and WorkflowStepModel = {
    Id: StepId
    Name: string
    Run: WorkflowRunModel
    Inputs: StepInputModel list
    Outputs: StepOutputModel list
    Metadata: MetadataMap
}

type EditorDocument =
    | CommandLineToolDoc of CommandLineToolModel
    | WorkflowDoc of WorkflowModel
    | ExpressionToolDoc of ExpressionToolModel
    | OperationDoc of OperationModel

let createRequirementNode key = {
    Id = newRequirementNodeId ()
    Key = key
    Fields = emptyStringMap
}

let createInput name = {
    Id = newInputId ()
    Name = name
    CwlType = None
    Optional = false
    InputBinding = None
    Metadata = emptyMetadataMap
}

let createOutput name = {
    Id = newOutputId ()
    Name = name
    CwlType = None
    OutputBinding = None
    OutputSource = []
    Metadata = emptyMetadataMap
}

let createStepInput name = {
    Id = newStepInputId ()
    Name = name
    Sources = []
    Metadata = emptyMetadataMap
}

let createStepOutput name = {
    Id = newStepOutputId ()
    Name = name
    Metadata = emptyMetadataMap
}

let createWorkflowStep name run = {
    Id = newStepId ()
    Name = name
    Run = run
    Inputs = []
    Outputs = []
    Metadata = emptyMetadataMap
}

let createCommandLineToolModel cwlVersion = {
    CwlVersion = cwlVersion
    Intent = []
    BaseCommand = []
    Inputs = []
    Outputs = []
    Requirements = []
    Hints = []
    Metadata = emptyMetadataMap
}

let createWorkflowModel cwlVersion = {
    CwlVersion = cwlVersion
    Intent = []
    Inputs = []
    Outputs = []
    Steps = []
    Requirements = []
    Hints = []
    Metadata = emptyMetadataMap
}

let createExpressionToolModel cwlVersion expression = {
    CwlVersion = cwlVersion
    Intent = []
    Expression = expression
    Inputs = []
    Outputs = []
    Requirements = []
    Hints = []
    Metadata = emptyMetadataMap
}

let createOperationModel cwlVersion = {
    CwlVersion = cwlVersion
    Intent = []
    Inputs = []
    Outputs = []
    Requirements = []
    Hints = []
    Metadata = emptyMetadataMap
}
