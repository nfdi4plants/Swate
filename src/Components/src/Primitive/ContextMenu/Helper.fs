module Swate.Components.Primitive.ContextMenu.Helper

open Feliz
open Swate.Components.Primitive.ContextMenu.Types

let create (label: string) (icon: string) (onClick: unit -> unit) =
    ContextMenuItem(
        text = Html.span label,
        icon = Html.i [ prop.className $"swt:iconify {icon}" ],
        label = label,
        iconClass = icon,
        onClick = (fun _ -> onClick ())
    )

let styled (label: string) (icon: string) (className: string) (onClick: unit -> unit) =
    ContextMenuItem(
        text = Html.span [ prop.className className; prop.text label ],
        icon = Html.i [ prop.className $"swt:iconify {icon} {className}" ],
        className = className,
        label = label,
        iconClass = icon,
        onClick = (fun _ -> onClick ())
    )

let disabled (label: string) (icon: string) =
    ContextMenuItem(
        text = Html.span label,
        icon = Html.i [ prop.className $"swt:iconify {icon}" ],
        label = label,
        iconClass = icon,
        disabled = true
    )

let divider = ContextMenuItem(isDivider = true)

let forItem label icon onClick item =
    create label icon (fun () -> onClick item)

let whenItem predicate label icon onClick item =
    if predicate item then
        [ forItem label icon onClick item ]
    else
        []
