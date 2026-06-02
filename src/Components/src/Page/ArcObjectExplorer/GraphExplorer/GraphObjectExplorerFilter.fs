module Swate.Components.Page.ARCObjectExplorer.GraphExplorer.GraphObjectExplorerFilter

open Swate.Components
open Swate.Components.Primitive.Select.Types
open Swate.Components.Shared
open Swate.Components.Page.ARCObjectExplorer.Model
open Swate.Components.Page.ARCObjectExplorer.GraphExplorer.Model


let private whitelistedGraphKinds =
    Set.ofList [
        GraphExplorerNodeKind.Arc
        GraphExplorerNodeKind.Group
        GraphExplorerNodeKind.Study
        GraphExplorerNodeKind.Assay
        GraphExplorerNodeKind.Workflow
        GraphExplorerNodeKind.Run
        GraphExplorerNodeKind.Protocol
        GraphExplorerNodeKind.FormalParameter
        GraphExplorerNodeKind.Process
        GraphExplorerNodeKind.Material
        GraphExplorerNodeKind.Data
    ]

let private datasetSemanticKindFromGraphKind
    (graphKind: GraphExplorerNodeKind)
    : GraphSemanticKind option
    =
    match graphKind with
    | GraphExplorerNodeKind.Study -> Some GraphSemanticKind.Study
    | GraphExplorerNodeKind.Assay -> Some GraphSemanticKind.Assay
    | GraphExplorerNodeKind.Workflow -> Some GraphSemanticKind.Workflow
    | GraphExplorerNodeKind.Run -> Some GraphSemanticKind.Run
    | _ -> None

let private toSelectedSemanticKindSet (selectedSemanticKinds: Set<string>) =
    selectedSemanticKinds
    |> Seq.choose GraphSemanticKind.tryParseLabel
    |> Set.ofSeq

let private tryResolveDatasetSelectionIndices
    (kindFilterOptions: SelectItem<string>[])
    : (int * Set<int>) option
    =
    let selectionIndices =
        kindFilterOptions
        |> Array.mapi (fun index option ->
            option.item
            |> GraphSemanticKind.tryParseLabel
            |> Option.map (fun semanticKind -> semanticKind, index))
        |> Array.choose id
        |> Map.ofArray

    match selectionIndices |> Map.tryFind GraphSemanticKind.datasetParent with
    | Some datasetIndex ->
        let datasetChildIndices =
            GraphSemanticKind.datasetChildren
            |> List.choose (fun semanticKind ->
                selectionIndices
                |> Map.tryFind semanticKind)

        if datasetChildIndices.Length = GraphSemanticKind.datasetChildren.Length then
            Some(datasetIndex, datasetChildIndices |> Set.ofList)
        else
            None
    | None ->
        None

let syncDatasetKindSelection
    (kindFilterOptions: SelectItem<string>[])
    (previousSelectedKindIndices: Set<int>)
    (nextSelectedKindIndices: Set<int>)
    : Set<int> =
    match tryResolveDatasetSelectionIndices kindFilterOptions with
    | Some(datasetIndex, datasetChildIndices) ->
        let wasDatasetSelected = previousSelectedKindIndices.Contains datasetIndex
        let isDatasetSelected = nextSelectedKindIndices.Contains datasetIndex

        if wasDatasetSelected = isDatasetSelected then
            nextSelectedKindIndices
        elif isDatasetSelected then
            Set.union nextSelectedKindIndices datasetChildIndices
        else
            Set.difference nextSelectedKindIndices datasetChildIndices
    | _ ->
        nextSelectedKindIndices

let private layerIdToSemanticKind =
    Map.ofList [
        "graph:datasets", GraphSemanticKind.Datasets
        "graph:protocols", GraphSemanticKind.Protocols
        "graph:formal-parameters", GraphSemanticKind.FormalParameters
        "graph:processes", GraphSemanticKind.Processes
        "graph:materials", GraphSemanticKind.Materials
        "graph:Data", GraphSemanticKind.Data
    ]

