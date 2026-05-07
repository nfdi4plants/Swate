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
        let normalizedPath = FileIOHelper.normalizePath pathValue

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
        |> FileIOHelper.normalizePath

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
    let relativePath = FileIOHelper.normalizePath requestedRelativePath

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

/// This function mutably sets the datamap on the correct parent based on the datamap parent info included in the file content DTO. It also ensures that the static hash is preserved to avoid unnecessary changes to the ARC when saving a datamap.
let setDataMapByParentInfo (arc: ARC) (dmpi: DatamapParentInfo) (dm: DataMap) : Result<unit, exn> =
    try
        match dmpi.Parent with
        | DataMapParent.Study ->
            arc.TryGetStudy dmpi.ParentId
            |> Option.iter (fun study ->
                if study.DataMap.IsSome then
                    dm.StaticHash <- study.DataMap.Value.StaticHash

                study.DataMap <- Some dm
            )

            Ok()
        | DataMapParent.Assay ->
            arc.TryGetAssay dmpi.ParentId
            |> Option.iter (fun assay ->
                if assay.DataMap.IsSome then
                    dm.StaticHash <- assay.DataMap.Value.StaticHash

                assay.DataMap <- Some dm
            )

            Ok()
        | DataMapParent.Workflow ->
            arc.TryGetWorkflow dmpi.ParentId
            |> Option.iter (fun workflow ->
                if workflow.DataMap.IsSome then
                    dm.StaticHash <- workflow.DataMap.Value.StaticHash

                workflow.DataMap <- Some dm
            )

            Ok()
        | DataMapParent.Run ->
            arc.TryGetRun dmpi.ParentId
            |> Option.iter (fun run ->
                if run.DataMap.IsSome then
                    dm.StaticHash <- run.DataMap.Value.StaticHash

                run.DataMap <- Some dm
            )

            Ok()
    with e ->
        Error(exn $"Failed to set datamap on ARC: {e.Message}")

/// This function should only be used for partial updated to an ARC based on a file content DTO.
let updateARCByFileContentDTO (oldArc: ARC) (dto: FileContentDTO) : Result<ARC, exn> =
    let arcfile = FileContentDTO.toArcFile dto

    match arcfile with
    | None -> Error(exn $"Unsupported file type for saving: {dto.fileType}")
    | Some arcfile ->
        match arcfile with
        // if we get a investigation we are only interested in updating the investigation part of the ARC, so we can avoid deserializing and reserializing the whole ARC which is costly for large ARCs.
        // This only works under the assumption, that we did not in fact do any changes to assay, study, ... . These reused from the existing Investigation
        | ArcFiles.Investigation newInvestigation ->
            newInvestigation.Assays <- oldArc.Assays
            newInvestigation.Studies <- oldArc.Studies
            newInvestigation.Runs <- oldArc.Runs
            newInvestigation.Workflows <- oldArc.Workflows

            let newArc =
                ARC.fromArcInvestigation (newInvestigation, oldArc.FileSystem, ?license = oldArc.License)

            newArc.StaticHash <- oldArc.StaticHash
            Ok newArc
        | ArcFiles.DataMap(dmpiOpt, dm) ->
            match dmpiOpt with
            | None -> Error(exn "DataMap file must include datamap parent info in its path for saving.")
            | Some dmpi ->
                match setDataMapByParentInfo oldArc dmpi dm with
                | Ok() -> Ok oldArc
                | Error e -> Error e
        | ArcFiles.Assay newAssay ->
            let oldAssayOpt = oldArc.TryGetAssay newAssay.Identifier

            match oldAssayOpt with
            | Some oldAssay ->
                newAssay.StaticHash <- oldAssay.StaticHash
                oldArc.SetAssay(newAssay.Identifier, newAssay)
                Ok oldArc
            | None ->
                oldArc.AddAssay(newAssay)
                Ok oldArc
        | ArcFiles.Study(newStudy, _) ->
            let oldStudyOpt = oldArc.TryGetStudy newStudy.Identifier

            match oldStudyOpt with
            | Some oldStudy ->
                newStudy.StaticHash <- oldStudy.StaticHash
                oldArc.SetStudy(newStudy.Identifier, newStudy)
                Ok oldArc
            | None ->
                oldArc.AddStudy(newStudy)
                Ok oldArc
        | ArcFiles.Workflow newWorkflow ->
            let oldWorkflowOpt = oldArc.TryGetWorkflow newWorkflow.Identifier

            match oldWorkflowOpt with
            | Some oldWorkflow ->
                newWorkflow.StaticHash <- oldWorkflow.StaticHash
                oldArc.SetWorkflow(newWorkflow.Identifier, newWorkflow)
                Ok oldArc
            | None ->
                oldArc.AddWorkflow(newWorkflow)
                Ok oldArc
        | ArcFiles.Run newRun ->
            let oldRunOpt = oldArc.TryGetRun newRun.Identifier

            match oldRunOpt with
            | Some oldRun ->
                newRun.StaticHash <- oldRun.StaticHash
                oldArc.SetRun(newRun.Identifier, newRun)
                Ok oldArc
            | None ->
                oldArc.AddRun(newRun)
                Ok oldArc
        | ArcFiles.Template _ -> Error(exn "Saving of template files is not supported.")


