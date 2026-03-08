[<AutoOpen>]
module Main.FileTreeCreator

open Swate.Electron.Shared
open System.Collections.Generic
open Fable.Core.JsInterop
open Swate.Electron.Shared.IPCTypes

let fs: obj = importAll "fs"
let pathMod: obj = importAll "path"
let childProcessDynamic: obj = importAll "node:child_process"

let private normalizePath (path: string) = path.Replace("\\", "/")

let private normalizeRootPath (path: string) =
    pathMod?resolve(path) |> unbox<string> |> normalizePath

let private shouldIgnoreDirName (name: string) =
    name = ".git"

let private shouldIgnorePath (path: string) =
    let normalizedPath = normalizePath path
    let tempXlsxPattern = """\.~\$.*\.xlsx$"""
    System.Text.RegularExpressions.Regex.IsMatch(normalizedPath, tempXlsxPattern)

let private tryGetRepoRelativePath (repoRoot: string) (absolutePath: string) =
    let relativePath =
        pathMod?relative (repoRoot, absolutePath)
        |> unbox<string>
        |> normalizePath

    if System.String.IsNullOrWhiteSpace relativePath || relativePath = "." then
        None
    else
        Some relativePath

let private tryGetLfsTrackedByAttributes
    (repoRoot: string)
    (repoRelativePaths: string[])
    : Fable.Core.JS.Promise<Dictionary<string, bool>> =
    promise {
        let results = Dictionary<string, bool>()

        if repoRelativePaths.Length = 0 then
            return results
        else
            let gitDir = pathMod?join(repoRoot, ".git") |> unbox<string>
            let isGitRepo =
                try
                    fs?existsSync(gitDir) |> unbox<bool>
                with _ ->
                    false

            if not isGitRepo then
                return results
            else
                return!
                    Fable.Core.JS.Constructors.Promise.Create(fun resolve _reject ->
                        try
                            let proc: obj =
                                childProcessDynamic?spawn (
                                    "git",
                                    [| "check-attr"; "-z"; "filter"; "--stdin" |],
                                    createObj [
                                        "cwd" ==> repoRoot
                                        "shell" ==> false
                                        "windowsHide" ==> true
                                    ]
                                )

                            let stdoutChunks = ResizeArray<string>()

                            proc?stdout?on (
                                "data",
                                fun d ->
                                    let msg = d?toString("utf8") |> unbox<string>
                                    stdoutChunks.Add(msg)
                            )
                            |> ignore

                            proc?on (
                                "close",
                                fun code ->
                                    let success =
                                        if isNull code then
                                            false
                                        else
                                            (unbox<float> code) = 0.

                                    if success then
                                        let stdout = System.String.Concat(stdoutChunks.ToArray())
                                        let segments = stdout.Split('\u0000')

                                        let lastIndex =
                                            if segments.Length > 0 && segments.[segments.Length - 1] = "" then
                                                segments.Length - 1
                                            else
                                                segments.Length

                                        let mutable i = 0

                                        while i + 2 < lastIndex do
                                            let path = segments.[i]
                                            let attr = segments.[i + 1]
                                            let value = segments.[i + 2]

                                            if attr = "filter" then
                                                results.[path] <- value = "lfs"

                                            i <- i + 3

                                    resolve results
                            )
                            |> ignore

                            proc?on ("error", fun _ -> resolve results) |> ignore

                            repoRelativePaths
                            |> Array.iter (fun relativePath -> proc?stdin?write(relativePath + "\u0000") |> ignore)

                            proc?stdin?``end`` () |> ignore
                        with _ ->
                            resolve results
                    )
    }

let getFileEntry (path: string) = promise {
    let! stats = fs?promises?stat (path)
    return IPCTypes.FileEntry.create (pathMod?basename (path), path, stats?isDirectory (), None)
}

/// Finds all files and subfolders of the given filepath
let getFileEntries (path: string) : Fable.Core.JS.Promise<FileEntry[]> =
    promise {
        let repoRoot = normalizeRootPath path

        let! rootStats = fs?promises?stat (repoRoot)
        let rootIsDir = rootStats?isDirectory () |> unbox<bool>

        let rootName = pathMod?basename (repoRoot) |> unbox<string>
        let rootEntry = IPCTypes.FileEntry.create (rootName, repoRoot, rootIsDir, None)

        if not rootIsDir then
            return [| rootEntry |]
        else
            let stack = ResizeArray<string>()
            stack.Add(repoRoot)

            let entries = ResizeArray<FileEntry>()
            entries.Add(rootEntry)

            while stack.Count > 0 do
                let currentDir = stack.[stack.Count - 1]
                stack.RemoveAt(stack.Count - 1)

                let! dirents =
                    fs?promises?readdir (currentDir, createObj [ "withFileTypes" ==> true ])
                    |> unbox<Fable.Core.JS.Promise<obj[]>>

                dirents
                |> Array.iter (fun dirent ->
                    let name = dirent?name |> unbox<string>
                    let isDir = dirent?isDirectory () |> unbox<bool>

                    if isDir then
                        if not (shouldIgnoreDirName name) then
                            let fullPath = pathMod?join (currentDir, name) |> unbox<string> |> normalizePath
                            entries.Add(IPCTypes.FileEntry.create (name, fullPath, true, None))
                            stack.Add(fullPath)
                    else
                        let fullPath = pathMod?join (currentDir, name) |> unbox<string> |> normalizePath

                        if not (shouldIgnorePath fullPath) then
                            entries.Add(IPCTypes.FileEntry.create (name, fullPath, false, Some false))
                )

            let repoRelativeFilePaths =
                entries.ToArray()
                |> Array.choose (fun entry ->
                    if entry.isDirectory then
                        None
                    else
                        tryGetRepoRelativePath repoRoot entry.path
                )

            let! lfsTracked = tryGetLfsTrackedByAttributes repoRoot repoRelativeFilePaths

            return
                entries.ToArray()
                |> Array.map (fun entry ->
                    if entry.isDirectory then
                        entry
                    else
                        match tryGetRepoRelativePath repoRoot entry.path with
                        | None -> entry
                        | Some relativePath ->
                            let tracked =
                                match lfsTracked.TryGetValue(relativePath) with
                                | true, value -> value
                                | _ -> false

                            { entry with isLfs = Some tracked }
                )
    }
