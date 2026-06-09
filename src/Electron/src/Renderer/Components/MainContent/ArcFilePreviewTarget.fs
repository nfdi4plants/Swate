module Renderer.Components.MainContent.ArcFilePreviewTarget

open Feliz
open Renderer.Components.MainContent
open Swate.Components.Page.ArcFileEditor.Types
open Swate.Components.Composite.AnnotationTable
open Swate.Components
open Swate.Components.Shared
open Swate.Components.Primitive.ErrorModal.Context
open Swate.Components.Primitive.ErrorModal.Types

let private resetTables (arcFile: ArcFiles) =
    let tables = arcFile.ArcTables()

    for removeIndex = tables.TableCount - 1 downto 0 do
        tables.RemoveTableAt removeIndex

    ArcFiles.refreshRef arcFile

[<ReactComponent>]
let private TableNavbarActions
    (props: ArcFileEditorHeaderProps, setArcFile: ArcFiles -> unit)
    =
    let isDeleteModalOpen, setIsDeleteModalOpen = React.useState false

    match props.activeView with
    | ActiveView.Table tableIndex when tableIndex >= 0 && tableIndex < props.arcFile.Tables().Count ->

        let openDeleteModal =
            fun _ -> setIsDeleteModalOpen true

        let confirmDelete () =
            props.arcFile |> resetTables |> setArcFile
            props.setActiveView ActiveView.Metadata

        React.Fragment [
            ResetTableConfirmationModal.ResetTableConfirmationModal(
                isDeleteModalOpen,
                setIsDeleteModalOpen,
                confirmDelete
            )
            Html.div [
                prop.className "swt:flex swt:items-center swt:gap-2"
                prop.children [
                    Html.button [
                        prop.type'.button
                        prop.className
                            "swt:btn swt:btn-square swt:btn-ghost swt:btn-sm swt:hover:bg-error swt:hover:text-error-content swt:hover:border-error"
                        prop.onClick openDeleteModal
                        prop.title "Reset"
                        prop.ariaLabel "Reset tables"
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

    let setArcFileInMemory (nextArcFile: ArcFiles) =
        promise {
            match! Helper.setArcFileInMemory nextArcFile with
            | Ok() -> ()
            | Error exn ->
                errorModal.enqueue (
                    ErrorModalRequest.create (
                        exn.Message,
                        title = "Could not update ARC in memory"
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
                        ErrorModalRequest.create (exn.Message, title = "Could not save ARC file")
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
                        ErrorModalRequest.create (exn.Message, title = "Could not pick files")
                    )

                    return [||]
            }),
            [| errorModal |]

        )

    let trailingNavbarElements =
        React.useCallback (
            (fun props -> TableNavbarActions(props, setArcFile)),
            [| box setArcFile |]
        )

    Swate.Components.Page.ArcFileEditor.Main.ArcFileEditor(
        arcFile,
        setArcFile,
        pickFilePaths,
        trailingNavbarElements = trailingNavbarElements
    )
