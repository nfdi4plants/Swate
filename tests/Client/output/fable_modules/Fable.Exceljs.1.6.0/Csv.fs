[<AutoOpen>]
module Fable.ExcelJs.Csv

type Csv =
    /// read from a file
    abstract member readFile: filename:string -> Async<unit>
    /// read from a stream
    abstract member read: filename:System.IO.Stream -> Async<unit>
    /// write to a file
    abstract member writeFile: filename:string -> Async<unit>
    /// write to a stream
    abstract member write: filename:System.IO.Stream * {|sheetName: string|} -> Async<unit>