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
/// This will reset Spreadsheet.Model to Spreadsheet.Model.init() and clear all webstorage.
| Reset
// <--> INTEROP <-->
| CreateAnnotationTable of tryUsePrevOutput:bool
| AddAnnotationBlock of InsertBuildingBlock
| InsertOntologyTerm of TermTypes.TermMinimal