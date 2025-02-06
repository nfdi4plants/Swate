namespace OfficeInterop

open Swate.Components.Shared
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
    FillHiddenColsStateStore    : FillHiddenColsState
} with
    static member init () = {
        FillHiddenColsStateStore = Inactive
    }

type Msg =
    // create and update table element functions
    | CreateAnnotationTable                 of tryUsePrevOutput:bool
    | UpdateArcFile                         of ArcFiles
    | InsertOntologyTerm                    of OntologyAnnotation
    | ValidateBuildingBlock
    | AddAnnotationBlock                    of CompositeColumn
    | AddAnnotationBlocks                   of CompositeColumn [] //* OfficeInterop.Types.Xml.ValidationTypes.TableValidation option
    | AddTemplate                           of ArcTable * int list * SelectiveImportModalState * string option
    | AddTemplates                          of ArcTable[] * Set<int*int> * SelectiveImportModalState
    | JoinTable                             of ArcTable * options: TableJoinOptions option
    | RemoveBuildingBlock
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

