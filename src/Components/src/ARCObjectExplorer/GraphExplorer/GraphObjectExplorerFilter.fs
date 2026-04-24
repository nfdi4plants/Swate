namespace Swate.Components.ARCObjectExplorer.GraphExplorer

open Swate.Components
open Swate.Components.Shared
open Swate.Components.ARCObjectExplorer.GraphExplorer.Model

module GraphObjectExplorerFilter =

    let private datasetKinds =
        Set.ofList [
            GraphExplorerNodeKind.Study
            GraphExplorerNodeKind.Assay
            GraphExplorerNodeKind.Workflow
            GraphExplorerNodeKind.Run
        ]

    let private datasetSemanticLabels =
        datasetKinds
        |> Set.toList
        |> List.map GraphExplorerNodeKind.label
        |> Set.ofList

    let private semanticLabels =
        Set.ofList [
            yield! datasetSemanticLabels
            "Protocols"
            "FormalParameters"
            "Processes"
            "Materials"
            "Data"
        ]

    let private datasetParentLabel = "Datasets"

    let private datasetChildLabels =
        datasetKinds
        |> Set.toList
        |> List.map GraphExplorerNodeKind.label

    let private tryFindSelectionIndexByLabel
        (kindFilterOptions: SelectItem<string>[])
        (targetLabel: string)
        =
        kindFilterOptions
        |> Array.tryFindIndex (fun option -> option.item = targetLabel)

    let private tryFindSelectionIndicesByLabels
        (kindFilterOptions: SelectItem<string>[])
        (targetLabels: string list)
        =
        let indices =
            targetLabels
            |> List.choose (tryFindSelectionIndexByLabel kindFilterOptions)

        if indices.Length = targetLabels.Length then
            Some(indices |> Set.ofList)
        else
            None

    let syncDatasetKindSelection
        (kindFilterOptions: SelectItem<string>[])
        (previousSelectedKindIndices: Set<int>)
        (nextSelectedKindIndices: Set<int>)
        : Set<int> =
        match
            tryFindSelectionIndexByLabel kindFilterOptions datasetParentLabel,
            tryFindSelectionIndicesByLabels kindFilterOptions datasetChildLabels
        with
        | Some datasetIndex, Some datasetChildIndices ->
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

    let private isDatasetSubKind (graphKind: GraphExplorerNodeKind) =
        datasetKinds.Contains graphKind

    let private layerIdToLabel =
        Map.ofList [
            "graph:datasets", "Datasets"
            "graph:protocols", "Protocols"
            "graph:formal-parameters", "FormalParameters"
            "graph:processes", "Processes"
            "graph:materials", "Materials"
            "graph:Data", "Data"
        ]

    let private datasetKindFromNodeMeta (nodeId: string) (nodeMetaById: Map<string, GraphNodeMeta>) =
        nodeMetaById
        |> Map.tryFind nodeId
        |> Option.bind (fun meta ->
            match meta.Tag with
            | Some GraphNodeTag.Dataset when isDatasetSubKind meta.GraphKind ->
                Some(GraphExplorerNodeKind.label meta.GraphKind)
            | _ -> None)

    let private semanticLabelsForNode
        (datasetKindInScope: string option)
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
                    [
                        if isDatasetSubKind meta.GraphKind then
                            GraphExplorerNodeKind.label meta.GraphKind
                    ]
                | GraphNodeTag.Protocol -> [ "Protocols" ]
                | GraphNodeTag.FormalParameter -> [ "FormalParameters" ]
                | GraphNodeTag.Process -> [ "Processes" ]
                | GraphNodeTag.ProcessEndpoint GraphProcessEndpointValueType.Material -> [ "Materials" ]
                | GraphNodeTag.ProcessEndpoint GraphProcessEndpointValueType.Data -> [ "Data" ]
                | GraphNodeTag.PropertyValue GraphPropertyValueOwnerTag.Dataset ->
                    [
                        yield! datasetKindInScope |> Option.toList
                    ]
                | GraphNodeTag.PropertyValue GraphPropertyValueOwnerTag.Protocol -> [ "Protocols" ]
                | GraphNodeTag.PropertyValue GraphPropertyValueOwnerTag.Process -> [ "Processes" ]
                | GraphNodeTag.PropertyValue (GraphPropertyValueOwnerTag.ProcessEndpoint GraphProcessEndpointValueType.Material) -> [ "Materials" ]
                | GraphNodeTag.PropertyValue (GraphPropertyValueOwnerTag.ProcessEndpoint GraphProcessEndpointValueType.Data) -> [ "Data" ]))
        |> Option.defaultValue []

    let private tryGetNodeLineageById (nodeId: string) (nodes: ArcExplorerNode list) =
        let rec loop (ancestors: ArcExplorerNode list) (nodes: ArcExplorerNode list) =
            nodes
            |> List.tryPick (fun node ->
                if node.id = nodeId then
                    Some(node, List.rev ancestors)
                else
                    loop (node :: ancestors) node.children)

        loop [] nodes

    let private selectedPathContext
        (selectedSemanticKinds: Set<string>)
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

                let selectedNodeLabels =
                    semanticLabelsForNode
                        selectedDatasetKindInScope
                        selectedNode.id
                        nodeMetaById

                let isSelectedNodeVisibleByKinds =
                    selectedNodeLabels
                    |> List.exists selectedSemanticKinds.Contains

                let selectedPathNodeIds =
                    lineage
                    |> List.map _.id
                    |> Set.ofList

                selectedPathNodeIds, isSelectedNodeVisibleByKinds))

    let filterNodesBySemanticKinds
        (selectedSemanticKinds: Set<string>)
        (nodes: ArcExplorerNode list)
        (nodeMetaById: Map<string, GraphNodeMeta>)
        (selectedNodeId: string option)
        =
        let visibleLayerIds =
            layerIdToLabel
            |> Map.toList
            |> List.choose (fun (layerId, label) ->
                if selectedSemanticKinds.Contains label then
                    Some layerId
                else
                    None)
            |> Set.ofList

        let selectedPathNodeIds, preserveSelectedPath =
            match selectedPathContext selectedSemanticKinds nodes nodeMetaById selectedNodeId with
            | Some(pathNodeIds, true) -> pathNodeIds, true
            | _ -> Set.empty, false

        let rec loop
            (isTopLevel: bool)
            (datasetKindInScope: string option)
            (branchHasHiddenAssociation: bool)
            (node: ArcExplorerNode)
            =
            let datasetKindForBranch =
                datasetKindFromNodeMeta node.id nodeMetaById
                |> Option.orElse datasetKindInScope

            let nodeSemanticLabels =
                semanticLabelsForNode
                    datasetKindForBranch
                    node.id
                    nodeMetaById

            let nodeHasHiddenAssociation =
                nodeSemanticLabels
                |> List.exists (fun label ->
                    semanticLabels.Contains label
                    && not (selectedSemanticKinds.Contains label))

            let hiddenAssociationInBranch =
                branchHasHiddenAssociation || nodeHasHiddenAssociation

            let isSelectedPathNode =
                preserveSelectedPath
                && selectedPathNodeIds.Contains node.id

            let filteredChildren =
                node.children
                |> List.choose (loop false datasetKindForBranch hiddenAssociationInBranch)

            let hasVisibleChildren =
                filteredChildren |> List.isEmpty |> not

            let isVisibleSemanticNode =
                nodeSemanticLabels
                |> List.exists selectedSemanticKinds.Contains

            let isHiddenByAssociation =
                hiddenAssociationInBranch && not isSelectedPathNode

            let includeNode =
                if isTopLevel then
                    if node.id = "graph:all" then
                        true
                    elif layerIdToLabel.ContainsKey node.id then
                        visibleLayerIds.Contains node.id
                    else
                        false
                elif isHiddenByAssociation then
                    false
                else
                    match node.kind with
                    | ArcExplorerNodeKind.Arc
                    | ArcExplorerNodeKind.Group -> hasVisibleChildren || isSelectedPathNode
                    | _ -> isVisibleSemanticNode || hasVisibleChildren || isSelectedPathNode

            if includeNode then
                Some { node with children = filteredChildren }
            else
                None

        nodes |> List.choose (loop true None false)
