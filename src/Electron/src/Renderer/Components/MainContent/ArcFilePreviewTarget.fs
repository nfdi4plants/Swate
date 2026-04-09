module Renderer.Components.MainContent.ArcFilePreviewTarget


open Feliz
open Renderer.Components.MainElement
open Renderer.Components.MainContent.Helper
open Swate.Components
open Swate.Components.Shared


[<ReactComponent>]
let ArcFilePreviewTarget (arcFile: ArcFiles) =

    let pageStateCtx = Renderer.Context.PageStateCtx.usePageState ()
    let arcObjectCtx = Renderer.Context.ArcObjectExplorerCtx.useArcObjectExplorer ()

    let isPendingSaveForCurrentArcFile =
        match arcObjectCtx.state.PendingArcFileSave, arcFile.TryGetRelativePath() with
        | Some pendingArcFile, Some currentRelativePath ->
            pendingArcFile.TryGetRelativePath()
            |> Option.exists (fun pendingRelativePath -> PathHelpers.pathsEqual pendingRelativePath currentRelativePath)
        | _ -> false

    let setArcFile =
        fun (arcFile: ArcFiles) ->
            let page = PageState.ArcFilePage arcFile

            pageStateCtx.setState (Some page)
            arcObjectCtx.setArcFileState (Some arcFile)
            arcObjectCtx.setPreviewState (Some page)
            arcObjectCtx.setPendingArcFileSave (Some arcFile)
            arcObjectCtx.setStatusMessage None

    let onSaveArcFile =
        fun _ ->
            promise {
                match! MainContentHelper.saveArcFile arcFile with
                | Ok() when isPendingSaveForCurrentArcFile -> arcObjectCtx.setPendingArcFileSave None
                | Ok() -> ()
                | Error exn -> pageStateCtx.setState (PageState.ErrorPage exn.Message |> Some)
            }
            |> Promise.start

    let renderHeader editorState =
        CreateARCitectNavbar editorState setArcFile onSaveArcFile

    ArcFileEditor.Main(arcFile, setArcFile, header = renderHeader)
