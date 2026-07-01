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

    [<Literal>]
    let TabIdDataKey = "workspace-tab-id"

    [<Literal>]
    let PaneIdDataKey = "workspace-pane-id"

    let dndObjectProps (obj: Swate.Components.JsBindings.IObject) : IReactProperty list =
        [ for key in Swate.Components.JsBindings.Object.keys obj do
            prop.custom (key, obj.get key) ]

    type prop with
        static member dataWorkspaceTabId (tabId: string) : IReactProperty =
            unbox ($"data-{TabIdDataKey}", tabId)

        static member dataWorkspacePaneId (paneId: string) : IReactProperty =
            unbox ($"data-{PaneIdDataKey}", paneId)

open TabBarHelper

[<Erase; Mangle(false)>]
type TabBar =

    [<ReactComponent>]
    static member private Tab
        (tab: WorkspaceTab, index: int, paneId: string, isActive: bool, onClose: string -> unit, ?key: string)
        =
        let workspaceCtx = useWorkspaceCtx ()
        let dragId = DndId.write (DndId.Tab(paneId, tab.Id))

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
            prop.dataWorkspaceTabId tab.Id
            prop.dataWorkspacePaneId paneId
            yield! dndProps
            prop.onClick (fun _ -> workspaceCtx.setActiveTabId (Some tab.Id))
            prop.children [
                match tab.Icon with
                | Some icon ->
                    Html.i [ prop.className [ icon; "swt:min-w-4" ] ]
                | None -> ()
                Html.span tab.Label
                Html.button [
                    prop.type'.button
                    prop.className
                        "swt:ml-1 swt:rounded-sm swt:p-0.5 swt:hover:bg-base-300 swt:inline-flex swt:items-center swt:justify-center"
                    prop.ariaLabel $"Close {tab.Label}"
                    prop.onClick (fun e ->
                        e.stopPropagation ()
                        onClose tab.Id
                    )
                    prop.children [
                        Html.i [
                            prop.className "swt:iconify swt:fluent--dismiss-12-filled swt:size-3.5"
                        ]
                    ]
                ]
            ]
        ]

    [<ReactComponent>]
    static member TabBar(paneId: string, ?key: string) =
        let paneCtx = usePaneCtx ()
        let workspaceCtx = useWorkspaceCtx ()

        let tabs = paneCtx.tabs
        let tabOrder = paneCtx.tabOrder
        let activeTabId = workspaceCtx.activeTabId

        let dragIds =
            React.useMemo (
                (fun () -> tabOrder |> Array.map (fun tabId -> DndId.write (DndId.Tab(paneId, tabId))) |> ResizeArray),
                [| box tabOrder; box paneId |]
            )

        let tabBarDroppable = DndKit.useDroppable ({| id = DndId.write (DndId.TabBar paneId) |})

        Html.div [
            prop.ref tabBarDroppable.setNodeRef
            prop.className [
                "swt:tabs swt:tabs-lift swt:w-full swt:overflow-x-auto swt:overflow-y-hidden swt:flex swt:flex-row swt:items-center swt:justify-start swt:pt-1 swt:border-b swt:border-base-content/50 swt:flex-nowrap swt:gap-0"
                if tabBarDroppable.isOver then
                    "swt:bg-primary/10"
            ]
            if workspaceCtx.debug then
                prop.testId $"workspace-tabbar-{paneId}"
            prop.children [
                DndKit.SortableContext(
                    items = dragIds,
                    strategy = DndKit.horizontalListSortingStrategy,
                    children =
                        React.Fragment [
                            for tabId in tabOrder do
                                match tabs |> Array.tryFind (fun t -> t.Id = tabId) with
                                | Some tab ->
                                    let index = tabs |> Array.findIndex (fun t -> t.Id = tabId)
                                    let isActive = activeTabId = Some tabId
                                    TabBar.Tab(
                                        tab,
                                        index,
                                        paneId,
                                        isActive,
                                        paneCtx.closeTab,
                                        key = $"{paneId}:{tabId}"
                                    )
                                | None -> ()
                        ]
                )
            ]
        ]
