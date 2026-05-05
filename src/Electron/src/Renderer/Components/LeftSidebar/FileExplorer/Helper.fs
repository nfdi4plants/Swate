module Renderer.Components.LeftSidebar.FileExplorer.Helper

open Renderer.Components.ARCHelper
open Swate.Components
open Swate.Components.ErrorModal
open Swate.Components.FileExplorer.Types
open Swate.Components.Shared
open Swate.Electron.Shared.FileIOHelper
open Swate.Electron.Shared.FileIOTypes
open Feliz
open Fable.Core
open Fable.Core.JsInterop
open ARCtrl
open Types

let private normalizeNodePath (path: string) = normalizePath path

let tryGetArcFileRelativePath (arcFile: ArcFiles) =
    arcFile.TryGetRelativePath() |> Option.map normalizePath

let tryPendingArcFileEntry (arcFile: ArcFiles) =
    tryGetArcFileRelativePath arcFile
    |> Option.map (fun path -> FileEntry.create (getFileName path, path, false))

let withPendingArcFileEntry (fileTree: FileEntry[]) (pendingArcFile: ArcFiles option) =
    match pendingArcFile |> Option.bind tryPendingArcFileEntry with
    | Some pendingEntry when
        fileTree
        |> Array.exists (fun entry -> PathHelpers.pathsEqual entry.path pendingEntry.path)
        |> not
        ->
        Array.append fileTree [| pendingEntry |]
    | _ -> fileTree

let tryFindPendingArcFileByPath (path: string) (pendingArcFile: ArcFiles option) =
    pendingArcFile
    |> Option.filter (fun arcFile ->
        tryGetArcFileRelativePath arcFile
        |> Option.exists (fun pendingPath -> PathHelpers.pathsEqual pendingPath path)
    )

let rec private collectSelectedDirectoryPathChain
    (selectedTreeItemPath: string option)
    (node: FileTreeNode)
    (loadedPaths: Set<string>)
    =
    let normalizedNodePath = normalizeNodePath node.path

    let isInSelectedPathChain =
        selectedTreeItemPath
        |> Option.exists (fun focusedPath -> isSameOrDescendantPath focusedPath normalizedNodePath)

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
            Set.singleton (normalizeNodePath root.path)
        else
            Set.empty

    collectSelectedDirectoryPathChain selectedTreeItemPath root rootPathSet

let rec loopPaths (loadedDirectoryPaths: Set<string>) (selectedTreeItemPath: string option) (parent: FileTreeNode) =
    match parent.isDirectory with
    | true ->
        let normalizedParentPath = normalizeNodePath parent.path
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
            FileTree.createFolder parent.name (Some parent.path) FileItemIcon.Folder with
                Id = parent.path
                IsExpanded =
                    selectedTreeItemPath
                    |> Option.exists (fun focusedPath -> isSameOrDescendantPath focusedPath parent.path)
                Children = children
        }
        |> Renderer.Components.FileExplorerLfs.withFileTreeNodeLfsState parent
        |> Some
    | false ->
        {
            FileTree.createFile parent.name (Some parent.path) FileItemIcon.Document with
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
                let requestedPath = normalizePath request.path

                let alreadyExists =
                    existingPaths
                    |> Seq.exists (fun path -> PathHelpers.pathsEqual (normalizePath path) requestedPath)

                if alreadyExists then
                    Error $"{label} '{identifier}' already exists."
                else
                    Ok {
                        ArcFile = arcFile
                        Path = requestedPath
                    }