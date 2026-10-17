module ElectronCore.FileTreeCreatorTests

open System
open System.Collections.Generic
open Fable.Core
open Fable.Core.JsInterop
open Main
open Main.ArcVault
open Main.Bindings.Filesystem
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

let private enrichFromRepository (repoPath: string) (entries: FileEntry[]) =
    FileTreeCreator.LargeObjectMetadataCache().Enrich(repoPath, entries, fun () -> true)

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

            let gitFactory =
                ProviderComposition.createGitFactory noAccounts WorkspaceSessionHost.windowOwnerOf

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
                            enrichFromRepository context.RepoPath [| pointerEntry; downloadedEntry |]

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
            "LFS metadata matches separator variants",
            fun () ->
                let largeObject = createObjectState "dataset/data.csv"

                let entries =
                    enrichFileEntries [ "dataset/data.csv", largeObject ] [|
                        createFileEntry "data.csv" "/repo/dataset\\data.csv"
                    |]

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

                        let! enrichedEntries = enrichFromRepository context.RepoPath [| entry |]

                        let enrichedEntry = enrichedEntries.[0]

                        Vitest.expect(enrichedEntry.largeObject).toEqual (None)
                    })
            }
        )

)

Vitest.describe (
    "FileTreeCreator.removePathAndDescendantsInPlace",
    fun () ->
        let createTreeEntry path isDirectory = {
            name = path |> PathHelpers.normalizePath |> PathHelpers.getFileName
            isDirectory = isDirectory
            path = path
            largeObject = None
        }

        Vitest.test (
            "removes only the target path and descendants",
            fun () ->
                let tree = Dictionary<string, FileEntry>()
                tree.Add("C:/arc", createTreeEntry "C:/arc" true)
                tree.Add("C:/arc/assays", createTreeEntry "C:/arc/assays" true)
                tree.Add("C:/arc/assays/A", createTreeEntry "C:/arc/assays/A" true)
                tree.Add("C:/arc/assays/A/isa.assay.xlsx", createTreeEntry "C:/arc/assays/A/isa.assay.xlsx" false)
                tree.Add("C:/arc/assays/AB", createTreeEntry "C:/arc/assays/AB" true)
                tree.Add("C:/arc/assays/AB/isa.assay.xlsx", createTreeEntry "C:/arc/assays/AB/isa.assay.xlsx" false)

                FileTreeCreator.removePathAndDescendantsInPlace "C:/arc/assays/A" tree

                Vitest.expect(tree.ContainsKey("C:/arc/assays/A")).toBe (false)
                Vitest.expect(tree.ContainsKey("C:/arc/assays/A/isa.assay.xlsx")).toBe (false)
                Vitest.expect(tree.ContainsKey("C:/arc")).toBe (true)
                Vitest.expect(tree.ContainsKey("C:/arc/assays")).toBe (true)
                Vitest.expect(tree.ContainsKey("C:/arc/assays/AB")).toBe (true)
                Vitest.expect(tree.ContainsKey("C:/arc/assays/AB/isa.assay.xlsx")).toBe (true)
        )

        Vitest.test (
            "removes several paths with their descendants and keeps siblings that share a name prefix",
            fun () ->
                let tree = Dictionary<string, FileEntry>()

                for path in
                    [
                        "C:/arc/dataset"
                        "C:/arc/dataset/sample-1.csv"
                        "C:/arc/dataset/sample-10.csv"
                        "C:/arc/dataset/run"
                        "C:/arc/dataset/run/raw.bin"
                        "C:/arc/dataset/runs"
                        "C:/arc/dataset/runs/raw.bin"
                    ] do
                    tree.Add(path, createTreeEntry path (not (path.EndsWith ".csv" || path.EndsWith ".bin")))

                // Descendants match case-insensitively, like PathHelpers.isSameOrDescendantPath.
                FileTreeCreator.removePathsAndDescendantsInPlace
                    [ "C:/arc/dataset/sample-1.csv"; "C:\\arc\\dataset\\RUN\\" ]
                    tree

                Vitest.expect(tree.Keys |> Seq.sort |> Seq.toArray).toEqual [|
                    "C:/arc/dataset"
                    "C:/arc/dataset/run"
                    "C:/arc/dataset/runs"
                    "C:/arc/dataset/runs/raw.bin"
                    "C:/arc/dataset/sample-10.csv"
                |]
        )
)

