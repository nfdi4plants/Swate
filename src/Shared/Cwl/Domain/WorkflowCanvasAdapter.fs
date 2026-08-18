/// Adapter between ARCtrl workflow types and a canvas-friendly graph model.
/// This isolates upcoming workflow-canvas logic from renderer-specific state.
module Swate.Components.Shared.Cwl.WorkflowCanvasAdapter

open System
open ARCtrl.CWL
open ARCtrl.WorkflowGraph
open Swate.Components.Shared.Cwl.WorkflowMutations

type CanvasNodeKind =
    | WorkflowInputNode
    | WorkflowStepNode
    | WorkflowOutputNode

type CanvasEdgeKind =
    | InputToStep
    | StepToStep
    | StepToOutput
    | InputToOutput

type CanvasNode = {
    Id: string
    Kind: CanvasNodeKind
    Label: string
}

type CanvasEdge = {
    Id: string
    Kind: CanvasEdgeKind
    SourceNodeId: string
    SourcePortId: string
    TargetNodeId: string
    TargetPortId: string
    SourceReference: string option
}

type WorkflowCanvasGraph = {
    Nodes: ResizeArray<CanvasNode>
    Edges: ResizeArray<CanvasEdge>
}

type WorkflowGraphDiagnostic = {
    Kind: string
    Message: string
    Scope: string option
    Reference: string option
}

type WorkflowGraphReadModel = {
    NodeCount: int
    EdgeCount: int
    Diagnostics: ResizeArray<WorkflowGraphDiagnostic>
}

type CanvasPort = {
    NodeId: string
    PortId: string
    Label: string
}

type private SourceParts = {
    StepId: string option
    PortId: string
}

let private stepNodeId (stepId: string) = $"step:{stepId}"

[<Literal>]
let WorkflowInputSourceNodeId = "in:workflow"

[<Literal>]
let WorkflowOutputSinkNodeId = "out:workflow"

let private tryStepIdFromNodeId (nodeId: string) =
    if nodeId.StartsWith("step:", StringComparison.Ordinal) then
        Some(nodeId.Substring("step:".Length))
    else
        None

let private tryFindOutputTypeByName (outputs: ResizeArray<CWLOutput>) (portId: string) =
    outputs
    |> Seq.tryFind (fun output -> output.Name = portId)
    |> Option.bind (fun output -> output.Type_)

let private tryResolveEdgeSourceType (workflow: CWLWorkflowDescription) (edge: CanvasEdge) : CWLType option =
    match edge.Kind with
    | InputToOutput
    | InputToStep ->
        workflow.Inputs
        |> Seq.tryFind (fun input -> input.Name = edge.SourcePortId)
        |> Option.bind (fun input -> input.Type_)
    | StepToOutput
    | StepToStep ->
        match tryStepIdFromNodeId edge.SourceNodeId with
        | None -> None
        | Some stepId ->
            workflow.Steps
            |> Seq.tryFind (fun step -> step.Id = stepId)
            |> Option.bind (fun step ->
                match step.Run with
                | WorkflowStepRun.RunCommandLineTool toolObj ->
                    let tool = unbox<CWLToolDescription> toolObj
                    tryFindOutputTypeByName tool.Outputs edge.SourcePortId
                | WorkflowStepRun.RunExpressionTool expressionToolObj ->
                    let expressionTool = unbox<CWLExpressionToolDescription> expressionToolObj
                    tryFindOutputTypeByName expressionTool.Outputs edge.SourcePortId
                | WorkflowStepRun.RunWorkflow nestedWorkflowObj ->
                    let nestedWorkflow = unbox<CWLWorkflowDescription> nestedWorkflowObj
                    tryFindOutputTypeByName nestedWorkflow.Outputs edge.SourcePortId
                | WorkflowStepRun.RunString _ -> None
                | WorkflowStepRun.RunOperation operationObj ->
                    let operation = unbox<CWLOperationDescription> operationObj
                    tryFindOutputTypeByName operation.Outputs edge.SourcePortId
            )

let private createEdgeId (sourceNodeId: string) (sourcePortId: string) (targetNodeId: string) (targetPortId: string) =
    $"edge:{sourceNodeId}/{sourcePortId}->{targetNodeId}/{targetPortId}"

