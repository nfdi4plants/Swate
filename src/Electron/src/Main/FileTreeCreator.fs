[<AutoOpen>]
module Main.FileTreeCreator

open System
open System.Collections.Generic
open Main.Bindings.Filesystem
open Main.Bindings.Path
open Main.Git.GitLfsService
open Swate.Components.Shared
open Swate.Electron.Shared.FileIOHelper
open Swate.Electron.Shared.FileIOTypes

/// Serializes asynchronous file-tree mutations against the latest published snapshot.
type FileTreeWorkQueue(onPreviousError: exn -> unit) =
    let mutable currentWork = promise { return () }

    member this.EnqueueFileTreeWork(operation: unit -> Fable.Core.JS.Promise<unit>) =
        let previousWork = currentWork

        let nextWork = promise {
            try
                do! previousWork
            with previousError ->
                onPreviousError previousError

            do! operation ()
        }

        currentWork <- nextWork
        nextWork

let private shouldIgnorePath (path: string) =
    let normalizedPath = PathHelpers.normalizeSeparators path
    let tempXlsxPattern = """\.~\$.*\.xlsx$"""

    System.Text.RegularExpressions.Regex.IsMatch(normalizedPath, tempXlsxPattern)
    || isLegacyDataMapPath normalizedPath

/// Enriches a single file entry with Git LFS metadata from `git lfs ls-files -j`.
let withFileEntryLfsMetadata (repoRoot: string) (lfsPathIndex: LfsPathIndex) (entry: FileEntry) : FileEntry =
    if entry.isDirectory then
        entry
    else
        match tryGetRepoRelativePath repoRoot entry.path with
        | Some relativePath ->
            let normalizedRelativePath = PathHelpers.normalizeSeparators relativePath

            match tryFindLsFileInfoByRelativePath lfsPathIndex normalizedRelativePath with
            | Some lfsInfo -> { entry with lfs = Some lfsInfo }
            | None -> { entry with lfs = None }
        | None -> { entry with lfs = None }

/// Enriches file entries with Git LFS metadata from `git lfs ls-files -j`.
let private withFileEntriesLfsMetadata
    (repoRoot: string)
    (lfsPathIndex: LfsPathIndex)
    (entries: FileEntry[])
    : FileEntry[] =
    entries |> Array.map (withFileEntryLfsMetadata repoRoot lfsPathIndex)

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

/// Removes a path and all descendants from a mutable file-tree snapshot.
let removePathAndDescendantsInPlace (targetPath: string) (fileTree: Dictionary<string, FileEntry>) =
    let normalizedTargetPath = PathHelpers.normalizePath targetPath

    if not (String.IsNullOrWhiteSpace normalizedTargetPath) then
        let keysToRemove =
            fileTree.Keys
            |> Seq.filter (fun path -> PathHelpers.isSameOrDescendantPath path normalizedTargetPath)
            |> Seq.toArray

        keysToRemove |> Array.iter (fun path -> fileTree.Remove(path) |> ignore)

let upsertFileEntryInPlace (entry: FileEntry) (fileTree: Dictionary<string, FileEntry>) = fileTree.[entry.path] <- entry

let getFileEntry (path: string) = promise {
    let! stats = statAsync path
    return FileEntry.create (basename path, path, stats.isDirectory (), None)
}

/// Refreshes one known relative path without rescanning the ARC directory.
let refreshFileTreeEntry
    (arcPath: string)
    (relativePath: string)
    (fileTree: Dictionary<string, FileEntry>)
    : Fable.Core.JS.Promise<Dictionary<string, FileEntry>> =
    promise {
        let absolutePath = join [| arcPath; relativePath |]
        let! entry = getFileEntry absolutePath

        let updatedTree =
            let nextTree = Dictionary<string, FileEntry>(fileTree)
            upsertFileEntryInPlace entry nextTree
            nextTree

        return updatedTree
    }

let getFileEntryWithLfsMetadata (repoRoot: string) (path: string) = promise {
    let normalizedRepoRoot = resolve [| repoRoot |] |> PathHelpers.normalizePath
    let! entry = getFileEntry path

    if entry.isDirectory then
        return entry
    else
        let! lfsPathIndex = tryGetLsFilesByRelativePath normalizedRepoRoot
        return withFileEntryLfsMetadata normalizedRepoRoot lfsPathIndex entry
}

let private scanFileEntries (path: string) : Fable.Core.JS.Promise<FileEntry[]> = promise {
    let scanRoot = resolve [| path |] |> PathHelpers.normalizePath

    let! rootStats = statAsync scanRoot
    let rootIsDir = rootStats.isDirectory ()

    let rootName = basename scanRoot
    let rootEntry = FileEntry.create (rootName, scanRoot, rootIsDir, None)

    if not rootIsDir then
        return [| rootEntry |]
    else
        let stack = ResizeArray<string>()
        stack.Add(scanRoot)

        let entries = ResizeArray<FileEntry>()
        entries.Add(rootEntry)

        while stack.Count > 0 do
            let currentDir = stack.[stack.Count - 1]
            stack.RemoveAt(stack.Count - 1)

            let! dirents = readdirWithTypesAsync currentDir (ReaddirOptions(withFileTypes = true))

            dirents
            |> Array.iter (fun dirent ->
                let name = dirent.name
                let isDir = dirent.isDirectory ()

                if isDir then
                    if not (name = ".git") then
                        let fullPath = join [| currentDir; name |] |> PathHelpers.normalizeSeparators
                        entries.Add(FileEntry.create (name, fullPath, true, None))
                        stack.Add(fullPath)
                else
                    let fullPath = join [| currentDir; name |] |> PathHelpers.normalizeSeparators

                    if not (shouldIgnorePath fullPath) then
                        entries.Add(FileEntry.create (name, fullPath, false, None))
            )

        return entries.ToArray()
}

/// Finds all files and subfolders below a path and enriches them relative to the repository root.
let getFileEntriesInSubtree (repoRoot: string) (path: string) : Fable.Core.JS.Promise<FileEntry[]> = promise {
    let normalizedRepoRoot = resolve [| repoRoot |] |> PathHelpers.normalizePath

    let! scannedEntries = scanFileEntries path
    let! lfsPathIndex = tryGetLsFilesByRelativePath normalizedRepoRoot
    return withFileEntriesLfsMetadata normalizedRepoRoot lfsPathIndex scannedEntries
}

/// Replaces one subtree in a copy of the current file-tree snapshot.
let refreshFileTreeSubtree
    (repoRoot: string)
    (path: string)
    (fileTree: Dictionary<string, FileEntry>)
    : Fable.Core.JS.Promise<Dictionary<string, FileEntry>> =
    promise {
        let! entries = getFileEntriesInSubtree repoRoot path
        let nextTree = Dictionary<string, FileEntry>(fileTree)
        removePathAndDescendantsInPlace path nextTree
        entries |> Array.iter (fun entry -> upsertFileEntryInPlace entry nextTree)
        return nextTree
    }

/// Scans a path and builds its keyed file tree.
let getFileTree (path: string) : Fable.Core.JS.Promise<Dictionary<string, FileEntry>> = promise {
    let! fileEntries = getFileEntriesInSubtree path path
    return createFileEntryTree fileEntries
}
