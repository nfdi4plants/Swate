[<AutoOpen>]
module Main.FileTreeCreator

open System
open System.Collections.Generic
open Fable.Core
open Main.Bindings.Filesystem
open Main.Bindings.Path
open Main.VersionControl
open Swate.Components.Shared
open Swate.Components.Shared.PathChildrenIndex
open Swate.Electron.Shared.FileIOHelper
open Swate.Electron.Shared.FileIOTypes
open Swate.Electron.Shared.VersionControlTypes
open VersionControlService.Abstractions


/// Keeps main-process FileTree entries and their path index synchronized.
type IndexedFileTree(entries: Dictionary<string, FileEntry>) =
    let children = PathChildrenIndex()
    do children.Rebuild entries.Keys

    new() = IndexedFileTree(Dictionary<string, FileEntry>())

    member _.Count = entries.Count

    member _.Values = entries.Values

    member _.ContainsKey(path: string) = entries.ContainsKey path

    member _.TryGetValue(path: string) = entries.TryGetValue path

    /// Creates a mutable working snapshot for an explicit replacement operation.
    member _.CopySnapshot() = Dictionary<string, FileEntry>(entries)

    member _.GetKnownDirectChildren(directoryPath: string) =
        children.GetDirectChildPaths directoryPath
        |> Seq.choose (fun childPath ->
            match entries.TryGetValue childPath with
            | true, entry -> Some(childPath, entry)
            | false, _ -> None
        )
        |> Map.ofSeq

    member _.CollectSubtreePaths(rootPaths: seq<string>) = children.CollectSubtreePaths rootPaths

    member _.ApplyChanges(removedPaths: string[], upsertedEntries: FileEntry[]) =
        removedPaths |> Array.iter (fun path -> entries.Remove(path) |> ignore)
        upsertedEntries |> Array.iter (fun entry -> entries.[entry.path] <- entry)
        removedPaths |> Seq.iter children.Remove
        upsertedEntries |> Seq.iter (fun entry -> children.Add entry.path)

    member _.ReplaceSnapshot(snapshot: Dictionary<string, FileEntry>) =
        if not (System.Object.ReferenceEquals(entries, snapshot)) then
            entries.Clear()
            snapshot |> Seq.iter (fun pair -> entries.[pair.Key] <- pair.Value)

        children.Rebuild entries.Keys

    member _.Clear() =
        entries.Clear()
        children.Rebuild entries.Keys

let private shouldIgnoreDirName (name: string) = name = ".git"

