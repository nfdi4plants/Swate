module Spreadsheet.Controller

open System.Collections.Generic
open Shared.TermTypes
open Shared.OfficeInteropTypes
open Spreadsheet
open Parser

module Map =
    let maxKeyValue (m:Map<'Key,'Value>) =
        m.Keys |> Seq.max

/// <summary>This function is used to save the active table to the tables map. is only executed if tables map is not empty.</summary>
let saveActiveTable (state: Spreadsheet.Model) : Spreadsheet.Model =
    if Map.isEmpty state.Tables then
        state
    else
        printfn "save table!"
        let parsed_activeTable = state.ActiveTable |> SwateBuildingBlock.ofTableMap
        let nextTable =
            let t = state.Tables.[state.ActiveTableIndex]
            {t with BuildingBlocks = parsed_activeTable}
        let nextTables = state.Tables.Change(state.ActiveTableIndex, fun _ -> Some nextTable)
        {state with Tables = nextTables}

/// <summary>This is the basic function to create new Tables from an array of InsertBuildingBlocks</summary>
let createAnnotationTable (name: string option) (insertBuildingBlocks: InsertBuildingBlock []) (state: Spreadsheet.Model) : Spreadsheet.Model =
    // calculate next index
    let newIndex = if Map.isEmpty state.Tables then 0 else state.Tables |> Map.maxKeyValue |> (+) 1
    let swateBuildingBlocks = insertBuildingBlocks |> Array.mapi (fun i bb -> bb.toSwateBuildingBlock i)
    // parse to active table
    let activeTable = SwateBuildingBlock.toTableMap swateBuildingBlocks
    // add new table to tablemap
    let newTables = state.Tables.Add(newIndex, SwateTable.init(swateBuildingBlocks, ?name = name))
    { state with
        Tables = newTables
        ActiveTableIndex = newIndex
        ActiveTable = activeTable
    }

/// <summary>Adds the most basic Swate table consisting of Input column "Source Name" and output column "Sample Name".</summary>
let createAnnotationTable_new (state: Spreadsheet.Model) : Spreadsheet.Model =
    // create empty rows
    let rows =
        let n_rows = 1
        Array.init n_rows (fun _ -> TermMinimal.empty)
    // create source column
    let source =
        let blueprint = BuildingBlockNamePrePrint.init(BuildingBlockType.Source)
        InsertBuildingBlock.create blueprint None None rows
    // create sample column
    let sample =
        let blueprint = BuildingBlockNamePrePrint.init(BuildingBlockType.Sample)
        InsertBuildingBlock.create blueprint None None rows
    // parse to SwateBuildingBlocks
    let insertBuildingBlocks = [|source; sample|]
    let name = HumanReadableIds.tableName()
    createAnnotationTable (Some name) insertBuildingBlocks state

let findNeighborTables (tableIndex:int) (tables: Map<int,Spreadsheet.SwateTable>) =
    let keys = tables.Keys
    let lower = keys |> Seq.tryFindBack (fun k -> k < tableIndex)
    let higher = keys |> Seq.tryFind (fun k -> k > tableIndex)
    Option.map (fun i -> i, tables.[i]) lower,
    Option.map (fun i -> i, tables.[i]) higher