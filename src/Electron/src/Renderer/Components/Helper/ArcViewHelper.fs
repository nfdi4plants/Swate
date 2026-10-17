module Renderer.Components.Helper.ArcViewHelper

open Fable.Core
open Renderer.Components.Helper.ArcViewSelection
open ARCitISA.Components.Shared
open ARCitISA.Electron.Shared.FileIOHelper

let private loadViewResult (previewPath: string) : JS.Promise<Result<Renderer.Types.PageState, string>> = promise {
    let! result = Api.ipcArcVaultApi.openFile previewPath

    return
        result
        |> Result.map Renderer.Types.PageState.fromFileContentDTO
        |> Result.mapError _.Message
}

let openView (path: string) : JS.Promise<Result<Renderer.Types.PageState, string>> = promise {
    let previewPath = resolveArcPreviewPath path
    let! result = loadViewResult previewPath
    return result |> Result.map (applyRequestedPathView path)
}
