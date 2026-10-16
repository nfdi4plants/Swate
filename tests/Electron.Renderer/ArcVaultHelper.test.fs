module ElectronRenderer.ArcVaultHelperTests

open Fable.Core
open Fable.Core.JsInterop
open Renderer.Components.Helper.ArcVaultHelper
open Renderer.Context.GitWorkflow
open Swate.Electron.Shared.IPCTypes
open Vitest

let mutable private createArcResult: Result<CreateArcOutcome, exn> =
    Ok CreateArcOutcome.Cancelled

let mutable private notesScaffoldingCallCount = 0

/// The effective auto-create value the mocked main process reports. None makes the call fail.
let mutable private effectiveAutoCreateNotesFolder: bool option = Some true

let private getUserDefaults () : JS.Promise<UserDefaultsDto> = promise {
    match effectiveAutoCreateNotesFolder with
    | Some value ->
        return {
            Effective = {
                builtInUserDefaults with
                    AutoCreateNotesFolder = value
            }
            BuiltIn = builtInUserDefaults
        }
    | None -> return failwith "main process unavailable"
}

Vitest.vi.mock (
    "./src/Electron/src/Renderer/Api.js",
    box (fun () ->
        createObj [
            "ipcUserDefaultsApi" ==> createObj [ "getUserDefaults" ==> getUserDefaults ]
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

let private runCreateArcWithDefaults autoCreateNotesFolder outcome = promise {
    createArcResult <- outcome
    notesScaffoldingCallCount <- 0
    effectiveAutoCreateNotesFolder <- autoCreateNotesFolder
    let errors = ResizeArray<string>()
    let! result = createArc errors.Add "Test ARC" false
    return result, errors.ToArray(), notesScaffoldingCallCount
}

let private runCreateArc outcome =
    runCreateArcWithDefaults (Some true) outcome

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

        Vitest.test (
            "skips notes scaffolding when the effective auto-create default is off",
            fun () -> promise {
                let path = "C:/arcs/created"

                let! result, errors, notesCalls =
                    runCreateArcWithDefaults (Some false) (Ok(CreateArcOutcome.Created path))

                Vitest.expect(result).toEqual (Some path)
                Vitest.expect(errors).toEqual ([||])
                Vitest.expect(notesCalls).toBe (0)
            }
        )

        Vitest.test (
            "scaffolds notes when the effective defaults cannot be read",
            fun () -> promise {
                let path = "C:/arcs/created"
                let! result, errors, notesCalls = runCreateArcWithDefaults None (Ok(CreateArcOutcome.Created path))
                Vitest.expect(result).toEqual (Some path)
                Vitest.expect(errors).toEqual ([||])
                Vitest.expect(notesCalls).toBe (1)
            }
        )
)

Vitest.describe (
    "ArcVaultHelper.legacyAutoCreateNotesFolderAction",
    fun () ->
        Vitest.test (
            "decides what happens to the old localStorage value",
            fun () ->
                let rows = [|
                    // stored value, effective default, expected action
                    None, true, LegacyAutoCreateNotesFolderAction.Leave
                    None, false, LegacyAutoCreateNotesFolderAction.Leave
                    Some "true", true, LegacyAutoCreateNotesFolderAction.RemoveKey
                    Some "false", false, LegacyAutoCreateNotesFolderAction.RemoveKey
                    Some "false", true, LegacyAutoCreateNotesFolderAction.AdoptThenRemoveKey false
                    Some "true", false, LegacyAutoCreateNotesFolderAction.AdoptThenRemoveKey true
                    Some "yes", true, LegacyAutoCreateNotesFolderAction.RemoveKey
                    Some "", false, LegacyAutoCreateNotesFolderAction.RemoveKey
                    Some "{\"enabled\":true}", true, LegacyAutoCreateNotesFolderAction.RemoveKey
                |]

                for stored, effective, expected in rows do
                    Vitest.expect(legacyAutoCreateNotesFolderAction stored effective).toEqual (expected)
        )
)
