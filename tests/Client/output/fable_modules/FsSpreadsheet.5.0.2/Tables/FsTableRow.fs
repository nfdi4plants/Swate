namespace FsSpreadsheet

[<AllowNullLiteral>]
type FsTableRow (rangeAddress : FsRangeAddress) = 

    inherit FsRangeRow(rangeAddress)