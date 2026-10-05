module ElectronRenderer.FileStateContextReloadTests

open System.Collections.Generic
open Browser.Dom
open Fable.Core
open Fable.Core.JsInterop
open Feliz
open Renderer.Components.Helper
open Renderer.Context.FileStateContext
open Renderer.Types
open ARCtrl
open Swate.Electron.Shared.FileIOTypes
open Swate.Components.Shared
open Swate.Components.Page.ArcFileEditor.Types
open Vitest

type private RendererPageState = Renderer.Types.PageState

let private bridgeName typeName = $"FABLE_REMOTING_{typeName}"

let private setBridgeProperty name value = window?(name) <- value

let private clearBridgeProperty name =
    emitJsStatement name "delete window[$0]"

let rec private waitUntil (predicate: unit -> bool, attempts: int) = promise {
    if predicate () then
        return ()
    elif attempts <= 0 then
        failwith "Timed out waiting for React effect."
    else
        do! Promise.sleep 1
        return! waitUntil (predicate, attempts - 1)
}

let private waitForEffect predicate = waitUntil (predicate, 50)

[<ReactComponent>]
let private FileTreeProbe (onFileTree: string[] -> unit) =
    let fileStateCtx = useFileStateCtx ()

    React.useEffect (
        (fun () -> fileStateCtx.state.FileTree |> Seq.map _.path |> Seq.toArray |> onFileTree),
        [| box fileStateCtx.state.FileTree |]
    )

    Html.none

[<ReactComponent>]
let private FileImportProbe (onImport: ActiveFileImportState option -> unit) =
    let fileStateCtx = useFileStateCtx ()

    React.useEffect ((fun () -> onImport fileStateCtx.activeFileImport), [| box fileStateCtx.activeFileImport |])

    Html.none

let private createSnapshot () =
    let snapshot = Dictionary<string, FileEntry>()
    snapshot.Add("", FileEntry.create ("arc", "", true, None))
    snapshot.Add("assays", FileEntry.create ("assays", "assays", true, None))

    snapshot.Add(
        "assays/assay-1/isa.assay.xlsx",
        FileEntry.create ("isa.assay.xlsx", "assays/assay-1/isa.assay.xlsx", false, None)
    )

    snapshot

let private fileImportApi loadActiveImport = {
    loadActiveImport = loadActiveImport
    pickAbsolutePaths = fun () -> JS.Constructors.Promise.resolve (Ok None)
    runImport = fun _ -> JS.Constructors.Promise.resolve (Ok ImportExternalFilesResult.Completed)
    cancelImport = fun _ -> JS.Constructors.Promise.resolve (Ok())
}

