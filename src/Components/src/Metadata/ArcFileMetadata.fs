namespace Swate.Components

open System
open Fable.Core
open Feliz
open ARCtrl
open Swate.Components.Metadata

[<Erase; Mangle(false)>]
type ArcFileMetadata =

    [<ReactComponent>]
    static member AssayMetadata(assay: ArcAssay, setAssay: ArcAssay -> unit) =
        Generic.Section [
            Generic.BoxedField(
                "Assay Metadata",
                content = [
                    FormComponents.TextInput(
                        assay.Identifier,
                        (fun _ -> ()),
                        label = "Identifier",
                        disabled = true
                    )
                    FormComponents.TextInput(
                        defaultArg assay.Title "",
                        (fun value ->
                            assay.Title <- if String.IsNullOrWhiteSpace value then None else Some value
                            setAssay assay
                        ),
                        label = "Title"
                    )
                    FormComponents.TextInput(
                        defaultArg assay.Description "",
                        (fun value ->
                            assay.Description <- if String.IsNullOrWhiteSpace value then None else Some value
                            setAssay assay
                        ),
                        label = "Description",
                        isArea = true
                    )
                    FormComponents.OntologyAnnotationInput(
                        assay.MeasurementType,
                        (fun annotation ->
                            assay.MeasurementType <- annotation
                            setAssay assay
                        ),
                        label = "Measurement Type"
                    )
                    FormComponents.OntologyAnnotationInput(
                        assay.TechnologyType,
                        (fun annotation ->
                            assay.TechnologyType <- annotation
                            setAssay assay
                        ),
                        label = "Technology Type"
                    )
                    FormComponents.OntologyAnnotationInput(
                        assay.TechnologyPlatform,
                        (fun annotation ->
                            assay.TechnologyPlatform <- annotation
                            setAssay assay
                        ),
                        label = "Technology Platform"
                    )
                    FormComponents.PersonsInput(
                        assay.Performers,
                        (fun persons ->
                            assay.Performers <- persons
                            setAssay assay
                        ),
                        label = "Performers"
                    )
                    FormComponents.CommentsInput(
                        assay.Comments,
                        (fun comments ->
                            assay.Comments <- comments
                            setAssay assay
                        ),
                        label = "Comments"
                    )
                ]
            )
        ]

    [<ReactComponent>]
    static member StudyMetadata(study: ArcStudy, setStudy: ArcStudy -> unit) =
        Generic.Section [
            Generic.BoxedField(
                "Study Metadata",
                content = [
                    FormComponents.TextInput(
                        study.Identifier,
                        (fun _ -> ()),
                        label = "Identifier",
                        disabled = true
                    )
                    FormComponents.TextInput(
                        defaultArg study.Title "",
                        (fun value ->
                            study.Title <- if String.IsNullOrWhiteSpace value then None else Some value
                            setStudy study
                        ),
                        label = "Title"
                    )
                    FormComponents.TextInput(
                        defaultArg study.Description "",
                        (fun value ->
                            study.Description <- if String.IsNullOrWhiteSpace value then None else Some value
                            setStudy study
                        ),
                        label = "Description",
                        isArea = true
                    )
                    FormComponents.PersonsInput(
                        study.Contacts,
                        (fun persons ->
                            study.Contacts <- persons
                            setStudy study
                        ),
                        label = "Contacts"
                    )
                    FormComponents.PublicationsInput(
                        study.Publications,
                        (fun publications ->
                            study.Publications <- publications
                            setStudy study
                        ),
                        label = "Publications"
                    )
                    FormComponents.DateTimeInput(
                        defaultArg study.SubmissionDate "",
                        (fun value ->
                            study.SubmissionDate <- if String.IsNullOrWhiteSpace value then None else Some value
                            setStudy study
                        ),
                        label = "Submission Date"
                    )
                    FormComponents.DateTimeInput(
                        defaultArg study.PublicReleaseDate "",
                        (fun value ->
                            study.PublicReleaseDate <- if String.IsNullOrWhiteSpace value then None else Some value
                            setStudy study
                        ),
                        label = "Public Release Date"
                    )
                    FormComponents.OntologyAnnotationsInput(
                        study.StudyDesignDescriptors,
                        (fun annotations ->
                            study.StudyDesignDescriptors <- annotations
                            setStudy study
                        ),
                        label = "Study Design Descriptors"
                    )
                    FormComponents.CommentsInput(
                        study.Comments,
                        (fun comments ->
                            study.Comments <- comments
                            setStudy study
                        ),
                        label = "Comments"
                    )
                ]
            )
        ]

    [<ReactComponent>]
    static member InvestigationMetadata
        (investigation: ArcInvestigation, setInvestigation: ArcInvestigation -> unit)
        =
        Generic.Section [
            Generic.BoxedField(
                "Investigation Metadata",
                content = [
                    FormComponents.TextInput(
                        investigation.Identifier,
                        (fun _ -> ()),
                        label = "Identifier",
                        disabled = true
                    )
                    FormComponents.TextInput(
                        defaultArg investigation.Title "",
                        (fun value ->
                            investigation.Title <- if String.IsNullOrWhiteSpace value then None else Some value
                            setInvestigation investigation
                        ),
                        label = "Title"
                    )
                    FormComponents.TextInput(
                        defaultArg investigation.Description "",
                        (fun value ->
                            investigation.Description <- if String.IsNullOrWhiteSpace value then None else Some value
                            setInvestigation investigation
                        ),
                        label = "Description",
                        isArea = true
                    )
                    FormComponents.PersonsInput(
                        investigation.Contacts,
                        (fun persons ->
                            investigation.Contacts <- persons
                            setInvestigation investigation
                        ),
                        label = "Contacts"
                    )
                    FormComponents.PublicationsInput(
                        investigation.Publications,
                        (fun publications ->
                            investigation.Publications <- publications
                            setInvestigation investigation
                        ),
                        label = "Publications"
                    )
                    FormComponents.DateTimeInput(
                        defaultArg investigation.SubmissionDate "",
                        (fun value ->
                            investigation.SubmissionDate <- if String.IsNullOrWhiteSpace value then None else Some value
                            setInvestigation investigation
                        ),
                        label = "Submission Date"
                    )
                    FormComponents.DateTimeInput(
                        defaultArg investigation.PublicReleaseDate "",
                        (fun value ->
                            investigation.PublicReleaseDate <- if String.IsNullOrWhiteSpace value then None else Some value
                            setInvestigation investigation
                        ),
                        label = "Public Release Date"
                    )
                    FormComponents.OntologySourceReferencesInput(
                        investigation.OntologySourceReferences,
                        (fun references ->
                            investigation.OntologySourceReferences <- references
                            setInvestigation investigation
                        ),
                        label = "Ontology Source References"
                    )
                    FormComponents.CommentsInput(
                        investigation.Comments,
                        (fun comments ->
                            investigation.Comments <- comments
                            setInvestigation investigation
                        ),
                        label = "Comments"
                    )
                ]
            )
        ]

    [<ReactComponent>]
    static member RunMetadata(run: ArcRun, setRun: ArcRun -> unit) =
        Generic.Section [
            Generic.BoxedField(
                "Run Metadata",
                content = [
                    FormComponents.TextInput(
                        run.Identifier,
                        (fun _ -> ()),
                        label = "Identifier",
                        disabled = true
                    )
                    FormComponents.TextInput(
                        defaultArg run.Title "",
                        (fun value ->
                            run.Title <- if String.IsNullOrWhiteSpace value then None else Some value
                            setRun run
                        ),
                        label = "Title"
                    )
                    FormComponents.TextInput(
                        defaultArg run.Description "",
                        (fun value ->
                            run.Description <- if String.IsNullOrWhiteSpace value then None else Some value
                            setRun run
                        ),
                        label = "Description",
                        isArea = true
                    )
                    FormComponents.OntologyAnnotationInput(
                        run.MeasurementType,
                        (fun annotation ->
                            run.MeasurementType <- annotation
                            setRun run
                        ),
                        label = "Measurement Type"
                    )
                    FormComponents.OntologyAnnotationInput(
                        run.TechnologyType,
                        (fun annotation ->
                            run.TechnologyType <- annotation
                            setRun run
                        ),
                        label = "Technology Type"
                    )
                    FormComponents.OntologyAnnotationInput(
                        run.TechnologyPlatform,
                        (fun annotation ->
                            run.TechnologyPlatform <- annotation
                            setRun run
                        ),
                        label = "Technology Platform"
                    )
                    FormComponents.CollectionOfStrings(
                        run.WorkflowIdentifiers,
                        (fun identifiers ->
                            run.WorkflowIdentifiers <- identifiers
                            setRun run
                        ),
                        label = "Workflow Identifiers"
                    )
                    FormComponents.PersonsInput(
                        run.Performers,
                        (fun persons ->
                            run.Performers <- persons
                            setRun run
                        ),
                        label = "Performers"
                    )
                    FormComponents.CommentsInput(
                        run.Comments,
                        (fun comments ->
                            run.Comments <- comments
                            setRun run
                        ),
                        label = "Comments"
                    )
                ]
            )
        ]

    [<ReactComponent>]
    static member WorkflowMetadata(workflow: ArcWorkflow, setWorkflow: ArcWorkflow -> unit) =
        Generic.Section [
            Generic.BoxedField(
                "Workflow Metadata",
                content = [
                    FormComponents.TextInput(
                        workflow.Identifier,
                        (fun _ -> ()),
                        label = "Identifier",
                        disabled = true
                    )
                    FormComponents.TextInput(
                        defaultArg workflow.Title "",
                        (fun value ->
                            workflow.Title <- if String.IsNullOrWhiteSpace value then None else Some value
                            setWorkflow workflow
                        ),
                        label = "Title"
                    )
                    FormComponents.TextInput(
                        defaultArg workflow.Description "",
                        (fun value ->
                            workflow.Description <- if String.IsNullOrWhiteSpace value then None else Some value
                            setWorkflow workflow
                        ),
                        label = "Description",
                        isArea = true
                    )
                    FormComponents.TextInput(
                        defaultArg workflow.Version "",
                        (fun value ->
                            workflow.Version <- if String.IsNullOrWhiteSpace value then None else Some value
                            setWorkflow workflow
                        ),
                        label = "Version"
                    )
                    FormComponents.OntologyAnnotationInput(
                        workflow.WorkflowType,
                        (fun annotation ->
                            workflow.WorkflowType <- annotation
                            setWorkflow workflow
                        ),
                        label = "Workflow Type"
                    )
                    FormComponents.TextInput(
                        defaultArg workflow.URI "",
                        (fun value ->
                            workflow.URI <- if String.IsNullOrWhiteSpace value then None else Some value
                            setWorkflow workflow
                        ),
                        label = "URI"
                    )
                    FormComponents.PersonsInput(
                        workflow.Contacts,
                        (fun persons ->
                            workflow.Contacts <- persons
                            setWorkflow workflow
                        ),
                        label = "Contacts"
                    )
                    FormComponents.CommentsInput(
                        workflow.Comments,
                        (fun comments ->
                            workflow.Comments <- comments
                            setWorkflow workflow
                        ),
                        label = "Comments"
                    )
                ]
            )
        ]

    [<ReactComponent>]
    static member TemplateMetadata(template: Template, setTemplate: Template -> unit) =
        Generic.Section [
            Generic.BoxedField(
                "Template Metadata",
                content = [
                    FormComponents.TextInput(
                        template.Id.ToString(),
                        (fun _ -> ()),
                        label = "Identifier",
                        disabled = true
                    )
                    FormComponents.TextInput(
                        template.Name,
                        (fun value ->
                            template.Name <- value
                            setTemplate template
                        ),
                        label = "Name"
                    )
                    FormComponents.TextInput(
                        template.Description,
                        (fun value ->
                            template.Description <- value
                            setTemplate template
                        ),
                        label = "Description",
                        isArea = true
                    )
                    FormComponents.TextInput(
                        template.Organisation.ToString(),
                        (fun value ->
                            template.Organisation <- Organisation.ofString value
                            setTemplate template
                        ),
                        label = "Organisation"
                    )
                    FormComponents.TextInput(
                        template.Version,
                        (fun value ->
                            template.Version <- value
                            setTemplate template
                        ),
                        label = "Version"
                    )
                    FormComponents.DateTimeInput(
                        template.LastUpdated,
                        (fun value ->
                            template.LastUpdated <- value
                            setTemplate template
                        ),
                        label = "Last Updated"
                    )
                    FormComponents.OntologyAnnotationsInput(
                        template.Tags,
                        (fun annotations ->
                            template.Tags <- annotations
                            setTemplate template
                        ),
                        label = "Tags"
                    )
                    FormComponents.OntologyAnnotationsInput(
                        template.EndpointRepositories,
                        (fun annotations ->
                            template.EndpointRepositories <- annotations
                            setTemplate template
                        ),
                        label = "Endpoint Repositories"
                    )
                    FormComponents.PersonsInput(
                        template.Authors,
                        (fun persons ->
                            template.Authors <- persons
                            setTemplate template
                        ),
                        label = "Authors"
                    )
                ]
            )
        ]

    [<ReactComponent>]
    static member DataMapMetadata(datamap: DataMap) =
        Generic.Section [
            Generic.BoxedField("DataMap", description = $"Data Contexts: {datamap.DataContexts.Count}")
        ]

    [<ReactComponent>]
    static member View(arcFile: ArcFiles, setArcFile: ArcFiles -> unit) =
        Html.div [
            prop.className "swt:p-4 swt:h-full"
            prop.children [
                match arcFile with
                | ArcFiles.Investigation investigation ->
                    ArcFileMetadata.InvestigationMetadata(
                        investigation,
                        fun updated -> setArcFile (ArcFiles.Investigation updated)
                    )
                | ArcFiles.Study(study, assays) ->
                    ArcFileMetadata.StudyMetadata(study, fun updated -> setArcFile (ArcFiles.Study(updated, assays)))
                | ArcFiles.Assay assay ->
                    ArcFileMetadata.AssayMetadata(assay, fun updated -> setArcFile (ArcFiles.Assay updated))
                | ArcFiles.Run run ->
                    ArcFileMetadata.RunMetadata(run, fun updated -> setArcFile (ArcFiles.Run updated))
                | ArcFiles.Workflow workflow ->
                    ArcFileMetadata.WorkflowMetadata(
                        workflow,
                        fun updated -> setArcFile (ArcFiles.Workflow updated)
                    )
                | ArcFiles.DataMap(_, datamap) -> ArcFileMetadata.DataMapMetadata(datamap)
                | ArcFiles.Template template ->
                    ArcFileMetadata.TemplateMetadata(
                        template,
                        fun updated -> setArcFile (ArcFiles.Template updated)
                    )
            ]
        ]
