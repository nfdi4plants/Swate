module Spreadsheet.Controller.Clipboard

open Fable.Core
open ARCtrl
open Swate.Components
open Swate.Components.Shared
open Swate.Components.ClipboardCodec

let getCellRowsByIndex (indices: CellCoordinate[]) (state: Spreadsheet.Model) =
    indices
    |> Array.groupBy _.y
    |> Array.sortBy fst
    |> Array.map (fun (_, row) ->
        row
        |> Array.sortBy _.x
        |> Array.map (fun index -> Generic.getCell (index.x, index.y) state)
    )

let copyCellByIndex (index: CellCoordinate) (state: Spreadsheet.Model) : JS.Promise<unit> =
    let cell = Generic.getCell (index.x, index.y) state
    ClipboardCodec.write (cell.ToClipboardStr()) (Some [| [| cell |] |])

let copyCellsByIndex (indices: CellCoordinate[]) (state: Spreadsheet.Model) : JS.Promise<unit> =
    let rows = getCellRowsByIndex indices state
    ClipboardCodec.write (CompositeCell.ToClipboardTableTxt rows) (Some rows)

let cutCellByIndex (index: CellCoordinate) (state: Spreadsheet.Model) : Spreadsheet.Model =
    let cell = Generic.getCell (index.x, index.y) state
    Table.clearCells [| index |] state |> ignore

    ClipboardCodec.write (cell.ToClipboardStr()) (Some [| [| cell |] |])
    |> Promise.start

    state

let cutCellsByIndices (indices: CellCoordinate[]) (state: Spreadsheet.Model) : Spreadsheet.Model =
    let rows = getCellRowsByIndex indices state
    Table.clearCells indices state |> ignore

    ClipboardCodec.write (CompositeCell.ToClipboardTableTxt rows) (Some rows)
    |> Promise.start

    state

let pasteCellByIndex (index: CellCoordinate) (state: Spreadsheet.Model) : JS.Promise<Spreadsheet.Model> = promise {
    let! content = ClipboardCodec.read ()
    let header = Generic.getHeader index.x state

    let cell =
        content.Payload
        |> Option.bind (fun payload -> payload.Rows |> Array.tryHead |> Option.bind Array.tryHead)
        |> Option.map (ClipboardCodec.toCompositeCell >> _.ConvertToValidCell(header))
        |> Option.defaultWith (fun () -> CompositeCell.fromTabTxt content.PlainText header |> Array.head)

    Generic.setCell (index.x, index.y) cell state
    return state
}

let pastePayloadByIndexExtend
    (index: CellCoordinate)
    (payload: ClipboardCodec.Payload)
    (state: Spreadsheet.Model)
    : Spreadsheet.Model =
    let columnCount = Generic.getColCount state

    let indexedCells =
        payload.Rows
        |> Array.mapi (fun rowOffset row ->
            row
            |> Array.mapi (fun columnOffset cell ->
                let columnIndex = index.x + columnOffset

                if columnIndex < columnCount then
                    let header = Generic.getHeader columnIndex state
                    let cell = ClipboardCodec.toCompositeCell cell |> _.ConvertToValidCell(header)

                    let coordinate: CellCoordinate = {|
                        x = columnIndex
                        y = index.y + rowOffset
                    |}

                    Some(coordinate, cell)
                else
                    None
            )
            |> Array.choose id
        )
        |> Array.concat

    Generic.setCells indexedCells state
    state

let pasteCellsByIndexExtend (index: CellCoordinate) (state: Spreadsheet.Model) : JS.Promise<Spreadsheet.Model> = promise {
    let! content = ClipboardCodec.read ()

    match content.Payload with
    | Some payload -> return pastePayloadByIndexExtend index payload state
    | None ->
        let header = Generic.getHeader index.x state
        let cells = CompositeCell.fromTabTxt content.PlainText header

        let indexedCells =
            cells
            |> Array.indexed
            |> Array.map (fun (i, c) ->
                let coordinate: CellCoordinate = {| x = index.x; y = index.y + i |}
                (coordinate, c)
            )

        Generic.setCells indexedCells state
        return state
}
