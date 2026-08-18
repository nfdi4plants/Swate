module CWLBuilder.Electron.Renderer.Documents.Types

open CWLBuilder.Electron.Renderer.Documents.Common

type RequirementNode = {
    Id: RequirementNodeId
    Key: string
    Fields: StringMap
}

type InputBindingModel = {
    Prefix: string option
    Position: int option
}

type OutputBindingModel = {
    Glob: string option
}

type InputModel = {
    Id: InputId
    Name: string
    CwlType: string option
    Optional: bool
    InputBinding: InputBindingModel option
    Metadata: StringMap
}

type OutputModel = {
    Id: OutputId
    Name: string
    CwlType: string option
    OutputBinding: OutputBindingModel option
    OutputSource: string list
    Metadata: StringMap
}

type StepInputModel = {
    Id: StepInputId
    Name: string
    Sources: string list
    Metadata: StringMap
}

type StepOutputModel = {
    Id: StepOutputId
    Name: string
    Metadata: StringMap
}

type CommandLineToolModel = {
    CwlVersion: string
    Intent: string list
    BaseCommand: string list
    Inputs: InputModel list
    Outputs: OutputModel list
    Requirements: RequirementNode list
    Hints: RequirementNode list
    Metadata: StringMap
}

type WorkflowModel = {
    CwlVersion: string
    Intent: string list
    Inputs: InputModel list
    Outputs: OutputModel list
    Steps: WorkflowStepModel list
    Requirements: RequirementNode list
    Hints: RequirementNode list
    Metadata: StringMap
}

and ExpressionToolModel = {
    CwlVersion: string
    Intent: string list
    Expression: string
    Inputs: InputModel list
    Outputs: OutputModel list
    Requirements: RequirementNode list
    Hints: RequirementNode list
    Metadata: StringMap
}

and OperationModel = {
    CwlVersion: string
    Intent: string list
    Inputs: InputModel list
    Outputs: OutputModel list
    Requirements: RequirementNode list
    Hints: RequirementNode list
    Metadata: StringMap
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
    Metadata: StringMap
}

type EditorDocument =
    | CommandLineToolDoc of CommandLineToolModel
    | WorkflowDoc of WorkflowModel
    | ExpressionToolDoc of ExpressionToolModel
    | OperationDoc of OperationModel

let createRequirementNode key =
    {
        Id = newRequirementNodeId ()
        Key = key
        Fields = emptyStringMap
    }

let createInput name =
    {
        Id = newInputId ()
        Name = name
        CwlType = None
        Optional = false
        InputBinding = None
        Metadata = emptyStringMap
    }

let createOutput name =
    {
        Id = newOutputId ()
        Name = name
        CwlType = None
        OutputBinding = None
        OutputSource = []
        Metadata = emptyStringMap
    }

let createStepInput name =
    {
        Id = newStepInputId ()
        Name = name
        Sources = []
        Metadata = emptyStringMap
    }

let createStepOutput name =
    {
        Id = newStepOutputId ()
        Name = name
        Metadata = emptyStringMap
    }

let createWorkflowStep name run =
    {
        Id = newStepId ()
        Name = name
        Run = run
        Inputs = []
        Outputs = []
        Metadata = emptyStringMap
    }

let createCommandLineToolModel cwlVersion =
    {
        CwlVersion = cwlVersion
        Intent = []
        BaseCommand = []
        Inputs = []
        Outputs = []
        Requirements = []
        Hints = []
        Metadata = emptyStringMap
    }

let createWorkflowModel cwlVersion =
    {
        CwlVersion = cwlVersion
        Intent = []
        Inputs = []
        Outputs = []
        Steps = []
        Requirements = []
        Hints = []
        Metadata = emptyStringMap
    }

let createExpressionToolModel cwlVersion expression =
    {
        CwlVersion = cwlVersion
        Intent = []
        Expression = expression
        Inputs = []
        Outputs = []
        Requirements = []
        Hints = []
        Metadata = emptyStringMap
    }

let createOperationModel cwlVersion =
    {
        CwlVersion = cwlVersion
        Intent = []
        Inputs = []
        Outputs = []
        Requirements = []
        Hints = []
        Metadata = emptyStringMap
    }
