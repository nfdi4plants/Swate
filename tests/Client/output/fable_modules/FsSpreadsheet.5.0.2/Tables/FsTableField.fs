namespace FsSpreadsheet

/// <summary>
/// Creates an FsTableFiled with given name, index, FsRangeColumn, totalRowLabel, and the totalsRowFunction.
/// </summary>
[<AllowNullLiteral>]
type FsTableField (name : string, index : int, column : FsRangeColumn, totalsRowLabel, totalsRowFunction) = 

    let mutable _totalsRowsFunction = totalsRowFunction
    let mutable _totalsRowLabel = totalsRowLabel
    let mutable _column = column
    let mutable _index = index
    let mutable _name = name

    /// <summary>
    /// Creates an empty FsTableField.
    /// </summary>
    new() = FsTableField("", 0, null, null, null)

    /// <summary>
    /// Creates an FsTableField with the given name.
    /// </summary>
    new(name : string) = FsTableField(name, 0, null, null, null)

    /// <summary>
    /// Creates an FsTableField with the given name and index.
    /// </summary>
    new(name : string, index : int) = FsTableField(name, index, null, null, null)

    /// <summary>
    /// Creates an FsTableField with the given name, index, and FsRangeColumn.
    /// </summary>
    new(name, index, column) = FsTableField(name, index, column, null, null)

    /// <summary>
    /// Gets or sets the FsRangeColumn of this FsTableField.
    /// </summary>
    member val Column = _column with get, set
        //with get() = 
        //    //let column =
        //    //    if _column = null then

        //    //    else               
        //            _column
        //    //column
        //and set(column) = _column <- column

    /// <summary>
    /// Gets or sets the 0-based index of the FsTableField inside the associated FsTable. 
    /// Sets the associated FsRangeColumn's column index accordingly.
    /// </summary>
    member this.Index 
        with get() = _index
        and set index = 
            if index = _index then ()
            else 
                _index <- index
                match _column with
                | null -> ()
                | _ -> 
                    let indDiff = index - _index
                    let newCol =
                        let raFstRow = this.Column.RangeAddress.FirstAddress.RowNumber
                        let newFstAddr = FsAddress(raFstRow, _index + indDiff)
                        let raLstRow = this.Column.RangeAddress.LastAddress.RowNumber
                        let newLstAddr = FsAddress(raLstRow, _index + indDiff)
                        FsRangeAddress(newFstAddr, newLstAddr)
                        |> FsRangeColumn
                    this.Column <- newCol

    /// <summary>
    /// The name of this FsTableField.
    /// </summary>
    member this.Name = _name

    /// <summary>
    /// Sets the name of the FsTableField. If `showHeaderRow` is true, takes the respective FsCellsCollection and renames the header cell 
    /// according to the name of the FsTableField.
    /// </summary>
    // TO DO: ask HLW: why this and not normal setter? If inferring from cellsColl, wouldn't it be better to name the method more precisely?
    member this.SetName(name, cellsCollection : FsCellsCollection, showHeaderRow : bool) =
        _name <- name
        if showHeaderRow then
            this.Column.FirstCell(cellsCollection).SetValueAs<string>(name)

    /// <summary>
    /// Sets the name of a given FsTableField. If `showHeaderRow` is true, takes the respective FsCellsCollection and renames the header cell 
    /// according to the name of the FsTableField.
    /// </summary>
    static member setName name cellsCollection showHeaderRow (tableField : FsTableField) =
        tableField.SetName(name, cellsCollection, showHeaderRow)
        tableField

    /// <summary>
    /// Creates a deep copy of this FsTableField.
    /// </summary>
    member this.Copy() =
        let col = this.Column.Copy()
        let ind = this.Index
        let nam = this.Name
        FsTableField(nam, ind, col, null, null)

    /// <summary>
    /// Returns a deep copy of a given FsTableField.
    /// </summary>
    static member copy (tableField : FsTableField) =
        tableField.Copy()

    /// <summary>
    /// Returns the header cell (taken from a given FsCellsCollection) for the FsTableField if `showHeaderRow` is true. Else fails.
    /// </summary>
    /// <exception cref="System.Exception">if `showHeaderRow` is false.</exception>
    member this.HeaderCell (cellsCollection : FsCellsCollection, showHeaderRow : bool) =
        if not showHeaderRow then 
            failwithf "tried to get header cell of table field \"%s\" even though showHeaderRow is set to zero" _name
        else
            this.Column.FirstCell(cellsCollection)

    /// <summary>
    /// Returns the header cell (taken from an FsCellsCollection) for a given FsTableField if `showHeaderRow` is true. Else fails.
    /// </summary>
    /// <exception cref="System.Exception">if `showHeaderRow` is false.</exception>
    static member getHeaderCell cellsCollection showHeaderRow (tableField : FsTableField) =
        tableField.HeaderCell(cellsCollection, showHeaderRow)

    /// <summary>
    /// Gets the collection of data cells for this FsTableField. Excludes the header and footer cells.
    /// </summary>
    /// <param name="cellsCollection">The FsCellsCollection respective to the FsTableField where the data cells are taken from.</param>
    member this.DataCells (cellsCollection : FsCellsCollection) =        
        this.Column.Cells(cellsCollection)
        |> Seq.skip 1 // ClosedXML implementation never shows header cell

    /// <summary>
    /// Gets the collection of data cells for a given FsTableField. Excludes the header and footer cells.
    /// </summary>
    /// <param name="cellsCollection">The FsCellsCollection respective to the FsTableField where the data cells are taken from.</param>
    /// <param name="tableField">The FsTableField to get the data cells from.</param>
    static member getDataCells cellsCollection (tableField : FsTableField) =
        tableField.DataCells(cellsCollection)