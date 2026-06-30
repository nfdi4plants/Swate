module ElectronRenderer.FileSystemHelperTests

open System
open Browser.Types
open Renderer.Components.Helper.FileSystemHelper
open Swate.Components.Composite.MarkdownText.Plugins
open Swate.Electron.Shared.FileIOHelper
open Swate.Electron.Shared.FileIOTypes
open Vitest

let private expectSourceId (file: MarkdownPromptFile) =
    match file.SourceId with
    | Some sourceId ->
        Vitest.expect(String.IsNullOrWhiteSpace sourceId).toBe (false)
        sourceId
    | None -> failwith $"Expected '{file.Name}' to have a source id."

let private noBrowserFileAbsolutePath (_: File) = promise { return None }

let private createAssetAdapter pickPaths resolveBrowserFileAbsolutePath =
    let pendingAssets = ResizeArray<ExternalAssetLink>()

    let adapter =
        createAssetFilePickerAdapterWithAbsolutePathResolverAsync
            pickPaths
            "assets"
            (fun asset -> pendingAssets.Add asset)
            resolveBrowserFileAbsolutePath

    adapter, pendingAssets

let private expectAsset (asset: ExternalAssetLink) sourceAbsolutePath markdownRelativePath =
    Vitest.expect(asset.sourceAbsolutePath).toBe (sourceAbsolutePath)
    Vitest.expect(asset.markdownRelativePath).toBe (markdownRelativePath)

