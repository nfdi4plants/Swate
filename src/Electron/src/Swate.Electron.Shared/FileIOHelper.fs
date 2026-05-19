module Swate.Electron.Shared.FileIOHelper

open System
open System.Collections.Generic
open ARCtrl
open Swate.Components.Shared
open Swate.Electron.Shared.FileIOTypes


let getNonEmptyPathParts (path: string) =
    PathHelpers.normalizePath path
    |> fun p -> p.Split('/', StringSplitOptions.RemoveEmptyEntries)

let getPathDepth (path: string) =
    path |> getNonEmptyPathParts |> Array.length

let pathsEqual (left: string) (right: string) =
    PathHelpers.normalizePath left = PathHelpers.normalizePath right

let private tryGetRepoRelativePathCore (repoRoot: string) (absolutePath: string) (allowRoot: bool) =
    let normalizedRoot = PathHelpers.normalizePath repoRoot
    let normalizedAbsolutePath = PathHelpers.normalizePath absolutePath

    if String.IsNullOrWhiteSpace normalizedRoot || String.IsNullOrWhiteSpace normalizedAbsolutePath then
        None
    elif pathsEqual normalizedAbsolutePath normalizedRoot then
        if allowRoot then Some "" else None
    elif PathHelpers.isSameOrDescendantPath normalizedAbsolutePath normalizedRoot then
        let prefix = normalizedRoot + "/"

        if normalizedAbsolutePath.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) then
            let relativePath = normalizedAbsolutePath.Substring(prefix.Length)

            if String.IsNullOrWhiteSpace relativePath || PathHelpers.containsPathTraversalSegments relativePath then
                None
            else
                Some relativePath
        else
            None
    else
        None

/// Tries to convert an absolute repository path to a repository-relative path.
/// Returns None for the repository root and unsafe traversal-like paths.
let tryGetRepoRelativePath (repoRoot: string) (absolutePath: string) =
    tryGetRepoRelativePathCore repoRoot absolutePath false

/// Tries to convert an absolute repository path to a repository-relative path.
/// Returns Some "" for the repository root and None for unsafe traversal-like paths.
let tryGetRepoRelativePathOrRoot (repoRoot: string) (absolutePath: string) =
    tryGetRepoRelativePathCore repoRoot absolutePath true

let tryGetPathSegmentAfterFolder (folderName: string) (path: string) =
    let segments = getNonEmptyPathParts path

    match
        segments
        |> Array.tryFindIndex (fun segment -> String.Equals(segment, folderName, StringComparison.OrdinalIgnoreCase))
    with
    | Some index when index + 1 < segments.Length ->
        let name = segments.[index + 1].Trim()

        if String.IsNullOrWhiteSpace name then None else Some name
    | _ -> None

let resolveArcPreviewPath (path: string) =
    Swate.Components.Shared.PathHelpers.resolveArcViewPath path

let private insertFileTreeEntry (root: FileTreeNode) (rootPath: string) (entry: FileEntry) =
    let parts = getNonEmptyPathParts entry.path
    let rootParts = getNonEmptyPathParts rootPath

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
                            Dictionary(),
                            entry.lfs
                        )

                    node.children.Add(part, newNode)
                    newNode

            if not isLast then
                loop child (index + 1)

        loop root rootParts.Length

let toFileTreeNode (fileEntries: FileEntry[]) =

    if fileEntries.Length = 0 then
        failwith "toFileTreeNode requires at least one file entry to determine the root path."

    let normalizedPaths =
        fileEntries |> Array.map (fun fileEntry -> PathHelpers.normalizePath fileEntry.path)

    let rootPath =
        normalizedPaths
        |> Array.distinct
        |> Array.sortBy (fun path -> getPathDepth path, path)
        |> Array.head

    let adaptedFileEntries =
        fileEntries
        |> Array.filter (fun fileEntry -> PathHelpers.normalizePath fileEntry.path <> rootPath)
        // Deterministic order avoids creating parents from file entries before their directory entries.
        |> Array.sortBy (fun fileEntry ->
            let depth = getPathDepth fileEntry.path
            depth, (if fileEntry.isDirectory then 0 else 1), PathHelpers.normalizePath fileEntry.path
        )

    let rootElement =
        let rootEntry =
            fileEntries
            |> Array.find (fun fileEntry -> PathHelpers.normalizePath fileEntry.path = rootPath)

        FileTreeNode.create (
            rootEntry.name,
            rootEntry.isDirectory,
            rootPath,
            Dictionary(),
            rootEntry.lfs
        )

    adaptedFileEntries
    |> Array.iter (fun fileEntry -> insertFileTreeEntry rootElement rootPath fileEntry)

    rootElement

