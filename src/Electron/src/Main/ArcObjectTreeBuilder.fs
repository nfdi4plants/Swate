[<RequireQualifiedAccess>]
module Main.ArcObjectTreeBuilder

open System
open System.Collections.Generic
open ARCtrl
open ARCtrl.Process.Conversion
open Swate.Components.Shared
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

type private SampleAccumulator = {
    DisplayName: string
    LookupKey: string
    Characteristics: Set<string>
    Factors: Set<string>
    DerivesFrom: Set<string>
    SourceTables: Set<string>
    Studies: Set<string>
    Assays: Set<string>
}

type private TableSampleProjection = {
    Index: int
    Name: string
    Samples: Map<string, SampleAccumulator>
}

let private sortedByName (nodes: ArcExplorerNode list) =
    nodes |> List.sortBy (fun node -> node.name.ToLowerInvariant())

let private sortLinksByName (links: ArcExplorerNodeLink list) =
    links |> List.sortBy (fun link -> link.name.ToLowerInvariant())

let private normalizeSampleName (name: string) =
    name.Trim()

let private tryNormalizeSampleName (name: string) =
    let normalized = normalizeSampleName name

    if String.IsNullOrWhiteSpace normalized then
        None
    else
        Some normalized

let private sampleLookupKey (name: string) =
    normalizeSampleName name

let private sampleNodeId parentId isReference lookupKey =
    if isReference then
        $"{parentId}:sample-ref:{lookupKey}"
    else
        $"{parentId}:sample:{lookupKey}"

let private addDistinctTexts (existing: Set<string>) (values: seq<string>) =
    values
    |> Seq.fold (fun state value ->
        match tryNormalizeSampleName value with
        | Some normalized -> Set.add normalized state
        | None -> state) existing

let private createSampleAccumulator displayName lookupKey =
    {
        DisplayName = displayName
        LookupKey = lookupKey
        Characteristics = Set.empty
        Factors = Set.empty
        DerivesFrom = Set.empty
        SourceTables = Set.empty
        Studies = Set.empty
        Assays = Set.empty
    }

let private mergeSampleAccumulator (left: SampleAccumulator) (right: SampleAccumulator) =
    {
        DisplayName =
            if String.IsNullOrWhiteSpace left.DisplayName then
                right.DisplayName
            else
                left.DisplayName
        LookupKey = left.LookupKey
        Characteristics = Set.union left.Characteristics right.Characteristics
        Factors = Set.union left.Factors right.Factors
        DerivesFrom = Set.union left.DerivesFrom right.DerivesFrom
        SourceTables = Set.union left.SourceTables right.SourceTables
        Studies = Set.union left.Studies right.Studies
        Assays = Set.union left.Assays right.Assays
    }

let private mergeSampleMaps (sampleMaps: seq<Map<string, SampleAccumulator>>) =
    sampleMaps
    |> Seq.collect Map.toList
    |> Seq.fold (fun state (lookupKey, sample) ->
        let merged =
            match state |> Map.tryFind lookupKey with
            | Some existing -> mergeSampleAccumulator existing sample
            | None -> sample

        state |> Map.add lookupKey merged) Map.empty

let private sampleSummaryOfAccumulator (sample: SampleAccumulator) : ArcExplorerSampleSummary =
    let toSortedList (values: Set<string>) =
        values |> Set.toList |> List.sortBy (fun value -> value.ToLowerInvariant())

    {
        Characteristics = toSortedList sample.Characteristics
        Factors = toSortedList sample.Factors
        DerivesFrom = toSortedList sample.DerivesFrom
        SourceTables = toSortedList sample.SourceTables
        Studies = toSortedList sample.Studies
        Assays = toSortedList sample.Assays
    }

let private factorDisplayName (factorValue: ARCtrl.Process.FactorValue) =
    factorValue.Category
    |> Option.bind (fun factor -> factor.FactorType |> Option.map (fun factorType -> factorType.NameText))
    |> Option.defaultValue factorValue.NameText

let private extractSamplesFromTable
    (tableName: string)
    (studyIdentifiers: seq<string>)
    (assayIdentifiers: seq<string>)
    (table: ArcTable)
    =
    let processes = table.GetProcesses()

    processes
    |> ARCtrl.Process.ProcessSequence.getSamples
    |> List.fold (fun state sample ->
        match tryNormalizeSampleName sample.NameAsString with
        | None -> state
        | Some displayName ->
            let lookupKey = sampleLookupKey displayName

            let existing =
                state
                |> Map.tryFind lookupKey
                |> Option.defaultValue (createSampleAccumulator displayName lookupKey)

            let updated =
                {
                    existing with
                        Characteristics =
                            addDistinctTexts
                                existing.Characteristics
                                (sample.Characteristics |> Option.defaultValue [] |> Seq.map _.NameText)
                        Factors =
                            addDistinctTexts
                                existing.Factors
                                (sample.FactorValues |> Option.defaultValue [] |> Seq.map factorDisplayName)
                        DerivesFrom =
                            addDistinctTexts
                                existing.DerivesFrom
                                (sample.DerivesFrom |> Option.defaultValue [] |> Seq.map _.NameAsString)
                        SourceTables = addDistinctTexts existing.SourceTables [ tableName ]
                        Studies = addDistinctTexts existing.Studies studyIdentifiers
                        Assays = addDistinctTexts existing.Assays assayIdentifiers
                }

            state |> Map.add lookupKey updated) Map.empty

let private extractTableSampleProjections
    (studyIdentifiers: seq<string>)
    (assayIdentifiers: seq<string>)
    (tables: ResizeArray<ArcTable>)
    =
    tables
    |> Seq.mapi (fun index table ->
        let tableName = fallbackTableName index table

        {
            Index = index
            Name = tableName
            Samples = extractSamplesFromTable tableName studyIdentifiers assayIdentifiers table
        })
    |> List.ofSeq

