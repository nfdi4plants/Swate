namespace SpreadsheetInterface

open Shared
open OfficeInteropTypes

///<summary>This type is used to interface between standalone, electron and excel logic and will forward the command to the correct logic.</summary>
type Msg =
| Initialize
| InitializeResponse of Swatehost
| CreateAnnotationTable of tryUsePrevOutput:bool
| RemoveBuildingBlock
| AddAnnotationBlock of InsertBuildingBlock
| AddAnnotationBlocks of InsertBuildingBlock []
| ImportFile of (string*InsertBuildingBlock []) []
/// Open modal for selected building block, allows editing on standalone only.
| EditBuildingBlock
/// Inserts TermMinimal to selected fields of one column
| InsertOntologyTerm of TermTypes.TermMinimal
| InsertFileNames of string list
/// Starts chain to export active table to isa json
| ExportJsonTable
/// Starts chain to export all tables to isa json
| ExportJsonTables
/// Starts chain to parse all tables to DAG
| ParseTablesToDag
| UpdateTermColumns
| UpdateTermColumnsResponse of TermTypes.TermSearchable []