module Main.IPC.FileSystemIO

open System
open Fable.Core
open Swate.Components.Shared
open Swate.Electron.Shared.FileIOTypes
open Swate.Electron.Shared.RenamePathRules


[<RequireQualifiedAccess>]
module ArcPathValidation =

    let isSafeRelativePathCandidate (pathValue: string) =
        let normalizedPath = PathHelpers.normalizePath pathValue

        not (String.IsNullOrWhiteSpace normalizedPath)
        && normalizedPath <> "."
        && not (Main.Bindings.Path.isAbsolute normalizedPath)
        && not (PathHelpers.containsPathTraversalSegments normalizedPath)

    let isWithinRootPath (rootPath: string) (candidatePath: string) =
        let normalizedRootPath =
            Main.Bindings.Path.resolve [| rootPath |]
            |> PathHelpers.normalizePathForFsComparison

        let normalizedCandidatePath =
            Main.Bindings.Path.resolve [| candidatePath |]
            |> PathHelpers.normalizePathForFsComparison

        normalizedCandidatePath = normalizedRootPath
        || normalizedCandidatePath.StartsWith(normalizedRootPath + "/")

let resolveAbsolutePath (pathValue: string) =
    Main.Bindings.Path.resolve [| pathValue |]

let tryGetArcRelativePath (arcPath: string) (requestedAbsolutePath: string) =
    let arcRoot = resolveAbsolutePath arcPath
    let absolutePath = resolveAbsolutePath requestedAbsolutePath

    let relativePath =
        Main.Bindings.Path.relative arcRoot absolutePath |> PathHelpers.normalizePath

    if String.IsNullOrWhiteSpace relativePath || relativePath = "." then
        Ok ""
    elif not (ArcPathValidation.isSafeRelativePathCandidate relativePath) then
        Error(exn $"Path '{requestedAbsolutePath}' is outside the active ARC root.")
    elif not (ArcPathValidation.isWithinRootPath arcRoot absolutePath) then
        Error(exn $"Path '{requestedAbsolutePath}' is outside the active ARC root.")
    else
        Ok relativePath

/// Resolves a relative path against the ARC root and rejects absolute or traversal-based escapes.
let tryResolveArcRelativePath (arcPath: string) (requestedRelativePath: string) =
    let normalizedArcPath = PathHelpers.normalizePath arcPath
    let relativePath = PathHelpers.normalizePath requestedRelativePath

    if String.IsNullOrWhiteSpace normalizedArcPath then
        Error(exn "ArcPath must not be empty.")
    elif not (Main.Bindings.Path.isAbsolute normalizedArcPath) then
        Error(exn "ArcPath must be absolute.")
    elif String.IsNullOrWhiteSpace relativePath then
        Error(exn "RelativePath must not be empty.")
    elif not (ArcPathValidation.isSafeRelativePathCandidate relativePath) then
        if Main.Bindings.Path.isAbsolute relativePath then
            Error(exn "RelativePath must not be absolute.")
        else
            Error(exn "RelativePath must not contain path traversal segments.")
    else
        let arcRootPath = resolveAbsolutePath normalizedArcPath
        let absolutePath = Main.Bindings.Path.resolve [| arcRootPath; relativePath |]

        if ArcPathValidation.isWithinRootPath arcRootPath absolutePath then
            Ok absolutePath
        else
            Error(exn "RelativePath resolves outside the ARC root.")

let pathExistsAsync (absolutePath: string) : JS.Promise<bool> = promise {
    let! fileExists = ARCtrl.FileSystemHelper.fileExistsAsync absolutePath

    if fileExists then
        return true
    else
        return! ARCtrl.FileSystemHelper.directoryExistsAsync absolutePath
}

let mkdirAsync (directoryPath: string) : JS.Promise<unit> =
    ARCtrl.FileSystemHelper.createDirectoryAsync directoryPath

type private FileSystemRetryStrategy = {
    DelaysMs: int[]
    IsTransientErrorCode: string option -> bool
}

// Windows file APIs can briefly report lock/contention errors while external processes still hold handles.
let private renameRetryStrategy = {
    DelaysMs = [| 0; 75; 200; 500 |]
    IsTransientErrorCode =
        function
        | Some "EPERM"
        | Some "EACCES"
        | Some "EBUSY" -> true
        | _ -> false
}

