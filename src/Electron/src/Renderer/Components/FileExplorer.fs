module Renderer.Components.FileExplorer

open System
open Browser.Dom
open Renderer
open Swate.Components.FileExplorerTypes
open Swate.Electron.Shared.FileIOTypes
open Swate.Electron.Shared.GitTypes
open Feliz


module private FileExplorerHelper =

    let normalizePath (path: string) = path.Replace("\\", "/").TrimEnd('/')

    let splitPath (path: string) =
        normalizePath path
        |> fun normalizedPath -> normalizedPath.Split('/', StringSplitOptions.RemoveEmptyEntries)

    let resolvePreviewPath (path: string) =
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

    let isFocusedPathOrAncestor (selectedTreeItemPath) (nodePath: string) =
        match selectedTreeItemPath with
        | Some focusedPath ->
            let normalizedNode = normalizePath nodePath
            let normalizedFocused = normalizePath focusedPath

            normalizedFocused = normalizedNode
            || normalizedFocused.StartsWith(normalizedNode + "/", StringComparison.OrdinalIgnoreCase)
        | None -> false

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
                FileTree.createFolder parent.name (Some parent.path) "swt:fluent--folder-24-regular" with
                    Id = parent.path
                    IsExpanded = isFocusedPathOrAncestor selectedTreeItemPath parent.path
                    IsLFS = parent.isLfs
                    Children = Some tmp
            }
        | false ->
            Some {
                FileTree.createFile parent.name (Some parent.path) "swt:fluent--document-24-regular" with
                    Id = parent.path
                    IsLFS = parent.isLfs
            }

    let private insertEntry (root: FileTreeNode) (rootPath: string) (entry: FileEntry) =
        let parts = splitPath entry.path
        let rootParts = splitPath rootPath

        if parts.Length > rootParts.Length then
            let rec loop (node: FileTreeNode) index =
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
                            FileTreeNode.create (
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
            failwith "getFileTree requires at least one file entry to determine the root path."

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

            FileTreeNode.create (
                rootEntry.name,
                rootEntry.isDirectory,
                rootPath,
                System.Collections.Generic.Dictionary(),
                rootEntry.isLfs
            )

        adaptedFileEntries
        |> Array.iter (fun fileEntry -> insertEntry rootElement rootPath fileEntry)

        rootElement

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

    match fileStateCtx.state.FileTree with
    | [||] -> EmptyFileTreePlaceholder()
    | _ ->

        let fileTree = fileStateCtx.state.FileTree |> getFileTree
        let rootRepoPath = fileTree.path |> normalizePath

        let fileItem = loopPaths fileStateCtx.state.SelectedTreeItemPath fileTree

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


            // This seems to behave oddly. It runs some git lfs command and then refreshes filetree in arcvault. But it does it with a type that does not track lfs?
            let! result = Api.ipcArcVaultApi.runGitLfs (unbox null) request

            return
                match result with
                | Ok _ -> Ok()
                | Error exn -> Error exn.Message
        }

        let setError (errorMsg: string option) =
            match errorMsg with
            | Some msg -> pageStateCtx.setState (Some(PageState.ErrorPage msg))
            | None -> pageStateCtx.setState (None)

        let toggleLfsMark =
            FileExplorerGitLfsHelper.ToggleLfsMark(rootRepoPath, setError, runToggleLfsMark)

        let contextMenuItems (item: FileItem) =
            FileExplorerGitLfsHelper.ContextMenuItems(item, toggleLfsMark)

        let openPreview (item: FileItem) =
            promise {
                match item.Path with
                | None -> pageStateCtx.setState (Some(PageState.ErrorPage $"File '{item.Name}' has no path."))
                | Some path when item.IsDirectory -> pageStateCtx.setState (None)
                | Some path ->
                    let previewPath = resolvePreviewPath path

                    if previewPath <> normalizePath path then
                        console.log ($"[Renderer] Redirecting Datamap click to file: {previewPath}")
                    else
                        console.log ($"[Renderer] Opening file: {previewPath}")

                    fileStateCtx.setSelectedTreeItemPath (Some path)

                    let! result = Api.ipcArcVaultApi.openFile (unbox null) previewPath

                    match result with
                    | Ok data ->
                        let pageState = PageState.fromFileContentDTO data
                        console.log ("[Renderer] Received data, processing...")
                        pageStateCtx.setState (Some pageState)
                    | Error exn ->
                        console.log ($"[Renderer] Error: {exn.Message}")

                        pageStateCtx.setState (
                            Some(PageState.ErrorPage $"Could not open preview for '{item.Name}': {exn.Message}")
                        )
            }
            |> Promise.start

        match fileItem with

        | Some fileItem ->
            Swate.Components.FileExplorer.FileExplorer(
                initialItems = [ fileItem ],
                onItemClick = openPreview,
                onContextMenu = contextMenuItems,
                ?selectedItemId = fileStateCtx.state.SelectedTreeItemPath
            )
        | None -> EmptyFileTreePlaceholder()