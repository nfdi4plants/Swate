/// Workflow-focused mutation helpers.
/// Keeps step/list mutation logic out of renderer components.
module Swate.Components.Shared.Cwl.WorkflowMutations

open System
open ARCtrl
open ARCtrl.CWL
open Swate.Components.Shared.Cwl.EditorMutations
open Swate.Components.Shared.Cwl.RequirementMutations

type WorkflowStepRunKind =
    | RunStringKind
    | RunCommandLineToolKind
    | RunWorkflowKind
    | RunExpressionToolKind
    | RunOperationKind

type WorkflowStepRunDetails = {
    KindLabel: string
    InputIds: string array
    OutputIds: string array
}

[<Literal>]
let WorkflowStepOriginalRunStringKey = "_cwlbuilder_original_run_string"

[<Literal>]
let WorkflowStepExternalRunAbsolutePathKey = "_cwlbuilder_external_run_abs_path"

let private tryGetStepMetadataString (step: WorkflowStep) (key: string) =
    try
        let value = step.GetPropertyValue(key)
        if isNull value then None else Some(string value)
    with _ ->
        None

let private toWorkflowRelativePath (workflowPath: string) (targetFilePath: string) =
    let workflowSegments = ArcPathHelper.split (ArcPathHelper.normalize workflowPath)

    let workflowDirectorySegments =
        if workflowSegments.Length <= 1 then
            [||]
        else
            workflowSegments.[0 .. workflowSegments.Length - 2]

    let targetSegments = ArcPathHelper.split (ArcPathHelper.normalize targetFilePath)

    let mutable sharedCount = 0
    let maxShared = min workflowDirectorySegments.Length targetSegments.Length

    while sharedCount < maxShared
          && String.Equals(
              workflowDirectorySegments.[sharedCount],
              targetSegments.[sharedCount],
              StringComparison.OrdinalIgnoreCase
          ) do
        sharedCount <- sharedCount + 1

    let upSegments = Array.create (workflowDirectorySegments.Length - sharedCount) ".."
    let downSegments = targetSegments.[sharedCount..]
    let relativeSegments = Array.append upSegments downSegments

    if relativeSegments.Length = 0 then
        "."
    else
        ArcPathHelper.combineMany relativeSegments

let private normalizeAbsolutePath (workflowPath: string) (runString: string) =
    let trimmedRunString = runString.Trim()
    let normalizedRun = ArcPathHelper.normalize trimmedRunString

    if
        normalizedRun.StartsWith("/", StringComparison.Ordinal)
        || normalizedRun.StartsWith("//", StringComparison.Ordinal)
        || (normalizedRun.Length > 2 && normalizedRun.[1] = ':' && normalizedRun.[2] = '/')
    then
        normalizedRun
    else
        ArcPathHelper.resolvePathFromFile workflowPath normalizedRun
        |> ArcPathHelper.normalize

let setWorkflowStepExternalRunMetadata (workflowPath: string) (step: WorkflowStep) (runString: string) =
    let trimmedRun = runString.Trim()

    if String.IsNullOrWhiteSpace trimmedRun |> not then
        step.SetProperty(WorkflowStepOriginalRunStringKey, trimmedRun)
        step.SetProperty(WorkflowStepExternalRunAbsolutePathKey, normalizeAbsolutePath workflowPath trimmedRun)

let clearWorkflowStepExternalRunMetadata (step: WorkflowStep) =
    step.SetProperty(WorkflowStepOriginalRunStringKey, null)
    step.SetProperty(WorkflowStepExternalRunAbsolutePathKey, null)

let tryGetWorkflowStepOriginalRunString (step: WorkflowStep) =
    tryGetStepMetadataString step WorkflowStepOriginalRunStringKey
    |> Option.bind (fun value -> if String.IsNullOrWhiteSpace value then None else Some value)

let tryGetWorkflowStepExternalRunAbsolutePath (step: WorkflowStep) =
    tryGetStepMetadataString step WorkflowStepExternalRunAbsolutePathKey
    |> Option.bind (fun value -> if String.IsNullOrWhiteSpace value then None else Some value)

let tryGetWorkflowStepExternalRunPathForSave
    (step: WorkflowStep)
    (targetWorkflowPath: string)
    (sourceWorkflowPath: string option)
    =
    match tryGetWorkflowStepOriginalRunString step, tryGetWorkflowStepExternalRunAbsolutePath step with
    | Some originalRun, Some absolutePath ->
        let normalizedTargetWorkflowPath = ArcPathHelper.normalizePathKey targetWorkflowPath

        match sourceWorkflowPath with
        | Some sourcePath when
            String.Equals(
                ArcPathHelper.normalizePathKey sourcePath,
                normalizedTargetWorkflowPath,
                StringComparison.OrdinalIgnoreCase
            )
            ->
            Some originalRun
        | _ -> Some(toWorkflowRelativePath normalizedTargetWorkflowPath absolutePath)
    | _ -> None

