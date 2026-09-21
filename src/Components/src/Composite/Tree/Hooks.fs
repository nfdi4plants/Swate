module Swate.Components.Composite.Tree.Hooks

open Browser.Types
open Fable.Core
open Feliz
open Swate.Components
open Swate.Components.Composite.Tree.Dom
open Swate.Components.Composite.Tree.State
open Swate.Components.Composite.Tree.Types

type private TreeNodeActionState<'T> = {
    DataSource: TreeDataSource<'T> option
    IsSelectionDisabled: bool
    IsNodeSelectable: TreeItem<'T> -> bool
    Lookup: TreeRowLookup<'T>
    FocusedId: string option
    SelectionMode: TreeSelectionMode
    ScrollToIndex: int -> unit
    SetSelection: Set<string> -> unit
    TreeState: TreeState<'T>
    OnError: exn -> unit
}

let private normalizeSelection selectionMode selectedIds =
    match selectionMode with
    | TreeSelectionMode.Single ->
        selectedIds
        |> Seq.tryHead
        |> Option.map Set.singleton
        |> Option.defaultValue Set.empty
    | TreeSelectionMode.Multiple -> selectedIds

let private toSelectionSet selectionMode (values: string[] option) =
    match selectionMode, values with
    | TreeSelectionMode.Single, Some values ->
        values
        |> Array.tryHead
        |> Option.map Set.singleton
        |> Option.defaultValue Set.empty
    | TreeSelectionMode.Multiple, Some values -> Set.ofArray values
    | _, None -> Set.empty

[<Hook>]
let useTreeState
    (selectionMode: TreeSelectionMode)
    (defaultExpandedIds: string[] option)
    (defaultSelectedIds: string[] option)
    =
    let expandedIds, setExpandedIds =
        React.useStateWithUpdater (defaultExpandedIds |> Option.map Set.ofArray |> Option.defaultValue Set.empty)

    let selectedIds, setSelectedIds =
        React.useStateWithUpdater (toSelectionSet selectionMode defaultSelectedIds)

    let activeId, setActiveId = React.useState<string option> None
    let focusedId, setFocusedId = React.useState<string option> None
    let selectionAnchorId, setSelectionAnchorId = React.useState<string option> None

    let loadedChildren, setLoadedChildren =
        React.useStateWithUpdater<Map<string, TreeLoadState<'T>>> (Map.empty)

    {
        ExpandedIds = expandedIds
        SetExpandedIds = setExpandedIds
        SelectedIds = selectedIds
        SetSelectedIds = setSelectedIds
        ActiveId = activeId
        SetActiveId = setActiveId
        FocusedId = focusedId
        SetFocusedId = setFocusedId
        SelectionAnchorId = selectionAnchorId
        SetSelectionAnchorId = setSelectionAnchorId
        LoadedChildren = loadedChildren
        SetLoadedChildren = setLoadedChildren
    }

