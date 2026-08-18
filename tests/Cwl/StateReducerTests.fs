module CWLBuilder.Renderer.Tests.StateReducerTests

open Expecto
open CWLBuilder.Electron.Renderer.Documents.Common
open CWLBuilder.Electron.Renderer.Documents.Types
open CWLBuilder.Electron.Renderer.State.Actions
open CWLBuilder.Electron.Renderer.State.Effects
open CWLBuilder.Electron.Renderer.State.Init
open CWLBuilder.Electron.Renderer.State.Reducer
open CWLBuilder.Electron.Renderer.State.Selectors
open CWLBuilder.Electron.Renderer.State.Types

let private sampleDocument =
    CommandLineToolDoc (createCommandLineToolModel "v1.2")

let reducerTests =
    testList "Renderer state reducer" [
        test "new document action creates session metadata and focus effect" {
            let nextState, effects = update (NewDocumentCreated sampleDocument) emptyState

            Expect.isSome nextState.Document "Reducer should store the new document"
            Expect.isSome nextState.Meta "Reducer should create document metadata"
            Expect.equal nextState.SessionId 1 "New document should begin a new session"
            Expect.equal effects [ FocusMainWindow "session.entry" ] "Reducer should request window focus after session entry"
            Expect.isFalse (isDirty nextState) "Fresh document should not be dirty"
        }

        test "document update increments revision and derives dirty state" {
            let state, _ = update (NewDocumentCreated sampleDocument) emptyState
            let nextState, effects = update (DocumentUpdated sampleDocument) state

            Expect.equal effects [] "Pure document updates should not emit effects"
            Expect.isTrue (isDirty nextState) "Updated document should become dirty"
        }

        test "leave editor with dirty document opens discard overlay" {
            let state, _ = update (NewDocumentCreated sampleDocument) emptyState
            let dirtyState, _ = update (DocumentUpdated sampleDocument) state
            let nextState, _ = update LeaveEditorRequested dirtyState

            Expect.equal nextState.Overlay ConfirmDiscard "Dirty leave requests should open the discard overlay"
            Expect.isSome nextState.Document "Dirty leave requests should keep the document until confirmed"
        }

        test "save completed updates saved revision and clears dirty state" {
            let state, _ = update (NewDocumentCreated sampleDocument) emptyState
            let dirtyState, _ = update (DocumentUpdated sampleDocument) state
            let savingState, _ = update (SavingStarted None) dirtyState
            let nextState, _ = update (SaveCompleted "saved.cwl") savingState

            Expect.isFalse (isDirty nextState) "Save completion should align saved revision with current revision"
            Expect.equal (currentFilePath nextState) (Some "saved.cwl") "Save completion should persist the file path"
            Expect.equal nextState.Notifications.InfoMessage (Some "Saved to saved.cwl") "Save completion should produce an info notification"
        }
    ]

[<Tests>]
let allTests = reducerTests
