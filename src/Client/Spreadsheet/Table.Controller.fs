module Spreadsheet.Table.Controller

open System.Collections.Generic
open Shared.TermTypes
open Shared.OfficeInteropTypes
open Spreadsheet
open Types
open ARCtrl.ISA
open Shared

module ControllerTableAux =

    let findEarlierTable (tableIndex:int) (tables: ArcTables) =
        let indices = [ 0 .. tables.TableCount-1 ]
        let lower = indices |> Seq.tryFindBack (fun k -> k < tableIndex)
        Option.map (fun i -> i, tables.GetTableAt i) lower

    let findLaterTable (tableIndex:int) (tables: ArcTables) =
        let indices = [ 0 .. tables.TableCount-1 ]
        let higher = indices |> Seq.tryFind (fun k -> k > tableIndex)
        Option.map (fun i -> i, tables.GetTableAt i) higher

    let findNeighborTables (tableIndex:int) (tables: ArcTables) =
        findEarlierTable tableIndex tables, findLaterTable tableIndex tables

open ControllerTableAux

let updateTableOrder (prevIndex:int, newIndex:int) (state:Spreadsheet.Model) =
    state.Tables.MoveTable(prevIndex, newIndex)
    {state with ArcFile = state.ArcFile}

let resetTableState () : LocalHistory.Model * Spreadsheet.Model =
    LocalHistory.Model.init().ResetAll(),
    Spreadsheet.Model.init()

let renameTable (tableIndex:int) (newName: string) (state: Spreadsheet.Model) : Spreadsheet.Model =
    state.Tables.RenameTableAt(tableIndex,newName)
    {state with ArcFile = state.ArcFile}

let removeTable (removeIndex: int) (state: Spreadsheet.Model) : Spreadsheet.Model =
    if state.Tables.TableCount = 0 then 
        state
    else
        state.Tables.RemoveTableAt removeIndex
        // if active table is removed get the next closest table and set it active
        match state.ActiveView with
        | ActiveView.Table i when i = removeIndex ->
            let nextView =
                let neighbors = findNeighborTables removeIndex state.Tables
                match neighbors with
                | Some (i, _), _ -> ActiveView.Table i
                | None, Some (i, _) -> ActiveView.Table i
                // This is a fallback option, which should never be hit
                | _ -> ActiveView.Metadata
            { state with
                ArcFile = state.ArcFile
                ActiveView = nextView }
        | ActiveView.Table i -> // Tables still exist and an inactive one was removed. Just remove it.
            let nextTable_Index = if i > removeIndex then i - 1 else i
            { state with
                ArcFile = state.ArcFile
                ActiveView = ActiveView.Table nextTable_Index }
        | _ -> state

///<summary>Add `n` rows to active table.</summary>
let addRows (n: int) (state: Spreadsheet.Model) : Spreadsheet.Model =
    state.ActiveTable.AddRowsEmpty(n)
    {state with ArcFile = state.ArcFile}

let deleteRow (index: int) (state: Spreadsheet.Model) : Spreadsheet.Model =
    state.ActiveTable.RemoveRow index
    {state with
        ArcFile = state.ArcFile
        SelectedCells = Set.empty }

let deleteRows (indexArr: int []) (state: Spreadsheet.Model) : Spreadsheet.Model =
    state.ActiveTable.RemoveRows indexArr
    {state with
        ArcFile = state.ArcFile
        SelectedCells = Set.empty }
    

let deleteColumn (index: int) (state: Spreadsheet.Model) : Spreadsheet.Model =
    state.ActiveTable.RemoveColumn index
    {state with
        ArcFile = state.ArcFile
        SelectedCells = Set.empty}

let fillColumnWithCell (index: int*int) (state: Spreadsheet.Model) : Spreadsheet.Model =
    let cell = state.ActiveTable.TryGetCellAt index
    let columnIndex = fst index
    state.ActiveTable.IteriColumns(fun i column ->
        let cell = cell|> Option.defaultValue (column.GetDefaultEmptyCell())
        if i = columnIndex then
            for cellRowIndex in 0 .. column.Cells.Length-1 do
                state.ActiveTable.UpdateCellAt(columnIndex, cellRowIndex, cell)
    )
    {state with ArcFile = state.ArcFile}

// Ui depends on main column name, maybe change this to depends on BuildingBlockType?
// Header main column name must be updated

//let editColumn (columnIndex: int, newType: SwateCell, b_type: BuildingBlockType option)  (state: Spreadsheet.Model) : Spreadsheet.Model =
//    let table = state.ActiveTable
//    let updateHeader (header: SwateCell) =
//        match newType with
//        | IsUnit _ -> header.toUnitHeader(?b_type = b_type)
//        | IsTerm _ -> header.toTermHeader(?b_type = b_type)
//        | IsFreetext _ -> header.toFreetextHeader(?b_type = b_type)
//        | IsHeader _ -> failwith "This is no viable input."
//        |> IsHeader
//    let updateBody (cell: SwateCell) =
//        match newType with
//        | IsUnit _ -> cell.toUnitCell()
//        | IsTerm _ -> cell.toTermCell()
//        | IsFreetext _ -> cell.toFreetextCell()
//        | IsHeader _ -> failwith "This is no viable input."
//    let nextTable =
//        table
//        |> Map.map (fun (c,r) cv ->
//            match r with
//            | 0 when c = columnIndex -> updateHeader cv
//            | _ when c = columnIndex -> updateBody cv
//            | _ -> cv
//        )
//    {state with ActiveTable = nextTable}