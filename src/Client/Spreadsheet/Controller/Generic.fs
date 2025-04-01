module Spreadsheet.Controller.Generic

open Spreadsheet
open ARCtrl
open Swate.Components.Shared

let getCell ((ci, ri): int * int) (state: Spreadsheet.Model) : CompositeCell =
    match state.ActiveView with
    | IsTable -> state.ActiveTable.Values[ci, ri]
    | IsDataMap -> state.DataMapOrDefault.GetCell(ci, ri)
    | IsMetadata -> failwith "Cannot get cell in metadata view"

let setCell ((ci, ri): int * int) (cell: CompositeCell) (state: Spreadsheet.Model) : unit =
    match state.ActiveView with
    | IsTable -> state.ActiveTable.UpdateCellAt(ci, ri, cell)
    | IsDataMap -> state.DataMapOrDefault.SetCell(ci, ri, cell)
    | IsMetadata -> failwith "Cannot set cell in metadata view"

let setCells (cells: ((int * int) * CompositeCell)[]) (state: Spreadsheet.Model) : unit =
    match state.ActiveView with
    | IsTable -> state.ActiveTable.SetCellsAt cells
    | IsDataMap ->
        for ((ci, ri), cell) in cells do
            state.DataMapOrDefault.SetCell(ci, ri, cell)
    | IsMetadata -> failwith "Unable to UpdateCell on Metadata sheet"

let getHeader (index: int) (state: Spreadsheet.Model) : CompositeHeader =
    match state.ActiveView with
    | IsTable -> state.ActiveTable.Headers.[index]
    | IsDataMap -> state.DataMapOrDefault.GetHeader(index)
    | IsMetadata -> failwith "Cannot get header in metadata view"

let getRowCount (state: Spreadsheet.Model) : int =
    match state.ActiveView with
    | IsTable -> state.ActiveTable.RowCount
    | IsDataMap -> state.DataMapOrDefault.RowCount
    | IsMetadata -> failwith "Cannot get row count in metadata view"

let getColCount (state: Spreadsheet.Model) : int =
    match state.ActiveView with
    | IsTable -> state.ActiveTable.ColumnCount
    | IsDataMap -> state.DataMapOrDefault.ColumnCount
    | IsMetadata -> failwith "Cannot get column count in metadata view"