namespace Swate.Components.Composite.Tree

open Fable.Core
open Fable.Core.JsInterop
open Feliz
open Swate.Components
open Swate.Components.Composite.Tree.Dom
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
            ?enableVirtualization: bool,
            ?estimateNodeHeight: int,
            ?onContextMenu: TreeContextMenuEvent<'T>,
            ?renderNode: TreeRenderProps<'T> -> ReactElement,
            ?leading: TreeRenderProps<'T> -> ReactElement,
            ?trailing: TreeRenderProps<'T> -> ReactElement,
            ?styleFn: TreeStyleFn<'T>,
            ?onError: exn -> unit,
            ?ref: IRefValue<TreeApi>,
            ?ariaLabel: string,
            ?debug: bool
        ) =
        let selectionMode = defaultArg selectionMode TreeSelectionMode.Single
        let isSelectionDisabled = defaultArg isSelectionDisabled false
        let isNodeSelectable = defaultArg isNodeSelectable (fun _ -> true)
        let enableVirtualization = defaultArg enableVirtualization false
        let estimateNodeHeight = defaultArg estimateNodeHeight 34
        let onError = defaultArg onError (fun error -> Browser.Dom.console.error error)
        let ariaLabel = defaultArg ariaLabel "Tree"
        let debug = defaultArg debug false

        let treeRef = React.useElementRef ()
        let scrollRef = React.useElementRef ()

        let loadTrackerRef =
            React.useRef<TreeLoadTracker> {
                nextRequestId = 0
                activeRequestIds = Map.empty
            }

        let presentationRef =
            React.useRef<TreePresentation<'T>> {
                renderNode = renderNode
                leading = leading
                trailing = trailing
            }

        presentationRef.current <- {
            renderNode = renderNode
            leading = leading
            trailing = trailing
        }

        let treeState: TreeState<'T> =
            useTreeState selectionMode defaultExpandedIds defaultSelectedIds

        let effectiveSelectedIds, setSelection =
            useControlledSelection selectionMode selectedIds onSelectionChange treeState

        let effectiveItems = rootItems items treeState.loadedChildren

        let lookup =
            React.useMemo (
                (fun () -> flattenVisible treeState.loadedChildren treeState.expandedIds effectiveItems),
                [|
                    box treeState.loadedChildren
                    box treeState.expandedIds
                    box effectiveItems
                |]
            )

        let rows = lookup.visibleNodes
        let nodesRef = React.useRef lookup.nodes
        nodesRef.current <- lookup.nodes
        let activeId = activeOrFirst treeState.activeId effectiveSelectedIds lookup
        let focusedId = visibleFocus treeState.focusedId lookup
        let shouldUseVirtualization = enableVirtualization && rows.Length > 0

        let pinnedFocusIndex =
            focusedId |> Option.bind (fun nodeId -> lookup.indices |> Map.tryFind nodeId)

        let virtualizer =
            Virtual.useVirtualizer (
                count = rows.Length,
                getScrollElement = (fun () -> scrollRef.current),
                estimateSize = (fun _ -> estimateNodeHeight),
                overscan = 8,
                rangeExtractor =
                    (fun range ->
                        match pinnedFocusIndex with
                        | Some index ->
                            [|
                                yield index
                                yield! Virtual.defaultRangeExtractor range
                            |]
                            |> Array.distinct
                            |> Array.sort
                        | None -> Virtual.defaultRangeExtractor range
                    )
            )

        let virtualRows = virtualizer.getVirtualItems ()

        let mountedNodeIds =
            if shouldUseVirtualization then
                virtualRows
                |> Array.choose (fun virtualRow ->
                    rows
                    |> Array.tryItem virtualRow.index
                    |> Option.map (fun row -> TreeItem.getId row.node)
                )
            else
                rows |> Array.map (fun row -> TreeItem.getId row.node)

        let tabStopId =
            focusedId
            |> Option.filter (fun nodeId -> mountedNodeIds |> Array.contains nodeId)
            |> Option.orElseWith (fun () ->
                activeId
                |> Option.filter (fun nodeId -> mountedNodeIds |> Array.contains nodeId)
            )
            |> Option.orElseWith (fun () -> mountedNodeIds |> Array.tryHead)

        let scrollToIndex index =
            if shouldUseVirtualization then
                virtualizer.scrollToIndex (
                    index,
                    align = Virtual.AlignOption.Auto,
                    behavior = Virtual.ScrollBehavior.Auto
                )

        let actions =
            useTreeNodeActions treeRef loadTrackerRef {
                items = items
                dataSource = dataSource
                isSelectionDisabled = isSelectionDisabled
                isNodeSelectable = isNodeSelectable
                lookup = lookup
                focusedId = focusedId |> Option.orElse activeId
                selectionMode = selectionMode
                effectiveSelectedIds = effectiveSelectedIds
                scrollToIndex = scrollToIndex
                setSelection = setSelection
                treeState = treeState
                onError = onError
            }

        React.useImperativeHandle (
            !!ref,
            (fun () ->
                TreeApi(
                    (fun nodeId ->
                        let invalidatedIds = invalidatedSubtreeIds items treeState.loadedChildren nodeId

                        if not invalidatedIds.IsEmpty then
                            loadTrackerRef.current.activeRequestIds <-
                                loadTrackerRef.current.activeRequestIds
                                |> Map.filter (fun cacheKey _ -> not (invalidatedIds.Contains cacheKey))

                            treeState.setLoadedChildren (removeCacheEntries invalidatedIds)

                            treeState.setExpandedIds (removeDescendantExpansions nodeId invalidatedIds)
                    ),
                    (fun () ->
                        loadTrackerRef.current.activeRequestIds <- Map.empty

                        treeState.setExpandedIds (preserveExpansionAfterInvalidateAll items treeState.loadedChildren)

                        treeState.setLoadedChildren (fun _ -> Map.empty)
                    )
                )
            )
        )

        let renderRow row =
            let nodeId = TreeItem.getId row.node

            let loadState =
                treeState.loadedChildren
                |> Map.tryFind nodeId
                |> Option.defaultValue emptyLoadState

            let isExpanded = treeState.expandedIds.Contains nodeId
            let canExpand = TreeItem.isBranch row.node
            let canSelect = not isSelectionDisabled && isNodeSelectable row.node
            let isSelected = effectiveSelectedIds |> Array.contains nodeId
            let isActive = activeId = Some nodeId
            let isFocused = focusedId = Some nodeId

            TreeNode.TreeNode(
                nodeId = nodeId,
                comparisonNode = row.comparisonNode,
                depth = row.depth,
                posInSet = row.posInSet,
                setSize = row.setSize,
                isExpanded = isExpanded,
                isSelected = isSelected,
                isActive = isActive,
                isFocused = isFocused,
                isTabStop = (tabStopId = Some nodeId),
                isLoading = (loadState.status = TreeLazyLoadStatus.Loading),
                error = loadState.error,
                canSelect = canSelect,
                canExpand = canExpand,
                className = Helper.nodeContainerClasses row canSelect canExpand isSelected isActive isFocused styleFn,
                debug = debug,
                nodesRef = nodesRef,
                presentationRef = presentationRef,
                onToggle = (fun () -> actions.expandNode nodeId),
                onSelect =
                    (fun event ->
                        event.preventDefault ()
                        event.stopPropagation ()
                        (unbox<Browser.Types.HTMLElement> event.currentTarget).focus ()

                        actions.selectNode
                            nodeId
                            (TreeController.selectionIntent event.shiftKey event.ctrlKey event.metaKey)
                    ),
                onFocus =
                    (fun () ->
                        if treeState.focusedId <> Some nodeId then
                            treeState.setFocusedId (Some nodeId)
                    ),
                onKeyDown = actions.onNodeKeyDown nodeId
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
                                for virtualRow in virtualRows do
                                    let row = rows.[virtualRow.index]
                                    let nodeId = TreeItem.getId row.node

                                    Html.div [
                                        prop.key nodeId
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
                            Html.div [
                                prop.key (TreeItem.getId row.node)
                                prop.children [ renderRow row ]
                            ]
                    ]
                ]

        let contextMenu =
            match onContextMenu with
            | Some contextMenuItems ->
                Swate.Components.Primitive.ContextMenu.ContextMenu.ContextMenu(
                    (fun data ->
                        let event, target = unbox<Browser.Types.MouseEvent * TreeItem<'T> option> data
                        contextMenuItems.Invoke(event, target) |> Array.toList
                    ),
                    ref = treeRef,
                    onSpawn =
                        (fun event ->
                            let target =
                                tryGetNodeId event
                                |> Option.bind (fun nodeId -> lookup.nodes |> Map.tryFind nodeId)

                            Some(box (event, target))
                        ),
                    debug = debug
                )
            | None -> Html.none

        Html.div [
            prop.ref treeRef
            prop.role "tree"
            prop.ariaLabel ariaLabel
            prop.custom ("aria-multiselectable", (selectionMode = TreeSelectionMode.Multiple))
            prop.custom ("data-tree-root", "true")
            prop.onBlur (fun event ->
                if focusMovedOutsideTree event then
                    treeState.setFocusedId None
            )
            if debug then
                prop.testId "generic-tree"
            prop.className (Helper.rootClasses styleFn)
            prop.children [
                treeContent
                contextMenu
                if debug then
                    Html.div [
                        prop.testId "tree-selected-ids"
                        prop.className "swt:hidden"
                        prop.text (effectiveSelectedIds |> String.concat ",")
                    ]
            ]
        ]
