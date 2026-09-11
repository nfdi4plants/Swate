module Spreadsheet.Tests.Clipboard

open Fable.Mocha
open ARCtrl
open Swate.Components.Shared
open global.Spreadsheet

let private createTableState () =
    let table = ArcTable.init "ClipboardTest"

    table.AddColumn(
        CompositeHeader.FreeText "First",
        ResizeArray [ CompositeCell.FreeText ""; CompositeCell.FreeText "" ]
    )

    table.AddColumn(
        CompositeHeader.Output IOType.Data,
        ResizeArray [
            CompositeCell.createDataFromString ""
            CompositeCell.createDataFromString ""
        ]
    )

    let assay = ArcAssay.init "TestAssay"
    assay.AddTable table
    Spreadsheet.Model.init (ArcFiles.Assay assay, Spreadsheet.ActiveView.Table 0)

let private createTermTableState (cell: CompositeCell) =
    let table = ArcTable.init "ClipboardTermTest"

    table.AddColumn(CompositeHeader.Characteristic(OntologyAnnotation.create "Measurement"), ResizeArray [ cell ])

    let assay = ArcAssay.init "TestAssay"
    assay.AddTable table
    Spreadsheet.Model.init (ArcFiles.Assay assay, Spreadsheet.ActiveView.Table 0)

let Main =
    testList "Spreadsheet Clipboard" [
        testCase "retains the shape of a multi-column selection"
        <| fun _ ->
            let state = createTableState ()
            state.ActiveTable.SetCellAt(0, 0, CompositeCell.FreeText "A")
            state.ActiveTable.SetCellAt(1, 0, CompositeCell.FreeText "B")
            state.ActiveTable.SetCellAt(0, 1, CompositeCell.FreeText "C")
            state.ActiveTable.SetCellAt(1, 1, CompositeCell.FreeText "D")

            let rows =
                Spreadsheet.Controller.Clipboard.getCellRowsByIndex
                    [|
                        {| x = 1; y = 1 |}
                        {| x = 0; y = 0 |}
                        {| x = 1; y = 0 |}
                        {| x = 0; y = 1 |}
                    |]
                    state
                |> Array.map (Array.map _.ToString())

            Expect.equal rows [| [| "A"; "B" |]; [| "C"; "D" |] |] "Selection shape should be retained."

        testCase "pastes every cell from a multi-column payload"
        <| fun _ ->
            let state = createTableState ()

            let payload =
                [|
                    [| CompositeCell.FreeText "A"; CompositeCell.FreeText "B" |]
                    [| CompositeCell.FreeText "C"; CompositeCell.FreeText "D" |]
                |]
                |> Swate.Components.ClipboardCodec.createPayload

            Spreadsheet.Controller.Clipboard.pastePayloadByIndexExtend {| x = 0; y = 0 |} payload state
            |> ignore

            let topLeft = state.ActiveTable.GetCellAt(0, 0)
            let topRight = state.ActiveTable.GetCellAt(1, 0)
            let bottomLeft = state.ActiveTable.GetCellAt(0, 1)
            let bottomRight = state.ActiveTable.GetCellAt(1, 1)

            Expect.equal (topLeft.ToString()) "A" "Top-left cell should be pasted."
            Expect.isTrue topLeft.isFreeText "The first column should use its FreeText header."
            Expect.equal (topRight.ToString()) "B" "Top-right cell should be pasted."
            Expect.isTrue topRight.isData "The second column should use its Data header."
            Expect.equal (bottomLeft.ToString()) "C" "Bottom-left cell should be pasted."
            Expect.isTrue bottomLeft.isFreeText "The first column should use its FreeText header."
            Expect.equal (bottomRight.ToString()) "D" "Bottom-right cell should be pasted."
            Expect.isTrue bottomRight.isData "The second column should use its Data header."

        testCase "pastes a term over a unitized cell"
        <| fun _ ->
            let destination =
                CompositeCell.createUnitizedFromString ("4", "metre", "UO", "UO:0000008")

            let state = createTermTableState destination
            let source = CompositeCell.createTermFromString ("explicit", "TST", "TST:1")
            let payload = Swate.Components.ClipboardCodec.createPayload [| [| source |] |]

            Spreadsheet.Controller.Clipboard.pastePayloadByIndexExtend {| x = 0; y = 0 |} payload state
            |> ignore

            let pasted = state.ActiveTable.GetCellAt(0, 0)
            Expect.isTrue pasted.isTerm "A pasted term should replace the destination unitized cell as a term."
            Expect.equal pasted.AsTerm.NameText "explicit" "The term name should be preserved."
            Expect.equal pasted.AsTerm.TermSourceREF (Some "TST") "The term source should be preserved."
            Expect.equal pasted.AsTerm.TermAccessionNumber (Some "TST:1") "The accession should be preserved."

        testCase "pastes a unitized cell over a term"
        <| fun _ ->
            let destination = CompositeCell.createTermFromString ("explicit", "TST", "TST:1")
            let state = createTermTableState destination

            let source =
                CompositeCell.createUnitizedFromString ("4", "metre", "UO", "UO:0000008")

            let payload = Swate.Components.ClipboardCodec.createPayload [| [| source |] |]

            Spreadsheet.Controller.Clipboard.pastePayloadByIndexExtend {| x = 0; y = 0 |} payload state
            |> ignore

            let pasted = state.ActiveTable.GetCellAt(0, 0)

            Expect.isTrue
                pasted.isUnitized
                "A pasted unitized cell should replace the destination term as a unitized cell."

            let value, unit = pasted.AsUnitized
            Expect.equal value "4" "The unitized value should be preserved."
            Expect.equal unit.NameText "metre" "The unit name should be preserved."
            Expect.equal unit.TermSourceREF (Some "UO") "The unit source should be preserved."

            Expect.equal unit.TermAccessionNumber (Some "UO:0000008") "The unit accession should be preserved."
    ]
