module Components.Tests.Table.ContextMenu

open Fable.Mocha
open ARCtrl
open Swate.Components
open Swate.Components.Composite.AnnotationTable.Types
open global.Swate.Components.Composite.AnnotationTable
open global.Swate.Components.Composite.AnnotationTable.Types.AnnotationTableContextMenu
open global.Swate.Components.Composite.Table
open global.Swate.Components.Composite.Table.Types
open global.Swate.Components.Primitive.ContextMenu.Types
open Browser.Types


type TestCases =

    static member private NoSelectionHandle() =
        SelectHandle(
            (fun _ -> false),
            (fun _ -> ()),
            (fun _ -> ()),
            (fun _ -> None),
            (fun _ -> ResizeArray()),
            (fun _ -> 0)
        )

    static member private TriggerMenuItem (item: ContextMenuItem) (spawnData: CellCoordinate) =
        item.onClick
        |> Option.iter (fun onClick ->
            onClick {|
                buttonEvent = unbox<MouseEvent> null
                spawnData = box spawnData
            |}
        )

    static member AddColumns(selectHandle: SelectHandle, pasteData: string[][], expectedColumns: CompositeColumn[]) =

        let clickedCell: CellCoordinate = {| x = 1; y = 1 |}

        let currentTable = Fixture.mkTable ()

        let cellCoordinates = selectHandle.getSelectedCells () |> Array.ofSeq

        //Group all cells based on their row
        let groupedCellCoordinates =
            cellCoordinates
            |> Array.groupBy (fun item -> item.y)
            |> Array.map (fun (_, row) -> row)

        let pasteBehavior =
            AnnotationTableClipboard.predictPasteBehaviour (clickedCell, currentTable, selectHandle, pasteData)

        Expect.equal
            pasteBehavior
            (PasteCases.AddColumns {|
                coordinate = clickedCell
                coordinates = groupedCellCoordinates
                data = expectedColumns |> ResizeArray
            |})
            "Should predict add column behavior"

    static member AddSingleCell(pasteData: string[][], expectedColumn: CompositeColumn[]) =

        let currentTable = Fixture.mkTable ()
        let clickedCell: CellCoordinate = {| x = 1; y = 1 |}

        let selectHandle: SelectHandle = Fixture.mkSelectHandle (1, 1, 1, 1)

        let pasteBehavior =
            AnnotationTableClipboard.predictPasteBehaviour (clickedCell, currentTable, selectHandle, pasteData)

        Expect.equal
            pasteBehavior
            (PasteCases.PasteCells {|
                data = expectedColumn |> ResizeArray
                coordinates = [| [| clickedCell |] |]
            |})
            "Should predict paste single cell behavior"

    static member PasteMultipleCells
        (selectHandle: SelectHandle, pasteData: string[][], expectedColumns: CompositeColumn[])
        =
        let currentTable = Fixture.mkTable ()
        let cellCoordinates = Fixture.getRangeOfSelectedCells (selectHandle)

        let clickedCell: CellCoordinate = {| x = 1; y = 1 |}

        let pasteBehavior =
            AnnotationTableClipboard.predictPasteBehaviour (clickedCell, currentTable, selectHandle, pasteData)

        Expect.equal
            pasteBehavior
            (PasteCases.PasteCells {|
                data = expectedColumns |> ResizeArray
                coordinates = cellCoordinates
            |})
            "Should predict paste fitted cells behavior"

    static member AddFittingTerm
        (selectHandle: SelectHandle, pasteData: string[][], expectedColumns: CompositeColumn[])
        =
        let currentTable = Fixture.mkTable ()
        let cellCoordinates = Fixture.getRangeOfSelectedCells (selectHandle)

        let clickedCell: CellCoordinate = {| x = 3; y = 1 |}

        let pasteBehavior =
            AnnotationTableClipboard.predictPasteBehaviour (clickedCell, currentTable, selectHandle, pasteData)

        Expect.equal
            pasteBehavior
            (PasteCases.PasteCells {|
                data = expectedColumns |> ResizeArray
                coordinates = cellCoordinates
            |})
            "Should predict paste fitted cells behavior"

    static member AddUnknownPattern(pasteData: string[][]) =
        let currentTable = Fixture.mkTable ()
        let selectHandle: SelectHandle = Fixture.mkSelectHandle (1, 1, 4, 4)

        let headers =
            let columnIndices =
                selectHandle.getSelectedCells ()
                |> Array.ofSeq
                |> Array.distinctBy (fun item -> item.x)

            columnIndices
            |> Array.map (fun index -> currentTable.GetColumn(index.x - 1).Header)

        let clickedCell: CellCoordinate = {| x = 4; y = 1 |}

        let adaptedData = pasteData |> Array.map (fun item -> item)

        let pasteBehavior =
            AnnotationTableClipboard.predictPasteBehaviour (clickedCell, currentTable, selectHandle, adaptedData)

        Expect.equal
            pasteBehavior
            (PasteCases.Unknown {|
                data = adaptedData
                headers = headers
            |})
            "Should predict paste fitted cells behavior"

    static member HeaderDeleteFirstColumn() =
        let table = Fixture.mkTable ()
        let selectHandle = TestCases.NoSelectionHandle()
        let originalColumnCount = table.ColumnCount
        let mutable updatedTable = table

        let menuItems =
            AnnotationTableContextMenu.CompositeHeaderContent(
                1,
                table,
                (fun nextTable -> updatedTable <- nextTable),
                selectHandle,
                ignore
            )

        TestCases.TriggerMenuItem menuItems.[6] {| x = 1; y = 0 |}

        Expect.equal
            updatedTable.ColumnCount
            (originalColumnCount - 1)
            "Delete column should remove the first data column"

        Expect.equal
            updatedTable.Headers.[0]
            (CompositeHeader.Output IOType.Data)
            "After deleting first column, previous second column should become first"

    static member IndexDeleteFirstRow() =
        let table = Fixture.mkTable ()
        let selectHandle = TestCases.NoSelectionHandle()
        let originalRowCount = table.RowCount
        let expectedFirstCellAfterDelete = table.GetCellAt(0, 1).ToTabStr()
        let mutable updatedTable = table

        let menuItems =
            AnnotationTableContextMenu.IndexColumnContent(
                1,
                table,
                (fun nextTable -> updatedTable <- nextTable),
                selectHandle
            )

        TestCases.TriggerMenuItem menuItems.[0] {| x = 0; y = 1 |}

        Expect.equal updatedTable.RowCount (originalRowCount - 1) "Delete row should remove the first data row"

        Expect.equal
            (updatedTable.GetCellAt(0, 0).ToTabStr())
            expectedFirstCellAfterDelete
            "After deleting first row, second row content should become first row"

    static member HeaderMoveColumnUsesSelectedHeaderIndex() =
        let table = Fixture.mkTable ()
        let selectHandle = TestCases.NoSelectionHandle()
        let mutable openedModal: AnnotationTable.ModalTypes option = None

        let menuItems =
            AnnotationTableContextMenu.CompositeHeaderContent(
                1,
                table,
                ignore,
                selectHandle,
                (fun modal -> openedModal <- modal)
            )

        TestCases.TriggerMenuItem menuItems.[7] {| x = 1; y = 0 |}

        match openedModal with
        | Some(AnnotationTable.ModalTypes.MoveColumn(_, arcTableIndex)) ->
            Expect.equal arcTableIndex.x 1 "Move column should target the first header column (1-based UI index)"
            Expect.equal arcTableIndex.y 0 "Move column target should stay on header row"
        | _ -> failwith "Move column menu entry should open move-column modal"

    static member private MkUnitPasteTable(targetUnit: OntologyAnnotation, ?sourceUnit: OntologyAnnotation) =
        let table = ARCtrl.ArcTable("UnitPasteTable", ResizeArray())

        let cells =
            [|
                yield CompositeCell.createUnitized ("1", targetUnit)
                match sourceUnit with
                | Some sourceUnit -> yield CompositeCell.createUnitized ("2", sourceUnit)
                | None -> ()
            |]
            |> ResizeArray

        table.AddColumn(CompositeHeader.Parameter(OntologyAnnotation("Temperature", "TEMP", "TEMP:0001")), cells)
        table

    static member private PasteCellsInto(table: ArcTable, pasteData: string[][]) =
        let selectHandle = Fixture.mkSelectHandle (1, 1, 1, 1)
        let clickedCell: CellCoordinate = {| x = 1; y = 1 |}

        let pasteBehavior =
            AnnotationTableClipboard.predictPasteBehaviour (clickedCell, table, selectHandle, pasteData)

        match pasteBehavior with
        | PasteCases.PasteCells pasteColumns ->
            let mutable updatedTable = table

            AnnotationTableClipboard.pasteCells (
                pasteColumns,
                clickedCell,
                selectHandle,
                table,
                (fun nextTable -> updatedTable <- nextTable)
            )

            updatedTable, pasteColumns
        | _ -> failwith "Unit paste should be predicted as PasteCells"

    static member CompactUnitPasteRestoresMetadataFromMatchingUnit() =
        let targetUnit = OntologyAnnotation("degree Celsius", "TARGET", "TARGET:0001")
        let sourceUnit = OntologyAnnotation("degree Celsius", "ALTUNIT", "ALTUNIT:0001")
        let table = TestCases.MkUnitPasteTable(targetUnit, sourceUnit = sourceUnit)
        let pasteData = [| [| "4"; "degree Celsius" |] |]

        let updatedTable, pasteColumns = TestCases.PasteCellsInto(table, pasteData)

        let firstValue, firstUnit = pasteColumns.data.[0].Cells.[0].AsUnitized

        Expect.equal firstValue "4" "Compact value-unit paste should preserve the numeric value"
        Expect.equal firstUnit.NameText "degree Celsius" "Compact value-unit paste should preserve the unit name"
        Expect.equal firstUnit.TermSourceREF None "Compact value-unit paste initially only contains the unit name"

        Expect.equal
            (updatedTable.GetCellAt(0, 0).ToTabStr())
            "4\tdegree Celsius\tALTUNIT\tALTUNIT:0001"
            "Compact value-unit paste should restore metadata from a matching unit, not from the target cell"

    static member CompactUnitPasteDoesNotUseOnlyTargetMetadata() =
        let targetUnit = OntologyAnnotation("degree Celsius", "TARGET", "TARGET:0001")
        let table = TestCases.MkUnitPasteTable(targetUnit)
        let pasteData = [| [| "4"; "degree Celsius" |] |]

        let updatedTable, _ = TestCases.PasteCellsInto(table, pasteData)

        Expect.equal
            (updatedTable.GetCellAt(0, 0).ToTabStr())
            "4\tdegree Celsius\t\t"
            "Compact value-unit paste should not inherit metadata from the target cell alone"

    static member ValueOnlyUnitPasteUsesTargetUnit() =
        let targetUnit = OntologyAnnotation("degree Celsius", "TARGET", "TARGET:0001")
        let table = TestCases.MkUnitPasteTable(targetUnit)
        let pasteData = [| [| "4" |] |]

        let updatedTable, _ = TestCases.PasteCellsInto(table, pasteData)

        Expect.equal
            (updatedTable.GetCellAt(0, 0).ToTabStr())
            "4\tdegree Celsius\tTARGET\tTARGET:0001"
            "Value-only unit paste should keep using the target unit"

    static member FullUnitPasteKeepsGivenMetadata() =
        let targetUnit = OntologyAnnotation("degree Celsius", "TARGET", "TARGET:0001")
        let sourceUnit = OntologyAnnotation("degree Celsius", "ALTUNIT", "ALTUNIT:0001")
        let table = TestCases.MkUnitPasteTable(targetUnit, sourceUnit = sourceUnit)
        let pasteData = [| [| "4"; "degree Celsius"; "GIVEN"; "GIVEN:0001" |] |]

        let updatedTable, _ = TestCases.PasteCellsInto(table, pasteData)

        Expect.equal
            (updatedTable.GetCellAt(0, 0).ToTabStr())
            "4\tdegree Celsius\tGIVEN\tGIVEN:0001"
            "Full value-unit-metadata paste should keep the given unit metadata"

    static member CompactUnitPasteKeepsUnmatchedUnitNameOnly() =
        let targetUnit = OntologyAnnotation("degree Celsius", "TARGET", "TARGET:0001")
        let sourceUnit = OntologyAnnotation("kelvin", "ALTUNIT", "ALTUNIT:0002")
        let table = TestCases.MkUnitPasteTable(targetUnit, sourceUnit = sourceUnit)
        let pasteData = [| [| "4"; "meter" |] |]

        let updatedTable, _ = TestCases.PasteCellsInto(table, pasteData)

        Expect.equal
            (updatedTable.GetCellAt(0, 0).ToTabStr())
            "4\tmeter\t\t"
            "Compact value-unit paste should keep an unmatched unit name without borrowing other metadata"

