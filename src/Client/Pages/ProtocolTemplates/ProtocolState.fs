namespace Messages

open Elmish
open Model
open Messages
open Protocol

module Protocol =

    open Swate.Components.Shared
    open Fable.Core

    let update
        (msg: Protocol.Msg)
        (state: Protocol.Model)
        (model: Model.Model)
        : Protocol.Model * Model.Model * Cmd<Messages.Msg> =

        match msg with
        | UpdateLoading next -> { state with Loading = next }, model, Cmd.none
        | UpdateShowSearch next -> { state with ShowSearch = next }, model, Cmd.none
        | UpdateImportConfig next -> { state with ImportConfig = next }, model, Cmd.none
        // ------ Protocol from Database ------
        | GetAllProtocolsRequest ->
            let now = System.DateTime.UtcNow

            let olderThanOneHour =
                state.LastUpdated
                |> Option.map (fun last -> (now - last) > System.TimeSpan(1, 0, 0))

            let cmd =
                if olderThanOneHour.IsNone || olderThanOneHour.Value then
                    GetAllProtocolsForceRequest |> ProtocolMsg |> Cmd.ofMsg
                else
                    Cmd.none

            state, model, cmd
        | GetAllProtocolsForceRequest ->
            let nextState = { state with Loading = true }

            let cmd =
                let updateRequestStateOnErrorCmd = UpdateLoading false |> ProtocolMsg |> Cmd.ofMsg

                Cmd.OfAsync.either
                    Api.templateApi.getTemplates
                    ()
                    (GetAllProtocolsResponse >> ProtocolMsg)
                    (curry GenericError updateRequestStateOnErrorCmd >> DevMsg)

            nextState, model, cmd
        | GetAllProtocolsResponse protocolsJson ->
            let state = { state with Loading = false }

            let templates =
                try
                    protocolsJson |> ARCtrl.Json.Templates.fromJsonString |> Ok
                with e ->
                    Result.Error e

            let nextState, cmd =
                match templates with
                | Ok t0 ->
                    let t = Array.ofSeq t0

                    let nextState = {
                        state with
                            LastUpdated = Some System.DateTime.UtcNow
                    }

                    nextState, UpdateTemplates t |> ProtocolMsg |> Cmd.ofMsg
                | Result.Error e -> state, GenericError(Cmd.none, e) |> DevMsg |> Cmd.ofMsg

            nextState, model, cmd
        | UpdateTemplates templates ->
            let nextState = { state with Templates = templates }
            nextState, model, Cmd.none
        | SelectProtocols prots ->
            let importArr: Types.FileImport.ImportTable list =
                List.init prots.Length (fun i -> { Index = i; FullImport = false })

            let nextState = {
                state with
                    TemplatesSelected = prots //
                    ShowSearch = false // Switch to config view
                    ImportConfig = { // default append all selected templates
                        Types.FileImport.SelectiveImportConfig.init () with
                            ImportTables = importArr
                    }
            }

            nextState, model, Cmd.none
        | AddProtocol prot ->
            let templates =
                if List.contains prot model.ProtocolState.TemplatesSelected then
                    model.ProtocolState.TemplatesSelected
                else
                    prot :: model.ProtocolState.TemplatesSelected

            let nextState = {
                state with
                    TemplatesSelected = templates
            }

            nextState, model, Cmd.none
        | ProtocolIncreaseTimesUsed templateId ->
            failwith "ParseUploadedFileRequest IS NOT IMPLEMENTED YET"
            //let cmd =
            //    Cmd.OfAsync.attempt
            //        Api.templateApi.increaseTimesUsedById
            //        templateId
            //        (curry GenericError Cmd.none >> DevMsg)
            state, model, Cmd.none

        // Client
        | RemoveSelectedProtocols ->
            let nextState = { state with TemplatesSelected = [] }
            nextState, model, Cmd.none