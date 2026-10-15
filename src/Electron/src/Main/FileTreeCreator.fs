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

type FileTreeDirectoryPage = { Entries: FileEntry[]; HasMore: bool }

type FileTreeDirectoryCursor = {
    Directory: Directory
    mutable Lookahead: Dirent option
}

let openFileTreeDirectoryCursor (arcPath: string) (relativeDirectoryPath: string) = promise {
    let normalizedArcPath = normalizeRootPath arcPath
    let directoryPath = resolveFileTreeDirectory normalizedArcPath relativeDirectoryPath
    let! directory = openDirectoryAsync directoryPath

    return
        directoryPath,
        {
            Directory = directory
            Lookahead = None
        }
}

/// Advances an already-open directory cursor by one bounded batch. The accepted lookahead is
/// retained by the cursor and becomes the first entry of the next batch.
let readFileTreeDirectoryCursorPage
    (directoryPath: string)
    (cursor: FileTreeDirectoryCursor)
    (pageSize: int)
    : Fable.Core.JS.Promise<FileTreeDirectoryPage> =
    promise {
        if pageSize < 1 then
            invalidArg (nameof pageSize) "Directory page size must be at least one."

        let entries = ResizeArray<FileEntry>()
        let mutable exhausted = false

        let tryAccept (dirent: Dirent) =
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

        cursor.Lookahead
        |> Option.iter (fun dirent ->
            cursor.Lookahead <- None
            tryAccept dirent |> Option.iter entries.Add
        )

        while not exhausted && entries.Count < pageSize do
            let! dirent = cursor.Directory.read ()

            if isNull (box dirent) then
                exhausted <- true
            else
                tryAccept dirent |> Option.iter entries.Add

        // Read until the next accepted entry so ignored filesystem entries do not create a false
        // positive HasMore result. Preserve that entry for the next batch.
        while not exhausted && cursor.Lookahead.IsNone do
            let! dirent = cursor.Directory.read ()

            if isNull (box dirent) then
                exhausted <- true
            elif tryAccept dirent |> Option.isSome then
                cursor.Lookahead <- Some dirent

        return {
            Entries = entries.ToArray()
            HasMore = cursor.Lookahead.IsSome
        }
    }

/// Reads a fresh bounded prefix for reconciliation. Incremental pagination uses a persistent cursor.
let readFileTreeDirectoryPrefix
    (arcPath: string)
    (relativeDirectoryPath: string)
    (pageSize: int)
    : Fable.Core.JS.Promise<FileTreeDirectoryPage> =
    promise {
        if pageSize < 1 then
            invalidArg (nameof pageSize) "Directory page size must be at least one."

        let! directoryPath, cursor = openFileTreeDirectoryCursor arcPath relativeDirectoryPath
        let! readResult = readFileTreeDirectoryCursorPage directoryPath cursor pageSize |> Promise.result
        let! closeResult = cursor.Directory.close () |> Promise.result

        let page =
            match readResult, closeResult with
            | Error error, _
            | _, Error error -> raise error
            | Ok page, Ok() -> page

        // The provider currently lists all objects. Share that snapshot across this prefix;
        // a path-scoped provider API would also bound the metadata lookup to the visible entries.
        let! entries = getFileEntriesWithLfsMetadata arcPath page.Entries
        return { page with Entries = entries }
    }

type FileTreeRootPage = {
    Entries: Dictionary<string, FileEntry>
    HasMore: bool
}

/// Adds or replaces a page without removing direct children that belong to pages not read by this request.
let mergeFileTreeDirectoryPage (entries: FileEntry[]) (fileTree: Dictionary<string, FileEntry>) =
    let nextTree = Dictionary<string, FileEntry>(fileTree)
    entries |> Array.iter (fun entry -> nextTree.[entry.path] <- entry)
    nextTree

/// Applies a fresh listing read from the start of a directory: only an exhaustive
/// snapshot may remove missing children. Incremental cursor pages, including the
/// final page, must use mergeFileTreeDirectoryPage instead.
let applyFreshFileTreeDirectorySnapshot
    (arcPath: string)
    (relativeDirectoryPath: string)
    (snapshot: FileTreeDirectoryPage)
    (fileTree: Dictionary<string, FileEntry>)
    : Dictionary<string, FileEntry> =
    let nextTree = mergeFileTreeDirectoryPage snapshot.Entries fileTree

    if not snapshot.HasMore then
        let directoryPath = resolveFileTreeDirectory arcPath relativeDirectoryPath

        let currentPaths =
            snapshot.Entries
            |> Array.map (fun entry -> PathHelpers.normalizePath entry.path)
            |> Set.ofArray

        // Enumerate the unchanged input while pruning the merged copy.
        fileTree.Values
        |> Seq.iter (fun entry ->
            let path = PathHelpers.normalizePath entry.path

            if
                PathHelpers.pathsEqual (dirname path) directoryPath
                && not (currentPaths.Contains path)
            then
                removePathAndDescendantsInPlace path nextTree
        )

    nextTree
