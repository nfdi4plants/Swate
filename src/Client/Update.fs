module Update

open Elmish
open Elmish.Navigation
open Thoth.Elmish
open System.Text.RegularExpressions

open Shared
open Routing
open Model
open Messages

open OfficeInterop.Types

/// This function matches a OfficeInterop.TryFindAnnoTableResult to either Success or Error
/// If Success it will pipe the tableName on to the msg input paramter.
/// If Error it will pipe the error message to GenericLog ("Error",errorMsg).
let matchActiveTableResToMsg activeTableNameRes (msg:string -> Cmd<Msg>) =
    match activeTableNameRes with
    | Success tableName ->
        msg tableName
    | Error eMsg ->
        Msg.Batch [
            UpdateFillHiddenColsState FillHiddenColsState.Inactive |> ExcelInterop
            GenericLog ("Error",eMsg) |> Dev
        ] |> Cmd.ofMsg

let urlUpdate (route: Route option) (currentModel:Model) : Model * Cmd<Msg> =
    match route with
    | Some page ->
        let nextPageState = {
            currentModel.PageState with
                CurrentPage = page
                CurrentUrl  = Route.toRouteUrl page
        }

        let nextModel = {
            currentModel with
                PageState = nextPageState
        }
        nextModel,Cmd.none
    | None ->
        //Browser.console.error("Error parsing url")
        let nextPageState = {
            currentModel.PageState with
                CurrentPage = Route.NotFound
        }

        let nextModel = {
            currentModel with
                PageState = nextPageState
        }
        nextModel,Cmd.none

let handleExcelInteropMsg (excelInteropMsg: ExcelInteropMsg) (currentModel:Model) : Model * Cmd<Msg> =

    match excelInteropMsg with

    | AutoFitTable ->
        let cmd =
            Cmd.OfPromise.either
                OfficeInterop.autoFitTable
                ()
                (GenericLog >> Dev)
                (GenericError >> Dev)
        currentModel, cmd

    | UpdateProtocolGroupHeader ->
        //let cmd =
        //    Cmd.OfPromise.either
        //        OfficeInterop.updateProtocolGroupHeader
        //        ()
        //        (GenericLog >> Dev)
        //        (GenericError >> Dev)
        failwith """Function "UpdateProtocolGroupHeader" is currently not supported."""
        currentModel, Cmd.none

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
                    (AnnotationTableExists >> ExcelInterop)
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

    | FillSelection (fillValue,fillTerm) ->
        let cmd =
            Cmd.OfPromise.either
                OfficeInterop.fillValue  
                (fillValue,fillTerm)
                (GenericLog >> Dev)
                (GenericError >> Dev)
        currentModel, cmd

    | AddAnnotationBlock (minBuildingBlockInfo) ->
        failwith """Function "AddAnnotationBlock" is currently not supported."""
        //let cmd =
        //    Cmd.OfPromise.either
        //        OfficeInterop.addAnnotationBlock  
        //        (minBuildingBlockInfo)
        //        (fun (newColName,format,msg) ->
        //            Msg.Batch [
        //                GenericLog ("Info",msg) |> Dev
        //                FormatColumn (newColName,format) |> ExcelInterop
        //                UpdateProtocolGroupHeader |> ExcelInterop
        //            ]
        //        )
        //        (GenericError >> Dev)
        currentModel, Cmd.none

    | AddAnnotationBlocks (minBuildingBlockInfos, protocol, validationOpt) ->
        failwith """Function "AddAnnotationBlocks" is currently not supported."""
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
        currentModel, Cmd.none

    | RemoveAnnotationBlock ->
        failwith """Function "RemoveAnnotationBlock" is currently not supported."""
        //let cmd =
        //    Cmd.OfPromise.either
        //        OfficeInterop.removeSelectedAnnotationBlock
        //        ()
        //        (fun msg ->
        //            Msg.Batch [
        //                GenericLog ("Info",msg) |> Dev
        //                AutoFitTable |> ExcelInterop
        //                UpdateProtocolGroupHeader |> ExcelInterop
        //            ]
        //        )
        //        (GenericError >> Dev)
        currentModel, Cmd.none

    | AddUnitToAnnotationBlock (format, unitTermOpt) ->
        failwith """Function "AddUnitToAnnotationBlock" is currently not supported."""
        //let cmd =
        //    Cmd.OfPromise.either
        //        OfficeInterop.addUnitToExistingBuildingBlock
        //        (format,unitTermOpt)
        //        (fun (newColName,format) ->
        //            Msg.Batch [
        //                FormatColumn (newColName, format) |> ExcelInterop
        //                UpdateProtocolGroupHeader |> ExcelInterop
        //            ]
        //        )
        //        (GenericError >> Dev)
        currentModel, Cmd.none

    | FormatColumn (colName,format) ->
        let cmd =
            Cmd.OfPromise.either
                (OfficeInterop.changeTableColumnFormat colName)
                format
                (fun x ->
                    Msg.Batch [
                        AutoFitTable |> ExcelInterop
                        GenericLog x |> Dev
                    ]
                )
                (GenericError >> Dev)
        currentModel,cmd

    | FormatColumns (resList) ->
        let cmd =
            Cmd.OfPromise.either
                OfficeInterop.changeTableColumnsFormat
                resList
                (fun x ->
                    Msg.Batch [
                        AutoFitTable |> ExcelInterop
                        GenericLog x |> Dev
                    ]
                )
                (GenericError >> Dev)
        currentModel,cmd

    | CreateAnnotationTable (isDark) ->
        let cmd =
            Cmd.OfPromise.either
                OfficeInterop.createAnnotationTable  
                (isDark)
                (fun (res,msg) ->
                    AnnotationtableCreated (msg) |> ExcelInterop
                )
                (GenericError >> Dev)
        currentModel,cmd

    | AnnotationtableCreated (range) ->
        let nextState = {
            currentModel.ExcelState with
                HasAnnotationTable = true
        }
        let msg =
            Msg.Batch [
                AutoFitTable |> ExcelInterop
                UpdateProtocolGroupHeader |> ExcelInterop
                GenericLog ("info", range) |> Dev
            ]
        currentModel.updateByExcelState nextState, Cmd.ofMsg msg


    | GetParentTerm ->
        let cmd =
            Cmd.OfPromise.either
                OfficeInterop.getParentTerm
                ()
                (StoreParentOntologyFromOfficeInterop >> TermSearch)
                (GenericError >> Dev)
        currentModel, cmd
    //
    | GetTableValidationXml ->
        failwith """Function "GetTableValidationXml" is currently not supported."""
        //let cmd =
        //    Cmd.OfPromise.either
        //        OfficeInterop.getTableRepresentation
        //        ()
        //        (fun (currentTableValidation, buildingBlocks,msg) ->
        //            StoreTableRepresentationFromOfficeInterop (currentTableValidation, buildingBlocks, msg) |> Validation)
        //        (GenericError >> Dev)
        currentModel, Cmd.none
    | WriteTableValidationToXml (newTableValidation,currentSwateVersion) ->
        let cmd =
            Cmd.OfPromise.either
                OfficeInterop.writeTableValidationToXml
                (newTableValidation, currentSwateVersion)
                (fun x ->
                    Msg.Batch [
                        GenericLog x |> Dev
                        GetTableValidationXml |> ExcelInterop
                    ]
                )
                (GenericError >> Dev)

        currentModel, cmd

    | AddTableValidationtoExisting (newTableValidation, newColNames, protocolInfo) ->
        let cmd =
            Cmd.OfPromise.either
                OfficeInterop.addTableValidationToExisting
                (newTableValidation, newColNames)
                (fun x ->
                    Msg.Batch [
                        GenericLog x |> Dev
                        WriteProtocolToXml protocolInfo |> ExcelInterop
                    ]
                )
                (GenericError >> Dev)
        currentModel, cmd

    | WriteProtocolToXml protocolInfo ->
        let cmd =
            Cmd.OfPromise.either
                OfficeInterop.writeProtocolToXml
                (protocolInfo)
                (fun res ->
                    Msg.Batch [
                        GenericLog res |> Dev
                        UpdateProtocolGroupHeader |> ExcelInterop
                        if currentModel.PageState.CurrentPage = Route.SettingsProtocol then GetActiveProtocolGroupXmlParsed |> SettingsProtocolMsg 
                    ]
                )
                (GenericError >> Dev)
        currentModel, cmd
    | DeleteAllCustomXml ->
        let cmd =
            Cmd.OfPromise.either
                OfficeInterop.deleteAllCustomXml
                ()
                (fun res ->
                    Msg.Batch [
                        GenericLog res |> Dev
                        UpdateProtocolGroupHeader |> ExcelInterop
                    ]
                )
                (GenericError >> Dev)
        currentModel, cmd
    | GetSwateCustomXml ->
        let cmd =
            Cmd.OfPromise.either
                OfficeInterop.getSwateCustomXml
                ()
                (fun xml ->
                    Msg.Batch [
                        GenericLog xml |> Dev
                        UpdateRawCustomXml (snd xml) |> SettingsXmlMsg
                    ]
                )
                (GenericError >> Dev)
        currentModel, cmd
    | UpdateSwateCustomXml newCustomXml ->
        let cmd =
            Cmd.OfPromise.either
                OfficeInterop.updateSwateCustomXml
                newCustomXml
                (fun x ->
                    Msg.Batch [
                        x |> (GenericLog >> Dev)
                        GetSwateCustomXml |> ExcelInterop
                    ]
                )
                (GenericError >> Dev)
        currentModel, cmd
    //
    | FillHiddenColsRequest ->
        failwith """Function "FillHiddenColsRequest" is currently not supported."""
        //let cmd =
        //    Cmd.OfPromise.either
        //        OfficeInterop.createSearchTermsIFromTable 
        //        ()
        //        (SearchForInsertTermsRequest >> Request >> Api)
        //        (fun e ->
        //            Msg.Batch [
        //                UpdateFillHiddenColsState FillHiddenColsState.Inactive |> ExcelInterop
        //                GenericError e |> Dev
        //            ] )
        //let cmd2 = UpdateFillHiddenColsState FillHiddenColsState.ExcelCheckHiddenCols |> ExcelInterop |> Cmd.ofMsg
        //let cmds = Cmd.batch [cmd; cmd2]
        currentModel, Cmd.none

    | FillHiddenColumns (tableName,insertTerms) ->
        let cmd =
            Cmd.OfPromise.either
                OfficeInterop.UpdateTableBySearchTermsI
                (tableName,insertTerms)
                (fun msg ->
                    Msg.Batch [
                        UpdateFillHiddenColsState FillHiddenColsState.Inactive |> ExcelInterop
                        GenericLog ("info",msg) |> Dev
                    ]
                )
                (fun e ->
                    Msg.Batch [
                        UpdateFillHiddenColsState FillHiddenColsState.Inactive |> ExcelInterop
                        GenericError e |> Dev
                    ] )
        let cmd2 = UpdateFillHiddenColsState FillHiddenColsState.ExcelWriteFoundTerms |> ExcelInterop |> Cmd.ofMsg
        let cmds = Cmd.batch [cmd; cmd2]
        currentModel, cmds


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
    | GetSelectedBuildingBlockSearchTerms ->
        failwith """Function "GetSelectedBuildingBlockSearchTerms" is currently not supported."""
        //let cmd =
        //    Cmd.OfPromise.either
        //        OfficeInterop.getAnnotationBlockDetails
        //        ()
        //        (GetSelectedBuildingBlockSearchTermsRequest >> BuildingBlockDetails)
        //        (fun x ->
        //            Msg.Batch [
        //                GenericError x |> Dev
        //                UpdateCurrentRequestState RequestBuildingBlockInfoStates.Inactive |> BuildingBlockDetails
        //            ]
        //        )
        //let cmd2 = Cmd.ofMsg (UpdateCurrentRequestState RequestBuildingBlockInfoStates.RequestExcelInformation |> BuildingBlockDetails) 
        currentModel, Cmd.none//Cmd.batch [cmd;cmd2]
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
        
