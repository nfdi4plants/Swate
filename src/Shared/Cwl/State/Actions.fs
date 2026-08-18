module Swate.Components.Shared.Cwl.State.Actions

open System
open Swate.Components.Shared.Cwl.Documents.Types
open Swate.Components.Shared.Cwl.State.Types

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
