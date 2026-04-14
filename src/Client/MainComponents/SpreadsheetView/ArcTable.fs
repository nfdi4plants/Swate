module MainComponents.SpreadsheetView.ArcTable

open Feliz

open Model
open Messages
open Swate.Components.AnnotationTable

[<ReactComponent>]
let Main (model: Model, dispatch) =

    let setTable =
        fun (table: ARCtrl.ArcTable) -> Spreadsheet.UpdateTable table |> SpreadsheetMsg |> dispatch

    AnnotationTable.Create(model.SpreadsheetModel.ActiveTable, setTable)