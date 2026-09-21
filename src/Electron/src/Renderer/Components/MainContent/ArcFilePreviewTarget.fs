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
let ArcFilePreviewTarget (requestedView: ActiveView option) =
    let arcStateCtx = Renderer.Context.ArcStateContext.useArcStateCtx ()
    let errorModal = useErrorModalCtx ()

    let arcFile = arcStateCtx.arcFile

    React.useEffectOnce (fun () -> fun () -> arcStateCtx.clear ())

    let runDataMapMutation (errorTitle: string) (operation: Fable.Core.JS.Promise<Result<unit, exn>>) =
        promise {
            match! operation with
            | Ok() -> ()
            | Error exn -> errorModal.enqueue (ErrorModalRequest.create (exn.Message, title = errorTitle))
        }
        |> Promise.catch (fun exn -> errorModal.enqueue (ErrorModalRequest.create (exn.Message, title = errorTitle)))
        |> Promise.start

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
                match arcFile with
                | None -> return Error(exn "No ARC file is open.")
                | Some currentArcFile ->
                    return!
                        importJsonRequestIntoCurrentTarget
                            currentArcFile
                            request
                            arcStateCtx.mutate
                            arcStateCtx.replace
            }),
            [| box arcFile; box arcStateCtx |]
        )

    match arcFile with
    | Some arcFile ->
        let addDataMap () =
            match arcFile.TryGetDataMapParentInfo() with
            | None -> ()
            | Some parentInfo ->
                ArcFileApiHelper.withArcFileRequest
                    (ArcFiles.DataMap(Some parentInfo, ARCtrl.DataMap.init ()))
                    Api.ipcArcVaultApi.addArcFile
                |> runDataMapMutation "DataMap could not be added"

        let deleteDataMap () =
            match arcFile.TryGetDataMapParentInfo() |> Option.map DatamapParentInfo.toPath with
            | None -> ()
            | Some path ->
                Api.ipcArcVaultApi.deletePath path
                |> runDataMapMutation "DataMap could not be deleted"

        Html.div [
            prop.key (string (editorKey arcFile requestedView))
            prop.className "swt:contents"
            prop.children [
                Swate.Components.Page.ArcFileEditor.Main.ArcFileEditor(
                    arcFile,
                    arcStateCtx.mutate,
                    arcStateCtx.replace,
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
    | None ->
        Html.div [
            prop.className "swt:contents"
            prop.children [
                Html.div [
                    prop.className "swt:flex-1 swt:flex swt:justify-center swt:items-center"
                    prop.text "No ARC file is loaded."
                ]
            ]
        ]
