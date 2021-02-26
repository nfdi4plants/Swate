module Messages

open Elmish
open Thoth.Elmish
open Shared

open ExcelColors
open OfficeInterop
open OfficeInterop.Types
open Model

//open ISADotNet

type ExcelInteropMsg =
    | Initialized                           of (string*string)
    | FillSelection                         of string * (DbDomain.Term option)
    | AddAnnotationBlock                    of OfficeInterop.Types.BuildingBlockTypes.MinimalBuildingBlock
    | AddAnnotationBlocks                   of OfficeInterop.Types.BuildingBlockTypes.MinimalBuildingBlock list * Xml.GroupTypes.Protocol * OfficeInterop.Types.Xml.ValidationTypes.TableValidation option
    | RemoveAnnotationBlock
    | AddUnitToAnnotationBlock              of unitTermName:string option * unitTermAccession:string option
    | FormatColumn                          of colname:string * formatString:string
    | FormatColumns                         of (string * string) list
    /// This message does not need the active annotation table as `PipeCreateAnnotationTableInfo` checks if any annotationtables exist in the active worksheet, and if so, errors.
    | CreateAnnotationTable                 of isDark:bool
    | AnnotationtableCreated                of string
    | AnnotationTableExists                 of TryFindAnnoTableResult
    | GetParentTerm
    | AutoFitTable
    | UpdateProtocolGroupHeader
    //
    | GetTableValidationXml
    | WriteTableValidationToXml             of newTableValidation:Xml.ValidationTypes.TableValidation * currentSwateVersion:string
    /// needs to set newColNames separately as these validations come from templates for protocol group insert
    | AddTableValidationtoExisting          of addedTableValidation:Xml.ValidationTypes.TableValidation * newColNames:string list * protocol:OfficeInterop.Types.Xml.GroupTypes.Protocol
    | WriteProtocolToXml                    of newProtocol:Xml.GroupTypes.Protocol
    | DeleteAllCustomXml
    | GetSwateCustomXml
    //
    | FillHiddenColsRequest
    | FillHiddenColumns                     of tableName:string*SearchTermI []
    | UpdateFillHiddenColsState             of FillHiddenColsState
    //
    | InsertFileNames                       of fileNameList:string list
    // Show Details to selected BuildingBlock
    | GetSelectedBuildingBlockSearchTerms
    // Development
    | TryExcel
    | TryExcel2
    //| ExcelSubscriptionMsg          of OfficeInterop.Types.Subscription.Msg

type TermSearchMsg =
    | ToggleSearchByParentOntology
    | SearchTermTextChange                  of string
    | TermSuggestionUsed                    of DbDomain.Term
    | NewSuggestions                        of DbDomain.Term []
    | StoreParentOntologyFromOfficeInterop  of obj option
    // Server
    | GetAllTermsByParentTermRequest        of OntologyInfo 
    | GetAllTermsByParentTermResponse       of DbDomain.Term []

type AdvancedSearchMsg =
    // Client
    | ResetAdvancedSearchState
    | ResetAdvancedSearchOptions
    | UpdateAdvancedTermSearchSubpage   of AdvancedTermSearchSubpages
    | ToggleModal                       of string
    | ToggleOntologyDropdown
    | UpdateAdvancedTermSearchOptions   of AdvancedTermSearchOptions
    | OntologySuggestionUsed            of DbDomain.Ontology option
    | ChangePageinationIndex            of int
    // Server
    /// Main function. Forward request to Request Api -> Server.
    | StartAdvancedSearch
    | NewAdvancedSearchResults          of DbDomain.Term []

type DevMsg =
    | LogTableMetadata
    | GenericLog            of (string*string)
    | GenericError          of exn
    | UpdateLastFullError   of exn option
    
type ApiRequestMsg =
    | TestOntologyInsert                        of (string*string*string*System.DateTime*string)
    | GetNewTermSuggestions                     of string
    | GetNewTermSuggestionsByParentTerm         of string*OntologyInfo
    | GetNewBuildingBlockNameSuggestions        of string
    | GetNewUnitTermSuggestions                 of string*relatedUnitSearch:UnitSearchRequest
    | GetNewAdvancedTermSearchResults           of AdvancedTermSearchOptions
    | FetchAllOntologies
    /// This function is used to search for all values found in the table main columns.
    /// InsertTerm [] is created by officeInterop and passed to server for db search.
    | SearchForInsertTermsRequest              of tableName:string*SearchTermI []
    //
    | GetAppVersion

