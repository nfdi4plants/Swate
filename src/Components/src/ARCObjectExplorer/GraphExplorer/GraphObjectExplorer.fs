namespace Swate.Components.ARCObjectExplorer.GraphExplorer

open Fable.Core
open Feliz
open Swate.Components.Shared
open Swate.Components.ARCObjectExplorer
open Swate.Components.ARCObjectExplorer.Model

[<Erase; Mangle(false)>]
type GraphObjectExplorer =

    [<ReactComponent>]
    static member private StoryExample() =
        let graphModels = React.useMemo ((fun () -> GraphObjectFixture.fakeGraphModels ()), [||])

        let nodes, nodeMetaById =
            React.useMemo ((fun () -> ArcExplorerNodes.toArcExplorerNodesWithMetaFromArcs graphModels), [| box graphModels |])

        let selection, setSelection = React.useState ArcSelection.empty

        let selectedKindIndices, setSelectedKindIndices =
            React.useState (KindFilter.defaultSelectedIndices KindFilter.graphObjectExplorerOptions)

        let viewModel =
            create
                nodes
                selection
                KindFilter.graphObjectExplorerOptions
                selectedKindIndices

        let collapsedExplorerItems =
            React.useMemo (
                (fun () -> GraphObjectFixture.collapseExplorerItems viewModel.ExplorerItems),
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
                initialItems = collapsedExplorerItems,
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
                collapsedExplorerItems,
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
