module Main.ArcVaultHelper


open System
open Swate.Components.Shared
open Swate.Electron.Shared.FileIOHelper
open Swate.Electron.Shared.FileIOTypes
open ARCtrl
open ARCtrl.Contract
open Fable.Electron
open Fable.Core
open Fable.Core.JsInterop
open Main
open Main.ArcMerge
open Main.Bindings
open Main.Bindings.Filesystem
open Node.Api

let private fsPromisesDynamic: obj = importAll "fs/promises"

/// Returns true when a path addresses Git's private repository metadata.
/// `.gitignore`, `.gitattributes`, and similarly named files remain ordinary ARC payload.
let isGitMetadataPath (pathValue: string) =
    pathValue
    |> getNonEmptyPathParts
    |> Array.exists (fun segment -> String.Equals(segment, ".git", StringComparison.OrdinalIgnoreCase))

let private getAllArcFilePathsIgnoringGitMetadataAsync (arcPath: string) =
    let rec collectFiles (absoluteDirectoryPath: string) (relativeDirectoryPath: string) = promise {
        let! entries = readdirWithTypesAsync absoluteDirectoryPath (ReaddirOptions(withFileTypes = true))

        let files = ResizeArray<string>()

        for entry in entries do
            let name = entry.name

            let relativePath =
                if relativeDirectoryPath = "" then
                    name
                else
                    $"{relativeDirectoryPath}/{name}"

            if not (isGitMetadataPath relativePath) then
                if entry.isDirectory () then
                    let absolutePath = path.join (absoluteDirectoryPath, name)
                    let! nestedFiles = collectFiles absolutePath relativePath
                    files.AddRange nestedFiles
                elif entry.isFile () then
                    files.Add relativePath

        return files.ToArray()
    }

    collectFiles arcPath ""

let private isGitMetadataContract (contract: Contract) =
    isGitMetadataPath contract.Path
    || match contract.Operation, contract.DTO with
       | Operation.RENAME, Some(DTO.Text targetPath) -> isGitMetadataPath targetPath
       | _ -> false

/// Loads an ARC while enforcing that Git metadata never enters its filesystem model.
let tryLoadArcIgnoringGitMetadataAsync (arcPath: string) = promise {
    let! paths = getAllArcFilePathsIgnoringGitMetadataAsync arcPath
    let arc = ARC.fromFilePaths paths

    match! fullFillContractBatchAsync arcPath (arc.GetReadContracts()) with
    | Error errors -> return Error errors
    | Ok contracts ->
        arc.SetISAFromContracts contracts
        return Ok arc
}

/// Replaces ARC.UpdateAsync for full ARC saves because its generated contracts may include Git metadata
/// and empty scaffold contracts that would overwrite existing payload files.
let updateArcPreservingExistingPayloadFiles (arcPath: string) (arc: ARC) = promise {
    let contractsToWrite = ResizeArray<Contract>()

    for contract in arc.GetUpdateContracts() |> Array.filter (isGitMetadataContract >> not) do
        match contract.Operation, contract.DTO with
        | Operation.CREATE, None ->
            let absolutePath = path.join (arcPath, contract.Path)
            let! fileExists = ARCtrl.FileSystemHelper.fileExistsAsync absolutePath

            if not fileExists then
                contractsToWrite.Add contract
        | _ -> contractsToWrite.Add contract

    match! fullFillContractBatchAsync arcPath (contractsToWrite.ToArray()) with
    | Ok _ -> ()
    | Error errors -> return failwith (PathHelpers.formatContractErrors errors)
}

/// This function mutably sets the datamap on the correct parent based on the datamap parent info included in the file content DTO.
/// It also ensures that the static hash is preserved to avoid unnecessary changes to the ARC when saving a datamap.
let private setDataMapByParentInfo (arc: ARC) (dmpi: DatamapParentInfo) (dm: DataMap) : Result<unit, exn> =
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

let private baselineDataMapStaticHash (dataMap: DataMap option) =
    dataMap |> Option.iter (fun dm -> dm.StaticHash <- cleanDataMapStaticHash dm)

/// Sets the current loaded ARC state as the clean in-memory baseline.
let baselineArcStaticHashes (arc: ARC) : unit =
    arc.License
    |> Option.iter (fun license -> license.StaticHash <- license.GetHashCode())

    for study in arc.Studies do
        study.StaticHash <- study.GetLightHashCode()
        baselineDataMapStaticHash study.DataMap

    for assay in arc.Assays do
        assay.StaticHash <- assay.GetLightHashCode()
        baselineDataMapStaticHash assay.DataMap

    for workflow in arc.Workflows do
        workflow.StaticHash <- workflow.GetLightHashCode()
        baselineDataMapStaticHash workflow.DataMap

    for run in arc.Runs do
        run.StaticHash <- run.GetLightHashCode()
        baselineDataMapStaticHash run.DataMap

    arc.StaticHash <- arc.GetLightHashCode()

