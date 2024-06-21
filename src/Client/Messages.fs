[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>] //will create build error without
module rec Messages

open Elmish
open Thoth.Elmish
open Shared
open Fable.Remoting.Client
open Fable.SimpleJson

open TermTypes
open ExcelColors
open OfficeInterop
open OfficeInteropTypes
open Model
open Routing
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

let curry f a b = f (a,b)

module TermSearch =

    type Msg =
        | UpdateSelectedTerm of OntologyAnnotation option 
        | UpdateParentTerm of OntologyAnnotation option


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

type StyleChangeMsg =
    | UpdateColorMode of ColorMode

module PersistentStorage =
    type Msg =
    | NewSearchableOntologies of Ontology []
    | UpdateAppVersion of string
    | UpdateShowSidebar of bool

module FilePicker =
    type Msg =
        | LoadNewFiles              of string list
        | UpdateFileNames           of newFileNames:(int*string) list

module BuildingBlock =

    open TermSearch

    type Msg =
    | UpdateHeaderWithIO of BuildingBlock.HeaderCellType * IOType
    | UpdateHeaderCellType of BuildingBlock.HeaderCellType
    | UpdateHeaderArg of U2<OntologyAnnotation,IOType> option
    | UpdateBodyCellType of BuildingBlock.BodyCellType
    | UpdateBodyArg of U2<string, OntologyAnnotation> option

module Protocol =

    type Msg =
        // // ------ Process from file ------
        | ParseUploadedFileRequest          of raw: byte []
        | ParseUploadedFileResponse         of (string * InsertBuildingBlock []) []
        // Client
        | UpdateTemplates                   of Template []
        | UpdateLoading                     of bool
        | RemoveUploadedFileParsed
        // // ------ Protocol from Database ------
        | GetAllProtocolsForceRequest
        | GetAllProtocolsRequest
        | GetAllProtocolsResponse           of string
        | SelectProtocol                    of Template
        | ProtocolIncreaseTimesUsed         of protocolName:string
        // Client
        | RemoveSelectedProtocol

type BuildingBlockDetailsMsg =
    | GetSelectedBuildingBlockTermsRequest      of TermSearchable []
    | GetSelectedBuildingBlockTermsResponse     of TermSearchable []
    | UpdateBuildingBlockValues                 of TermSearchable []
    | UpdateCurrentRequestState                 of RequestBuildingBlockInfoStates

type SettingsDataStewardMsg =
    // Client
    | UpdatePointerJson of string option

type TopLevelMsg =
    | CloseSuggestions

type Model = {
    ///PageState
    PageState                   : PageState
    ///Data that needs to be persistent once loaded
    PersistentStorageState      : PersistentStorageState
    ///Error handling, Logging, etc.
    DevState                    : DevState
    ///States regarding term search
    TermSearchState             : TermSearch.Model
    ///Use this in the future to model excel stuff like table data
    ExcelState                  : OfficeInterop.Model
    ///States regarding File picker functionality
    FilePickerState             : FilePicker.Model
    ProtocolState               : Protocol.Model
    ///Insert annotation columns
    AddBuildingBlockState       : BuildingBlock.Model
    ///Used to show selected building block information
    BuildingBlockDetailsState   : BuildingBlockDetailsState
    CytoscapeModel              : Cytoscape.Model
    /// Contains all information about spreadsheet view
    SpreadsheetModel            : Spreadsheet.Model
    History                     : LocalHistory.Model
} with
    member this.updateByExcelState (s:OfficeInterop.Model) =
        { this with ExcelState = s}

type Msg =
| DevMsg                of DevMsg
| OntologyMsg           of Ontologies.Msg
| TermSearchMsg         of TermSearch.Msg
| AdvancedSearchMsg     of AdvancedSearch.Msg
| OfficeInteropMsg      of OfficeInterop.Msg
| PersistentStorageMsg  of PersistentStorage.Msg
| FilePickerMsg         of FilePicker.Msg
| BuildingBlockMsg      of BuildingBlock.Msg
| ProtocolMsg           of Protocol.Msg
| BuildingBlockDetails  of BuildingBlockDetailsMsg
| CytoscapeMsg          of Cytoscape.Msg
| SpreadsheetMsg        of Spreadsheet.Msg
/// This is used to forward Msg to SpreadsheetMsg/OfficeInterop
| InterfaceMsg          of SpreadsheetInterface.Msg
//| SettingsProtocolMsg   of SettingsProtocolMsg
| UpdatePageState       of Routing.Route option
| UpdateIsExpert        of bool
| Batch                 of seq<Messages.Msg>
| Run                   of (unit -> unit)
| UpdateHistory         of LocalHistory.Model
/// Top level msg to test specific api interactions, only for dev.
| TestMyAPI
| TestMyPostAPI
| DoNothing