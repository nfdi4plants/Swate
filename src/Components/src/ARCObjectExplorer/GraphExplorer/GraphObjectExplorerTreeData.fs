module Swate.Components.ARCObjectExplorer.GraphExplorer.GraphObjectExplorerTreeData

open System
open Swate.Components.ARCObjectExplorer
open Swate.Components.ARCObjectExplorer.GraphExplorer.Model
open Swate.Components.Shared
open Swate.Components.FileExplorer.Types


let private groupItemType = GraphExplorerNodeKind.label GraphExplorerNodeKind.Group
let private materialItemType = GraphExplorerNodeKind.label GraphExplorerNodeKind.Material
let private dataItemType = GraphExplorerNodeKind.label GraphExplorerNodeKind.Data
let private descendantSummaryGroupKeys = Set.ofList [ "additional-properties"; "parameter-value"; "formal-parameters" ]

type private DescendantSummarySeed = {
    RuleKey: string
    SummaryName: string
    GroupNodeId: string
    MemberNodeIds: string list
}

type private DescendantSummaryRule = {
    RuleKey: string
    SummaryName: string
    SummaryGroupIdSuffix: string
    GroupNodeIds: Set<string>
    MemberNodeIds: Set<string>
}

let private tryGetGroupKeyFromId (id: string) =
    let marker = ":group:"
    let markerIndex = id.LastIndexOf(marker, StringComparison.Ordinal)

    if markerIndex < 0 then
        None
    else
        let keyStart = markerIndex + marker.Length

        if keyStart >= id.Length then
            None
        else
            Some(id.Substring(keyStart))

let private normalizeSummaryRuleKey (groupNode: FileItem) (ruleKey: string) =
    if String.Equals(groupNode.Name, "Additional Properties", StringComparison.OrdinalIgnoreCase) then
        "additional-properties"
    elif String.Equals(groupNode.Name, "Parameter Values", StringComparison.OrdinalIgnoreCase) then
        "parameter-value"
    elif String.Equals(groupNode.Name, "Formal Parameters", StringComparison.OrdinalIgnoreCase) then
        "formal-parameters"
    else
        ruleKey

let private keyFromGroupNode (groupNode: FileItem) =
    let rawKey =
        groupNode.Id
        |> tryGetGroupKeyFromId
        |> Option.defaultValue (
            groupNode.Name
                .Trim()
                .ToLowerInvariant()
                .Replace(" ", "-")
                .Replace("/", "-")
                .Replace("\\", "-")
                .Replace(":", "-")
        )

    normalizeSummaryRuleKey groupNode rawKey

let rec private collectSummarySeeds (item: FileItem) =
    let children = item.Children |> Option.defaultValue []

    let currentSeed =
        if item.ItemType = groupItemType then
            let ruleKey = keyFromGroupNode item

            if descendantSummaryGroupKeys.Contains ruleKey then
                let memberNodeIds =
                    children
                    |> List.filter (fun child ->
                        child.ItemType <> groupItemType
                        && child.ItemType <> "empty")
                    |> List.map _.Id

                if List.isEmpty memberNodeIds then
                    None
                else
                    Some {
                        RuleKey = ruleKey
                        SummaryName = item.Name
                        GroupNodeId = item.Id
                        MemberNodeIds = memberNodeIds
                    }
            else
                None
        else
            None

    (currentSeed |> Option.toList) @ (children |> List.collect collectSummarySeeds)

let private summaryRulesFromOriginalDescendants (item: FileItem) =
    let seeds =
        item.Children
        |> Option.defaultValue []
        |> List.collect collectSummarySeeds

    let rulesByKey, orderedKeys =
        seeds
        |> List.fold (fun (rulesByKey, orderedKeys) seed ->
            match Map.tryFind seed.RuleKey rulesByKey with
            | Some existing ->
                let updatedRule =
                    {
                        existing with
                            GroupNodeIds = Set.add seed.GroupNodeId existing.GroupNodeIds
                            MemberNodeIds = Set.union existing.MemberNodeIds (Set.ofList seed.MemberNodeIds)
                    }

                Map.add seed.RuleKey updatedRule rulesByKey, orderedKeys
            | None ->
                let newRule = {
                    RuleKey = seed.RuleKey
                    SummaryName = seed.SummaryName
                    SummaryGroupIdSuffix = $"group:flattened-{seed.RuleKey}"
                    GroupNodeIds = Set.singleton seed.GroupNodeId
                    MemberNodeIds = Set.ofList seed.MemberNodeIds
                }

                Map.add seed.RuleKey newRule rulesByKey, orderedKeys @ [ seed.RuleKey ]) (Map.empty, [])

    orderedKeys
    |> List.choose (fun key -> Map.tryFind key rulesByKey)

