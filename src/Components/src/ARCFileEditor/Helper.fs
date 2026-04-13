module Swate.Components.ArcFileEditor.Helper

open ARCtrl
open Swate.Components.Shared
open Swate.Components.ArcFileEditor.Types

[<Literal>]
let NewTablePrefix = "New Table"

let createNewTableName (tables: ResizeArray<ArcTable>) =
    let existingNames = tables |> Seq.map _.Name

    let rec loop index =
        let name = $"{NewTablePrefix} {index}"

        if Seq.contains name existingNames then
            loop (index + 1)
        else
            name

    loop 0

let tryGetAddRowsTarget (activeView: ActiveView, arcFileState: ArcFiles) =
    match activeView with
    | ActiveView.Table tableIndex ->
        arcFileState.TryGetActiveTable(Some tableIndex)
        |> Option.map (snd >> AddRowsTarget.Table)
    | ActiveView.DataMap -> arcFileState.TryGetDataMap() |> Option.map AddRowsTarget.DataMap
    | ActiveView.Metadata -> None