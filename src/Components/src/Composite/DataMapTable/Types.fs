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

    type DataMap with

        member this.GetSelectedCells(coordinates: seq<CellCoordinate>) =
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

        member this.SelectedCellsToTabText(coordinates: seq<CellCoordinate>) =
            this.GetSelectedCells(coordinates)
            |> Array.map (fun row ->
                row
                |> Array.map (fun cell ->
                    match cell with
                    | CompositeCell.Data _ -> cell.ToClipboardStr()
                    | _ -> cell.ToString()
                )
                |> String.concat "\t"
            )
            |> String.concat System.Environment.NewLine

        member this.PastePayload(startCoordinate: CellCoordinate, payload: Swate.Components.ClipboardCodec.Payload) =
            let requiredRowCount = startCoordinate.y - 1 + payload.Rows.Length

            if requiredRowCount > this.RowCount then
                this.DataContexts.AddRange(Array.init (requiredRowCount - this.RowCount) (fun _ -> DataContext()))

            payload.Rows
            |> Array.iteri (fun rowOffset row ->
                row
                |> Array.iteri (fun columnOffset dto ->
                    let columnIndex = startCoordinate.x - 1 + columnOffset
                    let rowIndex = startCoordinate.y - 1 + rowOffset

                    if columnIndex < this.ColumnCount then
                        let source = Swate.Components.ClipboardCodec.toCompositeCell dto
                        let target = this.GetCell(columnIndex, rowIndex)

                        let cell =
                            match source, columnIndex with
                            | CompositeCell.Unitized(_, unit), DataMapIndices.Unit -> CompositeCell.createTerm unit
                            | CompositeCell.Term term, _ when this.GetHeader(columnIndex).IsTermColumn ->
                                CompositeCell.createTerm term
                            | CompositeCell.Data _, DataMapIndices.Data -> source
                            | _ -> target.UpdateMainField(source.ToString())

                        this.SetCell(columnIndex, rowIndex, cell)
                )
            )

        member this.PasteTabText(startCoordinate: CellCoordinate, clipboardText: string) =
            let rows =
                clipboardText.TrimEnd([| '\r'; '\n' |]).Split([| "\r\n"; "\n"; "\r" |], System.StringSplitOptions.None)

            let requiredRowCount = startCoordinate.y - 1 + rows.Length

            if requiredRowCount > this.RowCount then
                this.DataContexts.AddRange(Array.init (requiredRowCount - this.RowCount) (fun _ -> DataContext()))

            rows
            |> Array.iteri (fun rowOffset row ->
                let values = row.Split '\t'
                let startColumnIndex = startCoordinate.x - 1
                let rowIndex = startCoordinate.y - 1 + rowOffset

                values
                |> Array.iteri (fun columnOffset value ->
                    let columnIndex = startColumnIndex + columnOffset

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
