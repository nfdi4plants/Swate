module Renderer.Components.MainContent.NotesDraftTarget

open Feliz
open Renderer.Context.UnsavedChangesContext
open Swate.Components.Shared
open Swate.Components.Composite.Notes.Editor
open Swate.Electron.Shared.FileIOTypes
open Renderer.Components.Helper
open Renderer.Components.Helper.FileSystemHelper

module MainContentHelper = Renderer.Components.MainContent.Helper

[<ReactComponent>]
let NotesDraftTarget () =

    let notesDraft, setNotesDraft = React.useState NotesDraft.init

    let notesUiState, updateNotesUiState = React.useStateWithUpdater NotesUiState.init

    let setNotesUiState nextUiState =
        updateNotesUiState (fun _ -> nextUiState)

    let pendingOverwriteRequest, setPendingOverwriteRequest =
        React.useState<FileContentDTO option> None

    let pendingImageAssetsRef = React.useRef ([]: ExternalAssetLink list)

    let pageStateCtx = Renderer.Context.PageStateContext.usePageStateCtx ()
    let fileStateCtx = Renderer.Context.FileStateContext.useFileStateCtx ()

    let unsavedChangesCtx =
        Renderer.Context.UnsavedChangesContext.useUnsavedChangesCtx ()

    let hasUnsavedDraft = State.hasUnsavedDraft notesDraft

    let tryCreateDraftPayload () =
        match NoteConversion.PayloadRequirements.tryResolve (notesDraft) with
        | None -> Error "Please enter a Title that can be used as a protocol name before saving."
        | Some(dateCreated, protocolName) ->
            NoteConversion.PayloadRequirements.tryCreateNewRootNotePayload (dateCreated, protocolName, notesDraft)

    let draftUnsavedPath =
        if hasUnsavedDraft then
            match tryCreateDraftPayload () with
            | Ok payload -> Some(PathHelpers.normalizePath payload.Intent.RelativePath)
            | Error _ -> Some($"{NoteConversion.notesRootFolder}/.draft.md")
        else
            None

    let imageFilePickerAdapter =
        React.useMemo (
            (fun _ ->
                createAssetFilePickerAdapter
                    Api.ipcArcVaultApi.pickExternalFilePaths
                    NoteConversion.noteAssetsFolderName
                    (fun asset -> pendingImageAssetsRef.current <- pendingImageAssetsRef.current @ [ asset ])
            ),
            [||]
        )

    let availableNotesTargets =
        React.useMemo (
            (fun _ -> MainContentHelper.availableExistingNoteTargets fileStateCtx.state.FileTree),
            [| box fileStateCtx.state.FileTree |]
        )

    let setSubmitState isSubmitting error =
        updateNotesUiState (fun current ->
            current
            |> (if isSubmitting then
                    State.startSubmitting
                else
                    State.stopSubmitting)
            |> State.setError error
        )

    let writeRequest (request: FileContentDTO) = promise {
        let! writeResult = MainContentHelper.writeNoteMarkdownFile request pendingImageAssetsRef.current

        match writeResult with
        | Result.Error error ->
            let message = $"Failed to write note: {error.Message}"
            setSubmitState false (Some message)
            return Error(exn message)
        | Ok() ->
            setPendingOverwriteRequest None
            pendingImageAssetsRef.current <- []

            let selectedPath = PathHelpers.normalizePath request.path

            fileStateCtx.setSelection (ArcSelection.forTreePath (Some selectedPath))
            setNotesDraft NotesDraft.init
            setNotesUiState NotesUiState.init

            let! previewResult = Api.ipcArcVaultApi.openFile request.path

            let pageState =
                match previewResult with
                | Ok previewData -> Renderer.Types.PageState.fromFileContentDTO previewData
                | Result.Error _ -> Renderer.Types.PageState.fromFileContentDTO request

            do! unsavedChangesCtx.RunWithoutGuard(fun () -> promise { pageStateCtx.setState (Some pageState) })

            return Ok()
    }

    let submitRequestAsync (overwrite: bool) (request: FileContentDTO) = promise {
        try
            setSubmitState true None

            if overwrite then
                return! writeRequest request
            else
                let! targetAvailabilityResult = checkTargetAvailability Api.ipcArcVaultApi.pathExists request.path

                match targetAvailabilityResult with
                | Error error ->
                    let message = $"Failed to check note target: {error.Message}"
                    setSubmitState false (Some message)
                    return Error(exn message)
                | Ok TargetAvailability.Exists ->
                    let message = $"A note already exists at '{request.path}'."
                    setPendingOverwriteRequest (Some request)
                    setSubmitState false None
                    return Error(UnsavedChangesSaveError.hide (exn message))
                | Ok TargetAvailability.Empty -> return! writeRequest request
        with error ->
            let message = $"Failed to write note: {error.Message}"
            setSubmitState false (Some message)
            return Error(exn message)
    }

    let submitRequest (overwrite: bool) (request: FileContentDTO) =
        promise {
            let! _ = submitRequestAsync overwrite request
            return ()
        }
        |> Promise.start

    let onSubmit =
        fun (payload: NotesSubmitPayload) -> submitRequest false (MainContentHelper.requestFromNotesPayload payload)

    let saveDraftAsync () =
        match tryCreateDraftPayload () with
        | Error message ->
            setSubmitState false (Some message)
            promise { return Error(exn message) }
        | Ok payload -> submitRequestAsync false (MainContentHelper.requestFromNotesPayload payload)

    useUnsavedChangesGuard (UnsavedChangesGuard.note draftUnsavedPath saveDraftAsync (fun () -> hasUnsavedDraft))

    React.Fragment [
        Notes.Wizard(
            notesDraft,
            setNotesDraft,
            notesUiState,
            setNotesUiState,
            onSubmit,
            availableNotesTargets,
            filePickerAdapter = imageFilePickerAdapter
        )
        FileTargetConflictModal.Main(
            isOpen = pendingOverwriteRequest.IsSome,
            targetPath = (pendingOverwriteRequest |> Option.map _.path),
            close =
                (fun () ->
                    setPendingOverwriteRequest None
                    setSubmitState false None
                ),
            overwrite = (fun () -> pendingOverwriteRequest |> Option.iter (submitRequest true)),
            isBusy = notesUiState.IsSubmitting
        )
    ]
