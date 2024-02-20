module Spreadsheet.Clipboard.Controller

open Fable.Core
open ARCtrl.ISA
open Shared

let copyCell (cell: CompositeCell) : JS.Promise<unit> =
    let tab = cell.ToTabStr()
    navigator.clipboard.writeText(tab)

let copyCells (cells: CompositeCell []) : JS.Promise<unit> =
    let tab = CompositeCell.ToTabTxt cells
    navigator.clipboard.writeText(tab)

let copyCellByIndex (index: int*int) (state: Spreadsheet.Model) : JS.Promise<unit> =
    let cell = state.ActiveTable.Values.[index]
    copyCell cell

let copyCellsByIndex (indices: (int*int) []) (state: Spreadsheet.Model) : JS.Promise<unit> =
    let cells = [|for index in indices do yield state.ActiveTable.Values.[index] |]
    log cells
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
    let cell = state.ActiveTable.Values.[index]
    // Remove selected cell value
    let emptyCell = cell.GetEmptyCell()
    state.ActiveTable.UpdateCellAt(fst index,snd index, emptyCell) 
    copyCell cell |> Promise.start
    state

let cutSelectedCell (state: Spreadsheet.Model) : Spreadsheet.Model =
    /// Array.min is used until multiple cells are supported, should this ever be intended
    let index = state.SelectedCells |> Set.toArray |> Array.min
    cutCellByIndex index state

let pasteCellByIndex (index: int*int) (state: Spreadsheet.Model) : JS.Promise<Spreadsheet.Model> =
    promise {
        let! tab = navigator.clipboard.readText()
        let cell = CompositeCell.fromTabTxt tab |> Array.head
        state.ActiveTable.SetCellAt(fst index, snd index, cell)
        return state
    }

let pasteCellsByIndexExtend (index: int*int) (state: Spreadsheet.Model) : JS.Promise<Spreadsheet.Model> =
    promise { 
        let! tab = navigator.clipboard.readText()
        let cells = CompositeCell.fromTabTxt tab
        let columnIndex, rowIndex = fst index, snd index
        let indexedCells = cells |> Array.indexed |> Array.map (fun (i,c) -> (columnIndex, rowIndex + i), c)
        state.ActiveTable.SetCellsAt indexedCells
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
            let cells = CompositeCell.fromTabTxt tab
            let rowCount = selectedSingleColumnCells.Count
            let cellsTrimmed = cells |> takeFromArray rowCount
            let indicesTrimmed = (Set.toArray selectedSingleColumnCells).[0..cellsTrimmed.Length-1]
            let indexedCellsTrimmed = Array.zip indicesTrimmed cellsTrimmed
            state.ActiveTable.SetCellsAt indexedCellsTrimmed
            return state
        }