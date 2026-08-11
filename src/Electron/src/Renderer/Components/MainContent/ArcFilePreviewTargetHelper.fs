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

let createDataMapInCurrentTarget
    (currentArcFile: ArcFiles)
    (publishArcFile: ActiveView option -> ArcFiles -> unit)
    (saveArcFile: ArcFiles -> JS.Promise<Result<unit, exn>>)
    =
    promise {
        let nextArcFile = ArcFiles.refreshRef currentArcFile

        match nextArcFile with
        | ArcFiles.Assay assay -> assay.DataMap <- Some(ARCtrl.DataMap.init ())
        | ArcFiles.Study(study, _) -> study.DataMap <- Some(ARCtrl.DataMap.init ())
        | ArcFiles.Run run -> run.DataMap <- Some(ARCtrl.DataMap.init ())
        | ArcFiles.Workflow workflow -> workflow.DataMap <- Some(ARCtrl.DataMap.init ())
        | _ -> ()

        return! publishAndPersistArcFile nextArcFile (publishArcFile (Some ActiveView.DataMap)) saveArcFile
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
