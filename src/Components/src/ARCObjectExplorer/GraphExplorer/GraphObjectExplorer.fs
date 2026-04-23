namespace Swate.Components.ARCObjectExplorer.GraphExplorer

open Fable.Core
open Feliz
open Swate.Components.Shared
open Swate.Components.ARCObjectExplorer
open Swate.Components.ARCObjectExplorer.Model
open Swate.Components.ARCObjectExplorer.GraphExplorer.Model

module GraphObjectExplorerFilter =

    let private semanticLabelFromTag =
        function
        | GraphNodeTag.Dataset -> Some "Datasets"
        | GraphNodeTag.Protocol -> Some "Protocols"
        | GraphNodeTag.FormalParameter -> Some "FormalParameters"
        | GraphNodeTag.Process -> Some "Processes"
        | GraphNodeTag.ProcessEndpoint GraphProcessEndpointValueType.Material -> Some "Materials"
        | GraphNodeTag.ProcessEndpoint GraphProcessEndpointValueType.Data -> Some "Data"
        | GraphNodeTag.PropertyValue GraphPropertyValueOwnerTag.Dataset -> Some "Datasets"
        | GraphNodeTag.PropertyValue GraphPropertyValueOwnerTag.Protocol -> Some "Protocols"
        | GraphNodeTag.PropertyValue GraphPropertyValueOwnerTag.Process -> Some "Processes"
        | GraphNodeTag.PropertyValue (GraphPropertyValueOwnerTag.ProcessEndpoint GraphProcessEndpointValueType.Material) -> Some "Materials"
        | GraphNodeTag.PropertyValue (GraphPropertyValueOwnerTag.ProcessEndpoint GraphProcessEndpointValueType.Data) -> Some "Data"

    let private layerIdToLabel =
        Map.ofList [
            "graph:datasets", "Datasets"
            "graph:protocols", "Protocols"
            "graph:formal-parameters", "FormalParameters"
            "graph:processes", "Processes"
            "graph:materials", "Materials"
            "graph:Data", "Data"
        ]

    let filterNodesBySemanticKinds
        (selectedSemanticKinds: Set<string>)
        (nodes: ArcExplorerNode list)
        (nodeMetaById: Map<string, GraphNodeMeta>)
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

        let rec loop (isTopLevel: bool) (node: ArcExplorerNode) =
            let filteredChildren =
                node.children |> List.choose (loop false)

            let hasVisibleChildren =
                filteredChildren |> List.isEmpty |> not

            let nodeSemanticLabel =
                nodeMetaById
                |> Map.tryFind node.id
                |> Option.bind (fun meta ->
                    meta.Tag
                    |> Option.bind semanticLabelFromTag)

            let isVisibleSemanticNode =
                nodeSemanticLabel
                |> Option.map selectedSemanticKinds.Contains
                |> Option.defaultValue false

            let includeNode =
                if isTopLevel then
                    if node.id = "graph:all" then
                        true
                    elif layerIdToLabel.ContainsKey node.id then
                        visibleLayerIds.Contains node.id
                    else
                        true
                else
                    match node.kind with
                    | ArcExplorerNodeKind.Arc
                    | ArcExplorerNodeKind.Group -> hasVisibleChildren
                    | _ -> isVisibleSemanticNode || hasVisibleChildren

            if includeNode then
                Some { node with children = filteredChildren }
            else
                None

        nodes |> List.choose (loop true)

[<Erase; Mangle(false)>]
type GraphObjectExplorer =

    [<ReactComponent>]
    static member private StoryExample() =
        let graphObjects = React.useMemo ((fun () -> GraphObjectFixture.fakeGraphObjects ()), [||])

        let nodes, nodeMetaById =
            React.useMemo ((fun () -> ArcExplorerNodes.toArcExplorerNodesWithMetaFromArcObjects graphObjects), [| box graphObjects |])

        let selection, setSelection = React.useState ArcSelection.empty

        let selectedKindIndices, setSelectedKindIndices =
            React.useState (KindFilter.defaultSelectedIndices KindFilter.graphObjectExplorerOptions)

        let visibleSemanticKinds =
            KindFilter.selectedLabels KindFilter.graphObjectExplorerOptions selectedKindIndices

        let filteredNodes =
            React.useMemo (
                (fun () ->
                    GraphObjectExplorerFilter.filterNodesBySemanticKinds
                        visibleSemanticKinds
                        nodes
                        nodeMetaById),
                [| box visibleSemanticKinds; box nodes; box nodeMetaById |]
            )

        let defaultArcKindIndices =
            React.useMemo (
                (fun () -> KindFilter.defaultSelectedIndices KindFilter.arcObjectExplorerOptions),
                [||]
            )

        let viewModel =
            create
                filteredNodes
                selection
                KindFilter.arcObjectExplorerOptions
                defaultArcKindIndices

        let explorerPaneItems =
            React.useMemo (
                (fun () -> viewModel.ExplorerItems),
                [| box viewModel.ExplorerItems |]
            )

        let treePaneItems =
            React.useMemo (
                (fun () ->
                    viewModel.ExplorerItems
                    |> GraphObjectExplorerTreeData.flattenNestedChildrenOnParentLevel
                    |> GraphObjectFixture.collapseExplorerItems),
                [| box viewModel.ExplorerItems |]
            )

        let setExplorerSelection (nodeId: string) (path: string option) =
            setSelection (ArcSelection.forExplorerNode nodeId path)

        let searchAction =
            ARCObjectWidget.SearchActionForExplorerItems(
                viewModel.SearchItems,
                (fun item ->
                    if item.Selectable then
                        setExplorerSelection item.Id item.Path),
                placeholder = "Search graph objects..."
            )

        let treePane =
            Swate.Components.FileExplorer.FileExplorer(
                initialItems = treePaneItems,
                ?selectedItemId = Some(selectedItemId viewModel),
                onItemClick =
                    (fun item ->
                        if item.Selectable then
                            setExplorerSelection item.Id item.Path),
                showBreadcrumbs = false,
                useDirectoryChevronToggle = true
            )

        let explorerPane =
            ARCObjectWidget.ExplorerContent(
                explorerPaneItems,
                ?selectedItemId = selectedItemId viewModel,
                onItemClick =
                    (fun item ->
                        if item.Selectable then
                            setExplorerSelection item.Id item.Path)
            )

        let detailsPane =
            GraphObjectDetails.GraphObjectDetails(
                selectedNode viewModel,
                selectedAncestors viewModel,
                nodeMetaById,
                (fun nodeId ->
                    match ARCExplorer.tryFindNodeById nodeId nodes with
                    | Some node -> setExplorerSelection node.id node.path
                    | None -> setSelection (ArcSelection.forExplorerNode nodeId None))
            )

        ARCObjectWidget.Main(
            navbar =
                ARCObjectWidget.Navbar(
                    selectedTitle viewModel,
                    selectedSubtitle viewModel,
                    KindFilter.graphObjectExplorerOptions,
                    selectedKindIndices,
                    setSelectedKindIndices,
                    rightActions = searchAction
                ),
            treePane = treePane,
            explorerPane = explorerPane,
            detailsPane = detailsPane
        )

    [<ReactComponent>]
    static member GraphObjectExplorer() =
        Html.div [
            prop.className "swt:min-h-screen swt:bg-base-200 swt:p-6"
            prop.children [ GraphObjectExplorer.StoryExample() ]
        ]

    [<ReactComponent>]
    static member Entry() =
        GraphObjectExplorer.GraphObjectExplorer()