let private parseSourceReference (source: string) =
    if String.IsNullOrWhiteSpace source then
        { StepId = None; PortId = "" }
    else
        // Strip optional '#' prefixes for graph-node lookup only.
        // The original source token is kept on edges and reused on writeback.
        let normalized =
            let trimmed = source.Trim()

            if trimmed.StartsWith "#" then
                trimmed.Substring(1)
            else
                trimmed

        let separatorIndex = normalized.IndexOf('/')

        if separatorIndex < 0 then
            {
                StepId = None
                PortId = normalized.Trim()
            }
        else
            let stepId = normalized.Substring(0, separatorIndex).Trim()
            let portId = normalized.Substring(separatorIndex + 1).Trim()

            {
                StepId =
                    if String.IsNullOrWhiteSpace stepId then
                        None
                    else
                        Some stepId
                PortId = portId
            }

let private edgeSourceReference (edge: CanvasEdge) =
    edge.SourceReference
    |> Option.bind (fun source ->
        if String.IsNullOrWhiteSpace source then
            None
        else
            Some(source.Trim())
    )
    |> Option.defaultWith (fun () ->
        match edge.Kind with
        | InputToStep
        | InputToOutput -> edge.SourcePortId
        | StepToStep
        | StepToOutput ->
            match tryStepIdFromNodeId edge.SourceNodeId with
            | Some stepId -> $"{stepId}/{edge.SourcePortId}"
            | None -> edge.SourcePortId
    )

let private tryAddEdge (edges: ResizeArray<CanvasEdge>) (edge: CanvasEdge) =
    if
        String.IsNullOrWhiteSpace edge.SourceNodeId
        || String.IsNullOrWhiteSpace edge.SourcePortId
        || String.IsNullOrWhiteSpace edge.TargetNodeId
        || String.IsNullOrWhiteSpace edge.TargetPortId
    then
        false
    elif edges |> Seq.exists (fun existing -> existing.Id = edge.Id) then
        false
    else
        edges.Add(edge)
        true

let private edgeKindFromNodeIds (sourceNodeId: string) (targetNodeId: string) =
    let sourceIsInput = sourceNodeId.StartsWith("in:", StringComparison.Ordinal)
    let sourceIsStep = sourceNodeId.StartsWith("step:", StringComparison.Ordinal)
    let targetIsStep = targetNodeId.StartsWith("step:", StringComparison.Ordinal)
    let targetIsOutput = targetNodeId.StartsWith("out:", StringComparison.Ordinal)

    match sourceIsInput, sourceIsStep, targetIsStep, targetIsOutput with
    | true, false, true, false -> Some InputToStep
    | false, true, true, false -> Some StepToStep
    | false, true, false, true -> Some StepToOutput
    | true, false, false, true -> Some InputToOutput
    | _ -> None

let private graphIssueKindKey kind =
    match kind with
    | GraphIssueKind.MissingReference -> "missing-reference"
    | GraphIssueKind.InvalidReference -> "invalid-reference"
    | GraphIssueKind.ResolutionFailed -> "resolution-failed"
    | GraphIssueKind.CycleDetected -> "cycle-detected"
    | GraphIssueKind.MissingCwlDescription -> "missing-cwl-description"
    | GraphIssueKind.UnexpectedRuntimeType -> "unexpected-runtime-type"

let buildWorkflowGraphReadModel
    (workflow: CWLWorkflowDescription)
    (workflowPath: string option)
    (tryResolveRunPath: (string -> CWLProcessingUnit option) option)
    =
    let strictUnresolvedRunReferences = tryResolveRunPath |> Option.isSome

    let options =
        WorkflowGraphBuildOptions.defaultOptions
        |> WorkflowGraphBuildOptions.withRootWorkflowFilePath workflowPath
        |> WorkflowGraphBuildOptions.withTryResolveRunPath tryResolveRunPath
        |> WorkflowGraphBuildOptions.withStrictUnresolvedRunReferences strictUnresolvedRunReferences
        |> WorkflowGraphBuildOptions.withExpandNestedWorkflows true

    let graph = WorkflowGraphApi.buildWith options (CWLProcessingUnit.Workflow workflow)

    let diagnostics =
        graph.Diagnostics
        |> Seq.map (fun issue -> {
            Kind = graphIssueKindKey issue.Kind
            Message = issue.Message
            Scope = issue.Scope
            Reference = issue.Reference
        })
        |> ResizeArray

    {
        NodeCount = graph.NodeCount
        EdgeCount = graph.EdgeCount
        Diagnostics = diagnostics
    }

