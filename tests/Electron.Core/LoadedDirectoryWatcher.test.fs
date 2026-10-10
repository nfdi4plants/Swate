module ElectronCore.LoadedDirectoryWatcherTests

open Fable.Core
open Fable.Core.JsInterop
open Fable.Electron.Main
open Main.ArcVault
open Main.Bindings.Filesystem
open Main.Bindings.Path
open Swate.Components.Shared
open Swate.Electron.Shared.FileIOTypes
open Vitest

Vitest.vi.mock ("chokidar", createObj [ "spy" ==> true ]) |> ignore

[<Import("watch", "chokidar")>]
let private watchMock: obj = jsNative

[<Emit("$0.mockReset()")>]
let private resetWatchMock (_spy: obj) : unit = jsNative

Vitest.beforeEach (fun () -> resetWatchMock watchMock)

[<Emit("$0.mock.calls.length")>]
let private invocationCount (_spy: obj) : int = jsNative

[<Emit("$0.mockImplementationOnce(() => { const handlers = new Map(); const watcher = { on(event, callback) { handlers.set(event, callback); if (event === 'all') { $1(); queueMicrotask(() => handlers.get('ready')?.()); } return watcher; }, close() { return Promise.resolve(); }, add() { return watcher; }, unwatch() { return watcher; }, getWatched() { return {}; } }; return watcher; })")>]
let private interceptNextWatchReady (_spy: obj) (_beforeReady: unit -> unit) : unit = jsNative

[<Emit("$0.mockImplementationOnce(() => { let ready; const watcher = { on(event, callback) { if (event === 'ready') { ready = callback; $2(() => ready?.()); } if (event === 'all') { $1(); } return watcher; }, close() { $3(); return Promise.resolve(); }, add() { return watcher; }, unwatch() { return watcher; }, getWatched() { return {}; } }; return watcher; })")>]
let private interceptNextControlledWatch
    (_spy: obj)
    (_onCreated: unit -> unit)
    (_captureReady: (unit -> unit) -> unit)
    (_onClosed: unit -> unit)
    : unit =
    jsNative

[<Emit("$0.mockImplementationOnce(() => { let error; const watcher = { on(event, callback) { if (event === 'error') { error = callback; } return watcher; }, close() { $3(); return Promise.resolve(); }, add() { return watcher; }, unwatch() { return watcher; }, getWatched() { return {}; } }; $1(); $2(value => error?.(value)); return watcher; })")>]
let private interceptNextFailingWatch
    (_spy: obj)
    (_onCreated: unit -> unit)
    (_captureError: (exn -> unit) -> unit)
    (_onClosed: unit -> unit)
    : unit =
    jsNative

[<Emit("$0.close = () => { $1(); return $2; }")>]
let private replaceWatcherClose
    (_watcher: Main.Bindings.Chokidar.IWatcher)
    (_onClose: unit -> unit)
    (_close: JS.Promise<unit>)
    : unit =
    jsNative

let private waitUntil description predicate =
    let rec loop remaining = promise {
        if predicate () then
            return ()
        elif remaining = 0 then
            return failwith $"Timed out waiting for {description}."
        else
            do! Promise.sleep 25
            return! loop (remaining - 1)
    }

    loop 200

let private containsPath path (vault: ArcVault) =
    vault.fileTree.Values
    |> Seq.exists (fun entry -> PathHelpers.pathsEqual entry.path path)

let private loadedDirectoryWatcher (vault: ArcVault) =
    vault.LoadedDirectoryWatcherController.Current.Watcher

[<Emit("Object.entries($0.getWatched()).flatMap(([directory, names]) => names.map(name => `${directory}/${name}`))")>]
let private watchedPaths (_watcher: Main.Bindings.Chokidar.IWatcher) : string[] = jsNative

let private isWatchedPath arcPath relativePath watcher =
    let expectedRelativePath = PathHelpers.normalizeCanonicalRelativePath relativePath

    watchedPaths watcher
    |> Array.exists (fun watchedPath ->
        let relativeWatchedPath =
            if isAbsolute watchedPath then
                Swate.Electron.Shared.FileIOHelper.tryGetRepoRelativePath arcPath watchedPath
                |> Option.defaultValue watchedPath
            else
                watchedPath

        PathHelpers.pathsEqual (PathHelpers.normalizeCanonicalRelativePath relativeWatchedPath) expectedRelativePath
    )

[<Emit("$0 === $1")>]
let private isSameWatcher (_left: Main.Bindings.Chokidar.IWatcher) (_right: Main.Bindings.Chokidar.IWatcher) : bool =
    jsNative

let private recordingWindow onSend =
    let send: obj = emitJsExpr onSend "((...args) => $0(args))"

    createObj [
        "id" ==> 901
        "title" ==> ""
        "isDestroyed" ==> (fun () -> false)
        "webContents"
        ==> createObj [ "send" ==> send; "isDestroyed" ==> (fun () -> false) ]
    ]
    |> unbox<BrowserWindow>

let private withLoadedDirectoryFixtureUsingWindow window testBody = promise {
    let! rootPath = TestHelpers.createTempDirectoryAsync "swate-loaded-directory-watcher-"
    let datasetPath = join [| rootPath; "dataset" |]
    let nestedPath = join [| datasetPath; "nested" |]

    try
        let! _ = mkdirAsync nestedPath (MkdirOptions(recursive = true))
        do! writeFileAsync (join [| datasetPath; "existing.txt" |]) "existing" TextEncoding.Utf8
        do! writeFileAsync (join [| nestedPath; "deep.txt" |]) "deep" TextEncoding.Utf8

        let vault = ArcVault(window)
        vault.path <- Some rootPath
        let! initialRootPage = Main.FileTreeCreator.getFileTreeRootPage rootPath
        vault.fileTree <- initialRootPage.Entries

        try
            do! testBody vault rootPath datasetPath nestedPath
            do! vault.StopFileWatcher()
            do! TestHelpers.removeDirectoryAsync rootPath
        with error ->
            do! vault.StopFileWatcher()
            do! TestHelpers.removeDirectoryAsync rootPath
            return raise error
    with error ->
        do! TestHelpers.removeDirectoryAsync rootPath
        return raise error
}

let private withLoadedDirectoryFixture testBody =
    withLoadedDirectoryFixtureUsingWindow (TestHelpers.testWindow ()) testBody

let private createNumberedFiles directory count = promise {
    let paths =
        Array.init count (fun index -> join [| directory; sprintf "file-%03d.txt" index |])

    for path in paths do
        do! writeFileAsync path "content" TextEncoding.Utf8

    return paths
}

let private findEntryOutsidePage (paths: string[]) (page: Main.FileTreeCreator.FileTreeDirectoryPage) =
    paths
    |> Array.find (fun path ->
        page.Entries
        |> Array.exists (fun entry -> PathHelpers.pathsEqual entry.path path)
        |> not
    )

