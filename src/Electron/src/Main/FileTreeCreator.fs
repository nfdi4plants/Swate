[<AutoOpen>]
module Main.FileTreeCreator

open Swate.Electron.Shared
open Fable.Core.JsInterop
open Swate.Electron.Shared.IPCTypes

let fs: obj = importAll "fs"
let pathMod: obj = importAll "path"

let getFileEntry (path: string) =
    promise {
        let! stats = fs?promises?stat(path)
        return
            IPCTypes.FileEntry.create(
                pathMod?basename(path),
                path,
                stats?isDirectory()
            )
    }

///Finds all files and subfolders of the given filepath
let rec getFileEntries path =
    let entries: string[] = fs?readdirSync(path)
    entries
    |> Array.collect (fun entry ->
        let fullPath = pathMod?join(path, entry)
        let isDir = fs?statSync(fullPath)?isDirectory()
        let currentEntry = IPCTypes.FileEntry.create(entry?name, fullPath, isDir)
        if isDir then
            Array.append [| currentEntry |] (getFileEntries fullPath)
        else
            [| currentEntry |]
    )