module Swate.Components.Page.ArcFileEditor.Helper

open Swate.Components.Shared
open Swate.Components.Page.ArcFileEditor.Types
open Swate.Components.Composite.Widgets.DataAnnotator.Types

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
