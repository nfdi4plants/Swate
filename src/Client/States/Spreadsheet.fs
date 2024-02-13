namespace Spreadsheet

open Shared
open OfficeInteropTypes
open ARCtrl.ISA

type TableClipboard = {
    Cell: CompositeCell option
} with
    static member init() = {
        Cell = None
    }

[<RequireQualifiedAccess>]
type ActiveView = 
| Table of index:int
| Metadata
with 
    /// <summary>
    /// Returns -1 if no table index given
    /// </summary>
    member this.TableIndex =
        match this with
        | Table i -> i
        | _ -> 0

///<summary>If you change this model, it will kill caching for users! if you apply changes to it, make sure to keep a version
///of it and add a try case for it to `tryInitFromLocalStorage` in Spreadsheet/LocalStorage.fs .</summary>
type Model = {
    ActiveView: ActiveView
    SelectedCells: Set<int*int>
    ArcFile: ArcFiles option
    Clipboard: TableClipboard
} with
    static member init() =
        {
            ActiveView = ActiveView.Metadata
            SelectedCells = Set.empty
            ArcFile = None
            Clipboard = TableClipboard.init()
        }
    member this.Tables
        with get() =
            match this.ArcFile with
            | Some (arcfile) ->
                arcfile.Tables()
            | None ->
                ResizeArray() |> ArcTables
    member this.ActiveTable
        with get() = 
            match this.ActiveView with
            | ActiveView.Table i -> this.Tables.GetTableAt(i)
            | ActiveView.Metadata -> 
                let t = ArcTable.init("NULL_TABLE") //return NULL_TABLE-named table for easier handling of return value
                t.AddColumn(CompositeHeader.FreeText "WARNING", [|CompositeCell.FreeText "If you see this table view, pls contact a developer and report it."|])
                t
    member this.getSelectedColumnHeader =
        if this.SelectedCells.IsEmpty then None else
            let columnIndex = this.SelectedCells |> Set.toList |> List.minBy fst |> fst
            let header = this.ActiveTable.GetColumn(columnIndex).Header
            Some header
    member this.GetAssay() =
        match this.ArcFile with | Some (Assay a) -> a | _ -> ArcAssay.init("ASSAY_NULL")
    member this.headerIsSelected =
        not this.SelectedCells.IsEmpty && this.SelectedCells |> Seq.exists (fun (c,r) -> r = 0)
    member this.CanHaveTables() = 
        match this.ArcFile with 
        | Some (ArcFiles.Assay _) | Some (ArcFiles.Study _) -> true
        | _ -> false
    member this.TableViewIsActive() =
        match this.ActiveView with
        | ActiveView.Table i -> true
        | _ -> false

type Msg =
// <--> UI <-->
| UpdateCell of (int*int) * CompositeCell
| UpdateCells of ((int*int) * CompositeCell) []
| UpdateHeader of columIndex: int * CompositeHeader
| UpdateActiveView of ActiveView
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
// /// Update column of index to new column type defined by given SwateCell.emptyXXX
// | EditColumn of index: int * newType: SwateCell * b_type: BuildingBlockType option
/// This will reset Spreadsheet.Model to Spreadsheet.Model.init() and clear all webstorage.
| Reset
| SetArcFileFromBytes of byte []
// <--> INTEROP <-->
| CreateAnnotationTable of tryUsePrevOutput:bool
| AddAnnotationBlock of CompositeColumn
| AddAnnotationBlocks of CompositeColumn []
| JoinTable of ArcTable * index: int option * options: TableJoinOptions option
| UpdateArcFile of ArcFiles
| InitFromArcFile of ArcFiles
| InsertOntologyAnnotation of OntologyAnnotation
| InsertOntologyAnnotations of OntologyAnnotation []
| UpdateTermColumns
| UpdateTermColumnsResponse of TermTypes.TermSearchable []
/// Starts chain to export active table to isa json
| ExportJsonTable
/// Starts chain to export all tables to isa json
| ExportJsonTables
/// Starts chain to export all tables to xlsx swate tables.
| ExportXlsx of ArcFiles
| ExportXlsxDownload of filename: string * byte []
/// Starts chain to parse all tables to DAG
| ParseTablesToDag
// <--> Result Messages <-->
///// This message will save `Model` to local storage and to session storage for history
//| Success of Model
///// This message will save `Model` to local storage
//| SuccessNoHistory of Model