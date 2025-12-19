module Components.Tests.Table.ContextMenu

open Fable.Mocha
open ARCtrl
open Swate.Components
open AnnotationTableContextMenu
open Fixture

type TestCases =

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
    ]