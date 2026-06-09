module ElectronCore.EnsureNotesFolderTests

open Fable.Core
open Fable.Core.JsInterop
open Main.IPC.ArcVaultsApi
open Vitest

let private fsPromisesDynamic: obj = importAll "fs/promises"
let private osDynamic: obj = importAll "os"
let private pathDynamic: obj = importAll "path"

let private createTempArcPathAsync () : JS.Promise<string * string> =
    promise {
        let tmpDir = osDynamic?tmpdir () |> unbox<string>
        let prefix = pathDynamic?join (tmpDir, "swate-notes-folder-") |> unbox<string>
        let! rootPath = fsPromisesDynamic?mkdtemp (prefix) |> unbox<JS.Promise<string>>
        let arcPath = pathDynamic?join (rootPath, "arc") |> unbox<string>

        let! _ =
            fsPromisesDynamic?mkdir (arcPath, createObj [ "recursive" ==> true ])
            |> unbox<JS.Promise<obj>>

        return rootPath, arcPath
    }

let private removePathAsync (path: string) : JS.Promise<unit> =
    promise {
        let! _ =
            fsPromisesDynamic?rm (path, createObj [ "recursive" ==> true; "force" ==> true ])
            |> unbox<JS.Promise<obj>>

        return ()
    }

let private pathExistsAsync (path: string) : JS.Promise<bool> =
    promise {
        try
            let! _ = fsPromisesDynamic?stat (path) |> unbox<JS.Promise<obj>>
            return true
        with _ ->
            return false
    }

let private readUtf8FileAsync (path: string) : JS.Promise<string> =
    fsPromisesDynamic?readFile (path, "utf8") |> unbox<JS.Promise<string>>

let private writeUtf8FileAsync (path: string) (content: string) : JS.Promise<unit> =
    promise {
        let! _ =
            fsPromisesDynamic?writeFile (path, content, "utf8")
            |> unbox<JS.Promise<obj>>

        return ()
    }

let private assertEnsureSucceeded (result: Result<unit, exn>) =
    match result with
    | Ok() -> ()
    | Error error -> failwith error.Message

Vitest.describe("ensureNotesFolderAtArcPath", fun () ->
    Vitest.test("creates notes folder and README on first call", fun () -> promise {
        let! rootPath, arcPath = createTempArcPathAsync ()

        try
            let notesFolderPath = pathDynamic?join (arcPath, "notes") |> unbox<string>
            let notesReadmePath = pathDynamic?join (notesFolderPath, "README.md") |> unbox<string>

            let! ensureResult = ensureNotesFolderAtArcPath arcPath
            assertEnsureSucceeded ensureResult

            let! hasNotesFolder = pathExistsAsync notesFolderPath
            let! hasNotesReadme = pathExistsAsync notesReadmePath
            let! readmeContent = readUtf8FileAsync notesReadmePath

            Vitest.expect(hasNotesFolder).toBe(true)
            Vitest.expect(hasNotesReadme).toBe(true)
            Vitest.expect(readmeContent.Contains("Automatically create notes folder")).toBe(true)
            Vitest.expect(readmeContent.Contains("Swate Settings")).toBe(true)
            do! removePathAsync rootPath
        with error ->
            do! removePathAsync rootPath
            return raise error
    })

    Vitest.test("does not create README if notes folder already existed", fun () -> promise {
        let! rootPath, arcPath = createTempArcPathAsync ()

        try
            let notesFolderPath = pathDynamic?join (arcPath, "notes") |> unbox<string>
            let notesReadmePath = pathDynamic?join (notesFolderPath, "README.md") |> unbox<string>

            let! _ =
                fsPromisesDynamic?mkdir (notesFolderPath, createObj [ "recursive" ==> true ])
                |> unbox<JS.Promise<obj>>

            let! ensureResult = ensureNotesFolderAtArcPath arcPath
            assertEnsureSucceeded ensureResult

            let! hasNotesReadme = pathExistsAsync notesReadmePath
            Vitest.expect(hasNotesReadme).toBe(false)
            do! removePathAsync rootPath
        with error ->
            do! removePathAsync rootPath
            return raise error
    })

    Vitest.test("does not overwrite README when called repeatedly", fun () -> promise {
        let! rootPath, arcPath = createTempArcPathAsync ()

        try
            let notesFolderPath = pathDynamic?join (arcPath, "notes") |> unbox<string>
            let notesReadmePath = pathDynamic?join (notesFolderPath, "README.md") |> unbox<string>
            let customContent = "custom readme content"

            let! firstEnsureResult = ensureNotesFolderAtArcPath arcPath
            assertEnsureSucceeded firstEnsureResult

            do! writeUtf8FileAsync notesReadmePath customContent

            let! secondEnsureResult = ensureNotesFolderAtArcPath arcPath
            assertEnsureSucceeded secondEnsureResult

            let! finalContent = readUtf8FileAsync notesReadmePath
            Vitest.expect(finalContent).toBe(customContent)
            do! removePathAsync rootPath
        with error ->
            do! removePathAsync rootPath
            return raise error
    })
)
