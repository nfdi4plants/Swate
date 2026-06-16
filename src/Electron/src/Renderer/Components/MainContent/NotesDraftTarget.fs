module Renderer.Components.MainContent.NotesDraftTarget

open Feliz
open Swate.Components.Shared
open Swate.Components.Composite.Notes.Editor
open Swate.Electron.Shared.FileIOHelper
open Swate.Electron.Shared.FileIOTypes
open Renderer.Components.Helper.NoteFileSystemHelper


[<ReactComponent>]
let NotesDraftTarget () =

    let notesDraft, setNotesDraft = React.useState NotesDraft.init
    let notesUiState, setNotesUiState = React.useState NotesUiState.init
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

    let submitRequest (request: FileContentDTO) =
        promise {
            setSubmitState true None

            let! writeResult = writeNoteWithAssets request

            match writeResult with
            | Result.Error exn -> setSubmitState false (Some $"Failed to write note: {exn.Message}")
            | Ok() ->
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

            submitRequest request

    Notes.Wizard(notesDraft, setNotesDraft, notesUiState, setNotesUiState, onSubmit, availableNotesTargets)
