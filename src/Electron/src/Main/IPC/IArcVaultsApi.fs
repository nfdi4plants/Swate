module Main.IPC.ArcVaultsApi

open System
open Swate.Components.Shared
open Swate.Electron.Shared
open Swate.Electron.Shared.IPCTypes
open Swate.Electron.Shared.GitTypes
open Swate.Electron.Shared.FileIOTypes
open Swate.Electron.Shared.FileIOHelper
open Fable.Core
open Fable.Electron
open Fable.Electron.Main
open Fable.Core.JsInterop
open Main
open Main.ArcMerge
open Node.Api
open ARCtrl
open Swate.Electron.Shared.DTOs.NoteSearchDto


let private fsPromisesDynamic: obj = importAll "fs/promises"
let private pathDynamic: obj = importAll "path"

[<RequireQualifiedAccess>]
module ArcPathValidation =

    let normalizePathForComparison (pathValue: string) =
        pathValue.Replace("\\", "/").Trim().TrimEnd('/').ToLowerInvariant()

    let containsTraversalSegments (pathValue: string) =
        pathValue.Split('/')
        |> Array.exists (fun segment -> segment = "." || segment = "..")

    let isSafeRelativePathCandidate (pathValue: string) =
        let normalizedPath = PathHelpers.normalizePath pathValue

        not (String.IsNullOrWhiteSpace normalizedPath)
        && normalizedPath <> "."
        && not (pathDynamic?isAbsolute (normalizedPath) |> unbox<bool>)
        && not (containsTraversalSegments normalizedPath)

    let isWithinRootPath (rootPath: string) (candidatePath: string) =
        let normalizedRootPath =
            pathDynamic?resolve (rootPath)
            |> unbox<string>
            |> normalizePathForComparison

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
    fsPromisesDynamic?readFile (absolutePath, "utf8")
    |> unbox<JS.Promise<string>>

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

let private isSameOrDescendantPathForComparison (path: string) (ancestorPath: string) =
    let normalize = PathHelpers.normalizePath >> ArcPathValidation.normalizePathForComparison
    let normalizedPath = normalize path
    let normalizedAncestorPath = normalize ancestorPath

    not (String.IsNullOrWhiteSpace normalizedPath)
    && not (String.IsNullOrWhiteSpace normalizedAncestorPath)
    && (normalizedPath = normalizedAncestorPath || normalizedPath.StartsWith(normalizedAncestorPath + "/"))

