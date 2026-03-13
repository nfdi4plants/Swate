module Spreadsheet.Tests.DataAnnotator

open Fable.Mocha
open ARCtrl
open global.Spreadsheet

let private createTableState rowCount =
    let table = ArcTable.init "TestTable"

    let sourceCells =
        Array.init rowCount (fun index -> CompositeCell.FreeText $"row-{index + 1}")
        |> ResizeArray

    table.AddColumn(CompositeHeader.FreeText "Source", sourceCells)

    let assay = ArcAssay.init "TestAssay"
    assay.AddTable table

    Spreadsheet.Model.init(ArcFiles.Assay assay, Spreadsheet.ActiveView.Table 0)

let private createEmptyTableState () =
    let table = ArcTable.init "EmptyTable"
    let assay = ArcAssay.init "TestAssay"
    assay.AddTable table

    Spreadsheet.Model.init(ArcFiles.Assay assay, Spreadsheet.ActiveView.Table 0)

let private addDataAnnotation selectors rowCount =
    let state = createTableState rowCount

    Spreadsheet.Controller.BuildingBlocks.addDataAnnotation
        {|
            fragmentSelectors = selectors
            fileName = "test.csv"
            fileType = "text/csv"
            targetColumn = DataAnnotator.TargetColumn.Autodetect
        |}
        state

let private addDataAnnotationToEmptyTable selectors =
    let state = createEmptyTableState ()

    Spreadsheet.Controller.BuildingBlocks.addDataAnnotation
        {|
            fragmentSelectors = selectors
            fileName = "test.csv"
            fileType = "text/csv"
            targetColumn = DataAnnotator.TargetColumn.Autodetect
        |}
        state

let Main =
    testList "Spreadsheet Data Annotator" [
        testCase "pads shorter selector lists to existing table row count" <| fun _ ->
            let nextState = addDataAnnotation [| "row=2" |] 3
            let outputColumn = nextState.ActiveTable.GetOutputColumn()

            Expect.equal nextState.ActiveTable.RowCount 3 "Row count should stay unchanged."
            Expect.equal outputColumn.Cells.Count 3 "Output column must match table row count."

            let firstCell = outputColumn.Cells.[0].AsData
            let secondCell = outputColumn.Cells.[1].AsData

            Expect.equal firstCell.FilePath (Some "test.csv") "First selector should populate the first row."
            Expect.equal firstCell.Selector (Some "row=2") "First selector should be written into the first row."
            Expect.equal secondCell.FilePath None "Remaining rows should stay empty."
            Expect.equal secondCell.Selector None "Remaining rows should stay empty."

        testCase "extends table rows when selector list is longer than current table" <| fun _ ->
            let nextState = addDataAnnotation [| "row=1"; "row=2"; "row=3" |] 1
            let outputColumn = nextState.ActiveTable.GetOutputColumn()

            Expect.equal nextState.ActiveTable.RowCount 3 "Table should grow to fit all selectors."
            Expect.equal outputColumn.Cells.Count 3 "Output column must include all selectors."

            let thirdCell = outputColumn.Cells.[2].AsData

            Expect.equal thirdCell.FilePath (Some "test.csv") "Newly added rows should receive annotation data."
            Expect.equal thirdCell.Selector (Some "row=3") "Selectors should be written in order."

        testCase "creates the first data column for an empty table" <| fun _ ->
            let nextState = addDataAnnotationToEmptyTable [| "row=1"; "row=2" |]
            let outputColumn = nextState.ActiveTable.GetOutputColumn()

            Expect.equal nextState.ActiveTable.RowCount 2 "Empty tables should grow to fit selected annotations."
            Expect.equal outputColumn.Cells.Count 2 "The created output column should include all selected annotations."

            let secondCell = outputColumn.Cells.[1].AsData

            Expect.equal secondCell.FilePath (Some "test.csv") "Created rows should store the selected file path."
            Expect.equal secondCell.Selector (Some "row=2") "Created rows should store selectors in order."
    ]
