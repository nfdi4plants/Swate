[<AutoOpen>]
module Fable.ExcelJs.Workbook

open Fable.Core
open Fable.Core.JsInterop
open Fable.ExcelJs
open Worksheet
open Xlsx

type Workbook =
    abstract member creator : string with get, set
    abstract member lastModifiedBy : string with get, set
    abstract member created : System.DateTime with get, set
    abstract member modified : System.DateTime with get, set
    abstract member lastPrinted : System.DateTime with get, set
    abstract member description : string with get, set
    abstract member keywords : string with get, set
    abstract member manager : string with get, set
    abstract member category : string with get, set
    abstract member company: string with get, set
    abstract member xlsx: Xlsx with get
    abstract member addWorksheet: string -> Worksheet
    [<Emit("$0.addWorksheet($1,{properties: $2})")>]
    abstract member addWorksheet: string * WorksheetProperties -> Worksheet
    /// Remove the worksheet using worksheet id
    abstract member removeWorksheet: int -> unit
    /// fetch sheet by name
    abstract member getWorksheet: string -> Worksheet
    /// fetch sheet by id.
    /// INFO: Be careful when using it!
    /// It tries to access to `worksheet.id` field. Sometimes (really very often) workbook has worksheets with id not starting from 1.
    /// For instance It happens when any worksheet has been deleted.
    /// It's much more safety when you assume that ids are random. And stop to use this function.
    abstract member getWorksheet: int -> Worksheet
    abstract member worksheets: Worksheet [] with get
    [<Emit("$0.eachSheet(function(worksheet, sheetId){$1([worksheet, sheetId])})")>] //oh boy
    abstract member eachSheet: func:(Worksheet*int -> unit) -> unit