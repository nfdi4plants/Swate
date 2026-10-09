module Swate.Components.Primitive.ContextMenu.Types

open Fable.Core
open Feliz

[<AllowNullLiteral>]
type ContextMenuClickEvent =
    abstract currentTarget: Browser.Types.HTMLElement
    abstract target: Browser.Types.EventTarget
    abstract nativeEvent: Browser.Types.MouseEvent
    abstract preventDefault: unit -> unit
    abstract stopPropagation: unit -> unit

[<JS.Pojo; AllowNullLiteral>]
type ContextMenuItem
    (
        ?text: ReactElement,
        ?icon: ReactElement,
        ?label: string,
        ?kbdbutton:
            {|
                element: ReactElement
                label: string
            |},
        ?isDivider: bool,
        ?onClick:
            {|
                buttonEvent: ContextMenuClickEvent
                spawnData: obj
            |}
                -> unit
    ) =
    member val text = text with get, set
    member val label = label with get, set
    member val icon = icon with get, set
    member val kbdbutton = kbdbutton with get, set
    member val isDivider: bool = defaultArg isDivider false with get, set
    member val onClick = onClick with get, set
