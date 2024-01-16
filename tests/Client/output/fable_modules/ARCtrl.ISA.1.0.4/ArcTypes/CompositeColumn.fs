namespace ARCtrl.ISA

open Fable.Core

[<AttachMembers>]
type CompositeColumn = {
    Header: CompositeHeader
    Cells: CompositeCell []
} with
    static member create (header: CompositeHeader, ?cells: CompositeCell []) = 
        let cells = Option.defaultValue [||] cells
        {
            Header = header
            Cells = cells
        }

    /// Returns true if header and cells are a valid combination. E.g. Term header with term or unitized cells. IO header with freetext cells.
    ///
    /// ?raiseExeption: Default false. Set true if this function should raise an exception instead of return false.
    // TODO! Do not only check cells.Head
    member this.Validate(?raiseException: bool) =
        let raiseExeption = Option.defaultValue false raiseException
        let header = this.Header
        let cells = this.Cells
        match header, cells with
        // no cell values will be handled later and is no error case
        | _, emptyCell when cells.Length = 0 -> 
            true
        | isTerm when header.IsTermColumn && (cells.[0].isTerm || cells.[0].isUnitized) -> 
            true
        | isNotTerm when not header.IsTermColumn && cells.[0].isFreeText -> 
            true
        | h, c -> 
            if raiseExeption then 
                let exampleCells = c.[0]
                let msg = $"Invalid combination of header `{h}` and cells `{exampleCells}`"
                failwith msg
            // Maybe still return `msg` somehow if `raiseExeption` is false?
            false

    /// <summary>
    /// Returns an array of all units found in the cells of this column. Returns None if no units are found.
    /// </summary>
    member this.TryGetColumnUnits() =
        let arr = [|
            for cell in this.Cells do
                if cell.isUnitized then
                    let _, unit = cell.AsUnitized
                    unit
        |]
        if Array.isEmpty arr then None else Some arr

    /// <summary>
    /// Simple predictor for empty default cells.
    ///
    /// Currently uses majority vote for the column to decide cell type.
    /// </summary>
    member this.GetDefaultEmptyCell() =
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