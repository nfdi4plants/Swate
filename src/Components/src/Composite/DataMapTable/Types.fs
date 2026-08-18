module Swate.Components.Composite.DataMapTable.Types

open Swate.Components
open Swate.Components.Composite.Table.Types

[<RequireQualifiedAccess>]
type Modal = Details of CellCoordinate

[<AutoOpen>]
module ARCtrlExtensions =

    open ARCtrl
    open Swate.Components
    open Helper
    open ArcTableAux

    type Datamap with

        member this.SelectedCellsToTabText(coordinates: seq<CellCoordinate>) =
            coordinates
            |> Seq.filter (fun coordinate -> coordinate.x > 0 && coordinate.y > 0)
            |> Seq.groupBy _.y
            |> Seq.sortBy fst
            |> Seq.map (fun (_, row) ->
                row
                |> Seq.sortBy _.x
                |> Seq.map (fun coordinate -> this.GetCell(coordinate.x - 1, coordinate.y - 1))
                |> Seq.toArray
            )
            |> Seq.toArray
            |> CompositeCell.ToClipboardTableTxt

        member this.PasteTabText(startCoordinate: CellCoordinate, clipboardText: string) =
            let rows =
                clipboardText.TrimEnd([| '\r'; '\n' |]).Split([| "\r\n"; "\n"; "\r" |], System.StringSplitOptions.None)

            let requiredRowCount = startCoordinate.y - 1 + rows.Length

            if requiredRowCount > this.RowCount then
                this.DataContexts.AddRange(Array.init (requiredRowCount - this.RowCount) (fun _ -> DataContext()))

            rows
            |> Array.iteri (fun rowOffset row ->
                row.Split '\t'
                |> Array.iteri (fun columnOffset value ->
                    let columnIndex = startCoordinate.x - 1 + columnOffset
                    let rowIndex = startCoordinate.y - 1 + rowOffset

                    if columnIndex < this.ColumnCount then
                        this.GetCell(columnIndex, rowIndex).UpdateMainField(value)
                        |> fun cell -> this.SetCell(columnIndex, rowIndex, cell)
                )
            )

        member this.ClearCells(coordinates: seq<CellCoordinate>) =
            coordinates
            |> Seq.distinct
            |> Seq.iter (fun coordinate -> this.Clear(coordinate.x - 1, coordinate.y - 1))

        member this.ClearSelectedCells(selectHandle: SelectHandle) =
            match selectHandle.getCount () with
            | c when c <= 100 ->
                let selectedCells = selectHandle.getSelectedCells ()

                this.ClearCells(selectedCells)
            | _ ->
                for col in 0 .. this.ColumnCount - 1 do
                    for row in 0 .. this.RowCount - 1 do
                        if selectHandle.contains ({| x = col + 1; y = row + 1 |}) then
                            this.Clear(col, row)
