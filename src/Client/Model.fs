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
        | "Debug"| "debug" -> Debug(System.DateTime.UtcNow,message)
        | "Info" | "info" -> Info (System.DateTime.UtcNow,message)
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
    ParentOntology          : OntologyInfo option
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
    static member init () = {
        QuickAcessIconsShown    = false
        BurgerVisible           = false
        IsDarkMode              = false
        ColorMode               = ExcelColors.colorfullMode
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


/// If this is changed, see also OfficeInterop.Types.ColumnCoreNames
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
        | Parameter         ->
            "Use parameters to annotate your experimental workflow. You can group parameters to create a protocol."
        | Factor            ->
            "Use factor columns to track the experimental conditions that govern your study.
            Most of the time, factors are the most important building blocks for downstream computational analysis."
        | Characteristics   ->
            "Use characteristics columns to annotate interesting properties of the source material.
            You can use any number of characteristics columns."
        | Sample            ->
            "The Sample Name column defines the resulting biological material of the annotated workflow.
            The name used must be a unique identifier.
            Samples can again be sources for further experimental workflows."
        | Data              ->
            "The Data column describes data files that results from your experiments.
            Additionally to the type of data, the annotated files must have a unique name.
            Data files can be sources for computational workflows."
        | Source            ->
            "The Source Name column defines the source of biological material used for your experiments.
            The name used must be a unique identifier. It can be an organism, a sample, or both.
            Every annotation table must start with the Source Name column"
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
        | Source            -> "Source Name"
        | _                 -> ""


type AddBuildingBlockState = {
    CurrentBuildingBlock                    : AnnotationBuildingBlock

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

        CurrentBuildingBlock                    = AnnotationBuildingBlock.init AnnotationBuildingBlockType.Parameter
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
    ActiveTableBuildingBlocks   : OfficeInterop.Types.BuildingBlockTypes.BuildingBlock []
    TableValidationScheme       : OfficeInterop.Types.Xml.ValidationTypes.TableValidation
    // Client view related
    DisplayedOptionsId      : int option
} with
    static member init () = {
        ActiveTableBuildingBlocks   = [||]
        TableValidationScheme       = OfficeInterop.Types.Xml.ValidationTypes.TableValidation.init()
        DisplayedOptionsId          = None
    }


open ISADotNet


/// This model is used for both protocol insert and protocol search
type ProtocolInsertState = {
    // Client view
    DisplayedProtDetailsId  : int option

    // Process.json file upload
    UploadData              : string
    ProcessModel            : ISADotNet.Process option

    // Database protocol template
    ProtocolSelected        : Shared.ProtocolTemplate option
    ValidationXml           : OfficeInterop.Types.Xml.ValidationTypes.TableValidation option
    BuildingBlockMinInfoList: OfficeInterop.Types.BuildingBlockTypes.MinimalBuildingBlock list
    ProtocolsAll            : Shared.ProtocolTemplate []

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
        ProcessModel            = None

        // Database protocol templates
        ProtocolSelected        = None
        ValidationXml           = None
        BuildingBlockMinInfoList= []
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
    BuildingBlockValues : Shared.SearchTermI []
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

type Model = {

    ///PageState
    PageState               : PageState

    ///Data that needs to be persistent once loaded
    PersistentStorageState  : PersistentStorageState
 
    ///Debouncing
    DebouncerState          : Debouncer.State

    ///Error handling, Logging, etc.
    DevState                : DevState

    ///Site Meta Options (Styling etc)
    SiteStyleState          : SiteStyleState

    ///States regarding term search
    TermSearchState         : TermSearchState

    AdvancedSearchState     : AdvancedSearchState

    ///Use this in the future to model excel stuff like table data
    ExcelState              : ExcelState

    ///Use this to log Api calls and maybe handle them better
    ApiState                : ApiState

    ///States regarding File picker functionality
    FilePickerState         : FilePickerState

    ProtocolInsertState     : ProtocolInsertState

    ///Insert annotation columns
    AddBuildingBlockState   : AddBuildingBlockState

    ///Create Validation scheme for Table
    ValidationState         : ValidationState

    ///Used to show selected building block information
    BuildingBlockDetailsState   : BuildingBlockDetailsState

    ///Used to manage all custom xml settings
    SettingsXmlState            : SettingsXmlState

    ///Used to manage functions specifically for data stewards
    SettingsDataStewardState    : SettingsDataStewardState
}

let initializeModel (pageOpt: Route option) = {
    DebouncerState              = Debouncer                 .create ()
    PageState                   = PageState                 .init pageOpt
    PersistentStorageState      = PersistentStorageState    .init ()
    DevState                    = DevState                  .init ()
    SiteStyleState              = SiteStyleState            .init ()
    TermSearchState             = TermSearchState           .init ()
    AdvancedSearchState         = AdvancedSearchState       .init ()
    ExcelState                  = ExcelState                .init ()
    ApiState                    = ApiState                  .init ()
    FilePickerState             = FilePickerState           .init ()
    AddBuildingBlockState       = AddBuildingBlockState     .init ()
    ValidationState             = ValidationState           .init ()
    ProtocolInsertState         = ProtocolInsertState       .init ()
    BuildingBlockDetailsState   = BuildingBlockDetailsState .init ()
    SettingsXmlState            = SettingsXmlState          .init ()
    SettingsDataStewardState    = SettingsDataStewardState  .init ()
}
