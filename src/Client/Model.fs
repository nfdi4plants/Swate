namespace Model

open Shared
open Feliz
open Routing
open Database

type LogItem =
    | Debug     of (System.DateTime*string)
    | Info      of (System.DateTime*string)
    | Error     of (System.DateTime*string)
    | Warning   of (System.DateTime*string)

    static member ofInteropLogginMsg (msg:InteropLogging.Msg) =
        match msg.LogIdentifier with
        | InteropLogging.Info   -> Info (System.DateTime.UtcNow,msg.MessageTxt)
        | InteropLogging.Debug  -> Debug(System.DateTime.UtcNow,msg.MessageTxt)
        | InteropLogging.Error  -> Error(System.DateTime.UtcNow,msg.MessageTxt)
        | InteropLogging.Warning -> Warning(System.DateTime.UtcNow,msg.MessageTxt)

    static member private DebugCell = Html.td [prop.className "bg-info text-info-content font-semibold"; prop.text "Debug"]
    static member private InfoCell = Html.td [prop.className "bg-primary text-primary-content font-semibold"; prop.text "Info"]
    static member private ErrorCell = Html.td [prop.className "bg-error text-error-content font-semibold"; prop.text "ERROR"]
    static member private WarningCell = Html.td [prop.className "bg-warning text-warning-content font-semibold"; prop.text "Warning"]

    static member toTableRow = function
        | Debug (t, m) ->
            Html.tr [
                Html.td (sprintf "[%s]" (t.ToShortTimeString()))
                LogItem.DebugCell
                Html.td m
            ]
        | Info  (t, m) ->
            Html.tr [
                Html.td (sprintf "[%s]" (t.ToShortTimeString()))
                LogItem.InfoCell
                Html.td m
            ]
        | Error (t, m) ->
            Html.tr [
                Html.td (sprintf "[%s]" (t.ToShortTimeString()))
                LogItem.ErrorCell
                Html.td m
            ]
        | Warning (t, m) ->
            Html.tr [
                Html.td (sprintf "[%s]" (t.ToShortTimeString()))
                LogItem.WarningCell
                Html.td m
            ]

    static member ofStringNow (level:string) (message: string) =
        match level with
        | "Debug"| "debug"  -> Debug(System.DateTime.UtcNow,message)
        | "Info" | "info"   -> Info (System.DateTime.UtcNow,message)
        | "Error" | "error" -> Error(System.DateTime.UtcNow,message)
        | "Warning" | "warning" -> Warning(System.DateTime.UtcNow,message)
        | others -> Error(System.DateTime.UtcNow,sprintf "Swate found an unexpected log identifier: %s" others)

module TermSearch =

    open ARCtrl

    type Model = {
        SelectedTerm    : OntologyAnnotation option
        ParentTerm      : OntologyAnnotation option

    } with
        static member init () = {
            SelectedTerm    = None
            ParentTerm      = None
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
        Subpage           : AdvancedSearchSubpages
        HasModalVisible                     : bool
        HasOntologyDropdownVisible          : bool
        HasAdvancedSearchResultsLoading     : bool
    } with
        static member init () = {
            ModalId                         = ""
            HasModalVisible                 = false
            HasOntologyDropdownVisible      = false
            AdvancedSearchOptions           = AdvancedSearchTypes.AdvancedSearchOptions.init ()
            AdvancedSearchTermResults       = [||]
            HasAdvancedSearchResultsLoading = false
            Subpage                         = InputFormSubpage
        }
        static member BuildingBlockHeaderId = "BuildingBlockHeader_ATS_Id"
        static member BuildingBlockBodyId = "BuildingBlockBody_ATS_Id"

type DevState = {
    Log             : LogItem list
    DisplayLogList  : LogItem list
} with
    static member init () = {
        DisplayLogList  = []
        Log             = []
    }

type PersistentStorageState = {
    SearchableOntologies    : (Set<string>*Ontology) []
    AppVersion              : string
    Host                    : Swatehost option
    ShowSideBar             : bool
    HasOntologiesLoaded     : bool
} with
    static member init () = {
        SearchableOntologies    = [||]
        Host                    = Some Swatehost.Browser
        AppVersion              = ""
        ShowSideBar             = false
        HasOntologiesLoaded     = false
    }

