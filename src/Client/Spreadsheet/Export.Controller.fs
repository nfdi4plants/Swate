module Spreadsheet.Export.Controller

open Shared.TermTypes
open Shared.OfficeInteropTypes
open Spreadsheet
open Spreadsheet.Table
open TypeConverter
open Types
open Helper

///<summary> Return active table.</summary>
let getTable (state: Spreadsheet.Model) =
    let name = state.Tables.[state.ActiveTableIndex].Name
    let swate_bbs = SwateBuildingBlock.ofTableMap state.ActiveTable
    let bbs = swate_bbs |> Array.map (fun x -> x.toBuildingBlock)
    name, bbs

///<summary> Returns all tables.</summary>
let getTables (state: Spreadsheet.Model) =
    let state = Controller.saveActiveTable state
    let tables =
        state.Tables.Values
        |> Array.ofSeq
        |> Array.map (fun t ->
            t.Name,
            t.BuildingBlocks
            |> Array.map (fun bb -> bb.toBuildingBlock)
        )
    tables
