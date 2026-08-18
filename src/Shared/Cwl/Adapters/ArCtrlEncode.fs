module CWLBuilder.Electron.Renderer.Adapters.ArCtrlEncode

open System
open DynamicObj
open ARCtrl.CWL
open CWLBuilder.Domain.RequirementMutations
open CWLBuilder.Domain.WorkflowMutations
open CWLBuilder.Electron.Renderer.Documents.Common
open CWLBuilder.Electron.Renderer.Documents.Types

let private cwlTypeFromKey (value: string option) =
    match value with
    | None -> None
    | Some key ->
        match key.Trim().ToLowerInvariant() with
        | "string" -> Some CWLType.String
        | "int" -> Some CWLType.Int
        | "long" -> Some CWLType.Long
        | "float" -> Some CWLType.Float
        | "double" -> Some CWLType.Double
        | "boolean" -> Some CWLType.Boolean
        | "stdout" -> Some CWLType.Stdout
        | "null" -> Some CWLType.Null
        | "file" -> Some (CWLType.file ())
        | "directory" -> Some (CWLType.directory ())
        | _ -> None

let private setMetadata (metadata: StringMap) (target: obj) =
    match target with
    | :? DynamicObj as dynamicObj ->
        metadata |> Map.iter (fun key value -> dynamicObj.SetProperty(key, value))
    | _ ->
        ()

let private asResizeArray<'T> (values: 'T seq) =
    ResizeArray<'T>(values)

let private optionalResizeArray values =
    if List.isEmpty values then None else Some (asResizeArray values)

let private encodeInputBinding (binding: InputBindingModel option) =
    binding
    |> Option.bind (fun item ->
        let binding = InputBinding.create(?prefix = item.Prefix, ?position = item.Position)
        if binding.Prefix.IsNone && binding.Position.IsNone && binding.ItemSeparator.IsNone && binding.Separate.IsNone then
            None
        else
            Some binding
    )

let private encodeOutputBinding (binding: OutputBindingModel option) =
    binding
    |> Option.bind (fun item ->
        let binding = OutputBinding.create(?glob = item.Glob)
        if binding.Glob.IsNone then None else Some binding
    )

let private encodeInput (input: InputModel) =
    let encoded =
        CWLInput(
            input.Name,
            ?type_ = cwlTypeFromKey input.CwlType,
            ?inputBinding = encodeInputBinding input.InputBinding,
            optional = input.Optional
        )

    setMetadata input.Metadata encoded
    encoded

let private encodeOutput (output: OutputModel) =
    let outputSource =
        match output.OutputSource with
        | [] -> None
        | [ single ] -> Some (OutputSource.Single single)
        | many -> Some (OutputSource.Multiple (asResizeArray many))

    let encoded =
        CWLOutput(
            output.Name,
            ?type_ = cwlTypeFromKey output.CwlType,
            ?outputBinding = encodeOutputBinding output.OutputBinding,
            ?outputSource = outputSource
        )

    setMetadata output.Metadata encoded
    encoded

let private orderedRequirementFields (fields: StringMap) =
    let priority key =
        if key = "dockerFile" then 10
        elif key = "dockerFileMode" then 20
        elif key = "timelimitMode" || key = "workReuseMode" || key = "networkAccessMode" then 10
        elif key = "timelimitValue" || key = "workReuseValue" || key = "networkAccessValue" then 20
        elif key.StartsWith("iwd.value:", StringComparison.Ordinal) then 10
        elif key.StartsWith("iwd.entryMode:", StringComparison.Ordinal) then 20
        elif key.StartsWith("iwd.entryname:", StringComparison.Ordinal) then 30
        elif key.StartsWith("iwd.entrynameMode:", StringComparison.Ordinal) then 40
        elif key.StartsWith("iwd.writable:", StringComparison.Ordinal) then 50
        else 100

    fields
    |> Map.toList
    |> List.sortBy (fun (key, _) -> priority key, key)

let private maxIndexedField prefix (fields: StringMap) =
    fields
    |> Map.toList
    |> List.choose (fun (key, _) ->
        if key.StartsWith(prefix, StringComparison.Ordinal) then
            let indexText = key.Substring(prefix.Length)
            match Int32.TryParse indexText with
            | true, index -> Some index
            | _ -> None
        else
            None
    )
    |> function
        | [] -> None
        | values -> Some (List.max values)

let private bootstrapRequirementFields addField key fields items =
    let repeat fieldKey count =
        for _index = 0 to count - 1 do
            addField items key fieldKey ""

    match key with
    | "schema-def" ->
        maxIndexedField "schema.name:" fields
        |> Option.iter (fun maxIndex -> repeat "schema.add" (maxIndex + 1))
    | "software" ->
        maxIndexedField "software.package:" fields
        |> Option.iter (fun maxIndex -> repeat "software.add" (maxIndex + 1))
    | "env-vars" ->
        maxIndexedField "env.name:" fields
        |> Option.iter (fun maxIndex -> repeat "env.add" (maxIndex + 1))
    | "initial-workdir" ->
        let kinds =
            fields
            |> Map.toList
            |> List.choose (fun (fieldKey, value) ->
                if fieldKey.StartsWith("iwd.kind:", StringComparison.Ordinal) then
                    let indexText = fieldKey.Substring("iwd.kind:".Length)
                    match Int32.TryParse indexText with
                    | true, index -> Some (index, value)
                    | _ -> None
                else
                    None
            )
            |> List.sortBy fst

        for _, kind in kinds do
            let addFieldKey =
                match kind with
                | "string" -> "iwd.addString"
                | "dirent" -> "iwd.addDirent"
                | "file" -> "iwd.addFile"
                | "directory" -> "iwd.addDirectory"
                | _ -> ""

            if String.IsNullOrWhiteSpace addFieldKey |> not then
                addField items key addFieldKey ""
    | _ ->
        ()

