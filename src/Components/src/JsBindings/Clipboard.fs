module Swate.Components.JsBindings.Clipboard

open Fable.Core
open ARCtrl
open Swate.Components

type ClipboardTableApi<'State> = {
    GetCell: CellCoordinate -> 'State -> CompositeCell
    SetCell: CellCoordinate -> CompositeCell -> 'State -> unit
    GetHeader: int -> 'State -> CompositeHeader
    SetCells: (CellCoordinate * CompositeCell)[] -> 'State -> unit
}

let writeText (text: string) : JS.Promise<unit> =
    Swate.Components.Shared.JsBindings.Clipboard.navigator.clipboard.writeText text

let readText () : JS.Promise<string> =
    Swate.Components.Shared.JsBindings.Clipboard.navigator.clipboard.readText ()

let copyCell (cell: CompositeCell) : JS.Promise<unit> = cell.ToTabStr() |> writeText

let copyCells (cells: CompositeCell[]) : JS.Promise<unit> =
    CompositeCell.ToTabTxt cells |> writeText

let copyCellByIndex (api: ClipboardTableApi<'State>) (index: CellCoordinate) (state: 'State) : JS.Promise<unit> =
    api.GetCell index state |> copyCell

let copyCellsByIndex (api: ClipboardTableApi<'State>) (indices: CellCoordinate[]) (state: 'State) : JS.Promise<unit> =
    indices |> Array.map (fun index -> api.GetCell index state) |> copyCells

let cutCellByIndex (api: ClipboardTableApi<'State>) (index: CellCoordinate) (state: 'State) : 'State =
    let cell = api.GetCell index state
    api.SetCell index (cell.GetEmptyCellFixed()) state
    copyCell cell |> Promise.start
    state

let cutCellsByIndices (api: ClipboardTableApi<'State>) (indices: CellCoordinate[]) (state: 'State) : 'State =
    let cells = ResizeArray()

    for index in indices do
        let cell = api.GetCell index state
        api.SetCell index (cell.GetEmptyCellFixed()) state
        cells.Add cell

    copyCells (Array.ofSeq cells) |> Promise.start
    state

let readCells (header: CompositeHeader) : JS.Promise<CompositeCell[]> = promise {
    let! tab = readText ()
    return CompositeCell.fromTabTxt tab header
}

let readCell (header: CompositeHeader) : JS.Promise<CompositeCell> = promise {
    let! cells = readCells header
    return Array.head cells
}

let pasteCellByIndex (api: ClipboardTableApi<'State>) (index: CellCoordinate) (state: 'State) : JS.Promise<'State> = promise {
    let header = api.GetHeader index.x state
    let! cell = readCell header
    api.SetCell index cell state
    return state
}

let pasteCellsByIndexExtend
    (api: ClipboardTableApi<'State>)
    (index: CellCoordinate)
    (state: 'State)
    : JS.Promise<'State> =
    promise {
        let header = api.GetHeader index.x state
        let! cells = readCells header

        let indexedCells =
            cells
            |> Array.indexed
            |> Array.map (fun (i, cell) ->
                let coordinate: CellCoordinate = {| x = index.x; y = index.y + i |}
                coordinate, cell
            )

        api.SetCells indexedCells state
        return state
    }
