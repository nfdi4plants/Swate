namespace Update

open Elmish

open Messages
open OfficeInterop
open OfficeInterop.Core
open Shared
open Model

module OfficeInterop = 
    let update (state: OfficeInterop.Model) (model:Model) (msg: OfficeInterop.Msg) : OfficeInterop.Model * Model * Cmd<Messages.Msg> =

        let innerUpdate (state: OfficeInterop.Model) (model: Model) (msg: OfficeInterop.Msg) =

            match msg with

            | UpdateArcFile arcFile ->
                log ("UpdateArcFile", arcFile)
                let cmd =
                    Cmd.OfPromise.either
                        Main.updateArcFile
                        arcFile
                        (curry GenericInteropLogs Cmd.none >> DevMsg)
                        (curry GenericError Cmd.none >> DevMsg)
                state, model, cmd

            | AutoFitTable hidecols ->
                let p = fun () -> ExcelJS.Fable.GlobalBindings.Excel.run (fun c -> OfficeInterop.Core.AnnotationTable.formatActive c hidecols)
                let cmd =
                    Cmd.OfPromise.attempt
                        p
                        ()
                        (curry GenericError Cmd.none >> DevMsg)
                state, model, cmd

            | TryFindAnnotationTable ->
                let p = fun () -> ExcelJS.Fable.GlobalBindings.Excel.run OfficeInterop.Core.AnnotationTable.tryGetActive
                let cmd =
                    Cmd.OfPromise.either
                        p
                        ()
                        (Option.isSome >> OfficeInterop.AnnotationTableExists >> OfficeInteropMsg)
                        (curry GenericError Cmd.none >> DevMsg)
                state, model, cmd
            | AnnotationTableExists exists ->
                let nextState = {
                    model.ExcelState with
                        HasAnnotationTable = exists
                }
                nextState, model, Cmd.none

            | InsertOntologyTerm ontologyAnnotation ->
                let cmd =
                    Cmd.OfPromise.either
                        OfficeInterop.Core.fillSelectedWithOntologyAnnotation  
                        (ontologyAnnotation)
                        (curry GenericInteropLogs Cmd.none >> DevMsg)
                        (curry GenericError Cmd.none >> DevMsg)
                state, model, cmd

            | AddAnnotationBlock compositeColumn ->
                let cmd =
                    Cmd.OfPromise.either
                        OfficeInterop.Core.Main.addCompositeColumn  
                        (compositeColumn)
                        (curry GenericInteropLogs Cmd.none >> DevMsg)
                        (curry GenericError Cmd.none >> DevMsg)
                state, model, cmd

            | AddAnnotationBlocks compositeColumn ->
                failwith "AddAnnotationBlocks not implemented yet"
                //let cmd =
                //    Cmd.OfPromise.either
                //        OfficeInterop.Core.addAnnotationBlocks
                //        compositeColumn
                //        (curry GenericInteropLogs Cmd.none >> DevMsg)
                //        (curry GenericError Cmd.none >> DevMsg)
                state, model, Cmd.none

            //| ImportFile buildingBlockTables ->
            //    let nextCmd =
            //        Cmd.OfPromise.either
            //            OfficeInterop.Core.addAnnotationBlocksInNewSheets
            //            buildingBlockTables
            //            (curry GenericInteropLogs Cmd.none >> DevMsg)
            //            (curry GenericError Cmd.none >> DevMsg)
            //    state, model, nextCmd

            | ExportJson (arcfile, jef) ->
                let jsonExport = UpdateUtil.JsonExportHelper.parseToJsonString(arcfile, jef)
                UpdateUtil.downloadFromString (jsonExport)
                state, model, Cmd.none

            | AddTemplate table ->
                let cmd =
                    Cmd.OfPromise.either
                        OfficeInterop.Core.joinTable
                        (table, Some ARCtrl.TableJoinOptions.WithValues)
                        (curry GenericInteropLogs Cmd.none >> DevMsg)
                        (curry GenericError Cmd.none >> DevMsg)
                state, model, cmd

            | JoinTable (table, options) ->
                let cmd =
                    Cmd.OfPromise.either
                        OfficeInterop.Core.joinTable
                        (table, options)
                        (curry GenericInteropLogs Cmd.none >> DevMsg)
                        (curry GenericError Cmd.none >> DevMsg)
                state, model, cmd

            | RemoveBuildingBlock ->
                let cmd =
                    Cmd.OfPromise.either
                        OfficeInterop.Core.removeSelectedAnnotationBlock
                        ()
                        (curry GenericInteropLogs Cmd.none >> DevMsg)
                        (curry GenericError Cmd.none >> DevMsg)
                state, model, cmd

            | UpdateUnitForCells ->
                let cmd =
                    Cmd.OfPromise.either
                        OfficeInterop.Core.convertBuildingBlock
                        ()
                        (curry GenericInteropLogs Cmd.none >> DevMsg)
                        (curry GenericError Cmd.none >> DevMsg)
                state, model, cmd

            | CreateAnnotationTable tryUsePrevOutput ->
                let cmd =
                    Cmd.OfPromise.either
                        OfficeInterop.Core.AnnotationTable.create  
                        (false, tryUsePrevOutput)
                        (curry GenericInteropLogs (AnnotationtableCreated |> OfficeInteropMsg |> Cmd.ofMsg) >> DevMsg) //success
                        (curry GenericError Cmd.none >> DevMsg) //error
                state, model,cmd
            | AnnotationtableCreated ->
                let nextState = {
                    model.ExcelState with
                        HasAnnotationTable = true
                }
                nextState, model, Cmd.none
            | ValidateBuildingBlock ->
                let cmd =
                    Cmd.OfPromise.either
                        OfficeInterop.Core.validateSelectedAndNeighbouringBuildingBlocks
                        ()
                        (curry GenericInteropLogs Cmd.none >> DevMsg)
                        (curry GenericError Cmd.none >> DevMsg)
                state, model, cmd

            | SendErrorsToFront msgs ->
                let cmd = Cmd.ofMsg(curry GenericInteropLogs Cmd.none msgs |> DevMsg)
                state, model, cmd

            | RectifyTermColumns ->
                let cmd =
                    Cmd.OfPromise.either
                        OfficeInterop.Core.rectifyTermColumns
                        (None, None)
                        (curry GenericInteropLogs Cmd.none >> DevMsg)
                        (curry GenericError Cmd.none >> DevMsg)
                state, model, cmd

            | UpdateFillHiddenColsState newState ->
                let nextState = {
                    model.ExcelState with
                        FillHiddenColsStateStore = newState
                }
                nextState, model, Cmd.none
            //
            | InsertFileNames fileNameList ->
                let cmd = 
                    Cmd.OfPromise.either
                        OfficeInterop.Core.insertFileNamesFromFilePicker 
                        (fileNameList)
                        (curry GenericLog Cmd.none >> DevMsg)
                        (curry GenericError Cmd.none >> DevMsg)
                state, model, cmd

            | UpdateTopLevelMetadata arcFiles ->
                let cmd =
                    Cmd.OfPromise.either
                        OfficeInterop.Core.updateTopLevelMetadata
                        (arcFiles)
                        (curry GenericInteropLogs Cmd.none >> DevMsg)
                        (curry GenericError Cmd.none >> DevMsg) //error
                state, model, cmd
            | DeleteTopLevelMetadata ->
                let cmd =
                    Cmd.OfPromise.either
                        OfficeInterop.Core.deleteTopLevelMetadata
                        ()
                        (curry GenericInteropLogs Cmd.none >> DevMsg)
                        (curry GenericError Cmd.none >> DevMsg) //error
                state, model, cmd
        try
            innerUpdate state model msg
        with
            | e -> 
                let cmd = GenericError (Cmd.none, e) |> DevMsg |> Cmd.ofMsg
                state, model, cmd