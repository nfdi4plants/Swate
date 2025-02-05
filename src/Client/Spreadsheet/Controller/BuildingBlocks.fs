module Spreadsheet.Controller.BuildingBlocks

open Spreadsheet
open Types
open ARCtrl
open Shared

open ExcelJS.Fable
open Excel
open GlobalBindings

open Regex

module SidebarControllerAux =

    /// <summary>
    /// Uses the first selected columnIndex from `state.SelectedCells` to determine if new column should be inserted or appended.
    /// </summary>
    /// <param name="state"></param>
    let getNextColumnIndex (state: Spreadsheet.Model) =
        // if cell is selected get column of selected cell we want to insert AFTER
        if not state.DeSelectedCells.IsEmpty then
            let indexNextToSelected = state.DeSelectedCells |> Set.toArray |> Array.head |> fst |> (+) 1
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

let addBuildingBlock(newColumn: CompositeColumn) (state: Spreadsheet.Model) : Messages.Msg * Spreadsheet.Model =
    let table = state.ActiveTable
    // add one to last column index OR to selected column index to append one to the right.
    let mutable nextIndex = getNextColumnIndex state
    let mutable newColumn = newColumn
    let msg =
        // nested if cases to only run table operations if necessary
        if newColumn.Header.isOutput then
            let hasOutput = table.TryGetOutputColumn()
            if hasOutput.IsSome then
                let txt = $"Found existing output column. Changed output column to \"{newColumn.Header.ToString()}\"."
                let msg0 = Model.ModalState.GeneralModals.Warning txt |> Model.ModalState.ModalTypes.GeneralModal |> Some |> Messages.UpdateModal
                newColumn <- {newColumn with Cells = hasOutput.Value.Cells}
                msg0
            else
                Messages.Msg.DoNothing
        elif newColumn.Header.isInput then
            let hasInput = table.TryGetInputColumn()
            if hasInput.IsSome then
                let txt = $"Found existing input column. Changed input column to \"{newColumn.Header.ToString()}\"."
                let msg0 = Model.ModalState.GeneralModals.Warning txt |> Model.ModalState.ModalTypes.GeneralModal |> Some |> Messages.UpdateModal
                newColumn <- {newColumn with Cells = hasInput.Value.Cells}
                msg0
            else
                Messages.Msg.DoNothing
        else
            Messages.Msg.DoNothing
    table.AddColumn(newColumn.Header, newColumn.Cells, nextIndex, true)
    msg, {state with ArcFile = state.ArcFile}


let addBuildingBlocks(newColumns: CompositeColumn []) (state: Spreadsheet.Model) : Spreadsheet.Model =
    let table = state.ActiveTable
    let mutable newColumns = newColumns
    let mutable nextIndex = getNextColumnIndex state
    table.AddColumns(newColumns,nextIndex)
    {state with ArcFile = state.ArcFile}

let addDataAnnotation (data: {| fragmentSelectors: string []; fileName: string; fileType: string; targetColumn: DataAnnotator.TargetColumn |}) (state: Spreadsheet.Model) : Spreadsheet.Model =
    let tryIfNone() =
        match state.ActiveTable.TryGetInputColumn(), state.ActiveTable.TryGetOutputColumn() with
        | Some _, None
        | None, None        -> CompositeHeader.Output IOType.Data
        | None, Some _      -> CompositeHeader.Input IOType.Data
        | Some _, Some _    -> failwith "Both Input and Output columns exist and no target column was specified"
    let newHeader =
        match data.targetColumn with
        | DataAnnotator.TargetColumn.Input      -> CompositeHeader.Input IOType.Data
        | DataAnnotator.TargetColumn.Output     -> CompositeHeader.Output IOType.Data
        | DataAnnotator.TargetColumn.Autodetect -> tryIfNone()
    let values = [|
        for selector in data.fragmentSelectors do
            let d = Data()
            d.FilePath <- Some data.fileName
            d.Selector <- Some selector
            d.Format <- Some data.fileType
            d.SelectorFormat <- Some Shared.URLs.Data.SelectorFormat.csv
            CompositeCell.createData d
    |]
    state.ActiveTable.AddColumn(newHeader, values, forceReplace=true)
    {state with ArcFile = state.ArcFile}

let joinTable(tableToAdd: ArcTable) (index: int option) (options: TableJoinOptions option) (state: Spreadsheet.Model) (templateName:string option): Spreadsheet.Model =
    
    if templateName.IsSome then
        //Should be updated to remove all kinds of extra symbols
        let templateName = System.Text.RegularExpressions.Regex.Replace(templateName.Value, "\W", "")
        let newTable = ArcTable.create(templateName, state.ActiveTable.Headers, state.ActiveTable.Values)
        state.ArcFile.Value.Tables().SetTable(state.ActiveTable.Name, newTable)

    let table = state.ActiveTable
    table.Join(tableToAdd,?index=index, ?joinOptions=options, forceReplace=true)
    {state with ArcFile = state.ArcFile}

let insertTerm_IntoSelected (term:OntologyAnnotation) (state: Spreadsheet.Model) : Spreadsheet.Model =
    let selected = state.DeSelectedCells |> Set.toArray
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