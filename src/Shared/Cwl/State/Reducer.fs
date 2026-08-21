module Swate.Components.Shared.Cwl.State.Reducer

open System
open ARCtrl.CWL
open Swate.Components.Shared.Cwl.CwlDefaults
open Swate.Components.Shared.Cwl.EditorTypes
open Swate.Components.Shared.Cwl.Documents.Common
open Swate.Components.Shared.Cwl.Documents.Types
open Swate.Components.Shared.Cwl.Adapters.ArCtrlEncode
open Swate.Components.Shared.Cwl.State.Actions
open Swate.Components.Shared.Cwl.State.Effects
open Swate.Components.Shared.Cwl.State.Init
open Swate.Components.Shared.Cwl.State.Selectors
open Swate.Components.Shared.Cwl.State.Types

let private nextRevision (Revision value) = Revision(value + 1)

let private withEffects (effects: AppEffect list) (state: AppState) =
    { state with PendingEffects = effects }, effects

let private newMeta filePath = {
    DocumentId = newDocumentId ()
    Revision = Revision 0
    SavedRevision = Revision 0
    FilePath = filePath
}

let private createDocument kind =
    match kind with
    | ProcessingUnitKind.CommandLineTool -> CommandLineToolDoc(createCommandLineToolModel DefaultCwlVersion)
    | ProcessingUnitKind.Workflow -> WorkflowDoc(createWorkflowModel DefaultCwlVersion)
    | ProcessingUnitKind.ExpressionTool -> ExpressionToolDoc(createExpressionToolModel DefaultCwlVersion "$(inputs)")
    | ProcessingUnitKind.Operation -> OperationDoc(createOperationModel DefaultCwlVersion)

let private clearEditorState (state: AppState) = {
    state with
        Document = None
        Meta = None
        Selection = emptySelection
        Overlay = NoOverlay
        Notifications = emptyNotifications
        Async = emptyAsync
        PendingEffects = []
}

