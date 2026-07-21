module Swate.Components.Composite.Tree.Hooks

open Browser.Types
open Fable.Core
open Feliz
open Swate.Components
open Swate.Components.Composite.Tree.Dom
open Swate.Components.Composite.Tree.State
open Swate.Components.Composite.Tree.Types

let private toSet (values: string[] option) =
    values |> Option.map Set.ofArray |> Option.defaultValue Set.empty

[<Hook>]
let useTreeState (defaultExpandedIds: string[] option) (defaultSelectedIds: string[] option) =
    let expandedIds, setExpandedIds =
        React.useStateWithUpdater (toSet defaultExpandedIds)

    let selectedIds, setSelectedIds =
        React.useStateWithUpdater (toSet defaultSelectedIds)

    let focusedId, setFocusedId = React.useState<string option> None

    let loadedChildren, setLoadedChildren =
        React.useStateWithUpdater<Map<string, TreeLoadState<'T>>> (Map.empty)

    {
        ExpandedIds = expandedIds
        SetExpandedIds = setExpandedIds
        SelectedIds = selectedIds
        SetSelectedIds = setSelectedIds
        FocusedId = focusedId
        SetFocusedId = setFocusedId
        LoadedChildren = loadedChildren
        SetLoadedChildren = setLoadedChildren
    }

[<Hook>]
let useControlledSelection
    (selectedIds: string[] option)
    (onSelectionChange: (string[] -> unit) option)
    (treeState: TreeState<'T>)
    =
    let effectiveSelectedIds =
        selectedIds
        |> Option.map Set.ofArray
        |> Option.defaultValue treeState.SelectedIds

    let setSelection nextSelectedIds =
        match selectedIds with
        | Some _ -> ()
        | None -> treeState.SetSelectedIds(fun _ -> nextSelectedIds)

        onSelectionChange
        |> Option.iter (fun handler -> handler (nextSelectedIds |> Set.toArray))

    effectiveSelectedIds, setSelection

[<Hook>]
let useTreeApi
    (apiRef: IRefValue<TreeApi option> option)
    (loadingNodeIdsRef: IRefValue<ResizeArray<string>>)
    setLoadedChildren
    setExpandedIds
    =
    React.useEffect (
        (fun () ->
            apiRef
            |> Option.iter (fun ref ->
                ref.current <-
                    Some {
                        InvalidateNode =
                            fun nodeId ->
                                loadingNodeIdsRef.current.Remove nodeId |> ignore
                                setLoadedChildren (invalidateNode nodeId)
                                setExpandedIds (fun current -> current |> Set.remove nodeId)
                        InvalidateAll =
                            fun () ->
                                loadingNodeIdsRef.current.Clear()
                                setLoadedChildren (fun _ -> Map.empty)
                                setExpandedIds (fun _ -> Set.empty)
                    }
            )

            fun () -> apiRef |> Option.iter (fun ref -> ref.current <- None)
        ),
        [| box apiRef |]
    )

[<Hook>]
let useTreeNodeActions
    (treeRef: IRefValue<HTMLElement option>)
    scrollToIndex
    (dataSource: TreeDataSource<'T> option)
    selectionMode
    isSelectionDisabled
    isNodeSelectable
    enableLazyLoading
    (loadingNodeIdsRef: IRefValue<ResizeArray<string>>)
    (loadRequestIdRef: IRefValue<int>)
    (treeState: TreeState<'T>)
    (lookup: TreeRowLookup<'T>)
    focusedId
    effectiveSelectedIds
    setSelection
    onError
    =
    let focusController: TreeFocusController<'T> = {
        Lookup = lookup
        SetFocusedId = treeState.SetFocusedId
        ScrollToIndex = scrollToIndex
        FocusDom = focusNodeAfterRender treeRef
    }

    let loadNode (node: TreeItem<'T>) =
        TreeController.loadBranchChildren
            dataSource
            enableLazyLoading
            loadingNodeIdsRef
            loadRequestIdRef
            treeState.LoadedChildren
            treeState.SetLoadedChildren
            treeState.SetExpandedIds
            onError
            node
        |> Promise.start

    React.useEffect (
        (fun () ->
            lookup.VisibleNodes
            |> Array.iter (fun row ->
                if
                    treeState.ExpandedIds.Contains row.Node.id
                    && canExpand dataSource treeState.LoadedChildren row.Node
                    && (directChildren treeState.LoadedChildren row.Node).IsNone
                then
                    loadNode row.Node
            )
        ),
        [|
            box dataSource
            box enableLazyLoading
            box treeState.ExpandedIds
            box treeState.LoadedChildren
            box lookup.VisibleNodes
        |]
    )

    let expandNode (node: TreeItem<'T>) =
        TreeController.expandNode
            dataSource
            enableLazyLoading
            loadingNodeIdsRef
            loadRequestIdRef
            treeState.LoadedChildren
            treeState.ExpandedIds
            treeState.SetExpandedIds
            treeState.SetLoadedChildren
            onError
            node

    let selectNode (node: TreeItem<'T>) extendSelection =
        TreeController.selectNode
            selectionMode
            isSelectionDisabled
            isNodeSelectable
            effectiveSelectedIds
            setSelection
            node
            extendSelection

    let onNodeKeyDown (node: TreeItem<'T>) (event: KeyboardEvent) =
        match event.key with
        | kbdEventCode.arrowDown ->
            event.preventDefault ()
            TreeController.focusByDelta focusController focusedId 1
        | kbdEventCode.arrowUp ->
            event.preventDefault ()
            TreeController.focusByDelta focusController focusedId -1
        // "Home" and "End" are KeyboardEvent.key values for jumping to the first or last visible node.
        | kbdEventCode.home ->
            event.preventDefault ()
            TreeController.focusFirst focusController
        | kbdEventCode.End ->
            event.preventDefault ()
            TreeController.focusLast focusController
        | kbdEventCode.arrowRight ->
            event.preventDefault ()

            if canExpand dataSource treeState.LoadedChildren node then
                if treeState.ExpandedIds.Contains node.id then
                    TreeController.focusFirstChild focusController node.id
                else
                    expandNode node
        | kbdEventCode.arrowLeft ->
            event.preventDefault ()

            TreeController.collapseOrFocusParent focusController treeState.ExpandedIds treeState.SetExpandedIds node.id
        | kbdEventCode.enter
        | kbdEventCode.space ->
            event.preventDefault ()

            if canExpand dataSource treeState.LoadedChildren node then
                expandNode node

            selectNode node (event.shiftKey || event.ctrlKey || event.metaKey)
        | _ -> ()

    {
        ExpandNode = expandNode
        SelectNode = selectNode
        OnNodeKeyDown = onNodeKeyDown
    }
