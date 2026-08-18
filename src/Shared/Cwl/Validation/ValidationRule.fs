/// Contract for a single validation rule.
module Swate.Components.Shared.Cwl.Validation.ValidationRule

open Swate.Components.Shared.Cwl.Validation.ValidationTypes
open Swate.Components.Shared.Cwl.Validation.ValidationContext

/// A validation rule is a function that examines the context and returns
/// zero or more issues. Rules are pure functions — no side effects.
type ValidationRule = {
    /// Unique rule identifier.
    Id: RuleId
    /// Human-readable description of what this rule checks.
    Description: string
    /// The rule implementation. Returns issues found.
    Run: ValidationContext -> ValidationIssue list
}

/// Helper: create an issue stamped with this rule's id.
let issue (rule: ValidationRule) (sev: Severity) (path: string) (msg: string) : ValidationIssue = {
    RuleId = rule.Id
    Severity = sev
    Path = path
    Message = msg
}
