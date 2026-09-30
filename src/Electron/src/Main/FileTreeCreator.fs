[<AutoOpen>]
module Main.FileTreeCreator

open System
open System.Collections.Generic
open Fable.Core
open Main.Bindings.Filesystem
open Main.Bindings.Path
open Main.VersionControl
open Swate.Components.Shared
open Swate.Electron.Shared.FileIOHelper
open Swate.Electron.Shared.FileIOTypes
open Swate.Electron.Shared.VersionControlTypes
open VersionControlService.Abstractions

let normalizeRootPath (path: string) =
    resolve [| path |] |> PathHelpers.normalizePath

let private shouldIgnoreDirName (name: string) = name = ".git"

let private shouldIgnorePath (path: string) =
    let normalizedPath = PathHelpers.normalizeSeparators path
    let tempXlsxPattern = """\.~\$.*\.xlsx$"""
    let temporaryLfsBackupPattern = """\.vcs-lfs-backup-[0-9a-fA-F]{32}$"""

    System.Text.RegularExpressions.Regex.IsMatch(normalizedPath, tempXlsxPattern)
    || System.Text.RegularExpressions.Regex.IsMatch(normalizedPath, temporaryLfsBackupPattern)
    || isLegacyDataMapPath normalizedPath

let tryListLargeObjects (repoRoot: string) (openSession: bool) : Fable.Core.JS.Promise<Map<string, ObjectStateDto>> = promise {
    try
        let context = OperationContext.detached "file-tree-objects"
        let host = WorkspaceSessionHost.get ()

        let! hostedSession =
            if openSession then
                promise {
                    let! opened = host.OpenSession(repoRoot, context) |> Async.StartAsPromise

                    return
                        match opened with
                        | Succeeded outcome
                        | PartiallySucceeded(outcome, _) -> Some outcome.Value
                        | Failed _ -> None
                }
            else
                promise { return host.TryGetSession repoRoot }

        match hostedSession with
        | None -> return Map.empty
        | Some hosted ->
            match hosted.Session.ObjectMaterialization with
            | None -> return Map.empty
            | Some materialization ->
                let! listed = materialization.ListObjects context |> Async.StartAsPromise

                match listed with
                | Succeeded outcome
                | PartiallySucceeded(outcome, _) ->
                    return
                        outcome.Value
                        |> Array.map (fun (objectState: ObjectState) ->
                            let dto = Mappings.objectState objectState
                            dto.Path, dto
                        )
                        |> Map.ofArray
                | Failed _ -> return Map.empty
    with _ ->
        return Map.empty
}

let private buildLargeObjectsByComparisonKey (largeObjectsByRelativePath: Map<string, ObjectStateDto>) =
    largeObjectsByRelativePath
    |> Map.toSeq
    |> Seq.groupBy (fun (relativePath, _) -> PathHelpers.normalizeForUnicodeComparison relativePath)
    |> Seq.choose (fun (comparisonKey, matchingObjects) ->
        match matchingObjects |> Seq.toList with
        | [ (_, largeObject) ] -> Some(comparisonKey, largeObject)
        | _ -> None
    )
    |> Map.ofSeq

type LargeObjectPathIndex = {
    Exact: Map<string, ObjectStateDto>
    Comparison: Map<string, ObjectStateDto>
}

let buildLargeObjectPathIndex (largeObjectsByRelativePath: Map<string, ObjectStateDto>) = {
    Exact = largeObjectsByRelativePath
    Comparison = buildLargeObjectsByComparisonKey largeObjectsByRelativePath
}

let withFileEntryLargeObjectMetadata (repoRoot: string) (index: LargeObjectPathIndex) (entry: FileEntry) : FileEntry =
    if entry.isDirectory then
        entry
    else
        match tryGetRepoRelativePath repoRoot entry.path with
        | Some relativePath ->
            let normalizedRelativePath = PathHelpers.normalizeSeparators relativePath

            let largeObject =
                match Map.tryFind normalizedRelativePath index.Exact with
                | Some largeObject -> Some largeObject
                | None ->
                    normalizedRelativePath
                    |> PathHelpers.normalizeForUnicodeComparison
                    |> fun comparisonKey -> Map.tryFind comparisonKey index.Comparison

            { entry with largeObject = largeObject }
        | None -> { entry with largeObject = None }

let withFileEntriesLfsMetadata
    (repoRoot: string)
    (largeObjectsByRelativePath: Map<string, ObjectStateDto>)
    (entries: FileEntry[])
    : FileEntry[] =
    let index = buildLargeObjectPathIndex largeObjectsByRelativePath

    entries |> Array.map (withFileEntryLargeObjectMetadata repoRoot index)

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

