module Renderer.Components.MainContent.NotesDraftTarget

open Feliz
open Swate.Components.Landing
open Swate.Components.Notes.Editor
open Swate.Electron.Shared
open Swate.Electron.Shared.FileIOTypes
open Swate.Electron.Shared.FileIOHelper
open ARCtrl.Contract
open Renderer.Components.MainContent.Types
open Renderer.Types

module private NotesDraftTargetHelper =

    open System

    let tryGetExistingTargetRef (path: string) =
        let segments = getPathParts path

        let tryResolveTarget (folderName: string) (kind: NotesTargetKind) =
            match
                segments
                |> Array.tryFindIndex (fun segment ->
                    String.Equals(segment, folderName, StringComparison.OrdinalIgnoreCase)
                )
            with
            | Some index when index + 1 < segments.Length ->
                let name = segments.[index + 1].Trim()

                if String.IsNullOrWhiteSpace name then
                    None
                else
                    Some { Name = name; Kind = kind }
            | _ -> None

        match tryResolveTarget "studies" NotesTargetKind.Study with
        | Some target -> Some target
        | None -> tryResolveTarget "assays" NotesTargetKind.Assay

    let createAvailableNotesTargets (fileEntries: FileEntry[]) =
        fileEntries
        |> Seq.choose (fun entry -> tryGetExistingTargetRef entry.path)
        |> Seq.distinctBy (fun target -> target.Kind, target.Name)
        |> Seq.sortBy (fun target ->
            let kindOrder =
                match target.Kind with
                | NotesTargetKind.Study -> 0
                | NotesTargetKind.Assay -> 1

            kindOrder, target.Name.ToLowerInvariant()
        )
        |> ResizeArray

open NotesDraftTargetHelper

[<ReactComponent>]
let NotesDraftTarget () =

    let notesDraft, setNotesDraft = React.useState NotesDraft.init
    let notesUiState, setNotesUiState = React.useState NotesUiState.init
    let pageStateCtx = Renderer.Context.PageStateCtx.usePageState ()
    let fileStateCtx = Renderer.Context.FileStateCtx.useFileState ()

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

                let! writeResult = Api.ipcArcVaultApi.writeFile (unbox null) request

                match writeResult with
                | Result.Error exn ->
                    setNotesUiState {
                        notesUiState with
                            IsSubmitting = false
                            Error = Some $"Failed to write note: {exn.Message}"
                    }
                | Ok() ->

                    fileStateCtx.setSelectedTreeItemPath (Some payload.Intent.RelativePath)
                    setNotesDraft NotesDraft.init
                    setNotesUiState NotesUiState.init

                    let! previewResult = Api.ipcArcVaultApi.openFile (unbox null) payload.Intent.RelativePath

                    match previewResult with
                    | Ok previewData ->
                        let page = PageState.fromFileContentDTO previewData
                        pageStateCtx.setState (Some page)
                    | Result.Error _ -> pageStateCtx.setState (Some(PageState.TextPage payload.Intent.Content))
            }
            |> Promise.start

    Notes.Wizard(notesDraft, setNotesDraft, notesUiState, setNotesUiState, onSubmit, availableNotesTargets)