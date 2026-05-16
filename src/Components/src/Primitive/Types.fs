[<AutoOpen>]
module Swate.Components.Primitives.Types

[<RequireQualifiedAccess>]
type DaisyuiSize =
    | XS
    | SM
    | MD
    | LG
    | XL

[<RequireQualifiedAccess>]
type DaisyuiColors =
    | Primary
    | Secondary
    | Accent
    | Info
    | Success
    | Warning
    | Error

[<RequireQualifiedAccess>]
type DaisyuiTooltipPosition =
    | Top
    | Right
    | Bottom
    | Left


type StateContext<'T> = { state: 'T; setState: 'T -> unit }

type StateUpdaterContext<'T> = {
    state: 'T
    setStateUpdater: ('T -> 'T) -> unit
}

module StateContext =
    let init initialState = {
        state = initialState
        setState = fun _ -> ()
    }