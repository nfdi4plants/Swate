module Swate.Components.Page.ARCObjectExplorer.Model

open System
open Swate.Components.Shared
open Swate.Components.Page.FileExplorer.Types
open Swate.Components.Page.ARCObjectExplorer.KindFilter

type ResolvedSelection = {
    ItemId: string
    Node: ArcExplorerNode
    Ancestors: ArcExplorerNode list
}

type Model = {
    VisibleKinds: Set<string>
    FilteredTree: ArcExplorerNode list
    ExplorerItems: FileItem list
    SearchItems: (string * string option * FileItem) array
    Selection: ResolvedSelection option
}

let nodeKindLabel = ArcExplorerNodeKind.label

let private inferredProcessRoleLabel (node: ArcExplorerNode) =
    let hasIdMarker (marker: string) =
        node.id.IndexOf(marker, StringComparison.OrdinalIgnoreCase) >= 0

    if hasIdMarker ":process:input:" then
        Some "Input"
    elif hasIdMarker ":process:output:" then
        Some "Output"
    elif node.name.StartsWith("About: Input", StringComparison.OrdinalIgnoreCase) then
        Some "Input"
    elif node.name.StartsWith("About: Output", StringComparison.OrdinalIgnoreCase) then
        Some "Output"
    else
        None

let private nodeRoleLabel (node: ArcExplorerNode) =
    inferredProcessRoleLabel node
    |> Option.defaultValue (if node.isReference then "Reference" else "Canonical")

let selectedItemId (model: Model) : string option = model.Selection |> Option.map _.ItemId

let selectedNode (model: Model) : ArcExplorerNode option = model.Selection |> Option.map _.Node

let selectedAncestors (model: Model) : ArcExplorerNode list =
    model.Selection |> Option.map _.Ancestors |> Option.defaultValue []

let selectedTitle (model: Model) : string =
    model.Selection
    |> Option.map (fun selection -> selection.Node.name)
    |> Option.defaultValue "No visible selection"

let selectedSubtitle (model: Model) : string =
    model.Selection
    |> Option.map (fun selection ->
        let role = nodeRoleLabel selection.Node
        $"{nodeKindLabel selection.Node.kind} | {role}"
    )
    |> Option.defaultValue "Selection"

let filterTreeByKinds (visibleKinds: Set<string>) (nodes: ArcExplorerNode list) =
    let rec loop (node: ArcExplorerNode) =
        let filteredChildren = node.children |> List.choose loop
        let hasVisibleChildren = filteredChildren |> List.isEmpty |> not
        let kindLabel = nodeKindLabel node.kind
        let isVisibleKind = visibleKinds.Contains kindLabel

        match node.kind with
        | ArcExplorerNodeKind.Arc ->
            Some {
                node with
                    children = filteredChildren
            }
        | ArcExplorerNodeKind.Group ->
            if hasVisibleChildren then
                Some {
                    node with
                        children = filteredChildren
                }
            else
                None
        | _ ->
            if isVisibleKind || hasVisibleChildren then
                Some {
                    node with
                        children = filteredChildren
                }
            else
                None

    nodes |> List.choose loop

let flattenFileItems (items: FileItem list) =
    let rec loop (items: FileItem list) =
        items
        |> List.collect (fun item -> item :: (item.Children |> Option.defaultValue [] |> loop))

    loop items

let flattenNodesWithAncestors (nodes: ArcExplorerNode list) =
    let rec loop (ancestors: ArcExplorerNode list) (nodes: ArcExplorerNode list) =
        nodes
        |> List.collect (fun node ->
            let orderedAncestors = List.rev ancestors
            (node, orderedAncestors) :: loop (node :: ancestors) node.children
        )

    loop [] nodes

let searchableItems (nodes: ArcExplorerNode list) (items: FileItem list) =
    let itemsById =
        items |> flattenFileItems |> List.map (fun item -> item.Id, item) |> Map.ofList

    nodes
    |> flattenNodesWithAncestors
    |> List.choose (fun (node, ancestors) ->
        if
            not node.isSelectable
            || node.kind = ArcExplorerNodeKind.Arc
            || node.kind = ArcExplorerNodeKind.Group
        then
            None
        else
            itemsById
            |> Map.tryFind node.id
            |> Option.map (fun item ->
                let lineagePart =
                    ancestors
                    |> List.filter (fun ancestor -> ancestor.kind <> ArcExplorerNodeKind.Arc)
                    |> List.map _.name
                    |> function
                        | [] -> None
                        | lineage ->
                            let lineageText = String.concat " / " lineage
                            Some $"In: {lineageText}"
                    |> Option.toList

                let subtitleParts = [
                    nodeKindLabel node.kind
                    nodeRoleLabel node
                    yield! lineagePart
                    yield! node.path |> Option.toList
                ]

                node.name, Some(String.concat " | " subtitleParts), item
            )
    )
    |> List.sortBy (fun (name, _, _) -> name.ToLowerInvariant())
    |> List.toArray

let tryGetNodeLineageById (nodeId: string) (nodes: ArcExplorerNode list) =
    let rec loop (ancestors: ArcExplorerNode list) (nodes: ArcExplorerNode list) =
        nodes
        |> List.tryPick (fun node ->
            if node.id = nodeId then
                Some(node, List.rev ancestors)
            else
                loop (node :: ancestors) node.children
        )

    loop [] nodes

let create
    (nodes: ArcExplorerNode list)
    (selection: ArcSelection)
    (kindFilterOptions)
    (selectedKindIndices: Set<int>)
    : Model =
    let visibleKinds = selectedLabels kindFilterOptions selectedKindIndices
    let filteredTree = filterTreeByKinds visibleKinds nodes
    let explorerItems = ARCExplorer.toFileItems filteredTree
    let searchItems = searchableItems filteredTree explorerItems

    let selectedItemId = ARCExplorer.getSelectedItemId filteredTree selection

    let resolvedSelection =
        selectedItemId
        |> Option.bind (fun nodeId ->
            tryGetNodeLineageById nodeId filteredTree
            |> Option.map (fun (node, ancestors) -> {
                ItemId = nodeId
                Node = node
                Ancestors = ancestors
            })
        )

    {
        VisibleKinds = visibleKinds
        FilteredTree = filteredTree
        ExplorerItems = explorerItems
        SearchItems = searchItems
        Selection = resolvedSelection
    }
