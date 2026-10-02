module ElectronCore.FileTreeCreatorTests

open System
open System.Collections.Generic
open Fable.Core
open Fable.Core.JsInterop
open Main
open Main.Bindings.Path
open Main.VersionControl
open Swate.Components.Shared
open Swate.Components.Shared.PathChildrenIndex
open Swate.Components.Composite.Authentication.Types
open Swate.Electron.Shared.FileIOHelper
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

let private removeFileAsync (path: string) : Fable.Core.JS.Promise<unit> = promise {
    let! _ =
        fsPromisesDynamic?rm (path, createObj [ "force" ==> true ])
        |> unbox<Fable.Core.JS.Promise<obj>>

    return ()
}

let private runInBatches count batchSize (operation: int -> Fable.Core.JS.Promise<unit>) = promise {
    let mutable offset = 0

    while offset < count do
        let lastIndex = min (offset + batchSize - 1) (count - 1)

        let operations = [| offset..lastIndex |] |> Array.map operation

        let! _ = Fable.Core.JS.Constructors.Promise.all operations
        offset <- lastIndex + 1
}

[<Emit("performance.now()")>]
let private performanceNow () : float = jsNative

[<Emit("console.log($0)")>]
let private logPerformanceMeasurement (_message: string) : unit = jsNative

[<Emit("$0.toFixed(2)")>]
let private formatDuration (_value: float) : string = jsNative

let private measurePromise (operation: unit -> Fable.Core.JS.Promise<'T>) = promise {
    let startedAt = performanceNow ()
    let! result = operation ()
    return result, performanceNow () - startedAt
}

let private median (values: float[]) =
    let sorted = Array.sort values
    let middle = sorted.Length / 2

    if sorted.Length % 2 = 0 then
        (sorted.[middle - 1] + sorted.[middle]) / 2.0
    else
        sorted.[middle]

let private percentile95 (values: float[]) =
    let sorted = Array.sort values

    let index =
        int (Math.Ceiling(0.95 * float sorted.Length)) - 1
        |> max 0
        |> min (sorted.Length - 1)

    sorted.[index]

type private ReconciliationMeasurement = {
    ChildCount: int
    Scenario: string
    Durations: float[]
}

let private formatMeasurement measurement =
    let medianDuration = median measurement.Durations
    let p95Duration = percentile95 measurement.Durations
    let maximumDuration = Array.max measurement.Durations

    $"{measurement.ChildCount} children | {measurement.Scenario}: median {formatDuration medianDuration} ms, p95 {formatDuration p95Duration} ms, max {formatDuration maximumDuration} ms"

let private directoryEntry name path =
    FileEntry.create (name, path, true, None)

let private createIndexedFileTree (fileTree: Dictionary<string, FileEntry>) = IndexedFileTree(fileTree)

