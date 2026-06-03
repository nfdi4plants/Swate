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

let private isLfs (item: FileItem) = item.IsLFS = Some true

let private needsLfsDownload item = isLfs item && (item.Downloaded <> Some true || item.IsLFSPointer = Some true)

let private hasLocalLfsCopy item = isLfs item && item.Downloaded = Some true && item.IsLFSPointer <> Some true

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
            | Some _ when not (isLfs item) -> setError (Some $"'{item.Name}' is not marked as a Git LFS file.")
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
        hasLocalLfsCopy
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
        (hasLocalLfsCopy >> not)
        (fun item -> $"'{item.Name}' is already downloaded.")
        (fun item msg -> $"Git LFS download failed for '{item.Name}': {msg}")

let private menuItem label icon disabled onClick =
    if disabled then
        FileExplorerContextMenuItem.disabled label icon
    else
        FileExplorerContextMenuItem.create label icon onClick

let private lfsMenuItem label icon (item: FileItem) disabled (action: FileItem -> unit) =
    menuItem label icon disabled (fun () -> action item)

let private downloadLfsMenuItem = lfsMenuItem "Download LFS file" "swt:fluent--cloud-arrow-down-24-regular"

let private freeLocalLfsCopyMenuItem =
    lfsMenuItem "Free local LFS copy" "swt:fluent--document-arrow-up-20-regular"

let private markedLfsActionItems isMarked isEnabled createItem action =
    match action with
    | Some handler when isEnabled || isMarked -> [ createItem (not isEnabled) handler ]
    | _ -> []

let private lfsStatusMenuItem isMarked =
    menuItem (if isMarked then "Git LFS: marked" else "Git LFS: not marked") "swt:fluent--tag-24-regular" true ignore

let lfsPillAction (item: FileItem) onDownloadLfsFile onFreeLocalLfsCopy =
    if item.IsDirectory then None
    elif needsLfsDownload item then onDownloadLfsFile |> Option.map (downloadLfsMenuItem item false)
    elif hasLocalLfsCopy item then onFreeLocalLfsCopy |> Option.map (freeLocalLfsCopyMenuItem item false)
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
        let isMarked = isLfs item

        [
            menuItem
                (if isMarked then "Unmark Git LFS" else "Mark Git LFS")
                (if isMarked then "swt:fluent--document-dismiss-24-regular" else "swt:fluent--document-add-24-regular")
                false
                (fun () -> onToggleLfsMark item (not isMarked))
            yield! markedLfsActionItems isMarked (needsLfsDownload item) (downloadLfsMenuItem item) onDownloadLfsFile
            yield! markedLfsActionItems isMarked (hasLocalLfsCopy item) (freeLocalLfsCopyMenuItem item) onFreeLocalLfsCopy
            lfsStatusMenuItem isMarked
        ]

let contextMenuItems (item: FileItem) (onToggleLfsMark: FileItem -> bool -> unit) (onFreeLocalLfsCopy: (FileItem -> unit) option) =
    contextMenuItemsWithDownload item onToggleLfsMark None onFreeLocalLfsCopy
