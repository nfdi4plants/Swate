module CWLBuilder.Electron.Renderer.Adapters.WorkflowGraphAdapter

open ARCtrl.CWL
open CWLBuilder.Domain.WorkflowCanvasAdapter
open CWLBuilder.Electron.Renderer.Documents.Types
open CWLBuilder.Electron.Renderer.Adapters.ArCtrlEncode

let private encodeWorkflowModel (model: WorkflowModel) =
    match toProcessingUnit (WorkflowDoc model) with
    | CWLProcessingUnit.Workflow workflow -> workflow
    | _ -> failwith "Expected Workflow processing unit"

let toCanvasGraph (model: WorkflowModel) =
    model |> encodeWorkflowModel |> CWLBuilder.Domain.WorkflowCanvasAdapter.toCanvasGraph

let buildWorkflowGraphReadModel
    (model: WorkflowModel)
    (workflowPath: string option)
    (tryResolveRunPath: (string -> EditorDocument option) option)
    =
    let resolver =
        tryResolveRunPath
        |> Option.map (fun resolveRunPath ->
            fun path ->
                resolveRunPath path
                |> Option.map toProcessingUnit
        )

    CWLBuilder.Domain.WorkflowCanvasAdapter.buildWorkflowGraphReadModel
        (encodeWorkflowModel model)
        workflowPath
        resolver
