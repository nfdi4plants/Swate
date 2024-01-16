namespace FsSpreadsheet

open Fable.Core

// Type based on the type XLWorksheet used in ClosedXml
/// <summary>
/// Creates an FsWorksheet with the given name, FsRows, FsTables, and FsCellsCollection.
/// </summary>
[<AttachMembers>]
type FsWorksheet (name, ?fsRows, ?fsTables, ?fsCellsCollection) =

    let mutable _name = name

    let _rows : ResizeArray<FsRow> = defaultArg fsRows <| ResizeArray()

    let _tables : ResizeArray<FsTable> = defaultArg fsTables <| ResizeArray()

    let mutable _cells : FsCellsCollection = fsCellsCollection  |> Option.defaultValue (FsCellsCollection())

    /// <summary>
    /// Creates an empty FsWorksheet with the given name.
    /// </summary>
    static member init (name) = 
        FsWorksheet(name, ResizeArray(), ResizeArray(), FsCellsCollection())

    // TO DO: finish
    //new (name, (fsCells : seq<FsCell>)) =
    //    let cellsCollection = FsCellsCollection()
    //    fsCells 
    //    |> Seq.iter (
    //        fun c -> cellsCollection.Add(c.Address.RowNumber, c.Address.ColumnNumber, c)
    //    )
    //    let rows = 
    //    FsWorksheet(name)


    // ----------
    // PROPERTIES
    // ----------

    /// <summary>
    /// The name of the FsWorksheet.
    /// </summary>
    member self.Name
        with get() = _name
        and set(name) = _name <- name

    /// <summary>
    /// The FsCellCollection of the FsWorksheet.
    /// </summary>
    member self.CellCollection
        with get () = _cells

    /// <summary>
    /// The FsTables of the FsWorksheet.
    /// </summary>
    member self.Tables 
        with get() = _tables
        
    /// <summary>
    /// The FsRows of the FsWorksheet.
    /// </summary>
    member self.Rows
        with get () = _rows

    /// <summary>
    /// The FsColumns of the FsWorksheet.
    /// </summary>
    member self.Columns
        with get () = 
            _cells.GetCells()
            |> Seq.groupBy (fun c -> c.ColumnNumber)
            |> Seq.map (fun (columnIndex,cells) -> 
                let newRange = 
                    cells
                    |> Seq.sortBy (fun c -> c.RowNumber)
                    |> fun cells ->
                        FsAddress((Seq.head cells |> fun c -> c.RowNumber), columnIndex),
                        FsAddress((Seq.last cells |> fun c -> c.RowNumber), columnIndex)
                    |> FsRangeAddress
                FsColumn(newRange,self.CellCollection)
            )

    // -------
    // METHODS
    // -------

    /// <summary>
    /// Returns a copy of the FsWorksheet.
    /// </summary>
    member self.Copy() =
        let fcc = self.CellCollection.Copy()
        let nam = self.Name
        let rws =
            let n = ResizeArray()
            self.Rows |> Seq.iter (fun r -> 
                n.Add <| r.Copy()
            )
            n
        let tbs = 
            let n = ResizeArray()
            self.Tables |> Seq.iter (fun t -> 
                n.Add <| t.Copy()
            )
            n
        FsWorksheet(nam, rws, tbs, fcc)
        //let newSheet = FsWorksheet(self.Name)
        //self.Tables 
        //|> List.iter (
        //    fun t -> 
        //        newSheet.Table(t.Name, t.RangeAddress, t.ShowHeaderRow) 
        //        |> ignore
        //)
        //for row in (self.Rows) do
        //    let (newRow : FsRow) = newSheet.Row(row.Index)
        //    for cell in row.Cells do
        //        let newCell = newRow.Cell(cell.Address,newSheet.CellCollection)
        //        newCell.SetValueAs(cell.Value)
        //        |> ignore
        //newSheet

    /// <summary>
    /// Returns a copy of a given FsWorksheet.
    /// </summary>
    static member copy (sheet : FsWorksheet) =
        sheet.Copy()

    // ------
    // Row(s)
    // ------

    /// <summary>
    /// Returns the FsRow at the given index. If it does not exist, it is created and appended first.
    /// </summary>
    member self.Row(rowIndex) = 
        match _rows |> Seq.tryFind (fun row -> row.Index = rowIndex) with
        | Some row ->
            row
        | None -> 
            let row = FsRow.createAt(rowIndex,self.CellCollection) 
            _rows.Add row
            row

    /// <summary>
    /// Returns the FsRow at the given FsRangeAddress. If it does not exist, it is created and appended first.
    /// </summary>
    member self.RowWithRange(rangeAddress : FsRangeAddress) = 
        if rangeAddress.FirstAddress.RowNumber <> rangeAddress.LastAddress.RowNumber then
            failwithf "Row may not have a range address spanning over different row indices"
        self.Row(rangeAddress.FirstAddress.RowNumber).RangeAddress <- rangeAddress

    /// <summary>
    /// Appends an FsRow to an FsWorksheet if the rowIndex is not already taken.
    /// </summary>
    static member appendRow (row : FsRow) (sheet : FsWorksheet) =
        sheet.Row(row.Index) |> ignore
        sheet

    /// <summary>
    /// Returns the FsRows of a given FsWorksheet.
    /// </summary>
    static member getRows (sheet : FsWorksheet) = 
        sheet.Rows

    /// <summary>
    /// Returns the FsRow at the given rowIndex of an FsWorksheet.
    /// </summary>
    static member getRowAt rowIndex sheet =
        FsWorksheet.getRows sheet
        // to do: getIndex
        |> Seq.find (FsRow.getIndex >> (=) rowIndex)

    /// <summary>
    /// Returns the FsRow at the given rowIndex of an FsWorksheet if it exists, else returns None.
    /// </summary>
    static member tryGetRowAt rowIndex (sheet : FsWorksheet) =
        sheet.Rows
        |> Seq.tryFind (FsRow.getIndex >> (=) rowIndex)

    /// <summary>
    /// Returns the FsRow matching or exceeding the given rowIndex if it exists, else returns None.
    /// </summary>
    static member tryGetRowAfter rowIndex (sheet : FsWorksheet) =
        sheet.Rows
        |> Seq.tryFind (fun r -> r.Index >= rowIndex)

    /// <summary>
    /// Inserts an FsRow into the FsWorksheet before a reference FsRow.
    /// </summary>
    member self.InsertBefore(row : FsRow, refRow : FsRow) =
        for row in _rows do
            if row.Index >= refRow.Index then
                row.Index <- row.Index + 1
        self.Row(row.Index) |> ignore
        self

    /// <summary>
    /// Inserts an FsRow into the FsWorksheet before a reference FsRow.
    /// </summary>
    static member insertBefore row (refRow : FsRow) (sheet : FsWorksheet) =
        sheet.InsertBefore(row, refRow)

    /// <summary>
    /// Returns true if the FsWorksheet contains an FsRow with the given rowIndex.
    /// </summary>
    member self.ContainsRowAt(rowIndex) =
        self.Rows
        |> Seq.exists (fun t -> t.Index = rowIndex)

    /// <summary>
    /// Returns true if the FsWorksheet contains an FsRow with the given rowIndex.
    /// </summary>
    static member containsRowAt rowIndex (sheet : FsWorksheet) =
        sheet.ContainsRowAt rowIndex

    /// <summary>
    /// Returns the number of FsRows contained in the FsWorksheet.
    /// </summary>
    static member countRows (sheet : FsWorksheet) =
        sheet.Rows.Count

    /// <summary>
    /// Removes the FsRow at the given rowIndex.
    /// </summary>
    member self.RemoveRowAt(rowIndex) =
        let toRemove = 
            _rows |> Seq.filter (fun r -> r.Index = rowIndex)
        for fsrowItem in toRemove do
            _rows.Remove (fsrowItem) |> ignore

    /// <summary>
    /// Removes the FsRow at a given rowIndex of an FsWorksheet.
    /// </summary>
    static member removeRowAt rowIndex (sheet : FsWorksheet) =
        sheet.RemoveRowAt(rowIndex)
        sheet

    /// <summary>
    /// Removes the FsRow at a given rowIndex of the FsWorksheet if the FsRow exists.
    /// </summary>
    member self.TryRemoveAt(rowIndex) =
        if self.ContainsRowAt rowIndex then
            self.RemoveRowAt rowIndex
        self

    /// <summary>
    /// Removes the FsRow at a given rowIndex of an FsWorksheet if the FsRow exists.
    /// </summary>
    static member tryRemoveAt rowIndex sheet =
        if FsWorksheet.containsRowAt rowIndex sheet then
            sheet.RemoveRowAt rowIndex

    /// <summary>
    /// Sorts the FsRows by their rowIndex.
    /// </summary>
    member self.SortRows() =
        let sorted = _rows |> Seq.sortBy (fun r -> r.Index) |> Array.ofSeq
        _rows.Clear()
        _rows.AddRange(sorted)

    /// <summary>
    /// Applies function f to all FsRows and returns the modified FsWorksheet.
    /// </summary>
    member self.MapRowsInPlace(f : FsRow -> FsRow) =
        for i in 0 .. (_rows.Count-1) do
            let r = _rows.[i]
            _rows.[i] <- f r
        self
    
    /// <summary>
    /// Applies function f in a given FsWorksheet to all FsRows and returns the modified FsWorksheet.
    /// </summary>
    static member mapRowsInPlace (f : FsRow -> FsRow) (sheet : FsWorksheet) =
        sheet.MapRowsInPlace f

    // QUESTION: Does that make any sense at all? After all, CellValues are changed inPlace anyway
    ///// Builds a new FsWorksheet whose FsRows are the result of of applying the given function f to each FsRow of the given FsWorksheet.
    //static member mapRows (f : FsRow -> FsRow) (sheet : FsWorksheet) =
    //    let newRows = List.map f sheet.Rows
    //    let newWs = FsWorksheet(sheet.Name)
    //    sheet.Tables 
    //    |> List.iter (
    //        fun t -> 
    //            newWs.Table(t.Name, t.RangeAddress, t.ShowHeaderRow) 
    //            |> ignore
    //    )
    //    newWs.Tables <- sheet.Tables

    /// <summary>
    /// Returns the highest index of any FsRow.
    /// </summary>
    member self.GetMaxRowIndex() =
        if self.Rows.Count = 0 then failwith "The FsWorksheet has no FsRows."
        self.Rows 
        |> Seq.maxBy (fun r -> r.Index)

    /// <summary>
    /// Returns the highest index of any FsRow in a given FsWorksheet.
    /// </summary>
    static member getMaxRowIndex (sheet : FsWorksheet) =
        sheet.GetMaxRowIndex()

    /// <summary>
    /// Gets the string values of the FsRow at the given 1-based rowIndex.
    /// </summary>
    member self.GetRowValuesAt(rowIndex) =
        if self.ContainsRowAt rowIndex then
            self.Row(rowIndex).Cells
            |> Seq.map (fun c -> c.Value)
        else Seq.empty

    /// <summary>
    /// Gets the string values of the FsRow at the given 1-based rowIndex of a given FsWorksheet.
    /// </summary>
    static member getRowValuesAt rowIndex (sheet : FsWorksheet) =
        sheet.GetRowValuesAt rowIndex

    /// <summary>
    /// Gets the string values at the given 1-based rowIndex of the FsRow if it exists, else returns None.
    /// </summary>
    member self.TryGetRowValuesAt(rowIndex) =
        if self.ContainsRowAt rowIndex then
            Some (self.GetRowValuesAt rowIndex)
        else None

    /// <summary>
    /// Takes an FsWorksheet and gets the string values at the given 1-based rowIndex of the FsRow if it exists, else returns None.
    /// </summary>
    static member tryGetRowValuesAt rowIndex (sheet : FsWorksheet) =
        sheet.TryGetRowValuesAt rowIndex

    // TO DO (later)
    ///// If an FsRow with index rowIndex exists in the FsWorksheet, moves it downwards by amount. Negative amounts will move the FsRow upwards.
    //static member moveRowVertical amount rowIndex (sheet : FsWorksheet) =
    //    match FsWorksheet.containsRowAt

    // TO DO (later)
    //static member moveRowBlockDownward rowIndex sheet =

    

    // TO DO (later)
    //static member tryGetIndexedRowValuesAt rowIndex sheet =

    /// <summary>
    /// Checks the cell collection and recreate the whole set of rows, so that all cells are placed in a row
    /// </summary>
    member self.RescanRows() =
        let rows = _rows |> Seq.map (fun r -> r.Index,r) |> Map.ofSeq
        _cells.GetCells()
        |> Seq.groupBy (fun c -> c.RowNumber)
        |> Seq.iter (fun (rowIndex,cells) -> 
            let newRange = 
                cells
                |> Seq.sortBy (fun c -> c.ColumnNumber)
                |> fun cells ->
                    FsAddress(rowIndex,Seq.head cells |> fun c -> c.ColumnNumber),
                    FsAddress(rowIndex,Seq.last cells |> fun c -> c.ColumnNumber)
                |> FsRangeAddress
            match Map.tryFind rowIndex rows with
            | Some row -> 
                row.RangeAddress <- newRange
            | None ->
                self.RowWithRange(newRange)
        )



    // ------
    // Column(s)
    // ------

    /// <summary>
    /// Returns the FsColumn at the given index. If it does not exist, it is created and appended first.
    /// </summary>
    member self.Column(columnIndex : int32) = FsColumn.createAt(columnIndex,self.CellCollection)

    /// <summary>
    /// Returns the FsColumn at the given FsRangeAddress. If it does not exist, it is created and appended first.
    /// </summary>
    member self.ColumnWithRange(rangeAddress : FsRangeAddress) = 
        if rangeAddress.FirstAddress.ColumnNumber <> rangeAddress.LastAddress.ColumnNumber then
            failwithf "Column may not have a range address spanning over different column indices"
        self.Column(rangeAddress.FirstAddress.ColumnNumber).RangeAddress <- rangeAddress

    /// <summary>
    /// Returns the FsColumns of a given FsWorksheet.
    /// </summary>
    static member getColumns (sheet : FsWorksheet) = 
        sheet.Columns

    /// <summary>
    /// Returns the FsColumn at the given columnIndex of an FsWorksheet.
    /// </summary>
    static member getColumnAt columnIndex sheet =
        FsWorksheet.getColumns sheet
        // to do: getIndex
        |> Seq.find (FsColumn.getIndex >> (=) columnIndex)

    /// <summary>
    /// Returns the FsColumn at the given columnIndex of an FsWorksheet if it exists, else returns None.
    /// </summary>
    static member tryGetColumnAt columnIndex (sheet : FsWorksheet) =
        sheet.Columns
        |> Seq.tryFind (FsColumn.getIndex >> (=) columnIndex)

    // --------
    // Table(s)
    // --------

    /// <summary>
    /// Returns the FsTable with the given tableName, rangeAddress, and showHeaderRow parameters. If it does not exist yet, it gets created and appended first.
    /// </summary>
    // TO DO: Ask HLW: Is this really a good name for the method?
    member self.Table(tableName,rangeAddress,?showHeaderRow : bool) = 
        let showHeaderRow = defaultArg showHeaderRow true
        match _tables |> Seq.tryFind (fun table -> table.Name = name) with
        | Some table ->
            table
        | None -> 
            let table = FsTable(tableName,rangeAddress,showHeaderRow)
            _tables.Add table
            table

    /// <summary>
    /// Returns the FsTable of the given name from an FsWorksheet if it exists. Else returns None.
    /// </summary>
    static member tryGetTableByName tableName (sheet : FsWorksheet) =
        sheet.Tables |> Seq.tryFind (fun t -> t.Name = tableName)

    /// <summary>
    /// Returns the FsTable of the given name from an FsWorksheet.
    /// </summary>
    static member getTableByName tableName (sheet : FsWorksheet) =
        try (sheet.Tables |> Seq.tryFind (fun t -> t.Name = tableName)).Value
        with _ -> failwith $"FsTable with name {tableName} is not presen in the FsWorksheet {sheet.Name}."

    // TO DO: tryGetTableByRangeAddress

    // TO DO: getTableByRangeAddress

    /// <summary>
    /// Adds an FsTable to the FsWorksheet if an FsTable with the same name is not already attached.
    /// </summary>
    // TO DO: Ask HLW: rather printfn or failwith?
    member self.AddTable(table : FsTable) =
        if self.Tables |> Seq.exists (fun t -> t.Name = table.Name) then
            printfn $"FsTable {table.Name} could not be appended as an FsTable with this name is already present in the FsWorksheet {self.Name}."
        else _tables.Add(table)
        self

    /// <summary>
    /// Adds an FsTable to the FsWorksheet if an FsTable with the same name is not already attached.
    /// </summary>
    static member addTable table (sheet : FsWorksheet) =
        sheet.AddTable table

    /// <summary>
    /// Adds a list of FsTables to the FsWorksheet. All FsTables with a name already present in the FsWorksheet are not attached.
    /// </summary>
    // TO DO: Ask HLW: rather printfn or failwith?
    member self.AddTables(tables) =
        tables |> List.iter (self.AddTable >> ignore)
        self

    /// <summary>
    /// Adds a list of FsTables to an FsWorksheet. All FsTables with a name already present in the FsWorksheet are not attached.
    /// </summary>
    static member addTables tables (sheet : FsWorksheet) =
        sheet.AddTables tables


    // -------
    // Cell(s)
    // -------

    /// <summary>
    /// Returns the FsCell at the given row- and columnIndex if the FsCell exists, else returns None.
    /// </summary>
    member self.TryGetCellAt(rowIndex, colIndex) =
        self.CellCollection.TryGetCell(rowIndex, colIndex)

    /// <summary>
    /// Returns the FsCell at the given row- and columnIndex of a given FsWorksheet if the FsCell exists, else returns None.
    /// </summary>
    static member tryGetCellAt rowIndex colIndex (sheet : FsWorksheet) =
        sheet.TryGetCellAt(rowIndex, colIndex)

    /// <summary>
    /// Returns the FsCell at the given row- and column index. Index is 1 based!
    /// </summary>
    member self.GetCellAt(rowIndex, colIndex) =
        self.TryGetCellAt(rowIndex, colIndex).Value

    /// <summary>
    /// Returns the FsCell at the given row- and columnIndex of a given FsWorksheet.
    /// </summary>
    static member getCellAt rowIndex colIndex (sheet : FsWorksheet) =
        sheet.GetCellAt(rowIndex, colIndex)

    /// <summary>
    /// Adds a FsCell to the FsWorksheet. !Exception if cell address already exists!
    /// </summary>
    member self.AddCell (cell:FsCell) =
        self.CellCollection.Add cell |> ignore
        self     

    /// <summary>
    /// Adds a sequence of FsCells to the FsWorksheet. !Exception if cell address already exists!
    /// </summary>
    member self.AddCells (cells:seq<FsCell>) =
        self.CellCollection.Add cells |> ignore
        self    

    /// <summary>
    /// Adds a value at the given row- and columnIndex to the FsWorksheet.
    ///
    /// If a cell exists at the given postion, it is shoved to the right.
    /// </summary>
    member self.InsertValueAt(value : System.IConvertible, rowIndex, colIndex)=
        let cell = FsCell(value)
        self.CellCollection.Add(int32 rowIndex, int32 colIndex, cell)

    /// <summary>
    /// Adds a value at the given row- and columnIndex to the FsWorksheet.
    ///
    /// If a cell exists at the given postion, it is shoved to the right.
    /// </summary>
    member self.InsertValueAt(value : obj, rowIndex, colIndex)=
        let cell = FsCell(value)
        self.CellCollection.Add(int32 rowIndex, int32 colIndex, cell)

    /// <summary>
    /// Adds a value at the given row- and columnIndex to a given FsWorksheet.
    ///
    /// If an FsCell exists at the given position, it is shoved to the right.
    /// </summary>
    static member insertValueAt (value : 'a) rowIndex colIndex (sheet : FsWorksheet)=
        sheet.InsertValueAt(value, rowIndex, colIndex)

    /// <summary>
    /// Adds a value at the given row- and columnIndex.
    ///
    /// If an FsCell exists at the given position, overwrites it.
    /// </summary>
    member self.SetValueAt(value : 'a, rowIndex, colIndex) =
        match self.CellCollection.TryGetCell(rowIndex, colIndex) with
        | Some c -> 
            c.SetValueAs value |> ignore
            self
        | None -> 
            self.CellCollection.Add(rowIndex, colIndex, value) |> ignore
            self

    /// <summary>
    /// Adds a value at the given row- and columnIndex of a given FsWorksheet.
    ///
    /// If an FsCell exists at the given position, it is overwritten.
    /// </summary>
    static member setValueAt (value : 'a) rowIndex colIndex (sheet : FsWorksheet) =
        sheet.SetValueAt(value, rowIndex, colIndex)

    /// <summary>
    /// Removes the value at the given row- and columnIndex from the FsWorksheet.
    /// </summary>
    member self.RemoveCellAt(rowIndex, colIndex) =
        self.CellCollection.RemoveCellAt(int32 rowIndex, int32 colIndex)
        self

    /// <summary>
    /// Removes the value at the given row- and columnIndex from an FsWorksheet.
    /// </summary>
    static member removeCellAt rowIndex colIndex (sheet : FsWorksheet) =
        sheet.RemoveCellAt(rowIndex, colIndex)

    /// <summary>
    /// Removes the value of an FsCell at given row- and columnIndex if it exists from the FsCollection.
    /// </summary>
    /// <remarks>Does nothing if the row or column of given index does not exist.</remarks>
    /// <exception cref="System.ArgumentNullException">if columnIndex is null.</exception>
    member self.TryRemoveValueAt(rowIndex, colIndex) =
        self.CellCollection.TryRemoveValueAt(rowIndex, colIndex)

    /// <summary>
    /// Removes the value of an FsCell at given row- and columnIndex if it exists from the FsCollection of a given FsWorksheet.
    /// </summary>
    /// <remarks>Does nothing if the row or column of given index does not exist.</remarks>
    /// <exception cref="System.ArgumentNullException">if columnIndex is null.</exception>
    static member tryRemoveValueAt rowIndex colIndex (sheet : FsWorksheet) =
        sheet.TryRemoveValueAt(rowIndex, colIndex)

    /// <summary>
    /// Removes the value of an FsCell at given row- and columnIndex from the FsCollection.
    /// </summary>
    /// <exception cref="System.ArgumentNullException">if rowIndex or columnIndex is null.</exception>
    /// <exception cref="System.Generic.KeyNotFoundException">if row or column at the given index does not exist.</exception>
    member self.RemoveValueAt(rowIndex, colIndex) =
        self.CellCollection.RemoveValueAt(rowIndex, colIndex)

    /// <summary>
    /// Removes the value of an FsCell at given row- and columnIndex from the FsCollection of a given FsWorksheet.
    /// </summary>
    /// <exception cref="System.ArgumentNullException">if rowIndex or columnIndex is null.</exception>
    /// <exception cref="System.Generic.KeyNotFoundException">if row or column at the given index does not exist.</exception>
    static member removeValueAt rowIndex colIndex (sheet : FsWorksheet) =
        sheet.RemoveValueAt(rowIndex, colIndex)

    /// <summary>
    /// Adds a FsCell to the FsWorksheet. !Exception if cell address already exists!
    /// </summary>
    static member addCell (cell:FsCell) (sheet :FsWorksheet) =
        sheet.AddCell cell 

    /// <summary>
    /// Adds a sequence of FsCells to the FsWorksheet. !Exception if cell address already exists!
    /// </summary>
    static member addCells (cell:seq<FsCell>) (sheet :FsWorksheet) =
        sheet.AddCells cell 
        


    // TO DO (later)
    //static member tryRemoveValueAt 

    // TO DO (later)
    //static member tryGetCellValueAt rowIndex colIndex sheet =

    // TO DO (later)
    //static member getCellValueAt rowIndex colIndex sheet =


    // ------------
    // Worksheet(s)
    // ------------

    // TO DO (later)
    //static member toSparseValueMatrix sheet =