type ApiResponseMsg =
    | TermSuggestionResponse                    of DbDomain.Term []
    | AdvancedTermSearchResultsResponse         of DbDomain.Term []
    | BuildingBlockNameSuggestionsResponse      of DbDomain.Term []
    | UnitTermSuggestionResponse                of DbDomain.Term [] * relatedUnitSearch:UnitSearchRequest
    | FetchAllOntologiesResponse                of DbDomain.Ontology []
    | SearchForInsertTermsResponse              of tableName:string*SearchTermI []  
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

type FilePickerMsg =
    | LoadNewFiles              of string list
    | UpdateFileNames           of newFileNames:(int*string) list
    ///
    | UpdateDNDDropped          of isDropped:bool

type AddBuildingBlockMsg =
    | NewBuildingBlockSelected  of AnnotationBuildingBlock
    | BuildingBlockNameChange   of string
    | ToggleSelectionDropdown

    | BuildingBlockNameSuggestionUsed   of DbDomain.Term
    | NewBuildingBlockNameSuggestions   of DbDomain.Term []

    | SearchUnitTermTextChange  of searchString:string * relatedUnitSearch:UnitSearchRequest
    | UnitTermSuggestionUsed    of unitTerm:DbDomain.Term* relatedUnitSearch:UnitSearchRequest
    | NewUnitTermSuggestions    of DbDomain.Term [] * relatedUnitSearch:UnitSearchRequest
    | ToggleBuildingBlockHasUnit

type ValidationMsg =
    // Client
    | UpdateDisplayedOptionsId of int option
    | UpdateTableValidationScheme of Xml.ValidationTypes.TableValidation
    // OfficeInterop
    | StoreTableRepresentationFromOfficeInterop of OfficeInterop.Types.Xml.ValidationTypes.TableValidation * buildingBlocks:OfficeInterop.Types.BuildingBlockTypes.BuildingBlock [] * msg:string

type ProtocolInsertMsg =
    // ------ Process from file ------
    | ParseJsonToProcessRequest of string
    | ParseJsonToProcessResult of Result<ISADotNet.Process,exn>
    // Client
    | RemoveProcessFromModel
    // ------ Protocol from Database ------
    | GetAllProtocolsRequest
    | GetAllProtocolsResponse of ProtocolTemplate []
    // Access xml from db and parse it
    /// Get Protocol Xml from db
    | GetProtocolXmlByProtocolRequest of ProtocolTemplate
    /// On return parse Protocol Xml
    | ParseProtocolXmlByProtocolRequest of ProtocolTemplate
    /// Store Result from ParseProtocolXmlByProtocolRequest in model
    | GetProtocolXmlByProtocolResponse of ProtocolTemplate * OfficeInterop.Types.Xml.ValidationTypes.TableValidation * OfficeInterop.Types.BuildingBlockTypes.MinimalBuildingBlock list
    // Client
    | UpdateUploadData of string
    | UpdateDisplayedProtDetailsId of int option
    | UpdateProtocolNameSearchQuery of string
    | UpdateProtocolTagSearchQuery of string
    | AddProtocolTag of string
    | RemoveProtocolTag of string
    | RemoveSelectedProtocol

type BuildingBlockDetailsMsg =
    | GetSelectedBuildingBlockSearchTermsRequest    of Shared.SearchTermI []
    | GetSelectedBuildingBlockSearchTermsResponse   of Shared.SearchTermI []
    | ToggleShowDetails
    | UpdateCurrentRequestState                     of RequestBuildingBlockInfoStates

type TopLevelMsg =
    | CloseSuggestions

type Msg =
    | Bounce                of (System.TimeSpan*string*Msg)
    | Api                   of ApiMsg
    | Dev                   of DevMsg
    | TermSearch            of TermSearchMsg
    | AdvancedSearch        of AdvancedSearchMsg
    | DebouncerSelfMsg      of Debouncer.SelfMessage<Msg>
    | ExcelInterop          of ExcelInteropMsg
    | StyleChange           of StyleChangeMsg
    | PersistentStorage     of PersistentStorageMsg
    | FilePicker            of FilePickerMsg
    | AddBuildingBlock      of AddBuildingBlockMsg
    | Validation            of ValidationMsg
    | ProtocolInsert        of ProtocolInsertMsg
    | BuildingBlockDetails  of BuildingBlockDetailsMsg
    | TopLevelMsg           of TopLevelMsg
    | UpdatePageState       of Routing.Route option
    | Batch                 of seq<Msg>
    | DoNothing
