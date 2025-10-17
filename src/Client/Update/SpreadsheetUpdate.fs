namespace Update

open Messages
open Elmish
open Spreadsheet
open LocalHistory
open Model
open Swate.Components
open Swate.Components.Shared
open Fable.Remoting.Client
open FsSpreadsheet.Js
open ARCtrl
open ARCtrl.Spreadsheet
open ARCtrl.Json

module Spreadsheet =

    module Helper =

        /// <summary>
        /// This function will store the information correctly.
        /// Can return save information to local storage (persistent between browser sessions) and session storage.
        /// It works based of exlusion. As it specifies certain messages not triggering history update.
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="state"></param>
        /// <param name="model"></param>
        /// <param name="cmd"></param>
        let updateHistoryStorageMsg (msg: Spreadsheet.Msg) (state: Spreadsheet.Model, model: Model, cmd) =
            
            let mutable snapshotJsonString = ""

            if model.PersistentStorageState.Autosave then
                snapshotJsonString <- state.ToJsonString()
                //This will cache the most up to date table state to local storage.
                //This is used as a simple autosave feature.
                Spreadsheet.Model.SaveToLocalStorage(snapshotJsonString)

            //This matchcase handles undo / redo functionality
            match msg with
            | UpdateActiveView _
            | Reset
            | InitFromArcFile _ -> state, model, cmd
            | _ ->
                let newCmd =
                    if model.PersistentStorageState.Autosave then
                        Cmd.OfPromise.either
                            model.History.SaveSessionSnapshotIndexedDB
                            (snapshotJsonString)
                            (fun newHistory -> Messages.History.UpdateAnd(newHistory, cmd) |> HistoryMsg)
                            (curry GenericError Cmd.none >> DevMsg)
                    else
                        cmd
                if model.PersistentStorageState.Host = Some Swatehost.ARCitect then
                    match state.ArcFile with // model is not yet updated at this position.
                    | Some(Assay assay) ->
                        let json = assay.ToJsonString()

                        ARCitect.api.Save(ARCitect.Interop.InteropTypes.ARCFile.Assay, json)
                        |> Promise.start
                    | Some(Study(study, _)) ->
                        let json = study.ToJsonString()

                        ARCitect.api.Save(ARCitect.Interop.InteropTypes.ARCFile.Study, json)
                        |> Promise.start
                    | Some(Investigation inv) ->
                        let json = inv.ToJsonString()

                        ARCitect.api.Save(ARCitect.Interop.InteropTypes.ARCFile.Investigation, json)
                        |> Promise.start
                    | Some(Template template) ->
                        let json = template.toJsonString()

                        ARCitect.api.Save(ARCitect.Interop.InteropTypes.ARCFile.Template, json)
                        |> Promise.start
                    | Some(DataMap (parentId, parent, datamap)) ->
                        let json =
                            match parent with
                            | None
                            | Some DataMapParent.Assay ->
                                let parentDataMap = ArcAssay.create(defaultArg parentId "default", datamap = datamap)
                                ArcAssay.toJsonString 0 parentDataMap
                            | Some DataMapParent.Study ->
                                let parentDataMap = ArcStudy.create(defaultArg parentId "default", datamap = datamap)
                                ArcStudy.toJsonString 0 parentDataMap
                        ARCitect.api.Save(ARCitect.Interop.InteropTypes.ARCFile.DataMap, json)
                        |> Promise.start
                    | _ -> ()

                state, model, newCmd

    let update
        (state: Spreadsheet.Model)
        (model: Model)
        (msg: Spreadsheet.Msg)
        : Spreadsheet.Model * Model * Cmd<Messages.Msg> =
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

        let innerUpdate (state: Spreadsheet.Model) (model: Model) (msg: Spreadsheet.Msg) =
            match msg with
            | UpdateState nextState -> nextState, model, Cmd.none
            | UpdateDatamap datamapOption ->
                let nextState = Controller.DataMap.updateDatamap datamapOption state
                nextState, model, Cmd.none
            | UpdateDataMapDataContextAt(index, dtx) ->
                let nextState = Controller.DataMap.updateDataMapDataContextAt dtx index state
                nextState, model, Cmd.none
            | AddTable table ->
                let nextState = Controller.Table.addTable table state
                nextState, model, Cmd.none
            | UpdateTable table ->
                let nextTable = table.Copy()
                let nextState = Controller.Table.updateTable nextTable state
                nextState, model, Cmd.none
            | CreateAnnotationTable usePrevOutput ->
                let nextState = Controller.Table.createTable usePrevOutput state
                nextState, model, Cmd.none
            | AddAnnotationBlock(index, column) ->
                let msg, nextState = Controller.BuildingBlocks.addBuildingBlock index column state
                let cmd = Cmd.ofMsg msg
                nextState, model, cmd
            | AddAnnotationBlocks(index, columns) ->
                let nextState = Controller.BuildingBlocks.addBuildingBlocks index columns state
                nextState, model, Cmd.none
            | AddDataAnnotation data ->
                let nextState =
                    match state.ActiveView with
                    | IsDataMap -> Controller.DataMap.addDataAnnotation data state
                    | IsTable -> Controller.BuildingBlocks.addDataAnnotation data state
                    | IsMetadata -> failwith "Unable to add data annotation in metadata view"

                nextState, model, Cmd.none
            | AddTemplates(tables, importType) ->
                let arcFile = model.SpreadsheetModel.ArcFile

                let updatedArcFile =
                    UpdateUtil.JsonImportHelper.updateTables
                        (tables |> ResizeArray)
                        importType
                        model.SpreadsheetModel.ActiveView.TryTableIndex
                        arcFile

                let nextState = {
                    state with
                        ArcFile = Some updatedArcFile
                }

                nextState, model, Cmd.none
            | JoinTable(table, index, options, templateName) ->
                let nextState =
                    Controller.BuildingBlocks.joinTable table index options state templateName

                nextState, model, Cmd.none
            | UpdateArcFile arcFile ->
                let reset = state.ActiveView.ArcFileHasView(arcFile) //verify that active view is still valid

                let nextState =
                    if reset then
                        Spreadsheet.Model.init (arcFile)
                    else
                        { state with ArcFile = Some arcFile }

                nextState, model, Cmd.none
            | InitFromArcFile arcFile ->
                let nextState = Spreadsheet.Model.init (arcFile)
                nextState, model, Cmd.none
            | InsertOntologyAnnotation(range, oa) ->
                let nextState = Controller.BuildingBlocks.insertTerm oa range state
                nextState, model, Cmd.none
            | InsertOntologyAnnotations oas ->
                failwith "InsertOntologyTerms not implemented in Spreadsheet.Update"
                //let cmd = createPromiseCmd <| fun _ -> Controller.insertTerms termMinimals state
                let cmd = Cmd.none
                state, model, cmd
            | UpdateCell(index, cell) ->
                Controller.Generic.setCell (index.x, index.y) cell state
                let nextState = { state with ArcFile = state.ArcFile }
                nextState, model, Cmd.none
            | UpdateCells arr ->
                Controller.Generic.setCells arr state
                let nextState = { state with ArcFile = state.ArcFile }
                nextState, model, Cmd.none
            | UpdateHeader(index, header) ->
                let nextState =
                    state.ActiveTable.UpdateHeader(index, header)
                    { state with ArcFile = state.ArcFile }

                nextState, model, Cmd.none
            | UpdateActiveView nextView ->
                let nextState = { state with ActiveView = nextView }

                nextState, model, Cmd.none
            | RemoveTable removeIndex ->
                let nextState = Controller.Table.removeTable removeIndex state
                nextState, model, Cmd.none
            | RenameTable(index, name) ->
                let nextState = Controller.Table.renameTable index name state
                nextState, model, Cmd.none
            | UpdateTableOrder(prev_index, new_index) ->
                let nextState = Controller.Table.updateTableOrder (prev_index, new_index) state
                nextState, model, Cmd.none
            | AddRows(n) ->
                let nextState =
                    if state.TableViewIsActive() then
                        Controller.Table.addRows n state
                    else
                        Controller.DataMap.addRows n state

                nextState, model, Cmd.none
            | Reset ->
                let nextState = Controller.Table.resetTableState ()

                let nextModel = {
                    model with
                        History = LocalHistory.Model.init ()
                }

                nextState, nextModel, Cmd.none
            | DeleteRow index ->
                let nextState =
                    if state.TableViewIsActive() then
                        Controller.Table.deleteRow index state
                    else
                        Controller.DataMap.deleteRow index state

                nextState, model, Cmd.none
            | DeleteRows indexArr ->
                let nextState =
                    if state.TableViewIsActive() then
                        Controller.Table.deleteRows indexArr state
                    else
                        Controller.DataMap.deleteRows indexArr state

                nextState, model, Cmd.none
            | DeleteColumn index ->
                let nextState = Controller.Table.deleteColumn index state
                nextState, model, Cmd.none
            | SetColumn(index, column) ->
                let nextState = Controller.Table.setColumn index column state
                nextState, model, Cmd.none
            | MoveColumn(current, next) ->
                let nextState = Controller.Table.moveColumn current next state
                nextState, model, Cmd.none
            | CopyCell index ->
                let cmd =
                    Cmd.OfPromise.attempt
                        (Controller.Clipboard.copyCellByIndex index)
                        state
                        (curry GenericError Cmd.none >> DevMsg)

                state, model, cmd
            | CopyCells indices ->
                let cmd =
                    Cmd.OfPromise.attempt
                        (Controller.Clipboard.copyCellsByIndex indices)
                        state
                        (curry GenericError Cmd.none >> DevMsg)

                state, model, cmd
            | CutCell index ->
                let nextState = Controller.Clipboard.cutCellByIndex index state
                nextState, model, Cmd.none
            | PasteCell index ->
                let cmd =
                    Cmd.OfPromise.either
                        (Controller.Clipboard.pasteCellByIndex index)
                        state
                        (UpdateState >> SpreadsheetMsg)
                        (curry GenericError Cmd.none >> DevMsg)

                state, model, cmd
            | PasteCellsExtend index ->
                let cmd =
                    Cmd.OfPromise.either
                        (Controller.Clipboard.pasteCellsByIndexExtend index)
                        state
                        (UpdateState >> SpreadsheetMsg)
                        (curry GenericError Cmd.none >> DevMsg)

                state, model, cmd
            | Clear indices ->
                let nextState = Controller.Table.clearCells indices state
                nextState, model, Cmd.none
            | FillColumnWithTerm index ->
                let nextState = Controller.Table.fillColumnWithCell index state
                nextState, model, Cmd.none
            //| EditColumn (columnIndex, newCellType, b_type) ->
            //    let cmd = createPromiseCmd <| fun _ -> Controller.editColumn (columnIndex, newCellType, b_type) state
            //    state, model, cmd
            | ImportXlsx bytes ->
                let cmd =
                    Cmd.OfPromise.either
                        Spreadsheet.IO.Xlsx.readFromBytes
                        bytes
                        (UpdateArcFile >> Messages.SpreadsheetMsg)
                        (Messages.curry Messages.GenericError Cmd.none >> Messages.DevMsg)

                state, model, cmd
            | ExportJson(arcfile, jef) ->
                let jsonExport = UpdateUtil.JsonExportHelper.parseToJsonString (arcfile, jef)
                UpdateUtil.downloadFromString (jsonExport)
                state, model, Cmd.none
            | ExportXlsx arcfile ->
                let name, fswb =
                    let n = System.DateTime.Now.ToUniversalTime().ToString("yyyyMMdd_hhmmss")

                    match arcfile with
                    | Investigation ai -> n + "_" + ArcInvestigation.FileName, ArcInvestigation.toFsWorkbook ai
                    | Study(as', aaList) -> n + "_" + ArcStudy.FileName, ArcStudy.toFsWorkbook (as', aaList)
                    | Assay aa -> n + "_" + ArcAssay.FileName, ArcAssay.toFsWorkbook aa
                    | Template t -> n + "_" + t.FileName, Spreadsheet.Template.toFsWorkbook t
                    | DataMap (_, p, d) ->
                        let fileName =
                            if p.IsSome then
                                n + "_" + p.Value.ToString() + "_" + "datamap"
                            else
                                n + "_" + "datamap"
                        fileName, Spreadsheet.DataMap.toFsWorkbook d

                let cmd =
                    Cmd.OfPromise.either
                        Xlsx.toXlsxBytes
                        fswb
                        (fun bytes -> ExportXlsxDownload(name, bytes) |> Messages.SpreadsheetMsg)
                        (Messages.curry Messages.GenericError Cmd.none >> Messages.DevMsg)

                state, model, cmd
            | ExportXlsxDownload(name, xlsxBytes) ->
                let _ = UpdateUtil.download (name, xlsxBytes)
                state, model, Cmd.none
            | SetCell(x, y) ->
                failwith "SetCell not implemented in Spreadsheet.Update"
                state, model, Cmd.none

        try
            innerUpdate state model msg
            |> Helper.updateHistoryStorageMsg msg
        with e ->
            let cmd = GenericError(Cmd.none, e) |> DevMsg |> Cmd.ofMsg
            state, model, cmd