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
            UpdateLastFullError (exn(eMsg) |> Some) |> Dev
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

let handleExcelInteropMsg (excelInteropMsg: ExcelInteropMsg) (currentState:ExcelState) : ExcelState * Cmd<Msg> =

    match excelInteropMsg with

    | PipeCreateAnnotationTableInfo createAnnotationTableMsg ->
        let cmd =
            Cmd.OfPromise.either
                OfficeInterop.getTableInfoForAnnoTableCreation
                ()
                (fun (allNames) -> createAnnotationTableMsg allNames |> ExcelInterop)
                (GenericError >> Dev)
        currentState, cmd

    /// This message is necessary to find the annotation table name in the active worksheet. The name is then passed on to the message variable.
    | PipeActiveAnnotationTable nextMsg ->
        let cmd =
            Cmd.OfPromise.either
                OfficeInterop.tryFindActiveAnnotationTable
                ()
                (fun activeTableNameRes -> nextMsg activeTableNameRes |> ExcelInterop)
                (GenericError >> Dev)
        currentState, cmd


    | AutoFitTable activeTableNameRes->
        let cmd name =
            Cmd.OfPromise.either
                OfficeInterop.autoFitTable
                (name)
                (fun msg -> InSync msg |> ExcelInterop)
                (GenericError >> Dev)
        let cmd = matchActiveTableResToMsg activeTableNameRes cmd 
        currentState, cmd

    | Initialized (h,p) ->
        let welcomeMsg = sprintf "Ready to go in %s running on %s" h p

        let nextState = {
            currentState with
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

        nextState, cmd
        
    | SyncContext (activeTableNameRes,passthroughMessage) ->
        currentState,
        Cmd.batch [
            Cmd.ofMsg (AutoFitTable activeTableNameRes |> ExcelInterop)
            Cmd.OfPromise.either
                OfficeInterop.tryFindActiveAnnotationTable
                ()
                (AnnotationTableExists >> ExcelInterop)
                (GenericError >> Dev)
            Cmd.OfPromise.either
                OfficeInterop.syncContext
                passthroughMessage
                (fun _ -> ExcelInterop (InSync passthroughMessage))
                (GenericError >> Dev)
        ]

    | AnnotationTableExists annoTableOpt ->
        let exists =
            match annoTableOpt with
            | Success name -> true
            | _ -> false
        let nextState = {
            currentState with
                HasAnnotationTable = exists
        }

        nextState,Cmd.none

    | InSync passthroughMessage ->
        currentState,
        ("Info",passthroughMessage)
        |> (GenericLog >> Dev)
        |> Cmd.ofMsg

    | FillSelection (activeTableNameRes,fillValue,fillTerm) ->
        let cmd name =
            Cmd.OfPromise.either
                OfficeInterop.fillValue  
                (name,fillValue,fillTerm)
                ((fun x -> SyncContext (activeTableNameRes,x)) >> ExcelInterop)
                (GenericError >> Dev)
        let cmd = matchActiveTableResToMsg activeTableNameRes cmd
        currentState, cmd

    | AddAnnotationBlock (activeTableNameRes,minBuildingBlockInfo) ->
        let cmd tableName =
            Cmd.OfPromise.either
                OfficeInterop.addAnnotationBlock  
                (tableName,minBuildingBlockInfo)
                (fun (newColName,format,msg) ->
                    FormatColumn (activeTableNameRes,newColName,format,msg) |> ExcelInterop
                )
                (GenericError >> Dev)
        let cmd = matchActiveTableResToMsg activeTableNameRes cmd 
        currentState, cmd

    //| AddAnnotationBlocks (activeTableNameRes,minBuildingBlockInfos) ->
    //    let cmd tableName =
    //        Cmd.OfPromise.either
    //            OfficeInterop.addAnnotationBlocks 
    //            (tableName,minBuildingBlockInfos)
    //            (fun (newColName,format,msg) ->
    //                FormatColumn (activeTableNameRes,newColName,format,msg) |> ExcelInterop
    //            )
    //            (GenericError >> Dev)
    //    let cmd = matchActiveTableResToMsg activeTableNameRes cmd 
    //    currentState, cmd

    | AddUnitToAnnotationBlock (activeTableNameRes, format, unitTermOpt) ->
        let cmd name =
            Cmd.OfPromise.either
                OfficeInterop.addUnitToExistingBuildingBlock
                (name,format,unitTermOpt)
                (fun (newColName,format,msg) ->
                    FormatColumn (activeTableNameRes, newColName, format, msg) |> ExcelInterop
                )
                (GenericError >> Dev)
        let cmd = matchActiveTableResToMsg activeTableNameRes cmd
        currentState, cmd

    | FormatColumn (activeTableNameRes,colName,format,msg) ->
        let cmd name =
            Cmd.batch [
                Cmd.ofMsg (InSync msg |> ExcelInterop)
                Cmd.OfPromise.either
                    (OfficeInterop.changeTableColumnFormat name colName)
                    format
                    ((fun msg -> SyncContext (activeTableNameRes,msg)) >> ExcelInterop)
                    (GenericError >> Dev)
            ]
        let cmd = matchActiveTableResToMsg activeTableNameRes cmd 
        currentState,cmd

    | CreateAnnotationTable (allTableNames,isDark) ->
        let cmd =
            Cmd.OfPromise.either
                OfficeInterop.createAnnotationTable  
                (allTableNames,isDark)
                (fun (res,msg) ->
                    Msg.Batch [
                        AnnotationtableCreated (res,msg) |> ExcelInterop
                    ]
                )
                (GenericError >> Dev)
        currentState,cmd

    | AnnotationtableCreated (activeTableNameRes,range) ->
        let nextState = {
            currentState with
                HasAnnotationTable = true
        }

        nextState,Cmd.ofMsg(SyncContext (activeTableNameRes,range)|> ExcelInterop)


    | GetParentTerm activeTableNameRes ->
        let cmd name =
            Cmd.OfPromise.either
                OfficeInterop.getParentTerm
                (name)
                (StoreParentOntologyFromOfficeInterop >> TermSearch)
                (GenericError >> Dev)
        let cmd = matchActiveTableResToMsg activeTableNameRes cmd
        currentState, cmd
    //
    | GetTableValidationXml activeTableNameRes ->
        let successCmd tableName =
            Cmd.OfPromise.either
                OfficeInterop.getTableRepresentation
                (tableName)
                (fun (currentTableValidation, buildingBlocks,msg) ->
                    StoreTableRepresentationFromOfficeInterop (currentTableValidation, buildingBlocks, msg) |> Validation)
                (GenericError >> Dev)
        let cmd = matchActiveTableResToMsg activeTableNameRes successCmd 
        currentState, cmd
    | WriteTableValidationToXml (newTableValidation,currentSwateVersion) ->
        let cmd =
            Cmd.OfPromise.either
                OfficeInterop.writeTableValidationToXml
                (newTableValidation, currentSwateVersion)
                (fun x ->
                    Msg.Batch [
                        GenericLog x |> Dev
                        PipeActiveAnnotationTable GetTableValidationXml |> ExcelInterop
                    ]
                )
                (GenericError >> Dev)

        currentState, cmd
    | DeleteAllCustomXml ->
        let cmd =
            Cmd.OfPromise.either
                OfficeInterop.deleteAllCustomXml
                ()
                (GenericLog >> Dev)
                (GenericError >> Dev)
        currentState, cmd
    | GetSwateCustomXml ->
        let cmd =
            Cmd.OfPromise.either
                OfficeInterop.getSwateCustomXml
                ()
                (GenericLog >> Dev)
                (GenericError >> Dev)
        currentState, cmd
    //
    | FillHiddenColsRequest activeTableNameRes ->
        let cmd name =
            Cmd.OfPromise.either
                OfficeInterop.createSearchTermsIFromTable 
                (name)
                (SearchForInsertTermsRequest >> Request >> Api)
                (fun e ->
                    Msg.Batch [
                        UpdateFillHiddenColsState FillHiddenColsState.Inactive |> ExcelInterop
                        GenericError e |> Dev
                    ] )
        let cmd = matchActiveTableResToMsg activeTableNameRes cmd
        let cmd2 = UpdateFillHiddenColsState FillHiddenColsState.ExcelCheckHiddenCols |> ExcelInterop |> Cmd.ofMsg
        let cmds = Cmd.batch [cmd; cmd2]
        currentState, cmds

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
        currentState, cmds


    | UpdateFillHiddenColsState newState ->
        let nextState = {
            currentState with
                FillHiddenColsStateStore = newState
        }
        nextState, Cmd.none
    //
    | InsertFileNames (activeTableNameRes,fileNameList) ->
        let nextState = currentState
        let cmd name = 
            Cmd.OfPromise.either
                OfficeInterop.insertFileNamesFromFilePicker 
                (name, fileNameList)
                ((fun x -> 
                    ("Debug",x) |> GenericLog) >> Dev
                )
                (GenericError >> Dev)
        let cmd = matchActiveTableResToMsg activeTableNameRes cmd
        nextState, cmd

    | TryExcel  ->
        let nextState = currentState
        let cmd = 
            Cmd.OfPromise.either
                OfficeInterop.exampleExcelFunction 
                ()
                ((fun x -> 
                    ("Debug",x) |> GenericLog) >> Dev
                )
                (GenericError >> Dev)
        nextState, cmd
    | TryExcel2 ->
        let nextState = currentState
        let cmd = 
            Cmd.OfPromise.either
                OfficeInterop.exampleExcelFunction2 
                ()
                ((fun x -> 
                    ("Debug",x) |> GenericLog) >> Dev
                )
                (GenericError >> Dev)
        nextState, cmd
    //| _ ->
    //    printfn "Hit currently non existing message"
    //    currentState, Cmd.none

    //| ExcelSubscriptionMsg msg ->
    //    let m,cmd = OfficeInterop.Types.Subscription.update msg currentState.SubscriptionState
    //    let nextState = {
    //        currentState with
    //            SubscriptionState = m
    //    }
    //    nextState, Cmd.map (ExcelSubscriptionMsg >> ExcelInterop) cmd

        
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

    | LogTableMetadata activeTableNameRes ->
        let cmd name =
            Cmd.OfPromise.either
                OfficeInterop.getTableMetaData
                (name)
                ((fun msg -> SyncContext (activeTableNameRes,msg)) >> ExcelInterop)
                (GenericError >> Dev)
        let cmd = matchActiveTableResToMsg activeTableNameRes cmd
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
        //let finishedCall = {
        //    currentState.currentCall with
        //        Status = Successfull
        //}

        //let nextState = {
        //    currentState with
        //        currentCall = noCall
        //        callHistory = finishedCall::currentState.callHistory
        //}

        //let cmds = Cmd.batch [
        //    ("Debug",sprintf "[ApiSuccess]: Call %s successfull." finishedCall.FunctionName) |> ApiSuccess |> Api |> Cmd.ofMsg
        //    suggestions |> NewSuggestions |> TermSearch |> Cmd.ofMsg
        //]

        //nextState, cmds
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
    match fujMsg with
    // Client
    | UpdateUploadData newDataString ->
        let nextState = {
            currentState with
                UploadData = newDataString
        }
        nextState, Cmd.ofMsg (ParseJsonToProcessRequest newDataString |> ProtocolInsert)
    | ParseJsonToProcessRequest parsableString ->
        let cmd =
            Cmd.OfAsync.either
                Api.isaDotNetApi.parseJsonToProcess
                parsableString
                (Ok >> ParseJsonToProcessResult)
                (Result.Error >> ParseJsonToProcessResult)
        currentState, Cmd.map ProtocolInsert cmd 
    | ParseJsonToProcessResult (Ok isaProcess) ->
        let nextState = {
            currentState with
                ProcessModel = Some isaProcess
        }
        nextState, Cmd.none
    | ParseJsonToProcessResult (Result.Error e) ->
        let cmd =
            GenericError e |> Dev |> Cmd.ofMsg 
        currentState, cmd
    //| SendJson ->
    //    let cmd =
    //        Cmd.OfAsync.perform
    //            Api.isaDotNetApi.tryTestProcess
    //            ISADotNet.Process.empty
    //            (fun x -> GenericLog ("info", "sent process"))
    //    currentState, Cmd.map Dev cmd 


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
    | UpdatePageState (pageOpt:Route option) ->
        let nextCmd =
            match pageOpt with
            | Some Routing.Route.Validation ->
                PipeActiveAnnotationTable GetTableValidationXml |> ExcelInterop |> Cmd.ofMsg
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
    /// does not work due to office.js ->
    /// https://stackoverflow.com/questions/42642863/office-js-nullifies-browser-history-functions-breaking-history-usage
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

        nextModel,Cmd.batch [Cmd.map DebouncerSelfMsg debouncerCmd]

    | DebouncerSelfMsg debouncerMsg ->
        let nextDebouncerState, debouncerCmd =
            Debouncer.update debouncerMsg currentModel.DebouncerState

        let nextModel = {
            currentModel with
                DebouncerState = nextDebouncerState
        }
        nextModel, debouncerCmd

    | ExcelInterop excelMsg ->
        let nextExcelState,nextCmd =
            currentModel.ExcelState
            |> handleExcelInteropMsg excelMsg

        let nextModel = {
            currentModel with
                ExcelState = nextExcelState
        }
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

    | TopLevelMsg topLevelMsg ->
        let nextModel, nextCmd =
            handleTopLevelMsg topLevelMsg currentModel

        nextModel, nextCmd