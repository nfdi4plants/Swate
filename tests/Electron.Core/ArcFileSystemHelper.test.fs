module ElectronCore.ArcFileSystemHelperTests

open System
open Fable.Core
open Fable.Core.JsInterop
open Main.Bindings.Path
open Main.IPC.FileSystemIO
open Main.ArcVaultTypes
open Swate.Components.Shared
open Swate.Electron.Shared.FileIOTypes
open Swate.Electron.Shared.RenamePathRules
open Vitest

let private fsPromisesDynamic: obj = importAll "fs/promises"
let private newGuidN () = Guid.NewGuid().ToString("N")

[<Emit("Object.assign(new Error($0), { code: $1 })")>]
let private nodeError (message: string) (code: string) : exn = jsNative

[<Emit("process.platform")>]
let private nodePlatform () : string = jsNative

let private withAssayArc =
    TestHelpers.withTempArcWith
        "swate-generic-fs-"
        "GenericFileSystemArc"
        (fun arc -> arc.InitAssay("AssayA") |> ignore)

let private renameRequest relativePath newName = {
    relativePath = relativePath
    newName = newName
}

let private moveRequest sourcePath targetPath overwrite = {
    sourceRelativePath = sourcePath
    targetRelativePath = targetPath
    overwrite = overwrite
}

let private createItemRequest parentPath name kind = {
    parentPath = parentPath
    name = name
    kind = kind
}

let private absoluteArcPath arcPath relativePath =
    relativePath
    |> PathHelpers.normalizePath
    |> fun path -> path.Split([| '/' |], StringSplitOptions.RemoveEmptyEntries)
    |> Array.append [| arcPath |]
    |> join

let private renameItemOrFail arcPath request = promise {
    match! ArcFileSystemHelper.renameGenericFileSystemItemOnDisk arcPath request with
    | Error error -> return failwith error.Message
    | Ok() -> return ()
}

let private createItemOrFail arcPath request = promise {
    match! ArcFileSystemHelper.createFileSystemItemOnDisk arcPath request with
    | Error error -> return failwith error.Message
    | Ok createdPath -> return createdPath
}

let private moveItemOrFail arcPath request = promise {
    match! ArcFileSystemHelper.moveGenericFileSystemItemOnDisk arcPath request with
    | Error error -> return failwith error.Message
    | Ok() -> return ()
}

let private deleteItemOrFail arcPath relativePath = promise {
    match! ArcFileSystemHelper.deleteGenericFileSystemItemOnDisk arcPath relativePath with
    | Error error -> return failwith error.Message
    | Ok() -> return ()
}

let private expectRelativePathExists arcPath relativePath expected = promise {
    let! exists = TestHelpers.pathExistsAsync (absoluteArcPath arcPath relativePath)
    Vitest.expect(exists).toBe (expected)
}

let private expectPathExistsRequest arcPath relativePath expected = promise {
    match tryResolveArcRelativePath arcPath relativePath with
    | Error error -> return failwith error.Message
    | Ok absolutePath ->
        let! exists = pathExistsAsync absolutePath
        Vitest.expect(exists).toBe (expected)
}

let private writeRelativeFileAsync arcPath relativePath content = promise {
    let absolutePath = absoluteArcPath arcPath relativePath

    let! _ =
        fsPromisesDynamic?writeFile (absolutePath, content, "utf8")
        |> unbox<JS.Promise<obj>>

    return ()
}

let private createRelativeDirectoryAsync arcPath relativePath = promise {
    let absolutePath = absoluteArcPath arcPath relativePath

    let! _ =
        fsPromisesDynamic?mkdir (absolutePath, createObj [ "recursive" ==> true ])
        |> unbox<JS.Promise<obj>>

    return ()
}

let private acceptImportedFiles () = promise { return Ok() }

let private importWithoutCancellation arcPath targetRelativePath sourcePaths =
    ArcFileSystemHelper.importExternalFilesOnDisk
        arcPath
        targetRelativePath
        sourcePaths
        ignore
        (fun () -> false)
        ignore
        ignore
        acceptImportedFiles

/// Imports without cancellation, fails unless the import completes, and returns the planned files.
let private importCollectingPlannedFiles arcPath targetRelativePath sourcePaths = promise {
    let plannedFiles = ResizeArray<string>()

    match!
        ArcFileSystemHelper.importExternalFilesOnDisk
            arcPath
            targetRelativePath
            sourcePaths
            ignore
            (fun () -> false)
            (fun planned -> plannedFiles.AddRange planned.FilePaths)
            ignore
            acceptImportedFiles
    with
    | Error error -> return failwith error.Message
    | Ok ImportExternalFilesResult.Cancelled -> return failwith "Expected import to complete."
    | Ok ImportExternalFilesResult.Completed -> return plannedFiles.ToArray()
}

let private readRelativeFileAsync rootPath relativePath =
    Main.Bindings.Filesystem.readFileAsync
        (absoluteArcPath rootPath relativePath)
        Main.Bindings.Filesystem.TextEncoding.Utf8

let private expectNoTemporaryImportDirectory arcPath relativePath = promise {
    let! entries = Main.Bindings.Filesystem.readdirAsync (absoluteArcPath arcPath relativePath)
    Vitest.expect(entries |> Array.exists _.StartsWith(".swate-import-")).toBe (false)
}

/// Creates `<temp root>/external/Run1` with nested files and an empty folder, and returns its path.
let private createExternalRunFolderAsync arcPath = promise {
    let sourceRoot = dirname arcPath
    do! createRelativeDirectoryAsync sourceRoot "external/Run1/raw"
    do! createRelativeDirectoryAsync sourceRoot "external/Run1/empty"
    do! writeRelativeFileAsync sourceRoot "external/Run1/notes.txt" "run notes"
    do! writeRelativeFileAsync sourceRoot "external/Run1/raw/results.csv" "a,b"
    return absoluteArcPath sourceRoot "external/Run1"
}

/// Adds `relativePath` with `content` inside the folder from `createExternalRunFolderAsync`.
let private createExternalRunFolderWithAsync relativePath content arcPath = promise {
    let! sourcePath = createExternalRunFolderAsync arcPath
    do! writeRelativeFileAsync sourcePath relativePath content
    return sourcePath
}

/// What Git LFS leaves in place of a large file that was not downloaded.
let private gitLfsPointerContent =
    "version https://git-lfs.github.com/spec/v1\noid sha256:"
    + String.replicate 64 "a"
    + "\nsize 104857600\n"

type private RejectedImport = {
    Description: string
    /// Prepares the ARC and the source, and returns the source file or folder to import.
    PrepareSource: string -> JS.Promise<string>
    TargetRelativePath: string
    ExpectedError: string
    /// Exists only if the import wrote something.
    AbsentRelativePath: string
}

