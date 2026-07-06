module Main.IPC.FilePickerApi

open System
open Fable.Electron
open Fable.Electron.Main
open Swate.Components.Shared
open Swate.Electron.Shared.FileIOTypes
open Swate.Electron.Shared.IPCTypes

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

let api (event: IpcMainInvokeEvent) : IFilePickerApi = {
    pickExternalFilePaths =
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
}