let handleTermSearchMsg (termSearchMsg: TermSearchMsg) (currentState:TermSearchState) : TermSearchState * Cmd<Msg> =
    match termSearchMsg with
    /// Toggle the search by parent ontology option on/off by clicking on a checkbox
    | ToggleSearchByParentOntology ->

        let nextState = {
            currentState with
                SearchByParentOntology = currentState.SearchByParentOntology |> not
        }

        nextState, Cmd.none

    | SearchTermTextChange newTerm ->

        let triggerNewSearch =
            newTerm.Length > 2
           
        let (delay, bounceId, msgToBounce) =
            (System.TimeSpan.FromSeconds 0.5),
            "GetNewTermSuggestions",
            (
                if triggerNewSearch then
                    match currentState.ParentOntology, currentState.SearchByParentOntology with
                    | Some parentOntology, true ->
                        (newTerm,parentOntology) |> (GetNewTermSuggestionsByParentTerm >> Request >> Api)
                    | None,_ | _, false ->
                        newTerm  |> (GetNewTermSuggestions >> Request >> Api)
                else
                    DoNothing
            )

        let nextState = {
            currentState with
                TermSearchText = newTerm
                SelectedTerm = None
                ShowSuggestions = triggerNewSearch
                HasSuggestionsLoading = true
        }

        nextState, ((delay, bounceId, msgToBounce) |> Bounce |> Cmd.ofMsg)

    | TermSuggestionUsed suggestion ->

        let nextState = {
            TermSearchState.init() with
                SelectedTerm = Some suggestion
                TermSearchText = suggestion.Name
        }
        nextState, Cmd.none

    | NewSuggestions suggestions ->

        let nextState = {
            currentState with
                TermSuggestions         = suggestions
                ShowSuggestions         = true
                HasSuggestionsLoading   = false
        }

        nextState,Cmd.none

    | StoreParentOntologyFromOfficeInterop parentTerm ->
        let pOnt =
            // if none, no parentOntology was found by office.js.
            // this happens e.g. if a field outside the table is selected
            if parentTerm.IsNone
            then None
            else
                let s = (string parentTerm.Value)
                let res =
                    OfficeInterop.Regex.parseColHeader s
                res.Ontology

        let nextState = {
            currentState with
                ParentOntology = pOnt
        }
        nextState, Cmd.none

    // Server

    | GetAllTermsByParentTermRequest ontInfo ->
        let cmd =
            Cmd.OfAsync.either
                Api.api.getAllTermsByParentTerm
                ontInfo
                (GetAllTermsByParentTermResponse >> TermSearch)
                (GenericError >> Dev)

        let nextState = {
            currentState with
                HasSuggestionsLoading = true
        }

        nextState, cmd

    | GetAllTermsByParentTermResponse terms ->
        let nextState = {
            currentState with
                TermSuggestions = terms
                HasSuggestionsLoading = false
                ShowSuggestions = true
                
        }
        nextState, Cmd.none

