module Components.Tests.Table.ContextMenu

open Fable.Mocha
open Fable.React
open Fable.React.Props
open Fable.Core
open Fable.Core.JsInterop
open Browser.Types
open ARCtrl
open ARCtrl.Spreadsheet
open Swate.Components
open AnnotationTableContextMenu
open Fixture
open Feliz
open Types.AnnotationTable

type TestCases =

    static member AddColumns() =
        let pasteData = Fixture.Column_Component_InstrumentModel

        let newCompositeColumns =
            let body =
                let rest = pasteData.[1..]
                if rest.Length > 0 then rest else [||]

            let columns = Array.append [| pasteData.[0] |] body |> Array.transpose
            let columnsList = columns |> Seq.toArray |> Array.map (Seq.toArray)
            ARCtrl.Spreadsheet.ArcTable.composeColumns columnsList |> ResizeArray

        let currentTable = Fixture.mkTable ()
        let clickedCell: CellCoordinate = {| x = 1; y = 1 |}

        let selectHandle: SelectHandle = Fixture.mkSelectHandle (1, 2, 1, 3)

        let pasteBehavior =
            Swate.Components.AnnotationTableContextMenu.AnnotationTableContextMenuUtil.predictPasteBehaviour (
                clickedCell,
                currentTable,
                selectHandle,
                pasteData
            )

        Expect.equal
            pasteBehavior
            (PasteCases.AddColumns {|
                data = newCompositeColumns
                columnIndex = clickedCell.x - 1
            |})
            "Should predict add columns behavior"

    static member AddSingleCell() =
        let pasteData = Fixture.Body_Component_InstrumentModel_SingleRow

        let compositeCell = CompositeCell.createFreeText (pasteData.[0].[0])

        let currentTable = Fixture.mkTable ()
        let clickedCell: CellCoordinate = {| x = 1; y = 1 |}

        let selectHandle: SelectHandle = Fixture.mkSelectHandle (1, 1, 1, 1)

        let pasteBehavior =
            Swate.Components.AnnotationTableContextMenu.AnnotationTableContextMenuUtil.predictPasteBehaviour (
                clickedCell,
                currentTable,
                selectHandle,
                pasteData
            )

        Expect.equal
            pasteBehavior
            (PasteCases.PasteColumns {|
                data = [| [| compositeCell |] |]
                coordinates = [| [| clickedCell |] |]
            |})
            "Should predict paste single cell behavior"

    static member PasteMultipleCells(rowEnd, columEnd, pasteData: string[][]) =
        let currentTable = Fixture.mkTable ()
        let selectHandle: SelectHandle = Fixture.mkSelectHandle (1, columEnd, 1, rowEnd)
        let cellCoordinates = Fixture.getRangeOfSelectedCells (selectHandle)

        let headers =
            let columnIndices =
                selectHandle.getSelectedCells ()
                |> Array.ofSeq
                |> Array.distinctBy (fun item -> item.x)

            columnIndices
            |> Array.map (fun index -> currentTable.GetColumn(index.x - 1).Header)

        let compositeCells =
            pasteData.[0 .. columEnd - 1]
            |> Array.map (fun row ->
                row.[0 .. rowEnd - 1]
                |> Array.mapi (fun i item -> CompositeCell.fromContentValid ([| item |], headers.[i]))
            )

        let clickedCell: CellCoordinate = {| x = 1; y = 1 |}

        let pasteBehavior =
            Swate.Components.AnnotationTableContextMenu.AnnotationTableContextMenuUtil.predictPasteBehaviour (
                clickedCell,
                currentTable,
                selectHandle,
                pasteData
            )

        Expect.equal
            pasteBehavior
            (PasteCases.PasteColumns {|
                data = compositeCells
                coordinates = cellCoordinates
            |})
            "Should predict paste fitted cells behavior"

    static member AddFittingTerm(startColumn: int, startRow: int, pasteData: string[][]) =
        let currentTable = Fixture.mkTable ()
        let selectHandle: SelectHandle = Fixture.mkSelectHandle (1, 1, 3, 3)
        let cellCoordinates = Fixture.getRangeOfSelectedCells (selectHandle)

        let headers =
            let columnIndices =
                selectHandle.getSelectedCells ()
                |> Array.ofSeq
                |> Array.distinctBy (fun item -> item.x)

            columnIndices
            |> Array.map (fun index -> currentTable.GetColumn(index.x - 1).Header)

        let clickedCell: CellCoordinate = {| x = 4; y = 1 |}

        let adaptedData =
            pasteData.[startRow..] |> Array.map (fun item -> item.[startColumn..])

        let pasteBehavior =
            Swate.Components.AnnotationTableContextMenu.AnnotationTableContextMenuUtil.predictPasteBehaviour (
                clickedCell,
                currentTable,
                selectHandle,
                adaptedData
            )

        let fittedCells =
            Swate.Components.AnnotationTableContextMenu.AnnotationTableContextMenuUtil.getFittedCells (
                adaptedData,
                headers
            )

        Expect.equal
            pasteBehavior
            (PasteCases.PasteColumns {|
                data = fittedCells
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
            Swate.Components.AnnotationTableContextMenu.AnnotationTableContextMenuUtil.predictPasteBehaviour (
                clickedCell,
                currentTable,
                selectHandle,
                adaptedData
            )

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
            testCase "Add columns" <| fun _ -> TestCases.AddColumns()
            testCase "Paste single Cell" <| fun _ -> TestCases.AddSingleCell()
            testCase $"Paste {2} Cell(s) in the same row. Paste {1} Cell(s) in the same column"
            <| fun _ -> TestCases.PasteMultipleCells(2, 1, Fixture.Body_Component_InstrumentModel_SingleRow)
            testCase $"Paste {2} Cell(s) in the same row. Paste {1} Cell(s) in the same column"
            <| fun _ -> TestCases.PasteMultipleCells(3, 1, Fixture.Body_Component_InstrumentModel_SingleRow)
            testCase $"Paste {3} Cell(s) in the same row. Paste {1} Cell(s) in the same column"
            <| fun _ -> TestCases.PasteMultipleCells(1, 2, Fixture.Body_Component_InstrumentModel_TwoRows)
            testCase $"Paste {2} Cell(s) in the same row. Paste {2} Cell(s) in the same column"
            <| fun _ -> TestCases.PasteMultipleCells(2, 2, Fixture.Body_Component_InstrumentModel_TwoRows)
            testCase $"Paste {3} Cell(s) in the same row. Paste {2} Cell(s) in the same column"
            <| fun _ -> TestCases.PasteMultipleCells(3, 2, Fixture.Body_Component_InstrumentModel_TwoRows)
            testCase $"Add fitting Term"
            <| fun _ -> TestCases.AddFittingTerm(2, 0, Fixture.Body_Component_InstrumentModel_SingleRow_Term)
            testCase $"Add fitting Term and 1 freetext"
            <| fun _ -> TestCases.AddFittingTerm(1, 0, Fixture.Body_Component_InstrumentModel_SingleRow_Term)
            testCase $"Add fitting Term and 2 freetexts"
            <| fun _ -> TestCases.AddFittingTerm(0, 0, Fixture.Body_Component_InstrumentModel_SingleRow_Term)
            testCase $"Add unit value"
            <| fun _ -> TestCases.AddFittingTerm(0, 0, Fixture.Body_Integer)
            testCase $"Convert term to unit"
            <| fun _ -> TestCases.AddFittingTerm(0, 0, Fixture.Body_Integer)
            testCase $"Add unknown value"
            <| fun _ -> TestCases.AddUnknownPattern(Fixture.Body_Empty)
        ]
    ]