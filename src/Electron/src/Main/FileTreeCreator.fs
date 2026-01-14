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
let rec getFileEntries (path: string) =
    let stat = fs?statSync(path)
    let isDir = stat?isDirectory()
    let name = pathMod?basename(path)

    let normalizedPath = path.Replace("\\", "/")

    let currentEntry =
        IPCTypes.FileEntry.create(name, normalizedPath, isDir)

    if isDir then
        let entries: string[] = fs?readdirSync(normalizedPath)

        let children =
            entries
            |> Array.collect (fun entry ->
                let fullPath = pathMod?join(normalizedPath, entry)
                getFileEntries fullPath
            )

        Array.append [| currentEntry |] children
    else
        [| currentEntry |]