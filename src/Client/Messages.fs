[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>] //will create build error without
module rec Messages

open Elmish
open Swate.Components.Shared
open Fable.Remoting.Client
open Fable.SimpleJson
open Database

open Model
open ARCtrl
open Routing
open Fable.Core

type System.Exception with
    member this.GetPropagatedError() =
        match this with
        | :? ProxyRequestException as exn ->
            try
                let response = exn.ResponseText |> Json.parseAs<{| error:string; ignored : bool; handled : bool |}>
                response.error
            with
                | ex -> ex.Message
        | ex ->
            ex.Message

let curry f a b = f (a, b)

module TermSearch =

    type Msg =
        | UpdateSelectedTerm    of OntologyAnnotation option
        | UpdateParentTerm      of OntologyAnnotation option


module AdvancedSearch =

    type Msg =
        | GetSearchResults of {| config:Swate.Components.Shared.DTOs.AdvancedSearchQuery; responseSetter: Term [] -> unit |}

type DevMsg =
    | LogTableMetadata
    | GenericLog            of Cmd<Messages.Msg> * (string*string)
    | GenericInteropLogs    of Cmd<Messages.Msg> * InteropLogging.Msg list
    | GenericError          of Cmd<Messages.Msg> * exn
    | UpdateDisplayLogList  of LogItem list

module PersistentStorage =
    type Msg =
    | UpdateAppVersion          of string
    | UpdateSwateDefaultSearch  of bool
    | AddTIBSearchCatalogue     of string
    | RemoveTIBSearchCatalogue  of string
    | SetTIBSearchCatalogues    of Set<string>
    | UpdateAutosave            of bool

module PageState =

    type Msg =
    | UpdateShowSidebar of bool
    | UpdateMainPage    of MainPage
    | UpdateSidebarPage of SidebarPage

module FilePicker =
    type Msg =
        | LoadNewFiles      of string list
        | UpdateFileNames   of newFileNames:(int*string) list

module BuildingBlock =

    open TermSearch

    type Msg =
    | UpdateHeaderWithIO    of CompositeHeaderDiscriminate * IOType
    | UpdateHeaderCellType  of CompositeHeaderDiscriminate
    | UpdateHeaderArg       of U2<OntologyAnnotation,IOType> option
    | UpdateBodyCellType    of CompositeCellDiscriminate
    | UpdateBodyArg         of U2<string, OntologyAnnotation> option
    | UpdateCommentHeader   of string

module Protocol =

    type Msg =
        // Client
        | UpdateTemplates               of Template []
        | UpdateLoading                 of bool
        | RemoveSelectedProtocols
        // // ------ Protocol from Database ------
        | GetAllProtocolsForceRequest
        | GetAllProtocolsRequest
        | GetAllProtocolsResponse       of string
        | SelectProtocols               of Template list
        | AddProtocol                   of Template
        | ProtocolIncreaseTimesUsed     of protocolName:string

type SettingsDataStewardMsg =
    // Client
    | UpdatePointerJson of string option

type TopLevelMsg =
    | CloseSuggestions

module History =

    type Msg =
    | UpdateAnd of LocalHistory.Model * Cmd<Messages.Msg>
    | UpdateHistoryPosition of int

type Msg =
| UpdateModel           of Model
| DevMsg                of DevMsg
| TermSearchMsg         of TermSearch.Msg
| AdvancedSearchMsg     of AdvancedSearch.Msg
| OfficeInteropMsg      of OfficeInterop.Msg
| PersistentStorageMsg  of PersistentStorage.Msg
| FilePickerMsg         of FilePicker.Msg
| BuildingBlockMsg      of BuildingBlock.Msg
| ProtocolMsg           of Protocol.Msg
| DataAnnotatorMsg      of DataAnnotator.Msg
| SpreadsheetMsg        of Spreadsheet.Msg
| ARCitectMsg           of ARCitect.Msg
/// This is used to forward Msg to SpreadsheetMsg/OfficeInterop
| InterfaceMsg          of SpreadsheetInterface.Msg
| PageStateMsg          of PageState.Msg
| Batch                 of seq<Messages.Msg>
| HistoryMsg            of History.Msg
| UpdateModal           of Model.ModalState.ModalTypes option
/// Top level msg to test specific api interactions, only for dev.
| Run                   of (unit -> unit)
| TestMyAPI
| TestMyPostAPI
| DoNothing