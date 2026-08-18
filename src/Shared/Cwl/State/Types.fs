module Swate.Components.Shared.Cwl.State.Types

open System
open Swate.Components.Shared.Cwl.Documents.Common
open Swate.Components.Shared.Cwl.Documents.Types

type EditorMeta = {
    DocumentId: DocumentId
    Revision: Revision
    SavedRevision: Revision
    FilePath: string option
}

type SelectionState = {
    ActiveInputId: InputId option
    ActiveOutputId: OutputId option
    ActiveStepId: StepId option
    ActiveStepInputId: StepInputId option
    ActiveStepOutputId: StepOutputId option
    FocusedRequirementId: RequirementNodeId option
}

type OverlayState =
    | NoOverlay
    | PreviewYaml of string
    | ConfirmDiscard

type NotificationState = {
    ErrorMessage: string option
    InfoMessage: string option
}

type AsyncState = {
    IsLoading: bool
    IsSaving: bool
    PendingLoadRequestId: Guid option
    PendingSaveRequestId: Guid option
}

type AppState = {
    Document: EditorDocument option
    Meta: EditorMeta option
    Selection: SelectionState
    Overlay: OverlayState
    Notifications: NotificationState
    Async: AsyncState
    SessionId: int
}
