module ElectronCore.LoadedDirectoryWatcherTests

open Fable.Core
open Fable.Core.JsInterop
open Fable.Electron.Main
open Main.ArcVault
open Main.LoadedDirectoryWatcher
open Main.Bindings.Filesystem
open Main.Bindings.Path
open Swate.Components.Shared
open Swate.Electron.Shared.FileIOTypes
open Vitest

/// Wraps the next start of the watcher of one directory; every other start runs unchanged.
let private wrapNextDirectoryWatcher (vault: ArcVault) (directoryPath: string) wrap =
    let start = vault.StartDirectoryWatcher

    vault.StartDirectoryWatcher <-
        fun path onChange onLost ->
            if PathHelpers.pathsEqual path directoryPath then
                vault.StartDirectoryWatcher <- start
                wrap start path onChange onLost
            else
                start path onChange onLost

/// Runs an action right before the next watcher of the directory starts.
let private beforeNextDirectoryWatcher vault directoryPath (action: unit -> unit) =
    wrapNextDirectoryWatcher
        vault
        directoryPath
        (fun start path onChange onLost ->
            action ()
            start path onChange onLost
        )

/// Makes the next watcher of the directory fail to start.
let private failNextDirectoryWatcher vault directoryPath (error: exn) =
    wrapNextDirectoryWatcher vault directoryPath (fun _ _ _ _ -> raise error)

/// Starts the next watcher of the directory normally and returns a function that ends it the way a
/// removed directory or a watcher error does: the watcher stops and reports its loss.
let private loseNextDirectoryWatcher vault directoryPath =
    let mutable lose: exn -> unit = ignore

    wrapNextDirectoryWatcher
        vault
        directoryPath
        (fun start path onChange onLost ->
            let stop = start path onChange onLost

            lose <-
                fun reason ->
                    stop ()
                    onLost reason

            stop
        )

    fun reason -> lose reason

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

let private directoryWatcher (directory: string) (vault: ArcVault) =
    vault.LoadedDirectoryWatcherController.Current.Watchers.TryFind directory

let private coveredDirectories (vault: ArcVault) =
    vault.LoadedDirectoryWatcherController.Current.Watchers
    |> Map.toArray
    |> Array.map fst

let private hasNoDirectoryWatchers (vault: ArcVault) =
    vault.LoadedDirectoryWatcherController.Current.Watchers.IsEmpty

let private retireDirectoryWatchers (vault: ArcVault) =
    for observer in vault.LoadedDirectoryWatcherController.RetireWatchers() do
        observer.Close()

