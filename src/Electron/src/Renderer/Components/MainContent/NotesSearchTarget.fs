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
    let arcObjectCtx = Renderer.Context.ArcObjectExplorerCtx.useArcObjectExplorer ()
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
                let selectedPath = normalizePath relativePath
                fileTreeCtx.setSelectedTreeItemPath (Some selectedPath)
                arcObjectCtx.setSelectedExplorerItemId None

                dto
                |> Renderer.Components.ARCHelper.previewLoadResultOfDto
                |> Renderer.Components.ARCHelper.applyLoadedPreview
                    pageCtx.setState
                    arcObjectCtx.setArcFileState
                    arcObjectCtx.setPreviewState
                    arcObjectCtx.setStatusMessage
            | Result.Error exn ->
                arcObjectCtx.setSelectedExplorerItemId None

                Renderer.Components.ARCHelper.applyPreviewError
                    pageCtx.setState
                    arcObjectCtx.setArcFileState
                    arcObjectCtx.setPreviewState
                    arcObjectCtx.setStatusMessage
                    $"Could not open note: {exn.Message}"
        }
        |> Promise.start

    SearchComponent.Main(notes, isLoading, error, openNote)
