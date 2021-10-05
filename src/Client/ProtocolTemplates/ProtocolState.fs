namespace Messages

open Elmish
open Model
open Messages
open Protocol

module Protocol =

    let update (fujMsg:Protocol.Msg) (currentState: Protocol.Model) : Protocol.Model * Cmd<Messages.Msg> =

        //let parseDBProtocol (prot:Shared.ProtocolTemplate) =
        //    let tableName,minBBInfos = prot.TableXml |> OfficeInterop.Regex.MinimalBuildingBlock.ofExcelTableXml
        //    let validationType =  prot.CustomXml |> TableValidation.ofXml
        //    if tableName <> validationType.AnnotationTable.Name then failwith "CustomXml and TableXml relate to different tables."
        //    prot, validationType, minBBInfos

        match fujMsg with
        //| ParseJsonToProcessRequest parsableString ->
        //    let cmd =
        //        Cmd.OfAsync.either
        //            Api.isaDotNetApi.parseJsonToProcess
        //            parsableString
        //            (Ok >> ParseJsonToProcessResult)
        //            (Result.Error >> ParseJsonToProcessResult)
        //    currentState, Cmd.map ProtocolInsert cmd 
        //| ParseJsonToProcessResult (Ok isaProcess) ->
        //    let nextState = {
        //        currentState with
        //            ProcessModel = Some isaProcess
        //    }
        //    nextState, Cmd.none
        //| ParseJsonToProcessResult (Result.Error e) ->
        //    let cmd =
        //        GenericError e |> Dev |> Cmd.ofMsg 
        //    currentState, cmd
        //| RemoveProcessFromModel ->
        //    let nextState = {
        //        currentState with
        //            ProcessModel = None
        //            UploadData = ""
        //    }
        //    nextState, Cmd.none
        | GetAllProtocolsRequest ->
            let nextState = {currentState with Loading = true}
            let cmd =
                Cmd.OfAsync.either
                    Api.api.getAllProtocolsWithoutXml
                    ()
                    (GetAllProtocolsResponse >> ProtocolMsg)
                    (GenericError >> Dev)
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
                    (GenericError >> Dev)
            currentState, cmd
        | GetProtocolByNameResponse (prot) -> 
            let nextState = {
                currentState with
                    ProtocolSelected = Some prot
                    //BuildingBlockMinInfoList = minBBInfoList
                    //ValidationXml = Some validation
                    DisplayedProtDetailsId = None
            }
            nextState, Cmd.ofMsg (UpdatePageState <| Some Routing.Route.Protocol)
        | ProtocolIncreaseTimesUsed protocolTemplateName ->
            let cmd =
                Cmd.OfAsync.attempt
                    Api.api.increaseTimesUsed
                    protocolTemplateName
                    (GenericError >> Dev)
            currentState, cmd
                
        // Client
        | UpdateLoading nextLoadingState ->
            let nextState = {
                currentState with Loading = nextLoadingState
            }
            nextState, Cmd.none
        //| UpdateUploadData newDataString ->
        //    let nextState = {
        //        currentState with
        //            UploadData = newDataString
        //    }
        //    nextState, Cmd.ofMsg (ParseJsonToProcessRequest newDataString |> ProtocolInsert)
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
