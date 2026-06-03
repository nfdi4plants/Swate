module Renderer.Components.FileExplorerLfs

open Fable.Core
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

let createToggleLfsMark
    (enqueueErrorModal: ErrorModalRequest -> unit)
    (runToggle: string -> bool -> JS.Promise<Result<unit, string>>)
    : (FileItem -> bool -> unit) =
    let setError (errorMsg: string option) =
        match errorMsg with
        | Some msg -> enqueueErrorModal (ErrorModalRequest.create(msg, title = "Git LFS update failed"))
        | None -> ()

    Swate.Components.Page.FileExplorer.FileExplorerGitLfsHelper.toggleLfsMark setError runToggle

let createFreeLocalLfsCopy
    (enqueueErrorModal: ErrorModalRequest -> unit)
    (runCleanup: string -> JS.Promise<Result<unit, string>>)
    : (FileItem -> unit) =
    let setError (errorMsg: string option) =
        match errorMsg with
        | Some msg -> enqueueErrorModal (ErrorModalRequest.create(msg, title = "Git LFS cleanup failed"))
        | None -> ()

    Swate.Components.Page.FileExplorer.FileExplorerGitLfsHelper.freeLocalLfsCopy setError runCleanup

let withLfsContextMenuItems
    (item: FileItem)
    (toggleLfsMark: FileItem -> bool -> unit)
    (freeLocalLfsCopy: FileItem -> unit)
    (baseItems: ContextMenuItem list)
    : ContextMenuItem list =
    baseItems
    @ Swate.Components.Page.FileExplorer.FileExplorerGitLfsHelper.contextMenuItems item toggleLfsMark (Some freeLocalLfsCopy)

let createContextMenuItems
    (enqueueErrorModal: ErrorModalRequest -> unit)
    (baseItems: FileItem -> ContextMenuItem list)
    : FileItem -> ContextMenuItem list =
    let toggleLfsMark =
        createToggleLfsMark enqueueErrorModal Renderer.Components.ARCHelper.runToggleLfsMark

    let freeLocalLfsCopy =
        createFreeLocalLfsCopy enqueueErrorModal Renderer.Components.ARCHelper.runFreeLocalLfsCopy

    fun item -> withLfsContextMenuItems item toggleLfsMark freeLocalLfsCopy (baseItems item)
