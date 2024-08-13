module Spreadsheet.Controller.Clipboard

open Fable.Core
open ARCtrl
open Shared

let copyCell (cell: CompositeCell) : JS.Promise<unit> =
    let tab = cell.ToTabStr()
    navigator.clipboard.writeText(tab)

let copyCells (cells: CompositeCell []) : JS.Promise<unit> =
    let tab = CompositeCell.ToTabTxt cells
    navigator.clipboard.writeText(tab)

let copyCellByIndex (index: int*int) (state: Spreadsheet.Model) : JS.Promise<unit> =
    let cell = Generic.getCell index state
    copyCell cell

let copyCellsByIndex (indices: (int*int) []) (state: Spreadsheet.Model) : JS.Promise<unit> =
    let cells = [| for index in indices do yield Generic.getCell index state |]
    copyCells cells

let copySelectedCell (state: Spreadsheet.Model) : JS.Promise<unit> =
    /// Array.head is used until multiple cells are supported, should this ever be intended
    let index = state.SelectedCells |> Set.toArray |> Array.min
    copyCellByIndex index state

let copySelectedCells (state: Spreadsheet.Model) : JS.Promise<unit> =
    /// Array.head is used until multiple cells are supported, should this ever be intended
    let indices = state.SelectedCells |> Set.toArray
    copyCellsByIndex indices state

let cutCellByIndex (index: int*int) (state: Spreadsheet.Model) : Spreadsheet.Model =
    let cell = Generic.getCell index state
    // Remove selected cell value
    let emptyCell = cell.GetEmptyCell()
    Generic.setCell index emptyCell state
    copyCell cell |> Promise.start
    state

let cutCellsByIndices (indices: (int*int) []) (state: Spreadsheet.Model) : Spreadsheet.Model =
    let cells = ResizeArray()
    for index in indices do
        let cell = Generic.getCell index state
        // Remove selected cell value
        let emptyCell = cell.GetEmptyCell()
        Generic.setCell index emptyCell state
        cells.Add(cell)
    copyCells (Array.ofSeq cells) |> Promise.start
    state

let cutSelectedCell (state: Spreadsheet.Model) : Spreadsheet.Model =
    /// Array.min is used until multiple cells are supported, should this ever be intended
    let index = state.SelectedCells |> Set.toArray |> Array.min
    cutCellByIndex index state

let cutSelectedCells (state: Spreadsheet.Model) : Spreadsheet.Model =
    /// Array.min is used until multiple cells are supported, should this ever be intended
    let indices = state.SelectedCells |> Set.toArray 
    cutCellsByIndices indices state

let pasteCellByIndex (index: int*int) (state: Spreadsheet.Model) : JS.Promise<Spreadsheet.Model> =
    promise {
        let! tab = navigator.clipboard.readText()
        let header = Generic.getHeader (fst index) state
        let cell = CompositeCell.fromTabTxt tab header |> Array.head
        Generic.setCell index cell state
        return state
    }

let pasteCellsByIndexExtend (index: int*int) (state: Spreadsheet.Model) : JS.Promise<Spreadsheet.Model> =
    promise { 
        let! tab = navigator.clipboard.readText()
        let header = Generic.getHeader (fst index) state
        let cells = CompositeCell.fromTabTxt tab header
        let columnIndex, rowIndex = fst index, snd index
        let indexedCells = cells |> Array.indexed |> Array.map (fun (i,c) -> (columnIndex, rowIndex + i), c)
        Generic.setCells indexedCells state
        return state 
    }

let pasteCellIntoSelected (state: Spreadsheet.Model) : JS.Promise<Spreadsheet.Model> =
    if state.SelectedCells.IsEmpty then
        promise {return state}
    else
        let minIndex = state.SelectedCells |> Set.toArray |> Array.min
        pasteCellByIndex minIndex state

let pasteCellsIntoSelected (state: Spreadsheet.Model) : JS.Promise<Spreadsheet.Model> =
    if state.SelectedCells.IsEmpty then
        promise {return state}
    else
        let columnIndex = state.SelectedCells |> Set.toArray |> Array.minBy fst |> fst
        let selectedSingleColumnCells = state.SelectedCells |> Set.filter (fun index -> fst index = columnIndex)
        promise {
            let! tab = navigator.clipboard.readText()
            let header = Generic.getHeader columnIndex state
            let cells = CompositeCell.fromTabTxt tab header
            if cells.Length = 1 then
                let cell = cells.[0]
                let newCells = selectedSingleColumnCells |> Array.ofSeq |> Array.map (fun index -> index, cell)
                Generic.setCells newCells state
                return state
            else
                let rowCount = selectedSingleColumnCells.Count
                let cellsTrimmed = cells |> Array.takeSafe rowCount
                let indicesTrimmed = (Set.toArray selectedSingleColumnCells).[0..cellsTrimmed.Length-1]
                let indexedCellsTrimmed = Array.zip indicesTrimmed cellsTrimmed
                Generic.setCells indexedCellsTrimmed state
                return state
        }