namespace Update

open Elmish
open Spreadsheet
open Model
open Shared
open OfficeInteropTypes
open Parser
open Spreadsheet.Table
open Spreadsheet.Sidebar

module Spreadsheet =

    ///<summary>This function will update the `state` to the session storage history control. It works based of exlusion. As it specifies certain messages not triggering history update.</summary>
    let private updateSessionStorage (state: Spreadsheet.Model, msg: Spreadsheet.Msg) : unit =
        match msg with
        | UpdateActiveTable _ | UpdateHistoryPosition _ | Reset | UpdateSelectedCells _ | CopySelectedCell | CopyCell _ -> ()
        | _ -> Spreadsheet.LocalStorage.tablesToSessionStorage state

    let update (state: Spreadsheet.Model) (model: Messages.Model) (msg: Spreadsheet.Msg) : Spreadsheet.Model * Messages.Model * Cmd<Messages.Msg> =
        /// run this after any message in this update function
        let save = Spreadsheet.LocalStorage.tablesToLocalStorage
        let inner_update (msg: Spreadsheet.Msg) =
            match msg with
            | CreateAnnotationTable usePrevOutput ->
                printfn "implemented usePrevOutput for new table input column"
                let nextState =
                    state
                    |> Controller.saveActiveTable
                    |> Controller.createAnnotationTable_new
                nextState, model, Cmd.none
            | AddAnnotationBlock minBuildingBlockInfo ->
                printfn "implement adding building block after selected col index"
                let nextState = Controller.addBuildingBlock state minBuildingBlockInfo
                nextState, model, Cmd.none
            | UpdateTable (index, cell) ->
                let nextState =
                    let nextTable = state.ActiveTable.Change(index, fun _ -> Some cell)
                    {state with ActiveTable = nextTable}
                nextState, model, Cmd.none
            | UpdateActiveTable nextIndex ->
                let nextState =
                    state
                    |> Controller.saveActiveTable
                    |> fun state ->
                        let nextTable = state.Tables.[nextIndex].BuildingBlocks |> SwateBuildingBlock.toTableMap
                        { state with
                            ActiveTableIndex = nextIndex
                            ActiveTable = nextTable
                        }
                nextState, model, Cmd.none
            | RemoveTable removeIndex ->
                let nextState = Controller.removeTable removeIndex state
                nextState, model, Cmd.none
            | RenameTable (index, name) ->
                let nextTable = { state.Tables.[index] with Name = name }
                let nextState = {state with Tables = state.Tables.Change(index, fun _ -> Some nextTable)}
                nextState, model, Cmd.none
            | UpdateTableOrder (prev_index, new_index) ->
                let tableOrder = state.TableOrder |> Controller.updateTableOrder (prev_index, new_index)
                let nextState = { state with TableOrder = tableOrder }
                nextState, model, Cmd.none
            | UpdateHistoryPosition (newPosition) ->
                let nextState = Spreadsheet.LocalStorage.updateHistoryPosition newPosition state
                nextState, model, Cmd.none
            | AddRows (n) ->
                let nextState = Controller.addRows n state
                nextState, model, Cmd.none
            | Reset ->
                let nextState = Controller.resetTableState()
                nextState, model, Cmd.none
            | DeleteRow index ->
                let nextState = Controller.deleteRow index state
                nextState, model, Cmd.none
            | DeleteColumn index ->
                let nextState = Controller.deleteColumn index state
                nextState, model, Cmd.none
            | UpdateSelectedCells nextSelectedCells ->
                let nextState = {state with SelectedCells = nextSelectedCells}
                nextState, model, Cmd.none
            | CopyCell index ->
                let nextState = Controller.copyCell index state
                nextState, model, Cmd.none
            | CopySelectedCell ->
                let nextState =
                    if state.SelectedCells.IsEmpty then state else
                        Controller.copySelectedCell state
                nextState, model, Cmd.none
            | CutCell index ->
                let nextState = Controller.cutCell index state
                nextState, model, Cmd.none
            | CutSelectedCell ->
                let nextState =
                    if state.SelectedCells.IsEmpty then state else
                        Controller.cutSelectedCell state
                nextState, model, Cmd.none
            | PasteCell index ->
                let nextState = if Controller.clipboardCell.IsNone then state else Controller.pasteCell index state
                nextState, model, Cmd.none
            | PasteSelectedCell ->
                let nextState =
                    if state.SelectedCells.IsEmpty || Controller.clipboardCell.IsNone then state else
                        Controller.pasteSelectedCell state
                nextState, model, Cmd.none
            | FillColumnWithTerm index ->
                let nextState = Controller.fillColumnWithTerm index state
                nextState, model, Cmd.none


        // execute inner and follow with save function
        inner_update msg
        |> fun (state, model, cmd) ->
            save state // This will cache the most up to date table state to local storage.
            updateSessionStorage (state, msg) // this will cache the table state for certain operations in session storage.
            state, model, cmd