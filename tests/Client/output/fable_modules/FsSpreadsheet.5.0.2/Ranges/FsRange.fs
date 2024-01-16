namespace FsSpreadsheet


type FsRange(rangeAddress : FsRangeAddress, styleValue) = 

    inherit FsRangeBase(rangeAddress)

    new (rangeAddress : FsRangeAddress) = FsRange(rangeAddress, null)
    new (rangeBase : FsRangeBase) = FsRange(rangeBase.RangeAddress, null)

    member self.Row(row : int32) = 
        if (row <= 0 || row + base.RangeAddress.FirstAddress.RowNumber - 1 > 1048576 ) then
            raise (new System.ArgumentOutOfRangeException(string row,sprintf "Row number must be between 1 and %i" 1048576))
        let firstCellAddress = FsAddress(base.RangeAddress.FirstAddress.RowNumber + row - 1,base.RangeAddress.FirstAddress.ColumnNumber)
        let lastCellAddress = FsAddress(base.RangeAddress.FirstAddress.RowNumber + row - 1,base.RangeAddress.LastAddress.ColumnNumber)       
        FsRangeRow(FsRangeAddress(firstCellAddress, lastCellAddress))

    member self.FirstRow() = 
        self.Row(1)