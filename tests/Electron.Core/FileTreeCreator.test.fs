module ElectronCore.FileTreeCreatorTests

open System
open System.Collections.Generic
open Fable.Core
open Fable.Core.JsInterop
open Main
open Main.Bindings.Path
open Main.VersionControl
open Swate.Components.Shared
open Swate.Components.Composite.Authentication.Types
open Swate.Electron.Shared.FileIOTypes
open Swate.Electron.Shared.VersionControlTypes
open VersionControlService.Abstractions
open Vitest
open ElectronCore.TestHelpers

module FileTreeCreator = Main.FileTreeCreator

let private fsPromisesDynamic: obj = importAll "fs/promises"
let private osDynamic: obj = importAll "os"
let private childProcessDynamic: obj = importAll "node:child_process"

let private fileTreeCreatorTestOptions = TestOptions(timeout = 20000)

let private normalizeSlashes (path: string) = path.Replace("\\", "/")

let private createFileEntry name path =
    ({
        name = name
        isDirectory = false
        path = path
        largeObject = None
    }
    : FileEntry)

let private createObjectState path =
    ({
        Path = path
        IsMaterialized = false
        IsLocallyAvailable = true
        SizeBytes = Some 10.0
        ObjectId = Some(String.replicate 64 "a")
    }
    : ObjectStateDto)

let private enrichFileEntries (objects: (string * ObjectStateDto) list) (entries: FileEntry[]) =
    FileTreeCreator.withFileEntriesLfsMetadata "/repo" (Map.ofList objects) entries

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

let private createDirectoryAsync (path: string) : Fable.Core.JS.Promise<unit> = promise {
    let! _ =
        fsPromisesDynamic?mkdir (path, createObj [ "recursive" ==> true ])
        |> unbox<Fable.Core.JS.Promise<obj>>

    return ()
}

let private directoryEntry name path =
    FileEntry.create (name, path, true, None)

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

let private expectHexObjectId (largeObject: ObjectStateDto) =
    Vitest.expect(largeObject.ObjectId.IsSome).toBe (true)

    let objectId = largeObject.ObjectId |> Option.get
    Vitest.expect(objectId.Length).toBe (64)
    Vitest.expect(System.Text.RegularExpressions.Regex.IsMatch(objectId, "^[0-9a-fA-F]{64}$")).toBe (true)