let handleAdvancedTermSearchMsg (advancedTermSearchMsg: AdvancedSearchMsg) (currentState:AdvancedSearchState) : AdvancedSearchState * Cmd<Msg> =
    match advancedTermSearchMsg with
    | ResetAdvancedSearchOptions ->
        let nextState = {
            currentState with
                AdvancedSearchOptions = AdvancedTermSearchOptions.init()
                AdvancedTermSearchSubpage = AdvancedTermSearchSubpages.InputFormSubpage
        }

        nextState,Cmd.none
    | UpdateAdvancedTermSearchSubpage subpage ->
        let tOpt =
            match subpage with
            |SelectedResultSubpage t   -> Some t
            | _                 -> None
        let nextState = {
            currentState with
                SelectedResult = tOpt
                AdvancedTermSearchSubpage = subpage
        }
        nextState, Cmd.none

    | ToggleModal modalId ->
        let nextState = {
            currentState with
                ModalId = modalId
                HasModalVisible = (not currentState.HasModalVisible)
        }

        nextState,Cmd.none

    | ToggleOntologyDropdown ->
        let nextState = {
            currentState with
                HasOntologyDropdownVisible = (not currentState.HasOntologyDropdownVisible)
        }

        nextState,Cmd.none

    | OntologySuggestionUsed suggestion ->

        let nextAdvancedSearchOptions = {
            currentState.AdvancedSearchOptions with
                Ontology = suggestion
        }

        let nextState = {
            currentState with
                AdvancedSearchOptions   = nextAdvancedSearchOptions
            }
        nextState, Cmd.ofMsg (AdvancedSearch ToggleOntologyDropdown)

    | UpdateAdvancedTermSearchOptions opts ->

        let nextState = {
            currentState with
                AdvancedSearchOptions = opts
        }

        nextState,Cmd.none

    | StartAdvancedSearch ->

        let nextState = {
            currentState with
                AdvancedTermSearchSubpage       = AdvancedTermSearchSubpages.ResultsSubpage
                HasAdvancedSearchResultsLoading = true
            
        }

        let nextCmd =
            currentState.AdvancedSearchOptions
            |> GetNewAdvancedTermSearchResults
            |> Request
            |> Api
            |> Cmd.ofMsg

        nextState,nextCmd

    | ResetAdvancedSearchState ->
        let nextState = AdvancedSearchState.init()

        nextState,Cmd.none

    | NewAdvancedSearchResults results ->
        let nextState = {
            currentState with
                AdvancedSearchTermResults       = results
                AdvancedTermSearchSubpage       = AdvancedTermSearchSubpages.ResultsSubpage
                HasAdvancedSearchResultsLoading = false
        }

        nextState,Cmd.none

    | ChangePageinationIndex index ->
        let nextState = {
            currentState with
                AdvancedSearchResultPageinationIndex = index
        }

        nextState,Cmd.none

let handleDevMsg (devMsg: DevMsg) (currentState:DevState) : DevState * Cmd<Msg> =
    match devMsg with
    | GenericLog (level,logText) ->
        let nextState = {
            currentState with
                Log = (LogItem.ofStringNow level logText)::currentState.Log
            }
        nextState, Cmd.none

    | GenericError (e) ->
        OfficeInterop.consoleLog (sprintf "GenericError occured: %s" e.Message)
        let nextState = {
            currentState with
                Log = LogItem.Error(System.DateTime.Now,e.Message)::currentState.Log
            }
        nextState, Cmd.ofMsg (UpdateLastFullError (Some e) |> Dev)

    | UpdateLastFullError (eOpt) ->
        let nextState = {
            currentState with
                LastFullError = eOpt
        }
        nextState, Cmd.none

    | LogTableMetadata ->
        let cmd =
            Cmd.OfPromise.either
                OfficeInterop.getTableMetaData
                ()
                (GenericLog >> Dev)
                (GenericError >> Dev)
        currentState, cmd

let handleApiRequestMsg (reqMsg: ApiRequestMsg) (currentState: ApiState) : ApiState * Cmd<Msg> =

    let handleTermSuggestionRequest (apiFunctionname:string) (responseHandler: DbDomain.Term [] -> ApiMsg) queryString =
        let currentCall = {
            FunctionName = apiFunctionname
            Status = Pending
        }

        let nextState = {
            currentState with
                currentCall = currentCall
        }
        let nextCmd = 
            Cmd.OfAsync.either
                Api.api.getTermSuggestions
                (5,queryString)
                (responseHandler >> Api)
                (ApiError >> Api)

        nextState,nextCmd

    let handleUnitTermSuggestionRequest (apiFunctionname:string) (responseHandler: (DbDomain.Term [] * UnitSearchRequest) -> ApiMsg) queryString (relUnit:UnitSearchRequest) =
        let currentCall = {
            FunctionName = apiFunctionname
            Status = Pending
        }

        let nextState = {
            currentState with
                currentCall = currentCall
        }
        let nextCmd = 
            Cmd.OfAsync.either
                Api.api.getUnitTermSuggestions
                (5,queryString,relUnit)
                (responseHandler >> Api)
                (ApiError >> Api)

        nextState,nextCmd

    let handleTermSuggestionByParentTermRequest (apiFunctionname:string) (responseHandler: DbDomain.Term [] -> ApiMsg) queryString (parentOntology:OntologyInfo) =
        let currentCall = {
            FunctionName = apiFunctionname
            Status = Pending
        }

        let nextState = {
            currentState with
                currentCall = currentCall
        }
        let nextCmd = 
            Cmd.OfAsync.either
                Api.api.getTermSuggestionsByParentTerm
                (5,queryString,parentOntology)
                (responseHandler >> Api)
                (ApiError >> Api)

        nextState,nextCmd

    match reqMsg with
    | TestOntologyInsert (a,b,c,d,e) ->

        let currentCall = {
            FunctionName = "testOntologyInsert"
            Status = Pending
        }

        let nextState = {
            currentState with
                currentCall = currentCall
        }

        nextState,
        Cmd.OfAsync.either
            Api.api.testOntologyInsert
            (a,b,c,d,e)
            (fun x -> ("Debug",sprintf "Successfully created %A" x) |> ApiSuccess |> Api)
            (ApiError >> Api)

        
        //let currentCall = {
        //        FunctionName = "getTermSuggestions"
        //        Status = Pending
        //}

        //let nextState = {
        //    currentState with
        //        currentCall = currentCall
        //}

        //nextState,
        //Cmd.OfAsync.either
        //    Api.api.getTermSuggestions
        //    (5,queryString)
        //    (TermSuggestionResponse >> Response >> Api)
        //    (ApiError >> Api)
    | GetNewTermSuggestions queryString ->
        handleTermSuggestionRequest
            "getTermSuggestions"
            (TermSuggestionResponse >> Response)
            queryString

    | GetNewTermSuggestionsByParentTerm (queryString,parentOntology) ->
        handleTermSuggestionByParentTermRequest
            "getTermSuggestionsByParentOntology"
            (TermSuggestionResponse >> Response)
            queryString
            parentOntology

    | GetNewUnitTermSuggestions (queryString,relUnit) ->
        handleUnitTermSuggestionRequest
            "getUnitTermSuggestions"
            (UnitTermSuggestionResponse >> Response)
            queryString
            relUnit

    | GetNewBuildingBlockNameSuggestions queryString ->
        handleTermSuggestionRequest
            "getBuildingBlockNameSuggestions"
            (BuildingBlockNameSuggestionsResponse >> Response)
            queryString

    | GetNewAdvancedTermSearchResults options ->
        let currentCall = {
                FunctionName = "getTermsForAdvancedSearch"
                Status = Pending
        }

        let nextState = {
            currentState with
                currentCall = currentCall
        }

        nextState,
        Cmd.OfAsync.either
            Api.api.getTermsForAdvancedSearch
            (options.Ontology,options.SearchTermName,options.MustContainName,options.SearchTermDefinition,options.MustContainDefinition,options.KeepObsolete)
            (AdvancedTermSearchResultsResponse >> Response >> Api)
            (ApiError >> Api)

    | FetchAllOntologies ->
        let currentCall = {
                FunctionName = "getAllOntologies"
                Status = Pending
        }

        let nextState = {
            currentState with
                currentCall = currentCall
        }

        nextState,
        Cmd.OfAsync.either
            Api.api.getAllOntologies
            ()
            (FetchAllOntologiesResponse >> Response >> Api)
            (ApiError >> Api)

    | SearchForInsertTermsRequest (tableName,insertTerms) ->
        let currentCall = {
            FunctionName = "getTermsByNames"
            Status = Pending
        }
        let nextState = {
            currentState with
                currentCall = currentCall
        }
        let cmd =
            Cmd.OfAsync.either
                Api.api.getTermsByNames
                insertTerms
                ((fun newTerms -> SearchForInsertTermsResponse (tableName,newTerms) ) >> Response >> Api)
                (fun e ->
                    Msg.Batch [
                        UpdateFillHiddenColsState FillHiddenColsState.Inactive |> ExcelInterop
                        ApiError e |> Api
                    ] )
        let cmd2 = UpdateFillHiddenColsState FillHiddenColsState.ServerSearchDatabase |> ExcelInterop |> Cmd.ofMsg
        //let cmd3 = GenericLog ("Debug", sprintf "%A" insertTerms) |> Dev |> Cmd.ofMsg
        let cmds = Cmd.batch [cmd; cmd2; (*cmd3*)]
        nextState, cmds
    //
    | GetAppVersion ->
        let currentCall = {
            FunctionName = "getAppVersion"
            Status = Pending
        }

        let nextState = {
            currentState with
                currentCall = currentCall
        }

        let cmd =
            Cmd.OfAsync.either
                Api.serviceApi.getAppVersion
                ()
                (GetAppVersionResponse >> Response >> Api)
                (ApiError >> Api)
            
        nextState, cmd
        