let tryEncodeWorkflowStepRunYaml (step: WorkflowStep) =
    match step.Run with
    | WorkflowStepRun.RunString _ -> None
    | WorkflowStepRun.RunCommandLineTool toolObj ->
        let tool = unbox<CWLToolDescription> toolObj
        CWLProcessingUnit.CommandLineTool tool |> Encode.encodeProcessingUnit |> Some
    | WorkflowStepRun.RunWorkflow workflowObj ->
        let workflow = unbox<CWLWorkflowDescription> workflowObj
        CWLProcessingUnit.Workflow workflow |> Encode.encodeProcessingUnit |> Some
    | WorkflowStepRun.RunExpressionTool expressionToolObj ->
        let expressionTool = unbox<CWLExpressionToolDescription> expressionToolObj

        CWLProcessingUnit.ExpressionTool expressionTool
        |> Encode.encodeProcessingUnit
        |> Some
    | WorkflowStepRun.RunOperation operationObj ->
        let operation = unbox<CWLOperationDescription> operationObj
        CWLProcessingUnit.Operation operation |> Encode.encodeProcessingUnit |> Some

let tryGetWorkflowStepRunDetails (step: WorkflowStep) =
    let toInputIds (inputs: ResizeArray<CWLInput> option) =
        inputs
        |> Option.defaultValue (ResizeArray())
        |> Seq.map (fun input -> input.Name)
        |> Seq.toArray

    let toOutputIds (outputs: ResizeArray<CWLOutput>) =
        outputs |> Seq.map (fun output -> output.Name) |> Seq.toArray

    match step.Run with
    | WorkflowStepRun.RunString _ -> None
    | WorkflowStepRun.RunCommandLineTool toolObj ->
        let tool = unbox<CWLToolDescription> toolObj

        Some {
            KindLabel = "CommandLineTool"
            InputIds = toInputIds tool.Inputs
            OutputIds = toOutputIds tool.Outputs
        }
    | WorkflowStepRun.RunWorkflow workflowObj ->
        let workflow = unbox<CWLWorkflowDescription> workflowObj

        Some {
            KindLabel = "Workflow"
            InputIds = workflow.Inputs |> Seq.map (fun input -> input.Name) |> Seq.toArray
            OutputIds = workflow.Outputs |> Seq.map (fun output -> output.Name) |> Seq.toArray
        }
    | WorkflowStepRun.RunExpressionTool expressionToolObj ->
        let expressionTool = unbox<CWLExpressionToolDescription> expressionToolObj

        Some {
            KindLabel = "ExpressionTool"
            InputIds = toInputIds expressionTool.Inputs
            OutputIds = toOutputIds expressionTool.Outputs
        }
    | WorkflowStepRun.RunOperation operationObj ->
        let operation = unbox<CWLOperationDescription> operationObj

        Some {
            KindLabel = "Operation"
            InputIds = operation.Inputs |> Seq.map (fun input -> input.Name) |> Seq.toArray
            OutputIds = operation.Outputs |> Seq.map (fun output -> output.Name) |> Seq.toArray
        }

let private tryGetStep (steps: ResizeArray<WorkflowStep>) (stepIndex: int) =
    if stepIndex >= 0 && stepIndex < steps.Count then
        Some steps.[stepIndex]
    else
        None

let private parseSourceText (text: string) =
    text.Split([| ',' |], StringSplitOptions.RemoveEmptyEntries)
    |> Array.map (fun item -> item.Trim())
    |> Array.filter (String.IsNullOrWhiteSpace >> not)
    |> ResizeArray
    |> fun source -> if source.Count = 0 then None else Some source

let stepInputSourceText (stepInput: StepInput) =
    stepInput.Source
    |> Option.defaultValue (ResizeArray())
    |> Seq.toList
    |> String.concat ", "

let stepOutputId (stepOutput: StepOutput) =
    match stepOutput with
    | StepOutput.StepOutputString id -> id
    | StepOutput.StepOutputRecord record -> record.Id

let stepRunDisplay (step: WorkflowStep) =
    match step.Run with
    | WorkflowStepRun.RunString runString -> runString
    | WorkflowStepRun.RunCommandLineTool _ -> "[inline CommandLineTool]"
    | WorkflowStepRun.RunWorkflow _ -> "[inline Workflow]"
    | WorkflowStepRun.RunExpressionTool _ -> "[inline ExpressionTool]"
    | WorkflowStepRun.RunOperation _ -> "[inline Operation]"

