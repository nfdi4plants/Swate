module Main.IPC.ArcVaultsApi

open System
open Fable.Core
open Fable.Electron
open Fable.Electron.Main
open Fable.Electron.Remoting.Main
open Swate.Components.Shared
open Swate.Electron.Shared
open Swate.Electron.Shared.IPCTypes
open Swate.Electron.Shared.IPCTypes.MainToRendererIpc
open Swate.Electron.Shared.GitTypes
open Swate.Electron.Shared.FileIOTypes
open Swate.Electron.Shared.FileIOHelper
open Swate.Electron.Shared.DTOs.NoteSearchDto
open Node.Api
open Main
open Main.IPC.Delete
open Main.IPC.Rename
open Swate.Electron.Shared.DTOs.ProvenanceGroupingDto
open Main.IPC.FileSystemIO


let ensureNotesFolderAtArcPath =
    Main.Notes.NoteScaffolding.ensureNotesFolderAtArcPath

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

let private tryResolveExistingArcRelativePath
    (arcPath: string)
    (relativePath: string)
    : JS.Promise<Result<string, exn>> =
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

let private showPathInFileExplorerAsync (arcPath: string) (relativePath: string) : JS.Promise<Result<unit, exn>> = promise {
    match! tryResolveExistingArcRelativePath arcPath relativePath with
    | Error pathError -> return Error pathError
    | Ok absolutePath ->
        try
            shell.showItemInFolder absolutePath
            return Ok()
        with shellError ->
            return Error(exn $"Could not show '{relativePath}' in file explorer: {shellError.Message}")
}

let private openPathWithDefaultApplicationAsync
    (arcPath: string)
    (relativePath: string)
    : JS.Promise<Result<unit, exn>> =
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
            return! withLoadedArcVault event (fun vault -> operation vault.path.Value)
        with e ->
            return Error e
    }

let private initGitRepositoryForCreatedArcDisposition
    (initRepository: string -> JS.Promise<Main.Git.GitService.GitResult<string>>)
    (initGit: bool)
    (disposition: ArcOpenDisposition)
    : JS.Promise<Main.Git.GitService.GitResult<string option>> =
    promise {
        match initGit, disposition.CreatedArcPath with
        | true, Some createdArcPath ->
            let! initResult = initRepository createdArcPath
            return initResult |> Result.map (fun _ -> Some createdArcPath)
        | _ -> return Ok None
    }

