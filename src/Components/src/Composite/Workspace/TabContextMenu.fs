namespace Swate.Components.Composite.Workspace

open Fable.Core
open Fable.Core.JsInterop
open Feliz
open Browser.Types
open Swate.Components
open Swate.Components.Primitive.ContextMenu
open Swate.Components.Primitive.ContextMenu.Types
open Swate.Components.Composite.Workspace.Types
open Swate.Components.Composite.Workspace.Context

module private TabContextMenuHelper =

    let dataKey = "data-workspace-tab-id"

    let tryGetSpawnData (e: MouseEvent) : ContextMenuSpawnData option =
        let target = e.target :?> HTMLElement
        target.closest ($"[{dataKey}]")
        |> Option.bind (fun el ->
            let el = el :?> HTMLElement
            let tabId = el?dataset?workspaceTabId
            let paneId = el?dataset?workspacePaneId
            match tabId, paneId with
            | null, _ | _, null -> None
            | tabId, paneId ->
                Some { tabId = string tabId; paneId = string paneId }
        )

open TabContextMenuHelper

[<Erase; Mangle(false)>]
type TabContextMenu =

    [<ReactComponent>]
    static member TabContextMenu
        (containerRef: IRefValue<Browser.Types.HTMLElement option>)
        =
        let ctx = useWorkspaceCtx ()

        let onSpawn (e: MouseEvent) : obj option =
            tryGetSpawnData e
            |> Option.map box

        let childInfo (data: obj) : ContextMenuItem list =
            let spawnData = unbox<ContextMenuSpawnData> data
            let tabId = spawnData.tabId
            let paneId = spawnData.paneId

            let tabs =
                ctx.panes
                |> Map.tryFind paneId
                |> Option.map (fun ps -> ps.tabs)
                |> Option.defaultValue [||]

            let close _ = ctx.closeTab tabId

            let closeOthers _ = ctx.closeOthers tabId

            let closeAll _ = ctx.closeAll ()

            let closeAllInPane _ = ctx.closeAllInPane paneId

            [
                ContextMenuItem(
                    text = Html.span "Close",
                    icon = Html.i [ prop.className "swt:iconify swt:fluent--dismiss-12-filled swt:size-4" ],
                    onClick = close
                )
                ContextMenuItem(
                    text = Html.span "Close Others",
                    icon = Html.i [ prop.className "swt:iconify swt:fluent--tab-desktop-multiple-bottom-20-regular swt:size-4" ],
                    onClick = closeOthers
                )
                ContextMenuItem(
                    text = Html.span "Close All",
                    icon = Html.i [ prop.className "swt:iconify swt:fluent--delete-20-filled swt:size-4" ],
                    onClick = closeAll
                )
                ContextMenuItem(
                    isDivider = true
                )
                ContextMenuItem(
                    text = Html.span "Close All in Pane",
                    icon = Html.i [ prop.className "swt:iconify swt:fluent--tab-desktop-20-regular swt:size-4" ],
                    onClick = closeAllInPane
                )
            ]

        ContextMenu.ContextMenu(childInfo, containerRef, onSpawn = onSpawn)