[<Hook>]
let useControlledSelection
    (selectionMode: TreeSelectionMode)
    (selectedIds: string[] option)
    (onSelectionChange: (string[] -> unit) option)
    (treeState: TreeState<'T>)
    =
    let effectiveSelectedIds =
        selectedIds
        |> Option.map (fun selectedIds -> toSelectionSet selectionMode (Some selectedIds))
        |> Option.defaultWith (fun () -> normalizeSelection selectionMode treeState.SelectedIds)

    let setSelection nextSelectedIds =
        let normalizedSelectedIds = normalizeSelection selectionMode nextSelectedIds

        match selectedIds with
        | Some _ -> ()
        | None -> treeState.SetSelectedIds(fun _ -> normalizedSelectedIds)

        onSelectionChange
        |> Option.iter (fun handler -> handler (normalizedSelectedIds |> Set.toArray))

    effectiveSelectedIds, setSelection

[<Hook>]
let useTreeApi
    (apiRef: IRefValue<TreeApi option> option)
    (activeRequestIdsRef: IRefValue<Map<string, int>>)
    setLoadedChildren
    setExpandedIds
    =
    React.useEffect (
        (fun () ->
            apiRef
            |> Option.iter (fun ref ->
                ref.current <-
                    Some(
                        TreeApi(
                            (fun nodeId ->
                                activeRequestIdsRef.current <- activeRequestIdsRef.current |> Map.remove nodeId
                                setLoadedChildren (Map.remove nodeId)
                                setExpandedIds (fun current -> current |> Set.remove nodeId)
                            ),
                            (fun () ->
                                activeRequestIdsRef.current <- Map.empty
                                setLoadedChildren (fun _ -> Map.empty)
                                setExpandedIds (fun _ -> Set.empty)
                            )
                        )
                    )
            )

            fun () -> apiRef |> Option.iter (fun ref -> ref.current <- None)
        ),
        [| box apiRef |]
    )

[<Hook>]
let internal useTreeNodeActions
    (treeRef: IRefValue<HTMLElement option>)
    scrollToIndex
    (activeRequestIdsRef: IRefValue<Map<string, int>>)
    (loadRequestIdRef: IRefValue<int>)
    (treeState: TreeState<'T>)
    (lookup: TreeRowLookup<'T>)
    (focusedId: string option)
    (selectionMode: TreeSelectionMode)
    (effectiveSelectedIdsRef: IRefValue<Set<string>>)
    (setSelection: Set<string> -> unit)
    (dataSource: TreeDataSource<'T> option)
    isSelectionDisabled
    (isNodeSelectable: TreeItem<'T> -> bool)
    (onError: exn -> unit)
    =
    // Row memoization deliberately ignores callback identity. Keep one current snapshot so
    // unchanged rows still invoke actions with the latest tree state and consumer callbacks.
    let actionStateRef =
        React.useRef<TreeNodeActionState<'T>> {
            DataSource = dataSource
            IsSelectionDisabled = isSelectionDisabled
            IsNodeSelectable = isNodeSelectable
            Lookup = lookup
            FocusedId = focusedId
            SelectionMode = selectionMode
            ScrollToIndex = scrollToIndex
            SetSelection = setSelection
            TreeState = treeState
            OnError = onError
        }

    actionStateRef.current <- {
        DataSource = dataSource
        IsSelectionDisabled = isSelectionDisabled
        IsNodeSelectable = isNodeSelectable
        Lookup = lookup
        FocusedId = focusedId
        SelectionMode = selectionMode
        ScrollToIndex = scrollToIndex
        SetSelection = setSelection
        TreeState = treeState
        OnError = onError
    }

    let currentFocusController (current: TreeNodeActionState<'T>) : TreeFocusController<'T> = {
        Lookup = current.Lookup
        SetActiveId = current.TreeState.SetActiveId
        SetFocusedId = current.TreeState.SetFocusedId
        SetSelectionAnchorId = current.TreeState.SetSelectionAnchorId
        ScrollToIndex = current.ScrollToIndex
        FocusDom = focusNodeAfterRender treeRef
    }

    let loadNode (current: TreeNodeActionState<'T>) (node: TreeItem<'T>) =
        TreeController.loadBranchChildren
            current.DataSource
            activeRequestIdsRef
            loadRequestIdRef
            current.TreeState.LoadedChildren
            current.TreeState.SetLoadedChildren
            current.TreeState.SetExpandedIds
            current.OnError
            node
        |> Promise.start

    React.useEffect (
        (fun () ->
            let current = actionStateRef.current

            lookup.VisibleNodes
            |> Array.iter (fun row ->
                if
                    treeState.ExpandedIds.Contains(TreeItem.getId row.node)
                    && TreeItem.isBranch row.node
                    && (directChildren treeState.LoadedChildren row.node).IsNone
                then
                    loadNode current row.node
            )
        ),
        [|
            box dataSource
            box treeState.ExpandedIds
            box treeState.LoadedChildren
            box lookup.VisibleNodes
            box onError
        |]
    )

    let expandNode (node: TreeItem<'T>) =
        let current = actionStateRef.current

        TreeController.expandNode
            current.DataSource
            activeRequestIdsRef
            loadRequestIdRef
            current.TreeState.LoadedChildren
            current.TreeState.ExpandedIds
            current.TreeState.SetExpandedIds
            current.TreeState.SetLoadedChildren
            current.OnError
            node

    let selectNode (node: TreeItem<'T>) intent =
        let current = actionStateRef.current

        TreeController.selectNode
            current.SelectionMode
            current.IsSelectionDisabled
            current.IsNodeSelectable
            current.Lookup.VisibleNodes
            current.TreeState.SelectionAnchorId
            current.TreeState.SetActiveId
            current.TreeState.SetSelectionAnchorId
            effectiveSelectedIdsRef.current
            current.SetSelection
            node
            intent

    let onNodeKeyDown (node: TreeItem<'T>) (event: KeyboardEvent) =
        if obj.ReferenceEquals(event.target, event.currentTarget) then
            let current = actionStateRef.current
            let focusController = currentFocusController current

            match event.key with
            | kbdEventCode.arrowDown ->
                event.preventDefault ()
                TreeController.focusByDelta focusController current.FocusedId 1
            | kbdEventCode.arrowUp ->
                event.preventDefault ()
                TreeController.focusByDelta focusController current.FocusedId -1
            // "Home" and "End" are KeyboardEvent.key values for jumping to the first or last visible node.
            | kbdEventCode.home ->
                event.preventDefault ()

                focusController.Lookup.VisibleNodes
                |> Array.tryHead
                |> Option.iter (fun row -> TreeController.tryFocusById focusController (TreeItem.getId row.node))
            | kbdEventCode.End ->
                event.preventDefault ()
                TreeController.focusLast focusController
            | kbdEventCode.arrowRight ->
                event.preventDefault ()

                if TreeItem.isBranch node then
                    if current.TreeState.ExpandedIds.Contains(TreeItem.getId node) then
                        focusController.Lookup.VisibleNodes
                        |> Array.tryFind (fun row -> row.parentId = Some(TreeItem.getId node))
                        |> Option.iter (fun row ->
                            TreeController.tryFocusById focusController (TreeItem.getId row.node)
                        )
                    else
                        expandNode node
            | kbdEventCode.arrowLeft ->
                event.preventDefault ()

                TreeController.collapseOrFocusParent
                    focusController
                    current.TreeState.ExpandedIds
                    current.TreeState.SetExpandedIds
                    (TreeItem.getId node)
            | kbdEventCode.enter
            | kbdEventCode.space ->
                event.preventDefault ()

                if event.key = kbdEventCode.enter && TreeItem.isBranch node then
                    expandNode node

                let intent =
                    if event.shiftKey then
                        TreeSelectionIntent.Range
                    elif event.ctrlKey || event.metaKey then
                        TreeSelectionIntent.Toggle
                    else
                        TreeSelectionIntent.Replace

                selectNode node intent
            | _ -> ()

    {
        ExpandNode = expandNode
        SelectNode = selectNode
        OnNodeKeyDown = onNodeKeyDown
    }