let private isSameWatcher (left: DirectoryObserver) (right: DirectoryObserver) = obj.ReferenceEquals(left, right)

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

        try
            do! vault.EnsureInitialFileTreeRootPage rootPath |> Promise.map ignore
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
            "refreshing the loaded tree keeps loaded descendants, loaded pages and directory watchers",
            TestOptions(timeout = 15000),
            fun () -> promise {
                do!
                    withLoadedDirectoryFixture (fun vault _ datasetPath nestedPath -> promise {
                        let! _ = createNumberedFiles datasetPath 250
                        do! vault.LoadNextFileTreeDirectoryPage "dataset"
                        do! vault.LoadNextFileTreeDirectoryPage "dataset"
                        do! vault.RefreshFileTreeDirectory "dataset/nested"
                        let watcher = (directoryWatcher "dataset" vault).Value
                        let loadedPaths = vault.fileTree.Keys |> Set.ofSeq

                        let directChildCount () =
                            vault.fileTree.Values
                            |> Seq.filter (fun entry -> PathHelpers.pathsEqual (dirname entry.path) datasetPath)
                            |> Seq.length

                        do! vault.RefreshLoadedFileTree()

                        Vitest.expect(vault.IsFileTreeDirectoryLoaded "dataset").toBe true
                        Vitest.expect(vault.IsFileTreeDirectoryLoaded "dataset/nested").toBe true
                        Vitest.expect(isSameWatcher watcher (directoryWatcher "dataset" vault).Value).toBe true
                        Vitest.expect(vault.fileTreeDirectoryHasMore.["dataset"]).toBe true
                        Vitest.expect(Set.isSubset loadedPaths (vault.fileTree.Keys |> Set.ofSeq)).toBe true
                        Vitest.expect(containsPath (join [| nestedPath; "deep.txt" |]) vault).toBe true
                        // The refresh keeps both loaded pages instead of shrinking the directory to one page.
                        Vitest.expect(directChildCount ()).toBeGreaterThanOrEqual 200

                        do! vault.LoadNextFileTreeDirectoryPage "dataset"

                        Vitest.expect(directChildCount ()).toBe 252
                        Vitest.expect(vault.fileTreeDirectoryHasMore.["dataset"]).toBe false
                    })
            }
        )

        Vitest.test (
            "moving a loaded directory away forgets it, keeps its parent, and loads it from scratch when it returns",
            TestOptions(timeout = 15000),
            fun () -> promise {
                do!
                    withLoadedDirectoryFixture (fun vault rootPath _ nestedPath -> promise {
                        do! vault.RefreshFileTreeDirectory "dataset"
                        do! vault.LoadNextFileTreeDirectoryPage "dataset/nested"
                        Vitest.expect(vault.fileTreeDirectoryHasMore.["dataset/nested"]).toBe false

                        let movedPath = join [| rootPath; "moved-nested" |]

                        do!
                            vault.WithLoadedDirectoryWatcherSuspended(fun () -> promise {
                                do! renameAsync nestedPath movedPath
                                do! vault.RefreshFileTreeDirectory "dataset"
                                do! vault.RefreshFileTreeDirectory ""
                            })

                        Vitest.expect(vault.fileTreeDirectoryHasMore.ContainsKey "dataset/nested").toBe false
                        Vitest.expect(vault.IsFileTreeDirectoryLoaded "dataset/nested").toBe false
                        Vitest.expect(vault.IsFileTreeDirectoryLoaded "dataset").toBe true
                        Vitest.expect(coveredDirectories vault).toEqual [| ""; "dataset" |]
                        Vitest.expect(containsPath movedPath vault).toBe true

                        do!
                            vault.WithLoadedDirectoryWatcherSuspended(fun () -> promise {
                                do! renameAsync movedPath nestedPath
                                do! vault.RefreshFileTreeDirectory "dataset"
                            })

                        do! vault.LoadNextFileTreeDirectoryPage "dataset/nested"
                        Vitest.expect(containsPath (join [| nestedPath; "deep.txt" |]) vault).toBe true
                    })
            }
        )

        Vitest.test (
            "partial refresh preserves a cached child directory and its descendants absent from the returned page",
            TestOptions(timeout = 15000),
            fun () -> promise {
                do!
                    withLoadedDirectoryFixture (fun vault rootPath datasetPath _ -> promise {
                        let! paths = createNumberedFiles datasetPath 150
                        let! page = Main.FileTreeCreator.readFileTreeDirectoryPrefix rootPath "dataset" 100
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
                        let! page = Main.FileTreeCreator.readFileTreeDirectoryPrefix rootPath "dataset" 100

                        let deletedPath = paths |> Array.find (fun path -> containsPath path vault)

                        let preservedPath = findEntryOutsidePage paths page

                        vault.fileTree.[preservedPath] <-
                            FileEntry.create (basename preservedPath, preservedPath, false)

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
            "loaded scopes grow explicitly and remain shallow",
            TestOptions(timeout = 15000),
            fun () -> promise {
                do!
                    withLoadedDirectoryFixture (fun vault _ datasetPath nestedPath -> promise {
                        Vitest.expect(vault.IsFileTreeDirectoryLoaded "dataset").toBe false

                        do! vault.RefreshFileTreeDirectory "dataset"

                        Vitest.expect(vault.IsFileTreeDirectoryLoaded "dataset").toBe true
                        Vitest.expect(vault.IsFileTreeDirectoryLoaded "dataset/nested").toBe false
                        Vitest.expect(coveredDirectories vault).toEqual [| ""; "dataset" |]
                        Vitest.expect(containsPath (join [| datasetPath; "existing.txt" |]) vault).toBe true
                        Vitest.expect(containsPath (join [| nestedPath; "deep.txt" |]) vault).toBe false

                        // Collapse is renderer-local, so no main-process action removes the loaded scope.
                        Vitest.expect(vault.IsFileTreeDirectoryLoaded "dataset").toBe true

                        do! vault.RefreshFileTreeDirectory "dataset/nested"
                        Vitest.expect(vault.IsFileTreeDirectoryLoaded "dataset/nested").toBe true
                        Vitest.expect(containsPath (join [| nestedPath; "deep.txt" |]) vault).toBe true
                        Vitest.expect(coveredDirectories vault).toEqual [| ""; "dataset"; "dataset/nested" |]
                    })
            }
        )

        Vitest.test (
            "polled root and loaded directories reconcile additions, deletions and removed loaded children",
            TestOptions(timeout = 20000),
            fun () -> promise {
                do!
                    withLoadedDirectoryFixture (fun vault rootPath datasetPath nestedPath -> promise {
                        do! vault.RefreshFileTreeDirectory "dataset"
                        Vitest.expect(coveredDirectories vault).toEqual [| ""; "dataset" |]

                        let rootFilePath = join [| rootPath; "external-root.txt" |]
                        do! writeFileAsync rootFilePath "root" TextEncoding.Utf8
                        do! waitUntil "root addition" (fun () -> containsPath rootFilePath vault)
                        do! rmAsync rootFilePath (RmOptions())
                        do! waitUntil "root deletion" (fun () -> not (containsPath rootFilePath vault))

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
                                "deleted loaded directory forgotten and removed from the tree"
                                (fun () ->
                                    not (vault.IsFileTreeDirectoryLoaded "dataset/nested")
                                    && not (containsPath nestedPath vault)
                                )
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
                        Vitest.expect(vault.IsFileTreeDirectoryLoaded "").toBe true
                        Vitest.expect(vault.loadedFileTreeDirectories.Contains "").toBe false
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
            "refreshing the loaded tree after a version control change keeps surviving scopes and forgets removed ones",
            TestOptions(timeout = 15000),
            fun () -> promise {
                do!
                    withLoadedDirectoryFixture (fun vault _ datasetPath nestedPath -> promise {
                        do! vault.RefreshFileTreeDirectory "dataset"
                        do! vault.RefreshFileTreeDirectory "dataset/nested"
                        let checkedOutPath = join [| datasetPath; "checked-out.txt" |]

                        do!
                            vault.WithLoadedDirectoryWatcherSuspended(fun () -> promise {
                                do! rmAsync nestedPath (RmOptions(recursive = true, force = true))
                                do! writeFileAsync checkedOutPath "checked out" TextEncoding.Utf8
                                do! vault.RefreshLoadedFileTree()
                            })

                        Vitest.expect(vault.IsFileTreeDirectoryLoaded "dataset").toBe true
                        Vitest.expect(vault.IsFileTreeDirectoryLoaded "dataset/nested").toBe false
                        Vitest.expect(containsPath (join [| datasetPath; "existing.txt" |]) vault).toBe true
                        Vitest.expect(containsPath checkedOutPath vault).toBe true
                        Vitest.expect(containsPath nestedPath vault).toBe false
                        Vitest.expect((directoryWatcher "dataset" vault).IsSome).toBe true
                        Vitest.expect((directoryWatcher "dataset/nested" vault).IsNone).toBe true
                    })
            }
        )

        Vitest.test (
            "refreshing a vanished loaded directory forgets it instead of failing",
            TestOptions(timeout = 15000),
            fun () -> promise {
                do!
                    withLoadedDirectoryFixture (fun vault _ _ nestedPath -> promise {
                        do! vault.RefreshFileTreeDirectory "dataset"
                        do! vault.RefreshFileTreeDirectory "dataset/nested"

                        do!
                            vault.WithLoadedDirectoryWatcherSuspended(fun () -> promise {
                                do! rmAsync nestedPath (RmOptions(recursive = true, force = true))
                                do! vault.RefreshFileTreeDirectory "dataset/nested"
                            })

                        Vitest.expect(vault.IsFileTreeDirectoryLoaded "dataset/nested").toBe false
                        Vitest.expect(vault.fileTreeDirectoryHasMore.ContainsKey "dataset/nested").toBe false
                        Vitest.expect(containsPath nestedPath vault).toBe false
                        Vitest.expect(containsPath (join [| nestedPath; "deep.txt" |]) vault).toBe false
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
                        (fun vault _ datasetPath _ -> promise {
                            do! vault.RefreshFileTreeDirectory "dataset"
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
                    withLoadedDirectoryFixture (fun vault _ datasetPath nestedPath -> promise {
                        do! vault.RefreshFileTreeDirectory "dataset"
                        do! vault.RefreshFileTreeDirectory "dataset/nested"

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
                            retireDirectoryWatchers vault
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
                            retireDirectoryWatchers vault

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
            "watcher suspension restores coverage and preserves operation outcomes",
            TestOptions(timeout = 15000),
            fun () -> promise {
                do!
                    withLoadedDirectoryFixture (fun vault _ _ _ -> promise {
                        do! vault.RefreshFileTreeDirectory "dataset"
                        let beforeSuccess = (directoryWatcher "dataset" vault).Value

                        let! result = vault.WithLoadedDirectoryWatcherSuspended(fun () -> promise { return 42 })
                        Vitest.expect(result).toBe 42
                        Vitest.expect(coveredDirectories vault).toEqual [| ""; "dataset" |]
                        let afterSuccess = (directoryWatcher "dataset" vault).Value
                        Vitest.expect(isSameWatcher beforeSuccess afterSuccess).toBe false

                        let operationError = exn "Expected operation failure."
                        let mutable capturedOperationError: exn option = None

                        try
                            do!
                                vault.WithLoadedDirectoryWatcherSuspended(fun () -> promise {
                                    return raise operationError
                                })
                        with error ->
                            capturedOperationError <- Some error

                        Vitest.expect(capturedOperationError.Value).toBe operationError
                        Vitest.expect(coveredDirectories vault).toEqual [| ""; "dataset" |]

                        Vitest.expect(isSameWatcher afterSuccess (directoryWatcher "dataset" vault).Value).toBe false
                    })
            }
        )

        Vitest.test (
            "watcher suspension restores coverage after a synchronous operation failure",
            TestOptions(timeout = 15000),
            fun () -> promise {
                do!
                    withLoadedDirectoryFixture (fun vault _ _ _ -> promise {
                        do! vault.RefreshFileTreeDirectory "dataset"
                        let before = (directoryWatcher "dataset" vault).Value
                        let operationError = exn "Expected synchronous operation failure."
                        let mutable capturedOperationError: exn option = None

                        try
                            do! vault.WithLoadedDirectoryWatcherSuspended(fun () -> raise operationError)
                        with error ->
                            capturedOperationError <- Some error

                        Vitest.expect(capturedOperationError.Value).toBe operationError
                        Vitest.expect(coveredDirectories vault).toEqual [| ""; "dataset" |]
                        Vitest.expect(isSameWatcher before (directoryWatcher "dataset" vault).Value).toBe false
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
                                Vitest.expect(hasNoDirectoryWatchers vault).toBe true
                                do! writeFileAsync addedPath "suspended" TextEncoding.Utf8
                                Vitest.expect(containsPath addedPath vault).toBe false
                            })

                        Vitest.expect(containsPath addedPath vault).toBe true
                        Vitest.expect((directoryWatcher "dataset" vault).IsSome).toBe true
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
                                Vitest.expect(hasNoDirectoryWatchers vault).toBe true
                                do! writeFileAsync addedPath "queued" TextEncoding.Utf8
                                do! firstOperationGate
                            })

                        let second =
                            vault.WithLoadedDirectoryWatcherSuspended(fun () -> promise {
                                enteredOperations.Add 2
                                Vitest.expect(hasNoDirectoryWatchers vault).toBe true
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

                        Vitest.expect((directoryWatcher "dataset" vault).IsSome).toBe true
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
                    withLoadedDirectoryFixture (fun vault _ _ _ -> promise {
                        do! vault.RefreshFileTreeDirectory "dataset"

                        let suspension =
                            vault.WithLoadedDirectoryWatcherSuspended(fun () -> promise {
                                Vitest.expect(hasNoDirectoryWatchers vault).toBe true
                                do! vault.RefreshFileTreeDirectory "dataset"
                                Vitest.expect(hasNoDirectoryWatchers vault).toBe true
                            })

                        do! suspension
                        Vitest.expect(coveredDirectories vault).toEqual [| ""; "dataset" |]
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

                        let stopping = vault.StopFileWatcher()
                        releaseQueue ()
                        do! stopping
                        do! refresh

                        Vitest.expect(containsPath stalePath vault).toBe false
                        Vitest.expect(vault.loadedFileTreeDirectories.Count).toBe 0
                        Vitest.expect(hasNoDirectoryWatchers vault).toBe true
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

                        Vitest.expect(vault.LoadedDirectoryWatcherController.Current.Refreshes.IsEmpty).toBe true
                        Vitest.expect(vault.loadedFileTreeDirectories.Count).toBe 0

                        releasePermanentClose ()
                        do! stopping
                        do! Promise.sleep 25

                        Vitest.expect(hasNoDirectoryWatchers vault).toBe true
                        Vitest.expect(vault.loadedFileTreeDirectories.Count).toBe 0
                        Vitest.expect(vault.LoadedDirectoryWatcherController.Current.PendingHandoffs.Count).toBe 0
                        Vitest.expect(vault.LoadedDirectoryWatcherController.Current.Refreshes.IsEmpty).toBe true
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

                        Vitest.expect((directoryWatcher "dataset" vault).IsSome).toBe true

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
            "first directory load catches changes made while watcher coverage is established",
            TestOptions(timeout = 15000),
            fun () -> promise {
                do!
                    withLoadedDirectoryFixture (fun vault _ datasetPath _ -> promise {
                        let handoffPath = join [| datasetPath; "created-during-watch-handoff.txt" |]

                        // The file appears after the listing and before the watcher, so only the catch-up
                        // read can find it.
                        beforeNextDirectoryWatcher
                            vault
                            datasetPath
                            (fun () -> writeFileSync handoffPath "handoff" TextEncoding.Utf8)

                        do! vault.RefreshFileTreeDirectory "dataset"

                        Vitest.expect(containsPath handoffPath vault).toBe true
                        Vitest.expect((directoryWatcher "dataset" vault).IsSome).toBe true
                    })
            }
        )

        Vitest.test (
            "a watcher that ends is retired with its directory, and loading the directory again watches it",
            TestOptions(timeout = 15000),
            fun () -> promise {
                do!
                    withLoadedDirectoryFixture (fun vault _ datasetPath _ -> promise {
                        let endWatcher = loseNextDirectoryWatcher vault datasetPath
                        do! vault.RefreshFileTreeDirectory "dataset"

                        endWatcher (exn "watcher error")

                        Vitest.expect((directoryWatcher "dataset" vault).IsNone).toBe true
                        Vitest.expect(vault.IsFileTreeDirectoryLoaded "dataset").toBe false

                        do! vault.RefreshFileTreeDirectory "dataset"
                        Vitest.expect(coveredDirectories vault).toEqual [| ""; "dataset" |]

                        let laterPath = join [| datasetPath; "after-reload.txt" |]
                        do! writeFileAsync laterPath "later" TextEncoding.Utf8
                        do! waitUntil "change seen by the new watcher" (fun () -> containsPath laterPath vault)
                    })
            }
        )

        Vitest.test (
            "a loaded directory deleted and created again on disk gets a new watcher",
            TestOptions(timeout = 15000),
            fun () -> promise {
                do!
                    withLoadedDirectoryFixture (fun vault _ _ nestedPath -> promise {
                        do! vault.RefreshFileTreeDirectory "dataset"
                        do! vault.RefreshFileTreeDirectory "dataset/nested"
                        let original = (directoryWatcher "dataset/nested" vault).Value

                        do! rmAsync nestedPath (RmOptions(recursive = true, force = true))

                        do!
                            waitUntil
                                "removed directory forgotten with its watcher"
                                (fun () ->
                                    not (vault.IsFileTreeDirectoryLoaded "dataset/nested")
                                    && (directoryWatcher "dataset/nested" vault).IsNone
                                )

                        let! _ = mkdirAsync nestedPath (MkdirOptions(recursive = true))
                        do! vault.RefreshFileTreeDirectory "dataset/nested"
                        let replacement = (directoryWatcher "dataset/nested" vault).Value
                        Vitest.expect(isSameWatcher original replacement).toBe false

                        let laterPath = join [| nestedPath; "after-recreation.txt" |]
                        do! writeFileAsync laterPath "later" TextEncoding.Utf8
                        do! waitUntil "change in the re-created directory" (fun () -> containsPath laterPath vault)
                    })
            }
        )

        Vitest.test (
            "covering a new directory keeps the existing watchers",
            TestOptions(timeout = 15000),
            fun () -> promise {
                do!
                    withLoadedDirectoryFixture (fun vault _ datasetPath _ -> promise {
                        do! vault.RefreshFileTreeDirectory "dataset"
                        let original = (directoryWatcher "dataset" vault).Value

                        do! vault.RefreshFileTreeDirectory "dataset/nested"

                        Vitest.expect((directoryWatcher "dataset/nested" vault).IsSome).toBe true
                        Vitest.expect(isSameWatcher original (directoryWatcher "dataset" vault).Value).toBe true

                        let changedPath = join [| datasetPath; "changed-after-expansion.txt" |]
                        do! writeFileAsync changedPath "changed" TextEncoding.Utf8
                        do! waitUntil "change seen by the kept watcher" (fun () -> containsPath changedPath vault)
                    })
            }
        )

        Vitest.test (
            "a directory whose watcher cannot start stays uncovered and keeps its handoff for the next coverage",
            TestOptions(timeout = 15000),
            fun () -> promise {
                do!
                    withLoadedDirectoryFixture (fun vault _ _ nestedPath -> promise {
                        do! vault.RefreshFileTreeDirectory "dataset"
                        let original = (directoryWatcher "dataset" vault).Value
                        failNextDirectoryWatcher vault nestedPath (exn "watcher failed to start")

                        vault.LoadedDirectoryWatcherController.AddLoadedDirectory("dataset/nested", true)
                        do! vault.EnsureLoadedDirectoryWatcherCoverage()

                        Vitest.expect((directoryWatcher "dataset/nested" vault).IsNone).toBe (true)
                        Vitest.expect(isSameWatcher original (directoryWatcher "dataset" vault).Value).toBe (true)

                        // The uncovered directory keeps its pending handoff, so the next coverage catches it up.
                        Vitest
                            .expect(
                                vault.LoadedDirectoryWatcherController.Current.PendingHandoffs.Contains
                                    "dataset/nested"
                            )
                            .toBe (true)

                        do! vault.EnsureLoadedDirectoryWatcherCoverage()
                        Vitest.expect((directoryWatcher "dataset/nested" vault).IsSome).toBe (true)

                        Vitest
                            .expect(
                                vault.LoadedDirectoryWatcherController.Current.PendingHandoffs.Contains
                                    "dataset/nested"
                            )
                            .toBe (false)
                    })
            }
        )

        Vitest.test (
            "first directory load during suspension catches up once the watchers are restored",
            TestOptions(timeout = 15000),
            fun () -> promise {
                do!
                    withLoadedDirectoryFixture (fun vault _ datasetPath _ -> promise {
                        let handoffPath = join [| datasetPath; "created-while-first-load-suspended.txt" |]

                        do!
                            vault.WithLoadedDirectoryWatcherSuspended(fun () -> promise {
                                do! vault.RefreshFileTreeDirectory "dataset"
                                Vitest.expect(hasNoDirectoryWatchers vault).toBe true
                                do! writeFileAsync handoffPath "handoff" TextEncoding.Utf8
                            })

                        Vitest.expect(containsPath handoffPath vault).toBe true
                        Vitest.expect((directoryWatcher "dataset" vault).IsSome).toBe true
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
                        let! page = Main.FileTreeCreator.readFileTreeDirectoryPrefix rootPath "dataset" 100
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
