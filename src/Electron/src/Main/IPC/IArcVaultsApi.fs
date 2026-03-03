module Main.IPC.IArcVaultsApi

open System
open Swate.Electron.Shared
open Swate.Electron.Shared.IPCTypes
open Fable.Core
open Fable.Electron
open Fable.Electron.Main
open Fable.Electron.Remoting.Main
open Fable.Core.JsInterop
open Main
open Main.Git
open Node.Api
open ARCtrl
open ARCtrl.Json


let private fsPromisesDynamic: obj = importAll "fs/promises"
let private pathDynamic: obj = importAll "path"

let private normalizePathForComparison (pathValue: string) =
    pathValue.Replace("\\", "/").Trim().TrimEnd('/').ToLowerInvariant()

let private containsTraversalSegments (relativePath: string) =
    relativePath.Split('/')
    |> Array.exists (fun segment -> segment = "." || segment = "..")

let private tryResolveArcRelativeWritePath (arcPath: string) (requestedRelativePath: string) =
    let relativePath =
        requestedRelativePath.Replace("\\", "/").TrimStart('/').Trim()

    if String.IsNullOrWhiteSpace relativePath then
        Error(exn "RelativePath must not be empty.")
    elif containsTraversalSegments relativePath then
        Error(exn "RelativePath must not contain path traversal segments.")
    else
        let arcRoot = pathDynamic?resolve(arcPath) |> unbox<string>
        let absolutePath = pathDynamic?resolve(arcRoot, relativePath) |> unbox<string>

        let normalizedArcRoot = normalizePathForComparison arcRoot
        let normalizedAbsolutePath = normalizePathForComparison absolutePath
        let isWithinArcRoot =
            normalizedAbsolutePath = normalizedArcRoot
            || normalizedAbsolutePath.StartsWith(normalizedArcRoot + "/")

        if isWithinArcRoot then
            Ok absolutePath
        else
            Error(exn "RelativePath resolves outside the ARC root.")

let private mkdirRecursiveAsync (directoryPath: string) : JS.Promise<unit> =
    promise {
        let mkdirPromise =
            fsPromisesDynamic?mkdir(directoryPath, createObj [ "recursive" ==> true ])
            |> unbox<JS.Promise<obj>>

        let! _ = mkdirPromise
        return ()
    }

let private writeUtf8FileAsync (absolutePath: string) (content: string) : JS.Promise<unit> =
    promise {
        let writePromise =
            fsPromisesDynamic?writeFile(absolutePath, content, "utf8")
            |> unbox<JS.Promise<obj>>

        let! _ = writePromise
        return ()
    }

let private copyInvestigationMetadata (source: ArcInvestigation) (target: ARC) =
    target.Title <- source.Title
    target.Description <- source.Description
    target.Contacts <- source.Contacts
    target.Publications <- source.Publications
    target.SubmissionDate <- source.SubmissionDate
    target.PublicReleaseDate <- source.PublicReleaseDate
    target.OntologySourceReferences <- source.OntologySourceReferences
    target.Comments <- source.Comments

let private toPreviewDataOrUnsupported (arcFile: ArcFiles) =
    ArcFileSaveMapping.tryCreatePreviewData arcFile
    |> Option.map Ok
    |> Option.defaultValue (Error(exn "Saving this file type is not supported yet in Electron."))

