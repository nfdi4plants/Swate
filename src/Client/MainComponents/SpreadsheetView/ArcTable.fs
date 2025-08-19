module MainComponents.SpreadsheetView.ArcTable

open Feliz

open Model
open Swate.Components

[<ReactComponent>]
let Main (model: Model) =

    let table = model.SpreadsheetModel.ActiveTable
    let table, setTable = React.useState (table)

    React.useEffect(
        (fun () -> setTable(model.SpreadsheetModel.ActiveTable)),
        [| box model |]
    )

    AnnotationTable.AnnotationTable(table, setTable)
