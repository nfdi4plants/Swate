module ElectronCore.FileTreeCreatorTests

open System
open Fable.Core
open Fable.Core.JsInterop
open Main
open Main.Bindings.Path
open Main.VersionControl
open Swate.Components.Composite.Authentication.Types
open Swate.Electron.Shared.FileIOTypes
open Swate.Electron.Shared.VersionControlTypes
open VersionControlService.Abstractions
open Vitest

module FileTreeCreator = Main.FileTreeCreator

let private fsPromisesDynamic: obj = importAll "fs/promises"
let private osDynamic: obj = importAll "os"
let private childProcessDynamic: obj = importAll "node:child_process"

let private fileTreeCreatorTestOptions = TestOptions(timeout = 20000)

let private normalizeSlashes (path: string) = path.Replace("\\", "/")

type private TempRepositoryContext = { RootPath: string; RepoPath: string }

let private createTempDirectoryAsync () : Fable.Core.JS.Promise<string> =
    let prefix =
        join [|
            osDynamic?tmpdir () |> unbox<string>
            "swate-electron-file-tree-"
        |]

    fsPromisesDynamic?mkdtemp (prefix) |> unbox<Fable.Core.JS.Promise<string>>

let private removeDirectoryAsync (path: string) : Fable.Core.JS.Promise<unit> = promise {
    let! _ =
        fsPromisesDynamic?rm (path, createObj [ "recursive" ==> true; "force" ==> true ])
        |> unbox<Fable.Core.JS.Promise<obj>>

    return ()
}

let private writeUtf8FileAsync (path: string) (content: string) : Fable.Core.JS.Promise<unit> = promise {
    let! _ =
        fsPromisesDynamic?writeFile (path, content, "utf8")
        |> unbox<Fable.Core.JS.Promise<obj>>

    return ()
}

let private runGitAsync (repoPath: string) (args: string[]) : Fable.Core.JS.Promise<string> = promise {
    let! output =
        Fable.Core.JS.Constructors.Promise.Create(fun resolve reject ->
            childProcessDynamic?execFile (
                "git",
                args,
                createObj [
                    "cwd" ==> repoPath
                    "encoding" ==> "utf8"
                    "shell" ==> false
                ],
                fun (error: obj) (stdout: obj) (stderr: obj) ->
                    if error |> Option.ofObj |> Option.isSome then
                        let stderrText =
                            stderr |> Option.ofObj |> Option.map string |> Option.defaultValue String.Empty

                        let argsText = String.concat " " args
                        reject (exn $"git {argsText} failed: {stderrText}")
                    else
                        stdout
                        |> Option.ofObj
                        |> Option.map string
                        |> Option.defaultValue String.Empty
                        |> resolve
            )
            |> ignore
        )

    return output
}

let private expectHexObjectId (largeObject: LargeObjectState) =
    Vitest.expect(largeObject.objectId.IsSome).toBe (true)

    let objectId = largeObject.objectId |> Option.get
    Vitest.expect(objectId.Length).toBe (64)
    Vitest.expect(System.Text.RegularExpressions.Regex.IsMatch(objectId, "^[0-9a-fA-F]{64}$")).toBe (true)

let private noAccounts: DataHubStrategies.DataHubAccountSource = {
    GetState = fun () -> AuthStateDto.Empty
    TryGetTokenForAccount = fun _ -> None
    TryGetTokenForHost = fun _ -> None
}

let private memoryBindings () =
    let mutable content: string option = None

    WorkspaceBindingStore.create
        CaseInsensitive
        (fun () -> content)
        (fun next ->
            content <- Some next
            Ok()
        )

let private createRuntime (settingsRoot: string) : VersionControlRuntime.VersionControlRuntime = {
    Catalog =
        ProviderComposition.createCatalog [
            ProviderComposition.createGitFactory noAccounts
            ProviderComposition.createLakeFsFactory
                (ProviderComposition.lakeFsOptions settingsRoot CaseInsensitive)
                VersionControlService.LakeFs.LakeFsCredentials.unconfigured
        ]
    Bindings = memoryBindings ()
    PathCaseSensitivity = CaseInsensitive
}

