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
    (invalidatedNodeIdsRef: IRefValue<ResizeArray<string>>)
    setLoadedChildren
    setExpandedIds
    =
    let markInvalidated nodeId =
        if
            loadingNodeIdsRef.current.Contains nodeId
            && not (invalidatedNodeIdsRef.current.Contains nodeId)
        then
            invalidatedNodeIdsRef.current.Add nodeId

    React.useEffect (
        (fun () ->
            apiRef
            |> Option.iter (fun ref ->
                ref.current <-
                    Some {
                        InvalidateNode =
                            fun nodeId ->
                                markInvalidated nodeId
                                setLoadedChildren (NodeState.invalidateNode nodeId)
                                setExpandedIds (fun current -> current |> Set.remove nodeId)
                        InvalidateAll =
                            fun () ->
                                for nodeId in loadingNodeIdsRef.current do
                                    markInvalidated nodeId

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
    (invalidatedNodeIdsRef: IRefValue<ResizeArray<string>>)
    (treeState: TreeState<'T>)
    (lookup: TreeRowLookup<'T>)
    focusedId
    effectiveSelectedIds
    setSelection
    onError
    =
    let focusDom = TreeDom.focusNodeAfterRender treeRef

    let focusById nodeId =
        TreeController.tryFocusById lookup treeState.SetFocusedId scrollToIndex focusDom nodeId

    let expandNode (node: TreeItem<'T>) =
        TreeController.expandNode
            dataSource
            enableLazyLoading
            loadingNodeIdsRef
            invalidatedNodeIdsRef
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
            TreeController.focusByDelta lookup focusedId treeState.SetFocusedId scrollToIndex focusDom 1
        | kbdEventCode.arrowUp ->
            event.preventDefault ()
            TreeController.focusByDelta lookup focusedId treeState.SetFocusedId scrollToIndex focusDom -1
        // "Home" and "End" are KeyboardEvent.key values for jumping to the first or last visible node.
        | kbdEventCode.home ->
            event.preventDefault ()
            TreeController.focusFirst lookup treeState.SetFocusedId scrollToIndex focusDom
        | kbdEventCode.End ->
            event.preventDefault ()
            TreeController.focusLast lookup treeState.SetFocusedId scrollToIndex focusDom
        | kbdEventCode.arrowRight ->
            event.preventDefault ()

            if NodeState.canExpand dataSource treeState.LoadedChildren node then
                if treeState.ExpandedIds.Contains node.id then
                    TreeController.focusFirstChild lookup treeState.SetFocusedId scrollToIndex focusDom node.id
                else
                    expandNode node
        | kbdEventCode.arrowLeft ->
            event.preventDefault ()

            TreeController.collapseOrFocusParent
                lookup
                treeState.ExpandedIds
                treeState.SetExpandedIds
                treeState.SetFocusedId
                scrollToIndex
                focusDom
                node.id
        | kbdEventCode.enter
        | kbdEventCode.space ->
            event.preventDefault ()

            if NodeState.canExpand dataSource treeState.LoadedChildren node then
                expandNode node

            selectNode node (event.shiftKey || event.ctrlKey || event.metaKey)
        | _ -> ()

    {
        ExpandNode = expandNode
        SelectNode = selectNode
        FocusById = focusById
        OnNodeKeyDown = onNodeKeyDown
    }
