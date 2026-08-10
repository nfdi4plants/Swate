module Renderer.Components.Helper.ArcViewHelper

open Fable.Core
open Swate.Components.Shared
open Swate.Electron.Shared.FileIOHelper

let private loadViewResult (previewPath: string) : JS.Promise<Result<Renderer.Types.PageState, string>> = promise {
    let! result = Api.ipcArcVaultApi.openFile previewPath

    return
        result
        |> Result.map Renderer.Types.PageState.fromFileContentDTO
        |> Result.mapError _.Message
}

let applyRequestedPathView (requestedPath: string) (pageState: Renderer.Types.PageState) =
    match PathHelpers.getNameFromPath requestedPath, pageState with
    | requestedFileName, Renderer.Types.PageState.ArcFilePage(arcFile, _) when
        PathHelpers.pathsEqual requestedFileName ARCtrl.ArcPathHelper.DataMapFileName
        ->
        Renderer.Types.PageState.ArcFilePage(arcFile, Some Swate.Components.Page.ArcFileEditor.Types.ActiveView.DataMap)
    | _ -> pageState

let openView (path: string) : JS.Promise<Result<Renderer.Types.PageState, string>> = promise {
    let previewPath = resolveArcPreviewPath path
    let! result = loadViewResult previewPath
    return result |> Result.map (applyRequestedPathView path)
}
