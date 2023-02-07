namespace Spreadsheet

open Shared

type Model = {
    ActiveTable: Map<(int*int), TermTypes.Term>
    //ActiveTableName: string
    //Tables: Map<string, OfficeInteropTypes.BuildingBlock []>
} with
    static member init() = {
        ActiveTable = Map.empty
        //ActiveTableName = ""
        //Tables = Map.empty
    }
    static member init(data: Map<(int*int), TermTypes.Term>) = {
        ActiveTable = data
        //ActiveTableName = ""
        //Tables = Map.empty
    }

type Msg =
| UpdateActiveTable of string
| UpdateTable of (int*int)*TermTypes.Term