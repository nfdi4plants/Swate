module Swate.Components.Page.FileExplorer.Helper

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