// Recursive directory deletion can briefly report non-empty or locked paths while Git/file watchers finish writes.
let private removeRetryStrategy = {
    DelaysMs = [| 0; 75; 200; 500 |]
    IsTransientErrorCode =
        function
        | Some "ENOTEMPTY"
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
            do! Main.Bindings.Filesystem.renameAsync sourceAbsolutePath targetAbsolutePath
            return Ok()
        with renameError ->
            let errorCode = Main.Bindings.Node.tryGetErrorCode renameError

            if
                attemptIndex < renameRetryStrategy.DelaysMs.Length - 1
                && renameRetryStrategy.IsTransientErrorCode errorCode
            then
                return! attempt (attemptIndex + 1)
            else
                return Error renameError
    }

    attempt 0

let removePathWithRetriesAsync
    (removePathAsync: string -> JS.Promise<unit>)
    (absolutePath: string)
    : JS.Promise<Result<unit, exn>> =
    let rec attempt (attemptIndex: int) = promise {
        if attemptIndex > 0 then
            do! Promise.sleep removeRetryStrategy.DelaysMs.[attemptIndex]

        try
            do! removePathAsync absolutePath
            return Ok()
        with removeError ->
            let errorCode = Main.Bindings.Node.tryGetErrorCode removeError

            if
                attemptIndex < removeRetryStrategy.DelaysMs.Length - 1
                && removeRetryStrategy.IsTransientErrorCode errorCode
            then
                return! attempt (attemptIndex + 1)
            else
                return Error removeError
    }

    attempt 0

let private removeGenericFileSystemItemAsync absolutePath = promise {
    do!
        Main.Bindings.Filesystem.rmAsync
            absolutePath
            (Main.Bindings.Filesystem.RmOptions(recursive = true, force = false))

    return ()
}

let mapRenameDiskError (sourcePath: string) (targetPath: string) (renameError: exn) =
    match Main.Bindings.Node.tryGetErrorCode renameError with
    | Some "EPERM"
    | Some "EACCES" ->
        exn
            $"Cannot rename '{sourcePath}' to '{targetPath}'. Windows reported a permission or file-lock conflict. If the destination already exists, choose a different name and close apps that may be using these paths."
    | Some "ENOTEMPTY"
    | Some "EEXIST" -> exn $"Cannot rename '{sourcePath}' to '{targetPath}' because the destination already exists."
    | Some "ENOENT" -> exn $"Cannot rename '{sourcePath}' because the source path no longer exists on disk."
    | _ -> renameError

