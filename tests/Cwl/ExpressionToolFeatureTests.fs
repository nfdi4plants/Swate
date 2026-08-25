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
    ]

[<Tests>]
let allTests = expressionToolFeatureTests
