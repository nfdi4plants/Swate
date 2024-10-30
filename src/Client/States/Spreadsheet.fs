namespace Spreadsheet

open Shared
open ARCtrl
open Fable.Core

type ColumnType =
| Main
| Unit
| TSR
| TAN
| DataSelector
| DataFormat
| DataSelectorFormat
with
    member this.AsNumber = 
        match this with
        | Main -> 0
        | Unit| DataSelector -> 1
        | TSR | DataFormat -> 2
        | TAN | DataSelectorFormat -> 3
    /// <summary>
    /// Use this function to get static column header names, such as "Unit", "Data Selector", etc.
    /// </summary>
    member this.ToColumnHeader() =
        match this with
        | Unit -> "Unit"
        | DataSelector -> "Data Selector"
        | DataFormat -> "Data Format"
        | DataSelectorFormat -> "Data Selector Format"
        | anyElse -> failwithf "Error. Unable to call `ColumnType.ToColumnHeader()` on %A!" anyElse
    member this.IsMainColumn = match this with | Main -> true | _ -> false
    member this.IsRefColumn = not this.IsMainColumn 


[<RequireQualifiedAccess>]
type ActiveView = 
| Table of index:int
| DataMap
| Metadata
with 
    /// <summary>
    /// A identifier that returns an integer based on the ActiveView type.
    /// </summary>
    member this.ViewIndex =
        match this with
        | Table i -> i
        | DataMap -> -1
        | Metadata -> -2

    member this.TryTableIndex =
        match this with
        | Table i -> Some i
        | _ -> None

    member this.ArcFileHasView(arcfile: ArcFiles) =
        match this with
        | Table i -> arcfile.HasTableAt(i)
        | DataMap -> arcfile.HasDataMap()
        | Metadata -> true

[<AutoOpen>]
module ActivePattern =

    let (|IsTable|IsDataMap|IsMetadata|) (input:ActiveView) =
        match input with
        | ActiveView.Table _ -> IsTable
        | ActiveView.DataMap -> IsDataMap
        | ActiveView.Metadata -> IsMetadata

