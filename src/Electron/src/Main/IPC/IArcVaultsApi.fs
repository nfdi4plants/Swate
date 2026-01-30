module Main.IPC.IArcVaultsApi

open Fable.Electron
open Swate.Electron.Shared.IPCTypes
open Fable.Electron.Main
open Fable.Core
open Fable.Core.JsInterop
open Main
open Node.Api
open ARCtrl
open ARCtrl.Json


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
            | None -> return Error(exn $"The ARC for path {arcPath} should exist")
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
    openFile =
        fun (path: string) -> promise {
            //let windowId = windowIdFromIpcEvent event
            match ARC_VAULTS.TryGetVault(1) with
            | None -> return Error(exn $"The ARC for window id {1} should exist")
            | Some vault ->
                Swate.Components.console.log ($"openFile path: {path}")
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

                Swate.Components.console.log (
                    $"Extracted identifier: {identifier} (isIsaFile={isIsaFile}, hasParentDir={hasParentDir}, fileName={fileName})"
                )

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
                    // Return the ARC/Investigation as JSON for proper rendering
                    Swate.Components.console.log ("Opening investigation file")

                    match vault.arc with
                    | Some arc ->
                        // ARC inherits from ArcInvestigation, serialize as investigation
                        let json = ARCtrl.ArcInvestigation.toJsonString 0 arc
                        return Ok(ArcFileData(ArcFileType.Investigation, json))
                    | None -> return Error(exn "ARC not loaded")

                | "study" ->
                    let study = vault.OpenStudy(identifier)

                    match study with
                    | Some s ->
                        Swate.Components.console.log (
                            "Found study: " + s.Identifier + " with " + string s.Tables.Count + " tables"
                        )

                        let json = ARCtrl.ArcStudy.toJsonString 0 s
                        return Ok(ArcFileData(ArcFileType.Study, json))
                    | None -> return Error(exn ("Study '" + identifier + "' not found in ARC"))

                | "assay" ->
                    let assay = vault.OpenAssay(identifier)

                    match assay with
                    | Some a ->
                        Swate.Components.console.log (
                            "Found assay: " + a.Identifier + " with " + string a.Tables.Count + " tables"
                        )

                        let json = ARCtrl.ArcAssay.toJsonString 0 a
                        return Ok(ArcFileData(ArcFileType.Assay, json))
                    | None -> return Error(exn ("Assay '" + identifier + "' not found in ARC"))

                | "run" ->
                    let run = vault.OpenRun(identifier)

                    match run with
                    | Some r ->
                        Swate.Components.console.log (
                            "Found run: " + r.Identifier + " with " + string r.Tables.Count + " tables"
                        )

                        let json = ARCtrl.ArcRun.toJsonString 0 r
                        return Ok(ArcFileData(ArcFileType.Run, json))
                    | None -> return Error(exn ("Run '" + identifier + "' not found in ARC"))

                | "workflow" ->
                    let workflow = vault.OpenWorkflow(identifier)

                    match workflow with
                    | Some w ->
                        Swate.Components.console.log ("Found workflow: " + w.Identifier)
                        let json = ARCtrl.ArcWorkflow.toJsonString 0 w
                        return Ok(ArcFileData(ArcFileType.Workflow, json))
                    | None -> return Error(exn ("Workflow '" + identifier + "' not found in ARC"))

                | "datamap" ->
                    // For datamap files, we need to read and parse the file directly
                    Swate.Components.console.log ("Opening datamap file")
                    // Datamaps need parent context - for now we'll parse just the datamap
                    try
                        let json = ARCtrl.DataMap.toJsonString 0 (ARCtrl.DataMap.init ())
                        return Ok(ArcFileData(ArcFileType.DataMap, json))
                    with e ->
                        return Error(exn ("Could not load datamap: " + e.Message))

                | _ ->
                    // Fallback to text preview for unknown file types
                    Swate.Components.console.log ("Unknown ISA file type, falling back to text preview")

                    try
                        let content = fs.readFileSync (path, "utf8")
                        return Ok(Text content)
                    with e ->
                        return Error(exn $"Could not read file {fileName}: {e.Message}")
        }
}