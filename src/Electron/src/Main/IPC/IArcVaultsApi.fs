module Main.IPC.ArcVaultsApi

open System
open Swate.Components.Shared
open Swate.Electron.Shared
open Swate.Electron.Shared.IPCTypes
open Swate.Electron.Shared.GitTypes
open Swate.Electron.Shared.FileIOTypes
open Swate.Electron.Shared.FileIOHelper
open Swate.Electron.Shared.RenamePathRules
open Fable.Core
open Fable.Electron
open Fable.Electron.Main
open Fable.Core.JsInterop
open Main
open Main.ArcMerge
open Main.ArcVaultHelper
open Node.Api
open ARCtrl
open Swate.Electron.Shared.DTOs.NoteSearchDto


let private fsPromisesDynamic: obj = importAll "fs/promises"
let private pathDynamic: obj = importAll "path"
let private processDynamic: obj = emitJsExpr () "process"

let private isWindowsPlatform () =
    try
        let platform = processDynamic?platform |> unbox<string>
        platform = "win32"
    with _ ->
        false

[<RequireQualifiedAccess>]
module ArcPathValidation =

    let normalizePathForComparison (pathValue: string) =
        PathHelpers.normalizePathForFsComparison pathValue

    let containsTraversalSegments (pathValue: string) =
        PathHelpers.containsPathTraversalSegments pathValue

    let isSafeRelativePathCandidate (pathValue: string) =
        let normalizedPath = PathHelpers.normalizePath pathValue

        not (String.IsNullOrWhiteSpace normalizedPath)
        && normalizedPath <> "."
        && not (pathDynamic?isAbsolute (normalizedPath) |> unbox<bool>)
        && not (containsTraversalSegments normalizedPath)

    let isWithinRootPath (rootPath: string) (candidatePath: string) =
        let normalizedRootPath =
            pathDynamic?resolve (rootPath) |> unbox<string> |> normalizePathForComparison

        let normalizedCandidatePath =
            pathDynamic?resolve (candidatePath)
            |> unbox<string>
            |> normalizePathForComparison

        normalizedCandidatePath = normalizedRootPath
        || normalizedCandidatePath.StartsWith(normalizedRootPath + "/")

let private tryGetArcRelativePath (arcPath: string) (requestedAbsolutePath: string) =
    let arcRoot = pathDynamic?resolve (arcPath) |> unbox<string>
    let absolutePath = pathDynamic?resolve (requestedAbsolutePath) |> unbox<string>

    let relativePath =
        pathDynamic?relative (arcRoot, absolutePath)
        |> unbox<string>
        |> PathHelpers.normalizePath

    if String.IsNullOrWhiteSpace relativePath || relativePath = "." then
        Ok ""
    elif not (ArcPathValidation.isSafeRelativePathCandidate relativePath) then
        Error(exn $"Path '{requestedAbsolutePath}' is outside the active ARC root.")
    elif not (ArcPathValidation.isWithinRootPath arcRoot absolutePath) then
        Error(exn $"Path '{requestedAbsolutePath}' is outside the active ARC root.")
    else
        Ok relativePath

/// This function resolves a given relative path against the ARC root path and ensures that the resolved absolute path is within the ARC root directory to prevent unauthorized file system access.
let private tryResolveArcRelativePath (arcPath: string) (requestedRelativePath: string) =
    let relativePath = PathHelpers.normalizePath requestedRelativePath

    if String.IsNullOrWhiteSpace relativePath then
        Error(exn "RelativePath must not be empty.")
    elif not (ArcPathValidation.isSafeRelativePathCandidate relativePath) then
        if pathDynamic?isAbsolute (relativePath) |> unbox<bool> then
            Error(exn "RelativePath must not be absolute.")
        else
            Error(exn "RelativePath must not contain path traversal segments.")
    else
        let arcRoot = pathDynamic?resolve (arcPath) |> unbox<string>
        let absolutePath = pathDynamic?resolve (arcRoot, relativePath) |> unbox<string>

        if ArcPathValidation.isWithinRootPath arcRoot absolutePath then
            Ok absolutePath
        else
            Error(exn "RelativePath resolves outside the ARC root.")

let private mkdirRecursiveAsync (directoryPath: string) : JS.Promise<unit> = promise {
    let mkdirPromise =
        fsPromisesDynamic?mkdir (directoryPath, createObj [ "recursive" ==> true ])
        |> unbox<JS.Promise<obj>>

    let! _ = mkdirPromise
    return ()
}

let private writeUtf8FileAsync (absolutePath: string) (content: string) : JS.Promise<unit> = promise {
    let writePromise =
        fsPromisesDynamic?writeFile (absolutePath, content, "utf8")
        |> unbox<JS.Promise<obj>>

    let! _ = writePromise
    return ()
}

