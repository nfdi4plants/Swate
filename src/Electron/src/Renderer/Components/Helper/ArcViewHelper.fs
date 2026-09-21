module Renderer.Components.Helper.ArcViewHelper

open Fable.Core
open Renderer.Components.Helper.ArcViewSelection
open Swate.Components.Shared
open Swate.Electron.Shared.FileIOHelper

let private loadViewResult
    (previewPath: string)
    (publishArcFile: ArcFiles -> unit)
    : JS.Promise<Result<Renderer.Types.PageState, string>> =
    promise {
        let! result = Api.ipcArcVaultApi.openFile previewPath

        return
            result
            |> Result.map (fun dto -> Renderer.Types.PageState.fromFileContentDTO (dto, publishArcFile))
            |> Result.mapError _.Message
    }

let openView (publishArcFile: ArcFiles -> unit) (path: string) : JS.Promise<Result<Renderer.Types.PageState, string>> = promise {
    let previewPath = resolveArcPreviewPath path
    let! result = loadViewResult previewPath publishArcFile
    return result |> Result.map (applyRequestedPathView path)
}
