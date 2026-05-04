[<AutoOpen>]
module Main.FileTreeCreator

open System
open System.Collections.Generic
open Fable.Core.JsInterop
open Main.Git.GitLfsAdapter
open Swate.Electron.Shared.FileIOTypes

let fs: obj = importAll "fs"
let pathMod: obj = importAll "path"

let private normalizePath (path: string) = path.Replace("\\", "/")

let normalizeRootPath (path: string) =
    pathMod?resolve (path) |> unbox<string> |> normalizePath

let private containsTraversalSegments (path: string) =
    path.Split('/') |> Array.exists (fun segment -> segment = "." || segment = "..")

let private shouldIgnoreDirName (name: string) = name = ".git"

let private shouldIgnorePath (path: string) =
    let normalizedPath = normalizePath path
    let tempXlsxPattern = """\.~\$.*\.xlsx$"""
    System.Text.RegularExpressions.Regex.IsMatch(normalizedPath, tempXlsxPattern)

let private tryGetRepoRelativePathCore (repoRoot: string) (absolutePath: string) (allowRoot: bool) =
    let relativePath =
        pathMod?relative (normalizeRootPath repoRoot, normalizeRootPath absolutePath)
        |> unbox<string>
        |> normalizePath

    if System.String.IsNullOrWhiteSpace relativePath || relativePath = "." then
        if allowRoot then Some "" else None
    elif containsTraversalSegments relativePath then
        None
    else
        Some relativePath

let tryGetRepoRelativePath (repoRoot: string) (absolutePath: string) =
    tryGetRepoRelativePathCore repoRoot absolutePath false

let tryGetRepoRelativePathOrRoot (repoRoot: string) (absolutePath: string) =
    tryGetRepoRelativePathCore repoRoot absolutePath true

/// Build the renderer snapshot using ARC-relative dictionary keys and FileEntry paths.
let toRendererFileTree (repoRoot: string) (entries: seq<FileEntry>) : Dictionary<string, FileEntry> =
    let rendererFileTree = Dictionary<string, FileEntry>()

    entries
    |> Seq.iter (fun entry ->
        match tryGetRepoRelativePathOrRoot repoRoot entry.path with
        | Some relativePath -> rendererFileTree.[relativePath] <- { entry with path = relativePath }
        | None -> ()
    )

    rendererFileTree

[<Literal>]
let private lfsLsFilesTimeoutMs = 15000

let private tryReadStringProperty (source: obj) (propertyName: string) =
    let value: obj = source?(propertyName)

    if isNullOrUndefined value then
        None
    else
        try
            Some(unbox<string> value)
        with _ ->
            None

let private tryReadBoolProperty (source: obj) (propertyName: string) =
    let value: obj = source?(propertyName)

    if isNullOrUndefined value then
        None
    else
        try
            Some(unbox<bool> value)
        with _ ->
            None

let private tryReadFloatProperty (source: obj) (propertyName: string) =
    let value: obj = source?(propertyName)

    if isNullOrUndefined value then
        None
    else
        try
            Some(unbox<float> value)
        with _ ->
            None

let private tryDecodeGitLfsLsFileInfo (entryObj: obj) : GitLfsLsFileInfo option =
    match
        tryReadStringProperty entryObj "name",
        tryReadFloatProperty entryObj "size",
        tryReadBoolProperty entryObj "checkout",
        tryReadBoolProperty entryObj "downloaded",
        tryReadStringProperty entryObj "oid_type",
        tryReadStringProperty entryObj "oid",
        tryReadStringProperty entryObj "version"
    with
    | Some name, Some size, Some checkout, Some downloaded, Some oidType, Some oid, Some version ->
        Some {
            name = normalizePath name
            size = size
            checkout = checkout
            downloaded = downloaded
            ``oid_type`` = oidType
            oid = oid
            version = version
        }
    | _ ->
        None

