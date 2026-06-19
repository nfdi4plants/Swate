module ElectronCore.ArcVaultHelperTests

open ARCtrl
open Main.ARCtrlExtensions
open Main.ArcVault
open Main.ArcVaultHelper
open Main.Bindings.Filesystem
open Main.Bindings.Path
open Swate.Components.Shared
open Vitest

let private mkdirRecursiveAsync (directoryPath: string) = promise {
    let! _ = mkdirAsync directoryPath (MkdirOptions(recursive = true))
    return ()
}

let private writeTextFileAsync (filePath: string) (content: string) =
    writeFileAsync filePath content TextEncoding.Utf8

let private addDataMapToAllEntityTypes (arc: ARC) =
    let study = ArcStudy("Study With DataMap")
    study.DataMap <- Some(DataMap.init ())
    arc.AddStudy(study)

    let assay = ArcAssay("Assay With DataMap")
    assay.DataMap <- Some(DataMap.init ())
    arc.AddAssay(assay)

    let workflow = ArcWorkflow("Workflow With DataMap")
    workflow.DataMap <- Some(DataMap.init ())
    arc.AddWorkflow(workflow)

    let run = ArcRun("Run With DataMap")
    run.DataMap <- Some(DataMap.init ())
    arc.AddRun(run)

Vitest.describe (
    "ArcVaultHelper",
    fun () ->
        Vitest.test (
            "file watcher polling defaults to Windows only",
            fun () ->
                Vitest.expect(shouldUsePollingByDefault "win32").toBe (true)
                Vitest.expect(shouldUsePollingByDefault "WIN32").toBe (true)
                Vitest.expect(shouldUsePollingByDefault "linux").toBe (false)
                Vitest.expect(shouldUsePollingByDefault "darwin").toBe (false)
        )

        Vitest.test (
            "Git metadata path detection excludes only exact .git path segments",
            fun () ->
                Vitest.expect(isGitMetadataPath ".git").toBe (true)
                Vitest.expect(isGitMetadataPath ".git/objects/ab/object").toBe (true)
                Vitest.expect(isGitMetadataPath "notes\\.GIT\\config").toBe (true)
                Vitest.expect(isGitMetadataPath ".gitignore").toBe (false)
                Vitest.expect(isGitMetadataPath ".gitattributes").toBe (false)
                Vitest.expect(isGitMetadataPath "notes/my.git/file.txt").toBe (false)
        )

        Vitest.test (
            "Swate write contracts contain only targeted ARC files",
            fun () ->
                let arc = ARC("TargetedWriteArc")
                addDataMapToAllEntityTypes arc
                arc.SetLicenseFulltext("license text")

                arc.SetFilePaths(
                    [|
                        ".git/config"
                        "payload.txt"
                        "missing-payload.txt"
                        "assays/Assay With DataMap/README.md"
                    |]
                )

                let actualPaths = arc.GetWriteContractsSwate() |> Array.map _.Path |> Array.sort

                let expectedPaths =
                    [|
                        "LICENSE"
                        "assays/.gitkeep"
                        "assays/Assay With DataMap/isa.assay.xlsx"
                        "assays/Assay With DataMap/isa.datamap.xlsx"
                        "isa.investigation.xlsx"
                        "runs/.gitkeep"
                        "runs/Run With DataMap/isa.datamap.xlsx"
                        "runs/Run With DataMap/isa.run.xlsx"
                        "studies/.gitkeep"
                        "studies/Study With DataMap/isa.datamap.xlsx"
                        "studies/Study With DataMap/isa.study.xlsx"
                        "workflows/.gitkeep"
                        "workflows/Workflow With DataMap/isa.datamap.xlsx"
                        "workflows/Workflow With DataMap/isa.workflow.xlsx"
                    |]
                    |> Array.sort

                Vitest.expect(actualPaths).toEqual (expectedPaths)
        )

        Vitest.test (
            "ArcScaffold writes the default gitignore into an ARC root",
            fun () -> promise {
                let! rootPath = TestHelpers.createTempDirectoryAsync "swate-default-gitignore-"
                let arcPath = join [| rootPath; "arc" |]
                let gitignorePath = join [| arcPath; ".gitignore" |]

                try
                    do! mkdirRecursiveAsync arcPath

                    match! Main.ArcScaffold.tryWriteDefaultGitignoreAsync arcPath with
                    | Error errors -> failwith (String.concat "\n" errors)
                    | Ok _ -> ()

                    let! gitignoreContent = readFileAsync gitignorePath TextEncoding.Utf8
                    Vitest.expect(gitignoreContent).toBe (Main.ArcScaffold.defaultGitignoreContent)
                    do! TestHelpers.removeDirectoryAsync rootPath
                with error ->
                    do! TestHelpers.removeDirectoryAsync rootPath
                    return raise error
            }
        )

        Vitest.test (
            "TryWriteAsyncSwate preserves payload and does not create unmanaged file-tree entries",
            fun () -> promise {
                let! rootPath = TestHelpers.createTempDirectoryAsync "swate-targeted-write-"
                let arcPath = join [| rootPath; "arc" |]
                let gitFolder = join [| arcPath; ".git" |]
                let assayFolder = join [| arcPath; "assays"; "Assay 1" |]
                let payloadPath = join [| arcPath; "payload.txt" |]
                let gitConfigPath = join [| gitFolder; "config" |]
                let readmePath = join [| assayFolder; "README.md" |]
                let missingPayloadPath = join [| arcPath; "missing-payload.txt" |]

                try
                    do! mkdirRecursiveAsync gitFolder
                    do! mkdirRecursiveAsync assayFolder
                    do! writeTextFileAsync payloadPath "payload"
                    do! writeTextFileAsync gitConfigPath "git-config"
                    do! writeTextFileAsync readmePath "readme"

                    let arc = ARC("TargetedWriteArc")
                    arc.AddAssay(ArcAssay("Assay 1"))

                    arc.SetFilePaths(
                        [|
                            ".git/config"
                            "payload.txt"
                            "missing-payload.txt"
                            "assays/Assay 1/README.md"
                        |]
                    )

                    match! arc.TryWriteAsyncSwate(arcPath) with
                    | Error errors -> failwith (String.concat "\n" errors)
                    | Ok _ -> ()

                    let! payload = readFileAsync payloadPath TextEncoding.Utf8
                    let! gitConfig = readFileAsync gitConfigPath TextEncoding.Utf8
                    let! readme = readFileAsync readmePath TextEncoding.Utf8
                    let! missingPayloadExists = TestHelpers.pathExistsAsync missingPayloadPath

                    let! assayFileExists = TestHelpers.pathExistsAsync (join [| assayFolder; "isa.assay.xlsx" |])

                    let! collectionGitKeepExists =
                        TestHelpers.pathExistsAsync (join [| arcPath; "assays"; ".gitkeep" |])

                    Vitest.expect(payload).toBe ("payload")
                    Vitest.expect(gitConfig).toBe ("git-config")
                    Vitest.expect(readme).toBe ("readme")
                    Vitest.expect(missingPayloadExists).toBe (false)
                    Vitest.expect(assayFileExists).toBe (true)
                    Vitest.expect(collectionGitKeepExists).toBe (true)
                    do! TestHelpers.removeDirectoryAsync rootPath
                with error ->
                    do! TestHelpers.removeDirectoryAsync rootPath
                    return raise error
            }
        )

        Vitest.test (
            "ARC vault save preserves note markdown files",
            fun () ->
                TestHelpers.withTempArcWith
                    "swate-save-notes-"
                    "NotesPreservingArc"
                    ignore
                    (fun arcPath -> promise {
                        let rootNotesFolder = join [| arcPath; "notes"; "2026-04-27"; "root_note" |]
                        let studyNotesFolder = join [| arcPath; "notes"; "2026-04-27"; "study_note" |]
                        let rootNotePath = join [| rootNotesFolder; "root_note.md" |]
                        let studyNotePath = join [| studyNotesFolder; "study_note.md" |]

                        let rootNoteContent = "---\ntitle: Root note\n---\n\nRoot note body."
                        let studyNoteContent = "---\ntitle: Study note\n---\n\nStudy note body."

                        do! mkdirRecursiveAsync rootNotesFolder
                        do! mkdirRecursiveAsync studyNotesFolder
                        do! writeTextFileAsync rootNotePath rootNoteContent
                        do! writeTextFileAsync studyNotePath studyNoteContent

                        let vault = ArcVault(TestHelpers.testWindow ())
                        vault.path <- Some arcPath
                        do! vault.LoadArc()

                        vault.arc.Value.Title <- Some "Saved title"
                        vault.arc.Value.StaticHash <- 0

                        match! vault.WriteArc() with
                        | Error error -> failwith error.Message
                        | Ok() -> ()

                        let! rootNoteAfterSave = readFileAsync rootNotePath TextEncoding.Utf8
                        let! studyNoteAfterSave = readFileAsync studyNotePath TextEncoding.Utf8

                        Vitest.expect(rootNoteAfterSave).toBe (rootNoteContent)
                        Vitest.expect(studyNoteAfterSave).toBe (studyNoteContent)
                    })
        )

        Vitest.test (
            "ARC loading and writing ignore Git metadata and preserve payload",
            fun () ->
                TestHelpers.withTempArcWith
                    "swate-load-arc-ignore-git-"
                    "IgnoreGitArc"
                    ignore
                    (fun arcPath -> promise {
                        let gitObjectFolder = join [| arcPath; ".git"; "objects"; "ab" |]
                        let nestedGitFolder = join [| arcPath; "notes"; ".git" |]
                        let payloadPath = join [| arcPath; "payload.txt" |]
                        let gitObjectPath = join [| gitObjectFolder; "object" |]
                        do! mkdirRecursiveAsync gitObjectFolder
                        do! mkdirRecursiveAsync nestedGitFolder
                        do! writeTextFileAsync gitObjectPath "git-object"
                        do! writeTextFileAsync (join [| nestedGitFolder; "config" |]) "nested-git-config"
                        do! writeTextFileAsync payloadPath "payload"

                        let! loadResult = ARC.LoadAsyncSwate arcPath
                        let loadedArc = TestHelpers.expectLoadedArc loadResult
                        let paths = loadedArc.FileSystem.Tree.ToFilePaths()

                        Vitest.expect(paths |> Array.exists isGitMetadataPath).toBe (false)

                        loadedArc.SetFilePaths(Array.append paths [| ".git/objects/ab/object" |])
                        loadedArc.Title <- Some "Saved title"
                        do! loadedArc.UpdateAsync arcPath

                        let! payload = readFileAsync payloadPath TextEncoding.Utf8
                        let! gitObject = readFileAsync gitObjectPath TextEncoding.Utf8
                        Vitest.expect(payload).toBe ("payload")
                        Vitest.expect(gitObject).toBe ("git-object")
                    })
        )

        Vitest.test (
            "normal ARC save does not restore deleted DTO-less files from a stale file tree",
            fun () ->
                TestHelpers.withTempArcWith
                    "swate-save-deleted-file-"
                    "DeletedFileArc"
                    ignore
                    (fun arcPath -> promise {
                        let payloadPath = join [| arcPath; "payload.txt" |]
                        let collectionGitKeepPath = join [| arcPath; "assays"; ".gitkeep" |]
                        do! writeTextFileAsync payloadPath "payload"

                        let! loadResult = ARC.LoadAsyncSwate arcPath
                        let loadedArc = TestHelpers.expectLoadedArc loadResult
                        let stalePaths = loadedArc.FileSystem.Tree.ToFilePaths()

                        Vitest.expect(stalePaths |> Array.contains "payload.txt").toBe (true)
                        Vitest.expect(stalePaths |> Array.contains "assays/.gitkeep").toBe (true)

                        do! rmAsync payloadPath (RmOptions())
                        do! rmAsync collectionGitKeepPath (RmOptions())

                        loadedArc.Title <- Some "Saved title"
                        do! loadedArc.UpdateAsync arcPath

                        let! payloadExists = TestHelpers.pathExistsAsync payloadPath
                        let! collectionGitKeepExists = TestHelpers.pathExistsAsync collectionGitKeepPath
                        Vitest.expect(payloadExists).toBe (false)
                        Vitest.expect(collectionGitKeepExists).toBe (false)

                        let! reloadedArc = TestHelpers.loadArcAsync arcPath
                        Vitest.expect(reloadedArc.Title).toEqual (Some "Saved title")
                    })
        )

        Vitest.test (
            "LoadArc reports load errors without crashing the printf formatter",
            fun () -> promise {
                let! rootPath = TestHelpers.createTempDirectoryAsync "swate-load-arc-error-"

                try
                    let vault = ArcVault(TestHelpers.testWindow ())
                    vault.path <- Some rootPath

                    let mutable capturedError: exn option = None

                    try
                        do! vault.LoadArc()
                    with error ->
                        capturedError <- Some error

                    match capturedError with
                    | None ->
                        do! TestHelpers.removeDirectoryAsync rootPath
                        return failwith "Expected LoadArc to fail for an invalid ARC folder."
                    | Some error ->
                        Vitest.expect(error.Message).toContain ("[Swate-0] Unable to load ARC:")
                        Vitest.expect(error.Message).not.toContain ("fmt.cont")
                        do! TestHelpers.removeDirectoryAsync rootPath
                with error ->
                    do! TestHelpers.removeDirectoryAsync rootPath
                    return raise error
            }
        )

        Vitest.test (
            "RenameOpenArcRoot moves the active ARC folder and updates the vault path",
            fun () ->
                TestHelpers.withTempArcWith
                    "swate-rename-arc-root-"
                    "RenameRootArc"
                    ignore
                    (fun arcPath -> promise {
                        let targetPath =
                            join [| dirname arcPath; "renamed-arc" |] |> PathHelpers.normalizePath

                        let vault = ArcVault(TestHelpers.testWindow ())
                        vault.path <- Some arcPath

                        do! vault.LoadArc()

                        match! vault.RenameOpenArcRoot "renamed-arc" with
                        | Error error -> failwith error.Message
                        | Ok renamedPath ->
                            Vitest.expect(renamedPath).toBe (targetPath)
                            Vitest.expect(vault.path).toEqual (Some targetPath)

                            let! oldPathExists = TestHelpers.pathExistsAsync arcPath
                            let! newPathExists = TestHelpers.pathExistsAsync targetPath
                            Vitest.expect(oldPathExists).toBe (false)
                            Vitest.expect(newPathExists).toBe (true)

                            let! reloadedArc = TestHelpers.loadArcAsync targetPath
                            Vitest.expect(reloadedArc.Identifier).toBe ("RenameRootArc")
                    })
        )

        Vitest.test (
            "RenameOpenArcRoot clears pending watcher state before moving the active ARC",
            fun () ->
                TestHelpers.withTempArcWith
                    "swate-rename-arc-root-watcher-"
                    "RenameRootWatcherArc"
                    ignore
                    (fun arcPath -> promise {
                        let vault = ArcVault(TestHelpers.testWindow ())
                        vault.path <- Some arcPath
                        do! vault.LoadArc()

                        let timeoutId = Fable.Core.JS.setTimeout (fun () -> ()) 60000

                        vault.fileWatcherReloadArcTimeout <- Some timeoutId

                        vault.fileWatcherPendingEvents.Add {
                            EventName = "change"
                            RelativePath = "isa.investigation.xlsx"
                            AbsolutePath = join [| arcPath; "isa.investigation.xlsx" |]
                        }

                        vault.fileWatcherPendingArcMergeEvents.Add {
                            EventName = "change"
                            RelativePath = "isa.investigation.xlsx"
                            AbsolutePath = join [| arcPath; "isa.investigation.xlsx" |]
                        }

                        try
                            match! vault.RenameOpenArcRoot "renamed-arc-watcher" with
                            | Error error -> failwith error.Message
                            | Ok _ ->
                                Vitest.expect(vault.fileWatcherReloadArcTimeout).toEqual (None)
                                Vitest.expect(vault.fileWatcherPendingEvents.Count).toBe (0)
                                Vitest.expect(vault.fileWatcherPendingArcMergeEvents.Count).toBe (0)
                        finally
                            vault.fileWatcherReloadArcTimeout |> Option.iter Fable.Core.JS.clearTimeout
                    })
        )

        Vitest.test (
            "RenameOpenArcRoot rejects destination conflicts without moving the active ARC",
            fun () ->
                TestHelpers.withTempArcWith
                    "swate-rename-arc-root-conflict-"
                    "RenameRootConflictArc"
                    ignore
                    (fun arcPath -> promise {
                        let targetPath =
                            join [| dirname arcPath; "existing-arc" |] |> PathHelpers.normalizePath

                        do! mkdirRecursiveAsync targetPath

                        let vault = ArcVault(TestHelpers.testWindow ())
                        vault.path <- Some arcPath
                        do! vault.LoadArc()

                        match! vault.RenameOpenArcRoot "existing-arc" with
                        | Ok _ -> failwith "Expected active ARC root rename to reject an existing destination."
                        | Error error ->
                            Vitest.expect(error.Message).toContain ("destination already exists")
                            Vitest.expect(vault.path).toEqual (Some arcPath)

                            let! oldPathExists = TestHelpers.pathExistsAsync arcPath
                            let! targetPathExists = TestHelpers.pathExistsAsync targetPath
                            Vitest.expect(oldPathExists).toBe (true)
                            Vitest.expect(targetPathExists).toBe (true)
                    })
        )

        Vitest.test (
            "tryBuildOpenArcRootRenamePlan applies the shared rename-name validation rules",
            fun () ->
                match tryBuildOpenArcRootRenamePlan "C:/work/current-arc" "bad\u0000name" with
                | Ok _ -> failwith "Expected ARC root rename to reject null characters."
                | Error error -> Vitest.expect(error.Message).toContain ("null")
        )

        Vitest.test (
            "LoadArc repairs zero-byte canonical ARC workbooks before retrying",
            fun () ->
                TestHelpers.withTempArcWith
                    "swate-load-arc-repair-"
                    "RepairArc"
                    ignore
                    (fun arcPath -> promise {
                        let assayFolder = join [| arcPath; "assays"; "New Assay" |]
                        let assayFile = join [| assayFolder; "isa.assay.xlsx" |]

                        do! mkdirRecursiveAsync assayFolder
                        do! writeTextFileAsync assayFile ""

                        let vault = ArcVault(TestHelpers.testWindow ())
                        vault.path <- Some arcPath

                        do! vault.LoadArc()

                        Vitest.expect(vault.arc.IsSome).toBe (true)
                        Vitest.expect(vault.arc.Value.ContainsAssay("New Assay")).toBe (true)
                        Vitest.expect(vault.hasUnsavedArcChanges).toBe (false)
                        Vitest.expect(vault.arc.Value.hasInMemoryChanges ()).toBe (false)

                        let! reloadedArc = TestHelpers.loadArcAsync arcPath
                        Vitest.expect(reloadedArc.ContainsAssay("New Assay")).toBe (true)
                    })
        )

        Vitest.test (
            "LoadArc baselines loaded datamap hashes without marking the ARC dirty",
            fun () ->
                TestHelpers.withTempArcWith
                    "swate-load-arc-datamap-baseline-"
                    "DatamapBaselineArc"
                    addDataMapToAllEntityTypes
                    (fun arcPath -> promise {
                        let vault = ArcVault(TestHelpers.testWindow ())
                        vault.path <- Some arcPath

                        do! vault.LoadArc()

                        Vitest.expect(vault.arc.IsSome).toBe (true)
                        Vitest.expect(vault.hasUnsavedArcChanges).toBe (false)
                        Vitest.expect(vault.arc.Value.hasInMemoryChanges ()).toBe (false)

                        let loadedArc = vault.arc.Value
                        Vitest.expect(loadedArc.GetAssay("Assay With DataMap").DataMap.Value.StaticHash).not.toBe (0)
                        Vitest.expect(loadedArc.GetStudy("Study With DataMap").DataMap.Value.StaticHash).not.toBe (0)

                        Vitest
                            .expect(loadedArc.GetWorkflow("Workflow With DataMap").DataMap.Value.StaticHash)
                            .not.toBe (0)

                        Vitest.expect(loadedArc.GetRun("Run With DataMap").DataMap.Value.StaticHash).not.toBe (0)
                    })
        )
)
