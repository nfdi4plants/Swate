namespace Swate.Components

open System
open Feliz
open Swate.Components.Shared
open Swate.Components.FileExplorerTypes
open ARCtrl


[<RequireQualifiedAccess>]
type ArcObjectExplorerContent =

    static member private asOptionalText (value: string option) =
        value
        |> Option.bind (fun text ->
            if String.IsNullOrWhiteSpace text then
                None
            else
                Some text)

    static member private summariseStrings (values: seq<string>) =
        values
        |> Seq.filter (fun value -> String.IsNullOrWhiteSpace value |> not)
        |> Seq.distinct
        |> Seq.toList
        |> function
            | [] -> None
            | [ single ] -> Some single
            | [ first; second ] -> Some $"{first}; {second}"
            | first :: second :: rest -> Some $"{first}; {second}; +{rest.Length} more"

    static member private personDisplayName (person: Person) =
        [ person.FirstName; person.MidInitials; person.LastName ]
        |> List.choose id
        |> List.filter (fun value -> String.IsNullOrWhiteSpace value |> not)
        |> function
            | [] -> person.ORCID |> ArcObjectExplorerContent.asOptionalText |> Option.defaultValue "Unnamed person"
            | parts -> String.concat " " parts

    static member private ontologyLabel (annotation: OntologyAnnotation option) =
        annotation |> Option.map _.NameText |> ArcObjectExplorerContent.asOptionalText

    static member private nodeKindLabel =
        function
        | ArcExplorerNodeKind.Arc -> "ARC"
        | ArcExplorerNodeKind.Group -> "Group"
        | ArcExplorerNodeKind.Study -> "Study"
        | ArcExplorerNodeKind.Assay -> "Assay"
        | ArcExplorerNodeKind.Workflow -> "Workflow"
        | ArcExplorerNodeKind.Run -> "Run"
        | ArcExplorerNodeKind.Table -> "Table"
        | ArcExplorerNodeKind.DataMap -> "DataMap"
        | ArcExplorerNodeKind.Note -> "Note"
        | ArcExplorerNodeKind.Sample -> "Sample"

    static member private nodeHasMetadata =
        function
        | ArcExplorerNodeKind.Arc
        | ArcExplorerNodeKind.Study
        | ArcExplorerNodeKind.Assay
        | ArcExplorerNodeKind.Workflow
        | ArcExplorerNodeKind.Run -> true
        | ArcExplorerNodeKind.Group
        | ArcExplorerNodeKind.Table
        | ArcExplorerNodeKind.DataMap
        | ArcExplorerNodeKind.Note
        | ArcExplorerNodeKind.Sample -> false

    static member private usesDetailedMetadataForm =
        function
        | ArcExplorerNodeKind.Study
        | ArcExplorerNodeKind.Assay
        | ArcExplorerNodeKind.Workflow
        | ArcExplorerNodeKind.Run -> true
        | _ -> false

    static member private arcFileMatchesMetadataNodeKind
        (selectedNodeKind: ArcExplorerNodeKind)
        (arcFile: ArcFiles)
        =
        match selectedNodeKind, arcFile with
        | ArcExplorerNodeKind.Arc, ArcFiles.Investigation _
        | ArcExplorerNodeKind.Study, ArcFiles.Study _
        | ArcExplorerNodeKind.Assay, ArcFiles.Assay _
        | ArcExplorerNodeKind.Workflow, ArcFiles.Workflow _
        | ArcExplorerNodeKind.Run, ArcFiles.Run _ -> true
        | _ -> false

    static member private textValue (value: string) =
        Html.span [
            prop.className "swt:text-sm swt:whitespace-pre-wrap swt:break-words"
            prop.text value
        ]

    static member private codeValue (value: string) =
        Html.code [
            prop.className "swt:text-xs swt:font-mono swt:break-all"
            prop.text value
        ]

    static member private textRow label value = label, ArcObjectExplorerContent.textValue value

    static member private codeRow label value = label, ArcObjectExplorerContent.codeValue value

    static member private optionalTextRow label value =
        value |> Option.map (fun text -> ArcObjectExplorerContent.textRow label text)

    static member private dataMapSummaryRows (dataMap: DataMap) =
        let headers =
            seq {
                for index in 0 .. dataMap.ColumnCount - 1 do
                    yield dataMap.GetHeader(index).ToString()
            }
            |> ArcObjectExplorerContent.summariseStrings

        [
            ArcObjectExplorerContent.textRow "Data Contexts" (string dataMap.DataContexts.Count)
            ArcObjectExplorerContent.textRow "Columns" (string dataMap.ColumnCount)
            yield! headers |> Option.map (fun value -> ArcObjectExplorerContent.textRow "Headers" value) |> Option.toList
        ]

    static member private tableSummaryRows (table: ArcTable) =
        let headers = table.Headers |> Seq.map _.ToString() |> ArcObjectExplorerContent.summariseStrings

        [
            ArcObjectExplorerContent.textRow "Name" table.Name
            ArcObjectExplorerContent.textRow "Rows" (string table.RowCount)
            ArcObjectExplorerContent.textRow "Columns" (string table.ColumnCount)
            yield! headers |> Option.map (fun value -> ArcObjectExplorerContent.textRow "Headers" value) |> Option.toList
        ]

    static member private metadataRows (arcFile: ArcFiles) =
        match arcFile with
        | ArcFiles.Investigation investigation ->
            [
                ArcObjectExplorerContent.textRow "Identifier" investigation.Identifier
                yield! ArcObjectExplorerContent.optionalTextRow "Title" (ArcObjectExplorerContent.asOptionalText investigation.Title) |> Option.toList
                yield! ArcObjectExplorerContent.optionalTextRow "Description" (ArcObjectExplorerContent.asOptionalText investigation.Description) |> Option.toList
                yield! ArcObjectExplorerContent.optionalTextRow "Submission Date" (ArcObjectExplorerContent.asOptionalText investigation.SubmissionDate) |> Option.toList
                yield! ArcObjectExplorerContent.optionalTextRow "Public Release" (ArcObjectExplorerContent.asOptionalText investigation.PublicReleaseDate) |> Option.toList
                yield!
                    ArcObjectExplorerContent.summariseStrings (investigation.Contacts |> Seq.map ArcObjectExplorerContent.personDisplayName)
                    |> Option.map (fun value -> ArcObjectExplorerContent.textRow "Contacts" value)
                    |> Option.toList
                yield! Some(ArcObjectExplorerContent.textRow "Publications" (string investigation.Publications.Count)) |> Option.toList
                yield! Some(ArcObjectExplorerContent.textRow "Ontology Sources" (string investigation.OntologySourceReferences.Count)) |> Option.toList
                yield! Some(ArcObjectExplorerContent.textRow "Comments" (string investigation.Comments.Count)) |> Option.toList
            ]
        | ArcFiles.Study(study, _) ->
            [
                ArcObjectExplorerContent.textRow "Identifier" study.Identifier
                yield! ArcObjectExplorerContent.optionalTextRow "Title" (ArcObjectExplorerContent.asOptionalText study.Title) |> Option.toList
                yield! ArcObjectExplorerContent.optionalTextRow "Description" (ArcObjectExplorerContent.asOptionalText study.Description) |> Option.toList
                yield! Some(ArcObjectExplorerContent.textRow "Tables" (string study.TableCount)) |> Option.toList
                yield! ArcObjectExplorerContent.optionalTextRow "Submission Date" (ArcObjectExplorerContent.asOptionalText study.SubmissionDate) |> Option.toList
                yield! ArcObjectExplorerContent.optionalTextRow "Public Release" (ArcObjectExplorerContent.asOptionalText study.PublicReleaseDate) |> Option.toList
                yield!
                    ArcObjectExplorerContent.summariseStrings (study.StudyDesignDescriptors |> Seq.map _.NameText)
                    |> Option.map (fun value -> ArcObjectExplorerContent.textRow "Design" value)
                    |> Option.toList
                yield!
                    ArcObjectExplorerContent.summariseStrings (study.Contacts |> Seq.map ArcObjectExplorerContent.personDisplayName)
                    |> Option.map (fun value -> ArcObjectExplorerContent.textRow "Contacts" value)
                    |> Option.toList
                yield! Some(ArcObjectExplorerContent.textRow "Publications" (string study.Publications.Count)) |> Option.toList
                yield! Some(ArcObjectExplorerContent.textRow "Comments" (string study.Comments.Count)) |> Option.toList
            ]
        | ArcFiles.Assay assay ->
            [
                ArcObjectExplorerContent.textRow "Identifier" assay.Identifier
                yield! ArcObjectExplorerContent.optionalTextRow "Title" (ArcObjectExplorerContent.asOptionalText assay.Title) |> Option.toList
                yield! ArcObjectExplorerContent.optionalTextRow "Description" (ArcObjectExplorerContent.asOptionalText assay.Description) |> Option.toList
                yield! Some(ArcObjectExplorerContent.textRow "Tables" (string assay.TableCount)) |> Option.toList
                yield! ArcObjectExplorerContent.ontologyLabel assay.MeasurementType |> Option.map (fun value -> ArcObjectExplorerContent.textRow "Measurement" value) |> Option.toList
                yield! ArcObjectExplorerContent.ontologyLabel assay.TechnologyType |> Option.map (fun value -> ArcObjectExplorerContent.textRow "Technology" value) |> Option.toList
                yield! ArcObjectExplorerContent.ontologyLabel assay.TechnologyPlatform |> Option.map (fun value -> ArcObjectExplorerContent.textRow "Platform" value) |> Option.toList
                yield!
                    ArcObjectExplorerContent.summariseStrings (assay.Performers |> Seq.map ArcObjectExplorerContent.personDisplayName)
                    |> Option.map (fun value -> ArcObjectExplorerContent.textRow "Performers" value)
                    |> Option.toList
                yield! Some(ArcObjectExplorerContent.textRow "Comments" (string assay.Comments.Count)) |> Option.toList
            ]
        | ArcFiles.Workflow workflow ->
            [
                ArcObjectExplorerContent.textRow "Identifier" workflow.Identifier
                yield! ArcObjectExplorerContent.optionalTextRow "Title" (ArcObjectExplorerContent.asOptionalText workflow.Title) |> Option.toList
                yield! ArcObjectExplorerContent.optionalTextRow "Description" (ArcObjectExplorerContent.asOptionalText workflow.Description) |> Option.toList
                yield! ArcObjectExplorerContent.optionalTextRow "Version" (ArcObjectExplorerContent.asOptionalText workflow.Version) |> Option.toList
                yield! ArcObjectExplorerContent.ontologyLabel workflow.WorkflowType |> Option.map (fun value -> ArcObjectExplorerContent.textRow "Type" value) |> Option.toList
                yield! ArcObjectExplorerContent.optionalTextRow "URI" (ArcObjectExplorerContent.asOptionalText workflow.URI) |> Option.toList
                yield!
                    ArcObjectExplorerContent.summariseStrings workflow.SubWorkflowIdentifiers
                    |> Option.map (fun value -> ArcObjectExplorerContent.textRow "Subworkflows" value)
                    |> Option.toList
                yield!
                    ArcObjectExplorerContent.summariseStrings (workflow.Contacts |> Seq.map ArcObjectExplorerContent.personDisplayName)
                    |> Option.map (fun value -> ArcObjectExplorerContent.textRow "Contacts" value)
                    |> Option.toList
                yield! Some(ArcObjectExplorerContent.textRow "Comments" (string workflow.Comments.Count)) |> Option.toList
            ]
        | ArcFiles.Run run ->
            [
                ArcObjectExplorerContent.textRow "Identifier" run.Identifier
                yield! ArcObjectExplorerContent.optionalTextRow "Title" (ArcObjectExplorerContent.asOptionalText run.Title) |> Option.toList
                yield! ArcObjectExplorerContent.optionalTextRow "Description" (ArcObjectExplorerContent.asOptionalText run.Description) |> Option.toList
                yield! Some(ArcObjectExplorerContent.textRow "Tables" (string run.TableCount)) |> Option.toList
                yield! ArcObjectExplorerContent.ontologyLabel run.MeasurementType |> Option.map (fun value -> ArcObjectExplorerContent.textRow "Measurement" value) |> Option.toList
                yield! ArcObjectExplorerContent.ontologyLabel run.TechnologyType |> Option.map (fun value -> ArcObjectExplorerContent.textRow "Technology" value) |> Option.toList
                yield! ArcObjectExplorerContent.ontologyLabel run.TechnologyPlatform |> Option.map (fun value -> ArcObjectExplorerContent.textRow "Platform" value) |> Option.toList
                yield!
                    ArcObjectExplorerContent.summariseStrings run.WorkflowIdentifiers
                    |> Option.map (fun value -> ArcObjectExplorerContent.textRow "Workflows" value)
                    |> Option.toList
                yield!
                    ArcObjectExplorerContent.summariseStrings (run.Performers |> Seq.map ArcObjectExplorerContent.personDisplayName)
                    |> Option.map (fun value -> ArcObjectExplorerContent.textRow "Performers" value)
                    |> Option.toList
                yield! Some(ArcObjectExplorerContent.textRow "Comments" (string run.Comments.Count)) |> Option.toList
            ]
        | ArcFiles.DataMap(_, dataMap) -> ArcObjectExplorerContent.dataMapSummaryRows dataMap
        | ArcFiles.Template template ->
            [
                ArcObjectExplorerContent.textRow "Name" template.Name
                yield! ArcObjectExplorerContent.optionalTextRow "Description" (ArcObjectExplorerContent.asOptionalText (Some template.Description)) |> Option.toList
                yield! ArcObjectExplorerContent.optionalTextRow "Version" (ArcObjectExplorerContent.asOptionalText (Some template.Version)) |> Option.toList
                yield! ArcObjectExplorerContent.optionalTextRow "Last Updated" (Some(template.LastUpdated.ToString("yyyy-MM-dd HH:mm"))) |> Option.toList
                yield! Some(ArcObjectExplorerContent.textRow "Organisation" (template.Organisation.ToString())) |> Option.toList
                yield!
                    ArcObjectExplorerContent.summariseStrings (template.Tags |> Seq.map _.NameText)
                    |> Option.map (fun value -> ArcObjectExplorerContent.textRow "Tags" value)
                    |> Option.toList
                yield!
                    ArcObjectExplorerContent.summariseStrings (template.Authors |> Seq.map ArcObjectExplorerContent.personDisplayName)
                    |> Option.map (fun value -> ArcObjectExplorerContent.textRow "Authors" value)
                    |> Option.toList
            ]

    static member private currentPreviewRowsForNode (selectedNode: ArcExplorerNode) (arcFile: ArcFiles) =
        match selectedNode.kind with
        | ArcExplorerNodeKind.Table ->
            match selectedNode.previewTarget with
            | ArcExplorerNodePreviewTarget.Table tableIndex when tableIndex >= 0 && tableIndex < arcFile.Tables().Count ->
                arcFile.Tables().[tableIndex] |> ArcObjectExplorerContent.tableSummaryRows |> Some
            | _ -> None
        | ArcExplorerNodeKind.DataMap -> WidgetArcFile.tryGetDataMap arcFile |> Option.map ArcObjectExplorerContent.dataMapSummaryRows
        | _ when arcFile.Tables().Count > 0 ->
            let tableNames = arcFile.Tables() |> Seq.map _.Name |> ArcObjectExplorerContent.summariseStrings

            Some [
                ArcObjectExplorerContent.textRow "Tables" (string (arcFile.Tables().Count))
                yield! tableNames |> Option.map (fun value -> ArcObjectExplorerContent.textRow "Names" value) |> Option.toList
            ]
        | _ -> WidgetArcFile.tryGetDataMap arcFile |> Option.map ArcObjectExplorerContent.dataMapSummaryRows

    static member private noMetadataMessage =
        function
        | ArcExplorerNodeKind.Table -> "Table nodes open a specific ARC table and expose its basic dimensions."
        | ArcExplorerNodeKind.Sample -> "Sample nodes are derived from table content and do not currently expose standalone metadata."
        | ArcExplorerNodeKind.Group -> "Select a concrete ARC object to inspect its metadata."
        | _ -> "No additional metadata is available for this selection."

    static member private filterArcExplorerTreeByKinds (visibleKinds: Set<string>) (nodes: ArcExplorerNode list) =
        let rec loop (node: ArcExplorerNode) =
            let filteredChildren = node.children |> List.choose loop
            let hasVisibleChildren = filteredChildren |> List.isEmpty |> not
            let kindLabel = ArcObjectExplorerContent.nodeKindLabel node.kind
            let isVisibleKind = visibleKinds.Contains kindLabel

            match node.kind with
            | ArcExplorerNodeKind.Arc ->
                Some { node with children = filteredChildren }
            | ArcExplorerNodeKind.Group ->
                if hasVisibleChildren then
                    Some { node with children = filteredChildren }
                else
                    None
            | _ ->
                if isVisibleKind || hasVisibleChildren then
                    Some { node with children = filteredChildren }
                else
                    None

        nodes |> List.choose loop

    static member private flattenFileItems(items: FileItem list) =
        let rec loop (items: FileItem list) =
            items
            |> List.collect (fun item ->
                item :: (item.Children |> Option.defaultValue [] |> loop))

        loop items

    static member private flattenArcExplorerNodesWithParent(nodes: ArcExplorerNode list) =
        let rec loop (parent: ArcExplorerNode option) (nodes: ArcExplorerNode list) =
            nodes
            |> List.collect (fun node -> (node, parent) :: loop (Some node) node.children)

        loop None nodes

    static member private searchableArcExplorerItems (nodes: ArcExplorerNode list) (items: FileItem list) =
        let itemsById =
            items
            |> ArcObjectExplorerContent.flattenFileItems
            |> List.map (fun item -> item.Id, item)
            |> Map.ofList

        nodes
        |> ArcObjectExplorerContent.flattenArcExplorerNodesWithParent
        |> List.choose (fun (node, parent) ->
            if
                not node.isSelectable
                || node.kind = ArcExplorerNodeKind.Arc
                || node.kind = ArcExplorerNodeKind.Group
            then
                None
            else
                itemsById
                |> Map.tryFind node.id
                |> Option.map (fun item ->
                    let parentPart =
                        parent
                        |> Option.map (fun parentNode -> $"Parent: {parentNode.name}")
                        |> Option.toList

                    let subtitleParts = [
                        ArcObjectExplorerContent.nodeKindLabel node.kind
                        if node.isReference then "Reference" else "Canonical"
                        yield! parentPart
                        yield! node.path |> Option.toList
                    ]

                    node.name, Some(String.concat " | " subtitleParts), item))
        |> List.sortBy (fun (name, _, _) -> name.ToLowerInvariant())
        |> List.toArray

    [<ReactComponent>]
    static member private ARCObjectSection(title: string, children: ReactElement list) =
        Html.div [
            prop.className "swt:rounded-lg swt:border swt:border-base-300 swt:bg-base-100 swt:p-3"
            prop.children [
                Html.h5 [ prop.className "swt:text-sm swt:font-semibold swt:mb-3"; prop.text title ]
                Html.div [
                    prop.className "swt:flex swt:flex-col swt:gap-3"
                    prop.children children
                ]
            ]
        ]

    [<ReactComponent>]
    static member private ARCObjectPropertyTable(rows: (string * ReactElement) list) =
        Html.dl [
            prop.className "swt:grid swt:grid-cols-1 swt:gap-y-3"
            prop.children [
                for label, value in rows do
                    Html.div [
                        prop.className "swt:flex swt:flex-col swt:gap-1"
                        prop.children [
                            Html.dt [
                                prop.className "swt:text-xs swt:font-semibold swt:uppercase swt:tracking-wide swt:opacity-60"
                                prop.text label
                            ]
                            Html.dd [
                                prop.className "swt:min-w-0"
                                prop.children [ value ]
                            ]
                        ]
                    ]
            ]
        ]

    [<ReactComponent>]
    static member private ARCObjectSelectionSection(selectedNode: ArcExplorerNode) =
        let rows = [
            ArcObjectExplorerContent.textRow "Kind" (ArcObjectExplorerContent.nodeKindLabel selectedNode.kind)
            ArcObjectExplorerContent.textRow "Role" (if selectedNode.isReference then "Reference" else "Canonical")
            if selectedNode.path.IsSome then
                ArcObjectExplorerContent.codeRow "Path" selectedNode.path.Value
            else
                ArcObjectExplorerContent.textRow "Path" "Virtual"
        ]

        ArcObjectExplorerContent.ARCObjectSection(
            "Selection",
            [
                Html.h4 [ prop.className "swt:text-base swt:font-semibold swt:break-words"; prop.text selectedNode.name ]
                ArcObjectExplorerContent.ARCObjectPropertyTable rows
            ]
        )

    [<ReactComponent>]
    static member private ARCObjectStatusSection(title: string, message: string) =
        ArcObjectExplorerContent.ARCObjectSection(
            title,
            [
                Html.p [
                    prop.className "swt:text-sm swt:opacity-80"
                    prop.text message
                ]
            ]
        )

    [<ReactComponent>]
    static member private ARCObjectErrorSection(title: string, message: string) =
        ArcObjectExplorerContent.ARCObjectSection(
            title,
            [
                Html.p [
                    prop.className "swt:text-sm swt:text-error"
                    prop.text message
                ]
            ]
        )

    [<ReactComponent>]
    static member private ARCObjectNoteContentSection(content: string) =
        ArcObjectExplorerContent.ARCObjectSection(
            "Note Content",
            [
                if String.IsNullOrWhiteSpace content then
                    Html.p [
                        prop.className "swt:text-sm swt:opacity-80"
                        prop.text "This note is empty."
                    ]
                else
                    Html.pre [
                        prop.className
                            "swt:min-w-0 swt:max-w-none swt:overflow-auto swt:whitespace-pre-wrap swt:break-words swt:rounded-lg swt:border swt:border-base-300 swt:bg-base-100 swt:p-4 swt:text-sm"
                        prop.text content
                    ]
            ]
        )

    [<ReactComponent>]
    static member private ARCObjectDetailedMetadataContent
        (arcFile: ArcFiles)
        (setArcFileState: ArcFiles option -> unit)
        =
        let setArcFile arcFile = setArcFileState (Some arcFile)

        Html.div [
            prop.className "swt:min-w-0"
            prop.children [
                match arcFile with
                | ArcFiles.Study(study, assays) ->
                    MetadataForms.StudyMetadata(study, fun updated -> setArcFile (ArcFiles.Study(updated, assays)))
                | ArcFiles.Assay assay ->
                    MetadataForms.AssayMetadata(assay, fun updated -> setArcFile (ArcFiles.Assay updated))
                | ArcFiles.Workflow workflow ->
                    MetadataForms.WorkflowMetadata(workflow, fun updated -> setArcFile (ArcFiles.Workflow updated))
                | ArcFiles.Run run ->
                    MetadataForms.RunMetadata(run, fun updated -> setArcFile (ArcFiles.Run updated))
                | _ -> Html.none
            ]
        ]

    [<ReactComponent>]
    static member private ARCObjectDetailsContent
        (selectedNode: ArcExplorerNode option)
        (previewState: ArcObjectPreviewState)
        (arcFileState: ArcFiles option)
        (setArcFileState: ArcFiles option -> unit)
        =
        match selectedNode with
        | None ->
            Html.div [
                prop.className
                    "swt:flex swt:flex-1 swt:items-center swt:justify-center swt:rounded-lg swt:border swt:border-dashed swt:border-base-300 swt:bg-base-200/40 swt:p-6"
                prop.children [
                    Html.p [
                        prop.className "swt:text-sm swt:text-center swt:opacity-70"
                        prop.text "Select an ARC object to inspect its details."
                    ]
                ]
            ]
        | Some selectedNode ->
            let metadataArcFile =
                arcFileState
                |> Option.filter (ArcObjectExplorerContent.arcFileMatchesMetadataNodeKind selectedNode.kind)

            let currentPreviewRows =
                arcFileState |> Option.bind (ArcObjectExplorerContent.currentPreviewRowsForNode selectedNode)

            Html.div [
                prop.className "swt:flex swt:flex-col swt:gap-3 swt:h-full"
                prop.children [
                    ArcObjectExplorerContent.ARCObjectSelectionSection(selectedNode)

                    match selectedNode.kind with
                    | ArcExplorerNodeKind.Note ->
                        match previewState with
                        | ArcObjectPreviewState.Text content -> ArcObjectExplorerContent.ARCObjectNoteContentSection(content)
                        | ArcObjectPreviewState.Error message -> ArcObjectExplorerContent.ARCObjectErrorSection("Note Content", message)
                        | ArcObjectPreviewState.NoneLoaded -> ArcObjectExplorerContent.ARCObjectStatusSection("Note Content", "Loading note content...")
                    | kind when kind = ArcExplorerNodeKind.DataMap ->
                        match previewState with
                        | ArcObjectPreviewState.Error message -> ArcObjectExplorerContent.ARCObjectErrorSection("Current Preview", message)
                        | _ ->
                            match currentPreviewRows with
                            | Some rows -> ArcObjectExplorerContent.ARCObjectSection("Current Preview", [ ArcObjectExplorerContent.ARCObjectPropertyTable rows ])
                            | None -> ArcObjectExplorerContent.ARCObjectStatusSection("Current Preview", "No DataMap preview is loaded.")
                    | kind when ArcObjectExplorerContent.nodeHasMetadata kind ->
                        match previewState with
                        | ArcObjectPreviewState.Error message -> ArcObjectExplorerContent.ARCObjectErrorSection("Metadata", message)
                        | _ ->
                            match metadataArcFile with
                            | Some arcFile when ArcObjectExplorerContent.usesDetailedMetadataForm selectedNode.kind ->
                                ArcObjectExplorerContent.ARCObjectDetailedMetadataContent arcFile setArcFileState
                            | Some arcFile ->
                                ArcObjectExplorerContent.ARCObjectSection("Metadata", [ ArcObjectExplorerContent.ARCObjectPropertyTable (ArcObjectExplorerContent.metadataRows arcFile) ])
                            | None -> ArcObjectExplorerContent.ARCObjectStatusSection("Metadata", "Loading metadata...")
                    | _ ->
                        match currentPreviewRows with
                        | Some rows -> ArcObjectExplorerContent.ARCObjectSection("Current Preview", [ ArcObjectExplorerContent.ARCObjectPropertyTable rows ])
                        | None -> ArcObjectExplorerContent.ARCObjectStatusSection("Current Preview", ArcObjectExplorerContent.noMetadataMessage selectedNode.kind)
                ]
            ]

    [<ReactComponent>]
    static member Main (props: ArcObjectExplorerProps) =
        let selectedKindIndices, setSelectedKindIndices =
            React.useState (Swate.Components.ARCObjectWidget.DefaultKindFilterIndices())

        let visibleKinds =
            Swate.Components.ARCObjectWidget.SelectedKindLabels(selectedKindIndices)

        let filteredExplorerTree =
            ArcObjectExplorerContent.filterArcExplorerTreeByKinds visibleKinds props.nodes

        let treePane =
            Swate.Components.ARCExplorer.CreateArcExplorer
                props.rootRepoPath
                filteredExplorerTree
                props.selectedExplorerItemId
                props.selectedTreeItemPath
                props.setSelectedExplorerItemId
                props.setSelectedTreeItemPath
                props.services

        let explorerItems =
            Swate.Components.ARCExplorer.toFileItems filteredExplorerTree

        let searchItems =
            ArcObjectExplorerContent.searchableArcExplorerItems filteredExplorerTree explorerItems

        let selectedItemId =
            Swate.Components.ARCExplorer.getSelectedItemId
                filteredExplorerTree
                props.selectedExplorerItemId
                props.selectedTreeItemPath

        let selectedNode =
            selectedItemId
            |> Option.bind (fun nodeId ->
                Swate.Components.ARCExplorer.tryFindNodeById nodeId filteredExplorerTree)

        let handleExplorerSelection =
            Swate.Components.ARCExplorer.createOpenPreviewHandler
                props.setSelectedExplorerItemId
                props.setSelectedTreeItemPath
                props.services

        let selectedTitle =
            selectedNode
            |> Option.map _.name
            |> Option.defaultValue "No visible selection"

        let selectedSubtitle =
            selectedNode
            |> Option.map (fun node ->
                let role = if node.isReference then "Reference" else "Canonical"
                $"{ArcObjectExplorerContent.nodeKindLabel node.kind} | {role}")
            |> Option.defaultValue "Selection"

        let searchAction =
            Swate.Components.ARCObjectWidget.SearchAction(
                searchItems,
                (fun (name, _, _) -> name),
                (fun (_, _, item) -> promise { handleExplorerSelection item |> Promise.start} |> Promise.start),
                itemSubtitle = (fun (_, subtitle, _) -> subtitle)
            )

        let navbar =
            Swate.Components.ARCObjectWidget.Navbar(
                selectedTitle,
                selectedSubtitle,
                selectedKindIndices,
                setSelectedKindIndices,
                rightActions = searchAction
            )

        let explorerPane =
            Swate.Components.ARCObjectWidget.ExplorerContent(
                explorerItems,
                ?selectedItemId = selectedItemId,
                onItemClick = (fun item -> promise { handleExplorerSelection item |> Promise.start} |> Promise.start)
            )

        let detailsPane =
            ArcObjectExplorerContent.ARCObjectDetailsContent selectedNode props.previewState props.arcFileState props.setArcFileState

        Swate.Components.ARCObjectWidget.Main(
            navbar = navbar,
            treePane = treePane,
            explorerPane = explorerPane,
            detailsPane = detailsPane
        )