let private withTempRepository
    (testBody: TempRepositoryContext -> Fable.Core.JS.Promise<unit>)
    : Fable.Core.JS.Promise<unit> =
    promise {
        let! rootPath = createTempDirectoryAsync ()

        try
            let repoPath = join [| rootPath; "repo" |]
            let runtime = createRuntime rootPath
            let gitFactory = ProviderComposition.createGitFactory noAccounts

            let! initialized =
                gitFactory.Initialize
                    {
                        TargetPath = repoPath
                        Location = None
                    }
                    (OperationContext.detached "file-tree-init")
                |> Async.StartAsPromise

            let binding =
                match initialized with
                | Succeeded outcome -> outcome.Value
                | PartiallySucceeded(_, failure) ->
                    failwith $"git init partially succeeded ({failure.Code}): {failure.Message}"
                | Failed failure -> failwith $"git init failed ({failure.Code}): {failure.Message}"

            let host = WorkspaceSessionHost.WorkspaceSessionHost(runtime)
            WorkspaceSessionHost.initialize host
            VersionControlRuntime.initialize runtime

            try
                do!
                    testBody {
                        RootPath = rootPath
                        RepoPath = binding.WorkspaceRoot
                    }

                do! host.CloseAll() |> Async.StartAsPromise
                do! removeDirectoryAsync rootPath
            with error ->
                do! host.CloseAll() |> Async.StartAsPromise
                do! removeDirectoryAsync rootPath
                return raise error
        with error ->
            do! removeDirectoryAsync rootPath
            return raise error
    }

