namespace Update

open Messages
open Elmish
open Spreadsheet
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
    /// This function will return the correct success message.
    /// Can return `SuccessNoHistory` or `Success`, both will save state to local storage but only `Success` will save state to session storage history control.
    /// It works based of exlusion. As it specifies certain messages not triggering history update.
    /// </summary>
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
            let cmd = createPromiseCmd <| fun _ -> Controller.createTable usePrevOutput state
            state, model, cmd
        | AddAnnotationBlock column ->
            let cmd = createPromiseCmd <| fun _ -> Controller.addBuildingBlock column state
            state, model, cmd
        | AddAnnotationBlocks columns ->
            let cmd = createPromiseCmd <| fun _ -> Controller.addBuildingBlocks columns state
            state, model, cmd
        | SetArcFile arcFile ->
            let cmd = createPromiseCmd <| fun _ -> { state with ArcFile = Some arcFile }
            state, model, cmd
        | InsertOntologyTerm oa ->
            let cmd = createPromiseCmd <| fun _ -> Controller.insertTerm_IntoSelected oa state
            state, model, cmd
        | InsertOntologyTerms termMinimals ->
            failwith "InsertOntologyTerms not implemented in Spreadsheet.Update"
            //let cmd = createPromiseCmd <| fun _ -> Controller.insertTerms termMinimals state
            let cmd = Cmd.none
            state, model, cmd
        | UpdateCell (index, cell) ->
            let cmd = createPromiseCmd <| fun _ ->
                state.ActiveTable.UpdateCellAt(fst index,snd index, cell)
                state
            state, model, cmd
        | UpdateActiveTable nextIndex ->
            let cmd = createPromiseCmd <| fun _ ->
                if nextIndex < 0 || nextIndex >= state.Tables.TableCount then
                    failwith $"Error. Cannot navigate to table: '{nextIndex + 1}'. Only '{state.Tables.TableCount}' tables found!"
                { state with
                    ActiveTableIndex = nextIndex }
            state, model, cmd
        | RemoveTable removeIndex ->
            logf "RemoveTable: %i" removeIndex
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
            let cmd = createPromiseCmd <| fun _ -> if state.Clipboard.Cell.IsNone then state else Controller.pasteCell index state
            state, model, cmd
        | PasteSelectedCell ->
            let cmd = createPromiseCmd <| fun _ ->
                if state.SelectedCells.IsEmpty || state.Clipboard.Cell.IsNone then state else
                    Controller.pasteSelectedCell state
            state, model, cmd
        | FillColumnWithTerm index ->
            let cmd = createPromiseCmd <| fun _ -> Controller.fillColumnWithCell index state
            state, model, cmd
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
        | Success nextState ->
            Spreadsheet.LocalStorage.tablesToLocalStorage nextState // This will cache the most up to date table state to local storage.
            Spreadsheet.LocalStorage.tablesToSessionStorage nextState // this will cache the table state for certain operations in session storage.
            nextState, model, Cmd.none
        | SuccessNoHistory nextState ->
            Spreadsheet.LocalStorage.tablesToLocalStorage nextState // This will cache the most up to date table state to local storage.
            nextState, model, Cmd.none