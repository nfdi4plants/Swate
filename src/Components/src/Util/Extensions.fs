namespace Swate.Components

open Feliz
open ARCtrl

[<AutoOpen>]
module ARCtrl =
    type ArcTable with
        member this.ClearCell(cellIndex) =
            let c = this.Values.[cellIndex]
            this.Values.[cellIndex] <- c.GetEmptyCell()

        member this.ClearSelectedCells(selectHandle: SelectHandle) =
            match selectHandle.getCount() with
            | c when c <= 100 ->
                let selectedCells = selectHandle.getSelectedCells()
                selectedCells |> Seq.iter (fun i ->
                    let c = this.Values.[(i.x - 1, i.y - 1)]
                    this.Values.[(i.x - 1, i.y - 1)] <- c.GetEmptyCell()
                )
            | c ->
                for (x,y) in this.Values.Keys do
                    if selectHandle.contains ({|x = x + 1; y = y + 1|}) then
                        let c = this.Values.[(x, y)]
                        this.Values.[(x, y)] <- c.GetEmptyCell()

[<AutoOpen>]
module Extensions =

    type prop with
        static member testid(value: string) = prop.custom ("data-testid", value)

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