type PageState = {
    SidebarPage : Routing.SidebarPage
    MainPage: Routing.MainPage
} with
    static member init () =
        {
            SidebarPage = SidebarPage.BuildingBlock
            MainPage = MainPage.Default
        }
    member this.IsHome =
        match this.MainPage with
        | MainPage.Default  -> true
        | _                 -> false

module FilePicker =
    type Model = {
        FileNames   : (int*string) list
    } with
        static member init () = {
            FileNames = []
        }

open Fable.Core

module BuildingBlock =

    open ARCtrl

    [<RequireQualifiedAccess>]
    type DropdownPage =
    | Main
    | More
    | IOTypes of CompositeHeaderDiscriminate

        member this.toString =
            match this with
            | Main      -> "Main Page"
            | More      -> "More"
            | IOTypes t -> t.ToString()

        member this.toTooltip =
            match this with
            | More      -> "More"
            | IOTypes t -> $"Per table only one {t} is allowed. The value of this column must be a unique identifier."
            | _         -> ""

    type BuildingBlockUIState = {
        DropdownIsActive    : bool
        DropdownPage        : DropdownPage
    } with
        static member init() = {
            DropdownIsActive    = false
            DropdownPage        = DropdownPage.Main
        }

    type Model = {

        HeaderCellType  : CompositeHeaderDiscriminate
        HeaderArg       : U2<OntologyAnnotation,IOType> option
        BodyCellType    : CompositeCellDiscriminate
        BodyArg         : U2<string, OntologyAnnotation> option

    } with
        static member init () = {

            HeaderCellType  = CompositeHeaderDiscriminate.Parameter
            HeaderArg       = None
            BodyCellType    = CompositeCellDiscriminate.Term
            BodyArg         = None
        }

        member this.TryHeaderOA() =
            match this.HeaderArg with
                | Some (U2.Case1 oa) -> Some oa
                | _ -> None

        member this.TryHeaderIO() =
            match this.HeaderArg with
                | Some (U2.Case2 io) -> Some io
                | _ -> None

        member this.TryBodyOA() =
            match this.BodyArg with
                | Some (U2.Case2 oa) -> Some oa
                | _ -> None

        member this.TryBodyString() =
            match this.BodyArg with
                | Some (U2.Case1 s) -> Some s
                | _ -> None

module Protocol =

    [<RequireQualifiedAccess>]
    type CommunityFilter =
    | All
    | OnlyCurated
    | Community of string
        member this.ToStringRdb() =
            match this with
            | All               -> "All"
            | OnlyCurated       -> "DataPLANT official"
            | Community name    -> name

        static member fromString(str: string) =
            match str with
            | "All" -> All
            | "DataPLANT official" -> OnlyCurated
            | anyElse -> Community anyElse

        static member CommunityFromOrganisation(org: ARCtrl.Organisation) =
            match org with
            | ARCtrl.Organisation.DataPLANT -> None
            | ARCtrl.Organisation.Other name -> Some <| Community name

    /// This model is used for both protocol insert and protocol search
    type Model = {
        // Client
        Loading             : bool
        LastUpdated         : System.DateTime option
        // ------ Protocol from Database ------
        TemplateSelected    : ARCtrl.Template option
        TemplatesSelected   : ARCtrl.Template list
        Templates           : ARCtrl.Template []
    } with
        static member init () = {
            // Client
            Loading             = false
            LastUpdated         = None
            TemplateSelected    = None
            TemplatesSelected   = []
            // ------ Protocol from Database ------
            Templates           = [||]
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

type Model = {
    ///PageState
    PageState               : PageState
    ///Data that needs to be persistent once loaded
    PersistentStorageState  : PersistentStorageState
    ///Error handling, Logging, etc.
    DevState                : DevState
    ///States regarding term search
    TermSearchState         : TermSearch.Model
    ///Use this in the future to model excel stuff like table data
    ExcelState              : OfficeInterop.Model
    ///States regarding File picker functionality
    FilePickerState         : FilePicker.Model
    ProtocolState           : Protocol.Model
    ///Insert annotation columns
    AddBuildingBlockState   : BuildingBlock.Model
    CytoscapeModel          : Cytoscape.Model
    ///
    DataAnnotatorModel      : DataAnnotator.Model
    /// Contains all information about spreadsheet view
    SpreadsheetModel        : Spreadsheet.Model
    History                 : LocalHistory.Model
    ModalState              : ModalState
}