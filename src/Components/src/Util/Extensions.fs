namespace Swate.Components

open Feliz

[<AutoOpen>]
module Extensions =

    let inline log e = Browser.Dom.console.log e

    type prop with
        static member testid (value: string) = prop.custom("data-testid", value)

[<RequireQualifiedAccess>]
module kbdEventCode =
    [<Literal>]
    let escape = "Escape"

    [<Literal>]
    let enter = "Enter"

    [<Literal>]
    let arrowDown = "ArrowDown"

    [<Literal>]
    let arrowUp = "ArrowUp"

    [<Literal>]
    let arrowLeft = "ArrowLeft"

    [<Literal>]
    let arrowRight = "ArrowRight"

    [<Literal>]
    let tab = "Tab"

    [<Literal>]
    let shift = "Shift"

    [<Literal>]
    let delete = "Delete"

    [<Literal>]
    let backspace = "Backspace"

    let key (key:string) = key.ToUpper() |> sprintf "Key%s"

