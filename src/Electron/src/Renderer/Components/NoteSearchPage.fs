module Renderer.Components.NoteSearchPage


open System
open Feliz
open Swate.Components
open Swate.Components.Shared
open Swate.Components.NoteTypes
open Swate.Components.Notes.Editor
open Swate.Electron.Shared
open Swate.Electron.Shared.FileIOTypes
open Swate.Electron.Shared.IPCTypes.IPCTypesHelper


let private normalizePath = PathHelpers.normalizeSeparators

let private tryResolveArcRelativePath (appState: AppState) (relativePath: string) =
    match appState with
    | AppState.ARC arcPath ->
        let root = normalizePath arcPath |> fun path -> path.TrimEnd('/')
        let normalizedRelative = normalizePath relativePath |> fun path -> path.TrimStart('/')

        if String.IsNullOrWhiteSpace normalizedRelative then
            None
        else
            Some $"{root}/{normalizedRelative}"
    | AppState.Init -> None

let private tryGetExistingTargetRef (path: string) =
    let segments = (normalizePath path).Split('/', StringSplitOptions.RemoveEmptyEntries)

    let tryResolveTarget (folderName: string) (kind: NotesTargetKind) =
        match
            segments
            |> Array.tryFindIndex (fun segment -> String.Equals(segment, folderName, StringComparison.OrdinalIgnoreCase))
        with
        | Some index when index + 1 < segments.Length ->
            let name = segments.[index + 1].Trim()

            if String.IsNullOrWhiteSpace name then
                None
            else
                Some {
                    Name = name
                    Kind = kind
                }
        | _ -> None

    match tryResolveTarget "studies" NotesTargetKind.Study with
    | Some target -> Some target
    | None -> tryResolveTarget "assays" NotesTargetKind.Assay

let createAvailableNotesTargets (fileEntries: FileEntry list) =
    fileEntries
    |> Seq.choose (fun entry -> tryGetExistingTargetRef entry.path)
    |> Seq.distinctBy (fun target -> target.Kind, target.Name)
    |> Seq.sortBy (fun target ->
        let kindOrder =
            match target.Kind with
            | NotesTargetKind.Study -> 0
            | NotesTargetKind.Assay -> 1

        kindOrder, target.Name.ToLowerInvariant ()
    )
    |> ResizeArray

[<ReactComponent>]
let CreateNotesSearchPage
    (
        appState: AppState,
        fileTree: FileEntry list,
        setSelectedTreeItemPath: string option -> unit,
        setPreviewData: PageState option -> unit
    ) =

    let notes, setNotes = React.useState ([]: NoteSearch list)
    let isLoading, setIsLoading = React.useState true
    let error, setError = React.useState (None: string option)

    React.useEffect (
        (fun () ->
            let mutable isDisposed = false

            setIsLoading true
            setError None

            promise {
                let! result = Api.ipcArcVaultApi.readNotes (unbox null)

                if not isDisposed then
                    match result with
                    | Ok nextNotes ->
                        setNotes (nextNotes |> Array.toList)
                        setIsLoading false
                    | Result.Error exn ->
                        setNotes []
                        setError (Some $"Failed to load notes: {exn.Message}")
                        setIsLoading false
            }
            |> Promise.start

            fun () -> isDisposed <- true
        ),
        [| box appState |]
    )

    let openNote (relativePath: string) =
        match tryResolveArcRelativePath appState relativePath with
        | None ->
            setPreviewData (Some(PageState.Error $"Could not resolve note path '{relativePath}'."))
        | Some absolutePath ->
            promise {
                setSelectedTreeItemPath (Some absolutePath)

                let! result = Api.ipcArcVaultApi.openFile (unbox null) absolutePath

                match result with
                | Ok previewData -> setPreviewData (Some previewData)
                | Result.Error exn ->
                    setPreviewData (Some(PageState.Error $"Could not open note: {exn.Message}"))
            }
            |> Promise.start

    SearchComponent.Main(notes, isLoading, error, openNote)

let createFromNotes
    (notesUiState: NotesUiState)
    (setNotesUiState: NotesUiState -> unit)
    (setNotesDraft: NotesDraft -> unit)
    appState
    setSelectedTreeItemPath
    setPreviewData
    (payload: NotesSubmitPayload)
    =
    promise {
        setNotesUiState {
            notesUiState with
                IsSubmitting = true
                Error = None
        }

        let request: WriteFileRequest = {
            RelativePath = payload.Intent.RelativePath
            Content = payload.Intent.Content
        }

        let! writeResult = Api.ipcArcVaultApi.writeFile (unbox null) request

        match writeResult with
        | Result.Error exn ->
            setNotesUiState {
                notesUiState with
                    IsSubmitting = false
                    Error = Some $"Failed to write note: {exn.Message}"
            }
        | Ok() ->
            let previewPath = tryResolveArcRelativePath appState payload.Intent.RelativePath

            previewPath |> setSelectedTreeItemPath
            setNotesDraft NotesDraft.init
            setNotesUiState NotesUiState.init

            match previewPath with
            | Some absolutePath ->
                let! previewResult = Api.ipcArcVaultApi.openFile (unbox null) absolutePath

                match previewResult with
                | Ok previewData -> setPreviewData (Some previewData)
                | Result.Error _ -> setPreviewData (Some(PageState.Text payload.Intent.Content))
            | None -> setPreviewData (Some(PageState.Text payload.Intent.Content))
    }
    |> Promise.start

