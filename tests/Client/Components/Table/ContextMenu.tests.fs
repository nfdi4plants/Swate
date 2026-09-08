module Components.Tests.Table.ContextMenu

open Fable.Mocha
open ARCtrl
open Swate.Components
open Swate.Components.Composite.AnnotationTable.Types
open global.Swate.Components.Composite.AnnotationTable
open global.Swate.Components.Composite.AnnotationTable.Types.AnnotationTableContextMenu
open global.Swate.Components.Composite.DataMapTable.Types
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

    static member PasteIntoClickedColumnOutsideSelection() =
        let currentTable = Fixture.mkTable ()
        let staleSelection = Fixture.mkSelectHandle (1, 1, 1, 1)
        let clickedTermCell: CellCoordinate = {| x = 3; y = 1 |}

        let termPasteBehavior =
            AnnotationTableContextMenuUtil.predictPasteBehaviour (
                clickedTermCell,
                currentTable,
                staleSelection,
                Fixture.Body_Component_InstrumentModel_SingleRow_Term_String
            )

        Expect.equal
            termPasteBehavior
            (PasteCases.PasteCells {|
                data = [| Fixture.Component_InstrumentModel_Term_Body |] |> ResizeArray
                coordinates = [| [| clickedTermCell |] |]
            |})
            "Term paste should use the clicked column header when the previous selection is elsewhere"

        let clickedUnitCell: CellCoordinate = {| x = 4; y = 1 |}
        let unitData = [| [| "4"; "Degree Celsius"; "UO"; "UO:000000001" |] |]

        let unitPasteBehavior =
            AnnotationTableContextMenuUtil.predictPasteBehaviour (
                clickedUnitCell,
                currentTable,
                staleSelection,
                unitData
            )

        let expectedUnitColumn =
            CompositeColumn.create (
                currentTable.GetColumn(3).Header,
                [|
                    CompositeCell.createUnitizedFromString ("4", "Degree Celsius", "UO", "UO:000000001")
                |]
                |> ResizeArray
            )

        Expect.equal
            unitPasteBehavior
            (PasteCases.PasteCells {|
                data = [| expectedUnitColumn |] |> ResizeArray
                coordinates = [| [| clickedUnitCell |] |]
            |})
            "Unit paste should use the clicked column header when the previous selection is elsewhere"

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

    static member DataMainFieldPreservesSelector() =
        let original = CompositeCell.createDataFromString "old.txt"
        let updated = original.UpdateMainField "DatamapTesting.txt#row=2"

        Expect.equal updated.AsData.FilePath (Some "DatamapTesting.txt") "The path should exclude the selector."
        Expect.equal updated.AsData.Selector (Some "row=2") "The selector behind # should be preserved."

    static member DataMapCopyIncludesSelector() =
        let dataMap =
            DataMap(ResizeArray [ DataContext(name = "DatamapTesting.txt#row=2") ])

        let copiedText = dataMap.SelectedCellsToTabText [ {| x = 1; y = 1 |} ]

        Expect.equal copiedText "DatamapTesting.txt#row=2" "Copied DataMap data should include its selector."

        let dataMapWithoutSelector =
            DataMap(ResizeArray [ DataContext(name = "DatamapTesting.txt") ])

        let copiedTextWithoutSelector =
            dataMapWithoutSelector.SelectedCellsToTabText [ {| x = 1; y = 1 |} ]

        Expect.equal
            copiedTextWithoutSelector
            "DatamapTesting.txt"
            "Copied DataMap data should not include trailing tabs for empty metadata."

    static member DataMapCopyKeepsTermsInGridColumns() =
        let source =
            DataMap(
                ResizeArray [
                    DataContext(
                        explication = OntologyAnnotation("explicit", "TST", "TST:1"),
                        unit = OntologyAnnotation("metre", "UO", "UO:0000008")
                    )
                ]
            )

        let copiedText =
            source.SelectedCellsToTabText [ {| x = 5; y = 1 |}; {| x = 6; y = 1 |} ]

        Expect.equal copiedText "explicit\tmetre" "Each selected DataMap cell should occupy one clipboard column."

        let target = DataMap(ResizeArray [ DataContext() ])
        target.PasteTabText({| x = 2; y = 1 |}, copiedText)

        Expect.equal target.DataContexts.[0].Label (Some "explicit") "The first value should use the target column."
        Expect.equal target.DataContexts.[0].Description (Some "metre") "The next value should not shift right."

    static member DataMapPasteRecognizesCompositeTermsAndUnits() =
        let dataMap = DataMap(ResizeArray [ DataContext() ])
        let term = CompositeCell.createTermFromString ("explicit", "TST", "TST:1")
        let unit = CompositeCell.createUnitizedFromString ("4", "metre", "UO", "UO:0000008")

        dataMap.PasteTabText({| x = 5; y = 1 |}, term.ToTabStr())
        dataMap.PasteTabText({| x = 6; y = 1 |}, unit.ToTabStr())

        Expect.equal
            dataMap.DataContexts.[0].Explication.Value.NameText
            "explicit"
            "The term should stay in its target column."

        Expect.equal
            dataMap.DataContexts.[0].Explication.Value.TermSourceREF
            (Some "TST")
            "Term metadata should be preserved."

        Expect.equal
            dataMap.DataContexts.[0].Unit.Value.NameText
            "metre"
            "The unit name should not shift one column right."

        Expect.equal dataMap.DataContexts.[0].Unit.Value.TermSourceREF (Some "UO") "Unit metadata should be preserved."
        Expect.equal dataMap.DataContexts.[0].ObjectType None "Pasting a unit must not modify the following column."

        let structuredTarget = DataMap(ResizeArray [ DataContext() ])
        let payload = Swate.Components.ClipboardCodec.createPayload [| [| unit |] |]
        structuredTarget.PastePayload({| x = 6; y = 1 |}, payload)

        Expect.equal
            structuredTarget.DataContexts.[0].Unit.Value.NameText
            "metre"
            "Structured unit paste should preserve the unit."

        Expect.equal
            structuredTarget.DataContexts.[0].Unit.Value.TermAccessionNumber
            (Some "UO:0000008")
            "Structured unit paste should preserve ontology metadata."

    static member ClipboardCodecRoundTripsCompositeCells() =
        let cells = [|
            [|
                CompositeCell.createFreeText "plain"
                CompositeCell.createTermFromString ("term", "TST", "TST:1")
                CompositeCell.createUnitizedFromString ("4", "metre", "UO", "UO:0000008")
                CompositeCell.createDataFromString "file.txt#row=2"
            |]
        |]

        let decoded =
            cells
            |> Swate.Components.ClipboardCodec.createPayload
            |> Swate.Components.ClipboardCodec.encode
            |> Swate.Components.ClipboardCodec.tryDecode
            |> Option.get

        let actual =
            decoded.Rows
            |> Array.map (Array.map (Swate.Components.ClipboardCodec.toCompositeCell >> _.ToTabStr()))

        let expected = cells |> Array.map (Array.map _.ToTabStr())
        Expect.equal actual expected "The typed clipboard codec should round-trip every composite cell kind."

    static member TableCopyIncludesSelector() =
        let dataCell = CompositeCell.createDataFromString "DatamapTesting.txt#row=2"

        Expect.equal
            (dataCell.ToClipboardStr())
            "DatamapTesting.txt#row=2"
            "Copied table data should include its selector in the displayed clipboard cell."

        Expect.equal
            (CompositeCell.ToClipboardTableTxt [| [| dataCell; CompositeCell.FreeText "label" |] |])
            "DatamapTesting.txt#row=2\tlabel"
            "A table data cell and its selector must occupy one clipboard cell."

    static member DataMapCellPastePreservesSelector() =
        let dataMap = DataMap(ResizeArray [ DataContext() ])

        dataMap.PasteTabText({| x = 1; y = 1 |}, "DatamapTesting.txt#row=2")

        Expect.equal dataMap.DataContexts.[0].FilePath (Some "DatamapTesting.txt") "The path should be pasted."
        Expect.equal dataMap.DataContexts.[0].Selector (Some "row=2") "The selector should be pasted."

    static member DataMapLabelSurvivesArcFileRefresh() =
        let assay = ArcAssay.init "assay"
        assay.DataMap <- Some(DataMap(ResizeArray [ DataContext(label = "asdhjasklhd") ]))

        let refreshed =
            Swate.Components.Shared.ARCtrlHelper.ArcFiles.refreshRef (
                Swate.Components.Shared.ARCtrlHelper.ArcFiles.Assay assay
            )

        let label = refreshed.TryGetDataMap().Value.DataContexts.[0].Label

        Expect.equal label (Some "asdhjasklhd") "Refreshing an ARC file must preserve DataMap labels."

    static member DataMapGridPasteGrowsRows() =
        let dataMap = DataMap(ResizeArray [ DataContext() ])

        dataMap.PasteTabText({| x = 1; y = 1 |}, "first.txt#row=2\tFirst\nsecond.txt#row=3\tSecond")

        Expect.equal dataMap.DataContexts.Count 2 "The DataMap should grow for additional clipboard rows."
        Expect.equal dataMap.DataContexts.[1].FilePath (Some "second.txt") "The second path should be pasted."
        Expect.equal dataMap.DataContexts.[1].Selector (Some "row=3") "The second selector should be pasted."
        Expect.equal dataMap.DataContexts.[1].Label (Some "Second") "The second label should be pasted."

    static member DataMapFillColumnCopiesCompleteCell() =
        let dataMap =
            DataMap(
                ResizeArray [
                    DataContext(name = "first.txt#row=2", format = "text/csv")
                    DataContext(name = "second.txt#row=3")
                ]
            )

        Swate.Components.Composite.Table.Helper.fillColumn
            dataMap.RowCount
            {| x = 1; y = 1 |}
            (fun coordinate -> dataMap.GetCell(coordinate.x - 1, coordinate.y - 1))
            _.Copy()
            (fun coordinate cell -> dataMap.SetCell(coordinate.x - 1, coordinate.y - 1, cell))

        Expect.equal dataMap.DataContexts.[1].FilePath (Some "first.txt") "The complete source path should be copied."
        Expect.equal dataMap.DataContexts.[1].Selector (Some "row=2") "The complete source selector should be copied."
        Expect.equal dataMap.DataContexts.[1].Format (Some "text/csv") "The complete source format should be copied."

    static member DataMapRowAndClearActions() =
        let dataMap =
            DataMap(
                ResizeArray [
                    DataContext(name = "first.txt#row=2", label = "First")
                    DataContext(name = "second.txt#row=3", label = "Second")
                ]
            )

        dataMap.ClearCells [ {| x = 2; y = 1 |} ]
        Expect.equal dataMap.DataContexts.[0].Label None "Clear should affect only the selected cell."

        for rowIndex in 0 .. dataMap.RowCount - 1 do
            dataMap.Clear(0, rowIndex)

        Expect.equal dataMap.DataContexts.[0].FilePath None "Clear Column should clear the first row."
        Expect.equal dataMap.DataContexts.[1].FilePath None "Clear Column should clear the second row."

        ([ {| x = 1; y = 1 |} ]: CellCoordinate list)
        |> Swate.Components.Composite.Table.Helper.selectedRowIndices dataMap.RowCount
        |> Array.iter dataMap.DataContexts.RemoveAt

        Expect.equal dataMap.DataContexts.Count 1 "Only the unselected row should remain."

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
            testCase "Paste uses clicked column rather than stale selection"
            <| fun _ -> TestCases.PasteIntoClickedColumnOutsideSelection()
            testCase "DataMap cell copy includes the selector behind #"
            <| fun _ -> TestCases.DataMapCopyIncludesSelector()
            testCase "DataMap term copy does not shift following columns"
            <| fun _ -> TestCases.DataMapCopyKeepsTermsInGridColumns()
            testCase "DataMap paste recognizes annotation-table terms and units"
            <| fun _ -> TestCases.DataMapPasteRecognizesCompositeTermsAndUnits()
            testCase "Typed clipboard codec round-trips composite cells"
            <| fun _ -> TestCases.ClipboardCodecRoundTripsCompositeCells()
            testCase "Table cell copy includes the selector behind #"
            <| fun _ -> TestCases.TableCopyIncludesSelector()
            testCase "DataMap cell paste includes the selector behind #"
            <| fun _ -> TestCases.DataMapCellPastePreservesSelector()
            testCase "DataMap labels survive ARC file refreshes"
            <| fun _ -> TestCases.DataMapLabelSurvivesArcFileRefresh()
            testCase "DataMap grid paste grows rows"
            <| fun _ -> TestCases.DataMapGridPasteGrowsRows()
            testCase "DataMap fill column copies the complete cell"
            <| fun _ -> TestCases.DataMapFillColumnCopiesCompleteCell()
            testCase "DataMap delete-row and clear actions update the selected targets"
            <| fun _ -> TestCases.DataMapRowAndClearActions()
            testCase "Data main-field paste preserves the selector behind #"
            <| fun _ -> TestCases.DataMainFieldPreservesSelector()
            testCase "Header delete targets first column correctly"
            <| fun _ -> TestCases.HeaderDeleteFirstColumn()
            testCase "Index delete targets first row correctly"
            <| fun _ -> TestCases.IndexDeleteFirstRow()
            testCase "Header move column keeps 1-based header index"
            <| fun _ -> TestCases.HeaderMoveColumnUsesSelectedHeaderIndex()
        ]
    ]
