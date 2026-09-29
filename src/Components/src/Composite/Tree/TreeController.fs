module internal Swate.Components.Composite.Tree.TreeController

open Fable.Core
open Feliz
open Swate.Components.Composite.Tree.State
open Swate.Components.Composite.Tree.Types

let private isLoadActive (controller: TreeLoadController<'T>) cacheKey =
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

let loadTreeItems (controller: TreeLoadController<'T>) target cacheKey = promise {
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
                    controller.treeState.setLoadedChildren (withLoadError cacheKey ex.Message)

                    if cacheKey <> rootCacheKey then
                        controller.treeState.setExpandedIds (Set.remove cacheKey)

                    controller.onError ex
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

let selectNode (controller: TreeSelectionController<'T>) (node: TreeItem<'T>) intent =
    let nodeId = TreeItem.getId node
    controller.treeState.setActiveId (Some nodeId)

    if not controller.isSelectionDisabled && controller.isNodeSelectable node then
        let anchorId =
            controller.treeState.selectionAnchorId
            |> Option.orElseWith (fun () -> controller.effectiveSelectedIds |> Array.tryLast)
            |> Option.defaultValue nodeId

        let nextSelectedIds =
            match controller.selectionMode, intent with
            | TreeSelectionMode.Multiple, TreeSelectionIntent.Toggle ->
                toggleSelection nodeId controller.effectiveSelectedIds
            | TreeSelectionMode.Multiple, TreeSelectionIntent.Range ->
                rangeSelection anchorId nodeId controller.isNodeSelectable controller.lookup.visibleNodes
            | _ -> [| nodeId |]

        controller.setSelection nextSelectedIds

        match intent, controller.treeState.selectionAnchorId with
        | TreeSelectionIntent.Range, Some _ -> ()
        | _ -> controller.treeState.setSelectionAnchorId (Some nodeId)

let focusNode (focusController: TreeFocusController<'T>) index nodeId =
    focusController.setActiveId (Some nodeId)
    focusController.setFocusedId (Some nodeId)
    focusController.scrollToIndex index
    focusController.focusDom nodeId

let tryFocusById (focusController: TreeFocusController<'T>) nodeId =
    focusController.lookup.indices
    |> Map.tryFind nodeId
    |> Option.iter (fun index -> focusNode focusController index nodeId)

let focusByDelta (focusController: TreeFocusController<'T>) focusedId delta =
    moveFocus delta focusedId focusController.lookup
    |> Option.iter (tryFocusById focusController)

let focusLast (focusController: TreeFocusController<'T>) =
    focusController.lookup.visibleNodes
    |> Array.tryLast
    |> Option.iter (fun row -> tryFocusById focusController (TreeItem.getId row.node))

let collapseOrFocusParent (focusController: TreeFocusController<'T>) expandedIds setExpandedIds nodeId =
    if expandedIds |> Set.contains nodeId then
        setExpandedIds (Set.remove nodeId)
    else
        focusController.lookup.parents
        |> Map.tryFind nodeId
        |> Option.iter (tryFocusById focusController)
