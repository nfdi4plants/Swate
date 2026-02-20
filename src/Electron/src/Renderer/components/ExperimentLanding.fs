module Renderer.components.ExperimentLanding

open System
open Feliz
open ARCtrl
open ARCtrl.Json
open Swate.Components
open Swate.Electron.Shared.IPCTypes
open Components.Forms
open Renderer.MetadataForms

type LandingDraft = {
    Identifier: string
    Title: string
    Description: string
    InvolvedPeople: ResizeArray<Person>
    Comments: ResizeArray<Comment>
    MainText: string
    Files: string list
    Publications: ResizeArray<Publication>
    SubmissionDate: string
    PublicReleaseDate: string
    StudyDesignDescriptors: ResizeArray<OntologyAnnotation>
    MeasurementType: OntologyAnnotation option
    TechnologyType: OntologyAnnotation option
    TechnologyPlatform: OntologyAnnotation option
} with
    static member init = {
        Identifier = ""
        Title = ""
        Description = ""
        InvolvedPeople = ResizeArray()
        Comments = ResizeArray()
        MainText = ""
        Files = []
        Publications = ResizeArray()
        SubmissionDate = ""
        PublicReleaseDate = ""
        StudyDesignDescriptors = ResizeArray()
        MeasurementType = None
        TechnologyType = None
        TechnologyPlatform = None
    }

type LandingUiState = {
    ShowQuestions: bool
    SelectedTarget: ExperimentTarget option
    Error: string option
    IsSubmitting: bool
} with
    static member init = {
        ShowQuestions = false
        SelectedTarget = None
        Error = None
        IsSubmitting = false
    }

let private toOptionalString (value: string) =
    let trimmed = value.Trim()
    if String.IsNullOrWhiteSpace trimmed then None else Some trimmed

let toCreateRequest (draft: LandingDraft) (target: ExperimentTarget) : CreateExperimentRequest = {
    Metadata = {
        Identifier = toOptionalString draft.Identifier
        Title = draft.Title.Trim()
        Description = draft.Description.Trim()
        InvolvedPeople = draft.InvolvedPeople |> Seq.map (Person.toJsonString 0) |> Seq.toArray
        Comments = draft.Comments |> Seq.map (Comment.toJsonString 0) |> Seq.toArray
        MainText = toOptionalString draft.MainText
        Files = draft.Files |> List.toArray
        Publications = draft.Publications |> Seq.map (Publication.toJsonString 0) |> Seq.toArray
        SubmissionDate = toOptionalString draft.SubmissionDate
        PublicReleaseDate = toOptionalString draft.PublicReleaseDate
        StudyDesignDescriptors =
            draft.StudyDesignDescriptors
            |> Seq.map (OntologyAnnotation.toJsonString 0)
            |> Seq.toArray
        MeasurementType = draft.MeasurementType |> Option.map (OntologyAnnotation.toJsonString 0)
        TechnologyType = draft.TechnologyType |> Option.map (OntologyAnnotation.toJsonString 0)
        TechnologyPlatform = draft.TechnologyPlatform |> Option.map (OntologyAnnotation.toJsonString 0)
    }
    Target = target
}

let private isRequiredDataValid (draft: LandingDraft) =
    not (String.IsNullOrWhiteSpace draft.Title) && not (String.IsNullOrWhiteSpace draft.Description)

let private boxedHelperField (title: string) (content: ReactElement) =
    Html.fieldSet [
        prop.className "swt:fieldset"
        prop.children [
            Html.legend [
                prop.className "swt:fieldset-legend"
                prop.text title
            ]
            Html.div [
                prop.className "swt:rounded-box swt:border swt:border-base-300 swt:bg-base-100 swt:p-3"
                prop.children [ content ]
            ]
        ]
    ]

let private boxedHelperContent (content: ReactElement) =
    Html.div [
        prop.className "swt:rounded-box swt:border swt:border-base-300 swt:bg-base-100 swt:p-3"
        prop.children [ content ]
    ]

