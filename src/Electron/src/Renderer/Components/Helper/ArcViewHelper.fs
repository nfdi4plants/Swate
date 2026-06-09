module Renderer.Components.Helper.ArcViewHelper

open Browser.Dom
open Fable.Core
open Swate.Components.Shared
open Swate.Electron.Shared.FileIOHelper
open Swate.Electron.Shared.FileIOTypes

type ViewLoadResult = {
    RendererPageState: Renderer.Types.PageState
}

let viewLoadResultOfDto (data: FileContentDTO) : ViewLoadResult =
    let pageState = Renderer.Types.PageState.fromFileContentDTO data

    { RendererPageState = pageState }

let applyLoadedView (setPageState: Renderer.Types.PageState option -> unit) (loaded: ViewLoadResult) =
    setPageState (Some loaded.RendererPageState)

let applyViewError (setPageState: Renderer.Types.PageState option -> unit) (errorMessage: string) =
    setPageState (Some(Renderer.Types.PageState.ErrorPage errorMessage))

let private loadViewResult (previewPath: string) : JS.Promise<Result<ViewLoadResult, string>> =
    promise {
        let! result = Api.ipcArcVaultApi.openFile previewPath

        match result with
        | Ok data ->
            return Ok(viewLoadResultOfDto data)
        | Error exn ->
            return Error exn.Message
    }

let openView (path: string) : JS.Promise<Result<ViewLoadResult, string>> =
    promise {
        let previewPath = resolveArcPreviewPath path

        if previewPath <> PathHelpers.normalizePath path then
            console.log ($"[Renderer] Redirecting Datamap click to file: {previewPath}")
        else
            console.log ($"[Renderer] Opening file: {previewPath}")

        return! loadViewResult previewPath
    }
