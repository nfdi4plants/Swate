module ARCitISA.Components.Page.ArcFileEditor.Helper

open ARCitISA.Components.Shared
open ARCitISA.Components.Page.ArcFileEditor.Types
open ARCitISA.Components.Composite.Widgets.DataAnnotator.Types

[<Literal>]
let TableDragIdPrefix = "table-"

let tableDragId index = $"{TableDragIdPrefix}{index}"

let tableDragIds tableCount =
    Seq.init tableCount tableDragId |> ResizeArray

let tryGetAddRowsTarget (activeView: ActiveView, arcFileState: ArcFiles) =
    match activeView with
    | ActiveView.Table tableIndex ->
        arcFileState.TryGetActiveTable(Some tableIndex)
        |> Option.map (snd >> AddRowsTarget.Table)
    | ActiveView.DataMap -> arcFileState.TryGetDataMap() |> Option.map AddRowsTarget.DataMap
    | ActiveView.Metadata -> None