let private notifyGitRepositoryInitialized (arcPath: string) =
    ARC_VAULTS.TryGetVaultByPath arcPath
    |> Option.iter (fun vault ->
        Remoting.createIpc ()
        |> Remoting.withWindow vault.window
        |> Remoting.buildProxySender<IGitRepositoryRendererApi>
        |> fun rendererApi -> rendererApi.gitRepositoryInitialized arcPath
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
                return Ok None
            elif r.filePaths.Length <> 1 then
                return Error(exn "Not exactly one path")
            else
                let arcPath = r.filePaths |> Array.exactlyOne |> PathHelpers.normalizePath

                let windowId = windowIdFromIpcEvent event
                let! disposition = ARC_VAULTS.OpenOrFocusArc(windowId, arcPath)
                return Ok(Some(ArcOpenDisposition.path disposition))
        }
    openARCByPath =
        fun (arcPath: string) -> promise {
            try
                let arcPath = PathHelpers.normalizePath arcPath
                let windowId = windowIdFromIpcEvent event
                let! disposition = ARC_VAULTS.OpenOrFocusArc(windowId, arcPath)
                return Ok(ArcOpenDisposition.path disposition)
            with e ->
                return Error e
        }
    createARC =
        fun (request: CreateArcRequest) -> promise {
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

                let arcPath =
                    ARCtrl.ArcPathHelper.combine arcContainerPath request.identifier
                    |> PathHelpers.normalizePath

                let windowId = windowIdFromIpcEvent event
                let! disposition = ARC_VAULTS.CreateOrFocusArc(windowId, arcPath, request.identifier)

                match!
                    initGitRepositoryForCreatedArcDisposition
                        Main.Git.GitProvisioningService.initRepository
                        request.initGit
                        disposition
                with
                | Error failure ->
                    Swate.Components.console.log (
                        $"Git init failed for '{ArcOpenDisposition.path disposition}': {failure.Message}"
                    )
                | Ok(Some initializedArcPath) -> notifyGitRepositoryInitialized initializedArcPath
                | Ok None -> ()

                return Ok(ArcOpenDisposition.path disposition)
        }
    ensureNotesFolder =
        fun () -> promise {
            try
                match tryGetVaultAndArcPath event with
                | Error error -> return Error error
                | Ok(_, arcPath) -> return! ensureNotesFolderAtArcPath arcPath
            with error ->
                return Error error
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
            runLoadedArcPathAction event (fun arcPath -> showPathInFileExplorerAsync arcPath relativePath)
    openPathWithDefaultApplication =
        fun (relativePath: string) ->
            runLoadedArcPathAction event (fun arcPath -> openPathWithDefaultApplicationAsync arcPath relativePath)
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
    pickImagePaths =
        fun () -> promise {
            try
                let properties = [|
                    Enums.Dialog.ShowOpenDialog.Options.Properties.OpenFile
                    Enums.Dialog.ShowOpenDialog.Options.Properties.MultiSelections
                |]

                let filters = [|
                    FileFilter(
                        "Images",
                        [|
                            "apng"
                            "avif"
                            "bmp"
                            "gif"
                            "heic"
                            "heif"
                            "ico"
                            "jpeg"
                            "jpg"
                            "png"
                            "svg"
                            "tif"
                            "tiff"
                            "webp"
                        |]
                    )
                |]

                let window = dialogParentFromIpcEvent event

                let! result = dialog.showOpenDialog (?window = window, properties = properties, filters = filters)

                if result.canceled then
                    return Error(exn "Cancelled")
                else
                    return Ok result.filePaths
            with e ->
                return Error(exn $"Could not pick image files: {e.Message}")
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
                        let absolutePath = resolveAbsolutePath filePath
                        let! content = ARCtrl.FileSystemHelper.readFileTextAsync absolutePath

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
    pathExists =
        fun (relativePath: string) ->
            runLoadedArcPathAction
                event
                (fun arcPath -> promise {
                    match tryResolveArcRelativePath arcPath relativePath with
                    | Error pathError -> return Error pathError
                    | Ok absolutePath ->
                        let! exists = pathExistsAsync absolutePath
                        return Ok exists
                })
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
    listProvenanceTables =
        fun () -> promise {
            try
                return!
                    withLoadedArcVault
                        event
                        (fun vault -> promise {
                            return Ok(Main.Provenance.ProvenanceGroupingReader.listTables vault.arc.Value)
                        })
            with e ->
                return Error e
        }
    loadProvenanceTable =
        fun (selection: ProvenanceTableSelectionDto) -> promise {
            try
                return!
                    withLoadedArcVault
                        event
                        (fun vault -> promise {
                            return Ok(Main.Provenance.ProvenanceGroupingReader.loadTable selection vault.arc.Value)
                        })
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
                return! withLoadedArcVault event (fun vault -> promise { return! vault.AddArcFile request })
            with e ->
                return Error e
        }
    createFileSystemItem =
        fun (request: CreateFileSystemItemRequest) -> promise {
            try
                return!
                    withLoadedArcVault
                        event
                        (fun vault -> promise {
                            return! ArcFileSystemHelper.createFileSystemItemOnDisk vault.path.Value request
                        })
            with e ->
                return Error e
        }
    copyExternalFilesToArc =
        fun (requests: CopyExternalFileRequest[]) -> promise {
            try
                return!
                    withLoadedArcVault
                        event
                        (fun vault ->
                            withBusyWriting
                                vault
                                (fun () -> promise {
                                    match!
                                        ArcFileSystemHelper.copyExternalFilesToArcOnDisk
                                            vault.path.Value
                                            requests
                                    with
                                    | Error error -> return Error error
                                    | Ok copiedPaths ->
                                        do! vault.RefreshFileTree()
                                        return Ok copiedPaths
                                }))
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
                    withLoadedArcVault
                        event
                        (fun vault -> promise {
                            let arcPath = vault.path.Value
                            let normalizedRelativePath = PathHelpers.normalizeRelativePath relativePath
                            let classification = ArcEntityPathRules.classifyDeleteTarget normalizedRelativePath

                            match classification with
                            | ArcEntityPathRules.DeletePathClassification.EntityFolderTarget _
                            | ArcEntityPathRules.DeletePathClassification.CanonicalFileTarget(ArcEntityPathRules.CanonicalArcFileTarget.EntityFile _,
                                                                                              _) ->
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
                            | ArcEntityPathRules.DeletePathClassification.CanonicalFileTarget(ArcEntityPathRules.CanonicalArcFileTarget.DataMapFile _,
                                                                                              normalizedGenericPath)
                            | ArcEntityPathRules.DeletePathClassification.GenericTarget normalizedGenericPath
                            | ArcEntityPathRules.DeletePathClassification.AddZoneDescendantTarget(_,
                                                                                                  normalizedGenericPath) ->
                                if ArcEntityPathRules.isDeletePathAllowed normalizedGenericPath |> not then
                                    return
                                        Error(
                                            exn
                                                "Deletion is only allowed for safe non-ARC filesystem items inside the ARC."
                                        )
                                else
                                    return!
                                        ArcFileSystemHelper.deleteGenericFileSystemItemOnDisk
                                            arcPath
                                            normalizedGenericPath
                            | ArcEntityPathRules.DeletePathClassification.CanonicalFileTarget(ArcEntityPathRules.CanonicalArcFileTarget.InvestigationFile,
                                                                                              _) ->
                                return Error(exn "Deleting the investigation file is not supported.")
                            | ArcEntityPathRules.DeletePathClassification.ProtectedTarget _ ->
                                return
                                    Error(
                                        exn
                                            "Deleting protected files (for example .gitkeep or readme.md) is not allowed."
                                    )
                            | ArcEntityPathRules.DeletePathClassification.DisallowedTarget _ ->
                                return
                                    Error(
                                        exn
                                            "Deletion is only allowed for safe non-ARC filesystem items inside the ARC."
                                    )
                        })
            with e ->
                return Error e
        }
    renamePath =
        fun (request: RenamePathRequest) -> promise {
            try
                return!
                    withLoadedArcVault
                        event
                        (fun vault -> promise {
                            let arcPath = vault.path.Value

                            match ArcEntityPathRules.classifyRenameTarget request.relativePath with
                            | ArcEntityPathRules.RenamePathClassification.GenericTarget _ ->
                                return! ArcFileSystemHelper.renameGenericFileSystemItemOnDisk arcPath request
                            | _ ->
                                match vault.arc with
                                | None -> return Error(exn "ARC is not loaded.")
                                | Some arcLocal ->
                                    let wasBusyWriting = vault.isBusyWriting
                                    vault.isBusyWriting <- true

                                    try
                                        match! ArcRenameHelper.renameArcEntityAsync arcPath request arcLocal with
                                        | Error renameError -> return Error renameError
                                        | Ok renamedArc ->
                                            vault.SetArc renamedArc
                                            vault.RefreshHasUnsavedArcChangesFlag()
                                            return Ok()
                                    finally
                                        vault.isBusyWriting <- wasBusyWriting
                        })
            with e ->
                return Error e
        }
    movePath =
        fun (request: MovePathRequest) -> promise {
            try
                return!
                    withLoadedArcVault
                        event
                        (fun vault -> promise {
                            return! ArcFileSystemHelper.moveGenericFileSystemItemOnDisk vault.path.Value request
                        })
            with e ->
                return Error e
        }
    renameOpenArcRoot =
        fun (newName: string) -> promise {
            try
                return!
                    withLoadedArcVault
                        event
                        (fun vault -> promise {
                            let oldPath = vault.path
                            let! renameResult = vault.RenameOpenArcRoot newName

                            match renameResult with
                            | Error renameError -> return Error renameError
                            | Ok renamedPath ->
                                oldPath |> Option.iter (fun path -> RECENT_ARCS.Remove(path) |> ignore)
                                RECENT_ARCS.Add(renamedPath) |> ignore
                                ARC_VAULTS.BroadcastRecentARCs()
                                return Ok renamedPath
                        })
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
                                | FileContentType.FileContentTypeIsPlainTextVariant ->
                                    let directoryPath = path.dirname absolutePath
                                    do! ARCtrl.FileSystemHelper.createDirectoryAsync directoryPath
                                    do! ARCtrl.FileSystemHelper.writeFileTextAsync absolutePath request.content
                                    do! vault.RefreshFileTree()
                                    return Ok()
                                | FileContentType.CLI ->
                                    return Error(exn "Direct writing of CLI files is not supported.")
                                | FileContentType.FileContentTypeIsISAFileVariant ->
                                    return
                                        Error(
                                            exn
                                                "Direct writing of ARC content files is not supported. Use saveArcFile for these file types to ensure ARC integrity."
                                        )
                                | _ ->
                                    return Error(exn $"Unsupported file content type for writing: {request.fileType}")
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
                            let! content = ARCtrl.FileSystemHelper.readFileTextAsync path
                            let fileType = FileContentDTO.inferTextFileTypeFromPath relativePath

                            let dto = FileContentDTO.create fileType content relativePath

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
