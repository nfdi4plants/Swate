module CWLBuilder.Electron.Renderer.Adapters.ArCtrlDecode

open System
open System.Globalization
open DynamicObj
open ARCtrl.CWL
open CWLBuilder.Domain.RequirementMutations
open CWLBuilder.Domain.WorkflowMutations
open CWLBuilder.Electron.Renderer.Documents.Common
open CWLBuilder.Electron.Renderer.Documents.Types

let private cwlTypeKey (value: CWLType option) =
    match value with
    | None -> None
    | Some cwlType ->
        match cwlType with
        | CWLType.String -> Some "string"
        | CWLType.Int -> Some "int"
        | CWLType.Long -> Some "long"
        | CWLType.Float -> Some "float"
        | CWLType.Double -> Some "double"
        | CWLType.Boolean -> Some "boolean"
        | CWLType.Stdout -> Some "stdout"
        | CWLType.Null -> Some "null"
        | CWLType.File _ -> Some "file"
        | CWLType.Directory _ -> Some "directory"
        | _ -> None

let private schemaSaladText value =
    match value with
    | SchemaSaladString.Literal text
    | SchemaSaladString.Include text
    | SchemaSaladString.Import text -> text

let private schemaSaladMode value =
    match value with
    | SchemaSaladString.Literal _ -> "literal"
    | SchemaSaladString.Include _ -> "include"
    | SchemaSaladString.Import _ -> "import"

let private invariantText (value: obj) =
    match value with
    | null -> None
    | :? string as text when String.IsNullOrWhiteSpace text |> not -> Some text
    | :? bool as flag -> Some (if flag then "true" else "false")
    | :? int as number -> Some (number.ToString(CultureInfo.InvariantCulture))
    | :? int64 as number -> Some (number.ToString(CultureInfo.InvariantCulture))
    | :? float as number -> Some (number.ToString(CultureInfo.InvariantCulture))
    | _ -> None

let private metadataFromDynamicObj (excludedKeys: string seq) (source: obj) =
    match source with
    | :? DynamicObj as dynamicObj ->
        let excluded =
            excludedKeys
            |> Seq.map (fun key -> key.ToLowerInvariant())
            |> Set.ofSeq

        dynamicObj.GetProperties(false)
        |> Seq.choose (fun kvp ->
            let normalizedKey = kvp.Key.ToLowerInvariant()
            if excluded.Contains normalizedKey then
                None
            else
                invariantText kvp.Value |> Option.map (fun value -> kvp.Key, value)
        )
        |> Map.ofSeq
    | _ ->
        emptyStringMap

let private addField key value fields =
    if String.IsNullOrWhiteSpace value then fields else Map.add key value fields

let private addOptionalField key value fields =
    value |> Option.map (fun text -> addField key text fields) |> Option.defaultValue fields

let private joinLines values =
    values |> Seq.toList |> String.concat "\n"

let private joinOutputSource (value: OutputSource) =
    value.AsValues() |> Seq.toList

let private decodeInputBinding (binding: InputBinding option) =
    binding
    |> Option.map (fun item ->
        {
            Prefix = item.Prefix
            Position = item.Position
        }
    )

let private decodeOutputBinding (binding: OutputBinding option) =
    binding
    |> Option.map (fun item ->
        {
            Glob = item.Glob
        }
    )

let private decodeInput (input: CWLInput) =
    {
        Id = newInputId ()
        Name = input.Name
        CwlType = cwlTypeKey input.Type_
        Optional = input.Optional |> Option.defaultValue false
        InputBinding = decodeInputBinding input.InputBinding
        Metadata =
            metadataFromDynamicObj
                [ "name"; "type"; "type_"; "optional"; "inputbinding" ]
                input
    }

let private decodeOutput (output: CWLOutput) =
    {
        Id = newOutputId ()
        Name = output.Name
        CwlType = cwlTypeKey output.Type_
        OutputBinding = decodeOutputBinding output.OutputBinding
        OutputSource =
            output.OutputSource
            |> Option.map joinOutputSource
            |> Option.defaultValue []
        Metadata =
            metadataFromDynamicObj
                [ "name"; "type"; "type_"; "outputbinding"; "outputsource" ]
                output
    }

