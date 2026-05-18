module Renderer.Components.MainContent.ArcFilePreviewTarget

open Feliz
open Renderer.Components.ARCHelper
open Renderer.Components.MainContent
open Swate.Components.Page.ArcFileEditor
open Swate.Components
open Swate.Components.Shared
open Swate.Components.Primitive.ErrorModal.Context
open Swate.Components.Primitive.ErrorModal.Types

[<ReactComponent>]
let ArcFilePreviewTarget (arcFile: ArcFiles) =
    let pageStateCtx = Renderer.Context.PageStateContext.usePageStateCtx ()
    let errorModal = useErrorModalCtx ()
    let arcScopeId = useCurrentArcScopeId ()

    let setArcFileInMemory (nextArcFile: ArcFiles) =
        promise {
            match! Helper.setArcFileInMemory nextArcFile with
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
                match! Helper.saveArcFile arcFile with
                | Ok() -> ()
                | Error exn ->
                    errorModal.enqueue (
                        ErrorModalRequest.create (exn.Message, title = "Could not save ARC file", ?scopeId = arcScopeId)
                    )
            }
            |> Promise.start

    let pickFilePaths =
        React.useCallback (
            (fun () -> promise {
                match! Api.ipcArcVaultApi.pickArcPaths () with
                | Ok paths -> return paths
                | Error exn ->
                    errorModal.enqueue (
                        ErrorModalRequest.create (exn.Message, title = "Could not pick files", ?scopeId = arcScopeId)
                    )

                    return [||]
            }),
            [| errorModal |]

        )

    Swate.Components.Page.ArcFileEditor.Main.ArcFileEditor(arcFile, setArcFile, pickFilePaths)