[<ReactComponent>]
let ExperimentLandingView
    (
        draft: LandingDraft,
        setDraft: LandingDraft -> unit,
        uiState: LandingUiState,
        setUiState: LandingUiState -> unit,
        onCreate: ExperimentTarget -> unit
    ) =

    let setError (value: string option) =
        setUiState { uiState with Error = value }

    let continueToQuestions () =
        if isRequiredDataValid draft then
            setUiState {
                uiState with
                    Error = None
                    ShowQuestions = true
            }
        else
            setError (Some "Title and Description are required.")

    let pickTarget target =
        setUiState {
            uiState with
                SelectedTarget = Some target
                Error = None
        }

    let createNow () =
        match uiState.SelectedTarget with
        | Some target ->
            if isRequiredDataValid draft then
                onCreate target
            else
                setError (Some "Title and Description are required.")
        | None -> setError (Some "Select Study or Assay before creating.")

    let targetSelectButton (label: string) (target: ExperimentTarget) =
        Html.button [
            prop.className [
                "swt:btn swt:flex-1"
                if uiState.SelectedTarget = Some target then
                    "swt:btn-primary"
                else
                    "swt:btn-outline"
            ]
            prop.onClick (fun _ -> pickTarget target)
            prop.text label
        ]

    Html.div [
        prop.className "swt:p-8 swt:flex swt:justify-center"
        prop.children [
            Html.div [
                prop.className "swt:w-full swt:max-w-3xl swt:rounded-box swt:border swt:border-base-300 swt:bg-base-200 swt:p-6 swt:space-y-4"
                prop.children [
                    Html.h2 [
                        prop.className "swt:text-3xl swt:font-bold swt:text-primary"
                        prop.text "Experiment Metadata"
                    ]
                    Html.p [
                        prop.className "swt:opacity-70"
                        prop.text "Only Title and Description are required. All other fields are optional."
                    ]
                    FormHelpers.TextInput(
                        draft.Identifier,
                        (fun v -> setDraft { draft with Identifier = v }),
                        label = "Identifier (Optional)",
                        placeholder = "Auto-generated if empty"
                    )
                    FormHelpers.TextInput(
                        draft.Title,
                        (fun v -> setDraft { draft with Title = v }),
                        label = "Title (Required)",
                        placeholder = "Experiment title"
                    )
                    FormHelpers.TextInput(
                        draft.Description,
                        (fun v -> setDraft { draft with Description = v }),
                        label = "Description (Required)",
                        isArea = true,
                        placeholder = "Experiment description"
                    )
                    boxedHelperField "Involved People" (
                        FormComponents.PersonsInput(
                            draft.InvolvedPeople,
                            (fun persons -> setDraft { draft with InvolvedPeople = persons })
                        )
                    )
                    boxedHelperField "Comments" (
                        FormComponents.CommentsInput(
                            draft.Comments,
                            (fun comments -> setDraft { draft with Comments = comments })
                        )
                    )
                    FormHelpers.TextInput(
                        draft.MainText,
                        (fun v -> setDraft { draft with MainText = v }),
                        label = "Main Text",
                        isArea = true,
                        placeholder = "Will be saved as protocol markdown"
                    )
                    Html.fieldSet [
                        prop.className "swt:fieldset"
                        prop.children [
                            Html.legend [
                                prop.className "swt:fieldset-legend"
                                prop.text "Files"
                            ]
                            Html.div [
                                prop.className "swt:border swt:border-dashed swt:border-base-content/30 swt:rounded-box swt:p-4 swt:bg-base-100"
                                prop.children [
                                    Html.p [
                                        prop.className "swt:opacity-70"
                                        prop.text "File picker is temporarily unavailable here. This placeholder will be connected once widgets are re-enabled."
                                    ]
                                ]
                            ]
                        ]
                    ]
                    Html.div [
                        prop.className "swt:flex swt:items-center swt:gap-3"
                        prop.children [
                            Html.button [
                                prop.className "swt:btn swt:btn-primary"
                                prop.onClick (fun _ -> continueToQuestions ())
                                prop.text "Continue"
                            ]
                            match uiState.Error with
                            | Some err ->
                                Html.span [
                                    prop.className "swt:text-error"
                                    prop.text err
                                ]
                            | None -> Html.none
                        ]
                    ]
                    if uiState.ShowQuestions then
                        Html.div [
                            prop.className "swt:mt-4 swt:rounded-box swt:border swt:border-base-300 swt:bg-base-100 swt:p-4 swt:space-y-3"
                            prop.children [
                                Html.h3 [
                                    prop.className "swt:text-lg swt:font-semibold"
                                    prop.text "Choose What To Create"
                                ]
                                Html.div [
                                    prop.className "swt:flex swt:gap-3"
                                    prop.children [
                                        targetSelectButton "Study" ExperimentTarget.Study
                                        targetSelectButton "Assay" ExperimentTarget.Assay
                                    ]
                                ]
                                match uiState.SelectedTarget with
                                | Some ExperimentTarget.Study ->
                                    Html.div [
                                        prop.className "swt:space-y-3"
                                        prop.children [
                                            boxedHelperContent (
                                                FormComponents.PublicationsInput(
                                                    draft.Publications,
                                                    (fun pubs -> setDraft { draft with Publications = pubs }),
                                                    label = "Publications"
                                                )
                                            )
                                            FormComponents.DateTimeInput(
                                                draft.SubmissionDate,
                                                (fun dateText -> setDraft { draft with SubmissionDate = dateText }),
                                                label = "Submission Date"
                                            )
                                            FormComponents.DateTimeInput(
                                                draft.PublicReleaseDate,
                                                (fun dateText -> setDraft { draft with PublicReleaseDate = dateText }),
                                                label = "Public Release Date"
                                            )
                                            boxedHelperContent (
                                                FormComponents.OntologyAnnotationsInput(
                                                    draft.StudyDesignDescriptors,
                                                    (fun descriptors -> setDraft { draft with StudyDesignDescriptors = descriptors }),
                                                    label = "Study Design Descriptors"
                                                )
                                            )
                                        ]
                                    ]
                                | Some ExperimentTarget.Assay ->
                                    Html.div [
                                        prop.className "swt:space-y-3"
                                        prop.children [
                                            FormComponents.OntologyAnnotationInput(
                                                draft.MeasurementType,
                                                (fun oa -> setDraft { draft with MeasurementType = oa }),
                                                label = "Measurement Type"
                                            )
                                            FormComponents.OntologyAnnotationInput(
                                                draft.TechnologyType,
                                                (fun oa -> setDraft { draft with TechnologyType = oa }),
                                                label = "Technology Type"
                                            )
                                            FormComponents.OntologyAnnotationInput(
                                                draft.TechnologyPlatform,
                                                (fun oa -> setDraft { draft with TechnologyPlatform = oa }),
                                                label = "Technology Platform"
                                            )
                                        ]
                                    ]
                                | None -> Html.none
                                Html.button [
                                    prop.className [
                                        "swt:btn swt:btn-secondary"
                                        if uiState.IsSubmitting then
                                            "swt:btn-disabled"
                                    ]
                                    prop.disabled uiState.IsSubmitting
                                    prop.onClick (fun _ -> createNow ())
                                    prop.text (
                                        match uiState.SelectedTarget with
                                        | Some ExperimentTarget.Study -> "Create Study"
                                        | Some ExperimentTarget.Assay -> "Create Assay"
                                        | None -> "Create"
                                    )
                                ]
                            ]
                        ]
                ]
            ]
        ]
    ]
