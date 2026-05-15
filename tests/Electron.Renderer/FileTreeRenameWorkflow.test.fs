module ElectronRenderer.FileTreeRenameWorkflowTests

open Browser.Dom
open Browser.Types
open Feliz
open ARCtrl
open Renderer.Components.LeftSidebar.FileExplorer.FileTreeRenameHelper
open Renderer.Components.LeftSidebar.FileExplorer.Types
open Swate.Components.FileExplorer.Types
open Swate.Components.Shared
open Swate.Electron.Shared.FileIOTypes
open Vitest

module RenameWorkflow = Renderer.Components.LeftSidebar.FileExplorer.FileTreeRenameWorkflow

let rec private waitUntil (predicate: unit -> bool, attempts: int) =
    promise {
        if predicate () then
            return ()
        elif attempts <= 0 then
            failwith "Timed out waiting for rename workflow."
        else
            do! Promise.sleep 1
            return! waitUntil (predicate, attempts - 1)
    }

let private renderToBody (element: ReactElement) = promise {
    let container = document.createElement ("div") :?> HTMLDivElement
    document.body.appendChild container |> ignore
    let root = ReactDOM.createRoot container
    root.render element
    do! Promise.sleep 0

    return
        container,
        (fun () ->
            root.unmount ()
            container.remove ()
        )
}

let private createFileItem (name: string) (path: string) =
    {
        FileTree.createFile name (Some path) FileItemIcon.Document with
            Id = path
    }

let private createFolderItem (name: string) (path: string) =
    {
        FileTree.createFolder name (Some path) FileItemIcon.Folder with
            Id = path
    }

Vitest.afterEach (fun () -> document.body.innerHTML <- "")

Vitest.describe("FileTreeRenameHelper", fun () ->
    Vitest.test("tryBuildRenameDraft rejects canonical ARC files", fun () ->
        let item = createFileItem "isa.assay.xlsx" "assays/OldAssay/isa.assay.xlsx"

        match tryBuildRenameDraft item with
        | Ok _ -> failwith "Expected canonical ARC files to be non-renameable."
        | Error _ -> ()
    )

    Vitest.test("tryRemapSelectionPath remaps descendants under renamed source prefixes", fun () ->
        let remapped =
            tryRemapSelectionPath
                "assays/OldAssay"
                "assays/NewAssay"
                (Some "assays/OldAssay/notes/protocol.md")

        Vitest.expect(remapped).toEqual(Some "assays/NewAssay/notes/protocol.md")
    )
)

