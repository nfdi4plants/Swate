namespace Swate.Components.ARCObjectExplorer.GraphExplorer

open System
open Swate.Components.ARCObjectExplorer
open Swate.Components.ARCObjectExplorer.Model
open Swate.Components.Shared
open Swate.Components.FileExplorerTypes

module GraphObjectExplorerTreeData =

    let private groupItemType = ArcExplorerNodeKind.label ArcExplorerNodeKind.Group

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

    let private keyFromGroupNode (groupNode: FileItem) =
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

    let rec private collectSummarySeeds (item: FileItem) =
        let children = item.Children |> Option.defaultValue []

        let currentSeed =
            if item.ItemType = groupItemType then
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
                        RuleKey = keyFromGroupNode item
                        SummaryName = item.Name
                        GroupNodeId = item.Id
                        MemberNodeIds = memberNodeIds
                    }
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
            ItemType = ArcExplorerNodeKind.label ArcExplorerNodeKind.Group
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
                    flattenedChildren
                    |> List.collect snd
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

    let flattenNestedChildrenOnParentLevel (items: FileItem list) =
        items
        |> List.map (fun item -> flattenItem item |> fst)
