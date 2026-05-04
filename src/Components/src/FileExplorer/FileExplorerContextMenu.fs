module Swate.Components.FileExplorerContextMenu

open Fable.Core
open Fable.Core.JsInterop
open Feliz
open Swate.Components
open Swate.Components.FileExplorerTypes

let private copyPathToClipboard (path: string) =
    promise {
        try
            let windowObj: obj = Browser.Dom.window
            do! windowObj?navigator?clipboard?writeText (path)
        with ex ->
            Browser.Dom.console.warn ($"Could not copy file path: {path}", ex)
    }
    |> Promise.start

let private defaultContextMenuItems
    (
        item: FileItem,
        model: FileExplorerLogic.Model,
        onItemClick: (FileItem -> unit) option,
        dispatch: FileExplorerLogic.Msg -> unit
    ) : ContextMenuItem list =
    let canExpandDirectory =
        match item.Children with
        | Some children -> not (List.isEmpty children)
        | None -> true

    [
        if not item.IsDirectory then
            {
                Label = "Open"
                Icon = "swt:fluent--open-24-regular"
                OnClick = fun () -> FileExplorerItemHelper.handleItemClick (item, onItemClick, dispatch)
                Disabled = None
            }

        match item.Path with
        | Some path ->
            {
                Label = "Copy Path"
                Icon = "swt:fluent--copy-24-regular"
                OnClick = fun () -> copyPathToClipboard path
                Disabled = None
            }
        | None -> ()

        if item.IsDirectory && canExpandDirectory then
            let isExpanded = model.ExpandedIds.Contains item.Id

            {
                Label = if isExpanded then "Collapse" else "Expand"
                Icon =
                    if isExpanded then
                        "swt:fluent--folder-open-24-regular"
                    else
                        "swt:fluent--folder-24-regular"
                OnClick = fun () -> dispatch (FileExplorerLogic.ToggleExpanded item.Id)
                Disabled = None
            }
    ]

let getContextMenuItems
    (
        item: FileItem,
        model: FileExplorerLogic.Model,
        onItemClick: (FileItem -> unit) option,
        onContextMenu: (FileItem -> Swate.Components.FileExplorerTypes.ContextMenuItem list) option,
        dispatch: FileExplorerLogic.Msg -> unit
    ) =
    let customItems =
        onContextMenu |> Option.map (fun fn -> fn item) |> Option.defaultValue []

    defaultContextMenuItems (item, model, onItemClick, dispatch) @ customItems

let toComponentMenuItem (item: Swate.Components.FileExplorerTypes.ContextMenuItem) =
    let isDisabled = defaultArg item.Disabled false
    let className = if isDisabled then "swt:opacity-50" else ""

    Swate.Components.ContextMenuItem(
        text = Html.span [ prop.className className; prop.text item.Label ],
        icon =
            Html.i [
                prop.className [
                    "swt:iconify " + item.Icon

                    if isDisabled then
                        "swt:opacity-50"
                ]
            ],
        onClick =
            (fun _ ->
                if not isDisabled then
                    item.OnClick()
            )
    )
