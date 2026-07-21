namespace Swate.Components.Composite.Tree

open Fable.Core
open Feliz
open Swate.Components
open Swate.Components.Composite.Tree.Context
open Swate.Components.Composite.Tree.Hooks
open Swate.Components.Composite.Tree.State
open Swate.Components.Composite.Tree.Types

[<Erase; Mangle(false)>]
type Tree =

    [<ReactComponent>]
    static member Tree<'T>
        (
            items: TreeItem<'T>[],
            ?dataSource: TreeDataSource<'T>,
            ?selectionMode: TreeSelectionMode,
            ?selectedIds: string[],
            ?defaultSelectedIds: string[],
            ?defaultExpandedIds: string[],
            ?onSelectionChange: string[] -> unit,
            ?isSelectionDisabled: bool,
            ?isNodeSelectable: TreeItem<'T> -> bool,
            ?enableLazyLoading: bool,
            ?enableVirtualization: bool,
            ?estimateNodeHeight: int,
            ?contextMenuItems: TreeItem<'T> option -> Swate.Components.Primitive.ContextMenu.Types.ContextMenuItem[],
            ?renderNode: TreeRenderProps<'T> -> ReactElement,
            ?leading: TreeRenderProps<'T> -> ReactElement,
            ?trailing: TreeRenderProps<'T> -> ReactElement,
            ?styleFn: TreeStyleFn<'T>,
            ?onError: exn -> unit,
            ?apiRef: IRefValue<TreeApi option>,
            ?ariaLabel: string,
            ?debug: bool
        ) =
        let selectionMode = defaultArg selectionMode TreeSelectionMode.Single
        let isSelectionDisabled = defaultArg isSelectionDisabled false
        let isNodeSelectable = defaultArg isNodeSelectable (fun _ -> true)
        let enableLazyLoading = defaultArg enableLazyLoading dataSource.IsSome
        let enableVirtualization = defaultArg enableVirtualization false
        let estimateNodeHeight = defaultArg estimateNodeHeight 34
        let onError = defaultArg onError ignore
        let debug = defaultArg debug false
        let treeRef = React.useElementRef ()
        let scrollRef = React.useElementRef ()
        let loadingNodeIdsRef = React.useRef<ResizeArray<string>> (ResizeArray())
        let loadRequestIdRef = React.useRef 0

        let treeState: TreeState<'T> = useTreeState defaultExpandedIds defaultSelectedIds

        let effectiveSelectedIds, setSelection =
            useControlledSelection selectedIds onSelectionChange treeState

        useTreeApi apiRef loadingNodeIdsRef treeState.SetLoadedChildren treeState.SetExpandedIds

        let lookup =
            React.useMemo (
                (fun () -> flattenVisible dataSource treeState.LoadedChildren treeState.ExpandedIds items),
                [|
                    box dataSource
                    box treeState.LoadedChildren
                    box treeState.ExpandedIds
                    box items
                |]
            )

        let focusedId = focusedOrFirst treeState.FocusedId lookup.VisibleNodes

        let rows = lookup.VisibleNodes

        let shouldUseVirtualization =
            TreeHelper.shouldUseVirtualization enableVirtualization rows.Length

        let virtualizer =
            Virtual.useVirtualizer (
                count = rows.Length,
                getScrollElement = (fun () -> scrollRef.current),
                estimateSize = (fun _ -> estimateNodeHeight),
                overscan = 8
            )

        let scrollToIndex index =
            if shouldUseVirtualization then
                virtualizer.scrollToIndex (
                    index,
                    {|
                        align = Some Virtual.AlignOption.Auto
                        behavior = Some Virtual.ScrollBehavior.Auto
                    |}
                )

        let actions =
            useTreeNodeActions
                treeRef
                scrollToIndex
                dataSource
                selectionMode
                isSelectionDisabled
                isNodeSelectable
                enableLazyLoading
                loadingNodeIdsRef
                loadRequestIdRef
                treeState
                lookup
                focusedId
                effectiveSelectedIds
                setSelection
                onError

        let contextMenu = ContextMenuAdapter.render contextMenuItems treeRef lookup debug

        let contextValue: TreeContextValue<'T> = {
            DataSource = dataSource
            SelectionMode = selectionMode
            SelectionDisabled = isSelectionDisabled
            IsNodeSelectable = isNodeSelectable
            RenderNode = renderNode
            Leading = leading
            Trailing = trailing
            StyleFn = styleFn
            ContextMenuItems = contextMenuItems
            OnError = onError
            Debug = debug
        }

        let renderRow row =
            let loadState = loadStateFor row.Node.id treeState.LoadedChildren
            let isExpanded = treeState.ExpandedIds.Contains row.Node.id
            let canExpandNode = canExpand dataSource treeState.LoadedChildren row.Node

            TreeNode.Row(
                row = row,
                isExpanded = isExpanded,
                isSelected = effectiveSelectedIds.Contains row.Node.id,
                isFocused = (focusedId = Some row.Node.id),
                isLoading = (loadState.Status = TreeLazyLoadStatus.Loading),
                error = loadState.Error,
                canExpand = canExpandNode,
                canSelect = ((not isSelectionDisabled) && isNodeSelectable row.Node),
                ?renderNode = renderNode,
                ?leading = leading,
                ?trailing = trailing,
                ?styleFn = styleFn,
                onToggle = (fun () -> actions.ExpandNode row.Node),
                onSelect =
                    (fun event ->
                        event.preventDefault ()
                        event.stopPropagation ()

                        if canExpandNode then
                            actions.ExpandNode row.Node

                        let extendSelection = event.shiftKey || event.ctrlKey || event.metaKey

                        actions.SelectNode row.Node extendSelection
                    ),
                onFocus =
                    (fun () ->
                        if treeState.FocusedId <> Some row.Node.id then
                            treeState.SetFocusedId(Some row.Node.id)
                    ),
                onKeyDown = actions.OnNodeKeyDown row.Node,
                debug = debug
            )

        let treeContent =
            if shouldUseVirtualization then
                Html.div [
                    prop.ref scrollRef
                    prop.className "swt:max-h-96 swt:overflow-auto"
                    prop.custom ("data-tree-virtualized", "true")
                    prop.children [
                        Html.div [
                            prop.style [
                                style.height (virtualizer.getTotalSize ())
                                style.position.relative
                            ]
                            prop.children [
                                for virtualRow in virtualizer.getVirtualItems () do
                                    let row = rows.[virtualRow.index]

                                    Html.div [
                                        prop.key row.Node.id
                                        prop.ref (fun element -> virtualizer.measureElement (Option.ofObj element))
                                        prop.custom ("data-index", virtualRow.index)
                                        prop.style [
                                            style.position.absolute
                                            style.top 0
                                            style.left 0
                                            style.width (length.percent 100)
                                            style.custom ("transform", $"translateY({virtualRow.start}px)")
                                        ]
                                        prop.children [ renderRow row ]
                                    ]
                            ]
                        ]
                    ]
                ]
            else
                Html.div [
                    prop.custom ("data-tree-virtualized", "false")
                    prop.children [
                        for row in rows do
                            Html.div [ prop.key row.Node.id; prop.children [ renderRow row ] ]
                    ]
                ]

        Html.div [
            prop.ref treeRef
            prop.role "tree"
            prop.ariaLabel (defaultArg ariaLabel "Tree")
            prop.custom ("aria-multiselectable", (selectionMode = TreeSelectionMode.Multiple))
            prop.custom ("data-tree-root", "true")
            if debug then
                prop.testId "generic-tree"
            prop.className (TreeHelper.rootClasses styleFn)
            prop.children [
                TreeCtx.Provider(unbox<TreeContextValue<obj>> (box contextValue), treeContent)
                contextMenu
                if debug then
                    Html.div [
                        prop.testId "tree-selected-ids"
                        prop.className "swt:hidden"
                        prop.text (TreeHelper.selectedIdsArray effectiveSelectedIds |> String.concat ",")
                    ]
            ]
        ]
