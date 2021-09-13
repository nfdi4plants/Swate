module Model

open Fable.React
open Fable.React.Props
open Fulma
open Shared
open TermTypes
open ProtocolTemplateTypes
open Thoth.Elmish
open Routing

type Cookies =
| IsDarkMode

    member this.toCookieString =
        match this with
        | IsDarkMode    -> "isDarkmode"

    static member ofString str =
        match str with
        | "isDarkmode"  -> IsDarkMode
        | anyElse       -> failwith (sprintf "Cookie-Parser encountered unknown cookie name: %s" anyElse)

type LogItem =
    | Debug of (System.DateTime*string)
    | Info  of (System.DateTime*string)
    | Error of (System.DateTime*string)

    static member ofInteropLogginMsg (msg:InteropLogging.Msg) =
        match msg.LogIdentifier with
        | InteropLogging.Info   -> Info (System.DateTime.UtcNow,msg.MessageTxt)
        | InteropLogging.Debug  -> Debug(System.DateTime.UtcNow,msg.MessageTxt)
        | InteropLogging.Error  -> Error(System.DateTime.UtcNow,msg.MessageTxt)

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
        | "Debug"| "debug"  -> Debug(System.DateTime.UtcNow,message)
        | "Info" | "info"   -> Info (System.DateTime.UtcNow,message)
        | "Error" | "error" -> Error(System.DateTime.UtcNow,message)
        | others -> Error(System.DateTime.UtcNow,sprintf "Swate found an unexpected log identifier: %s" others)

type TermSearchMode =
    | Simple
    | Advanced

type AdvancedTermSearchOptions = {
    Ontology                : DbDomain.Ontology option
    SearchTermName          : string
    MustContainName         : string 
    SearchTermDefinition    : string
    MustContainDefinition   : string
    KeepObsolete            : bool
    } with
        static member init() = {
            Ontology                = None
            SearchTermName          = ""
            MustContainName         = "" 
            SearchTermDefinition    = ""
            MustContainDefinition   = ""
            KeepObsolete            = true
        }

