module Renderer.Components.FileExplorerLfs

open Fable.Core
open Swate.Components.ErrorModal
open Swate.Components.FileExplorer.Types
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

let createToggleLfsMark
    (enqueueErrorModal: ErrorModalRequest -> unit)
    (arcScopeId: string option)
    (runToggle: string -> bool -> JS.Promise<Result<unit, string>>)
    : (FileItem -> bool -> unit) =
    let setError (errorMsg: string option) =
        match errorMsg with
        | Some msg ->
            enqueueErrorModal (ErrorModalRequest.create(msg, title = "Git LFS update failed", ?scopeId = arcScopeId))
        | None -> ()

    Swate.Components.FileExplorer.FileExplorerGitLfsHelper.toggleLfsMark setError runToggle

let createFreeLocalLfsCopy
    (enqueueErrorModal: ErrorModalRequest -> unit)
    (arcScopeId: string option)
    (runCleanup: string -> JS.Promise<Result<unit, string>>)
    : (FileItem -> unit) =
    let setError (errorMsg: string option) =
        match errorMsg with
        | Some msg ->
            enqueueErrorModal (ErrorModalRequest.create(msg, title = "Git LFS cleanup failed", ?scopeId = arcScopeId))
        | None -> ()

    Swate.Components.FileExplorer.FileExplorerGitLfsHelper.freeLocalLfsCopy setError runCleanup

let withLfsContextMenuItems
    (item: FileItem)
    (toggleLfsMark: FileItem -> bool -> unit)
    (freeLocalLfsCopy: FileItem -> unit)
    (baseItems: ContextMenuItem list)
    : ContextMenuItem list =
    baseItems
    @ Swate.Components.FileExplorer.FileExplorerGitLfsHelper.contextMenuItems item toggleLfsMark (Some freeLocalLfsCopy)

let createContextMenuItems
    (enqueueErrorModal: ErrorModalRequest -> unit)
    (arcScopeId: string option)
    (baseItems: FileItem -> ContextMenuItem list)
    : FileItem -> ContextMenuItem list =
    let toggleLfsMark =
        createToggleLfsMark enqueueErrorModal arcScopeId Renderer.Components.ARCHelper.runToggleLfsMark

    let freeLocalLfsCopy =
        createFreeLocalLfsCopy enqueueErrorModal arcScopeId Renderer.Components.ARCHelper.runFreeLocalLfsCopy

    fun item -> withLfsContextMenuItems item toggleLfsMark freeLocalLfsCopy (baseItems item)
