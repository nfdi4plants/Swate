[<AutoOpen>]
module Fable.ExcelJs.Row

open Fable.Core
open Fable.Core.JsInterop
open Fable.ExcelJs
open Cell


[<AllowNullLiteral>]
type Row =
    [<Emit("$0.cells")>]
    abstract member cells: Cell [] with get
    abstract member height: float with get, set
    abstract member hidden: bool with get, set
    abstract member outlineLevel: int with get, set
    abstract member collapsed: bool with get
    /// Return 0 index always as None, as xlsx rows start 1 indexed. Set will always set starting at index 1.
    abstract member values: CellValue [] with get, set
    abstract member number: int with get
    abstract member cellCount: int with get
    abstract member actualCellCount: int with get
    abstract member hasValues: bool with get
    /// Get cell by row 1 based index
    abstract member getCell: int -> Cell
    /// Get cell by column header or by column letter index
    abstract member getCell: string -> Cell
    /// iterate over all current cells in this column
    [<Emit("$0.eachCell(function(cell, rowIndex){$1([cell, rowIndex])})")>] //oh boy
    abstract member eachCell: func:(Cell*int -> unit) -> unit
    [<Emit("$0.eachCell({ includeEmpty: $1 }, function(cell, rowIndex){$2([cell, rowIndex])})")>]
    abstract member eachCell: includeEmpty:bool*func:(Cell*int -> unit) -> unit
    /// Commit a completed row to stream
    abstract member commit: unit -> unit