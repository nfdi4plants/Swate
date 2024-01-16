[<AutoOpen>]
module Fable.ExcelJs.Worksheet

open Fable.Core
open Fable.Core.JsInterop
open Fable.ExcelJs
open Cell
open Row
open Column
open Table

type WorksheetProperties =
    abstract member tabColor        : obj with get, set
    abstract member outlineLevelCol : int with get, set
    abstract member outlineLevelRow : int with get, set
    abstract member defaultRowHeight: int with get, set
    abstract member defaultColWidth : int with get, set
    abstract member dyDescent       : int with get, set

[<Erase>]
type WorksheetState =
/// make worksheet visible
| [<CompiledName("visible")>] Visible
/// make worksheet hidden
| [<CompiledName("hidden")>] Hidden
/// make worksheet hidden
| [<CompiledName("veryHidden")>] VeryHidden

type ITableRef =
    abstract member worksheet: Worksheet with get
    abstract member table: Table option with get
    abstract member ref: CellAdress with get, set
    abstract member name: string with get, set
    abstract member headerRow: bool with get, set
    abstract member displayName: string with get, set
    abstract member showFirstColumn: bool with get, set
    abstract member showLastColumn: bool with get, set

and Worksheet =
    abstract member id: int with get, set
    abstract member name: string with get, set
    abstract member state: WorksheetState with get, set
    abstract member properties: WorksheetProperties with get, set
    /// Get the last editable row in a worksheet (or undefined if there are none)
    abstract member lastRow: Row option with get
    abstract member lastColumn: Column option with get
    [<Emit("$0._lastRowNumber")>]
    abstract member lastRowNumber: int with get
    // Add column headers and define column keys and widths
    // Note: these column structures are a workbook-building convenience only,
    // apart from the column width, they will not be fully persisted.
    abstract member columns: Column [] with get, set
    [<Emit("$0._rows")>]
    abstract member rows: Row [] with get
    /// Not sure how this should be used
    abstract member tables: obj with get, set
    /// A count of the number of columns that have values.
    abstract member actualColumnCount: int with get
    /// The total column size of the document. Equal to the maximum cell count from all of the rows
    abstract member columnCount: int with get
    /// A count of the number of rows that have values. If a mid-document row is empty, it will not be included in the count.
    abstract member actualRowCount: int with get
    /// The total row size of the document. Equal to the row number of the last row that has values.
    abstract member rowCount: int with get
    /// add a table to a sheet
    abstract member addTable: Table -> ITableRef
    /// get a table from worksheet by name
    abstract member getTable: string -> ITableRef
    /// get all tables from worksheet
    abstract member getTables: unit -> ITableRef []
    /// remove table from worksheet by name
    abstract member removeTable: string -> unit
    /// Access an individual columns by key, letter and 1-based column number
    abstract member getColumn: string -> Column
    /// Access an individual columns by key, letter and 1-based column number
    abstract member getColumn: int -> Column
    abstract member addRow: CellValue [] -> Row
    /// Add a Row as `obj` with `column.key` as key and row value as value, after the last current row.
    /// `obj` can be anonymous record type or fable createObj.
    /// Use None to not set value for row for a given column or just skip.
    abstract member addRow: rowValues:RowValues -> Row
    /// obj can be any of the allowed inputs of `addRow`
    abstract member addRows: rowValuesArray:RowValues [] -> Row []
    /// insert new row at rowIndex and return as row object
    abstract member insertRow: rowIndex:int*RowValues -> Row
    /// insert new row with specified style at rowIndex and return as row object
    abstract member insertRow: rowIndex:int*RowValues*StyleOption -> Row
    /// insert an array of rows at rowIndex and return as row object array. Shifts all rows beginning from position by number of inserted rows
    abstract member insertRows: rowIndex:int*RowValues [] -> Row []
    /// insert an array of rows with specified style at rowIndex and return as row object array. Shifts all rows beginning from position by number of inserted rows
    abstract member insertRows: rowIndex:int*RowValues []*StyleOption -> Row []
    abstract member getRow: int -> Row
    abstract member getRows: int*int -> Row []
    /// Cut one or more rows (rows below are shifted up).
    /// Removed rows are replaced by nulls in worksheet.rows
    /// Known Issue: If a splice causes any merged cells to move, the results may be unpredictable.
    abstract member spliceRows: rowIndexStart:int*nRows:int -> unit
    /// Cut one or more rows and insert newRows instead.
    /// Known Issue: If a splice causes any merged cells to move, the results may be unpredictable.
    [<Emit("$0.spliceRows($1,$2,$3...)")>]
    abstract member spliceRows: rowIndexStart:int*nRows:int* [<ParamSeqAttribute>] newRows: CellValue [] [] -> unit
    /// Duplicate row at rowIndex
    [<Emit("$0.duplicateRow($1, 1, true)")>]
    abstract member duplicateRow: rowIndex:int -> unit
    /// Duplicate row `amount` times at rowIndex
    [<Emit("$0.duplicateRow($1, $2, true)")>]
    abstract member duplicateRow: rowIndex:int*amount:int -> unit
    /// Duplicate row at rowIndex, if
    /// `insert`: true if you want to insert new rows for the duplicates, or false if you want to replace them
    [<Emit("$0.duplicateRow($1, 1, $2)")>]
    abstract member duplicateRow: rowIndex:int*insert:bool -> unit
    /// Duplicate row `amount` times at rowIndex
    /// `insert`: true if you want to insert new rows for the duplicates, or false if you want to replace them
    abstract member duplicateRow: rowIndex:int*amount:int*insert:bool -> unit
    [<Emit("$0.eachRow(function(row, rowNumber){$1([row, rowNumber])})")>] //oh boy
    abstract member eachRow: func:(Row*int -> unit) -> unit
    [<Emit("$0.eachRow({ includeEmpty: $1 }, function(row, rowNumber){$2([row, rowNumber])})")>]
    abstract member eachRow: includeEmpty:bool*func:(Row*int -> unit) -> unit
    abstract member getCell: CellAdress -> Cell

