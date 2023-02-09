namespace Update

open Elmish
open Spreadsheet
open Model
open Shared
open OfficeInteropTypes
open Parser

module Spreadsheet =

    let update (state: Spreadsheet.Model) (model: Messages.Model) (msg: Spreadsheet.Msg) : Spreadsheet.Model * Messages.Model * Cmd<Messages.Msg> =
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