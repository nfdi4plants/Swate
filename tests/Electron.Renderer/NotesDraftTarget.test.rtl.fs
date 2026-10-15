module ElectronRenderer.NotesDraftTargetTests

open Browser.Types
open Fable.Core
open Renderer.Components.MainContent.NotesDraftTarget
open Swate.Components.Shared
open Vitest

let mutable private targetsResult: Result<ExistingTargetRef[], exn> = Ok [||]

Vitest.vi.mock (
    "./src/Electron/src/Renderer/Api.js",
    box (fun () -> {|
        ipcArcVaultApi = {|
            getNoteTargets = fun () -> JS.Constructors.Promise.resolve targetsResult
        |}
        ipcGitLabApi = obj ()
        ipcVersionControlApi = obj ()
        ipcAuthApi = obj ()
        ipcAppVersionApi = obj ()
        ipcUiSettingsApi = obj ()
        ipcTemplateApi = obj ()
        ipcValidationPackageApi = obj ()
    |})
)
|> ignore

Vitest.afterEach (fun () -> RTL.cleanup ())

Vitest.test (
    "notes can target existing entities while their File Explorer directories are unloaded",
    fun () -> promise {
        targetsResult <-
            Ok [|
                {
                    Name = "UnopenedStudy"
                    Kind = NotesTargetKind.Study
                }
                {
                    Name = "UnopenedAssay"
                    Kind = NotesTargetKind.Assay
                }
            |]

        RTL.render (NotesDraftTarget()) |> ignore
        do! RTL.act (fun () -> promise { do! Promise.sleep 0 })
        RTL.fireEvent.click (RTL.screen.getByTestId "notes-add-existing-button")

        let selector =
            RTL.screen.getByTestId "notes-existing-target-select" :?> HTMLSelectElement

        Vitest.expect(selector.disabled).toBe false
        Vitest.expect(selector.textContent).toContain "UnopenedStudy"
        Vitest.expect(selector.textContent).toContain "UnopenedAssay"
        Vitest.expect(RTL.screen.queryByText("No targets available").IsNone).toBe true
    }
)

Vitest.test (
    "note target lookup errors are shown in the note form",
    fun () -> promise {
        targetsResult <- Error(exn "Inventory unavailable")

        RTL.render (NotesDraftTarget()) |> ignore
        do! RTL.act (fun () -> promise { do! Promise.sleep 0 })

        Vitest.expect((RTL.screen.getByTestId "notes-error-text").textContent).toContain "Inventory unavailable"
    }
)