let stepRunKind (step: WorkflowStep) =
    match step.Run with
    | WorkflowStepRun.RunString _ -> RunStringKind
    | WorkflowStepRun.RunCommandLineTool _ -> RunCommandLineToolKind
    | WorkflowStepRun.RunWorkflow _ -> RunWorkflowKind
    | WorkflowStepRun.RunExpressionTool _ -> RunExpressionToolKind
    | WorkflowStepRun.RunOperation _ -> RunOperationKind

let isStepRunEditable (step: WorkflowStep) =
    match step.Run with
    | WorkflowStepRun.RunString _ -> true
    | _ -> false

let setWorkflowRequirementEnabled (workflow: CWLWorkflowDescription) (key: string) (enabled: bool) =
    workflow.Requirements <- toggleRequirement key enabled workflow.Requirements

let setWorkflowHintEnabled (workflow: CWLWorkflowDescription) (key: string) (enabled: bool) =
    workflow.Hints <- toggleHint key enabled workflow.Hints

let setWorkflowRequirementField (workflow: CWLWorkflowDescription) (key: string) (fieldKey: string) (value: string) =
    setRequirementFieldByKey workflow.Requirements key fieldKey value

let setWorkflowHintField (workflow: CWLWorkflowDescription) (key: string) (fieldKey: string) (value: string) =
    setHintFieldByKey workflow.Hints key fieldKey value

let setWorkflowRequirementDockerField
    (workflow: CWLWorkflowDescription)
    (key: string)
    (fieldKey: string)
    (value: string)
    =
    setWorkflowRequirementField workflow key fieldKey value

let setWorkflowHintDockerField (workflow: CWLWorkflowDescription) (key: string) (fieldKey: string) (value: string) =
    setWorkflowHintField workflow key fieldKey value

let addWorkflowInput (workflow: CWLWorkflowDescription) =
    let name = nextName "input" (workflow.Inputs |> Seq.map (fun input -> input.Name))
    let input = CWLInput(name)
    input.Type_ <- Some CWLType.String
    workflow.Inputs.Add(input)
    workflow.Inputs.Count - 1

let addWorkflowOutput (workflow: CWLWorkflowDescription) =
    let name =
        nextName "output" (workflow.Outputs |> Seq.map (fun output -> output.Name))

    let output = CWLOutput(name)
    output.Type_ <- Some(CWLType.file ())
    workflow.Outputs.Add(output)
    workflow.Outputs.Count - 1

let addWorkflowStep (workflow: CWLWorkflowDescription) =
    let stepId = nextName "step" (workflow.Steps |> Seq.map (fun step -> step.Id))

    let step =
        WorkflowStep.fromRunPath (stepId, ResizeArray(), ResizeArray(), "tool.cwl")

    workflow.Steps.Add(step)
    workflow.Steps.Count - 1

let removeWorkflowStep (activeStepIndex: int option) (steps: ResizeArray<WorkflowStep>) =
    removeAtAndSelectNext activeStepIndex steps

let moveWorkflowStepUp (activeStepIndex: int option) (steps: ResizeArray<WorkflowStep>) = moveUp activeStepIndex steps

let moveWorkflowStepDown (activeStepIndex: int option) (steps: ResizeArray<WorkflowStep>) =
    moveDown activeStepIndex steps

let setWorkflowStepIdAt (steps: ResizeArray<WorkflowStep>) (stepIndex: int) (newId: string) =
    match tryGetStep steps stepIndex with
    | Some step ->
        let trimmed = newId.Trim()

        if String.IsNullOrWhiteSpace trimmed |> not then
            step.Id <- trimmed
    | None -> ()

let setWorkflowStepRunAt (steps: ResizeArray<WorkflowStep>) (stepIndex: int) (runTarget: string) =
    match tryGetStep steps stepIndex with
    | Some step ->
        if isStepRunEditable step then
            step.Run <- WorkflowStepRun.RunString(runTarget.Trim())
            clearWorkflowStepExternalRunMetadata step
    | None -> ()

let setWorkflowStepRunKindAt (steps: ResizeArray<WorkflowStep>) (stepIndex: int) (runKind: WorkflowStepRunKind) =
    match tryGetStep steps stepIndex with
    | Some step ->
        let currentRunString =
            match step.Run with
            | WorkflowStepRun.RunString runString when String.IsNullOrWhiteSpace runString |> not -> runString
            | _ -> "tool.cwl"

        let nextRun =
            match runKind with
            | RunStringKind -> WorkflowStepRun.RunString currentRunString
            | RunCommandLineToolKind ->
                let tool = CWLToolDescription(ResizeArray())
                WorkflowStepRunOps.fromTool tool
            | RunWorkflowKind ->
                let workflow = CWLWorkflowDescription(ResizeArray(), ResizeArray(), ResizeArray())
                WorkflowStepRunOps.fromWorkflow workflow
            | RunExpressionToolKind ->
                let expressionTool = CWLExpressionToolDescription(ResizeArray(), "")
                WorkflowStepRunOps.fromExpressionTool expressionTool
            | RunOperationKind ->
                let operation = CWLOperationDescription(ResizeArray(), ResizeArray())
                WorkflowStepRunOps.fromOperation operation

        step.Run <- nextRun

        if runKind <> RunStringKind then
            clearWorkflowStepExternalRunMetadata step
    | None -> ()