let handleApiResponseMsg (resMsg: ApiResponseMsg) (currentState: ApiState) : ApiState * Cmd<Msg> =

    let handleTermSuggestionResponse (responseHandler: DbDomain.Term [] -> Msg) (suggestions: DbDomain.Term[]) =
        let finishedCall = {
            currentState.currentCall with
                Status = Successfull
        }

        let nextState = {
            currentState with
                currentCall = noCall
                callHistory = finishedCall::currentState.callHistory
        }

        let cmds = Cmd.batch [
            ("Debug",sprintf "[ApiSuccess]: Call %s successfull." finishedCall.FunctionName) |> ApiSuccess |> Api |> Cmd.ofMsg
            suggestions |> responseHandler |> Cmd.ofMsg
        ]

        nextState, cmds

    let handleUnitTermSuggestionResponse (responseHandler: DbDomain.Term [] * UnitSearchRequest -> Msg) (suggestions: DbDomain.Term[]) (relatedUnitSearch:UnitSearchRequest) =
        let finishedCall = {
            currentState.currentCall with
                Status = Successfull
        }

        let nextState = {
            currentState with
                currentCall = noCall
                callHistory = finishedCall::currentState.callHistory
        }

        let cmds = Cmd.batch [
            ("Debug",sprintf "[ApiSuccess]: Call %s successfull." finishedCall.FunctionName) |> ApiSuccess |> Api |> Cmd.ofMsg
            (suggestions,relatedUnitSearch) |> responseHandler |> Cmd.ofMsg
        ]

        nextState, cmds

    match resMsg with
    | TermSuggestionResponse suggestions ->

        handleTermSuggestionResponse
            (NewSuggestions >> TermSearch)
            suggestions

    | UnitTermSuggestionResponse (suggestions,relUnit) ->

        handleUnitTermSuggestionResponse
            (NewUnitTermSuggestions >> AddBuildingBlock)
            suggestions
            relUnit
            

    | BuildingBlockNameSuggestionsResponse suggestions ->

        handleTermSuggestionResponse
            (NewBuildingBlockNameSuggestions >> AddBuildingBlock)
            suggestions

    | AdvancedTermSearchResultsResponse results ->
        let finishedCall = {
            currentState.currentCall with
                Status = Successfull
        }

        let nextState = {
            currentState with
                currentCall = noCall
                callHistory = finishedCall::currentState.callHistory
        }

        let cmds = Cmd.batch [
            ("Debug",sprintf "[ApiSuccess]: Call %s successfull." finishedCall.FunctionName) |> ApiSuccess |> Api |> Cmd.ofMsg
            results |> NewAdvancedSearchResults |> AdvancedSearch |> Cmd.ofMsg
        ]

        nextState, cmds

    | FetchAllOntologiesResponse onts ->
        let finishedCall = {
            currentState.currentCall with
                Status = Successfull
        }

        let nextState = {
            currentState with
                currentCall = noCall
                callHistory = finishedCall::currentState.callHistory
        }

        let cmds = Cmd.batch [
            ("Debug",sprintf "[ApiSuccess]: Call %s successfull." finishedCall.FunctionName) |> ApiSuccess |> Api |> Cmd.ofMsg
            onts |> NewSearchableOntologies |> PersistentStorage |> Cmd.ofMsg
        ]

        nextState, cmds

    | SearchForInsertTermsResponse (tableName,insertTerms) ->
        let finishedCall = {
            currentState.currentCall with
                Status = Successfull
        }
        let nextState = {
            currentState with
                currentCall = noCall
                callHistory = finishedCall::currentState.callHistory
        }
        let cmd =
            FillHiddenColumns (tableName,insertTerms) |> ExcelInterop |> Cmd.ofMsg
        let cmd2 =
             ("Debug",sprintf "[ApiSuccess]: Call %s successfull." finishedCall.FunctionName) |> ApiSuccess |> Api |> Cmd.ofMsg
        let cmds =
            Cmd.batch [cmd; cmd2]
        nextState, cmds

    //
    | GetAppVersionResponse appVersion ->
        let finishedCall = {
            currentState.currentCall with
                Status = Successfull
        }

        let nextState = {
            currentState with
                currentCall = noCall
                callHistory = finishedCall::currentState.callHistory
        }

        let cmds = Cmd.batch [
            ("Debug",sprintf "[ApiSuccess]: Call %s successfull." finishedCall.FunctionName) |> ApiSuccess |> Api |> Cmd.ofMsg
            appVersion |> UpdateAppVersion |> PersistentStorage |> Cmd.ofMsg
        ]

        nextState, cmds

let handleApiMsg (apiMsg:ApiMsg) (currentState:ApiState) : ApiState * Cmd<Msg> =
    match apiMsg with
    | ApiError e ->
        let failedCall = {
            currentState.currentCall with
                Status = Failed e.Message
        }

        let nextState = {
            currentState with
                currentCall = noCall
                callHistory = failedCall::currentState.callHistory
        }

        nextState,
        ("Error",sprintf "[ApiError]: Call %s failed with: %s" failedCall.FunctionName e.Message)|> GenericLog |> Dev |> Cmd.ofMsg

    | ApiSuccess (level,logMsg) ->
        currentState,
        (level,logMsg) |> GenericLog |> Dev |> Cmd.ofMsg

    | Request req ->
        handleApiRequestMsg req currentState
    | Response res ->
        handleApiResponseMsg res currentState

let handlePersistenStorageMsg (persistentStorageMsg: PersistentStorageMsg) (currentState:PersistentStorageState) : PersistentStorageState * Cmd<Msg> =
    match persistentStorageMsg with
    | NewSearchableOntologies onts ->
        let nextState = {
            currentState with
                SearchableOntologies    = onts |> Array.map (fun ont -> ont.Name |> Suggestion.createBigrams, ont)
                HasOntologiesLoaded     = true
        }

        nextState,Cmd.none
    | UpdateAppVersion appVersion ->
        let nextState = {
            currentState with
                AppVersion = appVersion
        }
        nextState,Cmd.none

