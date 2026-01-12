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

let getFiles path =
    promise {
        let! filePaths = fs?promises?readdir(path, {| withFileTypes = true |})
        let entries =
            filePaths
            |> Array.map (fun entry ->
                IPCTypes.FileEntry.create(
                    entry?name,
                    pathMod?join(path, entry?name),
                    entry?isDirectory()
                )
            )
        return entries
    }

///Finds all files and subfolders of the given filepath and uses the file located in the path as the parent
let rec getFileTree path =
    promise {

        let! stats = fs?promises?stat(path)

        if(stats?isDirectory()) then
            let! filePaths = fs?promises?readdir(path, {| withFileTypes = true |})
            let children =
                filePaths
                |> Array.map (fun entry ->
                    getFileTree (pathMod?join(path, entry?name))
                )
            let! children = Promise.all(children)
            return
                IPCTypes.FileEntry.create(
                    pathMod?basename(path),
                    path,
                    true,
                    children
                )
        else
            return
                IPCTypes.FileEntry.create(
                    pathMod?basename(path),
                    path,
                    false
                )
    }