let private semanticKindForProcessEndpointValueType =
    function
    | GraphProcessEndpointValueType.Material -> GraphSemanticKind.Materials
    | GraphProcessEndpointValueType.Data -> GraphSemanticKind.Data

let private semanticKindForPropertyValueOwnerTag
    (datasetKindInScope: GraphSemanticKind option)
    (ownerTag: GraphPropertyValueOwnerTag)
    =
    match ownerTag with
    | GraphPropertyValueOwnerTag.Dataset -> datasetKindInScope
    | GraphPropertyValueOwnerTag.Protocol -> Some GraphSemanticKind.Protocols
    | GraphPropertyValueOwnerTag.Process -> Some GraphSemanticKind.Processes
    | GraphPropertyValueOwnerTag.ProcessEndpoint valueType ->
        Some(semanticKindForProcessEndpointValueType valueType)

let private datasetKindFromNodeMeta
    (nodeId: string)
    (nodeMetaById: Map<string, GraphNodeMeta>)
    : GraphSemanticKind option
    =
    nodeMetaById
    |> Map.tryFind nodeId
    |> Option.bind (fun meta ->
        match meta.Tag with
        | Some GraphNodeTag.Dataset ->
            datasetSemanticKindFromGraphKind meta.GraphKind
        | _ ->
            None)

let private semanticKindsForNode
    (datasetKindInScope: GraphSemanticKind option)
    (nodeId: string)
    (nodeMetaById: Map<string, GraphNodeMeta>)
    =
    nodeMetaById
    |> Map.tryFind nodeId
    |> Option.bind (fun meta ->
        meta.Tag
        |> Option.map (fun tag ->
            match tag with
            | GraphNodeTag.Dataset ->
                datasetSemanticKindFromGraphKind meta.GraphKind
                |> Option.toList
            | GraphNodeTag.Protocol -> [ GraphSemanticKind.Protocols ]
            | GraphNodeTag.FormalParameter -> [ GraphSemanticKind.FormalParameters ]
            | GraphNodeTag.Process -> [ GraphSemanticKind.Processes ]
            | GraphNodeTag.ProcessEndpoint valueType ->
                [ semanticKindForProcessEndpointValueType valueType ]
            | GraphNodeTag.PropertyValue ownerTag ->
                semanticKindForPropertyValueOwnerTag datasetKindInScope ownerTag
                |> Option.toList))
    |> Option.defaultValue []

let private selectedPathContext
    (selectedSemanticKinds: Set<GraphSemanticKind>)
    (nodes: ArcExplorerNode list)
    (nodeMetaById: Map<string, GraphNodeMeta>)
    (selectedNodeId: string option)
    =
    selectedNodeId
    |> Option.bind (fun nodeId ->
        tryGetNodeLineageById nodeId nodes
        |> Option.map (fun (selectedNode, ancestors) ->
            let lineage = ancestors @ [ selectedNode ]

            let selectedDatasetKindInScope =
                lineage
                |> List.fold (fun datasetKindInScope node ->
                    datasetKindFromNodeMeta node.id nodeMetaById
                    |> Option.orElse datasetKindInScope) None

            let selectedNodeSemanticKinds =
                semanticKindsForNode
                    selectedDatasetKindInScope
                    selectedNode.id
                    nodeMetaById

            let isSelectedNodeVisibleByKinds =
                selectedNodeSemanticKinds
                |> List.exists selectedSemanticKinds.Contains

            let selectedPathNodeIds =
                lineage
                |> List.map _.id
                |> Set.ofList

            selectedPathNodeIds, isSelectedNodeVisibleByKinds))

let private visibleLayerIdsForSelectedKinds
    (selectedSemanticKinds: Set<GraphSemanticKind>)
    =
    layerIdToSemanticKind
    |> Map.toList
    |> List.choose (fun (layerId, semanticKind) ->
        if selectedSemanticKinds.Contains semanticKind then
            Some layerId
        else
            None)
    |> Set.ofList

