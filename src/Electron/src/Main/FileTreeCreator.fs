[<AutoOpen>]
module Main.FileTreeCreator


open System
open System.Collections.Generic
open Fable.Core.JsInterop
open Swate.Electron.Shared.FileIOTypes

let fs: obj = importAll "fs"
let pathMod: obj = importAll "path"
let childProcessDynamic: obj = importAll "node:child_process"

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

let private tryGetLfsTrackedByAttributes
    (repoRoot: string)
    (repoRelativePaths: string[])
    : Fable.Core.JS.Promise<Dictionary<string, bool>> =
    promise {
        let results = Dictionary<string, bool>()

        if repoRelativePaths.Length = 0 then
            return results
        else
            let gitDir = pathMod?join (repoRoot, ".git") |> unbox<string>

            let isGitRepo =
                try
                    fs?existsSync (gitDir) |> unbox<bool>
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
                                    let msg = d?toString ("utf8") |> unbox<string>
                                    stdoutChunks.Add(msg)
                            )
                            |> ignore

                            proc?on (
                                "close",
                                fun code ->
                                    let success = if isNull code then false else (unbox<float> code) = 0.

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
                            |> Array.iter (fun relativePath -> proc?stdin?write (relativePath + "\u0000") |> ignore)

                            proc?stdin?``end`` () |> ignore
                        with _ ->
                            resolve results
                    )
    }

let private lfsPointerVersionLine = "version https://git-lfs.github.com/spec/v1"
let private lfsPointerProbeMaximumBytes = 64_000.0

let private clearLfsMetadata (entry: FileEntry) =
    {
        entry with
            isLfsPointer = None
            downloaded = None
            lfsSizeBytes = None
    }

let private tryParseLfsPointerSizeBytes (content: string) =
    let lines =
        content.Replace("\r\n", "\n").Split('\n', StringSplitOptions.RemoveEmptyEntries)
        |> Array.map _.Trim()
        |> Array.filter (fun line -> not (String.IsNullOrWhiteSpace line))

    if lines.Length < 3 || not (lines.[0].Equals(lfsPointerVersionLine, StringComparison.Ordinal)) then
        None
    else
        let hasOidLine =
            lines
            |> Array.exists (fun line ->
                let prefix = "oid sha256:"

                if line.StartsWith(prefix, StringComparison.Ordinal) then
                    let oid = line.Substring(prefix.Length)
                    oid.Length = 64
                    && (oid |> Seq.forall (fun c -> Char.IsDigit c || ('a' <= c && c <= 'f') || ('A' <= c && c <= 'F')))
                else
                    false
            )

        if not hasOidLine then
            None
        else
            lines
            |> Array.tryPick (fun line ->
                let prefix = "size "

                if line.StartsWith(prefix, StringComparison.Ordinal) then
                    let rawSize = line.Substring(prefix.Length).Trim()
                    let success, parsed = Int64.TryParse(rawSize)

                    if success && parsed >= 0L then
                        Some(float parsed)
                    else
                        None
                else
                    None
            )

let private tryGetOnDiskFileSizeBytes (absolutePath: string) : Fable.Core.JS.Promise<float option> =
    promise {
        try
            let! stats = fs?promises?stat (absolutePath)
            return Some(unbox<float> stats?size)
        with _ ->
            return None
    }

let private tryReadUtf8FileContent (absolutePath: string) : Fable.Core.JS.Promise<string option> =
    promise {
        try
            let! content = fs?promises?readFile (absolutePath, "utf8") |> unbox<Fable.Core.JS.Promise<string>>
            return Some content
        with _ ->
            return None
    }

let private tryGetPointerSizeBytes (absolutePath: string) (onDiskSizeBytes: float) : Fable.Core.JS.Promise<float option> =
    promise {
        if onDiskSizeBytes > lfsPointerProbeMaximumBytes then
            return None
        else
            let! content = tryReadUtf8FileContent absolutePath
            return content |> Option.bind tryParseLfsPointerSizeBytes
    }

let private annotateTrackedLfsEntry (entry: FileEntry) : Fable.Core.JS.Promise<FileEntry> =
    promise {
        let! onDiskSizeBytes = tryGetOnDiskFileSizeBytes entry.path

        match onDiskSizeBytes with
        | None -> return { clearLfsMetadata entry with isLfs = Some true }
        | Some onDiskSizeBytes ->
            let! pointerSizeBytes = tryGetPointerSizeBytes entry.path onDiskSizeBytes

            match pointerSizeBytes with
            | Some pointerSize ->
                return {
                    entry with
                        isLfs = Some true
                        isLfsPointer = Some true
                        downloaded = Some false
                        lfsSizeBytes = Some pointerSize
                }
            | None ->
                return {
                    entry with
                        isLfs = Some true
                        isLfsPointer = Some false
                        downloaded = Some true
                        lfsSizeBytes = Some onDiskSizeBytes
                }
    }

let private annotateFileEntryLfsState (entry: FileEntry) (isTracked: bool) : Fable.Core.JS.Promise<FileEntry> =
    if isTracked then
        annotateTrackedLfsEntry entry
    else
        promise { return { clearLfsMetadata entry with isLfs = Some false } }

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
        match tryGetRepoRelativePath normalizedRepoRoot entry.path with
        | None -> return entry
        | Some relativePath ->
            let! lfsTracked = tryGetLfsTrackedByAttributes normalizedRepoRoot [| relativePath |]

            let tracked =
                match lfsTracked.TryGetValue(relativePath) with
                | true, value -> value
                | _ -> false

            return! annotateFileEntryLfsState entry tracked
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
                        entries.Add(FileEntry.create (name, fullPath, false, Some false))
            )

        let scannedEntries = entries.ToArray()

        let repoRelativeFilePaths =
            scannedEntries
            |> Array.choose (fun entry ->
                if entry.isDirectory then
                    None
                else
                    tryGetRepoRelativePath repoRoot entry.path
            )

        let! lfsTracked = tryGetLfsTrackedByAttributes repoRoot repoRelativeFilePaths

        let enrichedEntries = ResizeArray<FileEntry>()

        for entry in scannedEntries do
            if entry.isDirectory then
                enrichedEntries.Add entry
            else
                match tryGetRepoRelativePath repoRoot entry.path with
                | None -> enrichedEntries.Add entry
                | Some relativePath ->
                    let tracked =
                        match lfsTracked.TryGetValue(relativePath) with
                        | true, value -> value
                        | _ -> false

                    let! enrichedEntry = annotateFileEntryLfsState entry tracked
                    enrichedEntries.Add enrichedEntry

        return enrichedEntries.ToArray()
}
