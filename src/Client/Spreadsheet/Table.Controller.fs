module Spreadsheet.Table.Controller

open System.Collections.Generic
open Shared.TermTypes
open Shared.OfficeInteropTypes
open Spreadsheet
open TypeConverter
open Types
open Helper

/// <summary>This function is used to save the active table to the tables map. is only executed if tables map is not empty.</summary>
let saveActiveTable (state: Spreadsheet.Model) : Spreadsheet.Model =
    if Map.isEmpty state.Tables then
        state
    else
        let parsed_activeTable = state.ActiveTable |> SwateBuildingBlock.ofTableMap
        let nextTable =
            let t = state.Tables.[state.ActiveTableIndex]
            {t with BuildingBlocks = parsed_activeTable}
        let nextTables = state.Tables.Change(state.ActiveTableIndex, fun _ -> Some nextTable)
        {state with Tables = nextTables}

let updateTableOrder (prevIndex:int, newIndex:int) (state:Spreadsheet.Model) =
    let m = state.TableOrder
    let tableOrder =
        m
        |> Map.toSeq
        |> Seq.map (fun (id, order) ->
            // the element to switch
            if order = prevIndex then 
                id, newIndex
            // keep value if smaller then newIndex
            elif order < newIndex then
                id, order
            elif order >= newIndex then
                id, order + 1
            else
                failwith "this should never happen"
        )
        |> Seq.sortBy snd
        // rebase order, this prevents ordering above "+" symbol in footer with System.Int32.MaxValue
        |> Seq.mapi (fun i (id, _) -> id, i)
        |> Map.ofSeq
    { state with TableOrder = tableOrder }

let resetTableState() : Spreadsheet.Model =
    Spreadsheet.LocalStorage.resetAll()
    Spreadsheet.Model.init()

let renameTable (tableIndex:int) (newName: string) (state: Spreadsheet.Model) : Spreadsheet.Model =
    let isNotUnique = state.Tables.Values |> Seq.map (fun x -> x.Name) |> Seq.contains newName
    if isNotUnique then failwith "Table names must be unique"
    let nextTable = { state.Tables.[tableIndex] with Name = newName }
    let nextState = {state with Tables = state.Tables.Change(tableIndex, fun _ -> Some nextTable)}
    nextState

let removeTable (removeIndex: int) (state: Spreadsheet.Model) : Spreadsheet.Model =
    let nextTables = state.Tables.Remove(removeIndex)
    // If the only existing table was removed init model from beginning
    if nextTables = Map.empty then
        Spreadsheet.Model.init()
    else
        // if active table is removed get the next closest table and set it active
        if state.ActiveTableIndex = removeIndex then
            let nextTable_Index =
                let neighbors = findNeighborTables removeIndex nextTables
                match neighbors with
                | Some (i, _), _ -> i
                | None, Some (i, _) -> i
                // This is a fallback option
                | _ -> nextTables.Keys |> Seq.head
            let nextTable = state.Tables.[nextTable_Index].BuildingBlocks |> SwateBuildingBlock.toTableMap
            { state with
                ActiveTableIndex = nextTable_Index
                Tables = nextTables
                ActiveTable = nextTable }
        // Tables still exist and an inactive one was removed. Just remove it.
        else
            { state with Tables = nextTables }

///<summary>Add `n` rows to active table.</summary>
let addRows (n: int) (state: Spreadsheet.Model) : Spreadsheet.Model =
    let keys = state.ActiveTable.Keys
    let maxRow = keys |> Seq.maxBy snd |> snd
    let maxCol = keys |> Seq.maxBy fst |> fst
    /// create new keys to add to active table
    let newKeys = [
        // iterate over all columns
        for c in 0 .. maxCol do
            // then create for EACH COLUMN and EACH ROW ABOVE maxRow UNTIL maxRow + number of new rows
            for r in (maxRow + 1) .. (maxRow + n) do
                yield c, r
    ]
    /// This MUST be 0, so no overlap between existing keys and new keys exists.
    /// This is important as Map.add would replace previous values on that key.
    let checkNewKeys =
        let keySet = keys |> Set.ofSeq
        let newKeysSet = newKeys |> Set.ofList
        Set.intersect keySet newKeysSet
        |> Set.count
    if checkNewKeys <> 0 then failwith "Error in `addRows` function. Unable to add new rows without replacing existing values. Please contact us with a bug report."
    let nextActiveTable =
        let prev = state.ActiveTable |> Map.toList
        let cellTypes =
            prev
            // remove header and row information
            |> List.choose (fun (index,v) -> if snd index <> 0 then Some (fst index, v) else None)
            |> List.distinctBy (fun (column,_) -> column) // get one cell for each column
            |> Map.ofList
        let next =
            newKeys
            |> List.map (fun x ->
                x, SwateCell.emptyOfCell cellTypes.[fst x]
            )
        prev@next
        |> Map.ofList
    let nextState = {state with ActiveTable = nextActiveTable}
    nextState

let deleteRow (index: int) (state: Spreadsheet.Model) : Spreadsheet.Model =
    let nextTable = 
        state.ActiveTable
        |> Map.toArray
        |> Array.filter (fun ((_,r),_) -> r <> index)
        |> Array.map (fun ((c,r),cvalue) ->
            let updatedIndex = if r > index then r-1 else r
            (c,updatedIndex), cvalue
        )
        |> Map.ofArray
    {state with ActiveTable = nextTable; SelectedCells = Set.empty }

