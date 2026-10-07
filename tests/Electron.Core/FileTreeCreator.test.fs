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
module ArcVaultHelper = Main.ArcVaultHelper

let private fsPromisesDynamic: obj = importAll "fs/promises"
let private osDynamic: obj = importAll "os"
let private childProcessDynamic: obj = importAll "node:child_process"

let private fileTreeCreatorTestOptions = TestOptions(timeout = 20000)

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

let private createDirectoryAtAsync (path: string) : Fable.Core.JS.Promise<unit> = promise {
    let! _ =
        fsPromisesDynamic?mkdir (path, createObj [ "recursive" ==> true ])
        |> unbox<Fable.Core.JS.Promise<obj>>

    return ()
}

let private withTempDirectory (testBody: string -> Fable.Core.JS.Promise<unit>) = promise {
    let! rootPath = createTempDirectoryAsync ()

    try
        do! testBody rootPath
        do! removeDirectoryAsync rootPath
    with error ->
        do! removeDirectoryAsync rootPath
        return raise error
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
            "individual entries annotate materialized and pointer large objects",
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

                        let! pointerEntry = FileTreeCreator.getFileEntry pointerFilePath
                        let! downloadedEntry = FileTreeCreator.getFileEntry downloadedFilePath

                        let! enrichedEntries =
                            FileTreeCreator.getFileEntriesWithLfsMetadata context.RepoPath [|
                                pointerEntry
                                downloadedEntry
                            |]

                        let pointerEntry = enrichedEntries.[0]
                        let downloadedEntry = enrichedEntries.[1]

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

                        let! entry = FileTreeCreator.getFileEntry untrackedLfsPath

                        let! enrichedEntries =
                            FileTreeCreator.getFileEntriesWithLfsMetadata context.RepoPath [| entry |]

                        let enrichedEntry = enrichedEntries.[0]

                        Vitest.expect(enrichedEntry.largeObject).toEqual (None)
                    })
            }
        )

)

Vitest.describe (
    "FileTreeCreator.removePathAndDescendants",
    fun () ->
        let removePathAndDescendants targetPath (fileTree: Dictionary<string, FileEntry>) =
            let nextTree = Dictionary<string, FileEntry>(fileTree)
            FileTreeCreator.removePathAndDescendantsInPlace targetPath nextTree
            nextTree

        let createTreeEntry path isDirectory = {
            name = path |> PathHelpers.normalizePath |> PathHelpers.getFileName
            isDirectory = isDirectory
            path = path
            largeObject = None
        }

        Vitest.test (
            "removes only the target path and descendants without mutating the input",
            fun () ->
                let tree = Dictionary<string, FileEntry>()
                tree.Add("C:/arc", createTreeEntry "C:/arc" true)
                tree.Add("C:/arc/assays", createTreeEntry "C:/arc/assays" true)
                tree.Add("C:/arc/assays/A", createTreeEntry "C:/arc/assays/A" true)
                tree.Add("C:/arc/assays/A/isa.assay.xlsx", createTreeEntry "C:/arc/assays/A/isa.assay.xlsx" false)
                tree.Add("C:/arc/assays/AB", createTreeEntry "C:/arc/assays/AB" true)
                tree.Add("C:/arc/assays/AB/isa.assay.xlsx", createTreeEntry "C:/arc/assays/AB/isa.assay.xlsx" false)

                let returnedTree = removePathAndDescendants "C:/arc/assays/A" tree

                Vitest.expect(returnedTree.ContainsKey("C:/arc/assays/A")).toBe (false)
                Vitest.expect(returnedTree.ContainsKey("C:/arc/assays/A/isa.assay.xlsx")).toBe (false)
                Vitest.expect(returnedTree.ContainsKey("C:/arc/assays/AB")).toBe (true)
                Vitest.expect(returnedTree.ContainsKey("C:/arc/assays/AB/isa.assay.xlsx")).toBe (true)
                Vitest.expect(tree.ContainsKey("C:/arc/assays/A")).toBe (true)
                Vitest.expect(tree.ContainsKey("C:/arc/assays/A/isa.assay.xlsx")).toBe (true)
                Vitest.expect(tree.ContainsKey("C:/arc/assays/AB")).toBe (true)
                Vitest.expect(tree.ContainsKey("C:/arc/assays/AB/isa.assay.xlsx")).toBe (true)
        )
)

