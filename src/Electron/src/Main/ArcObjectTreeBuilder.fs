[<RequireQualifiedAccess>]
module Main.ArcObjectTreeBuilder

open System
open System.Collections.Generic
open ARCtrl
open Swate.Components
open Swate.Electron.Shared.FileIOTypes
open Swate.Electron.Shared.FileIOHelper

let private dataMapFileName = "isa.datamap.xlsx"

let private tryFindEntryByRelativePath (entries: Dictionary<string, FileEntry>) (relativePath: string) =
    let normalizedPath = normalizePath relativePath

    match entries.TryGetValue normalizedPath with
    | true, entry -> Some entry
    | false, _ -> None

let private tryGetIsLfs (entries: Dictionary<string, FileEntry>) (relativePath: string) =
    tryFindEntryByRelativePath entries relativePath
    |> Option.bind (fun entry -> entry.isLfs)

let private siblingDataMapPath (parentPath: string) =
    let normalizedPath = normalizePath parentPath
    let lastSeparatorIndex = normalizedPath.LastIndexOf('/')

    if lastSeparatorIndex < 0 then
        dataMapFileName
    else
        $"{normalizedPath.Substring(0, lastSeparatorIndex + 1)}{dataMapFileName}"

let private fallbackTableName index (table: ArcTable) =
    if String.IsNullOrWhiteSpace table.Name then
        $"Table {index + 1}"
    else
        table.Name

let private noteDisplayName (relativePath: string) =
    let normalizedPath = normalizePath relativePath
    let fileNameStart = normalizedPath.LastIndexOf('/') + 1
    let fileName = normalizedPath.Substring(fileNameStart)
    let extensionStart = fileName.LastIndexOf('.')

    let nameWithoutExtension =
        if extensionStart > 0 then
            fileName.Substring(0, extensionStart)
        else
            fileName

    nameWithoutExtension.Replace("_", " ").Trim()

let private isNoteMarkdownPath (relativePath: string) =
    let lowered = normalizePath relativePath |> fun path -> path.ToLowerInvariant()
    lowered.StartsWith("notes/", StringComparison.Ordinal) && lowered.EndsWith(".md", StringComparison.Ordinal)

let private createGroupNode nodeId name children =
    ArcExplorerNode.create (nodeId, name, ArcExplorerNodeKind.Group, isSelectable = false, children = children)

let private createTableNodes parentId parentPath isLfs (tables: ResizeArray<ArcTable>) =
    tables
    |> Seq.mapi (fun index table ->
        ArcExplorerNode.create (
            $"{parentId}:table:{index}",
            fallbackTableName index table,
            ArcExplorerNodeKind.Table,
            path = Some parentPath,
            previewTarget = ArcExplorerNodePreviewTarget.Table index,
            isLfs = isLfs
        ))
    |> List.ofSeq

let private createDataMapChild parentId parentPath (entries: Dictionary<string, FileEntry>) hasDataMap =
    if not hasDataMap then
        []
    else
        let dataMapPath = siblingDataMapPath parentPath

        [
            ArcExplorerNode.create (
                $"{parentId}:datamap",
                "DataMap",
                ArcExplorerNodeKind.DataMap,
                path = Some dataMapPath,
                isLfs = tryGetIsLfs entries dataMapPath
            )
        ]

let private createStudyNode (entries: Dictionary<string, FileEntry>) (study: ArcStudy) =
    let studyPath = ARCtrl.Helper.Identifier.Study.fileNameFromIdentifier study.Identifier
    let nodeId = $"study:{study.Identifier}"
    let children =
        createDataMapChild nodeId studyPath entries study.DataMap.IsSome
        @ createTableNodes nodeId studyPath (tryGetIsLfs entries studyPath) study.Tables

    ArcExplorerNode.create (
        nodeId,
        study.Identifier,
        ArcExplorerNodeKind.Study,
        path = Some studyPath,
        isLfs = tryGetIsLfs entries studyPath,
        children = children
    )

