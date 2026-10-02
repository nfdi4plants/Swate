module internal Swate.Components.Composite.Tree.Hooks

open Browser.Types
open Fable.Core
open Feliz
open Swate.Components
open Swate.Components.Composite.Tree.Dom
open Swate.Components.Composite.Tree.State
open Swate.Components.Composite.Tree.Types

let private normalizeSelection selectionMode (selectedIds: string[]) =
    match selectionMode with
    | TreeSelectionMode.Single -> selectedIds |> Array.tryLast |> Option.toArray
    | TreeSelectionMode.Multiple -> selectedIds |> Array.distinct

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

    let previousMode, setPreviousMode = React.useState selectionMode
    let normalizationNotification = React.useRef<string[] option> None

    // Adjust this component's own state during rendering, before any child sees the old mode.
    // Only the external notification belongs in an effect, after the update is committed.
    if previousMode <> selectionMode then
        setPreviousMode selectionMode

        if selectedIds.IsNone && effectiveSelectedIds <> treeState.selectedIds then
            treeState.setSelectedIds (fun _ -> effectiveSelectedIds)
            normalizationNotification.current <- Some effectiveSelectedIds

    if effectiveSelectedIds.Length = 0 && treeState.selectionAnchorId.IsSome then
        treeState.setSelectionAnchorId None

    let setSelection nextSelectedIds =
        let normalizedSelectedIds = normalizeSelection selectionMode nextSelectedIds

        if selectedIds.IsNone then
            treeState.setSelectedIds (fun _ -> normalizedSelectedIds)

        onSelectionChange |> Option.iter (fun handler -> handler normalizedSelectedIds)

    React.useEffect (
        (fun () ->
            let notification = normalizationNotification.current
            normalizationNotification.current <- None

            notification
            |> Option.iter (fun ids -> onSelectionChange |> Option.iter (fun handler -> handler ids))
        ),
        [| box selectionMode |]
    )

    effectiveSelectedIds, setSelection

let private sameDataSource (left: TreeDataSource<'T> option) (right: TreeDataSource<'T> option) =
    match left, right with
    | Some left, Some right ->
        match left.cacheKey, right.cacheKey with
        | Some leftKey, Some rightKey -> leftKey = rightKey
        | _ -> obj.ReferenceEquals(left.getTreeItems, right.getTreeItems)
    | None, None -> true
    | _ -> false

