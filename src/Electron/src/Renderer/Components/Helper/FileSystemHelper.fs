module Renderer.Components.Helper.FileSystemHelper

open System
open Browser.Types
open Fable.Core
open Fable.Electron
open Swate.Components.Composite.MarkdownText.Plugins
open Swate.Components.Shared
open Swate.Electron.Shared.FileIOTypes

module ElectronMain = Fable.Electron.Main
module ElectronRenderer = Fable.Electron.Renderer

type ExternalAssetLink = {
    sourceAbsolutePath: string
    markdownRelativePath: string
}

type TargetAvailability =
    | Empty
    | Exists

let internal imageFileExtensions = [|
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

let internal fileExtensionsFromAcceptTypes (acceptTypes: string option) =
    let extensions =
        acceptTypes
        |> PluginTextInputHelpers.acceptTypeTokens
        |> List.toArray
        |> Array.collect (fun token ->
            if token = "image/*" then
                imageFileExtensions
            elif token.StartsWith(".") then
                let extension = token.Substring(1)

                if String.IsNullOrWhiteSpace extension then
                    [||]
                else
                    [| extension |]
            else
                [||]
        )
        |> Array.distinct

    if extensions.Length = 0 then None else Some extensions

let private normalizeExtension (extension: string) =
    extension
    |> Option.ofObj
    |> Option.map (fun value -> value.Trim())
    |> Option.bind (fun value ->
        let normalizedValue =
            if value.StartsWith(".") then
                value.Substring(1)
            else
                value

        if String.IsNullOrWhiteSpace normalizedValue then
            None
        else
            Some normalizedValue
    )

let private openDialogFiltersFromExtensions (filterExtensions: string[] option) =
    let normalizedExtensions =
        filterExtensions
        |> Option.defaultValue [||]
        |> Array.choose normalizeExtension
        |> Array.distinct

    if normalizedExtensions.Length = 0 then
        None
    else
        Some [| FileFilter("Supported files", normalizedExtensions) |]

let pickDirectory () : JS.Promise<Result<string, exn>> = promise {
    try
        let properties = [|
            ElectronMain.Enums.Dialog.ShowOpenDialog.Options.Properties.OpenDirectory
        |]

        let! result = ElectronMain.dialog.showOpenDialog (properties = properties)

        if result.canceled then
            return Error(exn "Cancelled")
        elif result.filePaths.Length <> 1 then
            return Error(exn "Not exactly one path")
        else
            return Ok(result.filePaths |> Array.exactlyOne)
    with e ->
        return Error(exn $"Could not pick directory: {e.Message}")
}

let pickAbsolutePaths (filterExtensions: string[] option) : JS.Promise<Result<string[], exn>> = promise {
    try
        let properties = [|
            ElectronMain.Enums.Dialog.ShowOpenDialog.Options.Properties.OpenFile
            ElectronMain.Enums.Dialog.ShowOpenDialog.Options.Properties.MultiSelections
        |]

        let filters = openDialogFiltersFromExtensions filterExtensions

        let! result =
            match filters with
            | Some filters -> ElectronMain.dialog.showOpenDialog (properties = properties, filters = filters)
            | None -> ElectronMain.dialog.showOpenDialog (properties = properties)

        if result.canceled then
            return Error(exn "Cancelled")
        else
            return Ok result.filePaths
    with e ->
        return Error(exn $"Could not pick files: {e.Message}")
}

let getPathForFile (file: File) : JS.Promise<Result<string, exn>> = promise {
    try
        return Ok(PathHelpers.normalizePath (ElectronRenderer.webUtils.getPathForFile file))
    with e ->
        return Error e
}

let private isAlreadyExistsError (error: exn) =
    error.Message.ToLowerInvariant().Contains("already exists")

let checkTargetAvailability (pathExists: string -> JS.Promise<Result<bool, exn>>) (targetPath: string) = promise {
    let normalizedTargetPath = PathHelpers.normalizePath targetPath

    match! pathExists normalizedTargetPath with
    | Ok true -> return Ok TargetAvailability.Exists
    | Ok false -> return Ok TargetAvailability.Empty
    | Error error -> return Error error
}

let ensureChildFolder
    (createFileSystemItem: CreateFileSystemItemRequest -> JS.Promise<Result<string, exn>>)
    (parentPath: string)
    (folderName: string)
    =
    promise {
        match!
            createFileSystemItem {
                parentPath = parentPath
                name = folderName
                kind = FileSystemItemKind.Folder
            }
        with
        | Ok _ -> return Ok()
        | Error error when isAlreadyExistsError error -> return Ok()
        | Error error -> return Error error
    }

let private combineRelativePath (parentPath: string) (childPath: string) =
    let normalizedParentPath = PathHelpers.normalizeCanonicalRelativePath parentPath
    let normalizedChildPath = PathHelpers.normalizeCanonicalRelativePath childPath

    if System.String.IsNullOrWhiteSpace normalizedParentPath then
        normalizedChildPath
    else
        $"{normalizedParentPath}/{normalizedChildPath}"

let private fileNameFromPath (path: string) =
    path
    |> Option.ofObj
    |> Option.map PathHelpers.getNameFromPath
    |> Option.defaultValue "image"
    |> fun name ->
        if String.IsNullOrWhiteSpace name then
            "image"
        else
            name.Trim()

let private assetMarkdownPath assetFolderName (fileName: string) =
    let assetFolder = PathHelpers.normalizeCanonicalRelativePath assetFolderName
    let imageFileName = fileNameFromPath fileName

    if String.IsNullOrWhiteSpace assetFolder then
        imageFileName
    else
        $"{assetFolder}/{imageFileName}"

let private tryGetBrowserFilePath (getPathForFile: File -> JS.Promise<Result<string, exn>>) (file: File) = promise {
    try
        match! getPathForFile file with
        | Ok path when not (String.IsNullOrWhiteSpace path) -> return Some(PathHelpers.normalizePath path)
        | Ok _
        | Error _ -> return None
    with _ ->
        return None
}

let private tryResolvePromptFileHostPath
    (tryResolveBrowserFilePath: File -> JS.Promise<string option>)
    (file: MarkdownPromptFile)
    =
    promise {
        match file.HostPath with
        | Some hostPath when not (String.IsNullOrWhiteSpace hostPath) -> return Some(PathHelpers.normalizePath hostPath)
        | _ ->
            match file.BrowserFile with
            | Some browserFile -> return! tryResolveBrowserFilePath browserFile
            | None -> return None
    }

let internal createAssetFilePickerAdapterWithBrowserFilePathResolverAsync
    (pickPaths: string[] option -> JS.Promise<Result<string[], exn>>)
    (assetFolderName: string)
    (addAsset: ExternalAssetLink -> unit)
    (tryResolveBrowserFilePath: File -> JS.Promise<string option>)
    =
    let toPromptFile absolutePath =
        let hostPath = PathHelpers.normalizePath absolutePath
        let fileName = fileNameFromPath hostPath

        {
            Name = fileName
            MimeType = Some "image/*"
            HostPath = Some hostPath
            BrowserFile = None
        }

    {
        PickFiles =
            (fun options -> promise {
                match! pickPaths (fileExtensionsFromAcceptTypes options.AcceptTypes) with
                | Ok paths -> return paths |> Array.map toPromptFile |> Array.toList
                | Error error when error.Message = "Cancelled" -> return []
                | Error error -> return raise error
            })
        ResolveMarkdownPath =
            (fun file -> promise {
                match! tryResolvePromptFileHostPath tryResolveBrowserFilePath file with
                | Some hostPath ->
                    let markdownPath = assetMarkdownPath assetFolderName file.Name

                    addAsset {
                        sourceAbsolutePath = hostPath
                        markdownRelativePath = markdownPath
                    }

                    return markdownPath
                | _ -> return raise (exn "Could not resolve the selected image path.")
            })
    }

let createAssetFilePickerAdapter
    (assetFolderName: string)
    (addAsset: ExternalAssetLink -> unit)
    =
    createAssetFilePickerAdapterWithBrowserFilePathResolverAsync
        pickAbsolutePaths
        assetFolderName
        addAsset
        (tryGetBrowserFilePath getPathForFile)

let writeFileWithOptionalExternalAssetLinks
    (writeFile: FileContentDTO -> JS.Promise<Result<unit, exn>>)
    (createFileSystemItem: CreateFileSystemItemRequest -> JS.Promise<Result<string, exn>>)
    (copyExternalFilesToArc: CopyExternalFileRequest[] -> JS.Promise<Result<string[], exn>>)
    (tryGetParentPath: string -> string option)
    (folderName: string)
    (request: FileContentDTO)
    (assets: ExternalAssetLink list)
    =
    let copyAssets parentPath = promise {
        match! ensureChildFolder createFileSystemItem parentPath folderName with
        | Error error -> return Error error
        | Ok() ->
            let copyRequests =
                assets
                |> List.map (fun asset -> {
                    sourceAbsolutePath = asset.sourceAbsolutePath
                    targetRelativePath = combineRelativePath parentPath asset.markdownRelativePath
                    overwrite = true
                })
                |> List.toArray

            match! copyExternalFilesToArc copyRequests with
            | Ok _ -> return Ok()
            | Error error -> return Error error
    }

    promise {
        match! writeFile request with
        | Error error -> return Error error
        | Ok() ->
            match assets, tryGetParentPath request.path with
            | [], _
            | _, None -> return Ok()
            | _, Some parentPath -> return! copyAssets parentPath
    }
