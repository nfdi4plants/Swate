module Messages

open Shared
open ExcelColors
open OfficeInterop
open Model
open Elmish
open Thoth.Elmish

type ExcelInteropMsg =
    | Initialized               of (string*string)
    | SyncContext               of string
    | InSync                    of string
    | TryExcel
    | FillSelection             of string * (DbDomain.Term option)
    | AddColumn                 of colname:string * formatString:string
    | FormatColumn              of colname:string * cloind:float * formatString:string
    | CreateAnnotationTable     of bool
    | AnnotationtableCreated    of string
    | AnnotationTableExists     of bool
    | GetParentTerm

type TermSearchMsg =
    | ToggleSearchByParentOntology
    | SearchTermTextChange      of string
    | TermSuggestionUsed        of DbDomain.Term
    | NewSuggestions            of DbDomain.Term []
    | StoreParentOntologyFromOfficeInterop  of obj option

type AdvancedSearchMsg =
    | ResetAdvancedSearchState
    | ResetAdvancedSearchOptions
    | ToggleModal                   of string
    | ToggleOntologyDropdown
    | AdvancedSearchOptionsChange   of AdvancedTermSearchOptions
    | AdvancedSearchResultSelected  of DbDomain.Term
    | OntologySuggestionUsed        of DbDomain.Ontology
    | StartAdvancedSearch
    | NewAdvancedSearchResults      of DbDomain.Term []
    | ChangePageinationIndex        of int

type DevMsg =
    | LogTableMetadata
    | GenericLog        of (string*string)
    | GenericError      of exn
    
type ApiRequestMsg =
    | TestOntologyInsert                        of (string*string*string*System.DateTime*string)
    | GetNewTermSuggestions                     of string
    | GetNewTermSuggestionsByParentTerm         of string*string
    | GetNewBuildingBlockNameSuggestions        of string
    | GetNewUnitTermSuggestions                 of string
    | GetNewAdvancedTermSearchResults           of AdvancedTermSearchOptions
    | FetchAllOntologies

type ApiResponseMsg =
    | TermSuggestionResponse                    of DbDomain.Term []
    | AdvancedTermSearchResultsResponse         of DbDomain.Term []
    | BuildingBlockNameSuggestionsResponse      of DbDomain.Term []
    | UnitTermSuggestionResponse                of DbDomain.Term []
    | FetchAllOntologiesResponse                of DbDomain.Ontology []

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

type FilePickerMsg =
    | NewFilesLoaded            of string list
    | RemoveFileFromFileList    of string

type AddBuildingBlockMsg =
    | NewBuildingBlockSelected  of AnnotationBuildingBlock
    | BuildingBlockNameChange   of string
    | ToggleSelectionDropdown

    | BuildingBlockNameSuggestionUsed   of string
    | NewBuildingBlockNameSuggestions   of DbDomain.Term []

    | SearchUnitTermTextChange  of string
    | UnitTermSuggestionUsed    of string
    | NewUnitTermSuggestions    of DbDomain.Term []
    | BuildingBlockHasUnitSwitch 

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
    | UpdatePageState of Routing.Route option
    | DoNothing