/// Replaces the just-added ARC entity with the disk round-tripped version.
/// This keeps lossy serialization details from becoming immediate unsaved changes.
let syncAddedArcFileFromPersisted (source: ARC) (target: ARC) (arcFile: ArcFiles) : unit =
    match arcFile with
    | ArcFiles.Assay assay ->
        source.TryGetAssay assay.Identifier
        |> Option.iter (fun sourceAssay -> target.SetAssay(assay.Identifier, sourceAssay))
    | ArcFiles.Study(study, _) ->
        source.TryGetStudy study.Identifier
        |> Option.iter (fun sourceStudy -> target.SetStudy(study.Identifier, sourceStudy))
    | ArcFiles.Workflow workflow ->
        source.TryGetWorkflow workflow.Identifier
        |> Option.iter (fun sourceWorkflow -> target.SetWorkflow(workflow.Identifier, sourceWorkflow))
    | ArcFiles.Run run ->
        source.TryGetRun run.Identifier
        |> Option.iter (fun sourceRun -> target.SetRun(run.Identifier, sourceRun))
    | _ -> ()

/// This function should only be used for partial updates to an ARC based on a file content DTO.
let updateARCByFileContentDTO (oldArc: ARC) (dto: FileContentDTO) : Result<ARC, exn> =
    let arcfile = FileContentDTO.toArcFile dto

    match arcfile with
    | None -> Error(exn $"Unsupported file type for saving: {dto.fileType}")
    | Some arcfile ->
        match arcfile with
        // if we get an investigation we are only interested in updating the investigation part of the ARC, so we can avoid deserializing and reserializing the whole ARC which is costly for large ARCs.
        // This only works under the assumption, that we did not in fact do any changes to assay, study, ... . These are reused from the existing Investigation.
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

let swatelogfn id fmt =
    Printf.kprintf (fun s -> Browser.Dom.console.log ("[Swate-" + string id + "] " + s)) fmt

let swatefailfn id fmt =
    Printf.kprintf (fun s -> failwith ("[Swate-" + string id + "] " + s)) fmt

type private CanonicalArcFileRepairSpec = {
    CollectionFolder: string
    FileName: string
    CreateContracts: string -> Contract[]
}

let private canonicalArcFileRepairSpecs = [|
    {
        CollectionFolder = "assays"
        FileName = "isa.assay.xlsx"
        CreateContracts = fun identifier -> (ArcAssay.init identifier).ToCreateContract(false)
    }
    {
        CollectionFolder = "studies"
        FileName = "isa.study.xlsx"
        CreateContracts = fun identifier -> (ArcStudy.init identifier).ToCreateContract(false)
    }
    {
        CollectionFolder = "workflows"
        FileName = "isa.workflow.xlsx"
        CreateContracts = fun identifier -> (ArcWorkflow.init identifier).ToCreateContract(false)
    }
    {
        CollectionFolder = "runs"
        FileName = "isa.run.xlsx"
        CreateContracts = fun identifier -> (ArcRun.init identifier).ToCreateContract(false)
    }
|]

let private isZeroByteZipReadError (errors: string[]) =
    errors
    |> Array.exists (fun error ->
        let normalizedError = error.ToLowerInvariant()

        normalizedError.Contains("error reading contract")
        && normalizedError.Contains("data length = 0")
    )

let private tryReadDirectoryAsync (directoryPath: string) = promise {
    try
        let! entries = fsPromisesDynamic?readdir (directoryPath) |> unbox<JS.Promise<string[]>>
        return entries
    with _ ->
        return [||]
}

let private tryGetFileSizeAsync (filePath: string) = promise {
    try
        let! stats = fsPromisesDynamic?stat (filePath) |> unbox<JS.Promise<obj>>
        return Some(stats?size |> unbox<float>)
    with _ ->
        return None
}

let private repairZeroByteCanonicalArcFile
    (windowId: int)
    (arcPath: string)
    (spec: CanonicalArcFileRepairSpec)
    (identifier: string)
    =
    promise {
        let relativePath =
            ARCtrl.ArcPathHelper.combineMany [| spec.CollectionFolder; identifier; spec.FileName |]

        let absolutePath =
            path.join (arcPath, spec.CollectionFolder, identifier, spec.FileName)

        let! fileSize = tryGetFileSizeAsync absolutePath

        match fileSize with
        | Some size when size = 0.0 ->
            swatelogfn windowId "Repairing zero-byte ARC workbook: %s" relativePath
            let! repairResult = fullFillContractBatchAsync arcPath (spec.CreateContracts identifier)

            match repairResult with
            | Ok _ -> return true
            | Error errors ->
                swatelogfn
                    windowId
                    "Unable to repair zero-byte ARC workbook '%s': %s"
                    relativePath
                    (PathHelpers.formatContractErrors errors)

                return false
        | _ -> return false
    }

