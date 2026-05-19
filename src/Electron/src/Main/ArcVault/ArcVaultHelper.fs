module Main.ArcVaultHelper


open Swate.Components.Shared
open Swate.Electron.Shared.FileIOHelper
open Swate.Electron.Shared.FileIOTypes
open ARCtrl
open Fable.Electron
open Fable.Core.JsInterop
open Main
open Main.ArcMerge
open Main.Bindings
open Node.Api

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

let private syncDataMapStaticHash (sourceDataMap: DataMap option) (targetDataMap: DataMap option) =
    match sourceDataMap, targetDataMap with
    | Some sourceDataMap, Some targetDataMap -> targetDataMap.StaticHash <- sourceDataMap.StaticHash
    | _ -> ()

/// Syncs static hashes from source ARC to target ARC for matching entities.
/// This keeps ARCtrl update contract generation scoped to actual changes.
let syncArcStaticHashes (source: ARC) (target: ARC) : unit =
    target.StaticHash <- source.StaticHash

    match source.License, target.License with
    | Some sourceLicense, Some targetLicense -> targetLicense.StaticHash <- sourceLicense.StaticHash
    | _ -> ()

    for targetStudy in target.Studies do
        match source.TryGetStudy targetStudy.Identifier with
        | Some sourceStudy ->
            targetStudy.StaticHash <- sourceStudy.StaticHash
            syncDataMapStaticHash sourceStudy.DataMap targetStudy.DataMap
        | None -> ()

    for targetAssay in target.Assays do
        match source.TryGetAssay targetAssay.Identifier with
        | Some sourceAssay ->
            targetAssay.StaticHash <- sourceAssay.StaticHash
            syncDataMapStaticHash sourceAssay.DataMap targetAssay.DataMap
        | None -> ()

    for targetWorkflow in target.Workflows do
        match source.TryGetWorkflow targetWorkflow.Identifier with
        | Some sourceWorkflow ->
            targetWorkflow.StaticHash <- sourceWorkflow.StaticHash
            syncDataMapStaticHash sourceWorkflow.DataMap targetWorkflow.DataMap
        | None -> ()

    for targetRun in target.Runs do
        match source.TryGetRun targetRun.Identifier with
        | Some sourceRun ->
            targetRun.StaticHash <- sourceRun.StaticHash
            syncDataMapStaticHash sourceRun.DataMap targetRun.DataMap
        | None -> ()

/// Copies ARC and preserves static hashes so unchanged entities are not treated as newly created.
let copyArcPreservingStaticHashes (arc: ARC) : ARC =
    let copiedArc = arc.Copy()
    syncArcStaticHashes arc copiedArc
    copiedArc

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

    return window
}

let shouldUsePollingByDefault (platform: string) =
    System.String.Equals(platform, "win32", System.StringComparison.OrdinalIgnoreCase)

let private currentNodePlatform () : string =
    emitJsExpr () "process.platform" |> unbox<string>

let createFileWatcher (path: string) (usePolling: bool option) =

    let ignoreFn =
        fun (path: string) ->
            let normalizedPath = path.Replace("\\", "/")

            let segments =
                normalizedPath.Trim('/').Split('/', System.StringSplitOptions.RemoveEmptyEntries)

            let tempXlsxPattern = """\.~\$.*\.xlsx$"""

            // skip temporary Excel files (created when editing an xlsx file)
            if System.Text.RegularExpressions.Regex.IsMatch(normalizedPath, tempXlsxPattern) then
                true
            // skip git folder itself (and its contents) to avoid expensive scans
            elif segments |> Array.exists (fun segment -> segment = ".git") then
                true
            else
                false

    // Native Windows file events can keep handles that block app-initiated folder renames.
    let usePolling = defaultArg usePolling (shouldUsePollingByDefault (currentNodePlatform ()))

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
            Chokidar.WatchOptions(
                cwd = path,
                awaitWriteFinish = true,
                ignored = !^ignoreFn,
                ignoreInitial = true
            )

    let watcher = Chokidar.Chokidar.watch (path, watcherOptions)

    watcher

open Fable.Electron.Remoting.Main

let sendArcHasUnsavedChangesUpdate (hasUnsavedChanges: bool) (window: BrowserWindow) =
    let sendMsg =
        Remoting.createIpc ()
        |> Remoting.withWindow window
        |> Remoting.buildProxySender<Swate.Electron.Shared.IPCTypes.MainToRendererIpc.IHasUnsavedArcChangesRendererApi>

    sendMsg.arcUnsavedChangesUpdate hasUnsavedChanges
