module Components.Tests.Table.ContextMenu

open Fable.Mocha
open ARCtrl
open Swate.Components
open AnnotationTableContextMenu
open Browser.Types
open Fixture

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

    static member private TriggerMenuItem (item: Swate.Components.ContextMenuItem) (spawnData: CellCoordinate) =
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
            AnnotationTableContextMenuUtil.predictPasteBehaviour (clickedCell, currentTable, selectHandle, pasteData)

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
            AnnotationTableContextMenuUtil.predictPasteBehaviour (clickedCell, currentTable, selectHandle, pasteData)

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
            AnnotationTableContextMenuUtil.predictPasteBehaviour (clickedCell, currentTable, selectHandle, pasteData)

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
            AnnotationTableContextMenuUtil.predictPasteBehaviour (clickedCell, currentTable, selectHandle, pasteData)

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
            AnnotationTableContextMenuUtil.predictPasteBehaviour (clickedCell, currentTable, selectHandle, adaptedData)

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

        Expect.equal updatedTable.ColumnCount (originalColumnCount - 1) "Delete column should remove the first data column"
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
        | _ ->
            failwith "Move column menu entry should open move-column modal"

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
        ]
    ]
