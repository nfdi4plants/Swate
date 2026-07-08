namespace Swate.Components.Composite.Workspace

open Fable.Core
open Fable.Core.JsInterop
open Feliz
open Swate.Components
open Swate.Components.JsBindings
open Swate.Components.Composite.Workspace.Types
open Swate.Components.Composite.Workspace.Context
open Swate.Components.Composite.Workspace.Helper.DndId

module private TabBarHelper =

    let dndObjectProps (obj: Swate.Components.JsBindings.IObject) : IReactProperty list =
        [ for key in Swate.Components.JsBindings.Object.keys obj do
              prop.custom (key, obj.get key) ]

open TabBarHelper

[<Erase; Mangle(false)>]
type TabBar =

    [<ReactComponent>]
    static member private Tab
        (tab: Tab<obj>, index: int, paneIdKey: string, isActive: bool, ?key: string)
        =
        let dispatchCtx = useWorkspaceDispatchCtx ()
        let paneStateCtx = useWorkspacePaneStateCtx ()
        let dragId = DndId.write (DndId.Tab(paneIdKey, tab.Id.Value))

        let sortable = DndKit.useSortable ({| id = dragId |})

        let style = [
            style.custom ("transform", DndKit.CSS.Transform.toString sortable.transform)
            style.custom ("transition", sortable.transition)
            style.cursor.grab
        ]

        let dndProps =
            [
                prop.ref sortable.setNodeRef
                prop.style style
            ]
            @ dndObjectProps sortable.attributes
            @ dndObjectProps sortable.listeners

        let tabClass = [
            "swt:tab swt:items-center swt:min-w-fit swt:gap-1 swt:flex-nowrap swt:select-none"
            if isActive then "swt:tab-active"
            if sortable.isDragging then "swt:opacity-30"
        ]

        Html.div [
            match key with
            | Some k -> prop.key k
            | None -> ()
            prop.className tabClass
            prop.custom ("data-workspace-tab-id", tab.Id.Value)
            prop.custom ("data-workspace-pane-id", paneIdKey)
            yield! dndProps
            prop.onMouseUp (fun e ->
                match e.button with
                | 0. ->
                    dispatchCtx.dispatch (box (FocusTab tab.Id))
                | 1. ->
                    dispatchCtx.dispatch (box (RemoveTab tab.Id))
                | _ -> ()
            )
            prop.children [
                paneStateCtx.renderTab (box tab)
                Html.button [
                    prop.type'.button
                    prop.className
                        "swt:ml-1 swt:rounded-sm swt:p-0.5 swt:hover:bg-base-300 swt:inline-flex swt:items-center swt:justify-center"
                    prop.ariaLabel $"Close {tab.Label}"
                    prop.onClick (fun e ->
                        e.stopPropagation ()
                        dispatchCtx.dispatch (box (RemoveTab tab.Id))
                    )
                    prop.onMouseUp (fun e -> e.stopPropagation ())
                    prop.children [
                        Html.i [
                            prop.className "swt:iconify swt:fluent--dismiss-12-filled swt:size-3.5"
                        ]
                    ]
                ]
            ]
        ]

    [<ReactComponent>]
    static member TabBar(paneId: PaneId, ?key: string) =
        let paneCtx = usePaneCtx ()
        let paneStateCtx = useWorkspacePaneStateCtx ()
        let dispatchCtx = useWorkspaceDispatchCtx ()

        let tabs = paneCtx.tabs
        let paneIdKey = paneId.Value.ToString("N")

        let dragIds =
            React.useMemo (
                (fun () ->
                    tabs
                    |> Array.map (fun tab -> DndId.write (DndId.Tab(paneIdKey, tab.Id.Value)))
                    |> ResizeArray
                ),
                [| box tabs; box paneIdKey |]
            )

        let tabBarDroppable = DndKit.useDroppable ({| id = DndId.write (DndId.TabBar paneIdKey) |})

        Html.div [
            prop.ref tabBarDroppable.setNodeRef
            prop.className [
                "swt:tabs swt:tabs-lift swt:w-full swt:overflow-x-auto swt:overflow-y-hidden swt:flex swt:flex-row swt:items-center swt:justify-start swt:pt-1 swt:border-b swt:border-base-content/50 swt:flex-nowrap swt:gap-0"
                if tabBarDroppable.isOver then
                    "swt:bg-primary/10"
            ]
            if paneStateCtx.debug then
                prop.testId $"workspace-tabbar-{paneIdKey}"
            prop.children [
                DndKit.SortableContext(
                    items = dragIds,
                    strategy = DndKit.horizontalListSortingStrategy,
                    children =
                        React.Fragment [
                            for tab in tabs do
                                let index = tabs |> Array.findIndex (fun t -> t.Id = tab.Id)
                                let isActive = paneCtx.focusedTab = Some tab.Id
                                TabBar.Tab(tab, index, paneIdKey, isActive, key = $"{paneIdKey}:{tab.Id.Value}")
                        ]
                )
            ]
        ]
