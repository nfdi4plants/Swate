namespace Spreadsheet

open Shared
open TermTypes
open OfficeInteropTypes

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

type HeaderCell = {
    BuildingBlockType: OfficeInteropTypes.BuildingBlockType
    HasUnit: bool
    Term: TermMinimal option
} with
    member this.isTermColumn = this.BuildingBlockType.isTermColumn
    member this.isFeaturedColumn = this.BuildingBlockType.isFeaturedColumn
    member this.getFeaturedTerm = this.BuildingBlockType.getFeaturedColumnTermMinimal
    member this.isOutputColumn = this.BuildingBlockType.isOutputColumn
    member this.isInputColumn = this.BuildingBlockType.isInputColumn
    member this.DisplayValue =
        let n = this.Term |> Option.map (fun x -> x.Name) |> Option.defaultValue ""
        let blueprint = OfficeInteropTypes.BuildingBlockNamePrePrint.create this.BuildingBlockType n
        blueprint.toAnnotationTableHeader()
    member this.updateDisplayValue(v:string) =
        let header = SwateColumnHeader.create (v)
        let blueprint = header.toBuildingBlockNamePrePrint
        // if not parsable then freetext
        // remove unit and term
        if blueprint.IsNone then
            {this with BuildingBlockType = BuildingBlockType.Freetext v; HasUnit = false; Term = None}
        // if is featured column, set type and term
        elif blueprint.Value.isFeaturedColumn then
            {this with BuildingBlockType = blueprint.Value.Type; Term = Some header.getFeaturedColTermMinimal}
        // if is term set term
        elif blueprint.Value.isTermColumn then
            let term = TermMinimal.create blueprint.Value.Name (this.Term |> Option.map (fun x -> x.TermAccession) |> Option.defaultValue "" )
            {this with BuildingBlockType = blueprint.Value.Type; Term = Some term}
        // This is input/output
        else
            {this with BuildingBlockType = blueprint.Value.Type; Term = None; HasUnit = false}
    static member create(b_type:OfficeInteropTypes.BuildingBlockType, ?hasUnit: bool, ?term: TermMinimal) = {
            BuildingBlockType = b_type
            HasUnit = Option.defaultValue false hasUnit
            Term = term
        }       

