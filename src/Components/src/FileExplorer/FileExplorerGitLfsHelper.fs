namespace Swate.Components.FileExplorerTypes

open Fable.Core

[<RequireQualifiedAccess>]
type FileExplorerGitLfsHelper =

    static member ToggleLfsMark
        (
            setError: string option -> unit,
            runToggle: string -> bool -> JS.Promise<Result<unit, string>>
        ) : (FileItem -> bool -> unit) =
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

    static member ContextMenuItems(item: FileItem, onToggleLfsMark: FileItem -> bool -> unit) : ContextMenuItem list =
        if item.IsDirectory then
            []
        else
            let isMarked = item.IsLFS = Some true

            [
                {
                    Label = if isMarked then "Unmark Git LFS" else "Mark as Git LFS"
                    Icon =
                        if isMarked then
                            "swt:fluent--document-dismiss-24-regular"
                        else
                            "swt:fluent--document-add-24-regular"
                    OnClick = fun () -> onToggleLfsMark item (not isMarked)
                    Disabled = None
                }
                {
                    Label =
                        if isMarked then
                            "Git LFS: marked"
                        else
                            "Git LFS: not marked"
                    Icon = "swt:fluent--tag-24-regular"
                    OnClick = fun () -> ()
                    Disabled = Some true
                }
            ]
