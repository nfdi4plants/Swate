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
    match node.kind with
    | TreeNodeKind.Leaf -> None
    | TreeNodeKind.Branch ->
        match loadedChildren |> Map.tryFind node.id |> Option.bind _.Children with
        | Some children -> Some children
        | None -> node.children

let canExpand
    (dataSource: TreeDataSource<'T> option)
    enableLazyLoading
    (loadedChildren: Map<string, TreeLoadState<'T>>)
    (node: TreeItem<'T>)
    =
    if node.kind <> TreeNodeKind.Branch then
        false
    else
        match directChildren loadedChildren node, dataSource with
        | Some children, _ -> children.Length > 0
        | None, Some source when enableLazyLoading -> source.getChildrenCount (Some node) <> 0
        | None, _ -> false

let flattenVisible loadedChildren expandedIds items =
    let nodes = ResizeArray<TreeVisibleNode<'T>>()
    let nodeMap = ResizeArray<string * TreeItem<'T>>()
    let parentMap = ResizeArray<string * string>()

    let rec loop ancestors parentId depth (items: TreeItem<'T>[]) =
        for index = 0 to items.Length - 1 do
            let item = items.[index]

            if not (ancestors |> Set.contains item.id) then
                nodes.Add {
                    node = item
                    depth = depth
                    parentId = parentId
                    posInSet = index + 1
                    setSize = items.Length
                }

                nodeMap.Add(item.id, item)
                parentId |> Option.iter (fun parentId -> parentMap.Add(item.id, parentId))

                if item.kind = TreeNodeKind.Branch && expandedIds |> Set.contains item.id then
                    match directChildren loadedChildren item with
                    | Some children -> loop (ancestors |> Set.add item.id) (Some item.id) (depth + 1) children
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

let nextSelection mode extendSelection nodeId selectedIds =
    match mode, extendSelection with
    | TreeSelectionMode.Multiple, true -> toggleSelection mode nodeId selectedIds
    | _ -> Set.singleton nodeId

let focusedOrFirst focusedId visibleNodes =
    focusedId
    |> Option.filter (fun id -> visibleNodes |> Array.exists (fun row -> row.node.id = id))
    |> Option.orElse (visibleNodes |> Array.tryHead |> Option.map _.node.id)

let moveFocus delta focusedId visibleNodes =
    if visibleNodes |> Array.isEmpty then
        None
    else
        let currentIndex =
            focusedId
            |> Option.bind (fun id -> visibleNodes |> Array.tryFindIndex (fun row -> row.node.id = id))
            |> Option.defaultValue 0

        let nextIndex = currentIndex + delta |> max 0 |> min (visibleNodes.Length - 1)

        Some visibleNodes.[nextIndex].node.id
