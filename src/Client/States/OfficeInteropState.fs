namespace OfficeInterop

open Shared
open ARCtrl
open TermTypes
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
    HasAnnotationTable          : bool
    FillHiddenColsStateStore    : FillHiddenColsState
} with
    static member init () = {
        HasAnnotationTable  = false
        FillHiddenColsStateStore = Inactive
    }

type Msg =
    // create and update table element functions
    | CreateAnnotationTable                 of tryUsePrevOutput:bool
    | ValidateAnnotationTable
    | AnnotationtableCreated
    | TryFindAnnotationTable
    | AnnotationTableExists                 of TryFindAnnoTableResult
    | InsertOntologyTerm                    of TermMinimal
    | ValidateBuildingBlock
    | AddAnnotationBlock                    of CompositeColumn
    | AddAnnotationBlocks                   of CompositeColumn [] //* OfficeInterop.Types.Xml.ValidationTypes.TableValidation option
    | ImportFile                            of (string*InsertBuildingBlock []) []
    | AddTemplate                           of ArcTable
    | JoinTable                             of ArcTable * index: int option * options: TableJoinOptions option
    | RemoveBuildingBlock
    | UpdateUnitForCells
    | AutoFitTable                          of hideRefCols:bool
    // Term search functions
    | GetParentTerm
    // table+database interconnected functions
    /// 
    | RectifyTermColumns
    ///
    | FillHiddenColumns                     of TermSearchable []
    ///
    | UpdateFillHiddenColsState             of FillHiddenColsState
    /// Show Details to selected BuildingBlock
    | GetSelectedBuildingBlockTerms
    //
    ///
    | InsertFileNames                       of fileNameList:string list
    | CreateTopLevelMetadata                of ArcFilesDiscriminate
    | UpdateTopLevelAssay                   of ArcAssay option
    | UpdateTopLevelInvestigation           of ArcInvestigation option
    | UpdateTopLevelStudy                   of (ArcStudy * ArcAssay list) option
    | UpdateTopLevelTemplate                of Template option
    | DeleteTopLevelMetadata                of string option
    // Development
    | TryExcel
    | TryExcel2
    //| ExcelSubscriptionMsg          of OfficeInterop.Types.Subscription.Msg

