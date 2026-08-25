module Swate.Tests.Cwl.EditorShellFeatureTests

open Expecto
open Swate.Components.Shared.Cwl.EditorTypes
open Swate.Components.Shared.Cwl.Documents.Common
open Swate.Components.Shared.Cwl.Documents.Types
open Swate.Components.Shared.Cwl.Features.EditorShellFeature
open Swate.Components.Shared.Cwl.State.Actions
open Swate.Components.Shared.Cwl.State.Init
open Swate.Components.Shared.Cwl.State.Reducer

let editorShellFeatureTests =
    testList "Editor shell and document helpers" [
        test "editor shell selector derives file label from state meta" {
            let state, _ = update (CreateNewRequested CommandLineTool) emptyState
            let props = toEditorShellProps state

            Expect.equal props.FileLabel "unsaved.cwl" "Unsaved document should render the default file label"
        }

        test "command line tool document helper updates base command immutably" {
            let model = createCommandLineToolModel "v1.2"

            let next =
                Swate.Components.Shared.Cwl.Documents.CommandLineTool.setBaseCommand "echo" model

            Expect.sequenceEqual next.BaseCommand [ "echo" ] "CommandLineTool helper should write the new base command"
            Expect.sequenceEqual model.BaseCommand [] "CommandLineTool helper should not mutate the original model"
        }

        test "workflow helper resolves active step by StepId" {
            let step = createWorkflowStep "qc" (ExternalRun "qc.cwl")

            let workflow = {
                createWorkflowModel "v1.2" with
                    Steps = [ step ]
            }

            let active =
                Swate.Components.Shared.Cwl.Documents.Workflow.tryFindStep step.Id workflow

            Expect.equal
                (active |> Option.map (fun item -> item.Id))
                (Some step.Id)
                "Workflow helper should resolve the step by StepId"
        }
    ]

[<Tests>]
let allTests = editorShellFeatureTests
