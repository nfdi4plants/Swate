module Update

open Elmish
open Thoth.Elmish

open Shared
open TermTypes
open OfficeInteropTypes
open Routing
open Model
open Messages

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

let handleDevMsg (devMsg: DevMsg) (currentState:DevState) : DevState * Cmd<Msg> =
    match devMsg with
    | GenericLog (nextCmd,(level,logText)) ->
        let nextState = {
            currentState with
                Log = (LogItem.ofStringNow level logText)::currentState.Log
        }
        nextState, nextCmd

    | GenericInteropLogs (nextCmd,logs) ->
        let parsedLogs = logs |> List.map LogItem.ofInteropLogginMsg
        let nextState = {
            currentState with
                Log = parsedLogs@currentState.Log
        }
        nextState, nextCmd

    | GenericError (nextCmd, e) ->
        OfficeInterop.consoleLog (sprintf "GenericError occured: %s" e.Message)
        let nextState = {
            currentState with
                Log = LogItem.Error(System.DateTime.Now,e.Message)::currentState.Log
                LastFullError = Some e
            }
        nextState, nextCmd

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
                (curry GenericLog Cmd.none >> Dev)
                (curry GenericError Cmd.none >> Dev)
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

    let handleTermSuggestionByParentTermRequest (apiFunctionname:string) (responseHandler: DbDomain.Term [] -> ApiMsg) queryString (termMin:TermMinimal) =
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
                (5,queryString,termMin)
                (responseHandler >> Api)
                (ApiError >> Api)

        nextState,nextCmd

    match reqMsg with
    | TestOntologyInsert (a,b,d,e) ->

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
            (a,b,d,e)
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

    | SearchForInsertTermsRequest (tableTerms) ->
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
                tableTerms
                (SearchForInsertTermsResponse >> Response >> Api)
                (fun e ->
                    Msg.Batch [
                        OfficeInterop.UpdateFillHiddenColsState OfficeInterop.FillHiddenColsState.Inactive |> OfficeInteropMsg
                        ApiError e |> Api
                    ] )
        let stateCmd = OfficeInterop.UpdateFillHiddenColsState OfficeInterop.FillHiddenColsState.ServerSearchDatabase |> OfficeInteropMsg |> Cmd.ofMsg
        let cmds = Cmd.batch [cmd; stateCmd]
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
            (TermSearch.NewSuggestions >> TermSearchMsg)
            suggestions

    | UnitTermSuggestionResponse (suggestions,relUnit) ->

        handleUnitTermSuggestionResponse
            (BuildingBlock.Msg.NewUnitTermSuggestions >> BuildingBlockMsg)
            suggestions
            relUnit
            

    | BuildingBlockNameSuggestionsResponse suggestions ->

        handleTermSuggestionResponse
            (BuildingBlock.Msg.NewBuildingBlockNameSuggestions >> BuildingBlockMsg)
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
            results |> AdvancedSearch.NewAdvancedSearchResults |> AdvancedSearchMsg |> Cmd.ofMsg
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

    | SearchForInsertTermsResponse (termsWithSearchResult) ->
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
            OfficeInterop.FillHiddenColumns (termsWithSearchResult) |> OfficeInteropMsg |> Cmd.ofMsg
        let loggingCmd =
             ("Debug",sprintf "[ApiSuccess]: Call %s successfull." finishedCall.FunctionName) |> ApiSuccess |> Api |> Cmd.ofMsg
        nextState, Cmd.batch [cmd; loggingCmd]

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

        nextState, curry GenericLog Cmd.none ("Error",sprintf "[ApiError]: Call %s failed with: %s" failedCall.FunctionName e.Message) |> Dev |> Cmd.ofMsg

    | ApiSuccess (level,logMsg) ->
        currentState, curry GenericLog Cmd.none (level,logMsg) |> Dev |> Cmd.ofMsg

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
    | GetSelectedBuildingBlockTermsRequest searchTerms ->
        let nextState = {
            currentState with
                CurrentRequestState = RequestBuildingBlockInfoStates.RequestDataBaseInformation
        }
        let cmd =
            Cmd.OfAsync.either
                Api.api.getTermsByNames
                searchTerms
                (GetSelectedBuildingBlockTermsResponse >> BuildingBlockDetails)
                (fun x ->
                    Msg.Batch [
                        curry GenericError Cmd.none x |> Dev
                        UpdateCurrentRequestState Inactive |> BuildingBlockDetails
                    ]
                )
        nextState, cmd
    | GetSelectedBuildingBlockTermsResponse searchTermResults ->
        let nextState = {
            currentState with
                ShowDetails         = true
                BuildingBlockValues = searchTermResults
                CurrentRequestState = Inactive
        }
        nextState, Cmd.none

let handleSettingsDataStewardMsg (topLevelMsg:SettingsDataStewardMsg) (currentState: SettingsDataStewardState) : SettingsDataStewardState * Cmd<Msg> =
    match topLevelMsg with
    // Client
    | UpdatePointerJson nextPointerJson ->
        let nextState = {
            currentState with
                PointerJson = nextPointerJson
        }
        nextState, Cmd.none

let handleXLSXConverterMsg (msg:XLSXConverterMsg) (currentModel: Model) : Model * Cmd<Msg> =
    match msg with
    // Client
    | StoreXLSXByteArray byteArr ->
        let nextModel = {
            currentModel with
                XLSXByteArray = byteArr
        }
        nextModel, Cmd.none
    | GetAssayJsonRequest byteArr ->
        let cmd =
            Cmd.OfAsync.either
                Api.isaDotNetCommonApi.toAssayJSON
                byteArr
                (GetAssayJsonResponse >> XLSXConverterMsg)
                (ApiError >> Api)
        currentModel, cmd
    | GetAssayJsonResponse jsonStr ->
        let nextModel = {
            currentModel with
                XLSXJSONResult = jsonStr
        }
        nextModel, Cmd.none