type SwateCell =
| IsUnit of UnitCell
| IsFreetext of FreetextCell
| IsTerm of TermCell
| IsHeader of HeaderCell
with
    member this.isHeader = match this with | IsHeader _ -> true | _ -> false
    member this.isUnit = match this with | IsUnit _ -> true | _ -> false
    member this.isTerm = match this with | IsTerm _ -> true | _ -> false
    member this.isFreetext = match this with | IsFreetext _ -> true | _ -> false
    member this.toUnitCell() =
        match this with
        | IsUnit _ -> this
        | IsFreetext text -> SwateCell.create(text.Value, ?unit = None)
        | IsTerm term -> SwateCell.create(unit = term.Term)
        | IsHeader _ -> failwith "Cannot parse header cell to unit cell"
    member this.toTermCell() =
        match this with
        | IsTerm _ -> this
        | IsUnit unit -> SwateCell.create(unit.Unit)
        | IsFreetext text -> SwateCell.create(text.Value, ?uid = None)
        | IsHeader _ -> failwith "Cannot parse header cell to term cell"
    member this.toFreetextCell() =
        match this with
        | IsFreetext _ -> this
        | IsTerm term -> SwateCell.create(term.Term.Name)
        | IsUnit unit -> SwateCell.create(unit.Unit.Name)
        | IsHeader _ -> failwith "Cannot parse header cell to freetext cell"
    member this.toFreetextHeader() =
        if not this.isHeader then failwith "toFreetextHeader function can only be used on Header cells"
        let header = this.Header
        { header with
            BuildingBlockType = BuildingBlockType.Freetext "Freetext"
            Term = None
            HasUnit = false
        }
    member this.toTermHeader(?b_type: BuildingBlockType) =
        if not this.isHeader then failwith "toTermHeader function can only be used on Header cells"
        let header = this.Header
        let b_type = Option.defaultValue BuildingBlockType.Parameter b_type
        if (header.isTermColumn || header.isFeaturedColumn) && header.Term.IsSome then
            {header with HasUnit = false; BuildingBlockType = b_type}
        else
            { header with
                BuildingBlockType = b_type
                Term = header.Term |> Option.defaultValue TermMinimal.empty |> Some
            }
    member this.toUnitHeader(?b_type: BuildingBlockType) =
        if not this.isHeader then failwith "toUnitHeader function can only be used on Header cells"
        let header = this.Header
        let b_type = Option.defaultValue BuildingBlockType.Parameter b_type
        if (header.isTermColumn || header.isFeaturedColumn) && header.Term.IsSome then
            {header with HasUnit = true; BuildingBlockType = b_type}
        else
            { header with
                BuildingBlockType = b_type
                Term = header.Term |> Option.defaultValue TermMinimal.empty |> Some
                HasUnit = true
            }
    member this.Unit : UnitCell =
        match this with
        | IsUnit c -> c
        | _ -> failwith "Not a Swate UnitCell."
    member this.Term : TermCell=
        match this with
        | IsTerm c -> c
        | _ -> failwith "Not a Swate TermCell."
    member this.Freetext : FreetextCell =
        match this with
        | IsFreetext c -> c
        | _ -> failwith "Not a Swate TermCell."
    member this.Header : HeaderCell =
        match this with
        | IsHeader cc -> cc
        | _ -> failwith "Not a ColumnHeader."
    /// This is used to update the main column value
    member this.updateDisplayValue (v: string) =
        match this with
        | IsHeader c        -> IsHeader <| c.updateDisplayValue(v)
        | IsTerm c          -> IsTerm {c with Term = {c.Term with Name = v}}
        | IsUnit c          -> IsUnit {c with Value = v}
        | IsFreetext c      -> IsFreetext {c with Value = v}
    member this.displayValue =
        match this with
        | IsHeader c        -> c.DisplayValue
        | IsTerm c          -> c.Term.Name
        | IsUnit c          -> c.Value
        | IsFreetext c      -> c.Value
    // Mirror create functions for body cells
    /// Creates TermCell
    static member create(term: TermTypes.TermMinimal) : SwateCell = TermCell.create(term) |> IsTerm
    /// Creates TermCell
    static member create(name: string, ?uid: string) : SwateCell = TermCell.create(name, ?uid = uid) |> IsTerm
    /// Creates UnitCell
    static member create(value:string, ?unit: TermTypes.TermMinimal) : SwateCell = UnitCell.create(value, ?unit = unit) |> IsUnit
    /// Creates UnitCell
    static member create(value:string, ?name: string, ?uid: string) : SwateCell = UnitCell.create(value, ?name = name, ?uid = uid) |> IsUnit
    /// Creates UnitCell
    static member create(unit: TermTypes.TermMinimal, ?value:string) : SwateCell = UnitCell.create(Option.defaultValue "" value, unit = unit) |> IsUnit
    /// Creates UnitCell
    static member create(name: string, uid: string, ?value:string) : SwateCell = UnitCell.create(Option.defaultValue "" value, name = name, uid = uid) |> IsUnit
    /// Creates FreetextCell
    static member create(value: string) : SwateCell = FreetextCell.create(value) |> IsFreetext
    // Mirror create functions for SwateColumnHeader
    /// Creates HeaderCell
    static member createHeader(b_type:OfficeInteropTypes.BuildingBlockType, ?hasUnit: bool, ?term: TermMinimal) = HeaderCell.create(b_type, ?hasUnit = hasUnit, ?term = term) |> IsHeader
    static member emptyTerm = IsTerm TermCell.empty
    static member emptyFreetext = IsFreetext FreetextCell.empty
    static member emptyUnit = IsUnit UnitCell.empty
    static member emptyHeader = SwateCell.createHeader(BuildingBlockType.Freetext "Freetext")
    static member emptyOfCell (cell: SwateCell) =
        match cell with
        | IsHeader _ -> SwateCell.emptyHeader
        | IsTerm _ -> SwateCell.emptyTerm
        | IsUnit _ -> SwateCell.emptyUnit
        | IsFreetext _ -> SwateCell.emptyFreetext

type HeaderCell with
    member this.getEmptyBodyCell =
        match this.HasUnit, this.isTermColumn with
        | true, true -> SwateCell.emptyUnit
        | false, true -> SwateCell.emptyTerm
        | false, false -> SwateCell.emptyFreetext
        | anyelse -> failwith $"Unexpected pattern trying to create bodycell for header: {anyelse}"

type SwateBuildingBlock = {
    Index: int
    Header: HeaderCell
    Rows: (int*SwateCell) []
} with
    static member create(index: int, header, rows) = {
        Index = index
        Header = header
        Rows = rows
    }
    member this.hasValues = this.Rows.Length <> 0

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
