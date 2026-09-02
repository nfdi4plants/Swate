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
    =
    let config = useTreeCtx<'T> ()

    let configRef = React.useRef config
    let treeStateRef = React.useRef treeState
    let lookupRef = React.useRef lookup
    let focusedIdRef = React.useRef focusedId
    let selectionModeRef = React.useRef selectionMode
    let scrollToIndexRef = React.useRef<int -> unit> scrollToIndex
    let setSelectionRef = React.useRef<Set<string> -> unit> setSelection

    configRef.current <- config
    treeStateRef.current <- treeState
    lookupRef.current <- lookup
    focusedIdRef.current <- focusedId
    selectionModeRef.current <- selectionMode
    scrollToIndexRef.current <- scrollToIndex
    setSelectionRef.current <- setSelection

    let currentFocusController () : TreeFocusController<'T> = {
        Lookup = lookupRef.current
        SetActiveId = treeStateRef.current.SetActiveId
        SetFocusedId = treeStateRef.current.SetFocusedId
        SetSelectionAnchorId = treeStateRef.current.SetSelectionAnchorId
        ScrollToIndex = scrollToIndexRef.current
        FocusDom = focusNodeAfterRender treeRef
    }

    let loadNode (node: TreeItem<'T>) =
        let currentConfig = configRef.current
        let currentTreeState = treeStateRef.current

        TreeController.loadBranchChildren
            currentConfig.DataSource
            activeRequestIdsRef
            loadRequestIdRef
            currentTreeState.LoadedChildren
            currentTreeState.SetLoadedChildren
            currentTreeState.SetExpandedIds
            currentConfig.OnError
            node
        |> Promise.start

    React.useEffect (
        (fun () ->
            lookup.VisibleNodes
            |> Array.iter (fun row ->
                if
                    treeState.ExpandedIds.Contains(TreeItem.id row.node)
                    && canExpand row.node
                    && (directChildren treeState.LoadedChildren row.node).IsNone
                then
                    loadNode row.node
            )
        ),
        [|
            box config.DataSource
            box treeState.ExpandedIds
            box treeState.LoadedChildren
            box lookup.VisibleNodes
        |]
    )

    let expandNode (node: TreeItem<'T>) =
        let currentConfig = configRef.current
        let currentTreeState = treeStateRef.current

        TreeController.expandNode
            currentConfig.DataSource
            activeRequestIdsRef
            loadRequestIdRef
            currentTreeState.LoadedChildren
            currentTreeState.ExpandedIds
            currentTreeState.SetExpandedIds
            currentTreeState.SetLoadedChildren
            currentConfig.OnError
            node

    let selectNode (node: TreeItem<'T>) intent =
        let currentConfig = configRef.current
        let currentTreeState = treeStateRef.current

        TreeController.selectNode
            selectionModeRef.current
            currentConfig.SelectionDisabled
            currentConfig.IsNodeSelectable
            lookupRef.current.VisibleNodes
            currentTreeState.SelectionAnchorId
            currentTreeState.SetActiveId
            currentTreeState.SetSelectionAnchorId
            effectiveSelectedIdsRef.current
            setSelectionRef.current
            node
            intent

    let onNodeKeyDown (node: TreeItem<'T>) (event: KeyboardEvent) =
        if obj.ReferenceEquals(event.target, event.currentTarget) then
            let currentConfig = configRef.current
            let currentTreeState = treeStateRef.current
            let focusController = currentFocusController ()

            match event.key with
            | kbdEventCode.arrowDown ->
                event.preventDefault ()
                TreeController.focusByDelta focusController focusedIdRef.current 1
            | kbdEventCode.arrowUp ->
                event.preventDefault ()
                TreeController.focusByDelta focusController focusedIdRef.current -1
            // "Home" and "End" are KeyboardEvent.key values for jumping to the first or last visible node.
            | kbdEventCode.home ->
                event.preventDefault ()
                TreeController.focusFirst focusController
            | kbdEventCode.End ->
                event.preventDefault ()
                TreeController.focusLast focusController
            | kbdEventCode.arrowRight ->
                event.preventDefault ()

                if canExpand node then
                    if currentTreeState.ExpandedIds.Contains(TreeItem.id node) then
                        TreeController.focusFirstChild focusController (TreeItem.id node)
                    else
                        expandNode node
            | kbdEventCode.arrowLeft ->
                event.preventDefault ()

                TreeController.collapseOrFocusParent
                    focusController
                    currentTreeState.ExpandedIds
                    currentTreeState.SetExpandedIds
                    (TreeItem.id node)
            | kbdEventCode.enter
            | kbdEventCode.space ->
                event.preventDefault ()

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
