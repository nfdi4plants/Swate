[<AutoOpen>]
module Fable.ExcelJs.Column

open Fable.Core
open Fable.Core.JsInterop
open Fable.ExcelJs
open Cell

[<AllowNullLiteral>]
type Column =
    abstract member header: string with get, set
    abstract member key: string with get, set
    abstract member width: int with get, set
    abstract member hidden: bool with get, set
    abstract member outlineLevel: int with get, set
    abstract member collapsed: bool with get
    abstract member number: int with get
    abstract member values: CellValue [] with get, set
    abstract member letter: string with get
    /// iterate over all current cells in this column
    [<Emit("$0.eachCell(function(cell, columnIndex){$1([cell, columnIndex])})")>] //oh boy
    abstract member eachCell: func:(Cell*int -> unit) -> unit
    [<Emit("$0.eachCell({ includeEmpty: $1 }, function(cell, columnIndex){$2([cell, columnIndex])})")>]
    abstract member eachCell: includeEmpty:bool*func:(Cell*int -> unit) -> unit