module Renderer.Components.LeftSidebar.FileExplorer.FileTreeContextMenu

open System
open Fable.Core
open Fable.Core.JsInterop
open Swate.Components.Page.FileExplorer.Types
open Swate.Components.Primitive.ErrorModal.Types
open Swate.Components.Shared
open Swate.Electron.Shared.FileIOTypes
open Renderer.Components.LeftSidebar.FileExplorer.FileTreeRenameHelper
open Renderer.Components.LeftSidebar.FileExplorer.Helper

type PathActionConfig = {
    showPathInFileExplorer: string -> JS.Promise<Result<unit, exn>>
    openPathWithDefaultApplication: string -> JS.Promise<Result<unit, exn>>
    enqueueError: ErrorModalRequest -> unit
    arcScopeId: string option
}

type ContextMenuConfig = {
    openItem: FileItem -> unit
    arcRootPath: string option
    openCreateModal: ArcExplorerNodeKind -> unit
    openFileSystemCreateModal: FileSystemItemKind -> FileItem -> unit
    requestRenameItem: FileItem -> unit
    requestDeleteItem: FileItem -> unit
    pathActionConfig: PathActionConfig
    enqueueError: ErrorModalRequest -> unit
    arcScopeId: string option
    runToggleLfsMark: string -> bool -> JS.Promise<Result<unit, string>>
    runFreeLocalLfsCopy: string -> JS.Promise<Result<unit, string>>
}

let private withDividers (groups: ContextMenuItem list list) =
    groups
    |> List.filter (List.isEmpty >> not)
    |> List.mapi (fun index group -> if index = 0 then group else ContextMenuItems.divider :: group)
    |> List.collect id

let tryGetAbsoluteItemPath (arcRootPath: string option) (item: FileItem) =
    match arcRootPath, item.Path with
    | Some rootPath, Some relativePath ->
        let normalizedRootPath = PathHelpers.normalizePath rootPath
        let normalizedRelativePath = PathHelpers.normalizeRelativePath relativePath

        if String.IsNullOrWhiteSpace normalizedRootPath then
            None
        elif String.IsNullOrWhiteSpace normalizedRelativePath then
            Some normalizedRootPath
        else
            Some(PathHelpers.normalizePath $"{normalizedRootPath}/{normalizedRelativePath}")
    | _ -> None

let tryGetRelativeItemPath (item: FileItem) =
    item.Path
    |> Option.map PathHelpers.normalizeRelativePath
    |> Option.map PathHelpers.normalizePath
    |> Option.filter (String.IsNullOrWhiteSpace >> not)

let private applyPathActionError (config: PathActionConfig) (title: string) (message: string) =
    config.enqueueError (
        ErrorModalRequest.create (
            message,
            title = title,
            ?scopeId = config.arcScopeId
        )
    )

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

let private copyTextToClipboard (text: string) =
    promise {
        try
            let windowObj: obj = Browser.Dom.window
            do! windowObj?navigator?clipboard?writeText (text)
        with ex ->
            Browser.Dom.console.warn ($"Could not copy filetree path: {text}", ex)
    }
    |> Promise.start

let private pathActionContextMenuItemsForRelativePath
    (config: PathActionConfig)
    (item: FileItem)
    (relativePath: string)
    =
    [
        if not item.IsDirectory then
            ContextMenuItems.create
                "Open with Default Application"
                "swt:fluent--open-24-regular"
                (fun () ->
                    runPathAction
                        config
                        "Open file failed"
                        config.openPathWithDefaultApplication
                        relativePath)

        ContextMenuItems.create
            "Open Folder Location"
            "swt:fluent--folder-open-24-regular"
            (fun () ->
                runPathAction
                    config
                    "Open folder location failed"
                    config.showPathInFileExplorer
                    relativePath)
    ]

let pathActionContextMenuItems (config: PathActionConfig) (item: FileItem) =
    match tryGetRelativeItemPath item with
    | None -> []
    | Some relativePath -> pathActionContextMenuItemsForRelativePath config item relativePath

let openContextMenuItems (config: ContextMenuConfig) (item: FileItem) =
    match tryGetRelativeItemPath item with
    | None -> []
    | Some relativePath ->
        [
            ContextMenuItems.create "Open" "swt:fluent--open-24-regular" (fun () -> config.openItem item)

            yield! pathActionContextMenuItemsForRelativePath config.pathActionConfig item relativePath
        ]

let copyPathContextMenuItems (arcRootPath: string option) (item: FileItem) =
    [
        match tryGetRelativeItemPath item with
        | Some relativePath ->
            ContextMenuItems.create "Copy Path" "swt:fluent--copy-24-regular" (fun () -> copyTextToClipboard relativePath)
        | None -> ()

        match tryGetAbsoluteItemPath arcRootPath item with
        | Some fullPath ->
            ContextMenuItems.create "Copy Full Path" "swt:fluent--copy-24-regular" (fun () -> copyTextToClipboard fullPath)
        | None -> ()
    ]

let arcCreateContextMenuItems (openCreateModal: ArcExplorerNodeKind -> unit) (item: FileItem) =
    if item.IsDirectory then
        arcCreateKinds
        |> List.sortBy arcCreateKindSortOrder
        |> List.map (fun kind ->
            ContextMenuItems.create
                $"Add {ArcExplorerNodeKind.label kind}"
                (arcCreateKindIcon kind)
                (fun () -> openCreateModal kind))
    else
        []

let fileSystemCreateContextMenuItems
    (openFileSystemCreateModal: FileSystemItemKind -> FileItem -> unit)
    (item: FileItem)
    =
    if canCreateFileSystemItemIn item then
        fileSystemCreateKinds
        |> List.map (fun kind ->
            ContextMenuItems.create
                $"New {fileSystemCreateKindLabel kind}"
                (fileSystemCreateKindIcon kind)
                (fun () -> openFileSystemCreateModal kind item))
    else
        []

let renameContextMenuItems (requestRenameItem: FileItem -> unit) (item: FileItem) =
    if canRenameItem item then
        [
            ContextMenuItems.create "Rename" "swt:fluent--edit-24-regular" (fun () -> requestRenameItem item)
        ]
    else
        []

let deleteContextMenuItems (requestDeleteItem: FileItem -> unit) (item: FileItem) =
    if canDeleteItem item then
        [
            ContextMenuItems.styled "Delete" "swt:fluent--delete-24-regular" "swt:text-error" (fun () ->
                requestDeleteItem item)
        ]
    else
        []

let arcContextMenuItems (config: ContextMenuConfig) (item: FileItem) =
    [
        yield! arcCreateContextMenuItems config.openCreateModal item
        yield! renameContextMenuItems config.requestRenameItem item
        yield! deleteContextMenuItems config.requestDeleteItem item
    ]

let createContextMenuItems (config: ContextMenuConfig) =
    let toggleLfsMark =
        Renderer.Components.FileExplorerLfs.createToggleLfsMark
            config.enqueueError
            config.arcScopeId
            config.runToggleLfsMark

    let freeLocalLfsCopy =
        Renderer.Components.FileExplorerLfs.createFreeLocalLfsCopy
            config.enqueueError
            config.arcScopeId
            config.runFreeLocalLfsCopy

    fun item ->
        withDividers [
            openContextMenuItems config item
            copyPathContextMenuItems config.arcRootPath item
            fileSystemCreateContextMenuItems config.openFileSystemCreateModal item
            Swate.Components.Page.FileExplorer.FileExplorerGitLfsHelper.contextMenuItems
                item
                toggleLfsMark
                (Some freeLocalLfsCopy)
            arcContextMenuItems config item
        ]