let update (action: AppAction) (state: AppState) : AppState * AppEffect list =
    match action with
    | CreateNewRequested kind ->
        let document = createDocument kind

        {
            state with
                Document = Some document
                Meta = Some(newMeta None)
                Selection = emptySelection
                Overlay = NoOverlay
                Notifications = emptyNotifications
                Async = emptyAsync
                SessionId = state.SessionId + 1
        }
        |> withEffects [ FocusMainWindow "session.entry" ]

    | LoadExistingRequested ->
        let requestId = Guid.NewGuid()

        {
            state with
                Async = {
                    state.Async with
                        IsLoading = true
                        PendingLoadRequestId = Some requestId
                }
        }
        |> withEffects [ ShowOpenDialog requestId ]

    | DocumentUpdated document ->
        let nextMeta =
            state.Meta
            |> Option.map (fun meta -> {
                meta with
                    Revision = nextRevision meta.Revision
            })

        {
            state with
                Document = Some document
                Meta = nextMeta
                Notifications = {
                    state.Notifications with
                        InfoMessage = None
                }
        }
        |> withEffects []

    | SelectionChanged selection -> { state with Selection = selection } |> withEffects []

    | PreviewOpened yaml ->
        {
            state with
                Overlay = PreviewYaml yaml
        }
        |> withEffects []

    | PreviewRequested ->
        match state.Document with
        | Some document ->
            let yaml = document |> toProcessingUnit |> Encode.encodeProcessingUnit

            {
                state with
                    Overlay = PreviewYaml yaml
            }
            |> withEffects []
        | None -> state |> withEffects []

    | PreviewClosed -> { state with Overlay = NoOverlay } |> withEffects []

    | LeaveEditorRequested ->
        if isDirty state then
            { state with Overlay = ConfirmDiscard } |> withEffects []
        else
            clearEditorState state |> withEffects []

    | DiscardConfirmed -> clearEditorState state |> withEffects []

    | DiscardCancelled -> { state with Overlay = NoOverlay } |> withEffects []

    | ErrorNotificationSet message ->
        {
            state with
                Notifications = {
                    state.Notifications with
                        ErrorMessage = message
                }
        }
        |> withEffects []

    | InfoNotificationSet message ->
        {
            state with
                Notifications = {
                    state.Notifications with
                        InfoMessage = message
                }
        }
        |> withEffects []

    | LoadDialogCompleted(requestId, dialogResult) ->
        match state.Async.PendingLoadRequestId with
        | Some pendingId when pendingId = requestId ->
            match dialogResult.Canceled, dialogResult.FilePath with
            | true, _
            | _, None ->
                {
                    state with
                        Async = {
                            state.Async with
                                IsLoading = false
                                PendingLoadRequestId = None
                        }
                }
                |> withEffects []
            | false, Some filePath -> state |> withEffects [ LoadCwlFile(requestId, filePath) ]
        | _ -> state |> withEffects []

    | LoadSucceeded(requestId, document, filePath) ->
        match state.Async.PendingLoadRequestId with
        | Some pendingId when pendingId = requestId ->
            {
                state with
                    Document = Some document
                    Meta = Some(newMeta (Some filePath))
                    Selection = emptySelection
                    Overlay = NoOverlay
                    Notifications = emptyNotifications
                    Async = {
                        state.Async with
                            IsLoading = false
                            PendingLoadRequestId = None
                    }
                    SessionId = state.SessionId + 1
            }
            |> withEffects [ FocusMainWindow "session.entry" ]
        | _ -> state |> withEffects []

    | LoadFailed(requestId, message) ->
        match state.Async.PendingLoadRequestId with
        | Some pendingId when pendingId = requestId ->
            {
                state with
                    Async = {
                        state.Async with
                            IsLoading = false
                            PendingLoadRequestId = None
                    }
                    Notifications = {
                        state.Notifications with
                            ErrorMessage = Some message
                    }
            }
            |> withEffects []
        | _ -> state |> withEffects []

    | SaveRequested ->
        match state.Meta with
        | Some meta ->
            let requestId = Guid.NewGuid()

            {
                state with
                    Async = {
                        state.Async with
                            IsSaving = true
                            PendingSaveRequestId = Some requestId
                    }
            }
            |> withEffects [ ShowSaveDialog(requestId, meta.Revision) ]
        | None -> state |> withEffects []

    | SaveDialogCompleted(requestId, revision, dialogResult) ->
        match state.Async.PendingSaveRequestId, state.Document with
        | Some pendingId, Some document when pendingId = requestId ->
            match dialogResult.Canceled, dialogResult.FilePath with
            | true, _
            | _, None ->
                {
                    state with
                        Async = {
                            state.Async with
                                IsSaving = false
                                PendingSaveRequestId = None
                        }
                }
                |> withEffects []
            | false, Some filePath ->
                let yaml = document |> toProcessingUnit |> Encode.encodeProcessingUnit
                state |> withEffects [ SaveCwlFile(requestId, revision, filePath, yaml) ]
        | _ -> state |> withEffects []

    | SaveSucceeded(requestId, savedRevision, filePath) ->
        match state.Async.PendingSaveRequestId, state.Meta with
        | Some pendingId, Some meta when pendingId = requestId ->
            let nextSavedRevision =
                if savedRevision > meta.SavedRevision then
                    savedRevision
                else
                    meta.SavedRevision

            {
                state with
                    Meta =
                        Some {
                            meta with
                                FilePath = Some filePath
                                SavedRevision = nextSavedRevision
                        }
                    Async = {
                        state.Async with
                            IsSaving = false
                            PendingSaveRequestId = None
                    }
                    Notifications = {
                        state.Notifications with
                            ErrorMessage = None
                            InfoMessage = Some $"Saved to {filePath}"
                    }
            }
            |> withEffects []
        | _ -> state |> withEffects []

    | SaveFailed(requestId, message) ->
        match state.Async.PendingSaveRequestId with
        | Some pendingId when pendingId = requestId ->
            {
                state with
                    Async = {
                        state.Async with
                            IsSaving = false
                            PendingSaveRequestId = None
                    }
                    Notifications = {
                        state.Notifications with
                            ErrorMessage = Some message
                    }
            }
            |> withEffects []
        | _ -> state |> withEffects []
