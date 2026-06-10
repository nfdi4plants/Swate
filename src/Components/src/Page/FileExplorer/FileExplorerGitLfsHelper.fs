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

let freeLocalLfsCopy
    (setError: string option -> unit)
    (runCleanup: string -> JS.Promise<Result<unit, string>>)
    : (FileItem -> unit) =
    fun item ->
        promise {
            match item.Path with
            | None -> ()
            | Some relativePath ->
                if System.String.IsNullOrWhiteSpace relativePath then
                    setError (Some "Cannot free the ARC root as a Git LFS file.")
                elif item.IsLFS <> Some true then
                    setError (Some $"'{item.Name}' is not marked as a Git LFS file.")
                elif item.Downloaded <> Some true || item.IsLFSPointer = Some true then
                    setError (Some $"'{item.Name}' is already stored locally as an LFS pointer.")
                else
                    let! result = runCleanup relativePath

                    match result with
                    | Ok _ -> setError None
                    | Error msg -> setError (Some $"Git LFS cleanup failed for '{item.Name}': {msg}")
        }
        |> Promise.start

let contextMenuItems
    (item: FileItem)
    (onToggleLfsMark: FileItem -> bool -> unit)
    (onFreeLocalLfsCopy: (FileItem -> unit) option)
    : ContextMenuItem list =
    if item.IsDirectory then
        []
    else
        let isMarked = item.IsLFS = Some true

        let hasLocalLfsCopy =
            isMarked && item.Downloaded = Some true && item.IsLFSPointer <> Some true

        [
            ContextMenuItem.create
                (if isMarked then "Unmark Git LFS" else "Mark Git LFS")
                (if isMarked then
                     "swt:fluent--document-dismiss-24-regular"
                 else
                     "swt:fluent--document-add-24-regular")
                (fun () -> onToggleLfsMark item (not isMarked))
            yield!
                match onFreeLocalLfsCopy with
                | Some freeLocalCopy when hasLocalLfsCopy -> [
                    ContextMenuItem.create
                        "Free local LFS copy"
                        "swt:fluent--document-arrow-up-20-regular"
                        (fun () -> freeLocalCopy item)
                  ]
                | Some _ when isMarked -> [
                    ContextMenuItem.disabled "Free local LFS copy" "swt:fluent--document-arrow-up-20-regular"
                  ]
                | _ -> []
            ContextMenuItem.disabled
                (if isMarked then
                     "Git LFS: marked"
                 else
                     "Git LFS: not marked")
                "swt:fluent--tag-24-regular"
        ]
