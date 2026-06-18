module Renderer.Components.Helper.FileSystemHelper

open System
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

let createAssetFilePickerAdapter
    (pickImagePaths: unit -> JS.Promise<Result<string[], exn>>)
    (assetFolderName: string)
    (addAsset: ExternalAssetLink -> unit)
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
            (fun () -> promise {
                match! pickImagePaths () with
                | Ok paths -> return paths |> Array.map toPromptFile |> Array.toList
                | Error error when error.Message = "Cancelled" -> return []
                | Error error -> return raise error
            })
        ResolveMarkdownPath =
            (fun file -> promise {
                match file.HostPath with
                | Some hostPath when not (String.IsNullOrWhiteSpace hostPath) ->
                    let markdownPath = assetMarkdownPath assetFolderName file.Name

                    addAsset {
                        sourceAbsolutePath = PathHelpers.normalizePath hostPath
                        markdownRelativePath = markdownPath
                    }

                    return markdownPath
                | _ -> return raise (exn "Could not resolve the selected image path.")
            })
    }

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
