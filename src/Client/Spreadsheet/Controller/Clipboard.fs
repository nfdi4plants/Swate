module Spreadsheet.Controller.Clipboard

open Fable.Core
open ARCtrl
open Swate.Components
open Swate.Components.Shared
open Swate.Components.ClipboardCodec

let copyCellByIndex (index: CellCoordinate) (state: Spreadsheet.Model) : JS.Promise<unit> =
    let cell = Generic.getCell (index.x, index.y) state
    let rows = [| [| cell |] |]
    ClipboardCodec.write (cell.ToClipboardStr()) (Some rows)

let copyCellsByIndex (indices: CellCoordinate[]) (state: Spreadsheet.Model) : JS.Promise<unit> =
    let cells = [|
        for index in indices do
            yield Generic.getCell (index.x, index.y) state
    |]

    let rows = cells |> Array.map Array.singleton
    ClipboardCodec.write (CompositeCell.ToTabTxt cells) (Some rows)

let cutCellByIndex (index: CellCoordinate) (state: Spreadsheet.Model) : Spreadsheet.Model =
    let cell = Generic.getCell (index.x, index.y) state
    // Remove selected cell value
    let emptyCell = cell.GetEmptyCellFixed()
    Generic.setCell (index.x, index.y) emptyCell state
    let rows = [| [| cell |] |]
    ClipboardCodec.write (cell.ToClipboardStr()) (Some rows) |> Promise.start
    state

let cutCellsByIndices (indices: CellCoordinate[]) (state: Spreadsheet.Model) : Spreadsheet.Model =
    let cells = ResizeArray()

    for index in indices do
        let cell = Generic.getCell (index.x, index.y) state
        // Remove selected cell value
        let emptyCell = cell.GetEmptyCellFixed()
        Generic.setCell (index.x, index.y) emptyCell state
        cells.Add(cell)

    let rows = cells |> Seq.map Array.singleton |> Seq.toArray
    ClipboardCodec.write (CompositeCell.ToTabTxt(Array.ofSeq cells)) (Some rows) |> Promise.start
    state

let pasteCellByIndex (index: CellCoordinate) (state: Spreadsheet.Model) : JS.Promise<Spreadsheet.Model> = promise {
    let! content = ClipboardCodec.read ()
    let header = Generic.getHeader index.x state

    let cell =
        content.Payload
        |> Option.bind (fun payload -> payload.Rows |> Array.tryHead |> Option.bind Array.tryHead)
        |> Option.map (ClipboardCodec.toCompositeCell >> _.ConvertToValidCell(header))
        |> Option.defaultWith (fun () -> CompositeCell.fromTabTxt content.PlainText header |> Array.head)

    Generic.setCell (index.x, index.y) cell state
    return state
}

let pasteCellsByIndexExtend (index: CellCoordinate) (state: Spreadsheet.Model) : JS.Promise<Spreadsheet.Model> = promise {
    let! content = ClipboardCodec.read ()
    let header = Generic.getHeader index.x state

    let cells =
        content.Payload
        |> Option.map (fun payload ->
            payload.Rows
            |> Array.choose Array.tryHead
            |> Array.map (ClipboardCodec.toCompositeCell >> _.ConvertToValidCell(header))
        )
        |> Option.defaultWith (fun () -> CompositeCell.fromTabTxt content.PlainText header)

    let indexedCells =
        cells
        |> Array.indexed
        |> Array.map (fun (i, c) ->
            let coordinate: CellCoordinate = {| x = index.x; y = index.y + i |}
            (coordinate, c)
        )

    Generic.setCells indexedCells state
    return state
}
