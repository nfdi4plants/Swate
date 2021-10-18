namespace Update

open Elmish

open Messages
open Model
open OfficeInterop
open Thoth.Elmish

open Shared
open OfficeInteropTypes


module OfficeInterop = 
    let update (excelInteropMsg: OfficeInterop.Msg) (currentModel:Messages.Model) : Messages.Model * Cmd<Messages.Msg> =

        match excelInteropMsg with

        | AutoFitTable hidecols ->
            let p = fun () -> ExcelJS.Fable.GlobalBindings.Excel.run (OfficeInterop.autoFitTable hidecols)
            let cmd =
                Cmd.OfPromise.either
                    p
                    ()
                    (curry GenericInteropLogs Cmd.none >> Dev)
                    (curry GenericError Cmd.none >> Dev)
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
                        OfficeInterop.tryFindActiveAnnotationTable
                        ()
                        (AnnotationTableExists >> OfficeInteropMsg)
                        (curry GenericError Cmd.none >> Dev)
                    Cmd.ofMsg (curry GenericLog Cmd.none ("Info",welcomeMsg) |> Dev)
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
                    OfficeInterop.insertOntologyTerm  
                    term
                    (curry GenericLog Cmd.none >> Dev)
                    (curry GenericError Cmd.none >> Dev)
            currentModel, cmd

        | AddAnnotationBlock (minBuildingBlockInfo) ->
            let cmd =
                Cmd.OfPromise.either
                    OfficeInterop.addAnnotationBlock  
                    (minBuildingBlockInfo)
                    (curry GenericInteropLogs Cmd.none >> Dev)
                    (curry GenericError Cmd.none >> Dev)
            currentModel, cmd

        | AddAnnotationBlocks minBuildingBlockInfos ->
            //let cmd =
            //    Cmd.OfPromise.either
            //        OfficeInterop.addAnnotationBlocksAsProtocol
            //        (minBuildingBlockInfos,protocol)
            //        (fun (resList,protocolInfo) ->
            //            let newColNames = resList |> List.map (fun (names,_,_) -> names)
            //            let changeColFormatInfos,msg = resList |> List.map (fun (names,format,msg) -> (names,format), msg ) |> List.unzip
            //            Msg.Batch [
            //                FormatColumns (changeColFormatInfos) |> ExcelInterop
            //                GenericLog ("Info", msg |> String.concat "; ") |> Dev
            //                /// This is currently used for protocol template insert from database
            //                if validationOpt.IsSome then
            //                    /// tableValidation is retrived from database and does not contain correct tablename and worksheetname.
            //                    /// But it is updated during 'addAnnotationBlocksAsProtocol' with the active annotationtable
            //                    /// The next step can be redesigned, as the protocol is also passed to 'AddTableValidationtoExisting'
            //                    let updatedValidation = {validationOpt.Value with AnnotationTable = Shared.AnnotationTable.create protocolInfo.AnnotationTable.Name protocolInfo.AnnotationTable.Worksheet}
            //                    AddTableValidationtoExisting (updatedValidation, newColNames, protocolInfo) |> ExcelInterop
            //                else
            //                    WriteProtocolToXml protocolInfo |> ExcelInterop
            //            ]
            //        )
            //        (GenericError >> Dev)
            let cmd =
                Cmd.OfPromise.either
                    OfficeInterop.addAnnotationBlocks
                    minBuildingBlockInfos
                    (curry GenericInteropLogs Cmd.none >> Dev)
                    (curry GenericError Cmd.none >> Dev)
            currentModel, cmd

        | RemoveAnnotationBlock ->
            let cmd =
                Cmd.OfPromise.either
                    OfficeInterop.removeSelectedAnnotationBlock
                    ()
                    (curry GenericInteropLogs Cmd.none >> Dev)
                    (curry GenericError Cmd.none >> Dev)
            currentModel, cmd

        | UpdateUnitForCells (unitTerm) ->
            let cmd =
                Cmd.OfPromise.either
                    OfficeInterop.updateUnitForCells
                    unitTerm
                    (curry GenericInteropLogs Cmd.none >> Dev)
                    (curry GenericError Cmd.none >> Dev)
            currentModel, cmd

        //| FormatColumn (colName,format) ->
        //    let cmd =
        //        Cmd.OfPromise.either
        //            (OfficeInterop.changeTableColumnFormat colName)
        //            format
        //            (fun x ->
        //                Msg.Batch [
        //                    AutoFitTable |> ExcelInterop
        //                    GenericLog x |> Dev
        //                ]
        //            )
        //            (GenericError >> Dev)
        //    currentModel,cmd

        //| FormatColumns (resList) ->
        //    let cmd =
        //        Cmd.OfPromise.either
        //            OfficeInterop.changeTableColumnsFormat
        //            resList
        //            (fun x ->
        //                Msg.Batch [
        //                    AutoFitTable |> ExcelInterop
        //                    GenericLog x |> Dev
        //                ]
        //            )
        //            (GenericError >> Dev)
        //    currentModel,cmd

        | CreateAnnotationTable (isDark) ->
            let cmd =
                Cmd.OfPromise.either
                    OfficeInterop.createAnnotationTable  
                    (isDark)
                    (curry GenericLog (AnnotationtableCreated |> OfficeInteropMsg |> Cmd.ofMsg) >> Dev)
                    (curry GenericError Cmd.none >> Dev)
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
                    OfficeInterop.getParentTerm
                    ()
                    (TermSearch.StoreParentOntologyFromOfficeInterop >> TermSearchMsg)
                    (curry GenericError Cmd.none >> Dev)
            currentModel, cmd
        //
        | GetTableValidationXml ->
            let cmd =
                Cmd.OfPromise.either
                    OfficeInterop.getTableRepresentation
                    ()
                    (Validation.StoreTableRepresentationFromOfficeInterop >> ValidationMsg)
                    (curry GenericError Cmd.none >> Dev)
            currentModel, cmd
        | WriteTableValidationToXml (newTableValidation,currentSwateVersion) ->
            let cmd =
                Cmd.OfPromise.either
                    OfficeInterop.writeTableValidationToXml
                    (newTableValidation, currentSwateVersion)
                    (curry GenericLog (GetTableValidationXml |> OfficeInteropMsg |> Cmd.ofMsg) >> Dev)
                    (curry GenericError Cmd.none >> Dev)

            currentModel, cmd

        //| AddTableValidationtoExisting (newTableValidation, newColNames, protocolInfo) ->
        //    failwith """Function "AddTableValidationtoExisting" is currently not supported."""
        //    //let cmd =
        //    //    Cmd.OfPromise.either
        //    //        OfficeInterop.addTableValidationToExisting
        //    //        (newTableValidation, newColNames)
        //    //        (fun x ->
        //    //            Msg.Batch [
        //    //                GenericLog x |> Dev
        //    //                WriteProtocolToXml protocolInfo |> ExcelInterop
        //    //            ]
        //    //        )
        //    //        (GenericError >> Dev)
        //    currentModel, Cmd.none

        //| WriteProtocolToXml protocolInfo ->
        //    let cmd =
        //        Cmd.OfPromise.either
        //            OfficeInterop.writeProtocolToXml
        //            (protocolInfo)
        //            (fun res ->
        //                Msg.Batch [
        //                    GenericLog res |> Dev
        //                    UpdateProtocolGroupHeader |> ExcelInterop
        //                    if currentModel.PageState.CurrentPage = Route.SettingsProtocol then GetActiveProtocolGroupXmlParsed |> SettingsProtocolMsg 
        //                ]
        //            )
        //            (GenericError >> Dev)
        //    currentModel, cmd
        | DeleteAllCustomXml ->
            let cmd =
                Cmd.OfPromise.either
                    OfficeInterop.deleteAllCustomXml
                    ()
                    (curry GenericLog Cmd.none >> Dev)
                    (curry GenericError Cmd.none >> Dev)
            currentModel, cmd
        | GetSwateCustomXml ->
            let cmd =
                Cmd.OfPromise.either
                    OfficeInterop.getSwateCustomXml
                    ()
                    (Some >> SettingsXml.UpdateRawCustomXml >> SettingsXmlMsg)
                    (curry GenericError Cmd.none >> Dev)
            currentModel, cmd
        | UpdateSwateCustomXml newCustomXml ->
            let cmd =
                Cmd.OfPromise.either
                    OfficeInterop.updateSwateCustomXml
                    newCustomXml
                    (curry GenericLog (OfficeInteropMsg GetSwateCustomXml |> Cmd.ofMsg) >> Dev)
                    (curry GenericError Cmd.none >> Dev)
            currentModel, cmd
        //
        | FillHiddenColsRequest ->
            let cmd =
                Cmd.OfPromise.either
                    OfficeInterop.getAllAnnotationBlockDetails 
                    ()
                    (SearchForInsertTermsRequest >> Request >> Api)
                    (curry GenericError (UpdateFillHiddenColsState FillHiddenColsState.Inactive |> OfficeInteropMsg |> Cmd.ofMsg) >> Dev)
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
                    OfficeInterop.UpdateTableByTermsSearchable
                    (termsWithSearchResult)
                    (curry GenericInteropLogs (UpdateFillHiddenColsState FillHiddenColsState.Inactive |> OfficeInteropMsg |> Cmd.ofMsg) >> Dev)
                    (curry GenericError (UpdateFillHiddenColsState FillHiddenColsState.Inactive |> OfficeInteropMsg |> Cmd.ofMsg) >> Dev)
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
                    OfficeInterop.insertFileNamesFromFilePicker 
                    (fileNameList)
                    (curry GenericLog Cmd.none >> Dev)
                    (curry GenericError Cmd.none >> Dev)
            currentModel, cmd

        //
        | GetSelectedBuildingBlockTerms ->
            let cmd =
                Cmd.OfPromise.either
                    OfficeInterop.getAnnotationBlockDetails
                    ()
                    (fun x ->
                        let msg = InteropLogging.Msg.create InteropLogging.Debug $"{x}"
                        Msg.Batch [
                            curry GenericInteropLogs Cmd.none [msg] |> Dev
                            GetSelectedBuildingBlockTermsRequest x |> BuildingBlockDetails
                        ]
                    )
                    (curry GenericError (UpdateCurrentRequestState RequestBuildingBlockInfoStates.Inactive |> BuildingBlockDetails |> Cmd.ofMsg) >> Dev)
            currentModel, cmd
        //
        | CreatePointerJson ->
            let cmd =
                Cmd.OfPromise.either
                    OfficeInterop.createPointerJson
                    ()
                    (fun x -> Some x |> UpdatePointerJson |> SettingDataStewardMsg)
                    (curry GenericError Cmd.none >> Dev)
            currentModel, cmd

        /// DEV
        | TryExcel  ->
            let cmd = 
                Cmd.OfPromise.either
                    OfficeInterop.exampleExcelFunction1
                    ()
                    ((fun x -> curry GenericLog Cmd.none ("Debug",x)) >> Dev)
                    (curry GenericError Cmd.none >> Dev)
            currentModel, cmd
        | TryExcel2 ->
            let cmd = 
                Cmd.OfPromise.either
                    OfficeInterop.exampleExcelFunction2 
                    ()
                    ((fun x -> curry GenericLog Cmd.none ("Debug",x)) >> Dev)
                    (curry GenericError Cmd.none >> Dev)
            currentModel, cmd
        //| _ ->
        //    printfn "Hit currently non existing message"
        //    currentState, Cmd.none