Vitest.describe("FileTreeRenameWorkflow", fun () ->
    let getRenameMenuItems (item: FileItem) =
        RenameWorkflow.renameContextMenuItems (fun _ -> ()) item

    Vitest.test("rename context menu item is shown for assay, study, workflow, and run entity folders", fun () ->
        let entityFolderPaths =
            [
                "assays/AssayA"
                "studies/StudyA"
                "workflows/WorkflowA"
                "runs/RunA"
            ]

        entityFolderPaths
        |> List.iter (fun path ->
            let item = createFolderItem (PathHelpers.getNameFromPath path) path
            let menuItems = getRenameMenuItems item
            Vitest.expect(menuItems.Length).toBe(1)
            Vitest.expect(menuItems.[0].Label).toBe("Rename")
            Vitest.expect(menuItems.[0].Icon).toBe("swt:fluent--edit-24-regular")
        )
    )

    Vitest.test("rename context menu item is hidden for canonical entity and datamap ARC files", fun () ->
        let canonicalFilePaths =
            [
                "assays/AssayA/isa.assay.xlsx"
                "assays/AssayA/isa.datamap.xlsx"
                "studies/StudyA/isa.study.xlsx"
                "studies/StudyA/isa.datamap.xlsx"
                "workflows/WorkflowA/isa.workflow.xlsx"
                "workflows/WorkflowA/isa.datamap.xlsx"
                "runs/RunA/isa.run.xlsx"
                "runs/RunA/isa.datamap.xlsx"
            ]

        canonicalFilePaths
        |> List.iter (fun path ->
            let item = createFileItem (PathHelpers.getNameFromPath path) path
            let menuItems = getRenameMenuItems item
            Vitest.expect(menuItems.Length).toBe(0)
        )
    )

    Vitest.test("rename context menu item is hidden for add-zone root folders", fun () ->
        let addZoneRootPaths =
            [
                "assays"
                "studies"
                "workflows"
                "runs"
            ]

        addZoneRootPaths
        |> List.iter (fun path ->
            let item = createFolderItem path path
            let menuItems = getRenameMenuItems item
            Vitest.expect(menuItems.Length).toBe(0)
        )
    )

    Vitest.test("rename context menu item is hidden for generic descendants", fun () ->
        let item = createFileItem "custom.txt" "studies/MyStudy/notes/custom.txt"
        let menuItems = getRenameMenuItems item
        Vitest.expect(menuItems.Length).toBe(0)
    )

    Vitest.test("rename context menu action requests a rename draft for renameable items", fun () ->
        let item = createFolderItem "OldAssay" "assays/OldAssay"
        let mutable pendingRenameDraft: ArcRenameDraft option = None

        let requestRenameItem =
            RenameWorkflow.requestRenameItem (fun draft -> pendingRenameDraft <- draft) ignore None

        let menuItems =
            RenameWorkflow.renameContextMenuItems requestRenameItem item

        Vitest.expect(menuItems.Length).toBe(1)
        menuItems.[0].OnClick()
        Vitest.expect(pendingRenameDraft.IsSome).toBe(true)
    )

    Vitest.test("rename request surfaces validation errors instead of silently swallowing", fun () ->
        let item = createFolderItem "assays" "assays"
        let mutable pendingRenameDraft: ArcRenameDraft option = None
        let mutable errorCount = 0

        RenameWorkflow.requestRenameItem
            (fun draft -> pendingRenameDraft <- draft)
            (fun _ -> errorCount <- errorCount + 1)
            None
            item

        Vitest.expect(pendingRenameDraft).toEqual(None)
        Vitest.expect(errorCount).toBe(1)
    )

    Vitest.test("inline rename button dispatches for assay, study, workflow, and run entity folders", fun () ->
        promise {
            let entityFolderPaths =
                [
                    "assays/AssayA"
                    "studies/StudyA"
                    "workflows/WorkflowA"
                    "runs/RunA"
                ]

            let items =
                entityFolderPaths
                |> List.map (fun path -> createFolderItem (PathHelpers.getNameFromPath path) path)

            let mutable renamedPaths: string list = []

            let onRenameItem (item: FileItem) =
                let path = item.Path |> Option.defaultValue ""
                renamedPaths <- path :: renamedPaths

            let! container, cleanup =
                renderToBody (
                    Swate.Components.FileExplorer.FileExplorer.FileExplorer(
                        initialItems = items,
                        getItemActions = RenameWorkflow.renameContextMenuItems onRenameItem
                    )
                )

            try
                entityFolderPaths
                |> List.iter (fun path ->
                    let itemName = PathHelpers.getNameFromPath path
                    let buttonSelector = $"button[aria-label='Rename {itemName}']"
                    let renameButton = container.querySelector buttonSelector

                    Vitest.expect(renameButton).not.toBeNull ()
                    (renameButton :?> HTMLElement).click ()
                )

                do! Promise.sleep 0

                Vitest.expect(renamedPaths |> List.rev).toEqual(entityFolderPaths)
            finally
                cleanup ()
        }
    )

    Vitest.test("confirmRenameItem dispatches renamePath, remaps active selection, and refreshes git status", fun () ->
        promise {
            let renameDraftResult =
                createFolderItem "OldAssay" "assays/OldAssay"
                |> tryBuildRenameDraft

            let renameDraft =
                match renameDraftResult with
                | Ok draft -> draft
                | Error error -> failwith error

            let mutable renameRequest: RenamePathRequest option = None
            let mutable renamedSelection: ArcSelection option = None
            let mutable closed = false
            let mutable gitStatusRefreshCount = 0
            let mutable reloadedPreviewPath: string option = None

            let config: RenameWorkflow.ConfirmRenameConfig = {
                pendingRenameDraft = Some renameDraft
                selectedTreePath = Some "assays/OldAssay/notes/protocol.md"
                pageState = Some(Renderer.Types.PageState.ArcFilePage(ArcFiles.Assay(ArcAssay.init "OldAssay")))
                closeRenameModal = fun () -> closed <- true
                setIsRenaming = ignore
                setSelection = fun selection -> renamedSelection <- Some selection
                refreshGitStatus = fun () -> gitStatusRefreshCount <- gitStatusRefreshCount + 1
                reloadPreviewByPath =
                    fun path ->
                        promise {
                            reloadedPreviewPath <- Some path
                            return Ok()
                        }
                renamePath =
                    fun request ->
                        promise {
                            renameRequest <- Some request
                            return Ok()
                        }
                enqueueError = fun _ -> failwith "Did not expect rename error."
                arcScopeId = None
            }

            RenameWorkflow.confirmRenameItem config "NewAssay"
            do! waitUntil ((fun () -> renameRequest.IsSome && closed), 50)

            match renameRequest with
            | None -> failwith "Expected renamePath to be dispatched."
            | Some request ->
                Vitest.expect(request.relativePath).toBe("assays/OldAssay")
                Vitest.expect(request.newName).toBe("NewAssay")

            Vitest.expect(renamedSelection.IsSome).toBe(true)
            Vitest.expect(renamedSelection.Value.TreePath).toEqual(Some "assays/NewAssay/notes/protocol.md")
            Vitest.expect(reloadedPreviewPath).toEqual(Some "assays/NewAssay/isa.assay.xlsx")
            Vitest.expect(gitStatusRefreshCount).toBe(1)
        }
    )
)
