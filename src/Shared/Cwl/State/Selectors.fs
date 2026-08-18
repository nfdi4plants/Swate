module CWLBuilder.Electron.Renderer.State.Selectors

open CWLBuilder.Electron.Renderer.Documents.Common
open CWLBuilder.Electron.Renderer.Documents.Types
open CWLBuilder.Electron.Renderer.State.Types

let isDirty (state: AppState) =
    match state.Meta with
    | Some meta -> meta.Revision <> meta.SavedRevision
    | None -> false

let currentFilePath (state: AppState) =
    state.Meta |> Option.bind (fun meta -> meta.FilePath)

let currentKindLabel (state: AppState) =
    match state.Document with
    | Some (CommandLineToolDoc _) -> Some "CommandLineTool"
    | Some (WorkflowDoc _) -> Some "Workflow"
    | Some (ExpressionToolDoc _) -> Some "ExpressionTool"
    | Some (OperationDoc _) -> Some "Operation"
    | None -> None

let currentRevision (state: AppState) =
    state.Meta |> Option.map (fun meta -> meta.Revision) |> Option.defaultValue (Revision 0)
