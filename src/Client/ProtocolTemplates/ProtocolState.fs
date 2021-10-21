namespace Messages

open Elmish
open Model
open Messages
open Protocol

module Protocol =

    open Shared

    let update (fujMsg:Protocol.Msg) (currentState: Protocol.Model) : Protocol.Model * Cmd<Messages.Msg> =

        match fujMsg with
        // // ------ Process from file ------
        | ParseUploadedFileRequest ->
            let nextModel = { currentState with Loading = true }
            let api =
                match currentState.JsonExportType with
                | JsonExportType.ProcessSeq ->
                    Api.swateJsonAPIv1.parseProcessSeqToBuildingBlocks
                | JsonExportType.Assay ->
                    Api.swateJsonAPIv1.parseAssayJsonToBuildingBlocks
                | JsonExportType.Table ->
                    Api.swateJsonAPIv1.parseTableJsonToBuildingBlocks
                | anythingElse -> failwith $"Cannot parse \"{anythingElse.ToString()}\" with this endpoint."
            let cmd =
                Cmd.OfAsync.either
                    api
                    currentState.UploadedFile
                    (ParseUploadedFileResponse >> ProtocolMsg)
                    (curry GenericError (UpdateLoading false |> ProtocolMsg |> Cmd.ofMsg) >> DevMsg)
            nextModel, cmd
        | ParseUploadedFileResponse buildingBlocksWithValue ->
            let nextCmd =
                match Array.tryExactlyOne buildingBlocksWithValue with
                | Some (_,buildingBlocksWithValue) -> Cmd.none
                | None -> Cmd.none
            currentState, nextCmd
        // Client
        | UpdateJsonExportType nextType ->
            let nextModel = {
                currentState with
                    ShowJsonTypeDropdown    = false
                    JsonExportType          = nextType
            }
            nextModel, Cmd.none
        | UpdateUploadFile nextFileStr ->
            let nextModel = {
                currentState with
                    UploadedFile = nextFileStr
            }
            nextModel, Cmd.none
        | UpdateShowJsonTypeDropdown show ->
            let nextModel = {
                currentState with
                    ShowJsonTypeDropdown = show
            }
            nextModel, Cmd.none 
        // ------ Protocol from Database ------
        | GetAllProtocolsRequest ->
            let nextState = {currentState with Loading = true}
            let cmd =
                Cmd.OfAsync.either
                    Api.api.getAllProtocolsWithoutXml
                    ()
                    (GetAllProtocolsResponse >> ProtocolMsg)
                    (curry GenericError Cmd.none >> DevMsg)
            nextState, cmd
        | GetAllProtocolsResponse protocols ->
            let nextState = {
                currentState with
                    ProtocolsAll = protocols
                    Loading = false
            }
            nextState, Cmd.none
        | GetProtocolByNameRequest protocolName ->
            let cmd =
                Cmd.OfAsync.either
                    Api.api.getProtocolByName
                    protocolName
                    (GetProtocolByNameResponse >> ProtocolMsg)
                    (curry GenericError Cmd.none >> DevMsg)
            currentState, cmd
        | GetProtocolByNameResponse prot -> 
            let nextState = {
                currentState with
                    ProtocolSelected = Some prot
                    //ValidationXml = Some validation
                    DisplayedProtDetailsId = None
            }
            nextState, Cmd.ofMsg (UpdatePageState <| Some Routing.Route.Protocol)
        | ProtocolIncreaseTimesUsed protocolTemplateName ->
            let cmd =
                Cmd.OfAsync.attempt
                    Api.api.increaseTimesUsed
                    protocolTemplateName
                    (curry GenericError Cmd.none >> DevMsg)
            currentState, cmd
                
        // Client
        | UpdateLoading nextLoadingState ->
            let nextState = {
                currentState with Loading = nextLoadingState
            }
            nextState, Cmd.none
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
                    ProtocolSearchTags = currentState.ProtocolSearchTags |> List.filter (fun x -> x <> tagStr)
            }
            nextState, Cmd.none
        | RemoveSelectedProtocol ->
            let nextState = {
                currentState with
                    ProtocolSelected = None
                    ValidationXml = None
            }
            nextState, Cmd.none
