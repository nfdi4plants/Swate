module Spreadsheet.Controller.Clipboard

open Fable.Core
open ARCtrl
open Swate.Components
open Swate.Components.Shared

let copyCell (cell: CompositeCell) : JS.Promise<unit> =
    let tab = cell.ToTabStr()
    navigator.clipboard.writeText (tab)

let copyCells (cells: CompositeCell[]) : JS.Promise<unit> =
    let tab = CompositeCell.ToTabTxt cells
    navigator.clipboard.writeText (tab)

let copyCellByIndex (index: CellCoordinate) (state: Spreadsheet.Model) : JS.Promise<unit> =
    let cell = Generic.getCell (index.x, index.y) state
    copyCell cell

let copyCellsByIndex (indices: CellCoordinate[]) (state: Spreadsheet.Model) : JS.Promise<unit> =
    let cells = [|
        for index in indices do
            yield Generic.getCell (index.x, index.y) state
    |]

    copyCells cells

let copySelectedCell (state: Spreadsheet.Model) : JS.Promise<unit> =
    /// Array.head is used until multiple cells are supported, should this ever be intended
    let index =
        if state.SelectedCells.IsSome then
            (state.SelectedCells.Value.xStart, state.SelectedCells.Value.yStart)
        else
            [||] |> Array.min
    let coordinate: CellCoordinate =
        {|
            x = fst index
            y = snd index
        |}
    copyCellByIndex coordinate state

let copySelectedCells (state: Spreadsheet.Model) : JS.Promise<unit> =
    /// Array.head is used until multiple cells are supported, should this ever be intended
    let indices =
        if state.SelectedCells.IsSome then
            CellCoordinateRange.toArray state.SelectedCells.Value |> Array.ofSeq
        else
            [| |]
    copyCellsByIndex indices state

let cutCellByIndex (index: CellCoordinate) (state: Spreadsheet.Model) : Spreadsheet.Model =
    let cell = Generic.getCell (index.x, index.y) state
    // Remove selected cell value
    let emptyCell = cell.GetEmptyCell()
    Generic.setCell (index.x, index.y) emptyCell state
    copyCell cell |> Promise.start
    state

let cutCellsByIndices (indices: CellCoordinate []) (state: Spreadsheet.Model) : Spreadsheet.Model =
    let cells = ResizeArray()

    for index in indices do
        let cell = Generic.getCell (index.x, index.y) state
        // Remove selected cell value
        let emptyCell = cell.GetEmptyCell()
        Generic.setCell (index.x, index.y) emptyCell state
        cells.Add(cell)

    copyCells (Array.ofSeq cells) |> Promise.start
    state

let cutSelectedCell (state: Spreadsheet.Model) : Spreadsheet.Model =
    /// Array.min is used until multiple cells are supported, should this ever be intended
    let index =
        if state.SelectedCells.IsSome then
            state.SelectedCells.Value.xStart, state.SelectedCells.Value.yStart
        else
            [||] |> Array.min
    let coordinate: CellCoordinate =
        {|
            x = fst index
            y = snd index
        |}
    cutCellByIndex coordinate state

let cutSelectedCells (state: Spreadsheet.Model) : Spreadsheet.Model =
    /// Array.min is used until multiple cells are supported, should this ever be intended
    let indices =
        if state.SelectedCells.IsSome then
            CellCoordinateRange.toArray state.SelectedCells.Value
            |> Array.ofSeq
        else
            [| |]
    cutCellsByIndices indices state

let pasteCellByIndex (index: CellCoordinate) (state: Spreadsheet.Model) : JS.Promise<Spreadsheet.Model> = promise {
    let! tab = navigator.clipboard.readText ()
    let header = Generic.getHeader index.x state
    let cell = CompositeCell.fromTabTxt tab header |> Array.head
    Generic.setCell (index.x, index.y) cell state
    return state
}

let pasteCellsByIndexExtend (index: CellCoordinate) (state: Spreadsheet.Model) : JS.Promise<Spreadsheet.Model> = promise {
    let! tab = navigator.clipboard.readText ()
    let header = Generic.getHeader index.x state
    let cells = CompositeCell.fromTabTxt tab header

    let indexedCells =
        cells
        |> Array.indexed
        |> Array.map (fun (i, c) ->
            let coordinate: CellCoordinate =
                {|
                    x = index.x
                    y = index.y + i
                |}
            (coordinate, c))

    Generic.setCells indexedCells state
    return state
}

let pasteCellIntoSelected (state: Spreadsheet.Model) : JS.Promise<Spreadsheet.Model> =
    if state.SelectedCells.IsSome then
        let coordinate: CellCoordinate =
            {|
                x = state.SelectedCells.Value.xStart
                y = state.SelectedCells.Value.yStart
            |}
        pasteCellByIndex coordinate state
    else
        promise { return state }

let pasteCellsIntoSelected (state: Spreadsheet.Model) : JS.Promise<Spreadsheet.Model> =
    if state.SelectedCells.IsSome then
        let columnIndex =
            if state.SelectedCells.IsSome then
                state.SelectedCells.Value.xStart
            else
                [||] |> Array.min

        let selectedSingleColumnCells =
            if state.SelectedCells.IsSome then
               CellCoordinateRange.toArray state.SelectedCells.Value |> Array.ofSeq |> Array.filter (fun index -> index.x = columnIndex)
            else
                [| |]

        promise {
            let! tab = navigator.clipboard.readText ()
            let header = Generic.getHeader columnIndex state
            let cells = CompositeCell.fromTabTxt tab header

            if cells.Length = 1 then
                let cell = cells.[0]

                let newCells =
                    selectedSingleColumnCells |> Array.ofSeq |> Array.map (fun index -> index, cell)

                Generic.setCells newCells state
                return state
            else
                let rowCount = selectedSingleColumnCells.Count
                let cellsTrimmed = cells |> Array.truncate rowCount

                let indicesTrimmed =
                    selectedSingleColumnCells.[0 .. cellsTrimmed.Length - 1]

                let indexedCellsTrimmed = Array.zip indicesTrimmed cellsTrimmed
                Generic.setCells indexedCellsTrimmed state
                return state
        }
    else
        promise { return state }