module Model

open Fable.React
open Fable.React.Props
open Fulma
open Shared
open TermTypes
open TemplateTypes
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
    | Warning of (System.DateTime*string)

    static member ofInteropLogginMsg (msg:InteropLogging.Msg) =
        match msg.LogIdentifier with
        | InteropLogging.Info   -> Info (System.DateTime.UtcNow,msg.MessageTxt)
        | InteropLogging.Debug  -> Debug(System.DateTime.UtcNow,msg.MessageTxt)
        | InteropLogging.Error  -> Error(System.DateTime.UtcNow,msg.MessageTxt)
        | InteropLogging.Warning -> Warning(System.DateTime.UtcNow,msg.MessageTxt)

    static member toTableRow = function
        | Debug (t,m) ->
            tr [] [
                td [] [str (sprintf "[%s]" (t.ToShortTimeString()))]
                td [Style [Color NFDIColors.LightBlue.Base; FontWeight "bold"]] [str "Debug"]
                td [] [str m]
            ]
        | Info  (t,m) ->
            tr [] [
                td [] [str (sprintf "[%s]" (t.ToShortTimeString()))]
                td [Style [Color NFDIColors.Mint.Base; FontWeight "bold"]] [str "Info"]
                td [] [str m]
            ]
        | Error (t,m) ->
            tr [] [
                td [] [str (sprintf "[%s]" (t.ToShortTimeString()))]
                td [Style [Color NFDIColors.Red.Base; FontWeight "bold"]] [str "ERROR"]
                td [] [str m]
            ]
        | Warning (t,m) ->
            tr [] [
                td [] [str (sprintf "[%s]" (t.ToShortTimeString()))]
                td [Style [Color NFDIColors.Yellow.Base; FontWeight "bold"]] [str "Warning"]
                td [] [str m]
            ]

    static member ofStringNow (level:string) (message: string) =
        match level with
        | "Debug"| "debug"  -> Debug(System.DateTime.UtcNow,message)
        | "Info" | "info"   -> Info (System.DateTime.UtcNow,message)
        | "Error" | "error" -> Error(System.DateTime.UtcNow,message)
        | "Warning" | "warning" -> Warning(System.DateTime.UtcNow,message)
        | others -> Error(System.DateTime.UtcNow,sprintf "Swate found an unexpected log identifier: %s" others)

type TermSearchMode =
    | Simple
    | Advanced

