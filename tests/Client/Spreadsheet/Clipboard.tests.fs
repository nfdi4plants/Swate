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
        CompositeHeader.FreeText "Second",
        ResizeArray [ CompositeCell.FreeText ""; CompositeCell.FreeText "" ]
    )

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

            Expect.equal (state.ActiveTable.GetCellAt(0, 0).ToString()) "A" "Top-left cell should be pasted."
            Expect.equal (state.ActiveTable.GetCellAt(1, 0).ToString()) "B" "Top-right cell should be pasted."
            Expect.equal (state.ActiveTable.GetCellAt(0, 1).ToString()) "C" "Bottom-left cell should be pasted."
            Expect.equal (state.ActiveTable.GetCellAt(1, 1).ToString()) "D" "Bottom-right cell should be pasted."
    ]
