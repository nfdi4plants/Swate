module Update

open Shared
open Routing
open Model
open Messages
open Elmish
open Elmish.Navigation
open Thoth.Elmish

let urlUpdate (result:Option<Page>) (currentModel:Model) : Model * Cmd<Msg> =
    match result with
    | Some page ->

        let nextPageState = {
            currentModel.PageState with
                CurrentPage = page
                CurrentUrl  = Page.toPath page
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
                CurrentPage = Page.NotFound
        }

        let nextModel = {
            currentModel with
                PageState = nextPageState
        }
        nextModel,Cmd.none

let handleExcelInteropMsg (excelInteropMsg: ExcelInteropMsg) (currentState:ExcelState) : ExcelState * Cmd<Msg> =
    match excelInteropMsg with

    | Initialized (h,p) ->
        let welcomeMsg = sprintf "Ready to go in %s running on %s" h p

        let nextState = {
            currentState with
                Host        = h
                Platform    = p
        }

        nextState, Cmd.ofMsg (("Info",welcomeMsg) |> (GenericLog >> Dev))
        
    | SyncContext passthroughMessage ->
        currentState,
        Cmd.OfPromise.either
            OfficeInterop.syncContext
            passthroughMessage
            (fun _ -> ExcelInterop (InSync passthroughMessage))
            (GenericError >> Dev)

    | InSync passthroughMessage ->
        currentState,
        ("Info",passthroughMessage)
        |> (GenericLog >> Dev)
        |> Cmd.ofMsg

    | TryExcel ->
        currentState,
        Cmd.OfPromise.either
            OfficeInterop.exampleExcelFunction 
            ()
            (SyncContext >> ExcelInterop)
            (GenericError >> Dev)

    | FillSelection fillValue ->
        currentState,
        Cmd.OfPromise.either
            OfficeInterop.fillValue  
            fillValue
            (SyncContext >> ExcelInterop)
            (GenericError >> Dev)

    | AddColumn columnValue ->
        currentState,
        Cmd.OfPromise.either
            OfficeInterop.addAnnotationColumn  
            columnValue
            (SyncContext >> ExcelInterop)
            (GenericError >> Dev)

    | CreateAnnotationTable isDark ->
        currentState,
        Cmd.OfPromise.either
            OfficeInterop.createAnnotationTable  
            isDark
            (SyncContext >> ExcelInterop)
            (GenericError >> Dev)

let handleSimpleTermSearchMsg (simpleTermSearchMsg: SimpleTermSearchMsg) (currentState:SimpleTermSearchState) : SimpleTermSearchState * Cmd<Msg> =
    match simpleTermSearchMsg with
    | SearchTermTextChange newTerm ->

        let triggerNewSearch =
            newTerm.Length > 2
           
        let (delay, bounceId, msgToBounce) =
            (System.TimeSpan.FromSeconds 0.5),
            "GetNewTermSuggestions",
            (
                if triggerNewSearch then
                    newTerm  |> (GetNewTermSuggestions >> Request >> Api)
                else
                    DoNothing
            )

        let nextState = {
            currentState with
                TermSearchText = newTerm
                ShowSuggestions = triggerNewSearch
                HasSuggestionsLoading = true
        }

        nextState, ((delay, bounceId, msgToBounce) |> Bounce |> Cmd.ofMsg)

    | TermSuggestionUsed suggestion ->

        let nextState = {
            SimpleTermSearchState.init() with
                TermSearchText = suggestion
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

let handleAdvancedTermSearchMsg (advancedTermSearchMsg: AdvancedTermSearchMsg) (currentState:AdvancedTermSearchState) : AdvancedTermSearchState * Cmd<Msg> =
    match advancedTermSearchMsg with
    |SearchOntologyTextChange newOntology ->
        let nextState = {
            currentState with
                ShowOntologySuggestions = newOntology.Length > 0
                OntologySearchText = newOntology
        }

        nextState,Cmd.none

    | OntologySuggestionUsed suggestion ->

        let nextAdvancedSearchOptions = {
            currentState.AdvancedSearchOptions with
                Ontology = Some suggestion
        }

        let nextState = {
            currentState with
                OntologySearchText      = suggestion.Name
                AdvancedSearchOptions   = nextAdvancedSearchOptions
                ShowOntologySuggestions = false
            }
        nextState, Cmd.none

    | AdvancedSearchOptionsChange opts ->

        let nextState = {
            currentState with
                AdvancedSearchOptions = opts
        }

        nextState,Cmd.none

    | AdvancedSearchResultUsed (res) ->
        let nextState = AdvancedTermSearchState.init()
            
        nextState,Cmd.ofMsg (res |> TermSuggestionUsed |> Simple  |> TermSearch)

    | NewAdvancedSearchResults results ->
        let nextState = {
            currentState with
                AdvancedSearchTermResults       = results
                ShowAdvancedSearchResults       = true
                HasAdvancedSearchResultsLoading = false
        }

        nextState,Cmd.none

let handleTermSearchMsg (termSearchMsg: TermSearchMsg) (currentState:TermSearchState) : TermSearchState * Cmd<Msg> =
    match termSearchMsg with
    | Simple simple ->
        let nextSimpleTermSearchState,nextCmd = handleSimpleTermSearchMsg simple currentState.Simple
        let nextState = {
            currentState with
                Simple = nextSimpleTermSearchState
        }
        nextState,nextCmd

    | Advanced advanced ->
        let nextSimpleAdvancedSearchState,nextCmd = handleAdvancedTermSearchMsg advanced currentState.Advanced
        let nextState = {
            currentState with
                Advanced = nextSimpleAdvancedSearchState
        }
        nextState,nextCmd

    | SwitchSearchMode ->
        let nextSearchMode =
            match currentState.SearchMode with
            | TermSearchMode.Simple    -> TermSearchMode.Advanced
            | TermSearchMode.Advanced  -> TermSearchMode.Simple

        let nextState = {
            currentState with
                Advanced    = AdvancedTermSearchState   .init()
                Simple      = SimpleTermSearchState     .init()
                SearchMode  = nextSearchMode
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
                LastFullError = Some e
                Log = LogItem.Error(System.DateTime.Now,e.Message)::currentState.Log
            }
        nextState, Cmd.none

    | LogTableMetadata ->
        currentState,
        Cmd.OfPromise.either
            OfficeInterop.getTableMetaData
            ()
            (SyncContext >> ExcelInterop)
            (GenericError >> Dev)

let handleApiRequestMsg (reqMsg: ApiRequestMsg) (currentState: ApiState) : ApiState * Cmd<Msg> =
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

    | GetNewTermSuggestions queryString ->
        let currentCall = {
                FunctionName = "getTermSuggestions"
                Status = Pending
        }

        let nextState = {
            currentState with
                currentCall = currentCall
        }

        nextState,
        Cmd.OfAsync.either
            Api.api.getTermSuggestions
            (5,queryString)
            (TermSuggestionResponse >> Response >> Api)
            (ApiError >> Api)

    | GetNewUnitTermSuggestions queryString ->
        let currentCall = {
                FunctionName = "getUnitTermSuggestions"
                Status = Pending
        }

        let nextState = {
            currentState with
                currentCall = currentCall
        }

        nextState,
        Cmd.OfAsync.either
            Api.api.getUnitTermSuggestions
            (5,queryString)
            (UnitTermSuggestionResponse >> Response >> Api)
            (ApiError >> Api)

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
            (options.Ontology,options.StartsWith,options.MustContain,options.EndsWith,options.KeepObsolete,options.DefinitionMustContain)
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

let handleApiResponseMsg (resMsg: ApiResponseMsg) (currentState: ApiState) : ApiState * Cmd<Msg> =
    match resMsg with
    | TermSuggestionResponse suggestions ->
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
            suggestions |> NewSuggestions |> Simple |> TermSearch |> Cmd.ofMsg
        ]

        nextState, cmds

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
            results |> NewAdvancedSearchResults |> Advanced |> TermSearch |> Cmd.ofMsg
        ]

        nextState, cmds

    | UnitTermSuggestionResponse suggestions ->
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
            suggestions |> NewUnitTermSuggestions |> AddBuildingBlock |> Cmd.ofMsg
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
    | NewFilesLoaded fileNames ->
        let nextState = {
            currentState with
                FileNames = fileNames
        }

        nextState, Cmd.none

    | RemoveFileFromFileList fileName ->
        let nextState = {
            currentState with
                FileNames =
                    currentState.FileNames
                    |> List.filter (fun fn -> not (fn = fileName))
        }

        nextState, Cmd.none

