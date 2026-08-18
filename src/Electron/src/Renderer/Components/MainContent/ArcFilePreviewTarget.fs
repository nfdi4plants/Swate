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

    let widgetNavbarElements =
        fun (props: ArcFileEditorHeaderProps) ->
            let button (dataMap: ARCtrl.Datamap option) =
                Swate.Components.Primitive.Buttons.Buttons.QuickAccessButton(
                    Html.i [
                        prop.className "swt:iconify swt:fluent--database-arrow-up-20-regular swt:size-5"
                    ],
                    "Add DataMap",
                    (fun _ ->
                        if dataMap.IsNone then
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
            | ArcFiles.Assay assay -> button assay.Datamap
            | ArcFiles.Study(study, _) -> button study.Datamap
            | ArcFiles.Run run -> button run.Datamap
            | ArcFiles.Workflow workflow -> button workflow.Datamap
            | _ -> Html.none

    Html.div [
        prop.key (string (editorKey arcFile requestedView))
        prop.className "swt:contents"
        prop.children [
            Swate.Components.Page.ArcFileEditor.Main.ArcFileEditor(
                arcFile,
                setArcFile,
                pickFilePaths,
                widgetNavbarElements = widgetNavbarElements,
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
