/// Service layer that wraps ARCtrl Decode/Encode pipelines.
/// Provides a clean API surface for the UI to load, save, and manipulate
/// CWL processing units without leaking YAMLicious/DynamicObj details.
module Swate.Components.Shared.Cwl.CwlService

open ARCtrl.CWL
open ARCtrl
open Swate.Components.Shared.Cwl.EditorTypes
open Swate.Components.Shared.Cwl.WorkflowMutations

let private hasSubstantiveYamlContent (yaml: string) =
    if isNull yaml then
        false
    else
        yaml.Replace("\r\n", "\n").Replace("\r", "\n").Split('\n')
        |> Array.exists (fun line ->
            let trimmed = line.Trim()

            trimmed <> ""
            && not (trimmed.StartsWith("#"))
            && trimmed <> "---"
            && trimmed <> "..."
        )

let private annotateResolvedWorkflowStepsFromRaw
    (rawWorkflow: CWLWorkflowDescription)
    (resolvedWorkflow: CWLWorkflowDescription)
    (workflowPath: string)
    =
    // ARCtrl run resolution currently replaces run-string references with inline run payloads.
    // Preserve original run-string tokens so save/save-as can roundtrip external step references.
    let rawRunByStepId =
        rawWorkflow.Steps
        |> Seq.choose (fun step ->
            match step.Run with
            | WorkflowStepRun.RunString runString when System.String.IsNullOrWhiteSpace runString |> not ->
                Some(step.Id, runString)
            | _ -> None
        )
        |> Map.ofSeq

    for step in resolvedWorkflow.Steps do
        match rawRunByStepId |> Map.tryFind step.Id with
        | Some runString -> setWorkflowStepExternalRunMetadata workflowPath step runString
        | None -> ()

let private tryAnnotateResolvedRuns (rawYaml: string) (resolvedEditorState: EditorState) =
    try
        let rawProcessingUnit = Decode.decodeCWLProcessingUnit rawYaml

        match rawProcessingUnit, resolvedEditorState.ProcessingUnit, resolvedEditorState.FilePath with
        | CWLProcessingUnit.Workflow rawWorkflow, CWLProcessingUnit.Workflow resolvedWorkflow, Some workflowPath ->
            annotateResolvedWorkflowStepsFromRaw rawWorkflow resolvedWorkflow workflowPath
            resolvedEditorState
        | _ -> resolvedEditorState
    with _ ->
        resolvedEditorState

let private applyWorkflowRunReferenceWriteback
    (workflow: CWLWorkflowDescription)
    (sourceWorkflowPath: string option)
    (targetWorkflowPath: string)
    =
    for step in workflow.Steps do
        match tryGetWorkflowStepExternalRunPathForSave step targetWorkflowPath sourceWorkflowPath with
        | Some runString -> step.Run <- WorkflowStepRun.RunString runString
        | None -> ()

// ---------------------------------------------------------------------------
// Load (Decode)
// ---------------------------------------------------------------------------

/// Decode a CWL YAML string into a CWLProcessingUnit.
/// Wraps Decode.decodeCWLProcessingUnit and converts exceptions to Result.
let tryLoad (yaml: string) : Result<CWLProcessingUnit, string> =
    if not (hasSubstantiveYamlContent yaml) then
        Error "CWL document is empty."
    else
        try
            Decode.decodeCWLProcessingUnit yaml |> Ok
        with ex ->
            let message = ex.Message

            if message.Contains("Field not found: class: Object []", System.StringComparison.OrdinalIgnoreCase) then
                Error "CWL root 'class' is missing or empty."
            else
                Error(sprintf "Failed to decode CWL: %s" message)

/// Load a CWL YAML string and wrap it in an EditorState.
let tryLoadToEditor (yaml: string) (filePath: string) : Result<EditorState, string> =
    tryLoad yaml |> Result.map (fun pu -> fromLoaded pu filePath)

/// Load a CWL YAML string and wrap it in an EditorState using a resolved view for editing.
/// Raw YAML is used to preserve original workflow step run-string references for save/writeback.
let tryLoadToEditorWithResolved
    (rawYaml: string)
    (resolvedYaml: string option)
    (filePath: string)
    : Result<EditorState, string> =
    let yamlToDecode = resolvedYaml |> Option.defaultValue rawYaml

    tryLoadToEditor yamlToDecode filePath
    |> Result.map (tryAnnotateResolvedRuns rawYaml)

// ---------------------------------------------------------------------------
// Save (Encode)
// ---------------------------------------------------------------------------

/// Encode a CWLProcessingUnit back to a CWL YAML string.
let save (pu: CWLProcessingUnit) : string = Encode.encodeProcessingUnit pu

/// Encode the processing unit from an EditorState.
/// Keep serialization transparent; formatting/canonicalization choices are handled elsewhere.
let saveFromEditor (state: EditorState) : string = save state.ProcessingUnit

/// Encode an editor state to CWL YAML for a specific save target.
/// For workflow steps loaded from external files, this restores run-string references
/// (relative to the save target) instead of serializing resolved inline objects.
let saveFromEditorForPath (state: EditorState) (targetPath: string) : string =
    let normalizedTargetPath = ArcPathHelper.normalize targetPath

    match state.ProcessingUnit with
    | CWLProcessingUnit.Workflow workflow ->
        let originalRuns =
            workflow.Steps |> Seq.map (fun step -> step, step.Run) |> Seq.toArray

        try
            applyWorkflowRunReferenceWriteback workflow state.FilePath normalizedTargetPath
            CWLProcessingUnit.Workflow workflow |> save
        finally
            for step, originalRun in originalRuns do
                step.Run <- originalRun
    | processingUnit -> save processingUnit

// ---------------------------------------------------------------------------
// Roundtrip verification
// ---------------------------------------------------------------------------

/// Decode then re-encode a CWL string. Returns Ok with the re-encoded string
/// if the roundtrip succeeds, or Error with the failure message.
let roundtrip (yaml: string) : Result<string, string> = tryLoad yaml |> Result.map save
