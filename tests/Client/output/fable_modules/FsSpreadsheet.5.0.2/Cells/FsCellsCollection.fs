namespace FsSpreadsheet

open System.Collections.Generic

module Dictionary = 

    let tryGet (k : 'Key) (dict : Dictionary<'Key,'Value>) =

        if dict.ContainsKey k then
            Some (dict.Item k)
        else None

/// <summary>
///
/// </summary>
type FsCellsCollection() =

    // ---------
    // VARIABLES
    // ---------

    let mutable _columnsUsed : Dictionary<int32, int32> = new Dictionary<int, int>()
    let mutable _deleted : Dictionary<int32, HashSet<int32>> = new Dictionary<int, HashSet<int>>()
    let mutable _rowsCollection : Dictionary<int, Dictionary<int, FsCell>> = new Dictionary<int, Dictionary<int, FsCell>>()

    let mutable _maxColumnUsed : int32 = 0
    let mutable _maxRowUsed : int32 = 0
    let mutable _rowsUsed : Dictionary<int32, int32> = new Dictionary<int, int>();

    let mutable _count = 0


    // ----------
    // PROPERTIES
    // ----------

    member this.Count 
        with get () = _count
        and private set(count) = _count <- count

    /// <summary>
    /// The highest rowIndex in The FsCellsCollection.
    /// </summary>
    /// <remarks>Do not confuse with the number of rows in the FsCellsCollection.</remarks>
    member this.MaxRowNumber = _maxRowUsed

    /// <summary>
    /// The highest columnIndex in The FsCellsCollection.
    /// </summary>
    /// <remarks>Do not confuse with the number of columns in the FsCellsCollection.</remarks>
    member this.MaxColumnNumber = _maxColumnUsed


    // -------
    // METHODS
    // -------

    /// <summary>
    /// Creates a deep copy of the FsCellsCollection.
    /// </summary>
    member this.Copy() =
        let cells = this.GetCells() |> Seq.map (fun (c : FsCell) -> c.Copy())
        FsCellsCollection.createFromCells cells

    /// <summary>
    /// Returns a deep copy of a given FsCellsCollection.
    /// </summary>
    static member copy (cellsCollection : FsCellsCollection) =
        cellsCollection.Copy()

    /// 
    static member private IncrementUsage(dictionary : Dictionary<int, int>, key : int32) =

        match Dictionary.tryGet key dictionary with
        | Some count -> 
            dictionary.[key] <- count + 1
        | None -> 
            dictionary.Add(key, 1)

    ///// <summary/>
    ///// <returns>True if the number was lowered to zero so MaxColumnUsed or MaxRowUsed may require
    ///// recomputation.</returns>
    static member private DecrementUsage(dictionary : Dictionary<int, int>, key : int32) =

        match Dictionary.tryGet key dictionary with
        | Some count when count > 1 -> 
            dictionary.[key] <- count - 1
            false
        | Some _ -> 
            dictionary.Remove(key) |> ignore
            true
        | None -> 
            false

    /// <summary>
    /// Creates an FsCellsCollection from the given FsCells.
    /// </summary>
    /// <remarks>Derives row- and columnIndices from the FsAddress of the FsCells.</remarks>
    static member createFromCells (cells : seq<FsCell>) =
        let fcc = FsCellsCollection()
        fcc.Add cells
        fcc

    // TO DO: Must create deep copy methods for ALL other objects in it first, and call them here.
    ///// <summary>Creates and returns a deep copy of the FsCellsCollection.</summary>
    //member this.Copy() =
    //    let newCellsColl = FsCellsCollection()
    //    let oldCells : seq<FsCell> = this.GetCells()
    //    let newCells = // create deep copy here
    //    newCellsColl.Add cells

    ///// <summary>
    ///// Creates and returns a deep copy of an FsCellsCollection.
    ///// </summary>
    //static member copy (cellsCollection : FsCellsCollection) =
    //    cellsCollection.Copy()

    /// <summary>
    /// Empties the whole FsCellsCollection.
    /// </summary>
    member this.Clear() =

        _count <- 0;
        _rowsUsed.Clear();
        _columnsUsed.Clear();

        _rowsCollection.Clear();
        _maxRowUsed <- 0;
        _maxColumnUsed <- 0

        this

    //public void Add(XLSheetPoint sheetPoint, XLCell cell)
    //{
    //    Add(sheetPoint.Row, sheetPoint.Column, cell);
    //}

    /// <summary>
    /// Adds an FsCell of given rowIndex and columnIndex to the FsCellsCollection.
    /// </summary>
    member this.Add(row : int32, column : int32, cell : FsCell) = 

        cell.RowNumber <- row
        cell.ColumnNumber <- column
        _count <- _count + 1

        FsCellsCollection.IncrementUsage(_rowsUsed, row);
        FsCellsCollection.IncrementUsage(_columnsUsed, column);

        let columnsCollection =
            match Dictionary.tryGet row _rowsCollection with
            | Some columnsCollection -> 
                columnsCollection
            | None -> 
                let columnsCollection = new Dictionary<int, FsCell>();
                _rowsCollection.Add(row, columnsCollection);
                columnsCollection

        columnsCollection.Add(column, cell);
        if row > _maxRowUsed then _maxRowUsed <- row;
        if column > _maxColumnUsed then _maxColumnUsed <- column;

        match Dictionary.tryGet row _deleted with
        | Some delHash -> 
            delHash.Remove(column) |> ignore
        | None -> 
            ()

    /// <summary>
    /// Adds an FsCell of given rowIndex and columnIndex to an FsCellsCollection.
    /// </summary>
    static member addCellWithIndeces rowIndex colIndex (cell : FsCell) (cellsCollection : FsCellsCollection) = 
        cellsCollection.Add(rowIndex, colIndex, cell)

    /// <summary>
    /// Adds an FsCell to the FsCellsCollection.
    /// </summary>
    /// <remarks>Derives row- and columnIndex from the FsAddress of the FsCell.</remarks>
    member this.Add(cell : FsCell) =
        this.Add(cell.Address.RowNumber, cell.Address.ColumnNumber, cell)

    /// <summary>
    /// Adds an FsCell to an FsCellsCollection.
    /// </summary>
    /// <remarks>Derives row- and columnIndex from the FsAddress of the FsCell.</remarks>
    static member addCell (cell : FsCell) (cellsCollection : FsCellsCollection) =
        cellsCollection.Add(cell)
        cellsCollection

    /// <summary>
    /// Adds FsCells to the FsCellsCollection.
    /// </summary>
    /// <remarks>Derives row- and columnIndeces from the FsAddress of the FsCells.</remarks>
    member this.Add(cells : seq<FsCell>) =
        cells |> Seq.iter (this.Add >> ignore)

    /// <summary>
    /// Adds FsCells to an FsCellsCollection.
    /// </summary>
    /// <remarks>Derives row- and columnIndeces from the FsAddress of the FsCells.</remarks>
    static member addCells (cells : seq<FsCell>) (cellsCollection : FsCellsCollection) =
        cellsCollection.Add cells
        cellsCollection

    /// <summary>
    /// Checks if an FsCell exists at given row- and columnIndex.
    /// </summary>
    member this.ContainsCellAt(rowIndex, colIndex) =
        match Dictionary.tryGet rowIndex _rowsCollection with
        | Some colsCollection -> colsCollection.ContainsKey colIndex
        | None -> false

    /// <summary>
    /// Checks if an FsCell exists at given row- and columnIndex of a given FsCellsCollection.
    /// </summary>
    static member containsCellAt rowIndex colIndex (cellsCollection : FsCellsCollection) =
        cellsCollection.ContainsCellAt(rowIndex, colIndex)

    //public void Remove(XLSheetPoint sheetPoint)
    //{
    //    Remove(sheetPoint.Row, sheetPoint.Column);
    //}

    /// <summary>
    /// Removes an FsCell of given rowIndex and columnIndex from the FsCellsCollection.
    /// </summary>
    member this.RemoveCellAt(row : int32, column : int32) = 

        _count <- _count - 1
        let rowRemoved = FsCellsCollection.DecrementUsage(_rowsUsed, row);
        let columnRemoved = FsCellsCollection.DecrementUsage(_columnsUsed, column);

        if rowRemoved && row = _maxRowUsed then

            _maxRowUsed <- 
                if (_rowsUsed.Keys :> IEnumerable<_>) |> Seq.isEmpty |> not then
                    _rowsUsed.Keys |> Seq.max
                else 0

        if columnRemoved && column = _maxColumnUsed then
        
            _maxColumnUsed <- 
                if (_columnsUsed.Keys :> IEnumerable<_>) |> Seq.isEmpty |> not then
                    _columnsUsed.Keys |> Seq.max
                else 0

        match Dictionary.tryGet row _deleted with
        | Some delHash when delHash.Contains(column) -> 
            ()
        | Some delHash ->
            delHash.Add(column) |> ignore         
        | None -> 
            let delHash = new HashSet<int>()
            delHash.Add(column) |> ignore 
            _deleted.Add(row, delHash)

        match Dictionary.tryGet row _rowsCollection with
        | Some columnsCollection -> 
            columnsCollection.Remove(column) |> ignore
            if columnsCollection.Count = 0 then
                _rowsCollection.Remove(row) |> ignore
        | None -> 
            ()

    /// <summary>
    /// Removes an FsCell of given rowIndex and columnIndex from an FsCellsCollection.
    /// </summary>
    static member removeCellAt rowIndex colIndex (cellsCollection : FsCellsCollection) = 
        cellsCollection.RemoveCellAt(rowIndex, colIndex)
        cellsCollection

    /// <summary>
    /// Removes the value of an FsCell at given row- and columnIndex if it exists from the FsCollection.
    /// </summary>
    /// <remarks>Does nothing if the row or column of given index does not exist.</remarks>
    /// <exception cref="System.ArgumentNullException">if columnIndex is null.</exception>
    member this.TryRemoveValueAt(rowIndex, colIndex) =
        match Dictionary.tryGet rowIndex _rowsCollection with
        | Some colsCollection ->
            try (colsCollection.Item colIndex).Value <- "" with
            _ -> ()
        | None -> ()

    /// <summary>
    /// Removes the value of an FsCell at given row- and columnIndex if it exists from a given FsCollection.
    /// </summary>
    /// <remarks>Does nothing if the row or column of given index does not exist.</remarks>
    /// <exception>Throws `System.ArgumentNullException` if columnIndex is null.</exception>
    static member tryRemoveValueAt rowIndex colIndex (cellsCollection : FsCellsCollection) =
        cellsCollection.TryRemoveValueAt(rowIndex, colIndex)
        cellsCollection

    /// <summary>
    /// Removes the value of an FsCell at given row- and columnIndex from the FsCollection.
    /// </summary>
    /// <exception cref="System.ArgumentNullException">if rowIndex or columnIndex is null.</exception>
    /// <exception cref="System.Generic.KeyNotFoundException">if row or column at the given index does not exist.</exception>
    member this.RemoveValueAt(rowIndex, colIndex) =
        _rowsCollection
            .Item(rowIndex)
            .Item(colIndex)
            .Value <- ""

    /// <summary>
    /// Removes the value of an FsCell at given row- and columnIndex from a given FsCollection.
    /// </summary>
    /// <exception cref="System.ArgumentNullException">if rowIndex or columnIndex is null.</exception>
    /// <exception cref="System.Generic.KeyNotFoundException">if row or column at the given index does not exist.</exception>
    static member removeValueAt rowIndex colIndex (cellsCollection : FsCellsCollection) =
        cellsCollection.RemoveValueAt(rowIndex, colIndex)
        cellsCollection

    /// <summary>
    /// Returns all FsCells of the FsCellsCollection.
    /// </summary>
    member this.GetCells() = 
        _rowsCollection.Values
        |> Seq.collect (fun columnsCollection -> columnsCollection.Values)

    /// <summary>
    /// Returns all FsCells of the FsCellsCollection.
    /// </summary>
    static member getCells (cellsCollection : FsCellsCollection) = 
        cellsCollection.GetCells()

    /// <summary>
    /// Returns the FsCells from given rowStart to rowEnd and columnStart to columnEnd and fulfilling the predicate.
    /// </summary>
    member this.GetCells(rowStart : int32, columnStart : int32, rowEnd : int32, columnEnd : int32, predicate : FsCell -> bool) = 

        let finalRow = if rowEnd > _maxRowUsed then _maxRowUsed else rowEnd
        let finalColumn = if columnEnd > _maxColumnUsed then _maxColumnUsed else columnEnd
        seq {
            for ro = rowStart to finalRow do

            match Dictionary.tryGet ro _rowsCollection with
            | Some columnsCollection -> 
                for co = columnStart to finalColumn do
                    match Dictionary.tryGet co columnsCollection with
                    | Some cell when predicate cell ->
                        yield cell
                    | _ -> ()                   
            | None -> ()
        }

    /// <summary>
    /// Returns the FsCells from an FsCellsCollection with given rowStart to rowEnd and columnStart to columnEnd and fulfilling the predicate.
    /// </summary>
    static member filterCellsFromTo rowStart columnStart rowEnd columnEnd (predicate : FsCell -> bool) (cellsCollection : FsCellsCollection) = 
        cellsCollection.GetCells(rowStart, columnStart, rowEnd, columnEnd, predicate)

    /// <summary>
    /// Returns the FsCells from given startAddress to lastAddress and fulfilling the predicate.
    /// </summary>
    member this.GetCells(startAddress : FsAddress, lastAddress : FsAddress, predicate : FsCell -> bool) =
        this.GetCells(startAddress.RowNumber,startAddress.ColumnNumber,lastAddress.RowNumber,lastAddress.ColumnNumber, predicate)

    
    /// <summary>
    /// Returns the FsCells from an FsCellsCollection with given startAddress to lastAddress and fulfilling the predicate.
    /// </summary>
    static member filterCellsFromToAddress startAddress lastAddress (predicate : FsCell -> bool) (cellsCollection : FsCellsCollection) =
        cellsCollection.GetCells(startAddress, lastAddress, predicate)

    /// <summary>
    /// Returns the FsCells from given rowStart to rowEnd and columnStart to columnEnd.
    /// </summary>
    member this.GetCells(rowStart : int32, columnStart : int32, rowEnd : int32, columnEnd : int32) = 
        let finalRow = if rowEnd > _maxRowUsed then _maxRowUsed else rowEnd
        let finalColumn = if columnEnd > _maxColumnUsed then _maxColumnUsed else columnEnd
        seq {
            for ro = rowStart to finalRow do
    
            match Dictionary.tryGet ro _rowsCollection with
            | Some columnsCollection -> 
                for co = columnStart to finalColumn do
                    match Dictionary.tryGet co columnsCollection with
                    | Some cell ->
                        yield cell
                    | _ -> ()
            | None -> ()
        }

    /// <summary>
    /// Returns the FsCells from an FsCellsCollection with given rowStart to rowEnd and columnStart to columnEnd.
    /// </summary>
    static member getCellsFromTo rowStart columnStart rowEnd columnEnd (cellsCollection : FsCellsCollection) = 
        cellsCollection.GetCells(rowStart, columnStart, rowEnd, columnEnd)

    /// <summary>
    /// Returns the FsCells from given startAddress to lastAddress.
    /// </summary>
    member this.GetCells(startAddress : FsAddress, lastAddress : FsAddress) =
        this.GetCells(startAddress.RowNumber,startAddress.ColumnNumber,lastAddress.RowNumber,lastAddress.ColumnNumber)

    /// <summary>
    /// Returns the FsCells from an FsCellsCollection with given startAddress to lastAddress.
    /// </summary>
    static member getCellsFromToAddress startAddress lastAddress (cellsCollection : FsCellsCollection) =
        cellsCollection.GetCells(startAddress, lastAddress)

    //public int FirstRowUsed(int rowStart, int columnStart, int rowEnd, int columnEnd, XLCellsUsedOptions options,
    //    Func<IXLCell, Boolean> predicate = null)
    //{
    //    int finalRow = rowEnd > MaxRowUsed ? MaxRowUsed : rowEnd;
    //    int finalColumn = columnEnd > MaxColumnUsed ? MaxColumnUsed : columnEnd;
    //    for (int ro = rowStart; ro <= finalRow; ro++)
    //    {
    //        if (RowsCollection.TryGetValue(ro, out Dictionary<int32, XLCell> columnsCollection))
    //        {
    //            for (int co = columnStart; co <= finalColumn; co++)
    //            {
    //                if (columnsCollection.TryGetValue(co, out XLCell cell)
    //                    && !cell.IsEmpty(options)
    //                    && (predicate == null || predicate(cell)))

    //                    return ro;
    //            }
    //        }
    //    }

    //    return 0;
    //}

    //public int FirstColumnUsed(int rowStart, int columnStart, int rowEnd, int columnEnd, XLCellsUsedOptions options,
    //    Func<IXLCell, Boolean> predicate = null)
    //{
    //    int finalRow = rowEnd > MaxRowUsed ? MaxRowUsed : rowEnd;
    //    int finalColumn = columnEnd > MaxColumnUsed ? MaxColumnUsed : columnEnd;
    //    int firstColumnUsed = finalColumn;
    //    var found = false;
    //    for (int ro = rowStart; ro <= finalRow; ro++)
    //    {
    //        if (RowsCollection.TryGetValue(ro, out Dictionary<int32, XLCell> columnsCollection))
    //        {
    //            for (int co = columnStart; co <= firstColumnUsed; co++)
    //            {
    //                if (columnsCollection.TryGetValue(co, out XLCell cell)
    //                    && !cell.IsEmpty(options)
    //                    && (predicate == null || predicate(cell))
    //                    && co <= firstColumnUsed)
    //                {
    //                    firstColumnUsed = co;
    //                    found = true;
    //                    break;
    //                }
    //            }
    //        }
    //    }

    //    return found ? firstColumnUsed : 0;
    //}

    //public int LastRowUsed(int rowStart, int columnStart, int rowEnd, int columnEnd, XLCellsUsedOptions options,
    //    Func<IXLCell, Boolean> predicate = null)
    //{
    //    int finalRow = rowEnd > MaxRowUsed ? MaxRowUsed : rowEnd;
    //    int finalColumn = columnEnd > MaxColumnUsed ? MaxColumnUsed : columnEnd;
    //    for (int ro = finalRow; ro >= rowStart; ro--)
    //    {
    //        if (RowsCollection.TryGetValue(ro, out Dictionary<int32, XLCell> columnsCollection))
    //        {
    //            for (int co = finalColumn; co >= columnStart; co--)
    //            {
    //                if (columnsCollection.TryGetValue(co, out XLCell cell)
    //                    && !cell.IsEmpty(options)
    //                    && (predicate == null || predicate(cell)))

    //                    return ro;
    //            }
    //        }
    //    }
    //    return 0;
    //}

    //public int LastColumnUsed(int rowStart, int columnStart, int rowEnd, int columnEnd, XLCellsUsedOptions options,
    //    Func<IXLCell, Boolean> predicate = null)
    //{
    //    int maxCo = 0;
    //    int finalRow = rowEnd > MaxRowUsed ? MaxRowUsed : rowEnd;
    //    int finalColumn = columnEnd > MaxColumnUsed ? MaxColumnUsed : columnEnd;
    //    for (int ro = finalRow; ro >= rowStart; ro--)
    //    {
    //        if (RowsCollection.TryGetValue(ro, out Dictionary<int, XLCell> columnsCollection))
    //        {
    //            for (int co = finalColumn; co >= columnStart && co > maxCo; co--)
    //            {
    //                if (columnsCollection.TryGetValue(co, out XLCell cell)
    //                    && !cell.IsEmpty(options)
    //                    && (predicate == null || predicate(cell)))

    //                    maxCo = co;
    //            }
    //        }
    //    }
    //    return maxCo;
    //}

    //public void RemoveAll(int32 rowStart, int32 columnStart,
    //                        int32 rowEnd, int32 columnEnd)
    //{
    //    int finalRow = rowEnd > MaxRowUsed ? MaxRowUsed : rowEnd;
    //    int finalColumn = columnEnd > MaxColumnUsed ? MaxColumnUsed : columnEnd;
    //    for (int ro = rowStart; ro <= finalRow; ro++)
    //    {
    //        if (RowsCollection.TryGetValue(ro, out Dictionary<int, XLCell> columnsCollection))
    //        {
    //            for (int co = columnStart; co <= finalColumn; co++)
    //            {
    //                if (columnsCollection.ContainsKey(co))
    //                    Remove(ro, co);
    //            }
    //        }
    //    }
    //}

    //public IEnumerable<XLSheetPoint> GetSheetPoints(int32 rowStart, int32 columnStart,
    //                                                int32 rowEnd, int32 columnEnd)
    //{
    //    int finalRow = rowEnd > MaxRowUsed ? MaxRowUsed : rowEnd;
    //    int finalColumn = columnEnd > MaxColumnUsed ? MaxColumnUsed : columnEnd;
    //    for (int ro = rowStart; ro <= finalRow; ro++)
    //    {
    //        if (RowsCollection.TryGetValue(ro, out Dictionary<int32, XLCell> columnsCollection))
    //        {
    //            for (int co = columnStart; co <= finalColumn; co++)
    //            {
    //                if (columnsCollection.ContainsKey(co))
    //                    yield return new XLSheetPoint(ro, co);
    //            }
    //        }
    //    }
    //}

    /// <summary>
    /// Returns the FsCell at given rowIndex and columnIndex if it exists. Otherwise returns None.
    /// </summary>
    member this.TryGetCell(row : int32, column : int32) = 

        if (row > _maxRowUsed || column > _maxColumnUsed) then
            None

        else
            match Dictionary.tryGet row _rowsCollection with
            | Some columnsCollection -> 
                match Dictionary.tryGet column columnsCollection with
                | Some cell -> Some cell
                | None -> None
            | None -> None

    /// <summary>
    /// Returns the FsCell from an FsCellsCollection at given rowIndex and columnIndex if it exists. Otherwise returns None.
    /// </summary>
    static member tryGetCell rowIndex colIndex (cellsCollection : FsCellsCollection) = 
        cellsCollection.TryGetCell(rowIndex, colIndex)

    //public XLCell GetCell(XLSheetPoint sp)
    //{
    //    return GetCell(sp.Row, sp.Column);
    //}

    //internal void SwapRanges(XLSheetRange sheetRange1, XLSheetRange sheetRange2, XLWorksheet worksheet)
    //{
    //    int32 rowCount = sheetRange1.LastPoint.Row - sheetRange1.FirstPoint.Row + 1;
    //    int32 columnCount = sheetRange1.LastPoint.Column - sheetRange1.FirstPoint.Column + 1;
    //    for (int row = 0; row < rowCount; row++)
    //    {
    //        for (int column = 0; column < columnCount; column++)
    //        {
    //            var sp1 = new XLSheetPoint(sheetRange1.FirstPoint.Row + row, sheetRange1.FirstPoint.Column + column);
    //            var sp2 = new XLSheetPoint(sheetRange2.FirstPoint.Row + row, sheetRange2.FirstPoint.Column + column);
    //            var cell1 = GetCell(sp1);
    //            var cell2 = GetCell(sp2);

    //            if (cell1 == null) cell1 = worksheet.Cell(sp1.Row, sp1.Column);
    //            if (cell2 == null) cell2 = worksheet.Cell(sp2.Row, sp2.Column);

    //            //if (cell1 != null)
    //            //{
    //            cell1.Address = new XLAddress(cell1.Worksheet, sp2.Row, sp2.Column, false, false);
    //            Remove(sp1);
    //            //if (cell2 != null)
    //            Add(sp1, cell2);
    //            //}

    //            //if (cell2 == null) continue;

    //            cell2.Address = new XLAddress(cell2.Worksheet, sp1.Row, sp1.Column, false, false);
    //            Remove(sp2);
    //            //if (cell1 != null)
    //            Add(sp2, cell1);
    //        }
    //    }
    //}

    //internal IEnumerable<XLCell> GetCells()
    //{
    //    return GetCells(1, 1, MaxRowUsed, MaxColumnUsed);
    //}

    //internal IEnumerable<XLCell> GetCells(Func<IXLCell, Boolean> predicate)
    //{
    //    for (int ro = 1; ro <= MaxRowUsed; ro++)
    //    {
    //        if (RowsCollection.TryGetValue(ro, out Dictionary<int32, XLCell> columnsCollection))
    //        {
    //            for (int co = 1; co <= MaxColumnUsed; co++)
    //            {
    //                if (columnsCollection.TryGetValue(co, out XLCell cell)
    //                    && (predicate == null || predicate(cell)))
    //                    yield return cell;
    //            }
    //        }
    //    }
    //}

    //public Boolean Contains(int32 row, int32 column)
    //{
    //    return RowsCollection.TryGetValue(row, out Dictionary<int32, XLCell> columnsCollection)
    //        && columnsCollection.ContainsKey(column);
    //}

    //public int32 MinRowInColumn(int32 column)
    //{
    //    for (int row = 1; row <= MaxRowUsed; row++)
    //    {
    //        if (RowsCollection.TryGetValue(row, out Dictionary<int32, XLCell> columnsCollection)
    //            && columnsCollection.ContainsKey(column))

    //            return row;
    //    }

    //    return 0;
    //}

    //public int32 MaxRowInColumn(int32 column)
    //{
    //    for (int row = MaxRowUsed; row >= 1; row--)
    //    {
    //        if (RowsCollection.TryGetValue(row, out Dictionary<int32, XLCell> columnsCollection)
    //            && columnsCollection.ContainsKey(column))

    //            return row;
    //    }

    //    return 0;
    //}

    //public int32 MinColumnInRow(int32 row)
    //{
    //    if (RowsCollection.TryGetValue(row, out Dictionary<int32, XLCell> columnsCollection)
    //        && columnsCollection.Any())

    //        return columnsCollection.Keys.Min();

    //    return 0;
    //}

    //public int32 MaxColumnInRow(int32 row)
    //{
    //    if (RowsCollection.TryGetValue(row, out Dictionary<int32, XLCell> columnsCollection)
    //        && columnsCollection.Any())

    //        return columnsCollection.Keys.Max();

    //    return 0;
    //}

    /// <summary>
    /// Returns all FsCells in the given columnIndex.
    /// </summary>
    member this.GetCellsInColumn(colIndex) =
        this.GetCells(1, colIndex, _maxRowUsed, colIndex)

    /// <summary>
    /// Returns all FsCells in an FsCellsCollection with the given columnIndex.
    /// </summary>
    static member getCellsInColumn colIndex (cellsCollection : FsCellsCollection) =
        cellsCollection.GetCellsInColumn colIndex

    /// <summary>
    /// Returns all FsCells in the given rowIndex.
    /// </summary>
    member this.GetCellsInRow(rowIndex) =
        this.GetCells(rowIndex, 1, rowIndex, _maxColumnUsed)

    /// <summary>
    /// Returns all FsCells in an FsCellsCollection with the given rowIndex.
    /// </summary>
    static member getCellsInRow rowIndex (cellsCollection : FsCellsCollection) =
        cellsCollection.GetCellsInRow rowIndex

    /// <summary>
    /// Returns the upper left corner of the FsCellsCollection.
    /// </summary>
    member this.GetFirstAddress() =
        if Seq.isEmpty _rowsCollection || Seq.isEmpty _rowsCollection.Keys then 
            FsAddress(0,0)
        else
            let minRow = _rowsCollection.Keys |> Seq.min
            let minCol = 
                _rowsCollection.Values 
                |> Seq.minBy (fun d -> Seq.min d.Keys)
                |> fun d -> Seq.min d.Keys
            FsAddress(minRow, minCol)

    /// <summary>
    /// Returns the upper left corner of a given FsCellsCollection.
    /// </summary>
    static member getFirstAddress (cells : FsCellsCollection) =
        cells.GetFirstAddress()

    /// <summary>
    /// Returns the lower right corner of the FsCellsCollection.
    /// </summary>
    member this.GetLastAddress() =
        FsAddress(this.MaxRowNumber, this.MaxColumnNumber)

    /// <summary>
    /// Returns the lower right corner of a given FsCellsCollection.
    /// </summary>
    static member getLastAddress (cells : FsCellsCollection) =
        cells.GetLastAddress()

    // TO DO: Add method to get FsRange when possible