module ElectronRenderer.FileTreeContextMenuTests

open Renderer.Components.LeftSidebar.FileExplorer.Helper
open Renderer.Components.LeftSidebar.FileExplorer.FileTreeContextMenu
open Swate.Components.Page.FileExplorer.Types
open Swate.Components.Shared
open Swate.Electron.Shared.FileIOTypes
open Vitest

let private createConfig () : PathActionConfig = {
    openPathInFileExplorer = fun _ -> promise { return Ok() }
    openPathWithDefaultApplication = fun _ -> promise { return Ok() }
    enqueueError = ignore
}

let private createContextMenuConfig () : ContextMenuConfig = {
    openItem = ignore
    arcRootPath = Some "C:\\arc-root"
    openCreateModal = ignore
    openFileSystemCreateModal = fun _ _ -> ()
    requestRenameItem = ignore
    requestDeleteItem = ignore
    pathActionConfig = createConfig ()
    enqueueError = ignore
    runToggleLfsMark = fun _ _ -> promise { return Ok() }
    runDownloadLfsFile = fun _ -> promise { return Ok() }
    runFreeLocalLfsCopy = fun _ -> promise { return Ok() }
}

let private createComposedContextMenuItems config item = createContextMenuItems config None item

let private createFileItem (name: string) (path: string option) = {
    FileTree.createFile name path FileItemIcon.Document with
        Id = defaultArg path name
}

let private createLfsFileItem (name: string) (path: string) (downloaded: bool) (isPointer: bool) = {
    createFileItem name (Some path) with
        IsLFS = Some true
        Downloaded = Some downloaded
        IsLFSPointer = Some isPointer
        SizeFormatted = Some "42 MB"
}

let private createFolderItem (name: string) (path: string option) = {
    FileTree.createFolder name path FileItemIcon.Folder with
        Id = defaultArg path name
}

let private labels items =
    items |> List.map _.Label |> List.toArray

let private groupedLabels items =
    items
    |> List.map (fun item ->
        if defaultArg item.IsDivider false then
            "<divider>"
        else
            item.Label
    )
    |> List.toArray

let private rootNotesActionContextMenuItems =
    rootFolderContextMenuItems "notes" "Create new item in" "swt:fluent--note-add-24-regular"