let rec collapseSingleChildSameNameDirectories (node: FileTreeNode) : FileTreeNode =
    let collapsedChildren =
        node.children.Values
        |> Seq.map collapseSingleChildSameNameDirectories
        |> Seq.toList

    let childrenByName = Dictionary<string, FileTreeNode>()

    collapsedChildren
    |> List.iter (fun (child: FileTreeNode) -> childrenByName.[child.name] <- child)

    let nodeWithCollapsedChildren =
        { node with
            children = childrenByName }

    if nodeWithCollapsedChildren.isDirectory && nodeWithCollapsedChildren.children.Count = 1 then
        let onlyChild = nodeWithCollapsedChildren.children.Values |> Seq.exactlyOne

        if onlyChild.isDirectory
           && String.Equals(nodeWithCollapsedChildren.name, onlyChild.name, StringComparison.OrdinalIgnoreCase) then
            // Preserve the displayed label while routing interactions to the deepest merged directory path.
            { onlyChild with
                name = nodeWithCollapsedChildren.name }
        else
            nodeWithCollapsedChildren
    else
        nodeWithCollapsedChildren

let tryGetExistingNotesTargetRef (path: string) : ExistingTargetRef option =
    let tryResolveTarget folderName kind =
        tryGetPathSegmentAfterFolder folderName path
        |> Option.map (fun name -> { Name = name; Kind = kind })

    match tryResolveTarget "studies" NotesTargetKind.Study with
    | Some target -> Some target
    | None -> tryResolveTarget "assays" NotesTargetKind.Assay

let createAvailableNotesTargets (fileEntries: seq<FileEntry>) =
    fileEntries
    |> Seq.choose (fun entry -> tryGetExistingNotesTargetRef entry.path)
    |> Seq.distinctBy (fun target -> target.Kind, target.Name)
    |> Seq.sortBy (fun target ->
        let kindOrder =
            match target.Kind with
            | NotesTargetKind.Study -> 0
            | NotesTargetKind.Assay -> 1

        kindOrder, target.Name.ToLowerInvariant()
    )
    |> ResizeArray

let combineMany = ARCtrl.ArcPathHelper.combineMany

let tryGetArcFilePath (arcRootPath: ArcRootPath) (arcFile: ArcFiles) =
    let arcRootPath = defaultArg arcRootPath ""
    let root = PathHelpers.normalizePath arcRootPath

    arcFile.TryGetRelativePath() |> Option.map (fun p -> combineMany [| root; p |])


[<RequireQualifiedAccess>]
module DTOType =

    open ARCtrl.Contract

    /// This function checks if the given DTOType is one of the plain text variants (JSON, YAML, CWL, PlainText).
    let isPlainTextVariant (dtoType: DTOType) =
        match dtoType with
        | DTOType.JSON
        | DTOType.YAML
        | DTOType.CWL
        | DTOType.PlainText -> true
        | _ -> false

    /// This function checks if the given DTOType is one of the ISA file variants (Investigation, Study, Assay, Run, Workflow, Datamap).
    let isISAFileVariant (dtoType: DTOType) =
        match dtoType with
        | DTOType.ISA_Investigation
        | DTOType.ISA_Study
        | DTOType.ISA_Assay
        | DTOType.ISA_Run
        | DTOType.ISA_Workflow
        | DTOType.ISA_Datamap -> true
        | _ -> false

    /// Active pattern for matching all plain text variants of DTOType: (JSON, YAML, CWL, PlainText)
    let (|DTOTypeIsPlainTextVariant|_|) (dtoType: DTOType) =
        if isPlainTextVariant dtoType then Some() else None

    /// Active pattern for matching all ISA file variants of DTOType: (Investigation, Study, Assay, Run, Workflow, Datamap)
    let (|DTOTypeIsISAFileVariant|_|) (dtoType: DTOType) =
        if isISAFileVariant dtoType then Some() else None