Vitest.describe (
    "fresh FileTree directory snapshots",
    fun () ->
        let createFixture () =
            let rootPath = FileTreeCreator.normalizeRootPath "arc"

            let entry relativePath isDirectory =
                let path = join [| rootPath; relativePath |]
                FileEntry.create (basename path, path, isDirectory)

            let initial =
                [|
                    entry "" true
                    entry "studies" true
                    entry "studies/existing.txt" false
                    entry "studies/missing" true
                    entry "studies/missing/old.txt" false
                    entry "studies/surviving" true
                    entry "studies/surviving/known.txt" false
                    entry "sibling.txt" false
                |]
                |> createFileEntryTree

            rootPath, entry, initial

        Vitest.test (
            "a partial fresh snapshot updates entries while retaining cached children and descendants",
            fun () ->
                let rootPath, entry, initial = createFixture ()
                let originalEntries = initial.Values |> Seq.toArray
                let existing = entry "studies/existing.txt" false

                let updated = {
                    existing with
                        largeObject = Some(createObjectState "studies/existing.txt")
                }

                let added = entry "studies/new.txt" false

                let snapshot: FileTreeCreator.FileTreeDirectoryPage = {
                    Entries = [| updated; added |]
                    HasMore = true
                }

                let refreshed =
                    FileTreeCreator.applyFreshFileTreeDirectorySnapshot rootPath "studies" snapshot initial

                Vitest.expect(refreshed.[existing.path]).toEqual updated
                Vitest.expect(refreshed.[added.path]).toEqual added
                Vitest.expect(refreshed.ContainsKey((entry "studies/missing" true).path)).toBe true
                Vitest.expect(refreshed.ContainsKey((entry "studies/missing/old.txt" false).path)).toBe true
                Vitest.expect(refreshed.ContainsKey((entry "studies/surviving/known.txt" false).path)).toBe true
                Vitest.expect(refreshed.ContainsKey((entry "sibling.txt" false).path)).toBe true
                Vitest.expect(initial.Values |> Seq.toArray).toEqual originalEntries
        )

        Vitest.test (
            "an exhaustive fresh snapshot removes missing children and preserves surviving descendants",
            fun () ->
                let rootPath, entry, initial = createFixture ()
                let originalEntries = initial.Values |> Seq.toArray
                let surviving = entry "studies/surviving" true
                let added = entry "studies/new.txt" false

                let snapshot: FileTreeCreator.FileTreeDirectoryPage = {
                    Entries = [| surviving; added |]
                    HasMore = false
                }

                let refreshed =
                    FileTreeCreator.applyFreshFileTreeDirectorySnapshot rootPath "studies" snapshot initial

                Vitest.expect(refreshed.ContainsKey((entry "studies/existing.txt" false).path)).toBe false
                Vitest.expect(refreshed.ContainsKey((entry "studies/missing" true).path)).toBe false
                Vitest.expect(refreshed.ContainsKey((entry "studies/missing/old.txt" false).path)).toBe false
                Vitest.expect(refreshed.[surviving.path]).toEqual surviving
                Vitest.expect(refreshed.ContainsKey((entry "studies/surviving/known.txt" false).path)).toBe true
                Vitest.expect(refreshed.[added.path]).toEqual added
                Vitest.expect(refreshed.ContainsKey((entry "sibling.txt" false).path)).toBe true
                Vitest.expect(initial.Values |> Seq.toArray).toEqual originalEntries
        )

        Vitest.test (
            "an exhaustive empty snapshot clears the directory without removing its parent or siblings",
            fun () ->
                let rootPath, entry, initial = createFixture ()
                let originalEntries = initial.Values |> Seq.toArray
                let snapshot: FileTreeCreator.FileTreeDirectoryPage = { Entries = [||]; HasMore = false }

                let refreshed =
                    FileTreeCreator.applyFreshFileTreeDirectorySnapshot rootPath "studies" snapshot initial

                let expected = [|
                    entry "" true
                    entry "studies" true
                    entry "sibling.txt" false
                |]

                Vitest.expect(refreshed.Values |> Seq.toArray).toEqual expected
                Vitest.expect(initial.Values |> Seq.toArray).toEqual originalEntries
        )

        Vitest.test (
            "the final incremental page merges without removing children from earlier pages",
            fun () ->
                let _, entry, initial = createFixture ()
                let originalEntries = initial.Values |> Seq.toArray
                let added = entry "studies/last.txt" false

                let finalPage: FileTreeCreator.FileTreeDirectoryPage = {
                    Entries = [| added |]
                    HasMore = false
                }

                let merged = FileTreeCreator.mergeFileTreeDirectoryPage finalPage.Entries initial

                Vitest.expect(merged.Count).toBe (initial.Count + 1)
                Vitest.expect(merged.[added.path]).toEqual added

                originalEntries
                |> Array.iter (fun previous -> Vitest.expect(merged.[previous.path]).toEqual previous)

                Vitest.expect(initial.Values |> Seq.toArray).toEqual originalEntries
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
                        let vault = ArcVault(TestHelpers.testWindow ())
                        vault.path <- Some rootPath
                        let! tree = vault.EnsureInitialFileTreeRootPage rootPath
                        let names = tree.Values |> Seq.map _.name |> Seq.toArray

                        Vitest.expect(tree.Count).toBe (3)
                        Vitest.expect(names |> Array.contains "README.md").toBe (true)
                        Vitest.expect(names |> Array.contains "dataset").toBe (true)
                        Vitest.expect(names |> Array.contains "file-0000.txt").toBe (false)
                        Vitest.expect(vault.fileTreeDirectoryHasMore.[""]).toBe false

                        tree.Values
                        |> Seq.iter (fun entry -> Vitest.expect(entry.largeObject).toEqual (None))
                    })
            }
        )

        Vitest.test (
            "a prefix read reports more entries only when an accepted entry follows",
            fileTreeCreatorTestOptions,
            fun () -> promise {
                do!
                    withTempDirectory (fun rootPath -> promise {
                        let studiesPath = join [| rootPath; "studies" |]
                        do! createDirectoryAtAsync studiesPath

                        for name in [ "entry-0.txt"; "entry-1.txt"; ".~$locked.xlsx" ] do
                            do! writeUtf8FileAsync (join [| studiesPath; name |]) "entry"

                        // The ignored Office lock file must not count as a further entry.
                        let! exactPage = FileTreeCreator.readFileTreeDirectoryPrefix rootPath "studies" 2

                        Vitest.expect(exactPage.Entries |> Array.map _.name |> Array.sort).toEqual [|
                            "entry-0.txt"
                            "entry-1.txt"
                        |]

                        Vitest.expect(exactPage.HasMore).toBe false

                        do! writeUtf8FileAsync (join [| studiesPath; "entry-2.txt" |]) "entry"

                        let! partialPage = FileTreeCreator.readFileTreeDirectoryPrefix rootPath "studies" 2
                        Vitest.expect(partialPage.Entries.Length).toBe 2
                        Vitest.expect(partialPage.HasMore).toBe true
                    })
            }
        )

        Vitest.test (
            "a directory read leaves out the staging folder of a running import",
            fileTreeCreatorTestOptions,
            fun () -> promise {
                do!
                    withTempDirectory (fun rootPath -> promise {
                        let datasetPath = join [| rootPath; "dataset" |]

                        let stagingPath =
                            join [|
                                datasetPath
                                Main.IPC.FileSystemIO.ArcFileSystemHelper.createTemporaryImportDirectoryName ()
                            |]

                        do! createDirectoryAtAsync stagingPath
                        do! writeUtf8FileAsync (join [| stagingPath; "staged.csv" |]) "staged"
                        do! writeUtf8FileAsync (join [| datasetPath; "kept.csv" |]) "kept"

                        let! page = FileTreeCreator.readFileTreeDirectoryPrefix rootPath "dataset" 100

                        Vitest.expect(page.Entries |> Array.map _.name).toEqual [| "kept.csv" |]
                        Vitest.expect(page.HasMore).toBe false
                    })
            }
        )

        Vitest.test (
            "cursor lookahead advances each underlying directory entry exactly once",
            fun () -> promise {
                let mutable readCalls = 0
                let mutable nextIndex = 0

                let dirents =
                    Array.init
                        5
                        (fun index ->
                            { new Dirent with
                                member _.name = $"entry-{index}"
                                member _.isDirectory() = false
                                member _.isFile() = true
                                member _.isSymbolicLink() = false
                            }
                        )

                let directory =
                    { new Directory with
                        member _.read() =
                            readCalls <- readCalls + 1

                            let result =
                                if nextIndex < dirents.Length then
                                    let dirent = dirents.[nextIndex]
                                    nextIndex <- nextIndex + 1
                                    dirent
                                else
                                    unbox null

                            JS.Constructors.Promise.resolve result

                        member _.close() = JS.Constructors.Promise.resolve ()
                    }

                let cursor: FileTreeCreator.FileTreeDirectoryCursor = {
                    DirectoryPath = "arc/studies"
                    Directory = directory
                    Lookahead = None
                }

                let! first = FileTreeCreator.readFileTreeDirectoryCursorPage cursor 2
                let! second = FileTreeCreator.readFileTreeDirectoryCursorPage cursor 2
                let! final = FileTreeCreator.readFileTreeDirectoryCursorPage cursor 2

                let names =
                    Array.concat [ first.Entries; second.Entries; final.Entries ]
                    |> Array.map _.name

                Vitest.expect(names).toEqual ([| "entry-0"; "entry-1"; "entry-2"; "entry-3"; "entry-4" |])
                Vitest.expect(readCalls).toBe 6
                Vitest.expect(final.HasMore).toBe false
            }
        )

        Vitest.test (
            "a prefix read with more entries hands over its open listing for the next page",
            fileTreeCreatorTestOptions,
            fun () -> promise {
                do!
                    withTempDirectory (fun rootPath -> promise {
                        let studiesPath = join [| rootPath; "studies" |]
                        do! createDirectoryAtAsync studiesPath

                        for index in 0..249 do
                            do! writeUtf8FileAsync (join [| studiesPath; sprintf "study-%03d" index |]) "study"

                        let! first, cursor =
                            FileTreeCreator.readFileTreeDirectoryPrefixWithCursor rootPath "studies" 100

                        Vitest.expect(first.HasMore).toBe true
                        Vitest.expect(cursor.IsSome).toBe true

                        let allPaths = ResizeArray(first.Entries |> Array.map _.path)
                        let mutable hasMore = first.HasMore

                        while hasMore do
                            let! page = FileTreeCreator.readFileTreeDirectoryCursorPage cursor.Value 100
                            allPaths.AddRange(page.Entries |> Array.map _.path)
                            hasMore <- page.HasMore

                        do! cursor.Value.Directory.close ()
                        Vitest.expect(allPaths |> Seq.distinct |> Seq.length).toBe 250

                        let! complete, completeCursor =
                            FileTreeCreator.readFileTreeDirectoryPrefixWithCursor rootPath "studies" 250

                        Vitest.expect(complete.HasMore).toBe false
                        Vitest.expect(completeCursor.IsNone).toBe true
                    })
            }
        )

        Vitest.test (
            "ten thousand directory entries are read in bounded prefixes",
            TestOptions(timeout = 120000),
            fun () -> promise {
                do!
                    withTempDirectory (fun rootPath -> promise {
                        let studiesPath = join [| rootPath; "studies" |]
                        do! createDirectoryAtAsync studiesPath

                        for index in 0..9999 do
                            do! writeUtf8FileAsync (join [| studiesPath; sprintf "study-%05d" index |]) "study"

                        let! first = FileTreeCreator.readFileTreeDirectoryPrefix rootPath "studies" 100
                        Vitest.expect(first.Entries.Length).toBe 100
                        Vitest.expect(first.HasMore).toBe true

                        let! almostComplete = FileTreeCreator.readFileTreeDirectoryPrefix rootPath "studies" 9999
                        Vitest.expect(almostComplete.Entries.Length).toBe 9999
                        Vitest.expect(almostComplete.HasMore).toBe true

                        let! complete = FileTreeCreator.readFileTreeDirectoryPrefix rootPath "studies" 10000
                        Vitest.expect(complete.Entries.Length).toBe 10000

                        Vitest.expect(complete.Entries |> Array.map _.path |> Array.distinct |> Array.length).toBe
                            10000

                        Vitest.expect(complete.HasMore).toBe false
                    })
            }
        )

        Vitest.test (
            "startup root loading is bounded and continues its listing through completion",
            fileTreeCreatorTestOptions,
            fun () -> promise {
                do!
                    withTempDirectory (fun rootPath -> promise {
                        let writes =
                            Array.init
                                250
                                (fun index ->
                                    writeUtf8FileAsync (join [| rootPath; sprintf "root-%03d.txt" index |]) "root"
                                )

                        let! _ = Fable.Core.JS.Constructors.Promise.all writes
                        let vault = ArcVault(TestHelpers.testWindow ())
                        vault.path <- Some rootPath

                        let! result =
                            promise {
                                let! initialTree = vault.EnsureInitialFileTreeRootPage rootPath

                                // One root entry plus exactly one bounded page of direct children.
                                Vitest.expect(initialTree.Count).toBe 101
                                Vitest.expect(vault.fileTreeDirectoryHasMore.[""]).toBe true
                                let cursor = vault.FileTreeDirectoryCursors.[""]

                                let! repeatedTree = vault.EnsureInitialFileTreeRootPage rootPath
                                Vitest.expect(repeatedTree).toBe initialTree
                                Vitest.expect(vault.FileTreeDirectoryCursors.[""]).toBe cursor

                                do! vault.LoadNextFileTreeDirectoryPage ""
                                Vitest.expect(vault.fileTree.Count).toBe 201
                                Vitest.expect(vault.fileTreeDirectoryHasMore.[""]).toBe true
                                Vitest.expect(vault.FileTreeDirectoryCursors.[""]).toBe cursor

                                do! vault.LoadNextFileTreeDirectoryPage ""
                                let! snapshot = vault.GetRendererFileTreeSnapshot()
                                Vitest.expect(snapshot.entries.Count).toBe 251
                                Vitest.expect(snapshot.directoryHasMore.[""]).toBe false
                                Vitest.expect(vault.FileTreeDirectoryCursors.Count).toBe 0

                                for index in 0..249 do
                                    Vitest.expect(snapshot.entries.ContainsKey(sprintf "root-%03d.txt" index)).toBe
                                        true
                            }
                            |> Promise.result

                        do! vault.StopFileWatcher()

                        match result with
                        | Ok() -> ()
                        | Error error -> return raise error
                    })
            }
        )

)