let private createSummaryFolder (parentItem: FileItem) (rule: DescendantSummaryRule) (children: FileItem list) =
    let groupAppearance = ARCExplorer.appearanceForNodeKind ArcExplorerNodeKind.Group

    {
        Id = $"{parentItem.Id}:{rule.SummaryGroupIdSuffix}"
        Name = rule.SummaryName
        Icon = groupAppearance.Icon
        IconTone = groupAppearance.IconTone
        IsExpanded = false
        Children = Some children
        IdRel = None
        IsDirectory = true
        IsLFS = None
        IsLFSPointer = None
        Checkout = None
        Downloaded = None
        Size = None
        SizeFormatted = None
        ItemType = groupItemType
        Label = Some rule.SummaryName
        Selectable = false
        Path = None
    }

let private summarizeDescendantBranches (parentItem: FileItem) (rule: DescendantSummaryRule) (descendants: FileItem list) =
    let summarizedLeaves =
        descendants
        |> List.filter (fun item -> Set.contains item.Id rule.MemberNodeIds)

    if List.isEmpty summarizedLeaves then
        descendants
    else
        let isRuleBranchOrMember (item: FileItem) =
            Set.contains item.Id rule.GroupNodeIds
            || Set.contains item.Id rule.MemberNodeIds

        let firstBranchNodeIndex =
            descendants
            |> List.tryFindIndex isRuleBranchOrMember
            |> Option.defaultValue 0

        let descendantsWithoutBranchNodes =
            descendants
            |> List.filter (isRuleBranchOrMember >> not)

        let insertionIndex =
            descendants
            |> List.take firstBranchNodeIndex
            |> List.filter (isRuleBranchOrMember >> not)
            |> List.length

        let summaryFolder = createSummaryFolder parentItem rule summarizedLeaves

        let beforeSummaryFolder =
            descendantsWithoutBranchNodes
            |> List.take insertionIndex

        let afterSummaryFolder =
            descendantsWithoutBranchNodes
            |> List.skip insertionIndex

        beforeSummaryFolder @ [ summaryFolder ] @ afterSummaryFolder

let rec private flattenItem (item: FileItem) : FileItem * FileItem list =
    let children, descendantsForAncestors =
        match item.Children with
        | Some children ->
            let flattenedChildren =
                children
                |> List.map flattenItem

            let descendantsForParent =
                let directChildren =
                    flattenedChildren
                    |> List.map fst

                let nestedDescendants =
                    flattenedChildren
                    |> List.collect (fun (_, descendants) ->
                        descendants
                        |> List.skip 1)

                directChildren @ nestedDescendants
                |> List.filter (fun descendant -> descendant.ItemType <> "empty")

            let descendantsForCurrentItem =
                (descendantsForParent, summaryRulesFromOriginalDescendants item)
                ||> List.fold (fun currentDescendants summaryRule ->
                    summarizeDescendantBranches item summaryRule currentDescendants)

            Some descendantsForCurrentItem, descendantsForParent
        | None ->
            None, []

    let flattenedItem =
        {
            item with
                Children = children
        }

    flattenedItem, flattenedItem :: descendantsForAncestors

let private siblingNameKey (name: string) =
    name.Trim().ToLowerInvariant()

let private isMergeableGroupFolder (item: FileItem) =
    item.ItemType = groupItemType
    && item.IsDirectory

let rec private normalizeSiblingLevelOnly (children: FileItem list) =
    children
    |> mergeSameNameGroupsOnSiblingLevel
    |> rehomeLeafLikeNodesIntoMatchingFolders
    |> hideRepresentedNonGroupSiblings

and private mergeGroupFolderNodes (nodes: FileItem list) =
    let firstNode = nodes |> List.head

    let mergedChildren =
        nodes
        |> List.collect (fun node -> node.Children |> Option.defaultValue [])
        |> List.distinctBy _.Id
        |> normalizeSiblingLevelOnly

    {
        firstNode with
            Children = Some mergedChildren
    }

