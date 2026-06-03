module Swate.Components.Page.FileExplorer.FileExplorerGitLfsHelper

open Fable.Core
open Swate.Components.Page.FileExplorer.Types

let toggleLfsMark
        (setError: string option -> unit)
        (runToggle: string -> bool -> JS.Promise<Result<unit, string>>)
    : (FileItem -> bool -> unit) =
    fun item markAsLfs ->
        promise {
            match item.Path with
            | None -> ()
            | Some relativePath ->
                if System.String.IsNullOrWhiteSpace relativePath then
                    setError (Some "Cannot mark ARC root as a Git LFS file.")
                else
                    let! result = runToggle relativePath markAsLfs

                    match result with
                    | Ok _ -> setError None
                    | Error msg -> setError (Some $"Git LFS update failed for '{item.Name}': {msg}")
        }
        |> Promise.start

let private runLfsFileAction
        (setError: string option -> unit)
        (runAction: string -> JS.Promise<Result<unit, string>>)
        (rootError: string)
        (canRun: FileItem -> bool)
        (blockedMessage: FileItem -> string)
        (failureMessage: FileItem -> string -> string)
    : FileItem -> unit =
    fun item ->
        promise {
            match item.Path with
            | None -> ()
            | Some path when System.String.IsNullOrWhiteSpace path -> setError (Some rootError)
            | Some _ when not (Swate.Components.Page.FileExplorer.Helper.isLfs item) ->
                setError (Some $"'{item.Name}' is not marked as a Git LFS file.")
            | Some _ when not (canRun item) -> setError (Some(blockedMessage item))
            | Some path ->
                match! runAction path with
                | Ok _ -> setError None
                | Error msg -> setError (Some(failureMessage item msg))
        }
        |> Promise.start

let freeLocalLfsCopy
        (setError: string option -> unit)
        (runCleanup: string -> JS.Promise<Result<unit, string>>)
    : FileItem -> unit =
    runLfsFileAction
        setError
        runCleanup
        "Cannot free the ARC root as a Git LFS file."
        Swate.Components.Page.FileExplorer.Helper.hasLocalLfsCopy
        (fun item -> $"'{item.Name}' is already stored locally as an LFS pointer.")
        (fun item msg -> $"Git LFS cleanup failed for '{item.Name}': {msg}")

let downloadLfsFile
        (setError: string option -> unit)
        (runDownload: string -> JS.Promise<Result<unit, string>>)
    : FileItem -> unit =
    runLfsFileAction
        setError
        runDownload
        "Cannot download the ARC root as a Git LFS file."
        (Swate.Components.Page.FileExplorer.Helper.hasLocalLfsCopy >> not)
        (fun item -> $"'{item.Name}' is already downloaded.")
        (fun item msg -> $"Git LFS download failed for '{item.Name}': {msg}")

let private createLfsActionItem label icon (item: FileItem) disabled (action: FileItem -> unit) =
    if disabled then
        FileExplorerContextMenuItem.disabled label icon
    else
        FileExplorerContextMenuItem.forItem label icon action item

let lfsPillAction (item: FileItem) onDownloadLfsFile onFreeLocalLfsCopy =
    if item.IsDirectory then None
    elif Swate.Components.Page.FileExplorer.Helper.needsLfsDownload item then
        onDownloadLfsFile
        |> Option.map (createLfsActionItem "Download LFS file" "swt:fluent--cloud-arrow-down-24-regular" item false)
    elif Swate.Components.Page.FileExplorer.Helper.hasLocalLfsCopy item then
        onFreeLocalLfsCopy
        |> Option.map (createLfsActionItem "Free local LFS copy" "swt:fluent--document-arrow-up-20-regular" item false)
    else None

let contextMenuItemsWithDownload
    (item: FileItem)
    (onToggleLfsMark: FileItem -> bool -> unit)
    (onDownloadLfsFile: (FileItem -> unit) option)
    (onFreeLocalLfsCopy: (FileItem -> unit) option)
    =
    if item.IsDirectory then
        []
    else
        let isMarked = Swate.Components.Page.FileExplorer.Helper.isLfs item
        let needsDownload = Swate.Components.Page.FileExplorer.Helper.needsLfsDownload item
        let hasLocalCopy = Swate.Components.Page.FileExplorer.Helper.hasLocalLfsCopy item

        [
            FileExplorerContextMenuItem.create
                (if isMarked then "Unmark Git LFS" else "Mark Git LFS")
                (if isMarked then "swt:fluent--document-dismiss-24-regular" else "swt:fluent--document-add-24-regular")
                (fun () -> onToggleLfsMark item (not isMarked))
            yield!
                match onDownloadLfsFile with
                | Some handler when needsDownload || isMarked ->
                    [
                        createLfsActionItem
                            "Download LFS file"
                            "swt:fluent--cloud-arrow-down-24-regular"
                            item
                            (not needsDownload)
                            handler
                    ]
                | _ -> []
            yield!
                match onFreeLocalLfsCopy with
                | Some handler when hasLocalCopy || isMarked ->
                    [
                        createLfsActionItem
                            "Free local LFS copy"
                            "swt:fluent--document-arrow-up-20-regular"
                            item
                            (not hasLocalCopy)
                            handler
                    ]
                | _ -> []
            FileExplorerContextMenuItem.disabled
                (if isMarked then "Git LFS: marked" else "Git LFS: not marked")
                "swt:fluent--tag-24-regular"
        ]

let contextMenuItems (item: FileItem) (onToggleLfsMark: FileItem -> bool -> unit) (onFreeLocalLfsCopy: (FileItem -> unit) option) =
    contextMenuItemsWithDownload item onToggleLfsMark None onFreeLocalLfsCopy
