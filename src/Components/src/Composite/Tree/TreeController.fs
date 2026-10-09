module internal Swate.Components.Composite.Tree.TreeController

open Fable.Core
open Feliz
open Swate.Components.Composite.Tree.State
open Swate.Components.Composite.Tree.Types
open Swate.Components.Composite.Tree.Dom

let private isLoadActive (controller: TreeNodeActionState<'T>) cacheKey =
    // The ref guards synchronously before React commits; the cached status drives the spinner
    // and prevents a committed Loading/Loaded entry from being requested again.
    hasActiveOrLoadedChildren cacheKey controller.treeState.loadedChildren
    || controller.trackerRef.current.activeRequestIds |> Map.containsKey cacheKey

let private nextRequestId (trackerRef: IRefValue<TreeLoadTracker>) =
    let requestId = trackerRef.current.nextRequestId + 1
    trackerRef.current.nextRequestId <- requestId
    requestId

let private isRequestCurrent (trackerRef: IRefValue<TreeLoadTracker>) cacheKey requestId =
    trackerRef.current.activeRequestIds |> Map.tryFind cacheKey = Some requestId

let private markLoadStarted (trackerRef: IRefValue<TreeLoadTracker>) cacheKey requestId =
    trackerRef.current.activeRequestIds <- trackerRef.current.activeRequestIds |> Map.add cacheKey requestId

let private markLoadFinished (trackerRef: IRefValue<TreeLoadTracker>) cacheKey requestId =
    if isRequestCurrent trackerRef cacheKey requestId then
        trackerRef.current.activeRequestIds <- trackerRef.current.activeRequestIds |> Map.remove cacheKey

#if FABLE_COMPILER
[<Emit("$0 instanceof Error ? $0 : new Error(typeof $0 === 'string' && $0.length > 0 ? $0 : 'Failed to load tree items')")>]
let private normalizeLoadError (error: obj) : exn = jsNative
#else
let private normalizeLoadError (error: obj) : exn =
    match error with
    | :? exn as exception' -> exception'
    | :? string as message when message.Length > 0 -> System.Exception message
    | _ -> System.Exception "Failed to load tree items"
#endif

let resetCache (current: TreeNodeActionState<'T>) =
    current.trackerRef.current.activeRequestIds <- Map.empty

    current.treeState.setExpandedIds (
        preserveExpansionAfterInvalidateAll current.items current.treeState.loadedChildren current.defaultExpandedIds
    )

    current.treeState.setLoadedChildren (fun _ -> Map.empty)

let loadTreeItems (controller: TreeNodeActionState<'T>) target cacheKey = promise {
    match controller.dataSource with
    | Some source when not (isLoadActive controller cacheKey) ->
        let requestId = nextRequestId controller.trackerRef
        markLoadStarted controller.trackerRef cacheKey requestId
        controller.treeState.setLoadedChildren (withLoading cacheKey)

        try
            try
                let! children = source.getTreeItems target

                if isRequestCurrent controller.trackerRef cacheKey requestId then
                    controller.treeState.setLoadedChildren (withLoaded cacheKey children)
            with ex ->
                if isRequestCurrent controller.trackerRef cacheKey requestId then
                    let error = normalizeLoadError (box ex)
                    controller.treeState.setLoadedChildren (withLoadError cacheKey error.Message)

                    if cacheKey <> rootCacheKey then
                        controller.treeState.setExpandedIds (Set.remove cacheKey)

                    controller.onError error
        finally
            markLoadFinished controller.trackerRef cacheKey requestId
    | _ -> ()
}

let expandNode (treeState: TreeState<'T>) (node: TreeItem<'T>) =
    if TreeItem.isBranch node then
        treeState.setExpandedIds (toggleExpanded (TreeItem.getId node))

let selectionIntent shiftKey ctrlKey metaKey =
    if shiftKey then TreeSelectionIntent.Range
    elif ctrlKey || metaKey then TreeSelectionIntent.Toggle
    else TreeSelectionIntent.Replace

let selectNode (controller: TreeNodeActionState<'T>) (node: TreeItem<'T>) intent =
    let nodeId = TreeItem.getId node
    controller.treeState.setActiveId (Some nodeId)

    if not controller.isSelectionDisabled && controller.isNodeSelectable node then
        let isVisible id =
            controller.lookup.indices |> Map.containsKey id

        let anchorId =
            controller.treeState.selectionAnchorId
            |> Option.filter (fun id -> controller.effectiveSelectedIds.Length > 0 && isVisible id)
            |> Option.orElseWith (fun () -> controller.effectiveSelectedIds |> Array.rev |> Array.tryFind isVisible)
            |> Option.defaultValue nodeId

        let nextSelectedIds =
            match controller.selectionMode, intent with
            | TreeSelectionMode.Multiple, TreeSelectionIntent.Toggle ->
                toggleSelection nodeId controller.effectiveSelectedIds
            | TreeSelectionMode.Multiple, TreeSelectionIntent.Range ->
                rangeSelection anchorId nodeId controller.isNodeSelectable controller.lookup
            | _ -> [| nodeId |]

        controller.setSelection nextSelectedIds

        let nextAnchor =
            if intent = TreeSelectionIntent.Range then
                anchorId
            else
                nodeId

        controller.treeState.setSelectionAnchorId (Some nextAnchor)

let focusNode (focusController: TreeNodeActionState<'T>) index nodeId =
    focusController.treeState.setActiveId (Some nodeId)
    focusController.treeState.setFocusedId (Some nodeId)
    focusController.scrollToIndex index
    focusNodeAfterRender focusController.treeRef nodeId

let tryFocusById (focusController: TreeNodeActionState<'T>) nodeId =
    focusController.lookup.indices
    |> Map.tryFind nodeId
    |> Option.iter (fun index -> focusNode focusController index nodeId)

let focusByDelta (focusController: TreeNodeActionState<'T>) focusedId delta =
    moveFocus delta focusedId focusController.lookup
    |> Option.iter (tryFocusById focusController)

let focusLast (focusController: TreeNodeActionState<'T>) =
    focusController.lookup.visibleNodes
    |> Array.tryLast
    |> Option.iter (fun row -> tryFocusById focusController (TreeItem.getId row.node))

let collapseOrFocusParent (focusController: TreeNodeActionState<'T>) nodeId =
    if focusController.treeState.expandedIds |> Set.contains nodeId then
        focusController.treeState.setExpandedIds (Set.remove nodeId)
    else
        focusController.lookup.parents
        |> Map.tryFind nodeId
        |> Option.iter (tryFocusById focusController)
