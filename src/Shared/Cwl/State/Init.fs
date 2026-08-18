module CWLBuilder.Electron.Renderer.State.Init

open CWLBuilder.Electron.Renderer.State.Types

let emptySelection = {
    ActiveInputId = None
    ActiveOutputId = None
    ActiveStepId = None
    ActiveStepInputId = None
    ActiveStepOutputId = None
    FocusedRequirementId = None
}

let emptyNotifications = {
    ErrorMessage = None
    InfoMessage = None
}

let emptyAsync = {
    IsLoading = false
    IsSaving = false
    PendingLoadRequestId = None
    PendingSaveRequestId = None
}

let emptyState = {
    Document = None
    Meta = None
    Selection = emptySelection
    Overlay = NoOverlay
    Notifications = emptyNotifications
    Async = emptyAsync
    SessionId = 0
}
