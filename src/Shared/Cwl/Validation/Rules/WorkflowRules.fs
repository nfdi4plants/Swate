/// Rules specific to Workflow documents.
module CWLBuilder.Validation.Rules.WorkflowRules

open ARCtrl.CWL
open CWLBuilder.Validation.ValidationTypes
open CWLBuilder.Validation.ValidationContext
open CWLBuilder.Validation.ValidationRule

/// WF.001 — Workflow must have at least one step.
let stepsRequired : ValidationRule =
    let r =
        { Id = RuleId "WF.001"
          Description = "Workflow must have at least one step."
          Run = fun _ -> [] }
    { r with
        Run = fun ctx ->
            match ctx.ProcessingUnit with
            | CWLProcessingUnit.Workflow wd when includeAdvisoryIssues ctx.Mode ->
                if wd.Steps.Count = 0 then
                    [ issue r Warning "steps" "Workflow has no steps defined." ]
                else []
            | _ -> [] }

/// WF.002 — Every step must have a non-empty id.
let stepIdsNonEmpty : ValidationRule =
    let r =
        { Id = RuleId "WF.002"
          Description = "Every workflow step must have a non-empty id."
          Run = fun _ -> [] }
    { r with
        Run = fun ctx ->
            match ctx.ProcessingUnit with
            | CWLProcessingUnit.Workflow wd ->
                wd.Steps
                |> Seq.toList
                |> List.mapi (fun i step ->
                    if System.String.IsNullOrWhiteSpace step.Id then
                        Some (issue r Error (sprintf "steps[%d]" i) "Step has an empty id.")
                    else None)
                |> List.choose id
            | _ -> [] }

/// WF.003 — Every step must specify a run target.
let stepRunRequired : ValidationRule =
    let r =
        { Id = RuleId "WF.003"
          Description = "Every workflow step must specify a run target."
          Run = fun _ -> [] }
    { r with
        Run = fun ctx ->
            match ctx.ProcessingUnit with
            | CWLProcessingUnit.Workflow wd ->
                wd.Steps
                |> Seq.toList
                |> List.mapi (fun i step ->
                    match step.Run with
                    | WorkflowStepRun.RunString s when System.String.IsNullOrWhiteSpace s ->
                        Some (issue r Error (sprintf "steps[%d].run" i)
                                (sprintf "Step '%s' has an empty run target." step.Id))
                    | _ -> None)
                |> List.choose id
            | _ -> [] }

/// All Workflow rules.
let all = [ stepsRequired; stepIdsNonEmpty; stepRunRequired ]
