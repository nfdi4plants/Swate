namespace Update

open Elmish
open Spreadsheet
open Model
open Shared
open OfficeInteropTypes
open Spreadsheet

module Spreadsheet =

    let update (state: Spreadsheet.Model) (model: Messages.Model) (msg: Spreadsheet.Msg) : Spreadsheet.Model * Messages.Model * Cmd<Messages.Msg> =
        match msg with
        | UpdateTable (index, term) ->
            let nextState =
                let nextTable = state.ActiveTable.Change(index, fun _ -> Some term)
                {state with ActiveTable = nextTable}
            nextState, model, Cmd.none
        | CreateAnnotationTable usePrevOutput ->
            printfn "usePrevOutput not implemented yet"
            let nextState = Controller.createAnnotationTable state
            nextState, model, Cmd.none
        //| UpdateActiveTable _ ->
        //    printfn "UpdateActiveTable not implemented yet!"
        //    state, model, Cmd.none