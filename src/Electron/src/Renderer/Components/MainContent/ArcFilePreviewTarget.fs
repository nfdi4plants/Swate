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
let ArcFilePreviewTarget (arcFile: ArcFiles, activeView: ActiveView option) =
    let pageStateCtx = Renderer.Context.PageStateContext.usePageStateCtx ()
    let errorModal = useErrorModalCtx ()

    let setArcFilePageState (nextArcFile: ArcFiles) =
        pageStateCtx.setState (Some(Renderer.Types.PageState.ArcFilePage(nextArcFile, activeView)))

    let setArcFileInMemoryWithErrorModal (nextArcFile: ArcFiles) =
        promise {
            match! Helper.setArcFileInMemory nextArcFile with
            | Ok() -> ()
            | Error exn ->
                errorModal.enqueue (ErrorModalRequest.create (exn.Message, title = "Could not update ARC in memory"))
        }
        |> Promise.start

    let setArcFile nextArcFile =
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
                return!
                    importJsonRequestIntoCurrentTarget arcFile request setArcFilePageState Helper.setArcFileInMemory
            }),
            [| box arcFile; box pageStateCtx |]
        )

    let widgetNavbarElements =
        fun (props: ArcFileEditorHeaderProps) ->
            let button (dataMap: ARCtrl.DataMap option) =
                Swate.Components.Primitive.Buttons.Buttons.QuickAccessButton(
                    Html.i [
                        prop.className "swt:iconify swt:fluent--database-arrow-up-20-regular swt:size-5"
                    ],
                    "Add DataMap",
                    (fun _ ->
                        if dataMap.IsNone then
                            props.setActiveView ActiveView.DataMap

                            promise {
                                match!
                                    createDataMapInCurrentTarget props.arcFile setArcFilePageState Helper.saveArcFile
                                with
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

    Html.div [
        prop.key (string (editorKey arcFile activeView))
        prop.className "swt:contents"
        prop.children [
            Swate.Components.Page.ArcFileEditor.Main.ArcFileEditor(
                arcFile,
                setArcFile,
                pickFilePaths,
                widgetNavbarElements = widgetNavbarElements,
                startingActiveView = (activeView |> Option.defaultValue ActiveView.Metadata),
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
