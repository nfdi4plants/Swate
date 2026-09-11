module ElectronCore.ArcVaultHelperTests

open ARCtrl
open Fable.Core.JsInterop
open Fable.Electron.Main
open Main.ARCtrlExtensions
open Main.ArcVault
open Main.ArcVaultHelper
open Main.ArcVaultTypes
open Main.Bindings.Filesystem
open Main.Bindings.Path
open Main.Notes.NoteConstants
open Swate.Components.Shared
open Swate.Electron.Shared.FileIOHelper
open Vitest

module WatcherHelpers = Main.WatcherHelpers

let private watcherTestOptions = TestOptions(timeout = 20000)

let private lifecycleTestWindow id isDestroyed onSend =
    // The remoting proxy calls webContents.send with channel and payload arguments.
    // Discard those transport details so lifecycle tests only observe whether a send occurred.
    let send: obj = emitJsExpr onSend "((..._args) => $0())"

    // ArcVault only needs this subset of BrowserWindow for lifecycle broadcasts. Keeping the
    // fixture minimal avoids constructing a real Electron window in the Vitest environment.
    createObj [
        "id" ==> id
        "isDestroyed" ==> (fun () -> isDestroyed)
        "webContents" ==> createObj [ "send" ==> send ]
    ]
    |> unbox<BrowserWindow>

let private mkdirRecursiveAsync (directoryPath: string) = promise {
    let! _ = mkdirAsync directoryPath (MkdirOptions(recursive = true))
    return ()
}

let private writeTextFileAsync (filePath: string) (content: string) =
    writeFileAsync filePath content TextEncoding.Utf8

let private arctrlDefaultGitignoreContent () =
    match ARCtrl.Contract.Git.gitignoreContract.DTO with
    | Some(ARCtrl.Contract.DTO.Text content) -> content
    | _ -> failwith "ARCtrl default .gitignore contract does not contain text content."

let private addDataMapToAllEntityTypes (arc: ARC) =
    let study = ArcStudy("Study With DataMap")
    study.DataMap <- Some(DataMap.init ())
    arc.AddStudy(study)

    let assay = ArcAssay("Assay With DataMap")
    assay.DataMap <- Some(DataMap.init ())
    arc.AddAssay(assay)

    let workflow = ArcWorkflow("Workflow With DataMap")
    workflow.DataMap <- Some(DataMap.init ())
    arc.AddWorkflow(workflow)

    let run = ArcRun("Run With DataMap")
    run.DataMap <- Some(DataMap.init ())
    arc.AddRun(run)

