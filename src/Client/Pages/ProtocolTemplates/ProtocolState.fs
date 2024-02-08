namespace Messages

open Elmish
open Model
open Messages
open Protocol

module Protocol =

    open Shared
    open Fable.Core

    let update (fujMsg:Protocol.Msg) (state: Protocol.Model) : Protocol.Model * Cmd<Messages.Msg> =

        match fujMsg with
        | UpdateLoading next ->
            {state with Loading = next}, Cmd.none
        // // ------ Process from file ------
        | ParseUploadedFileRequest bytes ->
            failwith "ParseUploadedFileRequest IS NOT IMPLEMENTED YET"
            //let cmd =
            //    Cmd.OfAsync.either
            //        Api.templateApi.tryParseToBuildingBlocks
            //        bytes
            //        (ParseUploadedFileResponse >> ProtocolMsg)
            //        (curry GenericError (UpdateLoading false |> ProtocolMsg |> Cmd.ofMsg) >> DevMsg)
            state, Cmd.none
        | ParseUploadedFileResponse buildingBlockTables ->
            let nextState = { state with UploadedFileParsed = buildingBlockTables }
            nextState, Cmd.none
        // ------ Protocol from Database ------
        | GetAllProtocolsRequest ->
            let now = System.DateTime.UtcNow
            let olderThanOneHour = state.LastUpdated |> Option.map (fun last -> (now - last) > System.TimeSpan(1,0,0))
            let cmd = 
                if olderThanOneHour.IsNone || olderThanOneHour.Value then GetAllProtocolsForceRequest |> ProtocolMsg |> Cmd.ofMsg else Cmd.none
            state, cmd
        | GetAllProtocolsForceRequest ->
            let nextState = {state with Loading = true}
            let cmd =
                let updateRequestStateOnErrorCmd = UpdateLoading false |> ProtocolMsg |> Cmd.ofMsg
                Cmd.OfAsync.either
                    Api.templateApi.getTemplates
                    ()
                    (GetAllProtocolsResponse >> ProtocolMsg)
                    (curry GenericError updateRequestStateOnErrorCmd >> DevMsg)
            nextState, cmd
        | GetAllProtocolsResponse protocolsJson ->
            let state = {state with Loading = false}
            let templates = 
                try
                    protocolsJson |> Array.map (ARCtrl.Template.Json.Template.fromJsonString) |> Ok
                with
                    | e -> Result.Error e
            let nextState, cmd = 
                match templates with
                | Ok t -> 
                    let nextState = { state with LastUpdated = Some System.DateTime.UtcNow }
                    nextState, UpdateTemplates t |> ProtocolMsg |> Cmd.ofMsg
                | Result.Error e -> state, GenericError (Cmd.none,e) |> DevMsg |> Cmd.ofMsg
            nextState, cmd
        | UpdateTemplates templates ->
            let nextState = {
                state with
                    Templates = templates
            }
            nextState, Cmd.none
        | SelectProtocol prot -> 
            let nextState = {
                state with
                    TemplateSelected = Some prot
            }
            nextState, Cmd.ofMsg (UpdatePageState <| Some Routing.Route.Protocol)
        | ProtocolIncreaseTimesUsed templateId ->
            failwith "ParseUploadedFileRequest IS NOT IMPLEMENTED YET"
            //let cmd =
            //    Cmd.OfAsync.attempt
            //        Api.templateApi.increaseTimesUsedById
            //        templateId
            //        (curry GenericError Cmd.none >> DevMsg)
            state, Cmd.none
                
        // Client
        | RemoveSelectedProtocol ->
            let nextState = {
                state with
                    TemplateSelected = None
            }
            nextState, Cmd.none
        | RemoveUploadedFileParsed ->
            let nextState = {state with UploadedFileParsed = Array.empty}
            nextState, Cmd.none