and private mergeSameNameGroupsOnSiblingLevel (children: FileItem list) =
    let rec loop acc remaining =
        match remaining with
        | [] -> List.rev acc
        | current :: tail when isMergeableGroupFolder current ->
            let sameNameGroups, nonMatchingGroups =
                tail
                |> List.partition (fun sibling ->
                    isMergeableGroupFolder sibling
                    && String.Equals(sibling.Name, current.Name, StringComparison.OrdinalIgnoreCase))

            let groupedNodes = current :: sameNameGroups

            let mergedNode =
                if List.length groupedNodes > 1 then
                    mergeGroupFolderNodes groupedNodes
                else
                    current

            loop (mergedNode :: acc) nonMatchingGroups
        | current :: tail ->
            loop (current :: acc) tail

    loop [] children

and private collectNodeIds (item: FileItem) =
    let childrenIds =
        item.Children
        |> Option.defaultValue []
        |> List.collect collectNodeIds

    item.Id :: childrenIds

and private hideRepresentedNonGroupSiblings (siblings: FileItem list) =
    let representedIdsInsideGroups =
        siblings
        |> List.filter isMergeableGroupFolder
        |> List.collect (fun folder ->
            folder.Children
            |> Option.defaultValue []
            |> List.collect collectNodeIds)
        |> Set.ofList

    siblings
    |> List.filter (fun sibling ->
        isMergeableGroupFolder sibling
        || representedIdsInsideGroups.Contains sibling.Id |> not)

and private tryGetTargetFolderNameForLeafLikeNode (item: FileItem) =
    let idContains (marker: string) =
        item.Id.IndexOf(marker, StringComparison.OrdinalIgnoreCase) >= 0

    if item.ItemType = materialItemType then
        Some "Material"
    elif item.ItemType = dataItemType then
        Some "Data"
    elif idContains ":additional-property:" then
        Some "Additional Properties"
    elif idContains ":parameter-value:" then
        Some "Parameter Values"
    elif idContains ":formal-parameter:" then
        Some "Formal Parameters"
    else
        None

and private rehomeLeafLikeNodesIntoMatchingFolders (siblings: FileItem list) =
    let foldersByName =
        siblings
        |> List.choose (fun item ->
            if isMergeableGroupFolder item then
                Some(siblingNameKey item.Name, item)
            else
                None)
        |> Map.ofList

    let movedByFolderId, movedNodeIds =
        siblings
        |> List.fold (fun (moves, movedIds) item ->
            if isMergeableGroupFolder item then
                moves, movedIds
            else
                match tryGetTargetFolderNameForLeafLikeNode item with
                | Some folderName ->
                    match foldersByName |> Map.tryFind (siblingNameKey folderName) with
                    | Some folder ->
                        let movedNodesForFolder =
                            moves
                            |> Map.tryFind folder.Id
                            |> Option.defaultValue []

                        let updatedMoves =
                            moves
                            |> Map.add folder.Id (movedNodesForFolder @ [ item ])

                        updatedMoves, movedIds |> Set.add item.Id
                    | None ->
                        moves, movedIds
                | None ->
                    moves, movedIds) (Map.empty, Set.empty)

    let siblingsWithoutMovedNodes =
        siblings
        |> List.filter (fun item -> movedNodeIds.Contains item.Id |> not)

    siblingsWithoutMovedNodes
    |> List.map (fun item ->
        match movedByFolderId |> Map.tryFind item.Id with
        | Some movedNodes ->
            let existingChildren = item.Children |> Option.defaultValue []
            let mergedChildren = (existingChildren @ movedNodes) |> List.distinctBy _.Id
            { item with Children = Some mergedChildren }
        | None ->
            item)

let rec private normalizeTreeForGraphView (item: FileItem) =
    let normalizedChildren =
        item.Children
        |> Option.map (fun children ->
            children
            |> List.map normalizeTreeForGraphView
            |> normalizeSiblingLevelOnly)

    { item with Children = normalizedChildren }

let flattenNestedChildrenOnParentLevel (items: FileItem list) =
    items
    |> List.map (fun item ->
        item
        |> flattenItem
        |> fst
        |> normalizeTreeForGraphView)
