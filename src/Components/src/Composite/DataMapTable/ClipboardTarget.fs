module Swate.Components.Composite.DataMapTable.ClipboardTarget

open global.ARCtrl
open Swate.Components
open Swate.Components.ClipboardContract.Contract
open Swate.Components.Composite.Table.Types

[<AutoOpen>]
module ARCtrlExtensions =

    type DataMap with

        member private this.ApplyClipboardCell(targetCoordinate: CellCoordinate, source: CompositeCell) =
            let columnIndex = targetCoordinate.x - 1
            let rowIndex = targetCoordinate.y - 1
            let target = this.GetCell(columnIndex, rowIndex)

            let cell =
                match source, columnIndex with
                | CompositeCell.Unitized(_, unit), column when this.GetHeader(column).IsTermColumn ->
                    CompositeCell.createTerm unit
                | CompositeCell.Term term, column when this.GetHeader(column).IsTermColumn ->
                    CompositeCell.createTerm term
                | CompositeCell.FreeText value, column when this.GetHeader(column).IsTermColumn ->
                    CompositeCell.createTerm (OntologyAnnotation.create value)
                | CompositeCell.Data _, DataMapIndices.Data -> source
                | _ -> target.UpdateMainField(source.ToString())

            this.SetCell(columnIndex, rowIndex, cell)

        member this.PasteStructuredCells
            (startCoordinate: CellCoordinate, selection: CellCoordinate[], source: CompositeCell[][])
            =
            let mapped = Mapping.map source startCoordinate selection

            let requiredRowCount =
                mapped
                |> Array.map (_.Target.y)
                |> Array.filter (fun row -> row > 0)
                |> Array.append [| this.RowCount |]
                |> Array.max

            if requiredRowCount > this.RowCount then
                this.DataContexts.AddRange(Array.init (requiredRowCount - this.RowCount) (fun _ -> DataContext()))

            mapped
            |> Array.filter (fun mapped ->
                mapped.Target.x > 0
                && mapped.Target.x <= this.ColumnCount
                && mapped.Target.y > 0
            )
            |> Array.iter (fun mapped -> this.ApplyClipboardCell(mapped.Target, mapped.Source))

        member this.PasteTabText(startCoordinate: CellCoordinate, clipboardText: string) =
            let rows =
                clipboardText.TrimEnd([| '\r'; '\n' |]).Split(LineBreaks, System.StringSplitOptions.None)
                |> Array.map (fun row -> row.Split([| '\t' |], System.StringSplitOptions.None))

            let requiredRowCount = startCoordinate.y - 1 + rows.Length

            if requiredRowCount > this.RowCount then
                this.DataContexts.AddRange(Array.init (requiredRowCount - this.RowCount) (fun _ -> DataContext()))

            rows
            |> Array.iteri (fun rowOffset row ->
                let startColumnIndex = startCoordinate.x - 1
                let rowIndex = startCoordinate.y - 1 + rowOffset

                row
                |> Array.iteri (fun columnOffset value ->
                    let columnIndex = startColumnIndex + columnOffset

                    if columnIndex < this.ColumnCount then
                        this.GetCell(columnIndex, rowIndex).UpdateMainField(value)
                        |> fun cell -> this.SetCell(columnIndex, rowIndex, cell)
                )
            )
