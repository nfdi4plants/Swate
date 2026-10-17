module ElectronCore.NotesTests

open ARCtrl
open Fable.Core
open Fable.Core.JsInterop
open Main.Notes.NoteScaffolding
open Main.ArcVault
open Main.IPC.ArcVaultsApi
open Swate.Components.Shared
open Vitest

let private fsPromisesDynamic: obj = importAll "fs/promises"
let private osDynamic: obj = importAll "os"
let private pathDynamic: obj = importAll "path"

let private createTempArcPathAsync () : JS.Promise<string * string> = promise {
    let tmpDir = osDynamic?tmpdir () |> unbox<string>
    let prefix = pathDynamic?join (tmpDir, "swate-notes-folder-") |> unbox<string>
    let! rootPath = fsPromisesDynamic?mkdtemp (prefix) |> unbox<JS.Promise<string>>
    let arcPath = pathDynamic?join (rootPath, "arc") |> unbox<string>

    let! _ =
        fsPromisesDynamic?mkdir (arcPath, createObj [ "recursive" ==> true ])
        |> unbox<JS.Promise<obj>>

    return rootPath, arcPath
}

let private removePathAsync (path: string) : JS.Promise<unit> = promise {
    let! _ =
        fsPromisesDynamic?rm (path, createObj [ "recursive" ==> true; "force" ==> true ])
        |> unbox<JS.Promise<obj>>

    return ()
}

let private pathExistsAsync (path: string) : JS.Promise<bool> = promise {
    try
        let! _ = fsPromisesDynamic?stat (path) |> unbox<JS.Promise<obj>>
        return true
    with _ ->
        return false
}

let private readUtf8FileAsync (path: string) : JS.Promise<string> =
    fsPromisesDynamic?readFile (path, "utf8") |> unbox<JS.Promise<string>>

let private writeUtf8FileAsync (path: string) (content: string) : JS.Promise<unit> = promise {
    let! _ = fsPromisesDynamic?writeFile (path, content, "utf8") |> unbox<JS.Promise<obj>>

    return ()
}

let private assertEnsureSucceeded (result: Result<unit, exn>) =
    match result with
    | Ok() -> ()
    | Error error -> failwith error.Message

Vitest.describe (
    "ensureNotesFolderAtArcPath",
    fun () ->
        Vitest.test (
            "creates notes folder and README on first call",
            fun () -> promise {
                let! rootPath, arcPath = createTempArcPathAsync ()

                try
                    let notesFolderPath = pathDynamic?join (arcPath, "notes") |> unbox<string>

                    let notesReadmePath =
                        pathDynamic?join (notesFolderPath, "README.md") |> unbox<string>

                    let! ensureResult = ensureNotesFolderAtArcPath arcPath
                    assertEnsureSucceeded ensureResult

                    let! hasNotesFolder = pathExistsAsync notesFolderPath
                    let! hasNotesReadme = pathExistsAsync notesReadmePath
                    let! readmeContent = readUtf8FileAsync notesReadmePath

                    Vitest.expect(hasNotesFolder).toBe (true)
                    Vitest.expect(hasNotesReadme).toBe (true)
                    Vitest.expect(readmeContent.Contains("Automatically create notes folder")).toBe (true)
                    Vitest.expect(readmeContent.Contains("Swate Settings")).toBe (true)
                    do! removePathAsync rootPath
                with error ->
                    do! removePathAsync rootPath
                    return raise error
            }
        )

        Vitest.test (
            "does not create README if notes folder already existed",
            fun () -> promise {
                let! rootPath, arcPath = createTempArcPathAsync ()

                try
                    let notesFolderPath = pathDynamic?join (arcPath, "notes") |> unbox<string>

                    let notesReadmePath =
                        pathDynamic?join (notesFolderPath, "README.md") |> unbox<string>

                    let! _ =
                        fsPromisesDynamic?mkdir (notesFolderPath, createObj [ "recursive" ==> true ])
                        |> unbox<JS.Promise<obj>>

                    let! ensureResult = ensureNotesFolderAtArcPath arcPath
                    assertEnsureSucceeded ensureResult

                    let! hasNotesReadme = pathExistsAsync notesReadmePath
                    Vitest.expect(hasNotesReadme).toBe (false)
                    do! removePathAsync rootPath
                with error ->
                    do! removePathAsync rootPath
                    return raise error
            }
        )

        Vitest.test (
            "does not overwrite README when called repeatedly",
            fun () -> promise {
                let! rootPath, arcPath = createTempArcPathAsync ()

                try
                    let notesFolderPath = pathDynamic?join (arcPath, "notes") |> unbox<string>

                    let notesReadmePath =
                        pathDynamic?join (notesFolderPath, "README.md") |> unbox<string>

                    let customContent = "custom readme content"

                    let! firstEnsureResult = ensureNotesFolderAtArcPath arcPath
                    assertEnsureSucceeded firstEnsureResult

                    do! writeUtf8FileAsync notesReadmePath customContent

                    let! secondEnsureResult = ensureNotesFolderAtArcPath arcPath
                    assertEnsureSucceeded secondEnsureResult

                    let! finalContent = readUtf8FileAsync notesReadmePath
                    Vitest.expect(finalContent).toBe (customContent)
                    do! removePathAsync rootPath
                with error ->
                    do! removePathAsync rootPath
                    return raise error
            }
        )

        Vitest.test (
            "refreshes the root FileTree after creating notes",
            fun () -> promise {
                let! rootPath, arcPath = createTempArcPathAsync ()

                try
                    let vault = ArcVault(TestHelpers.testWindow ())
                    vault.path <- Some arcPath
                    do! vault.EnsureInitialFileTreeRootPage arcPath |> Promise.map ignore

                    let! ensureResult = ensureNotesFolderAndRefreshFileTree vault arcPath
                    assertEnsureSucceeded ensureResult

                    let notesFolderPath = pathDynamic?join (arcPath, "notes") |> unbox<string>
                    let! hasNotesFolder = pathExistsAsync notesFolderPath

                    let treeHasNotes =
                        vault.fileTree.Values
                        |> Seq.exists (fun entry -> entry.isDirectory && entry.name = "notes")

                    Vitest.expect(hasNotesFolder).toBe (true)
                    Vitest.expect(treeHasNotes).toBe (true)
                    do! removePathAsync rootPath
                with error ->
                    do! removePathAsync rootPath
                    return raise error
            }
        )
)