/// Logical FileTree keys are case-sensitive even when the host filesystem is not.
let isSameOrDescendantLogicalPath (path: string) (ancestorPath: string) =
    let normalizedPath = PathHelpers.normalizePath path
    let normalizedAncestorPath = PathHelpers.normalizePath ancestorPath

    not (String.IsNullOrWhiteSpace normalizedAncestorPath)
    && (normalizedPath = normalizedAncestorPath
        || normalizedPath.StartsWith(normalizedAncestorPath + "/", StringComparison.Ordinal))

let private minimizeLogicalDirectoryPrefixes (targetPaths: seq<string>) =
    let normalizedTargets =
        targetPaths
        |> Seq.map PathHelpers.normalizePath
        |> Seq.filter (String.IsNullOrWhiteSpace >> not)
        |> Seq.distinct
        |> Seq.toArray

    let allTargets = HashSet<string>(normalizedTargets)

    normalizedTargets
    |> Array.filter (fun candidate ->
        let mutable parent = PathHelpers.tryGetParentPath candidate
        let mutable hasDeletedAncestor = false

        while parent.IsSome && not hasDeletedAncestor do
            let parentPath = parent.Value

            if String.IsNullOrWhiteSpace parentPath then
                parent <- None
            elif allTargets.Contains parentPath then
                hasDeletedAncestor <- true
            else
                parent <- PathHelpers.tryGetParentPath parentPath

        not hasDeletedAncestor
    )

/// Removes several directory paths and their descendants with one FileTree scan.
let removePathsAndDescendantsInPlace (targetPaths: seq<string>) (fileTree: Dictionary<string, FileEntry>) =
    let prefixes = minimizeLogicalDirectoryPrefixes targetPaths

    if not (Array.isEmpty prefixes) then
        let prefixSet = HashSet<string>(prefixes)

        let isUnderRemovedDirectory path =
            let normalizedPath = PathHelpers.normalizePath path
            let mutable candidate = Some normalizedPath
            let mutable shouldRemove = false

            while candidate.IsSome && not shouldRemove do
                let candidatePath = candidate.Value

                if prefixSet.Contains candidatePath then
                    shouldRemove <- true
                else
                    candidate <- PathHelpers.tryGetParentPath candidatePath

            shouldRemove

        let keysToRemove =
            fileTree.Keys |> Seq.filter isUnderRemovedDirectory |> Seq.toArray

        keysToRemove |> Array.iter (fun path -> fileTree.Remove(path) |> ignore)

/// Remove a path and all descendants from a file tree dictionary using normalized ancestor checks.
let removePathAndDescendants
    (targetPath: string)
    (fileTree: Dictionary<string, FileEntry>)
    : Dictionary<string, FileEntry> =
    let nextTree = Dictionary<string, FileEntry>(fileTree)
    removePathsAndDescendantsInPlace [ targetPath ] nextTree
    nextTree

let upsertFileEntryInPlace (entry: FileEntry) (fileTree: Dictionary<string, FileEntry>) = fileTree.[entry.path] <- entry

/// Add or replace a single file tree entry without mutating the current snapshot.
let upsertFileEntry (entry: FileEntry) (fileTree: Dictionary<string, FileEntry>) : Dictionary<string, FileEntry> =
    let nextTree = Dictionary<string, FileEntry>(fileTree)
    upsertFileEntryInPlace entry nextTree
    nextTree

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
        return upsertFileEntry entry fileTree
    }

let getFileEntryWithLfsMetadata (repoRoot: string) (path: string) = promise {
    let normalizedRepoRoot = normalizeRootPath repoRoot
    let! entry = getFileEntry path

    if entry.isDirectory then
        return entry
    else
        let! largeObjectsByRelativePath = tryListLargeObjects normalizedRepoRoot true

        let index = buildLargeObjectPathIndex largeObjectsByRelativePath
        return withFileEntryLargeObjectMetadata normalizedRepoRoot index entry
}

