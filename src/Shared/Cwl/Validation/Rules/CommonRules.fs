/// Rules that apply to any CWL document type.
module Swate.Components.Shared.Cwl.Validation.Rules.CommonRules

open ARCtrl.CWL
open Swate.Components.Shared.Cwl.Validation.ValidationTypes
open Swate.Components.Shared.Cwl.Validation.ValidationContext
open Swate.Components.Shared.Cwl.Validation.ValidationRule

/// COM.001 — cwlVersion must be set.
let cwlVersionRequired: ValidationRule =
    let r = {
        Id = RuleId "COM.001"
        Description = "cwlVersion must be set."
        Run = fun _ -> []
    }

    {
        r with
            Run =
                fun ctx ->
                    if System.String.IsNullOrWhiteSpace ctx.CwlVersion then
                        [ issue r Error "cwlVersion" "cwlVersion is required." ]
                    else
                        []
    }

/// COM.002 — inputs must not have empty names.
let inputNamesNonEmpty: ValidationRule =
    let r = {
        Id = RuleId "COM.002"
        Description = "Every input must have a non-empty name."
        Run = fun _ -> []
    }

    {
        r with
            Run =
                fun ctx ->
                    let inputs = CWLProcessingUnit.getInputs ctx.ProcessingUnit |> Seq.toList

                    inputs
                    |> List.mapi (fun i inp ->
                        if System.String.IsNullOrWhiteSpace inp.Name then
                            Some(issue r Error (sprintf "inputs[%d]" i) "Input has an empty name.")
                        else
                            None
                    )
                    |> List.choose id
    }

/// COM.003 — outputs must not have empty names.
let outputNamesNonEmpty: ValidationRule =
    let r = {
        Id = RuleId "COM.003"
        Description = "Every output must have a non-empty name."
        Run = fun _ -> []
    }

    {
        r with
            Run =
                fun ctx ->
                    let outputs = CWLProcessingUnit.getOutputs ctx.ProcessingUnit |> Seq.toList

                    outputs
                    |> List.mapi (fun i outp ->
                        if System.String.IsNullOrWhiteSpace outp.Name then
                            Some(issue r Error (sprintf "outputs[%d]" i) "Output has an empty name.")
                        else
                            None
                    )
                    |> List.choose id
    }

/// All common rules, in execution order.
let all = [
    cwlVersionRequired
    inputNamesNonEmpty
    outputNamesNonEmpty
]
