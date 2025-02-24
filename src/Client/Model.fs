namespace Model

open Swate.Components.Shared
open Feliz
open Routing
open Database

type LogItem =
    | Debug     of (System.DateTime*string)
    | Info      of (System.DateTime*string)
    | Error     of (System.DateTime*string)
    | Warning   of (System.DateTime*string)

    static member ofInteropLogginMsg (msg: InteropLogging.Msg) =
        match msg.LogIdentifier with
        | InteropLogging.Info       -> Info (System.DateTime.UtcNow,msg.MessageTxt)
        | InteropLogging.Debug      -> Debug(System.DateTime.UtcNow,msg.MessageTxt)
        | InteropLogging.Error      -> Error(System.DateTime.UtcNow,msg.MessageTxt)
        | InteropLogging.Warning    -> Warning(System.DateTime.UtcNow,msg.MessageTxt)

    static member private DebugCell     = Html.td [prop.className "bg-info text-info-content font-semibold"; prop.text "Debug"]
    static member private InfoCell      = Html.td [prop.className "bg-primary text-primary-content font-semibold"; prop.text "Info"]
    static member private ErrorCell     = Html.td [prop.className "bg-error text-error-content font-semibold"; prop.text "ERROR"]
    static member private WarningCell   = Html.td [prop.className "bg-warning text-warning-content font-semibold"; prop.text "Warning"]

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

    static member ofStringNow (level: string) (message: string) =
        match level with
        | "Debug"| "debug"      -> Debug(System.DateTime.UtcNow,message)
        | "Info" | "info"       -> Info (System.DateTime.UtcNow,message)
        | "Error" | "error"     -> Error(System.DateTime.UtcNow,message)
        | "Warning" | "warning" -> Warning(System.DateTime.UtcNow,message)
        | others                -> Error(System.DateTime.UtcNow,sprintf "Swate found an unexpected log identifier: %s" others)

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

type DevState = {
    Log             : LogItem list
    DisplayLogList  : LogItem list
} with
    static member init () = {
        DisplayLogList  = []
        Log             = []
    }

type PersistentStorageState = {
    AppVersion          : string
    Host                : Swatehost option
    SwateDefaultSearch  : bool
    TIBSearchCatalogues : Set<string>
    Autosave            : bool
} with
    static member init () = {
        Host                = Some Swatehost.Browser
        AppVersion          = ""
        SwateDefaultSearch  = true
        TIBSearchCatalogues = Set.empty
        Autosave            = true
    }

    member this.IsARCitect =
        match this.Host with
        | Some Swatehost.ARCitect -> true
        | _ -> false

    member this.TIBQueries =
        {|
            TermSearch = ResizeArray [
                for c in this.TIBSearchCatalogues do
                    let n = "TIB_" + c
                    let query: Swate.Components.SearchCall = fun (q: string) -> Swate.Components.Api.TIBApi.defaultSearch(q, 10, c)
                    yield (n, query)
            ];
            ParentSearch = ResizeArray [
                for c in this.TIBSearchCatalogues do
                    let n = "TIB_" + c
                    let query: Swate.Components.ParentSearchCall = fun (parent: string, query: string) -> Swate.Components.Api.TIBApi.searchChildrenOf(query, parent, 10, c)
                    yield (n, query)
            ];
            AllChildrenSearch = ResizeArray [
                for c in this.TIBSearchCatalogues do
                    let n = "TIB_" + c
                    let query: Swate.Components.AllChildrenSearchCall = fun (p: string) -> Swate.Components.Api.TIBApi.searchAllChildrenOf(p, 300, collection = c)
                    yield (n, query)
            ]
        |}

    member this.IsDisabledSwateDefaultSearch = not this.SwateDefaultSearch

type PageState = {
    SidebarPage : Routing.SidebarPage
    MainPage: Routing.MainPage
    ShowSideBar: bool
} with
    static member init () =
        {
            SidebarPage = SidebarPage.BuildingBlock
            MainPage    = MainPage.Default
            ShowSideBar = false
        }

    member this.IsHome =
        match this.MainPage with
        | MainPage.Default  -> true
        | _                 -> false

module FilePicker =

    type Model = {
        FileNames : (int*string) list
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
        CommentHeader   : string
    } with
        static member init () = {
            HeaderCellType  = CompositeHeaderDiscriminate.Parameter
            HeaderArg       = None
            BodyCellType    = CompositeCellDiscriminate.Term
            BodyArg         = None
            CommentHeader   = ""
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
            | "All"                 -> All
            | "DataPLANT official"  -> OnlyCurated
            | anyElse               -> Community anyElse

        static member CommunityFromOrganisation(org: ARCtrl.Organisation) =
            match org with
            | ARCtrl.Organisation.DataPLANT  -> None
            | ARCtrl.Organisation.Other name -> Some <| Community name

    /// This model is used for both protocol insert and protocol search
    type Model = {
        // Client
        Loading             : bool
        LastUpdated         : System.DateTime option
        // ------ Protocol from Database ------
        TemplatesSelected   : ARCtrl.Template list
        Templates           : ARCtrl.Template []
    } with
        static member init () = {
            // Client
            Loading             = false
            LastUpdated         = None
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
        | RequestDataBaseInformation    -> "Search Database"

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
    ARCitectState           : ARCitect.Model
    ModalState              : ModalState
}