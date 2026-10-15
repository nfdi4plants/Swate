module ElectronCore.NotesTargetsTests

open ARCtrl
open Fable.Core
open Fable.Core.JsInterop
open Main.ArcVault
open Swate.Components.Shared
open Vitest

let private windowId = 7181

Vitest.afterEach (fun () ->
    ARC_VAULTS.Vaults.Remove windowId |> ignore
    TestHelpers.electronMock?reset () |> ignore
)

let private expectTargets (result: Result<ExistingTargetRef[], exn>) =
    match result with
    | Ok targets -> targets
    | Error error -> failwith error.Message

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
