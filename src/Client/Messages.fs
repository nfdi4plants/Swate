module rec Messages

open Elmish
open Thoth.Elmish
open Shared
open Fable.Remoting.Client
open Fable.SimpleJson

open TermTypes
open ProtocolTemplateTypes
open ExcelColors
open OfficeInterop
open OfficeInteropTypes
open Model
open Routing

type System.Exception with
    member this.GetPropagatedError() =
        match this with
        | :? ProxyRequestException as exn ->
            let response = exn.ResponseText |> Json.parseAs<{| error:string; ignored : bool; handled : bool |}>
            response.error
        | ex ->
            ex.Message

let curry f a b = f (a,b)

module TermSearch =

    type Msg =
        | ToggleSearchByParentOntology
        | SearchTermTextChange                  of string
        | TermSuggestionUsed                    of DbDomain.Term
        | NewSuggestions                        of DbDomain.Term []
        | StoreParentOntologyFromOfficeInterop  of TermMinimal option
        // Server
        | GetAllTermsByParentTermRequest        of TermMinimal 
        | GetAllTermsByParentTermResponse       of DbDomain.Term []

module AdvancedSearch =

    type Msg =
        // Client
        | ResetAdvancedSearchState
        | ResetAdvancedSearchOptions
        | UpdateAdvancedTermSearchSubpage   of AdvancedSearch.AdvancedSearchSubpages
        | ToggleModal                       of string
        | ToggleOntologyDropdown
        | UpdateAdvancedTermSearchOptions   of AdvancedSearch.AdvancedSearchOptions
        | OntologySuggestionUsed            of DbDomain.Ontology option
        | ChangePageinationIndex            of int
        // Server
        /// Main function. Forward request to Request Api -> Server.
        | StartAdvancedSearch
        | NewAdvancedSearchResults          of DbDomain.Term []

type DevMsg =
    | LogTableMetadata
    | GenericLog            of Cmd<Messages.Msg> * (string*string)
    | GenericInteropLogs    of Cmd<Messages.Msg> * InteropLogging.Msg list
    | GenericError          of Cmd<Messages.Msg> * exn
    | UpdateLastFullError   of exn option
    
type ApiRequestMsg =
    | TestOntologyInsert                        of (string*string*System.DateTime*string)
    | GetNewTermSuggestions                     of string
    | GetNewTermSuggestionsByParentTerm         of string*TermMinimal
    | GetNewBuildingBlockNameSuggestions        of string
    | GetNewUnitTermSuggestions                 of string*relatedUnitSearch:UnitSearchRequest
    | GetNewAdvancedTermSearchResults           of AdvancedSearch.AdvancedSearchOptions
    | FetchAllOntologies
    /// TermSearchable [] is created by officeInterop and passed to server for db search.
    | SearchForInsertTermsRequest              of TermSearchable []
    //
    | GetAppVersion

type ApiResponseMsg =
    | TermSuggestionResponse                    of DbDomain.Term []
    | AdvancedTermSearchResultsResponse         of DbDomain.Term []
    | BuildingBlockNameSuggestionsResponse      of DbDomain.Term []
    | UnitTermSuggestionResponse                of DbDomain.Term [] * relatedUnitSearch:UnitSearchRequest
    | FetchAllOntologiesResponse                of DbDomain.Ontology []
    | SearchForInsertTermsResponse              of TermSearchable []  
    //
    | GetAppVersionResponse                     of string

type ApiMsg =
    | Request    of ApiRequestMsg
    | Response   of ApiResponseMsg
    | ApiError   of exn
    | ApiSuccess of (string*string)

type StyleChangeMsg =
    | ToggleBurger
    | ToggleQuickAcessIconsShown
    | ToggleColorMode

type PersistentStorageMsg =
    | NewSearchableOntologies of DbDomain.Ontology []
    | UpdateAppVersion of string

module FilePicker =
    type Msg =
        | LoadNewFiles              of string list
        | UpdateFileNames           of newFileNames:(int*string) list