let private readUtf8FileAsync (absolutePath: string) : JS.Promise<string> =
    fsPromisesDynamic?readFile (absolutePath, "utf8") |> unbox<JS.Promise<string>>

let private tryGetNodeErrorCode (error: exn) : string option =
    try
        error?code |> unbox<string> |> Option.ofObj
    with _ ->
        None

type private RenameRetryStrategy = {
    DelaysMs: int[]
    IsTransientErrorCode: string option -> bool
}

// Windows file APIs can briefly return lock/contention errors while external processes still hold handles.
// Retry with short backoff to avoid surfacing transient rename failures.
let private renameRetryStrategy = {
    DelaysMs = [| 0; 75; 200; 500 |]
    IsTransientErrorCode =
        function
        | Some "EPERM"
        | Some "EACCES"
        | Some "EBUSY" -> true
        | _ -> false
}

let private renameWithRetriesAsync
    (sourceAbsolutePath: string)
    (targetAbsolutePath: string)
    : JS.Promise<Result<unit, exn>> =
    let rec attempt (attemptIndex: int) = promise {
        if attemptIndex > 0 then
            do! Promise.sleep renameRetryStrategy.DelaysMs.[attemptIndex]

        try
            let! _ =
                fsPromisesDynamic?rename (sourceAbsolutePath, targetAbsolutePath)
                |> unbox<JS.Promise<obj>>

            return Ok()
        with renameError ->
            let errorCode = tryGetNodeErrorCode renameError

            if
                attemptIndex < renameRetryStrategy.DelaysMs.Length - 1
                && renameRetryStrategy.IsTransientErrorCode errorCode
            then
                return! attempt (attemptIndex + 1)
            else
                return Error renameError
    }

    attempt 0

open ARCtrl.Contract

