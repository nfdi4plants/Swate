module Renderer.Components.ArcExplorer

open System
open Browser.Dom
open Swate.Components.FileExplorerTypes
open Swate.Electron.Shared.IPCTypes.IPCTypesHelper
open Swate.Electron.Shared.FileIOTypes
open Swate.Electron.Shared.GitTypes

let private normalizePath (path: string) = path.Replace("\\", "/").TrimEnd('/')

let private resolvePreviewPath (path: string) =
    let normalized = normalizePath path
    let lowered = normalized.ToLowerInvariant()

    let isAssayDatamapFile =
        lowered.Contains("/assays/") && lowered.EndsWith("/isa.datamap.xlsx")

    let isStudyDatamapFile =
        lowered.Contains("/studies/") && lowered.EndsWith("/isa.datamap.xlsx")

    let isWorkflowDatamapFile =
        lowered.Contains("/workflows/") && lowered.EndsWith("/isa.datamap.xlsx")

    let isRunDatamapFile =
        lowered.Contains("/runs/") && lowered.EndsWith("/isa.datamap.xlsx")

    if isAssayDatamapFile then
        let folderPath = normalized.Substring(0, normalized.LastIndexOf('/'))
        $"{folderPath}/isa.assay.xlsx"
    elif isStudyDatamapFile then
        let folderPath = normalized.Substring(0, normalized.LastIndexOf('/'))
        $"{folderPath}/isa.study.xlsx"
    elif isWorkflowDatamapFile then
        let folderPath = normalized.Substring(0, normalized.LastIndexOf('/'))
        $"{folderPath}/isa.workflow.xlsx"
    elif isRunDatamapFile then
        let folderPath = normalized.Substring(0, normalized.LastIndexOf('/'))
        $"{folderPath}/isa.run.xlsx"
    else
        normalized

let private iconForNode (node: ArcExplorerNode) =
    match node.kind with
    | ArcExplorerNodeKind.Arc
    | ArcExplorerNodeKind.Group -> "swt:fluent--folder-24-regular"
    | ArcExplorerNodeKind.Table -> "swt:fluent--table-24-regular"
    | ArcExplorerNodeKind.DataMap -> "swt:fluent--database-24-regular"
    | ArcExplorerNodeKind.Sample -> "swt:fluent--tag-24-regular"
    | ArcExplorerNodeKind.Note
    | ArcExplorerNodeKind.Study
    | ArcExplorerNodeKind.Assay
    | ArcExplorerNodeKind.Workflow
    | ArcExplorerNodeKind.Run -> "swt:fluent--document-24-regular"

let rec private tryFindNodeIdByPath (path: string) (nodes: ArcExplorerNode list) =
    let normalizedTargetPath = normalizePath path

    let rec collectMatches (nodes: ArcExplorerNode list) =
        nodes
        |> List.collect (fun node ->
            let currentMatch =
                match node.path with
                | Some nodePath when normalizePath nodePath = normalizedTargetPath -> [ node ]
                | _ -> []

            currentMatch @ collectMatches node.children)

    let matches = collectMatches nodes

    matches
    |> List.tryFind (fun node -> not node.isReference)
    |> Option.orElseWith (fun () -> matches |> List.tryHead)
    |> Option.map (fun node -> node.id)

let rec tryFindNodeById (nodeId: string) (nodes: ArcExplorerNode list) =
    nodes
    |> List.tryPick (fun node ->
        if node.id = nodeId then
            Some node
        else
            tryFindNodeById nodeId node.children)

let rec private toFileItem (node: ArcExplorerNode) =
    let children = node.children |> List.map toFileItem

    let isDirectory =
        node.kind = ArcExplorerNodeKind.Arc
        || node.kind = ArcExplorerNodeKind.Group
        || not (List.isEmpty children)

    if isDirectory then
        {
            FileTree.createFolder node.name node.path (iconForNode node) with
                Id = node.id
                IsExpanded = node.kind = ArcExplorerNodeKind.Arc
                IsLFS = node.isLfs
                Selectable = node.isSelectable
                Children = Some children
        }
    else
        {
            FileTree.createFile node.name node.path (iconForNode node) with
                Id = node.id
                IsLFS = node.isLfs
                Selectable = node.isSelectable
        }

let toFileItems (nodes: ArcExplorerNode list) = nodes |> List.map toFileItem

let getSelectedItemId
    (nodes: ArcExplorerNode list)
    (selectedExplorerItemId: string option)
    (selectedTreeItemPath: string option)
    =
    selectedExplorerItemId
    |> Option.orElseWith (fun () ->
        selectedTreeItemPath |> Option.bind (fun path -> tryFindNodeIdByPath path nodes))

let createOpenPreviewHandler
    (setSelectedExplorerItemId: string option -> unit)
    (setSelectedTreeItemPath: string option -> unit)
    (setPageState: PageState option -> unit)
    =
    let openPreview (item: FileItem) =
        promise {
            match item.Path with
            | None ->
                setSelectedTreeItemPath None
                setSelectedExplorerItemId (Some item.Id)
                setPageState None
            | Some path ->
                let selectedPath = normalizePath path
                let previewPath = resolvePreviewPath path

                if previewPath <> selectedPath then
                    console.log ($"[Renderer] Redirecting Datamap click to file: {previewPath}")
                else
                    console.log ($"[Renderer] Opening file: {previewPath}")

                setSelectedTreeItemPath (Some selectedPath)
                setSelectedExplorerItemId (Some item.Id)
                let! result = Api.ipcArcVaultApi.openFile (unbox null) previewPath

                match result with
                | Ok data ->
                    console.log ("[Renderer] Received data, processing...")
                    setPageState (Some data)
                | Error exn ->
                    console.log ($"[Renderer] Error: {exn.Message}")
                    setPageState (Some(PageState.Error $"Could not open preview for '{item.Name}': {exn.Message}"))
        }
        |> Promise.start

    openPreview

let createArcExplorer
    (rootRepoPath: string option)
    (nodes: ArcExplorerNode list)
    (selectedExplorerItemId: string option)
    (selectedTreeItemPath: string option)
    (setSelectedExplorerItemId: string option -> unit)
    (setSelectedTreeItemPath: string option -> unit)
    (setPageState: PageState option -> unit)
    =
    let selectedItemId = getSelectedItemId nodes selectedExplorerItemId selectedTreeItemPath

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

        let! result = Api.ipcArcVaultApi.runGitLfs (unbox null) request

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

    let openPreview = createOpenPreviewHandler setSelectedExplorerItemId setSelectedTreeItemPath setPageState

    if List.isEmpty nodes then
        None
    else
        let items = toFileItems nodes

        Some(
            Swate.Components.FileExplorer.FileExplorer(
                initialItems = items,
                onItemClick = openPreview,
                onContextMenu = contextMenuItems,
                ?selectedItemId = selectedItemId,
                directoryInteractionMode = DirectoryInteractionMode.ToggleOnSingleClickSelectOnDoubleClick,
                useDirectoryChevronToggle = true
            )
        )
