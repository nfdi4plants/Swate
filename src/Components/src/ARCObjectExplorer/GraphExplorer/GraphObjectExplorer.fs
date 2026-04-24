namespace Swate.Components.ARCObjectExplorer.GraphExplorer

open Fable.Core
open Feliz
open Swate.Components.Shared
open Swate.Components.ARCObjectExplorer
open Swate.Components.ARCObjectExplorer.Model
open Swate.Components.ARCObjectExplorer.GraphExplorer.Model
open Swate.Components.FileExplorerTypes

module private GraphObjectExplorerHelper =

    let private graphKindForNode (nodeMetaById: Map<string, GraphNodeMeta>) (node: ArcExplorerNode) =
        nodeMetaById
        |> Map.tryFind node.id
        |> Option.map _.GraphKind
        |> Option.defaultValue (GraphExplorerNodeKind.ofArcExplorerNodeKind node.kind)

    let private graphKindLabelForNode (nodeMetaById: Map<string, GraphNodeMeta>) (node: ArcExplorerNode) =
        graphKindForNode nodeMetaById node
        |> GraphExplorerNodeKind.label

    let private roleLabelForNode (nodeMetaById: Map<string, GraphNodeMeta>) (node: ArcExplorerNode) =
        nodeMetaById
        |> Map.tryFind node.id
        |> Option.map _.RoleLabel
        |> Option.defaultValue (if node.isReference then "Reference" else "Canonical")

    let selectedSubtitle
        (selection: ResolvedSelection option)
        (nodeMetaById: Map<string, GraphNodeMeta>)
        =
        selection
        |> Option.map (fun selection ->
            let kindLabel = graphKindLabelForNode nodeMetaById selection.Node
            let role = roleLabelForNode nodeMetaById selection.Node
            $"{kindLabel} | {role}")
        |> Option.defaultValue "Selection"

    let private flattenFileItems (items: FileItem list) =
        let rec loop (items: FileItem list) =
            items
            |> List.collect (fun item ->
                item :: (item.Children |> Option.defaultValue [] |> loop))

        loop items

    let private flattenNodesWithAncestors (nodes: ArcExplorerNode list) =
        let rec loop (ancestors: ArcExplorerNode list) (nodes: ArcExplorerNode list) =
            nodes
            |> List.collect (fun node ->
                let orderedAncestors = List.rev ancestors
                (node, orderedAncestors) :: loop (node :: ancestors) node.children)

        loop [] nodes

    let searchableItems
        (nodes: ArcExplorerNode list)
        (items: FileItem list)
        (nodeMetaById: Map<string, GraphNodeMeta>)
        =
        let itemsById =
            items
            |> flattenFileItems
            |> List.map (fun item -> item.Id, item)
            |> Map.ofList

        nodes
        |> flattenNodesWithAncestors
        |> List.choose (fun (node, ancestors) ->
            let graphKind = graphKindForNode nodeMetaById node

            if
                not node.isSelectable
                || graphKind = GraphExplorerNodeKind.Arc
                || graphKind = GraphExplorerNodeKind.Group
            then
                None
            else
                itemsById
                |> Map.tryFind node.id
                |> Option.map (fun item ->
                    let lineagePart =
                        ancestors
                        |> List.filter (fun ancestor ->
                            graphKindForNode nodeMetaById ancestor <> GraphExplorerNodeKind.Arc)
                        |> List.map _.name
                        |> function
                            | [] -> None
                            | lineage ->
                                let lineageText = String.concat " / " lineage
                                Some $"In: {lineageText}"
                        |> Option.toList

                    let subtitleParts = [
                        graphKindLabelForNode nodeMetaById node
                        roleLabelForNode nodeMetaById node
                        yield! lineagePart
                        yield! node.path |> Option.toList
                    ]

                    node.name, Some(String.concat " | " subtitleParts), item))
        |> List.sortBy (fun (name, _, _) -> name.ToLowerInvariant())
        |> List.toArray

[<Erase; Mangle(false)>]
type GraphObjectExplorer =

    [<ReactComponent>]
    static member private StoryExample() =
        let graphObjects = React.useMemo ((fun () -> GraphObjectFixture.fakeGraphObjects ()), [||])

        let nodes, nodeMetaById =
            React.useMemo ((fun () -> GraphExplorerNodes.toArcExplorerNodesWithMetaFromArcObjects graphObjects), [| box graphObjects |])

        let selection, setSelection = React.useState ArcSelection.empty

        let selectedKindIndices, setSelectedKindIndices =
            React.useState (KindFilter.defaultSelectedIndices KindFilter.graphObjectExplorerOptions)

        let setSelectedKindIndicesWithDatasetCascade (nextSelectedKindIndices: Set<int>) =
            let syncedSelection =
                GraphObjectExplorerFilter.syncDatasetKindSelection
                    KindFilter.graphObjectExplorerOptions
                    selectedKindIndices
                    nextSelectedKindIndices

            setSelectedKindIndices syncedSelection

        let visibleSemanticKinds =
            KindFilter.selectedLabels KindFilter.graphObjectExplorerOptions selectedKindIndices

        let selectedNodeIdInTree =
            React.useMemo (
                (fun () -> ARCExplorer.getSelectedItemId nodes selection),
                [| box nodes; box selection |]
            )

        let filteredNodes =
            React.useMemo (
                (fun () ->
                    GraphObjectExplorerFilter.filterNodesBySemanticKinds
                        visibleSemanticKinds
                        nodes
                        nodeMetaById
                        selectedNodeIdInTree),
                [| box visibleSemanticKinds; box nodes; box nodeMetaById; box selectedNodeIdInTree |]
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
                (fun () -> GraphExplorerNodes.toGraphFileItems nodeMetaById viewModel.FilteredTree),
                [| box nodeMetaById; box viewModel.FilteredTree |]
            )

        let searchItems =
            React.useMemo (
                (fun () ->
                    GraphObjectExplorerHelper.searchableItems
                        viewModel.FilteredTree
                        explorerPaneItems
                        nodeMetaById),
                [| box viewModel.FilteredTree; box explorerPaneItems; box nodeMetaById |]
            )

        let selectedSubtitleText =
            React.useMemo (
                (fun () -> GraphObjectExplorerHelper.selectedSubtitle viewModel.Selection nodeMetaById),
                [| box viewModel.Selection; box nodeMetaById |]
            )

        let treePaneItems =
            React.useMemo (
                (fun () ->
                    explorerPaneItems
                    |> GraphObjectExplorerTreeData.flattenNestedChildrenOnParentLevel
                    |> GraphObjectFixture.collapseExplorerItems),
                [| box explorerPaneItems |]
            )

        let setExplorerSelection (nodeId: string) (path: string option) =
            setSelection (ArcSelection.forExplorerNode nodeId path)

        let searchAction =
            ARCObjectWidget.SearchActionForExplorerItems(
                searchItems,
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
                tileIconSizeClass = "swt:text-lg",
                contextIconSizeClass = "swt:text-sm",
                compactEntityTiles = true,
                stickyContextBreadcrumb = true,
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
                    selectedSubtitleText,
                    KindFilter.graphObjectExplorerOptions,
                    selectedKindIndices,
                    setSelectedKindIndicesWithDatasetCascade,
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
