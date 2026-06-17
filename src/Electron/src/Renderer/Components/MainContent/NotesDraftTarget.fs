module Renderer.Components.MainContent.NotesDraftTarget

open Feliz
open Swate.Components.Shared
open Swate.Components.Composite.Notes.Editor
open Swate.Components.Primitive.ErrorModal.Context
open Swate.Electron.Shared.FileIOTypes
open Swate.Electron.Shared.FileIOHelper
open Renderer.Components.MainContent.NoteTargetConflictHelper


[<ReactComponent>]
let NotesDraftTarget () =

    let notesDraft, setNotesDraft = React.useState NotesDraft.init
    let notesUiState, setNotesUiState = React.useState NotesUiState.init
    let pageStateCtx = Renderer.Context.PageStateContext.usePageStateCtx ()
    let fileStateCtx = Renderer.Context.FileStateContext.useFileStateCtx ()
    let errorModalCtx = useErrorModalCtx ()

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

    let submitRequest (payload: NotesSubmitPayload) (requestFileType: FileContentType) (request: FileContentDTO) =
        promise {
            setSubmitState true None

            let! writeResult = Api.ipcArcVaultApi.writeFile request

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
                    let fallbackData = FileContentDTO.create requestFileType payload.Intent.Content request.path

                    let pageState = Renderer.Types.PageState.fromFileContentDTO fallbackData
                    pageStateCtx.setState (Some pageState)
        }
        |> Promise.catch (fun exn -> setSubmitState false (Some $"Failed to write note: {exn.Message}"))
        |> Promise.start

    let onSubmit =
        fun (payload: NotesSubmitPayload) ->
            let targetPath = PathHelpers.normalizePath payload.Intent.RelativePath

            let requestFileType = FileContentDTO.inferTextFileTypeFromPath targetPath

            let request: FileContentDTO =
                FileContentDTO.create requestFileType payload.Intent.Content targetPath

            let submit () = submitRequest payload requestFileType request

            promise {
                setSubmitState true None

                let targetExistsInSnapshot =
                    PathHelpers.pathExistsInSnapshot (fileStateCtx.state.FileTree |> Array.map _.path) targetPath

                if targetExistsInSnapshot then
                    setSubmitState false None
                    showOverwriteConflictModal errorModalCtx targetPath submit
                else
                    let! targetExists = targetExistsOnDisk targetPath

                    if targetExists then
                        setSubmitState false None
                        showOverwriteConflictModal errorModalCtx targetPath submit
                    else
                        submit ()
            }
            |> Promise.catch (fun exn -> setSubmitState false (Some $"Failed to check target note: {exn.Message}"))
            |> Promise.start

    Notes.Wizard(notesDraft, setNotesDraft, notesUiState, setNotesUiState, onSubmit, availableNotesTargets)
