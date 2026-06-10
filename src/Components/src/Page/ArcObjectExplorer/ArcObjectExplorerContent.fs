namespace Swate.Components.Page.ARCObjectExplorer

open System
open Fable.Core
open Feliz
open ARCtrl
open Swate.Components
open Swate.Components.Shared
open Swate.Components.Page.ARCObjectExplorer.Model
open Swate.Components.Page.Metadata

type private ArcObjectPropertyValue =
    | Text of string
    | Code of string

type private ArcObjectPropertyRow = {
    Label: string
    Value: ArcObjectPropertyValue
}

module private ArcObjectExplorerContentHelper =

    let asOptionalText (value: string option) =
        value
        |> Option.bind (fun text -> if String.IsNullOrWhiteSpace text then None else Some text)

    let summarizeStrings (values: seq<string>) =
        values
        |> Seq.filter (fun value -> String.IsNullOrWhiteSpace value |> not)
        |> Seq.distinct
        |> Seq.toList
        |> function
            | [] -> None
            | [ single ] -> Some single
            | [ first; second ] -> Some $"{first}; {second}"
            | first :: second :: rest -> Some $"{first}; {second}; +{rest.Length} more"

    let summariseStringList (values: string list) =
        values
        |> List.filter (fun value -> String.IsNullOrWhiteSpace value |> not)
        |> List.distinct
        |> function
            | [] -> None
            | [ single ] -> Some single
            | [ first; second ] -> Some $"{first}; {second}"
            | first :: second :: rest -> Some $"{first}; {second}; +{rest.Length} more"

    let personDisplayName (person: Person) =
        [ person.FirstName; person.MidInitials; person.LastName ]
        |> List.choose id
        |> List.filter (fun value -> String.IsNullOrWhiteSpace value |> not)
        |> function
            | [] -> person.ORCID |> asOptionalText |> Option.defaultValue "Unnamed person"
            | parts -> String.concat " " parts

    let ontologyLabel (annotation: OntologyAnnotation option) =
        annotation |> Option.map _.NameText |> asOptionalText

    let usesDetailedMetadataForm =
        function
        | ArcExplorerNodeKind.Study
        | ArcExplorerNodeKind.Assay
        | ArcExplorerNodeKind.Workflow
        | ArcExplorerNodeKind.Run -> true
        | _ -> false

    let arcFileMatchesMetadataNodeKind (selectedNodeKind: ArcExplorerNodeKind, arcFile: ArcFiles) =
        match selectedNodeKind, arcFile with
        | ArcExplorerNodeKind.Arc, ArcFiles.Investigation _
        | ArcExplorerNodeKind.Study, ArcFiles.Study _
        | ArcExplorerNodeKind.Assay, ArcFiles.Assay _
        | ArcExplorerNodeKind.Workflow, ArcFiles.Workflow _
        | ArcExplorerNodeKind.Run, ArcFiles.Run _ -> true
        | _ -> false

    let textRow label value : ArcObjectPropertyRow = {
        Label = label
        Value = ArcObjectPropertyValue.Text value
    }

    let codeRow label value : ArcObjectPropertyRow = {
        Label = label
        Value = ArcObjectPropertyValue.Code value
    }

    let optionalTextRow label value =
        value |> asOptionalText |> Option.map (fun text -> textRow label text)

    let dataMapSummaryRows (dataMap: DataMap) =
        let headers =
            seq {
                for index in 0 .. dataMap.ColumnCount - 1 do
                    yield dataMap.GetHeader(index).ToString()
            }
            |> summarizeStrings

        [
            textRow "Data Contexts" (string dataMap.DataContexts.Count)
            textRow "Columns" (string dataMap.ColumnCount)
            yield! headers |> Option.map (fun value -> textRow "Headers" value) |> Option.toList
        ]

    let tableSummaryRows (table: ArcTable) =
        let headers = table.Headers |> Seq.map _.ToString() |> summarizeStrings

        [
            textRow "Name" table.Name
            textRow "Rows" (string table.RowCount)
            textRow "Columns" (string table.ColumnCount)
            yield! headers |> Option.map (fun value -> textRow "Headers" value) |> Option.toList
        ]

    let sampleSummaryRows (sampleName: string, sampleSummary: ArcExplorerSampleSummary) = [
        textRow "Name" sampleName
        textRow "Characteristics" (string sampleSummary.Characteristics.Length)
        yield!
            summariseStringList sampleSummary.Characteristics
            |> Option.map (fun value -> textRow "Characteristic Fields" value)
            |> Option.toList
        textRow "Factors" (string sampleSummary.Factors.Length)
        yield!
            summariseStringList sampleSummary.Factors
            |> Option.map (fun value -> textRow "Factor Fields" value)
            |> Option.toList
        yield!
            summariseStringList sampleSummary.DerivesFrom
            |> Option.map (fun value -> textRow "Derives From" value)
            |> Option.toList
        yield!
            summariseStringList sampleSummary.SourceTables
            |> Option.map (fun value -> textRow "Source Tables" value)
            |> Option.toList
        yield!
            summariseStringList sampleSummary.Studies
            |> Option.map (fun value -> textRow "Studies" value)
            |> Option.toList
        yield!
            summariseStringList sampleSummary.Assays
            |> Option.map (fun value -> textRow "Assays" value)
            |> Option.toList
    ]

    let investigationMetadataRows (investigation: ArcInvestigation) = [
        textRow "Identifier" investigation.Identifier
        yield! optionalTextRow "Title" investigation.Title |> Option.toList
        yield! optionalTextRow "Description" investigation.Description |> Option.toList
        yield! optionalTextRow "Submission Date" investigation.SubmissionDate |> Option.toList
        yield!
            optionalTextRow "Public Release" investigation.PublicReleaseDate
            |> Option.toList
        yield!
            summarizeStrings (investigation.Contacts |> Seq.map personDisplayName)
            |> Option.map (fun value -> textRow "Contacts" value)
            |> Option.toList
        textRow "Publications" (string investigation.Publications.Count)
        textRow "Ontology Sources" (string investigation.OntologySourceReferences.Count)
        textRow "Comments" (string investigation.Comments.Count)
    ]

    let studyMetadataRows (study: ArcStudy) = [
        textRow "Identifier" study.Identifier
        yield! optionalTextRow "Title" study.Title |> Option.toList
        yield! optionalTextRow "Description" study.Description |> Option.toList
        textRow "Tables" (string study.TableCount)
        yield! optionalTextRow "Submission Date" study.SubmissionDate |> Option.toList
        yield! optionalTextRow "Public Release" study.PublicReleaseDate |> Option.toList
        yield!
            summarizeStrings (study.StudyDesignDescriptors |> Seq.map _.NameText)
            |> Option.map (fun value -> textRow "Design" value)
            |> Option.toList
        yield!
            summarizeStrings (study.Contacts |> Seq.map personDisplayName)
            |> Option.map (fun value -> textRow "Contacts" value)
            |> Option.toList
        textRow "Publications" (string study.Publications.Count)
        textRow "Comments" (string study.Comments.Count)
    ]

    let assayMetadataRows (assay: ArcAssay) = [
        textRow "Identifier" assay.Identifier
        yield! optionalTextRow "Title" assay.Title |> Option.toList
        yield! optionalTextRow "Description" assay.Description |> Option.toList
        textRow "Tables" (string assay.TableCount)
        yield!
            ontologyLabel assay.MeasurementType
            |> Option.map (fun value -> textRow "Measurement" value)
            |> Option.toList
        yield!
            ontologyLabel assay.TechnologyType
            |> Option.map (fun value -> textRow "Technology" value)
            |> Option.toList
        yield!
            ontologyLabel assay.TechnologyPlatform
            |> Option.map (fun value -> textRow "Platform" value)
            |> Option.toList
        yield!
            summarizeStrings (assay.Performers |> Seq.map personDisplayName)
            |> Option.map (fun value -> textRow "Performers" value)
            |> Option.toList
        textRow "Comments" (string assay.Comments.Count)
    ]

    let workflowMetadataRows (workflow: ArcWorkflow) = [
        textRow "Identifier" workflow.Identifier
        yield! optionalTextRow "Title" workflow.Title |> Option.toList
        yield! optionalTextRow "Description" workflow.Description |> Option.toList
        yield! optionalTextRow "Version" workflow.Version |> Option.toList
        yield!
            ontologyLabel workflow.WorkflowType
            |> Option.map (fun value -> textRow "Type" value)
            |> Option.toList
        yield! optionalTextRow "URI" workflow.URI |> Option.toList
        yield!
            summarizeStrings workflow.SubWorkflowIdentifiers
            |> Option.map (fun value -> textRow "Subworkflows" value)
            |> Option.toList
        yield!
            summarizeStrings (workflow.Contacts |> Seq.map personDisplayName)
            |> Option.map (fun value -> textRow "Contacts" value)
            |> Option.toList
        textRow "Comments" (string workflow.Comments.Count)
    ]

    let runMetadataRows (run: ArcRun) = [
        textRow "Identifier" run.Identifier
        yield! optionalTextRow "Title" run.Title |> Option.toList
        yield! optionalTextRow "Description" run.Description |> Option.toList
        textRow "Tables" (string run.TableCount)
        yield!
            ontologyLabel run.MeasurementType
            |> Option.map (fun value -> textRow "Measurement" value)
            |> Option.toList
        yield!
            ontologyLabel run.TechnologyType
            |> Option.map (fun value -> textRow "Technology" value)
            |> Option.toList
        yield!
            ontologyLabel run.TechnologyPlatform
            |> Option.map (fun value -> textRow "Platform" value)
            |> Option.toList
        yield!
            summarizeStrings run.WorkflowIdentifiers
            |> Option.map (fun value -> textRow "Workflows" value)
            |> Option.toList
        yield!
            summarizeStrings (run.Performers |> Seq.map personDisplayName)
            |> Option.map (fun value -> textRow "Performers" value)
            |> Option.toList
        textRow "Comments" (string run.Comments.Count)
    ]

    let templateMetadataRows (template: Template) = [
        textRow "Name" template.Name
        yield! optionalTextRow "Description" (Some template.Description) |> Option.toList
        yield! optionalTextRow "Version" (Some template.Version) |> Option.toList
        yield!
            optionalTextRow "Last Updated" (Some(template.LastUpdated.ToString("yyyy-MM-dd HH:mm")))
            |> Option.toList
        textRow "Organisation" (template.Organisation.ToString())
        yield!
            summarizeStrings (template.Tags |> Seq.map _.NameText)
            |> Option.map (fun value -> textRow "Tags" value)
            |> Option.toList
        yield!
            summarizeStrings (template.Authors |> Seq.map personDisplayName)
            |> Option.map (fun value -> textRow "Authors" value)
            |> Option.toList
    ]

    let metadataRows (arcFile: ArcFiles) =
        match arcFile with
        | ArcFiles.Investigation investigation -> investigationMetadataRows investigation
        | ArcFiles.Study(study, _) -> studyMetadataRows study
        | ArcFiles.Assay assay -> assayMetadataRows assay
        | ArcFiles.Workflow workflow -> workflowMetadataRows workflow
        | ArcFiles.Run run -> runMetadataRows run
        | ArcFiles.DataMap(_, dataMap) -> dataMapSummaryRows dataMap
        | ArcFiles.Template template -> templateMetadataRows template

    let currentPreviewRowsForNode (selectedNode: ArcExplorerNode, arcFile: ArcFiles) =
        match selectedNode.kind with
        | ArcExplorerNodeKind.Sample ->
            selectedNode.sampleSummary
            |> Option.map (fun summary -> sampleSummaryRows (selectedNode.name, summary))
        | ArcExplorerNodeKind.Table ->
            match selectedNode.previewTarget with
            | ArcExplorerNodeViewTarget.Table tableIndex when tableIndex >= 0 && tableIndex < arcFile.Tables().Count ->
                arcFile.Tables().[tableIndex] |> tableSummaryRows |> Some
            | _ -> None
        | ArcExplorerNodeKind.DataMap -> arcFile.TryGetDataMap() |> Option.map dataMapSummaryRows
        | _ when arcFile.Tables().Count > 0 ->
            let tableNames = arcFile.Tables() |> Seq.map _.Name |> summarizeStrings

            Some [
                textRow "Tables" (string (arcFile.Tables().Count))
                yield! tableNames |> Option.map (fun value -> textRow "Names" value) |> Option.toList
            ]
        | _ -> arcFile.TryGetDataMap() |> Option.map dataMapSummaryRows

    let arcFileMatchesSelectedNodePreviewPath (selectedNode: ArcExplorerNode, arcFile: ArcFiles) =
        match selectedNode.path, arcFile.TryGetRelativePath() with
        | Some nodePath, Some arcFilePath ->
            let expectedPreviewPath = PathHelpers.resolveArcViewPath nodePath
            PathHelpers.pathsEqual expectedPreviewPath arcFilePath
        | _ -> false

