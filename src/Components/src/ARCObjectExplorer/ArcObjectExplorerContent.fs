namespace Swate.Components

open System
open Feliz
open ARCtrl
open Swate.Components.Shared


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

    [<ReactComponent>]
    static member private TextValue (value: string) =
        Html.span [
            prop.className "swt:text-sm swt:whitespace-pre-wrap swt:break-words"
            prop.text value
        ]

    [<ReactComponent>]
    static member private CodeValue (value: string) =
        Html.code [
            prop.className "swt:text-xs swt:font-mono swt:break-all"
            prop.text value
        ]

    static member private TextRow label value = label, ArcObjectExplorerContent.TextValue value

    static member private CodeRow label value = label, ArcObjectExplorerContent.CodeValue value

    static member private OptionalTextRow label value =
        value |> Option.map (fun text -> ArcObjectExplorerContent.TextRow label text)

    static member private DataMapSummaryRows (dataMap: DataMap) =
        let headers =
            seq {
                for index in 0 .. dataMap.ColumnCount - 1 do
                    yield dataMap.GetHeader(index).ToString()
            }
            |> ArcObjectExplorerContent.summariseStrings

        [
            ArcObjectExplorerContent.TextRow "Data Contexts" (string dataMap.DataContexts.Count)
            ArcObjectExplorerContent.TextRow "Columns" (string dataMap.ColumnCount)
            yield! headers |> Option.map (fun value -> ArcObjectExplorerContent.TextRow "Headers" value) |> Option.toList
        ]

    static member private TableSummaryRows (table: ArcTable) =
        let headers = table.Headers |> Seq.map _.ToString() |> ArcObjectExplorerContent.summariseStrings

        [
            ArcObjectExplorerContent.TextRow "Name" table.Name
            ArcObjectExplorerContent.TextRow "Rows" (string table.RowCount)
            ArcObjectExplorerContent.TextRow "Columns" (string table.ColumnCount)
            yield! headers |> Option.map (fun value -> ArcObjectExplorerContent.TextRow "Headers" value) |> Option.toList
        ]

    static member private MetadataRows (arcFile: ArcFiles) =
        match arcFile with
        | ArcFiles.Investigation investigation ->
            [
                ArcObjectExplorerContent.TextRow "Identifier" investigation.Identifier
                yield! ArcObjectExplorerContent.OptionalTextRow "Title" (ArcObjectExplorerContent.asOptionalText investigation.Title) |> Option.toList
                yield! ArcObjectExplorerContent.OptionalTextRow "Description" (ArcObjectExplorerContent.asOptionalText investigation.Description) |> Option.toList
                yield! ArcObjectExplorerContent.OptionalTextRow "Submission Date" (ArcObjectExplorerContent.asOptionalText investigation.SubmissionDate) |> Option.toList
                yield! ArcObjectExplorerContent.OptionalTextRow "Public Release" (ArcObjectExplorerContent.asOptionalText investigation.PublicReleaseDate) |> Option.toList
                yield!
                    ArcObjectExplorerContent.summariseStrings (investigation.Contacts |> Seq.map ArcObjectExplorerContent.personDisplayName)
                    |> Option.map (fun value -> ArcObjectExplorerContent.TextRow "Contacts" value)
                    |> Option.toList
                yield! Some(ArcObjectExplorerContent.TextRow "Publications" (string investigation.Publications.Count)) |> Option.toList
                yield! Some(ArcObjectExplorerContent.TextRow "Ontology Sources" (string investigation.OntologySourceReferences.Count)) |> Option.toList
                yield! Some(ArcObjectExplorerContent.TextRow "Comments" (string investigation.Comments.Count)) |> Option.toList
            ]
        | ArcFiles.Study(study, _) ->
            [
                ArcObjectExplorerContent.TextRow "Identifier" study.Identifier
                yield! ArcObjectExplorerContent.OptionalTextRow "Title" (ArcObjectExplorerContent.asOptionalText study.Title) |> Option.toList
                yield! ArcObjectExplorerContent.OptionalTextRow "Description" (ArcObjectExplorerContent.asOptionalText study.Description) |> Option.toList
                yield! Some(ArcObjectExplorerContent.TextRow "Tables" (string study.TableCount)) |> Option.toList
                yield! ArcObjectExplorerContent.OptionalTextRow "Submission Date" (ArcObjectExplorerContent.asOptionalText study.SubmissionDate) |> Option.toList
                yield! ArcObjectExplorerContent.OptionalTextRow "Public Release" (ArcObjectExplorerContent.asOptionalText study.PublicReleaseDate) |> Option.toList
                yield!
                    ArcObjectExplorerContent.summariseStrings (study.StudyDesignDescriptors |> Seq.map _.NameText)
                    |> Option.map (fun value -> ArcObjectExplorerContent.TextRow "Design" value)
                    |> Option.toList
                yield!
                    ArcObjectExplorerContent.summariseStrings (study.Contacts |> Seq.map ArcObjectExplorerContent.personDisplayName)
                    |> Option.map (fun value -> ArcObjectExplorerContent.TextRow "Contacts" value)
                    |> Option.toList
                yield! Some(ArcObjectExplorerContent.TextRow "Publications" (string study.Publications.Count)) |> Option.toList
                yield! Some(ArcObjectExplorerContent.TextRow "Comments" (string study.Comments.Count)) |> Option.toList
            ]
        | ArcFiles.Assay assay ->
            [
                ArcObjectExplorerContent.TextRow "Identifier" assay.Identifier
                yield! ArcObjectExplorerContent.OptionalTextRow "Title" (ArcObjectExplorerContent.asOptionalText assay.Title) |> Option.toList
                yield! ArcObjectExplorerContent.OptionalTextRow "Description" (ArcObjectExplorerContent.asOptionalText assay.Description) |> Option.toList
                yield! Some(ArcObjectExplorerContent.TextRow "Tables" (string assay.TableCount)) |> Option.toList
                yield! ArcObjectExplorerContent.ontologyLabel assay.MeasurementType |> Option.map (fun value -> ArcObjectExplorerContent.TextRow "Measurement" value) |> Option.toList
                yield! ArcObjectExplorerContent.ontologyLabel assay.TechnologyType |> Option.map (fun value -> ArcObjectExplorerContent.TextRow "Technology" value) |> Option.toList
                yield! ArcObjectExplorerContent.ontologyLabel assay.TechnologyPlatform |> Option.map (fun value -> ArcObjectExplorerContent.TextRow "Platform" value) |> Option.toList
                yield!
                    ArcObjectExplorerContent.summariseStrings (assay.Performers |> Seq.map ArcObjectExplorerContent.personDisplayName)
                    |> Option.map (fun value -> ArcObjectExplorerContent.TextRow "Performers" value)
                    |> Option.toList
                yield! Some(ArcObjectExplorerContent.TextRow "Comments" (string assay.Comments.Count)) |> Option.toList
            ]
        | ArcFiles.Workflow workflow ->
            [
                ArcObjectExplorerContent.TextRow "Identifier" workflow.Identifier
                yield! ArcObjectExplorerContent.OptionalTextRow "Title" (ArcObjectExplorerContent.asOptionalText workflow.Title) |> Option.toList
                yield! ArcObjectExplorerContent.OptionalTextRow "Description" (ArcObjectExplorerContent.asOptionalText workflow.Description) |> Option.toList
                yield! ArcObjectExplorerContent.OptionalTextRow "Version" (ArcObjectExplorerContent.asOptionalText workflow.Version) |> Option.toList
                yield! ArcObjectExplorerContent.ontologyLabel workflow.WorkflowType |> Option.map (fun value -> ArcObjectExplorerContent.TextRow "Type" value) |> Option.toList
                yield! ArcObjectExplorerContent.OptionalTextRow "URI" (ArcObjectExplorerContent.asOptionalText workflow.URI) |> Option.toList
                yield!
                    ArcObjectExplorerContent.summariseStrings workflow.SubWorkflowIdentifiers
                    |> Option.map (fun value -> ArcObjectExplorerContent.TextRow "Subworkflows" value)
                    |> Option.toList
                yield!
                    ArcObjectExplorerContent.summariseStrings (workflow.Contacts |> Seq.map ArcObjectExplorerContent.personDisplayName)
                    |> Option.map (fun value -> ArcObjectExplorerContent.TextRow "Contacts" value)
                    |> Option.toList
                yield! Some(ArcObjectExplorerContent.TextRow "Comments" (string workflow.Comments.Count)) |> Option.toList
            ]
        | ArcFiles.Run run ->
            [
                ArcObjectExplorerContent.TextRow "Identifier" run.Identifier
                yield! ArcObjectExplorerContent.OptionalTextRow "Title" (ArcObjectExplorerContent.asOptionalText run.Title) |> Option.toList
                yield! ArcObjectExplorerContent.OptionalTextRow "Description" (ArcObjectExplorerContent.asOptionalText run.Description) |> Option.toList
                yield! Some(ArcObjectExplorerContent.TextRow "Tables" (string run.TableCount)) |> Option.toList
                yield! ArcObjectExplorerContent.ontologyLabel run.MeasurementType |> Option.map (fun value -> ArcObjectExplorerContent.TextRow "Measurement" value) |> Option.toList
                yield! ArcObjectExplorerContent.ontologyLabel run.TechnologyType |> Option.map (fun value -> ArcObjectExplorerContent.TextRow "Technology" value) |> Option.toList
                yield! ArcObjectExplorerContent.ontologyLabel run.TechnologyPlatform |> Option.map (fun value -> ArcObjectExplorerContent.TextRow "Platform" value) |> Option.toList
                yield!
                    ArcObjectExplorerContent.summariseStrings run.WorkflowIdentifiers
                    |> Option.map (fun value -> ArcObjectExplorerContent.TextRow "Workflows" value)
                    |> Option.toList
                yield!
                    ArcObjectExplorerContent.summariseStrings (run.Performers |> Seq.map ArcObjectExplorerContent.personDisplayName)
                    |> Option.map (fun value -> ArcObjectExplorerContent.TextRow "Performers" value)
                    |> Option.toList
                yield! Some(ArcObjectExplorerContent.TextRow "Comments" (string run.Comments.Count)) |> Option.toList
            ]
        | ArcFiles.DataMap(_, dataMap) -> ArcObjectExplorerContent.DataMapSummaryRows dataMap
        | ArcFiles.Template template ->
            [
                ArcObjectExplorerContent.TextRow "Name" template.Name
                yield! ArcObjectExplorerContent.OptionalTextRow "Description" (ArcObjectExplorerContent.asOptionalText (Some template.Description)) |> Option.toList
                yield! ArcObjectExplorerContent.OptionalTextRow "Version" (ArcObjectExplorerContent.asOptionalText (Some template.Version)) |> Option.toList
                yield! ArcObjectExplorerContent.OptionalTextRow "Last Updated" (Some(template.LastUpdated.ToString("yyyy-MM-dd HH:mm"))) |> Option.toList
                yield! Some(ArcObjectExplorerContent.TextRow "Organisation" (template.Organisation.ToString())) |> Option.toList
                yield!
                    ArcObjectExplorerContent.summariseStrings (template.Tags |> Seq.map _.NameText)
                    |> Option.map (fun value -> ArcObjectExplorerContent.TextRow "Tags" value)
                    |> Option.toList
                yield!
                    ArcObjectExplorerContent.summariseStrings (template.Authors |> Seq.map ArcObjectExplorerContent.personDisplayName)
                    |> Option.map (fun value -> ArcObjectExplorerContent.TextRow "Authors" value)
                    |> Option.toList
            ]

    static member private CurrentPreviewRowsForNode (selectedNode: ArcExplorerNode) (arcFile: ArcFiles) =
        match selectedNode.kind with
        | ArcExplorerNodeKind.Table ->
            match selectedNode.previewTarget with
            | ArcExplorerNodePreviewTarget.Table tableIndex when tableIndex >= 0 && tableIndex < arcFile.Tables().Count ->
                arcFile.Tables().[tableIndex] |> ArcObjectExplorerContent.TableSummaryRows |> Some
            | _ -> None
        | ArcExplorerNodeKind.DataMap -> arcFile.TryGetDataMap() |> Option.map ArcObjectExplorerContent.DataMapSummaryRows
        | _ when arcFile.Tables().Count > 0 ->
            let tableNames = arcFile.Tables() |> Seq.map _.Name |> ArcObjectExplorerContent.summariseStrings

            Some [
                ArcObjectExplorerContent.TextRow "Tables" (string (arcFile.Tables().Count))
                yield! tableNames |> Option.map (fun value -> ArcObjectExplorerContent.TextRow "Names" value) |> Option.toList
            ]
        | _ -> arcFile.TryGetDataMap() |> Option.map ArcObjectExplorerContent.DataMapSummaryRows

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
            ArcObjectExplorerContent.TextRow "Kind" (ArcObjectExplorerView.nodeKindLabel selectedNode.kind)
            ArcObjectExplorerContent.TextRow "Role" (if selectedNode.isReference then "Reference" else "Canonical")
            if selectedNode.path.IsSome then
                ArcObjectExplorerContent.CodeRow "Path" selectedNode.path.Value
            else
                ArcObjectExplorerContent.TextRow "Path" "Virtual"
        ]

        ArcObjectExplorerContent.ARCObjectSection(
            "Selection",
            [
                Html.h4 [ prop.className "swt:text-base swt:font-semibold swt:break-words"; prop.text selectedNode.name ]
                ArcObjectExplorerContent.ARCObjectPropertyTable rows
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
                    ArcFileMetadata.StudyMetadata(study, fun updated -> setArcFile (ArcFiles.Study(updated, assays)))
                | ArcFiles.Assay assay ->
                    ArcFileMetadata.AssayMetadata(assay, fun updated -> setArcFile (ArcFiles.Assay updated))
                | ArcFiles.Workflow workflow ->
                    ArcFileMetadata.WorkflowMetadata(workflow, fun updated -> setArcFile (ArcFiles.Workflow updated))
                | ArcFiles.Run run ->
                    ArcFileMetadata.RunMetadata(run, fun updated -> setArcFile (ArcFiles.Run updated))
                | _ -> Html.none
            ]
        ]

    static member private arcFileMatchesSelectedNodePreviewPath
        (selectedNode: ArcExplorerNode)
        (arcFile: ArcFiles)
        =
        match selectedNode.path, arcFile.TryGetRelativePath() with
        | Some nodePath, Some arcFilePath ->
            let expectedPreviewPath = PathHelpers.resolveArcPreviewPath nodePath
            PathHelpers.pathsEqual expectedPreviewPath arcFilePath
        | _ -> false

    [<ReactComponent>]
    static member ARCObjectDetailsContent
        (selectedNode: ArcExplorerNode option)
        (selectedAncestors: ArcExplorerNode list)
        (previewState: PageState option)
        (arcFileState: ArcFiles option)
        (setArcFileState: ArcFiles option -> unit)
        (useDetailedMetadataForms: bool)
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
            let previewArcFile =
                arcFileState
                |> Option.filter (ArcObjectExplorerContent.arcFileMatchesSelectedNodePreviewPath selectedNode)

            let metadataArcFile =
                previewArcFile
                |> Option.filter (ArcObjectExplorerContent.arcFileMatchesMetadataNodeKind selectedNode.kind)

            let selectedObjectRows =
                match metadataArcFile with
                | Some _ -> None
                | None -> previewArcFile |> Option.bind (ArcObjectExplorerContent.CurrentPreviewRowsForNode selectedNode)

            let parentMetadataContext =
                match metadataArcFile, previewArcFile with
                | Some _, _ -> None
                | None, Some arcFile ->
                    selectedAncestors
                    |> List.rev
                    |> List.tryFind (fun ancestor -> ArcObjectExplorerContent.arcFileMatchesMetadataNodeKind ancestor.kind arcFile)
                    |> Option.map (fun parentNode -> parentNode, arcFile)
                | None, None -> None

            Html.div [
                prop.className "swt:flex swt:flex-col swt:gap-3 swt:h-full"
                prop.children [
                    ArcObjectExplorerContent.ARCObjectSelectionSection(selectedNode)
                    match previewState with
                    | Some(PageState.TextPage content) -> ArcObjectExplorerContent.ARCObjectNoteContentSection(content)
                    | Some(PageState.ErrorPage message) -> ArcObjectExplorerContent.ARCObjectErrorSection("Note Content", message)
                    | _ -> Html.none
                    match selectedObjectRows with
                    | Some rows ->
                        ArcObjectExplorerContent.ARCObjectSection(
                            "Current Object",
                            [ ArcObjectExplorerContent.ARCObjectPropertyTable rows ]
                        )
                    | None -> Html.none
                    match metadataArcFile with
                    | Some arcFile
                        when useDetailedMetadataForms
                                && ArcObjectExplorerContent.usesDetailedMetadataForm selectedNode.kind ->
                        ArcObjectExplorerContent.ARCObjectDetailedMetadataContent arcFile setArcFileState
                    | Some arcFile ->
                        ArcObjectExplorerContent.ARCObjectSection(
                            "Metadata",
                            [ ArcObjectExplorerContent.ARCObjectPropertyTable (ArcObjectExplorerContent.MetadataRows arcFile) ]
                        )
                    | None ->
                        match parentMetadataContext with
                        | Some(parentNode, arcFile) ->
                            let parentSubtitle =
                                Html.div [
                                    prop.className "swt:flex swt:flex-col swt:gap-1"
                                    prop.children [
                                        Html.span [
                                            prop.className "swt:text-xs swt:uppercase swt:tracking-wide swt:opacity-60"
                                            prop.text (ArcObjectExplorerView.nodeKindLabel parentNode.kind)
                                        ]
                                        Html.h4 [
                                            prop.className "swt:text-base swt:font-semibold swt:break-words"
                                            prop.text parentNode.name
                                        ]
                                    ]
                                ]

                            ArcObjectExplorerContent.ARCObjectSection(
                                "Parent Metadata",
                                [
                                    parentSubtitle
                                    ArcObjectExplorerContent.ARCObjectPropertyTable (ArcObjectExplorerContent.MetadataRows arcFile)
                                ]
                            )
                        | None -> Html.none
                ]
            ]
