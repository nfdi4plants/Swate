module Swate.Components.Primitive.Tree.State

open Swate.Components.Primitive.Tree.Types

module NodeState =

    let emptyLoadState = {
        Status = TreeLazyLoadStatus.Idle
        Children = None
        Error = None
    }

    let loadStateFor nodeId loadedChildren =
        loadedChildren |> Map.tryFind nodeId |> Option.defaultValue emptyLoadState

    let hasCachedChildren nodeId loadedChildren =
        loadedChildren |> Map.tryFind nodeId |> Option.bind _.Children |> Option.isSome

    let withLoading nodeId loadedChildren =
        loadedChildren
        |> Map.add nodeId {
            emptyLoadState with
                Status = TreeLazyLoadStatus.Loading
        }

    let withLoaded nodeId children loadedChildren =
        loadedChildren
        |> Map.add nodeId {
            Status = TreeLazyLoadStatus.Loaded
            Children = Some children
            Error = None
        }

    let withLoadError nodeId message loadedChildren =
        loadedChildren
        |> Map.add nodeId {
            Status = TreeLazyLoadStatus.Error
            Children = None
            Error = Some message
        }

    let invalidateNode nodeId loadedChildren = loadedChildren |> Map.remove nodeId

    let isBranch (node: TreeItem<'T>) = node.kind = TreeNodeKind.Branch

    let directChildren (loadedChildren: Map<string, TreeLoadState<'T>>) (node: TreeItem<'T>) =
        match loadedChildren |> Map.tryFind node.id |> Option.bind _.Children with
        | Some children -> Some children
        | None -> node.children

    let childCount (dataSource: TreeDataSource<'T> option) (node: TreeItem<'T>) =
        match node.children with
        | Some children -> children.Length
        | None ->
            match node.childrenCount, dataSource with
            | Some count, _ -> count
            | None, Some source -> source.GetChildrenCount(Some node)
            | None, None -> 0

    let canExpand dataSource node =
        if not (isBranch node) then
            false
        else
            match node.children, node.childrenCount, dataSource with
            | Some children, _, _ -> children.Length > 0
            | None, Some count, _ -> count > 0
            | None, None, Some _ -> true
            | None, None, None -> false

    let flattenVisible dataSource loadedChildren expandedIds items =
        let nodes = ResizeArray<TreeVisibleNode<'T>>()
        let nodeMap = ResizeArray<string * TreeItem<'T>>()
        let parentMap = ResizeArray<string * string option>()

        let rec loop parentId depth (items: TreeItem<'T>[]) =
            for item in items do
                nodes.Add {
                    Node = item
                    Depth = depth
                    ParentId = parentId
                }

                nodeMap.Add(item.id, item)
                parentMap.Add(item.id, parentId)

                if expandedIds |> Set.contains item.id then
                    match directChildren loadedChildren item with
                    | Some children -> loop (Some item.id) (depth + 1) children
                    | None ->
                        if canExpand dataSource item then
                            ()

        loop None 0 items

        {
            Nodes = nodeMap |> Seq.distinctBy fst |> Map.ofSeq
            Parents = parentMap |> Seq.distinctBy fst |> Map.ofSeq
            VisibleNodes = nodes.ToArray()
        }

    let parentOf nodeId lookup =
        lookup.Parents |> Map.tryFind nodeId |> Option.flatten

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

    let replaceSelection mode nodeId =
        match mode with
        | TreeSelectionMode.Single -> Set.singleton nodeId
        | TreeSelectionMode.Multiple -> Set.singleton nodeId

    let focusedOrFirst focusedId visibleNodes =
        focusedId
        |> Option.filter (fun id -> visibleNodes |> Array.exists (fun row -> row.Node.id = id))
        |> Option.orElse (visibleNodes |> Array.tryHead |> Option.map _.Node.id)

    let moveFocus delta focusedId visibleNodes =
        if visibleNodes |> Array.isEmpty then
            None
        else
            let currentIndex =
                focusedId
                |> Option.bind (fun id -> visibleNodes |> Array.tryFindIndex (fun row -> row.Node.id = id))
                |> Option.defaultValue 0

            let nextIndex = currentIndex + delta |> max 0 |> min (visibleNodes.Length - 1)

            Some visibleNodes.[nextIndex].Node.id
