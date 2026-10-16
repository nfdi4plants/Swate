module ARCitISA.Components.Composite.Widgets.CellInsertion

open ARCtrl
open ARCitISA.Components
open ARCitISA.Components.Shared
open ARCitISA.Components.Composite.AnnotationTable.Context
open ARCitISA.Components.Composite.DataMapTable
open ARCitISA.Components.Composite.Widgets.Types

/// Annotation tables use zero-based model coordinates; DataMap clipboard operations
/// accept the one-based view coordinates (including the header offset).
let tryGetTarget (arcFile: ArcFiles) activeTableIndex (selections: Map<string, AnnotationTableContext>) =
    let selectedCells key =
        selections
        |> Map.tryFind key
        |> Option.bind _.SelectedCells
        |> Option.filter (fun selection -> selection.xStart > 0 && selection.yStart > 0)

    match arcFile.TryGetActiveTable(activeTableIndex) with
    | Some(tableIndex, table) ->
        selectedCells table.Name
        |> Option.filter (fun selection -> selection.xStart <= table.ColumnCount && selection.yStart <= table.RowCount)
        |> Option.map (fun selection ->
            InsertTarget.Table(
                tableIndex,
                {|
                    xStart = selection.xStart - 1
                    xEnd = selection.xEnd - 1
                    yStart = selection.yStart - 1
                    yEnd = selection.yEnd - 1
                |}
            )
        )
    | None when activeTableIndex.IsNone ->
        arcFile.TryGetDataMap()
        |> Option.bind (fun dataMap ->
            selectedCells DataMapTable.SelectionContextKey
            |> Option.filter (fun selection -> selection.xStart <= dataMap.ColumnCount)
            |> Option.map InsertTarget.DataMap
        )
    | None -> None