//let handleSettingsProtocolMsg (topLevelMsg:SettingsProtocolMsg) (currentState: SettingsProtocolState) : SettingsProtocolState * Cmd<Msg> =
//    match topLevelMsg with
//    // Client
//    | UpdateProtocolsFromDB nextProtFromDB ->
//        let nextState = {
//            currentState with
//                ProtocolsFromDB = nextProtFromDB
//        }
//        nextState, Cmd.none
//    | UpdateProtocolsFromExcel nextProtFromExcel ->
//        let nextState = {
//            currentState with
//                ProtocolsFromExcel = nextProtFromExcel
//        }
//        nextState, Cmd.none
//    // Excel
//    | GetActiveProtocolGroupXmlParsed ->
//        let cmd =
//            Cmd.OfPromise.either
//                OfficeInterop.getActiveProtocolGroupXmlParsed
//                ()
//                (fun x ->
//                    Msg.Batch [
//                        UpdateProtocolsFromExcel x |> SettingsProtocolMsg
//                        GetProtocolsFromDBRequest x |> SettingsProtocolMsg
//                    ]
//                )
//                (GenericError >> Dev)
//        currentState, cmd
//    | UpdateProtocolByNewVersion (prot, protTemplate) ->
//        failwith """Function "UpdateProtocolByNewVersion" is currently not supported."""
//        //let cmd =
//        //    Cmd.OfPromise.either
//        //        OfficeInterop.updateProtocolByNewVersion
//        //        (prot,protTemplate)
//        //        (AddAnnotationBlocks >> ExcelInterop)
//        //        (GenericError >> Dev)
//        currentState, Cmd.none
//    // Server
//    | GetProtocolsFromDBRequest activeProtGroupOpt ->
//        let cmd =
//            match activeProtGroupOpt with
//            | Some protGroup ->
//                let protNames = protGroup.Protocols |> List.map (fun x -> x.Id) |> Array.ofList
//                Cmd.OfAsync.either
//                    Api.api.getProtocolsByName
//                    protNames
//                    (UpdateProtocolsFromDB >> SettingsProtocolMsg)
//                    (GenericError >> Dev)
//            | None ->
//                GenericLog ("Info", "No protocols found for active table") |> Dev |> Cmd.ofMsg
//        currentState, cmd
            
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
                OfficeInterop.GetTableValidationXml |> OfficeInteropMsg |> Cmd.ofMsg
            | Some Routing.Route.ProtocolSearch ->
                Protocol.GetAllProtocolsRequest |> ProtocolMsg |> Cmd.ofMsg
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

    | OfficeInteropMsg excelMsg ->
        let nextModel,nextCmd = currentModel |> Update.OfficeInterop.update excelMsg
        nextModel,nextCmd

    | TermSearchMsg termSearchMsg ->
        let nextTermSearchState,nextCmd =
            currentModel.TermSearchState
            |> TermSearch.update termSearchMsg

        let nextModel = {
            currentModel with
                TermSearchState = nextTermSearchState
        }
        nextModel,nextCmd

    | AdvancedSearchMsg advancedSearchMsg ->
        let nextAdvancedSearchState,nextCmd =
            currentModel.AdvancedSearchState
            |> AdvancedSearch.update advancedSearchMsg

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

    | FilePickerMsg filePickerMsg ->
        let nextFilePickerState,nextCmd =
            currentModel.FilePickerState
            |> FilePicker.update filePickerMsg

        let nextModel = {
            currentModel with
                FilePickerState = nextFilePickerState
        }

        nextModel,nextCmd

    | BuildingBlockMsg addBuildingBlockMsg ->
        let nextAddBuildingBlockState,nextCmd = 
            currentModel.AddBuildingBlockState
            |> BuildingBlock.update addBuildingBlockMsg

        let nextModel = {
            currentModel with
                AddBuildingBlockState = nextAddBuildingBlockState
            }
        nextModel, nextCmd

    | ValidationMsg validationMsg ->
        let nextValidationState, nextCmd =
            currentModel.ValidationState
            |> Validation.update validationMsg

        let nextModel = {
            currentModel with
                ValidationState = nextValidationState
            }
        nextModel, nextCmd

    | ProtocolMsg fileUploadJsonMsg ->
        let nextFileUploadJsonState, nextCmd =
            currentModel.ProtocolState
            |> Protocol.update fileUploadJsonMsg

        let nextModel = {
            currentModel with
                ProtocolState = nextFileUploadJsonState
            }
        nextModel, nextCmd

    | XLSXConverterMsg msg ->
        let nextModel, nextMsg = handleXLSXConverterMsg msg currentModel
        nextModel, nextMsg

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
            |> SettingsXml.update msg
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

    | JSONExporterMsg msg ->
        let nextModel, nextCmd = currentModel |> JSONExporter.update msg
        nextModel, nextCmd

    //| SettingsProtocolMsg msg ->
    //    let nextState, nextCmd =
    //        currentModel.SettingsProtocolState
    //        |> handleSettingsProtocolMsg msg
    //    let nextModel = {
    //        currentModel with
    //            SettingsProtocolState = nextState
    //    }
    //    nextModel, nextCmd

    | TopLevelMsg topLevelMsg ->
        let nextModel, nextCmd =
            handleTopLevelMsg topLevelMsg currentModel

        nextModel, nextCmd