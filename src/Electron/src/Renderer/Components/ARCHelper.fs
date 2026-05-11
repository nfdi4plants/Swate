module Renderer.Components.ARCHelper

open System
open Feliz
open Swate.Electron.Shared.FileIOHelper
open Swate.Electron.Shared.FileIOTypes
open Swate.Electron.Shared.GitTypes
open Swate.Components.Shared

/// This is boilerplate we do not need. Just ensure that the path is normalized inside `useAppStateCtx`. As we do this so many times, we should focus on the base information and ensure it is normalized at the source, not every time we use it.
///
/// In addition, i have seen component using `useCurrentArcScopeId` AND `useAppStateCtx` due to confusion.
[<Hook>]
let useCurrentArcScopeId () =
    let appStateCtx = Renderer.Context.AppStateContext.useAppStateCtx ()

    appStateCtx
    |> Option.map PathHelpers.normalizePath
    |> Option.bind (fun path -> if String.IsNullOrWhiteSpace path then None else Some path)

/// TODO: Check if this type is necessary. Looks like it just is an additonal wrapper around PageState? Not sure why we need this + helper boilerplate below.
type ViewLoadResult = {
    RendererPageState: Renderer.Types.PageState
}

let viewLoadResultOfDto (data: FileContentDTO) =
    let pageState = Renderer.Types.PageState.fromFileContentDTO data

    { RendererPageState = pageState }

let applyLoadedView (setPageState: Renderer.Types.PageState option -> unit) (loaded: ViewLoadResult) =
    setPageState (Some loaded.RendererPageState)

let applyViewError (setPageState: Renderer.Types.PageState option -> unit) (errorMessage: string) =
    setPageState (Some(Renderer.Types.PageState.ErrorPage errorMessage))

let runToggleLfsMark (relativePath: string) (markAsLfs: bool) = promise {
    let request: GitLfsRequest = {
        RequestId = Guid.NewGuid().ToString()
        RepoPath = ""
        Command =
            if markAsLfs then
                GitLfsCommand.Track
            else
                GitLfsCommand.Untrack
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

    if previewPath <> PathHelpers.normalizePath path then
        console.log ($"[Renderer] Redirecting Datamap click to file: {previewPath}")
    else
        console.log ($"[Renderer] Opening file: {previewPath}")

    return! loadViewResult previewPath
}
