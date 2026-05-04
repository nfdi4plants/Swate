module Renderer.Components.ARCHelper

open System
open Feliz
open Swate.Electron.Shared.FileIOHelper
open Swate.Electron.Shared.FileIOTypes
open Swate.Electron.Shared.GitTypes


[<Hook>]
let useCurrentArcScopeId () =
    let appStateCtx = Renderer.Context.AppStateContext.useAppStateCtx ()

    appStateCtx
    |> Option.map normalizePath
    |> Option.bind (fun path ->
        if String.IsNullOrWhiteSpace path then
            None
        else
            Some path
    )

type ViewLoadResult = {
    RendererPageState: Renderer.Types.PageState
}

let viewLoadResultOfDto (data: FileContentDTO) =
    let pageState = Renderer.Types.PageState.fromFileContentDTO data

    { RendererPageState = pageState }

let applyLoadedView
    (setPageState: Renderer.Types.PageState option -> unit)
    (loaded: ViewLoadResult)
    =
    setPageState (Some loaded.RendererPageState)

let applyViewError
    (setPageState: Renderer.Types.PageState option -> unit)
    (errorMessage: string)
    =
    setPageState (Some(Renderer.Types.PageState.ErrorPage errorMessage))

let runToggleLfsMark (relativePath: string) (markAsLfs: bool) = promise {
    let request: GitLfsRequest = {
        RequestId = Guid.NewGuid().ToString()
        RepoPath = ""
        Command = if markAsLfs then GitLfsCommand.Track else GitLfsCommand.Untrack
        FilePath = Some relativePath
        TimeoutMs = Some 10000
    }

    let! result = Api.ipcArcVaultApi.runGitLfs request

    return
        match result with
        | Ok _ -> Ok()
        | Error exn -> Error exn.Message
}

let private loadViewResult (previewPath: string) = promise {
    let! result = Api.ipcArcVaultApi.openFile previewPath

    return
        match result with
        | Ok data -> Ok(viewLoadResultOfDto data)
        | Error exn -> Error exn.Message
}

let openView (path: string) = promise {
    let previewPath = resolveArcPreviewPath path

    if previewPath <> normalizePath path then
        console.log ($"[Renderer] Redirecting Datamap click to file: {previewPath}")
    else
        console.log ($"[Renderer] Opening file: {previewPath}")

    return! loadViewResult previewPath
}
