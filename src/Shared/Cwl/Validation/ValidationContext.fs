/// Validation context: information available to every rule.
module CWLBuilder.Validation.ValidationContext

open ARCtrl.CWL

/// When is validation being triggered?
type ValidationMode =
    /// On initial load from disk — lenient; report but don't block.
    | OnLoad
    /// Live editing — surface warnings and errors as the user types.
    | Live
    /// Before save — strict; Error-severity issues block the save.
    | OnSave

/// Immutable context passed to every validation rule.
type ValidationContext = {
    /// The processing unit being validated.
    ProcessingUnit: CWLProcessingUnit
    /// CWL version string from the document, e.g. "v1.2", "v1.1".
    CwlVersion: string
    /// When the validation is being triggered.
    Mode: ValidationMode
}

/// Construct a context from a processing unit and mode.
let create (pu: CWLProcessingUnit) (cwlVersion: string) (mode: ValidationMode) : ValidationContext =
    { ProcessingUnit = pu; CwlVersion = cwlVersion; Mode = mode }

/// Advisory rules (Info/Warning) should be suppressed during Live editing.
let includeAdvisoryIssues (mode: ValidationMode) =
    mode <> Live
