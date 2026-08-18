module Renderer.Components.MainContent.ArcFilePreviewTargetHelper

open Fable.Core
open Swate.Components.Page.ArcFileEditor.Types
open Swate.Components.Composite.Widgets.JsonImport.Types
open Swate.Components.Shared

let editorKey (arcFile: ArcFiles) (requestedView: ActiveView option) =
    arcFile.TryGetRelativePath()
    |> Option.defaultValue (string arcFile.RelatedArcFilesDiscriminate),
    requestedView |> Option.map _.ViewIndex

let private publishAndPersistArcFile
    (nextArcFile: ArcFiles)
    (publishArcFile: ArcFiles -> unit)
    (persistArcFile: ArcFiles -> JS.Promise<Result<unit, exn>>)
    =
    promise {
        publishArcFile nextArcFile
        return! persistArcFile nextArcFile
    }

let importJsonRequestIntoCurrentTarget
    (currentArcFile: ArcFiles)
    (request: JsonImportRequest)
    (setArcFilePageState: ArcFiles -> unit)
    (setArcFileInMemory: ArcFiles -> JS.Promise<Result<unit, exn>>)
    =
    promise {
        match Json.Import.applyToCurrentArcFile (currentArcFile, request.ImportedFile) with
        | Error exn -> return Error exn
        | Ok nextArcFile -> return! publishAndPersistArcFile nextArcFile setArcFilePageState setArcFileInMemory
    }