let private tryListLargeObjects
    (repoRoot: string)
    (openSession: bool)
    : Fable.Core.JS.Promise<Map<string, ObjectStateDto>> =
    promise {
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

let private withFileEntryLfsMetadata
    (repoRoot: string)
    (largeObjectsByRelativePath: Map<string, ObjectStateDto>)
    (largeObjectsByComparisonKey: Map<string, ObjectStateDto>)
    (entry: FileEntry)
    : FileEntry =
    if entry.isDirectory then
        entry
    else
        match tryGetRepoRelativePath repoRoot entry.path with
        | Some relativePath ->
            let normalizedRelativePath = PathHelpers.normalizeSeparators relativePath

            let largeObject =
                match Map.tryFind normalizedRelativePath largeObjectsByRelativePath with
                | Some largeObject -> Some largeObject
                | None ->
                    normalizedRelativePath
                    |> PathHelpers.normalizeForUnicodeComparison
                    |> fun comparisonKey -> Map.tryFind comparisonKey largeObjectsByComparisonKey

            { entry with largeObject = largeObject }
        | None -> { entry with largeObject = None }

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

let withFileEntriesLfsMetadata
    (repoRoot: string)
    (largeObjectsByRelativePath: Map<string, ObjectStateDto>)
    (entries: FileEntry[])
    : FileEntry[] =
    let largeObjectsByComparisonKey =
        buildLargeObjectsByComparisonKey largeObjectsByRelativePath

    entries
    |> Array.map (withFileEntryLfsMetadata repoRoot largeObjectsByRelativePath largeObjectsByComparisonKey)

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

/// Reconciles only the immediate children of one ARC-relative directory.
/// Returns None when the current snapshot already matches disk.
let reconcileFileTreeDirectory
    (arcPath: string)
    (relativeDirectoryPath: string)
    (fileTree: IndexedFileTree)
    : Fable.Core.JS.Promise<FileTreeDirectoryUpdate option> =
    promise {
        let normalizedArcPath = resolve [| arcPath |] |> PathHelpers.normalizePath

        let normalizedRelativePath =
            PathHelpers.normalizeCanonicalRelativePath relativeDirectoryPath

        let absoluteDirectoryPath =
            resolve [| normalizedArcPath; normalizedRelativePath |]
            |> PathHelpers.normalizePath

        let resolvedRelativePath =
            tryGetRepoRelativePathOrRoot normalizedArcPath absoluteDirectoryPath

        if
            PathHelpers.containsPathTraversalSegments normalizedRelativePath
            || isAbsolute normalizedRelativePath
            || resolvedRelativePath <> Some normalizedRelativePath
        then
            return raise (exn $"Directory '{relativeDirectoryPath}' is outside the open ARC.")
        else
            let! directoryStats = statAsync absoluteDirectoryPath

            if not (directoryStats.isDirectory ()) then
                return raise (exn $"Path '{relativeDirectoryPath}' is not a directory.")
            else
                let! dirents = readdirWithTypesAsync absoluteDirectoryPath (ReaddirOptions(withFileTypes = true))

                let diskChildren =
                    dirents
                    |> Array.filter (fun dirent ->
                        not (shouldIgnoreDirName dirent.name)
                        && not (isIgnoredArcInventoryPath (join [| absoluteDirectoryPath; dirent.name |]))
                    )
                    |> Array.map (fun dirent ->
                        let childPath =
                            join [| absoluteDirectoryPath; dirent.name |] |> PathHelpers.normalizePath

                        childPath, FileEntry.create (dirent.name, childPath, dirent.isDirectory (), None)
                    )
                    |> Map.ofArray

                let knownDirectChildren = fileTree.GetKnownDirectChildren absoluteDirectoryPath

                let hasRemovedChildren =
                    knownDirectChildren
                    |> Map.exists (fun path _ -> not (Map.containsKey path diskChildren))

                let reconciledDiskChildren =
                    diskChildren
                    |> Map.map (fun path diskEntry ->
                        match Map.tryFind path knownDirectChildren with
                        | Some knownEntry when not diskEntry.isDirectory -> {
                            diskEntry with
                                largeObject = knownEntry.largeObject
                          }
                        | _ -> diskEntry
                    )

                let upsertedEntries =
                    reconciledDiskChildren
                    |> Map.toArray
                    |> Array.choose (fun (path, reconciledEntry) ->
                        match Map.tryFind path knownDirectChildren with
                        | Some knownEntry when knownEntry = reconciledEntry -> None
                        | _ -> Some reconciledEntry
                    )

                let hasAddedOrChangedChildren = upsertedEntries.Length > 0

                if not hasRemovedChildren && not hasAddedOrChangedChildren then
                    return None
                else
                    let missingFilePaths = ResizeArray<string>()

                    let directChildDirectoryRemovalRoots = HashSet<string>()

                    knownDirectChildren
                    |> Map.iter (fun childPath knownEntry ->
                        match Map.tryFind childPath diskChildren with
                        | None when knownEntry.isDirectory -> directChildDirectoryRemovalRoots.Add childPath |> ignore
                        | None -> missingFilePaths.Add childPath
                        | Some diskEntry when knownEntry.isDirectory && not diskEntry.isDirectory ->
                            directChildDirectoryRemovalRoots.Add childPath |> ignore
                        | Some _ -> ()
                    )

                    let subtreeRemovalKeys =
                        fileTree.CollectSubtreePaths directChildDirectoryRemovalRoots

                    let removedPaths = Array.append (missingFilePaths.ToArray()) subtreeRemovalKeys
                    fileTree.ApplyChanges(removedPaths, upsertedEntries)

                    return
                        Some {
                            directoryPath = normalizedRelativePath
                            children =
                                reconciledDiskChildren.Values
                                |> Seq.choose (fun entry ->
                                    tryGetRepoRelativePath normalizedArcPath entry.path
                                    |> Option.map (fun relativePath -> { entry with path = relativePath })
                                )
                                |> Seq.toArray
                        }
    }

let getFileEntryWithLfsMetadata (repoRoot: string) (path: string) = promise {
    let normalizedRepoRoot = resolve [| repoRoot |] |> PathHelpers.normalizePath
    let! entry = getFileEntry path

    if entry.isDirectory then
        return entry
    else
        let! largeObjectsByRelativePath = tryListLargeObjects normalizedRepoRoot true

        let largeObjectsByComparisonKey =
            buildLargeObjectsByComparisonKey largeObjectsByRelativePath

        return withFileEntryLfsMetadata normalizedRepoRoot largeObjectsByRelativePath largeObjectsByComparisonKey entry
}

/// Finds all files and subfolders of the given filepath
let getFileEntries (path: string) (openSession: bool) : Fable.Core.JS.Promise<FileEntry[]> = promise {
    let repoRoot = resolve [| path |] |> PathHelpers.normalizePath
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

                    if not (isIgnoredArcInventoryPath fullPath) then
                        entries.Add(FileEntry.create (name, fullPath, false, None))
            )

        let scannedEntries = entries.ToArray()
        let! largeObjectsByRelativePath = tryListLargeObjects repoRoot openSession
        return withFileEntriesLfsMetadata repoRoot largeObjectsByRelativePath scannedEntries
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