let private decodeRequirement (requirement: Requirement) =
    let fields =
        match requirement with
        | Requirement.InlineJavascriptRequirement value ->
            value.ExpressionLib
            |> Option.map joinLines
            |> Option.map (fun text -> Map.ofList [ "expressionLib", text ])
            |> Option.defaultValue Map.empty

        | Requirement.SchemaDefRequirement values ->
            values
            |> Seq.mapi (fun index schemaType ->
                [
                    "schema.name:" + string index, schemaType.Name
                    match cwlTypeKey (Some schemaType.Type_) with
                    | Some key -> "schema.type:" + string index, key
                    | None -> ()
                ]
            )
            |> Seq.collect id
            |> Map.ofSeq

        | Requirement.DockerRequirement value ->
            Map.empty
            |> addOptionalField "dockerPull" value.DockerPull
            |> addOptionalField "dockerImageId" value.DockerImageId
            |> addOptionalField "dockerLoad" value.DockerLoad
            |> addOptionalField "dockerImport" value.DockerImport
            |> addOptionalField "dockerOutputDirectory" value.DockerOutputDirectory
            |> addOptionalField "dockerRunOptions" (value.DockerRunOptions |> Option.map joinLines)
            |> addOptionalField "dockerFile" (value.DockerFile |> Option.map schemaSaladText)
            |> addOptionalField "dockerFileMode" (value.DockerFile |> Option.map schemaSaladMode)

        | Requirement.SoftwareRequirement values ->
            values
            |> Seq.mapi (fun index package ->
                [
                    "software.package:" + string index, package.Package
                    match package.Version |> Option.map joinLines with
                    | Some text -> "software.version:" + string index, text
                    | None -> ()
                    match package.Specs |> Option.map joinLines with
                    | Some text -> "software.specs:" + string index, text
                    | None -> ()
                ]
            )
            |> Seq.collect id
            |> Map.ofSeq

        | Requirement.LoadListingRequirement value ->
            Map.ofList [ "loadListing", LoadListingEnum.toCwlString value.LoadListing ]

        | Requirement.InitialWorkDirRequirement entries ->
            entries
            |> Seq.mapi (fun index entry ->
                let ix = string index
                match entry with
                | StringEntry value ->
                    [
                        "iwd.kind:" + ix, "string"
                        "iwd.value:" + ix, schemaSaladText value
                        "iwd.entryMode:" + ix, schemaSaladMode value
                    ]
                | DirentEntry dirent ->
                    [
                        "iwd.kind:" + ix, "dirent"
                        "iwd.value:" + ix, schemaSaladText dirent.Entry
                        "iwd.entryMode:" + ix, schemaSaladMode dirent.Entry
                        match dirent.Entryname with
                        | Some entryname ->
                            "iwd.entryname:" + ix, schemaSaladText entryname
                            "iwd.entrynameMode:" + ix, schemaSaladMode entryname
                        | None -> ()
                        match dirent.Writable with
                        | Some writable -> "iwd.writable:" + ix, if writable then "true" else "false"
                        | None -> ()
                    ]
                | FileEntry file ->
                    [
                        "iwd.kind:" + ix, "file"
                        match file.TryGetPropertyValue("location") |> Option.bind invariantText with
                        | Some location -> "iwd.value:" + ix, location
                        | None -> ()
                    ]
                | DirectoryEntry directory ->
                    [
                        "iwd.kind:" + ix, "directory"
                        match directory.TryGetPropertyValue("location") |> Option.bind invariantText with
                        | Some location -> "iwd.value:" + ix, location
                        | None -> ()
                    ]
            )
            |> Seq.collect id
            |> Map.ofSeq

        | Requirement.EnvVarRequirement values ->
            values
            |> Seq.mapi (fun index envVar ->
                [
                    "env.name:" + string index, envVar.EnvName
                    "env.value:" + string index, envVar.EnvValue
                ]
            )
            |> Seq.collect id
            |> Map.ofSeq

        | Requirement.ShellCommandRequirement ->
            Map.empty

        | Requirement.ResourceRequirement resource ->
            let fields =
                [ "coresMin"; "coresMax"; "ramMin"; "ramMax"; "tmpdirMin"; "tmpdirMax"; "outdirMin"; "outdirMax" ]
                |> List.choose (fun key ->
                    resource.TryGetInt64(key)
                    |> Option.map (fun value -> key, value.ToString(CultureInfo.InvariantCulture))
                    |> Option.orElseWith (fun () ->
                        resource.TryGetFloat(key)
                        |> Option.map (fun value -> key, value.ToString(CultureInfo.InvariantCulture))
                    )
                    |> Option.orElseWith (fun () ->
                        resource.TryGetExpression(key) |> Option.map (fun value -> key, value)
                    )
                )
            Map.ofList fields

        | Requirement.WorkReuseRequirement value ->
            Map.ofList [
                "workReuseMode", "bool"
                "workReuseValue", if value.EnableReuse then "true" else "false"
            ]

        | Requirement.WorkReuseExpressionRequirement expression ->
            Map.ofList [
                "workReuseMode", "expression"
                "workReuseValue", expression
            ]

        | Requirement.NetworkAccessRequirement value ->
            Map.ofList [
                "networkAccessMode", "bool"
                "networkAccessValue", if value.NetworkAccess then "true" else "false"
            ]

        | Requirement.NetworkAccessExpressionRequirement expression ->
            Map.ofList [
                "networkAccessMode", "expression"
                "networkAccessValue", expression
            ]

        | Requirement.InplaceUpdateRequirement value ->
            Map.ofList [ "inplaceUpdate", if value.InplaceUpdate then "true" else "false" ]

        | Requirement.ToolTimeLimitRequirement value ->
            match value with
            | ToolTimeLimitSeconds seconds ->
                Map.ofList [
                    "timelimitMode", "seconds"
                    "timelimitValue", seconds.ToString(CultureInfo.InvariantCulture)
                ]
            | ToolTimeLimitExpression expression ->
                Map.ofList [
                    "timelimitMode", "expression"
                    "timelimitValue", expression
                ]

        | Requirement.SubworkflowFeatureRequirement
        | Requirement.ScatterFeatureRequirement
        | Requirement.MultipleInputFeatureRequirement
        | Requirement.StepInputExpressionRequirement ->
            Map.empty

    {
        Id = newRequirementNodeId ()
        Key = requirementKey requirement |> Option.defaultValue "unsupported-requirement"
        Fields = fields
    }

