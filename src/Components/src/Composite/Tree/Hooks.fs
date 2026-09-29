module internal Swate.Components.Composite.Tree.Hooks

open Browser.Types
open Fable.Core
open Feliz
open Swate.Components
open Swate.Components.Composite.Tree.Dom
open Swate.Components.Composite.Tree.State
open Swate.Components.Composite.Tree.Types

let private normalizeSelection selectionMode (selectedIds: string[]) =
    let distinctIds = selectedIds |> Array.distinct

    match selectionMode with
    | TreeSelectionMode.Single -> distinctIds |> Array.truncate 1
    | TreeSelectionMode.Multiple -> distinctIds

[<Hook>]
let useTreeState
    (selectionMode: TreeSelectionMode)
    (defaultExpandedIds: string[] option)
    (defaultSelectedIds: string[] option)
    =
    let expandedIds, setExpandedIds =
        React.useStateWithUpdater (defaultExpandedIds |> Option.map Set.ofArray |> Option.defaultValue Set.empty)

    let selectedIds, setSelectedIds =
        React.useStateWithUpdater (
            defaultSelectedIds
            |> Option.defaultValue [||]
            |> normalizeSelection selectionMode
        )

    let activeId, setActiveId = React.useState<string option> None
    let focusedId, setFocusedId = React.useState<string option> None
    let selectionAnchorId, setSelectionAnchorId = React.useState<string option> None

    let loadedChildren, setLoadedChildren =
        React.useStateWithUpdater<Map<string, TreeLoadState<'T>>> Map.empty

    {
        expandedIds = expandedIds
        setExpandedIds = setExpandedIds
        selectedIds = selectedIds
        setSelectedIds = setSelectedIds
        activeId = activeId
        setActiveId = setActiveId
        focusedId = focusedId
        setFocusedId = setFocusedId
        selectionAnchorId = selectionAnchorId
        setSelectionAnchorId = setSelectionAnchorId
        loadedChildren = loadedChildren
        setLoadedChildren = setLoadedChildren
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
        |> Option.defaultValue treeState.selectedIds
        |> normalizeSelection selectionMode

    let setSelection nextSelectedIds =
        let normalizedSelectedIds = normalizeSelection selectionMode nextSelectedIds

        if selectedIds.IsNone then
            treeState.setSelectedIds (fun _ -> normalizedSelectedIds)

        onSelectionChange |> Option.iter (fun handler -> handler normalizedSelectedIds)

    React.useEffect (
        (fun () ->
            if selectedIds.IsNone then
                let normalizedSelectedIds = normalizeSelection selectionMode treeState.selectedIds

                if normalizedSelectedIds <> treeState.selectedIds then
                    treeState.setSelectedIds (fun _ -> normalizedSelectedIds)
                    onSelectionChange |> Option.iter (fun handler -> handler normalizedSelectedIds)
        ),
        [| box selectionMode |]
    )

    effectiveSelectedIds, setSelection

let private sameDataSource left right =
    match left, right with
    | Some left, Some right -> obj.ReferenceEquals(left, right)
    | None, None -> true
    | _ -> false

[<Hook>]
let useTreeNodeActions
    (treeRef: IRefValue<HTMLElement option>)
    (loadTrackerRef: IRefValue<TreeLoadTracker>)
    (actionState: TreeNodeActionState<'T>)
    =
    // Memoized rows deliberately ignore callback identity. Stable handlers read this current
    // snapshot so unchanged rows still see the latest state and consumer callbacks.
    let actionStateRef = React.useRef actionState
    actionStateRef.current <- actionState

    let currentFocusController (current: TreeNodeActionState<'T>) : TreeFocusController<'T> = {
        lookup = current.lookup
        setActiveId = current.treeState.setActiveId
        setFocusedId = current.treeState.setFocusedId
        scrollToIndex = current.scrollToIndex
        focusDom = focusNodeAfterRender treeRef
    }

    let currentLoadController (current: TreeNodeActionState<'T>) : TreeLoadController<'T> = {
        dataSource = current.dataSource
        trackerRef = loadTrackerRef
        treeState = current.treeState
        onError = current.onError
    }

    let previousDataSourceRef = React.useRef actionState.dataSource

    React.useEffect (
        (fun () ->
            let previousDataSource = previousDataSourceRef.current

            if not (sameDataSource previousDataSource actionState.dataSource) then
                loadTrackerRef.current.activeRequestIds <- Map.empty

                actionState.treeState.setExpandedIds (
                    preserveExpansionAfterInvalidateAll actionState.items actionState.treeState.loadedChildren
                )

                actionState.treeState.setLoadedChildren (fun _ -> Map.empty)

            previousDataSourceRef.current <- actionState.dataSource
        ),
        [| box actionState.dataSource |]
    )

    // Expansion is the state transition; this effect is the single place that starts loads.
    React.useEffect (
        (fun () ->
            let current = actionStateRef.current
            let loadController = currentLoadController current

            if
                current.items.Length = 0
                && current.dataSource.IsSome
                && not (current.treeState.loadedChildren |> Map.containsKey rootCacheKey)
            then
                TreeController.loadTreeItems loadController None rootCacheKey |> Promise.start

            current.lookup.visibleNodes
            |> Array.iter (fun row ->
                let nodeId = TreeItem.getId row.node

                if
                    current.treeState.expandedIds.Contains nodeId
                    && TreeItem.isBranch row.node
                    && (directChildren current.treeState.loadedChildren row.node).IsNone
                then
                    TreeController.loadTreeItems loadController (Some row.node) nodeId
                    |> Promise.start
            )
        ),
        [|
            box actionState.dataSource
            box actionState.items
            box actionState.treeState.expandedIds
            box actionState.treeState.loadedChildren
            box actionState.lookup.visibleNodes
        |]
    )

    let tryCurrentNode nodeId =
        actionStateRef.current.lookup.nodes |> Map.tryFind nodeId

    let expandNode nodeId =
        tryCurrentNode nodeId
        |> Option.iter (TreeController.expandNode actionStateRef.current.treeState)

    let selectNode nodeId intent =
        let current = actionStateRef.current

        current.lookup.nodes
        |> Map.tryFind nodeId
        |> Option.iter (fun node ->
            TreeController.selectNode
                {
                    selectionMode = current.selectionMode
                    isSelectionDisabled = current.isSelectionDisabled
                    isNodeSelectable = current.isNodeSelectable
                    lookup = current.lookup
                    effectiveSelectedIds = current.effectiveSelectedIds
                    setSelection = current.setSelection
                    treeState = current.treeState
                }
                node
                intent
        )

    let onNodeKeyDown nodeId (event: KeyboardEvent) =
        if obj.ReferenceEquals(event.target, event.currentTarget) then
            let current = actionStateRef.current
            let focusController = currentFocusController current

            current.lookup.nodes
            |> Map.tryFind nodeId
            |> Option.iter (fun node ->
                match event.key with
                | kbdEventCode.arrowDown ->
                    event.preventDefault ()
                    TreeController.focusByDelta focusController current.focusedId 1
                | kbdEventCode.arrowUp ->
                    event.preventDefault ()
                    TreeController.focusByDelta focusController current.focusedId -1
                | kbdEventCode.home ->
                    event.preventDefault ()

                    focusController.lookup.visibleNodes
                    |> Array.tryHead
                    |> Option.iter (fun row -> TreeController.tryFocusById focusController (TreeItem.getId row.node))
                | kbdEventCode.``end`` ->
                    event.preventDefault ()
                    TreeController.focusLast focusController
                | kbdEventCode.arrowRight ->
                    event.preventDefault ()

                    if TreeItem.isBranch node then
                        if current.treeState.expandedIds.Contains nodeId then
                            current.lookup.visibleNodes
                            |> Array.tryFind (fun row -> row.parentId = Some nodeId)
                            |> Option.iter (fun row ->
                                TreeController.tryFocusById focusController (TreeItem.getId row.node)
                            )
                        else
                            expandNode nodeId
                | kbdEventCode.arrowLeft ->
                    event.preventDefault ()

                    TreeController.collapseOrFocusParent
                        focusController
                        current.treeState.expandedIds
                        current.treeState.setExpandedIds
                        nodeId
                | kbdEventCode.enter
                | kbdEventCode.space ->
                    event.preventDefault ()

                    if event.key = kbdEventCode.enter && TreeItem.isBranch node then
                        expandNode nodeId

                    selectNode nodeId (TreeController.selectionIntent event.shiftKey event.ctrlKey event.metaKey)
                | _ -> ()
            )

    {
        expandNode = expandNode
        selectNode = selectNode
        onNodeKeyDown = onNodeKeyDown
    }