let private withTempRepository
    (testBody: TempRepositoryContext -> Fable.Core.JS.Promise<unit>)
    : Fable.Core.JS.Promise<unit> =
    promise {
        let! rootPath = createTempDirectoryAsync ()

        try
            let repoPath = join [| rootPath; "repo" |]

            let runtime =
                createRuntime rootPath VersionControlService.LakeFs.LakeFsCredentials.unconfigured (memoryBindings ())

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

                        let! entries = FileTreeCreator.getFileEntries context.RepoPath true

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

                        Vitest.expect(pointerLargeObject.Path).toBe ("pointer.psd")
                        Vitest.expect(pointerLargeObject.SizeBytes |> Option.get).toBeGreaterThan (0)
                        Vitest.expect(pointerLargeObject.IsMaterialized).toBe (false)
                        Vitest.expect(pointerLargeObject.IsLocallyAvailable).toBe (true)
                        expectHexObjectId pointerLargeObject

                        Vitest.expect(downloadedLargeObject.Path).toBe ("downloaded.psd")
                        Vitest.expect(downloadedLargeObject.SizeBytes |> Option.get).toBeGreaterThan (0)
                        Vitest.expect(downloadedLargeObject.IsMaterialized).toBe (true)
                        Vitest.expect(downloadedLargeObject.IsLocallyAvailable).toBe (true)
                        expectHexObjectId downloadedLargeObject
                    })
            }
        )

        Vitest.test (
            "exact LFS metadata path match remains available",
            fun () ->
                let largeObject = createObjectState "data.csv"

                let entries =
                    enrichFileEntries [ "data.csv", largeObject ] [| createFileEntry "data.csv" "/repo/data.csv" |]

                Vitest.expect(entries.[0].largeObject).toEqual (Some largeObject)
        )

        Vitest.test (
            "case-only disk path difference finds LFS metadata",
            fun () ->
                let largeObject = createObjectState "data.csv"

                let entries =
                    enrichFileEntries [ "data.csv", largeObject ] [| createFileEntry "Data.csv" "/repo/Data.csv" |]

                Vitest.expect(entries.[0].largeObject).toEqual (Some largeObject)
        )

        Vitest.test (
            "NFD disk path finds NFC Git metadata",
            fun () ->
                let gitName = "Messung_\u00E4.csv"
                let diskName = "Messung_a\u0308.csv"
                let largeObject = createObjectState gitName

                let entries =
                    enrichFileEntries [ gitName, largeObject ] [| createFileEntry diskName $"/repo/{diskName}" |]

                Vitest.expect(entries.[0].largeObject).toEqual (Some largeObject)
        )

        Vitest.test (
            "ambiguous case keys only match exact Git paths",
            fun () ->
                let lowerCaseObject = createObjectState "data.csv"
                let upperCaseObject = createObjectState "Data.csv"

                let entries =
                    enrichFileEntries [ "data.csv", lowerCaseObject; "Data.csv", upperCaseObject ] [|
                        createFileEntry "data.csv" "/repo/data.csv"
                        createFileEntry "Data.csv" "/repo/Data.csv"
                        createFileEntry "DaTa.csv" "/repo/DaTa.csv"
                    |]

                Vitest.expect(entries.[0].largeObject).toEqual (Some lowerCaseObject)
                Vitest.expect(entries.[1].largeObject).toEqual (Some upperCaseObject)
                Vitest.expect(entries.[2].largeObject).toEqual (None)
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

                        Vitest.expect(largeObject.Path).toBe ("single-pointer.psd")
                        Vitest.expect(largeObject.SizeBytes |> Option.get).toBeGreaterThan (0)
                        Vitest.expect(largeObject.IsMaterialized).toBe (true)
                        Vitest.expect(largeObject.IsLocallyAvailable).toBe (true)
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

                        let! entries = FileTreeCreator.getFileEntries context.RepoPath true

                        let plainEntry =
                            entries
                            |> Array.find (fun entry -> normalizeSlashes entry.path = normalizeSlashes plainFilePath)

                        Vitest.expect(plainEntry.largeObject).toEqual (None)
                    })
            }
        )
)

