module Swate.Components.Shared.Cwl.State.Reducer

open Swate.Components.Shared.Cwl.Documents.Common
open Swate.Components.Shared.Cwl.State.Actions
open Swate.Components.Shared.Cwl.State.Effects
open Swate.Components.Shared.Cwl.State.Init
open Swate.Components.Shared.Cwl.State.Selectors
open Swate.Components.Shared.Cwl.State.Types

let private nextRevision (Revision value) = Revision(value + 1)

let private newMeta filePath = {
    DocumentId = newDocumentId ()
    Revision = Revision 0
    SavedRevision = Revision 0
    FilePath = filePath
}

let private clearEditorState (state: AppState) = {
    state with
        Document = None
        Meta = None
        Selection = emptySelection
        Overlay = NoOverlay
        Notifications = emptyNotifications
        Async = emptyAsync
}

let update (action: AppAction) (state: AppState) : AppState * AppEffect list =
    match action with
    | NewDocumentCreated document ->
        {
            state with
                Document = Some document
                Meta = Some(newMeta None)
                Selection = emptySelection
                Overlay = NoOverlay
                Notifications = emptyNotifications
                Async = emptyAsync
                SessionId = state.SessionId + 1
        },
        [ FocusMainWindow "session.entry" ]

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
        },
        []

    | SelectionChanged selection -> { state with Selection = selection }, []

    | PreviewOpened yaml ->
        {
            state with
                Overlay = PreviewYaml yaml
        },
        []

    | PreviewClosed -> { state with Overlay = NoOverlay }, []

    | LeaveEditorRequested ->
        if isDirty state then
            { state with Overlay = ConfirmDiscard }, []
        else
            clearEditorState state, []

    | DiscardConfirmed -> clearEditorState state, []

    | DiscardCancelled -> { state with Overlay = NoOverlay }, []

    | ErrorNotificationSet message ->
        {
            state with
                Notifications = {
                    state.Notifications with
                        ErrorMessage = message
                }
        },
        []

    | InfoNotificationSet message ->
        {
            state with
                Notifications = {
                    state.Notifications with
                        InfoMessage = message
                }
        },
        []

    | LoadingStarted requestId ->
        {
            state with
                Async = {
                    state.Async with
                        IsLoading = true
                        PendingLoadRequestId = Some requestId
                }
        },
        []

    | LoadingFinished requestId ->
        match state.Async.PendingLoadRequestId with
        | Some pendingId when pendingId = requestId ->
            {
                state with
                    Async = {
                        state.Async with
                            IsLoading = false
                            PendingLoadRequestId = None
                    }
            },
            []
        | _ -> state, []

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
            },
            [ FocusMainWindow "session.entry" ]
        | _ -> state, []

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
            },
            []
        | _ -> state, []

    | SavingStarted(requestId, _) ->
        {
            state with
                Async = {
                    state.Async with
                        IsSaving = true
                        PendingSaveRequestId = Some requestId
                }
        },
        []

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
            },
            []
        | _ -> state, []

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
            },
            []
        | _ -> state, []
