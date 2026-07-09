module ElectronRenderer.FileTreeContextMenuTests

open Browser.Dom
open Browser.Types
open Fable.Core
open Feliz
open Renderer.Components.LeftSidebar.FileExplorer.Helper
open Renderer.Components.LeftSidebar.FileExplorer.Types
open Renderer.Components.LeftSidebar.FileExplorer.FileTreeAssignNoteHelper
open Renderer.Components.LeftSidebar.FileExplorer.FileTreeContextMenu
open Swate.Components.Page.FileExplorer.Modals
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
    openCreateModal = ignore
    openFileSystemCreateModal = fun _ _ -> ()
    requestAssignNoteItem = ignore
    requestRenameItem = ignore
    requestDeleteItem = ignore
    pathActionConfig = createConfig ()
    enqueueError = ignore
    runToggleLfsMark = fun _ _ -> promise { return Ok() }
    runDownloadLfsFile = fun _ -> promise { return Ok() }
    runFreeLocalLfsCopy = fun _ -> promise { return Ok() }
}

let private createComposedContextMenuItems config item =
    createContextMenuItems config (Some "C:/Arc") item

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

let rec private waitUntil (predicate: unit -> bool, attempts: int) = promise {
    if predicate () then
        return ()
    elif attempts <= 0 then
        failwith "Timed out waiting for condition."
    else
        do! Promise.sleep 0
        return! waitUntil (predicate, attempts - 1)
}

let private assignableNote sourceFolderPath noteFolderName : AssignableNoteRef = {
    SourceFolderPath = sourceFolderPath
    NoteFolderName = noteFolderName
    Label = noteFolderName
}

