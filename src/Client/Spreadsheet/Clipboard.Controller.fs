module Spreadsheet.Clipboard.Controller

open ARCtrl.ISA
open Shared

module ClipboardAux =
    let setClipboardCell (state: Spreadsheet.Model) (cell: CompositeCell option) =
        let nextState = {state with Clipboard = { state.Clipboard with Cell = cell}}
        nextState

open ClipboardAux

let copyCell (index: int*int) (state: Spreadsheet.Model) : Spreadsheet.Model =
    let cell = state.ActiveTable.TryGetCellAt(index)
    let nextState = {state with Clipboard = { state.Clipboard with Cell = cell}}
    nextState

let copySelectedCell (state: Spreadsheet.Model) : Spreadsheet.Model =
    /// Array.head is used until multiple cells are supported, should this ever be intended
    let index = state.SelectedCells |> Set.toArray |> Array.min
    copyCell index state

let cutCell (index: int*int) (state: Spreadsheet.Model) : Spreadsheet.Model =
    let cell = state.ActiveTable.TryGetCellAt(index)
    // Remove selected cell value
    
    let emptyCell = if cell.IsSome then cell.Value.GetEmptyCell() else state.ActiveTable.GetColumn(fst index).GetDefaultEmptyCell()
    state.ActiveTable.UpdateCellAt(fst index,snd index, emptyCell)
    let nextState = setClipboardCell state cell
    nextState

let cutSelectedCell (state: Spreadsheet.Model) : Spreadsheet.Model =
    /// Array.min is used until multiple cells are supported, should this ever be intended
    let index = state.SelectedCells |> Set.toArray |> Array.min
    cutCell index state

let pasteCell (index: int*int) (state: Spreadsheet.Model) : Spreadsheet.Model =
    match state.Clipboard.Cell with
    // Don't update if no cell in saved
    | None -> state
    | Some c ->
        state.ActiveTable.UpdateCellAt(fst index, snd index, c)
        state

let pasteSelectedCell (state: Spreadsheet.Model) : Spreadsheet.Model =
    if state.SelectedCells.IsEmpty then
        state
    else
        // TODO:
        //let arr = state.SelectedCells |> Set.toArray
        //let isOneColumn =
        //    let c = fst arr.[0] // can just use head of selected cells as all must be same column
        //    arr |> Array.forall (fun x -> fst x = c)
        //if not isOneColumn then failwith "Can only paste cells in one column at a time!"
        let minIndex = state.SelectedCells |> Set.toArray |> Array.min
        pasteCell minIndex state