module Renderer.Components.MainContent.NotesDraftTarget

open Feliz
open Swate.Components.Landing
open Swate.Components.Notes.Editor
open Swate.Components.Shared
open Swate.Electron.Shared
open Swate.Electron.Shared.FileIOTypes
open Swate.Electron.Shared.FileIOHelper
open ARCtrl.Contract
open Renderer.Components.MainContent.Types

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

    let onSubmit =
        fun (payload: NotesSubmitPayload) ->
            promise {
                setNotesUiState {
                    notesUiState with
                        IsSubmitting = true
                        Error = None
                }

                let request: FileContentDTO =
                    FileContentDTO.create DTOType.PlainText payload.Intent.Content payload.Intent.RelativePath

                let! writeResult = Api.ipcArcVaultApi.writeFile request

                match writeResult with
                | Result.Error exn ->
                    setNotesUiState {
                        notesUiState with
                            IsSubmitting = false
                            Error = Some $"Failed to write note: {exn.Message}"
                    }
                | Ok() ->
                    let selectedPath = normalizePath payload.Intent.RelativePath

                    fileStateCtx.setSelection (ArcSelection.forTreePath (Some selectedPath))
                    setNotesDraft NotesDraft.init
                    setNotesUiState NotesUiState.init

                    let! previewResult = Api.ipcArcVaultApi.openFile payload.Intent.RelativePath

                    match previewResult with
                    | Ok previewData ->
                        previewData
                        |> Renderer.Components.ARCHelper.viewLoadResultOfDto
                        |> Renderer.Components.ARCHelper.applyLoadedView pageStateCtx.setState
                    | Result.Error _ ->
                        FileContentDTO.create DTOType.PlainText payload.Intent.Content payload.Intent.RelativePath
                        |> Renderer.Components.ARCHelper.viewLoadResultOfDto
                        |> Renderer.Components.ARCHelper.applyLoadedView pageStateCtx.setState
            }
            |> Promise.start

    Notes.Wizard(notesDraft, setNotesDraft, notesUiState, setNotesUiState, onSubmit, availableNotesTargets)