let addWorkflowStepInputAt (steps: ResizeArray<WorkflowStep>) (stepIndex: int) =
    match tryGetStep steps stepIndex with
    | Some step ->
        let inputId = nextName "in" (step.In |> Seq.map (fun input -> input.Id))
        let stepInput = StepInput.create (inputId)
        WorkflowStep.addInput stepInput step
        Some(step.In.Count - 1)
    | None -> None

let removeWorkflowStepInputAt (steps: ResizeArray<WorkflowStep>) (stepIndex: int) (activeInputIndex: int option) =
    match tryGetStep steps stepIndex with
    | Some step -> removeAtAndSelectNext activeInputIndex step.In
    | None -> activeInputIndex

let moveWorkflowStepInputUp (steps: ResizeArray<WorkflowStep>) (stepIndex: int) (activeInputIndex: int option) =
    match tryGetStep steps stepIndex with
    | Some step -> moveUp activeInputIndex step.In
    | None -> activeInputIndex

let moveWorkflowStepInputDown (steps: ResizeArray<WorkflowStep>) (stepIndex: int) (activeInputIndex: int option) =
    match tryGetStep steps stepIndex with
    | Some step -> moveDown activeInputIndex step.In
    | None -> activeInputIndex

let setWorkflowStepInputIdAt (steps: ResizeArray<WorkflowStep>) (stepIndex: int) (inputIndex: int) (newId: string) =
    match tryGetStep steps stepIndex with
    | Some step ->
        let trimmed = newId.Trim()

        if String.IsNullOrWhiteSpace trimmed |> not then
            WorkflowStep.updateInputAt inputIndex (fun input -> { input with Id = trimmed }) step
    | None -> ()

let setWorkflowStepInputSourceAt
    (steps: ResizeArray<WorkflowStep>)
    (stepIndex: int)
    (inputIndex: int)
    (sourceText: string)
    =
    match tryGetStep steps stepIndex with
    | Some step ->
        let source = sourceText |> nonEmptyOrNone |> Option.bind parseSourceText
        WorkflowStep.updateInputAt inputIndex (fun input -> { input with Source = source }) step
    | None -> ()

let addWorkflowStepOutputAt (steps: ResizeArray<WorkflowStep>) (stepIndex: int) =
    match tryGetStep steps stepIndex with
    | Some step ->
        let outputId = nextName "out" (step.Out |> Seq.map stepOutputId)
        step.Out.Add(StepOutput.StepOutputString outputId)
        Some(step.Out.Count - 1)
    | None -> None

let removeWorkflowStepOutputAt (steps: ResizeArray<WorkflowStep>) (stepIndex: int) (activeOutputIndex: int option) =
    match tryGetStep steps stepIndex with
    | Some step -> removeAtAndSelectNext activeOutputIndex step.Out
    | None -> activeOutputIndex

let moveWorkflowStepOutputUp (steps: ResizeArray<WorkflowStep>) (stepIndex: int) (activeOutputIndex: int option) =
    match tryGetStep steps stepIndex with
    | Some step -> moveUp activeOutputIndex step.Out
    | None -> activeOutputIndex

let moveWorkflowStepOutputDown (steps: ResizeArray<WorkflowStep>) (stepIndex: int) (activeOutputIndex: int option) =
    match tryGetStep steps stepIndex with
    | Some step -> moveDown activeOutputIndex step.Out
    | None -> activeOutputIndex

let setWorkflowStepOutputIdAt (steps: ResizeArray<WorkflowStep>) (stepIndex: int) (outputIndex: int) (newId: string) =
    match tryGetStep steps stepIndex with
    | Some step when outputIndex >= 0 && outputIndex < step.Out.Count ->
        let trimmed = newId.Trim()

        if String.IsNullOrWhiteSpace trimmed |> not then
            let updatedOutput =
                match step.Out.[outputIndex] with
                | StepOutput.StepOutputString _ -> StepOutput.StepOutputString trimmed
                | StepOutput.StepOutputRecord record -> StepOutput.StepOutputRecord { record with Id = trimmed }

            step.Out.[outputIndex] <- updatedOutput
    | _ -> ()
