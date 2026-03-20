module Main.ArcExplorerTreeCreator

open System
open ARCtrl
open Swate.Electron.Shared.FileIOTypes

type private NoteTarget =
    | Root
    | Study of string
    | Assay of string
    | Workflow of string
    | Run of string

type private NoteEntry = {
    Name: string
    RelativePath: string
    AbsolutePath: string
    Target: NoteTarget
    IsLfs: bool option
}

module private ArcExplorerTreeCreator =

    let normalizePath (path: string) = path.Replace("\\", "/").TrimEnd('/')

    let normalizeRelativePath (path: string) = path.Replace("\\", "/").Trim('/').Trim()

    let fileNameFromPath (path: string) =
        let normalizedPath = normalizePath path

        if String.IsNullOrWhiteSpace normalizedPath then
            normalizedPath
        else
            let segments =
                normalizedPath.Split('/', StringSplitOptions.RemoveEmptyEntries)

            if segments.Length = 0 then normalizedPath else segments.[segments.Length - 1]

    let tryGetRepoRelativePath (repoRoot: string) (absolutePath: string) =
        let normalizedRoot = normalizePath repoRoot
        let normalizedAbsolutePath = normalizePath absolutePath

        if String.IsNullOrWhiteSpace normalizedRoot || String.IsNullOrWhiteSpace normalizedAbsolutePath then
            None
        elif String.Equals(normalizedRoot, normalizedAbsolutePath, StringComparison.OrdinalIgnoreCase) then
            None
        elif normalizedAbsolutePath.StartsWith(normalizedRoot + "/", StringComparison.OrdinalIgnoreCase) then
            Some(normalizedAbsolutePath.Substring(normalizedRoot.Length + 1))
        else
            None

    let isNoteMarkdownPath (relativePath: string) =
        let normalized = normalizeRelativePath relativePath
        normalized.StartsWith("Notes/", StringComparison.OrdinalIgnoreCase)
        && normalized.EndsWith(".md", StringComparison.OrdinalIgnoreCase)

    let tryParseNoteTarget (relativePath: string) =
        let segments =
            (normalizeRelativePath relativePath).Split('/', StringSplitOptions.RemoveEmptyEntries)

        if segments.Length < 2 || not (segments.[0].Equals("Notes", StringComparison.OrdinalIgnoreCase)) then
            Root
        elif
            segments.Length >= 5
            && segments.[1].Equals("studies", StringComparison.OrdinalIgnoreCase)
            && not (String.IsNullOrWhiteSpace segments.[2])
        then
            Study segments.[2]
        elif
            segments.Length >= 5
            && segments.[1].Equals("assays", StringComparison.OrdinalIgnoreCase)
            && not (String.IsNullOrWhiteSpace segments.[2])
        then
            Assay segments.[2]
        elif
            segments.Length >= 5
            && segments.[1].Equals("workflows", StringComparison.OrdinalIgnoreCase)
            && not (String.IsNullOrWhiteSpace segments.[2])
        then
            Workflow segments.[2]
        elif
            segments.Length >= 5
            && segments.[1].Equals("runs", StringComparison.OrdinalIgnoreCase)
            && not (String.IsNullOrWhiteSpace segments.[2])
        then
            Run segments.[2]
        else
            Root

    let stripExtension (fileName: string) =
        if String.IsNullOrWhiteSpace fileName then
            fileName
        else
            let normalizedFileName = fileNameFromPath fileName
            let extensionIndex = normalizedFileName.LastIndexOf('.')

            if extensionIndex <= 0 then
                normalizedFileName
            else
                normalizedFileName.Substring(0, extensionIndex)

    let sortByName (nodes: ArcExplorerNode list) =
        nodes |> List.sortBy (fun node -> node.name.ToLowerInvariant (), node.name)

    let createGroup id name children =
        ArcExplorerNode.create (id, name, ArcExplorerNodeKind.Group, isSelectable = false, children = children)

    let createRootNode (rootPath: string) (children: ArcExplorerNode list) (isLfs: bool option) =
        ArcExplorerNode.create (
            "arc",
            fileNameFromPath rootPath,
            ArcExplorerNodeKind.Arc,
            path = Some(normalizePath $"{normalizePath rootPath}/isa.investigation.xlsx"),
            isLfs = isLfs,
            children = children
        )

    let createObjectNode id name kind path isReference isLfs children =
        ArcExplorerNode.create (
            id,
            name,
            kind,
            path = path,
            isReference = isReference,
            isLfs = isLfs,
            children = children
        )

    let createTableNode parentId (tableIndex: int) (table: ArcTable) path isLfs =
        let tableName =
            if String.IsNullOrWhiteSpace table.Name then
                $"Table {tableIndex + 1}"
            else
                table.Name

        ArcExplorerNode.create (
            $"{parentId}:table:{tableIndex}",
            tableName,
            ArcExplorerNodeKind.Table,
            path = Some path,
            previewTarget = ArcExplorerNodePreviewTarget.Table tableIndex,
            isLfs = isLfs
        )

    let createSampleNode id name isReference =
        ArcExplorerNode.create (id, name, ArcExplorerNodeKind.Sample, isReference = isReference)

    let createNoteNode id name path isReference isLfs =
        ArcExplorerNode.create (
            id,
            name,
            ArcExplorerNodeKind.Note,
            path = Some path,
            isReference = isReference,
            isLfs = isLfs
        )

    let createDataMapNode parentId path isLfs =
        ArcExplorerNode.create (
            $"{parentId}:datamap",
            "DataMap",
            ArcExplorerNodeKind.DataMap,
            path = Some path,
            isLfs = isLfs
        )

    let investigationPreviewPath (arcPath: string) = $"{normalizePath arcPath}/isa.investigation.xlsx" |> normalizePath

    let studyPreviewPath (arcPath: string) (identifier: string) =
        $"{normalizePath arcPath}/studies/{identifier}/isa.study.xlsx" |> normalizePath

    let assayPreviewPath (arcPath: string) (identifier: string) =
        $"{normalizePath arcPath}/assays/{identifier}/isa.assay.xlsx" |> normalizePath

    let workflowPreviewPath (arcPath: string) (identifier: string) =
        $"{normalizePath arcPath}/workflows/{identifier}/isa.workflow.xlsx" |> normalizePath

    let runPreviewPath (arcPath: string) (identifier: string) =
        $"{normalizePath arcPath}/runs/{identifier}/isa.run.xlsx" |> normalizePath

    let objectDataMapPath (arcPath: string) (folderName: string) (identifier: string) =
        $"{normalizePath arcPath}/{folderName}/{identifier}/isa.datamap.xlsx" |> normalizePath

    let tryGetLfsByPath (lfsByPath: Map<string, bool option>) (path: string option) =
        path
        |> Option.bind (fun p -> lfsByPath |> Map.tryFind (normalizePath p) |> Option.defaultValue None)

    let collectNotes (arcPath: string) (fileEntries: FileEntry seq) =
        fileEntries
        |> Seq.filter (fun entry -> not entry.isDirectory)
        |> Seq.choose (fun entry ->
            tryGetRepoRelativePath arcPath entry.path
            |> Option.filter isNoteMarkdownPath
            |> Option.map (fun relativePath -> {
                Name = stripExtension entry.name
                RelativePath = normalizeRelativePath relativePath
                AbsolutePath = normalizePath entry.path
                Target = tryParseNoteTarget relativePath
                IsLfs = entry.isLfs
            }))
        |> List.ofSeq

    let extractSampleNames (tables: ResizeArray<ArcTable>) =
        tables
        |> Seq.collect (fun table ->
            table.Columns
            |> Seq.choose (fun column ->
                match column.Header.TryIOType() with
                | Some ioType when ioType = IOType.Sample ->
                    Some(
                        column.Cells
                        |> Seq.map (fun cell -> cell.ToFreeTextCell().AsFreeText.Trim())
                        |> Seq.filter (String.IsNullOrWhiteSpace >> not)
                    )
                | _ -> None)
            |> Seq.collect id)
        |> Seq.distinct
        |> Seq.sortBy (fun name -> name.ToLowerInvariant (), name)
        |> List.ofSeq

    let extractStudySampleNames (assaysByIdentifier: Map<string, ArcAssay>) (study: ArcStudy) =
        seq {
            yield! extractSampleNames study.Tables

            for assayIdentifier in study.RegisteredAssayIdentifiers |> Seq.distinct do
                match assaysByIdentifier |> Map.tryFind (assayIdentifier.ToLowerInvariant()) with
                | Some assay -> yield! extractSampleNames assay.Tables
                | None -> ()
        }
        |> Seq.distinct
        |> Seq.sortBy (fun name -> name.ToLowerInvariant (), name)
        |> List.ofSeq

    let tryFindNotesForTarget target notes =
        notes
        |> List.filter (fun note -> note.Target = target)

    let createNotesGroup parentId notes =
        if List.isEmpty notes then
            None
        else
            notes
            |> List.sortBy (fun note -> note.RelativePath.ToLowerInvariant (), note.RelativePath)
            |> List.map (fun note ->
                createNoteNode
                    $"{parentId}:note-ref:{note.RelativePath.ToLowerInvariant()}"
                    note.Name
                    note.AbsolutePath
                    true
                    note.IsLfs)
            |> sortByName
            |> createGroup $"{parentId}:notes" "Notes"
            |> Some

    let createSamplesGroup parentId (isReference: bool) (sampleNames: string list) =
        if List.isEmpty sampleNames then
            None
        else
            sampleNames
            |> List.map (fun sampleName ->
                let nodeIdPart = if isReference then "sample-ref" else "sample"
                createSampleNode $"{parentId}:{nodeIdPart}:{sampleName.ToLowerInvariant()}" sampleName isReference)
            |> sortByName
            |> createGroup $"{parentId}:samples" "Samples"
            |> Some

    let createTablesGroup parentId previewPath isLfs (tables: ResizeArray<ArcTable>) =
        tables
        |> Seq.mapi (fun tableIndex table -> createTableNode parentId tableIndex table previewPath isLfs)
        |> List.ofSeq
        |> function
            | [] -> None
            | tableNodes -> tableNodes |> createGroup $"{parentId}:tables" "Tables" |> Some

    let createStudyRelationshipsGroup arcPath lfsByPath parentId (study: ArcStudy) =
        let assayRefs =
            study.RegisteredAssayIdentifiers
            |> Seq.distinct
            |> Seq.sortBy (fun identifier -> identifier.ToLowerInvariant (), identifier)
            |> Seq.map (fun assayIdentifier ->
                let previewPath =
                    match study.Investigation with
                    | Some investigation when investigation.ContainsAssay assayIdentifier ->
                        Some(assayPreviewPath arcPath assayIdentifier)
                    | _ -> None

                createObjectNode
                    $"{parentId}:assay-ref:{assayIdentifier}"
                    assayIdentifier
                    ArcExplorerNodeKind.Assay
                    previewPath
                    true
                    (tryGetLfsByPath lfsByPath previewPath)
                    [])
            |> List.ofSeq

        if List.isEmpty assayRefs then
            None
        else
            assayRefs |> sortByName |> createGroup $"{parentId}:assays" "Assays" |> Some

    let createWorkflowRelationshipsGroup arcPath lfsByPath parentId (workflow: ArcWorkflow) =
        let workflowRefs =
            workflow.SubWorkflowIdentifiers
            |> Seq.distinct
            |> Seq.sortBy (fun identifier -> identifier.ToLowerInvariant (), identifier)
            |> Seq.map (fun workflowIdentifier ->
                let previewPath =
                    match workflow.Investigation with
                    | Some investigation when investigation.ContainsWorkflow workflowIdentifier ->
                        Some(workflowPreviewPath arcPath workflowIdentifier)
                    | _ -> None

                createObjectNode
                    $"{parentId}:workflow-ref:{workflowIdentifier}"
                    workflowIdentifier
                    ArcExplorerNodeKind.Workflow
                    previewPath
                    true
                    (tryGetLfsByPath lfsByPath previewPath)
                    [])
            |> List.ofSeq

        if List.isEmpty workflowRefs then
            None
        else
            workflowRefs |> sortByName |> createGroup $"{parentId}:workflows" "Subworkflows" |> Some

    let createRunRelationshipsGroup arcPath lfsByPath parentId (run: ArcRun) =
        let workflowRefs =
            run.WorkflowIdentifiers
            |> Seq.distinct
            |> Seq.sortBy (fun identifier -> identifier.ToLowerInvariant (), identifier)
            |> Seq.map (fun workflowIdentifier ->
                let previewPath =
                    match run.Investigation with
                    | Some investigation when investigation.ContainsWorkflow workflowIdentifier ->
                        Some(workflowPreviewPath arcPath workflowIdentifier)
                    | _ -> None

                createObjectNode
                    $"{parentId}:workflow-ref:{workflowIdentifier}"
                    workflowIdentifier
                    ArcExplorerNodeKind.Workflow
                    previewPath
                    true
                    (tryGetLfsByPath lfsByPath previewPath)
                    [])
            |> List.ofSeq

        if List.isEmpty workflowRefs then
            None
        else
            workflowRefs |> sortByName |> createGroup $"{parentId}:workflows" "Workflows" |> Some

    let createStudyNode arcPath assaysByIdentifier lfsByPath notes (study: ArcStudy) =
        let parentId = $"study:{study.Identifier}"
        let previewPath = Some(studyPreviewPath arcPath study.Identifier)
        let dataMapPath = objectDataMapPath arcPath "studies" study.Identifier
        let studyNotes = tryFindNotesForTarget (Study study.Identifier) notes
        let studySamples = extractStudySampleNames assaysByIdentifier study
        let studyIsLfs = tryGetLfsByPath lfsByPath previewPath

        [
            createStudyRelationshipsGroup arcPath lfsByPath parentId study
            createTablesGroup parentId (previewPath.Value) studyIsLfs study.Tables
            if study.DataMap.IsSome then
                createDataMapNode parentId dataMapPath (tryGetLfsByPath lfsByPath (Some dataMapPath)) |> Some
            createNotesGroup parentId studyNotes
            createSamplesGroup parentId false studySamples
        ]
        |> List.choose id
        |> createObjectNode
            parentId
            study.Identifier
            ArcExplorerNodeKind.Study
            previewPath
            false
            studyIsLfs

    let createAssayNode arcPath lfsByPath notes (assay: ArcAssay) =
        let parentId = $"assay:{assay.Identifier}"
        let previewPath = Some(assayPreviewPath arcPath assay.Identifier)
        let dataMapPath = objectDataMapPath arcPath "assays" assay.Identifier
        let assayNotes = tryFindNotesForTarget (Assay assay.Identifier) notes
        let assaySamples = extractSampleNames assay.Tables
        let assayIsLfs = tryGetLfsByPath lfsByPath previewPath

        [
            createTablesGroup parentId (previewPath.Value) assayIsLfs assay.Tables
            if assay.DataMap.IsSome then
                createDataMapNode parentId dataMapPath (tryGetLfsByPath lfsByPath (Some dataMapPath)) |> Some
            createNotesGroup parentId assayNotes
            createSamplesGroup parentId true assaySamples
        ]
        |> List.choose id
        |> createObjectNode
            parentId
            assay.Identifier
            ArcExplorerNodeKind.Assay
            previewPath
            false
            assayIsLfs

    let createWorkflowNode arcPath lfsByPath notes (workflow: ArcWorkflow) =
        let parentId = $"workflow:{workflow.Identifier}"
        let previewPath = Some(workflowPreviewPath arcPath workflow.Identifier)
        let dataMapPath = objectDataMapPath arcPath "workflows" workflow.Identifier
        let workflowNotes = tryFindNotesForTarget (Workflow workflow.Identifier) notes

        [
            createWorkflowRelationshipsGroup arcPath lfsByPath parentId workflow
            if workflow.DataMap.IsSome then
                createDataMapNode parentId dataMapPath (tryGetLfsByPath lfsByPath (Some dataMapPath)) |> Some
            createNotesGroup parentId workflowNotes
        ]
        |> List.choose id
        |> createObjectNode
            parentId
            workflow.Identifier
            ArcExplorerNodeKind.Workflow
            previewPath
            false
            (tryGetLfsByPath lfsByPath previewPath)

    let createRunNode arcPath lfsByPath notes (run: ArcRun) =
        let parentId = $"run:{run.Identifier}"
        let previewPath = Some(runPreviewPath arcPath run.Identifier)
        let dataMapPath = objectDataMapPath arcPath "runs" run.Identifier
        let runNotes = tryFindNotesForTarget (Run run.Identifier) notes
        let runSamples = extractSampleNames run.Tables
        let runIsLfs = tryGetLfsByPath lfsByPath previewPath

        [
            createRunRelationshipsGroup arcPath lfsByPath parentId run
            createTablesGroup parentId (previewPath.Value) runIsLfs run.Tables
            if run.DataMap.IsSome then
                createDataMapNode parentId dataMapPath (tryGetLfsByPath lfsByPath (Some dataMapPath)) |> Some
            createNotesGroup parentId runNotes
            createSamplesGroup parentId true runSamples
        ]
        |> List.choose id
        |> createObjectNode
            parentId
            run.Identifier
            ArcExplorerNodeKind.Run
            previewPath
            false
            runIsLfs

    let createTopLevelNotesGroup (notes: NoteEntry list) =
        let createCanonicalNoteNodes idPrefix noteEntries =
            noteEntries
            |> List.sortBy (fun note -> note.RelativePath.ToLowerInvariant (), note.RelativePath)
            |> List.map (fun note ->
                createNoteNode
                    $"{idPrefix}:{note.RelativePath.ToLowerInvariant()}"
                    note.Name
                    note.AbsolutePath
                    false
                    note.IsLfs)

        let children =
            notes
            |> createCanonicalNoteNodes "note"
            |> sortByName

        createGroup "group:notes" "Notes" children

    let createTopLevelSamplesGroup (assaysByIdentifier: Map<string, ArcAssay>) (arc: ARC) =
        let studyBuckets =
            arc.Studies
            |> Seq.choose (fun study ->
                let sampleNames = extractStudySampleNames assaysByIdentifier study

                sampleNames
                |> createSamplesGroup $"sample-index:study:{study.Identifier}" true
                |> Option.map (fun samplesGroup -> createGroup $"sample-index:study:{study.Identifier}:group" study.Identifier [ samplesGroup ]))
            |> List.ofSeq

        let runBuckets =
            arc.Runs
            |> Seq.choose (fun run ->
                let sampleNames = extractSampleNames run.Tables

                sampleNames
                |> createSamplesGroup $"sample-index:run:{run.Identifier}" true
                |> Option.map (fun samplesGroup -> createGroup $"sample-index:run:{run.Identifier}:group" run.Identifier [ samplesGroup ]))
            |> List.ofSeq

        [
            if not (List.isEmpty studyBuckets) then
                createGroup "group:samples:studies" "Studies" (studyBuckets |> sortByName)
            if not (List.isEmpty runBuckets) then
                createGroup "group:samples:runs" "Runs" (runBuckets |> sortByName)
        ]
        |> createGroup "group:samples" "Samples"

