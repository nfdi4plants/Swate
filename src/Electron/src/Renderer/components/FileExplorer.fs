module Renderer.components.FileExplorer

open Fable.Core

open Swate.Components
open Swate.Electron.Shared.IPCTypes
open Swate.Components.FileExplorerTypes

open Browser.Dom


let createFileTree (parent: FileItemDTO option) selectedTreeItemPath setSelectedTreeItemPath setShowLandingDraft setPreviewData setPreviewError setDidSelectFile =
    let normalizePath (path: string) = path.Replace("\\", "/").TrimEnd('/')

    let isFocusedPathOrAncestor (nodePath: string) =
        match selectedTreeItemPath with
        | Some focusedPath ->
            let normalizedNode = normalizePath nodePath
            let normalizedFocused = normalizePath focusedPath

            normalizedFocused = normalizedNode
            || normalizedFocused.StartsWith(normalizedNode + "/", System.StringComparison.OrdinalIgnoreCase)
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

                let result = {
                    FileTree.createFolder parent.Value.name (Some parent.Value.path) "swt:fluent--folder-24-regular" with
                        Id = parent.Value.path
                        IsExpanded = isFocusedPathOrAncestor parent.Value.path
                        Children = Some tmp
                }
                Some result
            | false ->
                Some(
                    {
                        FileTree.createFile parent.Value.name (Some parent.Value.path) "swt:fluent--document-24-regular" with
                            Id = parent.Value.path
                    }
                )
        else
            None

    let fileItem = loop parent

    let openPreview (parent: FileItemDTO option) setSelectedTreeItemPath setShowLandingDraft setPreviewData setPreviewError setDidSelectFile (item: FileItem) =
        promise {

            if parent.IsSome then

                let fileTree = parent.Value

                let isDirectoryByPath =
                    match item.Path with
                    | Some p when fileTree.children.ContainsKey(p) -> fileTree.children.[p].isDirectory
                    | _ -> item.IsDirectory

                if item.Path.IsSome && not isDirectoryByPath then
                    console.log ($"[Renderer] Opening file: {item.Path.Value}")
                    setSelectedTreeItemPath item.Path
                    setShowLandingDraft false
                    let! result = Api.openFile item.Path.Value

                    match result with
                    | Ok data ->
                        console.log ("[Renderer] Received data, processing...")
                        setPreviewData (Some data)
                        setPreviewError None
                        setDidSelectFile true

                        let fileType: SaveArcFileRequest option =
                            match data with
                            | PreviewData.ArcFileData (fileType, json)->
                                Some {
                                    FileType = fileType
                                    Json = json
                                }
                            | _ -> None

                        if fileType.IsSome then
                            let! result = Api.syncARC fileType.Value
                            match result with
                            | Ok () -> ()
                            | Error exn -> console.log ($"[Renderer] Error: {exn.Message}")


                        //if selectedTreeItemPath.IsSome && (selectedTreeItemPath.Value <> item.Path.Value) then
                        //    match data with
                        //    | PreviewData.ArcFileData (_, json) -> console.log($"json: {json}")
                        //    | _ -> console.log($"Nope")

                    | Error exn ->
                        console.log ($"[Renderer] Error: {exn.Message}")
                        setPreviewData (None)
                        setPreviewError (Some $"Could not open preview for '{item.Name}': {exn.Message}")
                        setDidSelectFile true
                elif item.Path.IsSome && isDirectoryByPath then
                    // Folders are not preview targets.
                    setPreviewError None
                else
                    setPreviewError (Some $"File '{item.Name}' has no path.")
        }
        |> Promise.start

    if fileItem.IsSome then
        Some(
            FileExplorer.FileExplorer(
                initialItems = [ fileItem.Value ],
                onItemClick = openPreview parent setSelectedTreeItemPath setShowLandingDraft setPreviewData setPreviewError setDidSelectFile,
                ?selectedItemId = selectedTreeItemPath
            )
        )
    else
        None

let insertEntry (root: FileItemDTO) (rootPath: string) (entry: FileEntry) =
    let parts = entry.path.Split('/', System.StringSplitOptions.RemoveEmptyEntries)

    let splittedRootPath = rootPath.Split('/', System.StringSplitOptions.RemoveEmptyEntries)

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
                let isDirectory =
                    if isLast then
                        entry.isDirectory
                    else
                        true

                let newNode =
                    FileItemDTO.create(
                        part,
                        isDirectory,
                        newPath,
                        System.Collections.Generic.Dictionary()
                    )
                node.children.Add(part, newNode)
                newNode

        if not isLast then
            loop child (index + 1)

    loop root splittedRootPath.Length

let getFileTree (fileEntries: FileEntry []) =

    let rootPath =
        fileEntries
        |> Array.map (fun fileEntry -> fileEntry.path)
        |> Array.map (fun path -> path.Split("/"))
        |> Array.sortByDescending (fun path -> path.Length)
        |> Array.last
        |> String.concat "/"

    let adaptedFileEntires =
        fileEntries
        |> Array.filter (fun fileEntry -> fileEntry.path <> rootPath)
        // Deterministic order avoids creating parents from file entries before their directory entries.
        |> Array.sortBy (fun fileEntry ->
            let depth =
                fileEntry.path.Split('/', System.StringSplitOptions.RemoveEmptyEntries).Length

            depth, (if fileEntry.isDirectory then 0 else 1), fileEntry.path
        )

    let rootElement =
        let tmp =
            fileEntries
            |> Array.find(fun fileEntry -> fileEntry.path = rootPath)
        FileItemDTO.create(tmp.name, tmp.isDirectory, tmp.path, System.Collections.Generic.Dictionary())

    adaptedFileEntires
    |> Array.iter (fun fileEntry -> insertEntry rootElement rootPath fileEntry)

    rootElement
