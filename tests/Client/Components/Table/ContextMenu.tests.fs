module Components.Tests.Table.ContextMenu

open Fable.Mocha
open ARCtrl
open ARCtrl.Spreadsheet
open Swate.Components

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

    let mkSelectHandle (yStart, yEnd, xStart, xEnd) =
        let range: CellCoordinateRange = {|
            yStart = yStart
            yEnd = yEnd
            xStart = xStart
            xEnd = xEnd
        |}

        new SelectHandle(
            (fun x -> failwith "Not implemented"),
            (fun x -> failwith "Not implemented"),
            (fun x -> failwith "Not implemented"),
            (fun x -> Some range),
            (fun x -> CellCoordinateRange.toArray range),
            (fun x -> CellCoordinateRange.count range)
        )

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

                let SelectHandle: SelectHandle = MockData.mkSelectHandle (0, 1, 0, 2)

                let pasteBehavior =
                    Swate.Components.AnnotationTableContextMenuUtil.predictPasteBehaviour (
                        clickedCell,
                        currentTable,
                        SelectHandle,
                        PasteData
                    )

                Expect.equal
                    pasteBehavior
                    (PasteCases.AddColumns {|
                        data = PasteData
                        columnIndex = clickedCell.x
                    |})
                    "Should predict paste columns behavior"
        ]
    ]