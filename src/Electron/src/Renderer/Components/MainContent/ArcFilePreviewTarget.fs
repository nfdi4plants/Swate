module Renderer.Components.MainContent.ArcFilePreviewTarget

open Feliz
open Renderer.Components.MainContent
open Renderer.Components.MainContent.ArcFilePreviewTargetHelper
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
            match! Helper.setArcFileInMemory nextArcFile with
            | Ok() -> ()
            | Error exn ->
                errorModal.enqueue (ErrorModalRequest.create (exn.Message, title = "Could not update ARC in memory"))
        }
        |> Promise.start

    let setArcFile nextArcFile =
        setArcFilePageState requestedView nextArcFile
        setArcFileInMemoryWithErrorModal nextArcFile

    let addDataMap () =
        match arcFile.TryGetDataMapParentInfo() with
        | None -> ()
        | Some parentInfo ->
            let dataMap = ARCtrl.DataMap.init ()

            promise {
                match!
                    Helper.withArcFileRequest (ArcFiles.DataMap(Some parentInfo, dataMap)) Api.ipcArcVaultApi.addArcFile
                with
                | Ok() ->
                    match arcFile.TryGetRelativePath() with
                    | None ->
                        errorModal.enqueue (
                            ErrorModalRequest.create (
                                "Could not resolve the parent ARC file path.",
                                title = "Could not open created DataMap"
                            )
                        )
                    | Some parentPath ->
                        match! Api.ipcArcVaultApi.openFile parentPath with
                        | Ok parentDto ->
                            match Swate.Electron.Shared.FileIOHelper.FileContentDTO.toArcFile parentDto with
                            | Some nextArcFile -> setArcFilePageState requestedView nextArcFile
                            | None ->
                                errorModal.enqueue (
                                    ErrorModalRequest.create (
                                        "Could not read the reloaded parent ARC file.",
                                        title = "Could not open created DataMap"
                                    )
                                )
                        | Error exn ->
                            errorModal.enqueue (
                                ErrorModalRequest.create (exn.Message, title = "Could not open created DataMap")
                            )
                | Error exn ->
                    errorModal.enqueue (ErrorModalRequest.create (exn.Message, title = "Could not add DataMap"))
            }
            |> Promise.catch (fun exn ->
                errorModal.enqueue (ErrorModalRequest.create (exn.Message, title = "Could not add DataMap"))
            )
            |> Promise.start

    let deleteDataMap () =
        match arcFile.TryGetDataMapParentInfo() |> Option.map DatamapParentInfo.toPath with
        | None -> ()
        | Some path ->
            promise {
                match! Api.ipcArcVaultApi.deletePath path with
                | Ok() ->
                    let nextArcFile = ArcFiles.refreshRef arcFile

                    if nextArcFile.TrySetParentDataMap None then
                        setArcFilePageState (Some ActiveView.Metadata) nextArcFile
                    else
                        pageStateCtx.setState None
                | Error exn ->
                    errorModal.enqueue (ErrorModalRequest.create (exn.Message, title = "Could not delete DataMap"))
            }
            |> Promise.catch (fun exn ->
                errorModal.enqueue (ErrorModalRequest.create (exn.Message, title = "Could not delete DataMap"))
            )
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
                return!
                    importJsonRequestIntoCurrentTarget
                        arcFile
                        request
                        (setArcFilePageState requestedView)
                        Helper.setArcFileInMemory
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