let handleAddBuildingBlockMsg (addBuildingBlockMsg:AddBuildingBlockMsg) (currentState: AddBuildingBlockState) : AddBuildingBlockState * Cmd<Msg> =
    match addBuildingBlockMsg with
    | NewBuildingBlockSelected nextBB ->
        let nextState = {
            currentState with
                CurrentBuildingBlock        = nextBB
                ShowBuildingBlockSelection  = false
        }

        nextState,Cmd.none

    | BuildingBlockNameChange newText ->
        let nextBB = {
            currentState.CurrentBuildingBlock with
                Name = newText
        }

        let nextState = {
            currentState with
                CurrentBuildingBlock = nextBB
        }

        nextState,Cmd.none

    | ToggleSelectionDropdown ->
        let nextState = {
            currentState with
                ShowBuildingBlockSelection = not currentState.ShowBuildingBlockSelection
        }

        nextState,Cmd.none

    | SearchUnitTermTextChange newTerm ->

        let triggerNewSearch =
            newTerm.Length > 2
       
        let (delay, bounceId, msgToBounce) =
            (System.TimeSpan.FromSeconds 0.5),
            "GetNewUnitTermSuggestions",
            (
                if triggerNewSearch then
                    newTerm  |> (GetNewUnitTermSuggestions >> Request >> Api)
                else
                    DoNothing
            )

        let nextState = {
            currentState with
                UnitTermSearchText              = newTerm
                ShowUnitTermSuggestions         = triggerNewSearch
                HasUnitTermSuggestionsLoading   = true
        }

        nextState, ((delay, bounceId, msgToBounce) |> Bounce |> Cmd.ofMsg)

    | NewUnitTermSuggestions suggestions ->
    
        let nextState = {
            currentState with
                UnitTermSuggestions             = suggestions
                ShowUnitTermSuggestions         = true
                HasUnitTermSuggestionsLoading   = false
        }

        nextState,Cmd.none

    | UnitTermSuggestionUsed suggestion ->

        let nextState = {
            currentState with
                UnitTermSearchText              = suggestion.Name
                UnitTerm                        = Some suggestion
                ShowUnitTermSuggestions         = false
                HasUnitTermSuggestionsLoading   = false
        }
        nextState, Cmd.none

    | BuildingBlockHasUnitSwitch ->

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
                    UnitTerm = None
                    UnitTermSearchText = ""
                    UnitTermSuggestions = [||]
                    ShowUnitTermSuggestions = false
                    HasUnitTermSuggestionsLoading = false
                }
        nextState, Cmd.none

let update (msg : Msg) (currentModel : Model) : Model * Cmd<Msg> =
    match msg with
    | DoNothing -> currentModel,Cmd.none
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