let private decodeHint (hint: HintEntry) =
    match hint with
    | KnownHint requirement ->
        decodeRequirement requirement
    | UnknownHint value ->
        {
            Id = newRequirementNodeId ()
            Key = "unknown-hint"
            Fields =
                Map.empty
                |> addOptionalField "class" value.Class
        }

let rec private decodeCommandLineTool (tool: CWLToolDescription) =
    {
        CwlVersion = tool.CWLVersion
        Intent = tool.Intent |> Option.defaultValue (ResizeArray()) |> Seq.toList
        BaseCommand = tool.BaseCommand |> Option.defaultValue (ResizeArray()) |> Seq.toList
        Inputs = tool.Inputs |> Option.defaultValue (ResizeArray()) |> Seq.map decodeInput |> Seq.toList
        Outputs = tool.Outputs |> Seq.map decodeOutput |> Seq.toList
        Requirements = tool.Requirements |> Option.defaultValue (ResizeArray()) |> Seq.map decodeRequirement |> Seq.toList
        Hints = tool.Hints |> Option.defaultValue (ResizeArray()) |> Seq.map decodeHint |> Seq.toList
        Metadata =
            metadataFromDynamicObj
                [ "cwlversion"; "class"; "intent"; "basecommand"; "inputs"; "outputs"; "requirements"; "hints" ]
                tool
    }

and private decodeWorkflow (workflow: CWLWorkflowDescription) =
    {
        CwlVersion = workflow.CWLVersion
        Intent = workflow.Intent |> Option.defaultValue (ResizeArray()) |> Seq.toList
        Inputs = workflow.Inputs |> Seq.map decodeInput |> Seq.toList
        Outputs = workflow.Outputs |> Seq.map decodeOutput |> Seq.toList
        Steps = workflow.Steps |> Seq.map decodeWorkflowStep |> Seq.toList
        Requirements = workflow.Requirements |> Option.defaultValue (ResizeArray()) |> Seq.map decodeRequirement |> Seq.toList
        Hints = workflow.Hints |> Option.defaultValue (ResizeArray()) |> Seq.map decodeHint |> Seq.toList
        Metadata =
            metadataFromDynamicObj
                [ "cwlversion"; "class"; "intent"; "inputs"; "outputs"; "steps"; "requirements"; "hints" ]
                workflow
    }

