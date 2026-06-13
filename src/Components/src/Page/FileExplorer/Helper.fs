module Swate.Components.Page.FileExplorer.Helper

open Swate.Components.Page.FileExplorer.Types

let iconClassName (baseClasses: string list) (item: FileItem) (getItemIconClass: FileItem -> string option) = [
    yield! baseClasses
    yield item.Icon |> FileItemIcon.className
    yield! item.IconTone |> Option.map FileItemIconTone.className |> Option.toList
    yield! getItemIconClass item |> Option.toList
]
