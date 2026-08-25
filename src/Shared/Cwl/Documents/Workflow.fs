module Swate.Components.Shared.Cwl.Documents.Workflow

open Swate.Components.Shared.Cwl.Documents.Common
open Swate.Components.Shared.Cwl.Documents.Types

let tryFindStep (stepId: StepId) (model: WorkflowModel) =
    model.Steps |> List.tryFind (fun step -> step.Id = stepId)
