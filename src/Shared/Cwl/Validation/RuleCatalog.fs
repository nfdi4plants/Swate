/// Central catalog of all validation rules, in execution order.
module CWLBuilder.Validation.RuleCatalog

open CWLBuilder.Validation.ValidationRule
open CWLBuilder.Validation.Rules

/// All registered rules. Common rules run first, then type-specific, then requirements.
let allRules : ValidationRule list =
    List.concat [
        CommonRules.all
        CommandLineToolRules.all
        WorkflowRules.all
        ExpressionToolRules.all
        RequirementsRules.all
    ]
