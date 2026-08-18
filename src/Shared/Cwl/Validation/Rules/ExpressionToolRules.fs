/// Rules specific to ExpressionTool documents.
module Swate.Components.Shared.Cwl.Validation.Rules.ExpressionToolRules

open ARCtrl.CWL
open Swate.Components.Shared.Cwl.Validation.ValidationTypes
open Swate.Components.Shared.Cwl.Validation.ValidationContext
open Swate.Components.Shared.Cwl.Validation.ValidationRule

/// EXP.001 — expression must not be empty.
let expressionRequired: ValidationRule =
    let r = {
        Id = RuleId "EXP.001"
        Description = "ExpressionTool must have a non-empty expression."
        Run = fun _ -> []
    }

    {
        r with
            Run =
                fun ctx ->
                    match ctx.ProcessingUnit with
                    | CWLProcessingUnit.ExpressionTool et ->
                        if System.String.IsNullOrWhiteSpace et.Expression then
                            [
                                issue r Error "expression" "ExpressionTool must have a non-empty expression."
                            ]
                        else
                            []
                    | _ -> []
    }

/// EXP.002 — ExpressionTool with JS expression should have InlineJavascriptRequirement.
let inlineJavascriptRequired: ValidationRule =
    let r = {
        Id = RuleId "EXP.002"
        Description = "ExpressionTool using JS should have InlineJavascriptRequirement."
        Run = fun _ -> []
    }

    {
        r with
            Run =
                fun ctx ->
                    match ctx.ProcessingUnit with
                    | CWLProcessingUnit.ExpressionTool et ->
                        let usesJs = et.Expression.Contains("${") || et.Expression.Contains("$(")

                        let hasReq =
                            et.Requirements
                            |> Option.defaultValue (ResizeArray())
                            |> Seq.exists (fun r ->
                                match r with
                                | Requirement.InlineJavascriptRequirement _ -> true
                                | _ -> false
                            )

                        if usesJs && not hasReq then
                            [
                                issue
                                    r
                                    Error
                                    "requirements"
                                    "ExpressionTool uses JavaScript but lacks InlineJavascriptRequirement."
                            ]
                        else
                            []
                    | _ -> []
    }

/// All ExpressionTool rules.
let all = [ expressionRequired; inlineJavascriptRequired ]
