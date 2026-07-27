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
    static member private Root<'T>(items: TreeItem<'T>[]) =
        let config = useTreeCtx<'T> ()
        let treeRef = React.useElementRef ()
        let scrollRef = React.useElementRef ()
        let loadingNodeIdsRef = React.useRef<ResizeArray<string>> (ResizeArray())
        let loadRequestIdRef = React.useRef 0

        let treeState: TreeState<'T> =
            useTreeState config.DefaultExpandedIds config.DefaultSelectedIds

        let effectiveSelectedIds, setSelection =
            useControlledSelection config.SelectedIds config.OnSelectionChange treeState

        useTreeApi config.ApiRef loadingNodeIdsRef treeState.SetLoadedChildren treeState.SetExpandedIds

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
                loadingNodeIdsRef
                loadRequestIdRef
                treeState
                lookup
                focusedId
                effectiveSelectedIds
                setSelection

        let renderRow row =
            let loadState = loadStateFor row.Node.id treeState.LoadedChildren
            let isExpanded = treeState.ExpandedIds.Contains row.Node.id

            let canExpandNode =
                canExpand config.DataSource config.EnableLazyLoading treeState.LoadedChildren row.Node

            TreeNode.Row(
                row = row,
                isExpanded = isExpanded,
                isSelected = effectiveSelectedIds.Contains row.Node.id,
                isFocused = (focusedId = Some row.Node.id),
                isLoading = (loadState.Status = TreeLazyLoadStatus.Loading),
                error = loadState.Error,
                canExpand = canExpandNode,
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
                onKeyDown = actions.OnNodeKeyDown row.Node
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
            prop.ariaLabel config.AriaLabel
            prop.custom ("aria-multiselectable", (config.SelectionMode = TreeSelectionMode.Multiple))
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
        let contextValue: TreeContextValue<'T> = {
            DataSource = dataSource
            SelectionMode = defaultArg selectionMode TreeSelectionMode.Single
            SelectedIds = selectedIds
            DefaultSelectedIds = defaultSelectedIds
            DefaultExpandedIds = defaultExpandedIds
            OnSelectionChange = onSelectionChange
            SelectionDisabled = defaultArg isSelectionDisabled false
            IsNodeSelectable = defaultArg isNodeSelectable (fun _ -> true)
            EnableLazyLoading = defaultArg enableLazyLoading dataSource.IsSome
            EnableVirtualization = defaultArg enableVirtualization false
            EstimateNodeHeight = defaultArg estimateNodeHeight 34
            OnContextMenu = onContextMenu
            RenderNode = renderNode
            Leading = leading
            Trailing = trailing
            StyleFn = styleFn
            OnError = defaultArg onError ignore
            ApiRef = apiRef
            AriaLabel = defaultArg ariaLabel "Tree"
            Debug = defaultArg debug false
        }

        TreeCtx.Provider(unbox<TreeContextValue<obj>> (box contextValue), Tree.Root(items))