let handleStyleChangeMsg (styleChangeMsg:StyleChangeMsg) (currentState:SiteStyleState) : SiteStyleState * Cmd<Msg> =
    match styleChangeMsg with
    | ToggleBurger          ->
        let nextState = {
            currentState with
                BurgerVisible = not currentState.BurgerVisible
        }

        nextState,Cmd.none

    | ToggleQuickAcessIconsShown ->
        let nextState = {
            currentState with
                QuickAcessIconsShown = not currentState.QuickAcessIconsShown
        }
        nextState, Cmd.none

    | ToggleColorMode       -> 
        let opposite = not currentState.IsDarkMode
        let nextState = {
            currentState with
                IsDarkMode = opposite;
                ColorMode = if opposite then ExcelColors.darkMode else ExcelColors.colorfullMode
        }
        nextState,Cmd.none

let handleFilePickerMsg (filePickerMsg:FilePickerMsg) (currentState: FilePickerState) : FilePickerState * Cmd<Msg> =
    match filePickerMsg with
    | LoadNewFiles fileNames ->
        let nextState = {
            FilePickerState.init() with
                FileNames = fileNames |> List.mapi (fun i x -> i+1,x)
        }
        let nextCmd = UpdatePageState (Some Routing.Route.FilePicker) |> Cmd.ofMsg
        nextState, nextCmd
    | UpdateFileNames newFileNames ->
        let nextState = {
            currentState with
                FileNames = newFileNames
        }
        nextState, Cmd.none
    | UpdateDNDDropped isDropped ->
        let nextState = {
            currentState with
                DNDDropped = isDropped
        }
        nextState, Cmd.none

let handleAddBuildingBlockMsg (addBuildingBlockMsg:AddBuildingBlockMsg) (currentState: AddBuildingBlockState) : AddBuildingBlockState * Cmd<Msg> =
    match addBuildingBlockMsg with
    | NewBuildingBlockSelected nextBB ->
        let nextState = {
            AddBuildingBlockState.init() with
                CurrentBuildingBlock        = nextBB
        }

        nextState,Cmd.none

    | ToggleSelectionDropdown ->
        let nextState = {
            currentState with
                ShowBuildingBlockSelection = not currentState.ShowBuildingBlockSelection
        }

        nextState,Cmd.none

    | SearchUnitTermTextChange (newTerm,relUnit) ->

        let triggerNewSearch =
            newTerm.Length > 2
       
        let (delay, bounceId, msgToBounce) =
            (System.TimeSpan.FromSeconds 0.5),
            "GetNewUnitTermSuggestions",
            (
                if triggerNewSearch then
                    (newTerm,relUnit) |> (GetNewUnitTermSuggestions >> Request >> Api)
                else
                    DoNothing
            )

        let nextState =
            match relUnit with
            | Unit1 ->
                { currentState with
                    UnitTermSearchText                  = newTerm
                    UnitSelectedTerm                    = None
                    ShowUnitTermSuggestions             = triggerNewSearch
                    HasUnitTermSuggestionsLoading       = true
                }
            | Unit2 ->
                { currentState with
                    Unit2TermSearchText                  = newTerm
                    Unit2SelectedTerm                    = None
                    ShowUnit2TermSuggestions             = triggerNewSearch
                    HasUnit2TermSuggestionsLoading       = true
                }

        nextState, ((delay, bounceId, msgToBounce) |> Bounce |> Cmd.ofMsg)

    | NewUnitTermSuggestions (suggestions,relUnit) ->

        let nextState =
            match relUnit with
            | Unit1 ->
                { currentState with
                        UnitTermSuggestions             = suggestions
                        ShowUnitTermSuggestions         = true
                        HasUnitTermSuggestionsLoading   = false
                }
            | Unit2 ->
                { currentState with
                    Unit2TermSuggestions             = suggestions
                    ShowUnit2TermSuggestions         = true
                    HasUnit2TermSuggestionsLoading   = false
                }

        nextState,Cmd.none

    | UnitTermSuggestionUsed (suggestion, relUnit) ->

        let nextState =
            match relUnit with
            | Unit1 ->
                { currentState with
                    UnitTermSearchText              = suggestion.Name
                    UnitSelectedTerm                = Some suggestion
                    ShowUnitTermSuggestions         = false
                    HasUnitTermSuggestionsLoading   = false
                }
            | Unit2 ->
                { currentState with
                    Unit2TermSearchText             = suggestion.Name
                    Unit2SelectedTerm               = Some suggestion
                    ShowUnit2TermSuggestions        = false
                    HasUnit2TermSuggestionsLoading  = false
                }
        nextState, Cmd.none

    | BuildingBlockNameChange newName ->

        let triggerNewSearch =
            newName.Length > 2
   
        let (delay, bounceId, msgToBounce) =
            (System.TimeSpan.FromSeconds 0.5),
            "GetNewBuildingBlockNameTermSuggestions",
            (
                if triggerNewSearch then
                    newName  |> (GetNewBuildingBlockNameSuggestions >> Request >> Api)
                else
                    DoNothing
            )

        let nextBB = {
            currentState.CurrentBuildingBlock with
                Name = newName
        }

        let nextState = {
            currentState with
                CurrentBuildingBlock                    = nextBB
                BuildingBlockSelectedTerm               = None
                ShowBuildingBlockTermSuggestions        = triggerNewSearch
                HasBuildingBlockTermSuggestionsLoading  = true
        }

        nextState, ((delay, bounceId, msgToBounce) |> Bounce |> Cmd.ofMsg)

    | NewBuildingBlockNameSuggestions suggestions ->

        let nextState = {
            currentState with
                BuildingBlockNameSuggestions            = suggestions
                ShowBuildingBlockTermSuggestions        = true
                HasBuildingBlockTermSuggestionsLoading  = false
        }

        nextState,Cmd.none

    | BuildingBlockNameSuggestionUsed suggestion ->
        
        let nextBB = {
            currentState.CurrentBuildingBlock with
                Name = suggestion.Name
        }

        let nextState = {
            currentState with
                CurrentBuildingBlock                    = nextBB

                BuildingBlockSelectedTerm               = Some suggestion
                ShowBuildingBlockTermSuggestions        = false
                HasBuildingBlockTermSuggestionsLoading  = false
        }
        nextState, Cmd.none

    | ToggleBuildingBlockHasUnit ->

        let hasUnit = not currentState.BuildingBlockHasUnit

        let nextState =
            if hasUnit then
                {
                    currentState with
                        BuildingBlockHasUnit = hasUnit
                }
            else
                {
                currentState with
                    BuildingBlockHasUnit = hasUnit
                    //UnitTerm = None
                    UnitTermSearchText = ""
                    UnitTermSuggestions = [||]
                    ShowUnitTermSuggestions = false
                    HasUnitTermSuggestionsLoading = false
                }
        nextState, Cmd.none

open OfficeInterop.Types.Xml.ValidationTypes

let handleValidationMsg (validationMsg:ValidationMsg) (currentState: ValidationState) : ValidationState * Cmd<Msg> =
    match validationMsg with
    /// This message gets its values from ExcelInteropMsg.GetTableRepresentation.
    /// It is used to update ValidationState.TableRepresentation and to transform the new information to ValidationState.TableValidationScheme.
    | StoreTableRepresentationFromOfficeInterop (tableValidation:TableValidation, buildingBlocks:BuildingBlockTypes.BuildingBlock [], msg) ->

        let nextCmd =
            GenericLog ("Info", msg) |> Dev |> Cmd.ofMsg

        let nextState = {
            currentState with
                ActiveTableBuildingBlocks = buildingBlocks
                TableValidationScheme = tableValidation
        }
        nextState, nextCmd

    | UpdateDisplayedOptionsId intOpt ->
        let nextState = {
            currentState with
                DisplayedOptionsId = intOpt
        }
        nextState, Cmd.none
    | UpdateTableValidationScheme tableValidation ->
        let nextState = {
            currentState with
                TableValidationScheme   = tableValidation
        }
        nextState, Cmd.none

