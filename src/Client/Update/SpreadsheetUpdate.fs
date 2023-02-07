namespace Update

open Elmish
open Spreadsheet
open Model
open Shared
open OfficeInteropTypes

module Spreadsheet =

    let update (state: Spreadsheet.Model) (model: Messages.Model) (msg: Spreadsheet.Msg) : Spreadsheet.Model * Messages.Model * Cmd<Messages.Msg> =
        match msg with
        | UpdateTable (index, term) ->
            let nextState = {state with ActiveTable = state.ActiveTable.Change(index, fun _ -> Some term)}
            nextState, model, Cmd.none
        | UpdateActiveTable _ ->
            printfn "UpdateActiveTable not implemented yet!"
            state, model, Cmd.none