module Renderer.Components.MainContent.ArcFilePreviewTarget

open Feliz
open Renderer.Components.ARCHelper
open Renderer.Components.MainContent.Helper
open Renderer.Components.WidgetRegistry
open Swate.Components.ArcFileEditor
open Swate.Components
open Swate.Components.Shared
open Swate.Components.ErrorModal

[<ReactComponent>]
let ArcFilePreviewTarget (arcFile: ArcFiles) =
    let pageStateCtx = Renderer.Context.PageStateCtx.usePageState ()
    let arcObjectCtx = Renderer.Context.ArcObjectExplorerCtx.useArcObjectExplorer ()
    let errorModal = ErrorModal.Context.useErrorModal ()
    let arcScopeId = useCurrentArcScopeId ()

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
                | Error exn ->
                    errorModal.enqueue (
                        ErrorModalRequest.create(exn.Message, title = "Could not save ARC file", ?scopeId = arcScopeId)
                    )
            }
            |> Promise.start

    let renderHeader _ =
        QuickAccessButton.QuickAccessButton("Save", Icons.Save(), onSaveArcFile)

    Main.ArcFileEditor(
        arcFile,
        setArcFile,
        templateServices,
        ?header = Some renderHeader,
        ?widgetServices = Some arcFileEditorWidgetServices
    )
