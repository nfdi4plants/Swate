module ElectronCore.ArcFileSystemHelperTests

open System
open Fable.Core
open Fable.Core.JsInterop
open Main.Bindings.Path
open Main.IPC.FileSystemIO
open Swate.Components.Shared
open Swate.Electron.Shared.FileIOTypes
open Vitest

let private fsPromisesDynamic: obj = importAll "fs/promises"
let private fsDynamic: obj = importAll "fs"

[<Emit("Object.assign(new Error($0), { code: $1 })")>]
let private nodeError (message: string) (code: string) : exn = jsNative

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

Vitest.describe (
    "ArcFileSystemHelper generic filesystem operations",
    fun () ->

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
                    let firstName = $"cancel-first-{Guid.NewGuid():N}.txt"
                    let secondName = $"cancel-second-{Guid.NewGuid():N}.txt"
                    do! writeRelativeFileAsync sourceDirectory firstName "first"
                    do! writeRelativeFileAsync sourceDirectory secondName "second"

                    let mutable cancelRequested = false
                    let mutable sawTemporaryImportInsideArc = false
                    let mutable sawTemporaryImportNextToArc = false

                    let onProgress progress =
                        let targetEntries =
                            fsDynamic?readdirSync (absoluteArcPath arcPath "assays/AssayA")
                            |> unbox<string[]>

                        let arcParentEntries = fsDynamic?readdirSync (dirname arcPath) |> unbox<string[]>

                        sawTemporaryImportInsideArc <-
                            sawTemporaryImportInsideArc
                            || (targetEntries |> Array.exists _.StartsWith(".swate-import-"))

                        sawTemporaryImportNextToArc <-
                            sawTemporaryImportNextToArc
                            || (arcParentEntries |> Array.exists _.StartsWith(".swate-import-"))

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
                    with
                    | Error error -> failwith error.Message
                    | Ok ImportExternalFilesResult.Completed -> failwith "Expected import cancellation."
                    | Ok ImportExternalFilesResult.Cancelled ->
                        let targetDirectory = absoluteArcPath arcPath "assays/AssayA"
                        let! firstExists = pathExistsAsync (join [| targetDirectory; firstName |])
                        let! secondExists = pathExistsAsync (join [| targetDirectory; secondName |])
                        let! entries = fsPromisesDynamic?readdir targetDirectory |> unbox<JS.Promise<string[]>>

                        Vitest.expect(firstExists).toBe (false)
                        Vitest.expect(secondExists).toBe (false)
                        Vitest.expect(sawTemporaryImportInsideArc).toBe (true)
                        Vitest.expect(sawTemporaryImportNextToArc).toBe (false)
                        Vitest.expect(entries |> Array.exists _.StartsWith(".swate-import-")).toBe (false)
                })
        )

        Vitest.test (
            "cleans temporary import files when a source copy fails",
            fun () ->
                withAssayArc (fun arcPath -> promise {
                    let sourceDirectory = dirname arcPath
                    let firstName = $"error-first-{Guid.NewGuid():N}.txt"
                    let missingName = $"missing-{Guid.NewGuid():N}.txt"
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
                    with
                    | Ok outcome -> failwith $"Expected import failure, got {outcome}."
                    | Error _ ->
                        let targetDirectory = absoluteArcPath arcPath "assays/AssayA"
                        let! firstExists = pathExistsAsync (join [| targetDirectory; firstName |])
                        let! entries = fsPromisesDynamic?readdir targetDirectory |> unbox<JS.Promise<string[]>>

                        Vitest.expect(firstExists).toBe (false)
                        Vitest.expect(entries |> Array.exists _.StartsWith(".swate-import-")).toBe (false)
                })
        )

        Vitest.test (
            "does not overwrite an existing destination file",
            fun () ->
                withAssayArc (fun arcPath -> promise {
                    let fileName = $"existing-{Guid.NewGuid():N}.txt"
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
