module Spreadsheet.Controller.Clipboard

open Fable.Core
open ARCtrl
open Swate.Components
open Swate.Components.Shared
open Swate.Components.ClipboardCodec

let private getCellsByIndex (indices: CellCoordinate[]) (state: Spreadsheet.Model) =
    indices |> Array.map (fun index -> Generic.getCell (index.x, index.y) state)

let private writeCells plainText (cells: CompositeCell[]) =
    let rows = cells |> Array.map Array.singleton
    ClipboardCodec.write plainText (Some rows)

let copyCellByIndex (index: CellCoordinate) (state: Spreadsheet.Model) : JS.Promise<unit> =
    let cell = Generic.getCell (index.x, index.y) state
    writeCells (cell.ToClipboardStr()) [| cell |]

let copyCellsByIndex (indices: CellCoordinate[]) (state: Spreadsheet.Model) : JS.Promise<unit> =
    let cells = getCellsByIndex indices state
    writeCells (CompositeCell.ToTabTxt cells) cells

let cutCellByIndex (index: CellCoordinate) (state: Spreadsheet.Model) : Spreadsheet.Model =
    let cell = Generic.getCell (index.x, index.y) state
    Table.clearCells [| index |] state |> ignore
    writeCells (cell.ToClipboardStr()) [| cell |] |> Promise.start
    state

let cutCellsByIndices (indices: CellCoordinate[]) (state: Spreadsheet.Model) : Spreadsheet.Model =
    let cells = getCellsByIndex indices state
    Table.clearCells indices state |> ignore
    writeCells (CompositeCell.ToTabTxt cells) cells |> Promise.start

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
