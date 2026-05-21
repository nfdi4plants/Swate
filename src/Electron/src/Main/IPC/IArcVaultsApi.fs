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

let private pathExistsAsync (absolutePath: string) : JS.Promise<bool> = promise {
    try
        let! _ = fsPromisesDynamic?access (absolutePath) |> unbox<JS.Promise<obj>>
        return true
    with _ ->
        return false
}

let private isDirectoryAsync (absolutePath: string) : JS.Promise<bool> = promise {
    let! stats = fsPromisesDynamic?stat (absolutePath) |> unbox<JS.Promise<obj>>
    return stats?isDirectory () |> unbox<bool>
}

let private mkdirAsync (directoryPath: string) : JS.Promise<unit> = promise {
    let! _ = fsPromisesDynamic?mkdir (directoryPath) |> unbox<JS.Promise<obj>>
    return ()
}

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

[<RequireQualifiedAccess>]
module ArcDeleteHelper =

    let private formatContractErrors (errors: string[]) =
        errors |> Array.map string |> String.concat "\n"

    let private removeFromMemory zone identifier (arc: ARC) =
        match zone with
        | ArcDeletePathRules.AddZone.Assays ->
            if arc.ContainsAssay(identifier) then
                arc.RemoveAssay(identifier)
        | ArcDeletePathRules.AddZone.Studies ->
            if arc.ContainsStudy(identifier) then
                arc.RemoveStudy(identifier)
        | ArcDeletePathRules.AddZone.Workflows ->
            if arc.ContainsWorkflow(identifier) then
                arc.DeleteWorkflow(identifier)
        | ArcDeletePathRules.AddZone.Runs ->
            if arc.ContainsRun(identifier) then
                arc.DeleteRun(identifier)

    let private removeFromDiskAsync zone identifier arcPath (arc: ARC) =
        match zone with
        | ArcDeletePathRules.AddZone.Assays -> arc.TryRemoveAssayAsync(arcPath, identifier)
        | ArcDeletePathRules.AddZone.Studies -> arc.TryRemoveStudyAsync(arcPath, identifier)
        | ArcDeletePathRules.AddZone.Workflows -> arc.TryRemoveWorkflowAsync(arcPath, identifier)
        | ArcDeletePathRules.AddZone.Runs -> arc.TryRemoveRunAsync(arcPath, identifier)

    let private tryGetEntityDeleteTarget relativePath =
        match ArcDeletePathRules.classifyDeleteTarget relativePath with
        | ArcDeletePathRules.DeletePathClassification.EntityFolderTarget(zone, identifier, normalizedRelativePath) ->
            Ok(zone, identifier, normalizedRelativePath)
        | ArcDeletePathRules.DeletePathClassification.CanonicalFileTarget(
            ArcDeletePathRules.CanonicalArcFileTarget.EntityFile(zone, identifier),
            normalizedRelativePath
          ) ->
            Ok(zone, identifier, normalizedRelativePath)
        | ArcDeletePathRules.DeletePathClassification.CanonicalFileTarget(
            ArcDeletePathRules.CanonicalArcFileTarget.DataMapFile _,
            _
          )
        | ArcDeletePathRules.DeletePathClassification.AddZoneDescendantTarget _ ->
            Error(exn "Generic filesystem delete paths do not use ARC entity delete contracts.")
        | ArcDeletePathRules.DeletePathClassification.CanonicalFileTarget(
            ArcDeletePathRules.CanonicalArcFileTarget.InvestigationFile,
            _
          ) ->
            Error(exn "Deleting the investigation file is not supported.")
        | ArcDeletePathRules.DeletePathClassification.ProtectedTarget _ ->
            Error(exn "Deleting protected files (for example .gitkeep or readme.md) is not allowed.")
        | ArcDeletePathRules.DeletePathClassification.DisallowedTarget _ ->
            Error(exn "Deletion is only allowed for descendants under studies/, assays/, workflows/, or runs/.")

    let deleteArcEntityAsync (arcPath: string) (relativePath: string) (arc: ARC) : JS.Promise<Result<ARC, exn>> =
        promise {
            match tryGetEntityDeleteTarget relativePath with
            | Error validationError -> return Error validationError
            | Ok(zone, identifier, normalizedRelativePath) ->
                try
                    match! ARC.tryLoadAsync arcPath with
                    | Error errors ->
                        return
                            Error(
                                exn
                                    $"Could not load ARC from disk before deleting '{normalizedRelativePath}': {formatContractErrors errors}"
                            )
                    | Ok diskArc ->
                        match! removeFromDiskAsync zone identifier arcPath diskArc with
                        | Error errors ->
                            return
                                Error(
                                    exn
                                        $"Could not delete ARC entity at '{normalizedRelativePath}': {formatContractErrors errors}"
                                )
                        | Ok _ ->
                            removeFromMemory zone identifier arc
                            arc.UpdateFileSystem()
                            return Ok arc
                with deleteError ->
                    return
                        Error(
                            exn
                                $"Could not delete ARC entity at '{normalizedRelativePath}': {deleteError.Message}"
                        )
        }

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

    let private normalizeRelativePathForComparison (path: string) =
        path
        |> PathHelpers.normalizeCanonicalRelativePath

    let renameArcEntityAsync (arcPath: string) (renamePlan: RenamePlan) (arc: ARC) : JS.Promise<Result<ARC, exn>> =
        let syncPlan = renamePlan.SyncPlan

        let renameAsync =
            match syncPlan.Zone with
            | ArcDeletePathRules.AddZone.Assays ->
                arc.RenameAssayAsync(arcPath, syncPlan.OldIdentifier, syncPlan.NewIdentifier)
            | ArcDeletePathRules.AddZone.Studies ->
                arc.RenameStudyAsync(arcPath, syncPlan.OldIdentifier, syncPlan.NewIdentifier)
            | ArcDeletePathRules.AddZone.Workflows ->
                arc.RenameWorkflowAsync(arcPath, syncPlan.OldIdentifier, syncPlan.NewIdentifier)
            | ArcDeletePathRules.AddZone.Runs ->
                arc.RenameRunAsync(arcPath, syncPlan.OldIdentifier, syncPlan.NewIdentifier)

        promise {
            try
                do! renameAsync
                return Ok arc
            with renameError ->
                return
                    Error(
                        exn
                            $"Could not rename ARC entity from '{renamePlan.SourcePath}' to '{renamePlan.TargetPath}': {renameError.Message}"
                    )
        }

    let private validateEntityRenameSourceClassification (classification: ArcDeletePathRules.RenamePathClassification) =
        match classification with
        | ArcDeletePathRules.RenamePathClassification.EntityFolderTarget(zone, identifier, normalizedRelativePath) ->
            Ok(zone, identifier, normalizedRelativePath)
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
            Error(exn "Renaming generic files or folders uses the generic filesystem rename path.")

    let tryBuildRenamePlan (request: RenamePathRequest) : Result<RenamePlan, exn> =
        let requestedRelativePath = normalizeRelativePathForComparison request.relativePath
        let sourceClassification = ArcDeletePathRules.classifyRenameTarget requestedRelativePath

        match validateEntityRenameSourceClassification sourceClassification with
        | Error validationError -> Error validationError
        | Ok(sourceZone, sourceIdentifier, sourcePath) ->
            match tryBuildRenameTargetPath sourcePath request.newName with
            | Error targetPathError -> Error(exn targetPathError)
            | Ok targetPath ->
                let targetIdentifier = PathHelpers.getNameFromPath targetPath

                Ok {
                    SourcePath = sourcePath
                    TargetPath = targetPath
                    SyncPlan = {
                        Zone = sourceZone
                        OldIdentifier = sourceIdentifier
                        NewIdentifier = targetIdentifier
                    }
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

[<RequireQualifiedAccess>]
module ArcFileSystemHelper =

    type CreateFileSystemItemPlan = {
        ParentPath: string
        TargetPath: string
        Kind: FileSystemItemKind
    }

    type GenericRenamePlan = {
        SourcePath: string
        TargetPath: string
    }

    let private normalizeRelativePathForComparison (path: string) =
        path
        |> PathHelpers.normalizeCanonicalRelativePath

    let private resolveArcRelativePathPair arcPath firstRelativePath secondRelativePath =
        match
            tryResolveArcRelativePath arcPath firstRelativePath,
            tryResolveArcRelativePath arcPath secondRelativePath
        with
        | Error pathError, _
        | _, Error pathError -> Error pathError
        | Ok firstAbsolutePath, Ok secondAbsolutePath -> Ok(firstAbsolutePath, secondAbsolutePath)

    let private ensureTargetDoesNotExist targetAbsolutePath errorMessage = promise {
        let! targetExists = pathExistsAsync targetAbsolutePath

        if targetExists then
            return Error(exn errorMessage)
        else
            return Ok()
    }

    let private createTargetAsync kind targetAbsolutePath =
        match kind with
        | FileSystemItemKind.File -> writeUtf8FileAsync targetAbsolutePath ""
        | FileSystemItemKind.Folder -> mkdirAsync targetAbsolutePath

    let tryBuildCreateFileSystemItemPlan
        (request: CreateFileSystemItemRequest)
        : Result<CreateFileSystemItemPlan, exn> =
        match tryBuildGenericFileSystemChildPath request.parentPath request.name with
        | Error validationError -> Error(exn validationError)
        | Ok targetPath ->
            Ok {
                ParentPath = PathHelpers.normalizeCanonicalRelativePath request.parentPath
                TargetPath = targetPath
                Kind = request.kind
            }

    let tryBuildGenericRenamePlan (request: RenamePathRequest) : Result<GenericRenamePlan, exn> =
        let requestedRelativePath = normalizeRelativePathForComparison request.relativePath

        match tryBuildGenericFileSystemRenameTargetPath requestedRelativePath request.newName with
        | Error validationError -> Error(exn validationError)
        | Ok targetPath ->
            Ok {
                SourcePath = requestedRelativePath
                TargetPath = targetPath
            }

    let createFileSystemItemOnDisk
        (arcPath: string)
        (request: CreateFileSystemItemRequest)
        : JS.Promise<Result<string, exn>> =
        promise {
            match tryBuildCreateFileSystemItemPlan request with
            | Error validationError -> return Error validationError
            | Ok plan ->
                match resolveArcRelativePathPair arcPath plan.ParentPath plan.TargetPath with
                | Error pathError -> return Error pathError
                | Ok(parentAbsolutePath, targetAbsolutePath) ->
                    try
                        let! parentIsDirectory = isDirectoryAsync parentAbsolutePath

                        if parentIsDirectory |> not then
                            return Error(exn $"Cannot create item because '{plan.ParentPath}' is not a folder.")
                        else
                            let! targetCheck =
                                ensureTargetDoesNotExist
                                    targetAbsolutePath
                                    $"A file or folder already exists at '{plan.TargetPath}'."

                            match targetCheck with
                            | Error conflictError -> return Error conflictError
                            | Ok() ->
                                do! createTargetAsync plan.Kind targetAbsolutePath
                                return Ok plan.TargetPath
                    with createError ->
                        return Error createError
        }

    let renameGenericFileSystemItemOnDisk
        (arcPath: string)
        (request: RenamePathRequest)
        : JS.Promise<Result<unit, exn>> =
        promise {
            match tryBuildGenericRenamePlan request with
            | Error validationError -> return Error validationError
            | Ok genericRenamePlan ->
                match resolveArcRelativePathPair arcPath genericRenamePlan.SourcePath genericRenamePlan.TargetPath with
                | Error pathError -> return Error pathError
                | Ok(sourceAbsolutePath, targetAbsolutePath) ->
                    let! targetCheck =
                        ensureTargetDoesNotExist
                            targetAbsolutePath
                            $"Cannot rename '{genericRenamePlan.SourcePath}' to '{genericRenamePlan.TargetPath}' because the destination already exists."

                    match targetCheck with
                    | Error conflictError -> return Error conflictError
                    | Ok() ->
                        let! renameResult =
                            renameWithRetriesAsync sourceAbsolutePath targetAbsolutePath

                        match renameResult with
                        | Ok() -> return Ok()
                        | Error renameError ->
                            return
                                Error(
                                    ArcRenameHelper.mapRenameDiskError
                                        genericRenamePlan.SourcePath
                                        genericRenamePlan.TargetPath
                                        renameError
                                )
        }

    let deleteGenericFileSystemItemOnDisk (arcPath: string) (relativePath: string) : JS.Promise<Result<unit, exn>> =
        promise {
            let normalizedRelativePath = normalizeRelativePathForComparison relativePath

            let isGenericDeleteTarget =
                match ArcDeletePathRules.classifyDeleteTarget normalizedRelativePath with
                | ArcDeletePathRules.DeletePathClassification.CanonicalFileTarget(
                    ArcDeletePathRules.CanonicalArcFileTarget.DataMapFile _,
                    _
                  )
                | ArcDeletePathRules.DeletePathClassification.AddZoneDescendantTarget _ ->
                    ArcDeletePathRules.isDeletePathAllowed normalizedRelativePath
                | _ -> false

            if not isGenericDeleteTarget then
                return Error(exn "Generic filesystem delete is only supported for non-entity add-zone descendants.")
            else
                match tryResolveArcRelativePath arcPath normalizedRelativePath with
                | Error pathError -> return Error pathError
                | Ok absolutePath ->
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

let private tryResolveExistingArcRelativePath (arcPath: string) (relativePath: string) : JS.Promise<Result<string, exn>> =
    promise {
        match tryResolveArcRelativePath arcPath relativePath with
        | Error pathError -> return Error pathError
        | Ok absolutePath ->
            let! exists = pathExistsAsync absolutePath

            if exists then
                return Ok absolutePath
            else
                return Error(exn $"Path '{relativePath}' does not exist.")
    }

let private showPathInFileExplorerAsync (arcPath: string) (relativePath: string) : JS.Promise<Result<unit, exn>> =
    promise {
        match! tryResolveExistingArcRelativePath arcPath relativePath with
        | Error pathError -> return Error pathError
        | Ok absolutePath ->
            try
                shell.showItemInFolder absolutePath
                return Ok()
            with shellError ->
                return Error(exn $"Could not show '{relativePath}' in file explorer: {shellError.Message}")
    }

let private openPathWithDefaultApplicationAsync (arcPath: string) (relativePath: string) : JS.Promise<Result<unit, exn>> =
    promise {
        match! tryResolveExistingArcRelativePath arcPath relativePath with
        | Error pathError -> return Error pathError
        | Ok absolutePath ->
            let! shellOpenResult = shell.openPath absolutePath

            if String.IsNullOrWhiteSpace shellOpenResult then
                return Ok()
            else
                return Error(exn $"Could not open '{relativePath}' with the default application: {shellOpenResult}")
    }

let private runLoadedArcPathAction
    (event: IpcMainInvokeEvent)
    (operation: string -> JS.Promise<Result<'T, exn>>)
    : JS.Promise<Result<'T, exn>> =
    promise {
        try
            return!
                withLoadedArcVault event (fun vault ->
                    operation vault.path.Value)
        with e ->
            return Error e
    }


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
    showPathInFileExplorer =
        fun (relativePath: string) ->
            runLoadedArcPathAction event (fun arcPath ->
                showPathInFileExplorerAsync arcPath relativePath)
    openPathWithDefaultApplication =
        fun (relativePath: string) ->
            runLoadedArcPathAction event (fun arcPath ->
                openPathWithDefaultApplicationAsync arcPath relativePath)
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
    addArcFile =
        fun (request: FileContentDTO) -> promise {
            try
                return!
                    withLoadedArcVault
                        event
                        (fun vault -> promise { return! vault.AddArcFile request })
            with e ->
                return Error e
        }
    createFileSystemItem =
        fun (request: CreateFileSystemItemRequest) -> promise {
            try
                return!
                    withLoadedArcVault event (fun vault -> promise {
                        return! ArcFileSystemHelper.createFileSystemItemOnDisk vault.path.Value request
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
                            let normalizedRelativePath = PathHelpers.normalizeRelativePath relativePath
                            let classification = ArcDeletePathRules.classifyDeleteTarget normalizedRelativePath

                            match classification with
                            | ArcDeletePathRules.DeletePathClassification.EntityFolderTarget _
                            | ArcDeletePathRules.DeletePathClassification.CanonicalFileTarget(
                                ArcDeletePathRules.CanonicalArcFileTarget.EntityFile _,
                                _
                              ) ->
                                match vault.arc with
                                | None -> return Error(exn "ARC is not loaded.")
                                | Some arcLocal ->
                                    let wasBusyWriting = vault.isBusyWriting
                                    vault.isBusyWriting <- true

                                    try
                                        match!
                                            ArcDeleteHelper.deleteArcEntityAsync
                                                arcPath
                                                normalizedRelativePath
                                                arcLocal
                                        with
                                        | Error deleteError -> return Error deleteError
                                        | Ok deletedArc ->
                                            vault.SetArc deletedArc
                                            vault.RefreshHasUnsavedArcChangesFlag()
                                            return Ok()
                                    finally
                                        vault.isBusyWriting <- wasBusyWriting
                            | ArcDeletePathRules.DeletePathClassification.CanonicalFileTarget(
                                ArcDeletePathRules.CanonicalArcFileTarget.DataMapFile _,
                                normalizedGenericPath
                              )
                            | ArcDeletePathRules.DeletePathClassification.AddZoneDescendantTarget(_, normalizedGenericPath) ->
                                if ArcDeletePathRules.isDeletePathAllowed normalizedGenericPath |> not then
                                    return
                                        Error(
                                            exn
                                                "Deletion is only allowed for descendants under studies/, assays/, workflows/, or runs/."
                                        )
                                else
                                    return!
                                        ArcFileSystemHelper.deleteGenericFileSystemItemOnDisk
                                            arcPath
                                            normalizedGenericPath
                            | ArcDeletePathRules.DeletePathClassification.CanonicalFileTarget(
                                ArcDeletePathRules.CanonicalArcFileTarget.InvestigationFile,
                                _
                              ) ->
                                return Error(exn "Deleting the investigation file is not supported.")
                            | ArcDeletePathRules.DeletePathClassification.ProtectedTarget _ ->
                                return
                                    Error(
                                        exn
                                            "Deleting protected files (for example .gitkeep or readme.md) is not allowed."
                                    )
                            | ArcDeletePathRules.DeletePathClassification.DisallowedTarget _ ->
                                return
                                    Error(
                                        exn
                                            "Deletion is only allowed for descendants under studies/, assays/, workflows/, or runs/."
                                    )
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

                            match ArcDeletePathRules.classifyRenameTarget request.relativePath with
                            | ArcDeletePathRules.RenamePathClassification.GenericTarget _ ->
                                return! ArcFileSystemHelper.renameGenericFileSystemItemOnDisk arcPath request
                            | _ ->
                                match ArcRenameHelper.tryBuildRenamePlan request with
                                | Error validationError -> return Error validationError
                                | Ok renamePlan ->
                                    match tryResolveArcRelativePath arcPath renamePlan.TargetPath with
                                    | Error pathError -> return Error pathError
                                    | Ok targetAbsolutePath ->
                                        let! targetExists = pathExistsAsync targetAbsolutePath

                                        if targetExists then
                                            return
                                                Error(
                                                    exn
                                                        $"Cannot rename '{renamePlan.SourcePath}' to '{renamePlan.TargetPath}' because the destination already exists."
                                                )
                                        else
                                            match vault.arc with
                                            | None -> return Error(exn "ARC is not loaded.")
                                            | Some arcLocal ->
                                                let wasBusyWriting = vault.isBusyWriting
                                                vault.isBusyWriting <- true

                                                try
                                                    match!
                                                        ArcRenameHelper.renameArcEntityAsync
                                                            arcPath
                                                            renamePlan
                                                            arcLocal
                                                    with
                                                    | Error renameError ->
                                                        return
                                                            Error(
                                                                ArcRenameHelper.mapRenameDiskError
                                                                    renamePlan.SourcePath
                                                                    renamePlan.TargetPath
                                                                    renameError
                                                            )
                                                    | Ok renamedArc ->
                                                        vault.SetArc renamedArc
                                                        vault.RefreshHasUnsavedArcChangesFlag()
                                                        return Ok()
                                                finally
                                                    vault.isBusyWriting <- wasBusyWriting
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