Vitest.describe (
    "FileTreeCreator shallow directory reconciliation",
    fun () ->
        Vitest.test (
            "discovers one level at a time, preserves surviving descendants, and skips unchanged snapshots",
            fun () -> promise {
                let! arcPath = createTempDirectoryAsync ()

                try
                    let datasetPath = join [| arcPath; "dataset" |] |> PathHelpers.normalizePath
                    let payloadPath = join [| datasetPath; "payload" |] |> PathHelpers.normalizePath
                    let rawPath = join [| payloadPath; "raw.bin" |] |> PathHelpers.normalizePath
                    do! createDirectoryAsync payloadPath
                    do! writeUtf8FileAsync rawPath "raw"

                    let initialTree = Dictionary<string, FileEntry>()
                    initialTree.[arcPath] <- directoryEntry (basename arcPath) arcPath
                    initialTree.[datasetPath] <- directoryEntry "dataset" datasetPath

                    let! datasetRefresh = FileTreeCreator.reconcileFileTreeDirectory arcPath "dataset" initialTree

                    let datasetTree = datasetRefresh |> Option.get
                    Vitest.expect(datasetTree.ContainsKey payloadPath).toBe (true)
                    Vitest.expect(datasetTree.ContainsKey rawPath).toBe (false)

                    let! payloadRefresh =
                        FileTreeCreator.reconcileFileTreeDirectory arcPath "dataset/payload" datasetTree

                    let payloadTree = payloadRefresh |> Option.get
                    Vitest.expect(payloadTree.ContainsKey rawPath).toBe (true)

                    let! unchanged = FileTreeCreator.reconcileFileTreeDirectory arcPath "dataset" payloadTree

                    Vitest.expect(unchanged.IsNone).toBe (true)
                    Vitest.expect(payloadTree.ContainsKey rawPath).toBe (true)

                    do! removeDirectoryAsync payloadPath

                    let! removed = FileTreeCreator.reconcileFileTreeDirectory arcPath "dataset" payloadTree

                    let removedTree = removed |> Option.get
                    Vitest.expect(removedTree.ContainsKey payloadPath).toBe (false)
                    Vitest.expect(removedTree.ContainsKey rawPath).toBe (false)
                    do! removeDirectoryAsync arcPath
                with error ->
                    do! removeDirectoryAsync arcPath
                    return raise error
            }
        )

        Vitest.test (
            "bulk child removal uses one subtree pass and preserves surviving directory descendants",
            fun () -> promise {
                let! tempPath = createTempDirectoryAsync ()

                try
                    let arcPath = PathHelpers.normalizePath tempPath
                    let datasetPath = join [| arcPath; "dataset" |] |> PathHelpers.normalizePath
                    let keepPath = join [| datasetPath; "keep" |] |> PathHelpers.normalizePath
                    let nestedPath = join [| keepPath; "nested.txt" |] |> PathHelpers.normalizePath

                    let removedDirectoryPath =
                        join [| datasetPath; "removed" |] |> PathHelpers.normalizePath

                    let removedNestedPath =
                        join [| removedDirectoryPath; "old.txt" |] |> PathHelpers.normalizePath

                    do! createDirectoryAsync keepPath
                    do! writeUtf8FileAsync nestedPath "keep"

                    let tree = Dictionary<string, FileEntry>()
                    tree.[arcPath] <- directoryEntry (basename arcPath) arcPath
                    tree.[datasetPath] <- directoryEntry "dataset" datasetPath
                    tree.[keepPath] <- directoryEntry "keep" keepPath
                    tree.[nestedPath] <- createFileEntry "nested.txt" nestedPath
                    tree.[removedDirectoryPath] <- directoryEntry "removed" removedDirectoryPath
                    tree.[removedNestedPath] <- createFileEntry "old.txt" removedNestedPath

                    let removedFilePaths =
                        Array.init
                            1000
                            (fun index ->
                                let name = $"file{index + 1:D4}.txt"
                                let path = join [| datasetPath; name |] |> PathHelpers.normalizePath
                                tree.[path] <- createFileEntry name path
                                path
                            )

                    let removalRoots = HashSet<string>()
                    removalRoots.Add removedDirectoryPath |> ignore
                    let mutable subtreePassCount = 0
                    let mutable visitedPathCount = 0

                    let countedPaths = seq {
                        subtreePassCount <- subtreePassCount + 1

                        for path in tree.Keys do
                            visitedPathCount <- visitedPathCount + 1
                            yield path
                    }

                    let subtreeRemovalKeys =
                        FileTreeCreator.collectDirectChildSubtreeRemovalKeys datasetPath removalRoots countedPaths

                    Vitest.expect(subtreePassCount).toBe (1)
                    Vitest.expect(visitedPathCount).toBe (tree.Count)
                    Vitest.expect(subtreeRemovalKeys).toContain (removedDirectoryPath)
                    Vitest.expect(subtreeRemovalKeys).toContain (removedNestedPath)
                    Vitest.expect(subtreeRemovalKeys).not.toContain (keepPath)
                    Vitest.expect(subtreeRemovalKeys).not.toContain (nestedPath)

                    let! reconciled = FileTreeCreator.reconcileFileTreeDirectory arcPath "dataset" tree
                    let reconciledTree = reconciled |> Option.get

                    Vitest.expect(removedFilePaths |> Array.forall (reconciledTree.ContainsKey >> not)).toBe (true)

                    Vitest.expect(reconciledTree.ContainsKey removedDirectoryPath).toBe (false)
                    Vitest.expect(reconciledTree.ContainsKey removedNestedPath).toBe (false)
                    Vitest.expect(reconciledTree.ContainsKey keepPath).toBe (true)
                    Vitest.expect(reconciledTree.ContainsKey nestedPath).toBe (true)
                    do! removeDirectoryAsync tempPath
                with error ->
                    do! removeDirectoryAsync tempPath
                    return raise error
            }
        )
)
