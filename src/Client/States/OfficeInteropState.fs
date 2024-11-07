namespace OfficeInterop

open Shared
open ARCtrl
open JsonImport

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
    | UpdateArcFile                         of ArcFiles
    | AnnotationtableCreated
    | TryFindAnnotationTable
    | AnnotationTableExists                 of bool
    | InsertOntologyTerm                    of OntologyAnnotation
    | ValidateBuildingBlock
    | AddAnnotationBlock                    of CompositeColumn
    | AddAnnotationBlocks                   of CompositeColumn [] //* OfficeInterop.Types.Xml.ValidationTypes.TableValidation option
    | AddTemplate                           of ArcTable
    | JoinTable                             of ArcTable * options: TableJoinOptions option
    | RemoveBuildingBlock
    | GetBuildingBlockDetails
    | UpdateUnitForCells
    | AutoFitTable                          of hideRefCols:bool
    // Term search functions
    // table+database interconnected functions
    /// 
    | RectifyTermColumns
    ///
    | UpdateFillHiddenColsState             of FillHiddenColsState
    //
    ///
    | InsertFileNames                       of fileNameList:string list
    | UpdateTopLevelMetadata                of ArcFiles
    | DeleteTopLevelMetadata
    | SendErrorsToFront                     of InteropLogging.Msg list
    | ExportJson                            of ArcFiles * JsonExportFormat
    //| ExcelSubscriptionMsg          of OfficeInterop.Types.Subscription.Msg

