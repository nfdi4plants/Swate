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

let private tryListLargeObjects (repoRoot: string) : Fable.Core.JS.Promise<Map<string, ObjectStateDto>> = promise {
    try
        let context = OperationContext.detached "file-tree-objects"
        let host = WorkspaceSessionHost.get ()

        let! opened = host.OpenSession(repoRoot, context) |> Async.StartAsPromise

        let hostedSession =
            match opened with
            | Succeeded outcome
            | PartiallySucceeded(outcome, _) -> Some outcome.Value
            | Failed _ -> None

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

/// Enriches an already-read batch of entries from one large-object metadata snapshot.
/// Directory-only batches avoid querying the repository entirely.
let getFileEntriesWithLfsMetadata (repoRoot: string) (entries: FileEntry[]) = promise {
    if entries |> Array.exists (fun entry -> not entry.isDirectory) then
        let normalizedRepoRoot = normalizeRootPath repoRoot
        let! largeObjectsByRelativePath = tryListLargeObjects normalizedRepoRoot
        return withFileEntriesLfsMetadata normalizedRepoRoot largeObjectsByRelativePath entries
    else
        return entries
}

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

/// Removes a path and all descendants from a mutable file tree in place.
let internal removePathAndDescendantsInPlace (targetPath: string) (fileTree: Dictionary<string, FileEntry>) : unit =
    let normalizedTargetPath = PathHelpers.normalizePath targetPath

    if not (String.IsNullOrWhiteSpace normalizedTargetPath) then
        let keysToRemove =
            fileTree.Keys
            |> Seq.filter (fun path -> PathHelpers.isSameOrDescendantPath path normalizedTargetPath)
            |> Seq.toArray

        keysToRemove |> Array.iter (fun path -> fileTree.Remove(path) |> ignore)

let getFileEntry (path: string) = promise {
    let! stats = statAsync path
    return FileEntry.create (basename path, path, stats.isDirectory (), None)
}

let private resolveFileTreeDirectory (arcPath: string) (relativeDirectoryPath: string) =
    let normalizedArcPath = normalizeRootPath arcPath

    let normalizedRelativePath =
        PathHelpers.normalizeCanonicalRelativePath relativeDirectoryPath

    if PathHelpers.containsPathTraversalSegments normalizedRelativePath then
        invalidArg (nameof relativeDirectoryPath) "The FileTree directory must stay inside the ARC."

    let resolvedDirectoryPath =
        resolve [| normalizedArcPath; normalizedRelativePath |]
        |> PathHelpers.normalizePath

    if not (PathHelpers.isSameOrDescendantPath resolvedDirectoryPath normalizedArcPath) then
        invalidArg (nameof relativeDirectoryPath) "The FileTree directory must stay inside the ARC."

    resolvedDirectoryPath

/// Reads exactly the immediate children of one ARC-relative directory.
let readFileTreeDirectory (arcPath: string) (relativeDirectoryPath: string) : Fable.Core.JS.Promise<FileEntry[]> = promise {
    let normalizedArcPath = normalizeRootPath arcPath
    let directoryPath = resolveFileTreeDirectory normalizedArcPath relativeDirectoryPath
    let! dirents = readdirWithTypesAsync directoryPath (ReaddirOptions(withFileTypes = true))

    let entries =
        dirents
        |> Array.choose (fun dirent ->
            let name = dirent.name
            let isDirectory = dirent.isDirectory ()
            let fullPath = join [| directoryPath; name |] |> PathHelpers.normalizeSeparators

            if
                (isDirectory && shouldIgnoreDirName name)
                || (not isDirectory && shouldIgnorePath fullPath)
            then
                None
            else
                Some(FileEntry.create (name, fullPath, isDirectory, None))
        )

    // TEMPORARILY DISABLED due to LFS problems:
    // FileTree LFS enrichment calls ObjectMaterialization.ListObjects, which enumerates
    // repository-wide object state and makes this shallow read depend on repository size.
    //
    // TODO: Re-enable only after VersionControlService supports bounded object-state
    // lookup for explicit repository paths. Do not restore repository-wide ListObjects here.
    // return! getFileEntriesWithLfsMetadata normalizedArcPath entries
    return entries
}

/// Builds the bounded startup snapshot: the ARC root and its immediate children.
let getFileTree (path: string) : Fable.Core.JS.Promise<Dictionary<string, FileEntry>> = promise {
    let normalizedArcPath = normalizeRootPath path
    let! rootEntry = getFileEntry normalizedArcPath

    if not rootEntry.isDirectory then
        return createFileEntryTree [| rootEntry |]
    else
        let! children = readFileTreeDirectory normalizedArcPath ""
        return createFileEntryTree (Array.append [| rootEntry |] children)
}

/// Reconciles one directory's direct children while retaining known descendants of surviving directories.
let reconcileFileTreeDirectory
    (arcPath: string)
    (relativeDirectoryPath: string)
    (currentChildren: FileEntry[])
    (fileTree: Dictionary<string, FileEntry>)
    : Dictionary<string, FileEntry> =
    let directoryPath = resolveFileTreeDirectory arcPath relativeDirectoryPath
    let nextTree = Dictionary<string, FileEntry>(fileTree)

    let currentByPath =
        currentChildren
        |> Array.map (fun entry -> PathHelpers.normalizePath entry.path, entry)
        |> Map.ofArray

    let knownDirectChildren =
        nextTree.Values
        |> Seq.filter (fun entry ->
            PathHelpers.pathsEqual (dirname (PathHelpers.normalizePath entry.path)) directoryPath
        )
        |> Seq.toArray

    knownDirectChildren
    |> Array.iter (fun knownChild ->
        let normalizedChildPath = PathHelpers.normalizePath knownChild.path

        if not (currentByPath.ContainsKey normalizedChildPath) then
            removePathAndDescendantsInPlace normalizedChildPath nextTree
    )

    currentChildren |> Array.iter (fun entry -> nextTree.[entry.path] <- entry)
    nextTree
