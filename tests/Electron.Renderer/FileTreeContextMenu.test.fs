module ElectronRenderer.FileTreeContextMenuTests

open Fable.Core
open Renderer.Components.LeftSidebar.FileExplorer.FileTreeContextMenu
open Swate.Components.Page.FileExplorer.Types
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
    runFreeLocalLfsCopy = fun _ -> promise { return Ok() }
}

let private createFileItem (name: string) (path: string option) =
    {
        FileTree.createFile name path FileItemIcon.Document with
            Id = defaultArg path name
    }

let private createFolderItem (name: string) (path: string option) =
    {
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
            item.Label)
    |> List.toArray

Vitest.describe("FileTreeContextMenu", fun () ->
    Vitest.test("folder path actions reveal the folder location only", fun () ->
        let item = createFolderItem "AssayA" (Some "assays/AssayA")
        let menuItems = pathActionContextMenuItems (createConfig ()) item

        Vitest.expect(labels menuItems).toEqual([| "Open Folder Location" |])
    )

    Vitest.test("file path actions reveal the location and open with the default application", fun () ->
        let item = createFileItem "protocol.md" (Some "assays/AssayA/protocol.md")
        let menuItems = pathActionContextMenuItems (createConfig ()) item

        Vitest.expect(labels menuItems).toEqual(
            [|
                "Open with Default Application"
                "Open Folder Location"
            |]
        )
    )

    Vitest.test("items without paths do not expose path actions", fun () ->
        let item = createFileItem "virtual.md" None
        let menuItems = pathActionContextMenuItems (createConfig ()) item

        Vitest.expect(menuItems.Length).toBe(0)
    )

    Vitest.test("absolute copy path resolver combines the active ARC root with filetree paths", fun () ->
        let item = createFileItem "protocol.md" (Some "assays/AssayA/protocol.md")

        Vitest.expect(tryGetAbsoluteItemPath (Some "C:\\arc-root") item).toEqual(
            Some "C:/arc-root/assays/AssayA/protocol.md"
        )
    )

    Vitest.test("relative copy path resolver keeps filetree paths relative", fun () ->
        let item = createFileItem "protocol.md" (Some "assays/AssayA/protocol.md")

        Vitest.expect(tryGetRelativeItemPath item).toEqual(Some "assays/AssayA/protocol.md")
    )

    Vitest.test("relative copy path resolver ignores missing paths", fun () ->
        let item = createFileItem "virtual.md" None

        Vitest.expect(tryGetRelativeItemPath item).toEqual(None)
    )

    Vitest.test("composed folder context menu is grouped with dividers", fun () ->
        let item = createFolderItem "AssayA" (Some "assays/AssayA")
        let menuItems = createContextMenuItems (createContextMenuConfig ()) item

        Vitest.expect(groupedLabels menuItems).toEqual(
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
                "<divider>"
                "Rename"
                "Delete"
            |]
        )
    )

    Vitest.test("new folder context menu action opens folder creation for the selected item", fun () ->
        let item = createFolderItem "AssayA" (Some "assays/AssayA")
        let mutable requestedCreate: (FileSystemItemKind * FileItem) option = None

        let menuItems =
            fileSystemCreateContextMenuItems
                (fun kind selectedItem -> requestedCreate <- Some(kind, selectedItem))
                item

        let newFolderItem = menuItems |> List.find (fun menuItem -> menuItem.Label = "New Folder")

        newFolderItem.OnClick()

        match requestedCreate with
        | Some(FileSystemItemKind.Folder, selectedItem) ->
            Vitest.expect(selectedItem.Id).toBe(item.Id)
        | Some(FileSystemItemKind.File, _) -> failwith "Expected folder creation to be requested."
        | None -> failwith "Expected new folder action to request creation."
    )

    Vitest.test("root ARC name context menu exposes generic root creation and ARC add actions", fun () ->
        let item = createFolderItem "MyArc" (Some "")
        let menuItems = rootContextMenuItems (createContextMenuConfig ()) item

        Vitest.expect(groupedLabels menuItems).toEqual(
            [|
                "New File"
                "New Folder"
                "<divider>"
                "Add Study"
                "Add Assay"
                "Add Workflow"
                "Add Run"
            |]
        )
    )

    Vitest.test("new folder action on the ARC root requests root-level folder creation", fun () ->
        let item = createFolderItem "MyArc" (Some "")
        let mutable requestedCreate: (FileSystemItemKind * FileItem) option = None

        let menuItems =
            fileSystemCreateContextMenuItems
                (fun kind selectedItem -> requestedCreate <- Some(kind, selectedItem))
                item

        let newFolderItem = menuItems |> List.find (fun menuItem -> menuItem.Label = "New Folder")

        newFolderItem.OnClick()

        match requestedCreate with
        | Some(FileSystemItemKind.Folder, selectedItem) ->
            Vitest.expect(selectedItem.Path).toEqual(Some "")
        | Some(FileSystemItemKind.File, _) -> failwith "Expected root folder creation to be requested."
        | None -> failwith "Expected new folder action to request root creation."
    )

    Vitest.test("generic file system creation is hidden for ARC collection roots", fun () ->
        let item = createFolderItem "assays" (Some "assays")
        let menuItems = fileSystemCreateContextMenuItems (fun _ _ -> ()) item

        Vitest.expect(menuItems.Length).toBe(0)
    )

    Vitest.test("composed file context menu is grouped with open, copy, git, and ARC actions", fun () ->
        let item = createFileItem "protocol.md" (Some "assays/AssayA/protocol.md")
        let menuItems = createContextMenuItems (createContextMenuConfig ()) item

        Vitest.expect(groupedLabels menuItems).toEqual(
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

    Vitest.test("delete action is styled as destructive ARC action", fun () ->
        let item = createFileItem "protocol.md" (Some "assays/AssayA/protocol.md")
        let menuItems = createContextMenuItems (createContextMenuConfig ()) item
        let deleteItem = menuItems |> List.find (fun menuItem -> menuItem.Label = "Delete")

        Vitest.expect(deleteItem.ClassName).toEqual(Some "swt:text-error")
    )
)
