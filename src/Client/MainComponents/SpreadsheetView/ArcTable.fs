module MainComponents.SpreadsheetView.ArcTable

open Feliz

open Model
open Messages
open Swate.Components

[<ReactComponent>]
let Main (model: Model, dispatch) =

    let table = model.SpreadsheetModel.ActiveTable
    let table, setTable = React.useState(table)
    let selectedCells, setSelectedCells = React.useState(model.SpreadsheetModel.SelectedCells)

    console.log (selectedCells |> Array.ofSeq)
    console.log (model.SpreadsheetModel.SelectedCells |> Array.ofSeq)

    React.useEffect(
        (fun () ->
            setTable(model.SpreadsheetModel.ActiveTable)
            if selectedCells <> model.SpreadsheetModel.SelectedCells then
                Spreadsheet.UpdateSelectedCells selectedCells |> SpreadsheetMsg |> dispatch
        ),
        [| box model; box selectedCells |]
    )

    AnnotationTable.AnnotationTable(table, setTable, setSelectedCells)
