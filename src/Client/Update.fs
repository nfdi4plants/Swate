[<AutoOpen>]
module Update.Update

open Elmish
open Thoth.Elmish

open Shared
open TermTypes
open OfficeInteropTypes
open Routing
open Messages
open Model

let urlUpdate (route: Route option) (currentModel:Model) : Model * Cmd<Messages.Msg> =
    match route with
    | Some (Route.Home queryIntegerOption) ->
        let swatehost = Swatehost.ofQueryParam queryIntegerOption
        let nextModel = {
            currentModel with 
                Model.PageState.CurrentPage = Route.BuildingBlock
                Model.PageState.IsExpert = false
                Model.PersistentStorageState.Host = Some swatehost
        }
        nextModel,Cmd.none
    | Some page ->
        let nextModel = {
            currentModel with 
                Model.PageState.CurrentPage = page
                Model.PageState.IsExpert = page.isExpert
        }
        nextModel,Cmd.none
    | None ->
        let nextModel = {
            currentModel with 
                Model.PageState.CurrentPage = Route.BuildingBlock
                Model.PageState.IsExpert = false
        }
        nextModel,Cmd.none

module AdvancedSearch =

    let update (msg: AdvancedSearch.Msg) (model:Model) : Model * Cmd<Messages.Msg> =
        match msg with
        | AdvancedSearch.GetSearchResults content -> 
            let cmd =
                Cmd.OfAsync.either 
                    Api.api.getTermsForAdvancedSearch
                    content.config
                    (fun terms -> Run (fun _ -> content.responseSetter terms))
                    (curry GenericError Cmd.none >> DevMsg)
                    
            model, cmd

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
            let parsedDisplayLogs = parsedLogs |> List.filter (fun x -> match x with | Error _ | Warning _ -> true; | _ -> false)
            let nextState = {
                Log = parsedLogs@currentState.Log
                DisplayLogList = parsedDisplayLogs@currentState.DisplayLogList
            }
            let batch = Cmd.batch [
                let modalName = "GenericInteropLogs"
                if List.isEmpty parsedDisplayLogs |> not then Cmd.ofEffect(fun dispatch -> Modals.Controller.renderModal(modalName, Modals.InteropLoggingModal.interopLoggingModal(nextState, dispatch)))
                nextCmd
            ]
            nextState, batch

        | GenericError (nextCmd, e) ->
            let nextState = {
                currentState with
                    Log = LogItem.Error(System.DateTime.Now,e.GetPropagatedError())::currentState.Log
                }
            let batch = Cmd.batch [
                let modalName = "GenericError"
                Cmd.ofEffect(fun _ -> Modals.Controller.renderModal(modalName, Modals.ErrorModal.errorModal(e)))
                nextCmd
            ]
            nextState, batch

        | UpdateDisplayLogList newList ->
            let nextState = {
                currentState with
                    DisplayLogList = newList
            }
            nextState, Cmd.none

        | LogTableMetadata ->
            let cmd =
                Cmd.OfPromise.either
                    OfficeInterop.Core.getTableMetaData
                    ()
                    (curry GenericLog Cmd.none >> DevMsg)
                    (curry GenericError Cmd.none >> DevMsg)
            currentState, cmd

let handlePersistenStorageMsg (persistentStorageMsg: PersistentStorage.Msg) (currentState:PersistentStorageState) : PersistentStorageState * Cmd<Msg> =
    match persistentStorageMsg with
    | PersistentStorage.NewSearchableOntologies onts ->
        let nextState = {
            currentState with
                SearchableOntologies    = onts |> Array.map (fun ont -> ont.Name |> SorensenDice.createBigrams, ont)
                HasOntologiesLoaded     = true
        }

        nextState,Cmd.none
    | PersistentStorage.UpdateAppVersion appVersion ->
        let nextState = {
            currentState with
                AppVersion = appVersion
        }
        nextState,Cmd.none
    | PersistentStorage.UpdateShowSidebar show ->
        {currentState with ShowSideBar = show}, Cmd.none

let handleBuildingBlockDetailsMsg (topLevelMsg:BuildingBlockDetailsMsg) (currentState: BuildingBlockDetailsState) : BuildingBlockDetailsState * Cmd<Msg> =
    match topLevelMsg with
    // Client
    | UpdateBuildingBlockValues nextValues ->
        let nextState = {
            currentState with
                BuildingBlockValues = nextValues
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
            BuildingBlockValues = searchTermResults
            CurrentRequestState = Inactive
        }
        let cmd = Cmd.ofEffect(fun dispatch ->
            Modals.Controller.renderModal("BuildingBlockDetails", Modals.BuildingBlockDetailsModal.buildingBlockDetailModal(nextState, dispatch))
        )
        nextState, cmd

module Ontologies =
    let update (omsg: Ontologies.Msg) (model: Model) =
        match omsg with
        | Ontologies.GetOntologies ->
            let cmd =
                Cmd.OfAsync.either 
                    Api.api.getAllOntologies
                    ()
                    (PersistentStorage.NewSearchableOntologies >> PersistentStorageMsg)
                    (curry GenericError Cmd.none >> DevMsg)
            model, cmd

