module Swate.Components.Composite.Workspace.WorkspaceModel

open System
open Fable.Core
open Swate.Components.Composite.Workspace.Types

module private Helper =

    let collapseRemovedPane (toBeRemoved: PaneId) (layout: Layout) =

        let rec loop (layout: Layout) : Layout =
            match layout with
            | Layout.Split(_,_,_, Layout.Single remove, keep)
            | Layout.Split(_,_,_, keep, Layout.Single remove) when remove = toBeRemoved ->
                keep
            | Layout.Split(splitId, dir, r, i1, i2) ->
                let next1 = loop i1
                let next2 = loop i2
                Layout.Split(splitId, dir, r, next1, next2)
            | _ -> layout

        match layout with
        // If the layout is a single pane, we just return it as is
        | Layout.Single id -> Layout.Single id
        | Layout.Split(_, _, _, _, _) as split ->
            loop split

    let splitPane (edge: EdgeDirection) (paneId: PaneId) (layout: Layout) : PaneId * Layout =
        let newPaneId: Leaf = PaneId(Guid.NewGuid())
        let newSplitId: SplitId = SplitId(Guid.NewGuid())
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

        let mutable isSplit = false

        let rec loop (layout: Layout) = 
            match layout with
            // Early exit if we have found the pane to split
            | _ when isSplit -> layout
            | Layout.Single id when id = paneId ->
                let split = splitSingleByEdge id
                isSplit <- true
                Layout.Split(newSplitId, split.direction, 0.5, Layout.Single split.first, Layout.Single split.second)
            | Layout.Split(splitId, dir, r, i1, i2) ->
                let next1 = loop i1
                let next2 = loop i2
                Layout.Split(splitId, dir, r, next1, next2)
            | _ -> layout

        let nextLayout =
            loop layout

        (newPaneId, nextLayout)

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
        let tabMoveAllowed =
            model
            |> ensureTabMoveAllowed tabId
        tabMoveAllowed

open Helper

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

    /// This function ensures that the Panes map is in sync with the Layout.
    /// 
    /// - If a pane is in the Layout but not in the Panes map, it will be removed from the Layout.
    static member EnsurePaneMapSync(model: WorkspaceModel<'T>) =
        let rec collectPaneIds (layout: Layout) : PaneId list =
            match layout with
            | Layout.Single id -> [ id ]
            | Layout.Split(_, _, _, l1, l2) ->
                collectPaneIds l1 @ collectPaneIds l2

        let paneIdsInLayout =

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
                            Tabs = targetPane.Tabs @ [ toBeMovedTab ]
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

    static member SetSplitRatio (splitId: SplitId) (ratio: float) (model: WorkspaceModel<'T>) =
        let clamped = max 0.15 (min 0.85 ratio)

        let mutable isUpdated = false

        let rec updateLayout (layout: Layout) : Layout =
            match layout with
            // Early exit if we have already updated the ratio
            | _ when isUpdated -> layout
            | Layout.Split(id, dir, _, l1, l2) when id.Value = splitId.Value ->
                isUpdated <- true
                Layout.Split(id, dir, clamped, l1, l2)
            | Layout.Split(id, dir, r, l1, l2) ->
                Layout.Split(id, dir, r, updateLayout l1, updateLayout l2)
            | _ -> layout

        { model with Layout = updateLayout model.Layout }

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

        | SetSplitRatio(splitId, ratio) ->
            model
            |> WorkspaceModel.SetSplitRatio splitId ratio

    next
    |> WorkspaceModel.EnsurePaneMapSync
    |> WorkspaceModel.EnsureValidFocus
