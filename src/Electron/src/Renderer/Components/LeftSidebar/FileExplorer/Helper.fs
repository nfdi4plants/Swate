module Renderer.Components.LeftSidebar.FileExplorer.Helper

open System
open Swate.Components.Page.FileExplorer.Types
open Swate.Components.Shared
open Swate.Electron.Shared.FileIOHelper
open Swate.Electron.Shared.FileIOTypes
open ARCtrl
open Types

let private normalizeNodePath (path: string) = PathHelpers.normalizePath path

let private pathSegments (path: string) = path |> normalizeNodePath |> getNonEmptyPathParts

let private lowerInvariant (value: string) = value.ToLowerInvariant()

let private iconForArcCollectionFolder =
    function
    | "studies" -> Some FileItemIcon.Study
    | "assays" -> Some FileItemIcon.Assay
    | "workflows" -> Some FileItemIcon.Workflow
    | "runs" -> Some FileItemIcon.Run
    | "notes" -> Some FileItemIcon.Notebook
    | _ -> None

let private colorClassForArcCollectionFolder =
    function
    | "studies" -> Some "swt:text-amber-500"
    | "assays" -> Some "swt:text-lime-500"
    | "workflows" -> Some "swt:text-emerald-500"
    | "runs" -> Some "swt:text-cyan-500"
    | _ -> None

let private iconForArcWorkbookFile =
    function
    | "isa.investigation.xlsx" -> Some FileItemIcon.BookOpen
    | "isa.study.xlsx" -> Some FileItemIcon.Study
    | "isa.assay.xlsx" -> Some FileItemIcon.Assay
    | "isa.workflow.xlsx" -> Some FileItemIcon.Workflow
    | "isa.run.xlsx" -> Some FileItemIcon.Run
    | _ -> None

let private colorClassForArcWorkbookFile =
    function
    | "isa.study.xlsx" -> Some "swt:text-amber-300"
    | "isa.assay.xlsx" -> Some "swt:text-lime-300"
    | "isa.workflow.xlsx" -> Some "swt:text-emerald-300"
    | "isa.run.xlsx" -> Some "swt:text-cyan-300"
    | _ -> None

let private colorClassForDatamapPath (path: string) =
    match DatamapParentInfo.tryFromPath path with
    | Some dmpi ->
        match dmpi.Parent with
        | DataMapParent.Study -> Some "swt:text-amber-700"
        | DataMapParent.Assay -> Some "swt:text-lime-700"
        | DataMapParent.Workflow -> Some "swt:text-emerald-700"
        | DataMapParent.Run -> Some "swt:text-cyan-700"
    | None -> None

let private folderIcon (path: string) =
    let segments = pathSegments path

    match segments |> Array.tryHead |> Option.map lowerInvariant, segments.Length with
    | Some rootSegment, 1 -> iconForArcCollectionFolder rootSegment |> Option.defaultValue FileItemIcon.Folder
    | Some "studies", 2 -> FileItemIcon.Study
    | Some "assays", 2 -> FileItemIcon.Assay
    | Some "workflows", 2 -> FileItemIcon.Workflow
    | Some "runs", 2 -> FileItemIcon.Run
    | Some "notes", _ -> FileItemIcon.Notebook
    | _ -> FileItemIcon.Folder

let private fileIcon (path: string) =
    let normalizedPath = normalizeNodePath path
    let segments = pathSegments normalizedPath
    let fileName = PathHelpers.getFileName normalizedPath |> lowerInvariant

    match colorClassForDatamapPath normalizedPath with
    | Some _ -> FileItemIcon.Map
    | None ->
        match iconForArcWorkbookFile fileName with
        | Some icon -> icon
        | None when (segments |> Array.tryHead |> Option.exists (fun segment -> String.Equals(segment, "notes", StringComparison.OrdinalIgnoreCase)))
                     && fileName.EndsWith(".md", StringComparison.OrdinalIgnoreCase) ->
            FileItemIcon.Note
        | None when fileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase) -> FileItemIcon.Table
        | None -> FileItemIcon.Document