let private includeTopLevelNode
    (visibleLayerIds: Set<string>)
    (node: ArcExplorerNode)
    =
    if node.id = "graph:all" then
        true
    elif layerIdToSemanticKind.ContainsKey node.id then
        visibleLayerIds.Contains node.id
    else
        false

let private includeNonTopLevelNode
    (isHiddenByAssociation: bool)
    (isSelectedPathNode: bool)
    (hasVisibleChildren: bool)
    (isVisibleSemanticNode: bool)
    (node: ArcExplorerNode)
    =
    if isHiddenByAssociation then
        false
    else
        match node.kind with
        | ArcExplorerNodeKind.Arc
        | ArcExplorerNodeKind.Group -> hasVisibleChildren || isSelectedPathNode
        | _ -> isVisibleSemanticNode || hasVisibleChildren || isSelectedPathNode

let private graphKindForNode
    (nodeMetaById: Map<string, GraphNodeMeta>)
    (node: ArcExplorerNode)
    =
    nodeMetaById
    |> Map.tryFind node.id
    |> Option.map _.GraphKind
    |> Option.defaultValue (GraphExplorerNodeKind.ofArcExplorerNodeKind node.kind)

let private isWhitelistedGraphKind
    (nodeMetaById: Map<string, GraphNodeMeta>)
    (node: ArcExplorerNode)
    =
    graphKindForNode nodeMetaById node
    |> whitelistedGraphKinds.Contains

let filterNodesBySemanticKinds
    (selectedSemanticKinds: Set<string>)
    (nodes: ArcExplorerNode list)
    (nodeMetaById: Map<string, GraphNodeMeta>)
    (selectedNodeId: string option)
    =
    let selectedSemanticKindSet =
        toSelectedSemanticKindSet selectedSemanticKinds

    let visibleLayerIds =
        visibleLayerIdsForSelectedKinds selectedSemanticKindSet

    let selectedPathNodeIds, preserveSelectedPath =
        match selectedPathContext selectedSemanticKindSet nodes nodeMetaById selectedNodeId with
        | Some(pathNodeIds, true) -> pathNodeIds, true
        | _ -> Set.empty, false

    let rec loop
        (isTopLevel: bool)
        (datasetKindInScope: GraphSemanticKind option)
        (branchHasHiddenAssociation: bool)
        (node: ArcExplorerNode)
        =
        let datasetKindForBranch =
            datasetKindFromNodeMeta node.id nodeMetaById
            |> Option.orElse datasetKindInScope

        let nodeSemanticKinds =
            semanticKindsForNode
                datasetKindForBranch
                node.id
                nodeMetaById

        let nodeHasHiddenAssociation =
            nodeSemanticKinds
            |> List.exists (fun semanticKind ->
                GraphSemanticKind.branchPrunableKinds.Contains semanticKind
                && not (selectedSemanticKindSet.Contains semanticKind))

        let hiddenAssociationInBranch =
            branchHasHiddenAssociation || nodeHasHiddenAssociation

        let isSelectedPathNode =
            preserveSelectedPath
            && selectedPathNodeIds.Contains node.id

        let filteredChildren =
            node.children
            |> List.collect (loop false datasetKindForBranch hiddenAssociationInBranch)

        let hasVisibleChildren =
            filteredChildren |> List.isEmpty |> not

        let isVisibleSemanticNode =
            nodeSemanticKinds
            |> List.exists selectedSemanticKindSet.Contains

        let isHiddenByAssociation =
            hiddenAssociationInBranch && not isSelectedPathNode

        let includeNode =
            if isTopLevel then
                includeTopLevelNode visibleLayerIds node
            else
                includeNonTopLevelNode
                    isHiddenByAssociation
                    isSelectedPathNode
                    hasVisibleChildren
                    isVisibleSemanticNode
                    node

        if includeNode then
            let filteredNode = { node with children = filteredChildren }

            if isWhitelistedGraphKind nodeMetaById filteredNode then
                [ filteredNode ]
            else
                filteredChildren
        else
            []

    nodes |> List.collect (loop true None false)

