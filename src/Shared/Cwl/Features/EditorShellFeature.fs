module Swate.Components.Shared.Cwl.Features.EditorShellFeature

open Swate.Components.Shared.Cwl.Documents.Common
open Swate.Components.Shared.Cwl.Documents.Types
open Swate.Components.Shared.Cwl.State.Selectors
open Swate.Components.Shared.Cwl.State.Types

type EditorShellProps = {
    FileLabel: string
    KindLabel: string option
    Version: int
    IsDirty: bool
    IsSaving: bool
    ErrorMessage: string option
    InfoMessage: string option
    CwlVersion: string option
}

let private currentCwlVersion (document: EditorDocument) =
    match document with
    | CommandLineToolDoc model -> model.CwlVersion
    | WorkflowDoc model -> model.CwlVersion
    | ExpressionToolDoc model -> model.CwlVersion
    | OperationDoc model -> model.CwlVersion

let toEditorShellProps (state: AppState) : EditorShellProps =
    let (Revision version) = currentRevision state

    {
        FileLabel = currentFilePath state |> Option.defaultValue "unsaved.cwl"
        KindLabel = currentKindLabel state
        Version = version
        IsDirty = isDirty state
        IsSaving = state.Async.IsSaving
        ErrorMessage = state.Notifications.ErrorMessage
        InfoMessage = state.Notifications.InfoMessage
        CwlVersion = state.Document |> Option.map currentCwlVersion
    }
