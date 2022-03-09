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
        | ParseUploadedFileResponse buildingBlockTables ->
            let nextCmd =
                match Array.tryExactlyOne buildingBlockTables with
                | Some (_,buildingBlocks) ->
                    Cmd.OfPromise.either
                        OfficeInterop.addAnnotationBlocks
                        buildingBlocks
                        (curry GenericInteropLogs Cmd.none >> DevMsg)
                        (curry GenericError Cmd.none >> DevMsg)
                | None ->
                    Cmd.OfPromise.either
                        OfficeInterop.addAnnotationBlocksInNewSheets
                        buildingBlockTables
                        (curry GenericInteropLogs Cmd.none >> DevMsg)
                        (curry GenericError Cmd.none >> DevMsg)
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
                    Api.protocolApi.getAllProtocolsWithoutXml
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
                    Api.protocolApi.getProtocolById
                    templateId
                    (GetProtocolByIdResponse >> ProtocolMsg)
                    (curry GenericError Cmd.none >> DevMsg)
            currentState, cmd
        | GetProtocolByIdResponse prot -> 
            let nextState = {
                currentState with
                    ProtocolSelected = Some prot
                    //ValidationXml = Some validation
                    DisplayedProtDetailsId = None
            }
            nextState, Cmd.ofMsg (UpdatePageState <| Some Routing.Route.Protocol)
        | ProtocolIncreaseTimesUsed templateId ->
            let cmd =
                Cmd.OfAsync.attempt
                    Api.protocolApi.increaseTimesUsedById
                    templateId
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
                    ProtocolFilterTags      = tagStr::currentState.ProtocolFilterTags
                    ProtocolTagSearchQuery  = ""
            }
            nextState, Cmd.none
        | RemoveProtocolTag tagStr ->
            let nextState = {
                currentState with
                    ProtocolFilterTags = currentState.ProtocolFilterTags |> List.filter (fun x -> x <> tagStr)
            }
            nextState, Cmd.none
        | AddProtocolErTag tagStr ->
            let nextState = {
                currentState with
                    ProtocolFilterErTags      = tagStr::currentState.ProtocolFilterErTags
                    ProtocolTagSearchQuery  = ""
            }
            nextState, Cmd.none
        | RemoveProtocolErTag tagStr ->
            let nextState = {
                currentState with
                    ProtocolFilterErTags = currentState.ProtocolFilterErTags |> List.filter (fun x -> x <> tagStr)
            }
            nextState, Cmd.none
        | UpdateCuratedCommunityFilter nextFilter ->
            let nextState = {
                currentState with
                    CuratedCommunityFilter = nextFilter
            }
            nextState, Cmd.none
        | UpdateTagFilterIsAnd isAnd ->
            let nextState = {
                currentState with
                    TagFilterIsAnd = isAnd
            }
            nextState, Cmd.none
        | RemoveSelectedProtocol ->
            let nextState = {
                currentState with
                    ProtocolSelected = None
                    ValidationXml = None
            }
            nextState, Cmd.none