let handleFileUploadJsonMsg (fujMsg:ProtocolInsertMsg) (currentState: ProtocolInsertState) : ProtocolInsertState * Cmd<Msg> =

    //let parseDBProtocol (prot:Shared.ProtocolTemplate) =
    //    let tableName,minBBInfos = prot.TableXml |> OfficeInterop.Regex.MinimalBuildingBlock.ofExcelTableXml
    //    let validationType =  prot.CustomXml |> TableValidation.ofXml
    //    if tableName <> validationType.AnnotationTable.Name then failwith "CustomXml and TableXml relate to different tables."
    //    prot, validationType, minBBInfos

    match fujMsg with
    //| ParseJsonToProcessRequest parsableString ->
    //    let cmd =
    //        Cmd.OfAsync.either
    //            Api.isaDotNetApi.parseJsonToProcess
    //            parsableString
    //            (Ok >> ParseJsonToProcessResult)
    //            (Result.Error >> ParseJsonToProcessResult)
    //    currentState, Cmd.map ProtocolInsert cmd 
    //| ParseJsonToProcessResult (Ok isaProcess) ->
    //    let nextState = {
    //        currentState with
    //            ProcessModel = Some isaProcess
    //    }
    //    nextState, Cmd.none
    //| ParseJsonToProcessResult (Result.Error e) ->
    //    let cmd =
    //        GenericError e |> Dev |> Cmd.ofMsg 
    //    currentState, cmd
    //| RemoveProcessFromModel ->
    //    let nextState = {
    //        currentState with
    //            ProcessModel = None
    //            UploadData = ""
    //    }
    //    nextState, Cmd.none
    | GetAllProtocolsRequest ->
        let nextState = {currentState with Loading = true}
        let cmd =
            Cmd.OfAsync.either
                Api.api.getAllProtocolsWithoutXml
                ()
                (GetAllProtocolsResponse >> ProtocolInsert)
                (GenericError >> Dev)
        nextState, cmd
    | GetAllProtocolsResponse protocols ->
        let nextState = {
            currentState with
                ProtocolsAll = protocols
                Loading = false
        }
        nextState, Cmd.none
    | GetProtocolXmlByProtocolRequest prot ->
        let cmd =
            Cmd.OfAsync.either
                Api.api.getProtocolXmlForProtocol
                prot
                (ParseProtocolXmlByProtocolRequest >> ProtocolInsert)
                (GenericError >> Dev)
        currentState, cmd
    | ParseProtocolXmlByProtocolRequest prot ->
        failwith """Function "ParseProtocolXmlByProtocolRequest" is currently not supported."""
        //let cmd =
        //    Cmd.OfFunc.either
        //        parseDBProtocol
        //        (prot)
        //        (GetProtocolXmlByProtocolResponse >> ProtocolInsert)
        //        (fun e ->
        //            Msg.Batch [
        //                GenericError e |> Dev
        //                UpdateLoading false |> ProtocolInsert 
        //            ]
        //        )
        currentState, Cmd.none
    | GetProtocolXmlByProtocolResponse (prot,validation,minBBInfoList) -> 
        let nextState = {
            currentState with
                ProtocolSelected = Some prot
                BuildingBlockMinInfoList = minBBInfoList
                ValidationXml = Some validation

                DisplayedProtDetailsId = None
        }
        nextState, Cmd.ofMsg (UpdatePageState <| Some Routing.Route.ProtocolInsert)
    | ProtocolIncreaseTimesUsed protocolTemplateName ->
        let cmd =
            Cmd.OfAsync.attempt
                Api.api.increaseTimesUsed
                protocolTemplateName
                (GenericError >> Dev)
        currentState, cmd
                
    // Client
    | UpdateLoading nextLoadingState ->
        let nextState = {
            currentState with Loading = nextLoadingState
        }
        nextState, Cmd.none
    //| UpdateUploadData newDataString ->
    //    let nextState = {
    //        currentState with
    //            UploadData = newDataString
    //    }
    //    nextState, Cmd.ofMsg (ParseJsonToProcessRequest newDataString |> ProtocolInsert)
    | UpdateDisplayedProtDetailsId idOpt ->
        let nextState = {
            currentState with
                DisplayedProtDetailsId = idOpt
        }
        nextState, Cmd.none
    | UpdateProtocolNameSearchQuery strVal ->
        let nextState = {
            currentState with ProtocolNameSearchQuery = strVal
        }
        nextState, Cmd.none
    | UpdateProtocolTagSearchQuery strVal ->
        let nextState = {
            currentState with ProtocolTagSearchQuery = strVal
        }
        nextState, Cmd.none
    | AddProtocolTag tagStr ->
        let nextState = {
            currentState with
                ProtocolSearchTags      = tagStr::currentState.ProtocolSearchTags
                ProtocolTagSearchQuery  = ""
        }
        nextState, Cmd.none
    | RemoveProtocolTag tagStr ->
        let nextState = {
            currentState with
                ProtocolSearchTags      = currentState.ProtocolSearchTags |> List.filter (fun x -> x <> tagStr)
        }
        nextState, Cmd.none
    | RemoveSelectedProtocol ->
        let nextState = {
            currentState with
                ProtocolSelected = None
                ValidationXml = None
                BuildingBlockMinInfoList = []
        }
        nextState, Cmd.none

let handleBuildingBlockMsg (topLevelMsg:BuildingBlockDetailsMsg) (currentState: BuildingBlockDetailsState) : BuildingBlockDetailsState * Cmd<Msg> =
    match topLevelMsg with
    // Client
    | ToggleShowDetails ->
        let nb = currentState.ShowDetails |> not
        let nextState = {
            currentState with
                ShowDetails         = nb
                BuildingBlockValues = if nb = false then [||] else currentState.BuildingBlockValues
        }
        nextState, Cmd.none
    | UpdateCurrentRequestState nextRequState ->
        let nextState = {
            currentState with
                CurrentRequestState = nextRequState
        }
        nextState, Cmd.none
    // Server
    | GetSelectedBuildingBlockSearchTermsRequest searchTerms ->
        let nextState = {
            currentState with
                CurrentRequestState = RequestBuildingBlockInfoStates.RequestDataBaseInformation
        }
        let cmd =
            Cmd.OfAsync.either
                Api.api.getTermsByNames
                searchTerms
                (GetSelectedBuildingBlockSearchTermsResponse >> BuildingBlockDetails)
                (fun x ->
                    Msg.Batch [
                        GenericError x |> Dev
                        UpdateCurrentRequestState Inactive |> BuildingBlockDetails
                    ]
                )
        nextState, cmd
    | GetSelectedBuildingBlockSearchTermsResponse searchTermResults ->
        let nextState = {
            currentState with
                ShowDetails         = true
                BuildingBlockValues = searchTermResults
                CurrentRequestState = Inactive
        }
        nextState, Cmd.none

