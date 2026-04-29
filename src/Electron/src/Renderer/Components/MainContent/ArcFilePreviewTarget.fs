module Renderer.Components.MainContent.ArcFilePreviewTarget

open Feliz
open Renderer.Components.ARCHelper
open Renderer.Components.MainContent.Helper
open Swate.Components.ArcFileEditor
open Swate.Components
open Swate.Components.Shared
open Swate.Components.ErrorModal

[<ReactComponent>]
let ArcFilePreviewTarget (arcFile: ArcFiles) =
    let pageStateCtx = Renderer.Context.PageStateContext.usePageStateCtx ()
    let errorModal = ErrorModal.Context.useErrorModalCtx ()
    let arcScopeId = useCurrentArcScopeId ()

    let stagePendingArcFileSave (nextArcFile: ArcFiles) =
        promise {
            match! MainContentHelper.setPendingArcFileSave (Some nextArcFile) with
            | Ok() -> ()
            | Error exn ->
                errorModal.enqueue (
                    ErrorModalRequest.create (exn.Message, title = "Could not stage ARC file save", ?scopeId = arcScopeId)
                )
        }
        |> Promise.start

    let setArcFile =
        fun (nextArcFile: ArcFiles) ->
            let page = Renderer.Types.PageState.ArcFilePage nextArcFile

            pageStateCtx.setState (Some page)
            stagePendingArcFileSave nextArcFile

    let onSaveArcFile =
        fun _ ->
            promise {
                match! MainContentHelper.saveArcFile arcFile with
                | Ok() -> ()
                | Error exn ->
                    errorModal.enqueue (
                        ErrorModalRequest.create (exn.Message, title = "Could not save ARC file", ?scopeId = arcScopeId)
                    )
            }
            |> Promise.start

    let renderTrailingNavbarElements _ =
        QuickAccessButton.QuickAccessButton("Save", Icons.Save(), onSaveArcFile)

    Main.ArcFileEditor(arcFile, setArcFile, trailingNavbarElements = renderTrailingNavbarElements)
