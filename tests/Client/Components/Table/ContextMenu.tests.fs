module Components.Tests.Table.ContextMenu

open Fable.Mocha
open ARCtrl
open ARCtrl.Spreadsheet
open Swate.Components
open AnnotationTableContextMenu
open Fixture

type TestCases =
    static member AddColumns () =
        testCase "Add columns"
            <| fun _ ->
                let pasteData = Fixture.Column_Component_InstrumentModel

                let newCompositeColumns =
                    let body =
                        let rest = pasteData.[1..]
                        if rest.Length > 0 then rest
                        else [||]
                    let columns = Array.append [| pasteData.[0] |] body |> Array.transpose
                    let columnsList = columns |> Seq.toArray |> Array.map (Seq.toArray)
                    ARCtrl.Spreadsheet.ArcTable.composeColumns columnsList |> ResizeArray

                let currentTable = Fixture.mkTable ()
                let clickedCell: CellCoordinate = {| x = 1; y = 1 |}

                let selectHandle: SelectHandle = Fixture.mkSelectHandle (1, 2, 1, 3)

                let pasteBehavior =
                    Swate.Components.AnnotationTableContextMenuUtil.predictPasteBehaviour (
                        clickedCell,
                        currentTable,
                        selectHandle,
                        pasteData
                    )

                Expect.equal
                    pasteBehavior
                    (PasteCases.AddColumns {|
                        data = newCompositeColumns
                        columnIndex = clickedCell.x
                    |})
                    "Should predict paste columns behavior"

    static member AddSingleCell () =
        testCase "Add single Cell"
            <| fun _ ->
                let pasteData = Fixture.Body_Component_InstrumentModel_SingleRow

                let compositeCell = CompositeCell.createFreeText(pasteData.[0].[0])

                let currentTable = Fixture.mkTable ()
                let clickedCell: CellCoordinate = {| x = 1; y = 1 |}

                let selectHandle: SelectHandle = Fixture.mkSelectHandle (1, 1, 1, 1)

                let pasteBehavior =
                    Swate.Components.AnnotationTableContextMenuUtil.predictPasteBehaviour (
                        clickedCell,
                        currentTable,
                        selectHandle,
                        pasteData
                    )

                Expect.equal
                    pasteBehavior
                    (PasteCases.PasteColumns {|
                        data = [|[|compositeCell|]|]
                        coordinates = [|[|clickedCell|]|]
                    |})
                    "Should predict paste cell behavior"

    static member AddMultipleCells (rowEnd, columEnd, pasteData:string[][]) =
        testCase $"Add {rowEnd} Cell(s) in the same row. Add {columEnd} Cell(s) in the same column "
            <| fun _ ->
                let currentTable = Fixture.mkTable ()
                let selectHandle: SelectHandle = Fixture.mkSelectHandle (1, columEnd, 1, rowEnd)
                let cellCoordinates = Fixture.getRangeOfSelectedCells(selectHandle)
                let headers =
                    let columnIndices = selectHandle.getSelectedCells() |> Array.ofSeq |> Array.distinctBy (fun item -> item.x)

                    columnIndices
                    |> Array.map (fun index -> currentTable.GetColumn(index.x - 1).Header)

                let compositeCells =
                    pasteData.[0..columEnd-1]
                    |> Array.map (fun row -> row.[0..rowEnd-1] |> Array.mapi (fun i item -> CompositeCell.fromContentValid([|item|], headers.[i])))

                let clickedCell: CellCoordinate = {| x = 1; y = 1 |}

                let pasteBehavior =
                    Swate.Components.AnnotationTableContextMenuUtil.predictPasteBehaviour (
                        clickedCell,
                        currentTable,
                        selectHandle,
                        pasteData
                    )

                let termIndices, lengthWithoutTerms = CompositeCell.getHeaderParsingInfo (headers)

                if
                    termIndices.Length > 0
                    && pasteData.[0].Length >= termIndices.Length + lengthWithoutTerms then
                    Expect.equal
                        pasteBehavior
                        (PasteCases.PasteFittedColumns {|
                            data = compositeCells
                            coordinates = cellCoordinates
                        |})
                        "Should predict paste fitted cells behavior"
                else
                    Expect.equal
                        pasteBehavior
                        (PasteCases.PasteColumns {|
                            data = compositeCells
                            coordinates = cellCoordinates
                        |})
                        "Should predict paste cells behavior"

let Main =
    testList "Context Menu" [
        testList "Prediction" [
            TestCases.AddColumns()
            TestCases.AddSingleCell()
            TestCases.AddMultipleCells(2, 1, Fixture.Body_Component_InstrumentModel_SingleRow)
            TestCases.AddMultipleCells(3, 1, Fixture.Body_Component_InstrumentModel_SingleRow)
            TestCases.AddMultipleCells(1, 2, Fixture.Body_Component_InstrumentModel_TwoRows)
            TestCases.AddMultipleCells(2, 2, Fixture.Body_Component_InstrumentModel_TwoRows)
            TestCases.AddMultipleCells(3, 2, Fixture.Body_Component_InstrumentModel_TwoRows)
        ]
    ]