let Main =

    testList "Context Menu" [
        testList "Prediction" [
            testCase "Add term column"
            <| fun _ ->
                TestCases.AddColumns(
                    Fixture.mkSelectHandle (1, 1, 3, 3),
                    Fixture.Component_Term_InstrumentModel_String,
                    [| Fixture.Component_InstrumentModel_Term_Column |]
                )
            testCase "Add unit column"
            <| fun _ ->
                TestCases.AddColumns(
                    Fixture.mkSelectHandle (1, 1, 3, 3),
                    Fixture.Component_Unit_InstrumentModel_String,
                    [| Fixture.Component_InstrumentModel_Unit_Column |]
                )
            testCase "Add unit - term column"
            <| fun _ ->
                TestCases.AddColumns(
                    Fixture.mkSelectHandle (1, 1, 3, 4),
                    Fixture.Component_Unit_InstrumentModel_Unit_Term_String,
                    Fixture.Component_Unit_InstrumentModel_Unit_Term_Columns
                )
            testCase "Paste single Cell"
            <| fun _ ->
                TestCases.AddSingleCell(
                    Fixture.Body_Component_InstrumentModel_Pseudo_SingleRow_String,
                    [|
                        Fixture.Body_Component_InstrumentModel_Pseudo_SingleRow_Column
                    |]
                )
            testCase $"Paste {1} Cell(s) in the same row. Paste {2} Cell(s) in the same column"
            <| fun _ ->
                TestCases.PasteMultipleCells(
                    Fixture.mkSelectHandle (1, 2, 1, 1),
                    Fixture.Body_Component_InstrumentModel_TwoRows_Term_Strings,
                    [|
                        Fixture.Body_Component_InstrumentModel_TwoRows_Term_Column
                    |]
                )
            testCase $"Paste {1} Cell(s) in the same row. Paste {3} Cell(s) in the same column"
            <| fun _ ->
                TestCases.PasteMultipleCells(
                    Fixture.mkSelectHandle (1, 3, 1, 1),
                    Fixture.Body_Component_InstrumentModel_ThreeRows_Term_Strings,
                    [|
                        Fixture.Body_Component_InstrumentModel_ThreeRows_Term_Column
                    |]
                )
            testCase $"Paste {2} Cell(s) in the same row. Paste {1} Cell(s) in the same column"
            <| fun _ ->
                TestCases.PasteMultipleCells(
                    Fixture.mkSelectHandle (1, 1, 1, 2),
                    Fixture.Body_Component_InstrumentModel_TwoColumns_Term_Strings,
                    Fixture.Body_Component_InstrumentModel_TwoColumns_Term_Columns
                )
            testCase $"Paste {2} Cell(s) in the same row. Paste {2} Cell(s) in the same column"
            <| fun _ ->
                TestCases.PasteMultipleCells(
                    Fixture.mkSelectHandle (1, 2, 1, 2),
                    Fixture.Body_Component_InstrumentModel_TwoRowsColumns_Term_Strings,
                    Fixture.Body_Component_InstrumentModel_TwoRowsColumns_Term_Columns
                )
            testCase $"Add fitting Term"
            <| fun _ ->
                TestCases.AddFittingTerm(
                    Fixture.mkSelectHandle (1, 1, 3, 3),
                    Fixture.Body_Component_InstrumentModel_SingleRow_Term_String,
                    [| Fixture.Component_InstrumentModel_Term_Body |]
                )
            testCase $"Add 1 Freetext and 1 Term"
            <| fun _ ->
                TestCases.AddFittingTerm(
                    Fixture.mkSelectHandle (1, 1, 2, 3),
                    Fixture.Body_Component_InstrumentModel_SingleRow_1Freetext_1_Term_Strings,
                    Fixture.Body_Component_InstrumentModel_SingleRow_1Freetext_1_Term_Columns
                )
            testCase $"Add unit value"
            <| fun _ ->
                TestCases.AddFittingTerm(
                    Fixture.mkSelectHandle (1, 1, 3, 3),
                    Fixture.Body_Integer,
                    [| Fixture.Body_Integer_Column |]
                )
            testCase $"Add unknown value"
            <| fun _ -> TestCases.AddUnknownPattern([| [| "" |] |])
        ]
        testList "Regression" [
            testCase "Header delete targets first column correctly"
            <| fun _ -> TestCases.HeaderDeleteFirstColumn()
            testCase "Index delete targets first row correctly"
            <| fun _ -> TestCases.IndexDeleteFirstRow()
            testCase "Header move column keeps 1-based header index"
            <| fun _ -> TestCases.HeaderMoveColumnUsesSelectedHeaderIndex()
            testCase "Compact value-unit TSV paste restores metadata from matching unit"
            <| fun _ -> TestCases.CompactUnitPasteRestoresMetadataFromMatchingUnit()
            testCase "Compact value-unit TSV paste does not use target metadata alone"
            <| fun _ -> TestCases.CompactUnitPasteDoesNotUseOnlyTargetMetadata()
            testCase "Value-only TSV paste into unitized cell uses target unit"
            <| fun _ -> TestCases.ValueOnlyUnitPasteUsesTargetUnit()
            testCase "Full unit TSV paste keeps given metadata"
            <| fun _ -> TestCases.FullUnitPasteKeepsGivenMetadata()
            testCase "Compact value-unit TSV paste keeps unmatched unit without metadata"
            <| fun _ -> TestCases.CompactUnitPasteKeepsUnmatchedUnitNameOnly()
        ]
    ]
