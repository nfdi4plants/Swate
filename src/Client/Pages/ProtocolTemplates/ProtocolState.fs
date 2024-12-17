namespace Messages

open Elmish
open Model
open Messages
open Protocol

module Protocol =

    open Shared
    open Fable.Core

    let update (fujMsg:Protocol.Msg) (state: Protocol.Model) (model: Model.Model) : Protocol.Model * Cmd<Messages.Msg> =

        match fujMsg with
        | UpdateLoading next ->
            {state with Loading = next}, Cmd.none
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
                    protocolsJson |> ARCtrl.Json.Templates.fromJsonString |> Ok
                with
                    | e -> Result.Error e
            let nextState, cmd =
                match templates with
                | Ok t0 ->
                    let t = Array.ofSeq t0
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
        | SelectProtocols prots ->
            log "SelectProtocols"
            let nextModel = {
                model with
                    Model.ProtocolState.TemplatesSelected = prots
                    Model.PageState.SidebarPage = Routing.SidebarPage.Protocol
            }
            state, Cmd.ofMsg (UpdateModel nextModel)
        | AddProtocol prot ->
            log "AddProtocol"
            let templates =
                if List.contains prot model.ProtocolState.TemplatesSelected then
                    model.ProtocolState.TemplatesSelected
                else
                    prot::model.ProtocolState.TemplatesSelected
            let nextState = {
                state with
                    TemplatesSelected = templates
            }
            nextState, Cmd.none
        | ProtocolIncreaseTimesUsed templateId ->
            failwith "ParseUploadedFileRequest IS NOT IMPLEMENTED YET"
            //let cmd =
            //    Cmd.OfAsync.attempt
            //        Api.templateApi.increaseTimesUsedById
            //        templateId
            //        (curry GenericError Cmd.none >> DevMsg)
            state, Cmd.none

        // Client
        | RemoveSelectedProtocols ->
            let nextState = {
                state with
                    TemplatesSelected   = []
            }
            nextState, Cmd.none
