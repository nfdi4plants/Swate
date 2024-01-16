module ARCtrl.ISA.Spreadsheet.CompositeColumn

open ARCtrl.ISA
open FsSpreadsheet

/// Checks if the column header is a deprecated IO Header. If so, fixes it.
///
/// The old format of IO Headers was only the type of IO so, e.g. "Source Name" or "Raw Data File".
///
/// A "Source Name" column will now be mapped to the propper "Input [Source Name]", and all other IO types will be mapped to "Output [<IO Type>]".
let fixDeprecatedIOHeader (col : FsColumn) = 
    match IOType.ofString (col.[1].ValueAsString()) with
    | IOType.FreeText _ -> col
    | IOType.Source -> 
        let comp = CompositeHeader.Input (IOType.Source)       
        col.[1].SetValueAs(comp.ToString())
        col
    | ioType ->
        let comp = CompositeHeader.Output (ioType)       
        col.[1].SetValueAs(comp.ToString())
        col

let fromFsColumns (columns : list<FsColumn>) : CompositeColumn =
    let header = 
        columns
        |> List.map (fun c -> c.[1])
        |> CompositeHeader.fromFsCells
    let l = columns.[0].RangeAddress.LastAddress.RowNumber
    let cells = 
        [|
        for i = 2 to l do
            columns
            |> List.map (fun c -> c.[i])
            |> CompositeCell.fromFsCells
        |]                 
    CompositeColumn.create(header,cells)


let toFsColumns (column : CompositeColumn) : FsCell list list =
    let hasUnit = column.Cells |> Seq.exists (fun c -> c.isUnitized)
    let isTerm = column.Header.IsTermColumn
    let header = CompositeHeader.toFsCells hasUnit column.Header
    let cells = column.Cells |> Array.map (CompositeCell.toFsCells isTerm hasUnit)
    if hasUnit then
        [
            [header.[0]; for i = 0 to column.Cells.Length - 1 do cells.[i].[0]]
            [header.[1]; for i = 0 to column.Cells.Length - 1 do cells.[i].[1]]
            [header.[2]; for i = 0 to column.Cells.Length - 1 do cells.[i].[2]]
            [header.[3]; for i = 0 to column.Cells.Length - 1 do cells.[i].[3]]
        ]
    elif isTerm then
        [
            [header.[0]; for i = 0 to column.Cells.Length - 1 do cells.[i].[0]]
            [header.[1]; for i = 0 to column.Cells.Length - 1 do cells.[i].[1]]
            [header.[2]; for i = 0 to column.Cells.Length - 1 do cells.[i].[2]]
        ]
    else
        [
            [header.[0]; for i = 0 to column.Cells.Length - 1 do cells.[i].[0]]
        ]