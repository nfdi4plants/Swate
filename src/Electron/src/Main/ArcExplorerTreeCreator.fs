module Main.ArcExplorerTreeCreator

open System
open ARCtrl
open Swate.Components
open Swate.Components.Shared
open Swate.Electron.Shared.FileIOTypes


module private ArcExplorerTreeCreator =

    let normalizePath = PathHelpers.normalizePath

    let normalizeRelativePath = PathHelpers.normalizeRelativePath

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
            NoteTarget.Root
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

    let createGroupIfAny id name (children: ArcExplorerNode list) =
        match children with
        | [] -> None
        | _ -> children |> createGroup id name |> Some

    let createNoteNodes createId isReference (notes: NoteEntry list) =
        notes
        |> List.sortBy (fun note ->
            note.Name.ToLowerInvariant (),
            note.Name,
            note.RelativePath.ToLowerInvariant (),
            note.RelativePath)
        |> List.map (fun note ->
            createNoteNode
                (createId note)
                note.Name
                note.AbsolutePath
                isReference
                note.IsLfs)

    let createReferenceGroup
        (lfsByPath: Map<string, bool option>)
        parentId
        groupIdSuffix
        groupName
        refIdPrefix
        (kind: ArcExplorerNodeKind)
        (previewPathForIdentifier: string -> string option)
        (identifiers: seq<string>)
        =
        identifiers
        |> Seq.distinct
        |> Seq.sortBy (fun identifier -> identifier.ToLowerInvariant (), identifier)
        |> Seq.map (fun identifier ->
            let previewPath = previewPathForIdentifier identifier

            createObjectNode
                $"{parentId}:{refIdPrefix}:{identifier}"
                identifier
                kind
                previewPath
                true
                (tryGetLfsByPath lfsByPath previewPath)
                [])
        |> List.ofSeq
        |> createGroupIfAny $"{parentId}:{groupIdSuffix}" groupName

    let createOptionalDataMapNode lfsByPath parentId dataMapPath hasDataMap =
        if hasDataMap then
            createDataMapNode parentId dataMapPath (tryGetLfsByPath lfsByPath (Some dataMapPath))
            |> Some
        else
            None

    let createCanonicalObjectNode parentId name kind previewPath isLfs children =
        children
        |> List.choose id
        |> createObjectNode parentId name kind (Some previewPath) false isLfs

    let createNotesGroup parentId (notes: NoteEntry list) =
        notes
        |> createNoteNodes (fun note -> $"{parentId}:note-ref:{note.RelativePath.ToLowerInvariant()}")
            true
        |> createGroupIfAny $"{parentId}:notes" "Notes"

    let createSamplesGroup parentId (isReference: bool) (sampleNames: string list) =
        sampleNames
        |> List.map (fun sampleName ->
            let nodeIdPart = if isReference then "sample-ref" else "sample"
            createSampleNode $"{parentId}:{nodeIdPart}:{sampleName.ToLowerInvariant()}" sampleName isReference)
        |> sortByName
        |> createGroupIfAny $"{parentId}:samples" "Samples"

    let createTablesGroup parentId previewPath isLfs (tables: ResizeArray<ArcTable>) =
        tables
        |> Seq.mapi (fun tableIndex table -> createTableNode parentId tableIndex table previewPath isLfs)
        |> List.ofSeq
        |> createGroupIfAny $"{parentId}:tables" "Tables"

    let createStudyRelationshipsGroup arcPath lfsByPath parentId (study: ArcStudy) =
        createReferenceGroup
            lfsByPath
            parentId
            "assays"
            "Assays"
            "assay-ref"
            ArcExplorerNodeKind.Assay
            (fun assayIdentifier ->
                match study.Investigation with
                | Some investigation when investigation.ContainsAssay assayIdentifier ->
                    Some(assayPreviewPath arcPath assayIdentifier)
                | _ -> None)
            study.RegisteredAssayIdentifiers

    let createWorkflowRelationshipsGroup arcPath lfsByPath parentId (workflow: ArcWorkflow) =
        createReferenceGroup
            lfsByPath
            parentId
            "workflows"
            "Subworkflows"
            "workflow-ref"
            ArcExplorerNodeKind.Workflow
            (fun workflowIdentifier ->
                match workflow.Investigation with
                | Some investigation when investigation.ContainsWorkflow workflowIdentifier ->
                    Some(workflowPreviewPath arcPath workflowIdentifier)
                | _ -> None)
            workflow.SubWorkflowIdentifiers

    let createRunRelationshipsGroup arcPath lfsByPath parentId (run: ArcRun) =
        createReferenceGroup
            lfsByPath
            parentId
            "workflows"
            "Workflows"
            "workflow-ref"
            ArcExplorerNodeKind.Workflow
            (fun workflowIdentifier ->
                match run.Investigation with
                | Some investigation when investigation.ContainsWorkflow workflowIdentifier ->
                    Some(workflowPreviewPath arcPath workflowIdentifier)
                | _ -> None)
            run.WorkflowIdentifiers

    let createStudyNode arcPath assaysByIdentifier lfsByPath notes (study: ArcStudy) =
        let parentId = $"study:{study.Identifier}"
        let previewPath = studyPreviewPath arcPath study.Identifier
        let dataMapPath = objectDataMapPath arcPath "studies" study.Identifier
        let studyNotes = tryFindNotesForTarget (Study study.Identifier) notes
        let studySamples = extractStudySampleNames assaysByIdentifier study
        let studyIsLfs = tryGetLfsByPath lfsByPath (Some previewPath)

        [
            createStudyRelationshipsGroup arcPath lfsByPath parentId study
            createTablesGroup parentId previewPath studyIsLfs study.Tables
            createOptionalDataMapNode lfsByPath parentId dataMapPath study.DataMap.IsSome
            createNotesGroup parentId studyNotes
            createSamplesGroup parentId false studySamples
        ]
        |> createCanonicalObjectNode
            parentId
            study.Identifier
            ArcExplorerNodeKind.Study
            previewPath
            studyIsLfs

    let createAssayNode arcPath lfsByPath notes (assay: ArcAssay) =
        let parentId = $"assay:{assay.Identifier}"
        let previewPath = assayPreviewPath arcPath assay.Identifier
        let dataMapPath = objectDataMapPath arcPath "assays" assay.Identifier
        let assayNotes = tryFindNotesForTarget (Assay assay.Identifier) notes
        let assaySamples = extractSampleNames assay.Tables
        let assayIsLfs = tryGetLfsByPath lfsByPath (Some previewPath)

        [
            createTablesGroup parentId previewPath assayIsLfs assay.Tables
            createOptionalDataMapNode lfsByPath parentId dataMapPath assay.DataMap.IsSome
            createNotesGroup parentId assayNotes
            createSamplesGroup parentId true assaySamples
        ]
        |> createCanonicalObjectNode
            parentId
            assay.Identifier
            ArcExplorerNodeKind.Assay
            previewPath
            assayIsLfs

    let createWorkflowNode arcPath lfsByPath notes (workflow: ArcWorkflow) =
        let parentId = $"workflow:{workflow.Identifier}"
        let previewPath = workflowPreviewPath arcPath workflow.Identifier
        let dataMapPath = objectDataMapPath arcPath "workflows" workflow.Identifier
        let workflowNotes = tryFindNotesForTarget (Workflow workflow.Identifier) notes
        let workflowIsLfs = tryGetLfsByPath lfsByPath (Some previewPath)

        [
            createWorkflowRelationshipsGroup arcPath lfsByPath parentId workflow
            createOptionalDataMapNode lfsByPath parentId dataMapPath workflow.DataMap.IsSome
            createNotesGroup parentId workflowNotes
        ]
        |> createCanonicalObjectNode
            parentId
            workflow.Identifier
            ArcExplorerNodeKind.Workflow
            previewPath
            workflowIsLfs

    let createRunNode arcPath lfsByPath notes (run: ArcRun) =
        let parentId = $"run:{run.Identifier}"
        let previewPath = runPreviewPath arcPath run.Identifier
        let dataMapPath = objectDataMapPath arcPath "runs" run.Identifier
        let runNotes = tryFindNotesForTarget (Run run.Identifier) notes
        let runSamples = extractSampleNames run.Tables
        let runIsLfs = tryGetLfsByPath lfsByPath (Some previewPath)

        [
            createRunRelationshipsGroup arcPath lfsByPath parentId run
            createTablesGroup parentId previewPath runIsLfs run.Tables
            createOptionalDataMapNode lfsByPath parentId dataMapPath run.DataMap.IsSome
            createNotesGroup parentId runNotes
            createSamplesGroup parentId true runSamples
        ]
        |> createCanonicalObjectNode
            parentId
            run.Identifier
            ArcExplorerNodeKind.Run
            previewPath
            runIsLfs

    let createTopLevelNotesGroup (notes: NoteEntry list) =
        let children =
            notes
            |> createNoteNodes (fun note -> $"note:{note.RelativePath.ToLowerInvariant()}") false

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
            studyBuckets |> sortByName |> createGroupIfAny "group:samples:studies" "Studies"
            runBuckets |> sortByName |> createGroupIfAny "group:samples:runs" "Runs"
        ]
        |> List.choose id
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
