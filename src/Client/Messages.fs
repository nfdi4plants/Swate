module Messages

open Elmish
open Thoth.Elmish

open ISADotNet

open Shared

open ExcelColors
open OfficeInterop
open OfficeInterop.Types
open Model

type ExcelInteropMsg =
    | PipeActiveAnnotationTable     of (TryFindAnnoTableResult -> ExcelInteropMsg)
    /// This is used to pipe (all table names []) to a ExcelInteropMsg.
    /// This is used to generate a new annotation table name.
    | PipeCreateAnnotationTableInfo of (string [] -> ExcelInteropMsg)
    | Initialized                   of (string*string)
    | SyncContext                   of activeAnnotationTable:TryFindAnnoTableResult*string
    | InSync                        of string
    | FillSelection                 of activeAnnotationTable:TryFindAnnoTableResult * string * (DbDomain.Term option)
    | AddColumn                     of activeAnnotationTable:TryFindAnnoTableResult * colname:string * format:(string*string option) option
    | FormatColumn                  of activeAnnotationTable:TryFindAnnoTableResult * colname:string * formatString:string * prevmsg:string
    /// This message does not need the active annotation table as `PipeCreateAnnotationTableInfo` checks if any annotationtables exist in the active worksheet, and if so, errors.
    | CreateAnnotationTable         of allTableNames:string [] * isDark:bool
    | AnnotationtableCreated        of activeAnnotationTable:TryFindAnnoTableResult * string
    | AnnotationTableExists         of activeAnnotationTable:TryFindAnnoTableResult
    | GetParentTerm                 of activeAnnotationTable:TryFindAnnoTableResult
    | AutoFitTable                  of activeAnnotationTable:TryFindAnnoTableResult
    //
    | GetTableValidationXml         of activeAnnotationTable:TryFindAnnoTableResult
    | WriteTableValidationToXml     of newTableValidation:XmlValidationTypes.TableValidation * currentSwateVersion:string
    | DeleteAllCustomXml
    | GetSwateValidationXml
    //
    | ToggleEventHandler
    | UpdateTablesHaveAutoEditHandler
    //
    | FillHiddenColsRequest         of activeAnnotationTable:TryFindAnnoTableResult
    | FillHiddenColumns             of tableName:string*SearchTermI []
    | UpdateFillHiddenColsState     of FillHiddenColsState
    //
    | InsertFileNames               of activeAnnotationTable:TryFindAnnoTableResult*fileNameList:string list
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
    | GetNewTermSuggestionsByParentTerm         of string*string
    | GetNewBuildingBlockNameSuggestions        of string
    | GetNewUnitTermSuggestions                 of string
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
    | UnitTermSuggestionResponse                of DbDomain.Term []
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
    | UpdateDNDDropped          of isDropped:bool

type AddBuildingBlockMsg =
    | NewBuildingBlockSelected  of AnnotationBuildingBlock
    | BuildingBlockNameChange   of string
    | ToggleSelectionDropdown

    | BuildingBlockNameSuggestionUsed   of string
    | NewBuildingBlockNameSuggestions   of DbDomain.Term []

    | SearchUnitTermTextChange  of string
    | UnitTermSuggestionUsed    of unitName:string*unitAccession:string
    | NewUnitTermSuggestions    of DbDomain.Term []
    | ToggleBuildingBlockHasUnit

type ValidationMsg =
    // Client
    | UpdateDisplayedOptionsId of int option
    | UpdateTableValidationScheme of XmlValidationTypes.TableValidation
    // OfficeInterop
    | StoreTableRepresentationFromOfficeInterop of OfficeInterop.Types.XmlValidationTypes.TableValidation * buildingBlocks:OfficeInterop.Types.BuildingBlockTypes.BuildingBlock [] * msg:string

type FileUploadJsonMsg =
    // Client
    | UpdateUploadData of string
    | ParseJsonToProcessRequest of string
    | ParseJsonToProcessResult of Result<Process,exn>

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
    | FileUploadJson        of FileUploadJsonMsg
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
            |> PipeActiveAnnotationTable
        )