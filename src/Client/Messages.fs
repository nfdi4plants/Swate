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

type DevMsg =
    | LogTableMetadata
    | GenericLog            of Cmd<Messages.Msg> * (string*string)
    | GenericInteropLogs    of Cmd<Messages.Msg> * InteropLogging.Msg list
    | GenericError          of Cmd<Messages.Msg> * exn
    | UpdateDisplayLogList  of LogItem list
    
type ApiRequestMsg =
    | GetNewUnitTermSuggestions                 of string
    | FetchAllOntologies
    /// TermSearchable [] is created by officeInterop and passed to server for db search.
    | SearchForInsertTermsRequest              of TermSearchable []
    //
    | GetAppVersion

type ApiResponseMsg =
    | UnitTermSuggestionResponse                of Term []
    | FetchAllOntologiesResponse                of Ontology []
    | SearchForInsertTermsResponse              of TermSearchable []  
    //
    | GetAppVersionResponse                     of string

type ApiMsg =
    | Request    of ApiRequestMsg
    | Response   of ApiResponseMsg
    | ApiError   of exn
    | ApiSuccess of (string*string)

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
    // Below everything is more or less deprecated
    // Is still used for unit update in office
    | SearchUnitTermTextChange  of searchString:string
    | UnitTermSuggestionUsed    of unitTerm:Term
    | NewUnitTermSuggestions    of Term []

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
    ///Debouncing
    DebouncerState              : Debouncer.State
    ///Error handling, Logging, etc.
    DevState                    : DevState
    ///States regarding term search
    TermSearchState             : TermSearch.Model
    ///Use this in the future to model excel stuff like table data
    ExcelState                  : OfficeInterop.Model
    /// This should be removed. Overhead making maintainance more difficult
    /// "Use this to log Api calls and maybe handle them better"
    ApiState                    : ApiState
    ///States regarding File picker functionality
    FilePickerState             : FilePicker.Model
    ProtocolState               : Protocol.Model
    ///Insert annotation columns
    AddBuildingBlockState       : BuildingBlock.Model
    ///Create Validation scheme for Table
    ValidationState             : Validation.Model
    ///Used to show selected building block information
    BuildingBlockDetailsState   : BuildingBlockDetailsState
    ///Used to manage all custom xml settings
    SettingsXmlState            : SettingsXml.Model
    JsonExporterModel           : JsonExporter.Model
    DagModel                    : Dag.Model
    CytoscapeModel              : Cytoscape.Model
    /// Contains all information about spreadsheet view
    SpreadsheetModel            : Spreadsheet.Model
    History                     : LocalHistory.Model
} with
    member this.updateByExcelState (s:OfficeInterop.Model) =
        { this with ExcelState = s}
    member this.updateByJsonExporterModel (m:JsonExporter.Model) =
        { this with JsonExporterModel = m}
    member this.updateByDagModel (m:Dag.Model) =
        { this with DagModel = m}

type Msg =
| Bounce                of (System.TimeSpan*string*Msg)
| DebouncerSelfMsg      of Debouncer.SelfMessage<Msg>
| Api                   of ApiMsg
| DevMsg                of DevMsg
| TermSearchMsg         of TermSearch.Msg
| AdvancedSearchMsg     of AdvancedSearch.Msg
| OfficeInteropMsg      of OfficeInterop.Msg
| PersistentStorageMsg  of PersistentStorage.Msg
| FilePickerMsg         of FilePicker.Msg
| BuildingBlockMsg      of BuildingBlock.Msg
| ProtocolMsg           of Protocol.Msg
| JsonExporterMsg       of JsonExporter.Msg
| BuildingBlockDetails  of BuildingBlockDetailsMsg
| CytoscapeMsg          of Cytoscape.Msg
| SpreadsheetMsg        of Spreadsheet.Msg
| DagMsg                of Dag.Msg
/// This is used to forward Msg to SpreadsheetMsg/OfficeInterop
| InterfaceMsg          of SpreadsheetInterface.Msg
//| SettingsProtocolMsg   of SettingsProtocolMsg
| TopLevelMsg           of TopLevelMsg
| UpdatePageState       of Routing.Route option
| UpdateIsExpert        of bool
| Batch                 of seq<Messages.Msg>
| Run                   of (unit -> unit)
| UpdateHistory         of LocalHistory.Model
/// Top level msg to test specific api interactions, only for dev.
| TestMyAPI
| TestMyPostAPI
| DoNothing