module Renderer.MetadataForms

open System
open Feliz
open Browser.Types
open ARCtrl
open Swate.Components
open Swate.Components.Shared
open Fable.Core.JsInterop
open Fetch
open Components.Forms
open Components.JsBindings


/// Generic form helpers for Electron metadata editing
type FormHelpers =

    /// Debounced text input component
    [<ReactComponent>]
    static member TextInput
        (
            value: string,
            setValue: string -> unit,
            ?label: string,
            ?placeholder: string,
            ?isArea: bool,
            ?disabled: bool,
            ?classes: string,
            ?isJoin: bool,
            ?rmv: MouseEvent -> unit,
            ?validator: string -> Result<unit, string>
        ) =
        let isArea = defaultArg isArea false
        let disabled = defaultArg disabled false
        let isJoin = defaultArg isJoin false
        let startedChange = React.useRef false
        let tempValue, setTempValue = React.useState value
        let debouncedValue = React.useDebounce (tempValue, 300)
        let validationError, setValidationError = React.useState (None: string option)

        // Update parent when debounced value changes
        React.useEffect (
            (fun () ->
                if startedChange.current then
                    // Validate if validator provided
                    match validator with
                    | Some v ->
                        match v debouncedValue with
                        | Result.Ok() ->
                            setValidationError None
                            setValue debouncedValue
                        | Result.Error msg -> setValidationError (Some msg)
                    | None ->
                        setValidationError None
                        setValue debouncedValue

                    startedChange.current <- false
            ),
            [| box debouncedValue |]
        )

        // Sync with external value changes
        React.useEffect ((fun () -> setTempValue value), [| box value |])

        let handleChange =
            fun (s: string) ->
                setTempValue s
                startedChange.current <- true

        let inputClasses = [
            if isJoin then
                "swt:join-item"
            "swt:input swt:input-bordered swt:w-full"
            if validationError.IsSome then
                "swt:input-error"
            if classes.IsSome then
                classes.Value
        ]

        Html.div [
            prop.className (
                if isJoin then
                    "swt:grow swt:join-item"
                else
                    "swt:fieldset swt:grow"
            )
            prop.children [
                if label.IsSome && not isJoin then
                    Generic.FieldTitle label.Value
                if isArea then
                    Html.textarea [
                        prop.className [
                            "swt:textarea swt:textarea-bordered swt:w-full"
                            if validationError.IsSome then
                                "swt:textarea-error"
                            if classes.IsSome then
                                classes.Value
                        ]
                        prop.disabled disabled
                        prop.readOnly disabled
                        prop.valueOrDefault tempValue
                        prop.onChange handleChange
                        if placeholder.IsSome then
                            prop.placeholder placeholder.Value
                    ]
                else
                    Html.div [
                        prop.className "swt:flex swt:gap-2 swt:items-center swt:w-full"
                        prop.children [
                            Html.input [
                                prop.className inputClasses
                                prop.type'.text
                                prop.disabled disabled
                                prop.readOnly disabled
                                prop.valueOrDefault tempValue
                                prop.onChange handleChange
                                if placeholder.IsSome then
                                    prop.placeholder placeholder.Value
                            ]
                            if rmv.IsSome then
                                Components.Forms.Helper.deleteButton rmv.Value
                        ]
                    ]
                if validationError.IsSome then
                    Html.p [
                        prop.className "swt:text-error swt:text-sm swt:mt-1"
                        prop.text validationError.Value
                    ]
            ]
        ]

