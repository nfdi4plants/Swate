module ElectronCore.ArcFileSystemHelperTests

open System
open Fable.Core
open Fable.Core.JsInterop
open Main.Bindings.Path
open Main.IPC.FileSystemIO
open ARCitISA.Components.Shared
open ARCitISA.Electron.Shared.FileIOTypes
open Vitest

let private fsPromisesDynamic: obj = importAll "fs/promises"
let private newGuidN () = Guid.NewGuid().ToString("N")

[<Emit("Object.assign(new Error($0), { code: $1 })")>]
let private nodeError (message: string) (code: string) : exn = jsNative

let private withAssayArc =
    TestHelpers.withTempArcWith
        "arcitisa-generic-fs-"
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
                let exactPattern = """^\.arcitisa-import-[0-9a-f]{32}$"""

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
                        Vitest.expect(targetEntries |> Array.exists _.StartsWith(".arcitisa-import-")).toBe (false)
                        Vitest.expect(arcParentEntries |> Array.exists _.StartsWith(".arcitisa-import-")).toBe (false)
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
            "cleans temporary import files when a source copy fails",
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
                            acceptImportedFiles
                    with
                    | Ok outcome -> failwith $"Expected import failure, got {outcome}."
                    | Error _ ->
                        let targetDirectory = absoluteArcPath arcPath "assays/AssayA"
                        let! firstExists = pathExistsAsync (join [| targetDirectory; firstName |])
                        let! entries = fsPromisesDynamic?readdir targetDirectory |> unbox<JS.Promise<string[]>>

                        Vitest.expect(firstExists).toBe (false)
                        Vitest.expect(entries |> Array.exists _.StartsWith(".arcitisa-import-")).toBe (false)
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
                            (fun () -> lifecycleOrder.Add "published")
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
                            acceptImportedFiles
                    with
                    | Ok outcome -> failwith $"Expected destination conflict, got {outcome}."
                    | Error _ ->
                        let destinationPath = absoluteArcPath arcPath $"assays/AssayA/{fileName}"

                        let! content =
                            fsPromisesDynamic?readFile (destinationPath, "utf8")
                            |> unbox<JS.Promise<string>>

                        Vitest.expect(content).toBe ("original content")
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
                let targetPath = "/tmp/arcitisa-retry-target"

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