let private pathsEqualForComparison (leftPath: string) (rightPath: string) =
    let normalize = FileIOHelper.normalizePath >> ArcPathValidation.normalizePathForComparison
    normalize leftPath = normalize rightPath

let private isSameOrDescendantPathForComparison (path: string) (ancestorPath: string) =
    let normalize = FileIOHelper.normalizePath >> ArcPathValidation.normalizePathForComparison
    let normalizedPath = normalize path
    let normalizedAncestorPath = normalize ancestorPath

    not (String.IsNullOrWhiteSpace normalizedPath)
    && not (String.IsNullOrWhiteSpace normalizedAncestorPath)
    && (normalizedPath = normalizedAncestorPath || normalizedPath.StartsWith(normalizedAncestorPath + "/"))

[<RequireQualifiedAccess>]
module ArcDeleteHelper =

    let private addZoneRoots =
        set [ "studies"; "assays"; "workflows"; "runs" ]

    let isDeletePathAllowed (relativePath: string) =
        let normalizedRelativePath = PathHelpers.normalizeRelativePath relativePath

        if String.IsNullOrWhiteSpace normalizedRelativePath then
            false
        else
            let segments =
                normalizedRelativePath.Split('/', StringSplitOptions.RemoveEmptyEntries)

            segments.Length >= 2
            && (segments.[0].ToLowerInvariant() |> addZoneRoots.Contains)

    let isPendingPathAffectedByDelete (deletedPath: string) (pendingArcFileSave: FileContentDTO option) =
        pendingArcFileSave
        |> Option.exists (fun pendingArcFileSave ->
            isSameOrDescendantPathForComparison pendingArcFileSave.path deletedPath
        )

    type MergeResult = {
        Arc: ARC
        PendingArcFileSave: FileContentDTO option
    }

    let mergeReloadedArcAfterDelete
        (deletedPath: string)
        (reloadedArc: ARC)
        (pendingArcFileSave: FileContentDTO option)
        : Result<MergeResult, exn> =
        let shouldDropPendingArcFileSave =
            isPendingPathAffectedByDelete deletedPath pendingArcFileSave

        if shouldDropPendingArcFileSave then
            Ok {
                Arc = reloadedArc
                PendingArcFileSave = None
            }
        else
            match pendingArcFileSave with
            | None ->
                Ok {
                    Arc = reloadedArc
                    PendingArcFileSave = None
                }
            | Some pendingArcFileSave ->
                match updateARCByFileContentDTO reloadedArc pendingArcFileSave with
                | Error mergeError -> Error mergeError
                | Ok mergedArc ->
                    Ok {
                        Arc = mergedArc
                        PendingArcFileSave = Some pendingArcFileSave
                    }