let private encodeRequirementCollection bootstrap addField nodes =
    nodes
    |> List.fold (fun state node ->
        let nextState = bootstrap node.Key true state
        bootstrapRequirementFields addField node.Key node.Fields nextState
        orderedRequirementFields node.Fields
        |> List.fold (fun acc (fieldKey, value) ->
            if fieldKey.StartsWith("iwd.kind:", StringComparison.Ordinal) then
                acc
            else
                addField acc node.Key fieldKey value
                acc
        ) nextState
    ) None

let private encodeRequirements nodes =
    encodeRequirementCollection
        toggleRequirement
        setRequirementFieldByKey
        nodes

let private encodeHints nodes =
    encodeRequirementCollection
        toggleHint
        setHintFieldByKey
        nodes

let private setWorkflowStepExternalRunMetadataFromModel (stepModel: WorkflowStepModel) (step: WorkflowStep) =
    stepModel.Metadata
    |> Map.tryFind WorkflowStepOriginalRunStringKey
    |> Option.iter (fun value -> step.SetProperty(WorkflowStepOriginalRunStringKey, value))

    stepModel.Metadata
    |> Map.tryFind WorkflowStepExternalRunAbsolutePathKey
    |> Option.iter (fun value -> step.SetProperty(WorkflowStepExternalRunAbsolutePathKey, value))

let rec private encodeCommandLineTool (model: CommandLineToolModel) =
    let encoded = CWLToolDescription(asResizeArray (model.Outputs |> List.map encodeOutput))
    encoded.CWLVersion <- model.CwlVersion
    encoded.Intent <- optionalResizeArray model.Intent
    encoded.BaseCommand <- optionalResizeArray model.BaseCommand
    encoded.Inputs <- model.Inputs |> List.map encodeInput |> asResizeArray |> Some
    encoded.Requirements <- encodeRequirements model.Requirements
    encoded.Hints <- encodeHints model.Hints
    setMetadata model.Metadata encoded
    encoded

and private encodeWorkflow (model: WorkflowModel) =
    let encoded =
        CWLWorkflowDescription(
            asResizeArray (model.Steps |> List.map encodeWorkflowStep),
            asResizeArray (model.Inputs |> List.map encodeInput),
            asResizeArray (model.Outputs |> List.map encodeOutput)
        )

    encoded.CWLVersion <- model.CwlVersion
    encoded.Intent <- optionalResizeArray model.Intent
    encoded.Requirements <- encodeRequirements model.Requirements
    encoded.Hints <- encodeHints model.Hints
    setMetadata model.Metadata encoded
    encoded

and private encodeExpressionTool (model: ExpressionToolModel) =
    let encoded =
        CWLExpressionToolDescription(
            asResizeArray (model.Outputs |> List.map encodeOutput),
            model.Expression
        )

    encoded.CWLVersion <- model.CwlVersion
    encoded.Intent <- optionalResizeArray model.Intent
    encoded.Inputs <- model.Inputs |> List.map encodeInput |> asResizeArray |> Some
    encoded.Requirements <- encodeRequirements model.Requirements
    encoded.Hints <- encodeHints model.Hints
    setMetadata model.Metadata encoded
    encoded

and private encodeOperation (model: OperationModel) =
    let encoded =
        CWLOperationDescription(
            asResizeArray (model.Inputs |> List.map encodeInput),
            asResizeArray (model.Outputs |> List.map encodeOutput)
        )

    encoded.CWLVersion <- model.CwlVersion
    encoded.Intent <- optionalResizeArray model.Intent
    encoded.Requirements <- encodeRequirements model.Requirements
    encoded.Hints <- encodeHints model.Hints
    setMetadata model.Metadata encoded
    encoded

and private encodeWorkflowRun (stepModel: WorkflowStepModel) =
    match stepModel.Run with
    | ExternalRun path -> WorkflowStepRun.RunString path
    | InlineCommandLineTool model -> WorkflowStepRunOps.fromTool (encodeCommandLineTool model)
    | InlineWorkflow model -> WorkflowStepRunOps.fromWorkflow (encodeWorkflow model)
    | InlineExpressionTool model -> WorkflowStepRunOps.fromExpressionTool (encodeExpressionTool model)
    | InlineOperation model -> WorkflowStepRunOps.fromOperation (encodeOperation model)

and private encodeWorkflowStep (model: WorkflowStepModel) =
    let stepInputs =
        model.Inputs
        |> List.map (fun input ->
            let source =
                match input.Sources with
                | [] -> None
                | values -> Some (asResizeArray values)

            StepInput.create(input.Name, ?source = source)
        )
        |> asResizeArray

    let stepOutputs =
        model.Outputs
        |> List.map (fun output -> StepOutput.StepOutputString output.Name)
        |> asResizeArray

    let encoded = WorkflowStep(model.Name, stepInputs, stepOutputs, encodeWorkflowRun model)
    setMetadata model.Metadata encoded
    match model.Run with
    | ExternalRun _ -> setWorkflowStepExternalRunMetadataFromModel model encoded
    | _ -> ()
    encoded

let toProcessingUnit (document: EditorDocument) =
    match document with
    | CommandLineToolDoc model -> CWLProcessingUnit.CommandLineTool (encodeCommandLineTool model)
    | WorkflowDoc model -> CWLProcessingUnit.Workflow (encodeWorkflow model)
    | ExpressionToolDoc model -> CWLProcessingUnit.ExpressionTool (encodeExpressionTool model)
    | OperationDoc model -> CWLProcessingUnit.Operation (encodeOperation model)
