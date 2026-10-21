module Renderer.Components.MainContent.NotesDraftTarget

open Feliz
open Swate.Components.Shared
open Swate.Components.Composite.Notes.Editor
open Swate.Electron.Shared.FileIOHelper
open Swate.Electron.Shared.FileIOTypes
open Renderer.Components.Helper
open Renderer.Components.Helper.FileSystemHelper
open Renderer


[<ReactComponent>]
let NotesDraftTarget () =

    let notesDraft, setNotesDraft = React.useState NotesDraft.init
    let notesUiState, updateNotesUiState = React.useStateWithUpdater NotesUiState.init
    let setNotesUiState state = updateNotesUiState (fun _ -> state)

    let pendingOverwriteRequest, setPendingOverwriteRequest =
        React.useState<FileContentDTO option> None

    let pageStateCtx = Renderer.Context.PageStateContext.usePageStateCtx ()
    let fileStateCtx = Renderer.Context.FileStateContext.useFileStateCtx ()

    let availableNotesTargets =
        MainSyncedState.useMainSyncedState {
            initial = ResizeArray<ExistingTargetRef>()
            load =
                fun () -> promise {
                    match! Api.ipcArcVaultApi.getNoteTargets () with
                    | Ok targets -> return ResizeArray targets
                    | Error error -> return raise error
                }
            subscribe = fun _ -> ignore
            onError =
                fun error ->
                    updateNotesUiState (fun current -> {
                        current with
                            Error = Some $"Failed to load note targets: {error.Message}"
                    })
            // The targets come from the ARC model, which the main process waits for, so the page loads them
            // once when it opens instead of on every File Explorer update.
            dependencies = [||]
        }

    let setSubmitState isSubmitting error =
        setNotesUiState {
            notesUiState with
                IsSubmitting = isSubmitting
                Error = error
        }

    let writeRequest (request: FileContentDTO) = promise {
        let! writeResult =
            writeFileWithEnsuredChildFolder
                Api.ipcArcVaultApi.writeFile
                Api.ipcArcVaultApi.createFileSystemItem
                NoteConversion.tryGetNoteFolderRelativePath
                NoteConversion.noteAssetsFolderName
                request

        match writeResult with
        | Result.Error exn -> setSubmitState false (Some $"Failed to write note: {exn.Message}")
        | Ok() ->
            setPendingOverwriteRequest None

            let selectedPath = PathHelpers.normalizePath request.path

            fileStateCtx.setSelection (ArcSelection.forTreePath (Some selectedPath))
            setNotesDraft NotesDraft.init
            setNotesUiState NotesUiState.init

            let! previewResult = Api.ipcArcVaultApi.openFile request.path

            match previewResult with
            | Ok previewData ->
                let pageState = Renderer.Types.PageState.fromFileContentDTO previewData
                pageStateCtx.setState (Some pageState)
            | Result.Error _ ->
                let pageState = Renderer.Types.PageState.fromFileContentDTO request
                pageStateCtx.setState (Some pageState)
    }

    let submitRequest (overwrite: bool) (request: FileContentDTO) =
        promise {
            setSubmitState true None

            if overwrite then
                do! writeRequest request
            else
                let! targetAvailabilityResult = checkTargetAvailability Api.ipcArcVaultApi.pathExists request.path

                match targetAvailabilityResult with
                | Error exn -> setSubmitState false (Some $"Failed to check note target: {exn.Message}")
                | Ok TargetAvailability.Exists ->
                    setSubmitState false None
                    setPendingOverwriteRequest (Some request)
                | Ok TargetAvailability.Empty -> do! writeRequest request
        }
        |> Promise.catch (fun exn -> setSubmitState false (Some $"Failed to write note: {exn.Message}"))
        |> Promise.start

    let onSubmit =
        fun (payload: NotesSubmitPayload) ->
            let targetPath = PathHelpers.normalizePath payload.Intent.RelativePath

            let request: FileContentDTO =
                FileContentDTO.create
                    (FileContentDTO.inferTextFileTypeFromPath targetPath)
                    payload.Intent.Content
                    targetPath

            submitRequest false request

    React.Fragment [
        Notes.Wizard(notesDraft, setNotesDraft, notesUiState, setNotesUiState, onSubmit, availableNotesTargets.state)
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
