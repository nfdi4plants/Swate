[<AutoOpen>]
module Fable.ExcelJs.Xlsx

open Fable.Core.JS

type Xlsx =
    /// read from a file
    abstract member readFile: filename:string -> Promise<unit>
    /// read from a stream
    abstract member read: filename:System.IO.Stream -> Promise<unit>
    /// load from a buffer
    abstract member load: filename:obj -> Promise<unit>
    /// write to a file
    abstract member writeFile: filename:string -> Promise<unit>
    /// write to a stream
    abstract member write: filename:System.IO.Stream -> Promise<unit>
    /// write to a new buffer
    abstract member writeBuffer: unit -> Promise<obj>