namespace Swate.Components

open Swate.Components.Shared
open Swate.Components.FileExplorerTypes

type ArcObjectExplorerViewModel = {
    VisibleKinds: Set<string>
    FilteredTree: ArcExplorerNode list
    ExplorerItems: FileItem list
    SearchItems: (string * string option * FileItem) array
    SelectedItemId: string option
    SelectedNodeLineage: (ArcExplorerNode * ArcExplorerNode list) option
    SelectedNode: ArcExplorerNode option
    SelectedAncestors: ArcExplorerNode list
    SelectedTitle: string
    SelectedSubtitle: string
}

[<RequireQualifiedAccess>]
module ArcObjectExplorerView =

    let nodeKindLabel =
        ArcExplorerNodeKind.label

    let filterTreeByKinds (visibleKinds: Set<string>) (nodes: ArcExplorerNode list) =
        let rec loop (node: ArcExplorerNode) =
            let filteredChildren = node.children |> List.choose loop
            let hasVisibleChildren = filteredChildren |> List.isEmpty |> not
            let kindLabel = nodeKindLabel node.kind
            let isVisibleKind = visibleKinds.Contains kindLabel

            match node.kind with
            | ArcExplorerNodeKind.Arc ->
                Some { node with children = filteredChildren }
            | ArcExplorerNodeKind.Group ->
                if hasVisibleChildren then
                    Some { node with children = filteredChildren }
                else
                    None
            | _ ->
                if isVisibleKind || hasVisibleChildren then
                    Some { node with children = filteredChildren }
                else
                    None

        nodes |> List.choose loop

    let private flattenFileItems (items: FileItem list) =
        let rec loop (items: FileItem list) =
            items
            |> List.collect (fun item ->
                item :: (item.Children |> Option.defaultValue [] |> loop))

        loop items

    let private flattenNodesWithAncestors (nodes: ArcExplorerNode list) =
        let rec loop (ancestors: ArcExplorerNode list) (nodes: ArcExplorerNode list) =
            nodes
            |> List.collect (fun node ->
                let orderedAncestors = List.rev ancestors
                (node, orderedAncestors) :: loop (node :: ancestors) node.children)

        loop [] nodes

    let searchableItems (nodes: ArcExplorerNode list) (items: FileItem list) =
        let itemsById =
            items
            |> flattenFileItems
            |> List.map (fun item -> item.Id, item)
            |> Map.ofList

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
                        if node.isReference then "Reference" else "Canonical"
                        yield! lineagePart
                        yield! node.path |> Option.toList
                    ]

                    node.name, Some(String.concat " | " subtitleParts), item))
        |> List.sortBy (fun (name, _, _) -> name.ToLowerInvariant())
        |> List.toArray

    let private tryGetNodeLineageById (nodeId: string) (nodes: ArcExplorerNode list) =
        let rec loop (ancestors: ArcExplorerNode list) (nodes: ArcExplorerNode list) =
            nodes
            |> List.tryPick (fun node ->
                if node.id = nodeId then
                    Some(node, List.rev ancestors)
                else
                    loop (node :: ancestors) node.children)

        loop [] nodes

    let create
        (nodes: ArcExplorerNode list)
        (selection: ArcSelection)
        (selectedKindIndices: Set<int>)
        =
        let visibleKinds = ARCObjectWidget.SelectedKindLabels selectedKindIndices
        let filteredTree = filterTreeByKinds visibleKinds nodes
        let explorerItems = ARCExplorer.toFileItems filteredTree
        let searchItems = searchableItems filteredTree explorerItems

        let selectedItemId = ARCExplorer.getSelectedItemId filteredTree selection

        let selectedNodeLineage =
            selectedItemId
            |> Option.bind (fun nodeId -> tryGetNodeLineageById nodeId filteredTree)

        let selectedNode =
            selectedNodeLineage
            |> Option.map fst

        let selectedAncestors =
            selectedNodeLineage
            |> Option.map snd
            |> Option.defaultValue []

        let selectedTitle =
            selectedNode
            |> Option.map (fun node -> node.name)
            |> Option.defaultValue "No visible selection"

        let selectedSubtitle =
            selectedNode
            |> Option.map (fun node ->
                let role = if node.isReference then "Reference" else "Canonical"
                $"{nodeKindLabel node.kind} | {role}")
            |> Option.defaultValue "Selection"

        {
            VisibleKinds = visibleKinds
            FilteredTree = filteredTree
            ExplorerItems = explorerItems
            SearchItems = searchItems
            SelectedItemId = selectedItemId
            SelectedNodeLineage = selectedNodeLineage
            SelectedNode = selectedNode
            SelectedAncestors = selectedAncestors
            SelectedTitle = selectedTitle
            SelectedSubtitle = selectedSubtitle
        }
