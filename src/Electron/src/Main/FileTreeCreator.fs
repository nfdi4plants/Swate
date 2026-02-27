[<AutoOpen>]
module Main.FileTreeCreator

open Swate.Electron.Shared
open Fable.Core.JsInterop
open Swate.Electron.Shared.IPCTypes

let fs: obj = importAll "fs"
let pathMod: obj = importAll "path"
let gitLfs: IPC.GitLfs.IGitLfs = IPC.GitLfs.gitLfs

let private isLfsTracked (repoRoot: string) (absolutePath: string) =
    let relativePath =
        pathMod?relative (repoRoot, absolutePath)
        |> unbox<string>
        |> fun p -> p.Replace("\\", "/")

    if System.String.IsNullOrWhiteSpace relativePath || relativePath = "." then
        false
    else
        gitLfs.IsTrackedByAttributes repoRoot relativePath

let getFileEntry (path: string) = promise {
    let! stats = fs?promises?stat (path)
    return IPCTypes.FileEntry.create (pathMod?basename (path), path, stats?isDirectory (), None)
}

/// Finds all files and subfolders of the given filepath
let getFileEntries (path: string) =
    let repoRoot = path.Replace("\\", "/")

    let rec loop (currentPath: string) =
        let stat = fs?statSync (currentPath)
        let isDir = stat?isDirectory ()
        let name = pathMod?basename (currentPath)
        let normalizedPath = currentPath.Replace("\\", "/")

        let isLfs =
            if isDir then
                None
            else
                Some(isLfsTracked repoRoot normalizedPath)

        let currentEntry = IPCTypes.FileEntry.create (name, normalizedPath, isDir, isLfs)

        if isDir then
            let entries: string[] = fs?readdirSync (normalizedPath)

            let children =
                entries
                |> Array.collect (fun entry ->
                    let fullPath = pathMod?join (normalizedPath, entry)
                    loop fullPath
                )

            Array.append [| currentEntry |] children
        else
            [| currentEntry |]

    loop repoRoot