let private getKnownDirectChildren (fileTree: IndexedFileTree) directoryPath =
    fileTree.GetKnownDirectChildren directoryPath

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
    "PathChildrenIndex",
    fun () ->
        Vitest.test (
            "returns only direct children",
            fun () ->
                let index = PathChildrenIndex()
                index.Rebuild [ "a"; "a/b"; "a/c"; "a/b/d" ]

                let children = index.GetDirectChildPaths "a"
                Vitest.expect(children.Length).toBe (2)
                Vitest.expect(children).toContain ("a/b")
                Vitest.expect(children).toContain ("a/c")
        )

        Vitest.test (
            "collects only the requested subtree",
            fun () ->
                let index = PathChildrenIndex()
                index.Rebuild [ "a"; "a/b"; "a/b/c"; "a/b/c/d"; "a/e" ]

                let paths = index.CollectSubtreePaths [ "a/b" ]
                Vitest.expect(paths.Length).toBe (3)
                Vitest.expect(paths).toContain ("a/b")
                Vitest.expect(paths).toContain ("a/b/c")
                Vitest.expect(paths).toContain ("a/b/c/d")
                Vitest.expect(paths).not.toContain ("a/e")
        )

        Vitest.test (
            "removing a path updates its parent and removes its child bucket",
            fun () ->
                let index = PathChildrenIndex()
                index.Rebuild [ "a"; "a/b"; "a/b/c" ]
                index.Remove "a/b"

                Vitest.expect(index.GetDirectChildPaths "a").toEqual [||]
                Vitest.expect(index.GetDirectChildPaths "a/b").toEqual [||]
        )

        Vitest.test (
            "rebuild completely replaces the previous paths",
            fun () ->
                let index = PathChildrenIndex()
                index.Rebuild [ "old"; "old/child" ]
                index.Rebuild [ "new"; "new/child" ]

                Vitest.expect(index.GetDirectChildPaths "old").toEqual [||]
                Vitest.expect(index.GetDirectChildPaths "new").toEqual [| "new/child" |]
        )
)

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
    "IndexedFileTree ownership",
    fun () ->
        Vitest.test (
            "ReplaceSnapshot replaces entries and indexed direct children",
            fun () ->
                let oldPath = "/repo/old.txt"
                let newPath = "/repo/new.txt"
                let initial = Dictionary<string, FileEntry>()
                initial.[oldPath] <- createFileEntry "old.txt" oldPath
                let tree = IndexedFileTree(initial)
                let replacement = Dictionary<string, FileEntry>()
                replacement.[newPath] <- createFileEntry "new.txt" newPath

                tree.ReplaceSnapshot replacement

                Vitest.expect(tree.ContainsKey oldPath).toBe (false)
                Vitest.expect(tree.ContainsKey newPath).toBe (true)
                Vitest.expect(tree.GetKnownDirectChildren "/repo" |> Map.containsKey newPath).toBe (true)
                Vitest.expect(tree.GetKnownDirectChildren "/repo" |> Map.containsKey oldPath).toBe (false)
        )

        Vitest.test (
            "Clear removes entries and indexed child relations",
            fun () ->
                let path = "/repo/folder/file.txt"
                let snapshot = Dictionary<string, FileEntry>()
                snapshot.[path] <- createFileEntry "file.txt" path
                let tree = IndexedFileTree(snapshot)

                tree.Clear()

                Vitest.expect(tree.Count).toBe (0)
                Vitest.expect(tree.GetKnownDirectChildren "/repo/folder" |> Map.isEmpty).toBe (true)
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
                    let indexedTree = createIndexedFileTree initialTree

                    let! datasetRefresh = FileTreeCreator.reconcileFileTreeDirectory arcPath "dataset" indexedTree

                    let datasetUpdate = datasetRefresh |> Option.get
                    Vitest.expect(datasetUpdate.directoryPath).toBe ("dataset")
                    Vitest.expect(datasetUpdate.children.Length).toBe (1)
                    Vitest.expect(datasetUpdate.children.[0].path).toBe ("dataset/payload")
                    Vitest.expect(initialTree.ContainsKey payloadPath).toBe (true)
                    Vitest.expect(initialTree.ContainsKey rawPath).toBe (false)

                    let! payloadRefresh =
                        FileTreeCreator.reconcileFileTreeDirectory arcPath "dataset/payload" indexedTree

                    let payloadUpdate = payloadRefresh |> Option.get
                    Vitest.expect(payloadUpdate.children.Length).toBe (1)
                    Vitest.expect(payloadUpdate.children.[0].path).toBe ("dataset/payload/raw.bin")
                    Vitest.expect(initialTree.ContainsKey rawPath).toBe (true)

                    let! unchanged = FileTreeCreator.reconcileFileTreeDirectory arcPath "dataset" indexedTree

                    Vitest.expect(unchanged.IsNone).toBe (true)
                    Vitest.expect(initialTree.ContainsKey rawPath).toBe (true)

                    do! removeDirectoryAsync payloadPath

                    let! removed = FileTreeCreator.reconcileFileTreeDirectory arcPath "dataset" indexedTree

                    let removedUpdate = removed |> Option.get
                    Vitest.expect(removedUpdate.children.Length).toBe (0)
                    Vitest.expect(initialTree.ContainsKey payloadPath).toBe (false)
                    Vitest.expect(initialTree.ContainsKey rawPath).toBe (false)
                    do! removeDirectoryAsync arcPath
                with error ->
                    do! removeDirectoryAsync arcPath
                    return raise error
            }
        )

        Vitest.test (
            "root reconciliation publishes authoritative relative children without discovering descendants",
            fun () -> promise {
                let! arcPath = createTempDirectoryAsync ()

                try
                    let oldPath = join [| arcPath; "old.txt" |] |> PathHelpers.normalizePath
                    let folderPath = join [| arcPath; "new-folder" |] |> PathHelpers.normalizePath
                    let deepPath = join [| folderPath; "deep.txt" |] |> PathHelpers.normalizePath
                    do! writeUtf8FileAsync oldPath "old"

                    let tree = Dictionary<string, FileEntry>()
                    tree.[arcPath] <- directoryEntry (basename arcPath) arcPath
                    tree.[oldPath] <- createFileEntry "old.txt" oldPath
                    let indexedTree = createIndexedFileTree tree

                    do! removeFileAsync oldPath
                    do! createDirectoryAsync folderPath
                    do! writeUtf8FileAsync deepPath "deep"

                    let! reconciliation = FileTreeCreator.reconcileFileTreeDirectory arcPath "" indexedTree

                    let reconciliation = reconciliation |> Option.get
                    Vitest.expect(reconciliation.directoryPath).toBe ("")
                    Vitest.expect(reconciliation.children.Length).toBe (1)
                    Vitest.expect(reconciliation.children.[0].path).toBe ("new-folder")
                    Vitest.expect(tree.ContainsKey oldPath).toBe (false)
                    Vitest.expect(tree.ContainsKey folderPath).toBe (true)
                    Vitest.expect(tree.ContainsKey deepPath).toBe (false)

                    do! removeDirectoryAsync arcPath
                with error ->
                    do! removeDirectoryAsync arcPath
                    return raise error
            }
        )

        Vitest.test (
            "removed directory traversal stays inside its indexed subtree with 64k unrelated entries",
            TestOptions(timeout = 120000),
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

                    let removedDeepDirectoryPath =
                        join [| removedDirectoryPath; "deep" |] |> PathHelpers.normalizePath

                    let removedDeepFilePath =
                        join [| removedDeepDirectoryPath; "deep.txt" |] |> PathHelpers.normalizePath

                    do! createDirectoryAsync keepPath
                    do! writeUtf8FileAsync nestedPath "keep"

                    let tree = Dictionary<string, FileEntry>()
                    tree.[arcPath] <- directoryEntry (basename arcPath) arcPath
                    tree.[datasetPath] <- directoryEntry "dataset" datasetPath
                    tree.[keepPath] <- directoryEntry "keep" keepPath
                    tree.[nestedPath] <- createFileEntry "nested.txt" nestedPath
                    tree.[removedDirectoryPath] <- directoryEntry "removed" removedDirectoryPath
                    tree.[removedNestedPath] <- createFileEntry "old.txt" removedNestedPath
                    tree.[removedDeepDirectoryPath] <- directoryEntry "deep" removedDeepDirectoryPath
                    tree.[removedDeepFilePath] <- createFileEntry "deep.txt" removedDeepFilePath

                    let totalKnownEntryCount = 64000

                    for index in 1 .. totalKnownEntryCount - tree.Count do
                        let name = $"unrelated-{index}.txt"
                        let path = join [| arcPath; "unrelated"; name |] |> PathHelpers.normalizePath
                        tree.[path] <- createFileEntry name path

                    let indexedTree = createIndexedFileTree tree

                    let subtreeRemovalKeys = indexedTree.CollectSubtreePaths [| removedDirectoryPath |]

                    Vitest.expect(tree.Count).toBe (totalKnownEntryCount)
                    Vitest.expect(subtreeRemovalKeys.Length).toBe (4)
                    Vitest.expect(subtreeRemovalKeys).toContain (removedDirectoryPath)
                    Vitest.expect(subtreeRemovalKeys).toContain (removedNestedPath)
                    Vitest.expect(subtreeRemovalKeys).toContain (removedDeepDirectoryPath)
                    Vitest.expect(subtreeRemovalKeys).toContain (removedDeepFilePath)
                    Vitest.expect(subtreeRemovalKeys).not.toContain (keepPath)
                    Vitest.expect(subtreeRemovalKeys).not.toContain (nestedPath)

                    let! reconciled = FileTreeCreator.reconcileFileTreeDirectory arcPath "dataset" indexedTree

                    let update = reconciled |> Option.get
                    Vitest.expect(update.children.Length).toBe (1)
                    Vitest.expect(update.children.[0].path).toBe ("dataset/keep")
                    Vitest.expect(tree.Count).toBe (totalKnownEntryCount - 4)
                    Vitest.expect(tree.ContainsKey removedDirectoryPath).toBe (false)
                    Vitest.expect(tree.ContainsKey removedNestedPath).toBe (false)
                    Vitest.expect(tree.ContainsKey removedDeepDirectoryPath).toBe (false)
                    Vitest.expect(tree.ContainsKey removedDeepFilePath).toBe (false)
                    Vitest.expect(tree.ContainsKey keepPath).toBe (true)
                    Vitest.expect(tree.ContainsKey nestedPath).toBe (true)

                    let remainingDirectChildren = getKnownDirectChildren indexedTree datasetPath

                    Vitest.expect(remainingDirectChildren.Count).toBe (1)
                    Vitest.expect(remainingDirectChildren.ContainsKey keepPath).toBe (true)
                    do! removeDirectoryAsync tempPath
                with error ->
                    do! removeDirectoryAsync tempPath
                    return raise error
            }
        )
)

