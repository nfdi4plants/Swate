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
            if CellCoordinateRange.contains (Some indices) index then
                Some indices
                |> setSelectedCells
            else
                let tempRange: CellCoordinateRange =
                    {|
                        xEnd = if index.x >= indices.xEnd then index.x else indices.xEnd
                        xStart = if index.x <= indices.xStart then index.x else indices.xStart
                        yEnd = if index.y >= indices.yEnd then index.y else indices.yEnd
                        yStart = if index.y <= indices.yStart then index.y else indices.yStart
                    |}
                Some tempRange
                |> setSelectedCells

        else
            let tempRange: CellCoordinateRange =
                {|
                    xEnd = index.x
                    xStart = index.x
                    yEnd = index.y
                    yStart = index.y
                |}
            Some tempRange
            |> setSelectedCells

    AnnotationTable.AnnotationTable(model.SpreadsheetModel.ActiveTable, setTable, onSelect)
