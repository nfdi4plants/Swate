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
open Swate.Electron.Shared.FileIOTypes

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
let ArcFilePreviewTarget (arcFile: ArcFiles) =
    let pageStateCtx = Renderer.Context.PageStateContext.usePageStateCtx ()
    let errorModal = useErrorModalCtx ()

    let setArcFilePageState (nextArcFile: ArcFiles) =
        let page = Renderer.Types.PageState.ArcFilePage nextArcFile

        pageStateCtx.setState (Some page)

    let updateArcFileInMemory (nextArcFile: ArcFiles) =
        Helper.setArcFileInMemory nextArcFile

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

    let exportJson =
        React.useCallback (
            (fun (arcFile: ArcFiles, jsonFormat: JsonExportFormat) -> promise {
                match Json.Export.tryParseToJsonString (arcFile, jsonFormat) with
                | Error exn -> return Error exn
                | Ok(fileName, content) ->
                    let request: JsonExportSaveRequest = {
                        suggestedFileName = fileName
                        content = content
                    }

                    match! Api.ipcArcVaultApi.saveJsonExport request with
                    | Ok _ -> return Ok()
                    | Error exn -> return Error exn
            }),
            [||]
        )

    let pickJsonFile =
        React.useCallback ((fun () -> Api.ipcArcVaultApi.pickJsonImportFile ()), [||])

    let importJson =
        React.useCallback (
            (fun (request: JsonImportRequest) -> promise {
                return!
                    importJsonRequestIntoCurrentTarget
                        arcFile
                        request
                        setArcFilePageState
                        updateArcFileInMemory
            }),
            [| box arcFile; box pageStateCtx |]
        )

    let trailingNavbarElements =
        React.useCallback ((fun props -> TableNavbarActions(props, setArcFile)), [| box setArcFile |])

    Swate.Components.Page.ArcFileEditor.Main.ArcFileEditor(
        arcFile,
        setArcFile,
        pickFilePaths,
        trailingNavbarElements = trailingNavbarElements,
        pickJsonFile = pickJsonFile,
        onImportJson = importJson,
        onExportJson = exportJson,
        onError =
            (fun message ->
                errorModal.enqueue (ErrorModalRequest.create (message, title = "Could not update ARC file editor"))
            )
    )