Vitest.describe (
    "FileTreeCreator initial and shallow loading contract",
    fun () ->
        Vitest.test (
            "initial loading is recursive while later reconciliation remains shallow",
            fun () -> promise {
                let! tempPath = createTempDirectoryAsync ()

                try
                    let arcPath = PathHelpers.normalizePath tempPath

                    let resourcesPath =
                        join [| arcPath; "studies"; "S1"; "resources" |] |> PathHelpers.normalizePath

                    let level1Path = join [| resourcesPath; "level1" |] |> PathHelpers.normalizePath
                    let level2Path = join [| level1Path; "level2" |] |> PathHelpers.normalizePath
                    let level3Path = join [| level2Path; "level3" |] |> PathHelpers.normalizePath
                    let deepFilePath = join [| level3Path; "deep.txt" |] |> PathHelpers.normalizePath

                    do! createDirectoryAsync level3Path
                    do! writeUtf8FileAsync deepFilePath "deep"

                    let! initialEntries = FileTreeCreator.getFileEntries arcPath false

                    let initialPaths =
                        initialEntries
                        |> Array.map (fun entry -> PathHelpers.normalizePath entry.path)
                        |> Set.ofArray

                    Vitest.expect(initialPaths.Contains resourcesPath).toBe (true)
                    Vitest.expect(initialPaths.Contains level1Path).toBe (true)
                    Vitest.expect(initialPaths.Contains level2Path).toBe (true)
                    Vitest.expect(initialPaths.Contains level3Path).toBe (true)
                    Vitest.expect(initialPaths.Contains deepFilePath).toBe (true)

                    let shallowTree = Dictionary<string, FileEntry>()
                    shallowTree.[arcPath] <- directoryEntry (basename arcPath) arcPath
                    shallowTree.[resourcesPath] <- directoryEntry "resources" resourcesPath
                    let indexedTree = createIndexedFileTree shallowTree

                    let! shallowRefresh =
                        FileTreeCreator.reconcileFileTreeDirectory arcPath "studies/S1/resources" indexedTree

                    let shallowUpdate = shallowRefresh |> Option.get
                    Vitest.expect(shallowUpdate.children.Length).toBe (1)
                    Vitest.expect(shallowUpdate.children.[0].path).toBe ("studies/S1/resources/level1")
                    Vitest.expect(shallowTree.ContainsKey level1Path).toBe (true)
                    Vitest.expect(shallowTree.ContainsKey level2Path).toBe (false)
                    Vitest.expect(shallowTree.ContainsKey level3Path).toBe (false)
                    Vitest.expect(shallowTree.ContainsKey deepFilePath).toBe (false)
                    do! removeDirectoryAsync tempPath
                with error ->
                    do! removeDirectoryAsync tempPath
                    return raise error
            }
        )

        Vitest.test (
            "shallow discovery ignores descendants below its direct directory layer",
            TestOptions(timeout = 120000),
            fun () -> promise {
                let! tempPath = createTempDirectoryAsync ()

                try
                    let arcPath = PathHelpers.normalizePath tempPath
                    let datasetPath = join [| arcPath; "dataset" |] |> PathHelpers.normalizePath
                    let directDirectoryCount = 100

                    do! createDirectoryAsync datasetPath

                    do!
                        runInBatches
                            directDirectoryCount
                            50
                            (fun index -> promise {
                                let directDirectoryName = $"folder{index + 1}"

                                let deepestDirectoryPath =
                                    join [| datasetPath; directDirectoryName; "level1"; "level2" |]
                                    |> PathHelpers.normalizePath

                                do! createDirectoryAsync deepestDirectoryPath

                                do! writeUtf8FileAsync (join [| deepestDirectoryPath; "payload.bin" |]) "payload"
                            })

                    let createUnmaterializedState () =
                        let tree = Dictionary<string, FileEntry>()
                        tree.[arcPath] <- directoryEntry (basename arcPath) arcPath
                        tree.[datasetPath] <- directoryEntry "dataset" datasetPath
                        tree, createIndexedFileTree tree

                    let _, warmupTree = createUnmaterializedState ()

                    let! _ = FileTreeCreator.reconcileFileTreeDirectory arcPath "dataset" warmupTree

                    let measuredEntries, measuredTree = createUnmaterializedState ()

                    let! measuredRefresh, duration =
                        measurePromise (fun () ->
                            FileTreeCreator.reconcileFileTreeDirectory arcPath "dataset" measuredTree
                        )

                    let measuredUpdate = measuredRefresh |> Option.get
                    Vitest.expect(measuredUpdate.children.Length).toBe (directDirectoryCount)

                    for index in 0 .. directDirectoryCount - 1 do
                        let directDirectoryName = $"folder{index + 1}"

                        let directDirectoryPath =
                            join [| datasetPath; directDirectoryName |] |> PathHelpers.normalizePath

                        let level1Path =
                            join [| directDirectoryPath; "level1" |] |> PathHelpers.normalizePath

                        Vitest.expect(measuredEntries.ContainsKey directDirectoryPath).toBe (true)
                        Vitest.expect(measuredEntries.ContainsKey level1Path).toBe (false)

                    logPerformanceMeasurement (
                        $"{directDirectoryCount} deep directory children | shallow discovery: {formatDuration duration} ms"
                    )

                    do! removeDirectoryAsync tempPath
                with error ->
                    do! removeDirectoryAsync tempPath
                    return raise error
            }
        )
)

