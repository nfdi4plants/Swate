namespace Spreadsheet

open Swate.Components
open ARCtrl
open Fable.Core
open FileImport

type ColumnType =
    | Main
    | Unit
    | TSR
    | TAN
    | DataSelector
    | DataFormat
    | DataSelectorFormat

    member this.AsNumber =
        match this with
        | Main -> 0
        | Unit
        | DataSelector -> 1
        | TSR
        | DataFormat -> 2
        | TAN
        | DataSelectorFormat -> 3

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

    member this.IsMainColumn =
        match this with
        | Main -> true
        | _ -> false

    member this.IsRefColumn = not this.IsMainColumn

[<RequireQualifiedAccess>]
type ActiveView =
    | Table of index: int
    | DataMap
    | Metadata

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

    /// This function is used to verify if the current arcfile supports the active view type.
    member this.ArcFileHasView(arcfile: ArcFiles) =
        match this with
        | Table i -> arcfile.HasTableAt(i)
        | DataMap -> arcfile.HasTableAt(-1)
        | Metadata -> arcfile.HasMetadata()

[<AutoOpen>]
module ActivePattern =

    let (|IsTable|IsDataMap|IsMetadata|) (input: ActiveView) =
        match input with
        | ActiveView.Table _ -> IsTable
        | ActiveView.DataMap -> IsDataMap
        | ActiveView.Metadata -> IsMetadata

///<summary>If you change this model, it will kill caching for users! if you apply changes to it, make sure to keep a version
///of it and add a try case for it to `tryInitFromLocalStorage` in Spreadsheet/LocalStorage.fs .</summary>
type Model = {
    ActiveView: ActiveView
    ArcFile: ArcFiles option
} with

    static member init(?arcfile) = {
        ActiveView = ActiveView.Metadata
        ArcFile = arcfile
    }

    member this.FileType =
        match this.ArcFile with
        | Some(Assay _) -> "Assay"
        | Some(Study _) -> "Study"
        | Some(Investigation _) -> "Investigation"
        | Some(Run _) -> "Run"
        | Some(Workflow _) -> "Workflow"
        | Some(DataMap _) -> "Datamap"
        | Some(Template _) -> "Template"
        | None -> "No File"

    member this.Tables =
        match this.ArcFile with
        | Some(arcfile) -> arcfile.ArcTables()
        | None -> ResizeArray() |> ArcTables

    member this.ActiveTable =
        match this.ActiveView with
        | ActiveView.Table i -> this.Tables.GetTableAt(i)
        | ActiveView.Metadata
        | ActiveView.DataMap ->
            let t = ArcTable.init ("NULL_TABLE") //return NULL_TABLE-named table for easier handling of return value

            t.AddColumn(
                CompositeHeader.FreeText "WARNING",
                [|
                    CompositeCell.FreeText "If you see this table view, pls contact a developer and report it."
                |]
                |> ResizeArray
            )

            t

    member this.HasMetadata() =
        match this.ArcFile with
        | Some(arcFile) -> arcFile.HasMetadata()
        | None -> false

    member this.HasDataMap() =
        match this.ArcFile with
        | Some(Assay a) -> a.DataMap.IsSome
        | Some(Run r) -> r.DataMap.IsSome
        | Some(Workflow w) -> w.DataMap.IsSome
        | Some(Study(s, _)) -> s.DataMap.IsSome
        | Some(DataMap(_, _)) -> true
        | _ -> false

    member this.DataMapOrDefault =
        match this.ArcFile with
        | Some(Assay a) when a.DataMap.IsSome -> a.DataMap.Value
        | Some(Run r) when r.DataMap.IsSome -> r.DataMap.Value
        | Some(Workflow w) when w.DataMap.IsSome -> w.DataMap.Value
        | Some(Study(s, _)) when s.DataMap.IsSome -> s.DataMap.Value
        | Some(DataMap(_, d)) -> d
        | _ -> DataMap.init ()

    member this.GetAssay() =
        match this.ArcFile with
        | Some(Assay a) -> a
        | _ -> ArcAssay.init ("ASSAY_NULL")

    member this.CanHaveTables() =
        match this.ArcFile with
        | Some(ArcFiles.Assay _)
        | Some(ArcFiles.Study _)
        | Some(ArcFiles.Run _) -> true
        | _ -> false

    member this.TableViewIsActive() =
        match this.ActiveView with
        | ActiveView.Table _ -> true
        | _ -> false

type Msg =
    // <--> UI <-->
    | UpdateState of Model
    | UpdateCell of CellCoordinate * CompositeCell
    | UpdateCells of (CellCoordinate * CompositeCell)[]
    | UpdateHeader of columIndex: int * CompositeHeader
    | UpdateActiveView of ActiveView
    | MoveColumn of current: int * next: int
    | UpdateDatamap of DataMap option
    | UpdateDataMapDataContextAt of index: int * DataContext
    | AddTable of ArcTable
    | UpdateTable of ArcTable
    | RemoveTable of index: int
    | RenameTable of index: int * name: string
    | UpdateTableOrder of pre_index: int * new_index: int
    | AddRows of int
    | DeleteRow of int
    | DeleteRows of int[]
    | DeleteColumn of int
    | SetColumn of index: int * column: CompositeColumn
    | CopyCell of index: CellCoordinate
    | CopyCells of indices: CellCoordinate[]
    | CutCell of index: CellCoordinate
    | PasteCell of index: CellCoordinate
    | SetCell of CellCoordinate * term: Term option
    /// This Msg will paste all cell from clipboard into column starting from index. It will extend the table if necessary.
    | PasteCellsExtend of index: CellCoordinate
    | Clear of index: CellCoordinate[]
    | FillColumnWithTerm of index: CellCoordinate
    // /// Update column of index to new column type defined by given SwateCell.emptyXXX
    // | EditColumn of index: int * newType: SwateCell * b_type: BuildingBlockType option
    /// This will reset Spreadsheet.Model to Spreadsheet.Model.init() and clear all webstorage.
    | Reset
    | ImportXlsx of byte[]
    // <--> INTEROP <-->
    | CreateAnnotationTable of tryUsePrevOutput: bool
    | AddAnnotationBlock of index: int option * CompositeColumn
    | AddAnnotationBlocks of index: int option * CompositeColumn[]
    | AddDataAnnotation of
        {|
            fragmentSelectors: string[]
            fileName: string
            fileType: string
            targetColumn: DataAnnotator.TargetColumn
        |}
    | AddTemplates of ArcTable[] * SelectiveImportConfig
    | JoinTable of ArcTable * index: int option * options: TableJoinOptions option * string option
    | UpdateArcFile of ArcFiles
    | InitFromArcFile of ArcFiles
    | InsertOntologyAnnotation of CellCoordinateRange * OntologyAnnotation
    | InsertOntologyAnnotations of OntologyAnnotation[]
    /// Starts chain to export active table to isa json
    | ExportJson of ArcFiles * JsonExportFormat
    /// Starts chain to export all tables to xlsx swate tables.
    | ExportXlsx of ArcFiles
    | ExportXlsxDownload of filename: string * byte[]
// <--> Result Messages <-->
///// This message will save `Model` to local storage and to session storage for history
//| Success of Model
///// This message will save `Model` to local storage
//| SuccessNoHistory of Model