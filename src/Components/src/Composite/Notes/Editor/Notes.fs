namespace Swate.Components.Composite.Notes.Editor

open Fable.Core
open Feliz
open Browser.Dom
open Swate.Components.Shared
open Swate.Components.Composite.MarkdownText.Plugins
open Swate.Components.Primitive.BaseModal


[<Erase; Mangle(false)>]
type Notes =

    [<ReactComponent>]
    static member private ExistingTargetModal
        (
            isOpen: bool,
            close: unit -> unit,
            selectedTarget: ExistingTargetRef option,
            setSelectedTarget: ExistingTargetRef option -> unit,
            availableTargets: ResizeArray<ExistingTargetRef>,
            isSubmitting: bool,
            canSubmitDraft: bool,
            error: string option,
            submit: unit -> unit
        ) =

        let createInExistingText =
            match selectedTarget |> Option.map _.Kind with
            | Some NotesTargetKind.Study -> "Create in Study"
            | Some NotesTargetKind.Assay -> "Create in Assay"
            | None -> "Create in Existing Target"

        let setClose isOpen =
            if not isOpen then
                close ()

        let createInExistingDisabled = isSubmitting || selectedTarget.IsNone || not canSubmitDraft

        let footer =
            Html.div [
                prop.className "swt:flex swt:gap-2 swt:justify-end swt:w-full"
                prop.children [
                    Html.button [
                        prop.className "swt:btn swt:btn-ghost"
                        prop.disabled isSubmitting
                        prop.onClick (fun _ -> close ())
                        prop.text "Cancel"
                    ]
                    Html.button [
                        prop.testId "notes-create-existing-button"
                        prop.className [
                            "swt:btn swt:btn-primary"
                            if createInExistingDisabled then
                                "swt:btn-disabled"
                        ]
                        prop.disabled createInExistingDisabled
                        prop.onClick (fun _ ->
                            if not createInExistingDisabled then
                                submit ()
                        )
                        prop.text createInExistingText
                    ]
                ]
            ]

        BaseModal.Modal(
            isOpen = isOpen,
            setIsOpen = setClose,
            header = Html.text "Existing Target",
            children =
                React.Fragment [
                    TargetSelector.Main(selectedTarget, setSelectedTarget, availableTargets, isSubmitting)
                    match error with
                    | Some message -> Html.span [ prop.className "swt:text-error"; prop.text message ]
                    | None -> Html.none
                ],
            footer = footer,
            debug = "notes-existing-target"
        )

    [<ReactComponent>]
    static member Wizard
        (
            draft: NotesDraft,
            setDraft: NotesDraft -> unit,
            uiState: NotesUiState,
            setUiState: NotesUiState -> unit,
            onSubmit: NotesSubmitPayload -> unit,
            availableExistingTargets: ResizeArray<ExistingTargetRef>,
            ?filePickerAdapter: MarkdownFilePickerAdapter
        ) =

        let setError (value: string option) =
            setUiState (State.setError value uiState)

        let submitRequirements = NoteConversion.tryResolvePayloadRequirements draft
        let canSubmitDraft = submitRequirements.IsSome

        let submitPayload onSuccess =
            function
            | Error message -> setError (Some message)
            | Ok payload ->
                onSuccess ()
                onSubmit payload

        let setExistingTargetSelector isOpen =
            setUiState {
                uiState with
                    ShowExistingTargetSelector = isOpen
                    Error = None
            }

        let openExistingTargetSelector () =
            match submitRequirements with
            | None -> ()
            | Some _ ->
                let selectedTarget =
                    draft.SelectedExistingTarget
                    |> Option.bind (fun targetRef -> availableExistingTargets |> Seq.tryFind ((=) targetRef))
                    |> Option.orElseWith (fun () -> availableExistingTargets |> Seq.tryHead)

                if draft.SelectedExistingTarget <> selectedTarget then
                    setDraft {
                        draft with
                            SelectedExistingTarget = selectedTarget
                    }

                setExistingTargetSelector true

        let submitToExisting () =
            match draft.SelectedExistingTarget, submitRequirements with
            | Some targetRef, Some requirements ->
                NoteConversion.tryCreateExistingTargetPayload targetRef requirements draft
                |> submitPayload (fun () -> setExistingTargetSelector false)
            | _ -> ()

        let submitNewRootNote () =
            match submitRequirements with
            | None -> ()
            | Some requirements ->
                NoteConversion.tryCreateNewRootNotePayload requirements draft |> submitPayload ignore

        Html.div [
            prop.className "swt:p-8 swt:flex swt:justify-center swt:overflow-y-auto"
            prop.children [
                Html.div [
                    prop.className
                        "swt:w-full swt:max-w-4xl swt:rounded-box swt:border swt:border-base-300 swt:bg-base-200 swt:p-6 swt:space-y-4 swt:h-fit"
                    prop.children [
                        Html.h2 [
                            prop.className "swt:text-3xl swt:font-bold swt:text-primary"
                            prop.text "Notes"
                        ]
                        NoteFormFields.Main(draft, setDraft, filePickerAdapter)
                        Actions.Main(
                            uiState.IsSubmitting,
                            canSubmitDraft,
                            openExistingTargetSelector,
                            submitNewRootNote,
                            uiState.Error
                        )
                        Notes.ExistingTargetModal(
                            uiState.ShowExistingTargetSelector,
                            (fun () -> setExistingTargetSelector false),
                            draft.SelectedExistingTarget,
                            (fun target ->
                                setDraft {
                                    draft with
                                        SelectedExistingTarget = target
                                }
                            ),
                            availableExistingTargets,
                            uiState.IsSubmitting,
                            canSubmitDraft,
                            uiState.Error,
                            submitToExisting
                        )
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
