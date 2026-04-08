module Renderer.Components.MainContent.NotesSearchTarget

open Feliz
open Feliz
open Swate.Components
open Swate.Components.NoteTypes
open Swate.Electron.Shared.FileIOHelper
open Renderer

[<ReactComponent>]
let NotesSearchTarget () =

    let pageCtx = Renderer.Context.PageStateCtx.usePageState ()
    let fileTreeCtx = Renderer.Context.FileStateCtx.useFileState ()
    let errorModal = Contexts.ErrorModal.useErrorModal ()
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
        [||]
    )

    let openNote (relativePath: string) =
        promise {

            let! result = Api.ipcArcVaultApi.openFile (unbox null) relativePath

            match result with
            | Ok dto ->
                match dto.fileType with
                | DTOType.DTOTypeIsPlainTextVariant ->
                    pageCtx.setState (Some(PageState.TextPage dto.content))
                    fileTreeCtx.setSelectedTreeItemPath (Some relativePath)
                | _ ->
                    errorModal.enqueue (
                        ErrorModalRequest.create(
                            $"Unsupported file type for note: {dto.fileType}",
                            title = "Could not open note"
                        )
                    )
            | Result.Error exn ->
                errorModal.enqueue (
                    ErrorModalRequest.create($"Could not open note: {exn.Message}", title = "Could not open note")
                )
        }
        |> Promise.start

    SearchComponent.Main(notes, isLoading, error, openNote)