Vitest.describe (
    "loaded FileTree directory watching",
    fun () ->
        Vitest.test (
            "partial refresh preserves a cached child absent from the returned page",
            TestOptions(timeout = 15000),
            fun () -> promise {
                do!
                    withLoadedDirectoryFixture (fun vault rootPath datasetPath _ -> promise {
                        let! paths = createNumberedFiles datasetPath 150
                        let! page = Main.FileTreeCreator.readFileTreeDirectoryPrefix rootPath "dataset" 0 100
                        let cachedPath = findEntryOutsidePage paths page
                        vault.fileTree.[cachedPath] <- FileEntry.create (basename cachedPath, cachedPath, false)
                        vault.LoadedDirectoryWatcherController.AddLoadedDirectory("dataset", false)

                        do! vault.RefreshFileTreeDirectory "dataset"

                        Vitest.expect(vault.fileTreeDirectoryHasMore.["dataset"]).toBe true
                        Vitest.expect(containsPath cachedPath vault).toBe true
                    })
            }
        )

        Vitest.test (
            "partial refresh preserves descendants of a cached directory absent from the returned page",
            TestOptions(timeout = 15000),
            fun () -> promise {
                do!
                    withLoadedDirectoryFixture (fun vault rootPath datasetPath _ -> promise {
                        let! paths = createNumberedFiles datasetPath 150
                        let! page = Main.FileTreeCreator.readFileTreeDirectoryPrefix rootPath "dataset" 0 100
                        let cachedDirectoryPath = findEntryOutsidePage paths page + "-directory"
                        let cachedDescendantPath = join [| cachedDirectoryPath; "deep.txt" |]
                        let! _ = mkdirAsync cachedDirectoryPath (MkdirOptions(recursive = true))
                        do! writeFileAsync cachedDescendantPath "deep" TextEncoding.Utf8

                        vault.fileTree.[cachedDirectoryPath] <-
                            FileEntry.create (basename cachedDirectoryPath, cachedDirectoryPath, true)

                        vault.fileTree.[cachedDescendantPath] <-
                            FileEntry.create (basename cachedDescendantPath, cachedDescendantPath, false)

                        vault.LoadedDirectoryWatcherController.AddLoadedDirectory("dataset", false)

                        do! vault.RefreshFileTreeDirectory "dataset"

                        Vitest.expect(vault.fileTreeDirectoryHasMore.["dataset"]).toBe true
                        Vitest.expect(containsPath cachedDirectoryPath vault).toBe true
                        Vitest.expect(containsPath cachedDescendantPath vault).toBe true
                    })
            }
        )

        Vitest.test (
            "loaded watcher removes a deleted file from a partial directory and preserves other cached entries",
            TestOptions(timeout = 20000),
            fun () -> promise {
                do!
                    withLoadedDirectoryFixture (fun vault rootPath datasetPath _ -> promise {
                        let! paths = createNumberedFiles datasetPath 150
                        do! vault.RefreshFileTreeDirectory "dataset"
                        let! page = Main.FileTreeCreator.readFileTreeDirectoryPrefix rootPath "dataset" 0 100

                        let deletedPath = paths |> Array.find (fun path -> containsPath path vault)

                        let preservedPath = findEntryOutsidePage paths page

                        vault.fileTree.[preservedPath] <-
                            FileEntry.create (basename preservedPath, preservedPath, false)

                        do! Promise.sleep 200

                        do! rmAsync deletedPath (RmOptions())

                        do!
                            waitUntil
                                "partial-directory file deletion"
                                (fun () -> not (containsPath deletedPath vault))

                        Vitest.expect(vault.fileTreeDirectoryHasMore.["dataset"]).toBe true
                        Vitest.expect(containsPath preservedPath vault).toBe true
                    })
            }
        )

        Vitest.test (
            "loaded watcher removes a deleted directory and its cached descendants from a partial directory",
            TestOptions(timeout = 20000),
            fun () -> promise {
                do!
                    withLoadedDirectoryFixture (fun vault _ datasetPath _ -> promise {
                        let! _ = createNumberedFiles datasetPath 150
                        let deletedDirectoryPath = join [| datasetPath; "deleted-directory" |]
                        let deletedDescendantPath = join [| deletedDirectoryPath; "deep.txt" |]
                        let! _ = mkdirAsync deletedDirectoryPath (MkdirOptions(recursive = true))
                        do! writeFileAsync deletedDescendantPath "deep" TextEncoding.Utf8
                        do! vault.RefreshFileTreeDirectory "dataset"

                        vault.fileTree.[deletedDirectoryPath] <-
                            FileEntry.create (basename deletedDirectoryPath, deletedDirectoryPath, true)

                        vault.fileTree.[deletedDescendantPath] <-
                            FileEntry.create (basename deletedDescendantPath, deletedDescendantPath, false)

                        do! Promise.sleep 200

                        do! rmAsync deletedDirectoryPath (RmOptions(recursive = true, force = true))

                        do!
                            waitUntil
                                "partial-directory directory deletion"
                                (fun () -> not (containsPath deletedDirectoryPath vault))

                        Vitest.expect(vault.fileTreeDirectoryHasMore.["dataset"]).toBe true
                        Vitest.expect(containsPath deletedDescendantPath vault).toBe false
                    })
            }
        )

        Vitest.test (
            "loaded watcher keeps a file recreated before its queued deletion is applied",
            TestOptions(timeout = 20000),
            fun () -> promise {
                do!
                    withLoadedDirectoryFixture (fun vault _ datasetPath _ -> promise {
                        let! paths = createNumberedFiles datasetPath 150
                        do! vault.RefreshFileTreeDirectory "dataset"

                        let recreatedPath = paths |> Array.find (fun path -> containsPath path vault)

                        do! Promise.sleep 200
                        let queueGate, releaseQueue = TestHelpers.deferred ()
                        vault.FileTreeUpdateTail <- queueGate
                        do! rmAsync recreatedPath (RmOptions())

                        do!
                            waitUntil
                                "queued loaded-directory deletion"
                                (fun () -> vault.LoadedDirectoryRefreshes.ContainsKey "dataset")

                        do! writeFileAsync recreatedPath "recreated" TextEncoding.Utf8
                        releaseQueue ()
                        do! vault.FileTreeUpdateTail

                        Vitest.expect(containsPath recreatedPath vault).toBe true
                    })
            }
        )

        Vitest.test (
            "loaded scopes grow explicitly and remain shallow",
            TestOptions(timeout = 15000),
            fun () -> promise {
                do!
                    withLoadedDirectoryFixture (fun vault rootPath datasetPath nestedPath -> promise {
                        Vitest.expect(vault.IsFileTreeDirectoryLoaded "dataset").toBe false

                        do! vault.RefreshFileTreeDirectory "dataset"

                        Vitest.expect(vault.IsFileTreeDirectoryLoaded "dataset").toBe true
                        Vitest.expect(vault.IsFileTreeDirectoryLoaded "dataset/nested").toBe false
                        Vitest.expect((loadedDirectoryWatcher vault).IsSome).toBe true
                        Vitest.expect(containsPath (join [| datasetPath; "existing.txt" |]) vault).toBe true
                        Vitest.expect(containsPath (join [| nestedPath; "deep.txt" |]) vault).toBe false

                        do!
                            waitUntil
                                "dataset watcher readiness"
                                (fun () ->
                                    isWatchedPath rootPath "dataset/existing.txt" (loadedDirectoryWatcher vault).Value
                                )

                        Vitest
                            .expect(
                                isWatchedPath rootPath "dataset/nested/deep.txt" (loadedDirectoryWatcher vault).Value
                            )
                            .toBe
                            false

                        // Collapse is renderer-local, so no main-process action removes the loaded scope.
                        Vitest.expect(vault.IsFileTreeDirectoryLoaded "dataset").toBe true

                        do! vault.RefreshFileTreeDirectory "dataset/nested"
                        Vitest.expect(vault.IsFileTreeDirectoryLoaded "dataset/nested").toBe true
                        Vitest.expect(containsPath (join [| nestedPath; "deep.txt" |]) vault).toBe true

                        do!
                            waitUntil
                                "nested watcher readiness"
                                (fun () ->
                                    isWatchedPath
                                        rootPath
                                        "dataset/nested/deep.txt"
                                        (loadedDirectoryWatcher vault).Value
                                )
                    })
            }
        )

        Vitest.test (
            "native shallow events reconcile loaded parents but ignore unloaded descendants",
            TestOptions(timeout = 15000),
            fun () -> promise {
                do!
                    withLoadedDirectoryFixture (fun vault _ datasetPath nestedPath -> promise {
                        do! vault.RefreshFileTreeDirectory "dataset"
                        do! Promise.sleep 200

                        let addedPath = join [| datasetPath; "added.txt" |]
                        do! writeFileAsync addedPath "added" TextEncoding.Utf8
                        do! waitUntil "loaded-directory addition" (fun () -> containsPath addedPath vault)

                        do! rmAsync addedPath (RmOptions())

                        do! waitUntil "loaded-directory deletion" (fun () -> not (containsPath addedPath vault))

                        do! vault.RefreshFileTreeDirectory "dataset/nested"
                        Vitest.expect(vault.IsFileTreeDirectoryLoaded "dataset/nested").toBe true

                        do! rmAsync nestedPath (RmOptions(recursive = true, force = true))

                        do!
                            waitUntil
                                "deleted loaded-directory scope removal"
                                (fun () -> not (vault.IsFileTreeDirectoryLoaded "dataset/nested"))

                        Vitest.expect(containsPath nestedPath vault).toBe false
                    })
            }
        )

        Vitest.test (
            "a shallow root refresh discovers external root additions",
            TestOptions(timeout = 15000),
            fun () -> promise {
                do!
                    withLoadedDirectoryFixture (fun vault rootPath _ _ -> promise {
                        let rootFilePath = join [| rootPath; "external-root.txt" |]
                        do! writeFileAsync rootFilePath "root" TextEncoding.Utf8

                        Vitest.expect(containsPath rootFilePath vault).toBe false
                        do! vault.RefreshFileTreeDirectory ""
                        Vitest.expect(containsPath rootFilePath vault).toBe true
                        Vitest.expect(vault.IsFileTreeDirectoryLoaded "").toBe false
                    })
            }
        )

        Vitest.test (
            "an import-style refresh updates loaded targets without materializing unloaded targets",
            TestOptions(timeout = 15000),
            fun () -> promise {
                do!
                    withLoadedDirectoryFixture (fun vault _ datasetPath nestedPath -> promise {
                        let importedDatasetPath = join [| datasetPath; "imported.csv" |]
                        let importedNestedPath = join [| nestedPath; "not-loaded.csv" |]

                        do! vault.RefreshFileTreeDirectory "dataset"
                        do! writeFileAsync importedDatasetPath "loaded" TextEncoding.Utf8
                        do! writeFileAsync importedNestedPath "unloaded" TextEncoding.Utf8

                        let! refreshedLoaded = vault.RefreshFileTreeDirectoryIfLoaded "dataset"
                        let! refreshedUnloaded = vault.RefreshFileTreeDirectoryIfLoaded "dataset/nested"

                        Vitest.expect(refreshedLoaded).toBe true
                        Vitest.expect(refreshedUnloaded).toBe false
                        Vitest.expect(containsPath importedDatasetPath vault).toBe true
                        Vitest.expect(containsPath importedNestedPath vault).toBe false
                    })
            }
        )

        Vitest.test (
            "major reset and later expansion serialize without stale overwrite",
            TestOptions(timeout = 15000),
            fun () -> promise {
                do!
                    withLoadedDirectoryFixture (fun vault _ datasetPath nestedPath -> promise {
                        do! vault.RefreshFileTreeDirectory "dataset"
                        do! vault.RefreshFileTreeDirectory "dataset/nested"

                        let reset = vault.ResetFileTreeToRoot()
                        let laterExpansion = vault.RefreshFileTreeDirectory "dataset"
                        do! reset
                        do! laterExpansion

                        Vitest.expect(vault.IsFileTreeDirectoryLoaded "dataset").toBe true
                        Vitest.expect(vault.IsFileTreeDirectoryLoaded "dataset/nested").toBe false
                        Vitest.expect(containsPath (join [| datasetPath; "existing.txt" |]) vault).toBe true
                        Vitest.expect(containsPath (join [| nestedPath; "deep.txt" |]) vault).toBe false

                        do! vault.ResetFileTreeToRoot()
                        Vitest.expect(vault.loadedFileTreeDirectories.Count).toBe 0
                        Vitest.expect((loadedDirectoryWatcher vault).IsNone).toBe true
                        Vitest.expect(containsPath (join [| datasetPath; "existing.txt" |]) vault).toBe false
                    })
            }
        )

        Vitest.test (
            "overlapping major reset requests share one root reset",
            TestOptions(timeout = 15000),
            fun () -> promise {
                let mutable publications = 0
                let window = recordingWindow (fun _ -> publications <- publications + 1)

                do!
                    withLoadedDirectoryFixtureUsingWindow
                        window
                        (fun vault _ _ _ -> promise {
                            do! vault.RefreshFileTreeDirectory "dataset"
                            publications <- 0

                            let resetGate, releaseReset = TestHelpers.deferred ()
                            vault.FileTreeUpdateTail <- resetGate

                            let firstReset = vault.ResetFileTreeToRoot()
                            let secondReset = vault.ResetFileTreeToRoot()
                            releaseReset ()

                            do! firstReset
                            do! secondReset

                            Vitest.expect(publications).toBe 1
                            Vitest.expect(vault.loadedFileTreeDirectories.Count).toBe 0
                            Vitest.expect((loadedDirectoryWatcher vault).IsNone).toBe true
                        })
            }
        )

        Vitest.test (
            "one payload event burst produces bounded refresh publications for its parent",
            TestOptions(timeout = 20000),
            fun () -> promise {
                let mutable publications = 0
                let window = recordingWindow (fun _ -> publications <- publications + 1)

                do!
                    withLoadedDirectoryFixtureUsingWindow
                        window
                        (fun vault rootPath datasetPath _ -> promise {
                            do! vault.RefreshFileTreeDirectory "dataset"

                            do!
                                waitUntil
                                    "payload watcher readiness"
                                    (fun () ->
                                        isWatchedPath
                                            rootPath
                                            "dataset/existing.txt"
                                            (loadedDirectoryWatcher vault).Value
                                    )

                            publications <- 0

                            let addedPaths =
                                Array.init 24 (fun index -> join [| datasetPath; $"burst-{index:D2}.txt" |])

                            let writes =
                                addedPaths
                                |> Array.map (fun path -> writeFileAsync path "burst" TextEncoding.Utf8)

                            let! _ = JS.Constructors.Promise.all writes

                            do!
                                waitUntil
                                    "coalesced payload burst"
                                    (fun () -> addedPaths |> Array.forall (fun path -> containsPath path vault))

                            Vitest.expect(publications).toBeGreaterThanOrEqual 1
                            Vitest.expect(publications).toBeLessThanOrEqual 2
                        })
            }
        )

        Vitest.test (
            "events for distinct loaded parents reconcile both parents",
            TestOptions(timeout = 20000),
            fun () -> promise {
                do!
                    withLoadedDirectoryFixture (fun vault rootPath datasetPath nestedPath -> promise {
                        do! vault.RefreshFileTreeDirectory "dataset"
                        do! vault.RefreshFileTreeDirectory "dataset/nested"

                        do!
                            waitUntil
                                "nested watcher readiness"
                                (fun () ->
                                    isWatchedPath
                                        rootPath
                                        "dataset/nested/deep.txt"
                                        (loadedDirectoryWatcher vault).Value
                                )

                        let datasetFile = join [| datasetPath; "dataset-event.txt" |]
                        let nestedFile = join [| nestedPath; "nested-event.txt" |]

                        let! _ =
                            JS.Constructors.Promise.all [|
                                writeFileAsync datasetFile "dataset" TextEncoding.Utf8
                                writeFileAsync nestedFile "nested" TextEncoding.Utf8
                            |]

                        do!
                            waitUntil
                                "independent loaded-parent refreshes"
                                (fun () -> containsPath datasetFile vault && containsPath nestedFile vault)
                    })
            }
        )

        Vitest.test (
            "events arriving during an active parent refresh produce one follow-up refresh",
            TestOptions(timeout = 15000),
            fun () -> promise {
                let mutable publications = 0
                let window = recordingWindow (fun _ -> publications <- publications + 1)

                do!
                    withLoadedDirectoryFixtureUsingWindow
                        window
                        (fun vault _ _ _ -> promise {
                            do! vault.RefreshFileTreeDirectory "dataset"
                            do! (loadedDirectoryWatcher vault).Value.close ()
                            vault.LoadedDirectoryWatcherController.RetireWatcher() |> ignore
                            publications <- 0

                            let firstRefreshGate, releaseFirstRefresh = TestHelpers.deferred ()
                            vault.FileTreeUpdateTail <- firstRefreshGate

                            vault.QueueLoadedDirectoryRefresh "dataset"

                            for _ in 1..12 do
                                vault.QueueLoadedDirectoryRefresh "dataset"

                            releaseFirstRefresh ()

                            do! waitUntil "coalesced refresh completion" (fun () -> publications = 2)

                            Vitest.expect(publications).toBe 2
                        })
            }
        )

        Vitest.test (
            "follow-up refresh publishes only the final state of repeated changes",
            TestOptions(timeout = 15000),
            fun () -> promise {
                let observedStates = ResizeArray<string>()
                let mutable isArmed = false
                let mutable vaultUnderTest: ArcVault option = None
                let mutable firstPath = ""
                let mutable intermediatePath = ""
                let mutable finalPath = ""

                let window =
                    recordingWindow (fun _ ->
                        if isArmed then
                            let vault = vaultUnderTest.Value

                            let visibleState =
                                if containsPath firstPath vault then "first"
                                elif containsPath intermediatePath vault then "intermediate"
                                elif containsPath finalPath vault then "final"
                                else "missing"

                            observedStates.Add visibleState

                            if observedStates.Count = 1 then
                                renameSync firstPath intermediatePath
                                vault.QueueLoadedDirectoryRefresh "dataset"
                                renameSync intermediatePath finalPath
                                vault.QueueLoadedDirectoryRefresh "dataset"
                    )

                do!
                    withLoadedDirectoryFixtureUsingWindow
                        window
                        (fun vault _ datasetPath _ -> promise {
                            vaultUnderTest <- Some vault
                            do! vault.RefreshFileTreeDirectory "dataset"
                            do! (loadedDirectoryWatcher vault).Value.close ()
                            vault.LoadedDirectoryWatcherController.RetireWatcher() |> ignore

                            firstPath <- join [| datasetPath; "race-first.txt" |]
                            intermediatePath <- join [| datasetPath; "race-intermediate.txt" |]
                            finalPath <- join [| datasetPath; "race-final.txt" |]
                            writeFileSync firstPath "race" TextEncoding.Utf8
                            isArmed <- true

                            vault.QueueLoadedDirectoryRefresh "dataset"
                            do! waitUntil "final coalesced directory state" (fun () -> observedStates.Count = 2)
                            do! vault.FileTreeUpdateTail

                            Vitest.expect(observedStates.Count).toBe 2
                            Vitest.expect(observedStates.[0]).toBe "first"
                            Vitest.expect(observedStates.[1]).toBe "final"
                            Vitest.expect(containsPath intermediatePath vault).toBe false
                            Vitest.expect(containsPath finalPath vault).toBe true
                        })
            }
        )

        Vitest.test (
            "major reset invalidates pending loaded-directory follow-up work",
            TestOptions(timeout = 15000),
            fun () -> promise {
                do!
                    withLoadedDirectoryFixture (fun vault _ datasetPath _ -> promise {
                        do! vault.RefreshFileTreeDirectory "dataset"

                        let refreshGate, releaseRefresh = TestHelpers.deferred ()
                        vault.FileTreeUpdateTail <- refreshGate
                        vault.QueueLoadedDirectoryRefresh "dataset"

                        let reset = vault.ResetFileTreeToRoot()
                        vault.QueueLoadedDirectoryRefresh "dataset"
                        releaseRefresh ()
                        do! reset

                        Vitest.expect(vault.loadedFileTreeDirectories.Count).toBe 0
                        Vitest.expect((loadedDirectoryWatcher vault).IsNone).toBe true
                        Vitest.expect(containsPath (join [| datasetPath; "existing.txt" |]) vault).toBe false
                    })
            }
        )

        Vitest.test (
            "watcher suspension restores exactly once and preserves operation outcomes",
            TestOptions(timeout = 15000),
            fun () -> promise {
                Vitest.vi.clearAllMocks ()

                do!
                    withLoadedDirectoryFixture (fun vault _ _ _ -> promise {
                        do! vault.RefreshFileTreeDirectory "dataset"

                        let successWatcherCount = invocationCount watchMock
                        let! result = vault.WithLoadedDirectoryWatcherSuspended(fun () -> promise { return 42 })
                        Vitest.expect(result).toBe 42
                        Vitest.expect(invocationCount watchMock).toBe (successWatcherCount + 1)

                        let operationError = exn "Expected operation failure."
                        let operationFailureWatcherCount = invocationCount watchMock
                        let mutable capturedOperationError: exn option = None

                        try
                            do!
                                vault.WithLoadedDirectoryWatcherSuspended(fun () -> promise {
                                    return raise operationError
                                })
                        with error ->
                            capturedOperationError <- Some error

                        Vitest.expect(capturedOperationError.Value).toBe operationError
                        Vitest.expect(invocationCount watchMock).toBe (operationFailureWatcherCount + 1)
                    })
            }
        )

        Vitest.test (
            "watcher suspension restores exactly once after a synchronous operation failure",
            TestOptions(timeout = 15000),
            fun () -> promise {
                Vitest.vi.clearAllMocks ()

                do!
                    withLoadedDirectoryFixture (fun vault _ _ _ -> promise {
                        do! vault.RefreshFileTreeDirectory "dataset"

                        let watcherCount = invocationCount watchMock
                        let operationError = exn "Expected synchronous operation failure."
                        let mutable capturedOperationError: exn option = None

                        try
                            do! vault.WithLoadedDirectoryWatcherSuspended(fun () -> raise operationError)
                        with error ->
                            capturedOperationError <- Some error

                        Vitest.expect(capturedOperationError.Value).toBe operationError
                        Vitest.expect(invocationCount watchMock).toBe (watcherCount + 1)
                        Vitest.expect((loadedDirectoryWatcher vault).IsSome).toBe true
                    })
            }
        )

        Vitest.test (
            "watcher restoration catches up scopes that were loaded before suspension",
            TestOptions(timeout = 15000),
            fun () -> promise {
                do!
                    withLoadedDirectoryFixture (fun vault _ datasetPath _ -> promise {
                        do! vault.RefreshFileTreeDirectory "dataset"
                        let addedPath = join [| datasetPath; "added-while-suspended.txt" |]

                        do!
                            vault.WithLoadedDirectoryWatcherSuspended(fun () -> promise {
                                Vitest.expect((loadedDirectoryWatcher vault).IsNone).toBe true
                                do! writeFileAsync addedPath "suspended" TextEncoding.Utf8
                                Vitest.expect(containsPath addedPath vault).toBe false
                            })

                        Vitest.expect(containsPath addedPath vault).toBe true
                        Vitest.expect((loadedDirectoryWatcher vault).IsSome).toBe true
                        Vitest.expect(vault.IsFileTreeDirectoryLoaded "dataset").toBe true
                    })
            }
        )

        Vitest.test (
            "concurrent suspended operations are queued and restore only after the queue drains",
            TestOptions(timeout = 15000),
            fun () -> promise {
                do!
                    withLoadedDirectoryFixture (fun vault _ datasetPath _ -> promise {
                        do! vault.RefreshFileTreeDirectory "dataset"

                        let firstOperationGate, releaseFirstOperation = TestHelpers.deferred ()
                        let secondOperationGate, releaseSecondOperation = TestHelpers.deferred ()
                        let enteredOperations = ResizeArray<int>()

                        let addedPath = join [| datasetPath; "added-by-first-queued-mutation.txt" |]

                        let first =
                            vault.WithLoadedDirectoryWatcherSuspended(fun () -> promise {
                                enteredOperations.Add 1
                                Vitest.expect((loadedDirectoryWatcher vault).IsNone).toBe true
                                do! writeFileAsync addedPath "queued" TextEncoding.Utf8
                                do! firstOperationGate
                            })

                        let second =
                            vault.WithLoadedDirectoryWatcherSuspended(fun () -> promise {
                                enteredOperations.Add 2
                                Vitest.expect((loadedDirectoryWatcher vault).IsNone).toBe true
                                Vitest.expect(containsPath addedPath vault).toBe true
                                do! secondOperationGate
                            })

                        do! waitUntil "first queued mutation" (fun () -> enteredOperations.Count = 1)
                        Vitest.expect(enteredOperations.[0]).toBe 1

                        releaseFirstOperation ()
                        do! first

                        do! waitUntil "second queued mutation" (fun () -> enteredOperations.Count = 2)
                        Vitest.expect(enteredOperations.[1]).toBe 2

                        releaseSecondOperation ()
                        do! second

                        Vitest.expect((loadedDirectoryWatcher vault).IsSome).toBe true
                        Vitest.expect(containsPath addedPath vault).toBe true
                        Vitest.expect(vault.IsFileTreeDirectoryLoaded "dataset").toBe true
                    })
            }
        )

        Vitest.test (
            "refresh during watcher suspension does not recreate coverage before restoration",
            TestOptions(timeout = 15000),
            fun () -> promise {
                do!
                    withLoadedDirectoryFixture (fun vault rootPath _ _ -> promise {
                        do! vault.RefreshFileTreeDirectory "dataset"

                        let suspension =
                            vault.WithLoadedDirectoryWatcherSuspended(fun () -> promise {
                                Vitest.expect((loadedDirectoryWatcher vault).IsNone).toBe true
                                do! vault.RefreshFileTreeDirectory "dataset"
                                Vitest.expect((loadedDirectoryWatcher vault).IsNone).toBe true
                            })

                        do! suspension
                        Vitest.expect((loadedDirectoryWatcher vault).IsSome).toBe true

                        Vitest
                            .expect(isWatchedPath rootPath "dataset/existing.txt" (loadedDirectoryWatcher vault).Value)
                            .toBe
                            true
                    })
            }
        )

        Vitest.test (
            "a mutation queued during restoration waits for watcher readiness and catch-up",
            TestOptions(timeout = 15000),
            fun () -> promise {
                do!
                    withLoadedDirectoryFixture (fun vault _ datasetPath _ -> promise {
                        do! vault.RefreshFileTreeDirectory "dataset"

                        let restorationCreated, signalRestorationCreated = TestHelpers.deferred ()
                        let mutable signalRestorationReady = ignore

                        interceptNextControlledWatch
                            watchMock
                            signalRestorationCreated
                            (fun ready -> signalRestorationReady <- ready)
                            ignore

                        let firstOperationGate, releaseFirstOperation = TestHelpers.deferred ()
                        let secondOperationGate, releaseSecondOperation = TestHelpers.deferred ()
                        let mutable secondEntered = false
                        let addedPath = join [| datasetPath; "added-before-restoration-finished.txt" |]

                        let first =
                            vault.WithLoadedDirectoryWatcherSuspended(fun () -> promise {
                                do! writeFileAsync addedPath "queued restoration" TextEncoding.Utf8
                                do! firstOperationGate
                            })

                        releaseFirstOperation ()
                        do! restorationCreated

                        let second =
                            vault.WithLoadedDirectoryWatcherSuspended(fun () -> promise {
                                secondEntered <- true
                                Vitest.expect((loadedDirectoryWatcher vault).IsNone).toBe true
                                Vitest.expect(containsPath addedPath vault).toBe true
                                do! secondOperationGate
                            })

                        Vitest.expect(secondEntered).toBe false
                        signalRestorationReady ()
                        do! first
                        do! waitUntil "mutation queued during restoration" (fun () -> secondEntered)

                        releaseSecondOperation ()
                        do! second
                        Vitest.expect((loadedDirectoryWatcher vault).IsSome).toBe true
                    })
            }
        )

        Vitest.test (
            "refresh invalidated by StopFileWatcher cannot publish or restore loaded scopes",
            TestOptions(timeout = 15000),
            fun () -> promise {
                do!
                    withLoadedDirectoryFixture (fun vault _ datasetPath _ -> promise {
                        do! vault.RefreshFileTreeDirectory "dataset"
                        let stalePath = join [| datasetPath; "stale-after-stop.txt" |]
                        do! writeFileAsync stalePath "stale" TextEncoding.Utf8

                        let queueGate, releaseQueue = TestHelpers.deferred ()
                        vault.FileTreeUpdateTail <- queueGate
                        let refresh = vault.RefreshFileTreeDirectory "dataset"

                        do! vault.StopFileWatcher()
                        releaseQueue ()
                        do! refresh

                        Vitest.expect(containsPath stalePath vault).toBe false
                        Vitest.expect(vault.loadedFileTreeDirectories.Count).toBe 0
                        Vitest.expect((loadedDirectoryWatcher vault).IsNone).toBe true
                    })
            }
        )

        Vitest.test (
            "loaded watcher event during permanent watcher close is rejected by stop teardown",
            TestOptions(timeout = 15000),
            fun () -> promise {
                do!
                    withLoadedDirectoryFixture (fun vault _ datasetPath _ -> promise {
                        let permanentCloseGate, releasePermanentClose = TestHelpers.deferred ()
                        let permanentCloseStarted, signalPermanentCloseStarted = TestHelpers.deferred ()
                        vault.StartFileWatcher()
                        replaceWatcherClose vault.watcher.Value signalPermanentCloseStarted permanentCloseGate
                        do! vault.RefreshFileTreeDirectory "dataset"

                        let latePath = join [| datasetPath; "late-during-stop.txt" |]
                        do! writeFileAsync latePath "late" TextEncoding.Utf8

                        let stopping = vault.StopFileWatcher()
                        do! permanentCloseStarted

                        // Execute the same admission method used by the loaded watcher callback while
                        // the permanent watcher close is still pending.
                        vault.QueueLoadedDirectoryRefresh "dataset"

                        Vitest.expect(vault.LoadedDirectoryRefreshes.IsEmpty).toBe true
                        Vitest.expect(vault.loadedFileTreeDirectories.Count).toBe 0

                        releasePermanentClose ()
                        do! stopping
                        do! Promise.sleep 25

                        Vitest.expect((loadedDirectoryWatcher vault).IsNone).toBe true
                        Vitest.expect(vault.loadedFileTreeDirectories.Count).toBe 0
                        Vitest.expect(vault.PendingLoadedDirectoryHandoffs.Count).toBe 0
                        Vitest.expect(vault.LoadedDirectoryRefreshes.IsEmpty).toBe true
                        Vitest.expect(containsPath latePath vault).toBe false
                    })
            }
        )

        Vitest.test (
            "start after stop creates a fresh loaded-directory watcher lifecycle",
            TestOptions(timeout = 15000),
            fun () -> promise {
                do!
                    withLoadedDirectoryFixture (fun vault _ datasetPath _ -> promise {
                        vault.StartFileWatcher()
                        do! vault.StopFileWatcher()
                        vault.StartFileWatcher()
                        do! vault.RefreshFileTreeDirectory "dataset"

                        Vitest.expect((loadedDirectoryWatcher vault).IsSome).toBe true

                        let restartedPath = join [| datasetPath; "after-restart.txt" |]
                        do! writeFileAsync restartedPath "restart" TextEncoding.Utf8

                        do!
                            waitUntil
                                "loaded-directory event after restart"
                                (fun () -> containsPath restartedPath vault)
                    })
            }
        )

        Vitest.test (
            "refresh invalidated by a root reset cannot repopulate loaded scopes",
            TestOptions(timeout = 15000),
            fun () -> promise {
                do!
                    withLoadedDirectoryFixture (fun vault _ datasetPath _ -> promise {
                        do! vault.RefreshFileTreeDirectory "dataset"
                        let stalePath = join [| datasetPath; "stale-after-reset.txt" |]
                        do! writeFileAsync stalePath "stale" TextEncoding.Utf8

                        let queueGate, releaseQueue = TestHelpers.deferred ()
                        vault.FileTreeUpdateTail <- queueGate
                        let refresh = vault.RefreshFileTreeDirectory "dataset"
                        let reset = vault.ResetFileTreeToRoot()

                        releaseQueue ()
                        do! refresh
                        do! reset

                        Vitest.expect(containsPath stalePath vault).toBe false
                        Vitest.expect(vault.loadedFileTreeDirectories.Count).toBe 0
                        Vitest.expect((loadedDirectoryWatcher vault).IsNone).toBe true
                    })
            }
        )

        Vitest.test (
            "first directory load catches changes made while watcher coverage is established",
            TestOptions(timeout = 15000),
            fun () -> promise {
                do!
                    withLoadedDirectoryFixture (fun vault _ datasetPath _ -> promise {
                        let handoffPath = join [| datasetPath; "created-during-watch-handoff.txt" |]

                        interceptNextWatchReady
                            watchMock
                            (fun () -> writeFileSync handoffPath "handoff" TextEncoding.Utf8)

                        do! vault.RefreshFileTreeDirectory "dataset"

                        Vitest.expect(containsPath handoffPath vault).toBe true
                        Vitest.expect((loadedDirectoryWatcher vault).IsSome).toBe true
                    })
            }
        )

        Vitest.test (
            "overlapping establishments close the superseded candidate and retain only the final generation",
            TestOptions(timeout = 15000),
            fun () -> promise {
                do!
                    withLoadedDirectoryFixture (fun vault _ _ _ -> promise {
                        let firstCreated, signalFirstCreated = TestHelpers.deferred ()
                        let firstClosed, signalFirstClosed = TestHelpers.deferred ()
                        let secondCreated, signalSecondCreated = TestHelpers.deferred ()
                        let mutable signalSecondReady = ignore
                        let mutable closeCount = 0

                        interceptNextControlledWatch
                            watchMock
                            signalFirstCreated
                            ignore
                            (fun () ->
                                closeCount <- closeCount + 1
                                signalFirstClosed ()
                            )

                        interceptNextControlledWatch
                            watchMock
                            signalSecondCreated
                            (fun ready -> signalSecondReady <- ready)
                            (fun () -> closeCount <- closeCount + 1)

                        vault.LoadedDirectoryWatcherController.AddLoadedDirectory("dataset", false)
                        let first = vault.RequestLoadedDirectoryWatcherCoverage true
                        do! firstCreated

                        let second = vault.RequestLoadedDirectoryWatcherCoverage true
                        do! firstClosed
                        do! secondCreated
                        signalSecondReady ()

                        let! firstGeneration = first
                        let! secondGeneration = second
                        Vitest.expect(firstGeneration.IsNone).toBe true
                        Vitest.expect(secondGeneration.IsSome).toBe true
                        Vitest.expect(closeCount).toBe 1
                        Vitest.expect((loadedDirectoryWatcher vault).IsSome).toBe true
                    })
            }
        )

        Vitest.test (
            "existing scopes stay active while replacement coverage becomes ready",
            TestOptions(timeout = 15000),
            fun () -> promise {
                do!
                    withLoadedDirectoryFixture (fun vault _ datasetPath _ -> promise {
                        do! vault.RefreshFileTreeDirectory "dataset"
                        let original = (loadedDirectoryWatcher vault).Value
                        let replacementCreated, signalReplacementCreated = TestHelpers.deferred ()
                        let mutable signalReplacementReady = ignore

                        interceptNextControlledWatch
                            watchMock
                            signalReplacementCreated
                            (fun ready -> signalReplacementReady <- ready)
                            ignore

                        let replacement = vault.RefreshFileTreeDirectory "dataset/nested"
                        do! replacementCreated
                        Vitest.expect(isSameWatcher original (loadedDirectoryWatcher vault).Value).toBe true

                        let changedPath = join [| datasetPath; "changed-during-replacement.txt" |]
                        do! writeFileAsync changedPath "changed" TextEncoding.Utf8

                        do!
                            waitUntil
                                "active watcher event during replacement"
                                (fun () -> vault.LoadedDirectoryRefreshes.ContainsKey "dataset")

                        signalReplacementReady ()
                        do! replacement

                        do!
                            waitUntil
                                "existing-scope mutation during replacement"
                                (fun () -> containsPath changedPath vault)

                        Vitest.expect((loadedDirectoryWatcher vault).IsSome).toBe true
                        Vitest.expect(isSameWatcher original (loadedDirectoryWatcher vault).Value).toBe false
                    })
            }
        )

        Vitest.test (
            "replacement error before ready retires the candidate and preserves the active generation",
            TestOptions(timeout = 15000),
            fun () -> promise {
                do!
                    withLoadedDirectoryFixture (fun vault _ _ _ -> promise {
                        do! vault.RefreshFileTreeDirectory "dataset"
                        let original = (loadedDirectoryWatcher vault).Value

                        let activeGeneration =
                            vault.LoadedDirectoryWatcherController.Current.ActiveGeneration

                        let candidateCreated, signalCandidateCreated = TestHelpers.deferred ()
                        let mutable signalCandidateError: exn -> unit = ignore
                        let mutable candidateCloseCount = 0

                        interceptNextFailingWatch
                            watchMock
                            signalCandidateCreated
                            (fun emitError -> signalCandidateError <- emitError)
                            (fun () -> candidateCloseCount <- candidateCloseCount + 1)

                        vault.LoadedDirectoryWatcherController.AddLoadedDirectory("dataset/nested", false)
                        let replacement = vault.RequestLoadedDirectoryWatcherCoverage true
                        do! candidateCreated
                        signalCandidateError (exn "candidate failed before ready")

                        let! replacementGeneration = replacement
                        do! vault.LoadedDirectoryWatcherController.Current.WatcherTransitionTail

                        Vitest.expect(replacementGeneration).toEqual (None)
                        Vitest.expect(candidateCloseCount).toBe (1)

                        Vitest
                            .expect(vault.LoadedDirectoryWatcherController.Current.ActiveGeneration)
                            .toEqual (activeGeneration)

                        Vitest.expect(isSameWatcher original (loadedDirectoryWatcher vault).Value).toBe (true)
                    })
            }
        )

        Vitest.test (
            "suspension cancels a watcher establishment that has not become ready",
            TestOptions(timeout = 15000),
            fun () -> promise {
                do!
                    withLoadedDirectoryFixture (fun vault _ _ _ -> promise {
                        let created, signalCreated = TestHelpers.deferred ()
                        let closed, signalClosed = TestHelpers.deferred ()
                        let operationGate, releaseOperation = TestHelpers.deferred ()
                        let operationEntered, signalOperationEntered = TestHelpers.deferred ()

                        interceptNextControlledWatch watchMock signalCreated ignore signalClosed

                        let refresh = vault.RefreshFileTreeDirectory "dataset"
                        do! created

                        let suspension =
                            vault.WithLoadedDirectoryWatcherSuspended(fun () -> promise {
                                Vitest.expect((loadedDirectoryWatcher vault).IsNone).toBe true
                                signalOperationEntered ()
                                do! operationGate
                            })

                        do! closed
                        do! operationEntered
                        do! refresh
                        Vitest.expect((loadedDirectoryWatcher vault).IsNone).toBe true

                        releaseOperation ()
                        do! suspension

                        Vitest.expect((loadedDirectoryWatcher vault).IsSome).toBe true
                    })
            }
        )

        Vitest.test (
            "first directory load during suspension catches up after restored watcher readiness",
            TestOptions(timeout = 15000),
            fun () -> promise {
                do!
                    withLoadedDirectoryFixture (fun vault _ datasetPath _ -> promise {
                        let handoffPath = join [| datasetPath; "created-while-first-load-suspended.txt" |]
                        interceptNextWatchReady watchMock ignore

                        do!
                            vault.WithLoadedDirectoryWatcherSuspended(fun () -> promise {
                                do! vault.RefreshFileTreeDirectory "dataset"
                                Vitest.expect((loadedDirectoryWatcher vault).IsNone).toBe true
                                do! writeFileAsync handoffPath "handoff" TextEncoding.Utf8
                            })

                        Vitest.expect(containsPath handoffPath vault).toBe true
                        Vitest.expect((loadedDirectoryWatcher vault).IsSome).toBe true
                    })
            }
        )

        Vitest.test (
            "pending watcher handoff merges partial listings and reconciles complete listings",
            TestOptions(timeout = 20000),
            fun () -> promise {
                do!
                    withLoadedDirectoryFixture (fun vault rootPath datasetPath _ -> promise {
                        let! paths = createNumberedFiles datasetPath 150
                        do! vault.RefreshFileTreeDirectory "dataset"
                        let! page = Main.FileTreeCreator.readFileTreeDirectoryPrefix rootPath "dataset" 0 100
                        let cachedPath = findEntryOutsidePage paths page
                        vault.fileTree.[cachedPath] <- FileEntry.create (basename cachedPath, cachedPath, false)

                        do! vault.WithLoadedDirectoryWatcherSuspended(fun () -> JS.Constructors.Promise.resolve ())

                        Vitest.expect(vault.fileTreeDirectoryHasMore.["dataset"]).toBe true
                        Vitest.expect(containsPath cachedPath vault).toBe true

                        do!
                            vault.WithLoadedDirectoryWatcherSuspended(fun () -> promise {
                                for path in paths do
                                    do! rmAsync path (RmOptions())
                            })

                        Vitest.expect(vault.fileTreeDirectoryHasMore.["dataset"]).toBe false
                        Vitest.expect(containsPath cachedPath vault).toBe false
                    })
            }
        )

        Vitest.test (
            "suspension during first-load establishment catches up after replacement readiness",
            TestOptions(timeout = 15000),
            fun () -> promise {
                do!
                    withLoadedDirectoryFixture (fun vault _ datasetPath _ -> promise {
                        let handoffPath = join [| datasetPath; "created-during-cancelled-handoff.txt" |]
                        let created, signalCreated = TestHelpers.deferred ()
                        let operationGate, releaseOperation = TestHelpers.deferred ()
                        let operationEntered, signalOperationEntered = TestHelpers.deferred ()

                        interceptNextControlledWatch watchMock signalCreated ignore ignore
                        interceptNextWatchReady watchMock ignore

                        let refresh = vault.RefreshFileTreeDirectory "dataset"
                        do! created

                        let suspension =
                            vault.WithLoadedDirectoryWatcherSuspended(fun () -> promise {
                                signalOperationEntered ()
                                do! operationGate
                            })

                        do! operationEntered
                        do! writeFileAsync handoffPath "handoff" TextEncoding.Utf8
                        do! refresh
                        Vitest.expect((loadedDirectoryWatcher vault).IsNone).toBe true

                        releaseOperation ()
                        do! suspension

                        Vitest.expect(containsPath handoffPath vault).toBe true
                        Vitest.expect((loadedDirectoryWatcher vault).IsSome).toBe true
                    })
            }
        )

        Vitest.test (
            "suspending for a loaded-directory rename restores only surviving scopes",
            TestOptions(timeout = 15000),
            fun () -> promise {
                do!
                    withLoadedDirectoryFixture (fun vault _ datasetPath nestedPath -> promise {
                        do! vault.RefreshFileTreeDirectory "dataset"
                        do! vault.RefreshFileTreeDirectory "dataset/nested"
                        let renamedPath = join [| datasetPath; "renamed" |]

                        do!
                            vault.WithLoadedDirectoryWatcherSuspended(fun () -> promise {
                                do! renameAsync nestedPath renamedPath
                                do! vault.RefreshFileTreeDirectory "dataset"
                            })

                        Vitest.expect(vault.IsFileTreeDirectoryLoaded "dataset").toBe true
                        Vitest.expect(vault.IsFileTreeDirectoryLoaded "dataset/nested").toBe false
                        Vitest.expect((loadedDirectoryWatcher vault).IsSome).toBe true
                        Vitest.expect(containsPath renamedPath vault).toBe true
                    })
            }
        )

        Vitest.test (
            "suspending for a loaded-directory move preserves unrelated loaded scopes",
            TestOptions(timeout = 15000),
            fun () -> promise {
                do!
                    withLoadedDirectoryFixture (fun vault rootPath _ nestedPath -> promise {
                        do! vault.RefreshFileTreeDirectory "dataset"
                        do! vault.RefreshFileTreeDirectory "dataset/nested"
                        let movedPath = join [| rootPath; "moved-nested" |]

                        do!
                            vault.WithLoadedDirectoryWatcherSuspended(fun () -> promise {
                                do! renameAsync nestedPath movedPath
                                do! vault.RefreshFileTreeDirectory "dataset"
                                do! vault.RefreshFileTreeDirectory ""
                            })

                        Vitest.expect(vault.IsFileTreeDirectoryLoaded "dataset").toBe true
                        Vitest.expect(vault.IsFileTreeDirectoryLoaded "dataset/nested").toBe false
                        Vitest.expect((loadedDirectoryWatcher vault).IsSome).toBe true
                        Vitest.expect(containsPath movedPath vault).toBe true
                    })
            }
        )
)

