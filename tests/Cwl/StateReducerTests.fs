module Swate.Tests.Cwl.StateReducerTests

open Expecto
open Swate.Components.Shared.Cwl.EditorTypes
open Swate.Components.Shared.Cwl.Documents.Common
open Swate.Components.Shared.Cwl.Documents.Types
open Swate.Components.Shared.Cwl.State.Actions
open Swate.Components.Shared.Cwl.State.Effects
open Swate.Components.Shared.Cwl.State.Init
open Swate.Components.Shared.Cwl.State.Reducer
open Swate.Components.Shared.Cwl.State.Selectors
open Swate.Components.Shared.Cwl.State.Types

let private sampleDocument = CommandLineToolDoc(createCommandLineToolModel "v1.2")

let private expectSingleSaveDialog effects =
    match effects with
    | [ ShowSaveDialog(requestId, revision) ] -> requestId, revision
    | _ -> failtestf "Expected one save dialog effect but got %A" effects

let reducerTests =
    testList "Renderer state reducer" [
        test "new document action creates session metadata and focus effect" {
            let nextState, effects = update (CreateNewRequested CommandLineTool) emptyState

            Expect.isSome nextState.Document "Reducer should store the new document"
            Expect.isSome nextState.Meta "Reducer should create document metadata"
            Expect.equal nextState.SessionId 1 "New document should begin a new session"

            Expect.equal
                effects
                [ FocusMainWindow "session.entry" ]
                "Reducer should request window focus after session entry"

            Expect.isFalse (isDirty nextState) "Fresh document should not be dirty"
        }

        test "document update increments revision and derives dirty state" {
            let state, _ = update (CreateNewRequested CommandLineTool) emptyState
            let nextState, effects = update (DocumentUpdated sampleDocument) state

            Expect.equal effects [] "Pure document updates should not emit effects"
            Expect.isTrue (isDirty nextState) "Updated document should become dirty"
        }

        test "leave editor with dirty document opens discard overlay" {
            let state, _ = update (CreateNewRequested CommandLineTool) emptyState
            let dirtyState, _ = update (DocumentUpdated sampleDocument) state
            let nextState, _ = update LeaveEditorRequested dirtyState

            Expect.equal nextState.Overlay ConfirmDiscard "Dirty leave requests should open the discard overlay"
            Expect.isSome nextState.Document "Dirty leave requests should keep the document until confirmed"
        }

        test "save completed updates saved revision and clears dirty state" {
            let state, _ = update (CreateNewRequested CommandLineTool) emptyState
            let dirtyState, _ = update (DocumentUpdated sampleDocument) state
            let savingState, effects = update SaveRequested dirtyState
            let requestId, revision = expectSingleSaveDialog effects

            let nextState, _ =
                update (SaveSucceeded(requestId, revision, "saved.cwl")) savingState

            Expect.isFalse (isDirty nextState) "Save completion should align saved revision with current revision"
            Expect.equal (currentFilePath nextState) (Some "saved.cwl") "Save completion should persist the file path"

            Expect.equal
                nextState.Notifications.InfoMessage
                (Some "Saved to saved.cwl")
                "Save completion should produce an info notification"
        }

        test "late save completion keeps newer revision dirty" {
            let state, _ = update (CreateNewRequested CommandLineTool) emptyState
            let dirtyState, _ = update (DocumentUpdated sampleDocument) state
            let savingState, effects = update SaveRequested dirtyState
            let requestId, revision = expectSingleSaveDialog effects
            let newerState, _ = update (DocumentUpdated sampleDocument) savingState

            let nextState, _ =
                update (SaveSucceeded(requestId, revision, "saved.cwl")) newerState

            Expect.isTrue (isDirty nextState) "Later edits must remain dirty after an older save completes"
        }

        test "clear editor state resets pending async request ids" {
            let state, _ = update (CreateNewRequested CommandLineTool) emptyState
            let savingState, _ = update SaveRequested state
            let nextState, _ = update DiscardConfirmed savingState

            Expect.equal nextState.Async.PendingSaveRequestId None "Discard should clear the save request id"
            Expect.equal nextState.Async.PendingLoadRequestId None "Discard should clear the load request id"
        }

        test "LoadExistingRequested emits ShowOpenDialog" {
            let nextState, effects = update LoadExistingRequested emptyState

            Expect.isTrue nextState.Async.IsLoading "Load request should set IsLoading"
            Expect.hasLength effects 1 "Load request should emit one dialog effect"
        }

        test "PreviewRequested emits pure preview state" {
            let state, _ = update (CreateNewRequested CommandLineTool) emptyState
            let nextState, effects = update PreviewRequested state

            Expect.equal effects [] "Preview should not emit file-system effects"
            Expect.notEqual nextState.Overlay NoOverlay "Preview should open a preview overlay"
        }

        test "LeaveEditorRequested for clean state clears document immediately" {
            let state, _ = update (CreateNewRequested CommandLineTool) emptyState
            let nextState, _ = update LeaveEditorRequested state

            Expect.isNone nextState.Document "Clean leave should clear the document immediately"
        }

        test "SaveRequested emits ShowSaveDialog with current revision snapshot" {
            let state, _ = update (CreateNewRequested CommandLineTool) emptyState
            let dirtyState, _ = update (DocumentUpdated sampleDocument) state
            let nextState, effects = update SaveRequested dirtyState

            Expect.hasLength effects 1 "Save should emit one dialog effect"
            Expect.isTrue nextState.Async.IsSaving "Save should set IsSaving"
        }
    ]

[<Tests>]
let allTests = reducerTests