let createArcExplorerTree (arcPath: string) (arc: ARC) (fileEntries: FileEntry seq) =
    let normalizedArcPath = ArcExplorerTreeCreator.normalizePath arcPath

    let fileEntryList = fileEntries |> List.ofSeq

    let assaysByIdentifier =
        arc.Assays
        |> Seq.map (fun assay -> assay.Identifier.ToLowerInvariant(), assay)
        |> Map.ofSeq

    let lfsByPath =
        fileEntryList
        |> Seq.map (fun entry -> ArcExplorerTreeCreator.normalizePath entry.path, entry.isLfs)
        |> Map.ofSeq

    let notes = ArcExplorerTreeCreator.collectNotes normalizedArcPath fileEntryList

    let children =
        [
            arc.Studies
            |> Seq.map (ArcExplorerTreeCreator.createStudyNode normalizedArcPath assaysByIdentifier lfsByPath notes)
            |> List.ofSeq
            |> ArcExplorerTreeCreator.sortByName
            |> ArcExplorerTreeCreator.createGroup "group:studies" "Studies"
            arc.Assays
            |> Seq.map (ArcExplorerTreeCreator.createAssayNode normalizedArcPath lfsByPath notes)
            |> List.ofSeq
            |> ArcExplorerTreeCreator.sortByName
            |> ArcExplorerTreeCreator.createGroup "group:assays" "Assays"
            arc.Workflows
            |> Seq.map (ArcExplorerTreeCreator.createWorkflowNode normalizedArcPath lfsByPath notes)
            |> List.ofSeq
            |> ArcExplorerTreeCreator.sortByName
            |> ArcExplorerTreeCreator.createGroup "group:workflows" "Workflows"
            arc.Runs
            |> Seq.map (ArcExplorerTreeCreator.createRunNode normalizedArcPath lfsByPath notes)
            |> List.ofSeq
            |> ArcExplorerTreeCreator.sortByName
            |> ArcExplorerTreeCreator.createGroup "group:runs" "Runs"
            ArcExplorerTreeCreator.createTopLevelNotesGroup notes
            ArcExplorerTreeCreator.createTopLevelSamplesGroup assaysByIdentifier arc
        ]

    let rootPath = ArcExplorerTreeCreator.investigationPreviewPath normalizedArcPath

    [
        ArcExplorerTreeCreator.createRootNode normalizedArcPath children (ArcExplorerTreeCreator.tryGetLfsByPath lfsByPath (Some rootPath))
    ]
