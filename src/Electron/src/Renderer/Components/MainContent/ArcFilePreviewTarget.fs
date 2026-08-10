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
let ArcFilePreviewTarget (arcFile: ArcFiles, startingActiveView: ActiveView option) =
    let pageStateCtx = Renderer.Context.PageStateContext.usePageStateCtx ()
    let errorModal = useErrorModalCtx ()

    let setArcFilePageState (nextArcFile: ArcFiles) =
        pageStateCtx.setState (Some(Renderer.Types.PageState.ArcFilePage(nextArcFile, startingActiveView)))

    let updateArcFileInMemory (nextArcFile: ArcFiles) = Helper.setArcFileInMemory nextArcFile

    let setArcFileInMemoryWithErrorModal (nextArcFile: ArcFiles) =
        promise {
            match! updateArcFileInMemory nextArcFile with
            | Ok() -> ()
            | Error exn ->
                errorModal.enqueue (ErrorModalRequest.create (exn.Message, title = "Could not update ARC in memory"))
        }
        |> Promise.start

    let setArcFile =
        fun (nextArcFile: ArcFiles) ->
            setArcFilePageState nextArcFile
            setArcFileInMemoryWithErrorModal nextArcFile

    let onSaveArcFile =
        fun _ ->
            promise {
                match! Helper.saveArcFile arcFile with
                | Ok() -> ()
                | Error exn ->
                    errorModal.enqueue (ErrorModalRequest.create (exn.Message, title = "Could not save ARC file"))
            }
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
                return! importJsonRequestIntoCurrentTarget arcFile request setArcFilePageState updateArcFileInMemory
            }),
            [| box arcFile; box pageStateCtx |]
        )

    let widgetNavbarElements =
        fun props ->
            let button (dataMap: ARCtrl.DataMap option) =
                Swate.Components.Primitive.Buttons.Buttons.QuickAccessButton(
                    Html.i [
                        prop.className "swt:iconify swt:fluent--database-arrow-up-20-regular swt:size-5"
                    ],
                    "Add DataMap",
                    (fun _ ->
                        if dataMap.IsNone then
                            let nextArcFile = ArcFiles.refreshRef props.arcFile

                            match nextArcFile with
                            | ArcFiles.Assay assay -> assay.DataMap <- Some(ARCtrl.DataMap.init ())
                            | ArcFiles.Study(study, _) -> study.DataMap <- Some(ARCtrl.DataMap.init ())
                            | ArcFiles.Run run -> run.DataMap <- Some(ARCtrl.DataMap.init ())
                            | ArcFiles.Workflow workflow -> workflow.DataMap <- Some(ARCtrl.DataMap.init ())
                            | _ -> ()

                            setArcFilePageState nextArcFile
                            props.setActiveView ActiveView.DataMap

                            promise {
                                match! Helper.saveArcFile nextArcFile with
                                | Ok() -> ()
                                | Error exn ->
                                    errorModal.enqueue (
                                        ErrorModalRequest.create (exn.Message, title = "Could not save DataMap")
                                    )
                            }
                            |> Promise.start
                    ),
                    isDisabled = dataMap.IsSome
                )

            match props.arcFile with
            | ArcFiles.Assay assay -> button assay.DataMap
            | ArcFiles.Study(study, _) -> button study.DataMap
            | ArcFiles.Run run -> button run.DataMap
            | ArcFiles.Workflow workflow -> button workflow.DataMap
            | _ -> Html.none

    Swate.Components.Page.ArcFileEditor.Main.ArcFileEditor(
        arcFile,
        setArcFile,
        pickFilePaths,
        widgetNavbarElements = widgetNavbarElements,
        startingActiveView = (startingActiveView |> Option.defaultValue ActiveView.Metadata),
        onImportJson = importJson,
        onError =
            (fun message ->
                errorModal.enqueue (ErrorModalRequest.create (message, title = "Could not update ARC file editor"))
            )
    )