[<ReactComponent>]
let AssayMetadata (assay: ArcAssay, setAssay: ArcAssay -> unit) =
    Generic.Section [
        Generic.BoxedField(
            "Assay Metadata",
            content = [
                FormHelpers.TextInput(
                    assay.Identifier,
                    (fun _ -> ()),
                    label = "Identifier",
                    disabled = true
                )
                FormHelpers.TextInput(
                    defaultArg assay.Title "",
                    (fun v ->
                        assay.Title <- if System.String.IsNullOrWhiteSpace v then None else Some v
                        setAssay assay
                    ),
                    label = "Title"
                )
                FormHelpers.TextInput(
                    defaultArg assay.Description "",
                    (fun v ->
                        assay.Description <- if System.String.IsNullOrWhiteSpace v then None else Some v
                        setAssay assay
                    ),
                    label = "Description",
                    isArea = true
                )
                FormComponents.OntologyAnnotationInput(
                    assay.MeasurementType,
                    (fun oa ->
                        assay.MeasurementType <- oa
                        setAssay assay
                    ),
                    label = "Measurement Type"
                )
                FormComponents.OntologyAnnotationInput(
                    assay.TechnologyType,
                    (fun oa ->
                        assay.TechnologyType <- oa
                        setAssay assay
                    ),
                    label = "Technology Type"
                )
                FormComponents.OntologyAnnotationInput(
                    assay.TechnologyPlatform,
                    (fun oa ->
                        assay.TechnologyPlatform <- oa
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
let StudyMetadata (study: ArcStudy, setStudy: ArcStudy -> unit) =
    Generic.Section [
        Generic.BoxedField(
            "Study Metadata",
            content = [
                FormHelpers.TextInput(
                    study.Identifier,
                    (fun _ -> ()),
                    label = "Identifier",
                    disabled = true
                )
                FormHelpers.TextInput(
                    defaultArg study.Title "",
                    (fun v ->
                        study.Title <- if System.String.IsNullOrWhiteSpace v then None else Some v
                        setStudy study
                    ),
                    label = "Title"
                )
                FormHelpers.TextInput(
                    defaultArg study.Description "",
                    (fun v ->
                        study.Description <- if System.String.IsNullOrWhiteSpace v then None else Some v
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
                    (fun pubs ->
                        study.Publications <- pubs
                        setStudy study
                    ),
                    label = "Publications"
                )
                FormComponents.DateTimeInput(
                    defaultArg study.SubmissionDate "",
                    (fun v ->
                        study.SubmissionDate <- if System.String.IsNullOrWhiteSpace v then None else Some v
                        setStudy study
                    ),
                    label = "Submission Date"
                )
                FormComponents.DateTimeInput(
                    defaultArg study.PublicReleaseDate "",
                    (fun v ->
                        study.PublicReleaseDate <- if System.String.IsNullOrWhiteSpace v then None else Some v
                        setStudy study
                    ),
                    label = "Public Release Date"
                )
                FormComponents.OntologyAnnotationsInput(
                    study.StudyDesignDescriptors,
                    (fun oas ->
                        study.StudyDesignDescriptors <- oas
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
let InvestigationMetadata (investigation: ArcInvestigation, setInvestigation: ArcInvestigation -> unit) =
    Generic.Section [
        Generic.BoxedField(
            "Investigation Metadata",
            content = [
                FormHelpers.TextInput(
                    investigation.Identifier,
                    (fun _ -> ()),
                    label = "Identifier",
                    disabled = true
                )
                FormHelpers.TextInput(
                    defaultArg investigation.Title "",
                    (fun v ->
                        investigation.Title <- if System.String.IsNullOrWhiteSpace v then None else Some v
                        setInvestigation investigation
                    ),
                    label = "Title"
                )
                FormHelpers.TextInput(
                    defaultArg investigation.Description "",
                    (fun v ->
                        investigation.Description <- if System.String.IsNullOrWhiteSpace v then None else Some v
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
                    (fun pubs ->
                        investigation.Publications <- pubs
                        setInvestigation investigation
                    ),
                    label = "Publications"
                )
                FormComponents.DateTimeInput(
                    defaultArg investigation.SubmissionDate "",
                    (fun v ->
                        investigation.SubmissionDate <- if System.String.IsNullOrWhiteSpace v then None else Some v
                        setInvestigation investigation
                    ),
                    label = "Submission Date"
                )
                FormComponents.DateTimeInput(
                    defaultArg investigation.PublicReleaseDate "",
                    (fun v ->
                        investigation.PublicReleaseDate <- if System.String.IsNullOrWhiteSpace v then None else Some v
                        setInvestigation investigation
                    ),
                    label = "Public Release Date"
                )
                FormComponents.OntologySourceReferencesInput(
                    investigation.OntologySourceReferences,
                    (fun osrs ->
                        investigation.OntologySourceReferences <- osrs
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
let RunMetadata (run: ArcRun, setRun: ArcRun -> unit) =
    Generic.Section [
        Generic.BoxedField(
            "Run Metadata",
            content = [
                FormHelpers.TextInput(
                    run.Identifier,
                    (fun _ -> ()),
                    label = "Identifier",
                    disabled = true
                )
                FormHelpers.TextInput(
                    defaultArg run.Title "",
                    (fun v ->
                        run.Title <- if System.String.IsNullOrWhiteSpace v then None else Some v
                        setRun run
                    ),
                    label = "Title"
                )
                FormHelpers.TextInput(
                    defaultArg run.Description "",
                    (fun v ->
                        run.Description <- if System.String.IsNullOrWhiteSpace v then None else Some v
                        setRun run
                    ),
                    label = "Description",
                    isArea = true
                )
                FormComponents.OntologyAnnotationInput(
                    run.MeasurementType,
                    (fun oa ->
                        run.MeasurementType <- oa
                        setRun run
                    ),
                    label = "Measurement Type"
                )
                FormComponents.OntologyAnnotationInput(
                    run.TechnologyType,
                    (fun oa ->
                        run.TechnologyType <- oa
                        setRun run
                    ),
                    label = "Technology Type"
                )
                FormComponents.OntologyAnnotationInput(
                    run.TechnologyPlatform,
                    (fun oa ->
                        run.TechnologyPlatform <- oa
                        setRun run
                    ),
                    label = "Technology Platform"
                )
                FormComponents.CollectionOfStrings(
                    run.WorkflowIdentifiers,
                    (fun ids ->
                        run.WorkflowIdentifiers <- ids
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
let WorkflowMetadata (workflow: ArcWorkflow, setWorkflow: ArcWorkflow -> unit) =
    Generic.Section [
        Generic.BoxedField(
            "Workflow Metadata",
            content = [
                FormHelpers.TextInput(
                    workflow.Identifier,
                    (fun _ -> ()),
                    label = "Identifier",
                    disabled = true
                )
                FormHelpers.TextInput(
                    defaultArg workflow.Title "",
                    (fun v ->
                        workflow.Title <- if System.String.IsNullOrWhiteSpace v then None else Some v
                        setWorkflow workflow
                    ),
                    label = "Title"
                )
                FormHelpers.TextInput(
                    defaultArg workflow.Description "",
                    (fun v ->
                        workflow.Description <- if System.String.IsNullOrWhiteSpace v then None else Some v
                        setWorkflow workflow
                    ),
                    label = "Description",
                    isArea = true
                )
                FormHelpers.TextInput(
                    defaultArg workflow.Version "",
                    (fun v ->
                        workflow.Version <- if System.String.IsNullOrWhiteSpace v then None else Some v
                        setWorkflow workflow
                    ),
                    label = "Version"
                )
                FormComponents.OntologyAnnotationInput(
                    workflow.WorkflowType,
                    (fun oa ->
                        workflow.WorkflowType <- oa
                        setWorkflow workflow
                    ),
                    label = "Workflow Type"
                )
                FormHelpers.TextInput(
                    defaultArg workflow.URI "",
                    (fun v ->
                        workflow.URI <- if System.String.IsNullOrWhiteSpace v then None else Some v
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
let TemplateMetadata (template: Template, setTemplate: Template -> unit) =
    Generic.Section [
        Generic.BoxedField(
            "Template Metadata",
            content = [
                FormHelpers.TextInput(
                    template.Id.ToString(),
                    (fun _ -> ()),
                    label = "Identifier",
                    disabled = true
                )
                FormHelpers.TextInput(
                    template.Name,
                    (fun v ->
                        template.Name <- v
                        setTemplate template
                    ),
                    label = "Name"
                )
                FormHelpers.TextInput(
                    template.Description,
                    (fun v ->
                        template.Description <- v
                        setTemplate template
                    ),
                    label = "Description",
                    isArea = true
                )
                FormHelpers.TextInput(
                    template.Organisation.ToString(),
                    (fun v ->
                        template.Organisation <- Organisation.ofString v
                        setTemplate template
                    ),
                    label = "Organisation"
                )
                FormHelpers.TextInput(
                    template.Version,
                    (fun v ->
                        template.Version <- v
                        setTemplate template
                    ),
                    label = "Version"
                )
                FormComponents.DateTimeInput(
                    template.LastUpdated,
                    (fun v ->
                        template.LastUpdated <- v
                        setTemplate template
                    ),
                    label = "Last Updated"
                )
                FormComponents.OntologyAnnotationsInput(
                    template.Tags,
                    (fun oas ->
                        template.Tags <- oas
                        setTemplate template
                    ),
                    label = "Tags"
                )
                FormComponents.OntologyAnnotationsInput(
                    template.EndpointRepositories,
                    (fun oas ->
                        template.EndpointRepositories <- oas
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
let DataMapMetadata (datamap: DataMap) =
    Generic.Section [
        Generic.BoxedField("DataMap", description = $"Data Contexts: {datamap.DataContexts.Count}")
    ]