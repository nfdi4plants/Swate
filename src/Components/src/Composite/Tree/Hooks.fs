module Swate.Components.Composite.Tree.Hooks

open Browser.Types
open Fable.Core
open Feliz
open Swate.Components
open Swate.Components.Composite.Tree.Context
open Swate.Components.Composite.Tree.Dom
open Swate.Components.Composite.Tree.State
open Swate.Components.Composite.Tree.Types

let private toSet (values: string[] option) =
    values |> Option.map Set.ofArray |> Option.defaultValue Set.empty

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
        React.useStateWithUpdater (toSet defaultExpandedIds)

    let selectedIds, setSelectedIds =
        React.useStateWithUpdater (toSelectionSet selectionMode defaultSelectedIds)

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
                                setLoadedChildren (invalidateNode nodeId)
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
let useTreeNodeActions
    (treeRef: IRefValue<HTMLElement option>)
    scrollToIndex
    (activeRequestIdsRef: IRefValue<Map<string, int>>)
    (loadRequestIdRef: IRefValue<int>)
    (treeState: TreeState<'T>)
    (lookup: TreeRowLookup<'T>)
    focusedId
    selectionMode
    effectiveSelectedIds
    setSelection
    =
    let config = useTreeCtx<'T> ()

    let focusController: TreeFocusController<'T> = {
        Lookup = lookup
        SetFocusedId = treeState.SetFocusedId
        ScrollToIndex = scrollToIndex
        FocusDom = focusNodeAfterRender treeRef
    }

    let loadNode (node: TreeItem<'T>) =
        TreeController.loadBranchChildren
            config.DataSource
            config.EnableLazyLoading
            activeRequestIdsRef
            loadRequestIdRef
            treeState.LoadedChildren
            treeState.SetLoadedChildren
            treeState.SetExpandedIds
            config.OnError
            node
        |> Promise.start

    React.useEffect (
        (fun () ->
            lookup.VisibleNodes
            |> Array.iter (fun row ->
                if
                    treeState.ExpandedIds.Contains row.node.id
                    && canExpand config.DataSource config.EnableLazyLoading treeState.LoadedChildren row.node
                    && (directChildren treeState.LoadedChildren row.node).IsNone
                then
                    loadNode row.node
            )
        ),
        [|
            box config.DataSource
            box config.EnableLazyLoading
            box treeState.ExpandedIds
            box treeState.LoadedChildren
            box lookup.VisibleNodes
        |]
    )

    let expandNode (node: TreeItem<'T>) =
        TreeController.expandNode
            config.DataSource
            config.EnableLazyLoading
            activeRequestIdsRef
            loadRequestIdRef
            treeState.LoadedChildren
            treeState.ExpandedIds
            treeState.SetExpandedIds
            treeState.SetLoadedChildren
            config.OnError
            node

    let selectNode (node: TreeItem<'T>) extendSelection =
        TreeController.selectNode
            selectionMode
            config.SelectionDisabled
            config.IsNodeSelectable
            effectiveSelectedIds
            setSelection
            node
            extendSelection

    let onNodeKeyDown (node: TreeItem<'T>) (event: KeyboardEvent) =
        if obj.ReferenceEquals(event.target, event.currentTarget) then
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

                if canExpand config.DataSource config.EnableLazyLoading treeState.LoadedChildren node then
                    if treeState.ExpandedIds.Contains node.id then
                        TreeController.focusFirstChild focusController node.id
                    else
                        expandNode node
            | kbdEventCode.arrowLeft ->
                event.preventDefault ()

                TreeController.collapseOrFocusParent
                    focusController
                    treeState.ExpandedIds
                    treeState.SetExpandedIds
                    node.id
            | kbdEventCode.enter
            | kbdEventCode.space ->
                event.preventDefault ()

                if canExpand config.DataSource config.EnableLazyLoading treeState.LoadedChildren node then
                    expandNode node

                selectNode node (event.shiftKey || event.ctrlKey || event.metaKey)
            | _ -> ()

    {
        ExpandNode = expandNode
        SelectNode = selectNode
        OnNodeKeyDown = onNodeKeyDown
    }
