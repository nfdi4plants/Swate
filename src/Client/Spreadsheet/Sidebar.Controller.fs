module Spreadsheet.Sidebar.Controller

open System.Collections.Generic
open Shared.TermTypes
open Shared.OfficeInteropTypes
open Spreadsheet
open Parser
open Types

/// <summary>This is the basic function to create new Tables from an array of InsertBuildingBlocks</summary>
let createAnnotationTable (name: string option) (insertBuildingBlocks: InsertBuildingBlock []) (state: Spreadsheet.Model) : Spreadsheet.Model =
    // calculate next index
    let newIndex = if Map.isEmpty state.Tables then 0 else state.Tables |> Map.maxKey |> (+) 1
    let swateBuildingBlocks = insertBuildingBlocks |> Array.mapi (fun i bb -> bb.toSwateBuildingBlock i)
    // parse to active table
    let activeTable = SwateBuildingBlock.toTableMap swateBuildingBlocks
    // add new table to tablemap
    let newTables = state.Tables.Add(newIndex, SwateTable.init(swateBuildingBlocks, ?name = name))
    let newTableOrder = state.TableOrder.Add(newIndex, newIndex)
    { state with
        Tables = newTables
        ActiveTableIndex = newIndex
        ActiveTable = activeTable
        TableOrder = newTableOrder
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

let private extendBuildingBlockToRowMax (rowMax: int) (bb: InsertBuildingBlock) =
    if bb.Rows.Length < rowMax then
        //e.g. 2 values, but 5 rows, but row index 0 is header, so rowMax index is 4, which means 5 items, but one header so -1 = 4
        let diff = rowMax - bb.Rows.Length 
        let rows = [|
            if bb.HasValues then yield! bb.Rows
            yield! Array.init diff (fun _ -> TermMinimal.empty)
        |]
        {bb with Rows = rows}
    else
        bb


let addBuildingBlock(insertBuildingBlock: InsertBuildingBlock) (state: Spreadsheet.Model) : Spreadsheet.Model =
    let table = state.ActiveTable
    let maxColKey, maxRowKey = table |> Map.maxKeys
    let nextColKey =
        // if cell is selected get column of selected cell we want to insert AFTER
        if not state.SelectedCells.IsEmpty then
            state.SelectedCells |> Set.toArray |> Array.head |> fst
        // if no cell selected insert at the end
        else
            maxColKey
        // add one to last column index OR to selected column index to append one to the right.
        |> (+) 1
    let swateBuildingBlock =
        insertBuildingBlock
        |> extendBuildingBlockToRowMax maxRowKey
        |> fun x -> x.toSwateBuildingBlock(nextColKey)
    let existing =
        let l = SwateBuildingBlock.ofTableMap_list table
        // if insert is not at the end, reindex all columns with higher index.
        if nextColKey <> maxColKey + 1 then
            l |> List.map (fun sb ->
                if sb.Index >= nextColKey then {sb with Index = sb.Index + 1} else sb
            )  
        else
            l
    let nextTable = swateBuildingBlock::existing |> SwateBuildingBlock.toTableMap
    let nextState = {
        state with ActiveTable = nextTable
    }
    nextState

let insertTerm (term:TermMinimal) (state: Spreadsheet.Model) : Spreadsheet.Model =
    let table = state.ActiveTable
    /// Filter out header row
    let selected = state.SelectedCells |> Set.toArray
    /// Make sure only one column is selected
    let isOneColumn =
        let columnIndex = fst selected.[0] // can just use head of selected cells as all must be same column
        selected |> Array.forall (fun x -> fst x = columnIndex)
    if not isOneColumn then failwith "Can only paste term in one column at a time!"
    /// Make sure either one column header is selected or only body cells are selected
    let onlyOrNoHeader =
        let hasHeader = selected |> Array.exists (fun (_,r) -> r = 0)
        if hasHeader then selected.Length = 1 else true
    if not onlyOrNoHeader then failwith "Can only paste term in header or body at a time!"
    let nextActiveTable = table |> Map.map (fun key cell ->
        let isSelected = Array.contains key selected
        match isSelected, cell with
        | false, _ -> cell
        | true, IsTerm t_cell -> IsTerm {t_cell with Term = term}
        | true, IsUnit u_cell -> IsUnit {u_cell with Unit = term}
        | true, IsFreetext f_cell -> IsFreetext {f_cell with Value = term.Name}
        // only update header if header is term column
        | true, IsHeader header -> 
            if header.isTermColumn then
                {header with Term = Some term} |> IsHeader
            else
                cell
    )
    let nextState = { state with ActiveTable = nextActiveTable }
    nextState