#r "nuget: Feliz"

open System
open Fable.Core
open Feliz

[<StringEnum>]
[<RequireQualifiedAccess>]
type SplitDirection =
    | Horizontal
    | Vertical

[<StringEnum>]
[<RequireQualifiedAccess>]
type EdgeDirection =
    | Top
    | Bottom
    | Left
    | Right

    member this.AsSplitDirection() =
        match this with
        | Top
        | Bottom -> SplitDirection.Horizontal
        | Left
        | Right -> SplitDirection.Vertical

[<Erase>]
type TabId =
    | TabId of string


    member this.Value =
        let (TabId id) = this
        id

type PaneId =
    | PaneId of Guid


    member this.Value =
        let (PaneId id) = this
        id

type Tab<'T> = {
    Id: TabId
    Label: string
    Payload: 'T
}

type Pane<'T> = {
    Id: PaneId
    Tabs: Tab<'T> list
    FocusedTab: TabId option
}

type Leaf = PaneId

[<RequireQualifiedAccess>]
type Level1 =
    | Single of Leaf
    | Split of SplitDirection * Leaf * Leaf

[<RequireQualifiedAccess>]
type Layout =
    | Single of Leaf
    | Split of SplitDirection * Level1 * Level1

type WorkspaceModel<'T> = {
    /// This defines the visual layout of the workspace. Up tp a (2x2) grid of panes can be defined. Each pane is identified by its PaneId, which is used to look up the tabs in the Panes map.
    Layout: Layout
    /// Maps ``PaneId`` to ``Pane<'T>``. A ``Pane`` contains the tabs and tab focus.
    PanesMap: Map<PaneId, Pane<'T>>
    /// The pane, which was last worked on. This can be switched to a tab in this pane, or was freshly created after a split.
    /// This is used to determine where to place new tabs.
    FocusedPane: PaneId
}

type Msg<'T> =
    // Tabs
    | AddTab of Tab<'T>
    | RemoveTab of TabId
    | FocusTab of TabId
    | MoveTab of tab: TabId * target: PaneId
    | ReorderTabs of pane: PaneId * newOrder: TabId list
    | SplitPaneByTabMove of TabId * PaneId * EdgeDirection
    | ClosePane of PaneId

