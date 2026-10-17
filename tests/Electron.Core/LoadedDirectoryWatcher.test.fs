module ElectronCore.LoadedDirectoryWatcherTests

open Fable.Core
open Fable.Core.JsInterop
open Fable.Electron.Main
open Main.ArcVault
open Main.LoadedDirectoryWatcher
open Main.WatcherConfiguration
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
                        let! page = TestHelpers.readFileTreeDirectoryPrefix rootPath "dataset" 100
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
            "loaded watcher removes a deleted file and a deleted directory with its descendants from a partial directory and preserves other cached entries",
            TestOptions(timeout = 20000),
            fun () -> promise {
                do!
                    withLoadedDirectoryFixture (fun vault rootPath datasetPath _ -> promise {
                        let! paths = createNumberedFiles datasetPath 150
                        let deletedDirectoryPath = join [| datasetPath; "deleted-directory" |]
                        let deletedDescendantPath = join [| deletedDirectoryPath; "deep.txt" |]
                        let! _ = mkdirAsync deletedDirectoryPath (MkdirOptions(recursive = true))
                        do! writeFileAsync deletedDescendantPath "deep" TextEncoding.Utf8
                        do! vault.RefreshFileTreeDirectory "dataset"
                        let! page = TestHelpers.readFileTreeDirectoryPrefix rootPath "dataset" 100

                        let deletedPath = paths |> Array.find (fun path -> containsPath path vault)
                        let preservedPath = findEntryOutsidePage paths page

                        vault.fileTree.[preservedPath] <-
                            FileEntry.create (basename preservedPath, preservedPath, false)

                        vault.fileTree.[deletedDirectoryPath] <-
                            FileEntry.create (basename deletedDirectoryPath, deletedDirectoryPath, true)

                        vault.fileTree.[deletedDescendantPath] <-
                            FileEntry.create (basename deletedDescendantPath, deletedDescendantPath, false)

                        do! rmAsync deletedPath (RmOptions())
                        do! rmAsync deletedDirectoryPath (RmOptions(recursive = true, force = true))

                        do!
                            waitUntil
                                "partial-directory deletions"
                                (fun () ->
                                    not (containsPath deletedPath vault)
                                    && not (containsPath deletedDirectoryPath vault)
                                )

                        Vitest.expect(vault.fileTreeDirectoryHasMore.["dataset"]).toBe true
                        Vitest.expect(containsPath deletedDescendantPath vault).toBe false
                        Vitest.expect(containsPath preservedPath vault).toBe true
                    })
            }
        )

        Vitest.test (
            "loaded scopes grow explicitly, remain shallow and keep the existing watchers",
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
                        let datasetWatcher = (directoryWatcher "dataset" vault).Value

                        do! vault.RefreshFileTreeDirectory "dataset/nested"
                        Vitest.expect(vault.IsFileTreeDirectoryLoaded "dataset/nested").toBe true
                        Vitest.expect(containsPath (join [| nestedPath; "deep.txt" |]) vault).toBe true
                        Vitest.expect(coveredDirectories vault).toEqual [| ""; "dataset"; "dataset/nested" |]
                        Vitest.expect(isSameWatcher datasetWatcher (directoryWatcher "dataset" vault).Value).toBe true

                        let changedPath = join [| datasetPath; "changed-after-expansion.txt" |]
                        do! writeFileAsync changedPath "changed" TextEncoding.Utf8
                        do! waitUntil "change seen by the kept watcher" (fun () -> containsPath changedPath vault)
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

                        // A rejected promise and an operation that throws before returning one both fail
                        // the suspension with their own error, and both restore coverage.
                        let failingOperations: (exn * (unit -> JS.Promise<unit>)) list = [
                            let rejected = exn "Expected operation failure."
                            rejected, (fun () -> promise { return raise rejected })
                            let thrown = exn "Expected synchronous operation failure."
                            thrown, (fun () -> raise thrown)
                        ]

                        let mutable previousWatcher = afterSuccess

                        for operationError, operation in failingOperations do
                            let mutable capturedOperationError: exn option = None

                            try
                                do! vault.WithLoadedDirectoryWatcherSuspended operation
                            with error ->
                                capturedOperationError <- Some error

                            Vitest.expect(capturedOperationError.Value).toBe operationError
                            Vitest.expect(coveredDirectories vault).toEqual [| ""; "dataset" |]
                            let restoredWatcher = (directoryWatcher "dataset" vault).Value
                            Vitest.expect(isSameWatcher previousWatcher restoredWatcher).toBe false
                            previousWatcher <- restoredWatcher
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

)

