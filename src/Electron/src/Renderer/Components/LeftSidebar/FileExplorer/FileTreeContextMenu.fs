module Renderer.Components.LeftSidebar.FileExplorer.FileTreeContextMenu

open Fable.Core
open Swate.Components
open Swate.Components.Page.FileExplorer.Types
open Swate.Components.Primitive.ErrorModal.Types
open Swate.Components.Shared
open Swate.Electron.Shared.FileIOTypes
open Renderer.Components.LeftSidebar.FileExplorer.FileTreeAssignNoteHelper
open Renderer.Components.LeftSidebar.FileExplorer.FileTreeRenameHelper
open Renderer.Components.LeftSidebar.FileExplorer.Helper

type PathActionConfig = {
    openPathInFileExplorer: string -> JS.Promise<Result<unit, exn>>
    openPathWithDefaultApplication: string -> JS.Promise<Result<unit, exn>>
    enqueueError: ErrorModalRequest -> unit
}

type ContextMenuConfig = {
    openItem: FileItem -> unit
    openCreateModal: ArcExplorerNodeKind -> unit
    openFileSystemCreateModal: FileSystemItemKind -> FileItem -> unit
    requestAssignNoteItem: FileItem -> unit
    requestRenameItem: FileItem -> unit
    requestDeleteItem: FileItem -> unit
    pathActionConfig: PathActionConfig
    enqueueError: ErrorModalRequest -> unit
    runToggleLfsMark: string -> bool -> JS.Promise<Result<unit, string>>
    runDownloadLfsFile: string -> JS.Promise<Result<unit, string>>
    runFreeLocalLfsCopy: string -> JS.Promise<Result<unit, string>>
}

let private withDividers (groups: ContextMenuItem list list) =
    groups
    |> List.filter (List.isEmpty >> not)
    |> List.mapi (fun index group ->
        if index = 0 then
            group
        else
            ContextMenuItem.divider :: group
    )
    |> List.collect id

let private applyPathActionError (config: PathActionConfig) (title: string) (message: string) =
    config.enqueueError (ErrorModalRequest.create (message, title = title))

let private runPathAction
    (config: PathActionConfig)
    (title: string)
    (execute: string -> JS.Promise<Result<unit, exn>>)
    (relativePath: string)
    =
    promise {
        match! execute relativePath with
        | Ok() -> ()
        | Error exn -> applyPathActionError config title exn.Message
    }
    |> Promise.catch (fun exn -> applyPathActionError config title exn.Message)
    |> Promise.start

let private pathActionContextMenuItemsForRelativePath
    (config: PathActionConfig)
    (item: FileItem)
    (relativePath: string)
    =
    [
        if not item.IsDirectory then
            ContextMenuItem.create
                "Open with Default Application"
                "swt:fluent--open-24-regular"
                (fun () -> runPathAction config "Open file failed" config.openPathWithDefaultApplication relativePath)

        ContextMenuItem.create
            "Open Folder Location"
            "swt:fluent--folder-open-24-regular"
            (fun () -> runPathAction config "Open folder location failed" config.openPathInFileExplorer relativePath)
    ]

let pathActionContextMenuItems (config: PathActionConfig) (item: FileItem) =
    match tryGetNonEmptyItemRelativePath item with
    | None -> []
    | Some relativePath -> pathActionContextMenuItemsForRelativePath config item relativePath

let openContextMenuItems (config: ContextMenuConfig) (item: FileItem) =
    match tryGetNonEmptyItemRelativePath item with
    | None -> []
    | Some relativePath -> [
        ContextMenuItem.create "Open" "swt:fluent--open-24-regular" (fun () -> config.openItem item)

        yield! pathActionContextMenuItemsForRelativePath config.pathActionConfig item relativePath
      ]

let copyPathContextMenuItems (arcRootPath: string option) (item: FileItem) = [
    match tryGetNonEmptyItemRelativePath item with
    | Some relativePath ->
        ContextMenuItem.create
            "Copy Path"
            "swt:fluent--copy-24-regular"
            (fun () ->
                promise {
                    try
                        do! Swate.Components.Shared.JsBindings.Clipboard.navigator.clipboard.writeText relativePath
                    with ex ->
                        Browser.Dom.console.warn ($"Could not copy filetree path: {relativePath}", ex)
                }
                |> Promise.start
            )
    | None -> ()

    match tryGetItemAbsolutePath arcRootPath item with
    | Some fullPath ->
        ContextMenuItem.create
            "Copy Full Path"
            "swt:fluent--copy-24-regular"
            (fun () ->
                promise {
                    try
                        do! Swate.Components.Shared.JsBindings.Clipboard.navigator.clipboard.writeText fullPath
                    with ex ->
                        Browser.Dom.console.warn ($"Could not copy filetree path: {fullPath}", ex)
                }
                |> Promise.start
            )
    | None -> ()
]

