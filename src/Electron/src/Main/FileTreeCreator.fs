[<AutoOpen>]
module Main.FileTreeCreator

open Swate.Electron.Shared
open Fable.Core.JsInterop
open Swate.Electron.Shared.IPCTypes

let fs: obj = importAll "fs"
let pathMod: obj = importAll "path"
let childProcessDynamic: obj = importAll "node:child_process"

let private tryGetLfsTrackedPaths (repoPath: string) =
    try
        let output: string =
            childProcessDynamic?execSync(
                "git lfs ls-files --name-only",
                createObj [ "cwd" ==> repoPath; "encoding" ==> "utf8"; "stdio" ==> "pipe" ]
            )
            |> unbox<string>

        output.Split([| '\n'; '\r' |], System.StringSplitOptions.RemoveEmptyEntries)
        |> Array.map (fun relPath ->
            pathMod?resolve(repoPath, relPath) |> unbox<string>
        )
        |> Array.map (fun p -> p.Replace("\\", "/"))
        |> Set.ofArray
    with _ ->
        Set.empty

let getFileEntry (path: string) =
    promise {
        let! stats = fs?promises?stat(path)
        return
            IPCTypes.FileEntry.create(
                pathMod?basename(path),
                path,
                stats?isDirectory(),
                None
            )
    }

///Finds all files and subfolders of the given filepath
let getFileEntries (path: string) =
    let lfsTrackedPaths = tryGetLfsTrackedPaths path

    let rec loop (path: string) =
        let stat = fs?statSync(path)
        let isDir = stat?isDirectory()
        let name = pathMod?basename(path)

        let normalizedPath = path.Replace("\\", "/")

        let currentEntry =
            IPCTypes.FileEntry.create(name, normalizedPath, isDir, Some(lfsTrackedPaths.Contains normalizedPath))

        if isDir then
            let entries: string[] = fs?readdirSync(normalizedPath)

            let children =
                entries
                |> Array.collect (fun entry ->
                    let fullPath = pathMod?join(normalizedPath, entry)
                    loop fullPath
                )

            Array.append [| currentEntry |] children
        else
            [| currentEntry |]

    loop path
