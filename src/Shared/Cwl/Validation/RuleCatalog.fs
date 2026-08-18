/// Central catalog of all validation rules, in execution order.
module Swate.Components.Shared.Cwl.Validation.RuleCatalog

open Swate.Components.Shared.Cwl.Validation.ValidationRule
open Swate.Components.Shared.Cwl.Validation.Rules

/// All registered rules. Common rules run first, then type-specific, then requirements.
let allRules: ValidationRule list =
    List.concat [
        CommonRules.all
        CommandLineToolRules.all
        WorkflowRules.all
        ExpressionToolRules.all
        RequirementsRules.all
    ]
