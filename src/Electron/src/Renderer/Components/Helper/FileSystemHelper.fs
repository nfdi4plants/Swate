module Renderer.Components.Helper.FileSystemHelper

open System
open System.Collections.Generic
open Fable.Core
open Swate.Components.Composite.MarkdownText.Plugins
open Swate.Components.Shared
open Swate.Electron.Shared.FileIOTypes

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

let private tryNormalizeNonEmptyPath (path: string) =
    path
    |> Option.ofObj
    |> Option.map PathHelpers.normalizePath
    |> Option.filter (String.IsNullOrWhiteSpace >> not)

let private assetMarkdownPath assetFolderName (fileName: string) =
    let assetFolder = PathHelpers.normalizeCanonicalRelativePath assetFolderName
    let imageFileName = fileNameFromPath fileName

    if String.IsNullOrWhiteSpace assetFolder then
        imageFileName
    else
        $"{assetFolder}/{imageFileName}"

let private tryGetStoredAbsolutePath (absolutePathsById: Dictionary<string, string>) (sourceId: string option) =
    match sourceId with
    | Some sourceId when not (String.IsNullOrWhiteSpace sourceId) ->
        match absolutePathsById.TryGetValue sourceId with
        | true, absolutePath -> tryNormalizeNonEmptyPath absolutePath
        | _ -> None
    | _ -> None

let private externalAssetLink assetFolderName (file: MarkdownPromptFile) sourceAbsolutePath = {
    sourceAbsolutePath = sourceAbsolutePath
    markdownRelativePath = assetMarkdownPath assetFolderName file.Name
}

let private copyRequestForAsset parentPath asset = {
    sourceAbsolutePath = asset.sourceAbsolutePath
    targetRelativePath = combineRelativePath parentPath asset.markdownRelativePath
    overwrite = true
}

let internal createAssetFilePickerAdapterWithPathPicker
    (pickPaths: PickExternalFilePathsRequest -> JS.Promise<Result<string[], exn>>)
    (assetFolderName: string)
    (addAsset: ExternalAssetLink -> unit)
    =
    let absolutePathsById = Dictionary<string, string>()

    let toPromptFile absolutePath =
        let sourceAbsolutePath = PathHelpers.normalizePath absolutePath
        let fileName = fileNameFromPath sourceAbsolutePath
        let sourceId = Guid.NewGuid().ToString("N")

        absolutePathsById.[sourceId] <- sourceAbsolutePath

        {
            Name = fileName
            MimeType = Some "image/*"
            SourceId = Some sourceId
            BrowserFile = None
        }

    {
        PickFiles =
            (fun options -> promise {
                match!
                    pickPaths {
                        filterExtensions = fileExtensionsFromAcceptTypes options.AcceptTypes
                        allowMultiple = options.AllowMultiple
                    }
                with
                | Ok paths -> return paths |> Array.map toPromptFile |> Array.toList
                | Error error when error.Message = "Cancelled" -> return []
                | Error error -> return raise error
            })
        ResolveMarkdownPath =
            (fun file -> promise {
                match tryGetStoredAbsolutePath absolutePathsById file.SourceId with
                | Some sourceAbsolutePath ->
                    let asset = externalAssetLink assetFolderName file sourceAbsolutePath
                    addAsset asset
                    return asset.markdownRelativePath
                | _ -> return raise (exn "Could not resolve the selected image source path.")
            })
    }

let createAssetFilePickerAdapter
    (pickExternalFilePaths: PickExternalFilePathsRequest -> JS.Promise<Result<string[], exn>>)
    (assetFolderName: string)
    (addAsset: ExternalAssetLink -> unit)
    =
    createAssetFilePickerAdapterWithPathPicker pickExternalFilePaths assetFolderName addAsset

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
                assets |> List.map (copyRequestForAsset parentPath) |> List.toArray

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
