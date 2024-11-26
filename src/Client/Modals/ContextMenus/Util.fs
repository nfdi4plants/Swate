module Modals.ContextMenus.Util

open ARCtrl

let isUnitOrTermCell (cell: CompositeCell option) =
    cell.IsSome && (cell.Value.isTerm || cell.Value.isUnitized)

let isHeader (rowIndex: int) = rowIndex < 0


let clear isSelectedCell index dispatch =
    fun _ ->
        if isSelectedCell then
            Spreadsheet.ClearSelected
            |> Messages.SpreadsheetMsg
            |> dispatch
        else
            Spreadsheet.Clear [|index|]
            |> Messages.SpreadsheetMsg
            |> dispatch

let fillColumn index dispatch =
    fun _ ->
        Spreadsheet.FillColumnWithTerm index |> Messages.SpreadsheetMsg |> dispatch

let copy isSelectedCell index dispatch =
    fun _ ->
        if isSelectedCell then
            Spreadsheet.CopySelectedCells |> Messages.SpreadsheetMsg |> dispatch
        else
            Spreadsheet.CopyCell index |> Messages.SpreadsheetMsg |> dispatch

let cut index dispatch = fun _ ->
    Spreadsheet.CutCell index |> Messages.SpreadsheetMsg |> dispatch


let paste isSelectedCell index dispatch = fun _ ->
    if isSelectedCell then
        Spreadsheet.PasteSelectedCells |> Messages.SpreadsheetMsg |> dispatch
    else
        Spreadsheet.PasteCell index |> Messages.SpreadsheetMsg |> dispatch

let deleteRow index (model: Model.Model) dispatch = fun _ ->
    let s = Set.toArray model.SpreadsheetModel.SelectedCells
    if Array.isEmpty s |> not && Array.forall (fun (c,r) -> c = fst index) s && Array.contains index s then
        let indexArr = s |> Array.map snd |> Array.distinct
        Spreadsheet.DeleteRows indexArr |> Messages.SpreadsheetMsg |> dispatch
    else
        Spreadsheet.DeleteRow (snd index) |> Messages.SpreadsheetMsg |> dispatch

let pasteAll index dispatch = fun _ ->
    Spreadsheet.PasteCellsExtend index |> Messages.SpreadsheetMsg |> dispatch
