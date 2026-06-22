module ElectronRenderer.FileSystemHelperTests

open Browser.Types
open Renderer.Components.Helper.FileSystemHelper
open Swate.Components.Composite.MarkdownText.Plugins
open Swate.Electron.Shared.FileIOHelper
open Swate.Electron.Shared.FileIOTypes
open Vitest

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
            "createAssetFilePickerAdapter resolves selected images and tracks copy source",
            fun () -> promise {
                let mutable pendingAssets = []

                let adapter =
                    createAssetFilePickerAdapter
                        (fun () -> promise { return Ok [| "C:/outside/diagram.png" |] })
                        "assets"
                        (fun asset -> pendingAssets <- pendingAssets @ [ asset ])

                let! pickedFiles = adapter.PickFiles()
                Vitest.expect(pickedFiles.Length).toBe (1)
                Vitest.expect(pickedFiles.[0].Name).toBe ("diagram.png")
                Vitest.expect(pickedFiles.[0].MimeType).toEqual (Some "image/*")
                Vitest.expect(pickedFiles.[0].HostPath).toEqual (Some "C:/outside/diagram.png")

                let! markdownPath = adapter.ResolveMarkdownPath pickedFiles.[0]
                Vitest.expect(markdownPath).toBe ("assets/diagram.png")

                Vitest.expect(pendingAssets.Length).toBe (1)
                Vitest.expect(pendingAssets.[0].sourceAbsolutePath).toBe ("C:/outside/diagram.png")
                Vitest.expect(pendingAssets.[0].markdownRelativePath).toBe ("assets/diagram.png")
            }
        )

        Vitest.test (
            "createAssetFilePickerAdapter resolves multiple selected images independently",
            fun () -> promise {
                let mutable pendingAssets = []

                let adapter =
                    createAssetFilePickerAdapter
                        (fun () -> promise { return Ok [| "C:/outside/diagram-a.png"; "D:/camera/diagram-b.jpg" |] })
                        "assets"
                        (fun asset -> pendingAssets <- pendingAssets @ [ asset ])

                let! pickedFiles = adapter.PickFiles()
                Vitest.expect(pickedFiles.Length).toBe (2)
                Vitest.expect(pickedFiles.[0].Name).toBe ("diagram-a.png")
                Vitest.expect(pickedFiles.[1].Name).toBe ("diagram-b.jpg")
                Vitest.expect(pickedFiles.[0].HostPath).toEqual (Some "C:/outside/diagram-a.png")
                Vitest.expect(pickedFiles.[1].HostPath).toEqual (Some "D:/camera/diagram-b.jpg")

                let! firstMarkdownPath = adapter.ResolveMarkdownPath pickedFiles.[0]
                let! secondMarkdownPath = adapter.ResolveMarkdownPath pickedFiles.[1]

                Vitest.expect(firstMarkdownPath).toBe ("assets/diagram-a.png")
                Vitest.expect(secondMarkdownPath).toBe ("assets/diagram-b.jpg")

                Vitest.expect(pendingAssets.Length).toBe (2)
                Vitest.expect(pendingAssets.[0].sourceAbsolutePath).toBe ("C:/outside/diagram-a.png")
                Vitest.expect(pendingAssets.[0].markdownRelativePath).toBe ("assets/diagram-a.png")
                Vitest.expect(pendingAssets.[1].sourceAbsolutePath).toBe ("D:/camera/diagram-b.jpg")
                Vitest.expect(pendingAssets.[1].markdownRelativePath).toBe ("assets/diagram-b.jpg")
            }
        )

        Vitest.test (
            "createAssetFilePickerAdapter resolves browser files through the injected host path resolver",
            fun () -> promise {
                let mutable pendingAssets = []

                let adapter =
                    createAssetFilePickerAdapterWithBrowserFilePathResolver
                        (fun () -> promise { return Ok [||] })
                        "assets"
                        (fun asset -> pendingAssets <- pendingAssets @ [ asset ])
                        (fun _ -> Some "C:/dropped/dropped-image.png")

                let promptFile: MarkdownPromptFile = {
                    Name = "dropped-image.png"
                    MimeType = Some "image/png"
                    HostPath = None
                    BrowserFile = Some(unbox<File> (obj ()))
                }

                let! markdownPath = adapter.ResolveMarkdownPath promptFile

                Vitest.expect(markdownPath).toBe ("assets/dropped-image.png")
                Vitest.expect(pendingAssets.Length).toBe (1)
                Vitest.expect(pendingAssets.[0].sourceAbsolutePath).toBe ("C:/dropped/dropped-image.png")
                Vitest.expect(pendingAssets.[0].markdownRelativePath).toBe ("assets/dropped-image.png")
            }
        )
)