let sourcePorts (workflow: CWLWorkflowDescription) =
    let ports = ResizeArray<CanvasPort>()

    for input in workflow.Inputs do
        ports.Add(
            {
                NodeId = WorkflowInputSourceNodeId
                PortId = input.Name
                Label = $"input/{input.Name}"
            }
        )

    for step in workflow.Steps do
        for stepOutput in step.Out do
            let outputId = stepOutputId stepOutput

            ports.Add(
                {
                    NodeId = stepNodeId step.Id
                    PortId = outputId
                    Label = $"{step.Id}/{outputId}"
                }
            )

    ports

let targetPorts (workflow: CWLWorkflowDescription) =
    let ports = ResizeArray<CanvasPort>()

    for step in workflow.Steps do
        for stepInput in step.In do
            ports.Add(
                {
                    NodeId = stepNodeId step.Id
                    PortId = stepInput.Id
                    Label = $"{step.Id}/{stepInput.Id}"
                }
            )

    for output in workflow.Outputs do
        ports.Add(
            {
                NodeId = WorkflowOutputSinkNodeId
                PortId = output.Name
                Label = $"output/{output.Name}"
            }
        )

    ports

let tryCreateConnectionEdge
    (sourceNodeId: string)
    (sourcePortId: string)
    (targetNodeId: string)
    (targetPortId: string)
    =
    if
        String.IsNullOrWhiteSpace sourceNodeId
        || String.IsNullOrWhiteSpace sourcePortId
        || String.IsNullOrWhiteSpace targetNodeId
        || String.IsNullOrWhiteSpace targetPortId
    then
        None
    else
        edgeKindFromNodeIds sourceNodeId targetNodeId
        |> Option.map (fun kind -> {
            Id = createEdgeId sourceNodeId sourcePortId targetNodeId targetPortId
            Kind = kind
            SourceNodeId = sourceNodeId
            SourcePortId = sourcePortId
            TargetNodeId = targetNodeId
            TargetPortId = targetPortId
            SourceReference = None
        })

let addConnection
    (graph: WorkflowCanvasGraph)
    (sourceNodeId: string)
    (sourcePortId: string)
    (targetNodeId: string)
    (targetPortId: string)
    =
    match tryCreateConnectionEdge sourceNodeId sourcePortId targetNodeId targetPortId with
    | Some edge -> tryAddEdge graph.Edges edge
    | None -> false

let removeConnection (graph: WorkflowCanvasGraph) (edgeId: string) =
    match graph.Edges |> Seq.tryFindIndex (fun edge -> edge.Id = edgeId) with
    | Some index ->
        graph.Edges.RemoveAt(index)
        true
    | None -> false

