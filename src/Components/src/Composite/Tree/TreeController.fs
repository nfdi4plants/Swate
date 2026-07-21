module Swate.Components.Composite.Tree.TreeController

open Fable.Core.JsInterop
open Feliz
open Swate.Components.Composite.Tree.State
open Swate.Components.Composite.Tree.Types

let private isLoadActive (loadingNodeIdsRef: IRefValue<ResizeArray<string>>) nodeId loadedChildren =
    hasActiveOrLoadedChildren nodeId loadedChildren
    || loadingNodeIdsRef.current.Contains nodeId

let private markLoadStarted (loadingNodeIdsRef: IRefValue<ResizeArray<string>>) nodeId =
    if not (loadingNodeIdsRef.current.Contains nodeId) then
        loadingNodeIdsRef.current.Add nodeId

let private markLoadFinished (loadingNodeIdsRef: IRefValue<ResizeArray<string>>) nodeId =
    loadingNodeIdsRef.current.Remove nodeId |> ignore

let private nextRequestId (loadRequestIdRef: IRefValue<int>) =
    loadRequestIdRef.current <- loadRequestIdRef.current + 1
    loadRequestIdRef.current

let private isLoadStillPending nodeId requestId loadedChildren =
    match loadedChildren |> Map.tryFind nodeId with
    | Some state -> state.Status = TreeLazyLoadStatus.Loading && state.RequestId = Some requestId
    | None -> false

let loadBranchChildren
    (dataSource: TreeDataSource<'T> option)
    enableLazyLoading
    (loadingNodeIdsRef: IRefValue<ResizeArray<string>>)
    (loadRequestIdRef: IRefValue<int>)
    (loadedChildren: Map<string, TreeLoadState<'T>>)
    (setLoadedChildren: (Map<string, TreeLoadState<'T>> -> Map<string, TreeLoadState<'T>>) -> unit)
    (setExpandedIds: (Set<string> -> Set<string>) -> unit)
    (onError: exn -> unit)
    (node: TreeItem<'T>)
    =
    promise {
        match
            dataSource,
            enableLazyLoading,
            directChildren loadedChildren node,
            isLoadActive loadingNodeIdsRef node.id loadedChildren
        with
        | Some source, true, None, false ->
            let requestId = nextRequestId loadRequestIdRef

            markLoadStarted loadingNodeIdsRef node.id

            setLoadedChildren (fun current ->
                if
                    hasActiveOrLoadedChildren node.id current
                    || directChildren current node |> Option.isSome
                then
                    current
                else
                    withLoading node.id requestId current
            )

            try
                try
                    let! children = source.GetTreeItems(Some node)

                    setLoadedChildren (fun current ->
                        if isLoadStillPending node.id requestId current then
                            withLoaded node.id children current
                        else
                            current
                    )
                with ex ->
                    setLoadedChildren (fun current ->
                        if isLoadStillPending node.id requestId current then
                            withLoadError node.id ex.Message current
                        else
                            current
                    )

                    setExpandedIds (fun current -> current |> Set.remove node.id)
                    onError ex
            finally
                markLoadFinished loadingNodeIdsRef node.id
        | _ -> ()
    }

let expandNode
    (dataSource: TreeDataSource<'T> option)
    enableLazyLoading
    (loadingNodeIdsRef: IRefValue<ResizeArray<string>>)
    (loadRequestIdRef: IRefValue<int>)
    (loadedChildren: Map<string, TreeLoadState<'T>>)
    (expandedIds: Set<string>)
    (setExpandedIds: (Set<string> -> Set<string>) -> unit)
    (setLoadedChildren: (Map<string, TreeLoadState<'T>> -> Map<string, TreeLoadState<'T>>) -> unit)
    (onError: exn -> unit)
    (node: TreeItem<'T>)
    =
    if canExpand dataSource loadedChildren node then
        setExpandedIds (toggleExpanded node.id)

        if not (expandedIds |> Set.contains node.id) then
            loadBranchChildren
                dataSource
                enableLazyLoading
                loadingNodeIdsRef
                loadRequestIdRef
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
            nextSelection selectionMode extendSelection node.id effectiveSelectedIds

        setSelection nextSelectedIds

let focusNode (focusController: TreeFocusController<'T>) index nodeId =
    focusController.SetFocusedId(Some nodeId)
    focusController.ScrollToIndex index
    focusController.FocusDom nodeId

let tryFocusById (focusController: TreeFocusController<'T>) nodeId =
    focusController.Lookup.VisibleNodes
    |> Array.tryFindIndex (fun row -> row.Node.id = nodeId)
    |> Option.iter (fun index -> focusNode focusController index nodeId)

let focusByDelta focusController focusedId delta =
    moveFocus delta focusedId focusController.Lookup.VisibleNodes
    |> Option.iter (tryFocusById focusController)

let focusFirst focusController =
    focusController.Lookup.VisibleNodes
    |> Array.tryHead
    |> Option.iter (fun row -> tryFocusById focusController row.Node.id)

let focusLast focusController =
    focusController.Lookup.VisibleNodes
    |> Array.tryLast
    |> Option.iter (fun row -> tryFocusById focusController row.Node.id)

let focusFirstChild focusController nodeId =
    focusController.Lookup.VisibleNodes
    |> Array.tryFind (fun row -> row.ParentId = Some nodeId)
    |> Option.iter (fun row -> tryFocusById focusController row.Node.id)

let collapseOrFocusParent focusController expandedIds setExpandedIds nodeId =
    if expandedIds |> Set.contains nodeId then
        setExpandedIds (fun current -> current |> Set.remove nodeId)
    else
        parentOf nodeId focusController.Lookup
        |> Option.iter (tryFocusById focusController)