Vitest.describe (
    "FileTreeCreator.upsertFileEntry",
    fun () ->
        let createTreeEntry path largeObject = {
            name = path |> PathHelpers.normalizePath |> PathHelpers.getFileName
            isDirectory = false
            path = path
            largeObject = largeObject
        }

        let pointerInfo: ObjectStateDto = {
            Path = "data.bin"
            SizeBytes = Some 128.0
            IsMaterialized = false
            IsLocallyAvailable = false
            ObjectId = Some "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"
        }

        Vitest.test (
            "adds exactly one entry while preserving existing entries",
            fun () ->
                let tree = Dictionary<string, FileEntry>()
                tree.Add("C:/arc/other.bin", createTreeEntry "C:/arc/other.bin" None)

                let updatedTree =
                    FileTreeCreator.upsertFileEntry (createTreeEntry "C:/arc/data.bin" (Some pointerInfo)) tree

                Vitest.expect(updatedTree.Count).toBe (2)
                Vitest.expect(updatedTree.ContainsKey("C:/arc/other.bin")).toBe (true)
                Vitest.expect(updatedTree.["C:/arc/data.bin"].largeObject).toEqual (Some pointerInfo)
                Vitest.expect(tree.Count).toBe (1)
                Vitest.expect(tree.ContainsKey("C:/arc/data.bin")).toBe (false)
        )

        Vitest.test (
            "replaces an entry without mutating the current file tree",
            fun () ->
                let tree = Dictionary<string, FileEntry>()
                tree.Add("C:/arc/data.bin", createTreeEntry "C:/arc/data.bin" None)
                tree.Add("C:/arc/other.bin", createTreeEntry "C:/arc/other.bin" None)

                let updatedTree =
                    FileTreeCreator.upsertFileEntry (createTreeEntry "C:/arc/data.bin" (Some pointerInfo)) tree

                Vitest.expect(updatedTree.Count).toBe (2)
                Vitest.expect(updatedTree.["C:/arc/data.bin"].largeObject).toEqual (Some pointerInfo)
                Vitest.expect(updatedTree.ContainsKey("C:/arc/other.bin")).toBe (true)
                Vitest.expect(tree.["C:/arc/data.bin"].largeObject).toEqual (None)
                Vitest.expect(tree.ContainsKey("C:/arc/other.bin")).toBe (true)
        )
)

