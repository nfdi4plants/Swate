/// Rules specific to CommandLineTool documents.
module CWLBuilder.Validation.Rules.CommandLineToolRules

open ARCtrl.CWL
open CWLBuilder.Domain.CwlDefaults
open CWLBuilder.Validation.ValidationTypes
open CWLBuilder.Validation.ValidationContext
open CWLBuilder.Validation.ValidationRule

/// CLT.001 — baseCommand should be set.
let baseCommandRequired : ValidationRule =
    let r =
        { Id = RuleId "CLT.001"
          Description = "CommandLineTool should have a baseCommand."
          Run = fun _ -> [] }
    { r with
        Run = fun ctx ->
            match ctx.ProcessingUnit with
            | CWLProcessingUnit.CommandLineTool td when includeAdvisoryIssues ctx.Mode ->
                match td.BaseCommand with
                | None -> [ issue r Warning "baseCommand" "CommandLineTool has no baseCommand." ]
                | Some bc when bc.Count = 0 ->
                    [ issue r Warning "baseCommand" "baseCommand is empty." ]
                | _ -> []
            | _ -> [] }

/// CLT.002 — CommandLineTool should have at least one output.
let outputsRequired : ValidationRule =
    let r =
        { Id = RuleId "CLT.002"
          Description = "CommandLineTool should have at least one output."
          Run = fun _ -> [] }
    { r with
        Run = fun ctx ->
            match ctx.ProcessingUnit with
            | CWLProcessingUnit.CommandLineTool td when includeAdvisoryIssues ctx.Mode ->
                if td.Outputs.Count = 0 then
                    [ issue r Warning "outputs" "CommandLineTool has no outputs defined." ]
                else []
            | _ -> [] }

/// CLT.003 — Inputs with inputBinding should have a position for v1.2.
let inputBindingPosition : ValidationRule =
    let r =
        { Id = RuleId "CLT.003"
          Description = "Inputs with inputBinding should specify position (v1.2)."
          Run = fun _ -> [] }
    { r with
        Run = fun ctx ->
            match ctx.ProcessingUnit with
            | CWLProcessingUnit.CommandLineTool td when includeAdvisoryIssues ctx.Mode && ctx.CwlVersion = DefaultCwlVersion ->
                td.Inputs
                |> Option.defaultValue (ResizeArray())
                |> Seq.toList
                |> List.mapi (fun i inp ->
                    match inp.InputBinding with
                    | Some ib when ib.Position.IsNone ->
                        Some (issue r Info (sprintf "inputs[%d].inputBinding" i)
                                (sprintf "Input '%s' has inputBinding without position." inp.Name))
                    | _ -> None)
                |> List.choose id
            | _ -> [] }

/// All CommandLineTool rules.
let all = [ baseCommandRequired; outputsRequired; inputBindingPosition ]