let update (msg : Msg) (model : Model) : Model * Cmd<Msg> =
    let innerUpdate (msg: Msg) (currentModel: Model) =
        match msg with
        | DoNothing -> currentModel,Cmd.none
        | Run callback -> 
            callback()
            model, Cmd.none
        | UpdateHistory next -> {model with History = next}, Cmd.none
        | TestMyAPI ->
            let cmd =
                Cmd.OfAsync.either
                    Api.testAPIv1.test
                        ()
                        (curry GenericLog Cmd.none)
                        (curry GenericError Cmd.none)
            currentModel, Cmd.map DevMsg cmd
        | TestMyPostAPI ->
            let cmd =
                Cmd.OfAsync.either
                    Api.testAPIv1.postTest
                        ("instrument Mod")
                        (curry GenericLog Cmd.none)
                        (curry GenericError Cmd.none)
            currentModel, Cmd.map DevMsg cmd
        | Batch msgSeq ->
            let cmd =
                Cmd.batch [
                    yield!
                        msgSeq |> Seq.map Cmd.ofMsg
                ]
            currentModel, cmd
        | UpdatePageState (pageOpt:Route option) ->
            let nextPageState =
                match pageOpt with
                | Some page -> {
                    currentModel.PageState with
                        CurrentPage = page
                    }
                | None -> {
                    currentModel.PageState with
                        CurrentPage = Route.BuildingBlock
                    }
            let nextModel = {
                currentModel with
                    PageState = nextPageState
            }
            nextModel, Cmd.none
        | UpdateIsExpert b ->
            let nextPageState = {
                currentModel.PageState with
                    IsExpert = b
            }
            let nextModel = {
                currentModel with
                    PageState = nextPageState
            }
            nextModel, Cmd.none        
        // does not work due to office.js ->
        // https://stackoverflow.com/questions/42642863/office-js-nullifies-browser-history-functions-breaking-history-usage
        //| Navigate route ->
        //    currentModel, Navigation.newUrl (Routing.Route.toRouteUrl route)

        | OntologyMsg msg ->
            let nextModel, cmd = Ontologies.update msg model
            nextModel, cmd

        | OfficeInteropMsg excelMsg ->
            let nextState,nextModel0,nextCmd = Update.OfficeInterop.update model.ExcelState currentModel excelMsg
            let nextModel = {nextModel0 with ExcelState = nextState}
            nextModel,nextCmd

        | SpreadsheetMsg msg ->
            let nextState, nextModel, nextCmd = Update.Spreadsheet.update currentModel.SpreadsheetModel currentModel msg
            let nextModel' = {nextModel with SpreadsheetModel = nextState}
            nextModel', nextCmd

        | InterfaceMsg msg ->
            Update.Interface.update currentModel msg

        | TermSearchMsg termSearchMsg ->
            let nextTermSearchState,nextCmd =
                currentModel.TermSearchState
                |> TermSearch.update termSearchMsg

            let nextModel = {
                currentModel with
                    TermSearchState = nextTermSearchState
            }
            nextModel,nextCmd
        | AdvancedSearchMsg msg ->
            let nextModel, cmd = AdvancedSearch.update msg model

            nextModel, cmd

        | DevMsg msg ->
            let nextDevState,nextCmd = currentModel.DevState |> Dev.update msg
        
            let nextModel = {
                currentModel with
                    DevState = nextDevState
            }
            nextModel,nextCmd

        | PersistentStorageMsg persistentStorageMsg ->
            let nextPersistentStorageState,nextCmd =
                currentModel.PersistentStorageState
                |> handlePersistenStorageMsg persistentStorageMsg

            let nextModel = {
                currentModel with
                    PersistentStorageState = nextPersistentStorageState
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
                |> BuildingBlock.Core.update addBuildingBlockMsg

            let nextModel = {
                currentModel with
                    AddBuildingBlockState = nextAddBuildingBlockState
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
                |> handleBuildingBlockDetailsMsg buildingBlockDetailsMsg

            let nextModel = {
                currentModel with
                    BuildingBlockDetailsState = nextState
                }
            nextModel, nextCmd

        //| SettingsXmlMsg msg ->
        //    let nextState, nextCmd =
        //        currentModel.SettingsXmlState
        //        |> SettingsXml.update msg
        //    let nextModel = {
        //        currentModel with
        //            SettingsXmlState = nextState
        //    }
        //    nextModel, nextCmd

        | CytoscapeMsg msg ->
            let nextState, nextModel0, nextCmd =
                Cytoscape.Update.update msg currentModel.CytoscapeModel currentModel 
            let nextModel =
                {nextModel0 with
                    CytoscapeModel = nextState}
            nextModel, nextCmd

    /// This function is used to determine which msg should be logged to activity log.
    /// The function is exception based, so msg which should not be logged needs to be added here.
    let matchMsgToLog (msg: Msg) =
        match msg with
        | DevMsg _ | UpdatePageState _ -> false
        | _ -> true

    let logg (msg:Msg) (model: Model) : Model =
        if matchMsgToLog msg then
            let l = 62
            let txt = $"{msg.ToString()}"
            let txt = if txt.Length > l then txt.Substring(0, l) +  ".." else txt
            let nextState = {
                model.DevState with
                    Log = (LogItem.ofStringNow "Info" txt)::model.DevState.Log
            }
            let nextModel = {
                model with DevState = nextState
            }
            nextModel
        else
            model

    logg msg model
    |> innerUpdate msg 