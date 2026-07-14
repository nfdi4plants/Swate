module Main.IPC.FilePickerApi

open System
open System.Collections.Generic
open Fable.Electron
open Fable.Electron.Main
open Main.Bindings
open Swate.Components.Shared
open Swate.Electron.Shared.FileIOTypes
open Swate.Electron.Shared.IPCTypes

// Keeps dropped file paths in main so renderer code never asks Electron for an absolute path directly.
let private droppedFilePathsByKey = Dictionary<string, string>()

let private normalizeDialogFilterExtension (extension: string) =
    extension
    |> Option.ofObj
    |> Option.map (fun value -> value.Trim())
    |> Option.bind (fun value ->
        let normalizedValue = if value.StartsWith(".") then value.Substring(1) else value

        if String.IsNullOrWhiteSpace normalizedValue then
            None
        else
            Some normalizedValue
    )

let private openDialogFiltersFromExtensions (filterExtensions: string[] option) =
    let normalizedExtensions =
        filterExtensions
        |> Option.defaultValue [||]
        |> Array.choose normalizeDialogFilterExtension
        |> Array.distinct

    if normalizedExtensions.Length = 0 then
        None
    else
        Some [| FileFilter("Supported files", normalizedExtensions) |]

let private openFileDialogProperties (allowMultiple: bool option) = [|
    Enums.Dialog.ShowOpenDialog.Options.Properties.OpenFile

    if allowMultiple |> Option.defaultValue true then
        Enums.Dialog.ShowOpenDialog.Options.Properties.MultiSelections
|]

/// Stores preload-captured dropped file paths by metadata key for later renderer resolution.
let private registerDroppedFilePaths (registrations: DroppedFilePathRegistration[]) =
    for registration in registrations do
        match
            registration.absolutePath
            |> Option.ofObj
            |> Option.map PathHelpers.normalizePath
        with
        | Some absolutePath when not (String.IsNullOrWhiteSpace absolutePath) ->
            droppedFilePathsByKey.[registration.key] <- absolutePath
        | _ -> ()

/// Listens for preload drop registrations outside the typed remoting bridge.
let registerDroppedFilePathListener () =
    Node.ipcMain.on (
        DroppedFilePathsRegisteredChannel,
        System.Action<IpcMainEvent, obj>(fun _event payload ->
            if not (isNull payload) then
                try
                    payload |> unbox<DroppedFilePathRegistration[]> |> registerDroppedFilePaths
                with _ ->
                    ()
        )
    )

let api (event: IpcMainInvokeEvent) : IFilePickerApi = {
    pickFilePaths =
        fun (request: PickExternalFilePathsRequest) -> promise {
            try
                let properties = openFileDialogProperties request.allowMultiple
                let filters = openDialogFiltersFromExtensions request.filterExtensions
                let window = dialogParentFromIpcEvent event

                let! result =
                    match filters with
                    | Some filters ->
                        dialog.showOpenDialog (?window = window, properties = properties, filters = filters)
                    | None -> dialog.showOpenDialog (?window = window, properties = properties)

                if result.canceled then
                    return Error(exn "Cancelled")
                else
                    return Ok(result.filePaths |> Array.map PathHelpers.normalizePath)
            with e ->
                return Error(exn $"Could not pick files: {e.Message}")
        }
    resolveDroppedFilePath =
        // Renderer receives only the browser File, so it resolves the main-owned absolute path by key.
        fun key -> promise {
            match droppedFilePathsByKey.TryGetValue key with
            | true, absolutePath -> return Ok absolutePath
            | _ -> return Error(exn "Could not resolve the dropped image source path.")
        }
}