let private assignableAsset sourceRelativePath relativeAssetPath : AssignableNoteAssetRef = {
    SourceRelativePath = sourceRelativePath
    RelativeAssetPath = relativeAssetPath
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

let private optionLabels (container: HTMLDivElement) =
    let optionNodes = container.querySelectorAll ("select option")

    [|
        for index in 0 .. optionNodes.length - 1 do
            optionNodes.[index].textContent
    |]

let private optionValues (container: HTMLDivElement) =
    let optionNodes = container.querySelectorAll ("select option")

    [|
        for index in 0 .. optionNodes.length - 1 do
            (optionNodes.[index] :?> HTMLOptionElement).value
    |]

let private selectAt (container: HTMLDivElement) index =
    (container.querySelectorAll "select").[index] :?> HTMLSelectElement

let private optionLabelsForSelect (container: HTMLDivElement) index =
    let optionNodes = (selectAt container index).querySelectorAll ("option")

    [|
        for optionIndex in 0 .. optionNodes.length - 1 do
            optionNodes.[optionIndex].textContent
    |]

let private optionValuesForSelect (container: HTMLDivElement) index =
    let optionNodes = (selectAt container index).querySelectorAll ("option")

    [|
        for optionIndex in 0 .. optionNodes.length - 1 do
            (optionNodes.[optionIndex] :?> HTMLOptionElement).value
    |]

let private selectValues (container: HTMLDivElement) =
    let selectNodes = container.querySelectorAll ("select")

    [|
        for index in 0 .. selectNodes.length - 1 do
            (selectNodes.[index] :?> HTMLSelectElement).value
    |]

[<Emit("""
const setter = Object.getOwnPropertyDescriptor(HTMLSelectElement.prototype, "value").set;
setter.call($0, $1);
$0.dispatchEvent(new Event("change", { bubbles: true }));
""")>]
let private changeSelectValue (element: HTMLSelectElement) (value: string) : unit = jsNative

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
            "relative copy path resolver keeps filetree paths relative",
            fun () ->
                let item = createFileItem "protocol.md" (Some "assays/AssayA/protocol.md")

                Vitest.expect(tryGetNonEmptyItemRelativePath item).toEqual (Some "assays/AssayA/protocol.md")
        )

        Vitest.test (
            "relative copy path resolver ignores missing paths",
            fun () ->
                let item = createFileItem "virtual.md" None

                Vitest.expect(tryGetNonEmptyItemRelativePath item).toEqual (None)
        )

        Vitest.test (
            "full path copy resolver combines the renderer ARC root and relative path",
            fun () ->
                let item = createFileItem "protocol.md" (Some "assays/AssayA/protocol.md")

                Vitest
                    .expect(tryGetItemAbsolutePath (Some "C:/Arc") item)
                    .toEqual (Some "C:/Arc/assays/AssayA/protocol.md")
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
                            "Assign Note"
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
            "root notes folder context menu does not expose rename or delete",
            fun () ->
                let item = createFolderItem "notes" (Some "notes")
                let menuItems = createComposedContextMenuItems (createContextMenuConfig ()) item

                Vitest.expect(groupedLabels menuItems).not.toContain ("Rename")
                Vitest.expect(groupedLabels menuItems).not.toContain ("Delete")
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
            "assign note context menu item is shown for assay folders",
            fun () ->
                let item = createFolderItem "AssayA" (Some "assays/AssayA")

                let mutable assignedItem = None

                let menuItems =
                    assignNoteContextMenuItems (fun item -> assignedItem <- Some item) item

                Vitest.expect(labels menuItems).toEqual ([| "Assign Note" |])

                menuItems.Head.OnClick()

                let assignedPath = assignedItem |> Option.bind _.Path
                Vitest.expect(assignedPath).toEqual (Some "assays/AssayA")
        )

        Vitest.test (
            "assign note context menu item is shown for study folders",
            fun () ->
                let item = createFolderItem "StudyA" (Some "studies/StudyA")

                let menuItems = assignNoteContextMenuItems ignore item

                Vitest.expect(labels menuItems).toEqual ([| "Assign Note" |])
        )

        Vitest.test (
            "note assignment target resolves study and assay folders",
            fun () ->
                let assayItem = createFolderItem "AssayA" (Some "assays/AssayA")
                let studyItem = createFolderItem "StudyA" (Some "studies/StudyA")

                Vitest
                    .expect(tryGetNoteAssignmentTarget assayItem)
                    .toEqual (
                        Some {
                            Name = "AssayA"
                            Kind = NotesTargetKind.Assay
                        }
                    )

                Vitest
                    .expect(tryGetNoteAssignmentTarget studyItem)
                    .toEqual (
                        Some {
                            Name = "StudyA"
                            Kind = NotesTargetKind.Study
                        }
                    )
        )

        Vitest.test (
            "assign note context menu item is hidden for note markdown files",
            fun () ->
                let item =
                    createFileItem
                        "Sampling_protocol.md"
                        (Some "notes/2026-06-15/Sampling_protocol/Sampling_protocol.md")

                let menuItems = assignNoteContextMenuItems ignore item

                Vitest.expect(menuItems.Length).toBe (0)
        )

        Vitest.test (
            "assign note context menu item is hidden for note folders",
            fun () ->
                let item =
                    createFolderItem "Sampling_protocol" (Some "notes/2026-06-15/Sampling_protocol")

                let menuItems = assignNoteContextMenuItems ignore item

                Vitest.expect(menuItems.Length).toBe (0)
        )

        Vitest.test (
            "assignable note options list root notes only",
            fun () ->
                let notes =
                    createAssignableNoteOptions [
                        FileEntry.create (
                            "Sampling_protocol.md",
                            "notes/2026-06-15/Sampling_protocol/Sampling_protocol.md",
                            false
                        )
                        FileEntry.create ("Sampling_protocol", "notes/2026-06-15/Sampling_protocol", true)
                        FileEntry.create (
                            "Assigned_protocol.md",
                            "assays/AssayA/protocols/Assigned_protocol/Assigned_protocol.md",
                            false
                        )
                        FileEntry.create ("draft.md", "notes/2026-06-15/Other_folder/draft.md", false)
                    ]

                Vitest.expect(notes.Count).toBe (1)
                Vitest.expect(notes.[0].SourceFolderPath).toBe ("notes/2026-06-15/Sampling_protocol")
                Vitest.expect(notes.[0].NoteFolderName).toBe ("Sampling_protocol")
        )

        Vitest.test (
            "assignable note asset options list assets for the selected note",
            fun () ->
                let note = assignableNote "notes/2026-06-15/Sampling_protocol" "Sampling_protocol"

                let assets =
                    createAssignableNoteAssetOptions
                        [
                            FileEntry.create (
                                "diagram.png",
                                "notes/2026-06-15/Sampling_protocol/assets/diagram.png",
                                false
                            )
                            FileEntry.create (
                                "raw.csv",
                                "notes/2026-06-15/Sampling_protocol/assets/nested/raw.csv",
                                false
                            )
                            FileEntry.create ("other.png", "notes/2026-06-15/Other/assets/other.png", false)
                            FileEntry.create ("assets", "notes/2026-06-15/Sampling_protocol/assets", true)
                        ]
                        (Some note)

                Vitest.expect(assets.Count).toBe (2)
                Vitest.expect(assets.[0].RelativeAssetPath).toBe ("diagram.png")
                Vitest.expect(assets.[1].RelativeAssetPath).toBe ("nested/raw.csv")
        )

        Vitest.test (
            "assigned note folder path targets the protocol folder",
            fun () ->
                let target = {
                    Name = "AssayA"
                    Kind = NotesTargetKind.Assay
                }

                Vitest
                    .expect(buildAssignedNoteFolderPath target "Sampling_protocol")
                    .toBe ("assays/AssayA/protocols/Sampling_protocol")
        )

        Vitest.test (
            "assigned note folder name keeps the root note date",
            fun () ->
                let note = assignableNote "notes/2026-06-15/Sampling_protocol" "Sampling_protocol"

                Vitest.expect(buildAssignedNoteFolderName note).toBe ("2026-06-15_Sampling_protocol")
        )

        Vitest.test (
            "assignable asset destinations follow the selected target entity folders",
            fun () ->
                let assayTarget = {
                    Name = "AssayA"
                    Kind = NotesTargetKind.Assay
                }

                let studyTarget = {
                    Name = "StudyA"
                    Kind = NotesTargetKind.Study
                }

                Vitest
                    .expect(assignableAssetDestinationsForTarget assayTarget |> List.toArray)
                    .toEqual (
                        [|
                            AssignNoteAssetDestination.Protocol
                            AssignNoteAssetDestination.Dataset
                        |]
                    )

                Vitest
                    .expect(assignableAssetDestinationsForTarget studyTarget |> List.toArray)
                    .toEqual (
                        [|
                            AssignNoteAssetDestination.Protocol
                            AssignNoteAssetDestination.Resource
                        |]
                    )
        )

        Vitest.test (
            "asset selector only displays destinations valid for the target entity",
            fun () -> promise {
                let assets =
                    ResizeArray [
                        assignableAsset "notes/2026-06-15/Sampling_protocol/assets/data.csv" "data.csv"
                    ]

                let assayTarget = {
                    Name = "AssayA"
                    Kind = NotesTargetKind.Assay
                }

                let studyTarget = {
                    Name = "StudyA"
                    Kind = NotesTargetKind.Study
                }

                let! assayContainer, assayCleanup =
                    let availableDestinations = assignableAssetDestinationsForTarget assayTarget

                    AssignNoteAssetSelector.AssignNoteAssetSelector(
                        assets,
                        availableDestinations,
                        Map.ofList [
                            (assets.[0].SourceRelativePath, AssignNoteAssetDestination.Protocol)
                        ],
                        (fun _ _ -> ())
                    )
                    |> renderToBody

                try
                    Vitest
                        .expect(optionLabelsForSelect assayContainer 0)
                        .toEqual ([| "Do not assign"; "protocol"; "dataset" |])

                    Vitest.expect(optionValuesForSelect assayContainer 0).toEqual ([| ""; "protocol"; "dataset" |])

                    Vitest
                        .expect(optionLabelsForSelect assayContainer 1)
                        .toEqual ([| "Do not assign"; "protocol"; "dataset" |])

                    Vitest.expect(selectValues assayContainer).toEqual ([| "protocol"; "protocol" |])
                finally
                    assayCleanup ()

                let! studyContainer, studyCleanup =
                    let availableDestinations = assignableAssetDestinationsForTarget studyTarget

                    AssignNoteAssetSelector.AssignNoteAssetSelector(
                        assets,
                        availableDestinations,
                        Map.ofList [
                            (assets.[0].SourceRelativePath, AssignNoteAssetDestination.Protocol)
                        ],
                        (fun _ _ -> ())
                    )
                    |> renderToBody

                try
                    Vitest
                        .expect(optionLabelsForSelect studyContainer 0)
                        .toEqual ([| "Do not assign"; "protocol"; "resource" |])

                    Vitest.expect(optionValuesForSelect studyContainer 0).toEqual ([| ""; "protocol"; "resource" |])

                    Vitest
                        .expect(optionLabelsForSelect studyContainer 1)
                        .toEqual ([| "Do not assign"; "protocol"; "resource" |])

                    Vitest.expect(selectValues studyContainer).toEqual ([| "protocol"; "protocol" |])
                finally
                    studyCleanup ()
            }
        )

        Vitest.test (
            "assign note modal disables interactions and submits selected asset destinations",
            fun () -> promise {
                let note = assignableNote "notes/2026-06-15/Sampling_protocol" "Sampling_protocol"

                let asset =
                    assignableAsset "notes/2026-06-15/Sampling_protocol/assets/data.csv" "data.csv"

                let mutable submitCalls = 0
                let mutable submittedNote: AssignableNoteRef option = None

                let mutable submittedDestinations: Map<string, AssignNoteAssetDestination> option =
                    None

                let mutable resolveSubmit: (unit -> unit) option = None

                let submit note assetDestinations =
                    submitCalls <- submitCalls + 1
                    submittedNote <- Some note
                    submittedDestinations <- Some assetDestinations
                    Promise.create (fun resolve _reject -> resolveSubmit <- Some resolve)

                let! _container, cleanup =
                    AssignNoteModal.AssignNoteModal(
                        isOpen = true,
                        itemName = Some "AssayA",
                        selectedNote = Some note,
                        setSelectedNote = ignore,
                        availableNotes = ResizeArray [ note ],
                        availableAssets = ResizeArray [ asset ],
                        availableAssetDestinations = [
                            AssignNoteAssetDestination.Protocol
                            AssignNoteAssetDestination.Dataset
                        ],
                        close = ignore,
                        submit = submit
                    )
                    |> renderToBody

                try
                    let modal =
                        document.body.querySelector ("[data-testid='modal_arc-assign-note']") :?> HTMLElement

                    let selectAt index =
                        (modal.querySelectorAll "select").[index] :?> HTMLSelectElement

                    let assignButton () =
                        (modal.querySelectorAll "button").[2] :?> HTMLButtonElement

                    let disabledStates () =
                        let selectNodes = modal.querySelectorAll ("select")

                        [|
                            for index in 0 .. selectNodes.length - 1 do
                                (selectNodes.[index] :?> HTMLSelectElement).disabled

                            (assignButton ()).disabled
                        |]

                    Vitest.expect(disabledStates ()).toEqual ([| false; false; false; false |])
                    Vitest.expect((selectAt 2).value).toBe ("protocol")

                    changeSelectValue (selectAt 2) "dataset"
                    do! Promise.sleep 0

                    assignButton().click ()

                    do! waitUntil ((fun () -> submitCalls = 1 && resolveSubmit.IsSome), 50)
                    do! waitUntil ((fun () -> disabledStates () |> Array.forall id), 50)

                    Vitest.expect(submittedNote).toEqual (Some note)

                    match submittedDestinations with
                    | Some destinations ->
                        Vitest.expect(destinations.Count).toBe (1)

                        Vitest
                            .expect(destinations |> Map.tryFind asset.SourceRelativePath)
                            .toEqual (Some AssignNoteAssetDestination.Dataset)
                    | None -> failwith "Expected submit to receive asset destinations."

                    resolveSubmit.Value()
                    do! waitUntil ((fun () -> disabledStates () |> Array.forall not), 50)
                finally
                    cleanup ()
            }
        )

        Vitest.test (
            "asset selector header destination overwrites every asset selector",
            fun () -> promise {
                let assets =
                    ResizeArray [
                        assignableAsset "notes/2026-06-15/Sampling_protocol/assets/data.csv" "data.csv"
                        assignableAsset "notes/2026-06-15/Sampling_protocol/assets/diagram.png" "diagram.png"
                    ]

                let availableDestinations = [
                    AssignNoteAssetDestination.Protocol
                    AssignNoteAssetDestination.Dataset
                ]

                let assetDestinations =
                    [
                        "notes/2026-06-15/Sampling_protocol/assets/data.csv", AssignNoteAssetDestination.Protocol
                        "notes/2026-06-15/Sampling_protocol/assets/diagram.png", AssignNoteAssetDestination.Dataset
                    ]
                    |> Map.ofList

                let updates = ResizeArray<string * AssignNoteAssetDestination option>()

                let! container, cleanup =
                    AssignNoteAssetSelector.AssignNoteAssetSelector(
                        assets,
                        availableDestinations,
                        assetDestinations,
                        (fun assetPath destination -> updates.Add(assetPath, destination))
                    )
                    |> renderToBody

                try
                    Vitest
                        .expect(optionLabelsForSelect container 0)
                        .toEqual ([| "Mixed"; "Do not assign"; "protocol"; "dataset" |])

                    Vitest.expect(selectValues container).toEqual ([| "__mixed__"; "protocol"; "dataset" |])

                    changeSelectValue (selectAt container 0) "dataset"
                    do! Promise.sleep 0

                    Vitest.expect(updates.Count).toBe (2)

                    Vitest
                        .expect(updates.ToArray())
                        .toEqual (
                            [|
                                ("notes/2026-06-15/Sampling_protocol/assets/data.csv",
                                 Some AssignNoteAssetDestination.Dataset)
                                ("notes/2026-06-15/Sampling_protocol/assets/diagram.png",
                                 Some AssignNoteAssetDestination.Dataset)
                            |]
                        )
                finally
                    cleanup ()
            }
        )

        Vitest.test (
            "assign note modal header destination submits every asset destination",
            fun () -> promise {
                let note = assignableNote "notes/2026-06-15/Sampling_protocol" "Sampling_protocol"

                let assets =
                    ResizeArray [
                        assignableAsset "notes/2026-06-15/Sampling_protocol/assets/data.csv" "data.csv"
                        assignableAsset "notes/2026-06-15/Sampling_protocol/assets/diagram.png" "diagram.png"
                    ]

                let mutable submittedDestinations: Map<string, AssignNoteAssetDestination> option =
                    None

                let submit _ assetDestinations =
                    submittedDestinations <- Some assetDestinations
                    promise { return () }

                let! _container, cleanup =
                    AssignNoteModal.AssignNoteModal(
                        isOpen = true,
                        itemName = Some "AssayA",
                        selectedNote = Some note,
                        setSelectedNote = ignore,
                        availableNotes = ResizeArray [ note ],
                        availableAssets = assets,
                        availableAssetDestinations = [
                            AssignNoteAssetDestination.Protocol
                            AssignNoteAssetDestination.Dataset
                        ],
                        close = ignore,
                        submit = submit
                    )
                    |> renderToBody

                try
                    let modal =
                        document.body.querySelector ("[data-testid='modal_arc-assign-note']") :?> HTMLElement

                    changeSelectValue ((modal.querySelectorAll "select").[1] :?> HTMLSelectElement) "dataset"
                    do! Promise.sleep 0

                    ((modal.querySelectorAll "button").[2] :?> HTMLButtonElement).click ()
                    do! waitUntil ((fun () -> submittedDestinations.IsSome), 50)

                    match submittedDestinations with
                    | Some destinations ->
                        Vitest.expect(destinations.Count).toBe (2)

                        for asset in assets do
                            Vitest
                                .expect(destinations |> Map.tryFind asset.SourceRelativePath)
                                .toEqual (Some AssignNoteAssetDestination.Dataset)
                    | None -> failwith "Expected submit to receive asset destinations."
                finally
                    cleanup ()
            }
        )

        Vitest.test (
            "assigned asset target path follows the selected asset destination",
            fun () ->
                let target = {
                    Name = "AssayA"
                    Kind = NotesTargetKind.Assay
                }

                let note = assignableNote "notes/2026-06-15/Sampling_protocol" "Sampling_protocol"

                let asset =
                    assignableAsset "notes/2026-06-15/Sampling_protocol/assets/diagram.png" "nested/diagram.png"

                Vitest
                    .expect(buildAssignedAssetTargetPath target note asset AssignNoteAssetDestination.Protocol)
                    .toBe ("assays/AssayA/protocols/2026-06-15_Sampling_protocol/assets/nested/diagram.png")

                Vitest
                    .expect(buildAssignedAssetTargetPath target note asset AssignNoteAssetDestination.Dataset)
                    .toBe ("assays/AssayA/dataset/2026-06-15_Sampling_protocol/assets/nested/diagram.png")

                let studyTarget = {
                    Name = "StudyA"
                    Kind = NotesTargetKind.Study
                }

                Vitest
                    .expect(buildAssignedAssetTargetPath studyTarget note asset AssignNoteAssetDestination.Resource)
                    .toBe ("studies/StudyA/resources/2026-06-15_Sampling_protocol/assets/nested/diagram.png")
        )

        Vitest.test (
            "assignNoteToTarget copies the note and moves selected assets within the assigned copy",
            fun () -> promise {
                let target = {
                    Name = "AssayA"
                    Kind = NotesTargetKind.Assay
                }

                let note = assignableNote "notes/2026-06-15/Sampling_protocol" "Sampling_protocol"

                let copyRequests = ResizeArray<CopyFileSystemItemRequest>()
                let moveRequests = ResizeArray<MovePathRequest>()
                let mutable closed = false

                let config: AssignNoteConfig = {
                    closeDialog = fun () -> closed <- true
                    refreshGitStatus = ignore
                    copyFileSystemItem =
                        fun request -> promise {
                            copyRequests.Add request
                            return Ok()
                        }
                    movePath =
                        fun request -> promise {
                            moveRequests.Add request
                            return Ok()
                        }
                    enqueueError = ignore
                }

                let assets = [
                    assignableAsset "notes/2026-06-15/Sampling_protocol/assets/unassigned.png" "unassigned.png"
                    assignableAsset "notes/2026-06-15/Sampling_protocol/assets/protocol.png" "protocol.png"
                    assignableAsset "notes/2026-06-15/Sampling_protocol/assets/data.csv" "data.csv"
                    assignableAsset
                        "notes/2026-06-15/Sampling_protocol/assets/nested/reference.pdf"
                        "nested/reference.pdf"
                ]

                let selectedDestinations =
                    [
                        "notes/2026-06-15/Sampling_protocol/assets/protocol.png", AssignNoteAssetDestination.Protocol
                        "notes/2026-06-15/Sampling_protocol/assets/data.csv", AssignNoteAssetDestination.Dataset
                        "notes/2026-06-15/Sampling_protocol/assets/nested/reference.pdf",
                        AssignNoteAssetDestination.Resource
                    ]
                    |> Map.ofList

                do! assignNoteToTarget config target note assets selectedDestinations

                Vitest.expect(closed).toBe (true)
                Vitest.expect(copyRequests.[0].sourceRelativePath).toBe ("notes/2026-06-15/Sampling_protocol")

                Vitest
                    .expect(copyRequests.[0].targetRelativePath)
                    .toBe ("assays/AssayA/protocols/2026-06-15_Sampling_protocol")

                Vitest.expect(copyRequests.[0].overwrite).toBe (false)

                Vitest
                    .expect(moveRequests.[0].sourceRelativePath)
                    .toBe ("assays/AssayA/protocols/2026-06-15_Sampling_protocol/assets/data.csv")

                Vitest
                    .expect(moveRequests.[0].targetRelativePath)
                    .toBe ("assays/AssayA/dataset/2026-06-15_Sampling_protocol/assets/data.csv")

                Vitest.expect(moveRequests.[0].overwrite).toBe (true)
                Vitest.expect(moveRequests.Count).toBe (1)
            }
        )

        Vitest.test (
            "assignNoteToTarget uses the dated assigned note folder before moving selected assets",
            fun () -> promise {
                let target = {
                    Name = "AssayA"
                    Kind = NotesTargetKind.Assay
                }

                let note = assignableNote "notes/2026-06-15/Sampling_protocol" "Sampling_protocol"

                let copyRequests = ResizeArray<CopyFileSystemItemRequest>()
                let moveRequests = ResizeArray<MovePathRequest>()

                let errors =
                    ResizeArray<Swate.Components.Primitive.ErrorModal.Types.ErrorModalRequest>()

                let mutable closed = false

                let config: AssignNoteConfig = {
                    closeDialog = fun () -> closed <- true
                    refreshGitStatus = ignore
                    copyFileSystemItem =
                        fun request -> promise {
                            copyRequests.Add request
                            return Ok()
                        }
                    movePath =
                        fun request -> promise {
                            moveRequests.Add request
                            return Ok()
                        }
                    enqueueError = errors.Add
                }

                let assets = [
                    assignableAsset "notes/2026-06-15/Sampling_protocol/assets/data.csv" "data.csv"
                ]

                let selectedDestinations =
                    [
                        "notes/2026-06-15/Sampling_protocol/assets/data.csv", AssignNoteAssetDestination.Dataset
                    ]
                    |> Map.ofList

                do! assignNoteToTarget config target note assets selectedDestinations

                Vitest.expect(errors.Count).toBe (0)
                Vitest.expect(closed).toBe (true)
                Vitest.expect(copyRequests.[0].overwrite).toBe (false)

                Vitest
                    .expect(moveRequests.[0].sourceRelativePath)
                    .toBe ("assays/AssayA/protocols/2026-06-15_Sampling_protocol/assets/data.csv")

                Vitest
                    .expect(moveRequests.[0].targetRelativePath)
                    .toBe ("assays/AssayA/dataset/2026-06-15_Sampling_protocol/assets/data.csv")

                Vitest.expect(moveRequests.[0].overwrite).toBe (true)
            }
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
