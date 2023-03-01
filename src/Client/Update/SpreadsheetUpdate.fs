namespace Update

open Elmish
open Spreadsheet
open Model
open Shared
open Parser
open Spreadsheet.Table
open Spreadsheet.Sidebar

module Spreadsheet =

    ///<summary>This function will return the correct success message.
    /// Can return `SuccessNoHistory` or `Success`, both will save state to local storage but only `Success` will save state to session storage history control.
    /// It works based of exlusion. As it specifies certain messages not triggering history update.</summary>
    let updateSessionStorageMsg (msg: Spreadsheet.Msg) =
        match msg with
        | UpdateActiveTable _ | UpdateHistoryPosition _ | Reset | UpdateSelectedCells _ | CopySelectedCell | CopyCell _ -> Spreadsheet.SuccessNoHistory
        | _ -> Spreadsheet.Success       

    let update (state: Spreadsheet.Model) (model: Messages.Model) (msg: Spreadsheet.Msg) : Spreadsheet.Model * Messages.Model * Cmd<Messages.Msg> =
        let createPromiseCmd (func: unit -> Spreadsheet.Model) =
            let nextState() = promise {
                return func()
            }
            Cmd.OfPromise.either
                nextState
                ()
                (updateSessionStorageMsg msg >> Messages.SpreadsheetMsg)
                (Messages.curry Messages.GenericError Cmd.none >> Messages.DevMsg)

        match msg with
        | CreateAnnotationTable usePrevOutput ->
            let cmd = createPromiseCmd <| fun _ ->
                state
                |> Controller.saveActiveTable
                |> Controller.createAnnotationTable_new usePrevOutput 
            state, model, cmd
        | AddAnnotationBlock minBuildingBlockInfo ->
            let cmd = createPromiseCmd <| fun _ -> Controller.addBuildingBlock minBuildingBlockInfo state
            state, model, cmd
        | AddAnnotationBlocks minBuildingBlockInfos ->
            let cmd = createPromiseCmd <| fun _ -> Controller.addBuildingBlocks minBuildingBlockInfos state
            state, model, cmd
        | ImportFile tables ->
            let cmd = createPromiseCmd <| fun _ -> Controller.createAnnotationTables tables state
            state, model, cmd
        | InsertOntologyTerm termMinimal ->
            let cmd = createPromiseCmd <| fun _ -> Controller.insertTerm termMinimal state
            state, model, cmd
        | UpdateTable (index, cell) ->
            let cmd = createPromiseCmd <| fun _ ->
                let nextTable = state.ActiveTable.Change(index, fun _ -> Some cell)
                {state with ActiveTable = nextTable}
            state, model, cmd
        | UpdateActiveTable nextIndex ->
            let cmd = createPromiseCmd <| fun _ ->
                state
                |> Controller.saveActiveTable
                |> fun state ->
                    let nextTable = state.Tables.[nextIndex].BuildingBlocks |> SwateBuildingBlock.toTableMap
                    { state with
                        ActiveTableIndex = nextIndex
                        ActiveTable = nextTable
                    }
            state, model, cmd
        | RemoveTable removeIndex ->
            let cmd = createPromiseCmd <| fun _ -> Controller.removeTable removeIndex state
            state, model, cmd
        | RenameTable (index, name) ->
            let cmd = createPromiseCmd <| fun _ -> Controller.renameTable index name state
            state, model, cmd
        | UpdateTableOrder (prev_index, new_index) ->
            let cmd = createPromiseCmd <| fun _ -> Controller.updateTableOrder (prev_index, new_index) state
            state, model, cmd
        | UpdateHistoryPosition (newPosition) ->
            let cmd = createPromiseCmd <| fun _ -> Spreadsheet.LocalStorage.updateHistoryPosition newPosition state
            state, model, cmd
        | AddRows (n) ->
            let cmd = createPromiseCmd <| fun _ -> Controller.addRows n state
            state, model, cmd
        | Reset ->
            let cmd = createPromiseCmd <| fun _ -> Controller.resetTableState()
            state, model, cmd
        | DeleteRow index ->
            let cmd = createPromiseCmd <| fun _ -> Controller.deleteRow index state
            state, model, cmd
        | DeleteRows indexArr ->
            let cmd = createPromiseCmd <| fun _ -> Controller.deleteRows indexArr state
            state, model, cmd
        | DeleteColumn index ->
            let cmd = createPromiseCmd <| fun _ -> Controller.deleteColumn index state
            state, model, cmd
        | UpdateSelectedCells nextSelectedCells ->
            let cmd = createPromiseCmd <| fun _ -> {state with SelectedCells = nextSelectedCells}
            state, model, cmd
        | CopyCell index ->
            let cmd = createPromiseCmd <| fun _ -> Controller.copyCell index state
            state, model, cmd
        | CopySelectedCell ->
            let cmd = createPromiseCmd <| fun _ ->
                if state.SelectedCells.IsEmpty then state else
                    Controller.copySelectedCell state
            state, model, cmd
        | CutCell index ->
            let cmd = createPromiseCmd <| fun _ -> Controller.cutCell index state
            state, model, cmd
        | CutSelectedCell ->
            let cmd = createPromiseCmd <| fun _ ->
                if state.SelectedCells.IsEmpty then state else
                    Controller.cutSelectedCell state
            state, model, cmd
        | PasteCell index ->
            let cmd = createPromiseCmd <| fun _ -> if Controller.clipboardCell.IsNone then state else Controller.pasteCell index state
            state, model, cmd
        | PasteSelectedCell ->
            let cmd = createPromiseCmd <| fun _ ->
                if state.SelectedCells.IsEmpty || Controller.clipboardCell.IsNone then state else
                    Controller.pasteSelectedCell state
            state, model, cmd
        | FillColumnWithTerm index ->
            let cmd = createPromiseCmd <| fun _ -> Controller.fillColumnWithTerm index state
            state, model, cmd
        | EditColumn (columnIndex, newCellType, b_type) ->
            let cmd = createPromiseCmd <| fun _ -> Controller.editColumn (columnIndex, newCellType, b_type) state 
            state, model, cmd
        | Success nextState ->
            Spreadsheet.LocalStorage.tablesToLocalStorage nextState // This will cache the most up to date table state to local storage.
            Spreadsheet.LocalStorage.tablesToSessionStorage nextState // this will cache the table state for certain operations in session storage.
            nextState, model, Cmd.none
        | SuccessNoHistory nextState ->
            Spreadsheet.LocalStorage.tablesToLocalStorage nextState // This will cache the most up to date table state to local storage.
            nextState, model, Cmd.none