let arcCreateContextMenuItems (openCreateModal: ArcExplorerNodeKind -> unit) (item: FileItem) =
    if item.IsDirectory then
        [
            yield!
                arcCreateKinds
                |> List.sortBy arcCreateKindSortOrder
                |> List.map (fun kind ->
                    ContextMenuItem.create
                        $"Add {ArcExplorerNodeKind.label kind}"
                        (arcCreateKindIcon kind)
                        (fun () -> openCreateModal kind)
                )

            ContextMenuItem.create
                "Add Note"
                "swt:fluent--note-add-24-regular"
                (fun () -> openCreateModal ArcExplorerNodeKind.Note)
        ]
    else
        []

let fileSystemCreateContextMenuItems
    (openFileSystemCreateModal: FileSystemItemKind -> FileItem -> unit)
    (item: FileItem)
    =
    if canCreateFileSystemItemIn item then
        fileSystemCreateKinds
        |> List.map (fun kind ->
            ContextMenuItem.create
                $"New {fileSystemCreateKindLabel kind}"
                (fileSystemCreateKindIcon kind)
                (fun () -> openFileSystemCreateModal kind item)
        )
    else
        []

let rootContextMenuItems (config: ContextMenuConfig) (rootItem: FileItem) =
    withDividers [
        fileSystemCreateContextMenuItems config.openFileSystemCreateModal rootItem
        arcCreateContextMenuItems config.openCreateModal rootItem
    ]

let renameContextMenuItems (requestRenameItem: FileItem -> unit) (item: FileItem) =
    if canRenameItem item then
        [
            ContextMenuItem.create "Rename" "swt:fluent--edit-24-regular" (fun () -> requestRenameItem item)
        ]
    else
        []

let deleteContextMenuItems (requestDeleteItem: FileItem -> unit) (item: FileItem) =
    if canDeleteItem item then
        [
            ContextMenuItem.styled
                "Delete"
                "swt:fluent--delete-24-regular"
                "swt:text-error"
                (fun () -> requestDeleteItem item)
        ]
    else
        []

let assignNoteContextMenuItems (requestAssignNoteItem: FileItem -> unit) (item: FileItem) =
    if canAssignNoteToItem item then
        [
            ContextMenuItem.create
                "Assign Note"
                "swt:fluent--arrow-move-24-regular"
                (fun () -> requestAssignNoteItem item)
        ]
    else
        []

let arcDeleteAndRenameContextMenuItems (config: ContextMenuConfig) (item: FileItem) = [
    yield! assignNoteContextMenuItems config.requestAssignNoteItem item
    yield! renameContextMenuItems config.requestRenameItem item
    yield! deleteContextMenuItems config.requestDeleteItem item
]

let createContextMenuItems (config: ContextMenuConfig) arcScopeId =
    let toggleLfsMark =
        Renderer.Components.FileExplorerLfs.createToggleLfsMark config.enqueueError arcScopeId config.runToggleLfsMark

    let downloadLfsFile =
        Renderer.Components.FileExplorerLfs.createDownloadLfsFile
            config.enqueueError
            arcScopeId
            config.runDownloadLfsFile

    let freeLocalLfsCopy =
        Renderer.Components.FileExplorerLfs.createFreeLocalLfsCopy
            config.enqueueError
            arcScopeId
            config.runFreeLocalLfsCopy

    fun item ->
        withDividers [
            openContextMenuItems config item
            copyPathContextMenuItems arcScopeId item
            fileSystemCreateContextMenuItems config.openFileSystemCreateModal item
            Swate.Components.Page.FileExplorer.FileExplorerGitLfsHelper.contextMenuItems
                item
                toggleLfsMark
                (Some downloadLfsFile)
                (Some freeLocalLfsCopy)
            arcCreateContextMenuItems config.openCreateModal item
            arcDeleteAndRenameContextMenuItems config item
        ]
