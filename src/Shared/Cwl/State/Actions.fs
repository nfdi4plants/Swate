module Swate.Components.Shared.Cwl.State.Actions

open System
open Swate.Components.Shared.Cwl.EditorTypes
open Swate.Components.Shared.Cwl.Documents.Common
open Swate.Components.Shared.Cwl.Documents.Types
open Swate.Components.Shared.Cwl.HostTypes
open Swate.Components.Shared.Cwl.State.Types

type AppAction =
    | CreateNewRequested of ProcessingUnitKind
    | LoadExistingRequested
    | DocumentUpdated of EditorDocument
    | SelectionChanged of SelectionState
    | PreviewOpened of string
    | PreviewRequested
    | PreviewClosed
    | LeaveEditorRequested
    | DiscardConfirmed
    | DiscardCancelled
    | ErrorNotificationSet of string option
    | InfoNotificationSet of string option
    | LoadDialogCompleted of Guid * DialogResult
    | LoadSucceeded of Guid * EditorDocument * string
    | LoadFailed of Guid * string
    | SaveRequested
    | SaveDialogCompleted of Guid * Revision * DialogResult
    | SaveSucceeded of Guid * Revision * string
    | SaveFailed of Guid * string
