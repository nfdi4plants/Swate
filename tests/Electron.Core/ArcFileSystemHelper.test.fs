module ElectronCore.ArcFileSystemHelperTests

open System
open Fable.Core
open Main.Bindings.Path
open Main.IPC.ArcVaultsApi
open Swate.Components.Shared
open Swate.Electron.Shared.FileIOTypes
open ARCtrl
open Vitest

let private assayParent = "assays/AssayA"

let private withAssayArc =
    TestHelpers.withTempArcWith
        "swate-generic-fs-"
        "GenericFileSystemArc"
        (fun arc -> arc.InitAssay("AssayA") |> ignore)

let private createRequest kind name = {
    parentPath = assayParent
    name = name
    kind = kind
}

let private createFileRequest name =
    createRequest FileSystemItemKind.File name

let private createFolderRequest name =
    createRequest FileSystemItemKind.Folder name

let private renameRequest relativePath newName = {
    relativePath = relativePath
    newName = newName
}

let private absoluteArcPath arcPath relativePath =
    relativePath
    |> PathHelpers.normalizePath
    |> fun path -> path.Split([| '/' |], StringSplitOptions.RemoveEmptyEntries)
    |> Array.append [| arcPath |]
    |> join

let private createItemOrFail arcPath request = promise {
    match! ArcFileSystemHelper.createFileSystemItemOnDisk arcPath request with
    | Error error -> return failwith error.Message
    | Ok createdPath -> return createdPath
}

let private renameItemOrFail arcPath request = promise {
    match! ArcFileSystemHelper.renameGenericFileSystemItemOnDisk arcPath request with
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
    Vitest.expect(exists).toBe(expected)
}

Vitest.describe("ArcFileSystemHelper generic filesystem operations", fun () ->
    Vitest.test("creates files and folders without changing ARC entities", fun () ->
        withAssayArc (fun arcPath -> promise {
            let! createdFilePath = createItemOrFail arcPath (createFileRequest "protocol.md")
            Vitest.expect(createdFilePath).toBe("assays/AssayA/protocol.md")
            do! expectRelativePathExists arcPath createdFilePath true

            let! createdFolderPath = createItemOrFail arcPath (createFolderRequest "resources")
            Vitest.expect(createdFolderPath).toBe("assays/AssayA/resources")
            do! expectRelativePathExists arcPath createdFolderPath true

            let! reloadedArc = TestHelpers.loadArcAsync arcPath
            Vitest.expect(reloadedArc.ContainsAssay("AssayA")).toBe(true)
        }))

    Vitest.test("rejects generic create conflicts", fun () ->
        withAssayArc (fun arcPath -> promise {
            let request = createFileRequest "protocol.md"
            let! _ = createItemOrFail arcPath request

            match! ArcFileSystemHelper.createFileSystemItemOnDisk arcPath request with
            | Ok _ -> failwith "Expected duplicate generic file creation to fail."
            | Error error -> Vitest.expect(error.Message).toContain("already exists")
        }))

    Vitest.test("renames generic files and rejects destination conflicts", fun () ->
        withAssayArc (fun arcPath -> promise {
            let! _ = createItemOrFail arcPath (createFileRequest "old.txt")

            do!
                renameItemOrFail
                    arcPath
                    (renameRequest "assays/AssayA/old.txt" "new.txt")

            do! expectRelativePathExists arcPath "assays/AssayA/old.txt" false
            do! expectRelativePathExists arcPath "assays/AssayA/new.txt" true

            let! _ = createItemOrFail arcPath (createFileRequest "conflict.txt")

            match!
                ArcFileSystemHelper.renameGenericFileSystemItemOnDisk
                    arcPath
                    (renameRequest "assays/AssayA/new.txt" "conflict.txt")
            with
            | Ok _ -> failwith "Expected generic rename conflict to fail."
            | Error error -> Vitest.expect(error.Message).toContain("destination already exists")
        }))

    Vitest.test("deletes generic files while leaving the ARC entity intact", fun () ->
        withAssayArc (fun arcPath -> promise {
            let! createdFilePath = createItemOrFail arcPath (createFileRequest "protocol.md")
            do! expectRelativePathExists arcPath createdFilePath true

            do! deleteItemOrFail arcPath createdFilePath
            do! expectRelativePathExists arcPath createdFilePath false

            let! reloadedArc = TestHelpers.loadArcAsync arcPath
            Vitest.expect(reloadedArc.ContainsAssay("AssayA")).toBe(true)
        }))
)
