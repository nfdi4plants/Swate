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

    let setArcFile =
        fun (arcFile: ArcFiles) ->
            let page = PageState.ArcFilePage arcFile

            pageStateCtx.setState (Some page)
            arcObjectCtx.setArcFileState (Some arcFile)
            arcObjectCtx.setPreviewState (Some page)
            arcObjectCtx.setStatusMessage None

    let onSaveArcFile =
        fun _ ->
            promise {
                match! MainContentHelper.saveArcFile arcFile with
                | Ok() -> ()
                | Error exn -> pageStateCtx.setState (PageState.ErrorPage exn.Message |> Some)
            }
            |> Promise.start

    let renderHeader editorState =
        CreateARCitectNavbar editorState setArcFile onSaveArcFile

    ArcFileEditor.Main(arcFile, setArcFile, header = renderHeader)
