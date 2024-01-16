namespace ExcelJS.Fable

open Fable.Core
open Fable.Core.JsInterop

module GlobalBindings =

    open ExcelJS.Fable

    [<Global>]
    let Office : Office.IExports = jsNative


    [<Global>]
    //[<CompiledName("Office.Excel")>]
    let Excel : Excel.IExports = jsNative

    [<Global>]
    let ExcelRangeLoadOptions : Excel.Interfaces.RangeLoadOptions = jsNative