let private parseGitLfsLsFilesByRelativePath (stdoutText: string) : Dictionary<string, GitLfsLsFileInfo> =
    let filesByRelativePath = Dictionary<string, GitLfsLsFileInfo>()

    try
        let parsed: obj = Fable.Core.JS.JSON.parse stdoutText
        let filesObj: obj = parsed?files

        if not (isNullOrUndefined filesObj) then
            let files = unbox<obj[]> filesObj

            files
            |> Array.iter (fun fileObj ->
                match tryDecodeGitLfsLsFileInfo fileObj with
                | Some info when not (String.IsNullOrWhiteSpace info.name) ->
                    let relativePath = normalizePath info.name
                    filesByRelativePath.[relativePath] <- { info with name = relativePath }
                | _ -> ()
            )
    with _ ->
        ()

    filesByRelativePath

let private tryGetGitLfsLsFilesByRelativePath
    (repoRoot: string)
    : Fable.Core.JS.Promise<Dictionary<string, GitLfsLsFileInfo>> =
    promise {
        try
            let! commandResult =
                runGitCaptured {
                    WorkingDirectory = Some repoRoot
                    Arguments = [| "lfs"; "ls-files"; "-j" |]
                    Environment = None
                    StandardInput = None
                    TimeoutMs = Some lfsLsFilesTimeoutMs
                }

            if commandResult.ExitCode <> 0 || commandResult.TimedOut then
                return Dictionary<string, GitLfsLsFileInfo>()
            else
                let stdoutText =
                    commandResult.StdoutText
                    |> Option.ofObj
                    |> Option.defaultValue String.Empty
                    |> _.Trim()

                if String.IsNullOrWhiteSpace stdoutText then
                    return Dictionary<string, GitLfsLsFileInfo>()
                else
                    return parseGitLfsLsFilesByRelativePath stdoutText
        with _ ->
            return Dictionary<string, GitLfsLsFileInfo>()
    }

let private withLfsMetadata
    (normalizedRepoRoot: string)
    (lfsFilesByRelativePath: Dictionary<string, GitLfsLsFileInfo>)
    (entry: FileEntry)
    =
    if entry.isDirectory then
        entry
    else
        match tryGetRepoRelativePath normalizedRepoRoot entry.path with
        | Some relativePath ->
            let normalizedRelativePath = normalizePath relativePath

            match lfsFilesByRelativePath.TryGetValue(normalizedRelativePath) with
            | true, lfsInfo -> { entry with lfs = Some lfsInfo }
            | _ -> { entry with lfs = None }
        | None ->
            { entry with lfs = None }

let getFileEntry (path: string) = promise {
    let! stats = fs?promises?stat (path)
    return FileEntry.create (pathMod?basename (path), path, stats?isDirectory (), None)
}

let getFileEntryWithLfsMetadata (repoRoot: string) (path: string) = promise {
    let normalizedRepoRoot = normalizeRootPath repoRoot
    let! entry = getFileEntry path

    if entry.isDirectory then
        return entry
    else
        let! lfsFilesByRelativePath = tryGetGitLfsLsFilesByRelativePath normalizedRepoRoot
        return withLfsMetadata normalizedRepoRoot lfsFilesByRelativePath entry
}

/// Finds all files and subfolders of the given filepath
let getFileEntries (path: string) : Fable.Core.JS.Promise<FileEntry[]> = promise {
    let repoRoot = normalizeRootPath path

    let! rootStats = fs?promises?stat (repoRoot)
    let rootIsDir = rootStats?isDirectory () |> unbox<bool>

    let rootName = pathMod?basename (repoRoot) |> unbox<string>
    let rootEntry = FileEntry.create (rootName, repoRoot, rootIsDir, None)

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
                        entries.Add(FileEntry.create (name, fullPath, true, None))
                        stack.Add(fullPath)
                else
                    let fullPath = pathMod?join (currentDir, name) |> unbox<string> |> normalizePath

                    if not (shouldIgnorePath fullPath) then
                        entries.Add(FileEntry.create (name, fullPath, false, None))
            )

        let scannedEntries = entries.ToArray()
        let! lfsFilesByRelativePath = tryGetGitLfsLsFilesByRelativePath repoRoot

        return
            scannedEntries
            |> Array.map (withLfsMetadata repoRoot lfsFilesByRelativePath)
}
