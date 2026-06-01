module Swate.Components.Page.FileExplorer.Helper

open Feliz
open Swate.Components.Page.FileExplorer.Types

let handleItemClick (item: FileItem) (onItemClick: (FileItem -> unit) option) (dispatch: FileExplorerLogic.Msg -> unit) =
    dispatch (FileExplorerLogic.SelectItem item.Id)
    onItemClick |> Option.iter (fun fn -> fn item)

let iconClassName (baseClasses: string list) (item: FileItem) (getItemIconClass: FileItem -> string option) =
    [
        yield! baseClasses
        yield item.Icon |> FileItemIcon.className
        yield! item.IconTone |> Option.map FileItemIconTone.className |> Option.toList
        yield! getItemIconClass item |> Option.toList
    ]

let toPrimitiveContextMenuItem (item: ContextMenuItem) =
    if defaultArg item.IsDivider false then
        Swate.Components.Primitive.ContextMenu.Types.ContextMenuItem(isDivider = true)
    else
        let isDisabled = defaultArg item.Disabled false

        let className =
            [
                item.ClassName

                if isDisabled then
                    Some "swt:opacity-50"
            ]
            |> List.choose id
            |> String.concat " "

        Swate.Components.Primitive.ContextMenu.Types.ContextMenuItem(
            text = Html.span [ prop.className className; prop.text item.Label ],
            icon =
                Html.i [
                    prop.className [
                        "swt:iconify " + item.Icon

                        if not (System.String.IsNullOrWhiteSpace className) then
                            className
                    ]
                ],
            onClick =
                (fun _ ->
                    if not isDisabled then
                        item.OnClick())
        )
