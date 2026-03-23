module Renderer.Components.ArcObjectExplorer

open System
open ARCtrl
open Feliz
open Swate.Components
open Swate.Electron.Shared
open Swate.Electron.Shared.GitTypes
open Swate.Electron.Shared.IPCTypes
open Swate.Electron.Shared.IPCTypes.IPCTypesHelper


let private toPreviewState =
    function
    | Some(PageState.Text content) -> ArcObjectPreviewState.Text content
    | Some(PageState.Error message) -> ArcObjectPreviewState.Error message
    | _ -> ArcObjectPreviewState.NoneLoaded

let private createArcExplorerServices
    (setPageState: PageState option -> unit)
    : ArcExplorerServices =
    let runToggleLfsMark (repoPath: string) (relativePath: string) (markAsLfs: bool) = promise {
        let request: GitLfsRequest = {
            RequestId = Guid.NewGuid().ToString()
            RepoPath = repoPath
            Command =
                if markAsLfs then
                    GitLfsCommand.Track
                else
                    GitLfsCommand.Untrack
            FilePath = Some relativePath
            TimeoutMs = Some 10000
        }

        let! result = Api.ipcArcVaultApi.runGitLfs (unbox null) request

        return
            match result with
            | Ok _ -> Ok()
            | Error exn -> Error exn.Message
    }

    let setStatusMessage (errorMsg: string option) =
        match errorMsg with
        | Some msg -> setPageState (Some(PageState.Error msg))
        | None -> setPageState None

    let openPreview (previewPath: string) = promise {
        let! result = Api.ipcArcVaultApi.openFile (unbox null) previewPath

        return
            match result with
            | Ok data ->
                setPageState (Some data)
                Ok()
            | Error exn -> Error exn.Message
    }

    {
        openPreview = openPreview
        setStatusMessage = setStatusMessage
        runToggleLfsMark = runToggleLfsMark
    }

[<ReactComponent>]
let Content
    (arcFileState: ArcFiles option)
    (pageState: PageState option)
    (setArcFileState: ArcFiles option -> unit)
    (setSelectedExplorerItemId: string option -> unit)
    (setSelectedTreeItemPath: string option -> unit)
    (setPageState: PageState option -> unit)
    =
    let appCtx = React.useContext Renderer.Context.AppStateCtx.AppStateCtx

    let workspaceCtx =
        React.useContext Renderer.Context.WorkspaceStateCtx.WorkspaceStateCtx

    let rootRepoPath =
        match appCtx.state with
        | AppState.ARC arcPath -> Some arcPath
        | AppState.Init -> None

    let props: ArcObjectExplorerProps = {
        rootRepoPath = rootRepoPath
        nodes = workspaceCtx.state.ArcExplorerTree
        selectedExplorerItemId = workspaceCtx.state.SelectedExplorerItemId
        selectedTreeItemPath = workspaceCtx.state.SelectedTreeItemPath
        arcFileState = arcFileState
        previewState = toPreviewState pageState
        setArcFileState = setArcFileState
        setSelectedExplorerItemId = setSelectedExplorerItemId
        setSelectedTreeItemPath = setSelectedTreeItemPath
        services = createArcExplorerServices setPageState
    }

    Swate.Components.ArcObjectExplorerContent.Main(props)

