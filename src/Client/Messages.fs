[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>] //will create build error without
module rec Messages

open Elmish
open Shared
open Fable.Remoting.Client
open Fable.SimpleJson
open Database

open Model
open ARCtrl
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
        | GetSearchResults of {| config:AdvancedSearchTypes.AdvancedSearchOptions; responseSetter: Term [] -> unit |}

module Ontologies =

   type Msg =
    | GetOntologies

type DevMsg =
    | LogTableMetadata
    | GenericLog            of Cmd<Messages.Msg> * (string*string)
    | GenericInteropLogs    of Cmd<Messages.Msg> * InteropLogging.Msg list
    | GenericError          of Cmd<Messages.Msg> * exn
    | UpdateDisplayLogList  of LogItem list

module PersistentStorage =
    type Msg =
    | NewSearchableOntologies   of Ontology []
    | UpdateAppVersion          of string
    | UpdateShowSidebar         of bool

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

type Msg =
| UpdateModel           of Model
| DevMsg                of DevMsg
| OntologyMsg           of Ontologies.Msg
| TermSearchMsg         of TermSearch.Msg
| AdvancedSearchMsg     of AdvancedSearch.Msg
| OfficeInteropMsg      of OfficeInterop.Msg
| PersistentStorageMsg  of PersistentStorage.Msg
| FilePickerMsg         of FilePicker.Msg
| BuildingBlockMsg      of BuildingBlock.Msg
| ProtocolMsg           of Protocol.Msg
// | CytoscapeMsg                  of Cytoscape.Msg
| DataAnnotatorMsg      of DataAnnotator.Msg
| SpreadsheetMsg        of Spreadsheet.Msg
/// This is used to forward Msg to SpreadsheetMsg/OfficeInterop
| InterfaceMsg          of SpreadsheetInterface.Msg
| Batch                 of seq<Messages.Msg>
| Run                   of (unit -> unit)
| UpdateHistory         of LocalHistory.Model
/// Top level msg to test specific api interactions, only for dev.
| TestMyAPI
| TestMyPostAPI
| UpdateModal           of Model.ModalState.ModalTypes option
| DoNothing