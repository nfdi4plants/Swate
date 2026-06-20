module Renderer.Components.MainContent.ArcFilePreviewTargetHelper

open Fable.Core
open Swate.Components.Composite.Widgets.JsonImport.Types
open Swate.Components.Page.ArcFileEditor.Types
open Swate.Components.Shared

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

let deleteSelectedTable
    (arcFile: ArcFiles)
    (tableIndex: int)
    (setArcFile: ArcFiles -> unit)
    (setActiveView: ActiveView -> unit)
    =
    arcFile.ArcTables().RemoveTableAt tableIndex

    arcFile |> ArcFiles.refreshRef |> setArcFile
    setActiveView ActiveView.Metadata
