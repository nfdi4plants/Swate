module MainComponents.SpreadsheetView.ArcTable

open Feliz

open Model
open Messages
open Swate.Components

[<ReactComponent>]
let Main (model: Model, dispatch) =

    let setTable = fun (table: ARCtrl.ArcTable) -> Spreadsheet.UpdateTable table |> SpreadsheetMsg |> dispatch
    let setSelectedCells = fun selectedCells -> Spreadsheet.UpdateSelectedCells selectedCells |> SpreadsheetMsg |> dispatch

    let onSelect (index: CellCoordinate) (indices: CellCoordinateRange option) =
        if indices.IsSome then
            let xValues = [| indices.Value.xStart..indices.Value.xEnd |]
            let yValues = [| indices.Value.yStart..indices.Value.yEnd |]
            [| for x in xValues do
                for y in yValues do
                    yield (x - 1, y - 1) |]
            |> Set.ofArray
            |> setSelectedCells

    AnnotationTable.AnnotationTable(model.SpreadsheetModel.ActiveTable, setTable, onSelect)
