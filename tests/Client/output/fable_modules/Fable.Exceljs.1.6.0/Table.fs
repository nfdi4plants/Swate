[<AutoOpen>]
module Fable.ExcelJs.Table

open Fable.Core
open Fable.Core.JsInterop
open Fable.ExcelJs
open Cell

[<RequireQualifiedAccess>]
type TotalsFunctions =
/// Default
| [<CompiledName("Total")>] Total
/// No totals function for this column
| [<CompiledName("none")>] None
/// Compute average for the column
| [<CompiledName("average")>] Average
/// Count the entries that are numbers
| [<CompiledName("countNums")>] CountNums
/// Count of entries
| [<CompiledName("count")>] Count
/// The maximum value in this column
| [<CompiledName("max")>] Max
/// The minimum value in this column
| [<CompiledName("min")>] Min
/// The standard deviation for this column
| [<CompiledName("stdDev")>] StdDev
/// The variance for this column
| [<CompiledName("var")>] Var
/// The sum of entries for this column
| [<CompiledName("sum")>] Sum
/// A custom formula. Requires an associated totalsRowFormula value.
| [<CompiledName("custom")>] Custom

with
    static member defaultValue = Total

// https://fable.io/docs/javascript/features.html#paramobject
[<AllowNullLiteral>]
[<Global>]
type TableColumn
    [<ParamObject; Emit("$0")>]
    (   
        name: string, 
        ?filterButton: bool, 
        ?totalsRowLabel: TotalsFunctions, 
        ?totalsRowFunction: string, 
        ?totalsRowFormula: obj
    ) =
    member val name: string = jsNative with get, set
    member val filterButton: bool = jsNative with get, set
    member val totalsRowLabel: TotalsFunctions = jsNative with get, set
    member val totalsRowFunction: string = jsNative with get, set
    member val totalsRowFormula: obj = jsNative with get, set

[<AllowNullLiteral>]
[<Global>]
type Table
    [<ParamObject; Emit("$0")>]
    (   name: string, 
        ref: CellAdress, 
        columns: TableColumn [], 
        rows: RowValues [] [], 
        ?displayName: string, 
        ?headerRow: bool, 
        ?totalsRow: bool, 
        ?style: obj
    ) =
    /// The name of the table
    member val name: string = jsNative with get, set
    /// The display name of the table.
    member val displayName: string = jsNative with get, set
    /// Top left cell of the table
    member val ref: CellAdress = jsNative with get, set
    /// Show headers at top of table
    member val headerRow: bool = jsNative with get, set
    /// Show totals at bottom of table
    member val totalsRow: bool = jsNative with get, set
     /// More style properties
    member val style: obj = jsNative with get, set
    /// columns
    member val columns: TableColumn [] = jsNative with get, set
    /// rows
    member val rows: RowValues [] [] = jsNative with get, set
    /// full range of table. Exmp "A1:D4"
    member val tableRef: CellRange = jsNative with get