Vitest.describe (
    "watcher configuration",
    fun () ->
        Vitest.test (
            "file watcher polling defaults to Windows only",
            fun () ->
                Vitest.expect(shouldUsePollingByDefault "win32").toBe (true)
                Vitest.expect(shouldUsePollingByDefault "WIN32").toBe (true)
                Vitest.expect(shouldUsePollingByDefault "linux").toBe (false)
                Vitest.expect(shouldUsePollingByDefault "darwin").toBe (false)
        )

        Vitest.test (
            "permanent watcher includes canonical structure and excludes deep payload files",
            fun () ->
                let rootPath = "C:/arc"

                Vitest.expect(isPermanentFileWatcherPathIgnored rootPath $"{rootPath}/studies").toBe (false)

                Vitest.expect(isPermanentFileWatcherPathIgnored rootPath $"{rootPath}/studies/S1").toBe (false)

                Vitest
                    .expect(isPermanentFileWatcherPathIgnored rootPath $"{rootPath}/studies/S1/isa.study.xlsx")
                    .toBe (false)

                Vitest
                    .expect(isPermanentFileWatcherPathIgnored rootPath $"{rootPath}/studies/S1/dataset/file-99999.txt")
                    .toBe (true)
        )

        Vitest.test (
            "permanent watcher observes the root files ARCtrl loads and leaves other root entries to File Explorer",
            fun () ->
                let rootPath = "C:/arc"

                for structuralName in
                    [
                        "isa.investigation.xlsx"
                        "LICENSE"
                        "LICENSE.md"
                        "LICENSE.txt"
                        "assays"
                    ] do
                    Vitest
                        .expect(isPermanentFileWatcherPathIgnored rootPath $"{rootPath}/{structuralName}")
                        .toBe (false)

                for payloadName in [ "README.md"; "notes"; "raw-data.csv" ] do
                    Vitest.expect(isPermanentFileWatcherPathIgnored rootPath $"{rootPath}/{payloadName}").toBe (true)
        )

        Vitest.test (
            "temporary import directory name matches the watcher ignore convention",
            fun () ->
                let directoryName = Main.IPC.FileSystemIO.ArcFileSystemHelper.createTemporaryImportDirectoryName ()
                let exactPattern = """^\.swate-import-[0-9a-f]{32}$"""

                Vitest.expect(System.Text.RegularExpressions.Regex.IsMatch(directoryName, exactPattern)).toBe (true)

                Vitest.expect(isFileWatcherPathIgnored $"C:/arc/{directoryName}/file.txt").toBe (true)
        )

        Vitest.test (
            "ignores LFS backup paths while accepting regular files",
            fun () ->
                let guidSuffix = String.replicate 32 "a"
                let backupPath = $"C:/arc/data.bin.vcs-lfs-backup-{guidSuffix}"

                Vitest.expect(isFileWatcherPathIgnored backupPath).toBe (true)
                Vitest.expect(isFileWatcherPathIgnored "C:/arc/data.bin").toBe (false)
        )
)

/// Waits until no change was reported for 200 ms.
let private waitForQuiet (changes: unit -> int) =
    let rec loop remaining = promise {
        let before = changes ()
        do! Promise.sleep 200

        if changes () = before then
            return ()
        elif remaining = 0 then
            return failwith "Timed out waiting for the watcher to go quiet."
        else
            return! loop (remaining - 1)
    }

    loop 20

/// Stands in for a native watcher, so a test emits events and errors itself and sees whether the watcher
/// was closed.
type private FakeNativeWatcher() =
    let mutable listener: WatchEventType -> string option -> unit = fun _ _ -> ()
    let mutable errorListener: exn -> unit = ignore

    member val Closed = false with get, set
    member _.Listen value = listener <- value
    member _.Emit(eventType, name) = listener eventType name
    member _.Fail error = errorListener error

    interface FSWatcher with
        member this.close() = this.Closed <- true

        member this.on(_, callback) =
            errorListener <- callback
            this :> FSWatcher

/// Starts a directory watcher on a fake native watcher and counts what it reports.
let private watchFake () =
    let native = FakeNativeWatcher()
    let mutable changes = 0
    let losses = ResizeArray<exn>()

    let stop =
        createDirectoryWatcherWith
            (fun _ listener ->
                native.Listen listener
                native :> FSWatcher
            )
            "C:/arc/dataset"
            (fun () -> changes <- changes + 1)
            losses.Add

    native, (fun () -> changes), losses, stop