module BuildingBlock =

    type Msg =
    | NewBuildingBlockSelected  of BuildingBlockNamePrePrint
    | BuildingBlockNameChange   of string
    | ToggleSelectionDropdown

    | BuildingBlockNameSuggestionUsed   of DbDomain.Term
    | NewBuildingBlockNameSuggestions   of DbDomain.Term []

    | SearchUnitTermTextChange  of searchString:string * relatedUnitSearch:UnitSearchRequest
    | UnitTermSuggestionUsed    of unitTerm:DbDomain.Term* relatedUnitSearch:UnitSearchRequest
    | NewUnitTermSuggestions    of DbDomain.Term [] * relatedUnitSearch:UnitSearchRequest
    | ToggleBuildingBlockHasUnit

module Validation =

    type Msg =
        // Client
        | UpdateDisplayedOptionsId of int option
        | UpdateTableValidationScheme of CustomXmlTypes.Validation.TableValidation
        // OfficeInterop
        | StoreTableRepresentationFromOfficeInterop of OfficeInterop.CustomXmlTypes.Validation.TableValidation * buildingBlocks:BuildingBlock []

module Protocol =

    type Msg =
        // // ------ Process from file ------
        | ParseUploadedFileRequest
        | ParseUploadedFileResponse         of (string * InsertBuildingBlock []) []
        // Client
        /// Update JsonExportType which defines the type of json which is supposedly uploaded. Determines function which will be used for parsing.
        | UpdateJsonExportType              of Shared.JsonExportType
        | UpdateUploadFile                  of jsonString:string
        | UpdateShowJsonTypeDropdown        of bool
        // // ------ Protocol from Database ------
        | GetAllProtocolsRequest
        | GetAllProtocolsResponse           of ProtocolTemplate []
        | GetProtocolByNameRequest          of string
        | GetProtocolByNameResponse         of ProtocolTemplate
        | ProtocolIncreaseTimesUsed         of protocolName:string
        // Client
        | UpdateDisplayedProtDetailsId      of int option
        | UpdateProtocolNameSearchQuery     of string
        | UpdateProtocolTagSearchQuery      of string
        | AddProtocolTag                    of string
        | RemoveProtocolTag                 of string
        | RemoveSelectedProtocol
        | UpdateLoading                     of bool

type BuildingBlockDetailsMsg =
    | GetSelectedBuildingBlockTermsRequest      of TermSearchable []
    | GetSelectedBuildingBlockTermsResponse     of TermSearchable []
    | ToggleShowDetails
    | UpdateCurrentRequestState                 of RequestBuildingBlockInfoStates

module SettingsXml =
    type Msg =
    //    // // Client // //
    | UpdateRawCustomXml                            of string option
    | UpdateNextRawCustomXml                        of string option

type SettingsDataStewardMsg =
    // Client
    | UpdatePointerJson of string option


type TopLevelMsg =
    | CloseSuggestions

type Model = {

    ///PageState
    PageState               : PageState

    ///Data that needs to be persistent once loaded
    PersistentStorageState  : PersistentStorageState
 
    ///Debouncing
    DebouncerState          : Debouncer.State

    ///Error handling, Logging, etc.
    DevState                : DevState

    ///Site Meta Options (Styling etc)
    SiteStyleState          : SiteStyleState

    ///States regarding term search
    TermSearchState         : TermSearch.Model

    AdvancedSearchState     : AdvancedSearch.Model

    ///Use this in the future to model excel stuff like table data
    ExcelState              : OfficeInterop.Model

    ///Use this to log Api calls and maybe handle them better
    ApiState                : ApiState

    ///States regarding File picker functionality
    FilePickerState         : FilePicker.Model

    ProtocolState           : Protocol.Model

    ///Insert annotation columns
    AddBuildingBlockState   : BuildingBlock.Model

    ///Create Validation scheme for Table
    ValidationState         : Validation.Model

    ///Used to show selected building block information
    BuildingBlockDetailsState   : BuildingBlockDetailsState

    ///Used to manage all custom xml settings
    SettingsXmlState            : SettingsXml.Model

    JsonExporterModel           : JsonExporter.Model

    TemplateMetadataModel       : TemplateMetadata.Model

    ///Used to manage functions specifically for data stewards
    SettingsDataStewardState    : SettingsDataStewardState

    WarningModal                : {|NextMsg:Msg; ModalMessage: string|} option
} with
    member this.updateByExcelState (s:OfficeInterop.Model) =
        { this with ExcelState = s}
    member this.updateByJsonExporterModel (m:JsonExporter.Model) =
        { this with JsonExporterModel = m}
    member this.updateByTemplateMetadataModel (m:TemplateMetadata.Model) =
        { this with TemplateMetadataModel = m}

