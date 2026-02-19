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
open System
open System.Text.RegularExpressions

let private fsDynamic: obj = importAll "fs"

let private toNonEmptyOption (value: string option) =
    value
    |> Option.bind (fun s ->
        let trimmed = s.Trim()
        if String.IsNullOrWhiteSpace trimmed then None else Some trimmed
    )

let private normalizeStringArray (values: string []) =
    values
    |> Array.map (fun s -> s.Trim())
    |> Array.filter (String.IsNullOrWhiteSpace >> not)

let private sanitizeIdentifierCandidate (candidate: string) =
    let cleaned = Regex.Replace(candidate, @"[^a-zA-Z0-9_\- ]", " ").Trim()
    let collapsed = Regex.Replace(cleaned, @"\s+", " ").Trim()
    if String.IsNullOrWhiteSpace collapsed then
        "Experiment"
    else
        collapsed

let private resolveIdentifier (metadata: ExperimentMetadata) =
    match toNonEmptyOption metadata.Identifier with
    | Some id -> id
    | None ->
        let titlePart =
            sanitizeIdentifierCandidate metadata.Title
        $"{titlePart} {DateTime.UtcNow:yyyyMMddHHmmss}"

let private parsePersons (values: string []) =
    values
    |> normalizeStringArray
    |> Array.map Person.fromJsonString
    |> ResizeArray

let private parseComments (values: string []) =
    values
    |> normalizeStringArray
    |> Array.map Comment.fromJsonString
    |> ResizeArray

let private parsePublications (values: string []) =
    values
    |> normalizeStringArray
    |> Array.map Publication.fromJsonString
    |> ResizeArray

let private parseOntologyAnnotations (values: string []) =
    values
    |> normalizeStringArray
    |> Array.map OntologyAnnotation.fromJsonString
    |> ResizeArray

let private parseOntologyOption (value: string option) =
    value
    |> toNonEmptyOption
    |> Option.map OntologyAnnotation.fromJsonString

let private fillInputColumnIfFilesExist (table: ArcTable) (files: string []) =
    let normalizedFiles = normalizeStringArray files

    if normalizedFiles.Length > 0 then
        if table.TryGetInputColumn().IsNone then
            table.AddColumn(CompositeHeader.Input IOType.Data)

        let rows =
            normalizedFiles
            |> Array.map (fun fileName ->
                ResizeArray [ CompositeCell.createDataFromString(fileName) ]
            )
            |> ResizeArray

        table.AddRows(rows)

let private createProtocolFile
    (arcRootPath: string)
    (target: ExperimentTarget)
    (identifier: string)
    (mainText: string option)
    =
    match toNonEmptyOption mainText with
    | None -> None
    | Some content ->
        let entityFolder =
            match target with
            | ExperimentTarget.Study -> "studies"
            | ExperimentTarget.Assay -> "assays"

        let relativePath = $"{entityFolder}/{identifier}/protocols/{identifier}_protocol.md"
        let entityPath = path.join (path.join (arcRootPath, entityFolder), identifier)
        let protocolDir = path.join (entityPath, "protocols")
        let absolutePath = path.join (protocolDir, $"{identifier}_protocol.md")

        fsDynamic?mkdirSync(protocolDir, createObj [ "recursive" ==> true ]) |> ignore
        fsDynamic?writeFileSync(absolutePath, content, "utf8") |> ignore

        Some relativePath

let private copyInvestigationMetadata (source: ArcInvestigation) (target: ARC) =
    target.Title <- source.Title
    target.Description <- source.Description
    target.Contacts <- source.Contacts
    target.Publications <- source.Publications
    target.SubmissionDate <- source.SubmissionDate
    target.PublicReleaseDate <- source.PublicReleaseDate
    target.OntologySourceReferences <- source.OntologySourceReferences
    target.Comments <- source.Comments

