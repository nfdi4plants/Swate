module Components.Tests.Table.ContextMenu

open Fable.Mocha
open ARCtrl
open ARCtrl.Spreadsheet
open Swate.Components
open Swate.Components

type PasteBehavior =
    | PasteColumns of {| data: string[][]; ColumnIndex: int |}
    | Default

let checkForHeaders (row: string[]) =
    let headers = ARCtrl.CompositeHeader.Cases |> Array.map (fun (_, header) -> header)

    let areHeaders =
        headers
        |> Array.collect (fun header -> row |> Array.map (fun cell -> cell.StartsWith(header)))

    Array.contains true areHeaders

let predictPasteBehavior
    (currentTable: ArcTable, clickedCell: CellCoordinate, selectedCells: CellCoordinateRange, rows: string[][])
    =
    if checkForHeaders rows.[0] then
        PasteColumns {|
            data = rows
            ColumnIndex = clickedCell.x
        |}
    else
        Default

let pasteIntoTable (currentTable: ArcTable, pasteObj: PasteBehavior) : ArcTable =
    match pasteObj with
    | PasteColumns columnInfo ->
        let columns = ArcTable.composeColumns columnInfo.data

        currentTable.AddColumns(columns, columnInfo.ColumnIndex)

        currentTable.Copy()


module MockData =

    /// <summary>
    /// Creates a mock table with some sample data.
    ///
    /// 1. Input [source]
    /// 2. Output [sample]
    /// 3. Component [instrument model]
    /// </summary>
    let mkTable () =
        let arcTable =
            ARCtrl.ArcTable("TestTable", ResizeArray(), System.Collections.Generic.Dictionary())

        arcTable.AddColumn(
            CompositeHeader.Input IOType.Source,
            [|
                for i in 0..100 do
                    CompositeCell.createFreeText $"Source {i}"
            |]
        )

        arcTable.AddColumn(
            CompositeHeader.Output IOType.Sample,
            [|
                for i in 0..100 do
                    CompositeCell.createFreeText $"Sample {i}"
            |]
        )

        arcTable.AddColumn(
            CompositeHeader.Component(OntologyAnnotation("instrument model", "MS", "MS:2138970")),
            [|
                for i in 0..100 do
                    CompositeCell.createTermFromString ("SCIEX instrument model", "MS", "MS:11111231")
            |]
        )

        arcTable

    module ClipboardData =

        /// 1 row
        let Column_Component_InstrumentModel = [|
            [| "Component [instrument model]"; "TSR ()"; "TAN ()" |]
            [| "SCIEX instrument model"; "MS"; "MS:424242" |]
        |]

open Swate.Components

let Main =
    testList "Context Menu" [
        testList "Prediction" [
            testCase "Paste columns"
            <| fun _ ->
                let PasteData = MockData.ClipboardData.Column_Component_InstrumentModel

                let currentTable = MockData.mkTable ()
                let clickedCell: CellCoordinate = {| x = 0; y = 0 |}

                let selectedCells: CellCoordinateRange = {|
                    yStart = 0
                    yEnd = 1
                    xStart = 0
                    xEnd = 2
                |}

                let pasteBehavior =
                    predictPasteBehavior (currentTable, clickedCell, selectedCells, PasteData)

                Expect.equal
                    pasteBehavior
                    (PasteColumns {|
                        data = PasteData
                        ColumnIndex = clickedCell.x
                    |})
                    "Should predict paste columns behavior"
        ]
    ]