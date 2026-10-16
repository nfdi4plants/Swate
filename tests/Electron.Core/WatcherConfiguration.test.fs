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

/// Runs a test body against a temporary directory observed by a poller with a short interval.
/// handleChange decides, per reported change, whether the poller treats it as handled.
let private withPolledDirectory (handleChange: int -> bool) testBody = promise {
    let! directoryPath = TestHelpers.createTempDirectoryAsync "swate-directory-poller-"
    let mutable changes = 0
    let errors = ResizeArray<exn>()

    let stop, ready =
        createDirectoryModificationPoller
            directoryPath
            20
            (fun () ->
                changes <- changes + 1
                handleChange changes
            )
            errors.Add

    try
        let! baseline = ready
        Vitest.expect(baseline).toEqual (Ok())
        do! testBody directoryPath (fun () -> changes) errors
    finally
        stop () |> ignore

    do! TestHelpers.removeDirectoryAsync directoryPath
}

Vitest.describe (
    "directory modification poller",
    fun () ->
        Vitest.test (
            "reports an added child once and then observes the new state",
            TestOptions(timeout = 10000),
            fun () ->
                withPolledDirectory
                    (fun _ -> true)
                    (fun directoryPath changes errors -> promise {
                        do! Promise.sleep 50
                        do! writeFileAsync (join [| directoryPath; "added.txt" |]) "added" TextEncoding.Utf8
                        do! waitUntil "reported addition" (fun () -> changes () = 1)
                        do! Promise.sleep 200

                        Vitest.expect(changes ()).toBe 1
                        Vitest.expect(errors.Count).toBe 0
                    })
        )

        Vitest.test (
            "reports an unhandled change again until it is handled",
            TestOptions(timeout = 10000),
            fun () ->
                withPolledDirectory
                    (fun change -> change >= 3)
                    (fun directoryPath changes _ -> promise {
                        do! Promise.sleep 50
                        do! writeFileAsync (join [| directoryPath; "added.txt" |]) "added" TextEncoding.Utf8
                        do! waitUntil "handled change" (fun () -> changes () = 3)
                        do! Promise.sleep 200

                        Vitest.expect(changes ()).toBe 3
                    })
        )

        Vitest.test (
            "reports a vanished directory once without errors and reports its return",
            TestOptions(timeout = 10000),
            fun () ->
                withPolledDirectory
                    (fun _ -> true)
                    (fun directoryPath changes errors -> promise {
                        do! TestHelpers.removeDirectoryAsync directoryPath
                        do! waitUntil "reported removal" (fun () -> changes () = 1)
                        do! Promise.sleep 200

                        Vitest.expect(changes ()).toBe 1
                        Vitest.expect(errors.Count).toBe 0

                        let! _ = Main.Bindings.Filesystem.mkdirAsync directoryPath (MkdirOptions(recursive = true))
                        do! waitUntil "reported return" (fun () -> changes () = 2)
                    })
        )

        Vitest.test (
            "reports nothing after it was stopped",
            TestOptions(timeout = 10000),
            fun () -> promise {
                let! directoryPath = TestHelpers.createTempDirectoryAsync "swate-directory-poller-stopped-"
                let mutable changes = 0

                let stop, ready =
                    createDirectoryModificationPoller
                        directoryPath
                        20
                        (fun () ->
                            changes <- changes + 1
                            true
                        )
                        ignore

                let! _ = ready
                do! stop ()
                do! writeFileAsync (join [| directoryPath; "added.txt" |]) "added" TextEncoding.Utf8
                do! Promise.sleep 200
                do! TestHelpers.removeDirectoryAsync directoryPath

                Vitest.expect(changes).toBe 0
            }
        )
)