let getItemIconClass (item: FileItem) =
    match item.Path with
    | None -> None
    | Some path when item.IsDirectory ->
        let segments = pathSegments path

        match segments |> Array.tryHead |> Option.map lowerInvariant, segments.Length with
        | Some rootSegment, 1 -> colorClassForArcCollectionFolder rootSegment
        | Some "studies", 2 -> Some "swt:text-amber-500"
        | Some "assays", 2 -> Some "swt:text-lime-500"
        | Some "workflows", 2 -> Some "swt:text-emerald-500"
        | Some "runs", 2 -> Some "swt:text-cyan-500"
        | _ -> None
    | Some path ->
        let normalizedPath = normalizeNodePath path
        let fileName = PathHelpers.getFileName normalizedPath |> lowerInvariant

        match colorClassForDatamapPath normalizedPath with
        | Some colorClass -> Some colorClass
        | None -> colorClassForArcWorkbookFile fileName

let canDeleteItem (item: FileItem) =
    item.Path
    |> Option.map PathHelpers.normalizeCanonicalRelativePath
    |> Option.exists ArcDeletePathRules.isDeletePathAllowed

let tryGetItemRelativePath (item: FileItem) =
    item.Path
    |> Option.map PathHelpers.normalizeRelativePath
    |> Option.map PathHelpers.normalizePath

let canCreateFileSystemItemIn (item: FileItem) =
    item.IsDirectory
    && (tryGetItemRelativePath item
        |> Option.exists ArcDeletePathRules.isGenericFileSystemParentAllowed)

let fileSystemCreateKinds = [ FileSystemItemKind.File; FileSystemItemKind.Folder ]

let fileSystemCreateKindLabel =
    function
    | FileSystemItemKind.File -> "File"
    | FileSystemItemKind.Folder -> "Folder"

let fileSystemCreateKindIcon =
    function
    | FileSystemItemKind.File -> "swt:fluent--document-add-24-regular"
    | FileSystemItemKind.Folder -> "swt:fluent--folder-add-24-regular"

let rec private collectSelectedDirectoryPathChain
    (selectedTreeItemPath: string option)
    (node: FileTreeNode)
    (loadedPaths: Set<string>)
    =
    let normalizedNodePath = PathHelpers.normalizePath node.path

    let isInSelectedPathChain =
        selectedTreeItemPath
        |> Option.exists (fun focusedPath -> PathHelpers.isSameOrDescendantPath focusedPath normalizedNodePath)

    if node.isDirectory then
        let nextLoadedPaths =
            if isInSelectedPathChain then
                Set.add normalizedNodePath loadedPaths
            else
                loadedPaths

        node.children.Values
        |> Seq.fold
            (fun state child -> collectSelectedDirectoryPathChain selectedTreeItemPath child state)
            nextLoadedPaths
    else
        loadedPaths

let requiredLoadedDirectoryPaths (selectedTreeItemPath: string option) (root: FileTreeNode) =
    let rootPathSet =
        if root.isDirectory then
            Set.singleton (PathHelpers.normalizePath root.path)
        else
            Set.empty

    collectSelectedDirectoryPathChain selectedTreeItemPath root rootPathSet

let rec loopPaths (loadedDirectoryPaths: Set<string>) (selectedTreeItemPath: string option) (parent: FileTreeNode) =
    match parent.isDirectory with
    | true ->
        let normalizedParentPath = PathHelpers.normalizePath parent.path
        let isDirectoryLoaded = loadedDirectoryPaths.Contains normalizedParentPath
        let hasSourceChildren = parent.children.Count > 0

        let mappedChildren =
            if isDirectoryLoaded then
                let ra = ResizeArray(parent.children.Values)

                ra.ToArray()
                |> Array.map (fun entry -> loopPaths loadedDirectoryPaths selectedTreeItemPath entry)
                |> Array.choose id
                |> List.ofArray
            else
                []

        let children =
            if isDirectoryLoaded then Some mappedChildren
            elif hasSourceChildren then None
            else Some []

        {
            FileTree.createFolder parent.name (Some parent.path) (folderIcon parent.path) with
                Id = parent.path
                IsExpanded =
                    selectedTreeItemPath
                    |> Option.exists (fun focusedPath -> PathHelpers.isSameOrDescendantPath focusedPath parent.path)
                Children = children
        }
        |> Renderer.Components.FileExplorerLfs.withFileTreeNodeLfsState parent
        |> Some
    | false ->
        {
            FileTree.createFile parent.name (Some parent.path) (fileIcon parent.path) with
                Id = parent.path
        }
        |> Renderer.Components.FileExplorerLfs.withFileTreeNodeLfsState parent
        |> Some

