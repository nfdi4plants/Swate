namespace Swate.Components.Page.CwlEditor

open System
open Fable.Core
open Fable.Core.JsInterop
open Feliz
open ARCtrl.CWL
open Swate.Components.JsBindings
open Swate.Components.JsBindings.XyFlow
open Swate.Components.Page.CwlEditor.LayoutEngine
open Swate.Components.Shared.Cwl.WorkflowCanvasAdapter
open Swate.Components.Shared.Cwl.WorkflowLayout
open Swate.Components.Shared.Cwl.WorkflowMutations

type private EdgeContextMenu = { EdgeId: string; X: float; Y: float }

[<AutoOpen>]
module private WorkflowCanvasHelpers =

    [<Literal>]
    let addWorkflowOutputHandleId = "__add_workflow_output__"

    let trySelectStepByNodeId
        (workflow: CWLWorkflowDescription)
        (setActiveStepIndex: int option -> unit)
        (nodeId: string)
        =
        if nodeId.StartsWith("step:", StringComparison.Ordinal) then
            let stepId = nodeId.Substring("step:".Length)
            let stepIndex = workflow.Steps |> Seq.tryFindIndex (fun step -> step.Id = stepId)
            setActiveStepIndex stepIndex

    let portHandlePercent (index: int) (count: int) =
        if count <= 1 then
            50.0
        else
            ((float index + 1.0) / (float count + 1.0)) * 100.0

    let tryStringValue (value: string) =
        if isNullOrUndefined (box value) then None else Some value

    let stringArrayOrEmpty (values: string array) =
        if isNullOrUndefined (box values) then [||] else values

    let createHandle (handleType: string) (position: string) (id: string) (alongPercent: float option) =
        let sideOffset =
            match position with
            | "left" -> Some("left", "-7px")
            | "right" -> Some("right", "-7px")
            | "top" -> Some("top", "-7px")
            | "bottom" -> Some("bottom", "-7px")
            | _ -> None

        let baseStyle = [
            if alongPercent.IsSome then
                match position with
                | "left"
                | "right" ->
                    "top" ==> $"{alongPercent.Value}%%"
                    "transform" ==> "translateY(-50%)"
                | "top"
                | "bottom" ->
                    "left" ==> $"{alongPercent.Value}%%"
                    "transform" ==> "translateX(-50%)"
                | _ -> ()
            match sideOffset with
            | Some(key, value) -> key ==> value
            | None -> ()
        ]

        match baseStyle with
        | [] -> XyFlow.Handle(``type`` = handleType, position = position, id = id)
        | _ -> XyFlow.Handle(``type`` = handleType, position = position, id = id, style = createObj baseStyle)

    let inputNodeComponent (props: XyFlow.NodeProps) : ReactElement =
        let data = props.data
        let label = tryStringValue data.label |> Option.defaultValue "input"
        let outputPorts = stringArrayOrEmpty data.outputPorts

        Html.div [
            prop.className "swt-cwl-node swt-cwl-node-input"
            prop.children [
                Html.div [ prop.className "swt-cwl-node-title"; prop.text label ]
                Html.div [
                    prop.className "swt-cwl-node-port-list"
                    prop.children [
                        if outputPorts.Length = 0 then
                            Html.div [
                                prop.className "swt-cwl-node-port-row swt-cwl-node-port-row-output"
                                prop.children [
                                    Html.div [
                                        prop.className "swt-cwl-node-port swt-cwl-node-port-output"
                                        prop.text "No inputs yet"
                                    ]
                                ]
                            ]
                        else
                            for index, portId in outputPorts |> Array.indexed do
                                Html.div [
                                    prop.key $"input-output-port-{portId}-{index}"
                                    prop.className "swt-cwl-node-port-row swt-cwl-node-port-row-output"
                                    prop.children [
                                        Html.div [
                                            prop.className "swt-cwl-node-port swt-cwl-node-port-output"
                                            prop.text portId
                                        ]
                                        createHandle "source" "right" portId (Some 50.0)
                                    ]
                                ]
                    ]
                ]
            ]
        ]

    let outputNodeComponent (props: XyFlow.NodeProps) : ReactElement =
        let data = props.data
        let label = tryStringValue data.label |> Option.defaultValue "output"
        let inputPorts = stringArrayOrEmpty data.inputPorts

        Html.div [
            prop.className "swt-cwl-node swt-cwl-node-output"
            prop.children [
                Html.div [ prop.className "swt-cwl-node-title"; prop.text label ]
                Html.div [
                    prop.className "swt-cwl-node-port-list"
                    prop.children [
                        if inputPorts.Length = 0 then
                            Html.div [
                                prop.className "swt-cwl-node-port-row swt-cwl-node-port-row-input"
                                prop.children [
                                    Html.div [
                                        prop.className "swt-cwl-node-port swt-cwl-node-port-input"
                                        prop.text "No outputs yet"
                                    ]
                                ]
                            ]
                        else
                            for index, portId in inputPorts |> Array.indexed do
                                Html.div [
                                    prop.key $"output-input-port-{portId}-{index}"
                                    prop.className "swt-cwl-node-port-row swt-cwl-node-port-row-input"
                                    prop.children [
                                        createHandle "target" "left" portId (Some 50.0)
                                        Html.div [
                                            prop.className "swt-cwl-node-port swt-cwl-node-port-input"
                                            prop.text portId
                                        ]
                                    ]
                                ]
                        Html.div [
                            prop.className "swt-cwl-node-port-row swt-cwl-node-port-row-input swt-cwl-node-port-row-add"
                            prop.children [
                                createHandle "target" "left" addWorkflowOutputHandleId (Some 50.0)
                                Html.div [
                                    prop.className "swt-cwl-node-port swt-cwl-node-port-input"
                                    prop.text "+ connect to add output"
                                ]
                            ]
                        ]
                    ]
                ]
            ]
        ]

    let stepNodeComponent (props: XyFlow.NodeProps) : ReactElement =
        let data = props.data
        let label = tryStringValue data.label |> Option.defaultValue "step"
        let inputPorts = stringArrayOrEmpty data.inputPorts
        let outputPorts = stringArrayOrEmpty data.outputPorts

        Html.div [
            prop.className "swt-cwl-node swt-cwl-node-step"
            prop.children [
                Html.div [ prop.className "swt-cwl-node-title"; prop.text label ]
                Html.div [
                    prop.className "swt-cwl-node-ports"
                    prop.children [
                        Html.div [
                            prop.className "swt-cwl-node-port-list"
                            prop.children [
                                for _, portId in inputPorts |> Array.indexed do
                                    Html.div [
                                        prop.key $"step-in-port-{portId}"
                                        prop.className "swt-cwl-node-port-row swt-cwl-node-port-row-input"
                                        prop.children [
                                            createHandle "target" "left" portId (Some 50.0)
                                            Html.div [
                                                prop.className "swt-cwl-node-port swt-cwl-node-port-input"
                                                prop.text portId
                                            ]
                                        ]
                                    ]
                            ]
                        ]
                        Html.div [
                            prop.className "swt-cwl-node-port-list"
                            prop.children [
                                for _, portId in outputPorts |> Array.indexed do
                                    Html.div [
                                        prop.key $"step-out-port-{portId}"
                                        prop.className "swt-cwl-node-port-row swt-cwl-node-port-row-output"
                                        prop.children [
                                            createHandle "source" "right" portId (Some 50.0)
                                            Html.div [
                                                prop.className "swt-cwl-node-port swt-cwl-node-port-output"
                                                prop.text portId
                                            ]
                                        ]
                                    ]
                            ]
                        ]
                    ]
                ]
            ]
        ]

    let nodeTypes =
        createObj [
            "workflowInput" ==> Func<XyFlow.NodeProps, ReactElement>(inputNodeComponent)
            "workflowStep" ==> Func<XyFlow.NodeProps, ReactElement>(stepNodeComponent)
            "workflowOutput" ==> Func<XyFlow.NodeProps, ReactElement>(outputNodeComponent)
        ]

    let createFlowNode
        (id: string)
        (nodeType: string)
        (x: float)
        (y: float)
        (label: string)
        (inputPorts: string array)
        (outputPorts: string array)
        =
        createObj [
            "id" ==> id
            "type" ==> nodeType
            "position" ==> createObj [ "x" ==> x; "y" ==> y ]
            "draggable" ==> true
            "data"
            ==> createObj [
                "label" ==> label
                "inputPorts" ==> inputPorts
                "outputPorts" ==> outputPorts
            ]
        ]

    let layoutNodeSize (node: CanvasNode) =
        match node.Kind with
        | WorkflowInputNode -> 230.0, 120.0
        | WorkflowStepNode -> 260.0, 150.0
        | WorkflowOutputNode -> 230.0, 120.0

    let deletedEdgeIds (removedEdgesObj: XyFlow.Edge array) =
        if isNullOrUndefined (box removedEdgesObj) then
            [||]
        else
            removedEdgesObj |> Array.choose (fun edge -> tryStringValue edge.id)

    let tryNodePosition (node: XyFlow.Node) =
        if isNullOrUndefined (box node.position) then
            None
        else
            Some(node.position.x, node.position.y)

