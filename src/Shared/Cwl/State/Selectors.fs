module Swate.Components.Shared.Cwl.State.Selectors

open Swate.Components.Shared.Cwl.Documents.Common
open Swate.Components.Shared.Cwl.Documents.Types
open Swate.Components.Shared.Cwl.State.Types

let isDirty (state: AppState) =
    match state.Meta with
    | Some meta -> meta.Revision <> meta.SavedRevision
    | None -> false

let currentFilePath (state: AppState) =
    state.Meta |> Option.bind (fun meta -> meta.FilePath)

let currentKindLabel (state: AppState) =
    match state.Document with
    | Some(CommandLineToolDoc _) -> Some "CommandLineTool"
    | Some(WorkflowDoc _) -> Some "Workflow"
    | Some(ExpressionToolDoc _) -> Some "ExpressionTool"
    | Some(OperationDoc _) -> Some "Operation"
    | None -> None

let currentRevision (state: AppState) =
    state.Meta
    |> Option.map (fun meta -> meta.Revision)
    |> Option.defaultValue (Revision 0)
