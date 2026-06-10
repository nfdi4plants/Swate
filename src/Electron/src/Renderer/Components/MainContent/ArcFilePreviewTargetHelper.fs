module Renderer.Components.MainContent.ArcFilePreviewTargetHelper

open Swate.Components.Page.ArcFileEditor.Types
open Swate.Components.Shared

let deleteSelectedTable
    (arcFile: ArcFiles)
    (tableIndex: int)
    (setArcFile: ArcFiles -> unit)
    (setActiveView: ActiveView -> unit)
    =
    arcFile.ArcTables().RemoveTableAt tableIndex

    arcFile |> ArcFiles.refreshRef |> setArcFile
    setActiveView ActiveView.Metadata
