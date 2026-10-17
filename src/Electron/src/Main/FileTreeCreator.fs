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

// A running import stages its files in a temporary folder of the target directory.
let private shouldIgnoreDirName (name: string) =
    name = ".git" || isTemporaryImportPath name

let private shouldIgnorePath (path: string) =
    let normalizedPath = PathHelpers.normalizeSeparators path
    let tempXlsxPattern = """\.~\$.*\.xlsx$"""
    let temporaryLfsBackupPattern = """\.vcs-lfs-backup-[0-9a-fA-F]{32}$"""

    System.Text.RegularExpressions.Regex.IsMatch(normalizedPath, tempXlsxPattern)
    || System.Text.RegularExpressions.Regex.IsMatch(normalizedPath, temporaryLfsBackupPattern)
    || isLegacyDataMapPath normalizedPath

/// Lists repository-wide large-object state. Returns None when no listing could be produced,
/// so callers retry instead of treating an unavailable session as "no large objects".
let private tryListLargeObjects (repoRoot: string) : Fable.Core.JS.Promise<Map<string, ObjectStateDto> option> = promise {
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
        | None -> return None
        | Some hosted ->
            match hosted.Session.ObjectMaterialization with
            | None -> return Some Map.empty
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
                        |> Some
                | Failed _ -> return None
    with _ ->
        return None
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

/// Shares one repository-wide large-object listing across File Explorer reads.
/// Directory pages, refreshes and watcher batches reuse the snapshot until Invalidate is called
/// after something that can change large-object state, such as a file change or a version control operation.
type LargeObjectMetadataCache() =
    let mutable version = 0
    let mutable listingCount = 0

    let mutable snapshot: (string * Fable.Core.JS.Promise<Map<string, ObjectStateDto>>) option =
        None

    // The modification time of the repository index when the current listing was started.
    let mutable indexModifiedAtListing: float option = None

    let indexModified (repoRoot: string) =
        try
            Some (statSync (join [| repoRoot; ".git"; "index" |])).mtimeMs
        with _ ->
            None

    member _.Invalidate() =
        version <- version + 1
        snapshot <- None

    /// Counts the listings read so far, so a reader can tell that a newer listing arrived.
    member _.ListingCount = listingCount

    /// Drops the listing when the repository index changed since the listing was started, and returns
    /// true then, because the listing should be renewed. Git rewrites the index whenever it checks files
    /// out, including git lfs pull and checkout, so large-object changes made outside Swate show there.
    /// Without a readable index the listing is dropped as well but false is returned: it is read again
    /// only when File Explorer next reads entries.
    member this.InvalidateIfRepositoryChanged(repoRoot: string) =
        let normalizedRepoRoot = normalizeRootPath repoRoot

        match indexModifiedAtListing, indexModified normalizedRepoRoot with
        | Some atListing, Some current when snapshot.IsSome && atListing = current -> false
        | _, Some _ ->
            this.Invalidate()
            true
        | _, None ->
            this.Invalidate()
            false

    /// canOpenSession prevents reopening a version control session for a vault that is closing.
    member private _.Get(repoRoot: string, canOpenSession: unit -> bool) =
        match snapshot with
        | Some(cachedRoot, cached) when PathHelpers.pathsEqual cachedRoot repoRoot -> cached
        | _ when not (canOpenSession ()) -> Fable.Core.JS.Constructors.Promise.resolve Map.empty
        | _ ->
            let capturedVersion = version
            // Taken before listing, so a change made while the listing runs counts as a later change.
            indexModifiedAtListing <- indexModified repoRoot

            let listing = promise {
                let! listed = tryListLargeObjects repoRoot

                match listed with
                | Some largeObjects ->
                    listingCount <- listingCount + 1
                    return largeObjects
                | None ->
                    // An unavailable listing is not kept, so the next read tries again.
                    if capturedVersion = version then
                        snapshot <- None

                    return Map.empty
            }

            snapshot <- Some(repoRoot, listing)
            listing

    /// Enriches an already-read batch of entries. Directory-only batches avoid the listing entirely.
    member this.Enrich(repoRoot: string, entries: FileEntry[], canOpenSession: unit -> bool) = promise {
        if entries |> Array.exists (fun entry -> not entry.isDirectory) then
            let normalizedRepoRoot = normalizeRootPath repoRoot
            let! largeObjectsByRelativePath = this.Get(normalizedRepoRoot, canOpenSession)
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

/// Removes paths and all their descendants from a mutable file tree in place, with the matching of
/// PathHelpers.isSameOrDescendantPath. The tree is scanned once, however many paths are removed.
let internal removePathsAndDescendantsInPlace
    (targetPaths: string seq)
    (fileTree: Dictionary<string, FileEntry>)
    : unit =
    let normalizedTargetPaths =
        targetPaths
        |> Seq.map PathHelpers.normalizePath
        |> Seq.filter (String.IsNullOrWhiteSpace >> not)
        |> Seq.toArray

    if normalizedTargetPaths.Length > 0 then
        let exactTargets = HashSet<string>(normalizedTargetPaths)
        // Descendants match case-insensitively.
        let ancestorTargets =
            HashSet<string>(normalizedTargetPaths |> Array.map _.ToLowerInvariant())

        let hasRemovedAncestor (normalizedPath: string) =
            let lowered = normalizedPath.ToLowerInvariant()
            let mutable separator = lowered.IndexOf '/'
            let mutable found = false

            while not found && separator >= 0 do
                if separator > 0 && ancestorTargets.Contains(lowered.Substring(0, separator)) then
                    found <- true

                separator <- lowered.IndexOf('/', separator + 1)

            found

        let keysToRemove =
            fileTree.Keys
            |> Seq.filter (fun path ->
                let normalizedPath = PathHelpers.normalizePath path
                exactTargets.Contains normalizedPath || hasRemovedAncestor normalizedPath
            )
            |> Seq.toArray

        keysToRemove |> Array.iter (fun path -> fileTree.Remove(path) |> ignore)

/// Removes a path and all descendants from a mutable file tree in place.
let internal removePathAndDescendantsInPlace (targetPath: string) (fileTree: Dictionary<string, FileEntry>) : unit =
    removePathsAndDescendantsInPlace [ targetPath ] fileTree

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

/// Number of entries File Explorer adds to a directory per page.
[<Literal>]
let FileTreeDirectoryPageSize = 100

type FileTreeDirectoryPage = { Entries: FileEntry[]; HasMore: bool }

/// An open directory listing that continues where its previous page ended, so loading the next page
/// never reads the loaded entries again. The accepted lookahead becomes the first entry of the next page.
type FileTreeDirectoryCursor = {
    DirectoryPath: string
    Directory: Directory
    mutable Lookahead: Dirent option
}

let openFileTreeDirectoryCursor (arcPath: string) (relativeDirectoryPath: string) = promise {
    let directoryPath =
        resolveFileTreeDirectory (normalizeRootPath arcPath) relativeDirectoryPath

    let! directory = openDirectoryAsync directoryPath

    return {
        DirectoryPath = directoryPath
        Directory = directory
        Lookahead = None
    }
}

/// Advances an open cursor by up to pageSize accepted entries. Reading continues to the next accepted
/// entry so ignored filesystem entries do not produce a false positive HasMore; that entry is kept as
/// the lookahead of the next page.
let readFileTreeDirectoryCursorPage
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

            let fullPath =
                join [| cursor.DirectoryPath; name |] |> PathHelpers.normalizeSeparators

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

/// Reads the first entryCount entries of a directory. When more entries follow, the still open cursor
/// is returned, so the next page continues after them without reading them again. An exhausted or
/// failed listing is closed.
let readFileTreeDirectoryPrefixWithCursor
    (arcPath: string)
    (relativeDirectoryPath: string)
    (entryCount: int)
    : Fable.Core.JS.Promise<FileTreeDirectoryPage * FileTreeDirectoryCursor option> =
    promise {
        if entryCount < 1 then
            invalidArg (nameof entryCount) "Directory entry count must be at least one."

        let! cursor = openFileTreeDirectoryCursor arcPath relativeDirectoryPath
        let! readResult = readFileTreeDirectoryCursorPage cursor entryCount |> Promise.result

        match readResult with
        | Ok page when page.HasMore -> return page, Some cursor
        | Ok page ->
            do! cursor.Directory.close ()
            return page, None
        | Error error ->
            let! _ = cursor.Directory.close () |> Promise.result
            return raise error
    }

/// Reads the first entryCount entries of a directory and closes the listing.
let readFileTreeDirectoryPrefix
    (arcPath: string)
    (relativeDirectoryPath: string)
    (entryCount: int)
    : Fable.Core.JS.Promise<FileTreeDirectoryPage> =
    promise {
        let! page, cursor = readFileTreeDirectoryPrefixWithCursor arcPath relativeDirectoryPath entryCount

        match cursor with
        | Some cursor -> do! cursor.Directory.close ()
        | None -> ()

        return page
    }

/// Moves every entry of a file tree to a new ARC root, for example after the open ARC folder was renamed.
let remapFileTreeRoot (sourceRoot: string) (targetRoot: string) (fileTree: Dictionary<string, FileEntry>) =
    let normalizedSourceRoot = PathHelpers.normalizePath sourceRoot
    let normalizedTargetRoot = PathHelpers.normalizePath targetRoot
    let remappedTree = Dictionary<string, FileEntry>()

    for entry in fileTree.Values do
        let entryPath = PathHelpers.normalizePath entry.path

        if PathHelpers.isSameOrDescendantPath entryPath normalizedSourceRoot then
            let isRoot = entryPath.Length = normalizedSourceRoot.Length

            let remappedPath =
                normalizedTargetRoot + entryPath.Substring(normalizedSourceRoot.Length)

            remappedTree.[remappedPath] <- {
                entry with
                    path = remappedPath
                    name = if isRoot then basename normalizedTargetRoot else entry.name
            }

    remappedTree

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

        let removedPaths =
            fileTree.Values
            |> Seq.map (fun entry -> PathHelpers.normalizePath entry.path)
            |> Seq.filter (fun path ->
                PathHelpers.pathsEqual (dirname path) directoryPath
                && not (currentPaths.Contains path)
            )
            |> Seq.toArray

        removePathsAndDescendantsInPlace removedPaths nextTree

    nextTree
