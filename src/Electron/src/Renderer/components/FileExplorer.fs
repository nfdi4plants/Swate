module Renderer.Components.FileExplorer

open System
open Browser.Dom
open Swate.Components.FileExplorerTypes
open Swate.Electron.Shared.IPCTypes.IPCTypesHelper
open Swate.Electron.Shared.FileIOTypes
open Swate.Electron.Shared.GitTypes

let private normalizePath (path: string) = path.Replace("\\", "/").TrimEnd('/')

let private splitPath (path: string) =
    normalizePath path
    |> fun normalizedPath -> normalizedPath.Split('/', StringSplitOptions.RemoveEmptyEntries)

let private resolvePreviewPath (path: string) =
    let normalized = normalizePath path
    let lowered = normalized.ToLowerInvariant()

    let isAssayDatamapFile =
        lowered.Contains("/assays/") && lowered.EndsWith("/isa.datamap.xlsx")

    let isStudyDatamapFile =
        lowered.Contains("/studies/") && lowered.EndsWith("/isa.datamap.xlsx")

    let isRunDatamapFile =
        lowered.Contains("/runs/") && lowered.EndsWith("/isa.datamap.xlsx")

    if isAssayDatamapFile then
        let folderPath = normalized.Substring(0, normalized.LastIndexOf('/'))
        $"{folderPath}/isa.assay.xlsx"
    elif isStudyDatamapFile then
        let folderPath = normalized.Substring(0, normalized.LastIndexOf('/'))
        $"{folderPath}/isa.study.xlsx"
    elif isRunDatamapFile then
        let folderPath = normalized.Substring(0, normalized.LastIndexOf('/'))
        $"{folderPath}/isa.run.xlsx"
    else
        normalized

let createFileTree
    (parent: FileItemDTO option)
    (selectedTreeItemPath: string option)
    (setSelectedTreeItemPath: string option -> unit)
    (setPageState: PageState option -> unit)
    =
    let rootRepoPath = parent |> Option.map (fun p -> normalizePath p.path)

    let isFocusedPathOrAncestor (nodePath: string) =
        match selectedTreeItemPath with
        | Some focusedPath ->
            let normalizedNode = normalizePath nodePath
            let normalizedFocused = normalizePath focusedPath

            normalizedFocused = normalizedNode
            || normalizedFocused.StartsWith(normalizedNode + "/", StringComparison.OrdinalIgnoreCase)
        | None -> false

    let rec loop (parent: FileItemDTO option) =
        if parent.IsSome then
            match parent.Value.isDirectory with
            | true ->
                let tmp =
                    let ra = ResizeArray(parent.Value.children.Values)

                    ra.ToArray()
                    |> Array.map (fun entry -> loop (Some entry))
                    |> Array.choose id
                    |> List.ofArray

                Some {
                    FileTree.createFolder parent.Value.name (Some parent.Value.path) "swt:fluent--folder-24-regular" with
                        Id = parent.Value.path
                        IsExpanded = isFocusedPathOrAncestor parent.Value.path
                        IsLFS = parent.Value.isLfs
                        Children = Some tmp
                }
            | false ->
                Some {
                    FileTree.createFile parent.Value.name (Some parent.Value.path) "swt:fluent--document-24-regular" with
                        Id = parent.Value.path
                        IsLFS = parent.Value.isLfs
                }
        else
            None

    let fileItem = loop parent

    let runToggleLfsMark (repoPath: string) (relativePath: string) (markAsLfs: bool) = promise {
        let request: GitLfsRequest = {
            RequestId = Guid.NewGuid().ToString()
            RepoPath = repoPath
            Command =
                if markAsLfs then
                    GitLfsCommand.Track
                else
                    GitLfsCommand.Untrack
            FilePath = Some relativePath
            TimeoutMs = Some 10000
        }

        let! result = Api.runGitLfs request

        return
            match result with
            | Ok _ -> Ok()
            | Error exn -> Error exn.Message
    }

    let setError (errorMsg: string option) =
        match errorMsg with
        | Some msg -> setPageState (Some(PageState.Error msg))
        | None -> setPageState None

    let toggleLfsMark =
        FileExplorerGitLfsHelper.ToggleLfsMark(rootRepoPath, setError, runToggleLfsMark)

    let contextMenuItems (item: FileItem) =
        FileExplorerGitLfsHelper.ContextMenuItems(item, toggleLfsMark)

    let openPreview (item: FileItem) =
        promise {
            match item.Path with
            | None -> setPageState (Some(PageState.Error $"File '{item.Name}' has no path."))
            | Some path when item.IsDirectory -> setPageState None
            | Some path ->
                let previewPath = resolvePreviewPath path

                if previewPath <> normalizePath path then
                    console.log ($"[Renderer] Redirecting Datamap click to file: {previewPath}")
                else
                    console.log ($"[Renderer] Opening file: {previewPath}")

                setSelectedTreeItemPath (Some previewPath)
                let! result = Api.openFile previewPath

                match result with
                | Ok data ->
                    console.log ("[Renderer] Received data, processing...")
                    setPageState (Some data)
                | Error exn ->
                    console.log ($"[Renderer] Error: {exn.Message}")
                    setPageState (Some(PageState.Error $"Could not open preview for '{item.Name}': {exn.Message}"))
        }
        |> Promise.start

    if fileItem.IsSome then
        Some(
            Swate.Components.FileExplorer.FileExplorer(
                initialItems = [ fileItem.Value ],
                onItemClick = openPreview,
                onContextMenu = contextMenuItems,
                ?selectedItemId = selectedTreeItemPath
            )
        )
    else
        None

