module Spreadsheet.Sidebar.Controller

open System.Collections.Generic
open Shared.TermTypes
open Shared.OfficeInteropTypes
open Spreadsheet
open TypeConverter
open Types
open Helper

/// <summary>This is the basic function to create new Tables from an array of SwateBuildingBlocks</summary>
let createAnnotationTable (name: string option) (swateBuildingBlocks: SwateBuildingBlock []) (state: Spreadsheet.Model) : Spreadsheet.Model =
    let rowMax = swateBuildingBlocks |> Array.map (fun bb -> bb.Rows.Length) |> Seq.max
    let swateBuildingBlocks = swateBuildingBlocks |> Array.map (extendBuildingBlockToRowMax rowMax)
    // calculate next index
    let newIndex = if Map.isEmpty state.Tables then 0 else state.Tables |> Map.maxKey |> (+) 1
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

/// <summary>This is the basic function to create new Tables from an array of InsertBuildingBlocks</summary>
let createAnnotationTable_ofInsertBuildingBlock (name: string option) (insertBuildingBlocks: InsertBuildingBlock []) (state: Spreadsheet.Model) : Spreadsheet.Model =
    let swateBuildingBlocks = insertBuildingBlocks |> Array.mapi (fun i bb -> bb.toSwateBuildingBlock i)
    createAnnotationTable name swateBuildingBlocks state

/// <summary>This function is used to create multiple tables at once.</summary>
let createAnnotationTables (tables: (string*InsertBuildingBlock []) []) (state: Spreadsheet.Model) : Spreadsheet.Model =
    let max = tables.Length-1
    let rec add (ind: int) tempState =
        if ind > max then
            tempState
        else
            let name, bbs = tables.[ind]
            let swateBuildingBlocks = bbs |> Array.mapi (fun i bb -> bb.toSwateBuildingBlock i)
            let next = createAnnotationTable (Some name) swateBuildingBlocks tempState
            add (ind + 1) next
    add 0 state

/// <summary>Adds the most basic Swate table consisting of Input column "Source Name" and output column "Sample Name".</summary>
let createAnnotationTable_new (usePrevOutput:bool) (state: Spreadsheet.Model) : Spreadsheet.Model =
    let mutable n_rows = 1
    let lastOutput =
        if usePrevOutput then
            let bbs = SwateBuildingBlock.ofTableMap state.ActiveTable
            bbs |> Array.tryFind (fun bb -> bb.Header.isOutputColumn)
        else
            None
    if lastOutput.IsSome then
        n_rows <- lastOutput.Value.Rows.Length
    // create empty rows
    let rows = Array.init n_rows (fun i -> i+1, SwateCell.emptyFreetext)
    // create source column
    let sourceDefault = SwateBuildingBlock.create(0,HeaderCell.create(BuildingBlockType.Source), rows)
    let source =
        if lastOutput.IsSome then
            SwateBuildingBlock.create(0,HeaderCell.create(BuildingBlockType.Source), lastOutput.Value.Rows)
        else
            sourceDefault
    // create sample column
    let sample = SwateBuildingBlock.create(1,HeaderCell.create(BuildingBlockType.Sample),rows)
    // parse to SwateBuildingBlocks
    let bbs = [|source; sample|]
    let name = HumanReadableIds.tableName()
    createAnnotationTable (Some name) bbs state

///<summary>If n.isOutputColumn and existing contains output column, return Some Existing-Output-Column-Index.</summary>
let private checkExistingOutput(n: InsertBuildingBlock, existing: SwateBuildingBlock list) =
    if n.ColumnHeader.isOutputColumn then
        existing
        |> List.tryFind (fun sbb -> sbb.Header.isOutputColumn)
        |> Option.map (fun sbb -> sbb.Index)
    else
        None

let addBuildingBlock(insertBuildingBlock: InsertBuildingBlock) (state: Spreadsheet.Model) : Spreadsheet.Model =
    let table = state.ActiveTable
    let mutable maxColKey, maxRowKey = table |> Map.maxKeys
    maxRowKey <- System.Math.Max(insertBuildingBlock.Rows.Length, maxRowKey)
    let mutable existing = SwateBuildingBlock.ofTableMap_list table
    let isOutputReplacement = checkExistingOutput(insertBuildingBlock,existing)
    match isOutputReplacement with
    | Some index ->
        let state = Table.Controller.editColumn(index,SwateCell.emptyFreetext,Some insertBuildingBlock.ColumnHeader.Type) state
        let msg = $"Found existing output column. Changed output column to \"{insertBuildingBlock.ColumnHeader.toAnnotationTableHeader()}\"."
        let modal = Modals.WarningModal.warningModalSimple(msg)
        Modals.Controller.renderModal("ColumnReplaced", modal)
        state
    | None ->
        let nextColKey =
            // if cell is selected get column of selected cell we want to insert AFTER
            if not state.SelectedCells.IsEmpty then
                state.SelectedCells |> Set.toArray |> Array.head |> fst
            // if no cell selected insert at the end
            else
                maxColKey
            // add one to last column index OR to selected column index to append one to the right.
            |> (+) 1
        let swateBuildingBlock = insertBuildingBlock.toSwateBuildingBlock(nextColKey)
        let nNewColumns = 1
        existing <-
            // if insert is not at the end, reindex all columns with higher index.
            if nextColKey <> maxColKey + nNewColumns then
                existing |> List.map (fun sb ->
                    if sb.Index >= nextColKey then {sb with Index = sb.Index + nNewColumns} else sb
                )  
            else
                existing
        let nextTable = (swateBuildingBlock::existing) |> List.map (extendBuildingBlockToRowMax maxRowKey) |> SwateBuildingBlock.toTableMap
        let nextState = {
            state with ActiveTable = nextTable; SelectedCells = Set.empty
        }
        nextState