Vitest.describe (
    "import watcher event classification",
    fun () ->
        Vitest.test (
            "ordinary payload imports do not request an ARC metadata merge",
            fun () -> promise {
                let events =
                    Main.WatcherHelpers.createImportedFileWatcherEvents "C:/arc" "assays/a1/dataset" [|
                        "C:/source/sample.raw"
                    |]

                Vitest.expect(Main.WatcherHelpers.filterArcMergeRelevantEvents events).toEqual ([||])

                Vitest
                    .expect(Main.WatcherHelpers.filterImportedFileWatcherOwnershipEvents "C:/arc" events)
                    .toEqual ([||])

                let mutable mergeInvocations = 0

                let! result =
                    Main.WatcherHelpers.tryTriggerImportedArcMerge
                        (fun _ ->
                            mergeInvocations <- mergeInvocations + 1
                            promise { return Ok() }
                        )
                        events

                Vitest.expect(result).toEqual (Ok())
                Vitest.expect(mergeInvocations).toBe 0
            }
        )

        Vitest.test (
            "canonical metadata imports retain the ARC metadata merge path",
            fun () -> promise {
                let events =
                    Main.WatcherHelpers.createImportedFileWatcherEvents "C:/arc" "assays/a1" [|
                        "C:/source/isa.assay.xlsx"
                    |]

                Vitest.expect(Main.WatcherHelpers.filterArcMergeRelevantEvents events).toHaveLength 1

                Vitest.expect(Main.WatcherHelpers.filterImportedFileWatcherOwnershipEvents "C:/arc" events).toHaveLength
                    1

                let mutable mergedEvents = []

                let! result =
                    Main.WatcherHelpers.tryTriggerImportedArcMerge
                        (fun events ->
                            mergedEvents <- events
                            promise { return Ok() }
                        )
                        events

                Vitest.expect(result).toEqual (Ok())
                Vitest.expect(mergedEvents |> List.length).toBe 1
            }
        )
)
