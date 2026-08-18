/// Editor-level types that wrap ARCtrl CWL types for UI state management.
/// These DTOs decouple the UI layer from DynamicObj internals, providing
/// typed, immutable snapshots that the view can render and the user can edit.
module Swate.Components.Shared.Cwl.EditorTypes

open ARCtrl.CWL
open Swate.Components.Shared.Cwl.CwlDefaults

// ---------------------------------------------------------------------------
// Processing-unit kind selector (start-mode)
// ---------------------------------------------------------------------------

/// Top-level CWL document kinds a user can create.
type ProcessingUnitKind =
    | CommandLineTool
    | Workflow
    | ExpressionTool
    | Operation

// ---------------------------------------------------------------------------
// Editor state – a thin wrapper over ARCtrl mutable objects.
//
// Strategy: we keep the *authoritative* state inside the ARCtrl mutable
// classes (CWLToolDescription / CWLWorkflowDescription / CWLExpressionToolDescription).
// The EditorState holds a reference to the active processing unit and
// bookkeeping metadata the UI needs (dirty flag, file path, etc.).
//
// When the user mutates a field in the UI, the change is applied directly to
// the underlying ARCtrl object AND the EditorState.Version counter is bumped
// so React can detect changes via reference/version comparison.
// ---------------------------------------------------------------------------

/// Tracks which processing unit is being edited and its lifecycle metadata.
type EditorState = {
    /// The CWL processing unit currently being edited.
    ProcessingUnit: CWLProcessingUnit
    /// Monotonically increasing counter bumped on every mutation.
    /// Enables cheap dirty-checking in the UI layer.
    Version: int
    /// File path from which the document was loaded (None for new documents).
    FilePath: string option
    /// True when the in-memory state differs from the last save.
    IsDirty: bool
    /// CWL version string chosen by the user (default "v1.2").
    CwlVersion: string
}

/// Create a fresh editor state for a new processing unit.
let createNew (kind: ProcessingUnitKind) : EditorState =
    let pu =
        match kind with
        | CommandLineTool ->
            let td = CWLToolDescription(ResizeArray())
            CWLProcessingUnit.CommandLineTool td
        | Workflow ->
            let wd = CWLWorkflowDescription(ResizeArray(), ResizeArray(), ResizeArray())
            CWLProcessingUnit.Workflow wd
        | ExpressionTool ->
            let et = CWLExpressionToolDescription(ResizeArray(), "$(inputs)")
            CWLProcessingUnit.ExpressionTool et
        | Operation ->
            let op = CWLOperationDescription(ResizeArray(), ResizeArray())
            CWLProcessingUnit.Operation op

    {
        ProcessingUnit = pu
        Version = 0
        FilePath = None
        IsDirty = false
        CwlVersion = DefaultCwlVersion
    }

/// Create an editor state from a decoded processing unit (loaded from file).
let fromLoaded (pu: CWLProcessingUnit) (filePath: string) : EditorState =
    let version =
        match pu with
        | CWLProcessingUnit.CommandLineTool td -> td.CWLVersion
        | CWLProcessingUnit.Workflow wd -> wd.CWLVersion
        | CWLProcessingUnit.ExpressionTool et -> et.CWLVersion
        | CWLProcessingUnit.Operation op -> op.CWLVersion

    {
        ProcessingUnit = pu
        Version = 0
        FilePath = Some filePath
        IsDirty = false
        CwlVersion = version
    }

/// Bump the version counter and mark state dirty after a mutation.
let touch (state: EditorState) : EditorState = {
    state with
        Version = state.Version + 1
        IsDirty = true
}
