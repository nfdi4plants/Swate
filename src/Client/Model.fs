module Model

open Fable.React
open Fable.React.Props
open Fulma
open Shared
open Thoth.Elmish
open Routing

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
    } with
        static member init() = {
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
} with
    static member init () = {
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
} with
    static member init () = {
        OntologySearchText              = ""
        HasOntologySuggestionsLoading   = false
        ShowOntologySuggestions         = false
        AdvancedSearchOptions           = AdvancedTermSearchOptions.init ()
        AdvancedSearchTermResults       = [||]
        HasAdvancedSearchResultsLoading = false
        ShowAdvancedSearchResults       = false
    }

type TermSearchState = {
    Advanced    : AdvancedTermSearchState
    Simple      : SimpleTermSearchState
    SearchMode  : TermSearchMode
} with
    static member init () = {
        Advanced    = AdvancedTermSearchState.init()
        Simple      = SimpleTermSearchState  .init()
        SearchMode  = TermSearchMode.Simple
    }

type SiteStyleState = {
    BurgerVisible   : bool
    IsDarkMode      : bool
    ColorMode       : ExcelColors.ColorMode
} with
    static member init () = {
        BurgerVisible   = false
        IsDarkMode      = false
        ColorMode       = ExcelColors.colorfullMode
    }

type DevState = {
    LastFullError                       : System.Exception option
    Log                                 : LogItem list
} with
    static member init () = {
        LastFullError   = None
        Log             = []
    }

type PersistentStorageState = {
    SearchableOntologies    : (Set<string>*DbDomain.Ontology) []
    HasOntologiesLoaded     : bool 
} with
    static member init () = {
        SearchableOntologies    = [||]
        HasOntologiesLoaded     = false
    }

type ExcelState = {
    Placeholder: string
} with
    static member init () = {
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
} with
    static member init() = {
        currentCall = noCall
        callHistory = []
    }

type PageState = {
    CurrentPage : Routing.Page
    CurrentUrl  : string
} with
    static member init (pageOpt:Page option) = 
        match pageOpt with
        | Some page -> {
            CurrentPage = page
            CurrentUrl = Page.toPath page
            }
        | None -> {
            CurrentPage = Page.Home
            CurrentUrl = ""
            }

type FilePickerState = {
    FileNames : string list
} with
    static member init () = {
        FileNames = []
    }

type Model = {

    //PageState
    PageState               : PageState

    //Data that needs to be persistent once loaded
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

    //States regarding File picker functionality
    FilePicker              : FilePickerState

    //Column insert
    AddColumnText           : string
    }


let initializeModel (pageOpt: Page option) = {
    DebouncerState          = Debouncer             .create()
    PageState               = PageState             .init pageOpt
    PersistentStorageState  = PersistentStorageState.init ()
    DevState                = DevState              .init ()
    SiteStyleState          = SiteStyleState        .init ()
    TermSearchState         = TermSearchState       .init ()
    ExcelState              = ExcelState            .init ()
    ApiState                = ApiState              .init ()
    FilePicker              = FilePickerState       .init ()
    AddColumnText           = ""
}
