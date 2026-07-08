module Swate.Components.Composite.Workspace.WorkspaceModel

open System
open Fable.Core
open Swate.Components.Composite.Workspace.Types

module private Helper =

    let collapseRemovedPane (toBeRemoved: PaneId) (layout: Layout) =

        match layout with
        | Layout.Single id -> Layout.Single id
        | Layout.Split(_, _, Level1.Single remove, Level1.Single keep)
        | Layout.Split(_, _, Level1.Single keep, Level1.Single remove) when remove = toBeRemoved -> Layout.Single keep
        | Layout.Split(_, _, Level1.Single remove, Level1.Split(p1, p2, p3, p4))
        | Layout.Split(_, _, Level1.Split(p1, p2, p3, p4), Level1.Single remove) when remove = toBeRemoved ->
            Layout.Split(p1, p2, Level1.Single p3, Level1.Single p4)
        | Layout.Split(dir, r, Level1.Single keep, Level1.Split(_, _, keep1, remove))
        | Layout.Split(dir, r, Level1.Single keep, Level1.Split(_, _, remove, keep1))
        | Layout.Split(dir, r, Level1.Split(_, _, remove, keep1), Level1.Single keep)
        | Layout.Split(dir, r, Level1.Split(_, _, keep1, remove), Level1.Single keep) when remove = toBeRemoved ->
            Layout.Split(dir, r, Level1.Single keep, Level1.Single keep1)
        | anyElse -> anyElse

    let splitPane (edge: EdgeDirection) (paneId: PaneId) (layout: Layout) : PaneId * Layout =
        let newPaneId: Leaf = PaneId(Guid.NewGuid())
        let targetDirection = edge.AsSplitDirection()

        let splitSingleByEdge (leaf: Leaf) =
            match edge with
            | EdgeDirection.Top -> {|
                direction = targetDirection
                first = newPaneId
                second = leaf
              |}
            | EdgeDirection.Bottom -> {|
                direction = targetDirection
                first = leaf
                second = newPaneId
              |}
            | EdgeDirection.Left -> {|
                direction = targetDirection
                first = newPaneId
                second = leaf
              |}
            | EdgeDirection.Right -> {|
                direction = targetDirection
                first = leaf
                second = newPaneId
              |}

        let splitLevel1 (lvl1: Level1) =
            match lvl1 with
            | Level1.Single id when id = paneId ->
                let split = splitSingleByEdge id
                Level1.Split(split.direction, 0.5, split.first, split.second)
            | _ -> lvl1

        let nextLayout =
            match layout with
            | Layout.Single id when id = paneId ->
                let split = splitSingleByEdge id
                Layout.Split(split.direction, 0.5, Level1.Single split.first, Level1.Single split.second)
            | Layout.Split(dir, _, _, _) when dir = targetDirection -> layout
            | Layout.Split(dir, r, l1, l2) ->
                let updatedL1 = splitLevel1 l1
                let updatedL2 = splitLevel1 l2
                Layout.Split(dir, r, updatedL1, updatedL2)
            | _ -> layout

        (newPaneId, nextLayout)

    let getAllowedEdgeSplits (paneIdParam: Leaf) (layout: Layout) =
        let defaultResponse = {|
            edges = []
            isTopAllowed = false
            isBottomAllowed = false
            isLeftAllowed = false
            isRightAllowed = false
        |}

        match layout with
        | Layout.Single id when id = paneIdParam ->
            {|
                defaultResponse with
                    edges = [
                        EdgeDirection.Top
                        EdgeDirection.Bottom
                        EdgeDirection.Left
                        EdgeDirection.Right
                    ]
                    isTopAllowed = true
                    isBottomAllowed = true
                    isLeftAllowed = true
                    isRightAllowed = true
            |}
        | Layout.Split(dir, _, Level1.Single targetId, _)
        | Layout.Split(dir, _, _, Level1.Single targetId) when targetId = paneIdParam ->
            match dir with
            | SplitDirection.Horizontal ->
                {|
                    defaultResponse with
                        edges = [
                            EdgeDirection.Top
                            EdgeDirection.Bottom
                        ]
                        isTopAllowed = true
                        isBottomAllowed = true
                |}
            | SplitDirection.Vertical ->
                {|
                    defaultResponse with
                        edges = [
                            EdgeDirection.Left
                            EdgeDirection.Right
                        ]
                        isLeftAllowed = true
                        isRightAllowed = true
                |}
        | _ ->
            defaultResponse

    let ensureEdgeSplitAllowed (paneId: Leaf) (edge: EdgeDirection) (layout: Layout) =
        let allowedEdges = getAllowedEdgeSplits paneId layout
        match edge with
        | EdgeDirection.Top -> allowedEdges.isTopAllowed
        | EdgeDirection.Bottom -> allowedEdges.isBottomAllowed
        | EdgeDirection.Left -> allowedEdges.isLeftAllowed
        | EdgeDirection.Right -> allowedEdges.isRightAllowed

    let ensureTabMoveAllowed (tabId: TabId) (model: WorkspaceModel<'T>) =
        let isExistingIsLastTab =
            model.PanesMap
            |> Map.tryPick (fun paneId pane ->
                match pane.Tabs |> List.tryFind (fun t -> t.Id = tabId) with
                | Some _ ->
                    let isLastTab = List.length pane.Tabs = 1
                    Some (paneId, isLastTab)
                | None ->
                    None
            )

        match isExistingIsLastTab with
        | Some (sourcePaneId, true) ->
            match model.Layout with
            | Layout.Single id when id = sourcePaneId -> false
            | _ -> true
        | Some (_, false) -> true
        | None ->
            Browser.Dom.console.warn $"Tab with ID {tabId.Value} not found in any pane."
            false

    let ensureTabEdgeDropAllowed (tabId: TabId) (paneId: Leaf) (edge: EdgeDirection) (model: WorkspaceModel<'T>) =
        let splitAllowed =
            model.Layout
            |> ensureEdgeSplitAllowed paneId edge
        let tabMoveAllowed =
            model
            |> ensureTabMoveAllowed tabId
        splitAllowed && tabMoveAllowed

open Helper

let ensureEdgeSplitAllowed (paneId: Leaf) (edge: EdgeDirection) (layout: Layout) =
    Helper.ensureEdgeSplitAllowed paneId edge layout

type WorkspaceModel<'T> with

    static member Init(?initialTabs: Tab<'T> [], ?initialActiveTabId: string) =
        let tabs =
            initialTabs
            |> Option.defaultValue [||]
            |> Array.toList

        let activeTabId =
            match initialActiveTabId with
            | Some id -> Some(TabId id)
            | None ->
                tabs |> List.tryHead |> Option.map (fun t -> t.Id)

        let id = PaneId(Guid.NewGuid())

        let pane = {
            Id = id
            Tabs = tabs
            FocusedTab = activeTabId
        }

        let layout = Layout.Single id

        {
            Layout = layout
            PanesMap = Map.ofList [ id, pane ]
            FocusedPane = id
        }

    static member CleanupEmptyPanes(model: WorkspaceModel<'T>) =
        let toBeRemovedPaneIds =
            model.PanesMap
            |> Map.toArray
            |> Array.filter (fun (_, pane) -> List.isEmpty pane.Tabs)
            |> Array.map fst

        if toBeRemovedPaneIds.Length = 0 then
            model
        else
            Array.fold
                (fun (acc: WorkspaceModel<'T>) (paneId: PaneId) ->
                    let updatedLayout = collapseRemovedPane paneId acc.Layout
                    let updatedPanesMap = Map.remove paneId acc.PanesMap

                    {
                        acc with
                            Layout = updatedLayout
                            PanesMap = updatedPanesMap
                    }
                )
                model
                toBeRemovedPaneIds

    static member EnsurePaneMapSync(model: WorkspaceModel<'T>) =
        let paneIdsInLayout =
            let collectPaneIds (layout: Layout) =
                match layout with
                | Layout.Single id -> [ id ]
                | Layout.Split(_, _, l1, l2) ->
                    let collectInnerIds (lvl1: Level1) =
                        match lvl1 with
                        | Level1.Single id -> [ id ]
                        | Level1.Split(_, _, l1, l2) -> [ l1; l2 ]

                    collectInnerIds l1 @ collectInnerIds l2

            collectPaneIds model.Layout |> Set.ofList<PaneId>

        let updatedPanesMap =
            model.PanesMap
            |> Map.filter (fun paneId _ -> Set.contains paneId paneIdsInLayout)

        {
            model with
                PanesMap = updatedPanesMap
        }

    static member EnsureValidFocus(model: WorkspaceModel<'T>) =
        let updatedPanesMap =
            model.PanesMap
            |> Map.map (fun _ pane ->
                match pane.FocusedTab with
                | Some focusedTabId when pane.Tabs |> List.exists (fun t -> t.Id = focusedTabId) -> pane
                | _ ->
                    let newFocusedTab = pane.Tabs |> List.tryHead |> Option.map (fun t -> t.Id)
                    { pane with FocusedTab = newFocusedTab }
            )

        let updatedFocusedPane =
            match model.FocusedPane with
            | focusedPaneId when Map.containsKey focusedPaneId updatedPanesMap -> focusedPaneId
            | _ ->
                updatedPanesMap
                |> Map.tryPick (fun _ pane -> Some(pane.Id))
                |> Option.defaultValue model.FocusedPane

        {
            model with
                PanesMap = updatedPanesMap
                FocusedPane = updatedFocusedPane
        }

    static member GetPaneIdForTab (tabId: TabId) (model: WorkspaceModel<'T>) : PaneId option =
        model.PanesMap
        |> Map.tryPick (fun paneId pane ->
            if pane.Tabs |> List.exists (fun t -> t.Id = tabId) then
                Some paneId
            else
                None
        )

    static member AddTab (tab: Tab<'T>) (model: WorkspaceModel<'T>) =

        let focusedPaneId = model.FocusedPane

        let updatePane (pane: Pane<'T>) = {
            pane with
                Tabs = tab :: pane.Tabs
                FocusedTab = Some tab.Id
        }

        {
            model with
                PanesMap =
                    model.PanesMap
                    |> Map.change
                        focusedPaneId
                        (fun pane ->
                            match pane with
                            | Some p -> Some(updatePane p)
                            | None -> None
                        )
        }

    static member RemoveTab (tabId: TabId) (model: WorkspaceModel<'T>) =

        let updatePane (pane: Pane<'T>) = {
            pane with
                Tabs = pane.Tabs |> List.filter (fun t -> t.Id <> tabId)
        }

        {
            model with
                PanesMap =
                    model.PanesMap
                    |> Map.map (fun _ pane ->
                        if pane.Tabs |> List.exists (fun t -> t.Id = tabId) then
                            updatePane pane
                        else
                            pane
                    )
        }

    static member RemoveOtherTabs (keepTabId: TabId) (model: WorkspaceModel<'T>) =
        match WorkspaceModel.GetPaneIdForTab keepTabId model with
        | Some paneId ->
            let existingPane =
                model.PanesMap
                |> Map.find paneId

            let kept =
                existingPane.Tabs
                |> List.filter (fun t -> t.Id = keepTabId)

            let updatedPane = {
                existingPane with
                    Tabs = kept
                    FocusedTab = Some keepTabId
            }

            {
                model with
                    PanesMap = model.PanesMap |> Map.add paneId updatedPane
                    FocusedPane = paneId
            }
        | None -> model

    static member RemoveAllTabs (model: WorkspaceModel<'T>) =
        let id = PaneId(Guid.NewGuid())

        let pane = {
            Id = id
            Tabs = []
            FocusedTab = None
        }

        {
            Layout = Layout.Single id
            PanesMap = Map.ofList [ id, pane ]
            FocusedPane = id
        }

    static member FocusTab (tabId: TabId) (model: WorkspaceModel<'T>) =
        let mutable focusedPane = model.FocusedPane

        let updatePane (paneId: PaneId) (pane: Pane<'T>) =
            if pane.Tabs |> List.exists (fun t -> t.Id = tabId) then
                focusedPane <- paneId
                { pane with FocusedTab = Some tabId }
            else
                pane

        let updatedPanesMap =
            model.PanesMap |> Map.map updatePane

        {
            model with
                PanesMap = updatedPanesMap
                FocusedPane = focusedPane
        }

    static member MoveTab (tabId: TabId) (targetPaneId: PaneId) (model: WorkspaceModel<'T>) =
        let nextSourcePane =
            model.PanesMap
            |> Map.tryPick (fun paneId pane ->
                match pane.Tabs |> List.tryFind (fun t -> t.Id = tabId) with
                | Some toBeMovedTab ->
                    let filteredTabsPane = {
                        pane with
                            Tabs = pane.Tabs |> List.filter (fun t -> t.Id <> tabId)
                    }

                    Some(paneId, toBeMovedTab, filteredTabsPane)
                | None -> None
            )

        match nextSourcePane with
        | Some(sourcePaneId, toBeMovedTab, updatedSourcePane) ->
            let nextTargetPane =
                model.PanesMap
                |> Map.tryFind targetPaneId
                |> Option.map (fun targetPane ->
                    let updatedTargetPane = {
                        targetPane with
                            Tabs = toBeMovedTab :: targetPane.Tabs
                            FocusedTab = Some toBeMovedTab.Id
                    }

                    updatedTargetPane
                )

            let nextMap =
                match nextTargetPane with
                | Some updatedTargetPane ->
                    model.PanesMap
                    |> Map.add sourcePaneId updatedSourcePane
                    |> Map.add targetPaneId updatedTargetPane
                | None ->
                    model.PanesMap

            {
                model with
                    PanesMap = nextMap
                    FocusedPane = targetPaneId
            }
        | None ->
            model

    static member ReorderTabs (paneId: PaneId) (fromIndex: int) (toIndex: int) (model: WorkspaceModel<'T>) =
        match model.PanesMap |> Map.tryFind paneId with
        | Some pane ->
            if fromIndex < 0 || toIndex < 0 || fromIndex >= pane.Tabs.Length || toIndex >= pane.Tabs.Length then
                model
            else
                let tabsArr = pane.Tabs |> List.toArray
                let element = tabsArr.[fromIndex]
                let withoutElement = pane.Tabs |> List.filter (fun t -> t.Id <> element.Id)
                let before = withoutElement |> List.truncate toIndex
                let after = withoutElement |> List.skip toIndex
                let reordered = before @ (element :: after)
                let updatedPane = { pane with Tabs = reordered }
                { model with PanesMap = model.PanesMap |> Map.add paneId updatedPane }
        | None -> model

    static member SplitPane (paneId: PaneId) (direction: EdgeDirection) (model: WorkspaceModel<'T>) =
        let newPaneId, nextLayout = splitPane direction paneId model.Layout

        let nextPaneMap =
            model.PanesMap
            |> Map.add newPaneId {
                Id = newPaneId
                Tabs = []
                FocusedTab = None
            }

        {
            model with
                Layout = nextLayout
                PanesMap = nextPaneMap
                FocusedPane = newPaneId
        }

    static member ClosePane (paneId: PaneId) (model: WorkspaceModel<'T>) =
        let clearedPane =
            match model.PanesMap |> Map.tryFind paneId with
            | Some pane -> { pane with Tabs = []; FocusedTab = None }
            | None ->
                {
                    Id = paneId
                    Tabs = []
                    FocusedTab = None
                }

        {
            model with
                PanesMap = model.PanesMap |> Map.add paneId clearedPane
        }

    static member SetSplitRatio (panePath: string) (ratio: float) (model: WorkspaceModel<'T>) =
        let clamped = max 0.15 (min 0.85 ratio)

        let rec updateLayout (path: string) (layout: Layout) : Layout =
            match layout with
            | Layout.Split(dir, _, l1, l2) when path = panePath ->
                Layout.Split(dir, clamped, l1, l2)
            | Layout.Split(dir, r, l1, l2) ->
                let firstPath = path + "/first"
                let secondPath = path + "/second"
                Layout.Split(dir, r, updateLevel1 firstPath l1, updateLevel1 secondPath l2)
            | _ -> layout

        and updateLevel1 (path: string) (lvl: Level1) : Level1 =
            match lvl with
            | Level1.Split(dir, _, l1, l2) when path = panePath ->
                Level1.Split(dir, clamped, l1, l2)
            | _ -> lvl

        { model with Layout = updateLayout "" model.Layout }

let update (model: WorkspaceModel<'T>) (msg: Msg<'T>) : WorkspaceModel<'T> =
    let next =
        match msg with
        | AddTab tab -> model |> WorkspaceModel.AddTab tab

        | RemoveTab tabId ->
            model
            |> WorkspaceModel.RemoveTab tabId
            |> WorkspaceModel.CleanupEmptyPanes

        | RemoveOtherTabs keepTabId -> model |> WorkspaceModel.RemoveOtherTabs keepTabId

        | RemoveAllTabs -> model |> WorkspaceModel.RemoveAllTabs

        | FocusTab tabId -> model |> WorkspaceModel.FocusTab tabId

        | MoveTab(tabId, targetPaneId) ->
            let isAllowedTabMove = ensureTabMoveAllowed tabId model

            if not isAllowedTabMove then
                model
            else
                model
                |> WorkspaceModel.MoveTab tabId targetPaneId
                |> WorkspaceModel.CleanupEmptyPanes

        | ReorderTabs(paneId, fromIndex, toIndex) ->
            model
            |> WorkspaceModel.ReorderTabs paneId fromIndex toIndex

        | SplitPaneByTabMove(tabId, paneId, edge) ->
            let tabEdgeDropAllowed =
                ensureTabEdgeDropAllowed tabId paneId edge model

            if not tabEdgeDropAllowed then
                model
            else
                let afterSplit = model |> WorkspaceModel.SplitPane paneId edge

                let newPaneId = afterSplit.FocusedPane

                afterSplit
                |> WorkspaceModel.MoveTab tabId newPaneId
                |> WorkspaceModel.CleanupEmptyPanes

        | ClosePane paneId ->
            model
            |> WorkspaceModel.ClosePane paneId
            |> WorkspaceModel.CleanupEmptyPanes

        | SetSplitRatio(panePath, ratio) ->
            model
            |> WorkspaceModel.SetSplitRatio panePath ratio

    next
    |> WorkspaceModel.EnsurePaneMapSync
    |> WorkspaceModel.EnsureValidFocus