Vitest.describe (
    "ArcVaultHelper",
    fun () ->
        Vitest.test (
            "DataMap add synchronization preserves the persisted static-hash baseline",
            fun () ->
                let parentInfo = DatamapParentInfo.create "DataMapAssay" DataMapParent.Assay
                let persistedDataMap = DataMap.init ()
                persistedDataMap.StaticHash <- 123
                let persistedAssay = ArcAssay("DataMapAssay")
                persistedAssay.DataMap <- Some persistedDataMap
                let persistedArc = ARC("PersistedArc")
                persistedArc.AddAssay persistedAssay

                let localDataMap = DataMap.init ()
                localDataMap.StaticHash <- 456
                let localAssay = ArcAssay("DataMapAssay")
                localAssay.DataMap <- Some localDataMap
                let localArc = ARC("LocalArc")
                localArc.AddAssay localAssay

                syncAddedArcFileFromPersisted
                    persistedArc
                    localArc
                    (ArcFiles.DataMap(Some parentInfo, persistedDataMap))

                Vitest.expect(persistedDataMap.StaticHash).toBe (123)
                Vitest.expect(persistedArc.GetAssay("DataMapAssay").DataMap.Value.StaticHash).toBe (123)
                Vitest.expect(localArc.GetAssay("DataMapAssay").DataMap.Value.StaticHash).toBe (123)
        )

        Vitest.test (
            "recent ARC broadcasts skip windows destroyed during simultaneous shutdown",
            fun () ->
                let mutable aliveWindowSendCount = 0

                let aliveWindow =
                    lifecycleTestWindow 1 false (fun () -> aliveWindowSendCount <- aliveWindowSendCount + 1)

                let destroyedWindow =
                    lifecycleTestWindow 2 true (fun () -> failwith "Destroyed window received an IPC message.")

                let vaults = ArcVaults()
                vaults.Vaults.Add(aliveWindow.id, ArcVault(aliveWindow))
                vaults.Vaults.Add(destroyedWindow.id, ArcVault(destroyedWindow))

                vaults.BroadcastRecentARCs()

                Vitest.expect(aliveWindowSendCount).toBe (1)
        )

        Vitest.test (
            "file watcher polling defaults to Windows only",
            fun () ->
                Vitest.expect(shouldUsePollingByDefault "win32").toBe (true)
                Vitest.expect(shouldUsePollingByDefault "WIN32").toBe (true)
                Vitest.expect(shouldUsePollingByDefault "linux").toBe (false)
                Vitest.expect(shouldUsePollingByDefault "darwin").toBe (false)
        )

        Vitest.test (
            "watcher options keep ARC metadata shallow and expanded directories non-recursive",
            fun () ->
                let pollingOptions = createFileWatcherOptions "C:/arc" (Some true)
                let nativeOptions = createFileWatcherOptions "C:/arc" (Some false)
                let expandedDirectoryOptions = createPayloadWatcherOptions "C:/arc" (Some true)

                Vitest.expect(pollingOptions.depth).toEqual (Some ArcFileWatcherDepth)
                Vitest.expect(pollingOptions.usePolling).toEqual (Some true)
                Vitest.expect(pollingOptions.interval).toEqual (Some 200)
                Vitest.expect(pollingOptions.binaryInterval).toEqual (Some 400)
                Vitest.expect(nativeOptions.depth).toEqual (Some ArcFileWatcherDepth)
                Vitest.expect(nativeOptions.usePolling).toEqual (None)
                Vitest.expect(expandedDirectoryOptions.depth).toEqual (Some 0)
                Vitest.expect(expandedDirectoryOptions.usePolling).toEqual (Some true)
                Vitest.expect(expandedDirectoryOptions.interval).toEqual (Some 200)
                Vitest.expect(expandedDirectoryOptions.binaryInterval).toEqual (Some 400)
        )

        Vitest.test (
            "waitForFileWatcherReady resolves from the native ready event",
            fun () -> promise {
                let mutable readyCallback: (unit -> unit) option = None

                let watcher =
                    createObj [
                        "on"
                        ==> (fun (eventName: obj) (callback: obj) ->
                            if string eventName = "ready" then
                                readyCallback <- Some(unbox callback)

                            null
                        )
                    ]
                    |> unbox<Main.Bindings.Chokidar.IWatcher>

                let ready = waitForFileWatcherReady watcher
                Vitest.expect(readyCallback.IsSome).toBe (true)
                readyCallback.Value()
                do! ready
            }
        )

        Vitest.test (
            "file-tree work is serialized in enqueue order",
            fun () -> promise {
                let vault = ArcVault(TestHelpers.testWindow ())
                let order = ResizeArray<string>()

                let delayedFirst =
                    vault.EnqueueFileTreeWork(fun () -> promise {
                        order.Add "first-start"

                        do!
                            Fable.Core.JS.Constructors.Promise.Create(fun resolve _ ->
                                Fable.Core.JS.setTimeout (fun () -> resolve ()) 20 |> ignore
                            )

                        order.Add "first-end"
                    })

                let second = vault.EnqueueFileTreeWork(fun () -> promise { order.Add "second" })

                do! delayedFirst
                do! second
                Vitest.expect(order.ToArray()).toEqual ([| "first-start"; "first-end"; "second" |])
            }
        )

        Vitest.test (
            "watcher batches keep only the final event for each normalized path",
            fun () ->
                let createEvent eventName relativePath : ArcVaultFileSystemEvent = {
                    EventName = eventName
                    RelativePath = relativePath
                    AbsolutePath = $"C:/arc/{relativePath}"
                }

                let coalesced =
                    [
                        createEvent "add" "data/File.txt"
                        createEvent "change" "other.txt"
                        createEvent "unlink" "DATA\\file.txt"
                    ]
                    |> WatcherHelpers.coalesceEventsByPath

                Vitest.expect(coalesced.Length).toBe (2)
                Vitest.expect(coalesced.[0].RelativePath).toBe ("other.txt")
                Vitest.expect(coalesced.[1].EventName).toBe ("unlink")
        )

        Vitest.test (
            "only ARCtrl read-contract paths and entity-directory deletions request ARC merges",
            fun () ->
                let createEvent eventName relativePath : ArcVaultFileSystemEvent = {
                    EventName = eventName
                    RelativePath = relativePath
                    AbsolutePath = $"C:/arc/{relativePath}"
                }

                [
                    "change", "isa.investigation.xlsx"
                    "change", "studies/S1/isa.study.xlsx"
                    "change", "workflows/W1/workflow.cwl"
                    "change", "runs/R1/run.cwl"
                    "change", "runs/R1/run.yml"
                    "unlinkDir", "assays/A1"
                ]
                |> List.iter (fun (eventName, path) ->
                    Vitest.expect(WatcherHelpers.isArcMergeRelevant (createEvent eventName path)).toBe (true)
                )

                [
                    "change", "studies/S1/dataset/raw.bin"
                    "add", "notes/readme.md"
                    "unlinkDir", "studies/S1/dataset"
                ]
                |> List.iter (fun (eventName, path) ->
                    Vitest.expect(WatcherHelpers.isArcMergeRelevant (createEvent eventName path)).toBe (false)
                )
        )

        Vitest.test (
            "directory refresh validation rejects traversal, missing paths, and files",
            fun () -> promise {
                let! rootPath = TestHelpers.createTempDirectoryAsync "swate-refresh-validation-"

                try
                    let directoryPath = join [| rootPath; "payload" |]
                    let filePath = join [| rootPath; "payload.txt" |]
                    do! mkdirRecursiveAsync directoryPath
                    do! writeTextFileAsync filePath "payload"

                    match! Main.IPC.FileSystemIO.tryResolveExistingArcDirectoryPath rootPath "payload" with
                    | Error error -> failwith error.Message
                    | Ok resolved -> Vitest.expect(PathHelpers.pathsEqual resolved directoryPath).toBe (true)

                    match! Main.IPC.FileSystemIO.tryResolveExistingArcDirectoryPath rootPath "../outside" with
                    | Ok _ -> failwith "Expected traversal to be rejected."
                    | Error error -> Vitest.expect(error.Message).toContain ("traversal")

                    match! Main.IPC.FileSystemIO.tryResolveExistingArcDirectoryPath rootPath "missing" with
                    | Ok _ -> failwith "Expected a missing directory to be rejected."
                    | Error error -> Vitest.expect(error.Message).toContain ("does not exist")

                    match! Main.IPC.FileSystemIO.tryResolveExistingArcDirectoryPath rootPath "payload.txt" with
                    | Ok _ -> failwith "Expected a file refresh request to be rejected."
                    | Error error -> Vitest.expect(error.Message).toContain ("not a directory")

                    do! TestHelpers.removeDirectoryAsync rootPath
                with error ->
                    do! TestHelpers.removeDirectoryAsync rootPath
                    return raise error
            }
        )

        Vitest.test (
            "OpenARC waits for watcher startup and installs the ARC and eager file tree",
            watcherTestOptions,
            fun () ->
                TestHelpers.withTempArcWith
                    "swate-watcher-startup-"
                    "WatcherStartupArc"
                    ignore
                    (fun arcPath -> promise {
                        let vault = ArcVault(TestHelpers.testWindow ())

                        try
                            do! vault.OpenARC arcPath
                            Vitest.expect(vault.watcher.IsSome).toBe (true)
                            Vitest.expect(vault.arc.Value.Identifier).toBe ("WatcherStartupArc")
                            Vitest.expect(vault.fileTree.Count).toBeGreaterThan (0)
                            Vitest.expect(vault.isFileWatcherInitializing).toBe (false)
                            do! vault.StopFileWatcher()
                        with error ->
                            do! vault.StopFileWatcher()
                            return raise error
                    })
        )

        Vitest.test (
            "failed ARC startup closes the watcher and clears pending state",
            watcherTestOptions,
            fun () -> promise {
                let! rootPath = TestHelpers.createTempDirectoryAsync "swate-watcher-startup-failure-"
                let invalidArcPath = join [| rootPath; "not-an-arc" |]
                do! mkdirRecursiveAsync invalidArcPath
                let vault = ArcVault(TestHelpers.testWindow ())

                vault.fileWatcherPendingEvents.Add {
                    EventName = "change"
                    RelativePath = "payload.txt"
                    AbsolutePath = join [| invalidArcPath; "payload.txt" |]
                }

                try
                    let mutable startupFailed = false

                    try
                        do! vault.OpenARC invalidArcPath
                    with _ ->
                        startupFailed <- true

                    Vitest.expect(startupFailed).toBe (true)
                    Vitest.expect(vault.watcher).toEqual (None)
                    Vitest.expect(vault.path).toEqual (None)
                    Vitest.expect(vault.arc).toEqual (None)
                    Vitest.expect(vault.fileWatcherPendingEvents.Count).toBe (0)
                    Vitest.expect(vault.fileWatcherPendingArcMergeEvents.Count).toBe (0)
                    do! TestHelpers.removeDirectoryAsync rootPath
                with error ->
                    do! vault.StopFileWatcher()
                    do! TestHelpers.removeDirectoryAsync rootPath
                    return raise error
            }
        )

        Vitest.test (
            "expanded payload directories receive live updates only until collapsed",
            watcherTestOptions,
            fun () ->
                TestHelpers.withTempArcWith
                    "swate-expanded-directory-watcher-"
                    "ExpandedDirectoryWatcherArc"
                    ignore
                    (fun arcPath -> promise {
                        let expandedDirectory = join [| arcPath; "studies"; "S1"; "dataset" |]
                        let secondExpandedDirectory = join [| arcPath; "studies"; "S1"; "dataset-two" |]

                        let liveFilePath =
                            join [| expandedDirectory; "live.txt" |] |> PathHelpers.normalizePath

                        let secondLiveFilePath =
                            join [| secondExpandedDirectory; "live.txt" |] |> PathHelpers.normalizePath

                        let collapsedFilePath =
                            join [| expandedDirectory; "collapsed.txt" |] |> PathHelpers.normalizePath

                        let stillLiveFilePath =
                            join [| secondExpandedDirectory; "still-live.txt" |]
                            |> PathHelpers.normalizePath

                        do! mkdirRecursiveAsync expandedDirectory
                        do! mkdirRecursiveAsync secondExpandedDirectory

                        let vault = ArcVault(TestHelpers.testWindow ())

                        let waitForWatcherBatch () =
                            Fable.Core.JS.Constructors.Promise.Create(fun resolve _ ->
                                Fable.Core.JS.setTimeout (fun () -> resolve ()) 4500 |> ignore
                            )

                        try
                            do! vault.OpenARC arcPath
                            do! vault.SetFileTreeDirectoryExpanded("studies/S1/dataset", true)
                            do! vault.SetFileTreeDirectoryExpanded("studies/S1/dataset-two", true)
                            Vitest.expect(vault.payloadWatcher.IsSome).toBe (true)
                            Vitest.expect(vault.expandedDirectoryPaths.Count).toBe (2)

                            do! writeTextFileAsync liveFilePath "live"
                            do! writeTextFileAsync secondLiveFilePath "live"
                            do! waitForWatcherBatch ()
                            Vitest.expect(vault.fileTree.ContainsKey liveFilePath).toBe (true)
                            Vitest.expect(vault.fileTree.ContainsKey secondLiveFilePath).toBe (true)

                            do! vault.SetFileTreeDirectoryExpanded("studies/S1/dataset", false)
                            Vitest.expect(vault.expandedDirectoryPaths.Count).toBe (1)

                            do! writeTextFileAsync collapsedFilePath "collapsed"
                            do! writeTextFileAsync stillLiveFilePath "still live"
                            do! waitForWatcherBatch ()
                            Vitest.expect(vault.fileTree.ContainsKey collapsedFilePath).toBe (false)
                            Vitest.expect(vault.fileTree.ContainsKey stillLiveFilePath).toBe (true)
                            do! vault.StopFileWatcher()
                        with error ->
                            do! vault.StopFileWatcher()
                            return raise error
                    })
        )

        Vitest.test (
            "Git metadata path detection excludes only exact .git path segments",
            fun () ->
                Vitest.expect(isGitMetadataPath ".git").toBe (true)
                Vitest.expect(isGitMetadataPath ".git/objects/ab/object").toBe (true)
                Vitest.expect(isGitMetadataPath "notes\\.GIT\\config").toBe (true)
                Vitest.expect(isGitMetadataPath ".gitignore").toBe (false)
                Vitest.expect(isGitMetadataPath ".gitattributes").toBe (false)
                Vitest.expect(isGitMetadataPath "notes/my.git/file.txt").toBe (false)
        )

        Vitest.test (
            "legacy isa_datamap paths are ignored in favor of the canonical workbook",
            fun () ->
                Vitest.expect(isLegacyDataMapPath "assays/assay_1/isa_datamap").toBe (true)
                Vitest.expect(isLegacyDataMapPath "assays/assay_1/ISA_DATAMAP").toBe (true)
                Vitest.expect(isLegacyDataMapPath "assays/assay_1/isa.datamap.xlsx").toBe (false)
                Vitest.expect(isLegacyDataMapPath "assays/isa_datamap/data.txt").toBe (false)
        )

        Vitest.test (
            "legacy isa_datamap file is migrated to the canonical workbook path",
            fun () -> promise {
                let! rootPath = TestHelpers.createTempDirectoryAsync "swate-legacy-datamap-"

                try
                    let assayFolder = join [| rootPath; "assays"; "assay_1" |]
                    let legacyPath = join [| assayFolder; "isa_datamap" |]
                    let canonicalPath = join [| assayFolder; "isa.datamap.xlsx" |]
                    do! mkdirRecursiveAsync assayFolder
                    do! writeTextFileAsync legacyPath "legacy-content"

                    let! migratedPaths = migrateLegacyDataMapPathsAsync rootPath [| "assays/assay_1/isa_datamap" |]

                    Vitest.expect(migratedPaths).toEqual ([| "assays/assay_1/isa.datamap.xlsx" |])
                    Vitest.expect(existsSync legacyPath).toBe (false)
                    Vitest.expect(existsSync canonicalPath).toBe (true)

                    let! migratedContent = readFileAsync canonicalPath TextEncoding.Utf8
                    Vitest.expect(migratedContent).toBe ("legacy-content")
                    do! TestHelpers.removeDirectoryAsync rootPath
                with error ->
                    do! TestHelpers.removeDirectoryAsync rootPath
                    return raise error
            }
        )

        Vitest.test (
            "canonical DataMap workbook wins over a legacy isa_datamap file",
            fun () -> promise {
                let! rootPath = TestHelpers.createTempDirectoryAsync "swate-canonical-datamap-"

                try
                    let assayFolder = join [| rootPath; "assays"; "assay_1" |]
                    let legacyPath = join [| assayFolder; "isa_datamap" |]
                    let canonicalPath = join [| assayFolder; "isa.datamap.xlsx" |]
                    do! mkdirRecursiveAsync assayFolder
                    do! writeTextFileAsync legacyPath "legacy-content"
                    do! writeTextFileAsync canonicalPath "canonical-content"

                    let! migratedPaths =
                        migrateLegacyDataMapPathsAsync rootPath [|
                            "assays/assay_1/isa_datamap"
                            "assays/assay_1/isa.datamap.xlsx"
                        |]

                    Vitest.expect(migratedPaths).toEqual ([| "assays/assay_1/isa.datamap.xlsx" |])
                    Vitest.expect(existsSync legacyPath).toBe (true)

                    let! canonicalContent = readFileAsync canonicalPath TextEncoding.Utf8
                    Vitest.expect(canonicalContent).toBe ("canonical-content")
                    do! TestHelpers.removeDirectoryAsync rootPath
                with error ->
                    do! TestHelpers.removeDirectoryAsync rootPath
                    return raise error
            }
        )

        Vitest.test (
            "Swate write contracts contain only targeted scaffold and ARC files",
            fun () ->
                let arc = ARC("TargetedWriteArc")
                addDataMapToAllEntityTypes arc
                arc.SetLicenseFulltext("license text")

                arc.SetFilePaths(
                    [|
                        ".git/config"
                        "payload.txt"
                        "missing-payload.txt"
                        "assays/Assay With DataMap/README.md"
                    |]
                )

                let actualPaths = arc.GetWriteContractsSwate() |> Array.map _.Path |> Array.sort

                let expectedPaths =
                    [|
                        ".gitignore"
                        "LICENSE"
                        "assays/.gitkeep"
                        "assays/Assay With DataMap/isa.assay.xlsx"
                        "assays/Assay With DataMap/isa.datamap.xlsx"
                        "isa.investigation.xlsx"
                        "notes/README.md"
                        "runs/.gitkeep"
                        "runs/Run With DataMap/isa.datamap.xlsx"
                        "runs/Run With DataMap/isa.run.xlsx"
                        "studies/.gitkeep"
                        "studies/Study With DataMap/isa.datamap.xlsx"
                        "studies/Study With DataMap/isa.study.xlsx"
                        "workflows/.gitkeep"
                        "workflows/Workflow With DataMap/isa.datamap.xlsx"
                        "workflows/Workflow With DataMap/isa.workflow.xlsx"
                    |]
                    |> Array.sort

                Vitest.expect(actualPaths).toEqual (expectedPaths)
        )

        Vitest.test (
            "TryWriteAsyncSwate writes the default gitignore and notes README into an ARC root",
            fun () -> promise {
                let! rootPath = TestHelpers.createTempDirectoryAsync "swate-default-gitignore-"
                let arcPath = join [| rootPath; "arc" |]
                let gitignorePath = join [| arcPath; ".gitignore" |]
                let notesReadmePath = join [| arcPath; NotesRootFolderName; NotesReadmeFileName |]

                try
                    do! mkdirRecursiveAsync arcPath

                    let arc = ARC("ScaffoldArc")

                    match! arc.TryWriteAsyncSwate(arcPath) with
                    | Error errors -> failwith (String.concat "\n" errors)
                    | Ok _ -> ()

                    let! gitignoreContent = readFileAsync gitignorePath TextEncoding.Utf8
                    let! notesReadmeContent = readFileAsync notesReadmePath TextEncoding.Utf8
                    Vitest.expect(gitignoreContent).toBe (arctrlDefaultGitignoreContent ())
                    Vitest.expect(notesReadmeContent).toBe (NotesReadmeContent)
                    do! TestHelpers.removeDirectoryAsync rootPath
                with error ->
                    do! TestHelpers.removeDirectoryAsync rootPath
                    return raise error
            }
        )

        Vitest.test (
            "TryWriteAsyncSwate preserves payload and does not create unmanaged file-tree entries",
            fun () -> promise {
                let! rootPath = TestHelpers.createTempDirectoryAsync "swate-targeted-write-"
                let arcPath = join [| rootPath; "arc" |]
                let gitFolder = join [| arcPath; ".git" |]
                let assayFolder = join [| arcPath; "assays"; "Assay 1" |]
                let payloadPath = join [| arcPath; "payload.txt" |]
                let gitConfigPath = join [| gitFolder; "config" |]
                let readmePath = join [| assayFolder; "README.md" |]
                let missingPayloadPath = join [| arcPath; "missing-payload.txt" |]

                try
                    do! mkdirRecursiveAsync gitFolder
                    do! mkdirRecursiveAsync assayFolder
                    do! writeTextFileAsync payloadPath "payload"
                    do! writeTextFileAsync gitConfigPath "git-config"
                    do! writeTextFileAsync readmePath "readme"

                    let arc = ARC("TargetedWriteArc")
                    arc.AddAssay(ArcAssay("Assay 1"))

                    arc.SetFilePaths(
                        [|
                            ".git/config"
                            "payload.txt"
                            "missing-payload.txt"
                            "assays/Assay 1/README.md"
                        |]
                    )

                    match! arc.TryWriteAsyncSwate(arcPath) with
                    | Error errors -> failwith (String.concat "\n" errors)
                    | Ok _ -> ()

                    let! payload = readFileAsync payloadPath TextEncoding.Utf8
                    let! gitConfig = readFileAsync gitConfigPath TextEncoding.Utf8
                    let! readme = readFileAsync readmePath TextEncoding.Utf8
                    let! missingPayloadExists = TestHelpers.pathExistsAsync missingPayloadPath

                    let! assayFileExists = TestHelpers.pathExistsAsync (join [| assayFolder; "isa.assay.xlsx" |])

                    let! collectionGitKeepExists =
                        TestHelpers.pathExistsAsync (join [| arcPath; "assays"; ".gitkeep" |])

                    Vitest.expect(payload).toBe ("payload")
                    Vitest.expect(gitConfig).toBe ("git-config")
                    Vitest.expect(readme).toBe ("readme")
                    Vitest.expect(missingPayloadExists).toBe (false)
                    Vitest.expect(assayFileExists).toBe (true)
                    Vitest.expect(collectionGitKeepExists).toBe (true)
                    do! TestHelpers.removeDirectoryAsync rootPath
                with error ->
                    do! TestHelpers.removeDirectoryAsync rootPath
                    return raise error
            }
        )

        Vitest.test (
            "ARC vault save preserves note markdown files",
            fun () ->
                TestHelpers.withTempArcWith
                    "swate-save-notes-"
                    "NotesPreservingArc"
                    ignore
                    (fun arcPath -> promise {
                        let rootNotesFolder = join [| arcPath; "notes"; "2026-04-27"; "root_note" |]
                        let studyNotesFolder = join [| arcPath; "notes"; "2026-04-27"; "study_note" |]
                        let rootNotePath = join [| rootNotesFolder; "root_note.md" |]
                        let studyNotePath = join [| studyNotesFolder; "study_note.md" |]

                        let rootNoteContent = "---\ntitle: Root note\n---\n\nRoot note body."
                        let studyNoteContent = "---\ntitle: Study note\n---\n\nStudy note body."

                        do! mkdirRecursiveAsync rootNotesFolder
                        do! mkdirRecursiveAsync studyNotesFolder
                        do! writeTextFileAsync rootNotePath rootNoteContent
                        do! writeTextFileAsync studyNotePath studyNoteContent

                        let vault = ArcVault(TestHelpers.testWindow ())
                        vault.path <- Some arcPath
                        do! vault.LoadArc()

                        vault.arc.Value.Title <- Some "Saved title"
                        vault.arc.Value.StaticHash <- 0

                        match! vault.WriteArc() with
                        | Error error -> failwith error.Message
                        | Ok() -> ()

                        let! rootNoteAfterSave = readFileAsync rootNotePath TextEncoding.Utf8
                        let! studyNoteAfterSave = readFileAsync studyNotePath TextEncoding.Utf8

                        Vitest.expect(rootNoteAfterSave).toBe (rootNoteContent)
                        Vitest.expect(studyNoteAfterSave).toBe (studyNoteContent)
                    })
        )

        Vitest.test (
            "ARC loading and writing ignore Git metadata and preserve payload",
            fun () ->
                TestHelpers.withTempArcWith
                    "swate-load-arc-ignore-git-"
                    "IgnoreGitArc"
                    ignore
                    (fun arcPath -> promise {
                        let gitObjectFolder = join [| arcPath; ".git"; "objects"; "ab" |]
                        let nestedGitFolder = join [| arcPath; "notes"; ".git" |]
                        let payloadPath = join [| arcPath; "payload.txt" |]
                        let gitObjectPath = join [| gitObjectFolder; "object" |]
                        do! mkdirRecursiveAsync gitObjectFolder
                        do! mkdirRecursiveAsync nestedGitFolder
                        do! writeTextFileAsync gitObjectPath "git-object"
                        do! writeTextFileAsync (join [| nestedGitFolder; "config" |]) "nested-git-config"
                        do! writeTextFileAsync payloadPath "payload"

                        let! loadResult = ARC.LoadAsyncSwate arcPath
                        let loadedArc = TestHelpers.expectLoadedArc loadResult
                        let paths = loadedArc.FileSystem.Tree.ToFilePaths()

                        Vitest.expect(paths |> Array.exists isGitMetadataPath).toBe (false)

                        loadedArc.SetFilePaths(Array.append paths [| ".git/objects/ab/object" |])
                        loadedArc.Title <- Some "Saved title"
                        do! loadedArc.UpdateAsync arcPath

                        let! payload = readFileAsync payloadPath TextEncoding.Utf8
                        let! gitObject = readFileAsync gitObjectPath TextEncoding.Utf8
                        Vitest.expect(payload).toBe ("payload")
                        Vitest.expect(gitObject).toBe ("git-object")
                    })
        )

        Vitest.test (
            "normal ARC save does not restore deleted DTO-less files from a stale file tree",
            fun () ->
                TestHelpers.withTempArcWith
                    "swate-save-deleted-file-"
                    "DeletedFileArc"
                    ignore
                    (fun arcPath -> promise {
                        let payloadPath = join [| arcPath; "payload.txt" |]
                        let collectionGitKeepPath = join [| arcPath; "assays"; ".gitkeep" |]
                        do! writeTextFileAsync payloadPath "payload"

                        let! loadResult = ARC.LoadAsyncSwate arcPath
                        let loadedArc = TestHelpers.expectLoadedArc loadResult
                        let stalePaths = loadedArc.FileSystem.Tree.ToFilePaths()

                        Vitest.expect(stalePaths |> Array.contains "payload.txt").toBe (true)
                        Vitest.expect(stalePaths |> Array.contains "assays/.gitkeep").toBe (true)

                        do! rmAsync payloadPath (RmOptions())
                        do! rmAsync collectionGitKeepPath (RmOptions())

                        loadedArc.Title <- Some "Saved title"
                        do! loadedArc.UpdateAsync arcPath

                        let! payloadExists = TestHelpers.pathExistsAsync payloadPath
                        let! collectionGitKeepExists = TestHelpers.pathExistsAsync collectionGitKeepPath
                        Vitest.expect(payloadExists).toBe (false)
                        Vitest.expect(collectionGitKeepExists).toBe (false)

                        let! reloadedArc = TestHelpers.loadArcAsync arcPath
                        Vitest.expect(reloadedArc.Title).toEqual (Some "Saved title")
                    })
        )

        Vitest.test (
            "LoadArc reports load errors without crashing the printf formatter",
            fun () -> promise {
                let! rootPath = TestHelpers.createTempDirectoryAsync "swate-load-arc-error-"

                try
                    let vault = ArcVault(TestHelpers.testWindow ())
                    vault.path <- Some rootPath

                    let mutable capturedError: exn option = None

                    try
                        do! vault.LoadArc()
                    with error ->
                        capturedError <- Some error

                    match capturedError with
                    | None ->
                        do! TestHelpers.removeDirectoryAsync rootPath
                        return failwith "Expected LoadArc to fail for an invalid ARC folder."
                    | Some error ->
                        Vitest.expect(error.Message).toContain ("[Swate-0] Unable to load ARC:")
                        Vitest.expect(error.Message).not.toContain ("fmt.cont")
                        do! TestHelpers.removeDirectoryAsync rootPath
                with error ->
                    do! TestHelpers.removeDirectoryAsync rootPath
                    return raise error
            }
        )

        Vitest.test (
            "RenameOpenArcRoot moves the active ARC folder and updates the vault path",
            fun () ->
                TestHelpers.withTempArcWith
                    "swate-rename-arc-root-"
                    "RenameRootArc"
                    ignore
                    (fun arcPath -> promise {
                        let targetPath =
                            join [| dirname arcPath; "renamed-arc" |] |> PathHelpers.normalizePath

                        let vault = ArcVault(TestHelpers.testWindow ())
                        vault.path <- Some arcPath

                        do! vault.LoadArc()

                        match! vault.RenameOpenArcRoot "renamed-arc" with
                        | Error error -> failwith error.Message
                        | Ok renamedPath ->
                            Vitest.expect(renamedPath).toBe (targetPath)
                            Vitest.expect(vault.path).toEqual (Some targetPath)

                            let! oldPathExists = TestHelpers.pathExistsAsync arcPath
                            let! newPathExists = TestHelpers.pathExistsAsync targetPath
                            Vitest.expect(oldPathExists).toBe (false)
                            Vitest.expect(newPathExists).toBe (true)

                            let! reloadedArc = TestHelpers.loadArcAsync targetPath
                            Vitest.expect(reloadedArc.Identifier).toBe ("RenameRootArc")
                    })
        )

        Vitest.test (
            "RenameOpenArcRoot restores expanded-directory watchers under the renamed root",
            watcherTestOptions,
            fun () ->
                TestHelpers.withTempArcWith
                    "swate-rename-expanded-watcher-"
                    "RenameExpandedWatcherArc"
                    ignore
                    (fun arcPath -> promise {
                        let expandedDirectory = join [| arcPath; "studies"; "S1"; "dataset" |]
                        do! mkdirRecursiveAsync expandedDirectory
                        let vault = ArcVault(TestHelpers.testWindow ())

                        try
                            do! vault.OpenARC arcPath
                            do! vault.SetFileTreeDirectoryExpanded("studies/S1/dataset", true)

                            match! vault.RenameOpenArcRoot "renamed-expanded-watcher" with
                            | Error error -> failwith error.Message
                            | Ok renamedPath ->
                                Vitest.expect(vault.payloadWatcher.IsSome).toBe (true)
                                Vitest.expect(vault.expandedDirectoryPaths.Count).toBe (1)

                                Vitest
                                    .expect(vault.expandedDirectoryPaths.Values |> Seq.exactlyOne)
                                    .toBe ("studies/S1/dataset")

                            do! vault.StopFileWatcher()
                        with error ->
                            do! vault.StopFileWatcher()
                            return raise error
                    })
        )

        Vitest.test (
            "RenameOpenArcRoot clears pending watcher state before moving the active ARC",
            fun () ->
                TestHelpers.withTempArcWith
                    "swate-rename-arc-root-watcher-"
                    "RenameRootWatcherArc"
                    ignore
                    (fun arcPath -> promise {
                        let vault = ArcVault(TestHelpers.testWindow ())
                        vault.path <- Some arcPath
                        do! vault.LoadArc()

                        let timeoutId = Fable.Core.JS.setTimeout (fun () -> ()) 60000

                        vault.fileWatcherReloadArcTimeout <- Some timeoutId

                        vault.fileWatcherPendingEvents.Add {
                            EventName = "change"
                            RelativePath = "isa.investigation.xlsx"
                            AbsolutePath = join [| arcPath; "isa.investigation.xlsx" |]
                        }

                        vault.fileWatcherPendingArcMergeEvents.Add {
                            EventName = "change"
                            RelativePath = "isa.investigation.xlsx"
                            AbsolutePath = join [| arcPath; "isa.investigation.xlsx" |]
                        }

                        try
                            match! vault.RenameOpenArcRoot "renamed-arc-watcher" with
                            | Error error -> failwith error.Message
                            | Ok _ ->
                                Vitest.expect(vault.fileWatcherReloadArcTimeout).toEqual (None)
                                Vitest.expect(vault.fileWatcherPendingEvents.Count).toBe (0)
                                Vitest.expect(vault.fileWatcherPendingArcMergeEvents.Count).toBe (0)
                        finally
                            vault.fileWatcherReloadArcTimeout |> Option.iter Fable.Core.JS.clearTimeout
                    })
        )

        Vitest.test (
            "RenameOpenArcRoot rejects destination conflicts without moving the active ARC",
            fun () ->
                TestHelpers.withTempArcWith
                    "swate-rename-arc-root-conflict-"
                    "RenameRootConflictArc"
                    ignore
                    (fun arcPath -> promise {
                        let targetPath =
                            join [| dirname arcPath; "existing-arc" |] |> PathHelpers.normalizePath

                        do! mkdirRecursiveAsync targetPath

                        let vault = ArcVault(TestHelpers.testWindow ())
                        vault.path <- Some arcPath
                        do! vault.LoadArc()

                        match! vault.RenameOpenArcRoot "existing-arc" with
                        | Ok _ -> failwith "Expected active ARC root rename to reject an existing destination."
                        | Error error ->
                            Vitest.expect(error.Message).toContain ("destination already exists")
                            Vitest.expect(vault.path).toEqual (Some arcPath)

                            let! oldPathExists = TestHelpers.pathExistsAsync arcPath
                            let! targetPathExists = TestHelpers.pathExistsAsync targetPath
                            Vitest.expect(oldPathExists).toBe (true)
                            Vitest.expect(targetPathExists).toBe (true)
                    })
        )

        Vitest.test (
            "tryBuildOpenArcRootRenamePlan applies the shared rename-name validation rules",
            fun () ->
                match tryBuildOpenArcRootRenamePlan "C:/work/current-arc" "bad\u0000name" with
                | Ok _ -> failwith "Expected ARC root rename to reject null characters."
                | Error error -> Vitest.expect(error.Message).toContain ("null")
        )

        Vitest.test (
            "LoadArc repairs zero-byte canonical ARC workbooks before retrying",
            fun () ->
                TestHelpers.withTempArcWith
                    "swate-load-arc-repair-"
                    "RepairArc"
                    ignore
                    (fun arcPath -> promise {
                        let assayFolder = join [| arcPath; "assays"; "New Assay" |]
                        let assayFile = join [| assayFolder; "isa.assay.xlsx" |]

                        do! mkdirRecursiveAsync assayFolder
                        do! writeTextFileAsync assayFile ""

                        let vault = ArcVault(TestHelpers.testWindow ())
                        vault.path <- Some arcPath

                        do! vault.LoadArc()

                        Vitest.expect(vault.arc.IsSome).toBe (true)
                        Vitest.expect(vault.arc.Value.ContainsAssay("New Assay")).toBe (true)
                        Vitest.expect(vault.hasUnsavedArcChanges).toBe (false)
                        Vitest.expect(vault.arc.Value.hasInMemoryChanges ()).toBe (false)

                        let! reloadedArc = TestHelpers.loadArcAsync arcPath
                        Vitest.expect(reloadedArc.ContainsAssay("New Assay")).toBe (true)
                    })
        )

        Vitest.test (
            "LoadArc baselines loaded datamap hashes without marking the ARC dirty",
            fun () ->
                TestHelpers.withTempArcWith
                    "swate-load-arc-datamap-baseline-"
                    "DatamapBaselineArc"
                    addDataMapToAllEntityTypes
                    (fun arcPath -> promise {
                        let vault = ArcVault(TestHelpers.testWindow ())
                        vault.path <- Some arcPath

                        do! vault.LoadArc()

                        Vitest.expect(vault.arc.IsSome).toBe (true)
                        Vitest.expect(vault.hasUnsavedArcChanges).toBe (false)
                        Vitest.expect(vault.arc.Value.hasInMemoryChanges ()).toBe (false)

                        let loadedArc = vault.arc.Value
                        Vitest.expect(loadedArc.GetAssay("Assay With DataMap").DataMap.Value.StaticHash).not.toBe (0)
                        Vitest.expect(loadedArc.GetStudy("Study With DataMap").DataMap.Value.StaticHash).not.toBe (0)

                        Vitest
                            .expect(loadedArc.GetWorkflow("Workflow With DataMap").DataMap.Value.StaticHash)
                            .not.toBe (0)

                        Vitest.expect(loadedArc.GetRun("Run With DataMap").DataMap.Value.StaticHash).not.toBe (0)
                    })
        )
)
