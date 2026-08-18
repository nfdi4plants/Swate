/// Core types for the CWLBuilder validation subsystem.
/// Replaces the lightweight Validation.fs in CWLBuilder.Domain with a
/// rule-based engine that lives in its own project.
module CWLBuilder.Validation.ValidationTypes

/// How severe is this issue?
type Severity =
    | Error
    | Warning
    | Info

/// A unique identifier for a validation rule.
/// Format: "<Category>.<Number>" e.g. "CLT.001", "WF.002", "REQ.001"
type RuleId = RuleId of string

/// A single validation finding.
type ValidationIssue = {
    RuleId: RuleId
    Severity: Severity
    /// JSON-pointer-style path to the offending element, e.g. "inputs[0].type"
    Path: string
    Message: string
}

/// Aggregate result of running all applicable rules.
type ValidationResult = {
    Issues: ValidationIssue list
}
with
    /// True when no Error-severity issues exist.
    member this.IsValid =
        this.Issues |> List.forall (fun i -> i.Severity <> Error)

    /// Only the issues that would block a save.
    member this.Errors =
        this.Issues |> List.filter (fun i -> i.Severity = Error)

    /// Combine two results.
    static member merge (a: ValidationResult) (b: ValidationResult) =
        { Issues = a.Issues @ b.Issues }

    static member ok = { Issues = [] }
