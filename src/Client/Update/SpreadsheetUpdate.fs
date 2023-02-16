namespace Update

open Elmish
open Spreadsheet
open Model
open Shared
open OfficeInteropTypes
open Parser

module Spreadsheet =

    ///<summary>This function will update the `state` to the session storage history control. It works based of exlusion. As it specifies certain messages not triggering history update.</summary>
    let private updateSessionStorage (state: Spreadsheet.Model, msg: Spreadsheet.Msg) : unit =
        match msg with
        | UpdateActiveTable _ | UpdateHistoryPosition _ | Reset -> ()
        | _ -> Spreadsheet.LocalStorage.tablesToSessionStorage state

    let update (state: Spreadsheet.Model) (model: Messages.Model) (msg: Spreadsheet.Msg) : Spreadsheet.Model * Messages.Model * Cmd<Messages.Msg> =
        /// run this after any message in this update function
        let save = Spreadsheet.LocalStorage.tablesToLocalStorage
        let inner_update (msg: Spreadsheet.Msg) =
            match msg with
            | UpdateTable (index, cell) ->
                let nextState =
                    let nextTable = state.ActiveTable.Change(index, fun _ -> Some cell)
                    {state with ActiveTable = nextTable}
                nextState, model, Cmd.none
            | CreateAnnotationTable usePrevOutput ->
                printfn "usePrevOutput not implemented yet"
                let nextState =
                    state
                    |> Controller.saveActiveTable
                    |> Controller.createAnnotationTable_new
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
                let nextTables = state.Tables.Remove(removeIndex)
                let nextState =
                    // If the only existing table was removed inti model from beginning
                    if nextTables = Map.empty then
                        Spreadsheet.Model.init()
                    else
                        // if active table is removed get the next closest table and set it active
                        if state.ActiveTableIndex = removeIndex then
                            let nextTable_Index =
                                    let neighbors = Controller.findNeighborTables removeIndex nextTables
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
            | Reset ->
                let nextState = Spreadsheet.Controller.resetTableState()
                nextState, model, Cmd.none


        // execute inner and follow with save function
        inner_update msg
        |> fun (state, model, cmd) ->
            save state // This will cache the most up to date table state to local storage.
            updateSessionStorage (state, msg) // this will cache the table state for certain operations in session storage.
            state, model, cmd