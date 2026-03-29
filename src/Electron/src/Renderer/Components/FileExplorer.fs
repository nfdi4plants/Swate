module Renderer.Components.FileExplorer

open System
open Browser.Dom
open Renderer
open Swate.Components.FileExplorerTypes
open Swate.Electron.Shared.FileIOHelper
open Swate.Electron.Shared.FileIOTypes
open Swate.Electron.Shared.GitTypes
open Feliz


module private FileExplorerHelper =

    let rec loopPaths (selectedTreeItemPath: string option) (parent: FileTreeNode) =
        match parent.isDirectory with
        | true ->
            let tmp =
                let ra = ResizeArray(parent.children.Values)

                ra.ToArray()
                |> Array.map (fun entry -> loopPaths selectedTreeItemPath entry)
                |> Array.choose id
                |> List.ofArray

            Some {
                FileTree.createFolder parent.name (Some parent.path) "swt:fluent--folder-24-regular" with
                    Id = parent.path
                    IsExpanded =
                        selectedTreeItemPath
                        |> Option.exists (fun focusedPath -> isSameOrDescendantPath focusedPath parent.path)
                    IsLFS = parent.isLfs
                    Children = Some tmp
            }
        | false ->
            Some {
                FileTree.createFile parent.name (Some parent.path) "swt:fluent--document-24-regular" with
                    Id = parent.path
                    IsLFS = parent.isLfs
            }

open FileExplorerHelper

[<ReactComponent>]
let EmptyFileTreePlaceholder () =
    Html.div [
        prop.className "swt:p-4 swt:text-center swt:text-gray-500"
        prop.text "No files found."
    ]

[<ReactComponent>]
let FileTree () =

    let pageStateCtx = Renderer.Context.PageStateCtx.usePageState ()
    let fileStateCtx = Renderer.Context.FileStateCtx.useFileState ()

    match fileStateCtx.state.FileTree with
    | [||] -> EmptyFileTreePlaceholder()
    | _ ->

        let fileTree = fileStateCtx.state.FileTree |> toFileTreeNode

        let fileItem = loopPaths fileStateCtx.state.SelectedTreeItemPath fileTree

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


            // This seems to behave oddly. It runs some git lfs command and then refreshes filetree in arcvault. But it does it with a type that does not track lfs?
            let! result = Api.ipcArcVaultApi.runGitLfs (unbox null) request

            return
                match result with
                | Ok _ -> Ok()
                | Error exn -> Error exn.Message
        }

        let setError (errorMsg: string option) =
            match errorMsg with
            | Some msg -> pageStateCtx.setState (Some(PageState.ErrorPage msg))
            | None -> pageStateCtx.setState (None)

        let toggleLfsMark =
            FileExplorerGitLfsHelper.ToggleLfsMark(setError, runToggleLfsMark)

        let contextMenuItems (item: FileItem) =
            FileExplorerGitLfsHelper.ContextMenuItems(item, toggleLfsMark)

        let openPreview (item: FileItem) =
            promise {
                match item.Path with
                | None -> pageStateCtx.setState (Some(PageState.ErrorPage $"File '{item.Name}' has no path."))
                | Some _ when item.IsDirectory -> pageStateCtx.setState (None)
                | Some path ->
                let previewPath = resolveArcPreviewPath path

                if previewPath <> normalizePath path then
                    console.log ($"[Renderer] Redirecting Datamap click to file: {previewPath}")
                else
                    console.log ($"[Renderer] Opening file: {previewPath}")

                fileStateCtx.setSelectedTreeItemPath (Some path)

                let! result = Api.ipcArcVaultApi.openFile (unbox null) previewPath

                match result with
                | Ok data ->
                    let pageState = PageState.fromFileContentDTO data
                    console.log ("[Renderer] Received data, processing...")
                    pageStateCtx.setState (Some pageState)
                | Error exn ->
                    console.log ($"[Renderer] Error: {exn.Message}")

                    pageStateCtx.setState (
                        Some(PageState.ErrorPage $"Could not open preview for '{item.Name}': {exn.Message}")
                    )
            }
            |> Promise.start

        match fileItem with

        | Some fileItem ->
            Swate.Components.FileExplorer.FileExplorer(
                initialItems = [ fileItem ],
                onItemClick = openPreview,
                onContextMenu = contextMenuItems,
                selectedItemId = fileStateCtx.state.SelectedTreeItemPath
            )
        | None -> EmptyFileTreePlaceholder()