[<RequireQualifiedAccess>]
module ArcDeleteHelper =


    let private tryGetNodeErrorCode (error: exn) : string option =
        try
            error?code |> unbox<string> |> Option.ofObj
        with _ ->
            None

    let private isMissingPathError (deleteError: exn) =
        match tryGetNodeErrorCode deleteError with
        | Some "ENOENT" -> true
        | _ -> false

    let private canonicalTargetExistsInMemory (arcLocal: ARC) (target: ArcDeletePathRules.CanonicalArcFileTarget) =
        match target with
        | ArcDeletePathRules.CanonicalArcFileTarget.InvestigationFile -> true
        | ArcDeletePathRules.CanonicalArcFileTarget.EntityFile(zone, identifier) ->
            match zone with
            | ArcDeletePathRules.AddZone.Assays -> arcLocal.ContainsAssay(identifier)
            | ArcDeletePathRules.AddZone.Studies -> arcLocal.ContainsStudy(identifier)
            | ArcDeletePathRules.AddZone.Workflows -> arcLocal.ContainsWorkflow(identifier)
            | ArcDeletePathRules.AddZone.Runs -> arcLocal.ContainsRun(identifier)
        | ArcDeletePathRules.CanonicalArcFileTarget.DataMapFile(zone, identifier) ->
            match zone with
            | ArcDeletePathRules.AddZone.Assays ->
                arcLocal.TryGetAssay(identifier) |> Option.exists (fun assay -> assay.DataMap.IsSome)
            | ArcDeletePathRules.AddZone.Studies ->
                arcLocal.TryGetStudy(identifier) |> Option.exists (fun study -> study.DataMap.IsSome)
            | ArcDeletePathRules.AddZone.Workflows ->
                arcLocal.TryGetWorkflow(identifier) |> Option.exists (fun workflow -> workflow.DataMap.IsSome)
            | ArcDeletePathRules.AddZone.Runs ->
                arcLocal.TryGetRun(identifier) |> Option.exists (fun run -> run.DataMap.IsSome)

    let tryCreateMemoryOnlyDeleteError (deletedPath: string) (arcLocal: ARC) (deleteError: exn) =
        if isMissingPathError deleteError then
            match ArcDeletePathRules.tryParseCanonicalArcFileTarget deletedPath with
            | Some target when canonicalTargetExistsInMemory arcLocal target ->
                Some(exn $"Target '{deletedPath}' exists only in memory and is not written to disk yet.")
            | _ -> None
        else
            None

    type MergeResult = {
        Arc: ARC
    }

    let private normalizeRelativePathForMerge (path: string) =
        path
        |> PathHelpers.normalizeRelativePath
        |> PathHelpers.normalizePath

    let private deduplicateEventPaths (paths: string seq) =
        paths
        |> Seq.distinctBy ArcPathValidation.normalizePathForComparison
        |> Seq.toList

    let private buildPrimaryUnlinkEventPaths (deletedPath: string) (preDeleteFileRelativePaths: string seq) =
        let normalizedDeletedPath = normalizeRelativePathForMerge deletedPath

        preDeleteFileRelativePaths
        |> Seq.map normalizeRelativePathForMerge
        |> Seq.filter (fun relativePath -> isSameOrDescendantPathForComparison relativePath normalizedDeletedPath)
        |> deduplicateEventPaths

    let buildDeleteUnlinkEvents (deletedPath: string) (preDeleteFileRelativePaths: string seq) : FileEvent list =
        let primaryPaths = buildPrimaryUnlinkEventPaths deletedPath preDeleteFileRelativePaths

        let effectivePaths =
            if primaryPaths.Length > 0 then
                primaryPaths
            else
                ArcDeletePathRules.buildFallbackUnlinkPaths deletedPath
                |> deduplicateEventPaths

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
                tryGetRepoRelativePath arcPath fileEntry.path |> Option.map normalizeRelativePathForMerge
        )
        |> Seq.toList

    let mergeReloadedArcAfterDelete
        (deletedPath: string)
        (preDeleteFileRelativePaths: string seq)
        (arcLocal: ARC)
        (reloadedArc: ARC)
        : Result<MergeResult, exn> =
        let arcLocalForMerge = arcLocal.Copy()
        let unlinkEvents = buildDeleteUnlinkEvents deletedPath preDeleteFileRelativePaths
        let mergedArc = ARC.merge arcLocalForMerge reloadedArc unlinkEvents

        Ok { Arc = mergedArc }


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

                        let! result = dialog.showOpenDialog (?window = window, properties = properties, defaultPath = arcPath)

                        if result.canceled then
                            return Error(exn "Cancelled")
                        else
                            let relativePaths =
                                result.filePaths
                                |> Array.map (tryGetArcRelativePath arcPath)

                            match relativePaths |> Array.tryFind Result.isError with
                            | Some(Error pathError) -> return Error pathError
                            | _ ->
                                return
                                    relativePaths
                                    |> Array.choose (function
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

                let filters = [| FileFilter("Delimited text files", [| "csv"; "tsv"; "txt" |]) |]
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
                    withLoadedArcVault event (fun vault ->
                        promise {
                            match! vault.WriteArc() with
                            | Error saveError -> return Error saveError
                            | Ok() ->
                                do! vault.RefreshFileTree()
                                return Ok()
                        }
                    )
            with e ->
                return Error e
        }
    setArcFileInMemory =
        fun (request: FileContentDTO) -> promise {
            try
                return!
                    withLoadedArcVault event (fun vault ->
                        promise {
                            match vault.UpdateArcBy request with
                            | Error saveError -> return Error saveError
                            | Ok() -> return Ok()
                        }
                    )
            with e ->
                return Error e
        }
    deletePath =
        fun (relativePath: string) -> promise {
            try
                let windowId = windowIdFromIpcEvent event

                match ARC_VAULTS.TryGetVault(windowId) with
                | None -> return Error(exn $"The ARC for window id {windowId} should exist")
                | Some vault ->
                    match vault.path, vault.arc with
                    | None, _
                    | _, None -> return Error(exn "ARC is not loaded.")
                    | Some arcPath, Some arcLocal ->
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

                                vault.isBusyWriting <- true

                                try
                                    let! deleteResult =
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
                                                match
                                                    ArcDeleteHelper.tryCreateMemoryOnlyDeleteError
                                                        normalizedRelativePath
                                                        arcLocal
                                                        deleteError
                                                with
                                                | Some memoryOnlyError -> return Error memoryOnlyError
                                                | None -> return Error deleteError
                                        }

                                    match deleteResult with
                                    | Error deleteError -> return Error deleteError
                                    | Ok() ->
                                        do! vault.RefreshFileTree()

                                        match! ARC.tryLoadAsync arcPath with
                                        | Error loadError ->
                                            return Error(exn $"Unable to reload ARC after deleting '{normalizedRelativePath}': {loadError}")
                                        | Ok reloadedArc ->
                                            match
                                                ArcDeleteHelper.mergeReloadedArcAfterDelete
                                                    normalizedRelativePath
                                                    preDeleteFileRelativePaths
                                                    arcLocal
                                                    reloadedArc
                                            with
                                            | Error mergeError -> return Error mergeError
                                            | Ok mergeResult ->
                                                vault.arc <- Some mergeResult.Arc
                                                vault.window.title <- mergeResult.Arc.Identifier
                                                return Ok()
                                finally
                                    vault.isBusyWriting <- false
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
                            let dto = FileContentDTO.create ARCtrl.Contract.DTOType.PlainText content relativePath
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
    cancelGitLfs =
        fun (requestId: string) -> GitLfs.cancelChannel requestId
    resolveCloseRequest =
        fun (decision: IPCTypesHelper.SaveBeforeQuitDecision) -> promise {
            try
                let windowId = windowIdFromIpcEvent event
                return! ARC_VAULTS.ResolveCloseRequest(windowId, decision)
            with e ->
                return Error e
        }
}
