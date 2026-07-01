module Swate.Components.Primitive.Tree.TreeController

open Fable.Core.JsInterop
open Swate.Components.Primitive.Tree.State
open Swate.Components.Primitive.Tree.Types

let loadBranchChildren
    (dataSource: TreeDataSource<'T> option)
    enableLazyLoading
    (loadedChildren: Map<string, TreeLoadState<'T>>)
    (setLoadedChildren: (Map<string, TreeLoadState<'T>> -> Map<string, TreeLoadState<'T>>) -> unit)
    (onError: exn -> unit)
    (node: TreeItem<'T>)
    =
    promise {
        match dataSource, enableLazyLoading, NodeState.hasCachedChildren node.id loadedChildren with
        | Some source, true, false ->
            setLoadedChildren (NodeState.withLoading node.id)

            try
                let! children = source.GetTreeItems(Some node)
                setLoadedChildren (NodeState.withLoaded node.id children)
            with ex ->
                setLoadedChildren (NodeState.withLoadError node.id ex.Message)
                onError ex
        | _ -> ()
    }

let expandNode
    (dataSource: TreeDataSource<'T> option)
    enableLazyLoading
    (loadedChildren: Map<string, TreeLoadState<'T>>)
    (expandedIds: Set<string>)
    (setExpandedIds: (Set<string> -> Set<string>) -> unit)
    (setLoadedChildren: (Map<string, TreeLoadState<'T>> -> Map<string, TreeLoadState<'T>>) -> unit)
    (onError: exn -> unit)
    (node: TreeItem<'T>)
    =
    if NodeState.canExpand dataSource node then
        setExpandedIds (NodeState.toggleExpanded node.id)

        if not (expandedIds |> Set.contains node.id) then
            loadBranchChildren dataSource enableLazyLoading loadedChildren setLoadedChildren onError node
            |> Promise.start

let selectNode
    (selectionMode: TreeSelectionMode)
    isSelectionDisabled
    (isNodeSelectable: TreeItem<'T> -> bool)
    (effectiveSelectedIds: Set<string>)
    (setSelection: Set<string> -> unit)
    (node: TreeItem<'T>)
    =
    if not isSelectionDisabled && isNodeSelectable node then
        let nextSelectedIds =
            NodeState.toggleSelection selectionMode node.id effectiveSelectedIds

        setSelection nextSelectedIds

let focusNode (setFocusedId: string option -> unit) (focusDom: string -> unit) nodeId =
    setFocusedId (Some nodeId)
    focusDom nodeId

let tryFocusById (lookup: TreeRowLookup<'T>) (setFocusedId: string option -> unit) (focusDom: string -> unit) nodeId =
    if lookup.Nodes |> Map.containsKey nodeId then
        focusNode setFocusedId focusDom nodeId

let focusByDelta lookup focusedId setFocusedId focusDom delta =
    NodeState.moveFocus delta focusedId lookup.VisibleNodes
    |> Option.iter (tryFocusById lookup setFocusedId focusDom)

let focusFirst lookup setFocusedId focusDom =
    lookup.VisibleNodes
    |> Array.tryHead
    |> Option.iter (fun row -> tryFocusById lookup setFocusedId focusDom row.Node.id)

let focusLast lookup setFocusedId focusDom =
    lookup.VisibleNodes
    |> Array.tryLast
    |> Option.iter (fun row -> tryFocusById lookup setFocusedId focusDom row.Node.id)

let collapseOrFocusParent lookup expandedIds setExpandedIds setFocusedId focusDom nodeId =
    if expandedIds |> Set.contains nodeId then
        setExpandedIds (fun current -> current |> Set.remove nodeId)
    else
        NodeState.parentOf nodeId lookup
        |> Option.iter (tryFocusById lookup setFocusedId focusDom)