[<RequireQualifiedAccess>]
module ArcFileSystemHelper =

    let private maxParallelExternalFileCopyWorkers = 16

    type CreateFileSystemItemPlan = {
        ParentPath: string
        TargetPath: string
        Kind: FileSystemItemKind
    }

    type RenamePlan = {
        SourcePath: string
        TargetPath: string
    }

    [<RequireQualifiedAccess; StringEnum>]
    type TransferKind =
        | Move
        | Copy

    type PathBasedTransferPlan = {
        SourcePath: string
        TargetPath: string
        Overwrite: bool
        TransferKind: TransferKind
    }

    let private resolveArcRelativePathPair arcPath firstRelativePath secondRelativePath =
        match
            tryResolveArcRelativePath arcPath firstRelativePath, tryResolveArcRelativePath arcPath secondRelativePath
        with
        | Error pathError, _
        | _, Error pathError -> Error pathError
        | Ok firstAbsolutePath, Ok secondAbsolutePath -> Ok(firstAbsolutePath, secondAbsolutePath)

    let private resolveCreatePathPair arcPath parentRelativePath targetRelativePath =
        let normalizedParentPath =
            parentRelativePath |> PathHelpers.normalizeCanonicalRelativePath

        let parentPath =
            if String.IsNullOrWhiteSpace normalizedParentPath then
                Ok(resolveAbsolutePath arcPath)
            else
                tryResolveArcRelativePath arcPath normalizedParentPath

        match parentPath, tryResolveArcRelativePath arcPath targetRelativePath with
        | Error pathError, _
        | _, Error pathError -> Error pathError
        | Ok parentAbsolutePath, Ok targetAbsolutePath -> Ok(parentAbsolutePath, targetAbsolutePath)

    let private ensureTargetDoesNotExist targetAbsolutePath errorMessage = promise {
        let! targetExists = pathExistsAsync targetAbsolutePath

        if targetExists then
            return Error(exn errorMessage)
        else
            return Ok()
    }

    let private createTargetAsync kind targetAbsolutePath =
        match kind with
        | FileSystemItemKind.File -> ARCtrl.FileSystemHelper.writeFileTextAsync targetAbsolutePath ""
        | FileSystemItemKind.Folder -> mkdirAsync targetAbsolutePath

    let private renameResolvedPathOnDisk sourcePath targetPath sourceAbsolutePath targetAbsolutePath = promise {
        match! renameWithRetriesAsync sourceAbsolutePath targetAbsolutePath with
        | Ok() -> return Ok()
        | Error renameError -> return Error(mapRenameDiskError sourcePath targetPath renameError)
    }

    let private copyResolvedPathWithOptionsAsync sourceAbsolutePath targetAbsolutePath copyOptions = promise {
        do! mkdirAsync (Main.Bindings.Path.dirname targetAbsolutePath)
        do! Main.Bindings.Filesystem.cpAsync sourceAbsolutePath targetAbsolutePath copyOptions
    }

    let private copyResolvedFilePathAsync sourceAbsolutePath targetAbsolutePath overwrite = promise {
        do! mkdirAsync (Main.Bindings.Path.dirname targetAbsolutePath)

        if overwrite then
            do! Main.Bindings.Filesystem.copyFileAsync sourceAbsolutePath targetAbsolutePath
        else
            do!
                Main.Bindings.Filesystem.copyFileWithModeAsync
                    sourceAbsolutePath
                    targetAbsolutePath
                    Main.Bindings.Filesystem.constants.COPYFILE_EXCL
    }

    let private copyResolvedPathOnDisk sourcePath targetPath sourceAbsolutePath targetAbsolutePath = promise {
        try
            do!
                copyResolvedPathWithOptionsAsync
                    sourceAbsolutePath
                    targetAbsolutePath
                    (Main.Bindings.Filesystem.CpOptions(recursive = true, force = false))

            return Ok()
        with copyError ->
            return Error(exn $"Cannot copy '{sourcePath}' to '{targetPath}': {copyError.Message}")
    }

    ///WIP must be simplified in future pr
    let private renameIgnoringErrorsAsync sourceAbsolutePath targetAbsolutePath = promise {
        try
            do! Main.Bindings.Filesystem.renameAsync sourceAbsolutePath targetAbsolutePath
            return ()
        with _ ->
            return ()
    }

    let private moveFileIntoDescendantPathOnDisk sourcePath targetPath sourceAbsolutePath targetAbsolutePath = promise {
        let sourceParentAbsolutePath = Main.Bindings.Path.dirname sourceAbsolutePath

        let tempFileName = ".swate-move-" + Guid.NewGuid().ToString("N") + ".tmp"

        let tempAbsolutePath =
            Main.Bindings.Path.join [| sourceParentAbsolutePath; tempFileName |]

        match! renameWithRetriesAsync sourceAbsolutePath tempAbsolutePath with
        | Error renameError -> return Error(mapRenameDiskError sourcePath targetPath renameError)
        | Ok() ->
            try
                do! mkdirAsync (Main.Bindings.Path.dirname targetAbsolutePath)

                match! renameWithRetriesAsync tempAbsolutePath targetAbsolutePath with
                | Ok() -> return Ok()
                | Error moveError ->
                    do! renameIgnoringErrorsAsync tempAbsolutePath sourceAbsolutePath
                    return Error(mapRenameDiskError sourcePath targetPath moveError)
            with moveError ->
                do! renameIgnoringErrorsAsync tempAbsolutePath sourceAbsolutePath
                return Error moveError
    }

    let private executeGenericPathTransferOnDisk
        (arcPath: string)
        (plan: PathBasedTransferPlan)
        (transferToTargetAsync: string -> string -> JS.Promise<Result<unit, exn>>)
        (transferIntoDescendantPathAsync: (string -> string -> JS.Promise<Result<unit, exn>>) option)
        : JS.Promise<Result<unit, exn>> =
        promise {
            match resolveArcRelativePathPair arcPath plan.SourcePath plan.TargetPath with
            | Error pathError -> return Error pathError
            | Ok(sourceAbsolutePath, targetAbsolutePath) ->
                let operationName = plan.TransferKind.ToString().ToLower()
                let! sourceExists = pathExistsAsync sourceAbsolutePath

                if sourceExists |> not then
                    return Error(exn $"Cannot {operationName} '{plan.SourcePath}' because it does not exist.")
                else
                    let targetIsSameOrDescendant =
                        PathHelpers.isSameOrDescendantPath plan.TargetPath plan.SourcePath

                    let! sourceIsDirectory = ARCtrl.FileSystemHelper.directoryExistsAsync sourceAbsolutePath

                    if sourceIsDirectory && targetIsSameOrDescendant then
                        return Error(exn $"{operationName} target must not be inside the source path.")
                    else
                        let! targetExists = pathExistsAsync targetAbsolutePath

                        match
                            targetExists, plan.Overwrite, targetIsSameOrDescendant, transferIntoDescendantPathAsync
                        with
                        | true, false, _, _ ->
                            return
                                Error(
                                    exn
                                        $"Cannot {operationName} '{plan.SourcePath}' to '{plan.TargetPath}' because the destination already exists."
                                )
                        | true, true, _, _ ->
                            match! removePathWithRetriesAsync removeGenericFileSystemItemAsync targetAbsolutePath with
                            | Error removeError -> return Error removeError
                            | Ok() -> return! transferToTargetAsync sourceAbsolutePath targetAbsolutePath
                        | false, _, true, Some transferIntoDescendantPathAsync ->
                            return! transferIntoDescendantPathAsync sourceAbsolutePath targetAbsolutePath
                        | false, _, _, _ -> return! transferToTargetAsync sourceAbsolutePath targetAbsolutePath
        }

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

    let createFileSystemItemOnDisk
        (arcPath: string)
        (request: CreateFileSystemItemRequest)
        : JS.Promise<Result<string, exn>> =
        promise {
            match tryBuildCreateFileSystemItemPlan request with
            | Error validationError -> return Error validationError
            | Ok plan ->
                match resolveCreatePathPair arcPath plan.ParentPath plan.TargetPath with
                | Error pathError -> return Error pathError
                | Ok(parentAbsolutePath, targetAbsolutePath) ->
                    try
                        let! parentIsDirectory = ARCtrl.FileSystemHelper.directoryExistsAsync parentAbsolutePath

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

    let copyExternalFileToArcOnDisk
        (arcPath: string)
        (request: CopyExternalFileRequest)
        : JS.Promise<Result<string, exn>> =
        promise {
            let sourcePath =
                request.sourceAbsolutePath |> Option.ofObj |> Option.defaultValue ""

            let targetPath =
                request.targetRelativePath |> PathHelpers.normalizeCanonicalRelativePath

            let targetExistsMessage () =
                $"Cannot copy '{sourcePath}' to '{targetPath}' because the destination already exists."

            try
                if String.IsNullOrWhiteSpace sourcePath then
                    raise (exn "Source file path is required.")

                if Main.Bindings.Path.isAbsolute sourcePath |> not then
                    raise (exn "Source file path must be absolute.")

                if ArcEntityPathRules.isGenericFileSystemTargetAllowed targetPath |> not then
                    raise (exn "Copy target must stay inside a safe generic ARC path.")

                let targetAbsolutePath =
                    match tryResolveArcRelativePath arcPath targetPath with
                    | Ok absolutePath -> absolutePath
                    | Error pathError -> raise pathError

                let sourceAbsolutePath = resolveAbsolutePath sourcePath
                let! sourceExists = ARCtrl.FileSystemHelper.fileExistsAsync sourceAbsolutePath

                if sourceExists |> not then
                    raise (exn $"Source file '{request.sourceAbsolutePath}' does not exist.")

                let! targetExists = pathExistsAsync targetAbsolutePath

                if targetExists && not request.overwrite then
                    raise (exn (targetExistsMessage ()))

                do! copyResolvedFilePathAsync sourceAbsolutePath targetAbsolutePath request.overwrite

                return Ok targetPath
            with copyError ->
                match Main.Bindings.Node.tryGetErrorCode copyError with
                | Some "EEXIST" when not request.overwrite -> return Error(exn (targetExistsMessage ()))
                | _ -> return Error copyError
        }

    let private getExternalFileCopyWorkerCount (fileCount: int) =
        Main.Bindings.Node.cpus().Length * 2
        |> max 1
        |> min maxParallelExternalFileCopyWorkers
        |> min fileCount

    let private groupExternalFileCopyRequestIndexesByTarget (requests: CopyExternalFileRequest[]) =
        requests
        |> Array.mapi (fun requestIndex request ->
            request.targetRelativePath |> PathHelpers.normalizeCanonicalRelativePath, requestIndex
        )
        |> Array.groupBy fst
        |> Array.map (snd >> Array.map snd >> Array.sort)

    let copyExternalFilesToArcOnDisk
        (arcPath: string)
        (requests: CopyExternalFileRequest[])
        : JS.Promise<Result<string[], exn>> =
        promise {
            if requests.Length = 0 then
                return Ok [||]
            else
                let requestGroups = groupExternalFileCopyRequestIndexesByTarget requests
                let workerCount = getExternalFileCopyWorkerCount requestGroups.Length
                let copiedPaths = Array.zeroCreate<string> requests.Length
                let mutable nextGroupIndex = 0
                let mutable firstCopyError: exn option = None

                let canContinueCopying () = firstCopyError |> Option.isNone

                let rememberCopyError copyError =
                    if canContinueCopying () then
                        firstCopyError <- Some copyError

                let copyWorker () = promise {
                    while nextGroupIndex < requestGroups.Length && canContinueCopying () do
                        let requestIndexes = requestGroups.[nextGroupIndex]
                        nextGroupIndex <- nextGroupIndex + 1

                        let mutable groupRequestIndex = 0

                        while groupRequestIndex < requestIndexes.Length && canContinueCopying () do
                            let requestIndex = requestIndexes.[groupRequestIndex]
                            groupRequestIndex <- groupRequestIndex + 1

                            match! copyExternalFileToArcOnDisk arcPath requests.[requestIndex] with
                            | Ok copiedPath -> copiedPaths.[requestIndex] <- copiedPath
                            | Error copyError -> rememberCopyError copyError
                }

                let workerPromises = Array.init workerCount (fun _ -> copyWorker ())

                let! _ = Fable.Core.JS.Constructors.Promise.all workerPromises

                match firstCopyError with
                | Some copyError -> return Error copyError
                | None -> return Ok copiedPaths
        }

    let tryBuildGenericRenamePlan (request: RenamePathRequest) : Result<RenamePlan, exn> =
        let requestedRelativePath =
            request.relativePath |> PathHelpers.normalizeCanonicalRelativePath

        match tryBuildGenericFileSystemRenameTargetPath requestedRelativePath request.newName with
        | Error validationError -> Error(exn validationError)
        | Ok targetPath ->
            Ok {
                SourcePath = requestedRelativePath
                TargetPath = targetPath
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
                        return!
                            renameResolvedPathOnDisk
                                genericRenamePlan.SourcePath
                                genericRenamePlan.TargetPath
                                sourceAbsolutePath
                                targetAbsolutePath
        }

    let private tryBuildGenericPathTransferPlan
        (kind: TransferKind)
        (sourceRelativePath: string)
        (targetRelativePath: string)
        (overwrite: bool)
        : Result<PathBasedTransferPlan, exn> =
        let operationName = kind.ToString().ToLower()

        let sourcePath =
            ArcEntityPathRules.tryNormalizeGenericFileSystemTarget
                $"Generic filesystem {operationName} is only supported for safe non-entity source paths."
                sourceRelativePath

        let targetPath =
            ArcEntityPathRules.tryNormalizeGenericFileSystemTarget
                $"Generic filesystem {operationName} targets must stay inside safe non-entity ARC paths."
                targetRelativePath

        match sourcePath, targetPath with
        | Error validationError, _
        | _, Error validationError -> Error(exn validationError)
        | Ok sourcePath, Ok targetPath when PathHelpers.pathsEqual sourcePath targetPath ->
            Error(exn $"{operationName} target is identical to the current path.")
        | Ok sourcePath, Ok targetPath ->
            Ok {
                SourcePath = sourcePath
                TargetPath = targetPath
                Overwrite = overwrite
                TransferKind = kind
            }

    let tryBuildGenericMovePlan (request: MovePathRequest) : Result<PathBasedTransferPlan, exn> =
        tryBuildGenericPathTransferPlan
            TransferKind.Move
            request.sourceRelativePath
            request.targetRelativePath
            request.overwrite

    let tryBuildGenericCopyPlan (request: CopyFileSystemItemRequest) : Result<PathBasedTransferPlan, exn> =
        tryBuildGenericPathTransferPlan
            TransferKind.Copy
            request.sourceRelativePath
            request.targetRelativePath
            request.overwrite

    let moveGenericFileSystemItemOnDisk (arcPath: string) (request: MovePathRequest) : JS.Promise<Result<unit, exn>> = promise {
        match tryBuildGenericMovePlan request with
        | Error validationError -> return Error validationError
        | Ok transferPlan ->
            let moveToTargetAsync sourceAbsolutePath targetAbsolutePath = promise {
                do! mkdirAsync (Main.Bindings.Path.dirname targetAbsolutePath)

                return!
                    renameResolvedPathOnDisk
                        transferPlan.SourcePath
                        transferPlan.TargetPath
                        sourceAbsolutePath
                        targetAbsolutePath
            }

            let moveIntoDescendantPathAsync sourceAbsolutePath targetAbsolutePath =
                moveFileIntoDescendantPathOnDisk
                    transferPlan.SourcePath
                    transferPlan.TargetPath
                    sourceAbsolutePath
                    targetAbsolutePath

            return!
                executeGenericPathTransferOnDisk
                    arcPath
                    transferPlan
                    moveToTargetAsync
                    (Some moveIntoDescendantPathAsync)
    }

    let copyGenericFileSystemItemOnDisk
        (arcPath: string)
        (request: CopyFileSystemItemRequest)
        : JS.Promise<Result<unit, exn>> =
        promise {
            match tryBuildGenericCopyPlan request with
            | Error validationError -> return Error validationError
            | Ok transferPlan ->
                let copyToTargetAsync sourceAbsolutePath targetAbsolutePath =
                    copyResolvedPathOnDisk
                        transferPlan.SourcePath
                        transferPlan.TargetPath
                        sourceAbsolutePath
                        targetAbsolutePath

                return! executeGenericPathTransferOnDisk arcPath transferPlan copyToTargetAsync None
        }

    let deleteGenericFileSystemItemOnDisk (arcPath: string) (relativePath: string) : JS.Promise<Result<unit, exn>> = promise {
        let normalizedRelativePath =
            relativePath |> PathHelpers.normalizeCanonicalRelativePath

        let isGenericDeleteTarget =
            match ArcEntityPathRules.classifyDeleteTarget normalizedRelativePath with
            | ArcEntityPathRules.DeletePathClassification.GenericTarget _
            | ArcEntityPathRules.DeletePathClassification.CanonicalFileTarget(ArcEntityPathRules.CanonicalArcFileTarget.DataMapFile _,
                                                                              _)
            | ArcEntityPathRules.DeletePathClassification.AddZoneDescendantTarget _ ->
                ArcEntityPathRules.isDeletePathAllowed normalizedRelativePath
            | _ -> false

        if not isGenericDeleteTarget then
            return Error(exn "Generic filesystem delete is only supported for safe non-entity paths.")
        else
            match tryResolveArcRelativePath arcPath normalizedRelativePath with
            | Error pathError -> return Error pathError
            | Ok absolutePath -> return! removePathWithRetriesAsync removeGenericFileSystemItemAsync absolutePath
    }
