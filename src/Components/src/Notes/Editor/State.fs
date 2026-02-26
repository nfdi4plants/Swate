namespace Swate.Components.Notes.Editor

module State =

    let setError (error: string option) (state: NotesUiState) = {
        state with
            Error = error
    }

    let clearError (state: NotesUiState) = {
        state with
            Error = None
    }

    let startSubmitting (state: NotesUiState) = {
        state with
            IsSubmitting = true
            Error = None
    }

    let stopSubmitting (state: NotesUiState) = {
        state with
            IsSubmitting = false
    }

    let showExistingTargetSelector (state: NotesUiState) = {
        state with
            ShowExistingTargetSelector = true
            Error = None
    }
