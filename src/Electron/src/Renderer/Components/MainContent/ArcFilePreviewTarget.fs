module Renderer.Components.MainContent.ArcFilePreviewTarget

open Feliz
open Renderer.Components.MainContent
open Renderer.Components.MainContent.ArcFilePreviewTargetHelper
open Renderer.Components.Helper
open Swate.Components.Page.ArcFileEditor.Types
open Swate.Components.Composite.Widgets.JsonImport.Types
open Swate.Components
open Swate.Components.Shared
open Swate.Components.Primitive.ErrorModal.Context
open Swate.Components.Primitive.ErrorModal.Types

[<ReactComponent>]
let ArcFilePreviewTarget (arcFile: ArcFiles, requestedView: ActiveView option) =
    let pageStateCtx = Renderer.Context.PageStateContext.usePageStateCtx ()
    let errorModal = useErrorModalCtx ()

    let setArcFilePageState nextRequestedView (nextArcFile: ArcFiles) =
        pageStateCtx.setState (Some(Renderer.Types.PageState.ArcFilePage(nextArcFile, nextRequestedView)))

    let setArcFileInMemoryWithErrorModal (nextArcFile: ArcFiles) =
        promise {
            match! ArcFileApiHelper.setArcFileInMemory nextArcFile with
            | Ok() -> ()
            | Error exn ->
                errorModal.enqueue (ErrorModalRequest.create (exn.Message, title = "Could not update ARC in memory"))
        }
        |> Promise.start

    let setArcFile nextArcFile =
        setArcFilePageState requestedView nextArcFile
        setArcFileInMemoryWithErrorModal nextArcFile

    let runDataMapMutation (errorTitle: string) (operation: Fable.Core.JS.Promise<Result<unit, exn>>) =
        promise {
            match! operation with
            | Ok() -> ()
            | Error exn -> errorModal.enqueue (ErrorModalRequest.create (exn.Message, title = errorTitle))
        }
        |> Promise.catch (fun exn -> errorModal.enqueue (ErrorModalRequest.create (exn.Message, title = errorTitle)))
        |> Promise.start

    let addDataMap () =
        match arcFile.TryGetDataMapParentInfo() with
        | None -> ()
        | Some parentInfo ->
            ArcFileApiHelper.addArcFile (ArcFiles.DataMap(Some parentInfo, ARCtrl.DataMap.init ()))
            |> runDataMapMutation "Could not add DataMap"

    let deleteDataMap () =
        match arcFile.TryGetDataMapParentInfo() |> Option.map DatamapParentInfo.toPath with
        | None -> ()
        | Some path ->
            Api.ipcArcVaultApi.deletePath path
            |> runDataMapMutation "Could not delete DataMap"

    let pickFilePaths =
        React.useCallback (
            (fun () -> promise {
                match! Api.ipcArcVaultApi.pickArcPaths () with
                | Ok paths -> return paths
                | Error exn ->
                    errorModal.enqueue (ErrorModalRequest.create (exn.Message, title = "Could not pick files"))

                    return [||]
            }),
            [| errorModal |]

        )

    let importJson =
        React.useCallback (
            (fun (request: JsonImportRequest) -> promise {
                return!
                    importJsonRequestIntoCurrentTarget
                        arcFile
                        request
                        (setArcFilePageState requestedView)
                        ArcFileApiHelper.setArcFileInMemory
            }),
            [| box arcFile; box pageStateCtx |]
        )

    Html.div [
        prop.key (string (editorKey arcFile requestedView))
        prop.className "swt:contents"
        prop.children [
            Swate.Components.Page.ArcFileEditor.Main.ArcFileEditor(
                arcFile,
                setArcFile,
                pickFilePaths,
                addDataMap,
                deleteDataMap,
                startingActiveView = (requestedView |> Option.defaultValue ActiveView.Metadata),
                onImportJson = importJson,
                onError =
                    (fun message ->
                        errorModal.enqueue (
                            ErrorModalRequest.create (message, title = "Could not update ARC file editor")
                        )
                    )
            )
        ]
    ]
