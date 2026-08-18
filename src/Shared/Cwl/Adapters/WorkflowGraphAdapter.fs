module Swate.Components.Shared.Cwl.Adapters.WorkflowGraphAdapter

open ARCtrl.CWL
open Swate.Components.Shared.Cwl.WorkflowCanvasAdapter
open Swate.Components.Shared.Cwl.Documents.Types
open Swate.Components.Shared.Cwl.Adapters.ArCtrlEncode

let private encodeWorkflowModel (model: WorkflowModel) =
    match toProcessingUnit (WorkflowDoc model) with
    | CWLProcessingUnit.Workflow workflow -> workflow
    | _ -> failwith "Expected Workflow processing unit"

let toCanvasGraph (model: WorkflowModel) =
    model
    |> encodeWorkflowModel
    |> Swate.Components.Shared.Cwl.WorkflowCanvasAdapter.toCanvasGraph

let buildWorkflowGraphReadModel
    (model: WorkflowModel)
    (workflowPath: string option)
    (tryResolveRunPath: (string -> EditorDocument option) option)
    =
    let resolver =
        tryResolveRunPath
        |> Option.map (fun resolveRunPath -> fun path -> resolveRunPath path |> Option.map toProcessingUnit)

    Swate.Components.Shared.Cwl.WorkflowCanvasAdapter.buildWorkflowGraphReadModel
        (encodeWorkflowModel model)
        workflowPath
        resolver