Vitest.describe (
    "Renderer FileTree incremental state",
    fun () ->
        Vitest.test (
            "applies one authoritative directory update without processing 64k unrelated entries",
            fun () ->
                let snapshot = Dictionary<string, FileEntry>()
                snapshot.[""] <- FileEntry.create ("arc", "", true, None)
                snapshot.["studies"] <- FileEntry.create ("studies", "studies", true, None)
                snapshot.["studies/S1"] <- FileEntry.create ("S1", "studies/S1", true, None)

                snapshot.["studies/S1/dataset"] <- FileEntry.create ("dataset", "studies/S1/dataset", true, None)

                let removedDirectoryPath = "studies/S1/dataset/removed"
                let removedFilePath = $"{removedDirectoryPath}/old.txt"
                let removedDeepDirectoryPath = $"{removedDirectoryPath}/deep"
                let removedDeepFilePath = $"{removedDeepDirectoryPath}/deep.txt"

                snapshot.[removedDirectoryPath] <- FileEntry.create ("removed", removedDirectoryPath, true, None)

                snapshot.[removedFilePath] <- FileEntry.create ("old.txt", removedFilePath, false, None)

                snapshot.[removedDeepDirectoryPath] <- FileEntry.create ("deep", removedDeepDirectoryPath, true, None)

                snapshot.[removedDeepFilePath] <- FileEntry.create ("deep.txt", removedDeepFilePath, false, None)

                let unrelatedEntryCount = 64000 - snapshot.Count

                for index in 1..unrelatedEntryCount do
                    let path = $"unrelated/file-{index}.txt"
                    snapshot.[path] <- FileEntry.create ($"file-{index}.txt", path, false, None)

                let unrelatedPath = "unrelated/file-1.txt"
                let unrelatedEntry = snapshot.[unrelatedPath]
                let initialState = RendererFileTreeState.ofSnapshot snapshot
                let addedPath = "studies/S1/dataset/new.txt"

                let nextState, application =
                    RendererFileTreeState.applyDirectoryUpdate
                        {
                            directoryPath = "studies/S1/dataset"
                            children = [| FileEntry.create ("new.txt", addedPath, false, None) |]
                        }
                        initialState

                Vitest.expect(application.processedPathCount).toBe (5)
                Vitest.expect(application.removedEntryCount).toBe (4)
                Vitest.expect(application.authoritativeChildCount).toBe (1)
                Vitest.expect(RendererFileTreeState.tryFind removedDirectoryPath nextState).toEqual (None)
                Vitest.expect(RendererFileTreeState.tryFind removedDeepFilePath nextState).toEqual (None)
                Vitest.expect(RendererFileTreeState.tryFind addedPath nextState).toBeDefined ()

                let survivingUnrelatedEntry =
                    RendererFileTreeState.tryFind unrelatedPath nextState |> Option.get

                Vitest.expect(survivingUnrelatedEntry).toBe (unrelatedEntry)
        )

        Vitest.test (
            "preserves surviving descendants, stays shallow, and is idempotent",
            fun () ->
                let snapshot = Dictionary<string, FileEntry>()
                snapshot.[""] <- FileEntry.create ("arc", "", true, None)
                snapshot.["dataset"] <- FileEntry.create ("dataset", "dataset", true, None)
                snapshot.["dataset/folder-a"] <- FileEntry.create ("folder-a", "dataset/folder-a", true, None)

                snapshot.["dataset/folder-a/deep.txt"] <-
                    FileEntry.create ("deep.txt", "dataset/folder-a/deep.txt", false, None)

                snapshot.["dataset/folder-b"] <- FileEntry.create ("folder-b", "dataset/folder-b", true, None)

                let update = {
                    directoryPath = "dataset"
                    children = [|
                        FileEntry.create ("folder-a", "dataset/folder-a", true, None)
                        FileEntry.create ("folder-b", "dataset/folder-b", true, None)
                        FileEntry.create ("new-folder", "dataset/new-folder", true, None)
                        FileEntry.create ("new.txt", "dataset/new.txt", false, None)
                    |]
                }

                let initialState = RendererFileTreeState.ofSnapshot snapshot
                let once, _ = RendererFileTreeState.applyDirectoryUpdate update initialState
                let twice, _ = RendererFileTreeState.applyDirectoryUpdate update once

                Vitest.expect(RendererFileTreeState.tryFind "dataset/folder-a/deep.txt" twice).toBeDefined ()
                Vitest.expect(RendererFileTreeState.tryFind "dataset/new-folder" twice).toBeDefined ()
                Vitest.expect(RendererFileTreeState.tryFind "dataset/new-folder/deep.txt" twice).toEqual (None)
                Vitest.expect(twice).toBe (once)
        )

        Vitest.test (
            "evicts removed directory subtrees and descendants on a directory-to-file transition",
            fun () ->
                let snapshot = Dictionary<string, FileEntry>()
                snapshot.[""] <- FileEntry.create ("arc", "", true, None)
                snapshot.["dataset"] <- FileEntry.create ("dataset", "dataset", true, None)
                snapshot.["dataset/removed"] <- FileEntry.create ("removed", "dataset/removed", true, None)
                snapshot.["dataset/removed/deep"] <- FileEntry.create ("deep", "dataset/removed/deep", true, None)

                snapshot.["dataset/removed/deep/file.txt"] <-
                    FileEntry.create ("file.txt", "dataset/removed/deep/file.txt", false, None)

                snapshot.["dataset/changed"] <- FileEntry.create ("changed", "dataset/changed", true, None)

                snapshot.["dataset/changed/old.txt"] <-
                    FileEntry.create ("old.txt", "dataset/changed/old.txt", false, None)

                let initialState = RendererFileTreeState.ofSnapshot snapshot

                let nextState, _ =
                    RendererFileTreeState.applyDirectoryUpdate
                        {
                            directoryPath = "dataset"
                            children = [|
                                FileEntry.create ("changed", "dataset/changed", false, None)
                            |]
                        }
                        initialState

                Vitest.expect(RendererFileTreeState.tryFind "dataset/removed" nextState).toEqual (None)
                Vitest.expect(RendererFileTreeState.tryFind "dataset/removed/deep/file.txt" nextState).toEqual (None)
                Vitest.expect(RendererFileTreeState.tryFind "dataset/changed/old.txt" nextState).toEqual (None)

                let changed =
                    RendererFileTreeState.tryFind "dataset/changed" nextState |> Option.get

                Vitest.expect(changed.isDirectory).toBe (false)
        )

        Vitest.test (
            "applies an authoritative shallow update to the ARC root",
            fun () ->
                let snapshot = Dictionary<string, FileEntry>()
                snapshot.[""] <- FileEntry.create ("arc", "", true, None)
                snapshot.["old.txt"] <- FileEntry.create ("old.txt", "old.txt", false, None)
                let initialState = RendererFileTreeState.ofSnapshot snapshot

                let nextState, _ =
                    RendererFileTreeState.applyDirectoryUpdate
                        {
                            directoryPath = ""
                            children = [|
                                FileEntry.create ("new.txt", "new.txt", false, None)
                                FileEntry.create ("new-folder", "new-folder", true, None)
                            |]
                        }
                        initialState

                Vitest.expect(RendererFileTreeState.tryFind "old.txt" nextState).toEqual (None)
                Vitest.expect(RendererFileTreeState.tryFind "new.txt" nextState).toBeDefined ()
                Vitest.expect(RendererFileTreeState.tryFind "new-folder" nextState).toBeDefined ()
                Vitest.expect(RendererFileTreeState.tryFind "new-folder/deep.txt" nextState).toEqual (None)
        )
)

