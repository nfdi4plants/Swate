module Renderer.components.ExperimentLanding

open System
open Feliz
open Swate.Components
open Swate.Electron.Shared.IPCTypes

type LandingDraft = {
    Identifier: string
    Title: string
    Description: string
    InvolvedPeople: string
    Comments: string
    MainText: string
    Files: string list
    Publications: string
    SubmissionDate: string
    PublicReleaseDate: string
    StudyDesignDescriptors: string
    MeasurementType: string
    TechnologyType: string
    TechnologyPlatform: string
} with
    static member init = {
        Identifier = ""
        Title = ""
        Description = ""
        InvolvedPeople = ""
        Comments = ""
        MainText = ""
        Files = []
        Publications = ""
        SubmissionDate = ""
        PublicReleaseDate = ""
        StudyDesignDescriptors = ""
        MeasurementType = ""
        TechnologyType = ""
        TechnologyPlatform = ""
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

let private splitMultiline (value: string) =
    value.Split([| '\n'; '\r' |], StringSplitOptions.RemoveEmptyEntries)
    |> Array.map (fun s -> s.Trim())
    |> Array.filter (String.IsNullOrWhiteSpace >> not)

let private toOptionalString (value: string) =
    let trimmed = value.Trim()
    if String.IsNullOrWhiteSpace trimmed then None else Some trimmed

let toCreateRequest (draft: LandingDraft) (target: ExperimentTarget) : CreateExperimentRequest = {
    Metadata = {
        Identifier = toOptionalString draft.Identifier
        Title = draft.Title.Trim()
        Description = draft.Description.Trim()
        InvolvedPeople = splitMultiline draft.InvolvedPeople
        Comments = splitMultiline draft.Comments
        MainText = toOptionalString draft.MainText
        Files = draft.Files |> List.toArray
        Publications = splitMultiline draft.Publications
        SubmissionDate = toOptionalString draft.SubmissionDate
        PublicReleaseDate = toOptionalString draft.PublicReleaseDate
        StudyDesignDescriptors = splitMultiline draft.StudyDesignDescriptors
        MeasurementType = toOptionalString draft.MeasurementType
        TechnologyType = toOptionalString draft.TechnologyType
        TechnologyPlatform = toOptionalString draft.TechnologyPlatform
    }
    Target = target
}

let private fieldTitle (title: string) =
    Html.legend [
        prop.className "swt:fieldset-legend"
        prop.text title
    ]

let private textInput (value: string) (setValue: string -> unit) (placeholder: string) =
    Html.input [
        prop.className "swt:input swt:input-bordered swt:w-full"
        prop.value value
        prop.placeholder placeholder
        prop.onChange setValue
    ]

let private textArea (value: string) (setValue: string -> unit) (placeholder: string) =
    Html.textarea [
        prop.className "swt:textarea swt:textarea-bordered swt:w-full swt:min-h-24"
        prop.value value
        prop.placeholder placeholder
        prop.onChange setValue
    ]

let private isRequiredDataValid (draft: LandingDraft) =
    not (String.IsNullOrWhiteSpace draft.Title) && not (String.IsNullOrWhiteSpace draft.Description)

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
                    Html.fieldSet [
                        prop.className "swt:fieldset"
                        prop.children [
                            fieldTitle "Identifier (Optional)"
                            textInput draft.Identifier (fun v -> setDraft { draft with Identifier = v }) "Auto-generated if empty"
                        ]
                    ]
                    Html.fieldSet [
                        prop.className "swt:fieldset"
                        prop.children [
                            fieldTitle "Title (Required)"
                            textInput draft.Title (fun v -> setDraft { draft with Title = v }) "Experiment title"
                        ]
                    ]
                    Html.fieldSet [
                        prop.className "swt:fieldset"
                        prop.children [
                            fieldTitle "Description (Required)"
                            textArea draft.Description (fun v -> setDraft { draft with Description = v }) "Experiment description"
                        ]
                    ]
                    Html.fieldSet [
                        prop.className "swt:fieldset"
                        prop.children [
                            fieldTitle "Involved People"
                            textArea
                                draft.InvolvedPeople
                                (fun v -> setDraft { draft with InvolvedPeople = v })
                                "One person per line"
                        ]
                    ]
                    Html.fieldSet [
                        prop.className "swt:fieldset"
                        prop.children [
                            fieldTitle "Comments"
                            textArea draft.Comments (fun v -> setDraft { draft with Comments = v }) "One comment per line"
                        ]
                    ]
                    Html.fieldSet [
                        prop.className "swt:fieldset"
                        prop.children [
                            fieldTitle "Main Text"
                            textArea draft.MainText (fun v -> setDraft { draft with MainText = v }) "Will be saved as protocol markdown"
                        ]
                    ]
                    Html.fieldSet [
                        prop.className "swt:fieldset"
                        prop.children [
                            fieldTitle "Files"
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
                                prop.text "Save"
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
                                            Html.fieldSet [
                                                prop.className "swt:fieldset"
                                                prop.children [
                                                    fieldTitle "Publications"
                                                    textArea
                                                        draft.Publications
                                                        (fun v -> setDraft { draft with Publications = v })
                                                        "One publication per line"
                                                ]
                                            ]
                                            Html.fieldSet [
                                                prop.className "swt:fieldset"
                                                prop.children [
                                                    fieldTitle "Submission Date"
                                                    textInput
                                                        draft.SubmissionDate
                                                        (fun v -> setDraft { draft with SubmissionDate = v })
                                                        "YYYY-MM-DD or free text"
                                                ]
                                            ]
                                            Html.fieldSet [
                                                prop.className "swt:fieldset"
                                                prop.children [
                                                    fieldTitle "Public Release Date"
                                                    textInput
                                                        draft.PublicReleaseDate
                                                        (fun v -> setDraft { draft with PublicReleaseDate = v })
                                                        "YYYY-MM-DD or free text"
                                                ]
                                            ]
                                            Html.fieldSet [
                                                prop.className "swt:fieldset"
                                                prop.children [
                                                    fieldTitle "Study Design Descriptors"
                                                    textArea
                                                        draft.StudyDesignDescriptors
                                                        (fun v -> setDraft { draft with StudyDesignDescriptors = v })
                                                        "One descriptor per line"
                                                ]
                                            ]
                                        ]
                                    ]
                                | Some ExperimentTarget.Assay ->
                                    Html.div [
                                        prop.className "swt:space-y-3"
                                        prop.children [
                                            Html.fieldSet [
                                                prop.className "swt:fieldset"
                                                prop.children [
                                                    fieldTitle "Measurement Type"
                                                    textInput
                                                        draft.MeasurementType
                                                        (fun v -> setDraft { draft with MeasurementType = v })
                                                        "Optional"
                                                ]
                                            ]
                                            Html.fieldSet [
                                                prop.className "swt:fieldset"
                                                prop.children [
                                                    fieldTitle "Technology Type"
                                                    textInput
                                                        draft.TechnologyType
                                                        (fun v -> setDraft { draft with TechnologyType = v })
                                                        "Optional"
                                                ]
                                            ]
                                            Html.fieldSet [
                                                prop.className "swt:fieldset"
                                                prop.children [
                                                    fieldTitle "Technology Platform"
                                                    textInput
                                                        draft.TechnologyPlatform
                                                        (fun v -> setDraft { draft with TechnologyPlatform = v })
                                                        "Optional"
                                                ]
                                            ]
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