let private rejectedImports = [
    {
        // A folder directly inside a collection folder would become an ARC entity.
        Description = "a folder where new folders may not be created"
        PrepareSource = createExternalRunFolderAsync
        TargetRelativePath = "assays"
        ExpectedError =
            "Cannot import 'Run1': Generic file and folder creation is only allowed inside safe ARC directories."
        AbsentRelativePath = "assays/Run1"
    }
    {
        // Such a file would block a later "Add Assay" with that identifier.
        Description = "a file without an extension into a collection folder"
        PrepareSource =
            fun arcPath -> promise {
                do! writeRelativeFileAsync (dirname arcPath) "Assay1" "content"
                return join [| dirname arcPath; "Assay1" |]
            }
        TargetRelativePath = "assays"
        ExpectedError = "Cannot import 'Assay1': Files directly inside an ARC collection folder need a file extension"
        AbsentRelativePath = "assays/Assay1"
    }
    {
        Description = "a file with an ARC metadata name"
        PrepareSource =
            fun arcPath -> promise {
                do! createRelativeDirectoryAsync arcPath "assays/AssayA/dataset"
                do! writeRelativeFileAsync (dirname arcPath) "isa.investigation.xlsx" "copied workbook"
                return join [| dirname arcPath; "isa.investigation.xlsx" |]
            }
        TargetRelativePath = "assays/AssayA/dataset"
        ExpectedError =
            "Cannot import 'isa.investigation.xlsx': Generic file and folder targets must stay inside the ARC and must not target canonical ARC files."
        AbsentRelativePath = "assays/AssayA/dataset/isa.investigation.xlsx"
    }
    {
        Description = "a folder that contains a .git folder"
        PrepareSource =
            fun arcPath -> promise {
                let! sourcePath = createExternalRunFolderAsync arcPath
                do! createRelativeDirectoryAsync sourcePath "raw/.git"
                do! writeRelativeFileAsync sourcePath "raw/.git/HEAD" "ref: main"
                return sourcePath
            }
        TargetRelativePath = ""
        ExpectedError = "Cannot import 'Run1/raw/.git' because '.git' is not allowed inside an ARC."
        AbsentRelativePath = "Run1"
    }
    {
        Description = "a folder that contains an ARC metadata file"
        PrepareSource = createExternalRunFolderWithAsync "raw/isa.assay.xlsx" "copied workbook"
        TargetRelativePath = ""
        ExpectedError = "Cannot import 'Run1/raw/isa.assay.xlsx' because 'isa.assay.xlsx' is reserved for ARC metadata."
        AbsentRelativePath = "Run1"
    }
    {
        // Copied from an ARC where the large file was not downloaded, it would look like data but hold none.
        Description = "a folder that contains a Git LFS pointer file"
        PrepareSource = createExternalRunFolderWithAsync "raw/large.bin" gitLfsPointerContent
        TargetRelativePath = ""
        ExpectedError =
            "Cannot import 'Run1/raw/large.bin' because it is only a Git LFS pointer, not the file's content. Download the file in its ARC first."
        AbsentRelativePath = "Run1"
    }
    {
        // The shortest possible pointer: an `oid` and a one-digit `size` after the version line.
        Description = "a folder that contains the shortest possible Git LFS pointer file"
        PrepareSource =
            createExternalRunFolderWithAsync
                "raw/empty.bin"
                ("version https://git-lfs.github.com/spec/v1\noid sha256:"
                 + String.replicate 64 "a"
                 + "\nsize 0\n")
        TargetRelativePath = ""
        ExpectedError = "Cannot import 'Run1/raw/empty.bin' because it is only a Git LFS pointer"
        AbsentRelativePath = "Run1"
    }
    {
        // The file tree hides lock files, so an imported one would never show up.
        Description = "a picked Excel lock file"
        PrepareSource =
            fun arcPath -> promise {
                do! createRelativeDirectoryAsync arcPath "assays/AssayA/dataset"
                do! writeRelativeFileAsync (dirname arcPath) "~$data.xlsx" "Excel lock file"
                return join [| dirname arcPath; "~$data.xlsx" |]
            }
        TargetRelativePath = "assays/AssayA/dataset"
        ExpectedError =
            "Cannot import '~$data.xlsx' because it is the lock file of a document that is open in Office or LibreOffice."
        AbsentRelativePath = "assays/AssayA/dataset/~$data.xlsx"
    }
    {
        Description = "a picked LibreOffice lock file"
        PrepareSource =
            fun arcPath -> promise {
                do! createRelativeDirectoryAsync arcPath "assays/AssayA/dataset"
                do! writeRelativeFileAsync (dirname arcPath) ".~lock.data.xlsx#" "LibreOffice lock file"
                return join [| dirname arcPath; ".~lock.data.xlsx#" |]
            }
        TargetRelativePath = "assays/AssayA/dataset"
        ExpectedError = "Cannot import '.~lock.data.xlsx#' because it is the lock file of a document"
        AbsentRelativePath = "assays/AssayA/dataset/.~lock.data.xlsx#"
    }
    {
        Description = "a picked Git LFS pointer file"
        PrepareSource =
            fun arcPath -> promise {
                do! createRelativeDirectoryAsync arcPath "assays/AssayA/dataset"
                do! writeRelativeFileAsync (dirname arcPath) "large.bin" gitLfsPointerContent
                return join [| dirname arcPath; "large.bin" |]
            }
        TargetRelativePath = "assays/AssayA/dataset"
        ExpectedError = "Cannot import 'large.bin' because it is only a Git LFS pointer"
        AbsentRelativePath = "assays/AssayA/dataset/large.bin"
    }
    {
        Description = "a folder that contains an ARC metadata file in capitals"
        PrepareSource = createExternalRunFolderWithAsync "raw/ISA.ASSAY.XLSX" "copied workbook"
        TargetRelativePath = ""
        ExpectedError = "Cannot import 'Run1/raw/ISA.ASSAY.XLSX' because 'ISA.ASSAY.XLSX' is reserved for ARC metadata."
        AbsentRelativePath = "Run1"
    }
    {
        // Unlike a nested `.gitkeep`, which is left out, a picked one follows the "New File" rules.
        Description = "a picked .gitkeep file"
        PrepareSource =
            fun arcPath -> promise {
                // ARCtrl puts a `.gitkeep` into `dataset` itself, so the import targets a folder without one.
                do! createRelativeDirectoryAsync arcPath "assays/AssayA/dataset/raw"
                do! writeRelativeFileAsync (dirname arcPath) ".gitkeep" ""
                return join [| dirname arcPath; ".gitkeep" |]
            }
        TargetRelativePath = "assays/AssayA/dataset/raw"
        ExpectedError =
            "Cannot import '.gitkeep': Generic file and folder targets must stay inside the ARC and must not target canonical ARC files."
        AbsentRelativePath = "assays/AssayA/dataset/raw/.gitkeep"
    }
    {
        // Loading the ARC would rename the legacy DataMap name to `isa.datamap.xlsx`.
        Description = "a file with the legacy DataMap name"
        PrepareSource =
            fun arcPath -> promise {
                do! createRelativeDirectoryAsync arcPath "assays/AssayA/dataset"
                do! writeRelativeFileAsync (dirname arcPath) "isa_datamap" "copied datamap"
                return join [| dirname arcPath; "isa_datamap" |]
            }
        TargetRelativePath = "assays/AssayA/dataset"
        ExpectedError = $"Cannot import 'isa_datamap': {legacyDataMapNameError}"
        AbsentRelativePath = "assays/AssayA/dataset/isa_datamap"
    }
    {
        Description = "a folder that contains a file with the legacy DataMap name"
        PrepareSource = createExternalRunFolderWithAsync "raw/isa_datamap" "copied datamap"
        TargetRelativePath = ""
        ExpectedError = $"Cannot import 'Run1/raw/isa_datamap': {legacyDataMapNameError}"
        AbsentRelativePath = "Run1"
    }
    {
        Description = "a folder that contains a symbolic link"
        PrepareSource =
            fun arcPath -> promise {
                let! sourcePath = createExternalRunFolderAsync arcPath

                do! TestHelpers.createFolderLinkAsync (join [| sourcePath; "raw" |]) (join [| sourcePath; "linked" |])

                return sourcePath
            }
        TargetRelativePath = ""
        ExpectedError = "Cannot import 'Run1/linked' because it is a symbolic link."
        AbsentRelativePath = "Run1"
    }
    {
        // `lstat` still recognizes a link whose target is gone.
        Description = "a folder that contains a link whose target is gone"
        PrepareSource =
            fun arcPath -> promise {
                let! sourcePath = createExternalRunFolderAsync arcPath
                let goneTargetPath = absoluteArcPath (dirname arcPath) "external/gone"
                do! createRelativeDirectoryAsync (dirname arcPath) "external/gone"
                do! TestHelpers.createFolderLinkAsync goneTargetPath (join [| sourcePath; "dangling" |])
                do! TestHelpers.removeDirectoryAsync goneTargetPath
                return sourcePath
            }
        TargetRelativePath = ""
        ExpectedError = "Cannot import 'Run1/dangling' because it is a symbolic link."
        AbsentRelativePath = "Run1"
    }
    {
        // The watcher ignores folders with this name too, so they would never be watched.
        Description = "a folder that contains a folder with the legacy DataMap name"
        PrepareSource =
            fun arcPath -> promise {
                let! sourcePath = createExternalRunFolderAsync arcPath
                do! createRelativeDirectoryAsync sourcePath "raw/isa_datamap"
                return sourcePath
            }
        TargetRelativePath = ""
        ExpectedError = $"Cannot import 'Run1/raw/isa_datamap': {legacyDataMapNameError}"
        AbsentRelativePath = "Run1"
    }
    {
        Description = "a folder that is itself a symbolic link"
        PrepareSource =
            fun arcPath -> promise {
                let! sourcePath = createExternalRunFolderAsync arcPath
                let linkPath = join [| dirname arcPath; "external"; "LinkedRun" |]
                do! TestHelpers.createFolderLinkAsync sourcePath linkPath
                return linkPath
            }
        TargetRelativePath = ""
        ExpectedError = "Cannot import 'LinkedRun' because it is a symbolic link."
        AbsentRelativePath = "LinkedRun"
    }
    {
        Description = "a folder into itself"
        PrepareSource =
            fun arcPath -> promise {
                do! createRelativeDirectoryAsync arcPath "assays/AssayA/dataset"
                return absoluteArcPath arcPath "assays/AssayA"
            }
        TargetRelativePath = "assays/AssayA/dataset"
        ExpectedError = "Cannot import folder 'AssayA' into itself."
        AbsentRelativePath = "assays/AssayA/dataset/AssayA"
    }
    {
        Description = "a folder over an existing folder with the same name"
        PrepareSource =
            fun arcPath -> promise {
                do! createRelativeDirectoryAsync arcPath "assays/AssayA/dataset/Run1"
                return! createExternalRunFolderAsync arcPath
            }
        TargetRelativePath = "assays/AssayA/dataset"
        ExpectedError = "Cannot import 'Run1' because an item with that name already exists."
        AbsentRelativePath = "assays/AssayA/dataset/Run1/raw"
    }
    {
        // The picked names are checked before the folder is scanned, so the problem inside it is not reached.
        Description = "a folder over an existing folder before looking inside it"
        PrepareSource =
            fun arcPath -> promise {
                do! createRelativeDirectoryAsync arcPath "assays/AssayA/dataset/Run1"
                let! sourcePath = createExternalRunFolderAsync arcPath
                do! createRelativeDirectoryAsync sourcePath "raw/.git"
                return sourcePath
            }
        TargetRelativePath = "assays/AssayA/dataset"
        ExpectedError = "Cannot import 'Run1' because an item with that name already exists."
        AbsentRelativePath = "assays/AssayA/dataset/Run1/raw"
    }
]