let syncARCFile (arc: ARC) (request: SaveArcFileRequest) : Result<PreviewData, exn> =
    try
        match ArcFileSaveMapping.tryParseSaveRequest request with
        | Error parseError ->
            Error parseError
        | Ok (ArcFiles.Investigation investigation) ->
            copyInvestigationMetadata investigation arc
            Ok(ArcFileData(ArcFilesDiscriminate.Investigation, ArcInvestigation.toJsonString 0 arc))
        | Ok (ArcFiles.Study(study, _)) ->
            if arc.TryGetStudy(study.Identifier).IsSome then
                arc.SetStudy(study.Identifier, study)
                toPreviewDataOrUnsupported (ArcFiles.Study(study, []))
            else
                arc.InitStudy(study.Identifier) |> ignore
                arc.RegisterStudy(study.Identifier)
                arc.SetStudy(study.Identifier, study)
                toPreviewDataOrUnsupported (ArcFiles.Study(study, []))
        | Ok (ArcFiles.Assay assay) ->
            if arc.TryGetAssay(assay.Identifier).IsSome then
                arc.SetAssay(assay.Identifier, assay)
                toPreviewDataOrUnsupported (ArcFiles.Assay assay)
            else
                arc.InitAssay(assay.Identifier) |> ignore
                arc.SetAssay(assay.Identifier, assay)
                toPreviewDataOrUnsupported (ArcFiles.Assay assay)
        | Ok (ArcFiles.Run run) ->
            if arc.TryGetRun(run.Identifier).IsNone then
                Error(exn $"Run '{run.Identifier}' not found in ARC.")
            else
                arc.SetRun(run.Identifier, run)
                toPreviewDataOrUnsupported (ArcFiles.Run run)
        | Ok (ArcFiles.Workflow workflow) ->
            if arc.TryGetWorkflow(workflow.Identifier).IsNone then
                Error(exn $"Workflow '{workflow.Identifier}' not found in ARC.")
            else
                arc.SetWorkflow(workflow.Identifier, workflow)
                toPreviewDataOrUnsupported (ArcFiles.Workflow workflow)
        | Ok (ArcFiles.DataMap _) ->
            Error(exn "Saving DataMap preview is not supported yet in Electron.")
        | Ok (ArcFiles.Template _) ->
            Error(exn "Saving Template preview is not supported yet in Electron.")
    with e ->
        Error e

let private persistArcChangesAndRefreshVault
    (vault: ArcVault)
    (arc: ARC)
    (arcPath: string)
    (afterArcPersist: unit -> unit)
    =
    promise {
        vault.isBusyWriting <- true

        try
            // Persist only changed ISA contracts (not full filesystem rewrite).
            do! arc.UpdateAsync(arcPath)
            afterArcPersist ()
            do! vault.LoadArc()

            let fileTree = getFileEntries arcPath |> createFileEntryTree
            vault.SetFileTree(fileTree)
        finally
            vault.isBusyWriting <- false
    }

let private tryGetVaultAndArcPath (event: IpcMainEvent) =
    let windowId = windowIdFromIpcEvent event

    match ARC_VAULTS.TryGetVault(windowId) with
    | None -> Error(exn $"The ARC for window id {windowId} should exist")
    | Some vault ->
        match vault.path with
        | Some arcPath -> Ok(vault, arcPath)
        | None -> Error(exn "ARC is not loaded.")