module WorkspaceHelper =

    let collapseRemovedPane (toBeRemoved: PaneId) (layout: Layout) =

        match layout with
        // If the layout is a single pane, we can't collapse it further.
        | Layout.Single id -> Layout.Single id
        // If we have ``Split(Single, Single)`` and one will be removed, we collapse to ``Single``
        | Layout.Split(_, Level1.Single remove, Level1.Single keep)
        | Layout.Split(_, Level1.Single keep, Level1.Single remove) when remove = toBeRemoved -> Layout.Single keep
        // If we have ``Split(Single, Split)`` and the single will be removed, we replace the outer split with the inner split.
        | Layout.Split(_, Level1.Single remove, Level1.Split(p1, p2, p3))
        | Layout.Split(_, Level1.Split(p1, p2, p3), Level1.Single remove) when remove = toBeRemoved ->
            Layout.Split(p1, Level1.Single p2, Level1.Single p3)
        // If we have ``Split(Single, Split)`` and the inner Split contains the pane to be removed, we collapse the inner split to a single.
        | Layout.Split(dir, Level1.Single keep, Level1.Split(_, keep1, remove))
        | Layout.Split(dir, Level1.Single keep, Level1.Split(_, remove, keep1))
        | Layout.Split(dir, Level1.Split(_, remove, keep1), Level1.Single keep)
        | Layout.Split(dir, Level1.Split(_, keep1, remove), Level1.Single keep) when remove = toBeRemoved ->
            Layout.Split(dir, Level1.Single keep, Level1.Single keep1)
        | anyElse -> anyElse

    let splitPane (edge: EdgeDirection) (paneId: PaneId) (layout: Layout) : PaneId * Layout =
        let newPaneId: Leaf = PaneId(Guid.NewGuid())
        /// The direction the new ``Split`` should have
        let targetDirection = edge.AsSplitDirection()

        /// Generic helper to split a single pane into two new panes based on the edge direction.
        /// Edge direction determines which pane will be the first and which will be the second in the split.
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
                Level1.Split(split.direction, split.first, split.second)
            | _ -> lvl1

        let nextLayout =
            match layout with
            | Layout.Single id when id = paneId ->
                let split = splitSingleByEdge id
                Layout.Split(split.direction, Level1.Single split.first, Level1.Single split.second)
            // Allow inner split only in opposite direction of the target direction.
            // This ensures that we don't create a 3x1 or 1x3 layout, but only a 2x2 layout.
            | Layout.Split(dir, _, _) when dir = targetDirection -> layout
            | Layout.Split(dir, l1, l2) ->
                let updatedL1 = splitLevel1 l1
                let updatedL2 = splitLevel1 l2
                Layout.Split(dir, updatedL1, updatedL2)
            | _ -> layout

        (newPaneId, nextLayout)

    /// This function checks if a pane can be split in the given direction based on the current layout.
    /// 
    /// ⚠️ It does not check if the split makes sense based on the number of tabs in any given pane.
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
        | Layout.Split(dir, Level1.Single targetId, _) 
        | Layout.Split(dir, _, Level1.Single targetId) when targetId = paneIdParam ->
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

    /// This function checks if a pane can be split in the given direction based on the current layout.
    /// 
    /// ⚠️ It does not check if the split makes sense based on the number of tabs in any given pane.
    let ensureEdgeSplitAllowed (paneId: Leaf) (edge: EdgeDirection) (layout: Layout) =
        let allowedEdges = getAllowedEdgeSplits paneId layout
        match edge with
        | EdgeDirection.Top -> allowedEdges.isTopAllowed
        | EdgeDirection.Bottom -> allowedEdges.isBottomAllowed
        | EdgeDirection.Left -> allowedEdges.isLeftAllowed
        | EdgeDirection.Right -> allowedEdges.isRightAllowed

    /// This function checks if a tab can be moved to a target pane based on the current layout.
    /// 
    /// - If there is a a ``Layout.Single`` with a single tab, we do not allow moving the tab to another pane, as this would leave the source pane empty.
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
            // Tab not found in any pane, we cannot move it, so we return
            false

    /// This function checks if a pane can be split in the given direction and if a tab can be moved to the new pane based on the current layout.
    let ensureTabEdgeDropAllowed (tabId: TabId) (paneId: Leaf) (edge: EdgeDirection) (model: WorkspaceModel<'T>) =
        let splitAllowed =
            model.Layout
            |> ensureEdgeSplitAllowed paneId edge
        let tabMoveAllowed =
            model 
            |> ensureTabMoveAllowed tabId
        splitAllowed && tabMoveAllowed

open WorkspaceHelper

type WorkspaceModel<'T> with

    static member Init(?initialTabs: Tab<'T>[], ?initialActiveTabId: string) =
        let tabs = initialTabs |> Option.defaultValue [||] |> Array.toList

        let activeTabId =
            match initialActiveTabId with
            | Some id -> Some(TabId id)
            | None -> None

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

    /// This function has one responsibility: Every pane in ``PanesMap`` must contain at least one tab.
    ///
    /// If a pane is empty:
    /// - Remove if from the Panes map
    /// - collapse any splits in the layout affected by the removed pane
    static member CleanupEmptyPanes(model: WorkspaceModel<'T>) =
        let toBeRemovedPaneIds =
            model.PanesMap
            |> Map.toArray
            |> Array.filter (fun (_, pane) -> List.isEmpty pane.Tabs)
            |> Array.map fst

        // If we do not have any panes to remove, we can return the model as is.
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
    /// - If a pane is in the Layout but not in the Panes map, it will be removed from the Layout.
    static member EnsurePaneMapSync(model: WorkspaceModel<'T>) =
        let paneIdsInLayout =
            let collectPaneIds (layout: Layout) =
                match layout with
                | Layout.Single id -> [ id ]
                | Layout.Split(_, l1, l2) ->
                    let collectInnerIds (lvl1: Level1) =
                        match lvl1 with
                        | Level1.Single id -> [ id ]
                        | Level1.Split(_, l1, l2) -> [ l1; l2 ]

                    collectInnerIds l1 @ collectInnerIds l2

            collectPaneIds model.Layout |> Set.ofList<PaneId>

        // Remove any panes from the map that are not in the layout
        let updatedPanesMap =
            model.PanesMap
            |> Map.filter (fun paneId _ -> Set.contains paneId paneIdsInLayout)

        {
            model with
                PanesMap = updatedPanesMap
        }

    /// - For every pane:
    ///   - if ``FocusedTab`` no longer exists, set FocusedTab to another one.
    /// - If ``FocusedPane`` no longer exists, set FocusedPane to another one.
    static member EnsureValidFocus(model: WorkspaceModel<'T>) =
        let updatedPanesMap =
            model.PanesMap
            |> Map.map (fun _ pane ->
                match pane.FocusedTab with
                | Some focusedTabId when pane.Tabs |> List.exists (fun t -> t.Id = focusedTabId) -> pane // Focused tab is valid, no change needed
                | _ ->
                    // Focused tab is invalid or not set, pick the first tab if available
                    let newFocusedTab = pane.Tabs |> List.tryHead |> Option.map (fun t -> t.Id)
                    { pane with FocusedTab = newFocusedTab }
            )

        let updatedFocusedPane =
            match model.FocusedPane with
            | focusedPaneId when Map.containsKey focusedPaneId updatedPanesMap -> focusedPaneId // Focused pane is valid, no change needed
            | _ ->
                // Focused pane is invalid or not set, pick the first available pane if any
                updatedPanesMap
                |> Map.tryPick (fun _ pane -> Some(pane.Id))
                |> Option.defaultValue model.FocusedPane

        {
            model with
                PanesMap = updatedPanesMap
                FocusedPane = updatedFocusedPane
        }

    /// This function removes a tab in the workspace model.
    /// If the tab is not found, the model remains unchanged.
    ///
    /// ⚠️ This function does not handle focus or cleanup empty panes.
    static member RemoveTab (tabId: TabId) (model: WorkspaceModel<'T>) =

        let updatePane (pane: Pane<'T>) = {
            pane with
                Pane.Tabs = pane.Tabs |> List.filter (fun t -> t.Id <> tabId)
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

    static member GetPaneIdForTab (tabId: TabId) (model: WorkspaceModel<'T>) : PaneId option =
        model.PanesMap
        |> Map.tryPick (fun paneId pane ->
            if pane.Tabs |> List.exists (fun t -> t.Id = tabId) then
                Some paneId
            else
                None
        )

    static member FocusTab (tabId: TabId) (model: WorkspaceModel<'T>) =

        let updatePane (pane: Pane<'T>) = {
            pane with
                Pane.FocusedTab = Some tabId
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

    /// This function adds a tab to the workspace model.
    ///
    /// - The tab will be added to the currently focused pane.
    /// - The newly added tab will be set as the focused tab in that pane.
    static member AddTab (tab: Tab<'T>) (model: WorkspaceModel<'T>) =

        let focusedPaneId = model.FocusedPane

        let updatePane (pane: Pane<'T>) = {
            pane with
                Pane.Tabs = tab :: pane.Tabs
                Pane.FocusedTab = Some tab.Id
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

    /// This function moves a tab from its current pane to a target pane.
    ///
    /// - If the tab is not found in any pane, the model remains unchanged.
    /// - If the target pane does not exist, the model remains unchanged.
    /// - If the tab is already in the target pane, the model remains unchanged.
    /// - After moving the tab, the moved tab will be set as the focused tab in the target pane.
    /// - ⚠️ This function does not cleanup empty panes.
    static member MoveTab (tabId: TabId) (targetPaneId: PaneId) (model: WorkspaceModel<'T>) =
        /// This function finds the source pane containing the tab to be moved and removes the tab from that pane.
        /// Return value contains the paneId, the tabs without the targetTab, and the targetTab itself.
        /// With this information, we can use ``Map.add`` to update the source pane and add the target tab to the target pane.
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
            /// This function finds the target pane and adds the tab to that pane, setting it as the focused tab.
            /// With this information, we can use ``Map.add`` to update the target pane.
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

            /// Update the PanesMap with the updated source and target panes. If one of the panes does not exist, we return the map unchanged.
            let nextMap =
                match nextTargetPane with
                | Some updatedTargetPane ->
                    model.PanesMap
                    |> Map.add sourcePaneId updatedSourcePane
                    |> Map.add targetPaneId updatedTargetPane
                | None ->
                    // Target pane does not exist, return the map unchanged
                    // We can only reach this point if the source pane was found, so we also implicitly check for the existence of the source pane.
                    model.PanesMap

            {
                model with
                    PanesMap = nextMap
                    FocusedPane = targetPaneId // Set the focused pane to the target pane after moving the tab
            }
        | None ->
            // Tab not found in any pane, return the model unchanged
            model

    /// This function reorders the tabs in a pane based on the provided new order of TabIds.
    /// 
    /// - If the pane does not exist, the model remains unchanged.
    /// - If the new order does not contain all TabIds in the pane, or contains TabIds that do not exist in the pane, an error is thrown.
    static member ReorderTabs (paneId: PaneId) (newOrder: TabId list) (model: WorkspaceModel<'T>) =
        let sanitizedInput = newOrder |> List.distinct

        match model.PanesMap |> Map.tryFind paneId with
        | Some pane ->
            if (Set newOrder <> Set (pane.Tabs |> List.map (fun t -> t.Id))) then
                failwithf "PaneId-%A: New order does not match the set of existing TabIds in the pane." paneId.Value
            let reorderedTabs =
                sanitizedInput
                |> List.choose (fun tabId -> pane.Tabs |> List.tryFind (fun t -> t.Id = tabId))
            
            let updatedPane = { pane with Tabs = reorderedTabs }
            { model with PanesMap = model.PanesMap |> Map.add paneId updatedPane }
        | None ->
            // Pane not found, return the model unchanged
            model

    /// This function splits a pane into two new panes, based on the specified direction.
    ///
    /// - The original pane will be replaced by a split layout containing two new panes. With one of them keeping the original PaneId.
    /// - An empty pane will be created with a new PaneId. The new pane will be added to the Panes map.
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

let update (model: WorkspaceModel<'T>) (msg: Msg<'T>) : WorkspaceModel<'T> =
    match msg with
    | AddTab tab -> model |> WorkspaceModel.AddTab tab

    | RemoveTab tabId -> model |> WorkspaceModel.RemoveTab tabId |> WorkspaceModel.CleanupEmptyPanes

    | FocusTab tabId -> model |> WorkspaceModel.FocusTab tabId

    | MoveTab(tabId, targetPaneId) ->

        /// do not allow moving a tab 
        let isAllowedTabMove = ensureTabMoveAllowed tabId model
        if not isAllowedTabMove then
            model // If the move is not allowed, we return the model unchanged.
        else
            model
            |> WorkspaceModel.MoveTab tabId targetPaneId
            |> WorkspaceModel.CleanupEmptyPanes
        
    | ReorderTabs(paneId, newOrder) ->
        model
        |> WorkspaceModel.ReorderTabs paneId newOrder

    | SplitPaneByTabMove(tabId, paneId, edge) ->
        let tabEdgeDropAllowed =
            ensureTabEdgeDropAllowed tabId paneId edge model
        
        if not tabEdgeDropAllowed then
            model // If the split is not allowed, we return the model unchanged.
        else
            model
            |> WorkspaceModel.SplitPane paneId edge
            |> WorkspaceModel.MoveTab tabId model.FocusedPane
            |> WorkspaceModel.CleanupEmptyPanes

    | ClosePane paneId ->
        // Implementation for closing a pane would go here
        model
    // After any message, we ensure that the Panes map is in sync with the Layout and that the focus is valid.
    |> WorkspaceModel.EnsurePaneMapSync
    |> WorkspaceModel.EnsureValidFocus

type CompClass =
    [<ReactComponent>]
    static member MyComponent
        (renderTab: Tab<'A> -> ReactElement, ?initialTabs: Tab<'A>[], ?initialActiveTabId: string)
        =

        let model, dispatch = React.useReducer (update, WorkspaceModel.Init())

        Html.div [

        ]