let private repairZeroByteCanonicalArcFiles (windowId: int) (arcPath: string) = promise {
    let mutable repairedAny = false

    for spec in canonicalArcFileRepairSpecs do
        let collectionPath = path.join (arcPath, spec.CollectionFolder)
        let! identifiers = tryReadDirectoryAsync collectionPath

        for identifier in identifiers do
            let! repaired = repairZeroByteCanonicalArcFile windowId arcPath spec identifier
            repairedAny <- repairedAny || repaired

    return repairedAny
}

/// Loads an ARC, repairing empty canonical workbooks that can be left behind by interrupted creates.
let tryLoadArcWithZeroByteRepair (windowId: int) (arcPath: string) = promise {
    let! loadResult = tryLoadArcIgnoringGitMetadataAsync arcPath

    match loadResult with
    | Ok arc ->
        baselineArcStaticHashes arc
        return Ok arc
    | Error errors when isZeroByteZipReadError errors ->
        let! repairedAny = repairZeroByteCanonicalArcFiles windowId arcPath

        if repairedAny then
            let! retryLoadResult = tryLoadArcIgnoringGitMetadataAsync arcPath

            match retryLoadResult with
            | Ok arc ->
                baselineArcStaticHashes arc
                return Ok arc
            | Error errors -> return Error errors
        else
            return Error errors
    | Error errors -> return Error errors
}

let createWindow () = promise {
    printfn "[Swate] Creating new window"
    let screenSize = screen.getPrimaryDisplay().workAreaSize

    let mainWindowOptions =
        BrowserWindowConstructorOptions(
            width = int screenSize.width,
            height = int screenSize.height,
            webPreferences = WebPreferences(preload = path.join (__dirname, "preload.fs.js"))
        )

    let window = BrowserWindow(mainWindowOptions)

    if isNullOrUndefined MAIN_WINDOW_VITE_DEV_SERVER_URL then
        do! window.loadFile (path.join (__dirname, $"../renderer/{MAIN_WINDOW_VITE_NAME}/index.html"))
    else
        window.webContents.openDevTools Enums.WebContents.OpenDevTools.Options.Mode.Right
        do! window.loadURL MAIN_WINDOW_VITE_DEV_SERVER_URL

    // Prevent links from opening new Electron windows
    window.webContents.setWindowOpenHandler (fun details ->
        Fable.Electron.Main.shell.openExternal details.url |> Promise.start
        WindowOpenHandlerResponse(Enums.Types.WindowOpenHandlerResponse.Action.Deny)
    )

    // Prevent navigation inside current Electron window
    window.webContents.onWillNavigate (fun event url _ _ _ _ ->
        let currentUrl = window.webContents.getURL ()

        if url <> currentUrl then
            event.preventDefault ()
            Fable.Electron.Main.shell.openExternal url |> Promise.start
    )

    window.title <- "Swate"

    return window
}

let shouldUsePollingByDefault (platform: string) =
    System.String.Equals(platform, "win32", System.StringComparison.OrdinalIgnoreCase)

let private currentNodePlatform () : string =
    emitJsExpr () "process.platform" |> unbox<string>

let createFileWatcher (path: string) (usePolling: bool option) =

    let ignoreFn =
        fun (path: string) ->
            let normalizedPath = PathHelpers.normalizeSeparators path
            let tempXlsxPattern = """\.~\$.*\.xlsx$"""

            System.Text.RegularExpressions.Regex.IsMatch(normalizedPath, tempXlsxPattern)
            || isGitMetadataPath normalizedPath

    // Native Windows file events can keep handles that block app-initiated folder renames.
    let usePolling =
        defaultArg usePolling (shouldUsePollingByDefault (currentNodePlatform ()))

    let watcherOptions =
        if usePolling then
            Chokidar.WatchOptions(
                cwd = path,
                awaitWriteFinish = true,
                ignored = !^ignoreFn,
                ignoreInitial = true,
                usePolling = true,
                interval = 200,
                binaryInterval = 400
            )
        else
            Chokidar.WatchOptions(cwd = path, awaitWriteFinish = true, ignored = !^ignoreFn, ignoreInitial = true)

    let watcher = Chokidar.Chokidar.watch (path, watcherOptions)

    watcher

open Fable.Electron.Remoting.Main

let sendArcHasUnsavedChangesUpdate (hasUnsavedChanges: bool) (window: BrowserWindow) =
    let sendMsg =
        Remoting.createIpc ()
        |> Remoting.withWindow window
        |> Remoting.buildProxySender<Swate.Electron.Shared.IPCTypes.MainToRendererIpc.IHasUnsavedArcChangesRendererApi>

    sendMsg.arcUnsavedChangesUpdate hasUnsavedChanges
