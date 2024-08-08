namespace SpreadsheetInterface

open Shared
open TermTypes

open ARCtrl

///<summary>This type is used to interface between standalone, electron and excel logic and will forward the command to the correct logic.</summary>
type Msg =
| Initialize            of Swatehost
| CreateAnnotationTable of tryUsePrevOutput:bool
| RemoveBuildingBlock
| UpdateDatamap of DataMap option
| UpdateDataMapDataContextAt of index: int * DataContext
| AddTable of ArcTable
| AddAnnotationBlock of CompositeColumn
| AddAnnotationBlocks of CompositeColumn []
| AddDataAnnotation of {| fragmentSelectors: string []; fileName: string; fileType: string; targetColumn: DataAnnotator.TargetColumn |}
/// This function will do preprocessing on the table to join
| AddTemplate           of ArcTable
| JoinTable             of ArcTable * columnIndex: int option * options: TableJoinOptions option
| UpdateArcFile         of ArcFiles
/// Open modal for selected building block, allows editing on standalone only.
| EditBuildingBlock
/// Inserts TermMinimal to selected fields of one column
| InsertOntologyAnnotation of OntologyAnnotation
| InsertFileNames of string list
| ImportXlsx of byte []
/// Starts chain to export active table to isa json
| ExportJson of ArcFiles * JsonExportFormat
| UpdateUnitForCells
| UpdateTermColumns
| UpdateTermColumnsResponse of TermTypes.TermSearchable []