[<Erase; Mangle(false)>]
type WorkflowCanvas =

    [<ReactComponent>]
    static member WorkflowCanvas
        (
            version: int,
            editorSessionId: int,
            workflow: CWLWorkflowDescription,
            workflowFilePath: string option,
            activeStepIndex: int option,
            setActiveStepIndex: int option -> unit,
            clearActiveOutputSelection: unit -> unit,
            commitMutation: (unit -> unit) -> unit
        ) : ReactElement =
        ignore activeStepIndex

        let nodePositionOverrides, setNodePositionOverrides =
            React.useState<Map<string, float * float>> (Map.empty)

        let isConnectionsExpanded, setConnectionsExpanded = React.useState (false)

        let edgeContextMenu, setEdgeContextMenu =
            React.useState<EdgeContextMenu option> (None)

        React.useEffect (
            (fun () ->
                setNodePositionOverrides Map.empty
                setConnectionsExpanded false
                setEdgeContextMenu None
            ),
            [| box editorSessionId |]
        )

        let setActiveNodePositionOverride (nodeId: string) (x: float) (y: float) =
            setNodePositionOverrides (nodePositionOverrides.Add(nodeId, (x, y)))

        let resetActiveLayout () = setNodePositionOverrides Map.empty

        let applyAutoLayout () =
            let graphForLayout = toCanvasGraph workflow

            let layoutNodes =
                graphForLayout.Nodes
                |> Seq.map (fun node ->
                    let width, height = layoutNodeSize node

                    {
                        Id = node.Id
                        Width = width
                        Height = height
                    }
                )

            let layoutEdges =
                graphForLayout.Edges
                |> Seq.map (fun edge -> {
                    SourceId = edge.SourceNodeId
                    TargetId = edge.TargetNodeId
                })

            let positions = dagreLayoutEngine.Layout layoutNodes layoutEdges LeftToRight
            setNodePositionOverrides positions

        let resolveNodePosition (nodeId: string) (fallbackX: float) (fallbackY: float) =
            nodePositionOverrides
            |> Map.tryFind nodeId
            |> Option.defaultValue (fallbackX, fallbackY)

        let workflowGraphReadModel =
            React.useMemo (
                (fun () -> buildWorkflowGraphReadModel workflow workflowFilePath None),
                [| box version; box workflow; box workflowFilePath |]
            )

        let graph, nodeLabelById, sourcePortLookup, targetPortLookup =
            React.useMemo (
                (fun () ->
                    let graph = toCanvasGraph workflow

                    let sourcePortLookup =
                        sourcePorts workflow
                        |> Seq.groupBy (fun port -> port.NodeId)
                        |> Seq.map (fun (nodeId, ports) ->
                            nodeId, (ports |> Seq.map (fun port -> port.PortId) |> Array.ofSeq)
                        )
                        |> Map.ofSeq

                    let targetPortLookup =
                        targetPorts workflow
                        |> Seq.groupBy (fun port -> port.NodeId)
                        |> Seq.map (fun (nodeId, ports) ->
                            nodeId, (ports |> Seq.map (fun port -> port.PortId) |> Array.ofSeq)
                        )
                        |> Map.ofSeq

                    let nodeLabelById =
                        graph.Nodes |> Seq.map (fun node -> node.Id, node.Label) |> Map.ofSeq

                    graph, nodeLabelById, sourcePortLookup, targetPortLookup
                ),
                [| box version; box workflow |]
            )

        let flowNodes =
            React.useMemo (
                (fun () ->
                    let inputColumnX = 40.0
                    let stepColumnX = 500.0
                    let outputColumnX = 980.0
                    let inputSinkY = 160.0
                    let stepRowStartY = 60.0
                    let outputSinkY = 160.0
                    let stepRowSpacing = 230.0

                    let inputNodes =
                        let outputPorts =
                            sourcePortLookup
                            |> Map.tryFind WorkflowInputSourceNodeId
                            |> Option.defaultValue [||]

                        let x, y = resolveNodePosition WorkflowInputSourceNodeId inputColumnX inputSinkY

                        [|
                            createFlowNode
                                WorkflowInputSourceNodeId
                                "workflowInput"
                                x
                                y
                                "Workflow inputs"
                                [||]
                                outputPorts
                        |]

                    let stepNodes =
                        workflow.Steps
                        |> Seq.mapi (fun index step ->
                            let nodeId = $"step:{step.Id}"

                            let inputPorts = targetPortLookup |> Map.tryFind nodeId |> Option.defaultValue [||]

                            let outputPorts = sourcePortLookup |> Map.tryFind nodeId |> Option.defaultValue [||]

                            let x, y =
                                resolveNodePosition nodeId stepColumnX (stepRowStartY + float index * stepRowSpacing)

                            createFlowNode nodeId "workflowStep" x y step.Id inputPorts outputPorts
                        )

                    let outputNodes =
                        let inputPorts =
                            targetPortLookup
                            |> Map.tryFind WorkflowOutputSinkNodeId
                            |> Option.defaultValue [||]

                        let x, y = resolveNodePosition WorkflowOutputSinkNodeId outputColumnX outputSinkY

                        [|
                            createFlowNode WorkflowOutputSinkNodeId "workflowOutput" x y "Workflow outputs" inputPorts [||]
                        |]

                    Seq.concat [ inputNodes :> seq<_>; stepNodes; outputNodes :> seq<_> ]
                    |> Array.ofSeq
                ),
                [|
                    box workflow
                    box nodePositionOverrides
                    box sourcePortLookup
                    box targetPortLookup
                |]
            )

        let flowEdges =
            React.useMemo (
                (fun () ->
                    graph.Edges
                    |> Seq.map (fun edge ->
                        let sourceLabel =
                            nodeLabelById
                            |> Map.tryFind edge.SourceNodeId
                            |> Option.defaultValue edge.SourceNodeId

                        let targetLabel =
                            nodeLabelById
                            |> Map.tryFind edge.TargetNodeId
                            |> Option.defaultValue edge.TargetNodeId

                        createObj [
                            "id" ==> edge.Id
                            "source" ==> edge.SourceNodeId
                            "target" ==> edge.TargetNodeId
                            "sourceHandle" ==> edge.SourcePortId
                            "targetHandle" ==> edge.TargetPortId
                            "deletable" ==> true
                            "label"
                            ==> $"{sourceLabel}/{edge.SourcePortId} -> {targetLabel}/{edge.TargetPortId}"
                            "labelShowBg" ==> false
                            "labelStyle"
                            ==> createObj [ "opacity" ==> 0; "transition" ==> "opacity 120ms ease" ]
                        ]
                    )
                    |> Array.ofSeq
                ),
                [| box graph; box nodeLabelById |]
            )

        let onNodeClick =
            Func<obj, XyFlow.Node, unit>(fun _ node ->
                setEdgeContextMenu None

                match tryStringValue node.id with
                | Some nodeId -> trySelectStepByNodeId workflow setActiveStepIndex nodeId
                | None -> ()
            )

        let onConnect =
            Func<XyFlow.Connection, unit>(fun connection ->
                setEdgeContextMenu None

                match connection.sourceHandle, connection.targetHandle with
                | Some sourcePortId, Some targetPortId ->
                    let sourceNodeId = tryStringValue connection.source
                    let targetNodeId = tryStringValue connection.target

                    match sourceNodeId, targetNodeId with
                    | Some sourceNodeId, Some targetNodeId ->
                        if
                            targetNodeId = WorkflowOutputSinkNodeId
                            && targetPortId = addWorkflowOutputHandleId
                        then
                            commitMutation (fun () ->
                                let newOutputIndex = addWorkflowOutput workflow
                                let newOutputPortId = workflow.Outputs.[newOutputIndex].Name
                                let nextGraph = toCanvasGraph workflow

                                if
                                    addConnection
                                        nextGraph
                                        sourceNodeId
                                        sourcePortId
                                        WorkflowOutputSinkNodeId
                                        newOutputPortId
                                then
                                    applyConnections nextGraph workflow
                            )
                        else
                            let nextGraph = toCanvasGraph workflow

                            if addConnection nextGraph sourceNodeId sourcePortId targetNodeId targetPortId then
                                commitMutation (fun () -> applyConnections nextGraph workflow)
                    | _ -> ()
                | _ -> ()
            )

        let removeDisconnectedWorkflowOutputs (workflow: CWLWorkflowDescription) (graph: WorkflowCanvasGraph) =
            let initialCount = workflow.Outputs.Count

            let connectedOutputPorts =
                graph.Edges
                |> Seq.filter (fun edge ->
                    edge.TargetNodeId = WorkflowOutputSinkNodeId
                    && (edge.Kind = StepToOutput || edge.Kind = InputToOutput)
                )
                |> Seq.map (fun edge -> edge.TargetPortId)
                |> Set.ofSeq

            for index = workflow.Outputs.Count - 1 downto 0 do
                let outputName = workflow.Outputs.[index].Name

                if connectedOutputPorts.Contains outputName |> not then
                    workflow.Outputs.RemoveAt(index)

            workflow.Outputs.Count < initialCount

        let removeEdgeById (edgeId: string) =
            let nextGraph = toCanvasGraph workflow

            if removeConnection nextGraph edgeId then
                commitMutation (fun () ->
                    applyConnections nextGraph workflow
                    let removedOutputs = removeDisconnectedWorkflowOutputs workflow nextGraph

                    if removedOutputs then
                        clearActiveOutputSelection ()
                )

        let onEdgesDelete =
            Func<XyFlow.Edge array, unit>(fun removedEdgesObj ->
                setEdgeContextMenu None
                let removedIds = deletedEdgeIds removedEdgesObj

                if removedIds.Length > 0 then
                    let nextGraph = toCanvasGraph workflow

                    let removedAny =
                        removedIds
                        |> Array.fold (fun anyRemoved edgeId -> removeConnection nextGraph edgeId || anyRemoved) false

                    if removedAny then
                        commitMutation (fun () ->
                            applyConnections nextGraph workflow
                            let removedOutputs = removeDisconnectedWorkflowOutputs workflow nextGraph

                            if removedOutputs then
                                clearActiveOutputSelection ()
                        )
            )

        let onPaneClick = Func<obj, unit>(fun _ -> setEdgeContextMenu None)

        let onEdgeContextMenu =
            Func<XyFlow.EdgeContextMenuEvent, XyFlow.Edge, unit>(fun eventObj edgeObj ->
                eventObj.preventDefault ()

                match tryStringValue edgeObj.id with
                | Some edgeId ->
                    setEdgeContextMenu (
                        Some {
                            EdgeId = edgeId
                            X = eventObj.clientX
                            Y = eventObj.clientY
                        }
                    )
                | None -> ()
            )

        let onNodeDragStop =
            Func<obj, XyFlow.Node, unit>(fun _ nodeObj ->
                match tryStringValue nodeObj.id, tryNodePosition nodeObj with
                | Some nodeId, Some(x, y) -> setActiveNodePositionOverride nodeId x y
                | _ -> ()
            )

        let reactFlowElement =
            let reactFlowElementKey = $"workflow-canvas:{editorSessionId}"

            XyFlow.ReactFlow(
                key = reactFlowElementKey,
                id = reactFlowElementKey,
                nodes = flowNodes,
                edges = flowEdges,
                nodeTypes = nodeTypes,
                fitView = true,
                fitViewOptions = {| padding = 0.22 |},
                minZoom = 0.05,
                maxZoom = 2.0,
                nodesDraggable = true,
                nodesConnectable = true,
                elementsSelectable = true,
                onNodeClick = onNodeClick,
                onNodeDragStop = onNodeDragStop,
                onConnect = onConnect,
                onEdgesDelete = onEdgesDelete,
                onEdgeContextMenu = onEdgeContextMenu,
                onPaneClick = onPaneClick,
                deleteKeyCode = "Delete",
                children = React.Fragment [ XyFlow.MiniMap(); XyFlow.Controls(); XyFlow.Background() ]
            )

        let edgeSourceGroupOrder (sourceNodeId: string) =
            if sourceNodeId.StartsWith("in:", StringComparison.Ordinal) then
                0
            elif sourceNodeId.StartsWith("step:", StringComparison.Ordinal) then
                let stepId = sourceNodeId.Substring("step:".Length)

                match workflow.Steps |> Seq.tryFindIndex (fun step -> step.Id = stepId) with
                | Some index -> index + 1
                | None -> Int32.MaxValue - 1
            else
                Int32.MaxValue

        let orderedEdges =
            graph.Edges
            |> Seq.sortBy (fun edge ->
                let sourceLabel =
                    nodeLabelById
                    |> Map.tryFind edge.SourceNodeId
                    |> Option.defaultValue edge.SourceNodeId

                let targetLabel =
                    nodeLabelById
                    |> Map.tryFind edge.TargetNodeId
                    |> Option.defaultValue edge.TargetNodeId

                edgeSourceGroupOrder edge.SourceNodeId, sourceLabel, edge.SourcePortId, targetLabel, edge.TargetPortId
            )
            |> Seq.toList

        let orderedDiagnostics =
            workflowGraphReadModel.Diagnostics
            |> Seq.sortBy (fun issue -> issue.Kind, issue.Message)
            |> Seq.toList

        Html.section [
            prop.testId "cwl-workflow-canvas"
            prop.className "swt:card swt:bg-base-200 swt:p-4"
            prop.children [
                Html.div [
                    prop.className "swt:flex swt:items-center swt:justify-between swt:gap-2"
                    prop.children [
                        Html.h3 [
                            prop.className "swt:font-semibold swt:text-base-content"
                            prop.text "Workflow Canvas"
                        ]
                        Html.div [
                            prop.className "swt:flex swt:gap-2"
                            prop.children [
                                Html.button [
                                    prop.testId "cwl-workflow-canvas-auto-layout"
                                    prop.className "swt:btn swt:btn-sm swt:btn-ghost swt:shrink-0"
                                    prop.text "Auto layout"
                                    prop.onClick (fun _ ->
                                        applyAutoLayout ()
                                        setEdgeContextMenu None
                                    )
                                ]
                                Html.button [
                                    prop.testId "cwl-workflow-canvas-reset-layout"
                                    prop.className "swt:btn swt:btn-sm swt:btn-ghost swt:shrink-0"
                                    prop.text "Reset layout"
                                    prop.onClick (fun _ ->
                                        resetActiveLayout ()
                                        setEdgeContextMenu None
                                    )
                                ]
                            ]
                        ]
                    ]
                ]
                Html.p [
                    prop.className "swt:text-base-content/60 swt:italic swt:p-4 swt:text-center"
                    prop.text
                        "Drag from source handles to target handles to connect ports. Use '+ connect to add output' to create workflow outputs directly from canvas. Right-click an edge to disconnect."
                ]
                Html.p [
                    prop.className "swt:text-base-content/60 swt:italic swt:p-4 swt:text-center"
                    prop.text
                        $"WorkflowGraph read model: {workflowGraphReadModel.NodeCount} nodes, {workflowGraphReadModel.EdgeCount} edges."
                ]
                Html.div [
                    prop.className "swt:flex swt:flex-col swt:gap-2"
                    prop.children [
                        Html.div [
                            prop.className "swt:flex swt:items-center swt:justify-between swt:gap-2"
                            prop.children [
                                Html.h4 [
                                    prop.className "swt:font-semibold swt:text-base-content"
                                    prop.text "WorkflowGraph diagnostics"
                                ]
                            ]
                        ]
                        if orderedDiagnostics.Length = 0 then
                            Html.p [
                                prop.className "swt:text-base-content/60 swt:italic swt:p-4 swt:text-center"
                                prop.text "No graph diagnostics detected."
                            ]
                        else
                            Html.ul [
                                prop.testId "cwl-workflow-canvas-diagnostics"
                                prop.className "swt:menu swt:bg-base-100 swt:rounded-box"
                                prop.children [
                                    for issue in orderedDiagnostics do
                                        let scopeText = issue.Scope |> Option.defaultValue "-"
                                        let referenceText = issue.Reference |> Option.defaultValue "-"

                                        Html.li [
                                            prop.key $"{issue.Kind}:{issue.Message}:{scopeText}:{referenceText}"
                                            prop.className "swt:min-w-0"
                                            prop.text
                                                $"{issue.Kind}: {issue.Message} (scope: {scopeText}; reference: {referenceText})"
                                        ]
                                ]
                            ]
                    ]
                ]
                Html.div [
                    prop.className "swt-cwl-workflow-canvas-flow"
                    prop.children [ reactFlowElement ]
                ]
                Html.div [
                    prop.className "swt:flex swt:flex-col swt:gap-2"
                    prop.children [
                        Html.div [
                            prop.className "swt:flex swt:items-center swt:justify-between swt:gap-2"
                            prop.children [
                                Html.h4 [
                                    prop.className "swt:font-semibold swt:text-base-content"
                                    prop.text "Current connections"
                                ]
                                Html.button [
                                    prop.testId "cwl-workflow-canvas-connections-toggle"
                                    prop.className "swt:btn swt:btn-sm swt:btn-ghost swt:shrink-0"
                                    prop.text (if isConnectionsExpanded then "Collapse" else "Expand")
                                    prop.onClick (fun _ -> setConnectionsExpanded (not isConnectionsExpanded))
                                ]
                            ]
                        ]
                        if isConnectionsExpanded then
                            if orderedEdges.Length = 0 then
                                Html.p [
                                    prop.className "swt:text-base-content/60 swt:italic swt:p-4 swt:text-center"
                                    prop.text "No connections defined yet."
                                ]
                            else
                                Html.ul [
                                    prop.className "swt:menu swt:bg-base-100 swt:rounded-box"
                                    prop.children [
                                        for edge in orderedEdges do
                                            let sourceLabel =
                                                nodeLabelById
                                                |> Map.tryFind edge.SourceNodeId
                                                |> Option.defaultValue edge.SourceNodeId

                                            let targetLabel =
                                                nodeLabelById
                                                |> Map.tryFind edge.TargetNodeId
                                                |> Option.defaultValue edge.TargetNodeId

                                            Html.li [
                                                prop.key edge.Id
                                                prop.className
                                                    "swt:flex swt:items-center swt:justify-between swt:gap-2 swt:w-full swt:min-w-0"
                                                prop.children [
                                                    Html.span [
                                                        prop.className "swt:shrink swt:min-w-0"
                                                        prop.text
                                                            $"{sourceLabel}/{edge.SourcePortId} -> {targetLabel}/{edge.TargetPortId}"
                                                    ]
                                                    Html.button [
                                                        prop.testId $"cwl-workflow-canvas-disconnect-{edge.Id}"
                                                        prop.className "swt:btn swt:btn-sm swt:btn-ghost swt:shrink-0"
                                                        prop.text "Disconnect"
                                                        prop.onClick (fun _ ->
                                                            removeEdgeById edge.Id
                                                            setEdgeContextMenu None
                                                        )
                                                    ]
                                                ]
                                            ]
                                    ]
                                ]
                        else
                            Html.p [
                                prop.className "swt:text-base-content/60 swt:italic swt:p-4 swt:text-center"
                                prop.text "Connection list is collapsed."
                            ]
                    ]
                ]
                match edgeContextMenu with
                | Some menu ->
                    Html.div [
                        prop.testId "cwl-workflow-canvas-context-menu"
                        prop.className
                            "swt-cwl-canvas-context-menu swt:flex swt:flex-col swt:gap-2 swt:bg-base-100 swt:p-2 swt:rounded-box swt:shadow-lg"
                        prop.style [
                            style.custom ("position", "fixed")
                            style.custom ("left", $"{menu.X}px")
                            style.custom ("top", $"{menu.Y}px")
                            style.zIndex 50
                        ]
                        prop.onClick (fun e -> e.stopPropagation ())
                        prop.onContextMenu (fun e -> e.preventDefault ())
                        prop.children [
                            Html.button [
                                prop.testId "cwl-workflow-canvas-delete-connection"
                                prop.className "swt:btn swt:btn-sm swt:btn-error swt:shrink-0"
                                prop.text "Delete connection"
                                prop.onClick (fun _ ->
                                    removeEdgeById menu.EdgeId
                                    setEdgeContextMenu None
                                )
                            ]
                            Html.button [
                                prop.testId "cwl-workflow-canvas-cancel-context-menu"
                                prop.className "swt:btn swt:btn-sm swt:btn-ghost swt:shrink-0"
                                prop.text "Cancel"
                                prop.onClick (fun _ -> setEdgeContextMenu None)
                            ]
                        ]
                    ]
                | None -> Html.none
            ]
        ]
