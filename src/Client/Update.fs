module Update

open Elmish
open Thoth.Elmish

open Shared
open TermTypes
open OfficeInteropTypes
open Routing
open Model
open Messages

let urlUpdate (route: Route option) (currentModel:Model) : Model * Cmd<Messages.Msg> =
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
        let nextPageState = {
            currentModel.PageState with
                CurrentPage = Route.NotFound
        }

        let nextModel = {
            currentModel with
                PageState = nextPageState
        }
        nextModel,Cmd.none

module Dev = 

    let update (devMsg: DevMsg) (currentState:DevState) : DevState * Cmd<Messages.Msg> =
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
            let nextState = {
                currentState with
                    Log = LogItem.Error(System.DateTime.Now,e.GetPropagatedError())::currentState.Log
                    LastFullError = Some (e)
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
                    (curry GenericLog Cmd.none >> DevMsg)
                    (curry GenericError Cmd.none >> DevMsg)
            currentState, cmd

let handleApiRequestMsg (reqMsg: ApiRequestMsg) (currentState: ApiState) : ApiState * Cmd<Messages.Msg> =

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
        

let handleApiResponseMsg (resMsg: ApiResponseMsg) (currentState: ApiState) : ApiState * Cmd<Messages.Msg> =

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

open Dev
open Messages

let handleApiMsg (apiMsg:ApiMsg) (currentState:ApiState) : ApiState * Cmd<Messages.Msg> =
    match apiMsg with
    | ApiError e ->
        let failedCall = {
            currentState.currentCall with
                Status = Failed (e.GetPropagatedError())
        }

        let nextState = {
            currentState with
                currentCall = noCall
                callHistory = failedCall::currentState.callHistory
        }

        nextState, curry GenericLog Cmd.none ("Error",sprintf "[ApiError]: Call %s failed with: %s" failedCall.FunctionName (e.GetPropagatedError())) |> DevMsg |> Cmd.ofMsg

    | ApiSuccess (level,logMsg) ->
        currentState, curry GenericLog Cmd.none (level,logMsg) |> DevMsg |> Cmd.ofMsg

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
                        curry GenericError Cmd.none x |> DevMsg
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
                Cmd.OfPromise.perform
                    OfficeInterop.getTableRepresentation
                    ()
                    (Validation.StoreTableRepresentationFromOfficeInterop >> ValidationMsg)
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
    | DevMsg msg ->
        let nextDevState,nextCmd = currentModel.DevState |> Dev.update msg
        
        let nextModel = {
            currentModel with
                DevState = nextDevState
        }
        nextModel,nextCmd

    | Api apiMsg ->
        let nextApiState,nextCmd = currentModel.ApiState |> handleApiMsg apiMsg

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

    | JsonExporterMsg msg ->
        let nextModel, nextCmd = currentModel |> JsonExporter.update msg
        nextModel, nextCmd

    | TemplateMetadataMsg msg ->
        let nextModel, nextCmd = currentModel |> TemplateMetadata.update msg
        nextModel, nextCmd

    | DagMsg msg ->
        let nextModel, nextCmd = currentModel |> Dag.update msg
        nextModel, nextCmd

    | TopLevelMsg topLevelMsg ->
        let nextModel, nextCmd =
            handleTopLevelMsg topLevelMsg currentModel

        nextModel, nextCmd