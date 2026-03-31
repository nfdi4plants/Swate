module Renderer.Components.ARCHelper

open Swate.Electron.Shared
open ARCtrl
open System
open Swate.Components
open Swate.Electron.Shared.FileIOHelper
open Swate.Electron.Shared.FileIOTypes
open Swate.Electron.Shared.GitTypes
open Renderer.Types

type PreviewLoadResult = {
    PageState: PageState
    ArcFileState: ArcFiles option
    PreviewState: PageState option
}

let previewLoadResultOfDto (data: FileContentDTO) =
    let pageState = pageStateOfFileContentDTO data

    {
        PageState = pageState
        ArcFileState = FileContentDTO.toArcFile data
        PreviewState = Some pageState
    }

let applyLoadedPreview
    (setPageState: PageState option -> unit)
    (setArcFileState: ArcFiles option -> unit)
    (setPreviewState: PageState option -> unit)
    (setStatusMessage: string option -> unit)
    (loaded: PreviewLoadResult)
    =
    setPageState (Some loaded.PageState)
    setArcFileState loaded.ArcFileState
    setPreviewState loaded.PreviewState
    setStatusMessage None

let clearArcObjectPreview
    (setArcFileState: ArcFiles option -> unit)
    (setPreviewState: PageState option -> unit)
    (setStatusMessage: string option -> unit)
    =
    setArcFileState None
    setPreviewState None
    setStatusMessage None

let applyPreviewError
    (setPageState: PageState option -> unit)
    (setArcFileState: ArcFiles option -> unit)
    (setPreviewState: PageState option -> unit)
    (setStatusMessage: string option -> unit)
    (errorMessage: string)
    =
    setPageState (Some(PageState.ErrorPage errorMessage))
    setArcFileState None
    setPreviewState (Some(PageState.ErrorPage errorMessage))
    setStatusMessage (Some errorMessage)

let runToggleLfsMark (relativePath: string) (markAsLfs: bool) = promise {
    let request: GitLfsRequest = {
        RequestId = Guid.NewGuid().ToString()
        RepoPath = ""
        Command = if markAsLfs then GitLfsCommand.Track else GitLfsCommand.Untrack
        FilePath = Some relativePath
        TimeoutMs = Some 10000
    }

    let! result = Api.ipcArcVaultApi.runGitLfs (unbox null) request

    return
        match result with
        | Ok _ -> Ok()
        | Error exn -> Error exn.Message
}

let private loadPreviewResult (previewPath: string) = promise {
    let! result = Api.ipcArcVaultApi.openFile (unbox null) previewPath

    return
        match result with
        | Ok data -> Ok(previewLoadResultOfDto data)
        | Error exn -> Error exn.Message
}

let openPreview (path: string) = promise {
    let previewPath = resolveArcPreviewPath path

    if previewPath <> normalizePath path then
        console.log ($"[Renderer] Redirecting Datamap click to file: {previewPath}")
    else
        console.log ($"[Renderer] Opening file: {previewPath}")

    return! loadPreviewResult previewPath
}

let createArcExplorerServices
    (setPageState: PageState option -> unit)
    (setArcFileState: ArcFiles option -> unit)
    (setPreviewState: PageState option -> unit)
    (setStatusMessage: string option -> unit)
    : ARCExplorerServices
    =
    let toggleLfsMark = runToggleLfsMark

    {
        openPreview =
            fun previewPath -> promise {
                let! result = loadPreviewResult previewPath

                match result with
                | Ok loaded ->
                    applyLoadedPreview setPageState setArcFileState setPreviewState setStatusMessage loaded
                    return Ok()
                | Error errorMessage ->
                    applyPreviewError setPageState setArcFileState setPreviewState setStatusMessage errorMessage
                    return Error errorMessage
            }
        setStatusMessage = setStatusMessage
        runToggleLfsMark =
            fun _rootRepoPath relativePath markAsLfs ->
                toggleLfsMark relativePath markAsLfs
    }
