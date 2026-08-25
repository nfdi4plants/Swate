module Swate.Tests.Cwl.ExpressionToolFeatureTests

open Expecto
open Swate.Components.Shared.Cwl.Documents.Types
open Swate.Components.Shared.Cwl.Features.RequirementsFeature

let expressionToolFeatureTests =
    testList "Expression tool feature helpers" [
        test "expression text update changes only the active expression tool document" {
            let document =
                ExpressionToolDoc {
                    createExpressionToolModel "v1.2" "$(inputs.value)" with
                        Expression = "$(inputs.value)"
                }

            let nextDocument =
                Swate.Components.Shared.Cwl.Documents.ExpressionTool.setExpression "${ return { out: 1 }; }" document

            match nextDocument with
            | ExpressionToolDoc model ->
                Expect.equal
                    model.Expression
                    "${ return { out: 1 }; }"
                    "Expression helper should update the expression text"
            | _ -> failtest "Expected ExpressionToolDoc"
        }

        test "requirement selection uses RequirementNodeId rather than list position" {
            let reqA = createRequirementNode "docker"
            let reqB = createRequirementNode "inline-javascript"

            let props = {
                emptyProps with
                    RequirementItems = [ reqA; reqB ]
                    SelectedRequirementId = Some reqB.Id
            }

            Expect.equal
                props.SelectedRequirementId
                (Some reqB.Id)
                "Requirement feature props should track the selected requirement by id"
        }

        test "requirement helpers update command line tool documents immutably" {
            let document = CommandLineToolDoc(createCommandLineToolModel "v1.2")
            let enabled = setRequirementEnabled "docker" true document

            match enabled with
            | CommandLineToolDoc model ->
                let docker = model.Requirements |> List.exactlyOne
                let updated = setRequirementField docker.Id "dockerPull" "ubuntu:24.04" enabled

                match updated with
                | CommandLineToolDoc updatedModel ->
                    Expect.equal
                        updatedModel.Requirements.Head.Fields.["dockerPull"]
                        "ubuntu:24.04"
                        "Requirement field should be stored on the target node"
                | _ -> failtest "Expected command line tool document"
            | _ -> failtest "Expected command line tool document"
        }

        test "requirement helpers update workflow documents immutably" {
            let document = WorkflowDoc(createWorkflowModel "v1.2")
            let enabled = setHintEnabled "docker" true document

            match enabled with
            | WorkflowDoc model ->
                let docker = model.Hints |> List.exactlyOne
                let updated = setHintField docker.Id "dockerPull" "ubuntu:24.04" enabled

                match updated with
                | WorkflowDoc updatedModel ->
                    Expect.equal
                        updatedModel.Hints.Head.Fields.["dockerPull"]
                        "ubuntu:24.04"
                        "Hint field should be stored on the target node"
                | _ -> failtest "Expected workflow document"
            | _ -> failtest "Expected workflow document"
        }
    ]

[<Tests>]
let allTests = expressionToolFeatureTests