Vitest.describe (
    "FileSystemHelper",
    fun () ->
        Vitest.test (
            "checkTargetAvailability normalizes the target and returns Empty when it does not exist",
            fun () -> promise {
                let requestedPaths = ResizeArray<string>()

                let pathExists path = promise {
                    requestedPaths.Add path
                    return Ok false
                }

                let! result = checkTargetAvailability pathExists "notes\\2026-06-15\\draft.md/"

                match result with
                | Ok TargetAvailability.Empty -> ()
                | _ -> failwith $"Expected empty target availability, got {result}."

                Vitest.expect(requestedPaths.ToArray()).toEqual ([| "notes/2026-06-15/draft.md" |])
            }
        )

        Vitest.test (
            "checkTargetAvailability returns Exists when the target exists",
            fun () -> promise {
                let pathExists _ = promise { return Ok true }

                let! result = checkTargetAvailability pathExists "notes/2026-06-15/draft.md"

                match result with
                | Ok TargetAvailability.Exists -> ()
                | _ -> failwith $"Expected existing target availability, got {result}."
            }
        )

        Vitest.test (
            "checkTargetAvailability propagates path check errors",
            fun () -> promise {
                let expectedError = exn "path check failed"
                let pathExists _ = promise { return Error expectedError }

                let! result = checkTargetAvailability pathExists "notes/2026-06-15/draft.md"

                match result with
                | Error actualError -> Vitest.expect(actualError.Message).toBe ("path check failed")
                | Ok availability -> failwith $"Expected path check failure, got {availability}."
            }
        )

        Vitest.test (
            "writeFileWithOptionalExternalAssetLinks does not create asset folder for plain note writes",
            fun () -> promise {
                let createdFolders = ResizeArray<CreateFileSystemItemRequest>()
                let copyRequests = ResizeArray<CopyExternalFileRequest[]>()

                let request =
                    FileContentDTO.create FileContentType.Markdown "plain note" "notes/2026-06-15/plain/plain.md"

                let writeFile _ = promise { return Ok() }

                let createFileSystemItem request = promise {
                    createdFolders.Add request
                    return Ok $"{request.parentPath}/{request.name}"
                }

                let copyExternalFilesToArc requests = promise {
                    copyRequests.Add requests
                    return Ok [||]
                }

                match!
                    writeFileWithOptionalExternalAssetLinks
                        writeFile
                        createFileSystemItem
                        copyExternalFilesToArc
                        (fun path -> Some(path.Substring(0, path.LastIndexOf('/'))))
                        "assets"
                        request
                        []
                with
                | Error error -> failwith error.Message
                | Ok() ->
                    Vitest.expect(createdFolders.Count).toBe (0)
                    Vitest.expect(copyRequests.Count).toBe (0)
            }
        )

        Vitest.test (
            "writeFileWithOptionalExternalAssetLinks creates asset folder and copies referenced images",
            fun () -> promise {
                let createdFolders = ResizeArray<CreateFileSystemItemRequest>()
                let copyRequests = ResizeArray<CopyExternalFileRequest[]>()

                let request =
                    FileContentDTO.create
                        FileContentType.Markdown
                        "![diagram](assets/diagram.png)"
                        "notes/2026-06-15/plain/plain.md"

                let assets = [
                    {
                        sourceAbsolutePath = "C:/outside/diagram.png"
                        markdownRelativePath = "assets/diagram.png"
                    }
                ]

                let writeFile _ = promise { return Ok() }

                let createFileSystemItem request = promise {
                    createdFolders.Add request
                    return Ok $"{request.parentPath}/{request.name}"
                }

                let copyExternalFilesToArc requests = promise {
                    copyRequests.Add requests
                    return Ok(requests |> Array.map _.targetRelativePath)
                }

                match!
                    writeFileWithOptionalExternalAssetLinks
                        writeFile
                        createFileSystemItem
                        copyExternalFilesToArc
                        (fun path -> Some(path.Substring(0, path.LastIndexOf('/'))))
                        "assets"
                        request
                        assets
                with
                | Error error -> failwith error.Message
                | Ok() ->
                    Vitest.expect(createdFolders.Count).toBe (1)
                    Vitest.expect(createdFolders.[0].parentPath).toBe ("notes/2026-06-15/plain")
                    Vitest.expect(createdFolders.[0].name).toBe ("assets")
                    Vitest.expect(createdFolders.[0].kind).toEqual (FileSystemItemKind.Folder)

                    Vitest.expect(copyRequests.Count).toBe (1)
                    Vitest.expect(copyRequests.[0].Length).toBe (1)
                    Vitest.expect(copyRequests.[0].[0].sourceAbsolutePath).toBe ("C:/outside/diagram.png")

                    Vitest
                        .expect(copyRequests.[0].[0].targetRelativePath)
                        .toBe ("notes/2026-06-15/plain/assets/diagram.png")

                    Vitest.expect(copyRequests.[0].[0].overwrite).toBe (true)
            }
        )

        Vitest.test (
            "fileExtensionsFromAcceptTypes maps image wildcard accepts to image picker extensions",
            fun () ->
                match fileExtensionsFromAcceptTypes (Some "image/*") with
                | Some extensions -> Vitest.expect(extensions).toEqual (imageFileExtensions)
                | None -> failwith "Expected image wildcard accept types to produce image extensions."
        )

        Vitest.test (
            "fileExtensionsFromAcceptTypes maps explicit extension accepts and ignores mime-only tokens",
            fun () ->
                match fileExtensionsFromAcceptTypes (Some ".png, image/jpeg, .SVG") with
                | Some extensions -> Vitest.expect(extensions).toEqual ([| "png"; "svg" |])
                | None -> failwith "Expected explicit accept types to produce picker extensions."
        )

        Vitest.test (
            "fileExtensionsFromAcceptTypes returns no picker filter for empty accepts",
            fun () -> Vitest.expect(fileExtensionsFromAcceptTypes None).toEqual (None)
        )

        Vitest.test (
            "createAssetFilePickerAdapter resolves selected images and tracks copy source",
            fun () -> promise {
                let mutable requestedExtensions: string[] option = None

                let adapter, pendingAssets =
                    createAssetAdapter
                        (fun extensions ->
                            requestedExtensions <- extensions
                            promise { return Ok [| "C:/outside/diagram.png" |] })
                        noBrowserFileAbsolutePath

                let! pickedFiles = adapter.PickFiles { AcceptTypes = Some "image/*" }

                match requestedExtensions with
                | Some extensions -> Vitest.expect(extensions).toEqual (imageFileExtensions)
                | None -> failwith "Expected image extensions to be passed to the absolute path picker."

                Vitest.expect(pickedFiles.Length).toBe (1)
                Vitest.expect(pickedFiles.[0].Name).toBe ("diagram.png")
                Vitest.expect(pickedFiles.[0].MimeType).toEqual (Some "image/*")
                expectSourceId pickedFiles.[0] |> ignore

                let! markdownPath = adapter.ResolveMarkdownPath pickedFiles.[0]
                Vitest.expect(markdownPath).toBe ("assets/diagram.png")

                Vitest.expect(pendingAssets.Count).toBe (1)
                expectAsset pendingAssets.[0] "C:/outside/diagram.png" "assets/diagram.png"
            }
        )

        Vitest.test (
            "createAssetFilePickerAdapter resolves multiple selected images independently",
            fun () -> promise {
                let adapter, pendingAssets =
                    createAssetAdapter
                        (fun _ -> promise { return Ok [| "C:/outside/diagram-a.png"; "D:/camera/diagram-b.jpg" |] })
                        noBrowserFileAbsolutePath

                let! pickedFiles = adapter.PickFiles { AcceptTypes = Some "image/*" }
                Vitest.expect(pickedFiles.Length).toBe (2)
                Vitest.expect(pickedFiles.[0].Name).toBe ("diagram-a.png")
                Vitest.expect(pickedFiles.[1].Name).toBe ("diagram-b.jpg")

                let firstSourceId = expectSourceId pickedFiles.[0]
                let secondSourceId = expectSourceId pickedFiles.[1]
                Vitest.expect(firstSourceId).not.toBe (secondSourceId)

                let! firstMarkdownPath = adapter.ResolveMarkdownPath pickedFiles.[0]
                let! secondMarkdownPath = adapter.ResolveMarkdownPath pickedFiles.[1]

                Vitest.expect(firstMarkdownPath).toBe ("assets/diagram-a.png")
                Vitest.expect(secondMarkdownPath).toBe ("assets/diagram-b.jpg")

                Vitest.expect(pendingAssets.Count).toBe (2)
                expectAsset pendingAssets.[0] "C:/outside/diagram-a.png" "assets/diagram-a.png"
                expectAsset pendingAssets.[1] "D:/camera/diagram-b.jpg" "assets/diagram-b.jpg"
            }
        )

        Vitest.test (
            "createAssetFilePickerAdapter resolves dropped browser files through the absolute path resolver",
            fun () -> promise {
                let adapter, pendingAssets =
                    createAssetAdapter
                        (fun _ -> promise { return Ok [||] })
                        (fun _ -> promise { return Some "C:/dropped/dropped-image.png" })

                let promptFile: MarkdownPromptFile = {
                    Name = "dropped-image.png"
                    MimeType = Some "image/png"
                    SourceId = None
                    BrowserFile = Some(unbox<File> (obj ()))
                }

                let! markdownPath = adapter.ResolveMarkdownPath promptFile

                Vitest.expect(markdownPath).toBe ("assets/dropped-image.png")
                Vitest.expect(pendingAssets.Count).toBe (1)
                expectAsset pendingAssets.[0] "C:/dropped/dropped-image.png" "assets/dropped-image.png"
            }
        )

        Vitest.test (
            "createAssetFilePickerAdapter rejects files without a source",
            fun () -> promise {
                let adapter, pendingAssets =
                    createAssetAdapter
                        (fun _ -> promise { return Ok [||] })
                        noBrowserFileAbsolutePath

                let promptFile: MarkdownPromptFile = {
                    Name = "dropped-image.png"
                    MimeType = Some "image/png"
                    SourceId = None
                    BrowserFile = None
                }

                try
                    let! _ = adapter.ResolveMarkdownPath promptFile
                    failwith "Expected markdown path resolution to fail."
                with error ->
                    Vitest.expect(error.Message).toBe ("Could not resolve the selected image source path.")
                    Vitest.expect(pendingAssets.Count).toBe (0)
            }
        )
)
