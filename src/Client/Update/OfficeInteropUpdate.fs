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

        | AutoFitTable ->
            let p = fun () -> ExcelJS.Fable.GlobalBindings.Excel.run OfficeInterop.autoFitTable
            let cmd =
                Cmd.OfPromise.either
                    p
                    ()
                    (GenericInteropLogs >> Dev)
                    (GenericError >> Dev)
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
                        (GenericError >> Dev)
                    Cmd.ofMsg (("Info",welcomeMsg) |> (GenericLog >> Dev))
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
                    (GenericLog >> Dev)
                    (GenericError >> Dev)
            currentModel, cmd

        | AddAnnotationBlock (minBuildingBlockInfo) ->
            let cmd =
                Cmd.OfPromise.either
                    OfficeInterop.addAnnotationBlock  
                    (minBuildingBlockInfo)
                    (GenericInteropLogs >> Dev)
                    (GenericError >> Dev)
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
                    (GenericInteropLogs >> Dev)
                    (GenericError >> Dev)
            currentModel, cmd

        | RemoveAnnotationBlock ->
            let cmd =
                Cmd.OfPromise.either
                    OfficeInterop.removeSelectedAnnotationBlock
                    ()
                    (GenericInteropLogs >> Dev)
                    (GenericError >> Dev)
            currentModel, cmd

        | UpdateUnitForCells (unitTerm) ->
            let cmd =
                Cmd.OfPromise.either
                    OfficeInterop.updateUnitForCells
                    unitTerm
                    (GenericInteropLogs >> Dev)
                    (GenericError >> Dev)
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
                    (fun msg ->
                        Msg.Batch [
                            GenericLog ("info", msg) |> Dev
                            AnnotationtableCreated (msg) |> OfficeInteropMsg
                        ]
                    )
                    (GenericError >> Dev)
            currentModel,cmd

        | AnnotationtableCreated (range) ->
            let nextState = {
                currentModel.ExcelState with
                    HasAnnotationTable = true
            }
            //let msg =
            //    Msg.Batch [
            //        //AutoFitTable |> ExcelInterop
            //        //UpdateProtocolGroupHeader |> ExcelInterop
            //        GenericLog ("info", range) |> Dev
            //    ]
            currentModel.updateByExcelState nextState, Cmd.none


        | GetParentTerm ->
            let cmd =
                Cmd.OfPromise.either
                    OfficeInterop.getParentTerm
                    ()
                    (TermSearch.StoreParentOntologyFromOfficeInterop >> TermSearchMsg)
                    (GenericError >> Dev)
            currentModel, cmd
        //
        | GetTableValidationXml ->
            let cmd =
                Cmd.OfPromise.either
                    OfficeInterop.getTableRepresentation
                    ()
                    (fun (currentTableValidation, buildingBlocks,msg) ->
                        Validation.StoreTableRepresentationFromOfficeInterop (currentTableValidation, buildingBlocks) |> ValidationMsg)
                    (GenericError >> Dev)
            currentModel, cmd
        | WriteTableValidationToXml (newTableValidation,currentSwateVersion) ->
            let cmd =
                Cmd.OfPromise.either
                    OfficeInterop.writeTableValidationToXml
                    (newTableValidation, currentSwateVersion)
                    (fun x ->
                        Msg.Batch [
                            GenericLog x |> Dev
                            GetTableValidationXml |> OfficeInteropMsg
                        ]
                    )
                    (GenericError >> Dev)

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
                    (GenericLog >> Dev)
                    (GenericError >> Dev)
            currentModel, cmd
        | GetSwateCustomXml ->
            failwith """Function "GetSwateCustomXml" is currently not supported."""
            //let cmd =
            //    Cmd.OfPromise.either
            //        OfficeInterop.getSwateCustomXml
            //        ()
            //        (fun xml ->
            //            Msg.Batch [
            //                GenericLog xml |> Dev
            //                UpdateRawCustomXml (snd xml) |> SettingsXmlMsg
            //            ]
            //        )
            //        (GenericError >> Dev)
            currentModel, Cmd.none//cmd
        | UpdateSwateCustomXml newCustomXml ->
            let cmd =
                Cmd.OfPromise.either
                    OfficeInterop.updateSwateCustomXml
                    newCustomXml
                    (fun x ->
                        Msg.Batch [
                            x |> (GenericLog >> Dev)
                            GetSwateCustomXml |> OfficeInteropMsg
                        ]
                    )
                    (GenericError >> Dev)
            currentModel, cmd
        //
        | FillHiddenColsRequest ->
            let cmd =
                Cmd.OfPromise.either
                    OfficeInterop.getAllAnnotationBlockDetails 
                    ()
                    (SearchForInsertTermsRequest >> Request >> Api)
                    (fun e ->
                        Msg.Batch [
                            UpdateFillHiddenColsState FillHiddenColsState.Inactive |> OfficeInteropMsg
                            GenericError e |> Dev
                        ] )
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
                    (fun msg ->
                        Msg.Batch [
                            UpdateFillHiddenColsState FillHiddenColsState.Inactive |> OfficeInteropMsg
                            GenericInteropLogs msg |> Dev
                        ]
                    )
                    (fun e ->
                        Msg.Batch [
                            UpdateFillHiddenColsState FillHiddenColsState.Inactive |> OfficeInteropMsg
                            GenericError e |> Dev
                        ] )
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
                    ((fun x -> 
                        ("Debug",x) |> GenericLog) >> Dev
                    )
                    (GenericError >> Dev)
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
                            GenericInteropLogs [msg] |> Dev
                            GetSelectedBuildingBlockTermsRequest x |> BuildingBlockDetails
                        ]
                    )
                    //(GetSelectedBuildingBlockSearchTermsRequest >> BuildingBlockDetails)
                    (fun x ->
                        Msg.Batch [
                            GenericError x |> Dev
                            UpdateCurrentRequestState RequestBuildingBlockInfoStates.Inactive |> BuildingBlockDetails
                        ]
                    )
            //let cmd2 = Cmd.ofMsg (UpdateCurrentRequestState RequestBuildingBlockInfoStates.RequestExcelInformation |> BuildingBlockDetails) 
            currentModel, cmd
        //
        | CreatePointerJson ->
            let cmd =
                Cmd.OfPromise.either
                    OfficeInterop.createPointerJson
                    ()
                    (fun x -> Some x |> UpdatePointerJson |> SettingDataStewardMsg)
                    (GenericError >> Dev)
            currentModel, cmd

        /// DEV
        | TryExcel  ->
            let cmd = 
                Cmd.OfPromise.either
                    OfficeInterop.exampleExcelFunction1
                    ()
                    ((fun x -> 
                        ("Debug",x) |> GenericLog) >> Dev
                    )
                    (GenericError >> Dev)
            currentModel, cmd
        | TryExcel2 ->
            let cmd = 
                Cmd.OfPromise.either
                    OfficeInterop.exampleExcelFunction2 
                    ()
                    ((fun x -> 
                        ("Debug",x) |> GenericLog) >> Dev
                    )
                    (GenericError >> Dev)
            currentModel, cmd
        //| _ ->
        //    printfn "Hit currently non existing message"
        //    currentState, Cmd.none