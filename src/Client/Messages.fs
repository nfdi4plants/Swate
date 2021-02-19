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
    | PipeActiveAnnotationTable             of (TryFindAnnoTableResult -> ExcelInteropMsg)
    /// This is used to pipe (all table names []) to a ExcelInteropMsg.
    /// This is used to generate a new annotation table name.
    | PipeCreateAnnotationTableInfo         of (string [] -> ExcelInteropMsg)
    | Initialized                           of (string*string)
    | FillSelection                         of activeAnnotationTable:TryFindAnnoTableResult * string * (DbDomain.Term option)
    | AddAnnotationBlock                    of activeAnnotationTable:TryFindAnnoTableResult * OfficeInterop.Types.BuildingBlockTypes.MinimalBuildingBlock
    | AddAnnotationBlocks                   of activeAnnotationTable:TryFindAnnoTableResult * OfficeInterop.Types.BuildingBlockTypes.MinimalBuildingBlock list * Xml.GroupTypes.Protocol * OfficeInterop.Types.Xml.ValidationTypes.TableValidation option
    | RemoveAnnotationBlock                 of activeAnnotationTable:TryFindAnnoTableResult
    | AddUnitToAnnotationBlock              of tryFindActiveAnnotationTable:TryFindAnnoTableResult * unitTermName:string option * unitTermAccession:string option
    | FormatColumn                          of activeAnnotationTable:TryFindAnnoTableResult * colname:string * formatString:string
    | FormatColumns                         of activeAnnotationTable:TryFindAnnoTableResult * (string * string) list
    /// This message does not need the active annotation table as `PipeCreateAnnotationTableInfo` checks if any annotationtables exist in the active worksheet, and if so, errors.
    | CreateAnnotationTable                 of allTableNames:string [] * isDark:bool
    | AnnotationtableCreated                of activeAnnotationTable:TryFindAnnoTableResult * string
    | AnnotationTableExists                 of activeAnnotationTable:TryFindAnnoTableResult
    | GetParentTerm                         of activeAnnotationTable:TryFindAnnoTableResult
    | AutoFitTable                          of activeAnnotationTable:TryFindAnnoTableResult
    | UpdateProtocolGroupHeader             of activeAnnotationTable:TryFindAnnoTableResult
    //
    | GetTableValidationXml                 of activeAnnotationTable:TryFindAnnoTableResult
    | WriteTableValidationToXml             of newTableValidation:Xml.ValidationTypes.TableValidation * currentSwateVersion:string
    /// needs to set newColNames separately as these validations come from templates for protocol group insert
    | AddTableValidationtoExisting          of addedTableValidation:Xml.ValidationTypes.TableValidation * newColNames:string list * protocol:OfficeInterop.Types.Xml.GroupTypes.Protocol
    | WriteProtocolToXml                    of newProtocol:Xml.GroupTypes.Protocol
    | DeleteAllCustomXml
    | GetSwateCustomXml
    //
    | FillHiddenColsRequest                 of activeAnnotationTable:TryFindAnnoTableResult
    | FillHiddenColumns                     of tableName:string*SearchTermI []
    | UpdateFillHiddenColsState             of FillHiddenColsState
    //
    | InsertFileNames                       of activeAnnotationTable:TryFindAnnoTableResult*fileNameList:string list
    // Development
    | TryExcel
    | TryExcel2
    //| ExcelSubscriptionMsg          of OfficeInterop.Types.Subscription.Msg

type TermSearchMsg =
    | ToggleSearchByParentOntology
    | SearchTermTextChange      of string
    | TermSuggestionUsed        of DbDomain.Term
    | NewSuggestions            of DbDomain.Term []
    | StoreParentOntologyFromOfficeInterop of obj option

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
    | LogTableMetadata      of activeAnnotationTable:TryFindAnnoTableResult
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
    | GetAllProtocolsResponse of Protocol []
    // Access xml from db and parse it
    /// Get Protocol Xml from db
    | GetProtocolXmlByProtocolRequest of Protocol
    /// On return parse Protocol Xml
    | ParseProtocolXmlByProtocolRequest of Protocol
    /// Store Result from ParseProtocolXmlByProtocolRequest in model
    | GetProtocolXmlByProtocolResponse of Protocol * OfficeInterop.Types.Xml.ValidationTypes.TableValidation * OfficeInterop.Types.BuildingBlockTypes.MinimalBuildingBlock list
    // Client
    | UpdateUploadData of string
    | UpdateDisplayedProtDetailsId of int option
    | UpdateProtocolNameSearchQuery of string
    | UpdateProtocolTagSearchQuery of string
    | AddProtocolTag of string
    | RemoveProtocolTag of string
    | RemoveSelectedProtocol


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
    | TopLevelMsg           of TopLevelMsg
    | UpdatePageState       of Routing.Route option
    | Batch                 of seq<Msg>
    | DoNothing

/// This function is used to easily pipe a message into `PipeActiveAnnotationTable`. This is designed for a message with (x1) other params.
let pipeNameTuple msg param =
    PipeActiveAnnotationTable
        (fun annotationTableOpt ->
            msg (annotationTableOpt,param)
        )

/// This function is used to easily pipe a message into `PipeActiveAnnotationTable`. This is designed for a message with (x1,x2) other params.
/// Use this as: (x1,x2) |> pipeNameTuple2 msg
let pipeNameTuple2 msg param =
    PipeActiveAnnotationTable
        (fun annotationTableOpt ->
            let constructParam =
                param |> fun (x,y) -> annotationTableOpt,x,y    
            msg (constructParam)
        )

/// This function is used to easily pipe a message into `PipeActiveAnnotationTable`. This is designed for a message with (x1,x2,x3) other params.
/// Use this as: (x1,x2,x3) |> pipeNameTuple3 msg
let pipeNameTuple3 msg param =
    PipeActiveAnnotationTable
        (fun annotationTableOpt ->
            let constructParam =
                param |> fun (x,y,z) -> annotationTableOpt,x,y,z    
            msg (constructParam)
        )

/// This function is used to easily pipe a message into `PipeActiveAnnotationTable`. This is designed for a message with (x1,x2,x3,x4) other params.
/// Use this as: (x1,x2,x3) |> pipeNameTuple4 msg
let pipeNameTuple4 msg param =
    PipeActiveAnnotationTable
        (fun annotationTableOpt ->
            let constructParam =
                param |> fun (x,y,z,u) -> annotationTableOpt,x,y,z,u 
            msg (constructParam)
        )