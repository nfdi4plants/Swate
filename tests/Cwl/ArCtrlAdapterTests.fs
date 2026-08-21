module Swate.Tests.Cwl.ArCtrlAdapterTests

open System.IO
open Expecto
open ARCtrl.CWL
open Swate.Components.Shared.Cwl.CwlService
open Swate.Tests.Cwl.TestFixtures
open Swate.Components.Shared.Cwl.Adapters.ArCtrlDecode
open Swate.Components.Shared.Cwl.Adapters.ArCtrlEncode
open Swate.Components.Shared.Cwl.Adapters.ValidationAdapter
open Swate.Components.Shared.Cwl.Documents.Common
open Swate.Components.Shared.Cwl.Documents.Types
open Swate.Components.Shared.Cwl.Validation.ValidationContext

module GraphAdapter = Swate.Components.Shared.Cwl.Adapters.WorkflowGraphAdapter

let private resolveRunReferences (workflowPath: string) (yaml: string) =
    let tryDecodeCwlFile (filePath: string) =
        if File.Exists filePath then
            File.ReadAllText filePath |> Decode.decodeCWLProcessingUnit |> Some
        else
            None

    let processingUnit = Decode.decodeCWLProcessingUnit yaml

    ARCtrl.CWLRunResolver.resolveRunReferencesFromLookup workflowPath processingUnit tryDecodeCwlFile
    |> Encode.encodeProcessingUnit

let adapterRoundtripTests =
    testList "ArCtrl adapters" [
        test "command line tool decode/encode roundtrip preserves key fields" {
            let processingUnit = Decode.decodeCWLProcessingUnit minimalToolYaml
            let document = fromProcessingUnit processingUnit
            let encodedYaml = document |> toProcessingUnit |> Encode.encodeProcessingUnit

            Expect.isTrue (encodedYaml.Contains "CommandLineTool") "Encoded document should remain a CommandLineTool"
            Expect.isTrue (encodedYaml.Contains "echo") "baseCommand should survive adapter roundtrip"
            Expect.isTrue (encodedYaml.Contains "message") "Input name should survive adapter roundtrip"
        }

        test "workflow decode from resolved processing unit preserves external run reference on encode" {
            let resolvedYaml =
                resolveRunReferences workflowWithExternalRunPath workflowWithExternalRunYaml

            let loadedState =
                match
                    tryLoadToEditorWithResolved
                        workflowWithExternalRunYaml
                        (Some resolvedYaml)
                        workflowWithExternalRunPath
                with
                | Ok state -> state
                | Error message -> failtestf "Expected workflow load success but got %s" message

            let document = fromProcessingUnit loadedState.ProcessingUnit
            let encodedProcessingUnit = document |> toProcessingUnit

            match encodedProcessingUnit with
            | CWLProcessingUnit.Workflow workflow ->
                match workflow.Steps.[0].Run with
                | WorkflowStepRun.RunString runTarget ->
                    Expect.equal
                        runTarget
                        "tools/echo.cwl"
                        "Adapter encode should restore external workflow run strings"
                | _ -> failtest "Expected external workflow run string after adapter encode"
            | _ -> failtest "Expected Workflow processing unit"
        }

        test "validation adapter reuses the current validation engine" {
            let document =
                Decode.decodeCWLProcessingUnit minimalExpressionToolYaml |> fromProcessingUnit

            let result = validateDocument OnSave document

            Expect.isTrue result.IsValid "Minimal expression tool fixture should validate through the adapter"
        }

        test "workflow graph adapter projects nodes, edges, and diagnostics from immutable workflow model" {
            let document =
                Decode.decodeCWLProcessingUnit minimalWorkflowYaml |> fromProcessingUnit

            match document with
            | WorkflowDoc workflow ->
                let canvasGraph = GraphAdapter.toCanvasGraph workflow
                let readModel = GraphAdapter.buildWorkflowGraphReadModel workflow None None

                Expect.isGreaterThan canvasGraph.Nodes.Count 0 "Canvas graph should contain nodes"
                Expect.isGreaterThan canvasGraph.Edges.Count 0 "Canvas graph should contain edges"
                Expect.isGreaterThan readModel.NodeCount 0 "Read model should contain nodes"
                Expect.isGreaterThan readModel.EdgeCount 0 "Read model should contain edges"
            | _ -> failtest "Expected Workflow document"
        }

        test "requirements and hints fixture survives adapter encode" {
            let processingUnit = Decode.decodeCWLProcessingUnit toolWithRequirementsAndHintsYaml
            let document = fromProcessingUnit processingUnit
            let encodedYaml = document |> toProcessingUnit |> Encode.encodeProcessingUnit

            Expect.isTrue
                (encodedYaml.Contains "InlineJavascriptRequirement")
                "Requirements should survive adapter encode"

            Expect.isTrue (encodedYaml.Contains "DockerRequirement") "Hints should survive adapter encode"
        }

        test "adapter roundtrip preserves nested custom metadata" {
            let processingUnit = Decode.decodeCWLProcessingUnit toolWithNestedMetadataYaml
            let document = fromProcessingUnit processingUnit
            let encodedYaml = document |> toProcessingUnit |> Encode.encodeProcessingUnit
            let roundtripped = Decode.decodeCWLProcessingUnit encodedYaml

            match fromProcessingUnit roundtripped with
            | CommandLineToolDoc tool ->
                let expectedMetadata =
                    Map.ofList [
                        "customMetadata",
                        MetadataObject(
                            Map.ofList [
                                "nested",
                                MetadataObject(
                                    Map.ofList [
                                        "enabled", MetadataBool true
                                        "thresholds", MetadataArray [ MetadataInt 1L; MetadataFloat 2.5 ]
                                        "labels",
                                        MetadataObject(
                                            Map.ofList [
                                                "primary", MetadataString "alpha"
                                                "secondary", MetadataString "beta"
                                            ]
                                        )
                                    ]
                                )
                            ]
                        )
                    ]

                Expect.equal
                    tool.Metadata
                    expectedMetadata
                    "Nested metadata should survive decode/encode/decode adapter roundtrip"
            | _ -> failtest "Expected CommandLineTool processing unit"
        }
    ]

[<Tests>]
let allTests = adapterRoundtripTests
