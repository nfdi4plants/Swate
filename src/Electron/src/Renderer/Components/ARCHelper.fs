module Renderer.Components.ARCHelper

open System
open Renderer.Types
open Swate.Components
open Swate.Components.Shared
open Swate.Electron.Shared.FileIOHelper
open Swate.Electron.Shared.FileIOTypes
open Swate.Electron.Shared.GitTypes

type PreviewLoadResult = {
    RendererPageState: Renderer.Types.PageState
    ArcFileState: ArcFiles option
    PreviewState: Swate.Components.Shared.PageState option
}

let private previewStateOfRendererPageState =
    function
    | Renderer.Types.PageState.ArcFilePage arcFile -> Some(Swate.Components.Shared.PageState.ArcFilePage arcFile)
    | Renderer.Types.PageState.TextPage content -> Some(Swate.Components.Shared.PageState.TextPage content)
    | Renderer.Types.PageState.UnknownPage -> Some Swate.Components.Shared.PageState.UnknownPage
    | Renderer.Types.PageState.LandingDraftPage -> Some Swate.Components.Shared.PageState.LandingDraftPage
    | Renderer.Types.PageState.NotesDraftPage -> Some Swate.Components.Shared.PageState.NotesDraftPage
    | Renderer.Types.PageState.NotesSearchPage -> Some Swate.Components.Shared.PageState.NotesSearchPage
    | Renderer.Types.PageState.ErrorPage errMsg -> Some(Swate.Components.Shared.PageState.ErrorPage errMsg)
    | Renderer.Types.PageState.GitDiffPage _
    | Renderer.Types.PageState.GitMergeConflictPage _
    | Renderer.Types.PageState.GitUnsupportedPage _
    | Renderer.Types.PageState.DataHubBrowser -> None

let previewLoadResultOfDto (data: FileContentDTO) =
    let pageState = Renderer.Types.PageState.fromFileContentDTO data

    {
        RendererPageState = pageState
        ArcFileState = FileContentDTO.toArcFile data
        PreviewState = previewStateOfRendererPageState pageState
    }

let applyLoadedPreview
    (setPageState: Renderer.Types.PageState option -> unit)
    (setArcFileState: ArcFiles option -> unit)
    (setPreviewState: Swate.Components.Shared.PageState option -> unit)
    (setStatusMessage: string option -> unit)
    (loaded: PreviewLoadResult)
    =
    setPageState (Some loaded.RendererPageState)
    setArcFileState loaded.ArcFileState
    setPreviewState loaded.PreviewState
    setStatusMessage None

let clearArcObjectPreview
    (setArcFileState: ArcFiles option -> unit)
    (setPreviewState: Swate.Components.Shared.PageState option -> unit)
    (setStatusMessage: string option -> unit)
    =
    setArcFileState None
    setPreviewState None
    setStatusMessage None

let applyPreviewError
    (setPageState: Renderer.Types.PageState option -> unit)
    (setArcFileState: ArcFiles option -> unit)
    (setPreviewState: Swate.Components.Shared.PageState option -> unit)
    (setStatusMessage: string option -> unit)
    (errorMessage: string)
    =
    setPageState (Some(Renderer.Types.PageState.ErrorPage errorMessage))
    setArcFileState None
    setPreviewState (Some(Swate.Components.Shared.PageState.ErrorPage errorMessage))
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
    (setPageState: Renderer.Types.PageState option -> unit)
    (setArcFileState: ArcFiles option -> unit)
    (setPreviewState: Swate.Components.Shared.PageState option -> unit)
    (setStatusMessage: string option -> unit)
    : ARCExplorerServices
    =
    let toggleLfsMark = runToggleLfsMark

    {
        openView =
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
