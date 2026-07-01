namespace Swate.Components.Composite.Workspace

open System
open Fable.Core
open Fable.Core.JsInterop
open Feliz
open Swate.Components
open Swate.Components.JsBindings
open Swate.Components.Composite.Workspace.Types
open Swate.Components.Composite.Workspace.Context
open Swate.Components.Composite.Workspace.Helper.PaneTree
open Swate.Components.Composite.Workspace.Helper.PaneState
open Swate.Components.Composite.Workspace.Helper.DndId

module private WorkspaceHelper =

    let freshPaneId () = Guid.NewGuid().ToString("N")

    let findPaneContainingTab (panes: Map<string, WorkspacePaneState>) (tabId: string) : string option =
        panes
        |> Map.tryPick (fun paneId state ->
            if state.tabs |> Array.exists (fun t -> t.Id = tabId) then
                Some paneId
            else
                None
        )

open WorkspaceHelper

[<Erase; Mangle(false)>]
type Workspace =

    [<ReactComponent>]
    static member private WorkspaceInner
        (
            activeDrag: string option,
            workspaceRef: IRefValue<Browser.Types.HTMLElement option>
        )
        =
        let ctx = useWorkspaceCtx ()

        DndKit.useDndMonitor (
            {|
                onDragStart = ctx.onDragStart
                onDragEnd = ctx.handleDragEnd
            |}
        )

        React.Fragment [
            PaneNode.PaneNode(ctx.layout, "root")
            TabContextMenu.TabContextMenu(workspaceRef)
            DndKit.DragOverlay(
                dropAnimation = {| duration = 0; easing = "linear" |},
                children =
                    match activeDrag with
                    | Some label ->
                        Html.div [
                            prop.className
                                "swt:tab swt:tab-active swt:shadow-xl swt:ring-2 swt:ring-primary swt:ring-offset-2 swt:ring-offset-base-100 swt:px-3 swt:py-1.5"
                            prop.children [ Html.span label ]
                        ]
                    | None -> Html.none
            )
        ]

    [<ReactComponent(true)>]
    static member Workspace
        (
            tabs: WorkspaceTab list,
            contentMap: Map<string, ReactElement>,
            onTabsChange: WorkspaceTab list -> unit,
            onActiveTabChange: string option -> unit,
            ?activeTabId: string option,
            ?ref: IRefValue<IWorkspaceHandle option>,
            ?className: string,
            ?debug: bool
        )
        =
        let debug = defaultArg debug false

        let initialPaneId = React.useMemo ((fun () -> freshPaneId ()), [||])

        let layout, setLayout =
            React.useState (Pane.Leaf initialPaneId)

        let panes, setPanes =
            React.useState (
                Map.ofList [
                    initialPaneId,
                    { WorkspacePaneState.Empty with
                        tabs = Array.ofList tabs
                        tabOrder = Array.ofList (tabs |> List.map _.Id)
                    }
                ]
            )

        let initialActive = defaultArg activeTabId None

        let activeTabId, setActiveTabId =
            React.useState initialActive

        let activeDrag, setActiveDrag =
            React.useState (None: string option)

        let workspaceElementRef = React.useElementRef ()

        // -- Local helper functions for state mutations --

        let closeTab (tabId: string) =
            match findPaneContainingTab panes tabId with
            | None -> ()
            | Some paneId ->
                let newPanes =
                    panes
                    |> Map.change paneId (fun optState ->
                        optState |> Option.map (fun s -> WorkspacePaneState.removeTab s tabId)
                    )
                match newPanes |> Map.tryFind paneId with
                | Some state when state.tabs.Length = 0 ->
                    let newPanes = newPanes |> Map.remove paneId
                    match Pane.removeLeaf layout paneId with
                    | Some newLayout ->
                        setLayout newLayout
                        setPanes newPanes
                        let remainingTabs =
                            newPanes
                            |> Map.toArray
                            |> Array.collect (fun (_, ps) -> ps.tabs)
                        match remainingTabs with
                        | [||] ->
                            setActiveTabId None
                            onActiveTabChange None
                        | _ ->
                            let nextActive = remainingTabs.[0].Id
                            setActiveTabId (Some nextActive)
                            onActiveTabChange (Some nextActive)
                    | None -> ()
                | _ ->
                    setPanes newPanes
                    let s = newPanes.[paneId]
                    setActiveTabId s.activeTabId
                    s.activeTabId |> onActiveTabChange
                let allTabs =
                    newPanes
                    |> Map.toArray
                    |> Array.collect (fun (_, ps) -> ps.tabs)
                    |> Array.toList
                onTabsChange allTabs

        let closeOthers (tabId: string) =
            match findPaneContainingTab panes tabId with
            | None -> ()
            | Some paneId ->
                let newPanes =
                    panes
                    |> Map.change paneId (fun optState ->
                        optState |> Option.map (fun s -> WorkspacePaneState.removeAllExcept s tabId)
                    )
                setPanes newPanes
                setActiveTabId (Some tabId)
                onActiveTabChange (Some tabId)
                let allTabs =
                    newPanes
                    |> Map.toArray
                    |> Array.collect (fun (_, ps) -> ps.tabs)
                    |> Array.toList
                onTabsChange allTabs

        let closeAll () =
            setPanes (Map.ofList [ initialPaneId, WorkspacePaneState.Empty ])
            setLayout (Pane.Leaf initialPaneId)
            setActiveTabId None
            onActiveTabChange None
            onTabsChange []

        let closeAllInPane (paneId: string) =
            let newPanes =
                panes |> Map.change paneId (fun _ -> Some (WorkspacePaneState.Empty))
            match newPanes |> Map.tryFind paneId with
            | Some s when s.tabs.Length = 0 ->
                let newPanes = newPanes |> Map.remove paneId
                match Pane.removeLeaf layout paneId with
                | Some newLayout ->
                    setLayout newLayout
                    setPanes newPanes
                | None ->
                    setPanes newPanes
                    setActiveTabId None
                    onActiveTabChange None
                    onTabsChange []
            | _ -> setPanes newPanes
            let remainingTabs =
                newPanes
                |> Map.remove paneId
                |> Map.toArray
                |> Array.collect (fun (_, ps) -> ps.tabs)
            match remainingTabs with
            | [||] ->
                setActiveTabId None
                onActiveTabChange None
            | _ ->
                let nextActive = remainingTabs.[0].Id
                setActiveTabId (Some nextActive)
                onActiveTabChange (Some nextActive)
            let allTabs =
                newPanes
                |> Map.toArray
                |> Array.collect (fun (_, ps) -> ps.tabs)
                |> Array.toList
            onTabsChange allTabs

        // -- Imperative handle for external API --

        let imperativeHandle =
            { new IWorkspaceHandle with
                member _.activateTab(tabId: string) =
                    setActiveTabId (Some tabId)

                member _.closeTab(tabId: string) =
                    closeTab tabId

                member _.openTab(tab: WorkspaceTab) =
                    let targetPaneId =
                        match activeTabId with
                        | Some id ->
                            findPaneContainingTab panes id
                            |> Option.defaultValue initialPaneId
                        | None -> initialPaneId
                    let newPanes =
                        panes
                        |> Map.change targetPaneId (fun optState ->
                            optState |> Option.map (fun s -> WorkspacePaneState.addTab s tab)
                        )
                    setPanes newPanes
                    setActiveTabId (Some tab.Id)
                    let allTabs =
                        newPanes
                        |> Map.toArray
                        |> Array.collect (fun (_, ps) -> ps.tabs)
                        |> Array.toList
                    onTabsChange allTabs
                    onActiveTabChange (Some tab.Id)
            }

        React.useImperativeHandle (
            !!ref,
            (fun () -> imperativeHandle),
            [| box layout; box panes; box activeTabId |]
        )

        React.useEffect (
            (fun () -> onActiveTabChange activeTabId),
            [| box activeTabId |]
        )

        // -- DnD setup --

        let pointerSensor =
            DndKit.useSensor (
                DndKit.PointerSensor,
                {| activationConstraint = {| distance = 8 |} |}
            )

        let sensors = DndKit.useSensors [| pointerSensor |]

        let onDragStart (event: DndKit.IDndKitEvent) =
            if not (isNull event.active) then
                let activeId = string event.active.id
                match DndId.read activeId with
                | Some (Tab(paneId, tabId)) ->
                    let label =
                        panes
                        |> Map.tryFind paneId
                        |> Option.bind (fun ps -> ps.tabs |> Array.tryFind (fun t -> t.Id = tabId))
                        |> Option.map (fun t -> t.Label)
                        |> Option.defaultValue tabId
                    setActiveDrag (Some label)
                | _ -> ()

        let handleDragEnd (event: DndKit.IDndKitEvent) =
            setActiveDrag None

            if isNull event.active || isNull event.over then
                ()
            else
                let activeId = string event.active.id
                let overId = string event.over.id

                match DndId.read activeId, DndId.read overId with

                // Branch 1: Split creation via edge zone
                | Some (Tab(sourcePaneId, tabId)), Some (EdgeZone(targetPaneId, _) as edgeId) ->
                    match edgeId.edgeToSplitDirection() with
                    | Some direction ->
                        let newLeafId = freshPaneId ()
                        match Pane.splitLeaf layout targetPaneId direction newLeafId with
                        | Some newLayout ->
                            let sourceState = panes |> Map.tryFind sourcePaneId
                            let tabToMove = sourceState |> Option.bind (fun s -> s.tabs |> Array.tryFind (fun t -> t.Id = tabId))
                            match tabToMove with
                            | Some tab ->
                                let newPanes =
                                    panes
                                    |> Map.change sourcePaneId (fun opt -> opt |> Option.map (fun s -> WorkspacePaneState.removeTab s tabId))
                                    |> Map.add newLeafId (WorkspacePaneState.addTab (WorkspacePaneState.Empty) tab)
                                let newPanes =
                                    match newPanes |> Map.tryFind sourcePaneId with
                                    | Some s when s.tabs.Length = 0 ->
                                        match Pane.removeLeaf newLayout sourcePaneId with
                                        | Some cleanedLayout ->
                                            setLayout cleanedLayout
                                            newPanes |> Map.remove sourcePaneId
                                        | None ->
                                            setLayout newLayout
                                            newPanes
                                    | _ ->
                                        setLayout newLayout
                                        newPanes
                                setPanes newPanes
                                setActiveTabId (Some tab.Id)
                                let allTabs =
                                    newPanes
                                    |> Map.toArray
                                    |> Array.collect (fun (_, ps) -> ps.tabs)
                                    |> Array.toList
                                onTabsChange allTabs
                                onActiveTabChange (Some tab.Id)
                            | None -> ()
                        | None -> ()
                    | None -> ()

                // Branch 2: Cross-pane tab move via tab bar drop
                | Some (Tab(sourcePaneId, tabId)), Some (TabBar targetPaneId) when targetPaneId <> sourcePaneId ->
                    let tabToMove =
                        panes
                        |> Map.tryFind sourcePaneId
                        |> Option.bind (fun s -> s.tabs |> Array.tryFind (fun t -> t.Id = tabId))
                    match tabToMove with
                    | Some tab ->
                        let newPanes =
                            panes
                            |> Map.change sourcePaneId (fun opt -> opt |> Option.map (fun s -> WorkspacePaneState.removeTab s tabId))
                            |> Map.change targetPaneId (fun opt ->
                                let current = opt |> Option.defaultValue (WorkspacePaneState.Empty)
                                WorkspacePaneState.addTab current tab |> Some
                            )
                        let newPanes =
                            match newPanes |> Map.tryFind sourcePaneId with
                            | Some s when s.tabs.Length = 0 ->
                                match Pane.removeLeaf layout sourcePaneId with
                                | Some cleanedLayout ->
                                    setLayout cleanedLayout
                                    newPanes |> Map.remove sourcePaneId
                                | None ->
                                    setLayout layout
                                    newPanes
                            | _ ->
                                setLayout layout
                                newPanes
                        setPanes newPanes
                        setActiveTabId (Some tab.Id)
                        let allTabs =
                            newPanes
                            |> Map.toArray
                            |> Array.collect (fun (_, ps) -> ps.tabs)
                            |> Array.toList
                        onTabsChange allTabs
                        onActiveTabChange (Some tab.Id)
                    | None -> ()

                // Branch 3: Same-pane reorder
                | Some (Tab(sourcePaneId, tabId)), Some (Tab(targetPaneId, targetTabId)) when targetPaneId = sourcePaneId ->
                    match panes |> Map.tryFind sourcePaneId with
                    | Some paneState ->
                        let fromIndex = paneState.tabOrder |> Array.tryFindIndex (fun id -> id = tabId)
                        let toIndex = paneState.tabOrder |> Array.tryFindIndex (fun id -> id = targetTabId)
                        match fromIndex, toIndex with
                        | Some fromIdx, Some toIdx when fromIdx <> toIdx ->
                            setPanes (
                                panes
                                |> Map.change sourcePaneId (fun opt ->
                                    opt |> Option.map (fun s -> WorkspacePaneState.reorderTab s fromIdx toIdx)
                                )
                            )
                        | _ -> ()
                    | None -> ()

                | _ -> ()

        // -- Context value --

        let workspaceCtxValue: WorkspaceCtxValue = {
            layout = layout
            setLayout = setLayout
            panes = panes
            setPanes = setPanes
            contentMap = contentMap
            activeTabId = activeTabId
            setActiveTabId = setActiveTabId
            handleDragEnd = handleDragEnd
            onDragStart = onDragStart
            debug = debug
            closeTab = closeTab
            closeOthers = closeOthers
            closeAll = closeAll
            closeAllInPane = closeAllInPane
        }

        Html.div [
            prop.ref workspaceElementRef
            prop.className [
                "swt:flex swt:flex-col swt:size-full swt:overflow-hidden"
                match className with
                | Some c -> c
                | None -> ()
            ]
            if debug then
                prop.testId "workspace-root"
            prop.children [
                WorkspaceCtx.Provider(
                    workspaceCtxValue,
                    DndKit.DndContext(
                        sensors = sensors,
                        collisionDetection = DndKit.closestCenter,
                        children = Workspace.WorkspaceInner(activeDrag, workspaceElementRef)
                    )
                )
            ]
        ]
