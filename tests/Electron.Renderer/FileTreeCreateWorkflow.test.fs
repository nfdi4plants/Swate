module ElectronRenderer.FileTreeCreateWorkflowTests

open Fable.Core
open Fable.Core.JsInterop
open ARCtrl
open Renderer.Components.LeftSidebar.FileExplorer
open Swate.Components.Shared
open Swate.Electron.Shared.FileIOTypes
open Vitest

// The workflow module reaches Api.js, whose IPC bridges are absent without the preload. No test calls them.
Vitest.vi.mock ("./src/Electron/src/Renderer/Api.js", box (fun () -> createObj [ "ipcArcVaultApi" ==> createObj [] ]))
|> ignore

let rec private waitUntil (predicate: unit -> bool, attempts: int) = promise {
    if predicate () then
        return ()
    elif attempts <= 0 then
        failwith "Timed out waiting for create workflow."
    else
        do! Promise.sleep 1
        return! waitUntil (predicate, attempts - 1)
}

type private CreateProbe = {
    mutable Busy: bool
    mutable Errors: string list
    mutable Revealed: string list
    mutable Closed: bool
}

let private createProbe () = {
    Busy = false
    Errors = []
    Revealed = []
    Closed = false
}

let private createConfig (probe: CreateProbe) : FileTreeCreateWorkflow.CreateConfig = {
    setIsCreating = fun busy -> probe.Busy <- busy
    applyError = fun message -> probe.Errors <- probe.Errors @ [ message ]
    revealInTree = fun path -> probe.Revealed <- probe.Revealed @ [ path ]
    closeCreateModal = fun () -> probe.Closed <- true
}

let private waitUntilIdle (probe: CreateProbe) =
    // The workflow sets busy to true synchronously, so a false value means it has finished.
    waitUntil ((fun () -> not probe.Busy), 50)

let private entry (path: string) =
    FileEntry.create (PathHelpers.getNameFromPath path, path, false, None)

let private createDraft () =
    match
        Renderer.Components.LeftSidebar.FileExplorer.Helper.tryBuildArcCreateDraft ArcFilesDiscriminate.Study "S1" [||]
    with
    | Ok draft -> draft
    | Error message -> failwith message

let private pendingReveal scope path : FileTreeCreateWorkflow.PendingReveal option =
    Some { ArcScopeId = scope; Path = path }