Vitest.describe (
    "FileTreeCreator shallow reconciliation performance diagnostics",
    fun () ->
        Vitest.test (
            "reports discovery, unchanged, incremental, and bulk-removal scaling",
            TestOptions(timeout = 600000),
            fun () -> promise {
                let measurements = ResizeArray<ReconciliationMeasurement>()
                let childCounts = [| 100; 1000; 7000 |]
                let totalKnownEntryCount = 64000

                for childCount in childCounts do
                    let! tempPath = createTempDirectoryAsync ()

                    try
                        let arcPath = PathHelpers.normalizePath tempPath
                        let datasetPath = join [| arcPath; "dataset" |] |> PathHelpers.normalizePath

                        let filePath index =
                            join [| datasetPath; $"file{index + 1}.txt" |] |> PathHelpers.normalizePath

                        let createFiles () =
                            runInBatches childCount 250 (fun index -> writeUtf8FileAsync (filePath index) "payload")

                        let unmaterializedTree =
                            let tree = Dictionary<string, FileEntry>()
                            tree.[arcPath] <- directoryEntry (basename arcPath) arcPath
                            tree.[datasetPath] <- directoryEntry "dataset" datasetPath

                            for index in 1 .. totalKnownEntryCount - 2 do
                                let name = $"unrelated-{index}.txt"

                                let path = join [| arcPath; "unrelated"; name |] |> PathHelpers.normalizePath

                                tree.[path] <- createFileEntry name path

                            tree

                        do! createDirectoryAsync datasetPath
                        do! createFiles ()

                        let cloneState (sourceTree: Dictionary<string, FileEntry>) =
                            let tree = Dictionary<string, FileEntry>(sourceTree)
                            tree, createIndexedFileTree tree

                        let _, warmupTree = cloneState unmaterializedTree

                        let! _ = FileTreeCreator.reconcileFileTreeDirectory arcPath "dataset" warmupTree

                        let discoveryDurations = ResizeArray<float>()
                        let mutable materializedEntries = Dictionary<string, FileEntry>()
                        let mutable materializedTree = IndexedFileTree(materializedEntries)

                        for _ in 1..3 do
                            let discoveryEntries, discoveryTree = cloneState unmaterializedTree

                            let! discoveryResult, duration =
                                measurePromise (fun () ->
                                    FileTreeCreator.reconcileFileTreeDirectory arcPath "dataset" discoveryTree
                                )

                            let discoveryUpdate = discoveryResult |> Option.get
                            Vitest.expect(discoveryUpdate.children.Length).toBe (childCount)
                            Vitest.expect(discoveryEntries.Count).toBe (totalKnownEntryCount + childCount)
                            Vitest.expect(discoveryEntries.ContainsKey(filePath 0)).toBe (true)
                            Vitest.expect(discoveryEntries.ContainsKey(filePath (childCount - 1))).toBe (true)
                            materializedEntries <- discoveryEntries
                            materializedTree <- discoveryTree
                            discoveryDurations.Add duration

                        let materializedBaseline = Dictionary<string, FileEntry>(materializedEntries)

                        let indexedDirectChildren = getKnownDirectChildren materializedTree datasetPath

                        Vitest.expect(indexedDirectChildren.Count).toBe (childCount)
                        Vitest.expect(materializedEntries.Count).toBe (totalKnownEntryCount + childCount)

                        measurements.Add {
                            ChildCount = childCount
                            Scenario = "first discovery"
                            Durations = discoveryDurations.ToArray()
                        }

                        let! unchangedWarmup =
                            FileTreeCreator.reconcileFileTreeDirectory arcPath "dataset" materializedTree

                        Vitest.expect(unchangedWarmup.IsNone).toBe (true)
                        let unchangedDurations = ResizeArray<float>()

                        for _ in 1..5 do
                            let! unchangedResult, duration =
                                measurePromise (fun () ->
                                    FileTreeCreator.reconcileFileTreeDirectory arcPath "dataset" materializedTree
                                )

                            Vitest.expect(unchangedResult.IsNone).toBe (true)
                            unchangedDurations.Add duration

                        measurements.Add {
                            ChildCount = childCount
                            Scenario = "unchanged refresh"
                            Durations = unchangedDurations.ToArray()
                        }

                        let addedWarmupPath = join [| datasetPath; "added-warmup.txt" |]
                        do! writeUtf8FileAsync addedWarmupPath "added"
                        let _, addedWarmupTree = cloneState materializedBaseline

                        let! addedWarmup = FileTreeCreator.reconcileFileTreeDirectory arcPath "dataset" addedWarmupTree

                        Vitest.expect(addedWarmup.IsSome).toBe (true)
                        do! removeFileAsync addedWarmupPath
                        let incrementalDurations = ResizeArray<float>()

                        for sampleIndex in 1..3 do
                            let addedPath =
                                join [| datasetPath; $"added-{sampleIndex}.txt" |] |> PathHelpers.normalizePath

                            do! writeUtf8FileAsync addedPath "added"
                            let incrementalEntries, incrementalTree = cloneState materializedBaseline

                            let! incrementalResult, duration =
                                measurePromise (fun () ->
                                    FileTreeCreator.reconcileFileTreeDirectory arcPath "dataset" incrementalTree
                                )

                            let incrementalUpdate = incrementalResult |> Option.get
                            Vitest.expect(incrementalUpdate.children.Length).toBe (childCount + 1)
                            let relativeAddedPath = tryGetRepoRelativePath arcPath addedPath |> Option.get

                            Vitest.expect(incrementalUpdate.children |> Array.map _.path).toContain (relativeAddedPath)

                            Vitest.expect(incrementalEntries.Count).toBe (totalKnownEntryCount + childCount + 1)

                            Vitest.expect(incrementalEntries.ContainsKey addedPath).toBe (true)
                            incrementalDurations.Add duration
                            do! removeFileAsync addedPath

                        measurements.Add {
                            ChildCount = childCount
                            Scenario = "one added file"
                            Durations = incrementalDurations.ToArray()
                        }

                        let bulkRemovalDurations = ResizeArray<float>()

                        for sampleIndex in 1..2 do
                            if sampleIndex > 1 then
                                do! createFiles ()

                            do! removeDirectoryAsync datasetPath
                            do! createDirectoryAsync datasetPath
                            let removalEntries, removalTree = cloneState materializedBaseline

                            let! removalResult, duration =
                                measurePromise (fun () ->
                                    FileTreeCreator.reconcileFileTreeDirectory arcPath "dataset" removalTree
                                )

                            let removalUpdate = removalResult |> Option.get
                            Vitest.expect(removalUpdate.children.Length).toBe (0)
                            Vitest.expect(removalEntries.Count).toBe (totalKnownEntryCount)
                            Vitest.expect(removalEntries.ContainsKey(filePath 0)).toBe (false)
                            Vitest.expect(removalEntries.ContainsKey(filePath (childCount - 1))).toBe (false)
                            bulkRemovalDurations.Add duration

                        measurements.Add {
                            ChildCount = childCount
                            Scenario = "bulk removal"
                            Durations = bulkRemovalDurations.ToArray()
                        }

                        do! removeDirectoryAsync tempPath
                    with error ->
                        do! removeDirectoryAsync tempPath
                        return raise error

                logPerformanceMeasurement "Shallow FileTree reconciliation performance"

                measurements |> Seq.iter (formatMeasurement >> logPerformanceMeasurement)
            }
        )
)
