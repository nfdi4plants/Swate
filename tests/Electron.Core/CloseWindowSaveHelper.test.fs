module ElectronCore.CloseWindowSaveHelperTests

open ARCtrl
open Swate.Components.Shared
open Swate.Electron.Shared.CloseWindowSaveHelper
open Vitest

let private mkAssay identifier = ArcFiles.Assay(ArcAssay.init identifier)

let private expectSome value =
    match value with
    | Some value -> value
    | None -> failwith "Expected value to exist."

Vitest.describe("CloseWindowSaveHelper.tryGetArcFileToSave", fun () ->
    Vitest.test("prefers pending ARC edits over the visible ARC page", fun () ->
        let pendingArcFile = mkAssay "pending_assay"
        let visibleArcFile = mkAssay "visible_assay"

        let result = tryGetArcFileToSave (Some pendingArcFile) (Some(PageState.ArcFilePage visibleArcFile)) |> expectSome

        match result.Source with
        | ArcFileSaveSource.PendingArcEdits -> ()
        | _ -> failwith "Expected pending ARC edits to win."

        Vitest.expect(result.ArcFile.TryGetRelativePath()).toEqual(pendingArcFile.TryGetRelativePath()))

    Vitest.test("falls back to the visible ARC page when no pending ARC edits exist", fun () ->
        let visibleArcFile = mkAssay "visible_assay"

        let result = tryGetArcFileToSave None (Some(PageState.ArcFilePage visibleArcFile)) |> expectSome

        match result.Source with
        | ArcFileSaveSource.VisibleArcPage -> ()
        | _ -> failwith "Expected the visible ARC page to be used."

        Vitest.expect(result.ArcFile.TryGetRelativePath()).toEqual(visibleArcFile.TryGetRelativePath()))

    Vitest.test("returns none for non-ARC pages when nothing is pending", fun () ->
        let result = tryGetArcFileToSave None (Some PageState.NotesDraftPage)

        Vitest.expect(result.IsNone).toBe(true))
)
