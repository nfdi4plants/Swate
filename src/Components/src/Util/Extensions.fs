namespace Swate.Components

open Feliz

[<AutoOpen>]
module Extensions =

    type prop with
        static member testid (value: string) = prop.custom("data-testid", value)

        static member dataRow (value: int) = prop.custom("data-row", value)
        static member dataColumn (value: int) = prop.custom("data-column", value)

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

open Fable.Core
open Fable.Core.JsInterop
[<Fable.Core.Global>]
type console =
    [<Emit("console.log($0)")>]
    static member inline log e = jsNative