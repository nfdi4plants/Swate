namespace FsSpreadsheet

open System.Collections.Generic
open System.Collections

open Fable.Core
// Type based on the type XLRow used in ClosedXml
/// <summary>
/// Creates an FsColumn from the given FsRangeAddress, consisting of FsCells within a given FsCellsCollection, and a styleValue.
/// </summary>
/// <remarks>The FsCellsCollection must only cover 1 column!</remarks>
/// <exception cref="System.Exception">if given FsCellsCollection has more than 1 column.</exception>
[<AttachMembers>]
type FsColumn (rangeAddress : FsRangeAddress, cells : FsCellsCollection)= 

    inherit FsRangeBase(rangeAddress)

    let cells = cells

    // ----------
    // Creation
    // ----------

    /// Creates an empty FsColumn, ranging from row 0, column 0 to row 0, column (1-based).
    static member empty() = FsColumn (FsRangeAddress(FsAddress(0,0), FsAddress(0,0)), FsCellsCollection())

    /// <summary>
    /// Create an FsColumn from a given FsCellsCollection and an columnColumn.
    /// </summary>
    /// <remarks>The appropriate range of the cells (i.e. minimum colIndex and maximum colIndex) is derived from the FsCells with the matching rowIndex.</remarks>
    static member createAt(index : int32, (cells : FsCellsCollection)) = 
        let getIndexBy (f : (FsCell -> int) -> seq<FsCell> -> FsCell) = 
            match cells.GetCellsInColumn index |> Seq.length with
            | 0 -> 1
            | _ ->
                (
                    cells.GetCellsInColumn index 
                    |> f (fun c -> c.Address.RowNumber)
                ).Address.RowNumber
        let minRowIndex = getIndexBy Seq.minBy
        let maxRowIndex = getIndexBy Seq.maxBy
        FsColumn (FsRangeAddress(FsAddress(minRowIndex, index), FsAddress(maxRowIndex, index)), cells)

    interface IEnumerable<FsCell> with
        member this.GetEnumerator() : System.Collections.Generic.IEnumerator<FsCell> = this.Cells.GetEnumerator()

    interface IEnumerable with
        member this.GetEnumerator() = (this :> IEnumerable<FsCell>).GetEnumerator() :> IEnumerator

    // ----------
    // PROPERTIES
    // ----------

    /// The associated FsCells.
    member this.Cells = 
        base.Cells(cells)

    /// <summary>
    /// The index of the FsColumn.
    /// </summary>
    member this.Index 
        with get() = this.RangeAddress.FirstAddress.ColumnNumber
        and set(i) = 
            this.RangeAddress.FirstAddress.ColumnNumber <- i
            this.RangeAddress.LastAddress.ColumnNumber <- i

    /// <summary>
    /// The number of the lowest row index of the FsColumn where an FsCell exists.
    /// </summary>
    member this.MinRowIndex
        with get() = this.RangeAddress.FirstAddress.RowNumber

    /// <summary>
    /// The number of the highest row index of the FsColumn where an FsCell exists.
    /// </summary>
    member this.MaxRowIndex
        with get() = this.RangeAddress.LastAddress.RowNumber


    // -------
    // METHODS
    // -------

    /// <summary>
    /// Creates a deep copy of this FsColumn.
    /// </summary>
    member this.Copy() =
        let ra = this.RangeAddress.Copy()
        let cells = this.Cells |> Seq.map (fun c -> c.Copy())
        let fcc = FsCellsCollection()
        fcc.Add cells
        FsColumn(ra, fcc)

    /// <summary>
    /// Returns a deep copy of a given FsColumn.
    /// </summary>
    static member copy (column : FsColumn) =
        column.Copy()

    /// <summary>
    /// Returns the index of the given FsColumn.
    /// </summary>
    static member getIndex (column : FsColumn) = 
        column.Index

    /// <summary>
    /// Checks if there is an FsCell at given row index.
    /// </summary>
    /// <param name="rowIndex">The number of the row where the presence of an FsCell shall be checked.</param>
    member this.HasCellAt(rowIndex) =
        this.Cells
        |> Seq.exists (fun c -> c.RowNumber = rowIndex)

    /// <summary>
    /// Checks if there is an FsCell at given row index of a given FsColumn.
    /// </summary>
    /// <param name="rowIndex">The number of the row where the presence of an FsCell shall be checked.</param>
    /// <param name="column"></param>
    static member hasCellAt rowIndex (column : FsColumn) =
        column.HasCellAt rowIndex

    /// <summary>
    /// Returns the FsCell at rowIndex.
    /// </summary>
    member this.Item (rowIndex) =
        // use FsRangeBase call with colindex 1
        base.Cell(FsAddress(rowIndex, 1), cells)

    /// <summary>
    /// Returns the FsCell at the given rowIndex from an FsColumn.
    /// </summary>
    static member item rowIndex (column : FsColumn) =
        column.Item(rowIndex)

    /// <summary>
    /// Returns the FsCell at the given rowIndex if it exists. Else returns None.
    /// </summary>
    /// <param name="rowIndex">The number of the column where the FsCell shall be retrieved.</param>
    member this.TryItem(rowIndex) =
        if this.HasCellAt rowIndex then Some this[rowIndex]
        else None

    /// <summary>
    /// Returns the FsCell at the given rowIndex if it exists in the given FsColumn. Else returns None.
    /// </summary>
    /// <param name="rowIndex">The number of the column where the FsCell shall be retrieved.</param>
    /// <param name="column"></param>
    static member tryItem rowIndex (column : FsColumn) =
        column.TryItem rowIndex

    ///// <summary>
    ///// Inserts the value at columnIndex as an FsCell. If there is an FsCell at the position, this FsCells and all the ones /right /to it are shifted to the right.
    ///// </summary>
    //member this.InsertValueAt(colIndex, (value : 'a)) =
    //    let cell = FsCell(value)
    //    cells.Add(int32 this.Index, int32 colIndex, cell)

    ///// <summary>
    ///// Adds a value at the given row- and columnIndex to FsRow using.
    /////
    ///// If a cell exists in the given position, shoves it to the right.
    ///// </summary>
    //static member insertValueAt colIndex value (row : FsRow) =
    //    row.InsertValueAt(colIndex, value) |> ignore
    //    row

    //member this.SortCells() = _cells <- _cells |> List.sortBy (fun c -> c.WorksheetColumn)

    // TO DO (later)
    ///// Takes an FsCellsCollection and creates an FsRow from the given rowIndex and the cells in the FsCellsCollection that share the same rowIndex.
    //static member fromCellsCollection rowIndex (cellsCollection : FsCellsCollection) =

    /// <summary>
    /// Transforms the FsColumn into a dense FsColumn.
    /// 
    /// FsColumns are sparse by default. This means there are no FsCells present between positions with that are filled with FsCells. In dense FsColumns, such "empty positions" are then filled with empty FsCells.
    /// </summary>
    member this.ToDenseColumn() =
        for i = this.MinRowIndex to this.MaxRowIndex do 
            ignore this[i]

    /// <summary>
    /// Transforms the given FsColumn into a dense FsColumn.
    /// 
    /// FsColumns are sparse by default. This means there are no FsCells present between positions with that are filled with FsCells. In dense FsColumns, such "empty positions" are then filled with empty FsCells.
    /// </summary>
    /// <param name="column">The FsColumn that gets transformed into a dense FsColumn.</param>
    /// <remarks>This is an in-place operation.</remarks>
    static member toDenseColumn (column : FsColumn) =
        column.ToDenseColumn()
        column

    /// <summary>
    /// Takes a given FsColumn and returns a new dense FsColumn from it.
    /// 
    /// FsColumns are sparse by default. This means there are no FsCells present between positions with that are filled with FsCells. In dense FsColumns, such "empty positions" are then filled with empty FsCells.
    /// </summary>
    /// <param name="column">The FsColumn that whose copy gets transformed into a dense FsColumn.</param>
    static member createDenseColumnOf (column : FsColumn) =
        let newColumn = column.Copy()
        newColumn.ToDenseColumn()
        newColumn