[<RequireQualifiedAccess>]
module FileContentDTO =

    open ARCtrl.Helper
    open ARCtrl.Contract
    open ARCtrl.ArcPathHelper

    let DEFAULT_JSON_EXPORT_FORMAT = JsonExportFormat.ARCtrl

    let create fileType content path : FileContentDTO = {|
        fileType = fileType
        content = content
        path = path
    |}

    let normalizeArcFileRequestPath (request: FileContentDTO) : FileContentDTO =
        let normalizedPath = PathHelpers.normalizePath request.path

        if normalizedPath = request.path then
            request
        else
            {| request with
                path = normalizedPath
            |}

    let toArcFile (dto: FileContentDTO) : ArcFiles option =

        let exportFormat = JsonExportFormat.ARCtrl

        let afd =
            match dto.fileType with
            | DTOType.ISA_Investigation -> Some ArcFilesDiscriminate.Investigation
            | DTOType.ISA_Study -> Some ArcFilesDiscriminate.Study
            | DTOType.ISA_Assay -> Some ArcFilesDiscriminate.Assay
            | DTOType.ISA_Run -> Some ArcFilesDiscriminate.Run
            | DTOType.ISA_Workflow -> Some ArcFilesDiscriminate.Workflow
            | DTOType.ISA_Datamap -> Some ArcFilesDiscriminate.DataMap
            | _ -> None

        match afd with
        | Some afd ->
            match Json.Generic.readFromJsonMap.TryGetValue((afd, exportFormat)) with
            | true, fn ->
                match fn dto.content with
                | ArcFiles.DataMap(None, dm) ->
                    let dmpi = DatamapParentInfo.tryFromPath dto.path
                    ArcFiles.DataMap(dmpi, dm)
                | anyElse -> anyElse
                |> Some
            | _ -> None
        | None -> None

    let fromArcFile (arcFile: ArcFiles) : FileContentDTO option =
        let exportFormat = JsonExportFormat.ARCtrl

        let dtoTypeOpt =
            match arcFile with
            | ArcFiles.Investigation _ ->
                Some {|
                    fileType = DTOType.ISA_Investigation
                    path = ARCtrl.ArcPathHelper.InvestigationFileName
                |}
            | ArcFiles.Study(s, _) ->
                Some {|
                    fileType = DTOType.ISA_Study
                    path = ARCtrl.Helper.Identifier.Study.fileNameFromIdentifier s.Identifier
                |}
            | ArcFiles.Assay(a) ->
                Some {|
                    fileType = DTOType.ISA_Assay
                    path = ARCtrl.Helper.Identifier.Assay.fileNameFromIdentifier a.Identifier
                |}
            | ArcFiles.Run r ->
                Some {|
                    fileType = DTOType.ISA_Run
                    path = ARCtrl.Helper.Identifier.Run.fileNameFromIdentifier r.Identifier
                |}
            | ArcFiles.Workflow w ->
                Some {|
                    fileType = DTOType.ISA_Workflow
                    path = ARCtrl.Helper.Identifier.Workflow.fileNameFromIdentifier w.Identifier
                |}
            | ArcFiles.DataMap(Some dmpi, dm) ->
                Some {|
                    fileType = DTOType.ISA_Datamap
                    path = DatamapParentInfo.toPath dmpi
                |}
            | _ -> None

        match dtoTypeOpt with
        | Some dtoType ->
            let _, json = Json.Export.parseToJsonString (arcFile, exportFormat)
            create dtoType.fileType json dtoType.path |> Some
        | None -> None

    let fromArcByPath (path: string) (arc: ARC) =
        let split = ARCtrl.ArcPathHelper.split path
        let exportFormat = DEFAULT_JSON_EXPORT_FORMAT

        /// This must be set if it returns Some
        let mutable discFileType: DTOType option = None

        let arcFileOpt =
            match split with
            | InvestigationPath _ ->
                discFileType <- Some DTOType.ISA_Investigation
                ArcFiles.Investigation arc |> Some
            | AssayPath p ->
                discFileType <- Some DTOType.ISA_Assay
                let identifier = (Identifier.Assay.identifierFromFileName p)
                let assay = arc.TryGetAssay identifier
                assay |> Option.map ArcFiles.Assay
            | StudyPath p ->
                discFileType <- Some DTOType.ISA_Study
                let identifier = (Identifier.Study.identifierFromFileName p)
                let study = arc.TryGetStudy identifier

                study
                |> Option.map (fun s ->
                    let assignedAssays =
                        s.RegisteredAssayIdentifiers |> Seq.choose arc.TryGetAssay |> List.ofSeq

                    ArcFiles.Study(s, assignedAssays)
                )
            | WorkflowPath p ->
                discFileType <- Some DTOType.ISA_Workflow

                let identifier = (Identifier.Workflow.identifierFromFileName p)
                let workflow = arc.TryGetWorkflow identifier
                workflow |> Option.map ArcFiles.Workflow
            | RunPath p ->
                discFileType <- Some DTOType.ISA_Run
                let identifier = (Identifier.Run.identifierFromFileName p)
                let run = arc.TryGetRun identifier
                run |> Option.map ArcFiles.Run
            | DatamapPath _ ->
                discFileType <- Some DTOType.ISA_Datamap

                match split with
                | [| AssaysFolderName; anyAssayName; DataMapFileName |] ->
                    let assay = arc.TryGetAssay(Identifier.Assay.identifierFromFileName anyAssayName)

                    let datamap =
                        assay
                        |> Option.bind (fun a -> a.DataMap)
                        |> Option.map (fun dm ->
                            let dmpi = DatamapParentInfo.create anyAssayName DataMapParent.Assay |> Some
                            dmpi, dm
                        )

                    datamap |> Option.map ArcFiles.DataMap
                | [| StudiesFolderName; anyStudyName; DataMapFileName |] ->
                    let study = arc.TryGetStudy(Identifier.Study.identifierFromFileName anyStudyName)

                    let datamap =
                        study
                        |> Option.bind (fun s -> s.DataMap)
                        |> Option.map (fun dm ->
                            let dmpi = DatamapParentInfo.create anyStudyName DataMapParent.Study |> Some
                            dmpi, dm
                        )

                    datamap |> Option.map ArcFiles.DataMap
                | [| WorkflowsFolderName; anyWorkflowName; DataMapFileName |] ->
                    let workflow =
                        arc.TryGetWorkflow(Identifier.Workflow.identifierFromFileName anyWorkflowName)

                    let datamap =
                        workflow
                        |> Option.bind (fun w -> w.DataMap)
                        |> Option.map (fun dm ->
                            let dmpi = DatamapParentInfo.create anyWorkflowName DataMapParent.Workflow |> Some
                            dmpi, dm
                        )

                    datamap |> Option.map ArcFiles.DataMap
                | [| RunsFolderName; anyRunName; DataMapFileName |] ->
                    let run = arc.TryGetRun(Identifier.Run.identifierFromFileName anyRunName)

                    let datamap =
                        run
                        |> Option.bind (fun r -> r.DataMap)
                        |> Option.map (fun dm ->
                            let dmpi = DatamapParentInfo.create anyRunName DataMapParent.Run |> Some
                            dmpi, dm
                        )

                    datamap |> Option.map ArcFiles.DataMap
                | _ -> None
            | _ -> None

        match arcFileOpt, discFileType with
        | Some arcFile, Some discFileType ->
            let _, json = Json.Export.parseToJsonString (arcFile, exportFormat)
            create discFileType json path |> Some
        | _ -> None