type Msg =
| Bounce                of (System.TimeSpan*string*Msg)
| DebouncerSelfMsg      of Debouncer.SelfMessage<Msg>
| Api                   of ApiMsg
| DevMsg                of DevMsg
| TermSearchMsg         of TermSearch.Msg
| AdvancedSearchMsg     of AdvancedSearch.Msg
| OfficeInteropMsg      of OfficeInterop.Msg
| StyleChange           of StyleChangeMsg
| PersistentStorage     of PersistentStorageMsg
| FilePickerMsg         of FilePicker.Msg
| BuildingBlockMsg      of BuildingBlock.Msg
| ValidationMsg         of Validation.Msg
| ProtocolMsg           of Protocol.Msg
| JsonExporterMsg       of JsonExporter.Msg
| TemplateMetadataMsg   of TemplateMetadata.Msg
| BuildingBlockDetails  of BuildingBlockDetailsMsg
| SettingsXmlMsg        of SettingsXml.Msg
| SettingDataStewardMsg of SettingsDataStewardMsg
//| SettingsProtocolMsg   of SettingsProtocolMsg
| TopLevelMsg           of TopLevelMsg
| UpdatePageState       of Routing.Route option
| Batch                 of seq<Messages.Msg>
/// This function is used to pass any 'Msg' through a warning modal, where the user needs to verify his decision.
| UpdateWarningModal    of {|NextMsg:Msg; ModalMessage: string|} option
| DoNothing

let initializeModel (pageOpt: Route option, pageEntry:SwateEntry) =
    let isDarkMode =
        let cookies = Browser.Dom.document.cookie
        let cookiesSplit = cookies.Split([|";"|], System.StringSplitOptions.RemoveEmptyEntries)
        cookiesSplit
        |> Array.tryFind (fun x -> x.StartsWith (Cookies.IsDarkMode.toCookieString + "="))
        |> fun cookieOpt ->
            if cookieOpt.IsSome then
                cookieOpt.Value.Replace(Cookies.IsDarkMode.toCookieString + "=","")
                |> fun cookie ->
                    match cookie with
                    | "false"| "False"  -> false
                    | "true" | "True"   -> true
                    | anyElse -> false
            else
                false
    {
        DebouncerState              = Debouncer                 .create ()
        PageState                   = PageState                 .init pageOpt
        PersistentStorageState      = PersistentStorageState    .init (pageEntry=pageEntry)
        DevState                    = DevState                  .init ()
        SiteStyleState              = SiteStyleState            .init (darkMode=isDarkMode)
        TermSearchState             = TermSearch.Model          .init ()
        AdvancedSearchState         = AdvancedSearch.Model      .init ()
        ExcelState                  = OfficeInterop.Model       .init ()
        ApiState                    = ApiState                  .init ()
        FilePickerState             = FilePicker.Model          .init ()
        AddBuildingBlockState       = BuildingBlock.Model       .init ()
        ValidationState             = Validation.Model          .init ()
        ProtocolState               = Protocol.Model            .init ()
        BuildingBlockDetailsState   = BuildingBlockDetailsState .init ()
        SettingsXmlState            = SettingsXml.Model         .init ()
        SettingsDataStewardState    = SettingsDataStewardState  .init ()
        JsonExporterModel           = JsonExporter.Model        .init ()
        TemplateMetadataModel       = TemplateMetadata.Model    .init ()
        WarningModal                = None
    }
