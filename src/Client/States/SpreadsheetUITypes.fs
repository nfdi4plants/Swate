namespace Spreadsheet

open Shared
open TermTypes

//type BodyCell = {
//    Term: TermMinimal
//    /// can only contain unit if header is None
//    Unit: TermMinimal option
//} with
//    static member internal create(v: TermMinimal, ?unit: TermMinimal) = {
//        Term = v
//        Unit = unit
//    }
//    static member internal create(termName: string, ?termAccession: string, ?unit: TermMinimal) = {
//        Term = TermTypes.TermMinimal.create termName (Option.defaultValue "" termAccession)
//        Unit = unit
//    }
//    static member internal create(termName: string, ?termAccession: string, ?unitName: string, ?unitAccession: string) = {
//        Term = TermTypes.TermMinimal.create termName (Option.defaultValue "" termAccession)
//        Unit =
//            if unitName.IsNone && unitAccession.IsNone then None else
//                TermTypes.TermMinimal.create
//                    (Option.defaultValue "" unitName)
//                    (Option.defaultValue "" unitAccession)
//                |> Some
//    }

type UnitCell = {
    Value: string
    Unit: TermMinimal
} with
    static member create(v: string, ?unit: TermMinimal) = {
        Value = v
        Unit = Option.defaultValue TermMinimal.empty unit
    }
    /// <summary>Used to create unitcell uid = unit term accession</summary>
    static member create(v: string, ?name: string, ?uid: string) = {
        Value = v
        Unit = TermMinimal.create (Option.defaultValue "" name) (Option.defaultValue "" uid)
    }
    static member empty = {
        Value = ""
        Unit = TermMinimal.empty
    }

type FreetextCell = {
    Value: string
} with
    static member create(v: string) : FreetextCell = {
        Value = v
    }
    static member empty : FreetextCell = {
        Value = ""
    }

type TermCell = {
    Term: TermTypes.TermMinimal
} with
    static member create(term: TermMinimal) : TermCell = {
        Term = term
    }
    /// <summary>Used to create unitcell uid = unit term accession</summary>
    static member create(?name: string, ?uid: string) : TermCell= {
        Term = TermMinimal.create (Option.defaultValue "" name) (Option.defaultValue "" uid)
    }
    static member empty : TermCell = {
        Term = TermMinimal.empty
    }

type SwateCell =
| IsUnit of UnitCell
| IsFreetext of FreetextCell
| IsTerm of TermCell
| IsHeader of OfficeInteropTypes.SwateColumnHeader
with
    member this.isHeader = match this with | IsHeader _ -> true | _ -> false
    member this.isUnit = match this with | IsUnit _ -> true | _ -> false
    member this.isTerm = match this with | IsTerm _ -> true | _ -> false
    member this.isFreetext = match this with | IsFreetext _ -> true | _ -> false
    member this.Unit =
        match this with
        | IsUnit c -> c
        | _ -> failwith "Not a Swate UnitCell."
    member this.Term =
        match this with
        | IsTerm c -> c
        | _ -> failwith "Not a Swate TermCell."
    member this.Freetext =
        match this with
        | IsFreetext c -> c
        | _ -> failwith "Not a Swate TermCell."
    member this.Header =
        match this with
        | IsHeader cc -> cc
        | _ -> failwith "Not a ColumnHeader."
    /// This is used to update the main column value
    member this.updateDisplayValue (v: string) =    
        match this with
        | IsHeader c        -> IsHeader {c with SwateColumnHeader = v}
        | IsTerm c          -> IsTerm {c with Term = {c.Term with Name = v}}
        | IsUnit c          -> IsUnit {c with Value = v}
        | IsFreetext c      -> IsFreetext {c with Value = v}
    member this.displayValue =
        match this with
        | IsHeader c        -> c.SwateColumnHeader
        | IsTerm c          -> c.Term.Name
        | IsUnit c          -> c.Value
        | IsFreetext c      -> c.Value
    // Mirror create functions for body cells
    /// Creates TermCell
    static member create(term: TermTypes.TermMinimal) = TermCell.create(term) |> IsTerm
    /// Creates TermCell
    static member create(name: string, ?uid: string) = TermCell.create(name, ?uid = uid) |> IsTerm
    /// Creates UnitCell
    static member create(value:string, ?unit: TermTypes.TermMinimal) = UnitCell.create(value, ?unit = unit) |> IsUnit
    /// Creates UnitCell
    static member create(value:string, ?name: string, ?uid: string) = UnitCell.create(value, ?name = name, ?uid = uid) |> IsUnit
    /// Creates UnitCell
    static member create(unit: TermTypes.TermMinimal, ?value:string) = UnitCell.create(Option.defaultValue "" value, unit = unit) |> IsUnit
    /// Creates UnitCell
    static member create(name: string, uid: string, ?value:string) = UnitCell.create(Option.defaultValue "" value, name = name, uid = uid) |> IsUnit
    /// Creates FreetextCell
    static member create(value: string) = FreetextCell.create(value) |> IsFreetext
    // Mirror create functions for SwateColumnHeader
    /// Creates HeaderCell
    static member createHeader(headerString: string, ?term: TermTypes.TermMinimal) = OfficeInteropTypes.SwateColumnHeader.init(headerString, ?term = term) |> IsHeader
    /// Creates HeaderCell
    static member createHeader(headerString: OfficeInteropTypes.SwateColumnHeader, ?term: TermTypes.TermMinimal) = OfficeInteropTypes.SwateColumnHeader.init(headerString, ?term = term) |> IsHeader
    static member emptyTerm = IsTerm TermCell.empty
    static member emptyFreetext = IsFreetext FreetextCell.empty
    static member emptyUnit = IsUnit UnitCell.empty
    static member emptyHeader = SwateCell.createHeader(headerString = "empty")
    static member emptyOfCell (cell: SwateCell) =
        match cell with
        | IsHeader _ -> SwateCell.emptyHeader
        | IsTerm _ -> SwateCell.emptyTerm
        | IsUnit _ -> SwateCell.emptyUnit
        | IsFreetext _ -> SwateCell.emptyFreetext

open OfficeInteropTypes

type SwateBuildingBlock = {
    Index: int
    Header: SwateColumnHeader
    Rows: (int*SwateCell) []
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