let private createSampleNodes
    parentId
    parentPath
    isReference
    isLfs
    (samples: Map<string, SampleAccumulator>)
    =
    samples
    |> Map.toList
    |> List.map (fun (_, sample) ->
        ArcExplorerNode.create (
            sampleNodeId parentId isReference sample.LookupKey,
            sample.DisplayName,
            ArcExplorerNodeKind.Sample,
            path = Some parentPath,
            isReference = isReference,
            sampleSummary = Some(sampleSummaryOfAccumulator sample),
            isLfs = isLfs
        ))
    |> sortedByName

let private createRelatedSampleLinks parentId parentPath isReference (samples: Map<string, SampleAccumulator>) =
    let subtitle =
        if isReference then
            Some "Reference sample"
        else
            Some "Canonical sample"

    samples
    |> Map.toList
    |> List.map (fun (_, sample) ->
        {
            targetId = sampleNodeId parentId isReference sample.LookupKey
            name = sample.DisplayName
            kind = ArcExplorerNodeKind.Sample
            subtitle = subtitle
            path = Some parentPath
        })
    |> sortLinksByName

let private createTableNodes parentId parentPath isReference isLfs (tableProjections: TableSampleProjection list) =
    tableProjections
    |> List.map (fun projection ->
        let tableNodeId = $"{parentId}:table:{projection.Index}"

        ArcExplorerNode.create (
            tableNodeId,
            projection.Name,
            ArcExplorerNodeKind.Table,
            path = Some parentPath,
            previewTarget = ArcExplorerNodeViewTarget.Table projection.Index,
            relatedSamples = createRelatedSampleLinks tableNodeId parentPath isReference projection.Samples,
            children =
                createSampleNodes
                    tableNodeId
                    parentPath
                    isReference
                    isLfs
                    projection.Samples,
            isLfs = isLfs
        ))

let private createStudyNode
    (entries: Dictionary<string, FileEntry>)
    (tableProjections: TableSampleProjection list)
    (study: ArcStudy)
    =
    let studyPath = ARCtrl.Helper.Identifier.Study.fileNameFromIdentifier study.Identifier
    let nodeId = $"study:{study.Identifier}"
    let studyIsLfs = tryGetIsLfs entries studyPath
    let children =
        createDataMapChild nodeId studyPath entries study.DataMap.IsSome
        @ createTableNodes nodeId studyPath false studyIsLfs tableProjections

    ArcExplorerNode.create (
        nodeId,
        study.Identifier,
        ArcExplorerNodeKind.Study,
        path = Some studyPath,
        isLfs = studyIsLfs,
        children = children
    )

let private createAssayNode
    (entries: Dictionary<string, FileEntry>)
    (tableProjections: TableSampleProjection list)
    (assay: ArcAssay)
    =
    let assayPath = ARCtrl.Helper.Identifier.Assay.fileNameFromIdentifier assay.Identifier
    let nodeId = $"assay:{assay.Identifier}"
    let assayIsLfs = tryGetIsLfs entries assayPath
    let children =
        createDataMapChild nodeId assayPath entries assay.DataMap.IsSome
        @ createTableNodes nodeId assayPath true assayIsLfs tableProjections

    ArcExplorerNode.create (
        nodeId,
        assay.Identifier,
        ArcExplorerNodeKind.Assay,
        path = Some assayPath,
        isLfs = assayIsLfs,
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
    let runIsLfs = tryGetIsLfs entries runPath
    let tableProjections = extractTableSampleProjections [] [] run.Tables
    let children =
        createDataMapChild nodeId runPath entries run.DataMap.IsSome
        @ createTableNodes nodeId runPath false runIsLfs tableProjections

    ArcExplorerNode.create (
        nodeId,
        run.Identifier,
        ArcExplorerNodeKind.Run,
        path = Some runPath,
        isLfs = runIsLfs,
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

let create (arcRootPath: string) (arc: ARC) (fileEntries: seq<FileEntry>) =
    let rendererEntries = toRendererFileTree arcRootPath fileEntries
    let investigationPath = ARCtrl.ArcPathHelper.InvestigationFileName
    let studyTableSamplesByIdentifier =
        arc.Studies
        |> Seq.map (fun study ->
            study.Identifier, extractTableSampleProjections [ study.Identifier ] [] study.Tables)
        |> Map.ofSeq

    let assayTableSamplesByIdentifier =
        arc.Assays
        |> Seq.map (fun assay ->
            let owningStudies =
                arc.Studies
                |> Seq.filter (fun study -> study.RegisteredAssayIdentifiers |> Seq.contains assay.Identifier)
                |> Seq.map _.Identifier
                |> Seq.toList

            assay.Identifier, extractTableSampleProjections owningStudies [ assay.Identifier ] assay.Tables)
        |> Map.ofSeq

    let groups =
        [
            let studies =
                arc.Studies
                |> Seq.map (fun study ->
                    let tableProjections =
                        studyTableSamplesByIdentifier
                        |> Map.tryFind study.Identifier
                        |> Option.defaultValue []

                    createStudyNode rendererEntries tableProjections study)
                |> List.ofSeq
                |> sortedByName

            if List.isEmpty studies |> not then
                createGroupNode "group:studies" "Studies" studies

            let assays =
                arc.Assays
                |> Seq.map (fun assay ->
                    let tableProjections =
                        assayTableSamplesByIdentifier
                        |> Map.tryFind assay.Identifier
                        |> Option.defaultValue []

                    createAssayNode rendererEntries tableProjections assay)
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
