module Swate.Components.Composite.Tree.TreeController

open Fable.Core.JsInterop
open Feliz
open Swate.Components.Composite.Tree.State
open Swate.Components.Composite.Tree.Types

let private isLoadActive
    (loadingNodeIdsRef: IRefValue<ResizeArray<string>>)
    (invalidatedNodeIdsRef: IRefValue<ResizeArray<string>>)
    nodeId
    loadedChildren
    =
    NodeState.hasActiveOrLoadedChildren nodeId loadedChildren
    || loadingNodeIdsRef.current.Contains nodeId
    || invalidatedNodeIdsRef.current.Contains nodeId

let private markLoadStarted (loadingNodeIdsRef: IRefValue<ResizeArray<string>>) nodeId =
    if not (loadingNodeIdsRef.current.Contains nodeId) then
        loadingNodeIdsRef.current.Add nodeId

let private markLoadFinished (loadingNodeIdsRef: IRefValue<ResizeArray<string>>) nodeId =
    loadingNodeIdsRef.current.Remove nodeId |> ignore

let private isLoadStillPending nodeId loadedChildren =
    match loadedChildren |> Map.tryFind nodeId with
    | Some state -> state.Status = TreeLazyLoadStatus.Loading
    | None -> false

let loadBranchChildren
    (dataSource: TreeDataSource<'T> option)
    enableLazyLoading
    (loadingNodeIdsRef: IRefValue<ResizeArray<string>>)
    (invalidatedNodeIdsRef: IRefValue<ResizeArray<string>>)
    (loadedChildren: Map<string, TreeLoadState<'T>>)
    (setLoadedChildren: (Map<string, TreeLoadState<'T>> -> Map<string, TreeLoadState<'T>>) -> unit)
    (setExpandedIds: (Set<string> -> Set<string>) -> unit)
    (onError: exn -> unit)
    (node: TreeItem<'T>)
    =
    promise {
        match
            dataSource, enableLazyLoading, isLoadActive loadingNodeIdsRef invalidatedNodeIdsRef node.id loadedChildren
        with
        | Some source, true, false ->
            markLoadStarted loadingNodeIdsRef node.id

            setLoadedChildren (fun current ->
                if NodeState.hasActiveOrLoadedChildren node.id current then
                    current
                else
                    NodeState.withLoading node.id current
            )

            try
                let! children = source.GetTreeItems(Some node)

                if not (invalidatedNodeIdsRef.current.Remove node.id) then
                    setLoadedChildren (fun current ->
                        if isLoadStillPending node.id current then
                            NodeState.withLoaded node.id children current
                        else
                            current
                    )

                markLoadFinished loadingNodeIdsRef node.id
            with ex ->
                if not (invalidatedNodeIdsRef.current.Remove node.id) then
                    setLoadedChildren (fun current ->
                        if isLoadStillPending node.id current then
                            NodeState.withLoadError node.id ex.Message current
                        else
                            current
                    )

                    setExpandedIds (fun current -> current |> Set.remove node.id)
                    onError ex

                markLoadFinished loadingNodeIdsRef node.id
        | _ -> ()
    }

let expandNode
    (dataSource: TreeDataSource<'T> option)
    enableLazyLoading
    (loadingNodeIdsRef: IRefValue<ResizeArray<string>>)
    (invalidatedNodeIdsRef: IRefValue<ResizeArray<string>>)
    (loadedChildren: Map<string, TreeLoadState<'T>>)
    (expandedIds: Set<string>)
    (setExpandedIds: (Set<string> -> Set<string>) -> unit)
    (setLoadedChildren: (Map<string, TreeLoadState<'T>> -> Map<string, TreeLoadState<'T>>) -> unit)
    (onError: exn -> unit)
    (node: TreeItem<'T>)
    =
    if NodeState.canExpand dataSource loadedChildren node then
        setExpandedIds (NodeState.toggleExpanded node.id)

        if not (expandedIds |> Set.contains node.id) then
            loadBranchChildren
                dataSource
                enableLazyLoading
                loadingNodeIdsRef
                invalidatedNodeIdsRef
                loadedChildren
                setLoadedChildren
                setExpandedIds
                onError
                node
            |> Promise.start

let selectNode
    (selectionMode: TreeSelectionMode)
    isSelectionDisabled
    (isNodeSelectable: TreeItem<'T> -> bool)
    (effectiveSelectedIds: Set<string>)
    (setSelection: Set<string> -> unit)
    (node: TreeItem<'T>)
    extendSelection
    =
    if not isSelectionDisabled && isNodeSelectable node then
        let nextSelectedIds =
            NodeState.nextSelection selectionMode extendSelection node.id effectiveSelectedIds

        setSelection nextSelectedIds

let focusNode
    (setFocusedId: string option -> unit)
    (scrollToIndex: int -> unit)
    (focusDom: string -> unit)
    index
    nodeId
    =
    setFocusedId (Some nodeId)
    scrollToIndex index
    focusDom nodeId

let tryFocusById (lookup: TreeRowLookup<'T>) (setFocusedId: string option -> unit) scrollToIndex focusDom nodeId =
    lookup.VisibleNodes
    |> Array.tryFindIndex (fun row -> row.Node.id = nodeId)
    |> Option.iter (fun index -> focusNode setFocusedId scrollToIndex focusDom index nodeId)

let focusByDelta lookup focusedId setFocusedId scrollToIndex focusDom delta =
    NodeState.moveFocus delta focusedId lookup.VisibleNodes
    |> Option.iter (tryFocusById lookup setFocusedId scrollToIndex focusDom)

let focusFirst lookup setFocusedId scrollToIndex focusDom =
    lookup.VisibleNodes
    |> Array.tryHead
    |> Option.iter (fun row -> tryFocusById lookup setFocusedId scrollToIndex focusDom row.Node.id)

let focusLast lookup setFocusedId scrollToIndex focusDom =
    lookup.VisibleNodes
    |> Array.tryLast
    |> Option.iter (fun row -> tryFocusById lookup setFocusedId scrollToIndex focusDom row.Node.id)

let focusFirstChild lookup setFocusedId scrollToIndex focusDom nodeId =
    lookup.VisibleNodes
    |> Array.tryFind (fun row -> row.ParentId = Some nodeId)
    |> Option.iter (fun row -> tryFocusById lookup setFocusedId scrollToIndex focusDom row.Node.id)

let collapseOrFocusParent lookup expandedIds setExpandedIds setFocusedId scrollToIndex focusDom nodeId =
    if expandedIds |> Set.contains nodeId then
        setExpandedIds (fun current -> current |> Set.remove nodeId)
    else
        NodeState.parentOf nodeId lookup
        |> Option.iter (tryFocusById lookup setFocusedId scrollToIndex focusDom)
