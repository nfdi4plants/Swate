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
            onSelectionChange: (string[] -> unit) option
        ) =
        let config = useTreeCtx<'T> ()
        let treeRef = React.useElementRef ()
        let scrollRef = React.useElementRef ()
        let activeRequestIdsRef = React.useRef<Map<string, int>> Map.empty
        let loadRequestIdRef = React.useRef 0

        let treeState: TreeState<'T> =
            useTreeState selectionMode defaultExpandedIds defaultSelectedIds

        let effectiveSelectedIds, setSelection =
            useControlledSelection selectionMode selectedIds onSelectionChange treeState

        useTreeApi config.ApiRef activeRequestIdsRef treeState.SetLoadedChildren treeState.SetExpandedIds

        let lookup =
            React.useMemo (
                (fun () -> flattenVisible treeState.LoadedChildren treeState.ExpandedIds items),
                [|
                    box treeState.LoadedChildren
                    box treeState.ExpandedIds
                    box items
                |]
            )

        let focusedId = focusedOrFirst treeState.FocusedId lookup.VisibleNodes
        let rows = lookup.VisibleNodes

        let shouldUseVirtualization =
            TreeHelper.shouldUseVirtualization config.EnableVirtualization rows.Length

        let virtualizer =
            Virtual.useVirtualizer (
                count = rows.Length,
                getScrollElement = (fun () -> scrollRef.current),
                estimateSize = (fun _ -> config.EstimateNodeHeight),
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
                activeRequestIdsRef
                loadRequestIdRef
                treeState
                lookup
                focusedId
                selectionMode
                effectiveSelectedIds
                setSelection

        let renderRow row =
            let loadState = loadStateFor row.node.id treeState.LoadedChildren
            let isExpanded = treeState.ExpandedIds.Contains row.node.id

            let canExpandNode =
                canExpand config.DataSource config.EnableLazyLoading treeState.LoadedChildren row.node

            TreeNode.Row(
                row = row,
                isExpanded = isExpanded,
                isSelected = effectiveSelectedIds.Contains row.node.id,
                isFocused = (focusedId = Some row.node.id),
                isLoading = (loadState.Status = TreeLazyLoadStatus.Loading),
                error = loadState.Error,
                canExpand = canExpandNode,
                onToggle = (fun () -> actions.ExpandNode row.node),
                onSelect =
                    (fun event ->
                        event.preventDefault ()
                        event.stopPropagation ()

                        if canExpandNode then
                            actions.ExpandNode row.node

                        let extendSelection = event.shiftKey || event.ctrlKey || event.metaKey

                        actions.SelectNode row.node extendSelection
                    ),
                onFocus =
                    (fun () ->
                        if treeState.FocusedId <> Some row.node.id then
                            treeState.SetFocusedId(Some row.node.id)
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
                                for virtualRow in virtualizer.getVirtualItems () do
                                    let row = rows.[virtualRow.index]

                                    Html.div [
                                        prop.key row.node.id
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
                            Html.div [ prop.key row.node.id; prop.children [ renderRow row ] ]
                    ]
                ]

        Html.div [
            prop.ref treeRef
            prop.role "tree"
            prop.ariaLabel config.AriaLabel
            prop.custom ("aria-multiselectable", (selectionMode = TreeSelectionMode.Multiple))
            prop.custom ("data-tree-root", "true")
            yield!
                config.OnContextMenu
                |> Option.map (fun onContextMenu ->
                    prop.onContextMenu (fun event ->
                        let target =
                            tryGetNodeId event
                            |> Option.bind (fun nodeId -> lookup.Nodes |> Map.tryFind nodeId)

                        onContextMenu.Invoke(event, target)
                    )
                )
                |> Option.toList
            if config.Debug then
                prop.testId "generic-tree"
            prop.className (TreeHelper.rootClasses config.StyleFn)
            prop.children [
                treeContent
                if config.Debug then
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
            ?enableLazyLoading: bool,
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

        let enableLazyLoading = defaultArg enableLazyLoading dataSource.IsSome
        let enableVirtualization = defaultArg enableVirtualization false
        let estimateNodeHeight = defaultArg estimateNodeHeight 34

        let styleFn = React.useMemo ((fun () -> styleFn), [| box styleFn |])

        let onError =
            React.useMemo ((fun () -> defaultArg onError ignore), [| box onError |])

        let ariaLabel = defaultArg ariaLabel "Tree"
        let debug = defaultArg debug false

        let contextValue: TreeContextValue<'T> =
            React.useMemo (
                (fun () -> {
                    DataSource = dataSource
                    SelectionDisabled = isSelectionDisabled
                    IsNodeSelectable = isNodeSelectable
                    EnableLazyLoading = enableLazyLoading
                    EnableVirtualization = enableVirtualization
                    EstimateNodeHeight = estimateNodeHeight
                    OnContextMenu = onContextMenu
                    RenderNode = renderNode
                    Leading = leading
                    Trailing = trailing
                    StyleFn = styleFn
                    OnError = onError
                    ApiRef = apiRef
                    AriaLabel = ariaLabel
                    Debug = debug
                }),
                [|
                    box dataSource
                    box isSelectionDisabled
                    box isNodeSelectable
                    box enableLazyLoading
                    box enableVirtualization
                    box estimateNodeHeight
                    box onContextMenu
                    box renderNode
                    box leading
                    box trailing
                    box styleFn
                    box onError
                    box apiRef
                    box ariaLabel
                    box debug
                |]
            )

        TreeCtx.Provider(
            unbox<TreeContextValue<obj>> (box contextValue),
            Tree.Root(items, selectionMode, selectedIds, defaultSelectedIds, defaultExpandedIds, onSelectionChange)
        )
