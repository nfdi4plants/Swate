[<AutoOpen>]
module Fable.ExcelJs.Extensions

open Fable.Core
open Fable.Core.JsInterop

type Row with
    /// create js object with `column.key` as key and row value as value.
    /// Can be used e.g for insertRow, addRow.
    static member createValues(keyValueSeq:(string*obj) seq) =
        !!createObj [
            for (columnKey, rowValue) in keyValueSeq do
                yield
                    columnKey ==> rowValue
        ]

type Column with
    static member create (header: string, ?key: string, ?width: int, ?outlineLevel: int, ?hidden: bool) : Column =
        !!createObj [
            "header", box header
            if key.IsSome then "key", box key.Value
            if width.IsSome then "width", box width.Value
            if outlineLevel.IsSome then "outlineLevel", box outlineLevel.Value
            if hidden.IsSome then "hidden", box hidden.Value
        ]

type WorksheetProperties with
    static member create(?tabColor: obj, ?outlineLevelCol: int, ?outlineLevelRow: int, ?defaultRowHeight: int, ?defaultColWidth: int, ?dyDescent: int) = 
        !!createObj [
            if tabColor.IsSome then "tabColor", box tabColor.Value
            if outlineLevelCol.IsSome then "outlineLevelCol", box outlineLevelCol.Value
            if outlineLevelRow.IsSome then "outlineLevelRow", box outlineLevelRow.Value
            if defaultRowHeight.IsSome then "defaultRowHeight", box defaultRowHeight.Value
            if defaultColWidth.IsSome then "defaultColWidth", box defaultColWidth.Value
            if dyDescent.IsSome then "dyDescent", box dyDescent.Value
        ]