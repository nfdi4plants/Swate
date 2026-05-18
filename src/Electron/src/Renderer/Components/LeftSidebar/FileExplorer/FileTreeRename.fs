namespace Renderer.Components.LeftSidebar.FileExplorer

open Fable.Core
open Feliz
open Swate.Components
open Swate.Components.Primitive.BaseModal
open Swate.Components.Primitive.ErrorModal.Types
open Swate.Components.Page.FileExplorer.Types
open Swate.Components.Shared
open Swate.Electron.Shared.FileIOTypes
open Renderer.Components.LeftSidebar.FileExplorer.Types
open Renderer.Components.LeftSidebar.FileExplorer.FileTreeRenameHelper

module FileTreeRenameWorkflow =

    type ConfirmRenameConfig = {
        pendingRenameDraft: ArcRenameDraft option
        selectedTreePath: string option
        pageState: Renderer.Types.PageState option
        closeRenameModal: unit -> unit
        setIsRenaming: bool -> unit
        setSelection: ArcSelection -> unit
        refreshGitStatus: unit -> unit
        reloadPreviewByPath: string -> JS.Promise<Result<unit, string>>
        renamePath: RenamePathRequest -> JS.Promise<Result<unit, exn>>
        enqueueError: ErrorModalRequest -> unit
        arcScopeId: string option
    }

    let private enqueueRenameError
        (enqueueError: ErrorModalRequest -> unit)
        (arcScopeId: string option)
        (errorMessage: string)
        =
        enqueueError (
            ErrorModalRequest.create (
                errorMessage,
                title = "Could not rename item",
                ?scopeId = arcScopeId
            )
        )

    let private applyRenameError (config: ConfirmRenameConfig) (errorMessage: string) =
        enqueueRenameError config.enqueueError config.arcScopeId errorMessage

    let private tryRemapActiveArcFilePath
        (sourcePath: string)
        (targetPath: string)
        (pageState: Renderer.Types.PageState option)
        =
        match pageState with
        | Some(Renderer.Types.PageState.ArcFilePage arcFile) ->
            arcFile.TryGetRelativePath()
            |> Option.bind (fun arcFilePath ->
                tryRemapSelectionPath sourcePath targetPath (Some arcFilePath)
            )
        | _ -> None

    let requestRenameItem
        (setPendingRenameDraft: ArcRenameDraft option -> unit)
        (enqueueError: ErrorModalRequest -> unit)
        (arcScopeId: string option)
        (item: FileItem)
        =
        match tryBuildRenameDraft item with
        | Ok renameDraft -> setPendingRenameDraft (Some renameDraft)
        | Error validationError -> enqueueRenameError enqueueError arcScopeId validationError

    let renameContextMenuItems (requestRenameItem: FileItem -> unit) (item: FileItem) =
        if canRenameItem item then
            [
                {
                    Label = "Rename"
                    Icon = "swt:fluent--edit-24-regular"
                    OnClick = fun () -> requestRenameItem item
                    Disabled = None
                }
            ]
        else
            []

    let confirmRenameItem (config: ConfirmRenameConfig) (newName: string) =
        match config.pendingRenameDraft with
        | None -> config.closeRenameModal ()
        | Some renameDraft ->
            match normalizeRenameName newName with
            | Error validationError -> applyRenameError config validationError
            | Ok normalizedNewName ->
                let targetPath = buildRenamedPath renameDraft.SourcePath normalizedNewName

                if PathHelpers.pathsEqual targetPath renameDraft.SourcePath then
                    config.closeRenameModal ()
                else
                    config.setIsRenaming true

                    promise {
                        let! renameResult =
                            config.renamePath {
                                relativePath = renameDraft.SourcePath
                                newName = normalizedNewName
                            }

                        match renameResult with
                        | Ok() ->
                            tryRemapSelectionPath renameDraft.SourcePath targetPath config.selectedTreePath
                            |> Option.iter (fun remappedSelectionPath ->
                                config.setSelection (ArcSelection.forTreePath (Some remappedSelectionPath))
                            )

                            match tryRemapActiveArcFilePath renameDraft.SourcePath targetPath config.pageState with
                            | Some remappedArcFilePath ->
                                let! reloadResult = config.reloadPreviewByPath remappedArcFilePath

                                match reloadResult with
                                | Ok() -> ()
                                | Error reloadError ->
                                    applyRenameError
                                        config
                                        $"Renamed item, but could not refresh the open ARC file preview: {reloadError}"
                            | None -> ()

                            config.refreshGitStatus ()
                            config.closeRenameModal ()
                        | Error renameError -> applyRenameError config renameError.Message
                    }
                    |> Promise.catch (fun promiseError -> applyRenameError config promiseError.Message)
                    |> Promise.map (fun _ -> config.setIsRenaming false)
                    |> Promise.start

[<Erase; Mangle(false)>]
type FileTreeRename =

    [<ReactComponent>]
    static member RenameModal
        (
            isOpen: bool,
            itemName: string option,
            initialName: string option,
            close: unit -> unit,
            submit: string -> unit,
            ?isRenaming: bool
        ) =

        let renameName, setRenameName = React.useState (initialName |> Option.defaultValue "")
        let isRenaming = defaultArg isRenaming false

        React.useEffect (
            (fun () -> setRenameName (initialName |> Option.defaultValue "")),
            [| box initialName; box isOpen |]
        )

        let setIsOpen isOpen =
            if not isOpen then
                close ()

        let displayName = itemName |> Option.defaultValue "this item"
        let normalizedNameResult = normalizeRenameName renameName
        let isValid = normalizedNameResult |> Result.isOk

        let submitIfValid () =
            match normalizeRenameName renameName with
            | Ok normalizedName -> submit normalizedName
            | Error _ -> ()

        let footer =
            Html.div [
                prop.className "swt:flex swt:gap-2 swt:justify-end swt:w-full"
                prop.children [
                    Html.button [
                        prop.className "swt:btn swt:btn-ghost"
                        prop.disabled isRenaming
                        prop.onClick (fun _ -> close ())
                        prop.text "Cancel"
                    ]
                    Html.button [
                        prop.className "swt:btn swt:btn-primary"
                        prop.disabled ((not isValid) || isRenaming)
                        prop.onClick (fun _ -> submitIfValid ())
                        prop.children [
                            if isRenaming then
                                Html.span [ prop.text "Renaming..." ]
                            else
                                Html.span [ prop.text "Rename" ]
                        ]
                    ]
                ]
            ]

        let content =
            Html.fieldSet [
                prop.className "swt:fieldset"
                prop.children [
                    Html.legend [
                        prop.className "swt:fieldset-legend"
                        prop.text "New name"
                    ]
                    Html.label [
                        prop.className "swt:input swt:w-full"
                        prop.children [
                            Html.input [
                                prop.autoFocus true
                                prop.disabled isRenaming
                                prop.value renameName
                                prop.onChange setRenameName
                                prop.onKeyDown (key.enter, fun _ -> submitIfValid ())
                            ]
                        ]
                    ]
                    Html.p [
                        prop.hidden isValid
                        prop.className "swt:text-error swt:text-sm"
                        prop.text "Name is required and must not contain path separators."
                    ]
                ]
            ]

        BaseModal.Modal(
            isOpen = isOpen,
            setIsOpen = setIsOpen,
            header = Html.text "Rename Item",
            description = Html.text $"Rename '{displayName}' in the current ARC.",
            children = content,
            footer = footer,
            debug = "arc-rename"
        )
