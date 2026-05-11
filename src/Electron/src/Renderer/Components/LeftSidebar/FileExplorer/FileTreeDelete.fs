namespace Renderer.Components.LeftSidebar.FileExplorer

open Renderer.Components.FileExplorerDeleteHelper
open Swate.Components
open Swate.Components.ErrorModal
open Swate.Components.FileExplorer.Types
open Swate.Components.Shared
open Fable.Core
open Feliz
open ARCtrl
open Helper

module FileTreeDeleteWorkflow =

    type ConfirmDeleteConfig = {
        pendingDeleteItem: FileItem option
        pendingArcFileSave: ArcFiles option
        closeDeleteModal: unit -> unit
        setIsDeleting: bool -> unit
        setPendingArcFileSave: ArcFiles option -> unit
        stagePendingArcFileSave: ArcFiles option -> JS.Promise<Result<unit, exn>>
        enqueueError: ErrorModalRequest -> unit
        arcScopeId: string option
    }

    let private applyDeleteError (config: ConfirmDeleteConfig) (errorMessage: string) =
        config.enqueueError (
            ErrorModalRequest.create (
                errorMessage,
                title = "Could not delete item",
                ?scopeId = config.arcScopeId
            )
        )

    let requestDeleteItem (setPendingDeleteItem: FileItem option -> unit) (item: FileItem) =
        if canDeleteItem item then
            setPendingDeleteItem (Some item)

    let confirmDeleteItem (config: ConfirmDeleteConfig) =
        match config.pendingDeleteItem |> Option.bind tryGetItemRelativePath with
        | None -> config.closeDeleteModal ()
        | Some deletePath when ArcDeletePathRules.isDeletePathAllowed deletePath |> not ->
            config.closeDeleteModal ()
        | Some deletePath ->
            config.setIsDeleting true

            promise {
                let pendingPath = tryGetArcFilePendingPath config.pendingArcFileSave

                let shouldClearPendingDraft =
                    FileExplorerDeleteHelper.isPendingPathAffectedByDelete deletePath pendingPath

                let! deleteResult = Api.ipcArcVaultApi.deletePath deletePath

                match deleteResult with
                | Ok() ->
                    if shouldClearPendingDraft then
                        config.setPendingArcFileSave None

                        match! config.stagePendingArcFileSave None with
                        | Ok() -> ()
                        | Error exn -> applyDeleteError config exn.Message

                    config.closeDeleteModal ()
                | Error exn -> applyDeleteError config exn.Message
            }
            |> Promise.catch (fun exn -> applyDeleteError config exn.Message)
            |> Promise.map (fun _ -> config.setIsDeleting false)
            |> Promise.start

    let deleteContextMenuItems (requestDeleteItem: FileItem -> unit) (item: FileItem) =
        if canDeleteItem item then
            [
                {
                    Label = "Delete"
                    Icon = "swt:fluent--delete-24-regular"
                    OnClick = fun () -> requestDeleteItem item
                    Disabled = None
                }
            ]
        else
            []

[<Erase; Mangle(false)>]
type FileTreeDelete =

    [<ReactComponent>]
    static member ConfirmModal
        (
            isOpen: bool,
            itemName: string option,
            close: unit -> unit,
            submit: unit -> unit,
            ?isDeleting: bool
        ) =

        let setIsOpen isOpen =
            if not isOpen then
                close ()

        let displayName = itemName |> Option.defaultValue "this item"
        let isDeleting = defaultArg isDeleting false

        let footer =
            Html.div [
                prop.className "swt:flex swt:gap-2 swt:justify-end swt:w-full"
                prop.children [
                    Html.button [
                        prop.className "swt:btn swt:btn-ghost"
                        prop.disabled isDeleting
                        prop.onClick (fun _ -> close ())
                        prop.text "Cancel"
                    ]
                    Html.button [
                        prop.className "swt:btn swt:btn-error"
                        prop.disabled isDeleting
                        prop.onClick (fun _ -> submit ())
                        prop.children [
                            if isDeleting then
                                Html.span [ prop.text "Deleting..." ]
                            else
                                Html.span [ prop.text "Delete" ]
                        ]
                    ]
                ]
            ]

        BaseModal.Modal(
            isOpen = isOpen,
            setIsOpen = setIsOpen,
            header = Html.text "Delete Item",
            description = Html.text $"Permanently delete '{displayName}'?",
            children = Html.none,
            footer = footer,
            debug = "arc-delete"
        )