let private windowId = 7181

let private expectTargets (result: Result<ExistingTargetRef[], exn>) =
    match result with
    | Ok targets -> targets
    | Error error -> failwith error.Message

Vitest.describe (
    "note targets",
    fun () ->
        Vitest.afterEach (fun () ->
            ARC_VAULTS.Vaults.Remove windowId |> ignore
            TestHelpers.electronMock?reset () |> ignore
        )

        Vitest.test (
            "note targets include studies and assays without loading their File Explorer directories",
            fun () -> promise {
                let arc = ARC("Notes")
                arc.AddStudy(ArcStudy("Study"))
                arc.AddAssay(ArcAssay("Assay"))
                arc.AddWorkflow(ArcWorkflow("Workflow"))
                let vault = TestHelpers.registerVault windowId "C:/notes-targets"
                vault.SetArc arc
                let api = Main.IPC.ArcVaultsApi.api (TestHelpers.ipcEvent windowId)

                let! result = api.getNoteTargets ()

                Vitest.expect(expectTargets result).toEqual [|
                    {
                        Name = "Study"
                        Kind = NotesTargetKind.Study
                    }
                    {
                        Name = "Assay"
                        Kind = NotesTargetKind.Assay
                    }
                |]

                Vitest.expect(vault.fileTree.Count).toBe 0
                Vitest.expect(vault.loadedFileTreeDirectories.Count).toBe 0

                arc.AddAssay(ArcAssay("AnotherAssay"))
                let! updatedResult = api.getNoteTargets ()
                Vitest.expect((expectTargets updatedResult).Length).toBe 3
            }
        )

        Vitest.test (
            "note targets wait for ARC initialization instead of using a partial file tree",
            fun () -> promise {
                let vault = TestHelpers.registerVault windowId "C:/loading-note-targets"
                let ready, finishInitialization = TestHelpers.deferred ()

                vault.ArcInitialization <-
                    Some(
                        promise {
                            do! ready
                            let arc = ARC("Notes")
                            arc.AddAssay(ArcAssay("LoadedAssay"))
                            vault.SetArc arc
                        }
                    )

                let api = Main.IPC.ArcVaultsApi.api (TestHelpers.ipcEvent windowId)
                let mutable completed = false

                let requested = promise {
                    let! result = api.getNoteTargets ()
                    completed <- true
                    return expectTargets result
                }

                do! Promise.sleep 0
                Vitest.expect(completed).toBe false
                finishInitialization ()
                let! targets = requested

                Vitest.expect(targets).toEqual [|
                    {
                        Name = "LoadedAssay"
                        Kind = NotesTargetKind.Assay
                    }
                |]
            }
        )
)