let private applyArcFileSaveRequest (arc: ARC) (request: SaveArcFileRequest) : Result<PreviewData, exn> =
    try
        match request.FileType with
        | ArcFileType.Investigation ->
            let investigation = ArcInvestigation.fromJsonString request.Json
            copyInvestigationMetadata investigation arc
            Ok(ArcFileData(ArcFileType.Investigation, ArcInvestigation.toJsonString 0 arc))
        | ArcFileType.Study ->
            let study = ArcStudy.fromJsonString request.Json

            if arc.TryGetStudy(study.Identifier).IsNone then
                Error(exn $"Study '{study.Identifier}' not found in ARC.")
            else
                arc.SetStudy(study.Identifier, study)
                Ok(ArcFileData(ArcFileType.Study, ArcStudy.toJsonString 0 study))
        | ArcFileType.Assay ->
            let assay = ArcAssay.fromJsonString request.Json

            if arc.TryGetAssay(assay.Identifier).IsNone then
                Error(exn $"Assay '{assay.Identifier}' not found in ARC.")
            else
                arc.SetAssay(assay.Identifier, assay)
                Ok(ArcFileData(ArcFileType.Assay, ArcAssay.toJsonString 0 assay))
        | ArcFileType.Run ->
            let run = ArcRun.fromJsonString request.Json

            if arc.TryGetRun(run.Identifier).IsNone then
                Error(exn $"Run '{run.Identifier}' not found in ARC.")
            else
                arc.SetRun(run.Identifier, run)
                Ok(ArcFileData(ArcFileType.Run, ArcRun.toJsonString 0 run))
        | ArcFileType.Workflow ->
            let workflow = ArcWorkflow.fromJsonString request.Json

            if arc.TryGetWorkflow(workflow.Identifier).IsNone then
                Error(exn $"Workflow '{workflow.Identifier}' not found in ARC.")
            else
                arc.SetWorkflow(workflow.Identifier, workflow)
                Ok(ArcFileData(ArcFileType.Workflow, ArcWorkflow.toJsonString 0 workflow))
        | ArcFileType.DataMap ->
            Error(exn "Saving DataMap preview is not supported yet in Electron.")
    with e ->
        Error e


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
    createExperimentFromLanding =
        fun (event: IpcMainEvent) (request: CreateExperimentRequest) -> promise {
            try
                let windowId = windowIdFromIpcEvent event

                match ARC_VAULTS.TryGetVault(windowId) with
                | None -> return Error(exn $"The ARC for window id {windowId} should exist")
                | Some vault ->
                    match vault.path, vault.arc with
                    | Some arcPath, Some arc ->

                        if String.IsNullOrWhiteSpace request.Metadata.Title then
                            return Error(exn "Title is required.")
                        elif String.IsNullOrWhiteSpace request.Metadata.Description then
                            return Error(exn "Description is required.")
                        else
                            let identifier = resolveIdentifier request.Metadata

                            if ARCtrl.Helper.Identifier.tryCheckValidCharacters identifier |> not then
                                return
                                    Error(
                                        exn
                                            "Identifier contains forbidden characters. Allowed: letters, digits, underscore (_), dash (-), and whitespace."
                                    )
                            elif request.Target = ExperimentTarget.Study && arc.TryGetStudy(identifier).IsSome then
                                return Error(exn $"Study with identifier '{identifier}' already exists.")
                            elif request.Target = ExperimentTarget.Assay && arc.TryGetAssay(identifier).IsSome then
                                return Error(exn $"Assay with identifier '{identifier}' already exists.")
                            else
                                let mutable previewData: PreviewData option = None

                                match request.Target with
                                | ExperimentTarget.Study ->
                                    let study = arc.InitStudy(identifier)
                                    arc.RegisterStudy(identifier)
                                    study.InitTable($"{identifier} Table") |> ignore

                                    study.Title <- Some request.Metadata.Title
                                    study.Description <- Some request.Metadata.Description
                                    study.Contacts <- parsePersons request.Metadata.InvolvedPeople
                                    study.Comments <- parseComments request.Metadata.Comments
                                    study.Publications <- parsePublications request.Metadata.Publications
                                    study.SubmissionDate <- toNonEmptyOption request.Metadata.SubmissionDate
                                    study.PublicReleaseDate <- toNonEmptyOption request.Metadata.PublicReleaseDate
                                    study.StudyDesignDescriptors <- parseOntologyAnnotations request.Metadata.StudyDesignDescriptors

                                    let firstTable = study.Tables.[0]
                                    fillInputColumnIfFilesExist firstTable request.Metadata.Files

                                    previewData <- Some(ArcFileData(ArcFileType.Study, ArcStudy.toJsonString 0 study))

                                | ExperimentTarget.Assay ->
                                    let assay = arc.InitAssay(identifier)
                                    assay.InitTable($"{identifier} Table") |> ignore

                                    assay.Title <- Some request.Metadata.Title
                                    assay.Description <- Some request.Metadata.Description
                                    assay.Performers <- parsePersons request.Metadata.InvolvedPeople
                                    assay.Comments <- parseComments request.Metadata.Comments
                                    assay.MeasurementType <- parseOntologyOption request.Metadata.MeasurementType
                                    assay.TechnologyType <- parseOntologyOption request.Metadata.TechnologyType
                                    assay.TechnologyPlatform <- parseOntologyOption request.Metadata.TechnologyPlatform

                                    let firstTable = assay.Tables.[0]
                                    fillInputColumnIfFilesExist firstTable request.Metadata.Files

                                    previewData <- Some(ArcFileData(ArcFileType.Assay, ArcAssay.toJsonString 0 assay))

                                vault.isBusyWriting <- true

                                try
                                    do! arc.WriteAsync(arcPath)
                                    let protocolPath =
                                        createProtocolFile arcPath request.Target identifier request.Metadata.MainText
                                    do! vault.LoadArc()

                                    let fileTree = getFileEntries arcPath |> createFileEntryTree
                                    vault.SetFileTree(fileTree)

                                    let response = {
                                        PreviewData =
                                            previewData
                                            |> Option.defaultWith (fun () -> Text "")
                                        CreatedIdentifier = identifier
                                        ProtocolPath = protocolPath
                                    }

                                    return Ok response
                                finally
                                    vault.isBusyWriting <- false
                    | _ -> return Error(exn "ARC is not loaded.")
            with e ->
                return Error e
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
                        match applyArcFileSaveRequest arc request with
                        | Error saveError -> return Error saveError
                        | Ok previewData ->
                            vault.isBusyWriting <- true

                            try
                                do! arc.WriteAsync(arcPath)
                                do! vault.LoadArc()

                                let fileTree = getFileEntries arcPath |> createFileEntryTree
                                vault.SetFileTree(fileTree)

                                return Ok previewData
                            finally
                                vault.isBusyWriting <- false
                    | _ -> return Error(exn "ARC is not loaded.")
            with e ->
                return Error e
        }
    openFile =
        fun (event: IpcMainEvent) (path: string) -> promise {
            let windowId = windowIdFromIpcEvent event

            match ARC_VAULTS.TryGetVault(windowId) with
            | None -> return Error(exn $"The ARC for window id {windowId} should exist")
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
