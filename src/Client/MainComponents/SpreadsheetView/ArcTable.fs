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
            let indices = indices.Value
            let tempRange: CellCoordinateRange =
                {|
                    xEnd = if index.x >= indices.xEnd then index.x - 1 else indices.xEnd - 1
                    xStart = if index.x <= indices.xStart then index.x - 1 else indices.xStart - 1
                    yEnd = if index.y >= indices.yEnd then index.y - 1 else indices.yEnd - 1
                    yStart = if index.y <= indices.yStart then index.y - 1 else indices.yStart - 1
                |}
            Some tempRange
            |> setSelectedCells

        else
            let tempRange: CellCoordinateRange =
                {|
                    xEnd = index.x - 1
                    xStart = index.x - 1
                    yEnd = index.y - 1
                    yStart = index.y - 1
                |}
            Some tempRange
            |> setSelectedCells

    AnnotationTable.AnnotationTable(model.SpreadsheetModel.ActiveTable, setTable, onSelect)
