namespace Spreadsheet

open Shared
open OfficeInteropTypes

type Model = {
    /// Keys: column * row
    ActiveTable: Map<(int*int), SwateCell>
    ActiveTableIndex: int
    Tables: Map<int, SwateTable>
    TableOrder: Map<int, int>
} with
    static member init() =
        {
            ActiveTable = Map.empty
            ActiveTableIndex = 0
            Tables = Map.empty
            TableOrder = Map.empty
        }

type Msg =
// <--> UI <-->
| UpdateTable of (int*int) * SwateCell
| UpdateActiveTable of index:int
| RemoveTable of index:int
| RenameTable of index:int * name:string
| UpdateTableOrder of pre_index:int * new_index:int
| UpdateHistoryPosition of newPosition:int
| AddRows of int
| DeleteRow of int
| DeleteColumn of int
/// This will reset Spreadsheet.Model to Spreadsheet.Model.init() and clear all webstorage.
| Reset
// <--> INTEROP <-->
| CreateAnnotationTable of tryUsePrevOutput:bool
| AddAnnotationBlock of InsertBuildingBlock