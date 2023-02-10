namespace Spreadsheet

open Shared

type BodyCell = {
    Term: TermTypes.TermMinimal
    /// can only contain unit if header is None
    Unit: TermTypes.TermMinimal option
} with
    static member internal create(v: TermTypes.TermMinimal, ?unit: TermTypes.TermMinimal) = {
        Term = v
        Unit = unit
    }
    static member internal create(termName: string, ?termAccession: string, ?unit: TermTypes.TermMinimal) = {
        Term = TermTypes.TermMinimal.create termName (Option.defaultValue "" termAccession)
        Unit = unit
    }
    static member internal create(termName: string, ?termAccession: string, ?unitName: string, ?unitAccession: string) = {
        Term = TermTypes.TermMinimal.create termName (Option.defaultValue "" termAccession)
        Unit =
            if unitName.IsNone && unitAccession.IsNone then None else
                TermTypes.TermMinimal.create
                    (Option.defaultValue "" unitName)
                    (Option.defaultValue "" unitAccession)
                |> Some
    }

type SwateCell =
| IsBody of BodyCell
| IsHeader of OfficeInteropTypes.SwateColumnHeader
with
    member this.isBody = match this with | IsBody _ -> true | _ -> false
    member this.isHeader = match this with | IsHeader _ -> true | _ -> false
    member this.Body =
        match this with
        | IsBody cc -> cc
        | _ -> failwith "Not a Swate BodyCell."
    member this.Header =
        match this with
        | IsHeader cc -> cc
        | _ -> failwith "Not a ColumnHeader."
    /// This is used to update the string in SwateColumnHeader or the Term.Name in BodyCell.
    member this.updateDisplayValue (v: string) =    
        match this with
        | IsHeader hc   -> IsHeader {hc with SwateColumnHeader = v}
        | IsBody bc     -> IsBody {bc with Term = {bc.Term with Name = v}}
    // Mirror create functions for BodyCell
    static member create(v: TermTypes.TermMinimal, ?unit: TermTypes.TermMinimal) =
        BodyCell.create(v, ?unit = unit) |> IsBody
    static member create(termName: string, ?termAccession: string, ?unit: TermTypes.TermMinimal) =
        BodyCell.create(termName, ?termAccession = termAccession, ?unit = unit) |> IsBody
    static member create(termName: string, ?termAccession: string, ?unitName: string, ?unitAccession: string) =
        BodyCell.create(termName, ?termAccession = termAccession, ?unitName = unitName, ?unitAccession = unitAccession) |> IsBody
    // Mirror create functions for SwateColumnHeader
    static member create(headerString: string, ?term: TermTypes.TermMinimal) = OfficeInteropTypes.SwateColumnHeader.init(headerString, ?term = term) |> IsHeader
    static member create(headerString: OfficeInteropTypes.SwateColumnHeader, ?term: TermTypes.TermMinimal) = OfficeInteropTypes.SwateColumnHeader.init(headerString, ?term = term) |> IsHeader

open OfficeInteropTypes

type SwateBuildingBlock = {
    Index: int
    Header: SwateColumnHeader
    Rows: (int*BodyCell) []
} with
    static member create(index: int, header, rows) = {
        Index = index
        Header = header
        Rows = rows
    }

type SwateTable = {
    Id: System.Guid
    Name: string
    BuildingBlocks: SwateBuildingBlock []
} with
    static member init() = {
        Id = System.Guid.NewGuid()
        Name = "New Table"
        BuildingBlocks = Array.empty
    }
    static member init(buildingblocks: SwateBuildingBlock [], ?name: string) = {
        Id = System.Guid.NewGuid()
        Name = Option.defaultValue "New Table" name
        BuildingBlocks = buildingblocks
    }

open System.Collections.Generic

type Model = {
    ActiveTable: Map<(int*int), SwateCell>
    ActiveTableIndex: int
    Tables: Map<int, SwateTable>
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
| UpdateTable of (int*int) * SwateCell
| UpdateActiveTable of index:int
| RemoveTable of index:int
| RenameTable of index:int * name:string
| CreateAnnotationTable of tryUsePrevOutput:bool