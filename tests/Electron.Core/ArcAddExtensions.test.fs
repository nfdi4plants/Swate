module ElectronCore.ArcAddExtensionsTests

open Fable.Core
open Main.ArcMerge
open Main.ArcVault
open Main.Bindings.Path
open Swate.Components.Shared
open Swate.Electron.Shared.FileIOHelper
open ARCtrl
open ARCtrl.Contract
open Vitest

let private expectSome (value: 'T option) (message: string) : 'T =
    match value with
    | Some value -> value
    | None -> failwith message

let private pathExistsAsync = TestHelpers.pathExistsAsync
let private loadArcAsync = TestHelpers.loadArcAsync
let private testWindow = TestHelpers.testWindow
let private withTempArc = TestHelpers.withTempArcWith "swate-addasync-" "AddAsyncArc"

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

    Vitest.test("AddArcFile uses the scoped ARC add path without persisting unrelated dirty in-memory edits", fun () ->
        withTempArc (fun arc -> arc.AddAssay(ArcAssay("ExistingAssay", title = "Old title"))) (fun arcPath -> promise {
            let! loadedArc = loadArcAsync arcPath
            loadedArc.GetAssay("ExistingAssay").Title <- Some "Unsaved local title"

            let vault = ArcVault(testWindow ())
            vault.path <- Some arcPath
            vault.SetArc loadedArc
            vault.SetHasUnsavedArcChanges true

            let newAssayRequest =
                FileContentDTO.fromArcFile(ArcFiles.Assay(ArcAssay("NewAssay")))
                |> expectSome <| "Expected new assay DTO."

            match! vault.AddArcFile newAssayRequest with
            | Error error -> failwith error.Message
            | Ok() -> ()

            let! reloadedAfterAdd = loadArcAsync arcPath
            Vitest.expect(reloadedAfterAdd.ContainsAssay("NewAssay")).toBe(true)
            Vitest.expect(reloadedAfterAdd.GetAssay("ExistingAssay").Title).toEqual(Some "Old title")

            let inMemoryArc = vault.arc |> expectSome <| "Expected vault ARC."
            Vitest.expect(inMemoryArc.ContainsAssay("NewAssay")).toBe(true)
            Vitest.expect(inMemoryArc.GetAssay("ExistingAssay").Title).toEqual(Some "Unsaved local title")
            Vitest.expect(vault.hasUnsavedArcChanges).toBe(true)
        }))
)