let handleSettingsXmlMsg (msg:SettingsXmlMsg) (currentState: SettingsXmlState) : SettingsXmlState * Cmd<Msg> =

    let matchXmlTypeToUpdateMsg msg (xmlType:OfficeInterop.Types.Xml.XmlTypes) =
        match xmlType with
        | OfficeInterop.Types.Xml.XmlTypes.ValidationType v ->
            Msg.Batch [
                GenericLog ("Info", msg) |> Dev
                GetAllValidationXmlParsedRequest |> SettingsXmlMsg
            ]
        | OfficeInterop.Types.Xml.XmlTypes.GroupType _ | OfficeInterop.Types.Xml.XmlTypes.ProtocolType _ ->
            Msg.Batch [
                GenericLog ("Info", msg) |> Dev
                GetAllProtocolGroupXmlParsedRequest |> SettingsXmlMsg
                UpdateProtocolGroupHeader |> ExcelInterop
            ]
        
    match msg with
    // // Client // //
    // Validation Xml
    | UpdateActiveSwateValidation nextActiveTableValid ->
        let nextState = {
            currentState with
                ActiveSwateValidation                   = nextActiveTableValid
                NextAnnotationTableForActiveValidation  = None
        }
        nextState, Cmd.none
    | UpdateNextAnnotationTableForActiveValidation nextAnnoTable ->
        let nextState = {
            currentState with
                NextAnnotationTableForActiveValidation = nextAnnoTable
        }
        nextState, Cmd.none
    | UpdateValidationXmls newValXmls ->
        let nextState = {
            currentState with
                ActiveSwateValidation                   = None
                NextAnnotationTableForActiveValidation  = None
                ValidationXmls                          = newValXmls
        }
        nextState, Cmd.none
    // Protocol group xml
    | UpdateProtocolGroupXmls newProtXmls ->
        let nextState = {
            currentState with
                ActiveProtocolGroup                     = None
                NextAnnotationTableForActiveProtGroup   = None
                ActiveProtocol                          = None
                NextAnnotationTableForActiveProtocol    = None
                ProtocolGroupXmls                       = newProtXmls
        }
        nextState, Cmd.none
    | UpdateActiveProtocolGroup nextActiveProtGroup ->
        let nextState= {
            currentState with
                ActiveProtocolGroup                     = nextActiveProtGroup
                NextAnnotationTableForActiveProtGroup   = None
        }
        nextState, Cmd.none
    | UpdateNextAnnotationTableForActiveProtGroup nextAnnoTable ->
        let nextState = {
            currentState with
                NextAnnotationTableForActiveProtGroup = nextAnnoTable
        }
        nextState, Cmd.none
    // Protocol xml
    | UpdateActiveProtocol protocol ->
        let nextState = {
            currentState with
                ActiveProtocol                          = protocol
                NextAnnotationTableForActiveProtocol    = None
        }
        nextState, Cmd.none
    | UpdateNextAnnotationTableForActiveProtocol nextAnnoTable ->
        let nextState = {
            currentState with
                NextAnnotationTableForActiveProtocol = nextAnnoTable
        }
        nextState, Cmd.none
    //
    | UpdateRawCustomXml rawXmlStr ->
        let nextState = {
            currentState with
                RawXml      = rawXmlStr
                NextRawXml  = ""
        }
        nextState, Cmd.none
    | UpdateNextRawCustomXml nextRawCustomXml ->
        let nextState = {
            currentState with
                NextRawXml = nextRawCustomXml
        }
        nextState, Cmd.none
    // OfficeInterop
    | GetAllValidationXmlParsedRequest ->
        let nextState = {
            currentState with
                ActiveSwateValidation                   = None
                NextAnnotationTableForActiveValidation  = None
        }
        let cmd =
            Cmd.OfPromise.either
                OfficeInterop.getAllValidationXmlParsed
                ()
                (GetAllValidationXmlParsedResponse >> SettingsXmlMsg)
                (GenericError >> Dev)
        nextState, cmd
    | GetAllValidationXmlParsedResponse (tableValidations, annoTables) ->
        let nextState = {
            currentState with
                FoundTables = annoTables
                ValidationXmls = tableValidations |> Array.ofList
        }
        let infoMsg = "Info", sprintf "Found %i checklist XML(s)." tableValidations.Length
        let infoCmd = GenericLog infoMsg |> Dev |> Cmd.ofMsg
        nextState, infoCmd
    | GetAllProtocolGroupXmlParsedRequest ->
        let nextState = {
            currentState with
                ActiveProtocolGroup                     = None
                NextAnnotationTableForActiveProtGroup   = None
                ActiveProtocol                          = None
                NextAnnotationTableForActiveProtocol    = None
        }
        let cmd =
            Cmd.OfPromise.either
                OfficeInterop.getAllProtocolGroupXmlParsed
                ()
                (GetAllProtocolGroupXmlParsedResponse >> SettingsXmlMsg)
                (GenericError >> Dev)
        nextState, cmd
    | GetAllProtocolGroupXmlParsedResponse (protocolGroupXmls, annoTables) ->
        let nextState = {
            currentState with
                FoundTables = annoTables
                ProtocolGroupXmls = protocolGroupXmls |> Array.ofList
        }
        let infoMsg = "Info", sprintf "Found %i protocol group XML(s)." protocolGroupXmls.Length
        let infoCmd = GenericLog infoMsg |> Dev |> Cmd.ofMsg
        nextState, infoCmd
    | RemoveCustomXmlRequest xmlType ->
        let cmd =
            Cmd.OfPromise.either
                OfficeInterop.removeXmlType
                xmlType
                (fun msg ->  matchXmlTypeToUpdateMsg msg xmlType)
                (GenericError >> Dev)
        currentState, cmd
    | ReassignCustomXmlRequest (prevXml,newXml) ->
        let cmd =
            Cmd.OfPromise.either
                OfficeInterop.updateAnnotationTableByXmlType
                (prevXml,newXml)
                // can use prevXml or newXml. Both are checked during 'updateAnnotationTableByXmlType' to be of the same kind
                (fun msg -> matchXmlTypeToUpdateMsg msg prevXml)
                (GenericError >> Dev)
        currentState, cmd

let handleSettingsDataStewardMsg (topLevelMsg:SettingsDataStewardMsg) (currentState: SettingsDataStewardState) : SettingsDataStewardState * Cmd<Msg> =
    match topLevelMsg with
    // Client
    | UpdatePointerJson nextPointerJson ->
        let nextState = {
            currentState with
                PointerJson = nextPointerJson
        }
        nextState, Cmd.none

let handleSettingsProtocolMsg (topLevelMsg:SettingsProtocolMsg) (currentState: SettingsProtocolState) : SettingsProtocolState * Cmd<Msg> =
    match topLevelMsg with
    // Client
    | UpdateProtocolsFromDB nextProtFromDB ->
        let nextState = {
            currentState with
                ProtocolsFromDB = nextProtFromDB
        }
        nextState, Cmd.none
    | UpdateProtocolsFromExcel nextProtFromExcel ->
        let nextState = {
            currentState with
                ProtocolsFromExcel = nextProtFromExcel
        }
        nextState, Cmd.none
    // Excel
    | GetActiveProtocolGroupXmlParsed ->
        let cmd =
            Cmd.OfPromise.either
                OfficeInterop.getActiveProtocolGroupXmlParsed
                ()
                (fun x ->
                    Msg.Batch [
                        UpdateProtocolsFromExcel x |> SettingsProtocolMsg
                        GetProtocolsFromDBRequest x |> SettingsProtocolMsg
                    ]
                )
                (GenericError >> Dev)
        currentState, cmd
    | UpdateProtocolByNewVersion (prot, protTemplate) ->
        failwith """Function "UpdateProtocolByNewVersion" is currently not supported."""
        //let cmd =
        //    Cmd.OfPromise.either
        //        OfficeInterop.updateProtocolByNewVersion
        //        (prot,protTemplate)
        //        (AddAnnotationBlocks >> ExcelInterop)
        //        (GenericError >> Dev)
        currentState, Cmd.none
    // Server
    | GetProtocolsFromDBRequest activeProtGroupOpt ->
        let cmd =
            match activeProtGroupOpt with
            | Some protGroup ->
                let protNames = protGroup.Protocols |> List.map (fun x -> x.Id) |> Array.ofList
                Cmd.OfAsync.either
                    Api.api.getProtocolsByName
                    protNames
                    (UpdateProtocolsFromDB >> SettingsProtocolMsg)
                    (GenericError >> Dev)
            | None ->
                GenericLog ("Info", "No protocols found for active table") |> Dev |> Cmd.ofMsg
        currentState, cmd
            
