/// Validation engine: runs all registered rules against a processing unit.
module CWLBuilder.Validation.ValidationEngine

open ARCtrl.CWL
open CWLBuilder.Validation.ValidationTypes
open CWLBuilder.Validation.ValidationContext

/// Run all rules from the catalog against the given context.
let validate (ctx: ValidationContext) : ValidationResult =
    let issues =
        RuleCatalog.allRules
        |> List.collect (fun rule -> rule.Run ctx)
    { Issues = issues }

/// Convenience: validate a processing unit with a mode, auto-extracting cwlVersion.
let validateProcessingUnit (pu: CWLProcessingUnit) (mode: ValidationMode) : ValidationResult =
    let cwlVer =
        match pu with
        | CWLProcessingUnit.CommandLineTool td -> td.CWLVersion
        | CWLProcessingUnit.Workflow wd -> wd.CWLVersion
        | CWLProcessingUnit.ExpressionTool et -> et.CWLVersion
        | CWLProcessingUnit.Operation op -> op.CWLVersion
    let ctx = create pu cwlVer mode
    validate ctx

/// Quick check: is the processing unit valid (no Error-severity issues)?
let isValid (pu: CWLProcessingUnit) : bool =
    let result = validateProcessingUnit pu OnSave
    result.IsValid
