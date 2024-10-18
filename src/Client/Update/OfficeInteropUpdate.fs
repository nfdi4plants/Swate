namespace Update

open Elmish

open Messages
open OfficeInterop
open Shared
open OfficeInteropTypes
open Model

module OfficeInterop = 
    let update (state: OfficeInterop.Model) (model:Model) (msg: OfficeInterop.Msg) : OfficeInterop.Model * Model * Cmd<Messages.Msg> =

        let innerUpdate (state: OfficeInterop.Model) (model: Model) (msg: OfficeInterop.Msg) =

            match msg with

            | AutoFitTable hidecols ->
                let p = fun () -> ExcelJS.Fable.GlobalBindings.Excel.run (OfficeInterop.Core.autoFitTable hidecols)
                let cmd =
                    Cmd.OfPromise.either
                        p
                        ()
                        (curry GenericInteropLogs Cmd.none >> DevMsg)
                        (curry GenericError Cmd.none >> DevMsg)
                state, model, cmd

            | TryFindAnnotationTable ->
                let cmd =
                    Cmd.OfPromise.either
                        OfficeInterop.Core.tryFindActiveAnnotationTable
                        ()
                        (OfficeInterop.AnnotationTableExists >> OfficeInteropMsg)
                        (curry GenericError Cmd.none >> DevMsg)
                state, model, cmd
            | AnnotationTableExists annoTableOpt ->
                let exists =
                    match annoTableOpt with
                    | Success name -> true
                    | _ -> false
                let nextState = {
                    model.ExcelState with
                        HasAnnotationTable = exists
                }
                nextState, model, Cmd.none

            | InsertOntologyTerm term ->
                let cmd =
                    Cmd.OfPromise.either
                        OfficeInterop.Core.insertOntologyTerm  
                        term
                        (curry GenericLog Cmd.none >> DevMsg)
                        (curry GenericError Cmd.none >> DevMsg)
                state, model, cmd

            | AddAnnotationBlock compositeColumn ->
                let cmd =
                    Cmd.OfPromise.either
                        OfficeInterop.Core.addAnnotationBlockHandler  
                        compositeColumn
                        (curry GenericInteropLogs Cmd.none >> DevMsg)
                        (curry GenericError Cmd.none >> DevMsg)
                state, model, cmd

            | AddAnnotationBlocks compositeColumn ->
                let cmd =
                    Cmd.OfPromise.either
                        OfficeInterop.Core.addAnnotationBlocks
                        compositeColumn
                        (curry GenericInteropLogs Cmd.none >> DevMsg)
                        (curry GenericError Cmd.none >> DevMsg)
                state, model, cmd

            | ImportFile buildingBlockTables ->
                let nextCmd =
                    Cmd.OfPromise.either
                        OfficeInterop.Core.addAnnotationBlocksInNewSheets
                        buildingBlockTables
                        (curry GenericInteropLogs Cmd.none >> DevMsg)
                        (curry GenericError Cmd.none >> DevMsg)
                state, model, nextCmd

            | AddTemplate table ->
                let msg = fun (t, i) -> JoinTable(t, i, Some ARCtrl.TableJoinOptions.WithValues) |> OfficeInteropMsg
                let cmd =
                    Cmd.OfPromise.either
                        OfficeInterop.Core.prepareTemplateInMemory
                        (table)
                        (msg)                        
                        (curry GenericError Cmd.none >> DevMsg)
                state, model, cmd

            | JoinTable (table, index, options) ->
                let cmd =
                    Cmd.OfPromise.either
                        OfficeInterop.Core.joinTable
                        (table, index, options)
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
                        OfficeInterop.Core.createAnnotationTable  
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

            | GetParentTerm ->
                let cmd =
                    Cmd.OfPromise.either
                        OfficeInterop.Core.getParentTerm
                        ()
                        (fun tmin -> tmin |> Option.map (fun t -> ARCtrl.OntologyAnnotation.fromTerm t.toTerm) |> TermSearch.UpdateParentTerm |> TermSearchMsg)
                        (curry GenericError Cmd.none >> DevMsg)
                state, model, cmd

            | RectifyTermColumns ->
                //failwith "FillHiddenColsRequest Not implemented yet"
                //let cmd =
                //    Cmd.OfPromise.either
                //        OfficeInterop.Core.getAllAnnotationBlockDetails 
                //        ()
                //        (fun (searchTerms, deprecationLogs) ->
                //            // Push possible deprecation messages by piping through "GenericInteropLogs"
                //            GenericInteropLogs (
                //                // This will be executed after "deprecationLogs" are handled by "GenericInteropLogs"
                //                SearchForInsertTermsRequest searchTerms |> Request |> Api |> Cmd.ofMsg,
                //                // This will be pushed to Activity logs, or as wanring modal to user in case of LogIdentifier.Warning
                //                deprecationLogs
                //            )
                //            |> DevMsg
                //        )
                //        (curry GenericError (UpdateFillHiddenColsState FillHiddenColsState.Inactive |> OfficeInteropMsg |> Cmd.ofMsg) >> DevMsg)
                //let stateCmd = UpdateFillHiddenColsState FillHiddenColsState.ExcelCheckHiddenCols |> OfficeInteropMsg |> Cmd.ofMsg
                //let cmds = Cmd.batch [cmd; stateCmd]
                let cmd =
                    Cmd.OfPromise.either
                        OfficeInterop.Core.rectifyTermColumns
                        ()
                        (curry GenericInteropLogs Cmd.none >> DevMsg)
                        (curry GenericError Cmd.none >> DevMsg)
                state, model, cmd

            | FillHiddenColumns termsWithSearchResult ->
                let nextState = {
                    model.ExcelState with
                        FillHiddenColsStateStore = FillHiddenColsState.ExcelWriteFoundTerms
                }
                let cmd =
                    Cmd.OfPromise.either
                        OfficeInterop.Core.UpdateTableByTermsSearchable
                        (termsWithSearchResult)
                        (curry GenericInteropLogs (UpdateFillHiddenColsState FillHiddenColsState.Inactive |> OfficeInteropMsg |> Cmd.ofMsg) >> DevMsg)
                        (curry GenericError (UpdateFillHiddenColsState FillHiddenColsState.Inactive |> OfficeInteropMsg |> Cmd.ofMsg) >> DevMsg)
                nextState, model, cmd


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

            //
            | GetSelectedBuildingBlockTerms ->
                let cmd =
                    Cmd.OfPromise.either
                        OfficeInterop.Core.getAnnotationBlockDetails
                        ()
                        (fun x ->
                            let msg = InteropLogging.Msg.create InteropLogging.Debug $"{x}"
                            Msg.Batch [
                                curry GenericInteropLogs Cmd.none [msg] |> DevMsg
                                GetSelectedBuildingBlockTermsRequest x |> BuildingBlockDetails
                            ]
                        )
                        (curry GenericError (UpdateCurrentRequestState RequestBuildingBlockInfoStates.Inactive |> BuildingBlockDetails |> Cmd.ofMsg) >> DevMsg)
                state, model, cmd
            | CreateTopLevelMetadata workSheetName ->
                let cmd =
                    Cmd.OfPromise.either
                        OfficeInterop.Core.createTopLevelMetadata
                        (workSheetName)
                        (curry GenericInteropLogs Cmd.none >> DevMsg)
                        (curry GenericError Cmd.none >> DevMsg) //error
                state, model, cmd
            | UpdateTopLevelMetadata arcFiles ->
                let cmd =
                    Cmd.OfPromise.either
                        OfficeInterop.Core.updateTopLevelMetadata
                        (arcFiles)
                        (curry GenericInteropLogs Cmd.none >> DevMsg)
                        (curry GenericError Cmd.none >> DevMsg) //error
                state, model, cmd
            | DeleteTopLevelMetadata identifier ->
                let cmd =
                    Cmd.OfPromise.either
                        OfficeInterop.Core.deleteTopLevelMetadata
                        (identifier)
                        (curry GenericInteropLogs Cmd.none >> DevMsg)
                        (curry GenericError Cmd.none >> DevMsg) //error
                state, model, cmd

            // DEV
            | TryExcel  ->
                let cmd = 
                    Cmd.OfPromise.either
                        OfficeInterop.Core.exampleExcelFunction1
                        ()
                        ((fun x -> curry GenericLog Cmd.none ("Debug", x)) >> DevMsg)
                        (curry GenericError Cmd.none >> DevMsg)
                state, model, cmd
            | TryExcel2 ->
                let cmd = 
                    Cmd.OfPromise.either
                        OfficeInterop.Core.exampleExcelFunction2 
                        ()
                        ((fun x -> curry GenericLog Cmd.none ("Debug", x)) >> DevMsg)
                        (curry GenericError Cmd.none >> DevMsg)
                state, model, cmd
        try
            innerUpdate state model msg
        with
            | e -> 
                let cmd = GenericError (Cmd.none, e) |> DevMsg |> Cmd.ofMsg
                state, model, cmd