Vitest.describe (
    "FileTreeCreator LFS metadata",
    fun () ->
        Vitest.test (
            "getFileEntries annotates materialized and pointer large objects",
            fileTreeCreatorTestOptions,
            fun () -> promise {
                do!
                    withTempRepository (fun context -> promise {
                        let pointerFilePath = join [| context.RepoPath; "pointer.psd" |]
                        let downloadedFilePath = join [| context.RepoPath; "downloaded.psd" |]

                        let! _ = runGitAsync context.RepoPath [| "lfs"; "install"; "--local" |]
                        let! _ = runGitAsync context.RepoPath [| "lfs"; "track"; "*.psd" |]
                        do! writeUtf8FileAsync pointerFilePath "Pointer-like content.\n"
                        do! writeUtf8FileAsync downloadedFilePath "Hydrated-like content.\n"

                        let! _ =
                            runGitAsync context.RepoPath [|
                                "add"
                                ".gitattributes"
                                "pointer.psd"
                                "downloaded.psd"
                            |]

                        ()

                        let! pointerContents =
                            runGitAsync context.RepoPath [| "lfs"; "pointer"; "--file"; pointerFilePath |]

                        do! writeUtf8FileAsync pointerFilePath pointerContents

                        let! entries = FileTreeCreator.getFileEntries context.RepoPath

                        let pointerEntry =
                            entries
                            |> Microsoft.FSharp.Collections.Array.find (fun entry ->
                                normalizeSlashes entry.path = normalizeSlashes pointerFilePath
                            )

                        let downloadedEntry =
                            entries
                            |> Microsoft.FSharp.Collections.Array.find (fun entry ->
                                normalizeSlashes entry.path = normalizeSlashes downloadedFilePath
                            )

                        Vitest.expect(pointerEntry.largeObject.IsSome).toBe (true)
                        Vitest.expect(downloadedEntry.largeObject.IsSome).toBe (true)

                        let pointerLargeObject = pointerEntry.largeObject |> Option.get
                        let downloadedLargeObject = downloadedEntry.largeObject |> Option.get

                        Vitest.expect(pointerLargeObject.path).toBe ("pointer.psd")
                        Vitest.expect(pointerLargeObject.sizeBytes |> Option.get).toBeGreaterThan (0)
                        Vitest.expect(pointerLargeObject.isMaterialized).toBe (false)
                        Vitest.expect(pointerLargeObject.isLocallyAvailable).toBe (true)
                        expectHexObjectId pointerLargeObject

                        Vitest.expect(downloadedLargeObject.path).toBe ("downloaded.psd")
                        Vitest.expect(downloadedLargeObject.sizeBytes |> Option.get).toBeGreaterThan (0)
                        Vitest.expect(downloadedLargeObject.isMaterialized).toBe (true)
                        Vitest.expect(downloadedLargeObject.isLocallyAvailable).toBe (true)
                        expectHexObjectId downloadedLargeObject
                    })
            }
        )

        Vitest.test (
            "getFileEntryWithLfsMetadata enriches a single staged LFS file",
            fileTreeCreatorTestOptions,
            fun () -> promise {
                do!
                    withTempRepository (fun context -> promise {
                        let pointerFilePath = join [| context.RepoPath; "single-pointer.psd" |]

                        let! _ = runGitAsync context.RepoPath [| "lfs"; "install"; "--local" |]
                        let! _ = runGitAsync context.RepoPath [| "lfs"; "track"; "*.psd" |]
                        do! writeUtf8FileAsync pointerFilePath "Single tracked content.\n"
                        let! _ = runGitAsync context.RepoPath [| "add"; ".gitattributes"; "single-pointer.psd" |]
                        ()

                        let! enrichedEntry =
                            FileTreeCreator.getFileEntryWithLfsMetadata context.RepoPath pointerFilePath

                        Vitest.expect(enrichedEntry.largeObject.IsSome).toBe (true)
                        let largeObject = enrichedEntry.largeObject |> Option.get

                        Vitest.expect(largeObject.path).toBe ("single-pointer.psd")
                        Vitest.expect(largeObject.sizeBytes |> Option.get).toBeGreaterThan (0)
                        Vitest.expect(largeObject.isMaterialized).toBe (true)
                        Vitest.expect(largeObject.isLocallyAvailable).toBe (true)
                        expectHexObjectId largeObject
                    })
            }
        )

        Vitest.test (
            "files absent from large-object listing keep metadata None",
            fileTreeCreatorTestOptions,
            fun () -> promise {
                do!
                    withTempRepository (fun context -> promise {
                        let untrackedLfsPath = join [| context.RepoPath; "untracked.psd" |]

                        let! _ = runGitAsync context.RepoPath [| "lfs"; "install"; "--local" |]
                        let! _ = runGitAsync context.RepoPath [| "lfs"; "track"; "*.psd" |]
                        do! writeUtf8FileAsync untrackedLfsPath "Untracked file content.\n"
                        let! _ = runGitAsync context.RepoPath [| "add"; ".gitattributes" |]
                        ()

                        let! enrichedEntry =
                            FileTreeCreator.getFileEntryWithLfsMetadata context.RepoPath untrackedLfsPath

                        Vitest.expect(enrichedEntry.largeObject).toEqual (None)
                    })
            }
        )

        Vitest.test (
            "no large objects keeps entries without metadata",
            fileTreeCreatorTestOptions,
            fun () -> promise {
                do!
                    withTempRepository (fun context -> promise {
                        let plainFilePath = join [| context.RepoPath; "plain.txt" |]
                        do! writeUtf8FileAsync plainFilePath "Plain text.\n"

                        let! entries = FileTreeCreator.getFileEntries context.RepoPath

                        let plainEntry =
                            entries
                            |> Array.find (fun entry -> normalizeSlashes entry.path = normalizeSlashes plainFilePath)

                        Vitest.expect(plainEntry.largeObject).toEqual (None)
                    })
            }
        )
)
