namespace Spreadsheet

open Shared
open OfficeInteropTypes


///<summary>If you change this model, it will kill caching for users! if you apply changes to it, make sure to keep a version
///of it and add a try case for it to `tryInitFromLocalStorage` in Spreadsheet/LocalStorage.fs .</summary>
type Model = {
    /// Keys: column * row
    ActiveTable: Map<(int*int), SwateCell>
    SelectedCells: Set<int*int>
    ActiveTableIndex: int
    Tables: Map<int, SwateTable>
    TableOrder: Map<int, int>
} with
    static member init() =
        {
            ActiveTable = Map.empty
            SelectedCells = Set.empty
            ActiveTableIndex = 0
            Tables = Map.empty
            TableOrder = Map.empty
        }
    member this.getSelectedColumnHeader =
        if this.SelectedCells.IsEmpty then None else
            let column = this.SelectedCells |> Set.toList |> List.minBy fst |> fst
            let header = this.ActiveTable.[column,0]
            Some header.Header
    member this.headerIsSelected =
        if this.SelectedCells.IsEmpty then false else
            this.SelectedCells |> Set.toList |> List.exists (fun (c,r) -> r = 0)
    member this.getColumn(index:int) =
        this.ActiveTable |> Map.toArray |> Array.choose (fun ((c,r),v) -> if c = index then Some (r,v) else None)

type Msg =
// <--> UI <-->
| UpdateTable of (int*int) * SwateCell
| UpdateActiveTable of index:int
| UpdateSelectedCells of Set<int*int>
| RemoveTable of index:int
| RenameTable of index:int * name:string
| UpdateTableOrder of pre_index:int * new_index:int
| UpdateHistoryPosition of newPosition:int
| AddRows of int
| DeleteRow of int
| DeleteRows of int []
| DeleteColumn of int
| CopySelectedCell
| CutSelectedCell
| PasteSelectedCell
| CopyCell of index:(int*int)
| CutCell of index:(int*int)
| PasteCell of index:(int*int)
| FillColumnWithTerm of index:(int*int)
/// Update column of index to new column type defined by given SwateCell.emptyXXX
| EditColumn of index: int * newType: SwateCell * b_type: BuildingBlockType option
/// This will reset Spreadsheet.Model to Spreadsheet.Model.init() and clear all webstorage.
| Reset
| ParseFileUpload of byte []
// <--> INTEROP <-->
| CreateAnnotationTable of tryUsePrevOutput:bool
| AddAnnotationBlock of InsertBuildingBlock
| AddAnnotationBlocks of InsertBuildingBlock []
| ImportFile of (string*InsertBuildingBlock []) []
| InsertOntologyTerm of TermTypes.TermMinimal
| InsertOntologyTerms of TermTypes.TermMinimal []
| UpdateTermColumns
| UpdateTermColumnsResponse of TermTypes.TermSearchable []
/// Starts chain to export active table to isa json
| ExportJsonTable
/// Starts chain to export all tables to isa json
| ExportJsonTables
/// Starts chain to export all tables to xlsx swate tables.
| ExportXlsx
/// Starts chain to parse all tables to DAG
| ParseTablesToDag
| ExportXlsxServerRequest of (string*BuildingBlock []) []
| ExportXlsxServerResponse of byte []
// <--> Result Messages <-->
/// This message will save `Model` to local storage and to session storage for history
| Success of Model
/// This message will save `Model` to local storage
| SuccessNoHistory of Model