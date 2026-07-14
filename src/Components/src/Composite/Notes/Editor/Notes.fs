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
            close: unit -> unit,
            availableTargets: ResizeArray<ExistingTargetRef>,
            isSubmitting: bool,
            error: string option,
            submit: ExistingTargetRef -> unit
        ) =

        let selectedTarget, setSelectedTarget =
            React.useState<ExistingTargetRef option> None

        let currentSelectedTarget =
            selectedTarget
            |> Option.bind (fun target -> availableTargets |> Seq.tryFind ((=) target))
            |> Option.orElseWith (fun () -> availableTargets |> Seq.tryHead)

        let createInExistingText =
            match currentSelectedTarget |> Option.map _.Kind with
            | Some NotesTargetKind.Study -> "Create in Study"
            | Some NotesTargetKind.Assay -> "Create in Assay"
            | None -> "Create in Existing Target"

        let setClose isOpen =
            if not isOpen then
                close ()

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
                            if isSubmitting || currentSelectedTarget.IsNone then
                                "swt:btn-disabled"
                        ]
                        prop.disabled (isSubmitting || currentSelectedTarget.IsNone)
                        prop.onClick (fun _ -> currentSelectedTarget |> Option.iter submit)
                        prop.text createInExistingText
                    ]
                ]
            ]

        BaseModal.Modal(
            isOpen = true,
            setIsOpen = setClose,
            header = Html.text "Existing Target",
            children =
                React.Fragment [
                    TargetSelector.Main(currentSelectedTarget, setSelectedTarget, availableTargets, isSubmitting)
                    match error with
                    | Some message -> Html.span [ prop.className "swt:text-error"; prop.text message ]
                    | None -> Html.none
                ],
            footer = footer,
            debug = "notes-existing-target"
        )

    [<ReactComponent(true)>]
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

        let createPayload (target: NotesTarget) (relativePath: string) =
            match draft.DateCreated with
            | None -> setError (Some "Date Created is required.")
            | Some dateCreated ->
                let content = NoteConversion.formatMarkdown draft

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

        let setExistingTargetSelector isOpen =
            setUiState {
                uiState with
                    ShowExistingTargetSelector = isOpen
                    Error = None
            }

        let openExistingTargetSelector () = setExistingTargetSelector true

        let submitToExisting targetRef =
            if Validation.isRequiredDataValid draft |> not then
                setError (Some "Please enter a Title and a Date Created value before submitting.")
            else
                match draft.DateCreated with
                | None -> setError (Some "Date Created is required.")
                | Some _ ->
                    match NoteConversion.resolveProtocolName draft with
                    | None -> setError (Some "Title is invalid for protocol naming. Choose a different title.")
                    | Some protocolName ->
                        match NoteConversion.mkExistingTargetRelativePath targetRef protocolName with
                        | None -> setError (Some "Could not resolve a safe target path.")
                        | Some relativePath ->
                            setExistingTargetSelector false
                            createPayload (NotesTarget.ExistingTarget targetRef) relativePath

        let submitNewRootNote () =
            if Validation.isRequiredDataValid draft |> not then
                setError (Some "Please enter a Title and a Date Created value before submitting.")
            else
                match draft.DateCreated with
                | None -> setError (Some "Date Created is required.")
                | Some dateCreated ->
                    match NoteConversion.resolveProtocolName draft with
                    | None -> setError (Some "Title is invalid for protocol naming. Choose a different title.")
                    | Some protocolName ->
                        match NoteConversion.mkNewRootNoteRelativePath dateCreated protocolName with
                        | None -> setError (Some "Could not resolve a safe note path.")
                        | Some relativePath -> createPayload NotesTarget.NewRootNote relativePath

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
                        Actions.Main(uiState.IsSubmitting, openExistingTargetSelector, submitNewRootNote, uiState.Error)
                        if uiState.ShowExistingTargetSelector then
                            Notes.ExistingTargetModal(
                                (fun () -> setExistingTargetSelector false),
                                availableExistingTargets,
                                uiState.IsSubmitting,
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
