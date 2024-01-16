namespace FsSpreadsheet

[<AllowNullLiteral>]
type FsRangeColumn(rangeAddress) =

    inherit FsRangeBase(rangeAddress)
    
    //new () = 
    //    let range = FsRangeAddress(FsAddress(0,0),FsAddress(0,0))
    //    FsRangeColumn(range)
    
    new(index) = FsRangeColumn(FsRangeAddress(FsAddress(0, index), FsAddress(0, index)))

    //new(rangeAddress) = FsRangeColumn()

    member self.Index 
        with get() = self.RangeAddress.FirstAddress.ColumnNumber
        and set(i) = 
            self.RangeAddress.FirstAddress.ColumnNumber <- i
            self.RangeAddress.LastAddress.ColumnNumber <- i

    member self.Cell(rowIndex, cellsCollection) = base.Cell(FsAddress(rowIndex - base.RangeAddress.FirstAddress.RowNumber + 1,1), cellsCollection)
    
    member self.FirstCell(cells : FsCellsCollection) = 
        //let firstAddrRow, firstAddrCol = base.RangeAddress.FirstAddress |> fun fa -> fa.RowNumber, fa.ColumnNumber
        //base.Cell(FsAddress(firstAddrRow, firstAddrCol), cells)
        base.Cell(FsAddress(1, 1), cells)

    member self.Cells(cellsCollection) = base.Cells(cellsCollection)

    static member fromRangeAddress (rangeAddress : FsRangeAddress) = 
        FsRangeColumn rangeAddress

    /// <summary>
    /// Creates a deep copy of this FsRangeColumn.
    /// </summary>
    member self.Copy() =
        FsRangeColumn(self.RangeAddress.Copy())

    /// <summary>
    /// Returns a deep copy of a given FsRangeColumn.
    /// </summary>
    static member copy (rangeColumn : FsRangeColumn) =
        rangeColumn.Copy()


[<AutoOpen>]
module Enhancements =
    type FsRangeAddress with

        /// <summary>
        /// Takes an FsRangeAddress and returns, for every column the range address spans, an FsRangeColumn.
        /// </summary>
        static member toRangeColumns (rangeAddress : FsRangeAddress) =
            let columns = [rangeAddress.FirstAddress.ColumnNumber .. rangeAddress.LastAddress.ColumnNumber]
            let fstRow = rangeAddress.FirstAddress.RowNumber
            let lstRow = rangeAddress.LastAddress.RowNumber
            columns
            |> Seq.map (
                fun c -> 
                    let fa = FsAddress(fstRow, c)
                    let la = FsAddress(lstRow, c)
                    FsRangeAddress(fa, la)
                    |> FsRangeColumn
            )