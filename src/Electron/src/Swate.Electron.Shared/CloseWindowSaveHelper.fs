module Swate.Electron.Shared.CloseWindowSaveHelper

open Swate.Components.Shared

[<RequireQualifiedAccess>]
type ArcFileSaveSource =
    | PendingArcEdits
    | VisibleArcPage

type ArcFileSaveTarget = {
    ArcFile: ArcFiles
    Source: ArcFileSaveSource
}

let tryGetArcFileToSave (pendingArcFile: ArcFiles option) (pageState: PageState option) : ArcFileSaveTarget option =
    match pendingArcFile with
    | Some arcFile -> Some { ArcFile = arcFile; Source = ArcFileSaveSource.PendingArcEdits }
    | None ->
        match pageState with
        | Some(PageState.ArcFilePage arcFile) -> Some { ArcFile = arcFile; Source = ArcFileSaveSource.VisibleArcPage }
        | _ -> None
