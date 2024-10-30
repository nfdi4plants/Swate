module Spreadsheet.Controller.Cells

open Model
open Spreadsheet

let mkCellId (columnIndex: int) (rowIndex: int) (state: Model) =
    $"Cell_{state.ActiveView.ViewIndex}-{columnIndex}-{rowIndex}"