let arcCreateKindIcon =
    function
    | ArcExplorerNodeKind.Study -> "swt:fluent--document-table-24-regular"
    | ArcExplorerNodeKind.Assay -> "swt:fluent--beaker-24-regular"
    | ArcExplorerNodeKind.Workflow -> "swt:fluent--flowchart-24-regular"
    | ArcExplorerNodeKind.Run -> "swt:fluent--play-24-regular"
    | kind -> failwithf "ARC node kind '%s' cannot be created from the file explorer." (ArcExplorerNodeKind.label kind)

let arcCreateKinds = [
    ArcExplorerNodeKind.Study
    ArcExplorerNodeKind.Assay
    ArcExplorerNodeKind.Workflow
    ArcExplorerNodeKind.Run
]

let arcCreateKindSortOrder =
    function
    | ArcExplorerNodeKind.Study -> 10
    | ArcExplorerNodeKind.Assay -> 20
    | ArcExplorerNodeKind.Workflow -> 30
    | ArcExplorerNodeKind.Run -> 40
    | _ -> 1000

let arcCreateKindDefaultIdentifier =
    function
    | ArcExplorerNodeKind.Study -> "New Study"
    | ArcExplorerNodeKind.Assay -> "New Assay"
    | ArcExplorerNodeKind.Workflow -> "New Workflow"
    | ArcExplorerNodeKind.Run -> "New Run"
    | kind -> failwithf "ARC node kind '%s' cannot be created from the file explorer." (ArcExplorerNodeKind.label kind)

let isArcCreateIdentifierValid (identifier: string) =
    let identifier = identifier.Trim()

    (System.String.IsNullOrWhiteSpace identifier |> not)
    && ARCtrl.Helper.Identifier.tryCheckValidCharacters identifier

let arcCreateIdentifierError =
    "Identifier is required and may only contain letters, digits, spaces, underscores, or dashes."

let tryCreateArcFile kind identifier =
    match kind with
    | ArcExplorerNodeKind.Study ->
        let study = ArcStudy.init identifier
        study.InitTable($"{identifier} Table") |> ignore
        Ok(ArcFiles.Study(study, []))
    | ArcExplorerNodeKind.Assay ->
        let assay = ArcAssay.init identifier
        assay.InitTable($"{identifier} Table") |> ignore
        Ok(ArcFiles.Assay assay)
    | ArcExplorerNodeKind.Workflow -> ArcWorkflow.init identifier |> ArcFiles.Workflow |> Ok
    | ArcExplorerNodeKind.Run ->
        let run = ArcRun.init identifier
        run.InitTable($"{identifier} Table") |> ignore
        Ok(ArcFiles.Run run)
    | kind -> Error $"Creating {ArcExplorerNodeKind.label kind} files is not supported from the file explorer."

let tryGetInlineArcCreateKind (rootPath: string) (item: FileItem) =
    if not item.IsDirectory then
        None
    else
        match item.Path with
        | Some path when getPathDepth path = getPathDepth rootPath + 1 ->
            match PathHelpers.getNameFromPath path |> fun name -> name.ToLowerInvariant() with
            | "studies" -> Some ArcExplorerNodeKind.Study
            | "assays" -> Some ArcExplorerNodeKind.Assay
            | "workflows" -> Some ArcExplorerNodeKind.Workflow
            | "runs" -> Some ArcExplorerNodeKind.Run
            | _ -> None
        | _ -> None

let tryBuildArcCreateDraft kind (identifier: string) (existingPaths: string seq) =
    let identifier = identifier.Trim()
    let label = ArcExplorerNodeKind.label kind

    if isArcCreateIdentifierValid identifier |> not then
        Error arcCreateIdentifierError
    else
        match tryCreateArcFile kind identifier with
        | Error errorMessage -> Error errorMessage
        | Ok arcFile ->
            match FileContentDTO.fromArcFile arcFile with
            | None -> Error $"Creating {label} files is not supported in Electron yet."
            | Some request ->
                let requestedPath = PathHelpers.normalizePath request.path

                let alreadyExists =
                    existingPaths
                    |> Seq.exists (fun path -> PathHelpers.pathsEqual (PathHelpers.normalizePath path) requestedPath)

                if alreadyExists then
                    Error $"{label} '{identifier}' already exists."
                else
                    Ok {
                        ArcFile = arcFile
                        Path = requestedPath
                    }
