namespace FsSpreadsheet

open System.Collections.Generic

open Fable.Core

/// <summary>
/// Creates an FsTable from the given name and FsRangeAddres, with totals row shown and header row shown or not, accordingly.
/// </summary>
[<AttachMembers>]
type FsTable (name : string, rangeAddress : FsRangeAddress, ?showTotalsRow : bool, ?showHeaderRow : bool) = 

    inherit FsRangeBase(rangeAddress)

    let mutable _name = name.Trim().Replace(" ","_")

    let mutable _lastRangeAddress = rangeAddress
    let mutable _showTotalsRow : bool = Option.defaultValue false showTotalsRow
    let mutable _showHeaderRow : bool = Option.defaultValue true showHeaderRow

    let mutable _fieldNames : Dictionary<string,FsTableField> = Dictionary()
    let _uniqueNames : HashSet<string> = HashSet()


    /// <summary>
    /// The name of the FsTable.
    /// </summary>
    member this.Name 
        with get() = _name

    /// <summary>
    /// Returns all fieldnames as `fieldname*FsTableField` dictionary.
    /// </summary>
    member this.GetFieldNames (cellsCollection : FsCellsCollection) =
        if (_fieldNames <> null && _lastRangeAddress <> null && _lastRangeAddress.Equals(this.RangeAddress)) then 
            _fieldNames;
        else 
            _lastRangeAddress <- this.RangeAddress

            //this.RescanFieldNames(cellsCollection)
                
            _fieldNames;

    /// <summary>
    /// The FsTableFields of this FsTable.
    /// </summary>
    member this.GetFields (cellsCollection : FsCellsCollection) =
        let columnCount = base.ColumnCount()
        //let offset = base.RangeAddress.FirstAddress.ColumnNumber
        Seq.init columnCount (fun i -> this.GetFieldAt(i, cellsCollection))

    /// <summary>
    /// Gets or sets if the header row is shown.
    /// </summary>
    member this.ShowHeaderRow 
        with get () = _showHeaderRow
        and set(showHeaderRow) = _showHeaderRow <- showHeaderRow

    /// <summary>
    /// Returns the header row as FsRangeRow. Scans for new fieldnames.
    /// </summary>
    [<System.ObsoleteAttribute("Obsolete after `4.0.0`. Use TryGetHeaderRow or GetHeaderRow instead!")>]
    member this.HeadersRow() = 
        if (not this.ShowHeaderRow) then null;        
        else 
            FsRange(base.RangeAddress).FirstRow()

    member this.TryGetHeaderRow(cellsCollection) =
        match this.ShowHeaderRow with
        | false -> None
        | true -> 
            let rowIndex = this.RangeAddress.FirstAddress.RowNumber 
            let firstAddress = FsAddress(rowIndex, this.RangeAddress.FirstAddress.ColumnNumber)
            let lastAddress = FsAddress(rowIndex, this.RangeAddress.LastAddress.ColumnNumber)
            let range = FsRangeAddress (firstAddress, lastAddress)
            FsRow(range, cellsCollection) |> Some

    member this.GetHeaderRow(cellsCollection) =
        match this.TryGetHeaderRow(cellsCollection) with
        | Some hr -> hr
        | None -> failwith $"""Error. Unable to get header row for table "{this.Name}" as `ShowHeaderRow` is set to `false`."""

    /// <summary>
    /// Returns the FsColumns from the FsTable.
    /// </summary>
    /// <param name="cellsCollection">The FsCellsCollection associated with this FsTable.</param>
    member this.GetColumns(cellsCollection : FsCellsCollection) = 
        seq {
            for i = this.RangeAddress.FirstAddress.ColumnNumber to this.RangeAddress.LastAddress.ColumnNumber do 
                let firstAddress = FsAddress(this.RangeAddress.FirstAddress.RowNumber, i)
                let lastAddress = FsAddress(this.RangeAddress.LastAddress.RowNumber, i)
                let range = FsRangeAddress (firstAddress, lastAddress)
                FsColumn(range, cellsCollection)
        }

    /// <summary>
    /// Returns the FsRows from the FsTable.
    /// </summary>
    /// <param name="cellsCollection">The FsCellsCollection associated with this FsTable.</param>
    member this.GetRows(cellsCollection : FsCellsCollection) =
        seq {
            for i = this.RangeAddress.FirstAddress.RowNumber to this.RangeAddress.LastAddress.RowNumber do 
                let firstAddress = FsAddress(i, this.RangeAddress.FirstAddress.ColumnNumber)
                let lastAddress = FsAddress(i, this.RangeAddress.LastAddress.ColumnNumber)
                let range = FsRangeAddress (firstAddress, lastAddress)
                FsRow(range, cellsCollection)
        }

    /// <summary>
    /// Updates the FsRangeAddress of the FsTable according to the FsTableFields associated.
    /// </summary>
    member this.RescanRange() =
        let rangeAddress = 
            _fieldNames.Values
            |> Seq.map (fun v -> v.Column.RangeAddress)
            |> Seq.reduce (fun r1 r2 -> r1.Union(r2))
        base.RangeAddress <- rangeAddress

    /// <summary>
    /// Updates the FsRangeAddress of a given FsTable according to the FsTableFields associated.
    /// </summary>
    static member rescanRange (table : FsTable) =
        table.RescanRange()
        table

    /// <summary>
    /// Returns a unique name consisting of the original name and an initial offset that is raised 
    /// if the original name with that offset is already present.
    /// </summary>
    /// <param name="originalName">Header name that was tried to be used.</param>
    /// <param name="initialOffset">First number that together with the originalName, leads to a unique column header.</param>
    /// <param name="enforceOffset">If true, the initial offset is always applied.</param>
    member this.GetUniqueName(originalName : string, initialOffset : int32, enforceOffset : bool) =
        let mutable name = originalName + if enforceOffset then string initialOffset else ""
        if _uniqueNames.Contains(name) then
        
            let mutable i = initialOffset
            name <- originalName + string i
            while _uniqueNames.Contains(name) do

                i <- i + 1
                name <- originalName + string i

        _uniqueNames.Add name |> ignore
        name

    /// <summary>
    /// Returns a unique name consisting of the original name and an initial offset that is raised 
    /// if the original name with that offset is already present.
    /// </summary>
    /// <param name="originalName">Header name that was tried to be used.</param>
    /// <param name="initialOffset">First number that together with the originalName, leads to a unique column header.</param>
    /// <param name="enforceOffset">If true, the initial offset is always applied.</param>
    /// <param name="table">The FsTable on which this function is called.</param>
    static member getUniqueNames originalName initialOffset enforceOffset (table : FsTable) =
        table.GetUniqueName(originalName, initialOffset, enforceOffset)

    /// <summary>
    /// Creates and adds FsTableFields from a sequence of field names to the FsTable.
    /// </summary>
    member this.InitFields(fieldNames : seq<string>) =
    //    _fieldNames = new Dictionary<String, IXLTableField>();    // let's _not_ do it this way.
        //let rangeCols = FsRangeAddress.toRangeColumns base.RangeAddress
        fieldNames
        |> Seq.iteri (
            fun i fn ->
                let tableField = FsTableField(fn, i, FsRangeColumn i)
                _fieldNames.Add(fn, tableField)
        )

    /// <summary>
    /// Creates and adds FsTableFields from a sequence of field names to a given FsTable.
    /// </summary>
    static member initFields (fieldNames : seq<string>) (table : FsTable) =
        table.InitFields fieldNames
        table

    /// <summary>
    /// Adds a sequence of FsTableFields to the FsTable.
    /// </summary>
    member this.AddFields(tableFields : seq<FsTableField>) =
        tableFields
        |> Seq.iter (
            fun tf -> _fieldNames.Add(tf.Name, tf)
        )

    /// <summary>
    /// Adds a sequence of FsTableFields to a given FsTable.
    /// </summary>
    static member addFields (tableFields : seq<FsTableField>) (table : FsTable) =
        table.AddFields tableFields
        table

    /// <summary>
    /// Returns the FsTableField with given name. If an FsTableField does not exist under this name in the FsTable, adds it.
    /// </summary>
    member this.Field(name : string, cellsCollection : FsCellsCollection) = 
        match Dictionary.tryGet name _fieldNames with
        | Some field -> 
            field
        | None -> 
            let maxIndex = 
                _fieldNames.Values 
                |> Seq.map (fun v -> v.Index) 
                |> fun s -> 
                    if Seq.length s = 0 then 0 else Seq.max s
            let range = 
                let offset = _fieldNames.Count
                let firstAddress = FsAddress(this.RangeAddress.FirstAddress.RowNumber,this.RangeAddress.FirstAddress.ColumnNumber + offset)
                let lastAddress = FsAddress(this.RangeAddress.LastAddress.RowNumber,this.RangeAddress.FirstAddress.ColumnNumber + offset)
                FsRangeAddress(firstAddress,lastAddress)
            let column = FsRangeColumn(range)
            let newField = FsTableField(name,maxIndex + 1,column,null,null)
            if this.ShowHeaderRow then
                newField.HeaderCell(cellsCollection,true).SetValueAs name |> ignore
            _fieldNames.Add(name,newField)
            this.RescanRange()
            newField

    /// <summary>
    /// Takes a name of an FsTableField and an FsCellsCollection (belonging to the FsWorksheet of this 
    /// FsTable) and returns the respective FsTableField.
    /// </summary>
    /// <exception cref="System.ArgumentException">if the header row has no field with the given name.</exception>
    member this.GetField(name : string, cellsCollection : FsCellsCollection) =
        let name = name.Replace("\r\n", "\n")
        try this.GetFieldNames(cellsCollection).Item name
        with _ -> failwith <| "The header row doesn't contain field name '" + name + "'."

    /// <summary>
    /// Takes a name of an FsTableField and an FsCellsCollection (belonging to the FsWorksheet of this 
    /// FsTable) and returns the respective FsTableField.
    /// </summary>
    /// <exception cref="System.ArgumentException">if the header row has no field with the given name.</exception>
    static member getField (name : string) (cellsCollection : FsCellsCollection) (table : FsTable) =
        table.GetField(name, cellsCollection)

    /// <summary>
    /// Takes the index of an FsTableField and an FsCellsCollection (belonging to the FsWorksheet of
    /// this FsTable) and returns the respective FsTableField.
    /// </summary>
    /// <exception cref="System.ArgumentException">if the FsTable has no FsTableField with the given index.</exception>
    member this.GetFieldAt(index, cellsCollection) =
        try 
            this.GetFieldNames(cellsCollection).Values
            |> Seq.find (fun ftf -> ftf.Index = index)
        with _ -> failwith $"FsTableField with index {index} does not exist in the FsTable."

    /// <summary>
    /// Takes a name of an FsTableField and an FsCellsCollection (belonging to the FsWorksheet of 
    /// this FsTable) and returns the index of the respective FsTableField.
    /// </summary>
    /// <exception cref="System.ArgumentException">if the header row has no field with the given name.</exception>
    member this.GetFieldIndex(name : string, cellsCollection) =
        this.GetField(name, cellsCollection).Index

    /// <summary>
    /// Renames a fieldname of the FsTable if it exists. Else fails.
    /// </summary>
    /// <exception cref="System.ArgumentException">if the FsTableField does not exist in the FsTable.</exception>
    member this.RenameField(oldName : string, newName : string) = 
        match Dictionary.tryGet oldName _fieldNames with
        | Some field -> 
            _fieldNames.Remove(oldName) |> ignore
            _fieldNames.Add(newName, field)
        | None -> 
            raise (System.ArgumentException("The FsTabelField does not exist in this FsTable", "oldName"))

    /// <summary>
    /// Renames a fieldname of the FsTable if it exists. Else fails.
    /// </summary>
    /// <exception cref="System.ArgumentException">if the FsTableField does not exist in the FsTable.</exception>
    static member renameField oldName newName (table : FsTable) =
        table.RenameField(oldName, newName)
        table

    /// <summary>
    /// Returns the header cell from a given FsCellsCollection with the given colum index if the cell exists. Else returns None.
    /// </summary>
    member this.TryGetHeaderCellOfColumnAt(cellsCollection : FsCellsCollection, colIndex : int) =
        let fstRowIndex = this.RangeAddress.FirstAddress.RowNumber
        cellsCollection.GetCellsInColumn colIndex
        |> Seq.tryFind (fun c -> c.RowNumber = fstRowIndex)

    /// <summary>
    /// Returns the header cell from a given FsCellsCollection with the given column index in a given FsTable if the cell exists. Else
    /// returns None.
    /// </summary>
    static member tryGetHeaderCellOfColumnIndexAt cellsCollection (colIndex : int) (table : FsTable) =
        table.TryGetHeaderCellOfColumnAt(cellsCollection, colIndex)

    /// <summary>
    /// Returns the header cell of a given FsRangeColumn from a given FsCellsCollection if the cell exists. Else returns None.
    /// </summary>
    member this.TryGetHeaderCellOfColumn(cellsCollection : FsCellsCollection, column : FsRangeColumn) =
        this.TryGetHeaderCellOfColumnAt(cellsCollection, column.Index)

    /// <summary>
    /// Returns the header cell of a given FsRangeColumn from a given FsCellsCollection in a given FsTable if the cell exists.
    /// Else returns None.
    /// </summary>
    static member tryGetHeaderCellOfColumn cellsCollection (column : FsRangeColumn) (table : FsTable) =
        table.TryGetHeaderCellOfColumn(cellsCollection, column)

    /// <summary>
    /// Returns the header cell from a given FsCellsCollection with the given colum index.
    /// </summary>
    /// <exception cref="System.NullReferenceException">if the FsCell cannot be found.</exception>
    member this.GetHeaderCellOfColumnAt(cellsCollection, colIndex : int) =
        this.TryGetHeaderCellOfColumnAt(cellsCollection, colIndex).Value

    /// <summary>
    /// Returns the header cell from a given FsCellsCollection with the given colum index in a given FsTable.
    /// </summary>
    /// <exception cref="System.NullReferenceException">if the FsCell cannot be found.</exception>
    static member getHeaderCellOfColumnIndexAt cellsCollection (colIndex : int) (table : FsTable) =
        table.GetHeaderCellOfColumnAt(cellsCollection, colIndex)

    /// <summary>
    /// Returns the header cell of a given FsRangeColumn from a given FsCellsCollection.
    /// </summary>
    /// <exception cref="System.NullReferenceException">if the FsCell cannot be found.</exception>
    member this.GetHeaderCellOfColumn(cellsCollection : FsCellsCollection, column : FsRangeColumn) =
        this.TryGetHeaderCellOfColumn(cellsCollection, column).Value

    /// <summary>
    /// Returns the header cell of a given FsRangeColumn from a given FsCellsCollection in a given FsTable.
    /// </summary>
    /// <exception cref="System.NullReferenceException">if the FsCell cannot be found.</exception>
    static member getHeaderCellOfColumn cellsCollection (column : FsRangeColumn) (table : FsTable) =
        table.GetHeaderCellOfColumn(cellsCollection, column)

    /// <summary>
    /// Returns the header cell of a given FsTableField from a given FsCellsCollection.
    /// </summary>
    member this.GetHeaderCellOfTableField(cellsCollection, tableField : FsTableField) =
        tableField.HeaderCell(cellsCollection, this.ShowHeaderRow)

    /// <summary>
    /// Returns the header cell of a given FsTableField from a given FsCellsCollection in a given FsTable.
    /// </summary>
    static member getHeaderCellOfTableField cellsCollection (tableField : FsTableField) (table : FsTable) =
        table.GetHeaderCellOfTableField(cellsCollection, tableField)

    /// <summary>
    /// Returns the header cell from an FsTableField with the given index using a given FsCellsCollection if the cell exists.
    /// Else returns None.
    /// </summary>
    member this.TryGetHeaderCellOfTableFieldAt(cellsCollection, tableFieldIndex : int) =
        _fieldNames.Values
        |> Seq.tryPick (
            fun tf -> 
                if tf.Index = tableFieldIndex then
                    Some (tf.HeaderCell(cellsCollection, this.ShowHeaderRow))
                else None
        )

    /// <summary>
    /// Returns the header cell from an FsTableField with the given index using a given FsCellsCollection if the cell exists
    /// in a given FsTable. Else returns None.
    /// </summary>
    static member tryGetHeaderCellOfTableFieldIndexAt cellsCollection (tableFieldIndex : int) (table : FsTable) =
        table.TryGetHeaderCellOfTableFieldAt(cellsCollection, tableFieldIndex)

    /// <summary>
    /// Returns the header cell from an FsTableField with the given index using a given FsCellsCollection.
    /// </summary>
    /// <exception cref="System.NullReferenceException">if the FsCell cannot be found.</exception>
    member this.GetHeaderCellOfTableFieldAt(cellsCollection, tableFieldIndex : int) =
        this.TryGetHeaderCellOfTableFieldAt(cellsCollection, tableFieldIndex).Value

    /// <summary>
    /// Returns the header cell from an FsTableField with the given index using a given FsCellsCollection in a given FsTable.
    /// </summary>
    /// <exception cref="System.NullReferenceException">if the FsCell cannot be found.</exception>
    static member getHeaderCellOfTableFieldIndexAt cellsCollection (tableFieldIndex : int) (table : FsTable) =
        table.GetHeaderCellOfTableFieldAt(cellsCollection, tableFieldIndex)

    /// <summary>
    /// Returns the header cell from an FsTableField with the given name using an FsCellsCollection in the FsTable if the cell exists.
    /// Else returns None.
    /// </summary>
    member this.TryGetHeaderCellByFieldName(cellsCollection, fieldName : string) =
        match Dictionary.tryGet fieldName _fieldNames with
        | Some tf -> Some (tf.HeaderCell(cellsCollection, this.ShowHeaderRow))
        | None -> None

    /// <summary>
    /// Returns the header cell from an FsTableField with the given name using an FsCellsCollection in a given FsTable if the cell exists.
    /// Else returns None.
    /// </summary>
    static member tryGetHeaderCellByFieldName cellsCollection (fieldName : string) (table : FsTable) =
        table.TryGetHeaderCellByFieldName(cellsCollection, fieldName)

    /// <summary>
    /// Returns the data cells from a given FsCellsCollection with the given colum index.
    /// </summary>
    /// <remarks>Column index must fit the FsCellsCollection, not the FsTable!</remarks>
    member this.GetDataCellsOfColumnAt(cellsCollection : FsCellsCollection, colIndex) =
        let fstRowIndex = this.RangeAddress.FirstAddress.RowNumber
        let lstRowIndex = this.RangeAddress.LastAddress.RowNumber
        [fstRowIndex + 1 .. lstRowIndex]
        |> Seq.choose (
            fun ri -> cellsCollection.TryGetCell(ri, colIndex)
        )

    /// <summary>
    /// Returns the data cells from a given FsCellsCollection with the given colum index in a given FsTable.
    /// </summary>
    /// <remarks>Column index must fit the FsCellsCollection, not the FsTable!</remarks>
    static member getDataCellsOfColumnIndexAt cellsCollection (colIndex : int) (table : FsTable) =
        table.GetDataCellsOfColumnAt(cellsCollection, colIndex)

    // TO DO: add equivalents of the other methods regarding header cell for data cells.

    /// <summary>
    /// Creates a deep copy of this FsTable.
    /// </summary>
    member this.Copy() =
        let ra = this.RangeAddress.Copy()
        let nam = this.Name
        let shr = this.ShowHeaderRow
        FsTable(nam, ra, false, shr)

    /// <summary>
    /// Returns a deep copy of a given FsTable.
    /// </summary>
    static member copy (table : FsTable) =
        table.Copy()


    /// Updates the TableFields according to the range of the table and the underlying cellcollection.
    ///
    /// For this, maps over the range of the table and sets the header of the table fields to the value of the cell. If no cell value is set, the header value and the underlying cell value are set to a default value.
    member this.RescanFieldNames(cellsCollection : FsCellsCollection) =
        if this.ShowHeaderRow then
            let oldFieldNames =  _fieldNames
            _fieldNames <- new Dictionary<string, FsTableField>()
            let headersRow = this.GetHeaderRow(cellsCollection);
            let mutable cellPos = 0
            for cell in headersRow do
                let mutable name = cell.ValueAsString() //GetString();
                match Dictionary.tryGet name oldFieldNames with
                | Some tableField ->
                    tableField.Index <- cellPos
                    _fieldNames.Add(name,tableField)
                    cellPos <- cellPos + 1
                | None -> 

                    // Be careful here. Fields names may actually be whitespace, but not empty
                    if (name = null) <> (name = "") then    // TO DO: ask: shouldn't this be XOR?

                        name <- this.GetUniqueName("Column", cellPos + 1, true)
                        cell.SetValueAs(name) |> ignore
                        cell.DataType <- DataType.String

                    if (_fieldNames.ContainsKey(name)) then
                        raise (System.ArgumentException("The header row contains more than one field name '" + name + "'."))

                    _fieldNames.Add(name, new FsTableField(name, cellPos))
                    cellPos <- cellPos + 1
        else

            let colCount = base.ColumnCount();
            for i = 1 to colCount do

                if _fieldNames.Values |> Seq.exists (fun v -> v.Index = i - 1) |> not then 

                    let name = "Column" + string i;

                    _fieldNames.Add(name, new FsTableField(name, i - 1));