let private withLoadedArcVault<'T>
    (event: IpcMainInvokeEvent)
    (operation: ArcVault -> JS.Promise<Result<'T, exn>>)
    : JS.Promise<Result<'T, exn>> =
    promise {
        let windowId = windowIdFromIpcEvent event

        match ARC_VAULTS.TryGetVault(windowId) with
        | None -> return Error(exn $"The ARC for window id {windowId} should exist")
        | Some vault ->
            match vault.path, vault.arc with
            | Some _, Some _ -> return! operation vault
            | _ -> return Error(exn "ARC is not loaded.")
    }

type private ArcMutationMergeResult = {
    Arc: ARC
}

let private applyArcMutationMergeResult (vault: ArcVault) (mergeResult: ArcMutationMergeResult) =
    vault.SetArc mergeResult.Arc

let private passthroughReloadedArc (reloadedArc: ARC) : JS.Promise<Result<ARC, exn>> =
    promise { return Ok reloadedArc }

let private runArcDiskMutation
    (vault: ArcVault)
    (reloadErrorContext: string)
    (diskMutation: unit -> JS.Promise<Result<unit, exn>>)
    (postReloadArcMutation: ARC -> JS.Promise<Result<ARC, exn>>)
    (mergeReloadedArc: ARC -> Result<ArcMutationMergeResult, exn>)
    : JS.Promise<Result<unit, exn>> =
    promise {
        vault.isBusyWriting <- true

        try
            let! diskMutationResult = diskMutation ()

            match diskMutationResult with
            | Error diskMutationError -> return Error diskMutationError
            | Ok() ->
                do! vault.RefreshFileTree()

                match! ARC.tryLoadAsync vault.path.Value with
                | Error loadError -> return Error(exn $"Unable to reload ARC after {reloadErrorContext}: {loadError}")
                | Ok reloadedArc ->
                    match! postReloadArcMutation reloadedArc with
                    | Error postReloadError -> return Error postReloadError
                    | Ok postMutationArc ->
                        match mergeReloadedArc postMutationArc with
                        | Error mergeError -> return Error mergeError
                        | Ok mergeResult ->
                            applyArcMutationMergeResult vault mergeResult
                            return Ok()
        finally
            vault.isBusyWriting <- false
    }

let private runRenameOperationSafely
    (operation: unit -> JS.Promise<Result<unit, exn>>)
    : JS.Promise<Result<unit, exn>> =
    promise {
        try
            return! operation ()
        with operationError ->
            return Error operationError
    }

let private runWithRenameScopedPolling
    (vault: ArcVault)
    (operation: unit -> JS.Promise<Result<unit, exn>>)
    : JS.Promise<Result<unit, exn>> =
    promise {
        let useScopedPolling = isWindowsPlatform () && vault.watcher.IsSome

        if useScopedPolling |> not then
            return! runRenameOperationSafely operation
        else
            do! vault.StopFileWatcher()
            vault.StartFileWatcher(usePolling = true)

            let! operationResult = runRenameOperationSafely operation

            do! vault.StopFileWatcher()
            vault.StartFileWatcher()

            return operationResult
    }

[<RequireQualifiedAccess>]
module ArcDeleteHelper =

    type MergeResult = { Arc: ARC }

    let private normalizeRelativePathForMerge (path: string) =
        path |> PathHelpers.normalizeRelativePath |> PathHelpers.normalizePath

    let private deduplicateEventPaths (paths: string seq) =
        paths
        |> Seq.distinctBy ArcPathValidation.normalizePathForComparison
        |> Seq.toList

    let private buildPrimaryUnlinkEventPaths (deletedPath: string) (preDeleteFileRelativePaths: string seq) =
        let normalizedDeletedPath = normalizeRelativePathForMerge deletedPath

        preDeleteFileRelativePaths
        |> Seq.map normalizeRelativePathForMerge
        |> Seq.filter (fun relativePath ->
            PathHelpers.isSameOrDescendantPathForFsComparison relativePath normalizedDeletedPath
        )
        |> deduplicateEventPaths

    let buildDeleteUnlinkEvents (deletedPath: string) (preDeleteFileRelativePaths: string seq) : FileEvent list =
        let primaryPaths =
            buildPrimaryUnlinkEventPaths deletedPath preDeleteFileRelativePaths

        let effectivePaths =
            if primaryPaths.Length > 0 then
                primaryPaths
            else
                ArcDeletePathRules.buildFallbackUnlinkPaths deletedPath |> deduplicateEventPaths

        effectivePaths
        |> List.map (fun path -> {
            EventName = EventName.Unlink
            Path = path
        })

    let getPreDeleteFileRelativePaths (arcPath: string) (fileEntries: seq<FileEntry>) =
        fileEntries
        |> Seq.choose (fun fileEntry ->
            if fileEntry.isDirectory then
                None
            else
                tryGetRepoRelativePath arcPath fileEntry.path
                |> Option.map normalizeRelativePathForMerge
        )
        |> Seq.toList

    let mergeReloadedArcAfterDelete
        (deletedPath: string)
        (preDeleteFileRelativePaths: string seq)
        (arcLocal: ARC)
        (reloadedArc: ARC)
        : Result<MergeResult, exn> =
        let arcLocalForMerge = copyArcPreservingStaticHashes arcLocal
        let unlinkEvents = buildDeleteUnlinkEvents deletedPath preDeleteFileRelativePaths
        let mergedArc = ARC.merge arcLocalForMerge reloadedArc unlinkEvents
        syncArcStaticHashes arcLocal mergedArc

        Ok { Arc = mergedArc }

[<RequireQualifiedAccess>]
module ArcRenameHelper =

    type IdentifierRenameSyncPlan = {
        Zone: ArcDeletePathRules.AddZone
        OldIdentifier: string
        NewIdentifier: string
    }

    type RenamePlan = {
        SourcePath: string
        TargetPath: string
        SyncPlan: IdentifierRenameSyncPlan
    }

    type MergeResult = {
        Arc: ARC
    }

    let private normalizeRelativePathForComparison (path: string) =
        path
        |> PathHelpers.normalizeCanonicalRelativePath

    let private applyIdentifierRenameSyncPlan (arc: ARC) (syncPlan: IdentifierRenameSyncPlan) =
        match syncPlan.Zone with
        | ArcDeletePathRules.AddZone.Assays ->
            arc.RenameAssay(syncPlan.OldIdentifier, syncPlan.NewIdentifier)
        | ArcDeletePathRules.AddZone.Studies ->
            arc.RenameStudy(syncPlan.OldIdentifier, syncPlan.NewIdentifier)
        | ArcDeletePathRules.AddZone.Workflows ->
            arc.RenameWorkflow(syncPlan.OldIdentifier, syncPlan.NewIdentifier)
        | ArcDeletePathRules.AddZone.Runs ->
            arc.RenameRun(syncPlan.OldIdentifier, syncPlan.NewIdentifier)

    let syncRenamedEntityIdentifierOnDisk
        (arcPath: string)
        (renamePlan: RenamePlan)
        : JS.Promise<Result<unit, exn>> =
        promise {
            match! ARC.tryLoadAsync arcPath with
            | Error loadError ->
                return
                    Error(
                        exn
                            $"Renamed folder from '{renamePlan.SourcePath}' to '{renamePlan.TargetPath}', but could not reload ARC for identifier sync: {loadError}"
                    )
            | Ok reloadedArc ->
                try
                    applyIdentifierRenameSyncPlan reloadedArc renamePlan.SyncPlan
                    do! reloadedArc.UpdateAsync arcPath
                    return Ok()
                with syncError ->
                    return
                        Error(
                            exn
                                $"Renamed folder from '{renamePlan.SourcePath}' to '{renamePlan.TargetPath}', but failed to sync ARC identifier from '{renamePlan.SyncPlan.OldIdentifier}' to '{renamePlan.SyncPlan.NewIdentifier}': {syncError.Message}"
                        )
        }

    let syncRenamedEntityIdentifierOnLoadedArc
        (arcPath: string)
        (renamePlan: RenamePlan)
        (reloadedArc: ARC)
        : JS.Promise<Result<ARC, exn>> =
        promise {
            try
                applyIdentifierRenameSyncPlan reloadedArc renamePlan.SyncPlan
                do! reloadedArc.UpdateAsync arcPath
                return Ok reloadedArc
            with syncError ->
                return
                    Error(
                        exn
                            $"Renamed folder from '{renamePlan.SourcePath}' to '{renamePlan.TargetPath}', but failed to sync ARC identifier from '{renamePlan.SyncPlan.OldIdentifier}' to '{renamePlan.SyncPlan.NewIdentifier}': {syncError.Message}"
                    )
        }

    let mapRenameDiskError (sourcePath: string) (targetPath: string) (renameError: exn) =
        match tryGetNodeErrorCode renameError with
        | Some "EPERM"
        | Some "EACCES" ->
            exn
                $"Cannot rename '{sourcePath}' to '{targetPath}'. Windows reported a permission or file-lock conflict. If the destination already exists, choose a different name and close apps that may be using these paths."
        | Some "ENOTEMPTY"
        | Some "EEXIST" ->
            exn
                $"Cannot rename '{sourcePath}' to '{targetPath}' because the destination already exists."
        | Some "ENOENT" ->
            exn
                $"Cannot rename '{sourcePath}' because the source path no longer exists on disk."
        | _ -> renameError

    let private validateRenamePathClassification (classification: ArcDeletePathRules.RenamePathClassification) =
        match classification with
        | ArcDeletePathRules.RenamePathClassification.EntityFolderTarget _ -> Ok()
        | ArcDeletePathRules.RenamePathClassification.RootTarget ->
            Error(exn "Renaming the ARC root is not allowed.")
        | ArcDeletePathRules.RenamePathClassification.DisallowedTarget _ ->
            Error(exn "Rename path must not contain path traversal segments.")
        | ArcDeletePathRules.RenamePathClassification.ProtectedTarget _ ->
            Error(exn "Renaming protected files (for example .gitkeep or readme.md) is not allowed.")
        | ArcDeletePathRules.RenamePathClassification.InvestigationFileTarget _ ->
            Error(exn "Renaming the investigation file is not supported.")
        | ArcDeletePathRules.RenamePathClassification.AddZoneRootTarget _ ->
            Error(exn "Renaming add-zone root folders (studies/, assays/, workflows/, runs/) is not allowed.")
        | ArcDeletePathRules.RenamePathClassification.CanonicalEntityFileTarget _
        | ArcDeletePathRules.RenamePathClassification.CanonicalDataMapFileTarget _ ->
            Error(exn "Renaming canonical ARC files is not supported. Rename the containing ARC entity folder instead.")
        | ArcDeletePathRules.RenamePathClassification.GenericTarget _ ->
            Error(exn "Renaming generic files or folders is not supported from the ARC file tree.")

    let private tryGetEntityFolderRenameTarget (classification: ArcDeletePathRules.RenamePathClassification) =
        match classification with
        | ArcDeletePathRules.RenamePathClassification.EntityFolderTarget(zone, identifier, normalizedRelativePath) ->
            Some(zone, identifier, normalizedRelativePath)
        | _ -> None

    let tryBuildRenamePlan (request: RenamePathRequest) : Result<RenamePlan, exn> =
        let requestedRelativePath = normalizeRelativePathForComparison request.relativePath
        let sourceClassification = ArcDeletePathRules.classifyRenameTarget requestedRelativePath

        match validateRenamePathClassification sourceClassification with
        | Error validationError -> Error validationError
        | Ok() ->
            let resolvedSourcePath = ArcDeletePathRules.resolveRenameSourcePath requestedRelativePath

            match tryBuildRenameTargetPath resolvedSourcePath request.newName with
            | Error targetPathError -> Error(exn targetPathError)
            | Ok targetPath ->
                let targetClassification = ArcDeletePathRules.classifyRenameTarget targetPath

                match validateRenamePathClassification targetClassification with
                | Error validationError -> Error validationError
                | Ok() ->
                    match
                        tryGetEntityFolderRenameTarget sourceClassification,
                        tryGetEntityFolderRenameTarget targetClassification
                    with
                    | Some(sourceZone, sourceIdentifier, sourcePath), Some(targetZone, targetIdentifier, targetPath) ->
                        if sourceZone <> targetZone then
                            Error(
                                exn
                                    $"Renamed folder from '{sourcePath}' to '{targetPath}', but ARC identifier sync only supports renames within the same entity zone."
                            )
                        else
                            Ok {
                                SourcePath = sourcePath
                                TargetPath = targetPath
                                SyncPlan = {
                                    Zone = sourceZone
                                    OldIdentifier = sourceIdentifier
                                    NewIdentifier = targetIdentifier
                                }
                            }
                    | _ ->
                        Error(
                            exn
                                $"Renamed folder from '{resolvedSourcePath}' to '{targetPath}', but ARC identifier sync requires validated ARC entity folder paths."
                        )

    let mergeReloadedArcAfterRename
        (renamePlan: RenamePlan)
        (arcLocal: ARC)
        (reloadedArc: ARC)
        : Result<MergeResult, exn> =
        try
            let arcLocalForMerge =
                if arcLocal.hasInMemoryChanges() then
                    let renamedLocalArc = arcLocal.Copy()
                    applyIdentifierRenameSyncPlan renamedLocalArc renamePlan.SyncPlan
                    renamedLocalArc
                else
                    arcLocal

            let mergedArc = ARC.merge arcLocalForMerge reloadedArc []
            Ok { Arc = mergedArc }
        with mergeError ->
            Error(
                exn
                    $"Unable to merge renamed ARC entity from '{renamePlan.SourcePath}' to '{renamePlan.TargetPath}': {mergeError.Message}"
            )


/// This depends on the types in this file, but the types on this file must call this to bind IPC calls :/
let api (event: IpcMainInvokeEvent) : IPCTypes.IArcVaultsApi = {
    openARC =
        fun () -> promise {
            let window = dialogParentFromIpcEvent event

            let! r =
                dialog.showOpenDialog (
                    ?window = window,
                    properties = [|
                        Enums.Dialog.ShowOpenDialog.Options.Properties.OpenDirectory
                    |]
                )

            if r.canceled then
                return Error(exn "Cancelled")
            elif r.filePaths.Length <> 1 then
                return Error(exn "Not exactly one path")
            else
                let arcPath = r.filePaths |> Array.exactlyOne
                let windowId = windowIdFromIpcEvent event
                let! disposition = ARC_VAULTS.OpenOrFocusArc(windowId, arcPath)
                return Ok(ArcOpenDisposition.path disposition)
        }
    openARCByPath =
        fun (arcPath: string) -> promise {
            try
                let windowId = windowIdFromIpcEvent event
                let! disposition = ARC_VAULTS.OpenOrFocusArc(windowId, arcPath)
                return Ok(ArcOpenDisposition.path disposition)
            with e ->
                return Error e
        }
    createARC =
        fun (identifier: string) -> promise {
            let window = dialogParentFromIpcEvent event

            let! r =
                dialog.showOpenDialog (
                    ?window = window,
                    properties = [|
                        Enums.Dialog.ShowOpenDialog.Options.Properties.OpenDirectory
                    |]
                )

            if r.canceled then
                return Error(exn "Cancelled")
            elif r.filePaths.Length <> 1 then
                return Error(exn "Not exactly one path")
            else
                let arcContainerPath = r.filePaths |> Array.exactlyOne
                let arcPath = ARCtrl.ArcPathHelper.combine arcContainerPath identifier
                let windowId = windowIdFromIpcEvent event
                let! disposition = ARC_VAULTS.CreateOrFocusArc(windowId, arcPath, identifier)
                return Ok(ArcOpenDisposition.path disposition)
        }
    closeARC =
        fun () -> promise {
            try
                let windowId = windowIdFromIpcEvent event
                let vault = ARC_VAULTS.TryGetVault(windowId)

                // Ensure the ARC stays in recent list before disposal marks it inactive.
                if vault.IsSome && vault.Value.path.IsSome then
                    RECENT_ARCS.Add(vault.Value.path.Value) |> ignore

                ARC_VAULTS.DisposeVault(windowId)
                return Ok()
            with e ->
                return Error e
        }
    getOpenPath =
        fun () -> promise {
            let windowId = windowIdFromIpcEvent event
            let vault = ARC_VAULTS.TryGetVault(windowId)

            return vault |> Option.bind (fun v -> v.path)
        }
    openArcFolderInFileExplorer =
        fun () -> promise {
            try
                let windowId = windowIdFromIpcEvent event

                match ARC_VAULTS.TryGetVault(windowId) with
                | None -> return Error(exn $"The ARC for window id {windowId} should exist")
                | Some vault ->
                    match vault.path with
                    | None -> return Error(exn "ARC is not loaded.")
                    | Some arcPath ->
                        let! shellOpenResult = shell.openPath arcPath

                        if String.IsNullOrWhiteSpace shellOpenResult then
                            return Ok()
                        else
                            return Error(exn $"Could not open ARC folder in file explorer: {shellOpenResult}")
            with e ->
                return Error e
        }
    getRecentARCs = fun _ -> promise { return RECENT_ARCS.Get() }
    removeRecentARC =
        fun arcpointer -> promise {
            try
                RECENT_ARCS.Remove(arcpointer.path) |> ignore
                ARC_VAULTS.BroadcastRecentARCs()
                return Ok()
            with e ->
                return Error e
        }
    pickArcPaths =
        fun () -> promise {
            try
                let windowId = windowIdFromIpcEvent event

                match ARC_VAULTS.TryGetVault(windowId) with
                | None -> return Error(exn $"The ARC for window id {windowId} should exist")
                | Some vault ->
                    match vault.path with
                    | None -> return Error(exn "ARC is not loaded.")
                    | Some arcPath ->
                        let properties = [|
                            Enums.Dialog.ShowOpenDialog.Options.Properties.OpenFile
                            Enums.Dialog.ShowOpenDialog.Options.Properties.MultiSelections
                        |]

                        let window = dialogParentFromIpcEvent event

                        let! result =
                            dialog.showOpenDialog (?window = window, properties = properties, defaultPath = arcPath)

                        if result.canceled then
                            return Error(exn "Cancelled")
                        else
                            let relativePaths = result.filePaths |> Array.map (tryGetArcRelativePath arcPath)

                            match relativePaths |> Array.tryFind Result.isError with
                            | Some(Error pathError) -> return Error pathError
                            | _ ->
                                return
                                    relativePaths
                                    |> Array.choose (
                                        function
                                        | Ok path when String.IsNullOrWhiteSpace path -> None
                                        | Ok path -> Some path
                                        | Error _ -> None
                                    )
                                    |> Ok
            with e ->
                return Error e
        }
    pickDirectory =
        fun () -> promise {
            try
                let properties = [|
                    Enums.Dialog.ShowOpenDialog.Options.Properties.OpenDirectory
                |]

                let window = dialogParentFromIpcEvent event

                let! result = dialog.showOpenDialog (?window = window, properties = properties)

                if result.canceled then
                    return Error(exn "Cancelled")
                elif result.filePaths.Length <> 1 then
                    return Error(exn "Not exactly one path")
                else
                    return Ok(result.filePaths |> Array.exactlyOne)
            with e ->
                return Error(exn $"Could not pick directory: {e.Message}")
        }
    pickAbsolutePaths =
        fun () -> promise {
            try
                let properties = [|
                    Enums.Dialog.ShowOpenDialog.Options.Properties.OpenFile
                    Enums.Dialog.ShowOpenDialog.Options.Properties.MultiSelections
                |]

                let window = dialogParentFromIpcEvent event

                let! result = dialog.showOpenDialog (?window = window, properties = properties)

                if result.canceled then
                    return Error(exn "Cancelled")
                else
                    return Ok result.filePaths
            with e ->
                return Error(exn $"Could not pick files: {e.Message}")
        }
    pickExternalTextFiles =
        fun _ -> promise {
            try
                let properties = [|
                    Enums.Dialog.ShowOpenDialog.Options.Properties.OpenFile
                    Enums.Dialog.ShowOpenDialog.Options.Properties.MultiSelections
                |]

                let filters = [|
                    FileFilter("Delimited text files", [| "csv"; "tsv"; "txt" |])
                |]

                let window = dialogParentFromIpcEvent event

                let! result = dialog.showOpenDialog (?window = window, properties = properties, filters = filters)

                if result.canceled then
                    return Error(exn "Cancelled")
                else
                    let importedFiles = ResizeArray<ImportedTextFile>()

                    for filePath in result.filePaths do
                        let absolutePath = pathDynamic?resolve (filePath) |> unbox<string>
                        let! content = readUtf8FileAsync absolutePath

                        importedFiles.Add {
                            Name = path.basename absolutePath
                            Content = content
                        }

                    return Ok(importedFiles.ToArray())
            with e ->
                return Error(exn $"Could not import external text files: {e.Message}")
        }
    getFileTree =
        fun () -> promise {
            try
                let windowId = windowIdFromIpcEvent event

                match ARC_VAULTS.TryGetVault(windowId) with
                | None -> return Error(exn $"The ARC for window id {windowId} should exist")
                | Some vault ->
                    let! fileTree = vault.GetRendererFileTreeSnapshot()
                    return Ok fileTree
            with e ->
                return Error e
        }
    readNotes =
        fun () -> promise {
            try
                let windowId = windowIdFromIpcEvent event

                match ARC_VAULTS.TryGetVault(windowId) with
                | None -> return Error(exn $"The ARC for window id {windowId} should exist")
                | Some vault ->
                    match vault.path with
                    | None -> return Error(exn "ARC is not loaded.")
                    | Some arcPath ->
                        let! fileEntries =
                            if vault.fileTree.Count > 0 then
                                promise { return vault.fileTree.Values |> Seq.toArray }
                            else
                                getFileEntries arcPath

                        let! notes = Main.NoteSearchReader.readNotes arcPath fileEntries
                        return Ok(notes |> Array.map NoteSearchNoteDto.ofNote)
            with e ->
                return Error e
        }
    saveArcFile =
        fun () -> promise {
            try
                return!
                    withLoadedArcVault
                        event
                        (fun vault -> promise {
                            match! vault.WriteArc() with
                            | Error saveError -> return Error saveError
                            | Ok() ->
                                do! vault.RefreshFileTree()
                                return Ok()
                        })
            with e ->
                return Error e
        }
    setArcFileInMemory =
        fun (request: FileContentDTO) -> promise {
            try
                return!
                    withLoadedArcVault
                        event
                        (fun vault -> promise {
                            match vault.UpdateArcByFileContentDTO request with
                            | Error saveError -> return Error saveError
                            | Ok() -> return Ok()
                        })
            with e ->
                return Error e
        }
    applyArcFileAndSave =
        fun (request: FileContentDTO) -> promise {
            try
                return!
                    withLoadedArcVault
                        event
                        (fun vault -> promise {
                            match! vault.ApplyArcFileAndSave(request) with
                            | Error saveError -> return Error saveError
                            | Ok() ->
                                do! vault.RefreshFileTree()
                                return Ok()
                        })
            with e ->
                return Error e
        }
    getHasUnsavedArcChanges =
        fun () -> promise {
            try
                return! withLoadedArcVault event (fun vault -> promise { return Ok vault.hasUnsavedArcChanges })
            with e ->
                return Error e
        }
    deletePath =
        fun (relativePath: string) -> promise {
            try
                return!
                    withLoadedArcVault event (fun vault ->
                        promise {
                            let arcPath = vault.path.Value
                            let arcLocal = vault.arc.Value
                            let normalizedRelativePath = PathHelpers.normalizeRelativePath relativePath

                            if ArcDeletePathRules.isDeletePathAllowed normalizedRelativePath |> not then
                                return
                                    Error(
                                        exn
                                            "Deletion is only allowed for descendants under studies/, assays/, workflows/, or runs/."
                                    )
                            else
                                match tryResolveArcRelativePath arcPath normalizedRelativePath with
                                | Error pathError -> return Error pathError
                                | Ok absolutePath ->
                                    let preDeleteFileRelativePaths =
                                        ArcDeleteHelper.getPreDeleteFileRelativePaths arcPath vault.fileTree.Values

                                    let deleteDiskMutation () =
                                        promise {
                                            try
                                                let! _ =
                                                    fsPromisesDynamic?rm (
                                                        absolutePath,
                                                        createObj [ "recursive" ==> true; "force" ==> false ]
                                                    )
                                                    |> unbox<JS.Promise<obj>>

                                                return Ok()
                                            with deleteError ->
                                                return Error deleteError
                                        }

                                    return!
                                        runArcDiskMutation
                                            vault
                                            $"deleting '{normalizedRelativePath}'"
                                            deleteDiskMutation
                                            passthroughReloadedArc
                                            (fun reloadedArc ->
                                                ArcDeleteHelper.mergeReloadedArcAfterDelete
                                                    normalizedRelativePath
                                                    preDeleteFileRelativePaths
                                                    arcLocal
                                                    reloadedArc
                                                |> Result.map (fun mergeResult -> { Arc = mergeResult.Arc }))
                        }
                    )
            with e ->
                return Error e
        }
    renamePath =
        fun (request: RenamePathRequest) -> promise {
            try
                return!
                    withLoadedArcVault event (fun vault ->
                        promise {
                            let arcPath = vault.path.Value
                            let arcLocal = vault.arc.Value

                            match ArcRenameHelper.tryBuildRenamePlan request with
                            | Error validationError -> return Error validationError
                            | Ok renamePlan ->
                                match
                                    tryResolveArcRelativePath arcPath renamePlan.SourcePath,
                                    tryResolveArcRelativePath arcPath renamePlan.TargetPath
                                with
                                | Error pathError, _
                                | _, Error pathError -> return Error pathError
                                | Ok sourceAbsolutePath, Ok targetAbsolutePath ->
                                    let renameDiskMutation () =
                                        promise {
                                            let! renameResult =
                                                renameWithRetriesAsync sourceAbsolutePath targetAbsolutePath

                                            match renameResult with
                                            | Ok() -> return Ok()
                                            | Error renameError ->
                                                return
                                                    Error(
                                                        ArcRenameHelper.mapRenameDiskError
                                                            renamePlan.SourcePath
                                                            renamePlan.TargetPath
                                                            renameError
                                                    )
                                        }

                                    return!
                                        runWithRenameScopedPolling
                                            vault
                                            (fun () ->
                                                runArcDiskMutation
                                                    vault
                                                    $"renaming '{renamePlan.SourcePath}' to '{renamePlan.TargetPath}'"
                                                    renameDiskMutation
                                                    (ArcRenameHelper.syncRenamedEntityIdentifierOnLoadedArc arcPath renamePlan)
                                                    (fun reloadedArc ->
                                                        ArcRenameHelper.mergeReloadedArcAfterRename
                                                            renamePlan
                                                            arcLocal
                                                            reloadedArc
                                                        |> Result.map (fun mergeResult -> { Arc = mergeResult.Arc })))
                        }
                    )
            with e ->
                return Error e
        }
    writeFile =
        fun (request: FileContentDTO) -> promise {
            try
                let windowId = windowIdFromIpcEvent event

                match ARC_VAULTS.TryGetVault(windowId) with
                | None -> return Error(exn $"The ARC for window id {windowId} should exist")
                | Some vault ->
                    match vault.path with
                    | None -> return Error(exn "ARC is not loaded.")
                    | Some arcPath ->
                        match tryResolveArcRelativePath arcPath request.path with
                        | Error pathError -> return Error pathError
                        | Ok absolutePath ->
                            vault.isBusyWriting <- true

                            try
                                match request.fileType with
                                | DTOType.DTOTypeIsPlainTextVariant ->
                                    let directoryPath = path.dirname absolutePath
                                    do! mkdirRecursiveAsync directoryPath
                                    do! writeUtf8FileAsync absolutePath request.content
                                    do! vault.RefreshFileTree()
                                    return Ok()
                                | DTOType.CLI -> return Error(exn "Direct writing of CLI files is not supported.")
                                | DTOType.DTOTypeIsISAFileVariant ->
                                    return
                                        Error(
                                            exn
                                                "Direct writing of ARC content files is not supported. Use saveArcFile for these file types to ensure ARC integrity."
                                        )
                                | _ -> return Error(exn $"Unsupported DTOType for writing: {request.fileType}")
                            finally
                                vault.isBusyWriting <- false
            with e ->
                return Error e
        }
    openFile =
        fun (relativePath: string) -> promise {
            let windowId = windowIdFromIpcEvent event

            match ARC_VAULTS.TryGetVault(windowId) with
            | None -> return Error(exn $"The ARC for window id {windowId} should exist")
            | Some vault when vault.arc.IsSome ->
                let arcfileDTO = FileContentDTO.fromArcByPath relativePath vault.arc.Value

                match arcfileDTO with
                | Some dto -> return Ok dto
                | _ ->
                    // Fallback to text preview for unknown file types
                    try
                        let absolutePath = tryResolveArcRelativePath vault.path.Value relativePath

                        match absolutePath with
                        | Error pathError -> return Error pathError
                        | Ok path ->
                            let content = fs.readFileSync (path, "utf8")

                            let dto =
                                FileContentDTO.create ARCtrl.Contract.DTOType.PlainText content relativePath

                            return Ok dto
                    with e ->
                        return Error(exn $"Could not read file {relativePath}: {e.Message}")
            | _ -> return Error(exn "ARC is not loaded.")
        }
    runGitLfs =
        fun (request: GitLfsRequest) -> promise {
            let windowId = windowIdFromIpcEvent event

            match ARC_VAULTS.TryGetVault(windowId) with
            | None -> return Error(exn $"The ARC for window id {windowId} should exist")
            | Some vault ->
                match vault.path with
                | None -> return Error(exn "ARC is not loaded.")
                | Some arcPath ->
                    // Always enforce the active ARC root to avoid running against arbitrary repos.
                    let enforcedRequest = { request with RepoPath = arcPath }
                    let! result = GitLfs.runChannel vault.window enforcedRequest

                    match result with
                    | Error e ->
                        Swate.Components.console.log ($"Error: {e.Message}")
                        return Error e
                    | Ok successResult ->
                        match enforcedRequest.Command with
                        | Track
                        | Untrack -> do! vault.RefreshFileTree()
                        | _ -> ()

                        return Ok successResult
        }
    cancelGitLfs = fun (requestId: string) -> GitLfs.cancelChannel requestId
    resolveCloseRequest =
        fun (decision: IPCTypesHelper.SaveBeforeQuitDecision) -> promise {
            try
                let windowId = windowIdFromIpcEvent event
                return! ARC_VAULTS.ResolveCloseRequest(windowId, decision)
            with e ->
                return Error e
        }
}

