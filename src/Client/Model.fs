namespace Model

open Fable.React
open Fable.React.Props
open Shared
open TermTypes
open TemplateTypes
open Thoth.Elmish
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

module TermSearch =

    open ARCtrl.ISA

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

type ApiCallStatus =
    | IsNone
    | Pending
    | Successfull
    | Failed of string

type ApiCallHistoryItem = {
    FunctionName   : string
    Status         : ApiCallStatus
}

type ApiState = {
    currentCall : ApiCallHistoryItem
    callHistory : ApiCallHistoryItem list
} with
    static member init() = {
        currentCall = ApiState.noCall
        callHistory = []
    }
    static member noCall = {
        FunctionName = "None"
        Status = IsNone
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

open OfficeInteropTypes
open Fable.Core

module BuildingBlock =

    open ARCtrl.ISA

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

        // Below everything is more or less deprecated
        // This section is used to add a unit directly to an already existing building block
        Unit2TermSearchText             : string
        Unit2SelectedTerm               : Term option
        Unit2TermSuggestions            : Term []
        HasUnit2TermSuggestionsLoading  : bool
        ShowUnit2TermSuggestions        : bool

    } with
        static member init () = {

            HeaderCellType      = HeaderCellType.Parameter
            HeaderArg           = None
            BodyCellType        = BodyCellType.Term
            BodyArg             = None

            // Below everything is more or less deprecated
            // This section is used to add a unit directly to an already existing building block
            Unit2TermSearchText                     = ""
            Unit2SelectedTerm                       = None
            Unit2TermSuggestions                    = [||]
            ShowUnit2TermSuggestions                = false
            HasUnit2TermSuggestionsLoading          = false
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
    type CommunityFilter =
    | All
    | OnlyCurated
    | OnlyCommunities
    | Community of string

        member this.ToStringRdb() =
            match this with
            | All               -> "All"
            | OnlyCurated       -> "DataPLANT official"
            | OnlyCommunities   -> "All Communities"
            | Community name    -> name

        static member fromString(str:string) =
            match str with
            | "All" -> All
            | "All Curated" -> OnlyCurated
            | "All Community" -> OnlyCommunities
            | anyElse -> Community anyElse

        static member CommunityFromOrganisation(org:ARCtrl.Template.Organisation) =
            match org with
            | ARCtrl.Template.Organisation.DataPLANT -> None
            | ARCtrl.Template.Other name -> Some <| Community name

    /// This model is used for both protocol insert and protocol search
    type Model = {
        // Client 
        Loading                 : bool
        LastUpdated             : System.DateTime option
        // // ------ Process from file ------
        UploadedFileParsed      : (string*InsertBuildingBlock []) []
        // ------ Protocol from Database ------
        TemplateSelected        : ARCtrl.Template.Template option
        Templates               : ARCtrl.Template.Template []
    } with
        static member init () = {
            // Client
            Loading                 = false
            LastUpdated             = None
            TemplateSelected        = None
            // // ------ Process from file ------
            UploadedFileParsed      = [||]
            // ------ Protocol from Database ------
            Templates               = [||]
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

// The main MODEL was shifted to 'Messages.fs' to allow saving 'Msg'
