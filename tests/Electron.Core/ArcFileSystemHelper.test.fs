module ElectronCore.ArcFileSystemHelperTests

open System
open Fable.Core
open Fable.Core.JsInterop
open Main.Bindings.Path
open Main.IPC.FileSystemIO
open Swate.Components.Shared
open Swate.Electron.Shared.FileIOTypes
open ARCtrl
open Vitest

let private fsPromisesDynamic: obj = importAll "fs/promises"

let private withAssayArc =
    TestHelpers.withTempArcWith
        "swate-generic-fs-"
        "GenericFileSystemArc"
        (fun arc -> arc.InitAssay("AssayA") |> ignore)

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

let private writeRelativeFileAsync arcPath relativePath content = promise {
    let absolutePath = absoluteArcPath arcPath relativePath
    let! _ = fsPromisesDynamic?writeFile (absolutePath, content, "utf8") |> unbox<JS.Promise<obj>>
    return ()
}

Vitest.describe("ArcFileSystemHelper generic filesystem operations", fun () ->

    Vitest.test("renames generic files and rejects destination conflicts", fun () ->
        withAssayArc (fun arcPath -> promise {
            do! writeRelativeFileAsync arcPath "assays/AssayA/old.txt" "old"

            do!
                renameItemOrFail
                    arcPath
                    (renameRequest "assays/AssayA/old.txt" "new.txt")

            do! expectRelativePathExists arcPath "assays/AssayA/old.txt" false
            do! expectRelativePathExists arcPath "assays/AssayA/new.txt" true

            do! writeRelativeFileAsync arcPath "assays/AssayA/conflict.txt" "conflict"

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
            let createdFilePath = "assays/AssayA/protocol.md"
            do! writeRelativeFileAsync arcPath createdFilePath "protocol"
            do! expectRelativePathExists arcPath createdFilePath true

            do! deleteItemOrFail arcPath createdFilePath
            do! expectRelativePathExists arcPath createdFilePath false

            let! reloadedArc = TestHelpers.loadArcAsync arcPath
            Vitest.expect(reloadedArc.ContainsAssay("AssayA")).toBe(true)
        }))
)
