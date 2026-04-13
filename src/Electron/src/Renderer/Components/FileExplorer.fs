module Renderer.Components.FileExplorer


open Renderer.Components.ARCHelper
open Swate.Components
open Swate.Components.Contexts
open Swate.Components.FileExplorerTypes
open Swate.Components.Shared
open Swate.Electron.Shared.FileIOHelper
open Swate.Electron.Shared.FileIOTypes
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
                FileTree.createFolder parent.name (Some parent.path) FileItemIcon.Folder with
                    Id = parent.path
                    IsExpanded =
                        selectedTreeItemPath
                        |> Option.exists (fun focusedPath -> isSameOrDescendantPath focusedPath parent.path)
                    IsLFS = parent.isLfs
                    Children = Some tmp
            }
        | false ->
            Some {
                FileTree.createFile parent.name (Some parent.path) FileItemIcon.Document with
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
    let arcObjectCtx = Renderer.Context.ArcObjectExplorerCtx.useArcObjectExplorer ()
    let errorModal = ErrorModal.Context.useErrorModal ()
    let arcScopeId = useCurrentArcScopeId ()

    match fileStateCtx.state.FileTree with
    | [||] -> EmptyFileTreePlaceholder()
    | _ ->

        let fileTree = fileStateCtx.state.FileTree |> toFileTreeNode

        let fileItem = loopPaths fileStateCtx.state.Selection.TreePath fileTree

        let setError (errorMsg: string option) =
            match errorMsg with
            | Some msg -> errorModal.enqueue (ErrorModalRequest.create(msg, title = "Git LFS update failed", ?scopeId = arcScopeId))
            | None -> ()

        let toggleLfsMark =
            FileExplorerGitLfsHelper.ToggleLfsMark(setError, Renderer.Components.ARCHelper.runToggleLfsMark)

        let contextMenuItems (item: FileItem) =
            FileExplorerGitLfsHelper.ContextMenuItems(item, toggleLfsMark)

        let openPreview (item: FileItem) =
            promise {
                match item.Path with
                | None ->
                    errorModal.enqueue (
                        ErrorModalRequest.create($"File '{item.Name}' has no path.", title = "Preview failed", ?scopeId = arcScopeId)
                    )
                | Some path when item.IsDirectory ->
                    let selectedPath = normalizePath path
                    fileStateCtx.setSelection (ArcSelection.forTreePath (Some selectedPath))

                    Renderer.Components.ARCHelper.clearArcObjectPreview
                        arcObjectCtx.setArcFileState
                        arcObjectCtx.setPreviewState
                        arcObjectCtx.setStatusMessage

                    pageStateCtx.setState None
                | Some path ->
                    let selectedPath = normalizePath path
                    fileStateCtx.setSelection (ArcSelection.forTreePath (Some selectedPath))

                    let! result = Renderer.Components.ARCHelper.openView selectedPath

                    match result with
                    | Ok loaded ->
                        console.log ("[Renderer] Received data, processing...")

                        Renderer.Components.ARCHelper.applyLoadedView
                            pageStateCtx.setState
                            arcObjectCtx.setArcFileState
                            arcObjectCtx.setPreviewState
                            arcObjectCtx.setStatusMessage
                            loaded
                    | Error errorMessage ->
                        let fullErrorMessage = $"Could not open preview for '{item.Name}': {errorMessage}"
                        console.log ($"[Renderer] Error: {fullErrorMessage}")

                        Renderer.Components.ARCHelper.applyViewError
                            pageStateCtx.setState
                            arcObjectCtx.setArcFileState
                            arcObjectCtx.setPreviewState
                            arcObjectCtx.setStatusMessage
                            fullErrorMessage
            }
            |> Promise.start

        match fileItem with

        | Some fileItem ->
            Swate.Components.FileExplorer.FileExplorer(
                initialItems = [ fileItem ],
                onItemClick = openPreview,
                onContextMenu = contextMenuItems,
                selectedItemId = fileStateCtx.state.Selection.TreePath
            )
        | None -> EmptyFileTreePlaceholder()
