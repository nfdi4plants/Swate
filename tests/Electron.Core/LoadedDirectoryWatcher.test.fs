module ElectronCore.LoadedDirectoryWatcherTests

open Fable.Core
open Fable.Core.JsInterop
open Fable.Electron.Main
open Main.ArcVault
open Main.Bindings.Filesystem
open Main.Bindings.Path
open Swate.Components.Shared
open Vitest

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
        let! initialTree = Main.FileTreeCreator.getFileTree rootPath
        vault.fileTree <- initialTree

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

Vitest.describe (
    "loaded FileTree directory watching",
    fun () ->
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
                        Vitest.expect(vault.loadedDirectoryWatcher.IsSome).toBe true
                        Vitest.expect(containsPath (join [| datasetPath; "existing.txt" |]) vault).toBe true
                        Vitest.expect(containsPath (join [| nestedPath; "deep.txt" |]) vault).toBe false

                        do!
                            waitUntil
                                "dataset watcher readiness"
                                (fun () ->
                                    isWatchedPath rootPath "dataset/existing.txt" vault.loadedDirectoryWatcher.Value
                                )

                        Vitest
                            .expect(
                                isWatchedPath rootPath "dataset/nested/deep.txt" vault.loadedDirectoryWatcher.Value
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
                                        vault.loadedDirectoryWatcher.Value
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

                        let unloadedDeepPath = join [| nestedPath; "unloaded.txt" |]
                        do! writeFileAsync unloadedDeepPath "unloaded" TextEncoding.Utf8
                        do! Promise.sleep 300
                        Vitest.expect(containsPath unloadedDeepPath vault).toBe false

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
                        Vitest.expect(vault.loadedDirectoryWatcher.IsNone).toBe true
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
                            Vitest.expect(vault.loadedDirectoryWatcher.IsNone).toBe true
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
                                            vault.loadedDirectoryWatcher.Value
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
                                        vault.loadedDirectoryWatcher.Value
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
                            do! vault.loadedDirectoryWatcher.Value.close ()
                            vault.loadedDirectoryWatcher <- None
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
                            do! vault.loadedDirectoryWatcher.Value.close ()
                            vault.loadedDirectoryWatcher <- None

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
                        Vitest.expect(vault.loadedDirectoryWatcher.IsNone).toBe true
                        Vitest.expect(containsPath (join [| datasetPath; "existing.txt" |]) vault).toBe false
                    })
            }
        )

        Vitest.test (
            "concurrent mutation lifecycles are serialized around watcher rebuilds",
            TestOptions(timeout = 15000),
            fun () -> promise {
                do!
                    withLoadedDirectoryFixture (fun vault _ _ _ -> promise {
                        do! vault.RefreshFileTreeDirectory "dataset"

                        let firstOperationGate, releaseFirstOperation = TestHelpers.deferred ()
                        let enteredOperations = ResizeArray<int>()

                        let first =
                            vault.WithLoadedDirectoryWatcherSuspended(fun () -> promise {
                                enteredOperations.Add 1
                                Vitest.expect(vault.loadedDirectoryWatcher.IsNone).toBe true
                                do! firstOperationGate
                            })

                        let second =
                            vault.WithLoadedDirectoryWatcherSuspended(fun () -> promise {
                                enteredOperations.Add 2
                                Vitest.expect(vault.loadedDirectoryWatcher.IsNone).toBe true
                            })

                        do! waitUntil "first mutation lifecycle" (fun () -> enteredOperations.Count = 1)
                        do! Promise.sleep 50
                        Vitest.expect(enteredOperations.Count).toBe 1
                        Vitest.expect(enteredOperations.[0]).toBe 1

                        releaseFirstOperation ()
                        do! first
                        do! second

                        Vitest.expect(enteredOperations.Count).toBe 2
                        Vitest.expect(enteredOperations.[0]).toBe 1
                        Vitest.expect(enteredOperations.[1]).toBe 2
                        Vitest.expect(vault.loadedDirectoryWatcher.IsSome).toBe true
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
                        Vitest.expect(vault.loadedDirectoryWatcher.IsSome).toBe true
                        Vitest.expect(containsPath renamedPath vault).toBe true
                    })
            }
        )

        Vitest.test (
            "suspending for a loaded-directory delete does not restore the deleted scope",
            TestOptions(timeout = 15000),
            fun () -> promise {
                do!
                    withLoadedDirectoryFixture (fun vault _ _ nestedPath -> promise {
                        do! vault.RefreshFileTreeDirectory "dataset"
                        do! vault.RefreshFileTreeDirectory "dataset/nested"

                        do!
                            vault.WithLoadedDirectoryWatcherSuspended(fun () -> promise {
                                do! rmAsync nestedPath (RmOptions(recursive = true, force = true))
                                do! vault.RefreshFileTreeDirectory "dataset"
                            })

                        Vitest.expect(vault.IsFileTreeDirectoryLoaded "dataset").toBe true
                        Vitest.expect(vault.IsFileTreeDirectoryLoaded "dataset/nested").toBe false
                        Vitest.expect(vault.loadedDirectoryWatcher.IsSome).toBe true
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
                        Vitest.expect(vault.loadedDirectoryWatcher.IsSome).toBe true
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