Vitest.describe (
    "FileStateContext reload hydration",
    fun () ->
        Vitest.test (
            "loads the current file tree snapshot when the provider mounts",
            fun () -> promise {
                let name = bridgeName "IFileTreeRendererApi"
                let importBridgeName = bridgeName "IFileImportRendererApi"
                let observedFileTrees = ResizeArray<string[]>()
                let mutable listenerRegistered = false
                let mutable disposeCalled = false
                let mutable snapshotLoadCalls = 0
                let mutable importSubscriptionRegistered = false
                let mutable importDisposeCalled = false
                let mutable publishDirectoryUpdate: FileTreeDirectoryUpdate -> unit = ignore

                let container = document.createElement ("div") :?> Browser.Types.HTMLDivElement
                document.body.appendChild container |> ignore
                let root = ReactDOM.createRoot container
                let mutable rootUnmounted = false

                try
                    setBridgeProperty
                        name
                        (createObj [
                            "fileTreeUpdate"
                            ==> fun (_listener: Dictionary<string, FileEntry> -> unit) ->
                                listenerRegistered <- true

                                fun () -> disposeCalled <- true
                            "fileTreeDirectoryUpdate"
                            ==> fun (listener: FileTreeDirectoryUpdate -> unit) ->
                                publishDirectoryUpdate <- listener

                                fun () -> disposeCalled <- true
                        ])

                    setBridgeProperty
                        importBridgeName
                        (createObj [
                            "fileImportStateUpdate"
                            ==> fun (_listener: ActiveFileImportState option -> unit) ->
                                importSubscriptionRegistered <- true
                                fun () -> importDisposeCalled <- true
                        ])

                    let loadSnapshot () = promise {
                        snapshotLoadCalls <- snapshotLoadCalls + 1
                        return Ok(createSnapshot ())
                    }

                    root.render (
                        FileStateCtxProviderWithSnapshots(
                            loadSnapshot,
                            fileImportApi (fun () -> JS.Constructors.Promise.resolve (Ok None)),
                            FileTreeProbe(fun paths -> observedFileTrees.Add paths)
                        )
                    )

                    do!
                        waitForEffect (fun () ->
                            observedFileTrees |> Seq.exists (Array.contains "assays/assay-1/isa.assay.xlsx")
                        )

                    Vitest.expect(listenerRegistered).toBe (true)
                    Vitest.expect(snapshotLoadCalls).toBe (1)

                    publishDirectoryUpdate {
                        directoryPath = "assays/assay-1"
                        children = [|
                            FileEntry.create ("new.txt", "assays/assay-1/new.txt", false, None)
                        |]
                    }

                    do!
                        waitForEffect (fun () ->
                            observedFileTrees
                            |> Seq.exists (fun paths ->
                                paths |> Array.contains "assays/assay-1/new.txt"
                                && not (paths |> Array.contains "assays/assay-1/isa.assay.xlsx")
                            )
                        )

                    root.unmount ()
                    rootUnmounted <- true
                    do! waitForEffect (fun () -> disposeCalled)

                    Vitest.expect(disposeCalled).toBe (true)
                finally
                    if not rootUnmounted then
                        root.unmount ()

                    container.remove ()
                    clearBridgeProperty name
                    clearBridgeProperty importBridgeName
            }
        )

        Vitest.test (
            "installs the pending full snapshot before applying only the latest buffered directory update",
            fun () -> promise {
                let fileTreeBridgeName = bridgeName "IFileTreeRendererApi"
                let importBridgeName = bridgeName "IFileImportRendererApi"
                let observedFileTrees = ResizeArray<string[]>()
                let mutable publishDirectoryUpdate: FileTreeDirectoryUpdate -> unit = ignore
                let mutable directoryListenerRegistered = false

                let mutable resolveSnapshot: (Result<Dictionary<string, FileEntry>, exn> -> unit) option =
                    None

                let snapshot = Dictionary<string, FileEntry>()
                snapshot.[""] <- FileEntry.create ("arc", "", true, None)
                snapshot.["dataset"] <- FileEntry.create ("dataset", "dataset", true, None)
                snapshot.["dataset/old.txt"] <- FileEntry.create ("old.txt", "dataset/old.txt", false, None)
                snapshot.["unrelated.txt"] <- FileEntry.create ("unrelated.txt", "unrelated.txt", false, None)

                let loadSnapshot () =
                    Promise.create (fun resolve _reject -> resolveSnapshot <- Some resolve)

                let dispose () = ()

                let container = document.createElement ("div") :?> Browser.Types.HTMLDivElement
                document.body.appendChild container |> ignore
                let root = ReactDOM.createRoot container

                try
                    setBridgeProperty
                        fileTreeBridgeName
                        (createObj [
                            "fileTreeUpdate" ==> fun (_: Dictionary<string, FileEntry> -> unit) -> dispose
                            "fileTreeDirectoryUpdate"
                            ==> fun (listener: FileTreeDirectoryUpdate -> unit) ->
                                publishDirectoryUpdate <- listener
                                directoryListenerRegistered <- true
                                dispose
                        ])

                    setBridgeProperty
                        importBridgeName
                        (createObj [
                            "fileImportStateUpdate"
                            ==> fun (_: ActiveFileImportState option -> unit) -> dispose
                        ])

                    root.render (
                        FileStateCtxProviderWithSnapshots(
                            loadSnapshot,
                            fileImportApi (fun () -> JS.Constructors.Promise.resolve (Ok None)),
                            FileTreeProbe(fun paths -> observedFileTrees.Add paths)
                        )
                    )

                    do! waitForEffect (fun () -> directoryListenerRegistered && resolveSnapshot.IsSome)

                    publishDirectoryUpdate {
                        directoryPath = "dataset"
                        children = [|
                            FileEntry.create ("first.txt", "dataset/first.txt", false, None)
                        |]
                    }

                    publishDirectoryUpdate {
                        directoryPath = "dataset"
                        children = [|
                            FileEntry.create ("final.txt", "dataset/final.txt", false, None)
                        |]
                    }

                    resolveSnapshot.Value(Ok snapshot)

                    do!
                        waitForEffect (fun () ->
                            observedFileTrees
                            |> Seq.exists (fun paths ->
                                paths |> Array.contains "unrelated.txt"
                                && paths |> Array.contains "dataset/final.txt"
                                && not (paths |> Array.contains "dataset/old.txt")
                                && not (paths |> Array.contains "dataset/first.txt")
                            )
                        )
                finally
                    root.unmount ()
                    container.remove ()
                    clearBridgeProperty fileTreeBridgeName
                    clearBridgeProperty importBridgeName
            }
        )

        Vitest.test (
            "keeps an active import visible when the file explorer child is unmounted and remounted",
            fun () -> promise {
                let fileTreeBridgeName = bridgeName "IFileTreeRendererApi"
                let importBridgeName = bridgeName "IFileImportRendererApi"
                let mutable publishImportState = ignore
                let mutable importListenerRegistered = false
                let mutable fileTreeDisposeCalled = false
                let mutable importDisposeCalled = false
                let observedImports = ResizeArray<ActiveFileImportState option>()
                let container = document.createElement ("div") :?> Browser.Types.HTMLDivElement
                document.body.appendChild container |> ignore
                let root = ReactDOM.createRoot container

                let render child =
                    root.render (
                        FileStateCtxProviderWithSnapshots(
                            (fun () -> JS.Constructors.Promise.resolve (Ok(createSnapshot ()))),
                            fileImportApi (fun () -> JS.Constructors.Promise.resolve (Ok None)),
                            child
                        )
                    )

                try
                    setBridgeProperty
                        fileTreeBridgeName
                        (createObj [
                            "fileTreeUpdate"
                            ==> fun (_: Dictionary<string, FileEntry> -> unit) ->
                                fileTreeDisposeCalled <- false
                                fun () -> fileTreeDisposeCalled <- true
                            "fileTreeDirectoryUpdate"
                            ==> fun (_: FileTreeDirectoryUpdate -> unit) ->
                                let dispose () = fileTreeDisposeCalled <- true
                                dispose
                        ])

                    setBridgeProperty
                        importBridgeName
                        (createObj [
                            "fileImportStateUpdate"
                            ==> fun (listener: ActiveFileImportState option -> unit) ->
                                publishImportState <- listener
                                importListenerRegistered <- true
                                fun () -> importDisposeCalled <- true
                        ])

                    render (FileImportProbe observedImports.Add)
                    do! waitForEffect (fun () -> importListenerRegistered)

                    let activeImport =
                        Some {
                            requestId = "survives-remount"
                            phase = FileImportPhase.Copying
                        }

                    publishImportState activeImport
                    do! waitForEffect (fun () -> observedImports |> Seq.contains activeImport)

                    render Html.none
                    do! Promise.sleep 0
                    render (FileImportProbe observedImports.Add)
                    do! waitForEffect (fun () -> observedImports |> Seq.filter ((=) activeImport) |> Seq.length >= 2)
                finally
                    root.unmount ()
                    container.remove ()
                    clearBridgeProperty fileTreeBridgeName
                    clearBridgeProperty importBridgeName
            }
        )
)