[<Hook>]
let useTreeNodeActions (actionState: TreeNodeActionState<'T>) =
    // Stable event handlers read the latest snapshot; renderer identities remain part of row equality.
    let actionStateRef = React.useRef actionState
    actionStateRef.current <- actionState

    let previousDataSourceRef = React.useRef actionState.dataSource

    React.useEffect (
        (fun () ->
            let previousDataSource = previousDataSourceRef.current

            if not (sameDataSource previousDataSource actionState.dataSource) then
                TreeController.resetCache actionStateRef.current

            previousDataSourceRef.current <- actionState.dataSource
        ),
        [| box actionState.dataSource |]
    )

    // Expansion is the state transition; this effect is the single place that starts loads.
    React.useEffect (
        (fun () ->
            let current = actionStateRef.current

            if
                current.items.Length = 0
                && current.dataSource.IsSome
                && not (current.treeState.loadedChildren |> Map.containsKey rootCacheKey)
            then
                TreeController.loadTreeItems current None rootCacheKey |> Promise.start

            current.lookup.visibleNodes
            |> Array.iter (fun row ->
                let nodeId = TreeItem.getId row.node

                if
                    current.treeState.expandedIds.Contains nodeId
                    && TreeItem.isBranch row.node
                    && (directChildren current.treeState.loadedChildren row.node).IsNone
                then
                    TreeController.loadTreeItems current (Some row.node) nodeId |> Promise.start
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
        |> Option.iter (fun node -> TreeController.selectNode current node intent)

    let onNodeKeyDown nodeId (event: KeyboardEvent) =
        if obj.ReferenceEquals(event.target, event.currentTarget) then
            let current = actionStateRef.current

            current.lookup.nodes
            |> Map.tryFind nodeId
            |> Option.iter (fun node ->
                match event.code with
                | kbdEventCode.arrowDown ->
                    event.preventDefault ()
                    TreeController.focusByDelta current current.focusedId 1
                | kbdEventCode.arrowUp ->
                    event.preventDefault ()
                    TreeController.focusByDelta current current.focusedId -1
                | kbdEventCode.home ->
                    event.preventDefault ()

                    current.lookup.visibleNodes
                    |> Array.tryHead
                    |> Option.iter (fun row -> TreeController.tryFocusById current (TreeItem.getId row.node))
                | kbdEventCode.``end`` ->
                    event.preventDefault ()
                    TreeController.focusLast current
                | kbdEventCode.arrowRight ->
                    event.preventDefault ()

                    if TreeItem.isBranch node then
                        if current.treeState.expandedIds.Contains nodeId then
                            current.lookup.firstChildren
                            |> Map.tryFind nodeId
                            |> Option.iter (TreeController.tryFocusById current)
                        else
                            expandNode nodeId
                | kbdEventCode.arrowLeft ->
                    event.preventDefault ()

                    TreeController.collapseOrFocusParent current nodeId
                | kbdEventCode.enter
                | kbdEventCode.space ->
                    event.preventDefault ()

                    if event.code = kbdEventCode.enter && TreeItem.isBranch node then
                        expandNode nodeId

                    selectNode nodeId (TreeController.selectionIntent event.shiftKey event.ctrlKey event.metaKey)
                | _ -> ()
            )

    let invalidateNode nodeId =
        let current = actionStateRef.current

        let invalidatedIds =
            invalidatedSubtreeIds current.items current.treeState.loadedChildren nodeId

        if not invalidatedIds.IsEmpty then
            current.trackerRef.current.activeRequestIds <-
                current.trackerRef.current.activeRequestIds
                |> Map.filter (fun cacheKey _ -> not (invalidatedIds.Contains cacheKey))

            current.treeState.setLoadedChildren (removeCacheEntries invalidatedIds)
            current.treeState.setExpandedIds (removeDescendantExpansions nodeId invalidatedIds)

    let invalidateAll () =
        TreeController.resetCache actionStateRef.current

    let actions =
        React.useMemo (
            (fun () -> {
                expandNode = expandNode
                selectNode = selectNode
                onNodeKeyDown = onNodeKeyDown
                invalidateNode = invalidateNode
                invalidateAll = invalidateAll
            }),
            [||]
        )

    actionStateRef, actions

module private FocusHistory =
    type State<'T> = {
        lookup: TreeRowLookup<'T>
        owner: HTMLElement option
    }

/// DOM focus needs a committed handoff when its owning row is removed by data updates.
[<Hook>]
let useFocusRecovery (currentRef: IRefValue<TreeNodeActionState<'T>>) =
    let history =
        React.useRef<FocusHistory.State<'T>> {
            lookup = currentRef.current.lookup
            owner = None
        }

    React.useLayoutEffect (
        (fun () ->
            let current = currentRef.current
            let previous = history.current

            match current.treeRef.current, previous.owner with
            | Some root, Some owner when not (root.contains owner) || obj.ReferenceEquals(root, owner) ->
                let activeElement = Browser.Dom.document.activeElement

                let mayRestore =
                    obj.ReferenceEquals(activeElement, Browser.Dom.document.body)
                    || obj.ReferenceEquals(activeElement, root)

                if mayRestore then
                    let rec visibleAncestor id =
                        if current.lookup.indices |> Map.containsKey id then
                            Some id
                        else
                            previous.lookup.parents |> Map.tryFind id |> Option.bind visibleAncestor

                    let fallback =
                        owner.getAttribute "data-tree-node-id"
                        |> Option.ofObj
                        |> Option.bind visibleAncestor
                        |> Option.orElseWith (fun () ->
                            activeOrFirst current.treeState.activeId current.effectiveSelectedIds current.lookup
                        )

                    // Keep focus within the Tree while the replacement virtual row or lazy root mounts.
                    root.focus ()

                    match fallback with
                    | Some nodeId -> TreeController.tryFocusById current nodeId
                    | None -> current.treeState.setFocusedId None
                else
                    history.current <- { previous with owner = None }
            | _ -> ()

            history.current <- {
                history.current with
                    lookup = current.lookup
            }
        ),
        [| box currentRef.current.lookup |]
    )

    let onFocus (event: FocusEvent) =
        history.current <- {
            history.current with
                owner = Some(event.target :?> HTMLElement)
        }

    let onBlur (event: FocusEvent) =
        if focusMovedOutsideTree event then
            let removedOwner =
                match currentRef.current.treeRef.current, history.current.owner with
                | Some root, Some owner -> not (root.contains owner)
                | _ -> false

            if not (isNull (box event.relatedTarget) && removedOwner) then
                history.current <- { history.current with owner = None }
                currentRef.current.treeState.setFocusedId None

    onFocus, onBlur
