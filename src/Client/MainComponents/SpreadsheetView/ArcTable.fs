module MainComponents.SpreadsheetView.ArcTable

open Feliz

open Model
open Messages
open Swate.Components

[<ReactComponent>]
let Main (model: Model, dispatch) =

    let setTable =
        fun (table: ARCtrl.ArcTable) -> Spreadsheet.UpdateTable table |> SpreadsheetMsg |> dispatch

    AnnotationTable.AnnotationTable(model.SpreadsheetModel.ActiveTable, setTable)