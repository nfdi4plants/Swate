namespace Swate.Components.FileExplorerTypes

open Fable.Core

[<RequireQualifiedAccess>]
type FileExplorerGitLfsHelper =

    static member private NormalizePath(path: string) = path.Replace("\\", "/").TrimEnd('/')

    static member private TryToRepoRelativePath(rootRepoPath: string, filePath: string) =
        let normalizedRepoPath = FileExplorerGitLfsHelper.NormalizePath rootRepoPath
        let normalizedFilePath = FileExplorerGitLfsHelper.NormalizePath filePath
        let prefix = normalizedRepoPath + "/"

        if normalizedFilePath = normalizedRepoPath then
            Some(normalizedRepoPath, "")
        elif normalizedFilePath.StartsWith(prefix, System.StringComparison.OrdinalIgnoreCase) then
            Some(normalizedRepoPath, normalizedFilePath.Substring(prefix.Length))
        else
            None

    static member ToggleLfsMark
        (
            rootRepoPath: string,
            setError: string option -> unit,
            runToggle: string -> string -> bool -> JS.Promise<Result<unit, string>>
        ) : (FileItem -> bool -> unit) =
        fun item markAsLfs ->
            promise {
                match item.Path with
                | None -> ()
                | Some itemPath ->
                    match FileExplorerGitLfsHelper.TryToRepoRelativePath(rootRepoPath, itemPath) with
                    | None -> setError (Some $"Could not resolve repository-relative path for '{item.Name}'.")
                    | Some(_, relativePath) when System.String.IsNullOrWhiteSpace relativePath ->
                        setError (Some "Cannot mark ARC root as a Git LFS file.")
                    | Some(repoPath, relativePath) ->
                        let! result = runToggle repoPath relativePath markAsLfs

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