namespace Swate.Components.Composite.Workspace

open System
open Fable.Core
open Feliz
open Swate.Components
open Swate.Components.JsBindings
open Swate.Components.Composite.Workspace.Types
open Swate.Components.Composite.Workspace.Context
open Swate.Components.Composite.Workspace.WorkspaceModel
open Swate.Components.Composite.Workspace.Helper.DndId

[<Erase; Mangle(false)>]
type Workspace =

    [<ReactComponent>]
    static member WorkspaceProvider
        (
            renderTabContent: Tab<'T> -> ReactElement,
            ?renderTab: Tab<'T> -> ReactElement,
            ?initialTabs: Tab<'T> [],
            ?children: ReactElement,
            ?debug: bool
        )
        =

        let debug = defaultArg debug false

        let model, dispatch =
            React.useReducer (
                update,
                WorkspaceModel.Init(
                    ?initialTabs = (initialTabs |> Option.map Array.ofSeq)
                )
            )

        let defaultRenderTab (tab: Tab<'T>) =
            Html.span [ prop.className "swt:text-sm"; prop.text tab.Label ]

        let renderTabFn = defaultArg renderTab defaultRenderTab

        let dispatchCtx: WorkspaceDispatchContext<'T> = { dispatch = dispatch }

        let layoutCtx: WorkspaceLayoutContext = { layout = model.Layout }

        let paneStateCtx: WorkspacePaneStateContext<'T> = {
            panesMap = model.PanesMap
            focusedPane = model.FocusedPane
            renderTabContent = renderTabContent
            renderTab = renderTabFn
            debug = debug
        }

        WorkspaceDispatchCtx.Provider(
            box dispatchCtx,
            WorkspaceLayoutCtx.Provider(
                layoutCtx,
                WorkspacePaneStateCtx.Provider(box paneStateCtx, defaultArg children Html.none)
            )
        )

    [<ReactComponent>]
    static member private WorkspaceInner
        (
            activeDrag: string option,
            workspaceRef: IRefValue<Browser.Types.HTMLElement option>
        )
        =
        let layoutCtx = useWorkspaceLayoutCtx ()
        let dndCtx = useWorkspaceDndCtx ()
        let sortableActiveRef = React.useRef true

        let sortableActiveCtx: SortableActiveContext = {
            isActiveRef = sortableActiveRef
        }

        DndKit.useDndMonitor (
            {|
                onDragStart = dndCtx.onDragStart
                onDragEnd = dndCtx.handleDragEnd
            |}
        )

        SortableActiveCtx.Provider(
            sortableActiveCtx,
            React.Fragment [
                PaneNode.PaneNode(layoutCtx.layout)
                TabContextMenu.TabContextMenu(workspaceRef)
                DndKit.DragOverlay(
                    dropAnimation = {| duration = 0; easing = "linear" |},
                    children =
                        match activeDrag with
                        | Some label ->
                            Html.div [
                                prop.style [ style.pointerEvents.none ]
                                prop.className
                                    "swt:tab swt:tab-active swt:shadow-xl swt:ring-2 swt:ring-primary swt:ring-offset-2 swt:ring-offset-base-100 swt:px-3 swt:py-1.5"
                                prop.children [ Html.span label ]
                            ]
                        | None -> Html.none
                )
                DropOverlay.DropOverlay(workspaceRef, sortableActiveRef)
            ]
        )

    [<ReactComponent>]
    static member Workspace<'T>
        (
            ?className: string,
            ?debug: bool
        )
        =
        let debug = defaultArg debug false

        let layoutCtx = useWorkspaceLayoutCtx ()
        let paneStateCtx = useWorkspacePaneStateCtx<'T> ()
        let dispatchCtx = useWorkspaceDispatchCtx<'T> ()

        let activeDrag, setActiveDrag = React.useState (None: string option)
        let workspaceElementRef = React.useElementRef ()

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
                | Some (Tab(paneIdKey, tabId)) ->
                    let panes = paneStateCtx.panesMap

                    let label =
                        panes
                        |> Map.tryPick (fun _ pane ->
                            pane.Tabs
                            |> List.tryFind (fun (t: Tab<'T>) -> t.Id.Value = tabId)
                            |> Option.map (fun t -> t.Label)
                        )
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

                | Some (Tab(sourcePaneKey, tabId)), Some (Tab(targetPaneKey, targetTabId)) when targetPaneKey = sourcePaneKey ->
                    let tabIdValue = TabId tabId
                    let targetTabIdValue = TabId targetTabId
                    let paneId = PaneId(Guid.Parse(sourcePaneKey))

                    let panes = paneStateCtx.panesMap

                    match panes |> Map.tryFind paneId with
                    | Some pane ->
                        let fromIndex = pane.Tabs |> List.tryFindIndex (fun (t: Tab<'T>) -> t.Id = tabIdValue)

                        let toIndex =
                            pane.Tabs |> List.tryFindIndex (fun (t: Tab<'T>) -> t.Id = targetTabIdValue)

                        match fromIndex, toIndex with
                        | Some fromIdx, Some toIdx when fromIdx <> toIdx ->
                            dispatchCtx.dispatch (ReorderTabs(paneId, fromIdx, toIdx))
                        | _ -> ()
                    | None -> ()

                | _ -> ()

        let dndCtxValue: WorkspaceDndContext =
            React.useMemo (
                (fun () ->
                    {
                        onDragStart = onDragStart
                        handleDragEnd = handleDragEnd
                        isDragging = activeDrag |> Option.isSome
                    }
                ),
                [| box onDragStart; box handleDragEnd; box activeDrag |]
            )

        Html.div [
            prop.ref workspaceElementRef
            prop.className [
                "swt:relative swt:flex swt:flex-col swt:size-full swt:overflow-hidden"
                match className with
                | Some c -> c
                | None -> ()
            ]
            if debug then
                prop.testId "workspace-root"
            prop.children [
                WorkspaceDndCtx.Provider(
                    dndCtxValue,
                    DndKit.DndContext(
                        sensors = sensors,
                        collisionDetection = DndKit.closestCenter,
                        children = Workspace.WorkspaceInner(activeDrag, workspaceElementRef)
                    )
                )
            ]
        ]

    [<ReactComponent>]
    static member Entry() =

        let genPayload () =
            let short = Guid.NewGuid().ToString("N").Substring(0, 8)
            $"Payload {short}"

        let initialTabs = [|
            { Id = TabId "tab-1"; Label = "Main.tsx"; Payload = genPayload () }
            { Id = TabId "tab-2"; Label = "utils.ts"; Payload = genPayload () }
            { Id = TabId "tab-3"; Label = "styles.css"; Payload = genPayload () }
        |]

        let renderTab (tab: Tab<string>) =
            Html.span tab.Label

        let renderTabContent (tab: Tab<string>) =
            Html.div [
                prop.className "swt:p-4 swt:size-full swt:flex swt:flex-col swt:gap-2"
                prop.children [
                    Html.h3 [
                        prop.className "swt:text-lg swt:font-semibold"
                        prop.text tab.Label
                    ]
                    Html.p [
                        prop.className "swt:text-sm swt:text-base-content/60"
                        prop.text (sprintf "Tab ID: %s" tab.Id.Value)
                    ]
                    Html.p [
                        prop.className "swt:text-sm swt:text-base-content/40"
                        prop.text tab.Payload
                    ]
                ]
            ]

        Workspace.WorkspaceProvider(
            renderTabContent = renderTabContent,
            renderTab = renderTab,
            initialTabs = initialTabs,
            debug = true,
            children =
                Html.div [
                    prop.className "swt:flex swt:flex-col swt:size-full swt:overflow-hidden"
                    prop.children [
                        Workspace.Toolbar()
                        Workspace.Workspace<string>(className = "swt:flex-1 swt:min-h-0")
                    ]
                ]
        )

    [<ReactComponent>]
    static member private Toolbar() =
        let dispatchCtx = useWorkspaceDispatchCtx<string> ()
        let tabCounter = React.useRef 4

        let addTab _ =
            let n = tabCounter.current
            tabCounter.current <- n + 1
            let tab = {
                Id = TabId (sprintf "tab-%d" n)
                Label = sprintf "NewFile%d.tsx" n
                Payload = sprintf "Payload %s" (Guid.NewGuid().ToString("N").Substring(0, 8))
            }
            dispatchCtx.dispatch (AddTab tab)

        Html.div [
            prop.className "swt:flex swt:gap-2 swt:p-2 swt:border-b swt:border-base-content/20 swt:bg-base-200"
            prop.children [
                Html.button [
                    prop.className "swt:btn swt:btn-sm swt:btn-primary"
                    prop.text "Add Tab"
                    prop.onClick addTab
                ]
                Html.button [
                    prop.className "swt:btn swt:btn-sm swt:btn-ghost"
                    prop.text "Close All"
                    prop.onClick (fun _ ->
                        dispatchCtx.dispatch RemoveAllTabs
                    )
                ]
                Html.span [
                    prop.className "swt:text-xs swt:text-base-content/50 swt:self-center swt:ml-auto"
                    prop.text "Drag tab to pane edge to split"
                ]
            ]
        ]
