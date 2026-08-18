module CWLBuilder.Electron.Renderer.State.Actions

open System
open CWLBuilder.Electron.Renderer.Documents.Types
open CWLBuilder.Electron.Renderer.State.Types

type AppAction =
    | NewDocumentCreated of EditorDocument
    | ExistingDocumentLoaded of EditorDocument * string
    | DocumentUpdated of EditorDocument
    | SelectionChanged of SelectionState
    | PreviewOpened of string
    | PreviewClosed
    | LeaveEditorRequested
    | DiscardConfirmed
    | DiscardCancelled
    | ErrorNotificationSet of string option
    | InfoNotificationSet of string option
    | LoadingStarted of Guid option
    | LoadingFinished
    | SavingStarted of Guid option
    | SaveCompleted of string
    | SaveFailed of string
