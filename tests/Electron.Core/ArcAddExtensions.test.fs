module ElectronCore.ArcAddExtensionsTests

open Fable.Core
open Fable.Core.JsInterop
open Fable.Electron
open Fable.Electron.Main
open Main.ArcMerge
open Main.ArcVault
open Main.Bindings.Path
open Swate.Components.Shared
open Swate.Electron.Shared.FileIOHelper
open ARCtrl
open ARCtrl.Contract
open Vitest

let private fsPromisesDynamic: obj = importAll "fs/promises"
let private osDynamic: obj = importAll "os"

let private expectLoadedArc (result: Result<ARC, string[]>) =
    match result with
    | Ok arc -> arc
    | Error errors -> failwith (errors |> String.concat "\n")

let private expectSome (value: 'T option) (message: string) : 'T =
    match value with
    | Some value -> value
    | None -> failwith message

let private createTempDirectoryAsync () : JS.Promise<string> =
    let prefix =
        join [|
            osDynamic?tmpdir () |> unbox<string>
            "swate-addasync-"
        |]

    fsPromisesDynamic?mkdtemp (prefix) |> unbox<JS.Promise<string>>

let private removeDirectoryAsync (path: string) : JS.Promise<unit> = promise {
    let! _ =
        fsPromisesDynamic?rm (path, createObj [ "recursive" ==> true; "force" ==> true ])
        |> unbox<JS.Promise<obj>>

    return ()
}

let private pathExistsAsync (path: string) : JS.Promise<bool> = promise {
    try
        let! _ = fsPromisesDynamic?access (path) |> unbox<JS.Promise<obj>>
        return true
    with _ ->
        return false
}

let private withTempArc (seedArc: ARC -> unit) (testBody: string -> JS.Promise<unit>) : JS.Promise<unit> = promise {
    let! rootPath = createTempDirectoryAsync ()
    let arcPath = join [| rootPath; "arc" |]

    try
        let arc = ARC("AddAsyncArc")
        seedArc arc
        do! arc.WriteAsync arcPath
        do! testBody arcPath
        do! removeDirectoryAsync rootPath
    with error ->
        do! removeDirectoryAsync rootPath
        return raise error
}

let private loadArcAsync (arcPath: string) : JS.Promise<ARC> = promise {
    let! loaded = ARC.tryLoadAsync arcPath
    return expectLoadedArc loaded
}

let private testWindow () =
    createObj [
        "id" ==> 0
        "title" ==> ""
    ]
    |> unbox<BrowserWindow>

Vitest.describe("ARC AddAsync", fun () ->
    Vitest.test("adds an assay and creates the canonical assay file", fun () ->
        withTempArc ignore (fun arcPath -> promise {
            let! arc = loadArcAsync arcPath
            do! arc.AddAsync(arcPath, ArcFiles.Assay(ArcAssay("NewAssay")))

            let canonicalPath = join [| arcPath; "assays"; "NewAssay"; "isa.assay.xlsx" |]
            let! exists = pathExistsAsync canonicalPath
            Vitest.expect(exists).toBe(true)

            let! reloadedArc = loadArcAsync arcPath
            Vitest.expect(reloadedArc.ContainsAssay("NewAssay")).toBe(true)
        }))

    Vitest.test("adds a study and creates the canonical study file", fun () ->
        withTempArc ignore (fun arcPath -> promise {
            let! arc = loadArcAsync arcPath
            do! arc.AddAsync(arcPath, ArcFiles.Study(ArcStudy("NewStudy"), []))

            let canonicalPath = join [| arcPath; "studies"; "NewStudy"; "isa.study.xlsx" |]
            let! exists = pathExistsAsync canonicalPath
            Vitest.expect(exists).toBe(true)

            let! reloadedArc = loadArcAsync arcPath
            Vitest.expect(reloadedArc.ContainsStudy("NewStudy")).toBe(true)
        }))

    Vitest.test("adds a run and creates the canonical run file", fun () ->
        withTempArc ignore (fun arcPath -> promise {
            let! arc = loadArcAsync arcPath
            do! arc.AddAsync(arcPath, ArcFiles.Run(ArcRun("NewRun")))

            let canonicalPath = join [| arcPath; "runs"; "NewRun"; "isa.run.xlsx" |]
            let! exists = pathExistsAsync canonicalPath
            Vitest.expect(exists).toBe(true)

            let! reloadedArc = loadArcAsync arcPath
            Vitest.expect(reloadedArc.ContainsRun("NewRun")).toBe(true)
        }))

    Vitest.test("adds a workflow and creates the canonical workflow file", fun () ->
        withTempArc ignore (fun arcPath -> promise {
            let! arc = loadArcAsync arcPath
            do! arc.AddAsync(arcPath, ArcFiles.Workflow(ArcWorkflow("NewWorkflow")))

            let canonicalPath = join [| arcPath; "workflows"; "NewWorkflow"; "isa.workflow.xlsx" |]
            let! exists = pathExistsAsync canonicalPath
            Vitest.expect(exists).toBe(true)

            let! reloadedArc = loadArcAsync arcPath
            Vitest.expect(reloadedArc.ContainsWorkflow("NewWorkflow")).toBe(true)
        }))

    Vitest.test("rejects duplicate entity identifiers", fun () ->
        withTempArc (fun arc -> arc.AddAssay(ArcAssay("ExistingAssay"))) (fun arcPath -> promise {
            let! arc = loadArcAsync arcPath
            let! addResult = arc.TryAddAsync(arcPath, ArcFiles.Assay(ArcAssay("ExistingAssay")))

            match addResult with
            | Ok _ -> failwith "Expected duplicate assay add to fail."
            | Error errors ->
                Vitest.expect(errors.[0]).toContain("already contains assay")
        }))

    Vitest.test("rejects unsupported ARC file kinds", fun () ->
        withTempArc ignore (fun arcPath -> promise {
            let! arc = loadArcAsync arcPath

            let unsupportedArcFiles = [|
                ArcFiles.Investigation(ArcInvestigation("Investigation")), "investigation"
                ArcFiles.DataMap(None, DataMap.init()), "datamap"
                ArcFiles.Template(Template.init "Template"), "template"
            |]

            for arcFile, expectedMessage in unsupportedArcFiles do
                let! addResult = arc.TryAddAsync(arcPath, arcFile)

                match addResult with
                | Ok _ -> failwith $"Expected {expectedMessage} add to fail."
                | Error errors -> Vitest.expect(errors.[0]).toContain(expectedMessage)
        }))

    Vitest.test("keeps existing entities when adding a new one", fun () ->
        withTempArc (fun arc -> arc.AddStudy(ArcStudy("ExistingStudy"))) (fun arcPath -> promise {
            let! arc = loadArcAsync arcPath
            do! arc.AddAsync(arcPath, ArcFiles.Assay(ArcAssay("NewAssay")))

            let! reloadedArc = loadArcAsync arcPath
            Vitest.expect(reloadedArc.ContainsStudy("ExistingStudy")).toBe(true)
            Vitest.expect(reloadedArc.ContainsAssay("NewAssay")).toBe(true)
        }))

    Vitest.test("ApplyArcFileAndSave adds new entities and updates existing entities through the same IPC-facing path", fun () ->
        withTempArc (fun arc -> arc.AddAssay(ArcAssay("ExistingAssay", title = "Old title"))) (fun arcPath -> promise {
            let! loadedArc = loadArcAsync arcPath
            let dirtyAssay = loadedArc.GetAssay("ExistingAssay")
            dirtyAssay.Title <- Some "Unsaved local title"

            let vault = ArcVault(testWindow ())
            vault.path <- Some arcPath
            vault.SetArc loadedArc
            vault.SetHasUnsavedArcChanges true

            let newAssayRequest =
                FileContentDTO.fromArcFile(ArcFiles.Assay(ArcAssay("NewAssay")))
                |> expectSome <| "Expected new assay DTO."

            match! vault.ApplyArcFileAndSave newAssayRequest with
            | Error error -> failwith error.Message
            | Ok() -> ()

            let! reloadedAfterAdd = loadArcAsync arcPath
            Vitest.expect(reloadedAfterAdd.ContainsAssay("NewAssay")).toBe(true)
            Vitest.expect(reloadedAfterAdd.GetAssay("ExistingAssay").Title).toEqual(Some "Old title")
            Vitest.expect(vault.hasUnsavedArcChanges).toBe(true)

            let updatedExistingAssay = ArcAssay("ExistingAssay", title = "Updated title")

            let existingAssayRequest =
                FileContentDTO.fromArcFile(ArcFiles.Assay updatedExistingAssay)
                |> expectSome <| "Expected existing assay DTO."

            match! vault.ApplyArcFileAndSave existingAssayRequest with
            | Error error -> failwith error.Message
            | Ok() -> ()

            let! reloadedArc = loadArcAsync arcPath
            Vitest.expect(reloadedArc.ContainsAssay("NewAssay")).toBe(true)
            Vitest.expect(reloadedArc.GetAssay("ExistingAssay").Title).toEqual(Some "Updated title")
            Vitest.expect(vault.hasUnsavedArcChanges).toBe(false)
        }))
)
