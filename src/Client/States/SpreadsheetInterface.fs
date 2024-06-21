namespace SpreadsheetInterface

open Shared
open OfficeInteropTypes

open ARCtrl

///<summary>This type is used to interface between standalone, electron and excel logic and will forward the command to the correct logic.</summary>
type Msg =
| Initialize of Swatehost
| CreateAnnotationTable of tryUsePrevOutput:bool
| RemoveBuildingBlock
| AddAnnotationBlock of CompositeColumn
| AddAnnotationBlocks of CompositeColumn []
| JoinTable of ArcTable * index: int option * options: TableJoinOptions option
| UpdateArcFile of ArcFiles
/// Open modal for selected building block, allows editing on standalone only.
| EditBuildingBlock
/// Inserts TermMinimal to selected fields of one column
| InsertOntologyAnnotation of OntologyAnnotation
| InsertFileNames of string list
| ImportXlsx of byte []
/// Starts chain to export active table to isa json
| ExportJson of ArcFiles * JsonExportFormat
| UpdateTermColumns
| UpdateTermColumnsResponse of TermTypes.TermSearchable []