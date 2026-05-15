[<AutoOpen>]
module ARCtrl.ArcTableExtensions

open ARCtrl
open ArcTableAux
open Swate.Components.Types
open ARCtrl.Spreadsheet

type ArcTable with
    member this.ClearCell(cellIndex: CellCoordinate) =
        let index = (cellIndex.x - 1, cellIndex.y - 1)
        let c = this.GetCellAt(index).GetEmptyCellFixed()
        this.SetCellAt(cellIndex.x - 1, cellIndex.y - 1, c)


    member this.ClearSelectedCells(selectHandle: SelectHandle) =
        let selectedCells = selectHandle.getSelectedCells ()
        let indices = selectedCells |> Seq.map (fun i -> (i.x - 1, i.y - 1)) |> Seq.toArray

        indices
        |> Array.iter (fun (ci, ri) ->
            let tempIndex = (ci, ri)
            let prev = this.GetCellAt(tempIndex)
            let next = prev.GetEmptyCellFixed()

            this.SetCellAt(ci, ri, next, true)
        )

    member this.SetCellsAt(cells: (CellCoordinate * CompositeCell)[]) =
        let columns = cells |> Array.groupBy (fun (index, cell) -> index)

        for coordinate, items in columns do
            SanityChecks.validateColumn
            <| CompositeColumn.create (this.Headers.[coordinate.x], (items |> Array.map snd) |> ResizeArray)

        for index, cell in cells do
            this.SetCellAt(index.x, index.y, cell, true)

    /// <summary>
    /// Returns a new ArcTable from all columns defined by ``indices``.
    /// </summary>
    member this.Subtable(indices: int[]) =
        let cols = indices |> Array.sort |> Array.map this.GetColumn
        let table = ArcTable.init (this.Name + " Subtable")

        for col in cols do
            table.AddColumn(col.Header, col.Cells)

        table

    /// <summary>
    /// Transforms ArcTable to excel compatible "values", row major
    /// </summary>
    member this.ToStringSeqs() =

        // Cancel if there are no columns
        if this.Columns.Count = 0 then
            [||]
        else
            let columns =
                this.Columns
                |> List.ofSeq
                |> List.sortBy ArcTable.classifyColumnOrder
                |> List.collect CompositeColumn.toStringCellColumns
                |> Seq.transpose
                |> Seq.map (fun column -> column |> Array.ofSeq)
                |> Array.ofSeq

            columns