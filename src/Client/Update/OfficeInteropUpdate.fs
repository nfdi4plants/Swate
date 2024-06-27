namespace Update

open Elmish

open Messages
open Model
open OfficeInterop
open Shared
open OfficeInteropTypes

module OfficeInterop = 
    let update (model:Messages.Model) (msg: OfficeInterop.Msg) : Messages.Model * Cmd<Messages.Msg> =

        match msg with

        | AutoFitTable hidecols ->
            let p = fun () -> ExcelJS.Fable.GlobalBindings.Excel.run (OfficeInterop.Core.autoFitTable hidecols)
            let cmd =
                Cmd.OfPromise.either
                    p
                    ()
                    (curry GenericInteropLogs Cmd.none >> DevMsg)
                    (curry GenericError Cmd.none >> DevMsg)
            model, cmd

        | TryFindAnnotationTable ->
            let cmd =
                Cmd.OfPromise.either
                    OfficeInterop.Core.tryFindActiveAnnotationTable
                    ()
                    (OfficeInterop.AnnotationTableExists >> OfficeInteropMsg)
                    (curry GenericError Cmd.none >> DevMsg)
            model, cmd
        | AnnotationTableExists annoTableOpt ->
            let exists =
                match annoTableOpt with
                | Success name -> true
                | _ -> false
            let nextState = {
                model.ExcelState with
                    HasAnnotationTable = exists
            }
            model.updateByExcelState nextState,Cmd.none

        | InsertOntologyTerm (term) ->
            let cmd =
                Cmd.OfPromise.either
                    OfficeInterop.Core.insertOntologyTerm  
                    term
                    (curry GenericLog Cmd.none >> DevMsg)
                    (curry GenericError Cmd.none >> DevMsg)
            model, cmd

        | AddAnnotationBlock (minBuildingBlockInfo) ->
            let cmd =
                Cmd.OfPromise.either
                    OfficeInterop.Core.addAnnotationBlockHandler  
                    (minBuildingBlockInfo)
                    (curry GenericInteropLogs Cmd.none >> DevMsg)
                    (curry GenericError Cmd.none >> DevMsg)
            model, cmd

        | AddAnnotationBlocks minBuildingBlockInfos ->
            let cmd =
                Cmd.OfPromise.either
                    OfficeInterop.Core.addAnnotationBlocks
                    minBuildingBlockInfos
                    (curry GenericInteropLogs Cmd.none >> DevMsg)
                    (curry GenericError Cmd.none >> DevMsg)
            model, cmd

        | ImportFile buildingBlockTables ->
            let nextCmd =
                Cmd.OfPromise.either
                    OfficeInterop.Core.addAnnotationBlocksInNewSheets
                    buildingBlockTables
                    (curry GenericInteropLogs Cmd.none >> DevMsg)
                    (curry GenericError Cmd.none >> DevMsg)
            model, nextCmd

        | RemoveBuildingBlock ->
            let cmd =
                Cmd.OfPromise.either
                    OfficeInterop.Core.removeSelectedAnnotationBlock
                    ()
                    (curry GenericInteropLogs Cmd.none >> DevMsg)
                    (curry GenericError Cmd.none >> DevMsg)
            model, cmd

        | UpdateUnitForCells (unitTerm) ->
            let cmd =
                Cmd.OfPromise.either
                    OfficeInterop.Core.updateUnitForCells
                    unitTerm
                    (curry GenericInteropLogs Cmd.none >> DevMsg)
                    (curry GenericError Cmd.none >> DevMsg)
            model, cmd

        | CreateAnnotationTable(tryUsePrevOutput) ->
            let cmd =
                Cmd.OfPromise.either
                    OfficeInterop.Core.createAnnotationTable  
                    (false,tryUsePrevOutput)
                    (curry GenericInteropLogs (AnnotationtableCreated |> OfficeInteropMsg |> Cmd.ofMsg) >> DevMsg)
                    (curry GenericError Cmd.none >> DevMsg)
            model,cmd

        | AnnotationtableCreated ->
            let nextState = {
                model.ExcelState with
                    HasAnnotationTable = true
            }
            model.updateByExcelState nextState, Cmd.none


        | GetParentTerm ->
            let cmd =
                Cmd.OfPromise.either
                    OfficeInterop.Core.getParentTerm
                    ()
                    (fun tmin -> tmin |> Option.map (fun t -> ARCtrl.OntologyAnnotation.fromTerm t.toTerm) |> TermSearch.UpdateParentTerm |> TermSearchMsg)
                    (curry GenericError Cmd.none >> DevMsg)
            model, cmd
        //
        | FillHiddenColsRequest ->
            failwith "FillHiddenColsRequest Not implemented yet"
            //let cmd =
            //    Cmd.OfPromise.either
            //        OfficeInterop.Core.getAllAnnotationBlockDetails 
            //        ()
            //        (fun (searchTerms,deprecationLogs) ->
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
            model, Cmd.none

        | FillHiddenColumns (termsWithSearchResult) ->
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
            model.updateByExcelState nextState, cmd


        | UpdateFillHiddenColsState newState ->
            let nextState = {
                model.ExcelState with
                    FillHiddenColsStateStore = newState
            }
            model.updateByExcelState nextState, Cmd.none
        //
        | InsertFileNames (fileNameList) ->
            let cmd = 
                Cmd.OfPromise.either
                    OfficeInterop.Core.insertFileNamesFromFilePicker 
                    (fileNameList)
                    (curry GenericLog Cmd.none >> DevMsg)
                    (curry GenericError Cmd.none >> DevMsg)
            model, cmd

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
            model, cmd

        // DEV
        | TryExcel  ->
            let cmd = 
                Cmd.OfPromise.either
                    OfficeInterop.Core.exampleExcelFunction1
                    ()
                    ((fun x -> curry GenericLog Cmd.none ("Debug",x)) >> DevMsg)
                    (curry GenericError Cmd.none >> DevMsg)
            model, cmd
        | TryExcel2 ->
            let cmd = 
                Cmd.OfPromise.either
                    OfficeInterop.Core.exampleExcelFunction2 
                    ()
                    ((fun x -> curry GenericLog Cmd.none ("Debug",x)) >> DevMsg)
                    (curry GenericError Cmd.none >> DevMsg)
            model, cmd
        //| _ ->
        //    printfn "Hit currently non existing message"
        //    currentState, Cmd.none