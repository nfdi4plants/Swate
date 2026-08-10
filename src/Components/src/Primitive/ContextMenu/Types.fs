module Swate.Components.Primitive.ContextMenu.Types

open Fable.Core
open Feliz

[<Global; AllowNullLiteral>]
type ContextMenuItem
    [<ParamObjectAttribute; Emit("$0")>]
    (
        ?text: ReactElement,
        ?icon: ReactElement,
        ?kbdbutton:
            {|
                element: ReactElement
                label: string
            |},
        ?isDivider: bool,
        ?disabled: bool,
        ?className: string,
        ?label: string,
        ?iconClass: string,
        ?onClick:
            {|
                buttonEvent: Browser.Types.MouseEvent
                spawnData: obj
            |}
                -> unit
    ) =
    member val text = text with get, set
    member val icon = icon with get, set
    member val kbdbutton = kbdbutton with get, set
    member val isDivider: bool = defaultArg isDivider false with get, set
    member val disabled: bool = defaultArg disabled false with get, set
    member val className = className with get, set
    member val label = label with get, set
    member val iconClass = iconClass with get, set
    member val onClick = onClick with get, set
    member this.Label = defaultArg this.label ""
    member this.Icon = defaultArg this.iconClass ""
    member this.Disabled = if this.disabled then Some true else None
    member this.ClassName = this.className
    member this.IsDivider = if this.isDivider then Some true else None

    member this.OnClick() =
        this.onClick |> Option.iter (fun handler -> handler (unbox null))
