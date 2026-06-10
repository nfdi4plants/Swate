module Renderer.Components.Helper.ArcViewHelper

open Browser.Dom
open Fable.Core
open Swate.Components.Shared
open Swate.Electron.Shared.FileIOHelper

let private loadViewResult (previewPath: string) : JS.Promise<Result<Renderer.Types.PageState, string>> =
    promise {
        let! result = Api.ipcArcVaultApi.openFile previewPath

        match result with
        | Ok data -> return Ok(Renderer.Types.PageState.fromFileContentDTO data)
        | Error exn ->
            return Error exn.Message
    }

let openView (path: string) : JS.Promise<Result<Renderer.Types.PageState, string>> =
    promise {
        let previewPath = resolveArcPreviewPath path

        if previewPath <> PathHelpers.normalizePath path then
            console.log ($"[Renderer] Redirecting Datamap click to file: {previewPath}")
        else
            console.log ($"[Renderer] Opening file: {previewPath}")

        return! loadViewResult previewPath
    }
