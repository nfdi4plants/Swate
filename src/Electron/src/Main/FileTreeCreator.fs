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

let normalizeRootPath (path: string) =
    resolve [| path |] |> PathHelpers.normalizePath

let private shouldIgnoreDirName (name: string) = name = ".git"

let private shouldIgnorePath (path: string) =
    let normalizedPath = PathHelpers.normalizeSeparators path
    let tempXlsxPattern = """\.~\$.*\.xlsx$"""
    System.Text.RegularExpressions.Regex.IsMatch(normalizedPath, tempXlsxPattern)

/// Enriches a single file entry with Git LFS metadata from `git lfs ls-files -j`.
let private withFileEntryLfsMetadata
    (repoRoot: string)
    (lfsFilesByRelativePath: Dictionary<string, GitLfsLsFileInfo>)
    (entry: FileEntry)
    : FileEntry =
    if entry.isDirectory then
        entry
    else
        match tryGetRepoRelativePath repoRoot entry.path with
        | Some relativePath ->
            let normalizedRelativePath = PathHelpers.normalizeSeparators relativePath

            match lfsFilesByRelativePath.TryGetValue(normalizedRelativePath) with
            | true, lfsInfo -> { entry with lfs = Some lfsInfo }
            | _ -> { entry with lfs = None }
        | None -> { entry with lfs = None }

/// Enriches file entries with Git LFS metadata from `git lfs ls-files -j`.
let private withFileEntriesLfsMetadata
    (repoRoot: string)
    (lfsFilesByRelativePath: Dictionary<string, GitLfsLsFileInfo>)
    (entries: FileEntry[])
    : FileEntry[] =
    entries |> Array.map (withFileEntryLfsMetadata repoRoot lfsFilesByRelativePath)

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

/// Remove a path and all descendants from a file tree dictionary using normalized ancestor checks.
let removePathAndDescendants
    (targetPath: string)
    (fileTree: Dictionary<string, FileEntry>)
    : Dictionary<string, FileEntry> =
    let normalizedTargetPath = PathHelpers.normalizePath targetPath
    let nextTree = Dictionary<string, FileEntry>(fileTree)

    if String.IsNullOrWhiteSpace normalizedTargetPath then
        nextTree
    else
        let keysToRemove =
            nextTree.Keys
            |> Seq.filter (fun path -> PathHelpers.isSameOrDescendantPath path normalizedTargetPath)
            |> Seq.toArray

        keysToRemove |> Array.iter (fun path -> nextTree.Remove(path) |> ignore)
        nextTree

/// Add or replace a single file tree entry without mutating the current snapshot.
let upsertFileEntry (entry: FileEntry) (fileTree: Dictionary<string, FileEntry>) : Dictionary<string, FileEntry> =
    let nextTree = Dictionary<string, FileEntry>(fileTree)
    nextTree.[entry.path] <- entry
    nextTree

let getFileEntry (path: string) = promise {
    let! stats = statAsync path
    return FileEntry.create (basename path, path, stats.isDirectory (), None)
}

let getFileEntryWithLfsMetadata (repoRoot: string) (path: string) = promise {
    let normalizedRepoRoot = normalizeRootPath repoRoot
    let! entry = getFileEntry path

    if entry.isDirectory then
        return entry
    else
        let! lfsFilesByRelativePath = tryGetLsFilesByRelativePath normalizedRepoRoot
        return withFileEntryLfsMetadata normalizedRepoRoot lfsFilesByRelativePath entry
}

/// Finds all files and subfolders of the given filepath
let getFileEntries (path: string) : Fable.Core.JS.Promise<FileEntry[]> = promise {
    let repoRoot = normalizeRootPath path

    let! rootStats = statAsync repoRoot
    let rootIsDir = rootStats.isDirectory ()

    let rootName = basename repoRoot
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

            let! dirents = readdirWithTypesAsync currentDir (ReaddirOptions(withFileTypes = true))

            dirents
            |> Array.iter (fun dirent ->
                let name = dirent.name
                let isDir = dirent.isDirectory ()

                if isDir then
                    if not (shouldIgnoreDirName name) then
                        let fullPath = join [| currentDir; name |] |> PathHelpers.normalizeSeparators
                        entries.Add(FileEntry.create (name, fullPath, true, None))
                        stack.Add(fullPath)
                else
                    let fullPath = join [| currentDir; name |] |> PathHelpers.normalizeSeparators

                    if not (shouldIgnorePath fullPath) then
                        entries.Add(FileEntry.create (name, fullPath, false, None))
            )

        let scannedEntries = entries.ToArray()
        let! lfsFilesByRelativePath = tryGetLsFilesByRelativePath repoRoot
        return withFileEntriesLfsMetadata repoRoot lfsFilesByRelativePath scannedEntries
}