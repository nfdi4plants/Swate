module ElectronCore.ArcAddExtensionsTests

open Main.ARCtrlExtensions
open Main.ArcMerge
open Main.ArcVault
open Main.Bindings.Path
open Swate.Components.Shared
open Swate.Electron.Shared.FileIOHelper
open ARCtrl
open Vitest

let private expectSome (value: 'T option) (message: string) : 'T =
    match value with
    | Some value -> value
    | None -> failwith message

let private pathExistsAsync = TestHelpers.pathExistsAsync

let private loadArcAsync arcPath = promise {
    match! ARC.LoadAsyncSwate arcPath with
    | Ok arc -> return arc
    | Error errors -> return failwith (PathHelpers.formatContractErrors errors)
}

let private testWindow = TestHelpers.testWindow

let private withTempArc =
    TestHelpers.withTempArcWith "swate-add-arc-file-" "AddArcFileArc"

let private createDefaultArcFile fileType identifier =
    Swate.Components.Shared.ARCtrlHelper.ArcFileDefaults.createDefaultArcFile fileType identifier

let private expectSingleEmptyTable (tables: ResizeArray<ArcTable>) (name: string) =
    Vitest.expect(tables.Count).toBe (1)
    Vitest.expect(tables.[0].Name).toBe (name)
    Vitest.expect(tables.[0].ColumnCount).toBe (0)

Vitest.describe (
    "ARC AddArcFileAsync",
    fun () ->
        Vitest.test (
            "adds an assay and creates the canonical assay file",
            fun () ->
                withTempArc
                    ignore
                    (fun arcPath -> promise {
                        let! arc = loadArcAsync arcPath
                        do! arc.AddArcFileAsync(arcPath, createDefaultArcFile ArcFilesDiscriminate.Assay "NewAssay")

                        Vitest.expect(arc.ContainsAssay("NewAssay")).toBe (true)
                        let addedAssay = arc.GetAssay("NewAssay")
                        expectSingleEmptyTable addedAssay.Tables "NewAssay Table"

                        let canonicalPath = join [| arcPath; "assays"; "NewAssay"; "isa.assay.xlsx" |]
                        let! exists = pathExistsAsync canonicalPath
                        Vitest.expect(exists).toBe (true)

                        let! reloadedArc = loadArcAsync arcPath
                        Vitest.expect(reloadedArc.ContainsAssay("NewAssay")).toBe (true)
                        let reloadedAssay = reloadedArc.GetAssay("NewAssay")
                        expectSingleEmptyTable reloadedAssay.Tables "NewAssay Table"
                    })
        )

        Vitest.test (
            "adds a tableless assay without synthesizing a table",
            fun () ->
                withTempArc
                    ignore
                    (fun arcPath -> promise {
                        let! arc = loadArcAsync arcPath
                        do! arc.AddArcFileAsync(arcPath, ArcFiles.Assay(ArcAssay("TablelessAssay")))

                        Vitest.expect(arc.ContainsAssay("TablelessAssay")).toBe (true)
                        Vitest.expect(arc.GetAssay("TablelessAssay").Tables.Count).toBe (0)

                        let! reloadedArc = loadArcAsync arcPath
                        Vitest.expect(reloadedArc.ContainsAssay("TablelessAssay")).toBe (true)
                        Vitest.expect(reloadedArc.GetAssay("TablelessAssay").Tables.Count).toBe (0)
                    })
        )

        Vitest.test (
            "adds the file explorer default assay via vault add path",
            fun () ->
                withTempArc
                    ignore
                    (fun arcPath -> promise {
                        let arcFile = createDefaultArcFile ArcFilesDiscriminate.Assay "New Assay"

                        let tables = arcFile.Tables()
                        expectSingleEmptyTable tables "New Assay Table"

                        let vault = ArcVault(testWindow ())
                        let! loadedArc = loadArcAsync arcPath
                        vault.path <- Some arcPath
                        vault.SetArc loadedArc

                        let request =
                            FileContentDTO.fromArcFile arcFile |> expectSome
                            <| "Expected default assay DTO."

                        match! vault.AddArcFile request with
                        | Error error -> failwith error.Message
                        | Ok() -> ()

                        let canonicalPath = join [| arcPath; "assays"; "New Assay"; "isa.assay.xlsx" |]
                        let! exists = pathExistsAsync canonicalPath
                        Vitest.expect(exists).toBe (true)

                        let! reloadedArc = loadArcAsync arcPath
                        Vitest.expect(reloadedArc.ContainsAssay("New Assay")).toBe (true)
                        let reloadedAssay = reloadedArc.GetAssay("New Assay")
                        expectSingleEmptyTable reloadedAssay.Tables "New Assay Table"

                        let inMemoryAssay = vault.arc.Value.GetAssay("New Assay")
                        expectSingleEmptyTable inMemoryAssay.Tables "New Assay Table"

                        let openedDto =
                            FileContentDTO.fromArcByPath "assays/New Assay/isa.assay.xlsx" vault.arc.Value
                            |> expectSome
                            <| "Expected open-file DTO for newly added assay."

                        let openedArcFile =
                            FileContentDTO.toArcFile openedDto |> expectSome
                            <| "Expected open-file DTO to decode to an assay."

                        let openedAssayTables = openedArcFile.Tables()
                        expectSingleEmptyTable openedAssayTables "New Assay Table"

                        Vitest.expect(vault.hasUnsavedArcChanges).toBe (false)
                        Vitest.expect(vault.arc.Value.hasInMemoryChanges ()).toBe (false)
                        Vitest.expect(vault.isBusyWriting).toBe (false)
                    })
        )

        Vitest.test (
            "vault add rejects duplicate entities",
            fun () ->
                withTempArc
                    (fun arc -> arc.AddAssay(ArcAssay("ExistingAssay")))
                    (fun arcPath -> promise {
                        let! loadedArc = loadArcAsync arcPath
                        let vault = ArcVault(testWindow ())
                        vault.path <- Some arcPath
                        vault.SetArc loadedArc

                        let request =
                            FileContentDTO.fromArcFile (ArcFiles.Assay(ArcAssay("ExistingAssay")))
                            |> expectSome
                            <| "Expected duplicate assay DTO."

                        match! vault.AddArcFile request with
                        | Ok() -> failwith "Expected duplicate entity add to fail."
                        | Error error ->
                            Vitest.expect(error.Message).toContain ("already contains assay")
                            Vitest.expect(vault.isBusyWriting).toBe (false)
                    })
        )

        Vitest.test (
            "adds a study and creates the canonical study file",
            fun () ->
                withTempArc
                    ignore
                    (fun arcPath -> promise {
                        let! arc = loadArcAsync arcPath
                        do! arc.AddArcFileAsync(arcPath, createDefaultArcFile ArcFilesDiscriminate.Study "NewStudy")

                        Vitest.expect(arc.ContainsStudy("NewStudy")).toBe (true)
                        let addedStudy = arc.GetStudy("NewStudy")
                        expectSingleEmptyTable addedStudy.Tables "NewStudy Table"

                        let canonicalPath = join [| arcPath; "studies"; "NewStudy"; "isa.study.xlsx" |]
                        let! exists = pathExistsAsync canonicalPath
                        Vitest.expect(exists).toBe (true)

                        let! reloadedArc = loadArcAsync arcPath
                        Vitest.expect(reloadedArc.ContainsStudy("NewStudy")).toBe (true)
                        let reloadedStudy = reloadedArc.GetStudy("NewStudy")
                        expectSingleEmptyTable reloadedStudy.Tables "NewStudy Table"
                    })
        )

        Vitest.test (
            "adds a run and creates the canonical run file",
            fun () ->
                withTempArc
                    ignore
                    (fun arcPath -> promise {
                        let! arc = loadArcAsync arcPath
                        do! arc.AddArcFileAsync(arcPath, createDefaultArcFile ArcFilesDiscriminate.Run "NewRun")

                        Vitest.expect(arc.ContainsRun("NewRun")).toBe (true)
                        let addedRun = arc.GetRun("NewRun")
                        expectSingleEmptyTable addedRun.Tables "NewRun Table"

                        let canonicalPath = join [| arcPath; "runs"; "NewRun"; "isa.run.xlsx" |]
                        let! exists = pathExistsAsync canonicalPath
                        Vitest.expect(exists).toBe (true)

                        let! reloadedArc = loadArcAsync arcPath
                        Vitest.expect(reloadedArc.ContainsRun("NewRun")).toBe (true)
                        let reloadedRun = reloadedArc.GetRun("NewRun")
                        expectSingleEmptyTable reloadedRun.Tables "NewRun Table"
                    })
        )

        Vitest.test (
            "adds a workflow and creates the canonical workflow file",
            fun () ->
                withTempArc
                    ignore
                    (fun arcPath -> promise {
                        let! arc = loadArcAsync arcPath
                        do! arc.AddArcFileAsync(arcPath, ArcFiles.Workflow(ArcWorkflow("NewWorkflow")))

                        let canonicalPath =
                            join [|
                                arcPath
                                "workflows"
                                "NewWorkflow"
                                "isa.workflow.xlsx"
                            |]

                        let! exists = pathExistsAsync canonicalPath
                        Vitest.expect(exists).toBe (true)

                        let! reloadedArc = loadArcAsync arcPath
                        Vitest.expect(reloadedArc.ContainsWorkflow("NewWorkflow")).toBe (true)
                    })
        )

        Vitest.test (
            "rejects duplicate entity identifiers",
            fun () ->
                withTempArc
                    (fun arc -> arc.AddAssay(ArcAssay("ExistingAssay")))
                    (fun arcPath -> promise {
                        let! arc = loadArcAsync arcPath
                        let! addResult = arc.TryAddArcFileAsync(arcPath, ArcFiles.Assay(ArcAssay("ExistingAssay")))

                        match addResult with
                        | Ok _ -> failwith "Expected duplicate assay add to fail."
                        | Error errors -> Vitest.expect(errors.[0]).toContain ("already contains assay")
                    })
        )

        Vitest.test (
            "rejects unsupported ARC file kinds",
            fun () ->
                withTempArc
                    ignore
                    (fun arcPath -> promise {
                        let! arc = loadArcAsync arcPath

                        let unsupportedArcFiles = [|
                            ArcFiles.Investigation(ArcInvestigation("Investigation")), "investigation"
                            ArcFiles.DataMap(None, DataMap.init ()), "datamap"
                            ArcFiles.Template(Template.init "Template"), "template"
                        |]

                        for arcFile, expectedMessage in unsupportedArcFiles do
                            let! addResult = arc.TryAddArcFileAsync(arcPath, arcFile)

                            match addResult with
                            | Ok _ -> failwith $"Expected {expectedMessage} add to fail."
                            | Error errors -> Vitest.expect(errors.[0]).toContain (expectedMessage)
                    })
        )

        // Vitest.test("does not mutate the in-memory ARC when add contracts fail", fun () ->
        //     withTempArc ignore (fun arcPath -> promise {
        //         let! arc = loadArcAsync arcPath
        //         do! mkdirRecursiveAsync (join [| arcPath; "assays" |])
        //         do! writeUtf8FileAsync (join [| arcPath; "assays"; "NewAssay" |]) "conflicting file"

        //         let! addResult = arc.TryAddArcFileAsync(arcPath, ArcFiles.Assay(ArcAssay("NewAssay")))

        //         match addResult with
        //         | Ok _ -> failwith "Expected assay add to fail because the assay folder path is already a file."
        //         | Error _ -> ()

        //         Vitest.expect(arc.ContainsAssay("NewAssay")).toBe(false)
        //     }))

        Vitest.test (
            "keeps existing entities when adding a new one",
            fun () ->
                withTempArc
                    (fun arc -> arc.AddStudy(ArcStudy("ExistingStudy")))
                    (fun arcPath -> promise {
                        let! arc = loadArcAsync arcPath
                        do! arc.AddArcFileAsync(arcPath, ArcFiles.Assay(ArcAssay("NewAssay")))

                        let! reloadedArc = loadArcAsync arcPath
                        Vitest.expect(reloadedArc.ContainsStudy("ExistingStudy")).toBe (true)
                        Vitest.expect(reloadedArc.ContainsAssay("NewAssay")).toBe (true)
                    })
        )

        Vitest.test (
            "AddArcFile uses the scoped ARC add path without persisting unrelated dirty in-memory edits",
            fun () ->
                withTempArc
                    (fun arc -> arc.AddAssay(ArcAssay("ExistingAssay", title = "Old title")))
                    (fun arcPath -> promise {
                        let! loadedArc = loadArcAsync arcPath
                        loadedArc.GetAssay("ExistingAssay").Title <- Some "Unsaved local title"

                        let vault = ArcVault(testWindow ())
                        vault.path <- Some arcPath
                        vault.SetArc loadedArc
                        vault.RefreshHasUnsavedArcChangesFlag()

                        let newAssayRequest =
                            FileContentDTO.fromArcFile (ArcFiles.Assay(ArcAssay("NewAssay"))) |> expectSome
                            <| "Expected new assay DTO."

                        match! vault.AddArcFile newAssayRequest with
                        | Error error -> failwith error.Message
                        | Ok() -> ()

                        let! reloadedAfterAdd = loadArcAsync arcPath
                        Vitest.expect(reloadedAfterAdd.ContainsAssay("NewAssay")).toBe (true)
                        Vitest.expect(reloadedAfterAdd.GetAssay("ExistingAssay").Title).toEqual (Some "Old title")

                        let inMemoryArc = vault.arc |> expectSome <| "Expected vault ARC."
                        Vitest.expect(inMemoryArc.ContainsAssay("NewAssay")).toBe (true)
                        Vitest.expect(inMemoryArc.GetAssay("ExistingAssay").Title).toEqual (Some "Unsaved local title")
                        Vitest.expect(inMemoryArc.GetAssay("NewAssay").StaticHash).not.toBe (0)
                        Vitest.expect(vault.hasUnsavedArcChanges).toBe (true)
                        Vitest.expect(vault.isBusyWriting).toBe (false)
                    })
        )
)
