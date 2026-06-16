module Renderer.Components.MainContent.NotesDraftTarget

open Feliz
open Swate.Components.Shared
open Swate.Components.Composite.Notes.Editor
open Swate.Electron.Shared.FileIOHelper
open Swate.Electron.Shared.FileIOTypes
open Renderer.Components.Helper
open Renderer.Components.Helper.FileSystemHelper


[<ReactComponent>]
let NotesDraftTarget () =

    let notesDraft, setNotesDraft = React.useState NotesDraft.init
    let notesUiState, setNotesUiState = React.useState NotesUiState.init

    let pendingOverwriteRequest, setPendingOverwriteRequest =
        React.useState<FileContentDTO option> None

    let pageStateCtx = Renderer.Context.PageStateContext.usePageStateCtx ()
    let fileStateCtx = Renderer.Context.FileStateContext.useFileStateCtx ()

    let availableNotesTargets =
        React.useMemo (
            (fun _ -> createAvailableNotesTargets fileStateCtx.state.FileTree),
            [| box fileStateCtx.state.FileTree |]
        )

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
        Notes.Wizard(notesDraft, setNotesDraft, notesUiState, setNotesUiState, onSubmit, availableNotesTargets)
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
