namespace Model

open Fable.React
open Fable.React.Props
open Shared
open TermTypes
open Feliz
open Routing

type WindowSize =
/// < 575
| Mini
/// > 575
| MobileMini
/// > 768
| Mobile
/// > 1023
| Tablet
/// > 1215
| Desktop
/// > 1407
| Widescreen
with
    member this.threshold =
        match this with
        | Mini -> 0
        | MobileMini -> 575
        | Mobile -> 768
        | Tablet -> 1023
        | Desktop -> 1215
        | Widescreen -> 1407
    static member ofWidth (width:int) =
        match width with
        | _ when width < MobileMini.threshold -> Mini
        | _ when width < Mobile.threshold -> MobileMini
        | _ when width < Tablet.threshold -> Mobile
        | _ when width < Desktop.threshold -> Tablet
        | _ when width < Widescreen.threshold -> Desktop  
        | _ when width >= Widescreen.threshold -> Widescreen
        | anyElse -> failwithf "'%A' triggered an unexpected error when calculating screen size from width." anyElse        

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

    static member private DebugCell = Html.td [prop.style [style.color NFDIColors.LightBlue.Base; style.fontWeight.bold]; prop.text "Debug"]
    static member private InfoCell = Html.td [prop.style [style.color NFDIColors.Mint.Base; style.fontWeight.bold]; prop.text "Info"]
    static member private ErrorCell = Html.td [prop.style [style.color NFDIColors.Red.Base; style.fontWeight.bold]; prop.text "ERROR"]
    static member private WarningCell = Html.td [prop.style [style.color NFDIColors.Yellow.Base; style.fontWeight.bold]; prop.text "Warning"]

    static member toTableRow = function
        | Debug (t,m) ->
            Html.tr [
                Html.td (sprintf "[%s]" (t.ToShortTimeString()))
                LogItem.DebugCell
                Html.td m
            ]
        | Info  (t,m) ->
            Html.tr [
                Html.td (sprintf "[%s]" (t.ToShortTimeString()))
                LogItem.InfoCell
                Html.td m
            ]
        | Error (t,m) ->
            Html.tr [
                Html.td (sprintf "[%s]" (t.ToShortTimeString()))
                LogItem.ErrorCell
                Html.td m
            ]
        | Warning (t,m) ->
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
        SelectedTerm            : OntologyAnnotation option
        ParentTerm              : OntologyAnnotation option

    } with
        static member init () = {
            SelectedTerm                = None
            ParentTerm                  = None
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
            ModalId                             = ""
            HasModalVisible                     = false
            HasOntologyDropdownVisible          = false
            AdvancedSearchOptions               = AdvancedSearchTypes.AdvancedSearchOptions.init ()
            AdvancedSearchTermResults           = [||]
            HasAdvancedSearchResultsLoading     = false
            Subpage                             = InputFormSubpage
        }
        static member BuildingBlockHeaderId = "BuildingBlockHeader_ATS_Id"
        static member BuildingBlockBodyId = "BuildingBlockBody_ATS_Id"

type DevState = {
    Log                                 : LogItem list
    DisplayLogList                      : LogItem list
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
        Host                    = None
        AppVersion              = ""
        ShowSideBar             = false
        HasOntologiesLoaded     = false
    }

type PageState = {
    CurrentPage : Routing.Route
    IsExpert    : bool
} with
    static member init () = 
        {
            CurrentPage = Route.BuildingBlock
            IsExpert = false
        }

module FilePicker =
    type Model = {
        FileNames       : (int*string) list
    } with
        static member init () = {
            FileNames = []
        }

open Fable.Core

module BuildingBlock =

    open ARCtrl

    type [<RequireQualifiedAccess>] HeaderCellType =
    | Component
    | Characteristic
    | Factor
    | Parameter
    | ProtocolType
    | ProtocolDescription
    | ProtocolUri
    | ProtocolVersion
    | ProtocolREF
    | Performer
    | Date
    | Input
    | Output
    with
        /// <summary>
        /// Returns true if the Building Block is a term column
        /// </summary>
        member this.IsTermColumn() =
            match this with
            | Component
            | Characteristic
            | Factor
            | Parameter 
            | ProtocolType -> true
            | _ -> false
        member this.HasOA() =
            match this with
            | Component
            | Characteristic
            | Factor
            | Parameter -> true
            | _ -> false

        member this.HasIOType() =
            match this with
            | Input 
            | Output -> true
            | _ -> false

    type [<RequireQualifiedAccess>] BodyCellType =
    | Term
    | Unitized
    | Text

    [<RequireQualifiedAccess>]
    type DropdownPage =
    | Main
    | More
    | IOTypes of HeaderCellType

        member this.toString =
            match this with
            | Main -> "Main Page"
            | More -> "More"
            | IOTypes (t) -> t.ToString()

        member this.toTooltip =
            match this with
            | More -> "More"
            | IOTypes (t) -> $"Per table only one {t} is allowed. The value of this column must be a unique identifier."
            | _ -> ""

    type BuildingBlockUIState = {
        DropdownIsActive    : bool
        DropdownPage        : DropdownPage
    } with
        static member init() = {
            DropdownIsActive    = false
            DropdownPage        = DropdownPage.Main
        }

    type Model = {

        HeaderCellType  : HeaderCellType
        HeaderArg       : U2<OntologyAnnotation,IOType> option
        BodyCellType    : BodyCellType
        BodyArg         : U2<string, OntologyAnnotation> option

    } with
        static member init () = {

            HeaderCellType      = HeaderCellType.Parameter
            HeaderArg           = None
            BodyCellType        = BodyCellType.Term
            BodyArg             = None
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

        static member fromString(str:string) =
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
        Loading                 : bool
        LastUpdated             : System.DateTime option
        // ------ Protocol from Database ------
        TemplateSelected        : ARCtrl.Template option
        Templates               : ARCtrl.Template []
    } with
        static member init () = {
            // Client
            Loading                 = false
            LastUpdated             = None
            TemplateSelected        = None
            // ------ Protocol from Database ------
            Templates               = [||]
        }

module DataAnnotator =

    type DataFile = {
        DataFileName: string
        DataFileType: string
        DataContent: string
        DataSize: float
    } with
        static member create(dfn, dft, dc, ds) = {
            DataFileName = dfn
            DataFileType = dft
            DataContent = dc
            DataSize = ds
        }

    type Model =
        {
            DataFile: DataFile option
        }
        static member init () = {
            DataFile = None
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
    BuildingBlockValues : TermSearchable []
} with
    static member init () = {
        CurrentRequestState = Inactive
        BuildingBlockValues = [||]
    }


type Model = {
    ///PageState
    PageState                   : PageState
    ///Data that needs to be persistent once loaded
    PersistentStorageState      : PersistentStorageState
    ///Error handling, Logging, etc.
    DevState                    : DevState
    ///States regarding term search
    TermSearchState             : TermSearch.Model
    ///Use this in the future to model excel stuff like table data
    ExcelState                  : OfficeInterop.Model
    ///States regarding File picker functionality
    FilePickerState             : FilePicker.Model
    ProtocolState               : Protocol.Model
    ///Insert annotation columns
    AddBuildingBlockState       : BuildingBlock.Model
    ///Used to show selected building block information
    BuildingBlockDetailsState   : BuildingBlockDetailsState
    CytoscapeModel              : Cytoscape.Model
    ///
    DataAnnotatorModel          : DataAnnotator.Model
    /// Contains all information about spreadsheet view
    SpreadsheetModel            : Spreadsheet.Model
    History                     : LocalHistory.Model
}