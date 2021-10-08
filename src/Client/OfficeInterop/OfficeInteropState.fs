namespace OfficeInterop

open Shared
open TermTypes
open OfficeInteropTypes
open OfficeInteropTypes

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

type Model = {
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

type Msg =
    | Initialized                           of (string*string)
    | InsertOntologyTerm                    of TermMinimal
    | AddAnnotationBlock                    of InsertBuildingBlock
    | AddAnnotationBlocks                   of InsertBuildingBlock list //* OfficeInterop.Types.Xml.ValidationTypes.TableValidation option
    | RemoveAnnotationBlock
    | UpdateUnitForCells                    of unitTerm:TermMinimal
    | FormatColumn                          of colname:string * formatString:string
    | FormatColumns                         of (string * string) list
    /// This message does not need the active annotation table as `PipeCreateAnnotationTableInfo` checks if any annotationtables exist in the active worksheet, and if so, errors.
    | CreateAnnotationTable                 of isDark:bool
    | AnnotationtableCreated
    | AnnotationTableExists                 of TryFindAnnoTableResult
    | GetParentTerm
    | AutoFitTable
    //
    | GetTableValidationXml
    | WriteTableValidationToXml             of newTableValidation:CustomXmlTypes.Validation.TableValidation * currentSwateVersion:string
    /// needs to set newColNames separately as these validations come from templates for protocol group insert
    //| AddTableValidationtoExisting          of addedTableValidation:Xml.ValidationTypes.TableValidation * newColNames:string list * protocol:CustomXmlTypes.Protocol.Protocol
    //| WriteProtocolToXml                    of newProtocol:Xml.GroupTypes.Protocol
    | DeleteAllCustomXml
    | GetSwateCustomXml
    | UpdateSwateCustomXml                  of string
    //
    | FillHiddenColsRequest
    | FillHiddenColumns                     of TermSearchable []
    | UpdateFillHiddenColsState             of FillHiddenColsState
    //
    | InsertFileNames                       of fileNameList:string list
    // Show Details to selected BuildingBlock
    | GetSelectedBuildingBlockTerms
    //
    | CreatePointerJson
    //
    // Development
    | TryExcel
    | TryExcel2
    //| ExcelSubscriptionMsg          of OfficeInterop.Types.Subscription.Msg