let private tryPersistPendingArcFileSave (vault: ArcVault) : JS.Promise<Result<unit, exn>> = promise {
    match vault.pendingArcFileSave with
    | None -> return Ok()
    | Some pendingArcFileSave ->
        match vault.path, vault.arc with
        | Some _, Some arc ->
            match updateARCByFileContentDTO arc pendingArcFileSave with
            | Error saveError -> return Error saveError
            | Ok newArc ->
                let! saveResult = vault.SetArc newArc

                match saveResult with
                | Ok() ->
                    vault.pendingArcFileSave <- None
                    return Ok()
                | Error saveError -> return Error saveError
        | _ -> return Error(exn "ARC is not loaded.")
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
        fun (request: FileContentDTO) -> promise {
            try
                let windowId = windowIdFromIpcEvent event

                match ARC_VAULTS.TryGetVault(windowId) with
                | None -> return Error(exn $"The ARC for window id {windowId} should exist")
                | Some vault ->
                    match vault.path, vault.arc with
                    | Some arcPath, Some arc ->
                        match updateARCByFileContentDTO arc request with
                        | Error saveError -> return Error saveError
                        | Ok newArc ->
                            let! saveResult = vault.SetArc newArc

                            match saveResult with
                            | Ok() ->
                                match vault.pendingArcFileSave with
                                | Some pendingSave when pathsEqualForComparison pendingSave.path request.path ->
                                    vault.pendingArcFileSave <- None
                                | _ -> ()

                                return Ok()
                            | Error saveError -> return Error saveError
                    | _ -> return Error(exn "ARC is not loaded.")
            with e ->
                return Error e
        }
    setPendingArcFileSave =
        fun (pendingArcFileSave: FileContentDTO option) -> promise {
            try
                let windowId = windowIdFromIpcEvent event

                match ARC_VAULTS.TryGetVault(windowId) with
                | None -> return Error(exn $"The ARC for window id {windowId} should exist")
                | Some vault ->
                    vault.pendingArcFileSave <- pendingArcFileSave
                    return Ok()
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
                    match vault.path with
                    | None -> return Error(exn "ARC is not loaded.")
                    | Some arcPath ->
                        let normalizedRelativePath = PathHelpers.normalizeRelativePath relativePath

                        if ArcDeleteHelper.isDeletePathAllowed normalizedRelativePath |> not then
                            return
                                Error(
                                    exn
                                        "Deletion is only allowed for descendants under studies/, assays/, workflows/, or runs/."
                                )
                        else
                            match tryResolveArcRelativePath arcPath normalizedRelativePath with
                            | Error pathError -> return Error pathError
                            | Ok absolutePath ->
                                vault.isBusyWriting <- true

                                try
                                    let! _ =
                                        fsPromisesDynamic?rm (
                                            absolutePath,
                                            createObj [ "recursive" ==> true; "force" ==> false ]
                                        )
                                        |> unbox<JS.Promise<obj>>

                                    let nextFileTree =
                                        removePathAndDescendants absolutePath vault.fileTree

                                    vault.SetFileTree nextFileTree

                                    match! ARC.tryLoadAsync arcPath with
                                    | Error loadError ->
                                        return Error(exn $"Unable to reload ARC after deleting '{normalizedRelativePath}': {loadError}")
                                    | Ok reloadedArc ->
                                        match
                                            ArcDeleteHelper.mergeReloadedArcAfterDelete
                                                normalizedRelativePath
                                                reloadedArc
                                                vault.pendingArcFileSave
                                        with
                                        | Error mergeError -> return Error mergeError
                                        | Ok mergeResult ->
                                            vault.arc <- Some mergeResult.Arc
                                            vault.pendingArcFileSave <- mergeResult.PendingArcFileSave
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

                match decision with
                | IPCTypesHelper.SaveBeforeQuitDecision.SaveAndClose ->
                    match ARC_VAULTS.TryGetVault(windowId) with
                    | None -> return Error(exn $"The ARC for window id {windowId} should exist")
                    | Some vault ->
                        let! saveResult = tryPersistPendingArcFileSave vault

                        match saveResult with
                        | Error saveError -> return Error saveError
                        | Ok() -> return! ARC_VAULTS.ResolveCloseRequest(windowId, decision)
                | _ -> return! ARC_VAULTS.ResolveCloseRequest(windowId, decision)
            with e ->
                return Error e
        }
}