let private createAssayNode (entries: Dictionary<string, FileEntry>) (assay: ArcAssay) =
    let assayPath = ARCtrl.Helper.Identifier.Assay.fileNameFromIdentifier assay.Identifier
    let nodeId = $"assay:{assay.Identifier}"
    let children =
        createDataMapChild nodeId assayPath entries assay.DataMap.IsSome
        @ createTableNodes nodeId assayPath (tryGetIsLfs entries assayPath) assay.Tables

    ArcExplorerNode.create (
        nodeId,
        assay.Identifier,
        ArcExplorerNodeKind.Assay,
        path = Some assayPath,
        isLfs = tryGetIsLfs entries assayPath,
        children = children
    )

let private createWorkflowNode (entries: Dictionary<string, FileEntry>) (workflow: ArcWorkflow) =
    let workflowPath = ARCtrl.Helper.Identifier.Workflow.fileNameFromIdentifier workflow.Identifier
    let nodeId = $"workflow:{workflow.Identifier}"
    let children = createDataMapChild nodeId workflowPath entries workflow.DataMap.IsSome

    ArcExplorerNode.create (
        nodeId,
        workflow.Identifier,
        ArcExplorerNodeKind.Workflow,
        path = Some workflowPath,
        isLfs = tryGetIsLfs entries workflowPath,
        children = children
    )

let private createRunNode (entries: Dictionary<string, FileEntry>) (run: ArcRun) =
    let runPath = ARCtrl.Helper.Identifier.Run.fileNameFromIdentifier run.Identifier
    let nodeId = $"run:{run.Identifier}"
    let children =
        createDataMapChild nodeId runPath entries run.DataMap.IsSome
        @ createTableNodes nodeId runPath (tryGetIsLfs entries runPath) run.Tables

    ArcExplorerNode.create (
        nodeId,
        run.Identifier,
        ArcExplorerNodeKind.Run,
        path = Some runPath,
        isLfs = tryGetIsLfs entries runPath,
        children = children
    )

let private createNoteNodes (entries: Dictionary<string, FileEntry>) =
    entries.Values
    |> Seq.filter (fun entry -> not entry.isDirectory && isNoteMarkdownPath entry.path)
    |> Seq.sortBy (fun entry -> normalizePath entry.path)
    |> Seq.map (fun entry ->
        let relativePath = normalizePath entry.path

        ArcExplorerNode.create (
            $"note:{relativePath}",
            noteDisplayName relativePath,
            ArcExplorerNodeKind.Note,
            path = Some relativePath,
            isLfs = entry.isLfs
        ))
    |> List.ofSeq

let private sortedByName (nodes: ArcExplorerNode list) =
    nodes |> List.sortBy (fun node -> node.name.ToLowerInvariant())

let create (arcRootPath: string) (arc: ARC) (fileEntries: seq<FileEntry>) =
    let rendererEntries = toRendererFileTree arcRootPath fileEntries
    let investigationPath = ARCtrl.ArcPathHelper.InvestigationFileName

    let groups =
        [
            let studies =
                arc.Studies
                |> Seq.map (createStudyNode rendererEntries)
                |> List.ofSeq
                |> sortedByName

            if List.isEmpty studies |> not then
                createGroupNode "group:studies" "Studies" studies

            let assays =
                arc.Assays
                |> Seq.map (createAssayNode rendererEntries)
                |> List.ofSeq
                |> sortedByName

            if List.isEmpty assays |> not then
                createGroupNode "group:assays" "Assays" assays

            let workflows =
                arc.Workflows
                |> Seq.map (createWorkflowNode rendererEntries)
                |> List.ofSeq
                |> sortedByName

            if List.isEmpty workflows |> not then
                createGroupNode "group:workflows" "Workflows" workflows

            let runs =
                arc.Runs
                |> Seq.map (createRunNode rendererEntries)
                |> List.ofSeq
                |> sortedByName

            if List.isEmpty runs |> not then
                createGroupNode "group:runs" "Runs" runs

            let notes = createNoteNodes rendererEntries

            if List.isEmpty notes |> not then
                createGroupNode "group:notes" "Notes" notes
        ]

    [
        ArcExplorerNode.create (
            "arc",
            arc.Identifier,
            ArcExplorerNodeKind.Arc,
            path = Some investigationPath,
            isLfs = tryGetIsLfs rendererEntries investigationPath,
            children = groups
        )
    ]
