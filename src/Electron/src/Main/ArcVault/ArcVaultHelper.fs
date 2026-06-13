module Main.ArcVaultHelper


open System
open Swate.Components.Shared
open Swate.Electron.Shared.FileIOHelper
open Swate.Electron.Shared.FileIOTypes
open ARCtrl
open Fable.Electron
open Fable.Core
open Fable.Core.JsInterop
open Main
open Main.ARCtrlExtensions
open Main.Bindings
open Node.Api
open Swate.Electron.Shared.RenamePathRules

let private fsPromisesDynamic: obj = importAll "fs/promises"
let private pathDynamic: obj = importAll "path"

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

type OpenArcRootRenamePlan = {
    SourcePath: string
    TargetPath: string
    NewName: string
}

let private resolveAbsolutePath (pathValue: string) =
    pathDynamic?resolve (pathValue) |> unbox<string> |> PathHelpers.normalizePath

let private tryGetNodeErrorCode (error: exn) : string option =
    try
        error?code |> unbox<string> |> Option.ofObj
    with _ ->
        None

let private pathExistsAsync (absolutePath: string) : JS.Promise<bool> = promise {
    let! fileExists = ARCtrl.FileSystemHelper.fileExistsAsync absolutePath

    if fileExists then
        return true
    else
        return! ARCtrl.FileSystemHelper.directoryExistsAsync absolutePath
}

let private renameWithRetriesAsync
    (sourceAbsolutePath: string)
    (targetAbsolutePath: string)
    : JS.Promise<Result<unit, exn>> =
    let retryDelaysMs = [| 0; 75; 200; 500 |]

    let isTransientRenameError =
        function
        | Some "EPERM"
        | Some "EACCES"
        | Some "EBUSY" -> true
        | _ -> false

    let rec attempt (attemptIndex: int) = promise {
        if attemptIndex > 0 then
            do! Promise.sleep retryDelaysMs.[attemptIndex]

        try
            let! _ =
                fsPromisesDynamic?rename (sourceAbsolutePath, targetAbsolutePath)
                |> unbox<JS.Promise<obj>>

            return Ok()
        with renameError ->
            let errorCode = tryGetNodeErrorCode renameError

            if attemptIndex < retryDelaysMs.Length - 1 && isTransientRenameError errorCode then
                return! attempt (attemptIndex + 1)
            else
                return Error renameError
    }

    attempt 0

let private mapArcRootRenameDiskError (sourcePath: string) (targetPath: string) (renameError: exn) =
    match tryGetNodeErrorCode renameError with
    | Some "EPERM"
    | Some "EACCES" ->
        exn
            $"Cannot rename '{sourcePath}' to '{targetPath}'. Windows reported a permission or file-lock conflict. If the destination already exists, choose a different name and close apps that may be using these paths."
    | Some "ENOTEMPTY"
    | Some "EEXIST" -> exn $"Cannot rename '{sourcePath}' to '{targetPath}' because the destination already exists."
    | Some "ENOENT" -> exn $"Cannot rename '{sourcePath}' because the source path no longer exists on disk."
    | _ -> renameError

let tryBuildOpenArcRootRenamePlan arcPath newName : Result<OpenArcRootRenamePlan, exn> =
    let candidateName = newName |> Option.ofObj |> Option.defaultValue String.Empty

    match validateRenameName candidateName with
    | Error validationError -> Error(exn validationError)
    | Ok normalizedName ->
        if
            (pathDynamic?isAbsolute (normalizedName) |> unbox<bool>)
            || PathHelpers.containsPathTraversalSegments normalizedName
        then
            Error(exn "ARC folder name must be a single folder name without path separators or traversal segments.")
        else
            let sourcePath = resolveAbsolutePath arcPath
            let parentPath = pathDynamic?dirname (sourcePath) |> unbox<string>

            let targetPath =
                pathDynamic?resolve (parentPath, normalizedName)
                |> unbox<string>
                |> PathHelpers.normalizePath

            if
                String.Equals(
                    PathHelpers.normalizePathForFsComparison sourcePath,
                    PathHelpers.normalizePathForFsComparison targetPath,
                    StringComparison.Ordinal
                )
            then
                Error(exn "New ARC folder name must be different from the current folder name.")
            else
                Ok {
                    SourcePath = sourcePath
                    TargetPath = targetPath
                    NewName = normalizedName
                }

let renameOpenArcRootDirectoryOnDisk arcPath newName : JS.Promise<Result<string, exn>> = promise {
    match tryBuildOpenArcRootRenamePlan arcPath newName with
    | Error validationError -> return Error validationError
    | Ok plan ->
        let! sourceExists = ARCtrl.FileSystemHelper.directoryExistsAsync plan.SourcePath

        if not sourceExists then
            return Error(exn $"Cannot rename '{plan.SourcePath}' because the source path no longer exists on disk.")
        else
            let! targetExists = pathExistsAsync plan.TargetPath

            if targetExists then
                return
                    Error(
                        exn
                            $"Cannot rename '{plan.SourcePath}' to '{plan.TargetPath}' because the destination already exists."
                    )
            else
                let! renameResult = renameWithRetriesAsync plan.SourcePath plan.TargetPath

                match renameResult with
                | Ok() -> return Ok plan.TargetPath
                | Error renameError ->
                    return Error(mapArcRootRenameDiskError plan.SourcePath plan.TargetPath renameError)
}

let createWindow () = promise {
    printfn "[Swate] Creating new window"
    let screenSize = screen.getPrimaryDisplay().workAreaSize

    let windowIconPath = Helper.Assets.getIcon ()

    let mainWindowOptions =
        BrowserWindowConstructorOptions(
            icon = (windowIconPath |> U2.Case2),
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
