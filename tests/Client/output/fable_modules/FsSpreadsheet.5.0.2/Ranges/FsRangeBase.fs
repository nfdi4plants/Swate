namespace FsSpreadsheet


[<AbstractClass>][<AllowNullLiteral>]
type FsRangeBase (rangeAddress : FsRangeAddress) = 
    //: XLStylizedBase, IXLRangeBase, IXLStylized


    let mutable _sortRows    = null
    let mutable _sortColumns = null
    let mutable _rangeAddress = rangeAddress


    static let mutable IdCounter = 0;
    let _id = 
        IdCounter <- IdCounter + 1
        IdCounter

    //abstract member OnRangeAddressChanged : FsRangeAddress*FsRangeAddress -> unit 
    
    //default this.OnRangeAddressChanged (oldAddress, newAddress) =
    
    //    Worksheet.RellocateRange(RangeType, oldAddress, newAddress);

    member this.Extend(address : FsAddress) = _rangeAddress.Extend(address)

    member this.RangeAddress
        with get () = _rangeAddress
        and set (rangeAdress) =
            if rangeAdress <> _rangeAddress then
                let oldAddress = _rangeAddress
                _rangeAddress <- rangeAdress
                //OnRangeAddressChanged(oldAddress, _rangeAddress);

    // TO DO: add description – important and complex function
    member this.Cell(cellAddressInRange : FsAddress, cells : FsCellsCollection) = 

        let absRow = cellAddressInRange.RowNumber + this.RangeAddress.FirstAddress.RowNumber - 1;
        let absColumn = cellAddressInRange.ColumnNumber + this.RangeAddress.FirstAddress.ColumnNumber - 1;

        if (absRow <= 0 || absRow > 1048576) then
            failwithf "Row number must be between 1 and %i" cells.MaxRowNumber

        if (absColumn <= 0 || absColumn > 16384) then
            failwithf "Column number must be between 1 and %i" cells.MaxColumnNumber

        let cell = cells.TryGetCell(absRow, absColumn)
        
        match cell with
        | Some cell -> 
            cell
        | None -> 

            //var styleValue = this.StyleValue;

            //if (styleValue == Worksheet.StyleValue)
            //{
            //    if (Worksheet.Internals.RowsCollection.TryGetValue(absRow, out XLRow row)
            //        && row.StyleValue != Worksheet.StyleValue)
            //        styleValue = row.StyleValue;
            //    else if (Worksheet.Internals.ColumnsCollection.TryGetValue(absColumn, out XLColumn column)
            //        && column.StyleValue != Worksheet.StyleValue)
            //        styleValue = column.StyleValue;
            //}
            let absoluteAddress = new FsAddress(absRow, absColumn, cellAddressInRange.FixedRow, cellAddressInRange.FixedColumn);

            // If the default style for this range base is empty, but the worksheet
            // has a default style, use the worksheet's default style
            let newCell = FsCell.createEmptyWithAdress absoluteAddress

            this.Extend(absoluteAddress)

            cells.Add(absRow, absColumn, newCell) |> ignore
            newCell

    /// <summary>
    /// Returns the FsCells of this FsRangeBase with the given FsCellsCollection.
    /// </summary>
    /// <param name="cells">The FsCellsCollection where the FsCells are retrieved from.</param>
    member this.Cells(cells : FsCellsCollection) = 
        cells.GetCells(this.RangeAddress.FirstAddress, this.RangeAddress.LastAddress)

    /// <summary>
    /// The number of columns in the FsRangeBase.
    /// </summary>
     member this.ColumnCount() =
        _rangeAddress.LastAddress.ColumnNumber - _rangeAddress.FirstAddress.ColumnNumber + 1

    /// <summary>
    /// The number of rows in the FsRangeBase.
    /// </summary>
    member this.RowCount() =
        _rangeAddress.LastAddress.RowNumber - _rangeAddress.FirstAddress.RowNumber + 1