module Swate.Tests.Cwl.ValidationEngineTests

open Expecto
open ARCtrl.CWL
open Swate.Components.Shared.Cwl.Validation.ValidationTypes
open Swate.Components.Shared.Cwl.Validation.ValidationContext
open Swate.Components.Shared.Cwl.Validation.ValidationEngine

// ---------------------------------------------------------------------------
// Helpers
// ---------------------------------------------------------------------------

let private mkToolCtx (td: CWLToolDescription) (mode: ValidationMode) =
    let pu = CWLProcessingUnit.CommandLineTool td
    create pu td.CWLVersion mode

let private mkWorkflowCtx (wd: CWLWorkflowDescription) (mode: ValidationMode) =
    let pu = CWLProcessingUnit.Workflow wd
    create pu wd.CWLVersion mode

let private mkExprCtx (et: CWLExpressionToolDescription) (mode: ValidationMode) =
    let pu = CWLProcessingUnit.ExpressionTool et
    create pu et.CWLVersion mode

// ---------------------------------------------------------------------------
// Engine-level tests
// ---------------------------------------------------------------------------

let engineTests =
    testList "ValidationEngine" [
        test "Valid minimal tool produces no errors" {
            let td = CWLToolDescription(ResizeArray [| CWLOutput("out") |])
            td.CWLVersion <- "v1.2"
            td.BaseCommand <- Some(ResizeArray [| "echo" |])
            td.Inputs <- Some(ResizeArray [| CWLInput("msg") |])
            let result = validateProcessingUnit (CWLProcessingUnit.CommandLineTool td) OnSave
            Expect.isTrue result.IsValid "Should be valid"
            Expect.isEmpty result.Errors "Should have no errors"
        }

        test "Tool with empty cwlVersion triggers COM.001" {
            let td = CWLToolDescription(ResizeArray [| CWLOutput("out") |])
            td.CWLVersion <- ""
            let result = validateProcessingUnit (CWLProcessingUnit.CommandLineTool td) OnSave
            let comIssues = result.Issues |> List.filter (fun i -> i.RuleId = RuleId "COM.001")
            Expect.isNonEmpty comIssues "Should trigger COM.001"
            Expect.equal comIssues.[0].Severity Error "COM.001 should be Error"
        }

        test "Tool with empty input name triggers COM.002" {
            let td = CWLToolDescription(ResizeArray [| CWLOutput("out") |])
            td.CWLVersion <- "v1.2"
            td.Inputs <- Some(ResizeArray [| CWLInput("") |])
            let result = validateProcessingUnit (CWLProcessingUnit.CommandLineTool td) OnSave
            let comIssues = result.Issues |> List.filter (fun i -> i.RuleId = RuleId "COM.002")
            Expect.isNonEmpty comIssues "Should trigger COM.002"
        }

        test "Tool with no baseCommand triggers CLT.001" {
            let td = CWLToolDescription(ResizeArray [| CWLOutput("out") |])
            td.CWLVersion <- "v1.2"
            let result = validateProcessingUnit (CWLProcessingUnit.CommandLineTool td) OnSave
            let issues = result.Issues |> List.filter (fun i -> i.RuleId = RuleId "CLT.001")
            Expect.isNonEmpty issues "Should trigger CLT.001"
            Expect.equal issues.[0].Severity Warning "CLT.001 should be Warning"
        }

        test "Tool with no outputs triggers CLT.002" {
            let td = CWLToolDescription(ResizeArray())
            td.CWLVersion <- "v1.2"
            let result = validateProcessingUnit (CWLProcessingUnit.CommandLineTool td) OnSave
            let issues = result.Issues |> List.filter (fun i -> i.RuleId = RuleId "CLT.002")
            Expect.isNonEmpty issues "Should trigger CLT.002"
        }

        test "Workflow with no steps triggers WF.001" {
            let wd = CWLWorkflowDescription(ResizeArray(), ResizeArray(), ResizeArray())
            wd.CWLVersion <- "v1.2"
            let result = validateProcessingUnit (CWLProcessingUnit.Workflow wd) OnSave
            let issues = result.Issues |> List.filter (fun i -> i.RuleId = RuleId "WF.001")
            Expect.isNonEmpty issues "Should trigger WF.001"
        }

        test "Workflow with empty step id triggers WF.002" {
            let step =
                WorkflowStep("", ResizeArray(), ResizeArray(), WorkflowStepRun.RunString "tool.cwl")

            let wd =
                CWLWorkflowDescription(ResizeArray [| step |], ResizeArray(), ResizeArray())

            wd.CWLVersion <- "v1.2"
            let result = validateProcessingUnit (CWLProcessingUnit.Workflow wd) OnSave
            let issues = result.Issues |> List.filter (fun i -> i.RuleId = RuleId "WF.002")
            Expect.isNonEmpty issues "Should trigger WF.002"
        }

        test "ExpressionTool with empty expression triggers EXP.001" {
            let et = CWLExpressionToolDescription(ResizeArray(), "")
            et.CWLVersion <- "v1.2"
            let result = validateProcessingUnit (CWLProcessingUnit.ExpressionTool et) OnSave
            let issues = result.Issues |> List.filter (fun i -> i.RuleId = RuleId "EXP.001")
            Expect.isNonEmpty issues "Should trigger EXP.001"
        }

        test "ExpressionTool with JS but no InlineJavascriptRequirement triggers EXP.002" {
            let et = CWLExpressionToolDescription(ResizeArray(), "${return 42;}")
            et.CWLVersion <- "v1.2"
            let result = validateProcessingUnit (CWLProcessingUnit.ExpressionTool et) OnSave
            let issues = result.Issues |> List.filter (fun i -> i.RuleId = RuleId "EXP.002")
            Expect.isNonEmpty issues "Should trigger EXP.002"
        }

        test "Operation can be validated without rule-engine crashes" {
            let op =
                CWLOperationDescription(ResizeArray [| CWLInput("in") |], ResizeArray [| CWLOutput("out") |])

            op.CWLVersion <- "v1.2"
            let result = validateProcessingUnit (CWLProcessingUnit.Operation op) OnSave
            Expect.isTrue result.IsValid "Minimal operation should be valid"
        }

        test "ResourceRequirement with max < min triggers REQ.005" {
            let td = CWLToolDescription(ResizeArray [| CWLOutput("out") |])
            td.CWLVersion <- "v1.2"
            let resource = ResourceRequirementInstance()
            resource.SetProperty("coresMin", 8L)
            resource.SetProperty("coresMax", 4L)
            td.Requirements <- Some(ResizeArray [| Requirement.ResourceRequirement resource |])

            let result = validateProcessingUnit (CWLProcessingUnit.CommandLineTool td) OnSave
            let reqIssues = result.Issues |> List.filter (fun i -> i.RuleId = RuleId "REQ.005")
            Expect.isNonEmpty reqIssues "Should emit REQ.005 for invalid resource min/max pair"
            Expect.equal reqIssues.[0].Severity Error "REQ.005 should be Error"
        }

        test "EnvVarRequirement with duplicate names triggers REQ.006" {
            let td = CWLToolDescription(ResizeArray [| CWLOutput("out") |])
            td.CWLVersion <- "v1.2"

            td.Requirements <-
                Some(
                    ResizeArray [|
                        Requirement.EnvVarRequirement(
                            ResizeArray [|
                                EnvironmentDef("PATH", "/bin")
                                EnvironmentDef("PATH", "/usr/bin")
                            |]
                        )
                    |]
                )

            let result = validateProcessingUnit (CWLProcessingUnit.CommandLineTool td) OnSave
            let reqIssues = result.Issues |> List.filter (fun i -> i.RuleId = RuleId "REQ.006")
            Expect.isNonEmpty reqIssues "Should emit REQ.006 for duplicate envName values"
            Expect.equal reqIssues.[0].Severity Error "REQ.006 should be Error"
        }

        test "InplaceUpdate=true with WorkReuse enabled triggers REQ.007 warning" {
            let td = CWLToolDescription(ResizeArray [| CWLOutput("out") |])
            td.CWLVersion <- "v1.2"

            td.Requirements <-
                Some(
                    ResizeArray [|
                        Requirement.InplaceUpdateRequirement(InplaceUpdateRequirementValue(true))
                        Requirement.WorkReuseRequirement(WorkReuseRequirementValue(true))
                    |]
                )

            let result = validateProcessingUnit (CWLProcessingUnit.CommandLineTool td) OnSave
            let reqIssues = result.Issues |> List.filter (fun i -> i.RuleId = RuleId "REQ.007")
            Expect.isNonEmpty reqIssues "Should emit REQ.007 advisory warning"
            Expect.equal reqIssues.[0].Severity Warning "REQ.007 should be Warning"
        }

        test "isValid returns true for well-formed tool" {
            let td = CWLToolDescription(ResizeArray [| CWLOutput("out") |])
            td.CWLVersion <- "v1.2"
            td.BaseCommand <- Some(ResizeArray [| "echo" |])
            td.Inputs <- Some(ResizeArray [| CWLInput("msg") |])
            Expect.isTrue (isValid (CWLProcessingUnit.CommandLineTool td)) "Should be valid"
        }

        test "isValid returns false for tool with empty input name" {
            let td = CWLToolDescription(ResizeArray [| CWLOutput("out") |])
            td.CWLVersion <- "v1.2"
            td.Inputs <- Some(ResizeArray [| CWLInput("") |])
            Expect.isFalse (isValid (CWLProcessingUnit.CommandLineTool td)) "Should be invalid"
        }

        test "All rules in catalog have unique ids" {
            let ids =
                Swate.Components.Shared.Cwl.Validation.RuleCatalog.allRules
                |> List.map (fun r -> r.Id)

            let uniqueIds = ids |> List.distinct
            Expect.equal ids.Length uniqueIds.Length "All rule IDs must be unique"
        }
    ]

let allTests = testList "Cwl.Validation" [ engineTests ]
