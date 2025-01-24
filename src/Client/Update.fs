[<AutoOpen>]
module Update.Update

open Elmish

open Shared
open Database
open Routing
open Messages
open Model

let urlUpdate (route: Route option) (model:Model) : Model * Cmd<Messages.Msg> =
    let cmd (host: Swatehost) = SpreadsheetInterface.Initialize host |> InterfaceMsg |> Cmd.ofMsg
    let host =
        match route with
        | Some (Routing.Route.Home queryIntegerOption) ->
            Swatehost.ofQueryParam queryIntegerOption
        | None ->
            Swatehost.Browser
    {model with Model.PersistentStorageState.Host = Some host}, cmd host

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
                if List.isEmpty parsedDisplayLogs |> not then
                    Model.ModalState.ExcelModals.InteropLogging |> Model.ModalState.ModalTypes.ExcelModal |> Some |> Messages.UpdateModal |> Cmd.ofMsg
                nextCmd
            ]
            nextState, batch

        | GenericError (nextCmd, e) ->
            let nextState = {
                currentState with
                    Log = LogItem.Error(System.DateTime.Now,e.GetPropagatedError())::currentState.Log
                }
            let batch = Cmd.batch [
                Model.ModalState.GeneralModals.Error e |> Model.ModalState.ModalTypes.GeneralModal |> Some |> Messages.UpdateModal |> Cmd.ofMsg
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
                    OfficeInterop.Core.Main.getTableMetaData
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

module DataAnnotator =
    let update (msg: DataAnnotator.Msg) (state: DataAnnotator.Model) (model: Model.Model) =
        match msg with
        | DataAnnotator.UpdateDataFile dataFile ->
            let parsedFile = dataFile |> Option.map (fun file ->
                let s = file.ExpectedSeparator
                DataAnnotator.ParsedDataFile.fromFileBySeparator s file
            )
            let nextState: DataAnnotator.Model = {
                DataFile = dataFile
                ParsedFile = parsedFile
            }
            nextState, model, Cmd.none
        | DataAnnotator.ToggleHeader ->
            let nextState =
                { state with ParsedFile = state.ParsedFile |> Option.map (fun file -> file.ToggleHeader())}
            nextState, model, Cmd.none
        | DataAnnotator.UpdateSeperator newSep ->
            let parsedFile = state.DataFile |> Option.map (fun file ->
                DataAnnotator.ParsedDataFile.fromFileBySeparator newSep file
            )
            let nextState = {
                state with
                    ParsedFile = parsedFile
            }
            nextState, model, Cmd.none

let update (msg : Msg) (model : Model) : Model * Cmd<Msg> =
    let innerUpdate (msg: Msg) (currentModel: Model) =
        match msg with
        | DoNothing -> currentModel,Cmd.none
        | Run callback ->
            callback()
            model, Cmd.none
        | UpdateModal modal ->
            {model with Model.ModalState.ActiveModal = modal}, Cmd.none
        | UpdateHistory next -> {model with History = next}, Cmd.none
        | UpdateHistoryAnd (newHistory, cmd) -> {model with History = newHistory}, cmd
        | UpdateState nextState -> {model with SpreadsheetModel = nextState}, Cmd.none
        | UpdateHistoryPosition newPosition ->
            let cmd =
                Cmd.OfPromise.either
                    LocalHistory.Model.fromIndexedDBByKeyPosition
                    (newPosition)
                    (fun nextState -> Messages.UpdateState nextState)
                    (curry GenericError Cmd.none >> DevMsg)
            {model with History.HistoryCurrentPosition = newPosition}, cmd
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
        | UpdateModel nextModel ->
            nextModel, Cmd.none

        | OntologyMsg msg ->
            let nextModel, cmd = Ontologies.update msg model
            nextModel, cmd

        | OfficeInteropMsg excelMsg ->
            let nextState,nextModel0, nextCmd = Update.OfficeInterop.update model.ExcelState currentModel excelMsg
            let nextModel = {nextModel0 with ExcelState = nextState}
            nextModel, nextCmd

        | SpreadsheetMsg msg ->
            let nextState, nextModel, nextCmd = Update.Spreadsheet.update currentModel.SpreadsheetModel currentModel msg
            let nextModel' = {nextModel with SpreadsheetModel = nextState}
            nextModel', nextCmd

        | InterfaceMsg msg ->
            Update.Interface.update currentModel msg

        | TermSearchMsg termSearchMsg ->
            let nextTermSearchState, nextCmd =
                currentModel.TermSearchState
                |> TermSearch.update termSearchMsg

            let nextModel = {
                currentModel with
                    TermSearchState = nextTermSearchState
            }
            nextModel, nextCmd
        | AdvancedSearchMsg msg ->
            let nextModel, cmd = AdvancedSearch.update msg model

            nextModel, cmd

        | DevMsg msg ->
            let nextDevState, nextCmd = currentModel.DevState |> Dev.update msg

            let nextModel = {
                currentModel with
                    DevState = nextDevState
            }
            nextModel, nextCmd

        | PersistentStorageMsg persistentStorageMsg ->
            let nextPersistentStorageState, nextCmd =
                currentModel.PersistentStorageState
                |> handlePersistenStorageMsg persistentStorageMsg

            let nextModel = {
                currentModel with
                    PersistentStorageState = nextPersistentStorageState
            }

            nextModel, nextCmd

        | FilePickerMsg filePickerMsg ->
            let nextFilePickerState, nextCmd =
                FilePicker.update filePickerMsg currentModel.FilePickerState model

            let nextModel = {
                currentModel with
                    FilePickerState = nextFilePickerState
            }

            nextModel, nextCmd

        | BuildingBlockMsg addBuildingBlockMsg ->
            let nextAddBuildingBlockState, nextCmd =
                currentModel.AddBuildingBlockState
                |> BuildingBlock.Core.update addBuildingBlockMsg

            let nextModel = {
                currentModel with
                    AddBuildingBlockState = nextAddBuildingBlockState
                }
            nextModel, nextCmd

        | ProtocolMsg fileUploadJsonMsg ->
            let nextFileUploadJsonState, nextCmd =
                Protocol.update fileUploadJsonMsg currentModel.ProtocolState model

            let nextModel = {
                currentModel with
                    ProtocolState = nextFileUploadJsonState
                }
            nextModel, nextCmd

        // | CytoscapeMsg msg ->
        //     let nextState, nextModel0, nextCmd =
        //         Cytoscape.Update.update msg currentModel.CytoscapeModel currentModel
        //     let nextModel =
        //         {nextModel0 with
        //             CytoscapeModel = nextState}
        //     nextModel, nextCmd

        | DataAnnotatorMsg msg ->
            let nextState, nextModel0, nextCmd = DataAnnotator.update msg currentModel.DataAnnotatorModel currentModel
            let nextModel = {nextModel0 with DataAnnotatorModel = nextState}
            nextModel, nextCmd

    /// This function is used to determine which msg should be logged to activity log.
    /// The function is exception based, so msg which should not be logged needs to be added here.
    let matchMsgToLog (msg: Msg) =
        match msg with
        | DevMsg _ | UpdateModel _ -> false
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