let toCanvasGraph (workflow: CWLWorkflowDescription) =
    let nodes = ResizeArray<CanvasNode>()
    let edges = ResizeArray<CanvasEdge>()

    nodes.Add(
        {
            Id = WorkflowInputSourceNodeId
            Kind = WorkflowInputNode
            Label = "workflow inputs"
        }
    )

    for step in workflow.Steps do
        nodes.Add(
            {
                Id = stepNodeId step.Id
                Kind = WorkflowStepNode
                Label = step.Id
            }
        )

    nodes.Add(
        {
            Id = WorkflowOutputSinkNodeId
            Kind = WorkflowOutputNode
            Label = "workflow outputs"
        }
    )

    for step in workflow.Steps do
        for stepInput in step.In do
            match stepInput.Source with
            | Some sources ->
                for source in sources do
                    let parsed = parseSourceReference source

                    match parsed.StepId with
                    | Some sourceStepId ->
                        let sourceNodeId = stepNodeId sourceStepId
                        let targetNodeId = stepNodeId step.Id

                        let edge = {
                            Id = createEdgeId sourceNodeId parsed.PortId targetNodeId stepInput.Id
                            Kind = StepToStep
                            SourceNodeId = sourceNodeId
                            SourcePortId = parsed.PortId
                            TargetNodeId = targetNodeId
                            TargetPortId = stepInput.Id
                            SourceReference = Some(source.Trim())
                        }

                        tryAddEdge edges edge |> ignore
                    | None ->
                        let sourceNodeId = WorkflowInputSourceNodeId
                        let targetNodeId = stepNodeId step.Id

                        let edge = {
                            Id = createEdgeId sourceNodeId parsed.PortId targetNodeId stepInput.Id
                            Kind = InputToStep
                            SourceNodeId = sourceNodeId
                            SourcePortId = parsed.PortId
                            TargetNodeId = targetNodeId
                            TargetPortId = stepInput.Id
                            SourceReference = Some(source.Trim())
                        }

                        tryAddEdge edges edge |> ignore
            | None -> ()

    for output in workflow.Outputs do
        match output.OutputSource with
        | Some outputSource ->
            for source in outputSource.AsValues() do
                let parsed = parseSourceReference source

                match parsed.StepId with
                | Some sourceStepId ->
                    let sourceNodeId = stepNodeId sourceStepId
                    let targetNodeId = WorkflowOutputSinkNodeId

                    let edge = {
                        Id = createEdgeId sourceNodeId parsed.PortId targetNodeId output.Name
                        Kind = StepToOutput
                        SourceNodeId = sourceNodeId
                        SourcePortId = parsed.PortId
                        TargetNodeId = targetNodeId
                        TargetPortId = output.Name
                        SourceReference = Some(source.Trim())
                    }

                    tryAddEdge edges edge |> ignore
                | None ->
                    let sourceNodeId = WorkflowInputSourceNodeId
                    let targetNodeId = WorkflowOutputSinkNodeId

                    let edge = {
                        Id = createEdgeId sourceNodeId parsed.PortId targetNodeId output.Name
                        Kind = InputToOutput
                        SourceNodeId = sourceNodeId
                        SourcePortId = parsed.PortId
                        TargetNodeId = targetNodeId
                        TargetPortId = output.Name
                        SourceReference = Some(source.Trim())
                    }

                    tryAddEdge edges edge |> ignore
        | None -> ()

    { Nodes = nodes; Edges = edges }

let applyConnections (graph: WorkflowCanvasGraph) (workflow: CWLWorkflowDescription) =
    let edgeList = graph.Edges |> Seq.toList

    for step in workflow.Steps do
        for index = 0 to step.In.Count - 1 do
            let stepInput = step.In.[index]
            let targetNodeId = stepNodeId step.Id

            let matchingEdges =
                edgeList
                |> List.filter (fun edge ->
                    edge.TargetNodeId = targetNodeId
                    && edge.TargetPortId = stepInput.Id
                    && (edge.Kind = InputToStep || edge.Kind = StepToStep)
                )

            let newSources =
                matchingEdges
                |> List.map edgeSourceReference
                |> List.filter (String.IsNullOrWhiteSpace >> not)
                |> ResizeArray

            stepInput.Source <- if newSources.Count = 0 then None else Some newSources

            step.In.[index] <- stepInput

    for output in workflow.Outputs do
        let targetNodeId = WorkflowOutputSinkNodeId

        let matchingEdges =
            edgeList
            |> List.filter (fun edge ->
                edge.TargetNodeId = targetNodeId
                && edge.TargetPortId = output.Name
                && (edge.Kind = StepToOutput || edge.Kind = InputToOutput)
            )

        let sourceReferences =
            matchingEdges
            |> List.map edgeSourceReference
            |> List.filter (String.IsNullOrWhiteSpace >> not)
            |> ResizeArray

        output.OutputSource <-
            if sourceReferences.Count = 0 then
                None
            elif sourceReferences.Count = 1 then
                Some(OutputSource.Single sourceReferences.[0])
            else
                Some(OutputSource.Multiple sourceReferences)

        match matchingEdges |> List.tryHead |> Option.bind (tryResolveEdgeSourceType workflow) with
        | Some inferredType -> output.Type_ <- Some inferredType
        | None -> ()
