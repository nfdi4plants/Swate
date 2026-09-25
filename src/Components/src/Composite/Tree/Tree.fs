namespace Swate.Components.Composite.Tree

open Fable.Core
open Feliz
open Swate.Components
open Swate.Components.Composite.Tree.Context
open Swate.Components.Composite.Tree.Dom
open Swate.Components.Composite.Tree.Hooks
open Swate.Components.Composite.Tree.State
open Swate.Components.Composite.Tree.Types

[<Erase; Mangle(false)>]
type Tree =

    [<ReactComponent>]
    static member private Root<'T>
        (
            items: TreeItem<'T>[],
            selectionMode: TreeSelectionMode,
            selectedIds: string[] option,
            defaultSelectedIds: string[] option,
            defaultExpandedIds: string[] option,
            onSelectionChange: (string[] -> unit) option,
            dataSource: TreeDataSource<'T> option,
            isSelectionDisabled: bool,
            isNodeSelectable: TreeItem<'T> -> bool,
            enableVirtualization: bool,
            estimateNodeHeight: int,
            onContextMenu: TreeContextMenuEvent<'T> option,
            styleFn: TreeStyleFn<'T> option,
            onError: exn -> unit,
            apiRef: IRefValue<TreeApi option> option,
            ariaLabel: string,
            debug: bool
        ) =
        let treeRef = React.useElementRef ()
        let scrollRef = React.useElementRef ()
        let activeRequestIdsRef = React.useRef<Map<string, int>> Map.empty
        let loadRequestIdRef = React.useRef 0

        let treeState: TreeState<'T> =
            useTreeState selectionMode defaultExpandedIds defaultSelectedIds

        let effectiveSelectedIds, setSelection =
            useControlledSelection selectionMode selectedIds onSelectionChange treeState

        let effectiveSelectedIdsRef = React.useRef effectiveSelectedIds
        effectiveSelectedIdsRef.current <- effectiveSelectedIds

        useTreeApi apiRef activeRequestIdsRef treeState.SetLoadedChildren treeState.SetExpandedIds

        let lookup =
            React.useMemo (
                (fun () -> flattenVisible treeState.LoadedChildren treeState.ExpandedIds items),
                [|
                    box treeState.LoadedChildren
                    box treeState.ExpandedIds
                    box items
                |]
            )

        let rows = lookup.VisibleNodes
        let activeId = activeOrFirst treeState.ActiveId effectiveSelectedIds rows
        let focusedId = visibleFocus treeState.FocusedId rows

        let shouldUseVirtualization = enableVirtualization && rows.Length > 0

        let virtualizer =
            Virtual.useVirtualizer (
                count = rows.Length,
                getScrollElement = (fun () -> scrollRef.current),
                estimateSize = (fun _ -> estimateNodeHeight),
                overscan = 8
            )

        // the virtualizer only returns the rows that are currently visible in the viewport,
        // so we need to keep track of which nodes are currently mounted in the DOM
        let virtualRows = virtualizer.getVirtualItems ()

        // get the ids of the nodes that are currently mounted in the DOM, based on the virtual rows
        let mountedNodeIds =
            if shouldUseVirtualization then
                virtualRows
                |> Seq.choose (fun virtualRow ->
                    rows
                    |> Array.tryItem virtualRow.index
                    |> Option.map (fun row -> TreeItem.getId row.node)
                )
                |> Seq.toArray
            else
                rows |> Array.map (fun row -> TreeItem.getId row.node)

        // tab stop is the node that is currently focused,
        // or if no node is focused, the active node, or if no node is active, the first mounted node
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
            useTreeNodeActions
                treeRef
                scrollToIndex
                activeRequestIdsRef
                loadRequestIdRef
                treeState
                lookup
                (focusedId |> Option.orElse activeId)
                selectionMode
                effectiveSelectedIdsRef
                setSelection
                dataSource
                isSelectionDisabled
                isNodeSelectable
                onError

        let renderRow row =
            let nodeId = TreeItem.getId row.node

            let loadState =
                treeState.LoadedChildren
                |> Map.tryFind nodeId
                |> Option.defaultValue emptyLoadState

            let isExpanded = treeState.ExpandedIds.Contains nodeId
            let canExpandNode = TreeItem.isBranch row.node

            TreeNode.Row(
                row = row,
                isExpanded = isExpanded,
                isSelected = effectiveSelectedIds.Contains nodeId,
                isActive = (activeId = Some nodeId),
                isFocused = (focusedId = Some nodeId),
                isTabStop = (tabStopId = Some nodeId),
                isLoading = (loadState.Status = TreeLazyLoadStatus.Loading),
                error = loadState.Error,
                canExpand = canExpandNode,
                onToggle = (fun () -> actions.ExpandNode row.node),
                onSelect =
                    (fun event ->
                        event.preventDefault ()
                        event.stopPropagation ()

                        let intent =
                            if event.shiftKey then
                                TreeSelectionIntent.Range
                            elif event.ctrlKey || event.metaKey then
                                TreeSelectionIntent.Toggle
                            else
                                TreeSelectionIntent.Replace

                        actions.SelectNode row.node intent
                    ),
                onFocus =
                    (fun () ->
                        if treeState.FocusedId <> Some nodeId then
                            treeState.SetFocusedId(Some nodeId)
                    ),
                onKeyDown = actions.OnNodeKeyDown row.node
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
                                |> Option.bind (fun nodeId -> lookup.Nodes |> Map.tryFind nodeId)

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
                    treeState.SetFocusedId None
            )
            if debug then
                prop.testId "generic-tree"
            prop.className (TreeHelper.rootClasses styleFn)
            prop.children [
                treeContent
                contextMenu
                if debug then
                    Html.div [
                        prop.testId "tree-selected-ids"
                        prop.className "swt:hidden"
                        prop.text (effectiveSelectedIds |> Set.toArray |> String.concat ",")
                    ]
            ]
        ]

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
            ?apiRef: IRefValue<TreeApi option>,
            ?ariaLabel: string,
            ?debug: bool
        ) =
        let selectionMode = defaultArg selectionMode TreeSelectionMode.Single
        let isSelectionDisabled = defaultArg isSelectionDisabled false

        let isNodeSelectable =
            React.useMemo ((fun () -> defaultArg isNodeSelectable (fun _ -> true)), [| box isNodeSelectable |])

        let enableVirtualization = defaultArg enableVirtualization false
        let estimateNodeHeight = defaultArg estimateNodeHeight 34

        let styleFn = React.useMemo ((fun () -> styleFn), [| box styleFn |])

        let onError =
            React.useMemo (
                (fun () -> defaultArg onError (fun error -> Browser.Dom.console.error error)),
                [| box onError |]
            )

        let ariaLabel = defaultArg ariaLabel "Tree"
        let debug = defaultArg debug false

        let contextValue: TreeContextValue =
            React.useMemo (
                (fun () -> {
                    SelectionDisabled = isSelectionDisabled
                    IsNodeSelectable = fun node -> isNodeSelectable (unbox<TreeItem<'T>> node)
                    RenderNode =
                        renderNode
                        |> Option.map (fun renderNode -> fun props -> renderNode (unbox<TreeRenderProps<'T>> props))
                    Leading =
                        leading
                        |> Option.map (fun leading -> fun props -> leading (unbox<TreeRenderProps<'T>> props))
                    Trailing =
                        trailing
                        |> Option.map (fun trailing -> fun props -> trailing (unbox<TreeRenderProps<'T>> props))
                    StyleFn =
                        styleFn
                        |> Option.map (fun styleFn ->
                            fun node classes -> styleFn (node |> Option.map unbox<TreeItem<'T>>) classes
                        )
                    Debug = debug
                }),
                [|
                    box isSelectionDisabled
                    box isNodeSelectable
                    box renderNode
                    box leading
                    box trailing
                    box styleFn
                    box debug
                |]
            )

        TreeCtx.Provider(
            contextValue,
            Tree.Root(
                items,
                selectionMode,
                selectedIds,
                defaultSelectedIds,
                defaultExpandedIds,
                onSelectionChange,
                dataSource,
                isSelectionDisabled,
                isNodeSelectable,
                enableVirtualization,
                estimateNodeHeight,
                onContextMenu,
                styleFn,
                onError,
                apiRef,
                ariaLabel,
                debug
            )
        )
