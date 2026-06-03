module Renderer.Components.ARCHelper

open System
open Swate.Components
open Swate.Electron.Shared.FileIOHelper
open Swate.Electron.Shared.FileIOTypes
open Swate.Electron.Shared.GitTypes
open Swate.Components.Shared

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

let runFreeLocalLfsCopy (relativePath: string) = promise {
    let request: GitLfsFreeLocalCopyRequest = { Path = relativePath }

    let! result = Renderer.GitApiClient.gitLfsFreeLocalCopy request

    return
        match result with
        | Ok operation when operation.Success -> Ok()
        | Ok operation -> Error(operation.Message |> Option.defaultValue "Git LFS cleanup failed.")
        | Error message -> Error message
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
