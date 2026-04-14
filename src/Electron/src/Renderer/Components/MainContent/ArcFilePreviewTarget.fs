module Renderer.Components.MainContent.ArcFilePreviewTarget

open Feliz
open Renderer.Components.MainContent.Helper
open Renderer.Components.MainElement
open Renderer.Components.WidgetRegistry
open Swate.Components.ArcFileEditor
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
        fun (nextArcFile: ArcFiles) ->
            let page = Renderer.Types.PageState.ArcFilePage nextArcFile

            pageStateCtx.setState (Some page)
            arcObjectCtx.setArcFileState (Some nextArcFile)
            arcObjectCtx.setPreviewState (Some(Swate.Components.Shared.PageState.ArcFilePage nextArcFile))
            arcObjectCtx.setPendingArcFileSave (Some nextArcFile)
            arcObjectCtx.setStatusMessage None

    let onSaveArcFile =
        fun _ ->
            promise {
                match! MainContentHelper.saveArcFile arcFile with
                | Ok() when isPendingSaveForCurrentArcFile -> arcObjectCtx.setPendingArcFileSave None
                | Ok() -> ()
                | Error exn -> pageStateCtx.setState (Renderer.Types.PageState.ErrorPage exn.Message |> Some)
            }
            |> Promise.start

    let renderHeader editorState =
        CreateARCitectNavbar editorState setArcFile onSaveArcFile

    Main.ArcFileEditor(arcFile, setArcFile, templateServices, ?navbar = Some renderHeader)