let private scanFileEntries (path: string) : Fable.Core.JS.Promise<FileEntry[]> = promise {
    let scanRoot = normalizeRootPath path

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
                    if not (shouldIgnoreDirName name) then
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

/// Reads only the immediate children of a directory. Child directories are represented as entries
/// and are not traversed.
let scanImmediateFileEntries (path: string) : Fable.Core.JS.Promise<FileEntry[]> = promise {
    let scanRoot = normalizeRootPath path
    let! dirents = readdirWithTypesAsync scanRoot (ReaddirOptions(withFileTypes = true))

    return
        dirents
        |> Array.choose (fun dirent ->
            let name = dirent.name
            let isDirectory = dirent.isDirectory ()
            let fullPath = join [| scanRoot; name |] |> PathHelpers.normalizeSeparators

            if
                (isDirectory && shouldIgnoreDirName name)
                || (not isDirectory && shouldIgnorePath fullPath)
            then
                None
            else
                Some(FileEntry.create (name, fullPath, isDirectory, None))
        )
}

/// Builds one current large-object lookup for all direct files participating in a shallow
/// reconciliation. Entries are enriched later, when the current FileTree is applied.
let prepareImmediateFileEntryLargeObjectIndex
    (repoRoot: string)
    (entryBatches: FileEntry[][])
    : Fable.Core.JS.Promise<LargeObjectPathIndex> =
    promise {
        let hasDirectFiles =
            entryBatches
            |> Array.collect id
            |> Array.exists (fun entry -> not entry.isDirectory)

        let! largeObjectsByRelativePath =
            if hasDirectFiles then
                tryListLargeObjects (normalizeRootPath repoRoot) false
            else
                promise { return Map.empty }

        return buildLargeObjectPathIndex largeObjectsByRelativePath
    }

/// Reconciles only a directory's direct children. Surviving child directories keep all already
/// materialized descendants; removed child directories lose their known subtrees.
let reconcileImmediateFileEntries
    (directoryPath: string)
    (entries: FileEntry[])
    (fileTree: Dictionary<string, FileEntry>)
    : Dictionary<string, FileEntry> * bool =
    let normalizedDirectoryPath = PathHelpers.normalizePath directoryPath

    let isDirectChild (path: string) =
        String.Equals(PathHelpers.normalizePath (dirname path), normalizedDirectoryPath, StringComparison.Ordinal)

    let diskEntries =
        entries
        |> Array.map (fun entry -> PathHelpers.normalizePath entry.path, entry)
        |> Map.ofArray

    let knownDirectChildren =
        fileTree.Keys
        |> Seq.filter isDirectChild
        |> Seq.map PathHelpers.normalizePath
        |> Seq.toArray

    let removedChildren =
        knownDirectChildren
        |> Array.filter (fun path -> not (diskEntries.ContainsKey path))

    let mutable changed = not (Array.isEmpty removedChildren)

    for KeyValue(path, entry) in diskEntries do
        match fileTree.TryGetValue path with
        | true, existing when existing = entry -> ()
        | _ -> changed <- true

    if not changed then
        fileTree, false
    else
        let nextTree = Dictionary<string, FileEntry>(fileTree)
        removePathsAndDescendantsInPlace removedChildren nextTree

        for KeyValue(_, entry) in diskEntries do
            match nextTree.TryGetValue entry.path with
            | true, existing when existing.isDirectory && entry.isDirectory ->
                // The directory entry itself can change without discarding its known descendants.
                nextTree.[entry.path] <- entry
            | true, existing when existing.isDirectory <> entry.isDirectory ->
                removePathsAndDescendantsInPlace [ entry.path ] nextTree
                nextTree.[entry.path] <- entry
            | _ -> nextTree.[entry.path] <- entry

        nextTree, true

/// Finds all files and subfolders of the given filepath
let getFileEntries (path: string) (openSession: bool) : Fable.Core.JS.Promise<FileEntry[]> = promise {
    let repoRoot = normalizeRootPath path
    let! scannedEntries = scanFileEntries repoRoot
    let! largeObjectsByRelativePath = tryListLargeObjects repoRoot openSession
    return withFileEntriesLfsMetadata repoRoot largeObjectsByRelativePath scannedEntries
}

/// Finds all files and subfolders below a path and enriches them from one repository object snapshot.
let getFileEntriesInSubtree (repoRoot: string) (path: string) : Fable.Core.JS.Promise<FileEntry[]> = promise {
    let normalizedRepoRoot = normalizeRootPath repoRoot
    let! scannedEntries = scanFileEntries path
    let! largeObjectsByRelativePath = tryListLargeObjects normalizedRepoRoot false
    return withFileEntriesLfsMetadata normalizedRepoRoot largeObjectsByRelativePath scannedEntries
}

/// Replaces one subtree in a single copy of the current file-tree snapshot.
let refreshFileTreeSubtree
    (repoRoot: string)
    (path: string)
    (fileTree: Dictionary<string, FileEntry>)
    : Fable.Core.JS.Promise<Dictionary<string, FileEntry>> =
    promise {
        let! entries = getFileEntriesInSubtree repoRoot path
        let nextTree = Dictionary<string, FileEntry>(fileTree)
        removePathsAndDescendantsInPlace [ path ] nextTree
        entries |> Array.iter (fun entry -> upsertFileEntryInPlace entry nextTree)
        return nextTree
    }

/// Scans a path and builds its keyed file tree.
let getFileTree (path: string) : Fable.Core.JS.Promise<Dictionary<string, FileEntry>> = promise {
    let! fileEntries = getFileEntries path true
    return createFileEntryTree fileEntries
}

/// Refreshes the tree without reopening a session that is being closed.
let getFileTreeFromOpenSession (path: string) : Fable.Core.JS.Promise<Dictionary<string, FileEntry>> = promise {
    let! fileEntries = getFileEntries path false
    return createFileEntryTree fileEntries
}