let private insertEntry (root: FileItemDTO) (rootPath: string) (entry: FileEntry) =
    let parts = splitPath entry.path
    let rootParts = splitPath rootPath

    if parts.Length > rootParts.Length then
        let rec loop (node: FileItemDTO) index =
            let part = parts[index]
            let isLast = index = parts.Length - 1

            let child =
                match node.children.TryGetValue(part) with
                | true, existing when ((not isLast) || entry.isDirectory) && not existing.isDirectory ->
                    // A node may first appear via a file path segment; upgrade it to a directory when needed.
                    let upgraded = { existing with isDirectory = true }
                    node.children.[part] <- upgraded
                    upgraded
                | true, existing -> existing
                | false, _ ->
                    let newPath = parts.[0..index] |> String.concat "/"

                    let newNode =
                        FileItemDTO.create (
                            part,
                            (if isLast then entry.isDirectory else true),
                            newPath,
                            System.Collections.Generic.Dictionary(),
                            entry.isLfs
                        )

                    node.children.Add(part, newNode)
                    newNode

            if not isLast then
                loop child (index + 1)

        loop root rootParts.Length

let getFileTree (fileEntries: FileEntry[]) =

    if fileEntries.Length = 0 then
        invalidArg "fileEntries" "fileEntries must not be empty."

    let normalizedPaths =
        fileEntries |> Array.map (fun fileEntry -> normalizePath fileEntry.path)

    let rootPath =
        normalizedPaths
        |> Array.distinct
        |> Array.sortBy (fun path -> splitPath path |> Array.length, path)
        |> Array.head

    let adaptedFileEntries =
        fileEntries
        |> Array.filter (fun fileEntry -> normalizePath fileEntry.path <> rootPath)
        // Deterministic order avoids creating parents from file entries before their directory entries.
        |> Array.sortBy (fun fileEntry ->
            let depth = splitPath fileEntry.path |> Array.length
            depth, (if fileEntry.isDirectory then 0 else 1), normalizePath fileEntry.path
        )

    let rootElement =
        let rootEntry =
            fileEntries
            |> Array.find (fun fileEntry -> normalizePath fileEntry.path = rootPath)

        FileItemDTO.create (
            rootEntry.name,
            rootEntry.isDirectory,
            rootPath,
            System.Collections.Generic.Dictionary(),
            rootEntry.isLfs
        )

    adaptedFileEntries
    |> Array.iter (fun fileEntry -> insertEntry rootElement rootPath fileEntry)

    rootElement