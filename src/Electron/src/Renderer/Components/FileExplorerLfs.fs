module Renderer.Components.FileExplorerLfs

open Swate.Components.Primitive.ErrorModal.Types
open Swate.Components.Page.FileExplorer.Types
open Swate.Electron.Shared.FileIOTypes


type FileTreeNodeLfsState = {
    IsLFS: bool option
    IsLFSPointer: bool option
    Downloaded: bool option
    Size: int64 option
    SizeFormatted: string option
}

let private mapLfsSize (sizeBytes: float option) =
    let size = sizeBytes |> Option.map int64
    size, (size |> Option.map FileTree.formatSize)

let getFileTreeNodeLfsState (node: FileTreeNode) : FileTreeNodeLfsState =
    let lfsSize, lfsSizeFormatted = mapLfsSize (node.lfs |> Option.map _.size)

    {
        IsLFS = node.lfs |> Option.map (fun _ -> true)
        IsLFSPointer = node.lfs |> Option.map (fun info -> not info.checkout)
        Downloaded = node.lfs |> Option.map _.downloaded
        Size = lfsSize
        SizeFormatted = lfsSizeFormatted
    }

let withFileTreeNodeLfsState (node: FileTreeNode) (item: FileItem) : FileItem =
    let lfsState = getFileTreeNodeLfsState node

    {
        item with
            IsLFS = lfsState.IsLFS
            IsLFSPointer = lfsState.IsLFSPointer
            Downloaded = lfsState.Downloaded
            Size = lfsState.Size
            SizeFormatted = lfsState.SizeFormatted
    }

let private createErrorReporter
    (enqueueErrorModal: ErrorModalRequest -> unit)
    (arcScopeId: string option)
    (title: string)
    =
    function
    | Some msg -> enqueueErrorModal (ErrorModalRequest.create(msg, title = title, ?scopeId = arcScopeId))
    | None -> ()

let private createLfsAction title action enqueueErrorModal arcScopeId runAction =
    action (createErrorReporter enqueueErrorModal arcScopeId title) runAction

let createToggleLfsMark enqueueErrorModal arcScopeId runToggle =
    createLfsAction "Git LFS update failed" Swate.Components.Page.FileExplorer.FileExplorerGitLfsHelper.toggleLfsMark enqueueErrorModal arcScopeId runToggle

let createFreeLocalLfsCopy enqueueErrorModal arcScopeId runCleanup =
    createLfsAction "Git LFS cleanup failed" Swate.Components.Page.FileExplorer.FileExplorerGitLfsHelper.freeLocalLfsCopy enqueueErrorModal arcScopeId runCleanup

let createDownloadLfsFile enqueueErrorModal arcScopeId runDownload =
    createLfsAction "Git LFS download failed" Swate.Components.Page.FileExplorer.FileExplorerGitLfsHelper.downloadLfsFile enqueueErrorModal arcScopeId runDownload

let createContextMenuItems enqueueErrorModal arcScopeId baseItems =
    let toggleLfsMark =
        createToggleLfsMark enqueueErrorModal arcScopeId Renderer.Components.ARCHelper.runToggleLfsMark

    let downloadLfsFile =
        createDownloadLfsFile enqueueErrorModal arcScopeId Renderer.Components.ARCHelper.runDownloadLfsFile

    let freeLocalLfsCopy =
        createFreeLocalLfsCopy enqueueErrorModal arcScopeId Renderer.Components.ARCHelper.runFreeLocalLfsCopy

    fun item ->
        baseItems item
        @ Swate.Components.Page.FileExplorer.FileExplorerGitLfsHelper.contextMenuItemsWithDownload item toggleLfsMark (Some downloadLfsFile) (Some freeLocalLfsCopy)

let createFileActionItems enqueueErrorModal arcScopeId baseItems =
    let downloadLfsFile =
        createDownloadLfsFile enqueueErrorModal arcScopeId Renderer.Components.ARCHelper.runDownloadLfsFile

    let freeLocalLfsCopy =
        createFreeLocalLfsCopy enqueueErrorModal arcScopeId Renderer.Components.ARCHelper.runFreeLocalLfsCopy

    fun item ->
        baseItems item
        @ Swate.Components.Page.FileExplorer.FileExplorerGitLfsHelper.fileActionItems item (Some downloadLfsFile) (Some freeLocalLfsCopy)
