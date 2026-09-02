module Swate.Components.Composite.Tree.State

open Swate.Components.Composite.Tree.Types

let emptyLoadState = {
    Status = TreeLazyLoadStatus.Idle
    Children = None
    Error = None
    RequestId = None
}

let loadStateFor nodeId loadedChildren =
    loadedChildren |> Map.tryFind nodeId |> Option.defaultValue emptyLoadState

let hasActiveOrLoadedChildren nodeId loadedChildren =
    match loadedChildren |> Map.tryFind nodeId with
    | Some state ->
        match state.Status with
        | TreeLazyLoadStatus.Loading
        | TreeLazyLoadStatus.Loaded -> true
        | TreeLazyLoadStatus.Idle
        | TreeLazyLoadStatus.Error -> false
    | None -> false

let withLoading nodeId requestId loadedChildren =
    loadedChildren
    |> Map.add nodeId {
        emptyLoadState with
            Status = TreeLazyLoadStatus.Loading
            RequestId = Some requestId
    }

let withLoaded nodeId children loadedChildren =
    loadedChildren
    |> Map.add nodeId {
        Status = TreeLazyLoadStatus.Loaded
        Children = Some children
        Error = None
        RequestId = None
    }

let withLoadError nodeId message loadedChildren =
    loadedChildren
    |> Map.add nodeId {
        Status = TreeLazyLoadStatus.Error
        Children = None
        Error = Some message
        RequestId = None
    }

let invalidateNode nodeId loadedChildren = loadedChildren |> Map.remove nodeId

let directChildren (loadedChildren: Map<string, TreeLoadState<'T>>) (node: TreeItem<'T>) =
    match node with
    | TreeItem.Leaf _ -> None
    | TreeItem.Branch _ ->
        match loadedChildren |> Map.tryFind (TreeItem.id node) |> Option.bind _.Children with
        | Some children -> Some children
        | None -> TreeItem.children node

let canExpand (node: TreeItem<'T>) = TreeItem.isBranch node

let flattenVisible loadedChildren expandedIds items =
    let nodes = ResizeArray<TreeVisibleNode<'T>>()
    let nodeMap = ResizeArray<string * TreeItem<'T>>()
    let parentMap = ResizeArray<string * string>()

    let rec loop ancestors parentId depth (items: TreeItem<'T>[]) =
        for index = 0 to items.Length - 1 do
            let item = items.[index]
            let itemId = TreeItem.id item

            if not (ancestors |> Set.contains itemId) then
                nodes.Add {
                    node = item
                    depth = depth
                    parentId = parentId
                    posInSet = index + 1
                    setSize = items.Length
                }

                nodeMap.Add(itemId, item)
                parentId |> Option.iter (fun parentId -> parentMap.Add(itemId, parentId))

                if TreeItem.isBranch item && expandedIds |> Set.contains itemId then
                    match directChildren loadedChildren item with
                    | Some children -> loop (ancestors |> Set.add itemId) (Some itemId) (depth + 1) children
                    | None -> ()

    loop Set.empty None 0 items

    {
        Nodes = nodeMap |> Seq.distinctBy fst |> Map.ofSeq
        Parents = parentMap |> Seq.distinctBy fst |> Map.ofSeq
        VisibleNodes = nodes.ToArray()
    }

let parentOf nodeId lookup = lookup.Parents |> Map.tryFind nodeId

let toggleExpanded nodeId expandedIds =
    if expandedIds |> Set.contains nodeId then
        expandedIds |> Set.remove nodeId
    else
        expandedIds |> Set.add nodeId

let private selectSingle nodeId selectedIds =
    if selectedIds |> Set.contains nodeId then
        selectedIds
    else
        Set.singleton nodeId

let toggleSelection mode nodeId selectedIds =
    match mode with
    | TreeSelectionMode.Single -> selectSingle nodeId selectedIds
    | TreeSelectionMode.Multiple ->
        if selectedIds |> Set.contains nodeId then
            selectedIds |> Set.remove nodeId
        else
            selectedIds |> Set.add nodeId

let rangeSelection anchorId targetId isNodeSelectable visibleNodes =
    let tryIndex nodeId =
        visibleNodes |> Array.tryFindIndex (fun row -> TreeItem.id row.node = nodeId)

    match tryIndex anchorId, tryIndex targetId with
    | Some anchorIndex, Some targetIndex ->
        let firstIndex = min anchorIndex targetIndex
        let lastIndex = max anchorIndex targetIndex

        visibleNodes.[firstIndex..lastIndex]
        |> Array.choose (fun row ->
            if isNodeSelectable row.node then
                Some(TreeItem.id row.node)
            else
                None
        )
        |> Set.ofArray
    | _ -> Set.singleton targetId

let activeOrFirst activeId selectedIds visibleNodes =
    let isVisible id =
        visibleNodes |> Array.exists (fun row -> TreeItem.id row.node = id)

    activeId
    |> Option.filter isVisible
    |> Option.orElseWith (fun () -> selectedIds |> Seq.tryFind isVisible)
    |> Option.orElseWith (fun () -> visibleNodes |> Array.tryHead |> Option.map (fun row -> TreeItem.id row.node))

let visibleFocus focusedId visibleNodes =
    focusedId
    |> Option.filter (fun id -> visibleNodes |> Array.exists (fun row -> TreeItem.id row.node = id))

let moveFocus delta focusedId visibleNodes =
    if visibleNodes |> Array.isEmpty then
        None
    else
        let currentIndex =
            focusedId
            |> Option.bind (fun id -> visibleNodes |> Array.tryFindIndex (fun row -> TreeItem.id row.node = id))
            |> Option.defaultValue 0

        let nextIndex = currentIndex + delta |> max 0 |> min (visibleNodes.Length - 1)

        Some(TreeItem.id visibleNodes.[nextIndex].node)
