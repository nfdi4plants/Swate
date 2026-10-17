module ElectronCore.WatcherConfigurationTests

open Main.Bindings.Filesystem
open Main.Bindings.Path
open Main.IPC.FileSystemIO
open Main.WatcherConfiguration
open Vitest

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
                let directoryName = ArcFileSystemHelper.createTemporaryImportDirectoryName ()
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
