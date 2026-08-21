module Swate.Components.Shared.Cwl.State.Actions

open System
open Swate.Components.Shared.Cwl.Documents.Common
open Swate.Components.Shared.Cwl.Documents.Types
open Swate.Components.Shared.Cwl.State.Types

type AppAction =
    | NewDocumentCreated of EditorDocument
    | DocumentUpdated of EditorDocument
    | SelectionChanged of SelectionState
    | PreviewOpened of string
    | PreviewClosed
    | LeaveEditorRequested
    | DiscardConfirmed
    | DiscardCancelled
    | ErrorNotificationSet of string option
    | InfoNotificationSet of string option
    | LoadingStarted of Guid
    | LoadingFinished of Guid
    | LoadSucceeded of Guid * EditorDocument * string
    | LoadFailed of Guid * string
    | SavingStarted of Guid * Revision
    | SaveSucceeded of Guid * Revision * string
    | SaveFailed of Guid * string
