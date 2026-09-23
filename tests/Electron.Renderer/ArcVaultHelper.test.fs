module ElectronRenderer.ArcVaultHelperTests

open Browser.Dom
open Fable.Core
open Fable.Core.JsInterop
open Renderer.Components.Helper.ArcVaultHelper
open Swate.Electron.Shared.IPCTypes
open Vitest

let mutable private createArcResult: Result<CreateArcOutcome, exn> =
    Ok CreateArcOutcome.Cancelled

let mutable private notesScaffoldingCallCount = 0

Vitest.vi.mock (
    "./src/Electron/src/Renderer/Api.js",
    box (fun () ->
        createObj [
            "ipcArcVaultApi"
            ==> createObj [
                "createARC"
                ==> fun (_: CreateArcRequest) -> JS.Constructors.Promise.resolve createArcResult
                "ensureNotesFolder"
                ==> fun () ->
                    notesScaffoldingCallCount <- notesScaffoldingCallCount + 1
                    JS.Constructors.Promise.resolve (Ok())
            ]
        ]
    )
)
|> ignore

let private runCreateArc outcome = promise {
    createArcResult <- outcome
    notesScaffoldingCallCount <- 0
    window.localStorage.clear ()
    let errors = ResizeArray<string>()
    let! result = createArc errors.Add "Test ARC" false
    return result, errors.ToArray(), notesScaffoldingCallCount
}

Vitest.describe (
    "ArcVaultHelper.createArc",
    fun () ->
        Vitest.test (
            "returns None without reporting an error or scaffolding notes when cancelled",
            fun () -> promise {
                let! result, errors, notesCalls = runCreateArc (Ok CreateArcOutcome.Cancelled)
                Vitest.expect(result).toEqual (None)
                Vitest.expect(errors).toEqual ([||])
                Vitest.expect(notesCalls).toBe (0)
            }
        )

        Vitest.test (
            "returns None without reporting an error or scaffolding notes when the created ARC was closed",
            fun () -> promise {
                let! result, errors, notesCalls = runCreateArc (Ok(CreateArcOutcome.CreatedButClosed "C:/arcs/closed"))

                Vitest.expect(result).toEqual (None)
                Vitest.expect(errors).toEqual ([||])
                Vitest.expect(notesCalls).toBe (0)
            }
        )

        Vitest.test (
            "returns None without reporting an error or scaffolding notes when an existing ARC was focused",
            fun () -> promise {
                let! result, errors, notesCalls = runCreateArc (Ok(CreateArcOutcome.FocusedExisting "C:/arcs/existing"))

                Vitest.expect(result).toEqual (None)
                Vitest.expect(errors).toEqual ([||])
                Vitest.expect(notesCalls).toBe (0)
            }
        )

        Vitest.test (
            "returns the created path and preserves post-create notes scaffolding",
            fun () -> promise {
                let path = "C:/arcs/created"
                let! result, errors, notesCalls = runCreateArc (Ok(CreateArcOutcome.Created path))
                Vitest.expect(result).toEqual (Some path)
                Vitest.expect(errors).toEqual ([||])
                Vitest.expect(notesCalls).toBe (1)
            }
        )

        Vitest.test (
            "returns None and reports a genuine error exactly once",
            fun () -> promise {
                let expectedError = exn "create failed"
                let! result, errors, notesCalls = runCreateArc (Error expectedError)
                Vitest.expect(result).toEqual (None)
                Vitest.expect(errors).toEqual ([| expectedError.Message |])
                Vitest.expect(notesCalls).toBe (0)
            }
        )
)
