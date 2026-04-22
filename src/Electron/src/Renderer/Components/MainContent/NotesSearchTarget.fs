module Renderer.Components.MainContent.NotesSearchTarget

open Feliz
open Swate.Components
open Swate.Components.Shared
open Swate.Electron.Shared.FileIOHelper
open Swate.Components.NoteTypes

[<ReactComponent>]
let NotesSearchTarget () =

    let pageCtx = Renderer.Context.PageStateContext.usePageStateCtx()
    let fileTreeCtx = Renderer.Context.FileStateContext.useFileStateCtx()
    let arcObjectCtx = Renderer.Context.ArcObjectExplorerContext.useArcObjectExplorerCtx()
    let notes, setNotes = React.useState ([]: Note list)

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
                fileTreeCtx.setSelection (ArcSelection.forTreePath (Some selectedPath))

                dto
                |> Renderer.Components.ARCHelper.viewLoadResultOfDto
                |> Renderer.Components.ARCHelper.applyLoadedView
                    pageCtx.setState
                    arcObjectCtx.setArcFileState
                    arcObjectCtx.setPreviewState
                    arcObjectCtx.setStatusMessage
            | Result.Error exn ->
                fileTreeCtx.setSelection (ArcSelection.clearExplorerNode fileTreeCtx.state.Selection)

                Renderer.Components.ARCHelper.applyViewError
                    pageCtx.setState
                    arcObjectCtx.setArcFileState
                    arcObjectCtx.setPreviewState
                    arcObjectCtx.setStatusMessage
                    $"Could not open note: {exn.Message}"
        }
        |> Promise.start

    SearchComponent.Main(notes, isLoading, error, openNote)
