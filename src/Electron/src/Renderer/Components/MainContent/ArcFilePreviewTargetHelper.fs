module Renderer.Components.MainContent.ArcFilePreviewTargetHelper

open Fable.Core
open Swate.Components.Page.ArcFileEditor.Types
open Swate.Components.Composite.Widgets.JsonImport.Types
open Swate.Components.Shared
open ARCtrl

let editorKey (arcFile: ArcFiles) (requestedView: ActiveView option) =
    arcFile.TryGetRelativePath()
    |> Option.defaultValue (string arcFile.RelatedArcFilesDiscriminate),
    requestedView |> Option.map _.ViewIndex

let importJsonRequestIntoCurrentTarget
    (currentArcFile: ArcFiles)
    (request: JsonImportRequest)
    (mutateArcFile: (ArcFiles -> unit) -> unit)
    (replaceArcFile: ArcFiles -> unit)
    =
    promise {
        match Json.Import.applyToCurrentArcFile (currentArcFile, request.ImportedFile) with
        | Error exn -> return Error exn
        | Ok nextArcFile ->
            if obj.ReferenceEquals(nextArcFile, currentArcFile) then
                mutateArcFile (fun _ -> ())
            else
                replaceArcFile nextArcFile

            return Ok()
    }