Vitest.describe (
    "directory watcher",
    fun () ->
        Vitest.test (
            "reports added, removed or renamed children and ignores content changes",
            fun () ->
                let native, changes, losses, _ = watchFake ()

                native.Emit(WatchEventType.Rename, Some "added.csv")
                native.Emit(WatchEventType.Change, Some "added.csv")
                native.Emit(WatchEventType.Rename, Some "renamed.csv")

                Vitest.expect(changes ()).toBe 2
                Vitest.expect(losses.Count).toBe 0
        )

        Vitest.test (
            "reports an event without a child name, which means the system dropped events",
            fun () ->
                let native, changes, _, _ = watchFake ()

                native.Emit(WatchEventType.Change, None)
                native.Emit(WatchEventType.Rename, None)

                Vitest.expect(changes ()).toBe 2
        )

        Vitest.test (
            "closes itself and reports the loss once when an event names the directory itself",
            fun () ->
                let native, changes, losses, _ = watchFake ()

                // Windows reports a removed watched directory as a stream of events with its own path.
                native.Emit(WatchEventType.Rename, Some @"\\?\C:\arc\dataset")
                native.Emit(WatchEventType.Rename, Some @"\\?\C:\arc\dataset")
                native.Emit(WatchEventType.Rename, Some "later.csv")

                Vitest.expect(native.Closed).toBe true
                Vitest.expect(losses.Count).toBe 1
                Vitest.expect(changes ()).toBe 0
        )

        Vitest.test (
            "closes itself and reports the loss once on a watcher error",
            fun () ->
                let native, changes, losses, _ = watchFake ()

                native.Fail(exn "EPERM")
                native.Fail(exn "EPERM")
                native.Emit(WatchEventType.Rename, Some "later.csv")

                Vitest.expect(native.Closed).toBe true
                Vitest.expect(losses.Count).toBe 1
                Vitest.expect(changes ()).toBe 0
        )

        Vitest.test (
            "reports nothing after it was stopped",
            fun () ->
                let native, changes, losses, stop = watchFake ()

                stop ()
                native.Emit(WatchEventType.Rename, Some "later.csv")
                native.Fail(exn "late error")

                Vitest.expect(native.Closed).toBe true
                Vitest.expect(changes ()).toBe 0
                Vitest.expect(losses.Count).toBe 0
        )

        Vitest.test (
            "reports a child added on disk and stays quiet while its content is rewritten",
            TestOptions(timeout = 15000),
            fun () -> promise {
                let! directoryPath = TestHelpers.createTempDirectoryAsync "swate-directory-watcher-"
                let mutable changes = 0
                let losses = ResizeArray<exn>()

                let stop =
                    createDirectoryWatcher directoryPath (fun () -> changes <- changes + 1) losses.Add

                try
                    let filePath = join [| directoryPath; "added.csv" |]
                    do! writeFileAsync filePath "added" TextEncoding.Utf8
                    do! waitUntil "reported addition" (fun () -> changes >= 1)
                    do! waitForQuiet (fun () -> changes)
                    let afterAddition = changes

                    for content in [ "more"; "even more"; "most" ] do
                        do! writeFileAsync filePath content TextEncoding.Utf8

                    do! Promise.sleep 300

                    Vitest.expect(changes).toBe afterAddition
                    Vitest.expect(losses.Count).toBe 0
                    stop ()
                    do! TestHelpers.removeDirectoryAsync directoryPath
                with error ->
                    stop ()
                    do! TestHelpers.removeDirectoryAsync directoryPath
                    return raise error
            }
        )
)

Vitest.describe (
    "exact-name existence check",
    fun () ->
        Vitest.test (
            "accepts only children that exist with exactly the checked name",
            fun () -> promise {
                let! directoryPath = TestHelpers.createTempDirectoryAsync "swate-exact-name-"

                try
                    do! writeFileAsync (join [| directoryPath; "Sample.csv" |]) "sample" TextEncoding.Utf8

                    let existsWithExactName =
                        Main.WatcherHelpers.existsSyncWithExactNameIn directoryPath

                    Vitest.expect(existsWithExactName (join [| directoryPath; "Sample.csv" |])).toBe true
                    // A case-only difference counts as gone, also on case-insensitive file systems.
                    Vitest.expect(existsWithExactName (join [| directoryPath; "sample.csv" |])).toBe false
                    Vitest.expect(existsWithExactName (join [| directoryPath; "missing.csv" |])).toBe false
                    do! TestHelpers.removeDirectoryAsync directoryPath
                with error ->
                    do! TestHelpers.removeDirectoryAsync directoryPath
                    return raise error
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
