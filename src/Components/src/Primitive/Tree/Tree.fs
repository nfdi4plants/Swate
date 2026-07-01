namespace Swate.Components.Primitive.Tree

open Fable.Core
open Feliz
open Swate.Components
open Swate.Components.Primitive.Tree.Context
open Swate.Components.Primitive.Tree.Helper
open Swate.Components.Primitive.Tree.Hooks
open Swate.Components.Primitive.Tree.State
open Swate.Components.Primitive.Tree.Types

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

        let treeState: TreeState<'T> = useTreeState defaultExpandedIds defaultSelectedIds

        let effectiveSelectedIds, setSelection =
            useControlledSelection selectedIds onSelectionChange treeState

        useTreeApi apiRef treeState.SetLoadedChildren treeState.SetExpandedIds

        let lookup =
            React.useMemo (
                (fun () -> NodeState.flattenVisible dataSource treeState.LoadedChildren treeState.ExpandedIds items),
                [|
                    box dataSource
                    box treeState.LoadedChildren
                    box treeState.ExpandedIds
                    box items
                |]
            )

        let focusedId = NodeState.focusedOrFirst treeState.FocusedId lookup.VisibleNodes

        React.useEffect (
            (fun () ->
                if focusedId <> treeState.FocusedId then
                    treeState.SetFocusedId focusedId
            ),
            [| box focusedId |]
        )

        let actions =
            useTreeNodeActions
                treeRef
                dataSource
                selectionMode
                isSelectionDisabled
                isNodeSelectable
                enableLazyLoading
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
            let node = row.Node
            let loadState = NodeState.loadStateFor node.id treeState.LoadedChildren
            let isExpanded = treeState.ExpandedIds.Contains node.id
            let canExpand = NodeState.canExpand dataSource node

            TreeNode.Row {
                Row = row
                IsExpanded = isExpanded
                IsSelected = effectiveSelectedIds.Contains node.id
                IsFocused = focusedId = Some node.id
                IsLoading = loadState.Status = TreeLazyLoadStatus.Loading
                Error = loadState.Error
                CanExpand = canExpand
                CanSelect = not isSelectionDisabled && isNodeSelectable node
                RenderNode = renderNode
                Leading = leading
                Trailing = trailing
                StyleFn = styleFn
                OnToggle = fun () -> actions.ExpandNode node
                OnSelect =
                    fun event ->
                        event.preventDefault ()
                        event.stopPropagation ()

                        if canExpand then
                            actions.ExpandNode node

                        actions.SelectNode node
                OnFocus = fun () -> treeState.SetFocusedId(Some node.id)
                OnKeyDown = actions.OnNodeKeyDown node
                Debug = debug
            }

        let rows = lookup.VisibleNodes

        let treeContent =
            if TreeHelper.shouldUseVirtualization enableVirtualization rows.Length then
                let virtualizer =
                    Virtual.useVirtualizer (
                        count = rows.Length,
                        getScrollElement = (fun () -> scrollRef.current),
                        estimateSize = (fun _ -> estimateNodeHeight),
                        overscan = 8
                    )

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
                TreeCtx.Provider(Some(toBoxedContext contextValue), treeContent)
                contextMenu
                if debug then
                    Html.div [
                        prop.testId "tree-selected-ids"
                        prop.className "swt:hidden"
                        prop.text (TreeHelper.selectedIdsArray effectiveSelectedIds |> String.concat ",")
                    ]
            ]
        ]