[<Erase; Mangle(false)>]
type ArcObjectExplorerContent =

    [<ReactComponent>]
    static member private Section(title: string, children: ReactElement list) =
        ArcObjectDetailsLayout.Section(title, children)

    [<ReactComponent>]
    static member private PropertyValueView(value: ArcObjectPropertyValue) =
        match value with
        | ArcObjectPropertyValue.Text value ->
            Html.span [
                prop.className "swt:text-sm swt:whitespace-pre-wrap swt:break-words"
                prop.text value
            ]
        | ArcObjectPropertyValue.Code value ->
            Html.code [
                prop.className "swt:text-xs swt:font-mono swt:break-all"
                prop.text value
            ]

    [<ReactComponent>]
    static member private Properties(rows: ArcObjectPropertyRow list) =
        Html.dl [
            prop.className "swt:grid swt:grid-cols-1 swt:gap-y-3"
            prop.children [
                for row in rows do
                    Html.div [
                        prop.className "swt:flex swt:flex-col swt:gap-1"
                        prop.children [
                            Html.dt [
                                prop.className
                                    "swt:text-xs swt:font-semibold swt:uppercase swt:tracking-wide swt:opacity-60"
                                prop.text row.Label
                            ]
                            Html.dd [
                                prop.className "swt:min-w-0"
                                prop.children [ ArcObjectExplorerContent.PropertyValueView(row.Value) ]
                            ]
                        ]
                    ]
            ]
        ]

    [<ReactComponent>]
    static member private AssociatedSampleLinksSection
        (links: ArcExplorerNodeLink list, onSelectNodeId: string -> unit)
        =
        ArcObjectExplorerContent.Section(
            "Associated Samples",
            [
                Html.div [
                    prop.className "swt:flex swt:flex-col swt:gap-2"
                    prop.children [
                        Html.p [
                            prop.className "swt:text-sm swt:opacity-70"
                            prop.text (
                                if links.Length = 1 then
                                    "This table is associated with 1 sample."
                                else
                                    $"This table is associated with {links.Length} samples."
                            )
                        ]
                        Html.div [
                            prop.className "swt:flex swt:flex-col swt:gap-2"
                            prop.children (
                                links
                                |> List.map (fun link ->
                                    Html.button [
                                        prop.key link.targetId
                                        prop.type'.button
                                        prop.className
                                            "swt:flex swt:w-full swt:items-start swt:justify-between swt:gap-3 swt:rounded-lg swt:border swt:border-base-300 swt:bg-base-100 swt:px-3 swt:py-2 swt:text-left hover:swt:border-primary/60 hover:swt:bg-base-200/60"
                                        prop.onClick (fun _ -> onSelectNodeId link.targetId)
                                        prop.children [
                                            Html.div [
                                                prop.className "swt:flex swt:min-w-0 swt:flex-col swt:gap-1"
                                                prop.children [
                                                    Html.span [
                                                        prop.className "swt:text-sm swt:font-medium swt:break-words"
                                                        prop.text link.name
                                                    ]
                                                    match link.subtitle with
                                                    | Some subtitle ->
                                                        Html.span [
                                                            prop.className "swt:text-xs swt:opacity-60"
                                                            prop.text subtitle
                                                        ]
                                                    | None -> Html.none
                                                ]
                                            ]
                                            Html.span [
                                                prop.className "swt:text-xs swt:font-mono swt:opacity-50"
                                                prop.text "Open"
                                            ]
                                        ]
                                    ]
                                )
                            )
                        ]
                    ]
                ]
            ]
        )

    [<ReactComponent>]
    static member private SelectionSection(selectedNode: ArcExplorerNode) =
        let rows = [
            ArcObjectExplorerContentHelper.textRow "Type" (nodeKindLabel selectedNode.kind)
            ArcObjectExplorerContentHelper.textRow
                "Role"
                (if selectedNode.isReference then
                     "Reference"
                 else
                     "Canonical")
            if selectedNode.path.IsSome then
                ArcObjectExplorerContentHelper.codeRow "Path" selectedNode.path.Value
            else
                ArcObjectExplorerContentHelper.textRow "Path" "Virtual"
        ]

        ArcObjectExplorerContent.Section(
            "Selection",
            [
                Html.h4 [
                    prop.className "swt:text-base swt:font-semibold swt:break-words"
                    prop.text selectedNode.name
                ]
                ArcObjectExplorerContent.Properties rows
            ]
        )

    [<ReactComponent>]
    static member private ErrorSection(title: string, message: string) =
        ArcObjectExplorerContent.Section(
            title,
            [
                Html.p [
                    prop.className "swt:text-sm swt:text-error"
                    prop.text message
                ]
            ]
        )

    [<ReactComponent>]
    static member private NoteContentSection(content: string) =
        ArcObjectExplorerContent.Section(
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
    static member private DetailedMetadataContent(arcFile: ArcFiles, setArcFileState: ArcFiles option -> unit) =
        let setArcFile arcFile = setArcFileState (Some arcFile)

        Html.div [
            prop.className "swt:min-w-0"
            prop.children [ ArcFileMetadata.ArcFileMetadata(arcFile, setArcFile) ]
        ]

    [<ReactComponent>]
    static member ARCObjectDetailsContent
        (
            selectedNode: ArcExplorerNode option,
            selectedAncestors: ArcExplorerNode list,
            previewState: PageState option,
            arcFileState: ArcFiles option,
            setArcFileState: ArcFiles option -> unit,
            onSelectNodeId: string -> unit,
            useDetailedMetadataForms: bool
        ) =
        match selectedNode with
        | None -> ArcObjectDetailsLayout.EmptyState("Select an ARC object to inspect its details.")
        | Some selectedNode ->
            let previewArcFile =
                arcFileState
                |> Option.filter (fun state ->
                    ArcObjectExplorerContentHelper.arcFileMatchesSelectedNodePreviewPath (selectedNode, state)
                )

            let metadataArcFile =
                previewArcFile
                |> Option.filter (fun file ->
                    ArcObjectExplorerContentHelper.arcFileMatchesMetadataNodeKind (selectedNode.kind, file)
                )

            let selectedObjectRows =
                match selectedNode.kind, metadataArcFile with
                | ArcExplorerNodeKind.Sample, _ ->
                    selectedNode.sampleSummary
                    |> Option.map (fun summary ->
                        ArcObjectExplorerContentHelper.sampleSummaryRows (selectedNode.name, summary)
                    )
                | _, Some _ -> None
                | _, None ->
                    previewArcFile
                    |> Option.bind (fun file ->
                        ArcObjectExplorerContentHelper.currentPreviewRowsForNode (selectedNode, file)
                    )

            let parentMetadataContext =
                match metadataArcFile, previewArcFile with
                | Some _, _ -> None
                | None, Some arcFile ->
                    selectedAncestors
                    |> List.rev
                    |> List.tryFind (fun ancestor ->
                        ArcObjectExplorerContentHelper.arcFileMatchesMetadataNodeKind (ancestor.kind, arcFile)
                    )
                    |> Option.map (fun parentNode -> parentNode, arcFile)
                | None, None -> None

            Html.div [
                prop.className "swt:flex swt:flex-col swt:gap-3 swt:h-full"
                prop.children [
                    ArcObjectExplorerContent.SelectionSection(selectedNode)
                    match previewState with
                    | Some(PageState.TextPage content) -> ArcObjectExplorerContent.NoteContentSection(content)
                    | Some(PageState.ErrorPage message) ->
                        ArcObjectExplorerContent.ErrorSection("Note Content", message)
                    | _ -> Html.none
                    match selectedObjectRows with
                    | Some rows ->
                        ArcObjectExplorerContent.Section("Current Object", [ ArcObjectExplorerContent.Properties rows ])
                    | None -> Html.none
                    match selectedNode.kind, selectedNode.relatedSamples with
                    | ArcExplorerNodeKind.Table, relatedSamples when List.isEmpty relatedSamples |> not ->
                        ArcObjectExplorerContent.AssociatedSampleLinksSection(relatedSamples, onSelectNodeId)
                    | _ -> Html.none
                    match metadataArcFile with
                    | Some arcFile when
                        useDetailedMetadataForms
                        && ArcObjectExplorerContentHelper.usesDetailedMetadataForm selectedNode.kind
                        ->
                        ArcObjectExplorerContent.DetailedMetadataContent(arcFile, setArcFileState)
                    | Some arcFile ->
                        ArcObjectExplorerContent.Section(
                            "Metadata",
                            [
                                ArcObjectExplorerContent.Properties(ArcObjectExplorerContentHelper.metadataRows arcFile)
                            ]
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
                                            prop.text (nodeKindLabel parentNode.kind)
                                        ]
                                        Html.h4 [
                                            prop.className "swt:text-base swt:font-semibold swt:break-words"
                                            prop.text parentNode.name
                                        ]
                                    ]
                                ]

                            ArcObjectExplorerContent.Section(
                                "Parent Metadata",
                                [
                                    parentSubtitle
                                    ArcObjectExplorerContent.Properties(
                                        ArcObjectExplorerContentHelper.metadataRows arcFile
                                    )
                                ]
                            )
                        | None -> Html.none
                ]
            ]