and private decodeExpressionTool (tool: CWLExpressionToolDescription) =
    {
        CwlVersion = tool.CWLVersion
        Intent = tool.Intent |> Option.defaultValue (ResizeArray()) |> Seq.toList
        Expression = tool.Expression
        Inputs = tool.Inputs |> Option.defaultValue (ResizeArray()) |> Seq.map decodeInput |> Seq.toList
        Outputs = tool.Outputs |> Seq.map decodeOutput |> Seq.toList
        Requirements = tool.Requirements |> Option.defaultValue (ResizeArray()) |> Seq.map decodeRequirement |> Seq.toList
        Hints = tool.Hints |> Option.defaultValue (ResizeArray()) |> Seq.map decodeHint |> Seq.toList
        Metadata =
            metadataFromDynamicObj
                [ "cwlversion"; "class"; "intent"; "expression"; "inputs"; "outputs"; "requirements"; "hints" ]
                tool
    }

and private decodeOperation (operation: CWLOperationDescription) =
    {
        CwlVersion = operation.CWLVersion
        Intent = operation.Intent |> Option.defaultValue (ResizeArray()) |> Seq.toList
        Inputs = operation.Inputs |> Seq.map decodeInput |> Seq.toList
        Outputs = operation.Outputs |> Seq.map decodeOutput |> Seq.toList
        Requirements = operation.Requirements |> Option.defaultValue (ResizeArray()) |> Seq.map decodeRequirement |> Seq.toList
        Hints = operation.Hints |> Option.defaultValue (ResizeArray()) |> Seq.map decodeHint |> Seq.toList
        Metadata =
            metadataFromDynamicObj
                [ "cwlversion"; "class"; "intent"; "inputs"; "outputs"; "requirements"; "hints" ]
                operation
    }

and private decodeWorkflowRun (step: WorkflowStep) =
    let externalRun =
        tryGetWorkflowStepOriginalRunString step
        |> Option.orElseWith (fun () ->
            match step.Run with
            | WorkflowStepRun.RunString runString when String.IsNullOrWhiteSpace runString |> not -> Some runString
            | _ -> None
        )

    match externalRun with
    | Some runString -> ExternalRun runString
    | None ->
        match step.Run with
        | WorkflowStepRun.RunString runString -> ExternalRun runString
        | WorkflowStepRun.RunCommandLineTool toolObj -> InlineCommandLineTool (decodeCommandLineTool (unbox<CWLToolDescription> toolObj))
        | WorkflowStepRun.RunWorkflow workflowObj -> InlineWorkflow (decodeWorkflow (unbox<CWLWorkflowDescription> workflowObj))
        | WorkflowStepRun.RunExpressionTool expressionToolObj -> InlineExpressionTool (decodeExpressionTool (unbox<CWLExpressionToolDescription> expressionToolObj))
        | WorkflowStepRun.RunOperation operationObj -> InlineOperation (decodeOperation (unbox<CWLOperationDescription> operationObj))

and private decodeWorkflowStep (step: WorkflowStep) =
    {
        Id = newStepId ()
        Name = step.Id
        Run = decodeWorkflowRun step
        Inputs =
            step.In
            |> Seq.map (fun input ->
                {
                    Id = newStepInputId ()
                    Name = input.Id
                    Sources = input.Source |> Option.defaultValue (ResizeArray()) |> Seq.toList
                    Metadata = Map.empty
                }
            )
            |> Seq.toList
        Outputs =
            step.Out
            |> Seq.map (fun output ->
                {
                    Id = newStepOutputId ()
                    Name = stepOutputId output
                    Metadata = Map.empty
                }
            )
            |> Seq.toList
        Metadata =
            metadataFromDynamicObj
                [ "id"; "run"; "in"; "out" ]
                step
    }

let fromProcessingUnit (processingUnit: CWLProcessingUnit) =
    match processingUnit with
    | CWLProcessingUnit.CommandLineTool tool -> CommandLineToolDoc (decodeCommandLineTool tool)
    | CWLProcessingUnit.Workflow workflow -> WorkflowDoc (decodeWorkflow workflow)
    | CWLProcessingUnit.ExpressionTool tool -> ExpressionToolDoc (decodeExpressionTool tool)
    | CWLProcessingUnit.Operation operation -> OperationDoc (decodeOperation operation)
