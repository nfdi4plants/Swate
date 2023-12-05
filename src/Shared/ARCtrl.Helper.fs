namespace Shared

open ARCtrl.ISA

/// This module contains helper functions which might be useful for ARCtrl
[<AutoOpen>]
module ARCtrlHelper =

    type ArcFiles =
    | Investigation of ArcInvestigation
    | Study         of ArcStudy * ArcAssay list
    | Assay         of ArcAssay

    with
        member this.Tables() : ArcTables =
            match this with
            | Investigation _ -> ArcTables(ResizeArray[])
            | Study (s,_) -> s
            | Assay a -> a

[<AutoOpen>]
module Extensions =

    type CompositeCell with
        member this.UpdateWithOA(oa:OntologyAnnotation) =
            match this with
            | CompositeCell.Term _ -> CompositeCell.createTerm oa
            | CompositeCell.Unitized (v,_) -> CompositeCell.createUnitized (v,oa)
            | CompositeCell.FreeText _ -> CompositeCell.createFreeText oa.NameText

        member this.ToEmptyCell() =
            match this with
            | CompositeCell.Term _ -> CompositeCell.emptyTerm
            | CompositeCell.Unitized (v,_) -> CompositeCell.emptyUnitized
            | CompositeCell.FreeText _ -> CompositeCell.emptyFreeText

    type CompositeColumn with

        member this.PredictNewColumnCell() =
            if not this.Header.IsTermColumn then
                CompositeCell.emptyFreeText
            else
                let unitCellCount, termCellCount =
                    this.Cells
                    |> Seq.fold (fun (units,terms) cell ->
                        if cell.isUnitized then (units+1,terms) else (units,terms+1)
                    ) (0,0)
                if termCellCount >= unitCellCount then
                    CompositeCell.emptyTerm
                else
                    CompositeCell.emptyUnitized

    type ArcTable with
        member this.GetCellAt(columnIndex: int, rowIndex: int) =
            match this.TryGetCellAt(columnIndex,rowIndex) with
            | Some c -> c
            | None -> failwith $"Error. Unable to get cell at position '{columnIndex},{rowIndex}' in table '{this.Name}'."
        member this.MapColumns(mapping: CompositeColumn -> unit) =
            for columnIndex in 0 .. (this.ColumnCount-1) do
                let column = this.GetColumn columnIndex
                mapping column
        member this.MapiColumns(mapping: int -> CompositeColumn -> unit) =
            for columnIndex in 0 .. (this.ColumnCount-1) do
                let column = this.GetColumn columnIndex
                mapping columnIndex column
                    
    type ArcTables with
        member this.MoveTable(oldIndex: int, newIndex: int) =
            let table = this.Tables.[oldIndex]
            this.Tables.Insert(newIndex, table)
            let updatedOldIndex = if newIndex <= oldIndex then oldIndex + 1 else oldIndex
            this.Tables.RemoveAt(updatedOldIndex)
