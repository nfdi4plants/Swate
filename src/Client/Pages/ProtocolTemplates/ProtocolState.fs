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
        | ParseUploadedFileRequest bytes ->
            let nextModel = { currentState with Loading = true }
            let cmd =
                Cmd.OfAsync.either
                    Api.templateApi.tryParseToBuildingBlocks
                    bytes
                    (ParseUploadedFileResponse >> ProtocolMsg)
                    (curry GenericError (UpdateLoading false |> ProtocolMsg |> Cmd.ofMsg) >> DevMsg)
            nextModel, cmd
        | ParseUploadedFileResponse buildingBlockTables ->
            let nextState = { currentState with UploadedFileParsed = buildingBlockTables; Loading = false }
            nextState, Cmd.none
        // ------ Protocol from Database ------
        | GetAllProtocolsRequest ->
            let nextState = {currentState with Loading = true}
            let cmd =
                Cmd.OfAsync.either
                    Api.templateApi.getAllTemplatesWithoutXml
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
        | GetProtocolByIdRequest templateId ->
            let cmd =
                Cmd.OfAsync.either
                    Api.templateApi.getTemplateById
                    templateId
                    (GetProtocolByIdResponse >> ProtocolMsg)
                    (curry GenericError Cmd.none >> DevMsg)
            currentState, cmd
        | GetProtocolByIdResponse prot -> 
            let nextState = {
                currentState with
                    ProtocolSelected = Some prot
                    //ValidationXml = Some validation
                    //DisplayedProtDetailsId = None
            }
            nextState, Cmd.ofMsg (UpdatePageState <| Some Routing.Route.Protocol)
        | ProtocolIncreaseTimesUsed templateId ->
            let cmd =
                Cmd.OfAsync.attempt
                    Api.templateApi.increaseTimesUsedById
                    templateId
                    (curry GenericError Cmd.none >> DevMsg)
            currentState, cmd
                
        // Client
        | UpdateLoading nextLoadingState ->
            let nextState = {
                currentState with Loading = nextLoadingState
            }
            nextState, Cmd.none
        | RemoveSelectedProtocol ->
            let nextState = {
                currentState with
                    ProtocolSelected = None
                    ValidationXml = None
            }
            nextState, Cmd.none
        | RemoveUploadedFileParsed ->
            let nextState = {currentState with UploadedFileParsed = Array.empty}
            nextState, Cmd.none
