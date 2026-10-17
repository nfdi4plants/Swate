module internal Swate.Components.Composite.Tree.State

open Swate.Components.Composite.Tree.Types

[<Literal>]
let rootCacheKey = "\u0000tree-root"

let emptyLoadState = {
    status = TreeLazyLoadStatus.Idle
    children = None
    error = None
}

let hasActiveOrLoadedChildren nodeId loadedChildren =
    match loadedChildren |> Map.tryFind nodeId with
    | Some state ->
        match state.status with
        | TreeLazyLoadStatus.Loading
        | TreeLazyLoadStatus.Loaded -> true
        | TreeLazyLoadStatus.Idle
        | TreeLazyLoadStatus.Error -> false
    | None -> false

let withLoading nodeId loadedChildren =
    loadedChildren
    |> Map.add nodeId {
        emptyLoadState with
            status = TreeLazyLoadStatus.Loading
    }

let withLoaded nodeId children loadedChildren =
    loadedChildren
    |> Map.add nodeId {
        status = TreeLazyLoadStatus.Loaded
        children = Some children
        error = None
    }

let withLoadError nodeId message loadedChildren =
    loadedChildren
    |> Map.add nodeId {
        status = TreeLazyLoadStatus.Error
        children = None
        error = Some message
    }

let rootItems (items: TreeItem<'T>[]) loadedChildren =
    if items.Length > 0 then
        items
    else
        loadedChildren
        |> Map.tryFind rootCacheKey
        |> Option.bind _.children
        |> Option.defaultValue items

/// <summary>
/// Returns the effective direct children for the specified tree node.
/// </summary>
/// <remarks>
/// For branch nodes, children previously loaded through the data source take
/// precedence over children provided directly by <c>TreeItem.Branch</c>.
/// The loaded children remain authoritative until the cached value is cleared
/// through <c>invalidateNode</c> or <c>invalidateAll</c>.
/// </remarks>
let directChildren (loadedChildren: Map<string, TreeLoadState<'T>>) (node: TreeItem<'T>) =
    match node with
    | TreeItem.Leaf _ -> None
    | TreeItem.Branch _ ->
        match loadedChildren |> Map.tryFind (TreeItem.getId node) |> Option.bind _.children with
        | Some children -> Some children
        | None -> TreeItem.tryGetChildren node

let flattenVisible loadedChildren expandedIds items =
    let nodes = ResizeArray<TreeVisibleNode<'T>>()
    let nodeMap = ResizeArray<string * TreeItem<'T>>()
    let parentMap = ResizeArray<string * string>()

    let comparisonNode node =
        match node with
        | TreeItem.Leaf props -> TreeItem.Leaf props
        | TreeItem.Branch(props, _) ->
            let directComparisonChildren =
                directChildren loadedChildren node
                |> Option.map (
                    Array.map (fun child ->
                        match child with
                        | TreeItem.Leaf props -> TreeItem.Leaf props
                        | TreeItem.Branch(props, _) -> TreeItem.Branch(props, None)
                    )
                )

            TreeItem.Branch(props, directComparisonChildren)

    let rec loop ancestors parentId depth (items: TreeItem<'T>[]) =
        for index = 0 to items.Length - 1 do
            let item = items.[index]
            let itemId = TreeItem.getId item

            if not (ancestors |> Set.contains itemId) then
                nodes.Add {
                    node = item
                    comparisonNode = comparisonNode item
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
        nodes = nodeMap |> Seq.distinctBy fst |> Map.ofSeq
        parents = parentMap |> Seq.distinctBy fst |> Map.ofSeq
        indices = nodes |> Seq.mapi (fun index row -> TreeItem.getId row.node, index) |> Map.ofSeq
        firstChildren =
            nodes
            |> Seq.choose (fun row -> row.parentId |> Option.map (fun parent -> parent, TreeItem.getId row.node))
            |> Seq.distinctBy fst
            |> Map.ofSeq
        visibleNodes = nodes.ToArray()
    }

let toggleExpanded nodeId expandedIds =
    if expandedIds |> Set.contains nodeId then
        expandedIds |> Set.remove nodeId
    else
        expandedIds |> Set.add nodeId

let toggleSelection nodeId selectedIds =
    if selectedIds |> Array.contains nodeId then
        selectedIds |> Array.filter ((<>) nodeId)
    else
        Array.append selectedIds [| nodeId |]

let rangeSelection anchorId targetId isNodeSelectable (lookup: TreeRowLookup<'T>) =
    match Map.tryFind anchorId lookup.indices, Map.tryFind targetId lookup.indices with
    | Some anchorIndex, Some targetIndex ->
        let firstIndex = min anchorIndex targetIndex
        let lastIndex = max anchorIndex targetIndex

        lookup.visibleNodes.[firstIndex..lastIndex]
        |> Array.choose (fun row ->
            if isNodeSelectable row.node then
                Some(TreeItem.getId row.node)
            else
                None
        )
    | _ -> [| targetId |]

let activeOrFirst activeId selectedIds lookup =
    let isVisible id = lookup.indices |> Map.containsKey id

    activeId
    |> Option.filter isVisible
    |> Option.orElseWith (fun () -> selectedIds |> Array.rev |> Array.tryFind isVisible)
    |> Option.orElseWith (fun () ->
        lookup.visibleNodes
        |> Array.tryHead
        |> Option.map (fun row -> TreeItem.getId row.node)
    )

let visibleFocus focusedId lookup =
    focusedId |> Option.filter (fun id -> lookup.indices |> Map.containsKey id)

let moveFocus delta focusedId lookup =
    if lookup.visibleNodes |> Array.isEmpty then
        None
    else
        let currentIndex =
            focusedId
            |> Option.bind (fun id -> lookup.indices |> Map.tryFind id)
            |> Option.defaultValue 0

        let nextIndex =
            currentIndex + delta |> max 0 |> min (lookup.visibleNodes.Length - 1)

        Some(TreeItem.getId lookup.visibleNodes.[nextIndex].node)

let private collectKnownNodes items loadedChildren =
    let rec collect visited nodes (items: TreeItem<'T>[]) =
        items
        |> Array.fold
            (fun (visited, nodes) item ->
                let nodeId = TreeItem.getId item

                if visited |> Set.contains nodeId then
                    visited, nodes
                else
                    let nextVisited = visited |> Set.add nodeId
                    let nextNodes = nodes |> Map.add nodeId item

                    match directChildren loadedChildren item with
                    | Some children -> collect nextVisited nextNodes children
                    | None -> nextVisited, nextNodes
            )
            (visited, nodes)

    let initialVisited, initialNodes = collect Set.empty Map.empty items

    loadedChildren
    |> Map.fold
        (fun (visited, nodes) _ state ->
            match state.children with
            | Some children -> collect visited nodes children
            | None -> visited, nodes
        )
        (initialVisited, initialNodes)
    |> snd

let knownSubtreeIds items loadedChildren nodeId =
    let knownNodes = collectKnownNodes items loadedChildren

    let rec collect visited currentId =
        if visited |> Set.contains currentId then
            visited
        else
            let nextVisited = visited |> Set.add currentId

            match
                knownNodes
                |> Map.tryFind currentId
                |> Option.bind (directChildren loadedChildren)
            with
            | Some children ->
                children
                |> Array.fold (fun current child -> collect current (TreeItem.getId child)) nextVisited
            | None -> nextVisited

    collect Set.empty nodeId

let invalidatedSubtreeIds items loadedChildren nodeId =
    let subtreeIds = knownSubtreeIds items loadedChildren nodeId

    if subtreeIds |> Seq.exists (fun id -> loadedChildren |> Map.containsKey id) then
        subtreeIds
    else
        Set.empty

let removeCacheEntries nodeIds loadedChildren =
    nodeIds
    |> Set.fold (fun current nodeId -> current |> Map.remove nodeId) loadedChildren

let removeDescendantExpansions nodeId subtreeIds expandedIds =
    subtreeIds
    |> Set.remove nodeId
    |> Set.fold (fun current descendantId -> current |> Set.remove descendantId) expandedIds

let private staticNodeIds items =
    let rec collect known (items: TreeItem<'T>[]) =
        items
        |> Array.fold
            (fun current item ->
                let next = current |> Set.add (TreeItem.getId item)

                match TreeItem.tryGetChildren item with
                | Some children -> collect next children
                | None -> next
            )
            known

    collect Set.empty items

let preserveExpansionAfterInvalidateAll items loadedChildren defaultExpandedIds expandedIds =
    let staticIds = staticNodeIds items

    let loadedDescendantIds =
        loadedChildren
        |> Map.toSeq
        |> Seq.collect (fun (_, state) ->
            state.children
            |> Option.defaultValue [||]
            |> Seq.collect (fun child -> knownSubtreeIds items loadedChildren (TreeItem.getId child))
        )
        |> Set.ofSeq

    let reloadableRoots =
        loadedChildren
        |> Map.keys
        |> Set.ofSeq
        |> Set.remove rootCacheKey
        |> Set.filter (fun nodeId -> loadedDescendantIds |> Set.contains nodeId |> not)

    let lazyRootIds =
        rootItems [||] loadedChildren |> Array.map TreeItem.getId |> Set.ofArray

    let defaultIds = defaultExpandedIds |> Set.ofArray

    expandedIds
    |> Set.filter (fun nodeId ->
        staticIds.Contains nodeId
        || reloadableRoots.Contains nodeId
        || lazyRootIds.Contains nodeId
        || defaultIds.Contains nodeId
    )
