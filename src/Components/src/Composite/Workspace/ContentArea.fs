namespace Swate.Components.Composite.Workspace

open Fable.Core
open Fable.Core.JsInterop
open Feliz
open Swate.Components
open Swate.Components.JsBindings
open Swate.Components.Composite.Workspace.Types
open Swate.Components.Composite.Workspace.Context
open Swate.Components.Composite.Workspace.Helper.PaneTree
open Swate.Components.Composite.Workspace.Helper.DndId

module private ContentAreaHelper =

    let edgeZoneClass (dir: EdgeDirection) =
        let abs =
            match dir with
            | EdgeDirection.Top -> "swt:absolute swt:top-0 swt:left-0 swt:right-0 swt:h-1/4"
            | EdgeDirection.Bottom -> "swt:absolute swt:bottom-0 swt:left-0 swt:right-0 swt:h-1/4"
            | EdgeDirection.Left -> "swt:absolute swt:top-0 swt:left-0 swt:bottom-0 swt:w-1/4"
            | EdgeDirection.Right -> "swt:absolute swt:top-0 swt:right-0 swt:bottom-0 swt:w-1/4"

        $"swt:z-20 {abs}"

    let edgeOverlayClass (dir: EdgeDirection) isOver =
        let baseClass =
            match dir with
            | EdgeDirection.Top -> "swt:border-b-4"
            | EdgeDirection.Bottom -> "swt:border-t-4"
            | EdgeDirection.Left -> "swt:border-r-4"
            | EdgeDirection.Right -> "swt:border-l-4"
        if isOver then
            $"swt:border-primary swt:bg-primary/20 {baseClass} swt:absolute swt:inset-0 swt:rounded swt:pointer-events-none"
        else
            "swt:hidden"

open ContentAreaHelper

[<Erase; Mangle(false)>]
type ContentArea =

    [<ReactComponent>]
    static member EdgeDropZone(paneId: string, dir: EdgeDirection, isEnabled: bool, ?debug: bool) =
        let debug = defaultArg debug false
        let edgeId = DndId.write (EdgeZone(paneId, dir))
        let droppable = DndKit.useDroppable ({| id = edgeId; disabled = not isEnabled |})

        if not isEnabled then
            Html.none
        else
            Html.div [
                prop.ref droppable.setNodeRef
                prop.className (edgeZoneClass dir)
                if debug then
                    prop.testId $"workspace-edge-{paneId}-{EdgeDirection.toString dir}"
                prop.children [
                    Html.div [
                        prop.className (edgeOverlayClass dir droppable.isOver)
                    ]
                ]
            ]

    [<ReactComponent>]
    static member ContentArea(paneId: string) =
        let paneCtx = usePaneCtx ()
        let workspaceCtx = useWorkspaceCtx ()
        let dndCtx = useWorkspaceDndCtx ()

        let contentMap = workspaceCtx.contentMap
        let activeTabId = workspaceCtx.activeTabId
        let layout = workspaceCtx.layout

        let canSplit dir =
            let splitDir =
                match dir with
                | EdgeDirection.Top | EdgeDirection.Bottom -> SplitDirection.Vertical
                | EdgeDirection.Left | EdgeDirection.Right -> SplitDirection.Horizontal
            Pane.canSplitLeaf layout paneId splitDir

        Html.div [
            prop.className "swt:relative swt:min-h-0 swt:flex-1 swt:overflow-hidden"
            if workspaceCtx.debug then
                prop.testId $"workspace-content-{paneId}"
            prop.children [
                yield! [
                    for (tabId, content) in contentMap |> Map.toArray do
                        Html.div [
                            prop.key tabId
                            prop.className "swt:h-full swt:w-full"
                            prop.style [
                                if Some tabId <> activeTabId then
                                    style.display.none
                            ]
                            prop.children content
                        ]
                ]
                match activeTabId with
                | None ->
                    Html.div [
                        prop.className "swt:flex swt:size-full swt:items-center swt:justify-center swt:text-base-content/40 swt:text-sm"
                        prop.text "No open editors"
                    ]
                | _ -> ()

                if dndCtx.isDragging then
                    // TODO: Make this a single component, which moves the overlay to the correct edge zone based on the current drag position
                    for dir in [ EdgeDirection.Top; EdgeDirection.Bottom; EdgeDirection.Left; EdgeDirection.Right ] do
                        ContentArea.EdgeDropZone(paneId, dir, canSplit dir, workspaceCtx.debug)
            ]
        ]
