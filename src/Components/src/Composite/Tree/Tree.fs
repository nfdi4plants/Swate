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

    [<ReactComponent(true)>]
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
            ?viewportClassName: string,
            ?onContextMenu: TreeContextMenuEvent<'T>,
            ?renderNode: TreeRenderProps<'T> -> ReactElement,
            ?leading: TreeRenderProps<'T> -> ReactElement,
            ?trailing: TreeRenderProps<'T> -> ReactElement,
            ?styleFn: TreeStyleFn<'T>,
            ?onError: exn -> unit,
            ?ref: IRefValue<TreeApi>,
            ?ariaLabel: string
        ) =
        let selectionMode = defaultArg selectionMode TreeSelectionMode.Single
        let isSelectionDisabled = defaultArg isSelectionDisabled false
        let isNodeSelectable = defaultArg isNodeSelectable (fun _ -> true)
        let enableVirtualization = defaultArg enableVirtualization false
        let estimateNodeHeight = defaultArg estimateNodeHeight 34
        let onError = defaultArg onError (fun error -> Browser.Dom.console.error error)
        let ariaLabel = defaultArg ariaLabel "Tree"

        let treeRef = React.useElementRef ()
        let scrollRef = React.useElementRef ()

        let loadTrackerRef =
            React.useRef<TreeLoadTracker> {
                nextRequestId = 0
                activeRequestIds = Map.empty
            }

        let presentation = {
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
                rangeExtractor = Virtual.pinnedRangeExtractor (pinnedFocusIndex |> Option.toArray)
            )

        let virtualRows = virtualizer.getVirtualItems ()

        let mountedIndices = virtualRows |> Array.map _.index |> Set.ofArray

        let isMounted nodeId =
            lookup.indices
            |> Map.tryFind nodeId
            |> Option.exists (fun index -> not shouldUseVirtualization || mountedIndices.Contains index)

        let firstMounted =
            if shouldUseVirtualization then
                virtualRows
                |> Array.tryHead
                |> Option.bind (fun row -> rows |> Array.tryItem row.index)
            else
                rows |> Array.tryHead

        let tabStopId =
            focusedId
            |> Option.filter isMounted
            |> Option.orElseWith (fun () -> activeId |> Option.filter isMounted)
            |> Option.orElseWith (fun () -> firstMounted |> Option.map (fun row -> TreeItem.getId row.node))

        let scrollToIndex index =
            if shouldUseVirtualization then
                virtualizer.scrollToIndex (
                    index,
                    align = Virtual.AlignOption.Auto,
                    behavior = Virtual.ScrollBehavior.Auto
                )

        let currentRef, actions =
            useTreeNodeActions {
                treeRef = treeRef
                trackerRef = loadTrackerRef
                items = items
                defaultExpandedIds = defaultArg defaultExpandedIds [||]
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

        let onTreeFocus, onTreeBlur = useFocusRecovery currentRef

        React.useImperativeHandle (
            !!ref,
            (fun () -> TreeApi(actions.invalidateNode, actions.invalidateAll)),
            [| box actions |]
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
                {
                    comparisonNode = row.comparisonNode
                    depth = row.depth
                    posInSet = row.posInSet
                    setSize = row.setSize
                    isExpanded = isExpanded
                    isSelected = isSelected
                    isActive = isActive
                    isFocused = isFocused
                    isTabStop = (tabStopId = Some nodeId)
                    isLoading = (loadState.status = TreeLazyLoadStatus.Loading)
                    error = loadState.error
                    canSelect = canSelect
                    className =
                        Helper.nodeContainerClasses row canSelect canExpand isSelected isActive isFocused styleFn
                    presentation = presentation
                },
                currentRef,
                actions
            )

        let treeContent =
            if shouldUseVirtualization then
                Html.div [
                    prop.ref scrollRef
                    prop.className [
                        "swt:overflow-auto"
                        defaultArg viewportClassName "swt:max-h-96"
                    ]
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
                                        yield!
                                            Virtual.rowProps (
                                                virtualRow.index,
                                                virtualRow.start,
                                                measureElement = virtualizer.measureElement
                                            )
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
                        let target = unbox<TreeContextMenuTarget<'T>> data
                        contextMenuItems.Invoke(target.event, target.item) |> Array.toList
                    ),
                    ref = treeRef,
                    onSpawn =
                        (fun event ->
                            let target =
                                tryGetNodeId event
                                |> Option.bind (fun nodeId -> lookup.nodes |> Map.tryFind nodeId)

                            Some(box (TreeContextMenuTarget(event, target)))
                        )
                )
            | None -> Html.none

        let rootLoadState =
            if items.Length = 0 then
                treeState.loadedChildren |> Map.tryFind rootCacheKey
            else
                None

        Html.div [
            prop.ref treeRef
            prop.role "tree"
            prop.tabIndex -1
            prop.ariaLabel ariaLabel
            prop.ariaMultiSelectable (selectionMode.Equals TreeSelectionMode.Multiple)
            prop.ariaBusy (
                rootLoadState
                |> Option.exists (fun state -> state.status = TreeLazyLoadStatus.Loading)
            )
            prop.custom ("data-tree-root", "true")
            prop.onFocus onTreeFocus
            prop.onBlur onTreeBlur
            prop.className (Helper.rootClasses styleFn)
            prop.children [
                treeContent
                contextMenu
                match rootLoadState with
                | Some state when state.status = TreeLazyLoadStatus.Loading ->
                    Html.div [ prop.role "status"; prop.text "Loading tree…" ]
                | Some state when state.status = TreeLazyLoadStatus.Error ->
                    Html.div [
                        prop.role "alert"
                        prop.children [
                            Html.span (defaultArg state.error "Failed to load tree items")
                            Html.button [
                                prop.type'.button
                                prop.text "Retry tree loading"
                                prop.onClick (fun _ -> actions.invalidateAll ())
                            ]
                        ]
                    ]
                | _ -> ()
            ]
        ]