let private withBusyWriting (vault: ArcVault) (operation: unit -> JS.Promise<Result<'T, exn>>) : JS.Promise<Result<'T, exn>> =
    promise {
        vault.isBusyWriting <- true

        try
            return! operation ()
        finally
            vault.isBusyWriting <- false
    }

let private toSharedGitFailureKind (kind: GitService.GitFailureKind) =
    match kind with
    | GitService.GitFailureKind.Unauthorized -> GitFailureKind.Unauthorized
    | GitService.GitFailureKind.Forbidden -> GitFailureKind.Forbidden
    | GitService.GitFailureKind.Network -> GitFailureKind.Network
    | GitService.GitFailureKind.Timeout -> GitFailureKind.Timeout
    | GitService.GitFailureKind.Canceled -> GitFailureKind.Canceled
    | GitService.GitFailureKind.Unknown -> GitFailureKind.Unknown

let private toGitOperationResult
    (successMessage: 'T -> string option)
    (result: GitService.GitResult<'T>)
    : Result<GitOperationResult, exn> =
    match result with
    | Ok payload ->
        Ok {
            Success = true
            Message = successMessage payload
            FailureKind = None
        }
    | Error failure ->
        Ok {
            Success = false
            Message = Some failure.Message
            FailureKind = Some(toSharedGitFailureKind failure.Kind)
        }

let private toStatusDto (status: GitService.GitStatusDto) : GitStatusDto = {
    Current = status.Current
    Tracking = status.Tracking
    Ahead = status.Ahead
    Behind = status.Behind
    IsClean = status.IsClean
    Files =
        status.Files
        |> Array.map (fun file -> {
            Path = file.Path
            Index = file.Index
            WorkingDir = file.WorkingDir
            OriginalPath = file.OriginalPath
        })
}

let private toDiffSummaryDto (diff: GitService.GitDiffSummaryDto) : GitDiffSummaryDto = {
    Changed = diff.Changed
    Insertions = diff.Insertions
    Deletions = diff.Deletions
}

let private createGitProgressReporter (vault: ArcVault) : GitService.GitProgressCallback =
    let rendererApi =
        Remoting.init
        |> Remoting.withWindow vault.window
        |> Remoting.buildClient<IMainUpdateRendererApi>

    fun progressEvent ->
        rendererApi.gitProgressUpdate {
            Method = Some progressEvent.method
            Stage = Some progressEvent.stage
            Progress = Some progressEvent.progress
            Processed = Some progressEvent.processed
            Total = Some progressEvent.total
        }

/// This depends on the types in this file, but the types on this file must call this to bind IPC calls :/
let api: IArcVaultsApi = {
    openARC =
        fun event -> promise {
            let! r =
                dialog.showOpenDialog (
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

                do! ARC_VAULTS.OpenARCInVault(windowId, arcPath)

                let recentARCs = ARCHolder.updateRecentARCs arcPath maxNumberRecentARCs
                ARC_VAULTS.BroadcastRecentARCs(recentARCs)

                let fileTree = getFileEntries arcPath |> createFileEntryTree

                ARC_VAULTS.SetFileTree(windowId, fileTree)

                return Ok arcPath
        }
    createARC =
        fun (event: IpcMainEvent) (identifier: string) -> promise {

            let! r =
                dialog.showOpenDialog (
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
                do! ARC_VAULTS.CreateARCInVault(windowId, arcPath, identifier)

                let recentARCs = ARCHolder.updateRecentARCs arcPath maxNumberRecentARCs
                ARC_VAULTS.BroadcastRecentARCs(recentARCs)

                let fileTree = getFileEntries arcPath |> createFileEntryTree

                ARC_VAULTS.SetFileTree(windowId, fileTree)
                return Ok arcPath
        }
    createARCInNewWindow =
        fun identifier -> promise {
            let! r =
                dialog.showOpenDialog (
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

                let recentARCs = ARCHolder.updateRecentARCs arcPath maxNumberRecentARCs

                match ARC_VAULTS.TryGetVaultByPath arcPath with
                | None ->
                    let! _ = ARC_VAULTS.RegisterVaultWithNewArc(arcPath, identifier)
                    ARC_VAULTS.BroadcastRecentARCs(recentARCs)
                    return Ok()
                | Some vault ->
                    vault.window.focus ()
                    ARC_VAULTS.BroadcastRecentARCs(recentARCs)
                    return Ok()
        }
    openARCInNewWindow =
        fun _ -> promise {
            let! r =
                dialog.showOpenDialog (
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
                let recentARCs = ARCHolder.updateRecentARCs arcPath maxNumberRecentARCs
                match ARC_VAULTS.TryGetVaultByPath arcPath with
                | None ->
                    let! windowId = ARC_VAULTS.RegisterVaultWithArc(arcPath)
                    ARC_VAULTS.BroadcastRecentARCs(recentARCs)

                    let fileTree = getFileEntries arcPath |> createFileEntryTree
                    ARC_VAULTS.SetFileTree(windowId, fileTree)

                    return Ok()
                | Some vault ->
                    vault.window.focus ()
                    ARC_VAULTS.BroadcastRecentARCs(recentARCs)
                    return Ok()
        }
    closeARC =
        fun event -> promise {
            try
                let windowId = windowIdFromIpcEvent event
                let vault = ARC_VAULTS.TryGetVault(windowId)

                if vault.IsSome && vault.Value.path.IsSome then
                    let recentARCs = ARCHolder.updateRecentARCs vault.Value.path.Value maxNumberRecentARCs
                    ARC_VAULTS.BroadcastRecentARCs(recentARCs)

                ARC_VAULTS.DisposeVault(windowId)
                return Ok()
            with e ->
                return Error e
        }
    focusExistingARCWindow =
        fun arcPath -> promise {
            match ARC_VAULTS.TryGetVaultByPath arcPath with
            | None ->
                let refreshedRecentARCs =
                    recentARCs
                    |> Array.filter (fun arc -> arc.path <> arcPath)

                if refreshedRecentARCs.Length <> recentARCs.Length then
                    setRecentARCs refreshedRecentARCs
                    ARC_VAULTS.BroadcastRecentARCs(refreshedRecentARCs)

                return Error(exn $"No open ARC window found for path {arcPath}.")
            | Some vault ->
                let recentARCs = ARCHolder.updateRecentARCs arcPath maxNumberRecentARCs
                vault.window.focus()
                ARC_VAULTS.BroadcastRecentARCs(recentARCs)
                return Ok()
        }
    getOpenPath =
        fun event -> promise {
            let windowId = windowIdFromIpcEvent event
            let vault = ARC_VAULTS.TryGetVault(windowId)

            if vault.IsSome then
                vault.Value.SetFileTree(vault.Value.fileTree)

            return vault |> Option.bind (fun v -> v.path)
        }
    getRecentARCs =
        fun _ -> promise {
            return recentARCs
        }
    checkForARC =
        fun path -> promise {
            return ARC_VAULTS.TryGetVaultByPath(path).IsSome
        }
    saveArcFile =
        fun (event: IpcMainEvent) (request: SaveArcFileRequest) -> promise {
            try
                let windowId = windowIdFromIpcEvent event

                match ARC_VAULTS.TryGetVault(windowId) with
                | None -> return Error(exn $"The ARC for window id {windowId} should exist")
                | Some vault ->
                    match vault.path, vault.arc with
                    | Some arcPath, Some arc ->
                        match syncARCFile arc request with
                        | Error saveError -> return Error saveError
                        | Ok previewData ->
                            do!
                                persistArcChangesAndRefreshVault
                                    vault
                                    arc
                                    arcPath
                                    (fun () -> ())

                            return Ok previewData
                    | _ -> return Error(exn "ARC is not loaded.")
            with e ->
                return Error e
        }
    writeFile =
        fun (event: IpcMainEvent) (request: WriteFileRequest) -> promise {
            try
                let windowId = windowIdFromIpcEvent event

                match ARC_VAULTS.TryGetVault(windowId) with
                | None -> return Error(exn $"The ARC for window id {windowId} should exist")
                | Some vault ->
                    match vault.path with
                    | None -> return Error(exn "ARC is not loaded.")
                    | Some arcPath ->
                        match tryResolveArcRelativeWritePath arcPath request.RelativePath with
                        | Error pathError ->
                            return Error pathError
                        | Ok absolutePath ->
                            vault.isBusyWriting <- true

                            try
                                let directoryPath = path.dirname absolutePath
                                do! mkdirRecursiveAsync directoryPath
                                do! writeUtf8FileAsync absolutePath request.Content

                                let fileTree = getFileEntries arcPath |> createFileEntryTree
                                vault.SetFileTree(fileTree)
                                return Ok()
                            finally
                                vault.isBusyWriting <- false
            with e ->
                return Error e
        }
    openFile =
        fun (event: IpcMainEvent) (path: string) -> promise {
            let windowId = windowIdFromIpcEvent event

            match ARC_VAULTS.TryGetVault(windowId) with
            | None -> return Error(exn $"The ARC for window id {windowId} should exist")
            | Some vault ->
                let normalizedPath = path.Replace("\\", "/")
                let pathParts = normalizedPath.Split('/')
                let fileName = pathParts |> Array.last

                // For isa.*.xlsx files, the identifier is the parent directory name
                // e.g., "studies/DilutionSeries/isa.study.xlsx" -> identifier is "DilutionSeries"
                let isIsaFile = fileName.StartsWith("isa.") && fileName.EndsWith(".xlsx")
                let hasParentDir = pathParts.Length >= 2

                let identifier =
                    if isIsaFile && hasParentDir then
                        pathParts.[pathParts.Length - 2]
                    elif fileName.Contains(".") then
                        fileName.Substring(0, fileName.LastIndexOf("."))
                    else
                        fileName

                // Determine the type based on the filename
                let fileType =
                    if fileName = "isa.investigation.xlsx" then "investigation"
                    elif fileName = "isa.study.xlsx" then "study"
                    elif fileName = "isa.assay.xlsx" then "assay"
                    elif fileName = "isa.run.xlsx" then "run"
                    elif fileName = "isa.workflow.xlsx" then "workflow"
                    elif fileName = "isa.datamap.xlsx" then "datamap"
                    else "unknown"

                match fileType with
                | "investigation" ->
                    match vault.arc with
                    | Some arc ->
                        // ARC inherits from ArcInvestigation; use shared preview mapping.
                        return
                            ArcFiles.Investigation arc
                            |> toPreviewDataOrUnsupported
                    | None -> return Error(exn "ARC not loaded")

                | "study" ->
                    let study = vault.OpenStudy(identifier)

                    match study with
                    | Some s ->
                        return
                            ArcFiles.Study(s, [])
                            |> toPreviewDataOrUnsupported
                    | None -> return Error(exn ("Study '" + identifier + "' not found in ARC"))

                | "assay" ->
                    let assay = vault.OpenAssay(identifier)

                    match assay with
                    | Some a ->
                        return
                            ArcFiles.Assay a
                            |> toPreviewDataOrUnsupported
                    | None -> return Error(exn ("Assay '" + identifier + "' not found in ARC"))

                | "run" ->
                    let run = vault.OpenRun(identifier)

                    match run with
                    | Some r ->
                        return
                            ArcFiles.Run r
                            |> toPreviewDataOrUnsupported
                    | None -> return Error(exn ("Run '" + identifier + "' not found in ARC"))

                | "workflow" ->
                    let workflow = vault.OpenWorkflow(identifier)

                    match workflow with
                    | Some w ->
                        return
                            ArcFiles.Workflow w
                            |> toPreviewDataOrUnsupported
                    | None -> return Error(exn ("Workflow '" + identifier + "' not found in ARC"))

                | "datamap" ->
                    match vault.arc with
                    | None -> return Error(exn "ARC not loaded")
                    | Some arc ->
                        let parentFolder =
                            if pathParts.Length >= 3 then
                                pathParts.[pathParts.Length - 3].ToLowerInvariant()
                            else
                                ""

                        let tryResolveDataMap () =
                            match parentFolder with
                            | "studies" ->
                                arc.TryGetStudy(identifier)
                                |> Option.bind (fun study -> study.DataMap)
                            | "assays" ->
                                arc.TryGetAssay(identifier)
                                |> Option.bind (fun assay -> assay.DataMap)
                            | "runs" ->
                                arc.TryGetRun(identifier)
                                |> Option.bind (fun run -> run.DataMap)
                            | _ ->
                                [
                                    arc.TryGetStudy(identifier)
                                    |> Option.bind (fun study -> study.DataMap)
                                    arc.TryGetAssay(identifier)
                                    |> Option.bind (fun assay -> assay.DataMap)
                                    arc.TryGetRun(identifier)
                                    |> Option.bind (fun run -> run.DataMap)
                                ]
                                |> List.tryPick id

                        match tryResolveDataMap () with
                        | Some dataMap ->
                            return Ok(ArcFileData(ArcFilesDiscriminate.DataMap, ARCtrl.DataMap.toJsonString 0 dataMap))
                        | None ->
                            return Error(exn $"DataMap '{identifier}' not found in ARC.")

                | _ ->
                    // Fallback to text preview for unknown file types
                    try
                        let content = fs.readFileSync (path, "utf8")
                        return Ok(Text content)
                    with e ->
                        return Error(exn $"Could not read file {fileName}: {e.Message}")
        }
    syncARC =
        fun (event: IpcMainEvent) (request: SaveArcFileRequest) -> promise {
            let windowId = windowIdFromIpcEvent event

            match ARC_VAULTS.TryGetVault(windowId) with
            | None -> return Error(exn $"The ARC for window id {windowId} should exist")
            | Some vault ->
                match vault.path, vault.arc with
                | Some arcPath, Some arc ->
                    match syncARCFile arc request with
                    | Error saveError -> return Error saveError
                    | Ok _ -> return Ok ()
                | _ -> return Error(exn "ARC is not loaded.")
        }
    getGitStatus =
        fun (event: IpcMainEvent) -> promise {
            match tryGetVaultAndArcPath event with
            | Error error -> return Error error
            | Ok (_, arcPath) ->
                let! result = GitService.getStatus arcPath

                match result with
                | Ok statusDto -> return Ok(toStatusDto statusDto)
                | Error failure ->
                    return Error(exn $"git status failed ({failure.Kind}): {failure.Message}")
        }
    getGitDiffSummary =
        fun (event: IpcMainEvent) -> promise {
            match tryGetVaultAndArcPath event with
            | Error error -> return Error error
            | Ok (_, arcPath) ->
                let! result = GitService.getDiffSummary arcPath

                match result with
                | Ok diffDto -> return Ok(toDiffSummaryDto diffDto)
                | Error failure ->
                    return Error(exn $"git diff summary failed ({failure.Kind}): {failure.Message}")
        }
    gitFetch =
        fun (event: IpcMainEvent) (request: GitRemoteOperationRequest) -> promise {
            match tryGetVaultAndArcPath event with
            | Error error -> return Error error
            | Ok (vault, arcPath) ->
                let progressReporter = createGitProgressReporter vault

                let! result = GitService.fetch arcPath request.Remote request.Branch (Some progressReporter)
                return toGitOperationResult (fun () -> Some "Fetch completed.") result
        }
    gitPull =
        fun (event: IpcMainEvent) (request: GitRemoteOperationRequest) -> promise {
            match tryGetVaultAndArcPath event with
            | Error error -> return Error error
            | Ok (vault, arcPath) ->
                let progressReporter = createGitProgressReporter vault

                return!
                    withBusyWriting vault (fun () -> promise {
                        let! result = GitService.pull arcPath request.Remote request.Branch (Some progressReporter)
                        return toGitOperationResult (fun () -> Some "Pull completed.") result
                    })
        }
    gitPush =
        fun (event: IpcMainEvent) (request: GitRemoteOperationRequest) -> promise {
            match tryGetVaultAndArcPath event with
            | Error error -> return Error error
            | Ok (vault, arcPath) ->
                let progressReporter = createGitProgressReporter vault

                let! result = GitService.push arcPath request.Remote request.Branch (Some progressReporter)
                return toGitOperationResult (fun () -> Some "Push completed.") result
        }
    gitStagePaths =
        fun (event: IpcMainEvent) (request: GitPathspecRequest) -> promise {
            match tryGetVaultAndArcPath event with
            | Error error -> return Error error
            | Ok (vault, arcPath) ->
                return!
                    withBusyWriting vault (fun () -> promise {
                        let! result = GitService.stagePaths arcPath request.Pathspecs
                        return toGitOperationResult (fun () -> Some "Files staged.") result
                    })
        }
    gitUnstagePaths =
        fun (event: IpcMainEvent) (request: GitPathspecRequest) -> promise {
            match tryGetVaultAndArcPath event with
            | Error error -> return Error error
            | Ok (vault, arcPath) ->
                return!
                    withBusyWriting vault (fun () -> promise {
                        let! result = GitService.unstagePaths arcPath request.Pathspecs
                        return toGitOperationResult (fun () -> Some "Files unstaged.") result
                    })
        }
    gitCommit =
        fun (event: IpcMainEvent) (request: GitCommitRequest) -> promise {
            match tryGetVaultAndArcPath event with
            | Error error -> return Error error
            | Ok (vault, arcPath) ->
                return!
                    withBusyWriting vault (fun () -> promise {
                        let! result = GitService.commit arcPath request.Message
                        return toGitOperationResult (fun commitHash -> Some $"Commit completed ({commitHash}).") result
                    })
        }
    createBranch =
        fun (event: IpcMainEvent) (request: GitCreateBranchRequest) -> promise {
            match tryGetVaultAndArcPath event with
            | Error error -> return Error error
            | Ok (vault, arcPath) ->
                return!
                    withBusyWriting vault (fun () -> promise {
                        let! result = GitService.createBranch arcPath request.Name request.StartPoint
                        return toGitOperationResult (fun () -> Some $"Branch '{request.Name}' created.") result
                    })
        }
    checkoutBranch =
        fun (event: IpcMainEvent) (request: GitCheckoutBranchRequest) -> promise {
            match tryGetVaultAndArcPath event with
            | Error error -> return Error error
            | Ok (vault, arcPath) ->
                return!
                    withBusyWriting vault (fun () -> promise {
                        let! result = GitService.checkoutBranch arcPath request.Name
                        return toGitOperationResult (fun () -> Some $"Checked out branch '{request.Name}'.") result
                    })
        }
    }