module TermSearch =

    type Model = {

        TermSearchText          : string

        SelectedTerm            : Term option
        TermSuggestions         : Term []

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

module AdvancedSearch =

    type AdvancedSearchSubpages =
    | InputFormSubpage
    | ResultsSubpage

    type Model = {
        ModalId                             : string
        ///
        AdvancedSearchOptions               : AdvancedSearchTypes.AdvancedSearchOptions
        AdvancedSearchTermResults           : Term []
        // Client visual design
        AdvancedTermSearchSubpage           : AdvancedSearchSubpages
        HasModalVisible                     : bool
        HasOntologyDropdownVisible          : bool
        HasAdvancedSearchResultsLoading     : bool
    } with
        static member init () = {
            ModalId                             = ""
            HasModalVisible                     = false
            HasOntologyDropdownVisible          = false
            AdvancedSearchOptions               = AdvancedSearchTypes.AdvancedSearchOptions.init ()
            AdvancedSearchTermResults           = [||]
            HasAdvancedSearchResultsLoading     = false
            AdvancedTermSearchSubpage           = InputFormSubpage
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
    DisplayLogList                      : LogItem list
} with
    static member init () = {
        LastFullError   = None
        DisplayLogList  = []
        Log             = []
    }

type PersistentStorageState = {
    SearchableOntologies    : (Set<string>*Ontology) []
    AppVersion              : string
    HasOntologiesLoaded     : bool
    PageEntry               : SwateEntry
} with
    static member init (?pageEntry:SwateEntry) = {
        SearchableOntologies    = [||]
        AppVersion              = ""
        HasOntologiesLoaded     = false
        PageEntry               = if pageEntry.IsSome then pageEntry.Value else SwateEntry.Core
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

module FilePicker =
    type Model = {
        FileNames       : (int*string) list
    } with
        static member init () = {
            FileNames = []
        }

open OfficeInteropTypes

module BuildingBlock =

    [<RequireQualifiedAccess>]
    type DropdownPage =
    | Main
    | ProtocolTypes
    | Output

        member this.toString =
            match this with
            | Main -> "Main Page"
            | ProtocolTypes -> "Protocol Columns"
            | Output -> "Output Columns"

        member this.toTooltip =
            match this with
            | ProtocolTypes -> "Protocol columns extend control for protocol parsing."
            | Output -> "Output columns allow to specify the exact output for your table. Per table only one output column is allowed. The value of this column must be a unique identifier."
            | _ -> ""

    type Model = {
        CurrentBuildingBlock                    : BuildingBlockNamePrePrint

        DropdownPage                            : DropdownPage

        BuildingBlockSelectedTerm               : Term option
        BuildingBlockNameSuggestions            : Term []
        ShowBuildingBlockSelection              : bool
        BuildingBlockHasUnit                    : bool
        ShowBuildingBlockTermSuggestions        : bool
        HasBuildingBlockTermSuggestionsLoading  : bool

        // This section is used to add a unit directly to a freshly created building block.
        UnitTermSearchText                      : string
        UnitSelectedTerm                        : Term option
        UnitTermSuggestions                     : Term []
        HasUnitTermSuggestionsLoading           : bool
        ShowUnitTermSuggestions                 : bool

        // This section is used to add a unit directly to an already existing building block
        Unit2TermSearchText                     : string
        Unit2SelectedTerm                       : Term option
        Unit2TermSuggestions                    : Term []
        HasUnit2TermSuggestionsLoading          : bool
        ShowUnit2TermSuggestions                : bool

    } with
        static member init () = {
            ShowBuildingBlockSelection              = false

            DropdownPage                            = DropdownPage.Main

            CurrentBuildingBlock                    = BuildingBlockNamePrePrint.init BuildingBlockType.Parameter
            BuildingBlockSelectedTerm               = None
            BuildingBlockNameSuggestions            = [||]
            ShowBuildingBlockTermSuggestions        = false
            HasBuildingBlockTermSuggestionsLoading  = false
            BuildingBlockHasUnit                    = false

            // This section is used to add a unit directly to a freshly created building block.
            UnitTermSearchText                      = ""
            UnitSelectedTerm                        = None
            UnitTermSuggestions                     = [||]
            ShowUnitTermSuggestions                 = false
            HasUnitTermSuggestionsLoading           = false

            // This section is used to add a unit directly to an already existing building block
            Unit2TermSearchText                     = ""
            Unit2SelectedTerm                       = None
            Unit2TermSuggestions                    = [||]
            ShowUnit2TermSuggestions                = false
            HasUnit2TermSuggestionsLoading          = false
        }

/// Validation scheme for Table
module Validation =
    type Model = {
        ActiveTableBuildingBlocks   : BuildingBlock []
        TableValidationScheme       : OfficeInterop.CustomXmlTypes.Validation.TableValidation
        // Client view related
        DisplayedOptionsId      : int option
    } with
        static member init () = {
            ActiveTableBuildingBlocks   = [||]
            TableValidationScheme       = OfficeInterop.CustomXmlTypes.Validation.TableValidation.init()
            DisplayedOptionsId          = None
        }


module Protocol =

    [<RequireQualifiedAccess>]
    type CuratedCommunityFilter =
    | Both
    | OnlyCurated
    | OnlyCommunity

    /// This model is used for both protocol insert and protocol search
    type Model = {
        // Client 
        Loading                 : bool
        // // ------ Process from file ------
        UploadedFile            : string
        ShowJsonTypeDropdown    : bool
        JsonExportType          : Shared.JsonExportType
        // ------ Protocol from Database ------
        ProtocolSelected        : Template option
        ValidationXml           : obj option //OfficeInterop.Types.Xml.ValidationTypes.TableValidation option
        ProtocolsAll            : Template []
    } with
        static member init () = {
            // Client
            Loading                 = false
            ProtocolSelected        = None
            // // ------ Process from file ------
            UploadedFile            = ""
            ShowJsonTypeDropdown    = false
            JsonExportType          = Shared.JsonExportType.Assay
            // ------ Protocol from Database ------
            ProtocolsAll            = [||]
            ValidationXml           = None
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

module SettingsXml =
    type Model = {
        // // Client // //
        // Validation xml
        ActiveSwateValidation                   : obj option //OfficeInterop.Types.Xml.ValidationTypes.TableValidation option
        NextAnnotationTableForActiveValidation  : string option
        // Protocol group xml
        ActiveProtocolGroup                     : obj option //OfficeInterop.Types.Xml.GroupTypes.ProtocolGroup option
        NextAnnotationTableForActiveProtGroup   : string option
        // Protocol
        ActiveProtocol                          : obj option //OfficeInterop.Types.Xml.GroupTypes.Protocol option
        NextAnnotationTableForActiveProtocol    : string option
        //
        RawXml                                  : string option
        NextRawXml                              : string option
        FoundTables                             : string []
        ValidationXmls                          : obj [] //OfficeInterop.Types.Xml.ValidationTypes.TableValidation []
    } with
        static member init () = {
            // Client
            ActiveSwateValidation                   = None
            NextAnnotationTableForActiveValidation  = None
            ActiveProtocolGroup                     = None
            NextAnnotationTableForActiveProtGroup   = None
            ActiveProtocol                          = None
            // Unused
            NextAnnotationTableForActiveProtocol    = None
            //
            RawXml                                  = None
            NextRawXml                              = None
            FoundTables                             = [||]
            ValidationXmls                          = [||]
        }

type SettingsDataStewardState = {
    PointerJson : string option
} with
    static member init () = {
        PointerJson = None
    }

// The main MODEL was shifted to 'Messages.fs' to allow saving 'Msg'