Vitest.describe (
    "FileTreeCreateWorkflow",
    fun () ->
        Vitest.test (
            "tryCreatePendingReveal ignores blank paths and normalizes separators",
            fun () ->
                Vitest.expect(FileTreeCreateWorkflow.tryCreatePendingReveal (Some "arc") "  ").toEqual (None)

                let reveal =
                    FileTreeCreateWorkflow.tryCreatePendingReveal (Some "arc") "studies\\S1"

                Vitest.expect(reveal |> Option.map _.Path).toEqual (Some "studies/S1")
        )

        Vitest.test (
            "resolveReveal reports nothing without a pending reveal",
            fun () ->
                let resolution =
                    FileTreeCreateWorkflow.resolveReveal None (Some "arc") [| entry "studies/S1" |]

                Vitest.expect(resolution.RevealPath).toEqual (None)
                Vitest.expect(resolution.VisiblePath).toEqual (None)
                Vitest.expect(resolution.ShouldClear).toBe (false)
        )

        Vitest.test (
            "resolveReveal keeps the reveal until the file tree contains the path",
            fun () ->
                let resolution =
                    FileTreeCreateWorkflow.resolveReveal (pendingReveal (Some "arc") "studies/S1") (Some "arc") [|
                        entry "studies/S2"
                    |]

                Vitest.expect(resolution.RevealPath).toEqual (Some "studies/S1")
                Vitest.expect(resolution.VisiblePath).toEqual (None)
                Vitest.expect(resolution.ShouldClear).toBe (false)
        )

        Vitest.test (
            "resolveReveal makes the path visible and clears the reveal once the file tree contains it",
            fun () ->
                let resolution =
                    FileTreeCreateWorkflow.resolveReveal (pendingReveal (Some "arc") "studies/S1") (Some "arc") [|
                        entry "studies/S1"
                    |]

                Vitest.expect(resolution.RevealPath).toEqual (Some "studies/S1")
                Vitest.expect(resolution.VisiblePath).toEqual (Some "studies/S1")
                Vitest.expect(resolution.ShouldClear).toBe (true)
        )

        Vitest.test (
            "resolveReveal matches the entry ignoring case and separators and returns the entry spelling",
            fun () ->
                let pending =
                    FileTreeCreateWorkflow.tryCreatePendingReveal (Some "arc") "Studies\\s1"

                let resolution =
                    FileTreeCreateWorkflow.resolveReveal pending (Some "arc") [| entry "studies/S1" |]

                Vitest.expect(resolution.VisiblePath).toEqual (Some "studies/S1")
                Vitest.expect(resolution.ShouldClear).toBe (true)
        )

        Vitest.test (
            "resolveReveal drops a reveal that belongs to another ARC",
            fun () ->
                let resolution =
                    FileTreeCreateWorkflow.resolveReveal (pendingReveal (Some "other") "studies/S1") (Some "arc") [|
                        entry "studies/S1"
                    |]

                Vitest.expect(resolution.RevealPath).toEqual (None)
                Vitest.expect(resolution.VisiblePath).toEqual (None)
                Vitest.expect(resolution.ShouldClear).toBe (true)
        )

        Vitest.test (
            "importRevealPath returns the target only for a completed import",
            fun () ->
                let reveal result =
                    FileTreeCreateWorkflow.importRevealPath result "assays/A"

                Vitest.expect(reveal (Ok(Some ImportExternalFilesResult.Completed))).toEqual (Some "assays/A")
                Vitest.expect(reveal (Ok(Some ImportExternalFilesResult.Cancelled))).toEqual (None)
                Vitest.expect(reveal (Ok None)).toEqual (None)
                Vitest.expect(reveal (Error(exn "failed"))).toEqual (None)
        )

        Vitest.test (
            "createArcEntry reveals the new path and closes the modal",
            fun () -> promise {
                let probe = createProbe ()
                let draft = createDraft ()

                FileTreeCreateWorkflow.createArcEntry (createConfig probe) (fun _ -> promise { return Ok() }) draft

                do! waitUntilIdle probe

                Vitest.expect(List.toArray probe.Revealed).toEqual ([| draft.Path |])
                Vitest.expect(probe.Closed).toBe (true)
                Vitest.expect(probe.Errors.Length).toBe (0)
                Vitest.expect(probe.Busy).toBe (false)
            }
        )

        Vitest.test (
            "createArcEntry reports the error and keeps the modal open when the create fails",
            fun () -> promise {
                let probe = createProbe ()

                FileTreeCreateWorkflow.createArcEntry
                    (createConfig probe)
                    (fun _ -> promise { return Error(exn "disk full") })
                    (createDraft ())

                do! waitUntilIdle probe

                Vitest.expect(List.toArray probe.Errors).toEqual ([| "disk full" |])
                Vitest.expect(probe.Closed).toBe (false)
                Vitest.expect(probe.Revealed.Length).toBe (0)
                Vitest.expect(probe.Busy).toBe (false)
            }
        )

        Vitest.test (
            "createFileSystemItem reveals the path the create call returns",
            fun () -> promise {
                let probe = createProbe ()

                let request: CreateFileSystemItemRequest = {
                    parentPath = "assays/A"
                    name = "notes.txt"
                    kind = FileSystemItemKind.File
                }

                FileTreeCreateWorkflow.createFileSystemItem
                    (createConfig probe)
                    (fun _ -> promise { return Ok "assays/A/notes.txt" })
                    request

                do! waitUntilIdle probe

                Vitest.expect(List.toArray probe.Revealed).toEqual ([| "assays/A/notes.txt" |])
                Vitest.expect(probe.Closed).toBe (true)
                Vitest.expect(probe.Errors.Length).toBe (0)
            }
        )

        Vitest.test (
            "createFileSystemItem reports one error when the create call rejects",
            fun () -> promise {
                let probe = createProbe ()

                let request: CreateFileSystemItemRequest = {
                    parentPath = "assays/A"
                    name = "notes.txt"
                    kind = FileSystemItemKind.File
                }

                FileTreeCreateWorkflow.createFileSystemItem
                    (createConfig probe)
                    (fun _ -> promise {
                        failwith "boom"
                        return Ok "assays/A/notes.txt"
                    })
                    request

                do! waitUntilIdle probe

                Vitest.expect(List.toArray probe.Errors).toEqual ([| "boom" |])
                Vitest.expect(probe.Closed).toBe (false)
                Vitest.expect(probe.Revealed.Length).toBe (0)
                Vitest.expect(probe.Busy).toBe (false)
            }
        )

        Vitest.test (
            "createDataMap reports the error and reveals nothing when the create fails",
            fun () -> promise {
                let probe = createProbe ()
                let parentInfo = DatamapParentInfo.create "A" DataMapParent.Assay

                FileTreeCreateWorkflow.createDataMap
                    (createConfig probe)
                    (fun _ -> promise { return Error(exn "denied") })
                    parentInfo

                do! waitUntilIdle probe

                Vitest.expect(List.toArray probe.Errors).toEqual ([| "denied" |])
                Vitest.expect(probe.Revealed.Length).toBe (0)
            }
        )

        Vitest.test (
            "createDataMap reveals the path of the DataMap parent",
            fun () -> promise {
                let probe = createProbe ()
                let parentInfo = DatamapParentInfo.create "A" DataMapParent.Assay

                FileTreeCreateWorkflow.createDataMap (createConfig probe) (fun _ -> promise { return Ok() }) parentInfo
                do! waitUntilIdle probe

                Vitest.expect(List.toArray probe.Revealed).toEqual ([| DatamapParentInfo.toPath parentInfo |])
                Vitest.expect(probe.Errors.Length).toBe (0)
            }
        )
)
