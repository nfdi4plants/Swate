module ElectronCore.WatcherConfigurationTests

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
