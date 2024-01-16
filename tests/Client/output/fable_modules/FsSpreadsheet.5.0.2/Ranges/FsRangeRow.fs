namespace FsSpreadsheet

[<AllowNullLiteral>]
type FsRangeRow(rangeAddress) =

    inherit FsRangeBase(rangeAddress)
    
    //new () = 
    //    let range = FsRangeAddress(FsAddress(0,0),FsAddress(0,0))
    //    FsRangeColumn(range)
    
    new (index) = FsRangeRow (FsRangeAddress(FsAddress(index,0),FsAddress(index,0)))

    member self.Index 
        with get() = self.RangeAddress.FirstAddress.RowNumber
        and set(i) = 
            self.RangeAddress.FirstAddress.RowNumber <- i
            self.RangeAddress.LastAddress.RowNumber <- i
   
    member self.Cell(columnIndex,cells) = base.Cell(FsAddress(1,columnIndex - base.RangeAddress.FirstAddress.ColumnNumber + 1),cells)

    member self.Cells(cells) = base.Cells(cells)