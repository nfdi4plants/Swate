/// CWL YAML test fixtures and roundtrip helpers.
/// These were previously in production code (CwlHandling/Roundtrip.fs) and have
/// been relocated here so production assemblies contain only production code.
module Swate.Tests.Cwl.TestFixtures

open System.IO
open ARCtrl.CWL

let private fixtureRoot =
    Path.GetFullPath(Path.Combine(__SOURCE_DIRECTORY__, "Fixtures", "Cwl"))

let private combineFixturePath (segments: string list) =
    segments
    |> List.fold (fun current segment -> Path.Combine(current, segment)) fixtureRoot

let private normalizeFixtureText (text: string) = text.Replace("\r\n", "\n")

let private readFixture (segments: string list) =
    combineFixturePath segments |> File.ReadAllText |> normalizeFixtureText

// ---------------------------------------------------------------------------
// File-backed CWL fixtures for testing
// ---------------------------------------------------------------------------

let minimalToolYaml = readFixture [ "minimal-command-line-tool.cwl" ]

let minimalWorkflowYaml = readFixture [ "minimal-workflow.cwl" ]

let minimalExpressionToolYaml = readFixture [ "minimal-expression-tool.cwl" ]

let minimalOperationYaml = readFixture [ "minimal-operation.cwl" ]

let workflowWithExternalRunPath =
    combineFixturePath [ "workflow-with-external-run"; "main.cwl" ]

let workflowWithExternalRunToolPath =
    combineFixturePath [ "workflow-with-external-run"; "tools"; "echo.cwl" ]

let workflowWithExternalRunYaml =
    readFixture [ "workflow-with-external-run"; "main.cwl" ]

let workflowWithSequenceIoPath =
    combineFixturePath [ "workflow-with-sequence-io"; "main.cwl" ]

let workflowWithSequenceIoYaml =
    readFixture [ "workflow-with-sequence-io"; "main.cwl" ]

let toolWithRequirementsAndHintsYaml =
    readFixture [ "command-line-tool-with-requirements-hints.cwl" ]

let toolWithNestedMetadataYaml =
    readFixture [ "command-line-tool-with-nested-metadata.cwl" ]

// ---------------------------------------------------------------------------
// Roundtrip helpers
// ---------------------------------------------------------------------------

/// Decode then re-encode a CWL YAML string.
let runRoundtrip (yaml: string) : CWLProcessingUnit * string =
    let pu = Decode.decodeCWLProcessingUnit yaml
    let encoded = Encode.encodeProcessingUnit pu
    pu, encoded

/// Verify that a minimal tool can be roundtripped.
let verifyToolRoundtrip () =
    let pu, encoded = runRoundtrip minimalToolYaml

    match pu with
    | CWLProcessingUnit.CommandLineTool _ -> Ok encoded
    | _ -> Error "Expected CommandLineTool but got different type"

/// Verify that a minimal workflow can be roundtripped.
let verifyWorkflowRoundtrip () =
    let pu, encoded = runRoundtrip minimalWorkflowYaml

    match pu with
    | CWLProcessingUnit.Workflow _ -> Ok encoded
    | _ -> Error "Expected Workflow but got different type"

/// Verify that a minimal expression tool can be roundtripped.
let verifyExpressionToolRoundtrip () =
    let pu, encoded = runRoundtrip minimalExpressionToolYaml

    match pu with
    | CWLProcessingUnit.ExpressionTool _ -> Ok encoded
    | _ -> Error "Expected ExpressionTool but got different type"

/// Verify that a minimal operation can be roundtripped.
let verifyOperationRoundtrip () =
    let pu, encoded = runRoundtrip minimalOperationYaml

    match pu with
    | CWLProcessingUnit.Operation _ -> Ok encoded
    | _ -> Error "Expected Operation but got different type"
