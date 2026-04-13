module Swate.Components.ArcFileEditor.EmptyTableView.Helper

open ARCtrl
open Swate.Components
open Swate.Components.Shared

let tryGetActiveTable (arcFile: ArcFiles) (activeTableIndex: int option) =
    arcFile.TryGetActiveTable(activeTableIndex)

let createMinimalTable (arcFile: ArcFiles) (activeTableIndex: int option) (setArcFile: ArcFiles -> unit) =
    match tryGetActiveTable arcFile activeTableIndex with
    | Some(_, activeTable) ->
        let newColumns = [|
            CompositeColumn.create (CompositeHeader.Input IOType.Sample)
            CompositeColumn.create CompositeHeader.ProtocolUri
            CompositeColumn.create (CompositeHeader.Output IOType.Sample)
        |]

        activeTable.AddColumns(newColumns)
        activeTable.AddRowsEmpty(3)
        setArcFile (WidgetArcFile.refreshRef arcFile)
    | None -> ()

let getOutputTables (arcFile: ArcFiles) =
    arcFile.Tables()
    |> Seq.filter (fun table -> table.TryGetOutputColumn().IsSome)
    |> Seq.toArray

let tryCreatePreviewColumn (table: ArcTable) =
    match table.TryGetOutputColumn() with
    | Some outputColumn ->
        match outputColumn.Header.TryIOType() with
        | Some ioType -> Some(CompositeColumn.create (CompositeHeader.Input ioType, outputColumn.Cells))
        | None -> None
    | None -> None

let previewCells (cells: seq<CompositeCell>) =
    cells |> Seq.truncate 10 |> Seq.map string |> Seq.toArray

let importSelectedPreviousOutput
    (arcFile: ArcFiles)
    (activeTableIndex: int option)
    (selectedTable: ArcTable option)
    (setArcFile: ArcFiles -> unit)
    =
    match tryGetActiveTable arcFile activeTableIndex, selectedTable with
    | Some(activeIndex, activeTable), Some sourceTable ->
        match sourceTable.TryGetOutputColumn() with
        | Some outputColumn ->
            match outputColumn.Header.TryIOType() with
            | Some ioType ->

                activeTable.AddColumn(CompositeHeader.Input ioType, outputColumn.Cells)

                setArcFile (WidgetArcFile.refreshRef arcFile)
                true
            | None -> false
        | None -> false
    | _ -> false