Vitest.describe (
    "shallow FileTree loading",
    fun () ->
        Vitest.test (
            "startup includes only the root and direct children even for a large nested payload",
            fileTreeCreatorTestOptions,
            fun () -> promise {
                do!
                    withTempDirectory (fun rootPath -> promise {
                        let payloadPath = join [| rootPath; "dataset" |]
                        do! createDirectoryAtAsync payloadPath
                        do! writeUtf8FileAsync (join [| rootPath; "README.md" |]) "ARC"

                        let writes =
                            Array.init
                                1000
                                (fun index ->
                                    writeUtf8FileAsync (join [| payloadPath; $"file-{index:D4}.txt" |]) "payload"
                                )

                        let! _ = Fable.Core.JS.Constructors.Promise.all writes
                        let! tree = FileTreeCreator.getFileTree rootPath
                        let names = tree.Values |> Seq.map _.name |> Seq.toArray

                        Vitest.expect(tree.Count).toBe (3)
                        Vitest.expect(names |> Array.contains "README.md").toBe (true)
                        Vitest.expect(names |> Array.contains "dataset").toBe (true)
                        Vitest.expect(names |> Array.contains "file-0000.txt").toBe (false)
                    })
            }
        )

        Vitest.test (
            "each shallow read loads exactly one expanded level",
            fileTreeCreatorTestOptions,
            fun () -> promise {
                do!
                    withTempDirectory (fun rootPath -> promise {
                        let studiesPath = join [| rootPath; "studies" |]
                        let studyPath = join [| studiesPath; "S1" |]
                        let datasetPath = join [| studyPath; "dataset" |]
                        do! createDirectoryAtAsync datasetPath
                        do! writeUtf8FileAsync (join [| studyPath; "isa.study.xlsx" |]) "study"
                        do! writeUtf8FileAsync (join [| datasetPath; "sample.txt" |]) "sample"

                        let! studiesChildren = FileTreeCreator.readFileTreeDirectory rootPath "studies"
                        Vitest.expect(studiesChildren |> Array.map _.name).toEqual ([| "S1" |])

                        let! studyChildren = FileTreeCreator.readFileTreeDirectory rootPath "studies/S1"
                        let childNames = studyChildren |> Array.map _.name |> Array.sort
                        Vitest.expect(childNames).toEqual ([| "dataset"; "isa.study.xlsx" |])

                        Vitest
                            .expect(studyChildren |> Array.exists (fun entry -> entry.name = "sample.txt"))
                            .toBe (false)
                    })
            }
        )

        Vitest.test (
            "directory reconciliation adds and removes direct children while preserving surviving descendants",
            fun () ->
                let rootPath = resolve [| "arc" |]
                let studiesPath = join [| rootPath; "studies" |]
                let removedPath = join [| studiesPath; "removed" |]
                let removedChildPath = join [| removedPath; "old.txt" |]
                let survivingPath = join [| studiesPath; "surviving" |]
                let survivingChildPath = join [| survivingPath; "known.txt" |]
                let newChildPath = join [| studiesPath; "new.txt" |]

                let initial =
                    [|
                        FileEntry.create ("arc", rootPath, true)
                        FileEntry.create ("studies", studiesPath, true)
                        FileEntry.create ("removed", removedPath, true)
                        FileEntry.create ("old.txt", removedChildPath, false)
                        FileEntry.create ("surviving", survivingPath, true)
                        FileEntry.create ("known.txt", survivingChildPath, false)
                    |]
                    |> createFileEntryTree

                let currentChildren = [|
                    FileEntry.create ("surviving", survivingPath, true)
                    FileEntry.create ("new.txt", newChildPath, false)
                |]

                let reconciled =
                    FileTreeCreator.reconcileFileTreeDirectory rootPath "studies" currentChildren initial

                Vitest.expect(reconciled.ContainsKey(removedPath)).toBe (false)
                Vitest.expect(reconciled.ContainsKey(removedChildPath)).toBe (false)
                Vitest.expect(reconciled.ContainsKey(survivingChildPath)).toBe (true)
                Vitest.expect(reconciled.ContainsKey(newChildPath)).toBe (true)
        )

        Vitest.test (
            "permanent watcher includes canonical structure and excludes deep payload files",
            fun () ->
                let rootPath = "C:/arc"

                Vitest
                    .expect(ArcVaultHelper.isPermanentFileWatcherPathIgnored rootPath $"{rootPath}/studies")
                    .toBe (false)

                Vitest
                    .expect(ArcVaultHelper.isPermanentFileWatcherPathIgnored rootPath $"{rootPath}/studies/S1")
                    .toBe (false)

                Vitest
                    .expect(
                        ArcVaultHelper.isPermanentFileWatcherPathIgnored
                            rootPath
                            $"{rootPath}/studies/S1/isa.study.xlsx"
                    )
                    .toBe (false)

                Vitest
                    .expect(
                        ArcVaultHelper.isPermanentFileWatcherPathIgnored
                            rootPath
                            $"{rootPath}/studies/S1/dataset/file-99999.txt"
                    )
                    .toBe (true)
        )
)
