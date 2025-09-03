module Spreadsheet.Controller.Clipboard

open Fable.Core
open ARCtrl
open Swate.Components
open Swate.Components.Shared

let copyCell (cell: CompositeCell) : JS.Promise<unit> =
    let tab = cell.ToTabStr()
    navigator.clipboard.writeText (tab)

let copyCells (cells: CompositeCell[]) : JS.Promise<unit> =
    let tab = CompositeCell.ToTabTxt cells
    navigator.clipboard.writeText (tab)

let copyCellByIndex (index: CellCoordinate) (state: Spreadsheet.Model) : JS.Promise<unit> =
    let cell = Generic.getCell (index.x, index.y) state
    copyCell cell

let copyCellsByIndex (indices: CellCoordinate[]) (state: Spreadsheet.Model) : JS.Promise<unit> =
    let cells = [|
        for index in indices do
            yield Generic.getCell (index.x, index.y) state
    |]

    copyCells cells

let cutCellByIndex (index: CellCoordinate) (state: Spreadsheet.Model) : Spreadsheet.Model =
    let cell = Generic.getCell (index.x, index.y) state
    // Remove selected cell value
    let emptyCell = cell.GetEmptyCell()
    Generic.setCell (index.x, index.y) emptyCell state
    copyCell cell |> Promise.start
    state

let cutCellsByIndices (indices: CellCoordinate[]) (state: Spreadsheet.Model) : Spreadsheet.Model =
    let cells = ResizeArray()

    for index in indices do
        let cell = Generic.getCell (index.x, index.y) state
        // Remove selected cell value
        let emptyCell = cell.GetEmptyCell()
        Generic.setCell (index.x, index.y) emptyCell state
        cells.Add(cell)

    copyCells (Array.ofSeq cells) |> Promise.start
    state

let pasteCellByIndex (index: CellCoordinate) (state: Spreadsheet.Model) : JS.Promise<Spreadsheet.Model> = promise {
    let! tab = navigator.clipboard.readText ()
    let header = Generic.getHeader index.x state
    let cell = CompositeCell.fromTabTxt tab header |> Array.head
    Generic.setCell (index.x, index.y) cell state
    return state
}

let pasteCellsByIndexExtend (index: CellCoordinate) (state: Spreadsheet.Model) : JS.Promise<Spreadsheet.Model> = promise {
    let! tab = navigator.clipboard.readText ()
    let header = Generic.getHeader index.x state
    let cells = CompositeCell.fromTabTxt tab header

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