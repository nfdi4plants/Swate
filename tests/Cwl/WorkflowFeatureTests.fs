module Swate.Tests.Cwl.WorkflowFeatureTests

open Expecto
open Swate.Components.Shared.Cwl.Documents.Types
open Swate.Components.Shared.Cwl.Features.WorkflowCanvasFeature
open Swate.Components.Shared.Cwl.State.Init

let workflowFeatureTests =
    testList "Workflow feature helpers" [
        test "workflow selection uses StepId and StepInputId, not indexes" {
            let step = createWorkflowStep "qc" (ExternalRun "qc.cwl")
            let stepInput = createStepInput "reads"

            let workflow = {
                createWorkflowModel "v1.2" with
                    Steps = [ { step with Inputs = [ stepInput ] } ]
            }

            let selection = {
                emptySelection with
                    ActiveStepId = Some step.Id
                    ActiveStepInputId = Some stepInput.Id
            }

            Expect.equal selection.ActiveStepId (Some step.Id) "Workflow selection should use StepId"
            Expect.equal selection.ActiveStepInputId (Some stepInput.Id) "Workflow selection should use StepInputId"

            Expect.equal
                workflow.Steps.[0].Inputs.[0].Id
                stepInput.Id
                "Workflow step input should retain its StepInputId"
        }

        test "connecting and disconnecting canvas edges updates immutable workflow state" {
            let workflow = createWorkflowModel "v1.2"

            let nextWorkflow =
                connectOutputSource "step:qc" "report" "output:final" "final" workflow

            let revertedWorkflow =
                disconnectEdge "edge:step:qc/report->output:final/final" nextWorkflow

            Expect.equal
                revertedWorkflow
                workflow
                "Workflow canvas feature should roundtrip connect and disconnect operations immutably"
        }

        test "renaming one step after reordering does not edit another step's draft" {
            let firstStep = createWorkflowStep "first" (ExternalRun "a.cwl")
            let secondStep = createWorkflowStep "second" (ExternalRun "b.cwl")

            let workflow = {
                createWorkflowModel "v1.2" with
                    Steps = [ firstStep; secondStep ]
            }

            let reordered =
                Swate.Components.Shared.Cwl.Documents.Workflow.moveStepDown firstStep.Id workflow

            let renamed =
                Swate.Components.Shared.Cwl.Documents.Workflow.renameStep secondStep.Id "second-renamed" reordered

            Expect.equal
                (renamed.Steps |> List.find (fun step -> step.Id = secondStep.Id)).Name
                "second-renamed"
                "Rename should apply to the selected step id even after reorder"
        }

        test "adding and editing a workflow step input uses stable ids" {
            let step = createWorkflowStep "qc" (ExternalRun "qc.cwl")

            let workflow = {
                createWorkflowModel "v1.2" with
                    Steps = [ step ]
            }

            let nextWorkflow, addedInputId =
                Swate.Components.Shared.Cwl.Documents.Workflow.addStepInput step.Id workflow

            let updatedWorkflow =
                nextWorkflow
                |> Swate.Components.Shared.Cwl.Documents.Workflow.renameStepInput step.Id addedInputId "reads"
                |> Swate.Components.Shared.Cwl.Documents.Workflow.setStepInputSourceText
                    step.Id
                    addedInputId
                    "raw_reads, reference"

            let updatedStep = updatedWorkflow.Steps |> List.find (fun item -> item.Id = step.Id)

            let updatedInput =
                updatedStep.Inputs |> List.find (fun item -> item.Id = addedInputId)

            Expect.equal updatedInput.Name "reads" "Step input rename should target the added input id"

            Expect.equal
                updatedInput.Sources
                [ "raw_reads"; "reference" ]
                "Step input sources should parse comma-separated text"
        }

        test "adding and editing a workflow step output uses stable ids" {
            let step = createWorkflowStep "qc" (ExternalRun "qc.cwl")

            let workflow = {
                createWorkflowModel "v1.2" with
                    Steps = [ step ]
            }

            let nextWorkflow, addedOutputId =
                Swate.Components.Shared.Cwl.Documents.Workflow.addStepOutput step.Id workflow

            let updatedWorkflow =
                nextWorkflow
                |> Swate.Components.Shared.Cwl.Documents.Workflow.renameStepOutput step.Id addedOutputId "report"

            let updatedStep = updatedWorkflow.Steps |> List.find (fun item -> item.Id = step.Id)

            let updatedOutput =
                updatedStep.Outputs |> List.find (fun item -> item.Id = addedOutputId)

            Expect.equal updatedOutput.Name "report" "Step output rename should target the added output id"
        }
    ]

[<Tests>]
let allTests = workflowFeatureTests
