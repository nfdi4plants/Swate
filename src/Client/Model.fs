module Model

open Fable.React
open Fable.React.Props
open Fulma
open Shared
open Thoth.Elmish

type LogItem =
    | Debug of (System.DateTime*string)
    | Info  of (System.DateTime*string)
    | Error of (System.DateTime*string)

    static member toTableRow = function
        | Debug (t,m) ->
            tr [] [
                td [] [str (sprintf "[%s]" (t.ToShortTimeString()))]
                td [Style [Color "green"; FontWeight "bold"]] [str "Debug"]
                td [] [str m]
            ]
        | Info  (t,m) ->
            tr [] [
                td [] [str (sprintf "[%s]" (t.ToShortTimeString()))]
                td [Style [Color "lightblue"; FontWeight "bold"]] [str "Info"]
                td [] [str m]
            ]
        | Error (t,m) ->
            tr [] [
                td [] [str (sprintf "[%s]" (t.ToShortTimeString()))]
                td [Style [Color "red"; FontWeight "bold"]] [str "ERROR"]
                td [] [str m]
            ]

    static member ofStringNow (level:string) (message: string) =
        match level with
        | "Debug" -> Debug(System.DateTime.UtcNow,message)
        | "Info"  -> Info (System.DateTime.UtcNow,message)
        | "Error" -> Error(System.DateTime.UtcNow,message)

type TermSearchMode =
    | Simple
    | Advanced

type AdvancedTermSearchOptions = {
    Ontology                : DbDomain.Ontology option
    StartsWith              : string
    MustContain             : string 
    EndsWith                : string
    DefinitionMustContain   : string
    KeepObsolete            : bool
}

let initAdvancedTermSearchOptions () = {
    Ontology                = None
    StartsWith              = ""
    MustContain             = "" 
    EndsWith                = ""
    DefinitionMustContain   = ""
    KeepObsolete            = true
}

//TO-DO refactor model to different types as it already has become quite complicated

type SimpleTermSearchState = {
    Debouncer               : Debouncer.State
    TermSearchText          : string
    TermSuggestions         : DbDomain.Term []
    HasSuggestionsLoading   : bool
    ShowSuggestions         : bool
}

let initSimpleTermSearchState () = {
    Debouncer               = Debouncer.create()
    TermSearchText          = ""
    TermSuggestions         = [||]
    HasSuggestionsLoading   = false
    ShowSuggestions         = false
}

type AdvancedTermSearchState = {
    OntologySearchText              : string
    HasOntologySuggestionsLoading   : bool
    ShowOntologySuggestions         : bool
    AdvancedSearchOptions           : AdvancedTermSearchOptions
    AdvancedSearchTermResults       : DbDomain.Term []
    HasAdvancedSearchResultsLoading : bool
    ShowAdvancedSearchResults       : bool
}

let initAdvancedTermSearchState () = {
    OntologySearchText              = ""
    HasOntologySuggestionsLoading   = false
    ShowOntologySuggestions         = false
    AdvancedSearchOptions           = initAdvancedTermSearchOptions ()
    AdvancedSearchTermResults       = [||]
    HasAdvancedSearchResultsLoading = false
    ShowAdvancedSearchResults       = false
}

type TermSearchState = {
    Advanced    : AdvancedTermSearchState
    Simple      : SimpleTermSearchState
    SearchMode  : TermSearchMode
}

let initTermSearchState() = {
    Advanced    = initAdvancedTermSearchState()
    Simple      = initSimpleTermSearchState()
    SearchMode  = TermSearchMode.Simple
}

type SiteStyleState = {
    BurgerVisible   : bool
    IsDarkMode      : bool
    ColorMode       : ExcelColors.ColorMode
}

let initSiteStyleState () = {
    BurgerVisible   = false
    IsDarkMode      = false
    ColorMode       = ExcelColors.colorfullMode
}

type DevState = {
    LastFullError                       : System.Exception option
    Log                                 : LogItem list
}

let initDevState () = {
    LastFullError   = None
    Log             = []
}

type PersistentStorageState = {
    SearchableOntologies    : (Set<string>*DbDomain.Ontology) []
    HasOntologiesLoaded     : bool 
}

let initPersistentStorageState () = {
    SearchableOntologies    = [||]
    HasOntologiesLoaded     = false
}

type ExcelState = {
    Placeholder: string
}

let initExcelState() = {
    Placeholder = ""
}

type ApiCallStatus =
    | IsNone
    | Pending
    | Successfull
    | Failed of string

type ApiCallHistoryItem = {
    FunctionName   : string
    Status         : ApiCallStatus
}

let noCall = {
    FunctionName = "None"
    Status = IsNone
}

type ApiState = {
    currentCall : ApiCallHistoryItem
    callHistory : ApiCallHistoryItem list
}

let initApiState () = {
    currentCall = noCall
    callHistory = []
}

type Model = {
    //One time sync with server
    PersistentStorageState  : PersistentStorageState
 
    //Debouncing
    DebouncerState          : Debouncer.State

    //Error handling, Logging, etc.
    DevState                : DevState

    //Site Meta Options (Styling etc)
    SiteStyleState          : SiteStyleState

    //States regarding term search
    TermSearchState         : TermSearchState

    //Use this in the future to model excel stuff like table data
    ExcelState              : ExcelState

    //Use this to log Api calls and maybe handle them better
    ApiState                : ApiState

    //Column insert
    AddColumnText           : string
    }


let initializeModel () = {
    PersistentStorageState  = initPersistentStorageState()
    DebouncerState          = Debouncer.create()
    DevState                = initDevState()
    SiteStyleState          = initSiteStyleState()
    TermSearchState         = initTermSearchState()
    ExcelState              = initExcelState()
    ApiState                = initApiState()
    AddColumnText           = ""
}
