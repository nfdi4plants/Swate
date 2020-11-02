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
        | _ -> Error(System.DateTime.UtcNow,"wut?")

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

type TermSearchState = {
    TermSearchText          : string
    SelectedTerm            : DbDomain.Term option
    TermSuggestions         : DbDomain.Term []
    ParentOntology          : string option
    SearchByParentOntology  : bool
    HasSuggestionsLoading   : bool
    ShowSuggestions         : bool
} with
    static member init () = {
        TermSearchText              = ""
        SelectedTerm                = None
        TermSuggestions             = [||]
        ParentOntology              = None
        SearchByParentOntology      = true
        HasSuggestionsLoading       = false
        ShowSuggestions             = false
    }

type AdvancedSearchState = {
    ModalId                             : string
    HasModalVisible                     : bool
    HasOntologyDropdownVisible          : bool
    AdvancedSearchOptions               : AdvancedTermSearchOptions
    AdvancedSearchTermResults           : DbDomain.Term []
    HasAdvancedSearchResultsLoading     : bool
    ShowAdvancedSearchResults           : bool
    AdvancedSearchResultPageinationIndex: int
    SelectedResult                      : DbDomain.Term option
} with
    static member init () = {
        ModalId                             = ""
        HasModalVisible                     = false
        HasOntologyDropdownVisible          = false
        AdvancedSearchOptions               = AdvancedTermSearchOptions.init ()
        AdvancedSearchTermResults           = [||]
        HasAdvancedSearchResultsLoading     = false
        ShowAdvancedSearchResults           = false
        AdvancedSearchResultPageinationIndex= 0
        SelectedResult                      = None
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
    Host                : string
    Platform            : string
    HasAnnotationTable  : bool
} with
    static member init () = {
        Host                = ""
        Platform            = ""
        HasAnnotationTable  = false
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
    CurrentPage : Routing.Route
    CurrentUrl  : string
} with
    static member init (pageOpt:Route option) = 
        match pageOpt with
        | Some page -> {
            CurrentPage = page
            CurrentUrl = Route.toRouteUrl page
            }
        | None -> {
            CurrentPage = Route.Home
            CurrentUrl = ""
            }

type FilePickerState = {
    FileNames : string list
} with
    static member init () = {
        FileNames = []
    }

type AnnotationBuildingBlockType =
    | NoneSelected
    | Parameter         
    | Factor            
    | Characteristics
    | Source
    | Sample            
    | Data              

    static member toString = function
        | NoneSelected      -> "NoneSelected"
        | Parameter         -> "Parameter"
        | Factor            -> "Factor"
        | Characteristics   -> "Characteristics"
        | Sample            -> "Sample"
        | Data              -> "Data"
        | Source            -> "Source"

    static member toShortExplanation = function
        | Parameter         -> "Use parameter columns to annotate your experimental workflow. multiple parameters form a protocol. Example: centrifugation time, precipitate agent, ..."
        | Factor            -> "Use factor columns to track the experimental conditions that govern your study. Example: temperature,light,..."
        | Characteristics   -> "Use characteristics columns to annotate interesting properties of your organism. Example: strain,phenotype,... "
        | Sample            -> "Use sample columns to mark the name of the sample that your experimental workflow produced."
        | Data              -> "Use data columns to mark the data file name that your computational analysis produced"
        | Source            -> "Attention: you normally dont have to add this manually if you initialize an annotation table. The Source column defines the organism that is subject to your study. It is the first column of every study file."
        | _                 -> "Please select an annotation building block."

    static member toLongExplanation = function
        | Parameter         -> "Placeholder pls ignore"
        | Factor            -> "Placeholder pls ignore"
        | Characteristics   -> "Placeholder pls ignore"
        | Sample            -> "Placeholder pls ignore"
        | Data              -> "Placeholder pls ignore"
        | Source            -> "Placeholder pls ignore"
        | _                 -> "You should not be able to see this text."

type AnnotationBuildingBlock = {
    Type : AnnotationBuildingBlockType
    Name : string
} with
    static member init (t : AnnotationBuildingBlockType) = {
        Type = t
        Name = ""
    }

    static member toAnnotationTableHeader (block : AnnotationBuildingBlock) =
        match block.Type with
        | Parameter         -> sprintf "Parameter [%s]" block.Name
        | Factor            -> sprintf "Factor [%s]" block.Name
        | Characteristics   -> sprintf "Characteristics [%s]" block.Name
        | Sample            -> "Sample Name"
        | Data              -> "Data File Name"
        | _                 -> ""


type AddBuildingBlockState = {
    CurrentBuildingBlock                    : AnnotationBuildingBlock

    BuildingBlockNameSuggestions            : DbDomain.Term []
    ShowBuildingBlockSelection              : bool
    BuildingBlockHasUnit                    : bool
    ShowBuildingBlockNameSuggestions        : bool
    HasBuildingBlockNameSuggestionsLoading  : bool

    UnitTermSearchText                      : string
    UnitTermSuggestions                     : DbDomain.Term []
    HasUnitTermSuggestionsLoading           : bool
    ShowUnitTermSuggestions                 : bool
    UnitFormat                              : string

} with
    static member init () = {
        CurrentBuildingBlock                    = AnnotationBuildingBlock.init NoneSelected

        BuildingBlockNameSuggestions            = [||]
        ShowBuildingBlockSelection              = false
        BuildingBlockHasUnit                    = false
        ShowBuildingBlockNameSuggestions        = false
        HasBuildingBlockNameSuggestionsLoading  = false

        UnitTermSearchText                      = ""
        UnitTermSuggestions                     = [||]
        HasUnitTermSuggestionsLoading           = false
        ShowUnitTermSuggestions                 = false
        UnitFormat                              = ""
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

    AdvancedSearchState     : AdvancedSearchState

    //Use this in the future to model excel stuff like table data
    ExcelState              : ExcelState

    //Use this to log Api calls and maybe handle them better
    ApiState                : ApiState

    //States regarding File picker functionality
    FilePickerState         : FilePickerState

    //Insert annotation columns
    AddBuildingBlockState   : AddBuildingBlockState
    }


let initializeModel (pageOpt: Route option) = {
    DebouncerState          = Debouncer             .create()
    PageState               = PageState             .init pageOpt
    PersistentStorageState  = PersistentStorageState.init ()
    DevState                = DevState              .init ()
    SiteStyleState          = SiteStyleState        .init ()
    TermSearchState         = TermSearchState       .init ()
    AdvancedSearchState     = AdvancedSearchState   .init ()
    ExcelState              = ExcelState            .init ()
    ApiState                = ApiState              .init ()
    FilePickerState         = FilePickerState       .init ()
    AddBuildingBlockState   = AddBuildingBlockState .init ()
}
