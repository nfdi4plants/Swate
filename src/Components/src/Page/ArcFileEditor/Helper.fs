module Swate.Components.Page.ArcFileEditor.Helper

open ARCtrl
open Swate.Components.Shared
open Swate.Components.Page.ArcFileEditor.Types
open Swate.Components.Composite.Widgets.DataAnnotator.Types

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

let tryGetDataAnnotatorDestination (activeView: ActiveView, arcFile: ArcFiles) =
    match activeView with
    | ActiveView.Table index ->
        match arcFile.TryGetActiveTable(Some index) with
        | Some(_, table) -> Ok(AnnotationDestination.Table table)
        | None -> Error "No active table is available for Data Annotator."
    | ActiveView.DataMap ->
        match arcFile.TryGetDataMap() with
        | Some dataMap -> Ok(AnnotationDestination.DataMap dataMap)
        | None -> Error "No DataMap is available for Data Annotator."
    | ActiveView.Metadata -> Error "Data Annotator is not available in Metadata view."

let applyDataAnnotatorInputToArcFile
    (destination: AnnotationDestination, arcFile: ArcFiles, setArcFile: ArcFiles -> unit)
    =
    (fun annotationInput ->
        let result =
            match destination with
            | AnnotationDestination.Table table ->
                Swate.Components.Composite.Widgets.DataAnnotator.Helper.applyToTable table annotationInput
            | AnnotationDestination.DataMap dataMap ->
                Swate.Components.Composite.Widgets.DataAnnotator.Helper.applyToDataMap dataMap annotationInput

        match result with
        | Ok _ ->
            setArcFile (ArcFiles.refreshRef arcFile)
            result
        | Error _ -> result
    )