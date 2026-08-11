module Renderer.Components.MainContent.ArcFilePreviewTargetHelper

open Fable.Core
open Swate.Components.Page.ArcFileEditor.Types
open Swate.Components.Composite.Widgets.JsonImport.Types
open Swate.Components.Shared

let editorKey (arcFile: ArcFiles) (activeView: ActiveView option) =
    arcFile.TryGetRelativePath()
    |> Option.defaultValue (string arcFile.RelatedArcFilesDiscriminate),
    activeView |> Option.map _.ViewIndex

let createDataMapInCurrentTarget
    (currentArcFile: ArcFiles)
    (setArcFilePageState: ArcFiles -> unit)
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

        setArcFilePageState nextArcFile
        return! saveArcFile nextArcFile
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
        | Ok nextArcFile ->
            setArcFilePageState nextArcFile
            return! setArcFileInMemory nextArcFile
    }