Vitest.describe (
    "File explorer state reconciliation",
    fun () ->
        Vitest.test (
            "isSelectionMissing detects removed selections after file-tree updates",
            fun () ->
                let remainingPaths = [| ""; "assays"; "assays/assay-a/isa.assay.xlsx" |]

                Vitest
                    .expect(
                        FileExplorerStateReconciliation.isSelectionMissing
                            remainingPaths
                            (Some "assays/assay-b/isa.assay.xlsx")
                    )
                    .toBe (true)

                Vitest
                    .expect(
                        FileExplorerStateReconciliation.isSelectionMissing
                            remainingPaths
                            (Some "assays/assay-a/isa.assay.xlsx")
                    )
                    .toBe (false)
        )

        Vitest.test (
            "shouldResetPageStateAfterSelectionRemoval only resets file-preview states",
            fun () ->
                let workflowArcFile =
                    ArcWorkflow.init "DeletePreviewWorkflow"
                    |> Swate.Components.Shared.ARCtrlHelper.ArcFiles.Workflow

                Vitest
                    .expect(
                        FileExplorerStateReconciliation.shouldResetPageStateAfterSelectionRemoval (
                            Some(RendererPageState.ArcFilePage(workflowArcFile, None))
                        )
                    )
                    .toBe (true)

                Vitest
                    .expect(
                        FileExplorerStateReconciliation.shouldResetPageStateAfterSelectionRemoval (
                            Some(RendererPageState.MarkdownPage "# md")
                        )
                    )
                    .toBe (true)

                Vitest
                    .expect(
                        FileExplorerStateReconciliation.shouldResetPageStateAfterSelectionRemoval (
                            Some(RendererPageState.TextPage "txt")
                        )
                    )
                    .toBe (true)

                Vitest
                    .expect(
                        FileExplorerStateReconciliation.shouldResetPageStateAfterSelectionRemoval (
                            Some RendererPageState.UnknownPage
                        )
                    )
                    .toBe (true)

                Vitest
                    .expect(
                        FileExplorerStateReconciliation.shouldResetPageStateAfterSelectionRemoval (
                            Some(RendererPageState.ErrorPage "err")
                        )
                    )
                    .toBe (true)

                Vitest
                    .expect(
                        FileExplorerStateReconciliation.shouldResetPageStateAfterSelectionRemoval (
                            Some RendererPageState.NotesDraftPage
                        )
                    )
                    .toBe (false)

                Vitest
                    .expect(
                        FileExplorerStateReconciliation.shouldResetPageStateAfterSelectionRemoval (
                            Some RendererPageState.ProvenanceGroupingPage
                        )
                    )
                    .toBe (false)

                Vitest
                    .expect(FileExplorerStateReconciliation.shouldResetPageStateAfterSelectionRemoval None)
                    .toBe (false)
        )

        Vitest.test (
            "DataMap tree changes reload a stale open parent preview",
            fun () ->
                let assay = ArcAssay.init "DataMapAssay"
                assay.DataMap <- Some(DataMap.init ())

                let pageState =
                    Some(RendererPageState.ArcFilePage(ArcFiles.Assay assay, Some ActiveView.DataMap))

                let fileTreeWithDataMap = [|
                    FileEntry.create ("isa.datamap.xlsx", "assays/DataMapAssay/isa.datamap.xlsx", false, None)
                |]

                Vitest
                    .expect(FileExplorerStateReconciliation.tryGetDataMapMismatchReload fileTreeWithDataMap pageState)
                    .toEqual (None)

                let fileTree = [|
                    FileEntry.create ("DataMapAssay", "assays/DataMapAssay", true, None)
                    FileEntry.create ("isa.assay.xlsx", "assays/DataMapAssay/isa.assay.xlsx", false, None)
                |]

                Vitest
                    .expect(FileExplorerStateReconciliation.tryGetDataMapMismatchReload fileTree pageState)
                    .toEqual (Some("assays/DataMapAssay/isa.assay.xlsx", Some ActiveView.Metadata))

                assay.DataMap <- None

                Vitest
                    .expect(FileExplorerStateReconciliation.tryGetDataMapMismatchReload fileTreeWithDataMap pageState)
                    .toEqual (Some("assays/DataMapAssay/isa.assay.xlsx", Some ActiveView.DataMap))

                let standaloneDataMapPage =
                    Some(
                        RendererPageState.ArcFilePage(
                            ArcFiles.DataMap(
                                Some(DatamapParentInfo.create "DataMapAssay" DataMapParent.Assay),
                                DataMap.init ()
                            ),
                            Some ActiveView.DataMap
                        )
                    )

                Vitest
                    .expect(FileExplorerStateReconciliation.tryGetDataMapMismatchReload fileTree standaloneDataMapPage)
                    .toEqual (None)
        )

        Vitest.test (
            "fromFileContentDTO maps markdown files to MarkdownPage",
            fun () ->
                let dto: FileContentDTO = {|
                    fileType = FileContentType.Markdown
                    content = "# My Note"
                    path = "notes/my-note.md"
                |}

                let pageState = RendererPageState.fromFileContentDTO dto

                match pageState with
                | RendererPageState.MarkdownPage markdownContent -> Vitest.expect(markdownContent).toBe ("# My Note")
                | _ -> failwith "Expected MarkdownPage for markdown file content DTO."
        )

        Vitest.test (
            "fromFileContentDTO opens an ARC workbook without tables on Metadata",
            fun () ->
                let dto =
                    Swate.Electron.Shared.FileIOHelper.FileContentDTO.fromArcFile (
                        ArcFiles.Assay(ArcAssay.init "assay")
                    )
                    |> Option.defaultWith (fun () -> failwith "Expected an assay DTO.")

                match RendererPageState.fromFileContentDTO dto with
                | RendererPageState.ArcFilePage(_, Some ActiveView.Metadata) -> ()
                | _ -> failwith "Expected the Metadata starting view."
        )

        Vitest.test (
            "fromFileContentDTO opens an ARC workbook with tables on its first table",
            fun () ->
                let assay = ArcAssay.init "assay"
                assay.AddTable(ArcTable.init "table")

                let dto =
                    Swate.Electron.Shared.FileIOHelper.FileContentDTO.fromArcFile (ArcFiles.Assay assay)
                    |> Option.defaultWith (fun () -> failwith "Expected an assay DTO.")

                match RendererPageState.fromFileContentDTO dto with
                | RendererPageState.ArcFilePage(_, Some(ActiveView.Table 0)) -> ()
                | _ -> failwith "Expected the first table starting view."
        )

        Vitest.test (
            "fromFileContentDTO opens a DataMap workbook on DataMap",
            fun () ->
                let parent = DatamapParentInfo.create "assay" DataMapParent.Assay

                let dto =
                    Swate.Electron.Shared.FileIOHelper.FileContentDTO.fromArcFile (
                        ArcFiles.DataMap(Some parent, DataMap.init ())
                    )
                    |> Option.defaultWith (fun () -> failwith "Expected a DataMap DTO.")

                match RendererPageState.fromFileContentDTO dto with
                | RendererPageState.ArcFilePage(_, Some ActiveView.DataMap) -> ()
                | _ -> failwith "Expected the DataMap starting view."
        )

        Vitest.test (
            "a redirected DataMap sidebar click opens the owning workbook on DataMap",
            fun () ->
                let assay = ArcAssay.init "assay"
                assay.AddTable(ArcTable.init "table")

                let pageState =
                    RendererPageState.ArcFilePage(ArcFiles.Assay assay, Some(ActiveView.Table 0))
                    |> Renderer.Components.Helper.ArcViewSelection.applyRequestedPathView
                        "assays/assay/isa.datamap.xlsx"

                match pageState with
                | RendererPageState.ArcFilePage(_, Some ActiveView.DataMap) -> ()
                | _ -> failwith "Expected the redirected DataMap click to select the DataMap view."
        )

)