let handleTopLevelMsg (topLevelMsg:TopLevelMsg) (currentModel: Model) : Model * Cmd<Msg> =
    match topLevelMsg with
    // Client
    | CloseSuggestions ->
        let nextModel = {
            currentModel with
                TermSearchState = {
                    currentModel.TermSearchState with
                        ShowSuggestions = false
                }
                AddBuildingBlockState = {
                    currentModel.AddBuildingBlockState with
                        ShowBuildingBlockTermSuggestions = false
                        ShowUnitTermSuggestions = false
                        ShowUnit2TermSuggestions = false
                }
        }
        nextModel, Cmd.none

let update (msg : Msg) (currentModel : Model) : Model * Cmd<Msg> =
    match msg with
    | DoNothing -> currentModel,Cmd.none
    | Batch msgSeq ->
        let cmd =
            Cmd.batch [
                yield!
                    msgSeq |> Seq.map Cmd.ofMsg
            ]
        currentModel, cmd
    | UpdateWarningModal (nextModalOpt) ->
        let nextModel = {
            currentModel with
                WarningModal = nextModalOpt
        }
        nextModel, Cmd.none
    | UpdatePageState (pageOpt:Route option) ->
        let nextCmd =
            match pageOpt with
            | Some Routing.Route.Validation ->
                GetTableValidationXml |> ExcelInterop |> Cmd.ofMsg
            | Some Routing.Route.ProtocolSearch ->
                GetAllProtocolsRequest |> ProtocolInsert |> Cmd.ofMsg
            | _ ->
                Cmd.none
        let nextPageState =
            match pageOpt with
            | Some page -> {
                CurrentPage = page
                CurrentUrl = Route.toRouteUrl page
                }
            | None -> {
                CurrentPage = Route.Home
                CurrentUrl = ""
                }
        let nextModel = {
            currentModel with
                PageState = nextPageState
        }
        nextModel, nextCmd
    // does not work due to office.js ->
    // https://stackoverflow.com/questions/42642863/office-js-nullifies-browser-history-functions-breaking-history-usage
    //| Navigate route ->
    //    currentModel, Navigation.newUrl (Routing.Route.toRouteUrl route)
    | Bounce (delay, bounceId, msgToBounce) ->

        let (debouncerModel, debouncerCmd) =
            currentModel.DebouncerState
            |> Debouncer.bounce delay bounceId msgToBounce

        let nextModel = {
            currentModel with
                DebouncerState = debouncerModel
        }

        nextModel,Cmd.map DebouncerSelfMsg debouncerCmd

    | DebouncerSelfMsg debouncerMsg ->
        let nextDebouncerState, debouncerCmd =
            Debouncer.update debouncerMsg currentModel.DebouncerState

        let nextModel = {
            currentModel with
                DebouncerState = nextDebouncerState
        }
        nextModel, debouncerCmd

    | ExcelInterop excelMsg ->
        let nextModel,nextCmd =
            currentModel
            |> handleExcelInteropMsg excelMsg
        nextModel,nextCmd

    | TermSearch termSearchMsg ->
        let nextTermSearchState,nextCmd =
            currentModel.TermSearchState
            |> handleTermSearchMsg termSearchMsg

        let nextModel = {
            currentModel with
                TermSearchState = nextTermSearchState
        }
        nextModel,nextCmd

    | AdvancedSearch advancedSearchMsg ->
        let nextAdvancedSearchState,nextCmd =
            currentModel.AdvancedSearchState
            |> handleAdvancedTermSearchMsg advancedSearchMsg

        let nextModel = {
            currentModel with
                AdvancedSearchState = nextAdvancedSearchState
        }
        nextModel,nextCmd
    | Dev devMsg ->
        let nextDevState,nextCmd =
            currentModel.DevState
            |> handleDevMsg devMsg
        
        let nextModel = {
            currentModel with
                DevState = nextDevState
        }
        nextModel,nextCmd

    | Api apiMsg ->
        let nextApiState,nextCmd =
            currentModel.ApiState
            |> handleApiMsg apiMsg

        let nextModel = {
            currentModel with
                ApiState = nextApiState
        }
        nextModel,nextCmd

    | PersistentStorage persistentStorageMsg ->
        let nextPersistentStorageState,nextCmd =
            currentModel.PersistentStorageState
            |> handlePersistenStorageMsg persistentStorageMsg

        let nextModel = {
            currentModel with
                PersistentStorageState = nextPersistentStorageState
        }

        nextModel,nextCmd

    | StyleChange styleChangeMsg ->
        let nextSiteStyleState,nextCmd =
            currentModel.SiteStyleState
            |> handleStyleChangeMsg styleChangeMsg

        let nextModel = {
            currentModel with
                SiteStyleState = nextSiteStyleState
        }

        nextModel,nextCmd

    | FilePicker filePickerMsg ->
        let nextFilePickerState,nextCmd =
            currentModel.FilePickerState
            |> handleFilePickerMsg filePickerMsg

        let nextModel = {
            currentModel with
                FilePickerState = nextFilePickerState
        }

        nextModel,nextCmd

    | AddBuildingBlock addBuildingBlockMsg ->
        let nextAddBuildingBlockState,nextCmd = 
            currentModel.AddBuildingBlockState
            |> handleAddBuildingBlockMsg addBuildingBlockMsg

        let nextModel = {
            currentModel with
                AddBuildingBlockState = nextAddBuildingBlockState
            }
        nextModel, nextCmd

    | Validation validationMsg ->
        let nextValidationState, nextCmd =
            currentModel.ValidationState
            |> handleValidationMsg validationMsg

        let nextModel = {
            currentModel with
                ValidationState = nextValidationState
            }
        nextModel, nextCmd

    | ProtocolInsert fileUploadJsonMsg ->
        let nextFileUploadJsonState, nextCmd =
            currentModel.ProtocolInsertState
            |> handleFileUploadJsonMsg fileUploadJsonMsg

        let nextModel = {
            currentModel with
                ProtocolInsertState = nextFileUploadJsonState
            }
        nextModel, nextCmd

    | BuildingBlockDetails buildingBlockDetailsMsg ->
        let nextState, nextCmd =
            currentModel.BuildingBlockDetailsState
            |> handleBuildingBlockMsg buildingBlockDetailsMsg

        let nextModel = {
            currentModel with
                BuildingBlockDetailsState = nextState
            }
        nextModel, nextCmd

    | SettingsXmlMsg msg ->
        let nextState, nextCmd =
            currentModel.SettingsXmlState
            |> handleSettingsXmlMsg msg
        let nextModel = {
            currentModel with
                SettingsXmlState = nextState
        }
        nextModel, nextCmd

    | SettingDataStewardMsg msg ->
        let nextState, nextCmd =
            currentModel.SettingsDataStewardState
            |> handleSettingsDataStewardMsg msg
        let nextModel = {
            currentModel with
                SettingsDataStewardState = nextState
        }
        nextModel, nextCmd

    | SettingsProtocolMsg msg ->
        let nextState, nextCmd =
            currentModel.SettingsProtocolState
            |> handleSettingsProtocolMsg msg
        let nextModel = {
            currentModel with
                SettingsProtocolState = nextState
        }
        nextModel, nextCmd

    | TopLevelMsg topLevelMsg ->
        let nextModel, nextCmd =
            handleTopLevelMsg topLevelMsg currentModel

        nextModel, nextCmd