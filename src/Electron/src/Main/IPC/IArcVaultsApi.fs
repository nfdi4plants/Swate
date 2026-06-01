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
open ARCtrl.Contract
open ARC
open Main.IPC.FileSystemIO
open Main.IPC.Delete
open Main.IPC.Rename
open Swate.Electron.Shared.DTOs.NoteSearchDto


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
                        let absolutePath = resolveAbsolutePath filePath
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
                            | ArcDeletePathRules.DeletePathClassification.GenericTarget normalizedGenericPath
                            | ArcDeletePathRules.DeletePathClassification.AddZoneDescendantTarget(_, normalizedGenericPath) ->
                                if ArcDeletePathRules.isDeletePathAllowed normalizedGenericPath |> not then
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
                                            "Deletion is only allowed for safe non-ARC filesystem items inside the ARC."
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

