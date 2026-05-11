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

    let setArcFileInMemory (nextArcFile: ArcFiles) =
        promise {
            match! MainContentHelper.setArcFileInMemory nextArcFile with
            | Ok() -> ()
            | Error exn ->
                errorModal.enqueue (
                    ErrorModalRequest.create (
                        exn.Message,
                        title = "Could not update ARC in memory",
                        ?scopeId = arcScopeId
                    )
                )
        }
        |> Promise.start

    let setArcFile =
        fun (nextArcFile: ArcFiles) ->
            let page = Renderer.Types.PageState.ArcFilePage nextArcFile

            pageStateCtx.setState (Some page)
            setArcFileInMemory nextArcFile

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

    let pickFilePaths =
        fun () -> promise {
            match! Api.ipcArcVaultApi.pickArcPaths () with
            | Ok paths -> return paths
            | Error exn ->
                errorModal.enqueue (
                    ErrorModalRequest.create (exn.Message, title = "Could not pick files", ?scopeId = arcScopeId)
                )

                return [||]
        }

    let renderTrailingNavbarElements _ =
        QuickAccessButton.QuickAccessButton("Save", Icons.Save(), onSaveArcFile)

    Main.ArcFileEditor(arcFile, setArcFile, pickFilePaths, trailingNavbarElements = renderTrailingNavbarElements)