module Spreadsheet.Controller.BuildingBlocks

open System.Collections.Generic
open Shared.TermTypes
open Spreadsheet
open Types
open ARCtrl
open Shared


module SidebarControllerAux =

    /// <summary>
    /// Uses the first selected columnIndex from `state.SelectedCells` to determine if new column should be inserted or appended.
    /// </summary>
    /// <param name="state"></param>
    let getNextColumnIndex (state: Spreadsheet.Model) =
        // if cell is selected get column of selected cell we want to insert AFTER
        if not state.SelectedCells.IsEmpty then
            let indexNextToSelected = state.SelectedCells |> Set.toArray |> Array.head |> fst |> (+) 1 
            indexNextToSelected
        else
            state.ActiveTable.ColumnCount

module SanityChecks =
    /// Make sure only one column is selected
    let verifyOnlyOneColumnSelected (selectedCells: (int*int) [])=
        let isOneColumn =
            let columnIndex = fst selectedCells.[0] // can just use head of selected cells as all must be same column
            selectedCells |> Array.forall (fun x -> fst x = columnIndex)
        if not isOneColumn then failwith "Can only paste term in one column at a time!"


open SidebarControllerAux

let addBuildingBlock(newColumn: CompositeColumn) (state: Spreadsheet.Model) : Spreadsheet.Model =
    let table = state.ActiveTable
    // add one to last column index OR to selected column index to append one to the right.
    let mutable nextIndex = getNextColumnIndex state
    let mutable newColumn = newColumn
    if newColumn.Header.isOutput then
        let hasOutput = table.TryGetOutputColumn()
        if hasOutput.IsSome then
            let msg = $"Found existing output column. Changed output column to \"{newColumn.Header.ToString()}\"."
            let modal = Modals.WarningModal.warningModalSimple(msg)
            Modals.Controller.renderModal("ColumnReplaced", modal)
            newColumn <- {newColumn with Cells = hasOutput.Value.Cells}
    if newColumn.Header.isInput then
        let hasInput = table.TryGetInputColumn()
        if hasInput.IsSome then
            let msg = $"Found existing input column. Changed input column to \"{newColumn.Header.ToString()}\"."
            let modal = Modals.WarningModal.warningModalSimple(msg)
            Modals.Controller.renderModal("ColumnReplaced", modal)
            newColumn <- {newColumn with Cells = hasInput.Value.Cells}
    table.AddColumn(newColumn.Header, newColumn.Cells, nextIndex, true)
    {state with ArcFile = state.ArcFile}


let addBuildingBlocks(newColumns: CompositeColumn []) (state: Spreadsheet.Model) : Spreadsheet.Model =
    let table = state.ActiveTable
    let mutable newColumns = newColumns
    let mutable nextIndex = getNextColumnIndex state
    table.AddColumns(newColumns,nextIndex)
    {state with ArcFile = state.ArcFile}

let joinTable(tableToAdd: ArcTable) (index: int option) (options: TableJoinOptions option) (state: Spreadsheet.Model) : Spreadsheet.Model =
    let table = state.ActiveTable
    table.Join(tableToAdd,?index=index, ?joinOptions=options, forceReplace=true)
    {state with ArcFile = state.ArcFile}

let insertTerm_IntoSelected (term:OntologyAnnotation) (state: Spreadsheet.Model) : Spreadsheet.Model =
    let selected = state.SelectedCells |> Set.toArray
    SanityChecks.verifyOnlyOneColumnSelected selected
    for (colIndex, rowIndex) in selected do
        let c = Generic.getCell (colIndex,rowIndex) state
        let newCell = c.UpdateWithOA term
        Controller.Generic.setCell (colIndex,rowIndex) newCell state
    {state with ArcFile = state.ArcFile}

//let insertCells_IntoSelected (term:CompositeCell []) (state: Spreadsheet.Model) : Spreadsheet.Model =
//    let table = state.ActiveTable
//    let selected = state.SelectedCells |> Set.toArray
//    SanityChecks.verifyOnlyOneColumnSelected selected
//    /// Make sure either one column header is selected or only body cells are selected
//    let mutable index = 0
//    let nextActiveTable = table |> Map.map (fun key cell ->
//        let isSelected = Array.contains key selected
//        let next =
//            let term = Array.tryItem index term
//            match isSelected, cell with
//            | false, _ | true, IsHeader _ -> cell
//            | true, IsTerm t_cell -> if term.IsSome then IsTerm {t_cell with Term = term.Value} else cell
//            | true, IsUnit u_cell -> if term.IsSome then IsUnit {u_cell with Unit = term.Value} else cell
//            | true, IsFreetext f_cell -> if term.IsSome then IsFreetext {f_cell with Value = term.Value.Name} else cell
//        if next <> cell then
//            index <- index + 1;
//        next
//    )
//    let nextState = { state with ActiveTable = nextActiveTable }
//    nextState

/////<summary> This function is used to get information for the "Update Ontology Terms" Quick access button</summary>
//let getUpdateTermColumns (state: Spreadsheet.Model) =
//    let swate_bbs = SwateBuildingBlock.ofTableMap state.ActiveTable
//    let bbs = swate_bbs |> Array.map (fun x -> x.toBuildingBlock)
//    let deprecationMsgs = OfficeInterop.Core.checkForDeprecation bbs
//    let terms = bbs |> Array.collect OfficeInterop.BuildingBlockFunctions.toTermSearchable
//    let msg = deprecationMsgs
//    terms, msg

/////<summary> This function is used to write the results from "Update Ontology Terms" Quick access button</summary>
//let setUpdateTermColumns (terms: TermSearchable []) (state: Spreadsheet.Model) =
//    let mutable table = state.ActiveTable
//    terms
//    |> Array.iter (fun term ->
//        for row in term.RowIndices do
//            let key = term.ColIndex,row
//            let changeCell newCell = table.Add(key, newCell)
//            let sc = table.[key]
//            let newCell = term.toSwateCell(sc)
//            table <- changeCell newCell
//    )
//    {state with ActiveTable = table}