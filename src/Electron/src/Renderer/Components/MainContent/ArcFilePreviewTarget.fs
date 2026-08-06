module Renderer.Components.MainContent.ArcFilePreviewTarget

open Feliz
open Renderer.Components.MainContent
open Renderer.Components.MainContent.ArcFilePreviewTargetHelper
open Swate.Components.Page.ArcFileEditor.Types
open Swate.Components.Composite.AnnotationTable
open Swate.Components.Composite.Widgets.JsonImport.Types
open Swate.Components
open Swate.Components.Shared
open Swate.Components.Primitive.ErrorModal.Context
open Swate.Components.Primitive.ErrorModal.Types
open Swate.Components.Page.ArcFileEditor.Types

[<ReactComponent>]
let private TableNavbarActions (props: ArcFileEditorHeaderProps, setArcFile: ArcFiles -> unit) =
    let isDeleteModalOpen, setIsDeleteModalOpen = React.useState false

    match props.activeView with
    | ActiveView.Table tableIndex when tableIndex >= 0 && tableIndex < props.arcFile.Tables().Count ->
        let tableName = props.arcFile.Tables().[tableIndex].Name
        let deleteLabel = $"Delete Table: {tableName}"

        let openDeleteModal = fun _ -> setIsDeleteModalOpen true

        let confirmDelete () =
            deleteSelectedTable props.arcFile tableIndex setArcFile props.setActiveView

        React.Fragment [
            ResetTableConfirmationModal.ResetTableConfirmationModal(
                isDeleteModalOpen,
                setIsDeleteModalOpen,
                confirmDelete,
                tableName = tableName
            )
            Html.div [
                prop.className "swt:flex swt:items-center swt:gap-2"
                prop.children [
                    Html.button [
                        prop.type'.button
                        prop.className
                            "swt:btn swt:btn-square swt:btn-ghost swt:btn-sm swt:hover:bg-error swt:hover:text-error-content swt:hover:border-error"
                        prop.onClick openDeleteModal
                        prop.title deleteLabel
                        prop.ariaLabel deleteLabel
                        prop.children [
                            Html.i [
                                prop.className "swt:iconify swt:fluent--delete-20-filled swt:size-5"
                            ]
                        ]
                    ]
                ]
            ]
        ]
    | _ -> Html.none

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

    let trailingNavbarElements =
        React.useCallback ((fun props -> TableNavbarActions(props, setArcFile)), [| box setArcFile |])

    let widgetNavbarElements =
        fun props ->
            let button (dataMap: ARCtrl.DataMap option) setDataMap =
                Swate.Components.Primitive.Buttons.Buttons.QuickAccessButton(
                    Html.i [
                        prop.className "swt:iconify swt:fluent--database-arrow-up-20-regular swt:size-5"
                    ],
                    "Add DataMap",
                    (fun _ ->
                        if dataMap.IsNone then
                            setDataMap (Some(ARCtrl.DataMap.init ()))
                            let nextArcFile = ArcFiles.refreshRef props.arcFile
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
            | ArcFiles.Assay assay -> button assay.DataMap (fun value -> assay.DataMap <- value)
            | ArcFiles.Study(study, _) -> button study.DataMap (fun value -> study.DataMap <- value)
            | ArcFiles.Run run -> button run.DataMap (fun value -> run.DataMap <- value)
            | ArcFiles.Workflow workflow -> button workflow.DataMap (fun value -> workflow.DataMap <- value)
            | _ -> Html.none

    let editorKey =
        arcFile.TryGetRelativePath()
        |> Option.defaultValue (string arcFile.RelatedArcFilesDiscriminate),
        startingActiveView |> Option.map _.ViewIndex

    Html.div [
        prop.key (string editorKey)
        prop.className "swt:contents"
        prop.children [
            Swate.Components.Page.ArcFileEditor.Main.ArcFileEditor(
                arcFile,
                setArcFile,
                pickFilePaths,
                widgetNavbarElements = widgetNavbarElements,
                trailingNavbarElements = trailingNavbarElements,
                startingActiveView = (startingActiveView |> Option.defaultValue ActiveView.Metadata),
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
