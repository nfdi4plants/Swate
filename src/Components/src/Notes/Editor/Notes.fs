namespace Swate.Components.Notes.Editor

open Fable.Core
open Feliz
open Browser.Dom

[<Erase; Mangle(false)>]
type Notes =

    [<ReactComponent>]
    static member Wizard
        (
            draft: NotesDraft,
            setDraft: NotesDraft -> unit,
            uiState: NotesUiState,
            setUiState: NotesUiState -> unit,
            onSubmit: NotesSubmitPayload -> unit,
            availableExistingTargets: ResizeArray<ExistingTargetRef>
        ) =

        let setError (value: string option) =
            setUiState (State.setError value uiState)

        let createPayload (target: NotesTarget) (relativePath: string) =
            match draft.DateCreated with
            | None ->
                setError (Some "Date Created is required.")
            | Some dateCreated ->
                let content = Conversion.formatMarkdown draft

                let payload = {
                    Intent = {
                        RelativePath = relativePath
                        Content = content
                        Target = target
                    }
                    Title = draft.Title.Trim()
                    DateCreated = dateCreated
                    Tags = draft.Tags |> Seq.toList
                }

                onSubmit payload

        let toggleExistingTargetSelector () =
            uiState
            |> State.toggleExistingTargetSelector
            |> setUiState

        let submitToExisting () =
            if Validation.isRequiredDataValid draft |> not then
                setError (Some "Please enter a Title and a Date Created value before submitting.")
            else
                match draft.SelectedExistingTarget with
                | None ->
                    setError (Some "Select a Study or Assay target first.")
                | Some targetRef ->
                    match draft.DateCreated with
                    | None ->
                        setError (Some "Date Created is required.")
                    | Some dateCreated ->
                        match Conversion.resolveProtocolName draft with
                        | None ->
                            setError (Some "Title is invalid for protocol naming. Choose a different title.")
                        | Some protocolName ->
                            match Conversion.mkExistingTargetRelativePath targetRef dateCreated protocolName with
                            | None ->
                                setError (Some "Could not resolve a safe target path.")
                            | Some relativePath ->
                                createPayload (NotesTarget.ExistingTarget targetRef) relativePath

        let submitNewRootNote () =
            if Validation.isRequiredDataValid draft |> not then
                setError (Some "Please enter a Title and a Date Created value before submitting.")
            else
                match draft.DateCreated with
                | None ->
                    setError (Some "Date Created is required.")
                | Some dateCreated ->
                    match Conversion.resolveProtocolName draft with
                    | None ->
                        setError (Some "Title is invalid for protocol naming. Choose a different title.")
                    | Some protocolName ->
                        match Conversion.mkNewRootNoteRelativePath dateCreated protocolName with
                        | None ->
                            setError (Some "Could not resolve a safe note path.")
                        | Some relativePath ->
                            createPayload NotesTarget.NewRootNote relativePath

        Html.div [
            prop.className "swt:p-8 swt:flex swt:justify-center"
            prop.children [
                Html.div [
                    prop.className "swt:w-full swt:max-w-4xl swt:rounded-box swt:border swt:border-base-300 swt:bg-base-200 swt:p-6 swt:space-y-4"
                    prop.children [
                        Html.h2 [
                            prop.className "swt:text-3xl swt:font-bold swt:text-primary"
                            prop.text "Notes"
                        ]
                        NoteFormFields.Main(draft, setDraft)
                        Actions.Main(
                            uiState.IsSubmitting,
                            uiState.ShowExistingTargetSelector,
                            toggleExistingTargetSelector,
                            submitNewRootNote,
                            uiState.Error
                        )
                        if uiState.ShowExistingTargetSelector then
                            let createInExistingText =
                                match draft.SelectedExistingTarget |> Option.map _.Kind with
                                | Some NotesTargetKind.Study -> "Create in Study"
                                | Some NotesTargetKind.Assay -> "Create in Assay"
                                | None -> "Create in Existing Target"

                            Html.div [
                                prop.className "swt:mt-4 swt:rounded-box swt:border swt:border-base-300 swt:bg-base-100 swt:p-4 swt:space-y-3"
                                prop.children [
                                    Html.h3 [
                                        prop.className "swt:text-lg swt:font-semibold"
                                        prop.text "Existing Target"
                                    ]
                                    TargetSelector.Main(
                                        draft.SelectedExistingTarget,
                                        (fun target -> setDraft { draft with SelectedExistingTarget = target }),
                                        availableExistingTargets,
                                        uiState.IsSubmitting
                                    )
                                    Html.button [
                                        prop.testId "notes-create-existing-button"
                                        prop.className [
                                            "swt:btn swt:btn-primary"
                                            if uiState.IsSubmitting || draft.SelectedExistingTarget.IsNone then
                                                "swt:btn-disabled"
                                        ]
                                        prop.disabled (uiState.IsSubmitting || draft.SelectedExistingTarget.IsNone)
                                        prop.onClick (fun _ -> submitToExisting ())
                                        prop.text createInExistingText
                                    ]
                                ]
                            ]
                    ]
                ]
            ]
        ]

    [<ReactComponent>]
    static member Entry() =
        let draft, setDraft = React.useState NotesDraft.init
        let uiState, setUiState = React.useState NotesUiState.init

        let onSubmit (payload: NotesSubmitPayload) =
            console.log ("Notes submit payload", payload)

        let availableTargets =
            ResizeArray [
                {
                    Name = "DemoStudy"
                    Kind = NotesTargetKind.Study
                }
                {
                    Name = "DemoAssay"
                    Kind = NotesTargetKind.Assay
                }
            ]

        Notes.Wizard(draft, setDraft, uiState, setUiState, onSubmit, availableTargets)
