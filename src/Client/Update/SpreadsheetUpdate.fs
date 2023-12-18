namespace Update

open Messages
open Elmish
open Spreadsheet
open LocalHistory
open Model
open Shared
open Spreadsheet.Table
open Spreadsheet.Sidebar
open Spreadsheet.Clipboard
open Fable.Remoting.Client
open Fable.Remoting.Client.InternalUtilities
open FsSpreadsheet
open FsSpreadsheet.Exceljs
open ARCtrl.ISA
open ARCtrl.ISA.Spreadsheet
open Spreadsheet.Sidebar.Controller

module Spreadsheet =

    module Helper =
        open Browser
        open Fable.Core
        open Fable.Core.JS
        open Fable.Core.JsInterop

        let download(filename, bytes:byte []) = bytes.SaveFileAs(filename)

        /// <summary>
        /// This function will store the information correctly.
        /// Can return save information to local storage (persistent between browser sessions) and session storage.
        /// It works based of exlusion. As it specifies certain messages not triggering history update.
        /// </summary>
        let updateHistoryStorageMsg (msg: Spreadsheet.Msg) (state: Spreadsheet.Model, model: Messages.Model, cmd) =
            match msg with
            | UpdateActiveTable _ | UpdateHistoryPosition _ | Reset | UpdateSelectedCells _ | CopySelectedCell | CopyCell _ -> 
                state.SaveToLocalStorage() // This will cache the most up to date table state to local storage.
                state, model, cmd
            | _ -> 
                state.SaveToLocalStorage() // This will cache the most up to date table state to local storage.
                let nextHistory = model.History.SaveSessionSnapshot state // this will cache the table state for certain operations in session storage.
                state, {model with History = nextHistory}, cmd

    let update (state: Spreadsheet.Model) (model: Messages.Model) (msg: Spreadsheet.Msg) : Spreadsheet.Model * Messages.Model * Cmd<Messages.Msg> =

        //let createPromiseCmd (func: unit -> Spreadsheet.Model) =
        //    let nextState() = promise {
        //        return func()
        //    }
        //    Cmd.OfPromise.either
        //        nextState
        //        ()
        //        (updateSessionStorageMsg msg >> Messages.SpreadsheetMsg)
        //        (Messages.curry Messages.GenericError Cmd.none >> Messages.DevMsg)

        //let newHistoryController (state, model, cmd) =
        //    updateSessionStorageMsg msg, model 

        let innerUpdate (state: Spreadsheet.Model) (model: Messages.Model) (msg: Spreadsheet.Msg) =
            match msg with
            | CreateAnnotationTable usePrevOutput ->
                let nextState = Controller.createTable usePrevOutput state
                nextState, model, Cmd.none
            | AddAnnotationBlock column ->
                let nextState = Controller.addBuildingBlock column state
                nextState, model, Cmd.none
            | AddAnnotationBlocks columns ->
                let nextState = Controller.addBuildingBlocks columns state
                nextState, model, Cmd.none
            | SetArcFile arcFile ->
                let nextState = { state with ArcFile = Some arcFile }
                nextState, model, Cmd.none
            | InsertOntologyTerm oa ->
                let nextState = Controller.insertTerm_IntoSelected oa state
                nextState, model, Cmd.none
            | InsertOntologyTerms termMinimals ->
                failwith "InsertOntologyTerms not implemented in Spreadsheet.Update"
                //let cmd = createPromiseCmd <| fun _ -> Controller.insertTerms termMinimals state
                let cmd = Cmd.none
                state, model, cmd
            | UpdateCell (index, cell) ->
                let nextState = 
                    state.ActiveTable.UpdateCellAt(fst index,snd index, cell)
                    {state with ArcFile = state.ArcFile}
                nextState, model, Cmd.none
            | UpdateHeader (index, header) ->
                let nextState = 
                    state.ActiveTable.UpdateHeader(index, header)
                    {state with ArcFile = state.ArcFile}
                nextState, model, Cmd.none
            | UpdateActiveTable nextIndex ->
                let nextState = 
                    if nextIndex < 0 || nextIndex >= state.Tables.TableCount then
                        failwith $"Error. Cannot navigate to table: '{nextIndex + 1}'. Only '{state.Tables.TableCount}' tables found!"
                    { state with
                        ActiveTableIndex = nextIndex }
                nextState, model, Cmd.none
            | RemoveTable removeIndex ->
                let nextState = Controller.removeTable removeIndex state
                nextState, model, Cmd.none
            | RenameTable (index, name) ->
                let nextState = Controller.renameTable index name state
                nextState, model, Cmd.none
            | UpdateTableOrder (prev_index, new_index) ->
                let nextState = Controller.updateTableOrder (prev_index, new_index) state
                nextState, model, Cmd.none
            | UpdateHistoryPosition (newPosition) ->
                let nextState, nextModel =
                    match newPosition with
                    | _ when model.History.NextPositionIsValid(newPosition) |> not ->
                        state, model
                    | _ ->
                        /// Run this first so an error breaks the function before any mutables are changed
                        let nextState = 
                            Spreadsheet.Model.fromSessionStorage(newPosition)
                        Browser.WebStorage.sessionStorage.setItem(Keys.swate_session_history_position, string newPosition)
                        let nextModel = {model with History.HistoryCurrentPosition = newPosition}
                        nextState, nextModel
                nextState, nextModel, Cmd.none
            | AddRows (n) ->
                let nextState = Controller.addRows n state
                nextState, model, Cmd.none
            | Reset ->
                let nextHistory, nextState = Controller.resetTableState()
                let nextModel = {model with History = nextHistory}
                nextState, nextModel, Cmd.none
            | DeleteRow index ->
                let nextState = Controller.deleteRow index state
                nextState, model, Cmd.none
            | DeleteRows indexArr ->
                let nextState = Controller.deleteRows indexArr state
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
                let nextState = if state.Clipboard.Cell.IsNone then state else Controller.pasteCell index state
                nextState, model, Cmd.none
            | PasteSelectedCell ->
                let nextState = 
                    if state.SelectedCells.IsEmpty || state.Clipboard.Cell.IsNone then state else
                        Controller.pasteSelectedCell state
                nextState, model, Cmd.none
            | FillColumnWithTerm index ->
                let nextState = Controller.fillColumnWithCell index state
                nextState, model, Cmd.none
            //| EditColumn (columnIndex, newCellType, b_type) ->
            //    let cmd = createPromiseCmd <| fun _ -> Controller.editColumn (columnIndex, newCellType, b_type) state 
            //    state, model, cmd
            | SetArcFileFromBytes bytes ->
                let cmd =
                    Cmd.OfPromise.either
                        Spreadsheet.IO.readFromBytes
                        bytes
                        (SetArcFile >> Messages.SpreadsheetMsg)
                        (Messages.curry Messages.GenericError Cmd.none >> Messages.DevMsg)
                state, model, cmd
            | ExportJsonTable ->
                failwith "ExportsJsonTable is not implemented"
                //let exportJsonState = {model.JsonExporterModel with Loading = true}
                //let nextModel = model.updateByJsonExporterModel exportJsonState
                //let func() = promise {
                //    return Controller.getTable state
                //}
                //let cmd =
                //    Cmd.OfPromise.either
                //        func
                //        ()
                //        (JsonExporter.State.ParseTableServerRequest >> Messages.JsonExporterMsg)
                //        (Messages.curry Messages.GenericError (JsonExporter.State.UpdateLoading false |> Messages.JsonExporterMsg |> Cmd.ofMsg) >> Messages.DevMsg)
                //state, nextModel, cmd
                state, model, Cmd.none
            | ExportJsonTables ->
                failwith "ExportJsonTables is not implemented"
                //let exportJsonState = {model.JsonExporterModel with Loading = true}
                //let nextModel = model.updateByJsonExporterModel exportJsonState
                //let func() = promise {
                //    return Controller.getTables state
                //}
                //let cmd =
                //    Cmd.OfPromise.either
                //        func
                //        ()
                //        (JsonExporter.State.ParseTablesServerRequest >> Messages.JsonExporterMsg)
                //        (Messages.curry Messages.GenericError (JsonExporter.State.UpdateLoading false |> Messages.JsonExporterMsg |> Cmd.ofMsg) >> Messages.DevMsg)
                //state, nextModel, cmd
                state, model, Cmd.none
            | ParseTablesToDag ->
                failwith "ParseTablesToDag is not implemented"
                //let dagState = {model.DagModel with Loading = true}
                //let nextModel = model.updateByDagModel dagState
                //let func() = promise {
                //    return Controller.getTables state
                //}
                //let cmd =
                //    Cmd.OfPromise.either
                //        func
                //        ()
                //        (Dag.ParseTablesDagServerRequest >> Messages.DagMsg)
                //        (Messages.curry Messages.GenericError (Dag.UpdateLoading false |> Messages.DagMsg |> Cmd.ofMsg) >> Messages.DevMsg)
                //state, nextModel, cmd
                state, model, Cmd.none
            | ExportXlsx arcfile->
                // we highjack this loading function
                let exportJsonState = {model.JsonExporterModel with Loading = true}
                let nextModel = model.updateByJsonExporterModel exportJsonState
                let fswb =
                    match arcfile with
                    | Investigation ai ->
                        ArcInvestigation.toFsWorkbook ai
                    | Study (as',aaList) ->
                        ArcStudy.toFsWorkbook (as', aaList)
                    | Assay aa ->
                        ArcAssay.toFsWorkbook aa
                let func() = promise {
                    return state.Tables
                }
                let cmd =
                    Cmd.OfPromise.either
                        FsSpreadsheet.Exceljs.Xlsx.toBytes
                        fswb
                        (ExportXlsxDownload >> Messages.SpreadsheetMsg)
                        (Messages.curry Messages.GenericError (JsonExporter.State.UpdateLoading false |> Messages.JsonExporterMsg |> Cmd.ofMsg) >> Messages.DevMsg)
                state, nextModel, cmd
            | ExportXlsxDownload xlsxBytes ->
                let n = System.DateTime.Now.ToUniversalTime().ToString("yyyyMMdd_hhmmss")
                let _ = Helper.download ($"{n}_assay.xlsx",xlsxBytes)
                let nextJsonExporter = {
                    model.JsonExporterModel with
                        Loading             = false
                }
                let nextModel = model.updateByJsonExporterModel nextJsonExporter
                state, nextModel, Cmd.none
            | UpdateTermColumns ->
                //let getUpdateTermColumns() = promise {
                //    return Controller.getUpdateTermColumns state
                //}
                //let cmd =
                //    Cmd.OfPromise.either
                //        getUpdateTermColumns
                //        ()
                //        (fun (searchTerms,deprecationLogs) ->
                //            // Push possible deprecation messages by piping through "GenericInteropLogs"
                //            Messages.GenericInteropLogs (
                //                // This will be executed after "deprecationLogs" are handled by "GenericInteropLogs"
                //                Messages.SearchForInsertTermsRequest searchTerms |> Messages.Request |> Messages.Api |> Cmd.ofMsg,
                //                // This will be pushed to Activity logs, or as wanring modal to user in case of LogIdentifier.Warning
                //                deprecationLogs
                //            )
                //            |> Messages.DevMsg
                //        )
                //        (Messages.curry Messages.GenericError (OfficeInterop.UpdateFillHiddenColsState OfficeInterop.FillHiddenColsState.Inactive |> OfficeInteropMsg |> Cmd.ofMsg) >> DevMsg)
                //let stateCmd = OfficeInterop.UpdateFillHiddenColsState OfficeInterop.FillHiddenColsState.ExcelCheckHiddenCols |> OfficeInteropMsg |> Cmd.ofMsg
                //let cmds = Cmd.batch [cmd; stateCmd]
                //state, model, cmds
                failwith "UpdateTermColumns is not implemented yet"
                state,model,Cmd.none
            | UpdateTermColumnsResponse terms ->
                //let nextExcelState = {
                //    model.ExcelState with
                //        FillHiddenColsStateStore = OfficeInterop.FillHiddenColsState.ExcelWriteFoundTerms
                //}
                //let nextModel = model.updateByExcelState nextExcelState
                //let setUpdateTermColumns terms = promise {return Controller.setUpdateTermColumns terms state}
                //let cmd =
                //    Cmd.OfPromise.either
                //        setUpdateTermColumns
                //        (terms)
                //        (fun r -> Msg.Batch [
                //            Spreadsheet.Success r |> SpreadsheetMsg
                //            OfficeInterop.UpdateFillHiddenColsState OfficeInterop.FillHiddenColsState.Inactive |> OfficeInteropMsg
                //        ])
                //        (curry GenericError (OfficeInterop.UpdateFillHiddenColsState OfficeInterop.FillHiddenColsState.Inactive |> OfficeInteropMsg |> Cmd.ofMsg) >> DevMsg)
                //state, nextModel, cmd
                failwith "UpdateTermColumnsResponse is not implemented yet"
                state,model,Cmd.none
            //| Success nextState ->
            //    nextState.SaveToLocalStorage() // This will cache the most up to date table state to local storage.
            //    let nextHistory = model.History.SaveSessionSnapshot nextState // this will cache the table state for certain operations in session storage.
            //    nextState, {model with History = nextHistory}, Cmd.none
            //| SuccessNoHistory nextState ->
            //    nextState.SaveToLocalStorage() // This will cache the most up to date table state to local storage.
            //    nextState, model, Cmd.none

        innerUpdate state model msg
        |> Helper.updateHistoryStorageMsg msg