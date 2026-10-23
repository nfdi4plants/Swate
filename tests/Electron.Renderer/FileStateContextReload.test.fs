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
        (fun () -> fileStateCtx.state.FileTree |> Array.map _.path |> onFileTree),
        [| box fileStateCtx.state.FileTree |]
    )

    Html.none

[<ReactComponent>]
let private FileImportProbe (onImport: ActiveFileImportState option -> unit) =
    let fileStateCtx = useFileStateCtx ()

    React.useEffect ((fun () -> onImport fileStateCtx.activeFileImport), [| box fileStateCtx.activeFileImport |])

    Html.none

[<ReactComponent>]
let private FileStateProbe (onController: FileStateController -> unit) =
    let fileStateCtx = useFileStateCtx ()

    React.useEffect ((fun () -> onController fileStateCtx), [| box fileStateCtx |])

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

// The unsubscribe callback must be a plain JS function. A curried F# lambda here made the unmount cleanup throw
// "clo is not a function".
let private noDispose: unit -> unit = Fable.Core.JsInterop.emitJsExpr () "() => {}"

/// Mounts the provider with the given import api and returns the result of one importExternalFiles call.
let private runImportExternalFiles (importApi: FileImportApi) = promise {
    let fileTreeBridgeName = bridgeName "IFileTreeRendererApi"
    let importBridgeName = bridgeName "IFileImportRendererApi"
    let controllers = ResizeArray<FileStateController>()
    let container = document.createElement ("div") :?> Browser.Types.HTMLDivElement
    document.body.appendChild container |> ignore
    let root = ReactDOM.createRoot container

    try
        setBridgeProperty
            fileTreeBridgeName
            (createObj [
                "fileTreeUpdate" ==> fun (_: Dictionary<string, FileEntry> -> unit) -> noDispose
            ])

        setBridgeProperty
            importBridgeName
            (createObj [
                "fileImportStateUpdate"
                ==> fun (_: ActiveFileImportState option -> unit) -> noDispose
            ])

        root.render (
            FileStateCtxProviderWithSnapshots(
                (fun () -> JS.Constructors.Promise.resolve (Ok(createSnapshot ()))),
                importApi,
                FileStateProbe controllers.Add
            )
        )

        do! waitForEffect (fun () -> controllers.Count > 0)
        return! controllers.[controllers.Count - 1].importExternalFiles "assays/assay-1"
    finally
        root.unmount ()
        container.remove ()
        clearBridgeProperty fileTreeBridgeName
        clearBridgeProperty importBridgeName
}

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
    "FileStateContext importExternalFiles",
    fun () ->
        Vitest.test (
            "returns Ok None when the picker is closed",
            fun () -> promise {
                let! result =
                    runImportExternalFiles (fileImportApi (fun () -> JS.Constructors.Promise.resolve (Ok None)))

                Vitest.expect(result).toEqual (Ok None)
            }
        )

        Vitest.test (
            "returns the completed result when the import completes",
            fun () -> promise {
                let api = {
                    fileImportApi (fun () -> JS.Constructors.Promise.resolve (Ok None)) with
                        pickAbsolutePaths = fun () -> JS.Constructors.Promise.resolve (Ok(Some "auth-1"))
                }

                let! result = runImportExternalFiles api

                Vitest.expect(result).toEqual (Ok(Some ImportExternalFilesResult.Completed))
            }
        )

        Vitest.test (
            "returns the error when the picker fails",
            fun () -> promise {
                let api = {
                    fileImportApi (fun () -> JS.Constructors.Promise.resolve (Ok None)) with
                        pickAbsolutePaths = fun () -> JS.Constructors.Promise.resolve (Error(exn "picker failed"))
                }

                let! result = runImportExternalFiles api

                match result with
                | Error ex -> Vitest.expect(ex.Message).toBe ("picker failed")
                | Ok _ -> failwith "Expected the picker error."
            }
        )

        Vitest.test (
            "returns the error when the import fails",
            fun () -> promise {
                let api = {
                    fileImportApi (fun () -> JS.Constructors.Promise.resolve (Ok None)) with
                        pickAbsolutePaths = fun () -> JS.Constructors.Promise.resolve (Ok(Some "auth-1"))
                        runImport = fun _ -> JS.Constructors.Promise.resolve (Error(exn "import failed"))
                }

                let! result = runImportExternalFiles api

                match result with
                | Error ex -> Vitest.expect(ex.Message).toBe ("import failed")
                | Ok _ -> failwith "Expected the import error."
            }
        )

        Vitest.test (
            "returns the cancelled result when the import is cancelled",
            fun () -> promise {
                let api = {
                    fileImportApi (fun () -> JS.Constructors.Promise.resolve (Ok None)) with
                        pickAbsolutePaths = fun () -> JS.Constructors.Promise.resolve (Ok(Some "auth-1"))
                        runImport = fun _ -> JS.Constructors.Promise.resolve (Ok ImportExternalFilesResult.Cancelled)
                }

                let! result = runImportExternalFiles api

                Vitest.expect(result).toEqual (Ok(Some ImportExternalFilesResult.Cancelled))
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
                            Some(RendererPageState.PDFPage "pdf")
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
                    .expect(
                        FileExplorerStateReconciliation.shouldResetPageStateAfterSelectionRemoval (
                            Some(RendererPageState.ProvenanceGraphPage "request")
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
            "fromFileContentDTO maps markdown files to MarkdownPage",
            fun () ->
                let dto: FileContentDTO = {|
                    fileType = FileContentType.PDF
                    content = "PDF content"
                    path = "protocols/my-protocol.pdf"
                |}

                let pageState = RendererPageState.fromFileContentDTO dto

                match pageState with
                | RendererPageState.PDFPage pdfContent -> Vitest.expect(pdfContent).toBe ("PDF content")
                | _ -> failwith "Expected PDFPage for PDF file content DTO."
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
