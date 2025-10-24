namespace Swate.Components

open Feliz
open ARCtrl

[<AutoOpen>]
module ARCtrl =
    type ArcTable with
        member this.ClearCell(cellIndex: CellCoordinate) =
            let index = (cellIndex.x - 1, cellIndex.y - 1)
            let c = this.Values.Item(index).GetEmptyCell()
            this.SetCellAt(cellIndex.x - 1, cellIndex.y - 1, c)

        member this.ClearSelectedCells(selectHandle: SelectHandle) =
            let selectedCells = selectHandle.getSelectedCells ()
            selectedCells
            |> Seq.iter (fun index ->
                let tempIndex = (index.x - 1, index.y - 1)
                let c = this.Values.Item(tempIndex).GetEmptyCell()
                this.SetCellAt(index.x - 1, index.y - 1, c))

[<AutoOpen>]
module Extensions =

    type prop with
        static member inline testid(value: string) : IReactProperty = unbox ("data-testid", value)

        static member inline dataRow(value: int) : IReactProperty = unbox ("data-row", value)
        static member inline dataColumn(value: int) : IReactProperty = unbox ("data-column", value)

        static member inline spread(props: obj) : IReactProperty[] =
            props
            |> Fable.Core.JS.Constructors.Object.entries
            |> Seq.map (fun (k, v) -> unbox<IReactProperty> (k, v))
            |> Array.ofSeq

        static member inline style(value: obj) : IReactProperty = unbox ("style", value)

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

    [<Literal>]
    let f2 = "F2"

    let key (key: string) = key.ToUpper() |> sprintf "Key%s"

open Fable.Core
open Fable.Core.JsInterop

[<Fable.Core.Global>]
type console =
    [<Emit("console.log($0)")>]
    static member inline log e = jsNative

    [<Emit("console.warn($0)")>]
    static member inline warn e = jsNative

    [<Emit("console.error($0)")>]
    static member inline error e = jsNative

[<Erase>]
type Clipboard =
    abstract member writeText: string -> JS.Promise<unit>
    abstract member readText: unit -> JS.Promise<string>

[<Erase>]
type Navigator =
    abstract member clipboard: Clipboard

[<AutoOpen>]
module GlobalBindings =

    [<Emit("navigator")>]
    let navigator: Navigator = jsNative