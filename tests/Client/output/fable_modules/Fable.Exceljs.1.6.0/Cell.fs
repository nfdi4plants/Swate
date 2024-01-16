[<AutoOpen>]
module Fable.ExcelJs.Cell

open Fable.Core
open Fable.Core.JsInterop
open Fable.ExcelJs

[<AllowNullLiteral>]
type Cell =
    abstract member value: CellValue with get, set
    /// use string value of cell
    abstract member text: string with get
    /// use html-safe string for rendering...
    /// const html = '<div>' + cell.html + '</div>';
    abstract member html: string with get
    [<Emit("$0.type")>]
    abstract member ``type``: int with get
    abstract member numFmt: string with get, set
    abstract member address: string with get
    /// assign (or get) a name for a cell (will overwrite any other names that cell had)
    abstract member name: string with get, set
    /// assign (or get) an array of names for a cell (cells can have more than one name)
    abstract member names: string [] with get, set
    /// remove a name from a cell
    abstract member removeName: string -> unit
    abstract member isMerged: bool with get
    abstract member formula: string with get
    abstract member result: obj with get
    abstract member col: int with get
    abstract member row: int with get
    //abstract member workbook: Workbook.Workbook with get
    //abstract member Worksheet: Worksheet with get