Vitest.describe (
    "FileTreeContextMenu",
    fun () ->
        Vitest.test (
            "ARC create drafts include a basic identifier-named annotation table when supported",
            fun () ->
                let tableCapableKinds = [|
                    ArcExplorerNodeKind.Study
                    ArcExplorerNodeKind.Assay
                    ArcExplorerNodeKind.Run
                |]

                for kind in tableCapableKinds do
                    let identifier = $"Default {ArcExplorerNodeKind.label kind}"

                    match tryCreateArcFile kind identifier with
                    | Ok arcFile ->
                        let tables = arcFile.Tables()
                        Vitest.expect(tables.Count).toBe (1)
                        let table = tables.[0]
                        Vitest.expect(table.Name).toBe ($"{identifier} Table")
                        Vitest.expect(table.ColumnCount).toBe (3)
                        Vitest.expect(table.RowCount).toBe (ARCtrlHelper.ArcFileDefaults.BasicAnnotationTableRowCount)
                        Vitest.expect(table.Headers.[0].ToString()).toBe ("Input [Source Name]")
                        Vitest.expect(table.Headers.[1].ToString()).toBe ("Protocol Uri")
                        Vitest.expect(table.Headers.[2].ToString()).toBe ("Output [Sample Name]")
                    | Error error -> failwith error

                match tryCreateArcFile ArcExplorerNodeKind.Workflow "Default Workflow" with
                | Ok arcFile -> Vitest.expect(arcFile.Tables().Count).toBe (0)
                | Error error -> failwith error
        )

        Vitest.test (
            "folder path actions reveal the folder location only",
            fun () ->
                let item = createFolderItem "AssayA" (Some "assays/AssayA")
                let menuItems = pathActionContextMenuItems (createConfig ()) item

                Vitest.expect(labels menuItems).toEqual ([| "Open Folder Location" |])
        )

        Vitest.test (
            "file path actions reveal the location and open with the default application",
            fun () ->
                let item = createFileItem "protocol.md" (Some "assays/AssayA/protocol.md")
                let menuItems = pathActionContextMenuItems (createConfig ()) item

                Vitest
                    .expect(labels menuItems)
                    .toEqual (
                        [|
                            "Open with Default Application"
                            "Open Folder Location"
                        |]
                    )
        )

        Vitest.test (
            "items without paths do not expose path actions",
            fun () ->
                let item = createFileItem "virtual.md" None
                let menuItems = pathActionContextMenuItems (createConfig ()) item

                Vitest.expect(menuItems.Length).toBe (0)
        )

        Vitest.test (
            "absolute copy path resolver combines the active ARC root with filetree paths",
            fun () ->
                let item = createFileItem "protocol.md" (Some "assays/AssayA/protocol.md")

                Vitest
                    .expect(tryGetAbsoluteItemPath (Some "C:\\arc-root") item)
                    .toEqual (Some "C:/arc-root/assays/AssayA/protocol.md")
        )

        Vitest.test (
            "relative copy path resolver keeps filetree paths relative",
            fun () ->
                let item = createFileItem "protocol.md" (Some "assays/AssayA/protocol.md")

                Vitest.expect(tryGetRelativeItemPath item).toEqual (Some "assays/AssayA/protocol.md")
        )

        Vitest.test (
            "relative copy path resolver ignores missing paths",
            fun () ->
                let item = createFileItem "virtual.md" None

                Vitest.expect(tryGetRelativeItemPath item).toEqual (None)
        )

        Vitest.test (
            "composed folder context menu is grouped with dividers",
            fun () ->
                let item = createFolderItem "AssayA" (Some "assays/AssayA")
                let menuItems = createComposedContextMenuItems (createContextMenuConfig ()) item

                Vitest
                    .expect(groupedLabels menuItems)
                    .toEqual (
                        [|
                            "Open"
                            "Open Folder Location"
                            "<divider>"
                            "Copy Path"
                            "Copy Full Path"
                            "<divider>"
                            "New File"
                            "New Folder"
                            "<divider>"
                            "Add Study"
                            "Add Assay"
                            "Add Workflow"
                            "Add Run"
                            "Add Note"
                            "<divider>"
                            "Rename"
                            "Delete"
                        |]
                    )
        )

        Vitest.test (
            "new folder context menu action opens folder creation for the selected item",
            fun () ->
                let item = createFolderItem "AssayA" (Some "assays/AssayA")
                let mutable requestedCreate: (FileSystemItemKind * FileItem) option = None

                let menuItems =
                    fileSystemCreateContextMenuItems
                        (fun kind selectedItem -> requestedCreate <- Some(kind, selectedItem))
                        item

                let newFolderItem =
                    menuItems |> List.find (fun menuItem -> menuItem.Label = "New Folder")

                newFolderItem.OnClick()

                match requestedCreate with
                | Some(FileSystemItemKind.Folder, selectedItem) -> Vitest.expect(selectedItem.Id).toBe (item.Id)
                | Some(FileSystemItemKind.File, _) -> failwith "Expected folder creation to be requested."
                | None -> failwith "Expected new folder action to request creation."
        )

        Vitest.test (
            "root ARC name context menu exposes generic root creation and ARC add actions",
            fun () ->
                let item = createFolderItem "MyArc" (Some "")
                let menuItems = rootContextMenuItems (createContextMenuConfig ()) item

                Vitest
                    .expect(groupedLabels menuItems)
                    .toEqual (
                        [|
                            "New File"
                            "New Folder"
                            "<divider>"
                            "Add Study"
                            "Add Assay"
                            "Add Workflow"
                            "Add Run"
                            "Add Note"
                        |]
                    )
        )

        Vitest.test (
            "add note action requests note creation",
            fun () ->
                let item = createFolderItem "AssayA" (Some "assays/AssayA")
                let mutable requestedCreateKind = None

                let config = {
                    createContextMenuConfig () with
                        openCreateModal = fun kind -> requestedCreateKind <- Some kind
                }

                let menuItems = createComposedContextMenuItems config item

                let addNoteItem =
                    menuItems |> List.find (fun menuItem -> menuItem.Label = "Add Note")

                addNoteItem.OnClick()

                Vitest.expect(requestedCreateKind).toEqual (Some ArcExplorerNodeKind.Note)
        )

        Vitest.test (
            "root notes folder row exposes add note action",
            fun () ->
                let item = createFolderItem "notes" (Some "notes")
                let mutable didRequestNote = false

                let menuItems =
                    rootNotesActionContextMenuItems (fun () -> didRequestNote <- true) item

                Vitest.expect(labels menuItems).toEqual ([| "Create new item in" |])
                Vitest.expect(menuItems.Head.Icon).toBe ("swt:fluent--note-add-24-regular")

                menuItems.Head.OnClick()

                Vitest.expect(didRequestNote).toBe (true)
        )

        Vitest.test (
            "root notes action is hidden for nested notes folders",
            fun () ->
                let item = createFolderItem "2026-06-15" (Some "notes/2026-06-15")
                let menuItems = rootNotesActionContextMenuItems ignore item

                Vitest.expect(menuItems.Length).toBe (0)
        )

        Vitest.test (
            "root notes folder context menu does not expose rename",
            fun () ->
                let item = createFolderItem "notes" (Some "notes")
                let menuItems = createComposedContextMenuItems (createContextMenuConfig ()) item

                Vitest.expect(groupedLabels menuItems).not.toContain ("Rename")
        )

        Vitest.test (
            "native structural entity child folders do not expose rename or delete",
            fun () ->
                let protectedPaths = [
                    "assays/AssayA/dataset"
                    "assays/AssayA/protocols"
                    "studies/StudyA/protocols"
                    "studies/StudyA/resources"
                ]

                protectedPaths
                |> List.iter (fun path ->
                    let item = createFolderItem (PathHelpers.getNameFromPath path) (Some path)

                    let menuItemLabels =
                        createComposedContextMenuItems (createContextMenuConfig ()) item
                        |> groupedLabels

                    Vitest.expect(menuItemLabels).toContain ("New File")
                    Vitest.expect(menuItemLabels).toContain ("New Folder")
                    Vitest.expect(menuItemLabels).not.toContain ("Rename")
                    Vitest.expect(menuItemLabels).not.toContain ("Delete")
                )
        )

        Vitest.test (
            "new folder action on the ARC root requests root-level folder creation",
            fun () ->
                let item = createFolderItem "MyArc" (Some "")
                let mutable requestedCreate: (FileSystemItemKind * FileItem) option = None

                let menuItems =
                    fileSystemCreateContextMenuItems
                        (fun kind selectedItem -> requestedCreate <- Some(kind, selectedItem))
                        item

                let newFolderItem =
                    menuItems |> List.find (fun menuItem -> menuItem.Label = "New Folder")

                newFolderItem.OnClick()

                match requestedCreate with
                | Some(FileSystemItemKind.Folder, selectedItem) -> Vitest.expect(selectedItem.Path).toEqual (Some "")
                | Some(FileSystemItemKind.File, _) -> failwith "Expected root folder creation to be requested."
                | None -> failwith "Expected new folder action to request root creation."
        )

        Vitest.test (
            "generic file system creation is hidden for ARC collection roots",
            fun () ->
                let item = createFolderItem "assays" (Some "assays")
                let menuItems = fileSystemCreateContextMenuItems (fun _ _ -> ()) item

                Vitest.expect(menuItems.Length).toBe (0)
        )

        Vitest.test (
            "composed file context menu is grouped with open, copy, git, and ARC actions",
            fun () ->
                let item = createFileItem "protocol.md" (Some "assays/AssayA/protocol.md")
                let menuItems = createComposedContextMenuItems (createContextMenuConfig ()) item

                Vitest
                    .expect(groupedLabels menuItems)
                    .toEqual (
                        [|
                            "Open"
                            "Open with Default Application"
                            "Open Folder Location"
                            "<divider>"
                            "Copy Path"
                            "Copy Full Path"
                            "<divider>"
                            "Mark Git LFS"
                            "Git LFS: not marked"
                            "<divider>"
                            "Rename"
                            "Delete"
                        |]
                    )
        )

        Vitest.test (
            "composed LFS pointer menu enables download and disables freeing the local copy",
            fun () -> promise {
                let item = createLfsFileItem "pointer.bin" "data/pointer.bin" false true
                let mutable downloadedPath = None

                let config = {
                    createContextMenuConfig () with
                        runDownloadLfsFile =
                            fun path -> promise {
                                downloadedPath <- Some path
                                return Ok()
                            }
                }

                let menuItems = createComposedContextMenuItems config item

                Vitest.expect(groupedLabels menuItems).toContain ("Download LFS file")

                let downloadItem =
                    menuItems |> List.find (fun menuItem -> menuItem.Label = "Download LFS file")

                let freeItem =
                    menuItems |> List.find (fun menuItem -> menuItem.Label = "Free local LFS copy")

                Vitest.expect(downloadItem.Disabled).toEqual (None)
                Vitest.expect(freeItem.Disabled).toEqual (Some true)

                downloadItem.OnClick()
                do! Promise.sleep 0

                Vitest.expect(downloadedPath).toEqual (Some "data/pointer.bin")
            }
        )

        Vitest.test (
            "composed downloaded LFS menu disables download and enables freeing the local copy",
            fun () -> promise {
                let item = createLfsFileItem "downloaded.bin" "data/downloaded.bin" true false
                let mutable freedPath = None

                let config = {
                    createContextMenuConfig () with
                        runFreeLocalLfsCopy =
                            fun path -> promise {
                                freedPath <- Some path
                                return Ok()
                            }
                }

                let menuItems = createComposedContextMenuItems config item

                let downloadItem =
                    menuItems |> List.find (fun menuItem -> menuItem.Label = "Download LFS file")

                let freeItem =
                    menuItems |> List.find (fun menuItem -> menuItem.Label = "Free local LFS copy")

                Vitest.expect(downloadItem.Disabled).toEqual (Some true)
                Vitest.expect(freeItem.Disabled).toEqual (None)

                freeItem.OnClick()
                do! Promise.sleep 0

                Vitest.expect(freedPath).toEqual (Some "data/downloaded.bin")
            }
        )

        Vitest.test (
            "delete action is styled as destructive ARC action",
            fun () ->
                let item = createFileItem "protocol.md" (Some "assays/AssayA/protocol.md")
                let menuItems = createComposedContextMenuItems (createContextMenuConfig ()) item
                let deleteItem = menuItems |> List.find (fun menuItem -> menuItem.Label = "Delete")

                Vitest.expect(deleteItem.ClassName).toEqual (Some "swt:text-error")
        )
)