let private expectErrorContaining (expectedMessagePart: string) (result: Result<'T, exn>) =
    match result with
    | Ok value -> failwith $"Expected an error containing '{expectedMessagePart}', but the operation returned {value}."
    | Error error -> Vitest.expect(error.Message).toContain (expectedMessagePart)

let private expectRoute (relativePath: string) expected actual =
    if actual <> expected then
        failwith $"Expected '{relativePath}' to route to {expected}, but it routed to {actual}."

let private expectDeleteRoute arcPath relativePath expected = promise {
    let! route = ArcFileSystemHelper.resolveDeletePathRouteAsync arcPath relativePath
    expectRoute relativePath expected route
}

let private expectGenericRename arcPath relativePath expected = promise {
    let! isGenericRename = ArcFileSystemHelper.isGenericRenamePathAsync arcPath relativePath
    expectRoute relativePath expected isGenericRename
}

Vitest.describe (
    "ArcFileSystemHelper generic filesystem operations",
    fun () ->

        Vitest.test (
            "temporary import directory name matches the watcher ignore convention",
            fun () ->
                let directoryName = ArcFileSystemHelper.createTemporaryImportDirectoryName ()
                let exactPattern = """^\.swate-import-[0-9a-f]{32}$"""

                Vitest.expect(System.Text.RegularExpressions.Regex.IsMatch(directoryName, exactPattern)).toBe (true)

                Vitest
                    .expect(Main.ArcVaultHelper.isFileWatcherPathIgnored $"C:/arc/{directoryName}/file.txt")
                    .toBe (true)
        )

        Vitest.test (
            "ignores LFS backup paths while accepting regular files",
            fun () ->
                let guidSuffix = String.replicate 32 "a"
                let backupPath = $"C:/arc/data.bin.vcs-lfs-backup-{guidSuffix}"

                Vitest.expect(Main.ArcVaultHelper.isFileWatcherPathIgnored backupPath).toBe (true)
                Vitest.expect(Main.ArcVaultHelper.isFileWatcherPathIgnored "C:/arc/data.bin").toBe (false)
        )

        Vitest.test (
            "ignores the lock files of open Office and LibreOffice documents, but not regular files or folders",
            fun () ->
                // Excel writes `~$Book1.xlsx` next to an open `Book1.xlsx`; `.~$` is kept for older matches.
                for lockFilePath in
                    [
                        "C:/arc/~$Book1.xlsx"
                        "C:/arc/assays/AssayA/~$Report.docx"
                        "C:/arc/.~$Book1.xlsx"
                        "C:/arc/.~lock.Book1.xlsx#"
                    ] do
                    Vitest.expect(Main.ArcVaultHelper.isFileWatcherPathIgnored lockFilePath).toBe (true)

                for regularPath in
                    [
                        "C:/arc/Book1.xlsx"
                        "C:/arc/data~$1.xlsx"
                        "C:/arc/~$drafts"
                        "C:/arc/~$drafts/notes.md"
                    ] do
                    Vitest.expect(Main.ArcVaultHelper.isFileWatcherPathIgnored regularPath).toBe (false)
        )

        Vitest.test (
            "leaves files the watcher ignores out of the imported file events",
            fun () ->
                // The watcher never reports these files, so a registration for them would never be released.
                let guidSuffix = String.replicate 32 "a"

                let events =
                    Main.WatcherHelpers.createImportedWatcherEvents "C:/arc" "assays/AssayA/dataset" {
                        FolderPaths = [| $"Run1/.swate-import-{guidSuffix}" |]
                        FilePaths = [|
                            "Run1/notes.txt"
                            "Run1/~$results.xlsx"
                            $"Run1/.swate-import-{guidSuffix}/raw.txt"
                            $"Run1/data.bin.vcs-lfs-backup-{guidSuffix}"
                        |]
                    }

                Vitest.expect(events |> Array.map _.RelativePath).toEqual ([| "assays/AssayA/dataset/Run1/notes.txt" |])
        )

        Vitest.test (
            "builds addDir events for planned folders and add events for planned files, below the target or at the ARC root",
            fun () ->
                let addDir = Main.Bindings.Chokidar.Events.AddDir.ToString()
                let add = Main.Bindings.Chokidar.Events.Add.ToString()

                let plannedPaths = {
                    FolderPaths = [| "Run1"; "Run1/raw" |]
                    FilePaths = [| "Run1/raw/results.csv" |]
                }

                let eventsFor target =
                    Main.WatcherHelpers.createImportedWatcherEvents "C:/arc" target plannedPaths
                    |> Array.map (fun event -> event.EventName, event.RelativePath)

                Vitest
                    .expect(eventsFor "assays/AssayA/dataset")
                    .toEqual (
                        [|
                            addDir, "assays/AssayA/dataset/Run1"
                            addDir, "assays/AssayA/dataset/Run1/raw"
                            add, "assays/AssayA/dataset/Run1/raw/results.csv"
                        |]
                    )

                Vitest
                    .expect(eventsFor "")
                    .toEqual (
                        [|
                            addDir, "Run1"
                            addDir, "Run1/raw"
                            add, "Run1/raw/results.csv"
                        |]
                    )
        )

        Vitest.test (
            "checks whether relative files and directories exist",
            fun () ->
                withAssayArc (fun arcPath -> promise {
                    do! createRelativeDirectoryAsync arcPath "notes/existing-note"
                    do! writeRelativeFileAsync arcPath "notes/existing-note/existing-note.md" "hello"

                    do! expectPathExistsRequest arcPath "notes/existing-note/existing-note.md" true
                    do! expectPathExistsRequest arcPath "notes/existing-note" true
                })
        )

        Vitest.test (
            "reports missing safe relative paths as absent",
            fun () ->
                withAssayArc (fun arcPath -> promise { do! expectPathExistsRequest arcPath "notes/missing-note" false })
        )

        Vitest.test (
            "rejects unsafe path existence requests",
            fun () ->
                withAssayArc (fun arcPath -> promise {
                    match tryResolveArcRelativePath arcPath "../outside" with
                    | Ok _ -> failwith "Expected traversal path to be rejected."
                    | Error error -> Vitest.expect(error.Message.Length > 0).toBe (true)

                    match tryResolveArcRelativePath arcPath arcPath with
                    | Ok _ -> failwith "Expected absolute path to be rejected."
                    | Error error -> Vitest.expect(error.Message.Length > 0).toBe (true)
                })
        )

        Vitest.test (
            "creates generic folders through the ARCtrl-backed create path",
            fun () ->
                withAssayArc (fun arcPath -> promise {
                    match!
                        ArcFileSystemHelper.createFileSystemItemOnDisk
                            arcPath
                            (createItemRequest "assays/AssayA" "attachments" FileSystemItemKind.Folder)
                    with
                    | Error error -> failwith error.Message
                    | Ok createdPath ->
                        Vitest.expect(createdPath).toBe ("assays/AssayA/attachments")

                        let! isDirectory =
                            ARCtrl.FileSystemHelper.directoryExistsAsync (absoluteArcPath arcPath createdPath)

                        Vitest.expect(isDirectory).toBe (true)
                })
        )

        Vitest.test (
            "imports an external file into an ARC folder",
            fun () ->
                withAssayArc (fun arcPath -> promise {
                    let sourcePath = join [| dirname arcPath; "external.txt" |]
                    do! writeRelativeFileAsync (dirname arcPath) "external.txt" "imported content"

                    let progress = ResizeArray<float>()

                    match!
                        ArcFileSystemHelper.importExternalFilesOnDisk
                            arcPath
                            "assays/AssayA"
                            [| sourcePath |]
                            progress.Add
                            (fun () -> false)
                            ignore
                            ignore
                            acceptImportedFiles
                    with
                    | Error error -> failwith error.Message
                    | Ok ImportExternalFilesResult.Cancelled -> failwith "Expected import to complete."
                    | Ok ImportExternalFilesResult.Completed ->
                        let importedPath = absoluteArcPath arcPath "assays/AssayA/external.txt"

                        let! importedContent =
                            fsPromisesDynamic?readFile (importedPath, "utf8") |> unbox<JS.Promise<string>>

                        Vitest.expect(importedContent).toBe ("imported content")
                        Vitest.expect(progress.Count).toBe (2)
                        Vitest.expect(progress.[0]).toBe (0.0)
                        Vitest.expect(progress.[1]).toBe (1.0)
                })
        )

        Vitest.test (
            "cancels a folder import while it is copied into the staging folder",
            fun () ->
                withAssayArc (fun arcPath -> promise {
                    let! sourcePath = createExternalRunFolderAsync arcPath
                    do! createRelativeDirectoryAsync arcPath "assays/AssayA/dataset"
                    let mutable cancelRequested = false

                    // The first progress report comes after the first entry is staged.
                    let onProgress progress =
                        if progress > 0.0 then
                            cancelRequested <- true

                    match!
                        ArcFileSystemHelper.importExternalFilesOnDisk
                            arcPath
                            "assays/AssayA/dataset"
                            [| sourcePath |]
                            onProgress
                            (fun () -> cancelRequested)
                            ignore
                            ignore
                            acceptImportedFiles
                    with
                    | Error error -> failwith error.Message
                    | Ok ImportExternalFilesResult.Completed -> failwith "Expected import cancellation."
                    | Ok ImportExternalFilesResult.Cancelled ->
                        do! expectRelativePathExists arcPath "assays/AssayA/dataset/Run1" false
                        do! expectNoTemporaryImportDirectory arcPath "assays/AssayA/dataset"
                })
        )

        Vitest.test (
            "cancels an import without leaving temporary or imported files",
            fun () ->
                withAssayArc (fun arcPath -> promise {
                    let sourceDirectory = dirname arcPath
                    let firstName = $"cancel-first-{newGuidN ()}.txt"
                    let secondName = $"cancel-second-{newGuidN ()}.txt"
                    do! writeRelativeFileAsync sourceDirectory firstName "first"
                    do! writeRelativeFileAsync sourceDirectory secondName "second"

                    let mutable cancelRequested = false

                    let onProgress progress =
                        if progress > 0.0 then
                            cancelRequested <- true

                    match!
                        ArcFileSystemHelper.importExternalFilesOnDisk
                            arcPath
                            "assays/AssayA"
                            [|
                                join [| sourceDirectory; firstName |]
                                join [| sourceDirectory; secondName |]
                            |]
                            onProgress
                            (fun () -> cancelRequested)
                            ignore
                            ignore
                            acceptImportedFiles
                    with
                    | Error error -> failwith error.Message
                    | Ok ImportExternalFilesResult.Completed -> failwith "Expected import cancellation."
                    | Ok ImportExternalFilesResult.Cancelled ->
                        let targetDirectory = absoluteArcPath arcPath "assays/AssayA"
                        let! firstExists = pathExistsAsync (join [| targetDirectory; firstName |])
                        let! secondExists = pathExistsAsync (join [| targetDirectory; secondName |])
                        let! targetEntries = fsPromisesDynamic?readdir targetDirectory |> unbox<JS.Promise<string[]>>

                        let! arcParentEntries =
                            fsPromisesDynamic?readdir (dirname arcPath) |> unbox<JS.Promise<string[]>>

                        Vitest.expect(firstExists).toBe (false)
                        Vitest.expect(secondExists).toBe (false)
                        Vitest.expect(targetEntries |> Array.exists _.StartsWith(".swate-import-")).toBe (false)
                        Vitest.expect(arcParentEntries |> Array.exists _.StartsWith(".swate-import-")).toBe (false)
                })
        )

        Vitest.test (
            "rolls back imported files when post-import validation fails",
            fun () ->
                withAssayArc (fun arcPath -> promise {
                    let sourceDirectory = dirname arcPath
                    let fileName = $"invalid-arc-import-{newGuidN ()}.xlsx"
                    let sourcePath = join [| sourceDirectory; fileName |]
                    do! writeRelativeFileAsync sourceDirectory fileName "malformed ARC workbook"

                    match!
                        ArcFileSystemHelper.importExternalFilesOnDisk
                            arcPath
                            "assays/AssayA"
                            [| sourcePath |]
                            ignore
                            (fun () -> false)
                            ignore
                            ignore
                            (fun () -> promise { return Error(exn "ARC synchronization failed") })
                    with
                    | Ok outcome -> failwith $"Expected validation failure, got {outcome}."
                    | Error error ->
                        Vitest.expect(error.Message).toContain ("ARC synchronization failed")

                        let importedPath = absoluteArcPath arcPath $"assays/AssayA/{fileName}"
                        let! importedFileExists = pathExistsAsync importedPath
                        Vitest.expect(importedFileExists).toBe (false)
                })
        )

        Vitest.test (
            "rejects a picked file that no longer exists and writes nothing",
            fun () ->
                withAssayArc (fun arcPath -> promise {
                    let sourceDirectory = dirname arcPath
                    let firstName = $"error-first-{newGuidN ()}.txt"
                    let missingName = $"missing-{newGuidN ()}.txt"
                    do! writeRelativeFileAsync sourceDirectory firstName "first"

                    match!
                        ArcFileSystemHelper.importExternalFilesOnDisk
                            arcPath
                            "assays/AssayA"
                            [|
                                join [| sourceDirectory; firstName |]
                                join [| sourceDirectory; missingName |]
                            |]
                            ignore
                            (fun () -> false)
                            ignore
                            ignore
                            acceptImportedFiles
                    with
                    | Ok outcome -> failwith $"Expected import failure, got {outcome}."
                    | Error error ->
                        Vitest.expect(error.Message).toContain ("ENOENT")

                        let targetDirectory = absoluteArcPath arcPath "assays/AssayA"
                        let! firstExists = pathExistsAsync (join [| targetDirectory; firstName |])
                        let! entries = fsPromisesDynamic?readdir targetDirectory |> unbox<JS.Promise<string[]>>

                        Vitest.expect(firstExists).toBe (false)
                        Vitest.expect(entries |> Array.exists _.StartsWith(".swate-import-")).toBe (false)
                })
        )

        Vitest.test (
            "keeps published files when only staging-directory cleanup fails",
            fun () ->
                withAssayArc (fun arcPath -> promise {
                    let sourceDirectory = dirname arcPath
                    let fileName = $"cleanup-failure-{newGuidN ()}.txt"
                    let sourcePath = join [| sourceDirectory; fileName |]
                    let mutable cleanupErrorWasLogged = false
                    let lifecycleOrder = ResizeArray<string>()
                    do! writeRelativeFileAsync sourceDirectory fileName "published content"

                    match!
                        ArcFileSystemHelper.importExternalFilesOnDiskWithTemporaryCleanup
                            (fun _ ->
                                lifecycleOrder.Add "cleanup"
                                JS.Constructors.Promise.reject (nodeError "staging directory busy" "EBUSY")
                            )
                            (fun _ -> cleanupErrorWasLogged <- true)
                            arcPath
                            "assays/AssayA"
                            [| sourcePath |]
                            ignore
                            (fun () -> false)
                            ignore
                            (fun _ -> lifecycleOrder.Add "published")
                            (fun () -> promise {
                                lifecycleOrder.Add "validate"
                                return Ok()
                            })
                    with
                    | Error error -> failwith error.Message
                    | Ok ImportExternalFilesResult.Cancelled -> failwith "Expected import to complete."
                    | Ok ImportExternalFilesResult.Completed ->
                        let importedPath = absoluteArcPath arcPath $"assays/AssayA/{fileName}"
                        let! importedFileExists = pathExistsAsync importedPath
                        Vitest.expect(importedFileExists).toBe (true)
                        Vitest.expect(cleanupErrorWasLogged).toBe (true)
                        Vitest.expect(lifecycleOrder.[0]).toBe ("published")
                        Vitest.expect(lifecycleOrder.[lifecycleOrder.Count - 1]).toBe ("validate")

                        Vitest
                            .expect(
                                lifecycleOrder
                                |> Seq.skip 1
                                |> Seq.take (lifecycleOrder.Count - 2)
                                |> Seq.forall ((=) "cleanup")
                            )
                            .toBe (true)
                })
        )

        Vitest.test (
            "does not overwrite an existing destination file",
            fun () ->
                withAssayArc (fun arcPath -> promise {
                    let fileName = $"existing-{newGuidN ()}.txt"
                    let sourceDirectory = dirname arcPath
                    do! writeRelativeFileAsync sourceDirectory fileName "new content"
                    do! writeRelativeFileAsync arcPath $"assays/AssayA/{fileName}" "original content"

                    match!
                        ArcFileSystemHelper.importExternalFilesOnDisk
                            arcPath
                            "assays/AssayA"
                            [| join [| sourceDirectory; fileName |] |]
                            ignore
                            (fun () -> false)
                            ignore
                            ignore
                            acceptImportedFiles
                    with
                    | Ok outcome -> failwith $"Expected destination conflict, got {outcome}."
                    | Error error ->
                        Vitest.expect(error.Message).toContain ("because an item with that name already exists")

                        let destinationPath = absoluteArcPath arcPath $"assays/AssayA/{fileName}"

                        let! content =
                            fsPromisesDynamic?readFile (destinationPath, "utf8")
                            |> unbox<JS.Promise<string>>

                        Vitest.expect(content).toBe ("original content")
                })
        )

        Vitest.test (
            "imports an external folder with its nested files and empty folders",
            fun () ->
                withAssayArc (fun arcPath -> promise {
                    let! sourcePath = createExternalRunFolderAsync arcPath
                    do! createRelativeDirectoryAsync arcPath "assays/AssayA/dataset"
                    let mutable plannedFiles = [||]
                    let mutable plannedFolders = [||]
                    let mutable publishedFolders = [||]
                    let lifecycleOrder = ResizeArray<string>()

                    // The import handler registers watcher ownership in onPathsPlanned, so it must run
                    // before anything appears in the target.
                    let onPathsPlanned (planned: PlannedImportPaths) =
                        plannedFiles <- planned.FilePaths
                        plannedFolders <- planned.FolderPaths

                        if
                            Main.Bindings.Filesystem.existsSync (absoluteArcPath arcPath "assays/AssayA/dataset/Run1")
                        then
                            lifecycleOrder.Add "planned after publishing started"
                        else
                            lifecycleOrder.Add "planned"

                    match!
                        ArcFileSystemHelper.importExternalFilesOnDisk
                            arcPath
                            "assays/AssayA/dataset"
                            [| sourcePath |]
                            ignore
                            (fun () -> false)
                            onPathsPlanned
                            (fun folders ->
                                publishedFolders <- folders
                                lifecycleOrder.Add "published"
                            )
                            acceptImportedFiles
                    with
                    | Error error -> failwith error.Message
                    | Ok ImportExternalFilesResult.Cancelled -> failwith "Expected import to complete."
                    | Ok ImportExternalFilesResult.Completed ->
                        let! notes = readRelativeFileAsync arcPath "assays/AssayA/dataset/Run1/notes.txt"
                        let! results = readRelativeFileAsync arcPath "assays/AssayA/dataset/Run1/raw/results.csv"

                        let! emptyFolderExists =
                            ARCtrl.FileSystemHelper.directoryExistsAsync (
                                absoluteArcPath arcPath "assays/AssayA/dataset/Run1/empty"
                            )

                        Vitest.expect(plannedFiles).toEqual ([| "Run1/notes.txt"; "Run1/raw/results.csv" |])
                        Vitest.expect(plannedFolders).toEqual ([| "Run1"; "Run1/empty"; "Run1/raw" |])
                        Vitest.expect(lifecycleOrder |> Seq.toArray).toEqual ([| "planned"; "published" |])

                        // Only the picked folder is reported, not the folders inside it.
                        Vitest
                            .expect(publishedFolders)
                            .toEqual ([| absoluteArcPath arcPath "assays/AssayA/dataset/Run1" |])

                        Vitest.expect(notes).toBe ("run notes")
                        Vitest.expect(results).toBe ("a,b")
                        Vitest.expect(emptyFolderExists).toBe (true)
                        do! expectNoTemporaryImportDirectory arcPath "assays/AssayA/dataset"
                })
        )

        Vitest.test (
            "leaves lock files, staging leftovers and the ARC's own git files out of an imported folder, but keeps README files",
            fun () ->
                withAssayArc (fun arcPath -> promise {
                    let guidSuffix = String.replicate 32 "a"
                    let! sourcePath = createExternalRunFolderAsync arcPath
                    do! writeRelativeFileAsync sourcePath "raw/~$results.xlsx" "Excel lock file"
                    do! writeRelativeFileAsync sourcePath "raw/.~lock.results.csv#" "LibreOffice lock file"
                    do! writeRelativeFileAsync sourcePath "raw/.gitattributes" "*.csv filter=lfs"
                    do! writeRelativeFileAsync sourcePath "empty/.gitkeep" ""
                    do! writeRelativeFileAsync sourcePath "raw/.GITKEEP" ""
                    // README files share the protected list with the git files, but "New File" may create them.
                    do! writeRelativeFileAsync sourcePath "raw/README.md" "about the raw data"
                    do! writeRelativeFileAsync sourcePath $"raw/results.csv.vcs-lfs-backup-{guidSuffix}" "backup"
                    do! createRelativeDirectoryAsync sourcePath $".swate-import-{guidSuffix}"
                    do! writeRelativeFileAsync sourcePath $".swate-import-{guidSuffix}/staged.txt" "staged"
                    do! createRelativeDirectoryAsync arcPath "assays/AssayA/dataset"

                    let! plannedFiles = importCollectingPlannedFiles arcPath "assays/AssayA/dataset" [| sourcePath |]

                    Vitest
                        .expect(plannedFiles)
                        .toEqual (
                            [|
                                "Run1/notes.txt"
                                "Run1/raw/README.md"
                                "Run1/raw/results.csv"
                            |]
                        )

                    for skippedPath in
                        [
                            "raw/~$results.xlsx"
                            "raw/.~lock.results.csv#"
                            "raw/.gitattributes"
                            "empty/.gitkeep"
                            "raw/.GITKEEP"
                            $"raw/results.csv.vcs-lfs-backup-{guidSuffix}"
                            $".swate-import-{guidSuffix}"
                        ] do
                        do! expectRelativePathExists arcPath $"assays/AssayA/dataset/Run1/{skippedPath}" false

                    do! expectRelativePathExists arcPath "assays/AssayA/dataset/Run1/empty" true
                    do! expectNoTemporaryImportDirectory arcPath "assays/AssayA/dataset"
                })
        )

        Vitest.test (
            "imports folders named like lock files or ARC git files with their contents",
            fun () ->
                withAssayArc (fun arcPath -> promise {
                    let! sourcePath = createExternalRunFolderAsync arcPath
                    do! createRelativeDirectoryAsync sourcePath "~$archive.old"
                    do! createRelativeDirectoryAsync sourcePath ".gitkeep"
                    do! writeRelativeFileAsync sourcePath "~$archive.old/data.csv" "a,b"
                    do! writeRelativeFileAsync sourcePath ".gitkeep/inner.txt" "inner"
                    do! createRelativeDirectoryAsync arcPath "assays/AssayA/dataset"

                    let! plannedFiles = importCollectingPlannedFiles arcPath "assays/AssayA/dataset" [| sourcePath |]

                    let! lockFolderContent =
                        readRelativeFileAsync arcPath "assays/AssayA/dataset/Run1/~$archive.old/data.csv"

                    let! gitFolderContent =
                        readRelativeFileAsync arcPath "assays/AssayA/dataset/Run1/.gitkeep/inner.txt"

                    Vitest.expect(plannedFiles |> Array.contains "Run1/~$archive.old/data.csv").toBe (true)
                    Vitest.expect(plannedFiles |> Array.contains "Run1/.gitkeep/inner.txt").toBe (true)
                    Vitest.expect(lockFolderContent).toBe ("a,b")
                    Vitest.expect(gitFolderContent).toBe ("inner")
                })
        )

        Vitest.test (
            "rolls back a folder import when a source file disappears while it is copied",
            fun () ->
                withAssayArc (fun arcPath -> promise {
                    let! sourcePath = createExternalRunFolderAsync arcPath
                    do! createRelativeDirectoryAsync arcPath "assays/AssayA/dataset"

                    // Planning has finished at this point, so copying into the staging folder is the first
                    // step that misses the file.
                    let removeSourceFile _ =
                        Main.Bindings.Filesystem.unlinkSync (join [| sourcePath; "raw"; "results.csv" |])

                    let! result =
                        ArcFileSystemHelper.importExternalFilesOnDisk
                            arcPath
                            "assays/AssayA/dataset"
                            [| sourcePath |]
                            ignore
                            (fun () -> false)
                            removeSourceFile
                            ignore
                            acceptImportedFiles

                    result |> expectErrorContaining "ENOENT"
                    do! expectRelativePathExists arcPath "assays/AssayA/dataset/Run1" false
                    do! expectNoTemporaryImportDirectory arcPath "assays/AssayA/dataset"
                })
        )

        Vitest.test (
            "keeps an item that appears in the target after the checks and stops the import",
            fun () ->
                withAssayArc (fun arcPath -> promise {
                    let! sourcePath = createExternalRunFolderAsync arcPath
                    do! createRelativeDirectoryAsync arcPath "assays/AssayA/dataset"
                    let conflictingFolder = absoluteArcPath arcPath "assays/AssayA/dataset/Run1"

                    // Another program creates `Run1` after the import checked that the name is free. The
                    // rollback only removes what the import created, so this folder must survive.
                    let createConflictingFolder _ =
                        Main.Bindings.Filesystem.mkdirSync
                            conflictingFolder
                            (Main.Bindings.Filesystem.MkdirOptions(recursive = true))

                        TestHelpers.writeText (join [| conflictingFolder; "mine.txt" |]) "my data"

                    let! result =
                        ArcFileSystemHelper.importExternalFilesOnDisk
                            arcPath
                            "assays/AssayA/dataset"
                            [| sourcePath |]
                            ignore
                            (fun () -> false)
                            createConflictingFolder
                            ignore
                            acceptImportedFiles

                    result |> expectErrorContaining "EEXIST"
                    let! mine = readRelativeFileAsync arcPath "assays/AssayA/dataset/Run1/mine.txt"
                    Vitest.expect(mine).toBe ("my data")
                    do! expectRelativePathExists arcPath "assays/AssayA/dataset/Run1/notes.txt" false
                    do! expectNoTemporaryImportDirectory arcPath "assays/AssayA/dataset"
                })
        )

        Vitest.test (
            "imports files that only resemble Git LFS pointers",
            fun () ->
                withAssayArc (fun arcPath -> promise {
                    let! sourcePath = createExternalRunFolderAsync arcPath
                    // The version line is not at the start of this file.
                    do!
                        writeRelativeFileAsync
                            sourcePath
                            "raw/lfs-notes.md"
                            $"A pointer looks like this:\n{gitLfsPointerContent}"
                    // A pointer never exceeds 1024 bytes, and this file has extra content after the `size` line.
                    do!
                        writeRelativeFileAsync
                            sourcePath
                            "raw/pointer-like.txt"
                            (gitLfsPointerContent + String.replicate 1024 "x")
                    // It has no `oid` and no `size` line, so it can't be a pointer.
                    do!
                        writeRelativeFileAsync
                            sourcePath
                            "raw/version-only.txt"
                            "version https://git-lfs.github.com/spec/v1\n"
                    // Well under 1024 bytes, but the lines after the version line are prose.
                    do!
                        writeRelativeFileAsync
                            sourcePath
                            "raw/prose-after-version.md"
                            ("version https://git-lfs.github.com/spec/v1\n"
                             + String.replicate
                                 4
                                 "This note describes the measurement setup and how the samples were prepared.\n")

                    do! createRelativeDirectoryAsync arcPath "assays/AssayA/dataset"

                    let! plannedFiles = importCollectingPlannedFiles arcPath "assays/AssayA/dataset" [| sourcePath |]

                    Vitest.expect(plannedFiles).toContain ("Run1/raw/lfs-notes.md")
                    Vitest.expect(plannedFiles).toContain ("Run1/raw/pointer-like.txt")
                    Vitest.expect(plannedFiles).toContain ("Run1/raw/version-only.txt")
                    Vitest.expect(plannedFiles).toContain ("Run1/raw/prose-after-version.md")
                })
        )

        Vitest.test (
            "imports several folders picked together, even when their contents share names",
            fun () ->
                withAssayArc (fun arcPath -> promise {
                    let sourceRoot = dirname arcPath
                    let! firstSourcePath = createExternalRunFolderAsync arcPath
                    do! createRelativeDirectoryAsync sourceRoot "external/Run2"
                    // Only the picked items need unique names, so both folders may contain `notes.txt`.
                    do! writeRelativeFileAsync sourceRoot "external/Run2/notes.txt" "second run notes"
                    do! createRelativeDirectoryAsync arcPath "assays/AssayA/dataset"

                    let! plannedFiles =
                        importCollectingPlannedFiles arcPath "assays/AssayA/dataset" [|
                            firstSourcePath
                            absoluteArcPath sourceRoot "external/Run2"
                        |]

                    let! firstNotes = readRelativeFileAsync arcPath "assays/AssayA/dataset/Run1/notes.txt"
                    let! secondNotes = readRelativeFileAsync arcPath "assays/AssayA/dataset/Run2/notes.txt"

                    Vitest
                        .expect(plannedFiles)
                        .toEqual (
                            [|
                                "Run1/notes.txt"
                                "Run1/raw/results.csv"
                                "Run2/notes.txt"
                            |]
                        )

                    Vitest.expect(firstNotes).toBe ("run notes")
                    Vitest.expect(secondNotes).toBe ("second run notes")
                    do! expectNoTemporaryImportDirectory arcPath "assays/AssayA/dataset"
                })
        )

        Vitest.test (
            "rejects picking two items with the same name, ignoring case, and writes nothing",
            fun () ->
                withAssayArc (fun arcPath -> promise {
                    let sourceRoot = dirname arcPath
                    do! createRelativeDirectoryAsync sourceRoot "external/a/Run1"
                    do! writeRelativeFileAsync sourceRoot "external/a/Run1/notes.txt" "run notes"
                    do! createRelativeDirectoryAsync sourceRoot "external/a/Run1/.git"
                    do! createRelativeDirectoryAsync sourceRoot "external/b"
                    do! writeRelativeFileAsync sourceRoot "external/b/run1" "a file named like the folder"
                    do! createRelativeDirectoryAsync arcPath "assays/AssayA/dataset"

                    let! result =
                        importWithoutCancellation arcPath "assays/AssayA/dataset" [|
                            absoluteArcPath sourceRoot "external/a/Run1"
                            absoluteArcPath sourceRoot "external/b/run1"
                        |]

                    result |> expectErrorContaining "Cannot import multiple items named 'Run1'."
                    do! expectRelativePathExists arcPath "assays/AssayA/dataset/Run1" false
                    do! expectRelativePathExists arcPath "assays/AssayA/dataset/run1" false
                    do! expectNoTemporaryImportDirectory arcPath "assays/AssayA/dataset"
                })
        )

        Vitest.test (
            "rejects a nested name with a backslash and writes nothing",
            fun (context: TestContext) ->
                context.skip (nodePlatform () = "win32", "Windows does not allow a backslash in a file name")

                withAssayArc (fun arcPath -> promise {
                    let! sourcePath = createExternalRunFolderAsync arcPath

                    let! _ =
                        fsPromisesDynamic?writeFile (join [| sourcePath; "raw"; "a\\b.csv" |], "x", "utf8")
                        |> unbox<JS.Promise<obj>>

                    do! createRelativeDirectoryAsync arcPath "assays/AssayA/dataset"
                    let! result = importWithoutCancellation arcPath "assays/AssayA/dataset" [| sourcePath |]

                    result |> expectErrorContaining "must not contain path separators"
                    result |> expectErrorContaining "Run1/raw/a\\b.csv"
                    do! expectRelativePathExists arcPath "assays/AssayA/dataset/Run1" false
                    do! expectNoTemporaryImportDirectory arcPath "assays/AssayA/dataset"
                })
        )

        for rejected in rejectedImports do
            Vitest.test (
                $"rejects importing {rejected.Description} and writes nothing",
                fun () ->
                    withAssayArc (fun arcPath -> promise {
                        let! sourcePath = rejected.PrepareSource arcPath
                        let! result = importWithoutCancellation arcPath rejected.TargetRelativePath [| sourcePath |]

                        result |> expectErrorContaining rejected.ExpectedError
                        do! expectRelativePathExists arcPath rejected.AbsentRelativePath false
                        do! expectNoTemporaryImportDirectory arcPath rejected.TargetRelativePath
                    })
            )

        Vitest.test (
            "rolls back every imported folder when post-import validation fails",
            fun () ->
                withAssayArc (fun arcPath -> promise {
                    let sourceRoot = dirname arcPath
                    let! firstSourcePath = createExternalRunFolderAsync arcPath
                    do! createRelativeDirectoryAsync sourceRoot "external/Run2/raw"
                    do! writeRelativeFileAsync sourceRoot "external/Run2/raw/data.csv" "c,d"
                    do! createRelativeDirectoryAsync arcPath "assays/AssayA/dataset"

                    let! result =
                        ArcFileSystemHelper.importExternalFilesOnDisk
                            arcPath
                            "assays/AssayA/dataset"
                            [|
                                firstSourcePath
                                absoluteArcPath sourceRoot "external/Run2"
                            |]
                            ignore
                            (fun () -> false)
                            ignore
                            ignore
                            (fun () -> promise { return Error(exn "ARC synchronization failed") })

                    result |> expectErrorContaining "ARC synchronization failed"
                    do! expectRelativePathExists arcPath "assays/AssayA/dataset/Run1" false
                    do! expectRelativePathExists arcPath "assays/AssayA/dataset/Run2" false
                    do! expectNoTemporaryImportDirectory arcPath "assays/AssayA/dataset"
                })
        )

        Vitest.test (
            "cancels a folder import while the folder is still being scanned",
            fun () ->
                withAssayArc (fun arcPath -> promise {
                    let! sourcePath = createExternalRunFolderAsync arcPath
                    do! createRelativeDirectoryAsync arcPath "assays/AssayA/dataset"
                    let mutable filesWerePlanned = false

                    match!
                        ArcFileSystemHelper.importExternalFilesOnDisk
                            arcPath
                            "assays/AssayA/dataset"
                            [| sourcePath |]
                            ignore
                            (fun () -> true)
                            (fun _ -> filesWerePlanned <- true)
                            ignore
                            acceptImportedFiles
                    with
                    | Error error -> failwith error.Message
                    | Ok ImportExternalFilesResult.Completed -> failwith "Expected import cancellation."
                    | Ok ImportExternalFilesResult.Cancelled ->
                        // The import handler registers watcher ownership when files are planned, so a scan
                        // that stops early must not plan anything.
                        Vitest.expect(filesWerePlanned).toBe (false)
                        do! expectRelativePathExists arcPath "assays/AssayA/dataset/Run1" false
                        do! expectNoTemporaryImportDirectory arcPath "assays/AssayA/dataset"
                })
        )

        Vitest.test (
            "rolls back a folder import that is cancelled while its contents are published",
            fun () ->
                withAssayArc (fun arcPath -> promise {
                    let! sourcePath = createExternalRunFolderAsync arcPath
                    do! createRelativeDirectoryAsync arcPath "assays/AssayA/dataset"
                    let mutable cancelledDuringPublishing = false

                    // Only the top-level folder is registered for rollback, so the nested items that
                    // were already published must be removed together with it.
                    let isCancellationRequested () =
                        if
                            Main.Bindings.Filesystem.existsSync (
                                absoluteArcPath arcPath "assays/AssayA/dataset/Run1/notes.txt"
                            )
                        then
                            cancelledDuringPublishing <- true

                        cancelledDuringPublishing

                    match!
                        ArcFileSystemHelper.importExternalFilesOnDisk
                            arcPath
                            "assays/AssayA/dataset"
                            [| sourcePath |]
                            ignore
                            isCancellationRequested
                            ignore
                            ignore
                            acceptImportedFiles
                    with
                    | Error error -> failwith error.Message
                    | Ok ImportExternalFilesResult.Completed -> failwith "Expected import cancellation."
                    | Ok ImportExternalFilesResult.Cancelled ->
                        Vitest.expect(cancelledDuringPublishing).toBe (true)
                        do! expectRelativePathExists arcPath "assays/AssayA/dataset/Run1" false
                        do! expectNoTemporaryImportDirectory arcPath "assays/AssayA/dataset"
                })
        )

        Vitest.test (
            "resolves a path through a link to its real path",
            fun () ->
                withAssayArc (fun arcPath -> promise {
                    let linkedArcPath = join [| dirname arcPath; "linked-arc" |]
                    do! TestHelpers.createFolderLinkAsync arcPath linkedArcPath

                    let! realArcPath = tryResolveRealPathAsync arcPath
                    let! resolvedLinkedArcPath = tryResolveRealPathAsync linkedArcPath

                    Vitest
                        .expect(PathHelpers.normalizePathForFsComparison resolvedLinkedArcPath)
                        .toBe (PathHelpers.normalizePathForFsComparison realArcPath)
                })
        )

        Vitest.test (
            "returns a path that cannot be resolved as given",
            fun () ->
                withAssayArc (fun arcPath -> promise {
                    let linkedArcPath = join [| dirname arcPath; "linked-arc" |]
                    do! TestHelpers.createFolderLinkAsync arcPath linkedArcPath

                    // Resolving a missing path fails like it does on drives that cannot resolve real paths.
                    let missingPath = join [| linkedArcPath; "missing" |]
                    let! resolvedPath = tryResolveRealPathAsync missingPath

                    Vitest.expect(resolvedPath).toBe (missingPath)
                })
        )

        Vitest.test (
            "rejects importing a folder into itself when the ARC is opened through a link",
            fun () ->
                withAssayArc (fun arcPath -> promise {
                    do! createRelativeDirectoryAsync arcPath "backup/inner"
                    let linkedArcPath = join [| dirname arcPath; "linked-arc" |]
                    do! TestHelpers.createFolderLinkAsync arcPath linkedArcPath

                    let! result =
                        importWithoutCancellation linkedArcPath "backup/inner" [| absoluteArcPath arcPath "backup" |]

                    result |> expectErrorContaining "Cannot import folder 'backup' into itself."
                    do! expectRelativePathExists arcPath "backup/inner/backup" false
                    do! expectNoTemporaryImportDirectory arcPath "backup/inner"
                })
        )

        Vitest.test (
            "imports the content of a picked file link",
            TestOptions(skip = not (TestHelpers.canCreateFileLinks ())),
            fun () ->
                withAssayArc (fun arcPath -> promise {
                    let sourceRoot = dirname arcPath
                    do! createRelativeDirectoryAsync arcPath "assays/AssayA/dataset"
                    do! createRelativeDirectoryAsync sourceRoot "external"
                    do! writeRelativeFileAsync sourceRoot "external/notes.txt" "linked notes"

                    let linkPath = absoluteArcPath sourceRoot "external/linked-notes.txt"
                    do! TestHelpers.createFileLinkAsync (absoluteArcPath sourceRoot "external/notes.txt") linkPath

                    match! importWithoutCancellation arcPath "assays/AssayA/dataset" [| linkPath |] with
                    | Error error -> failwith error.Message
                    | Ok ImportExternalFilesResult.Cancelled -> failwith "Expected import to complete."
                    | Ok ImportExternalFilesResult.Completed ->
                        let! notes = readRelativeFileAsync arcPath "assays/AssayA/dataset/linked-notes.txt"
                        Vitest.expect(notes).toBe ("linked notes")
                })
        )

        Vitest.test (
            "protects structural entity child folders while allowing their contents",
            fun () ->
                withAssayArc (fun arcPath -> promise {
                    match!
                        ArcFileSystemHelper.createFileSystemItemOnDisk
                            arcPath
                            (createItemRequest "assays/AssayA" "dataset" FileSystemItemKind.Folder)
                    with
                    | Ok createdPath -> failwith $"Expected structural folder creation to be rejected: {createdPath}"
                    | Error error -> Vitest.expect(error.Message.Length).toBeGreaterThan (0)

                    do! createRelativeDirectoryAsync arcPath "assays/AssayA/dataset"

                    let! createdPath =
                        createItemOrFail
                            arcPath
                            (createItemRequest "assays/AssayA/dataset" "raw.txt" FileSystemItemKind.File)

                    Vitest.expect(createdPath).toBe ("assays/AssayA/dataset/raw.txt")
                    do! expectRelativePathExists arcPath createdPath true

                    match! ArcFileSystemHelper.deleteGenericFileSystemItemOnDisk arcPath "assays/AssayA/dataset" with
                    | Ok() -> failwith "Expected structural folder deletion to be rejected."
                    | Error error -> Vitest.expect(error.Message.Length).toBeGreaterThan (0)

                    match!
                        ArcFileSystemHelper.renameGenericFileSystemItemOnDisk
                            arcPath
                            (renameRequest "assays/AssayA/dataset" "raw-data")
                    with
                    | Ok() -> failwith "Expected structural folder rename to be rejected."
                    | Error error -> Vitest.expect(error.Message.Length).toBeGreaterThan (0)

                    do! deleteItemOrFail arcPath createdPath
                    do! expectRelativePathExists arcPath createdPath false
                })
        )

        Vitest.test (
            "creates generic files and folders at the ARC root",
            fun () ->
                withAssayArc (fun arcPath -> promise {
                    let! folderPath = createItemOrFail arcPath (createItemRequest "" "docs" FileSystemItemKind.Folder)

                    Vitest.expect(folderPath).toBe ("docs")

                    let! isDirectory =
                        ARCtrl.FileSystemHelper.directoryExistsAsync (absoluteArcPath arcPath folderPath)

                    Vitest.expect(isDirectory).toBe (true)

                    let! filePath = createItemOrFail arcPath (createItemRequest "" "notes.txt" FileSystemItemKind.File)

                    Vitest.expect(filePath).toBe ("notes.txt")

                    let! isFile = ARCtrl.FileSystemHelper.fileExistsAsync (absoluteArcPath arcPath filePath)

                    Vitest.expect(isFile).toBe (true)
                })
        )

        Vitest.test (
            "creates missing README files and keeps them protected",
            fun () ->
                withAssayArc (fun arcPath -> promise {
                    let! rootReadmePath =
                        createItemOrFail arcPath (createItemRequest "" "README.md" FileSystemItemKind.File)

                    Vitest.expect(rootReadmePath).toBe ("README.md")
                    do! expectRelativePathExists arcPath rootReadmePath true

                    do! createRelativeDirectoryAsync arcPath "assays/AssayA/attachments"

                    let! assayReadmePath =
                        createItemOrFail
                            arcPath
                            (createItemRequest "assays/AssayA/attachments" "README.md" FileSystemItemKind.File)

                    Vitest.expect(assayReadmePath).toBe ("assays/AssayA/attachments/README.md")
                    do! expectRelativePathExists arcPath assayReadmePath true

                    for readmePath in [ rootReadmePath; assayReadmePath ] do
                        let! deleteResult = ArcFileSystemHelper.deleteGenericFileSystemItemOnDisk arcPath readmePath
                        expectErrorContaining "protected files" deleteResult
                        do! expectRelativePathExists arcPath readmePath true

                    let! existingReadmeResult =
                        ArcFileSystemHelper.createFileSystemItemOnDisk
                            arcPath
                            (createItemRequest "" "README.md" FileSystemItemKind.File)

                    expectErrorContaining "already exists" existingReadmeResult
                })
        )

        Vitest.test (
            "creates, renames, and deletes files directly inside an add zone without touching entities",
            fun () ->
                withAssayArc (fun arcPath -> promise {
                    let! entityFolderResult =
                        ArcFileSystemHelper.createFileSystemItemOnDisk
                            arcPath
                            (createItemRequest "assays" "NewAssay" FileSystemItemKind.Folder)

                    expectErrorContaining "only allowed inside safe ARC directories" entityFolderResult
                    do! expectRelativePathExists arcPath "assays/NewAssay" false

                    let! canonicalFileResult =
                        ArcFileSystemHelper.createFileSystemItemOnDisk
                            arcPath
                            (createItemRequest "assays" "isa.assay.xlsx" FileSystemItemKind.File)

                    expectErrorContaining "must not target canonical ARC files" canonicalFileResult
                    do! expectRelativePathExists arcPath "assays/isa.assay.xlsx" false

                    let! extensionlessFileResult =
                        ArcFileSystemHelper.createFileSystemItemOnDisk
                            arcPath
                            (createItemRequest "assays" "Assay1" FileSystemItemKind.File)

                    expectErrorContaining "file extension" extensionlessFileResult
                    do! expectRelativePathExists arcPath "assays/Assay1" false

                    let! createdPath =
                        createItemOrFail arcPath (createItemRequest "assays" "overview.md" FileSystemItemKind.File)

                    Vitest.expect(createdPath).toBe ("assays/overview.md")

                    let! isAddZoneRootFile = ArcFileSystemHelper.isAddZoneRootFileAsync arcPath createdPath
                    Vitest.expect(isAddZoneRootFile).toBe (true)

                    let! isEntityFolderFile = ArcFileSystemHelper.isAddZoneRootFileAsync arcPath "assays/AssayA"
                    Vitest.expect(isEntityFolderFile).toBe (false)

                    do! renameItemOrFail arcPath (renameRequest createdPath "summary.md")
                    do! expectRelativePathExists arcPath createdPath false
                    do! expectRelativePathExists arcPath "assays/summary.md" true

                    do! deleteItemOrFail arcPath "assays/summary.md"
                    do! expectRelativePathExists arcPath "assays/summary.md" false

                    let! entityDeleteResult =
                        ArcFileSystemHelper.deleteGenericFileSystemItemOnDisk arcPath "assays/AssayA"

                    expectErrorContaining "safe non-entity paths" entityDeleteResult
                    do! expectRelativePathExists arcPath "assays/AssayA" true

                    let! reloadedArc = TestHelpers.loadArcAsync arcPath
                    Vitest.expect(reloadedArc.ContainsAssay("AssayA")).toBe (true)
                })
        )

        Vitest.test (
            "routes deletes of files inside an add zone to the generic delete instead of the entity delete",
            fun () ->
                withAssayArc (fun arcPath -> promise {
                    do! writeRelativeFileAsync arcPath "assays/overview.md" "overview"
                    do! writeRelativeFileAsync arcPath "assays/AssayA/protocol.md" "protocol"

                    do!
                        expectDeleteRoute
                            arcPath
                            "assays/overview.md"
                            (ArcFileSystemHelper.DeletePathRoute.GenericItem "assays/overview.md")

                    do!
                        expectDeleteRoute
                            arcPath
                            "assays/AssayA"
                            (ArcFileSystemHelper.DeletePathRoute.ArcEntity "assays/AssayA")

                    // A missing path keeps the entity route, so only real files take the generic route.
                    do!
                        expectDeleteRoute
                            arcPath
                            "assays/MissingAssay"
                            (ArcFileSystemHelper.DeletePathRoute.ArcEntity "assays/MissingAssay")

                    do!
                        expectDeleteRoute
                            arcPath
                            "assays/AssayA/isa.assay.xlsx"
                            (ArcFileSystemHelper.DeletePathRoute.ArcEntity "assays/AssayA/isa.assay.xlsx")

                    do!
                        expectDeleteRoute
                            arcPath
                            "assays/AssayA/protocol.md"
                            (ArcFileSystemHelper.DeletePathRoute.GenericItem "assays/AssayA/protocol.md")

                    let! investigationRoute =
                        ArcFileSystemHelper.resolveDeletePathRouteAsync arcPath "isa.investigation.xlsx"

                    match investigationRoute with
                    | ArcFileSystemHelper.DeletePathRoute.Rejected _ -> ()
                    | route -> failwith $"Expected investigation delete to be rejected, but it routed to {route}."

                    let! readmeRoute =
                        ArcFileSystemHelper.resolveDeletePathRouteAsync arcPath "assays/AssayA/README.md"

                    match readmeRoute with
                    | ArcFileSystemHelper.DeletePathRoute.Rejected _ -> ()
                    | route -> failwith $"Expected README delete to be rejected, but it routed to {route}."

                    // A file with a canonical ARC name directly inside an add zone is neither an entity nor a generic file.
                    do! writeRelativeFileAsync arcPath "assays/isa.investigation.xlsx" ""

                    let! canonicalNameRoute =
                        ArcFileSystemHelper.resolveDeletePathRouteAsync arcPath "assays/isa.investigation.xlsx"

                    match canonicalNameRoute with
                    | ArcFileSystemHelper.DeletePathRoute.Rejected _ -> ()
                    | route ->
                        failwith $"Expected canonical-name file delete to be rejected, but it routed to {route}."
                })
        )

        Vitest.test (
            "routes renames of files inside an add zone to the generic rename instead of the entity rename",
            fun () ->
                withAssayArc (fun arcPath -> promise {
                    do! writeRelativeFileAsync arcPath "assays/overview.md" "overview"

                    do! expectGenericRename arcPath "assays/overview.md" true
                    do! expectGenericRename arcPath "assays/AssayA" false
                    do! expectGenericRename arcPath "assays/MissingAssay" false
                    do! expectGenericRename arcPath "assays/AssayA/protocol.md" true
                })
        )

        Vitest.test (
            "renames dot files directly inside an add zone to regular file names",
            fun () ->
                withAssayArc (fun arcPath -> promise {
                    do! writeRelativeFileAsync arcPath "assays/.DS_Store" ""

                    let! isAddZoneRootFile = ArcFileSystemHelper.isAddZoneRootFileAsync arcPath "assays/.DS_Store"
                    Vitest.expect(isAddZoneRootFile).toBe (true)

                    do! renameItemOrFail arcPath (renameRequest "assays/.DS_Store" "notes.md")
                    do! expectRelativePathExists arcPath "assays/.DS_Store" false
                    do! expectRelativePathExists arcPath "assays/notes.md" true
                })
        )

        Vitest.test (
            "handles files directly inside every add zone on disk",
            fun () ->
                withAssayArc (fun arcPath -> promise {
                    for zone in [ "studies"; "assays"; "workflows"; "runs" ] do
                        do! createRelativeDirectoryAsync arcPath zone

                        // New files need an extension and then use the generic operations.
                        let! extensionlessResult =
                            ArcFileSystemHelper.createFileSystemItemOnDisk
                                arcPath
                                (createItemRequest zone "Entity1" FileSystemItemKind.File)

                        expectErrorContaining "file extension" extensionlessResult
                        do! expectRelativePathExists arcPath $"{zone}/Entity1" false

                        let! overviewPath =
                            createItemOrFail arcPath (createItemRequest zone "overview.md" FileSystemItemKind.File)

                        do!
                            expectDeleteRoute
                                arcPath
                                overviewPath
                                (ArcFileSystemHelper.DeletePathRoute.GenericItem overviewPath)

                        do! expectGenericRename arcPath overviewPath true
                        do! renameItemOrFail arcPath (renameRequest overviewPath "summary.md")
                        do! deleteItemOrFail arcPath $"{zone}/summary.md"
                        do! expectRelativePathExists arcPath $"{zone}/summary.md" false

                        // An existing extensionless file can still be cleaned up.
                        do! writeRelativeFileAsync arcPath $"{zone}/LICENSE" "license"

                        do!
                            expectDeleteRoute
                                arcPath
                                $"{zone}/LICENSE"
                                (ArcFileSystemHelper.DeletePathRoute.GenericItem $"{zone}/LICENSE")

                        do! renameItemOrFail arcPath (renameRequest $"{zone}/LICENSE" "LICENSE.md")
                        do! deleteItemOrFail arcPath $"{zone}/LICENSE.md"
                        do! expectRelativePathExists arcPath $"{zone}/LICENSE.md" false

                        // A dot file can become a regular file but not an entity-shaped path.
                        do! writeRelativeFileAsync arcPath $"{zone}/.DS_Store" ""

                        let! entityShapedRenameResult =
                            ArcFileSystemHelper.renameGenericFileSystemItemOnDisk
                                arcPath
                                (renameRequest $"{zone}/.DS_Store" "Entity1")

                        expectErrorContaining "file extension" entityShapedRenameResult
                        do! expectRelativePathExists arcPath $"{zone}/.DS_Store" true
                        do! expectRelativePathExists arcPath $"{zone}/Entity1" false

                        do! renameItemOrFail arcPath (renameRequest $"{zone}/.DS_Store" "notes.md")
                        do! expectRelativePathExists arcPath $"{zone}/notes.md" true
                })
        )

        Vitest.test (
            "renames generic files and rejects destination conflicts",
            fun () ->
                withAssayArc (fun arcPath -> promise {
                    do! writeRelativeFileAsync arcPath "assays/AssayA/old.txt" "old"

                    do! renameItemOrFail arcPath (renameRequest "assays/AssayA/old.txt" "new.txt")

                    do! expectRelativePathExists arcPath "assays/AssayA/old.txt" false
                    do! expectRelativePathExists arcPath "assays/AssayA/new.txt" true

                    do! writeRelativeFileAsync arcPath "assays/AssayA/conflict.txt" "conflict"

                    match!
                        ArcFileSystemHelper.renameGenericFileSystemItemOnDisk
                            arcPath
                            (renameRequest "assays/AssayA/new.txt" "conflict.txt")
                    with
                    | Ok _ -> failwith "Expected generic rename conflict to fail."
                    | Error error -> Vitest.expect(error.Message).toContain ("destination already exists")
                })
        )

        Vitest.test (
            "deletes generic files while leaving the ARC entity intact",
            fun () ->
                withAssayArc (fun arcPath -> promise {
                    let createdFilePath = "assays/AssayA/protocol.md"
                    do! writeRelativeFileAsync arcPath createdFilePath "protocol"
                    do! expectRelativePathExists arcPath createdFilePath true

                    do! deleteItemOrFail arcPath createdFilePath
                    do! expectRelativePathExists arcPath createdFilePath false

                    let! reloadedArc = TestHelpers.loadArcAsync arcPath
                    Vitest.expect(reloadedArc.ContainsAssay("AssayA")).toBe (true)
                })
        )

        Vitest.test (
            "moves generic note folders with nested assets and requires overwrite for conflicts",
            fun () ->
                withAssayArc (fun arcPath -> promise {
                    let sourceFolder = "notes/2026-06-15/Field_observations"
                    let targetFolder = "assays/AssayA/protocols/Sampling_protocol"

                    do! createRelativeDirectoryAsync arcPath $"{sourceFolder}/assets"
                    do! writeRelativeFileAsync arcPath $"{sourceFolder}/Field_observations.md" "source"
                    do! writeRelativeFileAsync arcPath $"{sourceFolder}/assets/image.txt" "asset"

                    do! moveItemOrFail arcPath (moveRequest sourceFolder targetFolder false)

                    do! expectRelativePathExists arcPath sourceFolder false
                    do! expectRelativePathExists arcPath $"{targetFolder}/Field_observations.md" true
                    do! expectRelativePathExists arcPath $"{targetFolder}/assets/image.txt" true

                    let secondSourceFolder = "notes/2026-06-16/Field_observations"
                    do! createRelativeDirectoryAsync arcPath secondSourceFolder
                    do! writeRelativeFileAsync arcPath $"{secondSourceFolder}/Field_observations.md" "replacement"

                    match!
                        ArcFileSystemHelper.moveGenericFileSystemItemOnDisk
                            arcPath
                            (moveRequest secondSourceFolder targetFolder false)
                    with
                    | Ok() -> failwith "Expected move without overwrite to reject an existing target."
                    | Error error -> Vitest.expect(error.Message).toContain ("destination already exists")

                    do! moveItemOrFail arcPath (moveRequest secondSourceFolder targetFolder true)
                    do! expectRelativePathExists arcPath secondSourceFolder false
                    do! expectRelativePathExists arcPath $"{targetFolder}/Field_observations.md" true
                })
        )

        Vitest.test (
            "moves generic files into descendant paths",
            fun () ->
                withAssayArc (fun arcPath -> promise {
                    let sourceFile = "notes/raw"
                    let targetFile = "notes/raw/archive/raw.txt"

                    do! createRelativeDirectoryAsync arcPath "notes"
                    do! writeRelativeFileAsync arcPath sourceFile "raw"

                    do! moveItemOrFail arcPath (moveRequest sourceFile targetFile false)

                    let! sourceFileStillExists =
                        ARCtrl.FileSystemHelper.fileExistsAsync (absoluteArcPath arcPath sourceFile)

                    let! sourcePathIsDirectory =
                        ARCtrl.FileSystemHelper.directoryExistsAsync (absoluteArcPath arcPath sourceFile)

                    Vitest.expect(sourceFileStillExists).toBe (false)
                    Vitest.expect(sourcePathIsDirectory).toBe (true)
                    do! expectRelativePathExists arcPath targetFile true
                })
        )

        Vitest.test (
            "rejects moving generic folders into descendant paths",
            fun () ->
                withAssayArc (fun arcPath -> promise {
                    let sourceFolder = "notes/folder"
                    let targetFolder = "notes/folder/archive/folder"

                    do! createRelativeDirectoryAsync arcPath sourceFolder

                    match!
                        ArcFileSystemHelper.moveGenericFileSystemItemOnDisk
                            arcPath
                            (moveRequest sourceFolder targetFolder false)
                    with
                    | Ok() -> failwith "Expected moving a folder into itself to fail."
                    | Error error -> Vitest.expect(error.Message).toContain ("inside the source path")

                    do! expectRelativePathExists arcPath sourceFolder true
                    do! expectRelativePathExists arcPath targetFolder false
                })
        )

        Vitest.test (
            "retries transient recursive remove errors",
            fun () -> promise {
                let mutable attempts = 0
                let targetPath = "/tmp/swate-retry-target"

                let removePathAsync path = promise {
                    attempts <- attempts + 1
                    Vitest.expect(path).toBe (targetPath)

                    if attempts < 3 then
                        return raise (nodeError "directory is still changing" "ENOTEMPTY")
                }

                match! removePathWithRetriesAsync removePathAsync targetPath with
                | Error error -> return failwith error.Message
                | Ok() ->
                    Vitest.expect(attempts).toBe (3)
                    return ()
            }
        )
)
