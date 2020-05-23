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
    | FillSelection             of string
    | AddColumn                 of string
    | CreateAnnotationTable     of bool

type SimpleTermSearchMsg =
    | SearchTermTextChange      of string
    | TermSuggestionUsed        of string
    | NewSuggestions            of DbDomain.Term []

type AdvancedTermSearchMsg =
    | SearchOntologyTextChange      of string
    | AdvancedSearchOptionsChange   of AdvancedTermSearchOptions
    | AdvancedSearchResultUsed      of string
    | OntologySuggestionUsed        of DbDomain.Ontology
    | NewAdvancedSearchResults      of DbDomain.Term []
    
type TermSearchMsg =
    | SwitchSearchMode
    | Simple    of SimpleTermSearchMsg
    | Advanced  of AdvancedTermSearchMsg

type DevMsg =
    | LogTableMetadata
    | GenericLog        of (string*string)
    | GenericError      of exn
    
type ApiRequestMsg =
    | TestOntologyInsert                        of (string*string*string*System.DateTime*string)
    | GetNewTermSuggestions                     of string
    | GetNewAdvancedTermSearchResults           of AdvancedTermSearchOptions
    | FetchAllOntologies

type ApiResponseMsg =
    | TermSuggestionResponse                    of DbDomain.Term []
    | GetNewAdvancedTermSearchResultsResponse   of DbDomain.Term []
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

type Msg =
    | Bounce                of (System.TimeSpan*string*Msg)
    | Api                   of ApiMsg
    | Dev                   of DevMsg
    | TermSearch            of TermSearchMsg
    | DebouncerSelfMsg      of Debouncer.SelfMessage<Msg>
    | ExcelInterop          of ExcelInteropMsg
    | StyleChange           of StyleChangeMsg
    | PersistentStorage     of PersistentStorageMsg
    | FilePicker            of FilePickerMsg
    | AddBuildingBlock      of AddBuildingBlockMsg
    | DoNothing