let addBuildingBlocks(insertBuildingBlocks: InsertBuildingBlock []) (state: Spreadsheet.Model) : Spreadsheet.Model =
    let table = state.ActiveTable
    let mutable maxColKey, maxRowKey = table |> Map.maxKeys
    maxRowKey <-
        let maxRowNew = insertBuildingBlocks |> Array.map (fun x -> x.Rows.Length) |> Array.max
        System.Math.Max(maxRowNew, maxRowKey)
    let mutable nextColKey =
        // if cell is selected get column of selected cell we want to insert AFTER
        if not state.SelectedCells.IsEmpty then
            state.SelectedCells |> Set.toArray |> Array.head |> fst
        // if no cell selected insert at the end
        else
            maxColKey
        // add one to last column index OR to selected column index to append one to the right.
        |> (+) 1
    let nNewColumns = insertBuildingBlocks.Length
    let existing =
        let l = SwateBuildingBlock.ofTableMap_list table
        // if insert is not at the end, reindex all columns with higher index.
        if nextColKey <> maxColKey + 1 then
            l |> List.map (fun sb ->
                if sb.Index >= nextColKey then {sb with Index = sb.Index + nNewColumns} else sb
            )  
        else
            l
    let swateBuildingBlocks =
        insertBuildingBlocks
        |> Array.map (fun bbs ->
            let sbb = bbs.toSwateBuildingBlock(nextColKey)
            nextColKey <- nextColKey + 1
            sbb
        )
        |> List.ofArray
    let nextTable = (swateBuildingBlocks@existing) |> List.map (extendBuildingBlockToRowMax maxRowKey) |> SwateBuildingBlock.toTableMap
    let nextState = {
        state with ActiveTable = nextTable; SelectedCells = Set.empty
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

let insertTerms (term:TermMinimal []) (state: Spreadsheet.Model) : Spreadsheet.Model =
    let table = state.ActiveTable
    /// Filter out header row
    let selected = state.SelectedCells |> Set.toArray
    /// Make sure only one column is selected
    let isOneColumn =
        let columnIndex = fst selected.[0] // can just use head of selected cells as all must be same column
        selected |> Array.forall (fun x -> fst x = columnIndex)
    if not isOneColumn then failwith "Can only paste terms in one column at a time!"
    /// Make sure either one column header is selected or only body cells are selected
    let hasHeader = selected |> Array.exists (fun (_,r) -> r = 0)
    if hasHeader then failwith "Can only paste multiple terms in table body!"
    let mutable index = 0
    let nextActiveTable = table |> Map.map (fun key cell ->
        let isSelected = Array.contains key selected
        let next =
            let term = Array.tryItem index term
            match isSelected, cell with
            | false, _ | true, IsHeader _ -> cell
            | true, IsTerm t_cell -> if term.IsSome then IsTerm {t_cell with Term = term.Value} else cell
            | true, IsUnit u_cell -> if term.IsSome then IsUnit {u_cell with Unit = term.Value} else cell
            | true, IsFreetext f_cell -> if term.IsSome then IsFreetext {f_cell with Value = term.Value.Name} else cell
        if next <> cell then
            index <- index + 1;
        next
    )
    let nextState = { state with ActiveTable = nextActiveTable }
    nextState

///<summary> This function is used to get information for the "Update Ontology Terms" Quick access button</summary>
let getUpdateTermColumns (state: Spreadsheet.Model) =
    let swate_bbs = SwateBuildingBlock.ofTableMap state.ActiveTable
    let bbs = swate_bbs |> Array.map (fun x -> x.toBuildingBlock)
    let deprecationMsgs = OfficeInterop.Core.checkForDeprecation bbs
    let terms = bbs |> Array.collect OfficeInterop.BuildingBlockFunctions.toTermSearchable
    let msg = deprecationMsgs
    terms, msg

///<summary> This function is used to write the results from "Update Ontology Terms" Quick access button</summary>
let setUpdateTermColumns (terms: TermSearchable []) (state: Spreadsheet.Model) =
    let mutable table = state.ActiveTable
    terms
    |> Array.iter (fun term ->
        for row in term.RowIndices do
            let key = term.ColIndex,row
            let changeCell newCell = table.Add(key, newCell)
            let sc = table.[key]
            let newCell = term.toSwateCell(sc)
            table <- changeCell newCell
    )
    {state with ActiveTable = table}