namespace Spreadsheet

open Shared

type Table = {
    Id: System.Guid
    Name: string
    BuildingBlocks: OfficeInteropTypes.BuildingBlock []
} with
    static member init() = {
        Id = System.Guid.NewGuid()
        Name = "New Table"
        BuildingBlocks = Array.empty
    }

open System.Collections.Generic

type Model = {
    ActiveTable: Map<(int*int), TermTypes.TermMinimal>
    ActiveTableIndex: int
    Tables: Map<int, Table>
} with
    static member init() = {
        ActiveTable = Map.empty
        ActiveTableIndex = 0
        Tables = Map.empty
    }
    static member init(data: Dictionary<(int*int), TermTypes.TermMinimal>) = {
        ActiveTable = Map.empty
        ActiveTableIndex = 0
        Tables = Map.empty
    }

type Msg =
//| UpdateActiveTable of string
| UpdateTable of (int*int)*TermTypes.TermMinimal
| UpdateActiveTable of int
| CreateAnnotationTable of tryUsePrevOutput:bool