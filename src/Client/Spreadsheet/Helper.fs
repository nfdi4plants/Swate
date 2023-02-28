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