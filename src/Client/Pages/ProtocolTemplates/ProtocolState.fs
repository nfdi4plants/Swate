namespace Messages

open Elmish
open Model
open Messages
open Protocol

module Protocol =

    open Shared
    open Fable.Core

    let update (fujMsg:Protocol.Msg) (currentState: Protocol.Model) : Protocol.Model * Cmd<Messages.Msg> =

        match fujMsg with
        // // ------ Process from file ------
        | ParseUploadedFileRequest bytes ->
            let nextModel = { currentState with Loading = true }
            failwith "ParseUploadedFileRequest IS NOT IMPLEMENTED YET"
            //let cmd =
            //    Cmd.OfAsync.either
            //        Api.templateApi.tryParseToBuildingBlocks
            //        bytes
            //        (ParseUploadedFileResponse >> ProtocolMsg)
            //        (curry GenericError (UpdateLoading false |> ProtocolMsg |> Cmd.ofMsg) >> DevMsg)
            nextModel, Cmd.none
        | ParseUploadedFileResponse buildingBlockTables ->
            let nextState = { currentState with UploadedFileParsed = buildingBlockTables; Loading = false }
            nextState, Cmd.none
        // ------ Protocol from Database ------
        | GetAllProtocolsRequest ->
            let nextState = {currentState with Loading = true}
            let cmd =
                Cmd.OfAsync.either
                    Api.templateApi.getTemplates
                    ()
                    (GetAllProtocolsResponse >> ProtocolMsg)
                    (curry GenericError Cmd.none >> DevMsg)
            nextState, cmd
        | GetAllProtocolsResponse protocolsJson ->
            let protocols = protocolsJson |> Array.map (ARCtrl.Template.Json.Template.fromJsonString)
            let nextState = {
                currentState with
                    ProtocolsAll = protocols
                    Loading = false
            }
            nextState, Cmd.none
        | SelectProtocol prot -> 
            let nextState = {
                currentState with
                    ProtocolSelected = Some prot
            }
            nextState, Cmd.ofMsg (UpdatePageState <| Some Routing.Route.Protocol)
        | ProtocolIncreaseTimesUsed templateId ->
            failwith "ParseUploadedFileRequest IS NOT IMPLEMENTED YET"
            //let cmd =
            //    Cmd.OfAsync.attempt
            //        Api.templateApi.increaseTimesUsedById
            //        templateId
            //        (curry GenericError Cmd.none >> DevMsg)
            currentState, Cmd.none
                
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
            }
            nextState, Cmd.none
        | RemoveUploadedFileParsed ->
            let nextState = {currentState with UploadedFileParsed = Array.empty}
            nextState, Cmd.none
