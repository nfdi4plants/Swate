module Spreadsheet.Helper

open System.Collections.Generic
open Shared.TermTypes
open Shared.OfficeInteropTypes
open Spreadsheet

let findEarlierTable (tableIndex:int) (tables: Map<int,Spreadsheet.SwateTable>) =
    let keys = tables.Keys
    let lower = keys |> Seq.tryFindBack (fun k -> k < tableIndex)
    Option.map (fun i -> i, tables.[i]) lower

let findLaterTable (tableIndex:int) (tables: Map<int,Spreadsheet.SwateTable>) =
    let keys = tables.Keys
    let higher = keys |> Seq.tryFind (fun k -> k > tableIndex)
    Option.map (fun i -> i, tables.[i]) higher

let findNeighborTables (tableIndex:int) (tables: Map<int,Spreadsheet.SwateTable>) =
    findEarlierTable tableIndex tables, findLaterTable tableIndex tables

let extendBuildingBlockToRowMax (rowMax: int) (bb: SwateBuildingBlock) =
    if bb.Rows.Length < rowMax then
        //e.g. 2 values, but 5 rows, but row index 0 is header, so rowMax index is 4, which means 5 items, but one header so -1 = 4
        let diff = rowMax - bb.Rows.Length 
        let rows = [|
            yield! bb.Rows
            yield! Array.init diff (fun i ->
                let index = i + bb.Rows.Length + 1 //i = 0..diff, +1 to adjust for header, +bb.Rows.Length to add on existing rows.
                let extendRows =
                    if bb.Rows <> Array.empty then
                        (Array.last >> snd) bb.Rows
                    else
                        bb.Header.getEmptyBodyCell
                index, extendRows
            )
        |]
        {bb with Rows = rows}
    else
        bb