type TermSearchState = {

    TermSearchText          : string

    SelectedTerm            : DbDomain.Term option
    TermSuggestions         : DbDomain.Term []

    ParentOntology          : TermMinimal option
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

type AdvancedTermSearchSubpages =
| InputFormSubpage
| ResultsSubpage
| SelectedResultSubpage of DbDomain.Term

type AdvancedSearchState = {
    ModalId                             : string
    ///
    AdvancedSearchOptions               : AdvancedTermSearchOptions
    AdvancedSearchTermResults           : DbDomain.Term []
    SelectedResult                      : DbDomain.Term option
    // Client visual design
    AdvancedTermSearchSubpage           : AdvancedTermSearchSubpages
    HasModalVisible                     : bool
    HasOntologyDropdownVisible          : bool
    HasAdvancedSearchResultsLoading     : bool
    AdvancedSearchResultPageinationIndex: int
} with
    static member init () = {
        ModalId                             = ""
        HasModalVisible                     = false
        HasOntologyDropdownVisible          = false
        AdvancedSearchOptions               = AdvancedTermSearchOptions.init ()
        AdvancedSearchTermResults           = [||]
        HasAdvancedSearchResultsLoading     = false
        AdvancedTermSearchSubpage           = InputFormSubpage
        AdvancedSearchResultPageinationIndex= 0
        SelectedResult                      = None
    }

type SiteStyleState = {
    QuickAcessIconsShown : bool
    BurgerVisible   : bool
    IsDarkMode      : bool
    ColorMode       : ExcelColors.ColorMode
} with
    static member init (?darkMode) = {
        QuickAcessIconsShown    = false
        BurgerVisible           = false
        IsDarkMode              = if darkMode.IsSome then darkMode.Value else false
        ColorMode               = if darkMode.IsSome && darkMode.Value = true then ExcelColors.darkMode else ExcelColors.colorfullMode
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
    AppVersion              : string
    HasOntologiesLoaded     : bool 
} with
    static member init () = {
        SearchableOntologies    = [||]
        AppVersion              = ""
        HasOntologiesLoaded     = false
    }

type FillHiddenColsState =
| Inactive
| ExcelCheckHiddenCols
| ServerSearchDatabase
| ExcelWriteFoundTerms
    member this.toReadableString =
        match this with
        | Inactive          -> ""
        | ExcelCheckHiddenCols  -> "Check Hidden Cols"
        | ServerSearchDatabase  -> "Search Database"
        | ExcelWriteFoundTerms  -> "Write Terms"

type ExcelState = {
    Host                        : string
    Platform                    : string
    HasAnnotationTable          : bool
    TablesHaveAutoEditHandler   : bool
    FillHiddenColsStateStore    : FillHiddenColsState
} with
    static member init () = {
        Host                = ""
        Platform            = ""
        HasAnnotationTable  = false
        TablesHaveAutoEditHandler = false
        FillHiddenColsStateStore = Inactive
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
    FileNames       : (int*string) list
    /// Used for drag and drop, to determine if something is currently dragged or not.
    /// Necessary to deactivate pointer events on children during drag.
    DNDDropped      : bool
} with
    static member init () = {
        FileNames = []
        /// This is used to deactivate pointerevents of drag and drop childs during drag and drop
        DNDDropped = true
    }

open OfficeInteropTypes

type AddBuildingBlockState = {
    CurrentBuildingBlock                    : BuildingBlockNamePrePrint

    BuildingBlockSelectedTerm               : DbDomain.Term option
    BuildingBlockNameSuggestions            : DbDomain.Term []
    ShowBuildingBlockSelection              : bool
    BuildingBlockHasUnit                    : bool
    ShowBuildingBlockTermSuggestions        : bool
    HasBuildingBlockTermSuggestionsLoading  : bool

    /// This section is used to add a unit directly to a freshly created building block.
    UnitTermSearchText                      : string
    UnitSelectedTerm                        : DbDomain.Term option
    UnitTermSuggestions                     : DbDomain.Term []
    HasUnitTermSuggestionsLoading           : bool
    ShowUnitTermSuggestions                 : bool

    /// This section is used to add a unit directly to an already existing building block
    Unit2TermSearchText                     : string
    Unit2SelectedTerm                       : DbDomain.Term option
    Unit2TermSuggestions                    : DbDomain.Term []
    HasUnit2TermSuggestionsLoading          : bool
    ShowUnit2TermSuggestions                : bool

} with
    static member init () = {
        ShowBuildingBlockSelection              = false

        CurrentBuildingBlock                    = BuildingBlockNamePrePrint.init BuildingBlockType.Parameter
        BuildingBlockSelectedTerm               = None
        BuildingBlockNameSuggestions            = [||]
        ShowBuildingBlockTermSuggestions        = false
        HasBuildingBlockTermSuggestionsLoading  = false
        BuildingBlockHasUnit                    = false

        /// This section is used to add a unit directly to a freshly created building block.
        UnitTermSearchText                      = ""
        UnitSelectedTerm                        = None
        UnitTermSuggestions                     = [||]
        ShowUnitTermSuggestions                 = false
        HasUnitTermSuggestionsLoading           = false

        /// This section is used to add a unit directly to an already existing building block
        Unit2TermSearchText                     = ""
        Unit2SelectedTerm                       = None
        Unit2TermSuggestions                    = [||]
        ShowUnit2TermSuggestions                = false
        HasUnit2TermSuggestionsLoading          = false
    }

/// Validation scheme for Table
type ValidationState = {
    ActiveTableBuildingBlocks   : BuildingBlockTypes.BuildingBlock []
    TableValidationScheme       : OfficeInterop.Types.Xml.ValidationTypes.TableValidation
    // Client view related
    DisplayedOptionsId      : int option
} with
    static member init () = {
        ActiveTableBuildingBlocks   = [||]
        TableValidationScheme       = OfficeInterop.Types.Xml.ValidationTypes.TableValidation.init()
        DisplayedOptionsId          = None
    }


//open ISADotNet


/// This model is used for both protocol insert and protocol search
type ProtocolInsertState = {
    // Client view
    DisplayedProtDetailsId  : int option

    // Process.json file upload
    UploadData              : string
    //ProcessModel            : ISADotNet.Process option

    // Database protocol template
    ProtocolSelected        : ProtocolTemplate option
    ValidationXml           : OfficeInterop.Types.Xml.ValidationTypes.TableValidation option
    // 
    ProtocolsAll            : ProtocolTemplate []

    ProtocolNameSearchQuery : string
    ProtocolTagSearchQuery  : string
    ProtocolSearchTags      : string list
    Loading                 : bool

} with
    static member init () = {
        // Client view
        DisplayedProtDetailsId  = None

        // ISADotNet Process.json file upload
        UploadData              = ""
        //ProcessModel            = None

        // Database protocol templates
        ProtocolSelected        = None
        ValidationXml           = None
        ProtocolsAll            = [||]
        ProtocolNameSearchQuery = ""
        ProtocolTagSearchQuery  = ""
        ProtocolSearchTags      = []

        Loading                 = false
    }

type RequestBuildingBlockInfoStates =
| Inactive
| RequestExcelInformation
| RequestDataBaseInformation
    member this.toStringMsg =
        match this with
        | Inactive                      -> ""
        | RequestExcelInformation       -> "Check Columns"
        | RequestDataBaseInformation    -> "Search Database "

type BuildingBlockDetailsState = {
    CurrentRequestState : RequestBuildingBlockInfoStates
    ShowDetails         : bool
    BuildingBlockValues : TermSearchable []
} with
    static member init () = {
        CurrentRequestState = Inactive
        ShowDetails         = false
        BuildingBlockValues = [||]
    }

type SettingsXmlState = {
    // // Client // //
    // Validation xml
    ActiveSwateValidation                   : OfficeInterop.Types.Xml.ValidationTypes.TableValidation option
    NextAnnotationTableForActiveValidation  : AnnotationTable option
    // Protocol group xml
    ActiveProtocolGroup                     : OfficeInterop.Types.Xml.GroupTypes.ProtocolGroup option
    NextAnnotationTableForActiveProtGroup   : AnnotationTable option
    // Protocol
    ActiveProtocol                          : OfficeInterop.Types.Xml.GroupTypes.Protocol option
    NextAnnotationTableForActiveProtocol    : AnnotationTable option
    //
    RawXml                                  : string
    NextRawXml                              : string
    FoundTables                             : Shared.AnnotationTable []
    ProtocolGroupXmls                       : OfficeInterop.Types.Xml.GroupTypes.ProtocolGroup []
    ValidationXmls                          : OfficeInterop.Types.Xml.ValidationTypes.TableValidation []
} with
    static member init () = {
        // Client
        ActiveSwateValidation                   = None
        NextAnnotationTableForActiveValidation  = None
        ActiveProtocolGroup                     = None
        NextAnnotationTableForActiveProtGroup   = None
        ActiveProtocol                          = None
        /// Unused
        NextAnnotationTableForActiveProtocol    = None
        //
        RawXml                                  = ""
        NextRawXml                              = ""
        FoundTables                             = [||]
        ProtocolGroupXmls                       = [||]
        ValidationXmls                          = [||]
    }

type SettingsDataStewardState = {
    PointerJson : string option
} with
    static member init () = {
        PointerJson = None
    }

type SettingsProtocolState = {
    ProtocolsFromExcel  : OfficeInterop.Types.Xml.GroupTypes.ProtocolGroup option
    ProtocolsFromDB     : ProtocolTemplate []
} with
    static member init () = {
        ProtocolsFromExcel  = None
        ProtocolsFromDB     = [||]
    }

/// The main MODEL was shifted to 'Messages.fs' to allow saving 'Msg'