let deleteRows (indexArr: int []) (state: Spreadsheet.Model) : Spreadsheet.Model =
    let filter (table:Map<int*int,SwateCell>) =
        table
        |> Map.toArray
        |> Array.filter (fun ((_,r),_) -> Array.contains r indexArr |> not)
    let mutable rowIndex = 0
    let reindex (mapArr:((int*int)*SwateCell) []) =
        mapArr
        |> Array.groupBy (fun ((c,r),v) -> r)
        |> Array.sortBy fst
        |> Array.collect (fun (_,arr) ->
            let nextRowIndex = rowIndex
            rowIndex <- rowIndex + 1
            arr |> Array.map (fun ((c,_),v) -> (c,nextRowIndex),v)
        )
    let nextTable =
        state.ActiveTable
        |> filter
        |> reindex
        |> Map.ofArray
    {state with ActiveTable = nextTable; SelectedCells = Set.empty }
    

let deleteColumn (index: int) (state: Spreadsheet.Model) : Spreadsheet.Model =
    let nextTable = 
        state.ActiveTable
        |> Map.toArray
        |> Array.filter (fun ((c,_),_) -> c <> index)
        |> Array.map (fun ((c,r),cvalue) ->
            let updateIndex = if c > index then c-1 else c
            (updateIndex,r), cvalue
        )
        |> Map.ofArray
    {state with ActiveTable = nextTable; SelectedCells = Set.empty}

let mutable clipboardCell: SwateCell option = None

let copyCell (index: int*int) (state: Spreadsheet.Model) : Spreadsheet.Model =
    let cell = state.ActiveTable.[index]
    clipboardCell <- Some cell
    state

let copySelectedCell (state: Spreadsheet.Model) : Spreadsheet.Model =
    /// Array.head is used until multiple cells are supported, should this ever be intended
    let index = state.SelectedCells |> Set.toArray |> Array.min
    copyCell index state

let cutCell (index: int*int) (state: Spreadsheet.Model) : Spreadsheet.Model =
    let cell = state.ActiveTable.[index]
    // Remove selected cell value
    let emptyCell = SwateCell.emptyOfCell cell
    let nextActiveTable = state.ActiveTable.Add(index, emptyCell)
    let nextState = {state with ActiveTable = nextActiveTable}
    clipboardCell <- Some cell
    nextState

let cutSelectedCell (state: Spreadsheet.Model) : Spreadsheet.Model =
    /// Array.head is used until multiple cells are supported, should this ever be intended
    let index = state.SelectedCells |> Set.toArray |> Array.min
    cutCell index state

let pasteCell (index: int*int) (state: Spreadsheet.Model) : Spreadsheet.Model =
    let targetCell = state.ActiveTable.[index]
    let table = state.ActiveTable
    match clipboardCell, targetCell with
    // Don't update if no cell in saved
    | None, _ -> state
     // Don't update if source cell and target cell are not of same type
    | Some (IsTerm _), IsTerm _ ->
        let nextTable = table.Add(index, clipboardCell.Value)
        {state with ActiveTable = nextTable}
    | Some (IsFreetext _), IsFreetext _ ->
        let nextTable = table.Add(index, clipboardCell.Value)
        {state with ActiveTable = nextTable}
    | Some (IsUnit _), IsUnit _ ->
        let nextTable = table.Add(index, clipboardCell.Value)
        {state with ActiveTable = nextTable}
    | Some (IsHeader _), IsHeader _ ->
        let nextTable = table.Add(index, clipboardCell.Value)
        {state with ActiveTable = nextTable}
    | _,_ -> state

let pasteSelectedCell (state: Spreadsheet.Model) : Spreadsheet.Model =
    if state.SelectedCells.IsEmpty then
        state
    else
        // TODO:
        //let arr = state.SelectedCells |> Set.toArray
        //let isOneColumn =
        //    let c = fst arr.[0] // can just use head of selected cells as all must be same column
        //    arr |> Array.forall (fun x -> fst x = c)
        //if not isOneColumn then failwith "Can only paste cells in one column at a time!"
        let minIndex = state.SelectedCells |> Set.toArray |> Array.min
        pasteCell minIndex state

let fillColumnWithTerm (index: int*int) (state: Spreadsheet.Model) : Spreadsheet.Model =
    let column = fst index
    let term = state.ActiveTable.[index]
    let nextActiveTable =
        state.ActiveTable
        |> Map.map (fun (c,r) v ->
            // change term value in same column but NOT in header
            if c = column && r <> 0 then
                term
            else
                v
        )
    let nextState = {state with ActiveTable = nextActiveTable}
    nextState

/// Ui depends on main column name, maybe change this to depends on BuildingBlockType?
/// Header main column name must be updated

let editColumn (columnIndex: int, newType: SwateCell, b_type: BuildingBlockType option)  (state: Spreadsheet.Model) : Spreadsheet.Model =
    let table = state.ActiveTable
    let updateHeader (header: SwateCell) =
        match newType with
        | IsUnit _ -> header.toUnitHeader(?b_type = b_type)
        | IsTerm _ -> header.toTermHeader(?b_type = b_type)
        | IsFreetext _ -> header.toFreetextHeader(?b_type = b_type)
        | IsHeader _ -> failwith "This is no viable input."
        |> IsHeader
    let updateBody (cell: SwateCell) =
        match newType with
        | IsUnit _ -> cell.toUnitCell()
        | IsTerm _ -> cell.toTermCell()
        | IsFreetext _ -> cell.toFreetextCell()
        | IsHeader _ -> failwith "This is no viable input."
    let nextTable =
        table
        |> Map.map (fun (c,r) cv ->
            match r with
            | 0 when c = columnIndex -> updateHeader cv
            | _ when c = columnIndex -> updateBody cv
            | _ -> cv
        )
    {state with ActiveTable = nextTable}