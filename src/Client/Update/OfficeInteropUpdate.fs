namespace Update

open Elmish

open Messages
open Model
open OfficeInterop
open Shared
open OfficeInteropTypes


module OfficeInterop = 
    let update (excelInteropMsg: OfficeInterop.Msg) (currentModel:Messages.Model) : Messages.Model * Cmd<Messages.Msg> =

        match excelInteropMsg with

        | AutoFitTable hidecols ->
            let p = fun () -> ExcelJS.Fable.GlobalBindings.Excel.run (OfficeInterop.Core.autoFitTable hidecols)
            let cmd =
                Cmd.OfPromise.either
                    p
                    ()
                    (curry GenericInteropLogs Cmd.none >> DevMsg)
                    (curry GenericError Cmd.none >> DevMsg)
            currentModel, cmd

        | Initialized (h,p) ->
            let welcomeMsg = sprintf "Ready to go in %s running on %s" h p

            let nextModel = {
                currentModel.ExcelState with
                    Host        = h
                    Platform    = p
            } 

            let cmd =
                Cmd.batch [
                    Cmd.ofMsg (GetAppVersion |> Request |> Api)
                    Cmd.ofMsg (FetchAllOntologies |> Request |> Api)
                    Cmd.OfPromise.either
                        OfficeInterop.Core.tryFindActiveAnnotationTable
                        ()
                        (AnnotationTableExists >> OfficeInteropMsg)
                        (curry GenericError Cmd.none >> DevMsg)
                    Cmd.ofMsg (curry GenericLog Cmd.none ("Info",welcomeMsg) |> DevMsg)
                ]

            currentModel.updateByExcelState nextModel, cmd

        | AnnotationTableExists annoTableOpt ->
            let exists =
                match annoTableOpt with
                | Success name -> true
                | _ -> false
            let nextState = {
                currentModel.ExcelState with
                    HasAnnotationTable = exists
            }

            currentModel.updateByExcelState nextState,Cmd.none

        | InsertOntologyTerm (term) ->
            let cmd =
                Cmd.OfPromise.either
                    OfficeInterop.Core.insertOntologyTerm  
                    term
                    (curry GenericLog Cmd.none >> DevMsg)
                    (curry GenericError Cmd.none >> DevMsg)
            currentModel, cmd

        | AddAnnotationBlock (minBuildingBlockInfo) ->
            let cmd =
                Cmd.OfPromise.either
                    OfficeInterop.Core.addAnnotationBlockHandler  
                    (minBuildingBlockInfo)
                    (curry GenericInteropLogs Cmd.none >> DevMsg)
                    (curry GenericError Cmd.none >> DevMsg)
            currentModel, cmd

        | AddAnnotationBlocks minBuildingBlockInfos ->
            let cmd =
                Cmd.OfPromise.either
                    OfficeInterop.Core.addAnnotationBlocks
                    minBuildingBlockInfos
                    (curry GenericInteropLogs Cmd.none >> DevMsg)
                    (curry GenericError Cmd.none >> DevMsg)
            currentModel, cmd

        | RemoveAnnotationBlock ->
            let cmd =
                Cmd.OfPromise.either
                    OfficeInterop.Core.removeSelectedAnnotationBlock
                    ()
                    (curry GenericInteropLogs Cmd.none >> DevMsg)
                    (curry GenericError Cmd.none >> DevMsg)
            currentModel, cmd

        | UpdateUnitForCells (unitTerm) ->
            let cmd =
                Cmd.OfPromise.either
                    OfficeInterop.Core.updateUnitForCells
                    unitTerm
                    (curry GenericInteropLogs Cmd.none >> DevMsg)
                    (curry GenericError Cmd.none >> DevMsg)
            currentModel, cmd

        | CreateAnnotationTable (isDark, tryUsePrevOutput) ->
            let cmd =
                Cmd.OfPromise.either
                    OfficeInterop.Core.createAnnotationTable  
                    (isDark,tryUsePrevOutput)
                    (curry GenericInteropLogs (AnnotationtableCreated |> OfficeInteropMsg |> Cmd.ofMsg) >> DevMsg)
                    (curry GenericError Cmd.none >> DevMsg)
            currentModel,cmd

        | AnnotationtableCreated ->
            let nextState = {
                currentModel.ExcelState with
                    HasAnnotationTable = true
            }
            currentModel.updateByExcelState nextState, Cmd.none


        | GetParentTerm ->
            let cmd =
                Cmd.OfPromise.either
                    OfficeInterop.Core.getParentTerm
                    ()
                    (TermSearch.StoreParentOntologyFromOfficeInterop >> TermSearchMsg)
                    (curry GenericError Cmd.none >> DevMsg)
            currentModel, cmd
        //
        | GetTableValidationXml ->
            let cmd =
                Cmd.OfPromise.either
                    OfficeInterop.Core.getTableRepresentation
                    ()
                    (Validation.StoreTableRepresentationFromOfficeInterop >> ValidationMsg)
                    (curry GenericError Cmd.none >> DevMsg)
            currentModel, cmd
        | WriteTableValidationToXml (newTableValidation,currentSwateVersion) ->
            let cmd =
                Cmd.OfPromise.either
                    OfficeInterop.Core.writeTableValidationToXml
                    (newTableValidation, currentSwateVersion)
                    (curry GenericLog (GetTableValidationXml |> OfficeInteropMsg |> Cmd.ofMsg) >> DevMsg)
                    (curry GenericError Cmd.none >> DevMsg)

            currentModel, cmd

        | DeleteAllCustomXml ->
            let cmd =
                Cmd.OfPromise.either
                    OfficeInterop.Core.deleteAllCustomXml
                    ()
                    (curry GenericLog Cmd.none >> DevMsg)
                    (curry GenericError Cmd.none >> DevMsg)
            currentModel, cmd
        | GetSwateCustomXml ->
            let cmd =
                Cmd.OfPromise.either
                    OfficeInterop.Core.getSwateCustomXml
                    ()
                    (Some >> SettingsXml.UpdateRawCustomXml >> SettingsXmlMsg)
                    (curry GenericError Cmd.none >> DevMsg)
            currentModel, cmd
        | UpdateSwateCustomXml newCustomXml ->
            let cmd =
                Cmd.OfPromise.either
                    OfficeInterop.Core.updateSwateCustomXml
                    newCustomXml
                    (curry GenericLog (OfficeInteropMsg GetSwateCustomXml |> Cmd.ofMsg) >> DevMsg)
                    (curry GenericError Cmd.none >> DevMsg)
            currentModel, cmd
        //
        | FillHiddenColsRequest ->
            let cmd =
                Cmd.OfPromise.either
                    OfficeInterop.Core.getAllAnnotationBlockDetails 
                    ()
                    (fun (searchTerms,deprecationLogs) ->
                        // Push possible deprecation messages by piping through "GenericInteropLogs"
                        GenericInteropLogs (
                            // This will be executed after "deprecationLogs" are handled by "GenericInteropLogs"
                            SearchForInsertTermsRequest searchTerms |> Request |> Api |> Cmd.ofMsg,
                            // This will be pushed to Activity logs, or as wanring modal to user in case of LogIdentifier.Warning
                            deprecationLogs
                        )
                        |> DevMsg
                    )
                    (curry GenericError (UpdateFillHiddenColsState FillHiddenColsState.Inactive |> OfficeInteropMsg |> Cmd.ofMsg) >> DevMsg)
            let stateCmd = UpdateFillHiddenColsState FillHiddenColsState.ExcelCheckHiddenCols |> OfficeInteropMsg |> Cmd.ofMsg
            let cmds = Cmd.batch [cmd; stateCmd]
            currentModel, cmds

        | FillHiddenColumns (termsWithSearchResult) ->
            let nextState = {
                currentModel.ExcelState with
                    FillHiddenColsStateStore = FillHiddenColsState.ExcelWriteFoundTerms
            }
            let cmd =
                Cmd.OfPromise.either
                    OfficeInterop.Core.UpdateTableByTermsSearchable
                    (termsWithSearchResult)
                    (curry GenericInteropLogs (UpdateFillHiddenColsState FillHiddenColsState.Inactive |> OfficeInteropMsg |> Cmd.ofMsg) >> DevMsg)
                    (curry GenericError (UpdateFillHiddenColsState FillHiddenColsState.Inactive |> OfficeInteropMsg |> Cmd.ofMsg) >> DevMsg)
            currentModel.updateByExcelState nextState, cmd


        | UpdateFillHiddenColsState newState ->
            let nextState = {
                currentModel.ExcelState with
                    FillHiddenColsStateStore = newState
            }
            currentModel.updateByExcelState nextState, Cmd.none
        //
        | InsertFileNames (fileNameList) ->
            let cmd = 
                Cmd.OfPromise.either
                    OfficeInterop.Core.insertFileNamesFromFilePicker 
                    (fileNameList)
                    (curry GenericLog Cmd.none >> DevMsg)
                    (curry GenericError Cmd.none >> DevMsg)
            currentModel, cmd

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
            currentModel, cmd

        /// DEV
        | TryExcel  ->
            let cmd = 
                Cmd.OfPromise.either
                    OfficeInterop.Core.exampleExcelFunction1
                    ()
                    ((fun x -> curry GenericLog Cmd.none ("Debug",x)) >> DevMsg)
                    (curry GenericError Cmd.none >> DevMsg)
            currentModel, cmd
        | TryExcel2 ->
            let cmd = 
                Cmd.OfPromise.either
                    OfficeInterop.Core.exampleExcelFunction2 
                    ()
                    ((fun x -> curry GenericLog Cmd.none ("Debug",x)) >> DevMsg)
                    (curry GenericError Cmd.none >> DevMsg)
            currentModel, cmd
        //| _ ->
        //    printfn "Hit currently non existing message"
        //    currentState, Cmd.none