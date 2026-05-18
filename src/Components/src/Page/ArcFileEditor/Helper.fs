module Swate.Components.Page.ArcFileEditor.Helper

open ARCtrl
open Swate.Components.Shared
open Swate.Components.Page.ArcFileEditor.Types

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

let applyDataAnnotatorInputToArcFile (activeView: ActiveView, arcFile: ArcFiles, setArcFile: ArcFiles -> unit, onError: string -> unit) =
    (fun annotationInput ->
        match activeView with
        | ActiveView.Table index ->
            arcFile.TryGetActiveTable(Some index)
            |> Option.iter (fun (_, table) ->
                match Swate.Components.Composite.Widgets.DataAnnotator.Helper.applyToTable table annotationInput with
                | Ok _ ->
                    ()
                | Error errorMsg ->
                    onError ("Error applying annotation: " + errorMsg)

                setArcFile (ArcFiles.refreshRef arcFile)
            )
        | ActiveView.DataMap ->
            arcFile.TryGetDataMap()
            |> Option.iter (fun dataMap ->
                match Swate.Components.Composite.Widgets.DataAnnotator.Helper.applyToDataMap dataMap annotationInput with
                | Ok _ ->
                    ()
                | Error errorMsg ->
                    onError ("Error applying annotation: " + errorMsg)

                setArcFile (ArcFiles.refreshRef arcFile)
            )

        | _ -> ()
    )