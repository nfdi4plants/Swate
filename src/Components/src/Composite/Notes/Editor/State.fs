namespace Swate.Components.Composite.Notes.Editor

open System

module State =

    let setError (error: string option) (state: NotesUiState) = { state with Error = error }

    let clearError (state: NotesUiState) = { state with Error = None }

    let startSubmitting (state: NotesUiState) = {
        state with
            IsSubmitting = true
            Error = None
    }

    let stopSubmitting (state: NotesUiState) = { state with IsSubmitting = false }

    let hasUnsavedDraft (draft: NotesDraft) =
        let initial = NotesDraft.init

        not (String.IsNullOrWhiteSpace draft.Title)
        || draft.DateCreated <> initial.DateCreated
        || draft.Tags.Count > 0
        || not (String.IsNullOrWhiteSpace draft.MainText)