///<summary>If you change this model, it will kill caching for users! if you apply changes to it, make sure to keep a version
///of it and add a try case for it to `tryInitFromLocalStorage` in Spreadsheet/LocalStorage.fs .</summary>
type Model = {
    ActiveView: ActiveView
    SelectedCells: Set<int*int>
    ActiveCell: (U2<int,(int*int)> * ColumnType) option
    ArcFile: ArcFiles option
} with
    member this.CellIsActive(index: U2<int, int*int>, columnType) =
        match this.ActiveCell, index with
        | Some (U2.Case1 (headerIndex), ct), U2.Case1 (targetIndex) -> headerIndex = targetIndex && ct = columnType
        | Some (U2.Case2 (ci, ri), ct), U2.Case2 targetIndex -> (ci,ri) = targetIndex && ct = columnType
        | _ -> false
    member this.CellIsSelected(index: int*int) =
        this.SelectedCells.Contains((index))
    member this.CellIsIdle(index: U2<int, int*int>, columnType) =
        this.CellIsActive(index, columnType) |> not
    static member init() =
        {
            ActiveView = ActiveView.Metadata
            SelectedCells = Set.empty
            ActiveCell = None
            ArcFile = None
        }
    member this.FileType =
        match this.ArcFile with
        | Some (Assay _) -> "Assay"
        | Some (Study _) -> "Study"
        | Some (Investigation _) -> "Investigation"
        | Some (Template _) -> "Template"
        | None -> "No File"

    static member init(arcFile: ArcFiles) =
        {
            ActiveView = ActiveView.Metadata
            SelectedCells = Set.empty
            ActiveCell = None
            ArcFile = Some arcFile
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
            | ActiveView.Metadata | ActiveView.DataMap -> 
                let t = ArcTable.init("NULL_TABLE") //return NULL_TABLE-named table for easier handling of return value
                t.AddColumn(CompositeHeader.FreeText "WARNING", [|CompositeCell.FreeText "If you see this table view, pls contact a developer and report it."|])
                t
    member this.HasDataMap() =
        match this.ArcFile with
        | Some (Assay a) -> a.DataMap.IsSome
        | Some (Study (s,_)) -> s.DataMap.IsSome
        | _ -> false
    member this.DataMapOrDefault =
        match this.ArcFile with
        | Some (Assay a) when a.DataMap.IsSome -> a.DataMap.Value
        | Some (Study (s,_)) when s.DataMap.IsSome -> s.DataMap.Value
        | _ -> DataMap.init()
    member this.GetAssay() =
        match this.ArcFile with | Some (Assay a) -> a | _ -> ArcAssay.init("ASSAY_NULL")
    member this.CanHaveTables() = 
        match this.ArcFile with 
        | Some (ArcFiles.Assay _) | Some (ArcFiles.Study _) -> true
        | _ -> false
    member this.TableViewIsActive() =
        match this.ActiveView with
        | ActiveView.Table i -> true
        | _ -> false

[<RequireQualifiedAccess>]
type Key =
    | Up
    | Down
    | Left
    | Right


type Msg =
// <--> UI <-->
| UpdateState of Model
| UpdateCell of (int*int) * CompositeCell
| UpdateCells of ((int*int) * CompositeCell) []
| UpdateHeader of columIndex: int * CompositeHeader
| UpdateActiveView of ActiveView
| UpdateSelectedCells of Set<int*int>
| MoveSelectedCell of Key
| MoveColumn of current:int * next:int
| UpdateActiveCell of (U2<int,(int*int)> * ColumnType) option
| SetActiveCellFromSelected
| UpdateDatamap of DataMap option
| UpdateDataMapDataContextAt of index: int * DataContext
| AddTable of ArcTable
| RemoveTable of index:int
| RenameTable of index:int * name:string
| UpdateTableOrder of pre_index:int * new_index:int
| UpdateHistoryPosition of newPosition:int
| AddRows of int
| DeleteRow of int
| DeleteRows of int []
| DeleteColumn of int
| SetColumn of index:int * column: CompositeColumn
| CopySelectedCell
| CopySelectedCells
| CutSelectedCell
| CutSelectedCells
| PasteSelectedCell
| PasteSelectedCells
| CopyCell of index:(int*int)
| CopyCells of indices:(int*int) []
| CutCell of index:(int*int)
| PasteCell of index:(int*int)
/// This Msg will paste all cell from clipboard into column starting from index. It will extend the table if necessary.
| PasteCellsExtend of index:(int*int)
| Clear of index:(int*int) []
| ClearSelected
| FillColumnWithTerm of index:(int*int)
// /// Update column of index to new column type defined by given SwateCell.emptyXXX
// | EditColumn of index: int * newType: SwateCell * b_type: BuildingBlockType option
/// This will reset Spreadsheet.Model to Spreadsheet.Model.init() and clear all webstorage.
| Reset
| ImportXlsx of byte []
// <--> INTEROP <-->
| CreateAnnotationTable of tryUsePrevOutput:bool
| AddAnnotationBlock of CompositeColumn
| AddAnnotationBlocks of CompositeColumn []
| AddDataAnnotation of {| fragmentSelectors: string []; fileName: string; fileType: string; targetColumn: DataAnnotator.TargetColumn |}
| AddTemplate of ArcTable
| JoinTable of ArcTable * index: int option * options: TableJoinOptions option
| UpdateArcFile of ArcFiles
| InitFromArcFile of ArcFiles
| InsertOntologyAnnotation of OntologyAnnotation
| InsertOntologyAnnotations of OntologyAnnotation []
| UpdateTermColumnsResponse of TermTypes.TermSearchable []
/// Starts chain to export active table to isa json
| ExportJson of ArcFiles * JsonExportFormat
/// Starts chain to export all tables to xlsx swate tables.
| ExportXlsx of ArcFiles
| ExportXlsxDownload of filename: string * byte []
// <--> Result Messages <-->
///// This message will save `Model` to local storage and to session storage for history
//| Success of Model
///// This message will save `Model` to local storage
//| SuccessNoHistory of Model