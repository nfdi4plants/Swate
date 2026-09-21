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
    (destination: AnnotationDestination, arcFile: ArcFiles, mutateArcFile: (ArcFiles -> unit) -> unit)
    =
    (fun annotationInput ->
        let rootRelativeInput = {
            annotationInput with
                FileName = toArcRootRelativeFilePath arcFile annotationInput.FileName
        }

        match destination with
        | AnnotationDestination.Table table ->
            let tableIndex =
                arcFile.Tables()
                |> Seq.tryFindIndex (fun candidate -> System.Object.ReferenceEquals(candidate, table))

            match tableIndex with
            | Some index when index < arcFile.Tables().Count ->
                let mutable result = Error "The Data Annotator mutation was not applied."

                mutateArcFile (fun current ->
                    result <-
                        Swate.Components.Composite.Widgets.DataAnnotator.Helper.applyToTable
                            (current.Tables().[index])
                            rootRelativeInput
                )

                result
            | _ -> Error "The Data Annotator target table is no longer available."
        | AnnotationDestination.DataMap _ ->
            match arcFile.TryGetDataMap() with
            | Some _ ->
                let mutable result = Error "The Data Annotator mutation was not applied."

                mutateArcFile (fun current ->
                    match current.TryGetDataMap() with
                    | Some dataMap ->
                        result <-
                            Swate.Components.Composite.Widgets.DataAnnotator.Helper.applyToDataMap
                                dataMap
                                rootRelativeInput
                    | None -> result <- Error "The Data Annotator target DataMap is no longer available."
                )

                result
            | None -> Error "The Data Annotator target DataMap is no longer available."
    )
