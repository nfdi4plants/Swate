module Swate.Tests.Cwl.RoundtripTests

open System
open System.IO
open Expecto
open ARCtrl.CWL
open Swate.Components.Shared.Cwl.CwlService
open Swate.Tests.Cwl.TestFixtures
open Swate.Components.Shared.Cwl.CwlDefaults
open Swate.Components.Shared.Cwl.EditorTypes
open Swate.Components.Shared.Cwl.EditorMutations
open Swate.Components.Shared.Cwl.CommandLineToolMutations
open Swate.Components.Shared.Cwl.RequirementMutations
open Swate.Components.Shared.Cwl.WorkflowMutations
open Swate.Components.Shared.Cwl.ExpressionToolMutations
open Swate.Components.Shared.Cwl.WorkflowCanvasAdapter
open Swate.Components.Shared.Cwl.Validation.ValidationTypes
open Swate.Components.Shared.Cwl.Validation.ValidationContext
open Swate.Components.Shared.Cwl.Validation.ValidationEngine

let roundtripTests =
    testList "Roundtrip" [
        test "Minimal CommandLineTool round-trips" {
            let result = verifyToolRoundtrip ()

            match result with
            | Result.Ok encoded ->
                Expect.isTrue (encoded.Contains "CommandLineTool") "Should contain class"
                Expect.isTrue (encoded.Contains "echo") "Should contain baseCommand"
            | Result.Error msg -> failtest msg
        }

        test "Minimal Workflow round-trips" {
            let result = verifyWorkflowRoundtrip ()

            match result with
            | Result.Ok encoded ->
                Expect.isTrue (encoded.Contains "Workflow") "Should contain class"
                Expect.isTrue (encoded.Contains "step1") "Should contain step"
            | Result.Error msg -> failtest msg
        }

        test "Minimal ExpressionTool round-trips" {
            let result = verifyExpressionToolRoundtrip ()

            match result with
            | Result.Ok encoded ->
                Expect.isTrue (encoded.Contains "ExpressionTool") "Should contain class"
                Expect.isTrue (encoded.Contains "expression") "Should contain expression"
            | Result.Error msg -> failtest msg
        }

        test "ExpressionTool single-line expression encodes as quoted scalar and decodes unchanged" {
            let expression = "${return {'output_val': inputs.input_val + 1};}"
            let pu = Decode.decodeCWLProcessingUnit minimalExpressionToolYaml

            match pu with
            | CWLProcessingUnit.ExpressionTool et ->
                et.Expression <- expression
                let encoded = Encode.encodeProcessingUnit pu

                Expect.isTrue
                    (encoded.Contains "expression: \"")
                    "Single-line expression should be quoted in YAML output"

                match Decode.decodeCWLProcessingUnit encoded with
                | CWLProcessingUnit.ExpressionTool decoded ->
                    Expect.equal decoded.Expression expression "Quoted expression should decode back to original value"
                | _ -> failtest "Expected ExpressionTool after decode"
            | _ -> failtest "Expected ExpressionTool fixture"
        }

        test "ExpressionTool multiline expression preserves blank lines via block scalar roundtrip" {
            let normalize (value: string) =
                value.Replace("\r\n", "\n").TrimEnd('\r', '\n')

            let expression =
                "${\n  const next = inputs.input_val + 1;\n\n  return {'output_val': next};\n}"

            let pu = Decode.decodeCWLProcessingUnit minimalExpressionToolYaml

            match pu with
            | CWLProcessingUnit.ExpressionTool et ->
                et.Expression <- expression
                let encoded = Encode.encodeProcessingUnit pu

                Expect.isTrue
                    (encoded.Contains "expression: |")
                    "Multiline expression should be emitted as block scalar"

                match Decode.decodeCWLProcessingUnit encoded with
                | CWLProcessingUnit.ExpressionTool decoded ->
                    Expect.isTrue (decoded.Expression.Contains "\n\n") "Decoded expression should preserve blank lines"

                    Expect.equal
                        (normalize decoded.Expression)
                        (normalize expression)
                        "Multiline expression should roundtrip text content"
                | _ -> failtest "Expected ExpressionTool after decode"
            | _ -> failtest "Expected ExpressionTool fixture"
        }

        test "Minimal Operation round-trips" {
            let result = verifyOperationRoundtrip ()

            match result with
            | Result.Ok encoded ->
                Expect.isTrue (encoded.Contains "Operation") "Should contain class"
                Expect.isTrue (encoded.Contains "input_val") "Should contain operation input"
            | Result.Error msg -> failtest msg
        }

        test "tryLoad returns Ok for valid CWL" {
            let result = tryLoad minimalToolYaml
            Expect.isOk result "Should decode valid CWL"
        }

        test "tryLoad returns Error for invalid CWL" {
            let result = tryLoad "this is not valid CWL"
            Expect.isError result "Should fail on invalid CWL"
        }

        test "tryLoad returns clear error for empty CWL input" {
            let result = tryLoad ""
            Expect.equal result (Result.Error "CWL document is empty.") "Empty input should return explicit error"
        }

        test "tryLoad returns clear error for comment-only CWL input" {
            let result = tryLoad "# only a comment\n\n---\n...\n"

            Expect.equal
                result
                (Result.Error "CWL document is empty.")
                "Comment/doc-marker-only input should return explicit error"
        }

        test "roundtrip function succeeds for tool yaml" {
            let result = roundtrip minimalToolYaml
            Expect.isOk result "Roundtrip should succeed"
        }
    ]

let goldenFixtureTests =
    testList "Golden fixtures" [
        test "workflow fixture with external run keeps relative run reference on roundtrip" {
            Expect.isTrue (File.Exists workflowWithExternalRunPath) "Workflow fixture file should exist"
            Expect.isTrue (File.Exists workflowWithExternalRunToolPath) "Nested tool fixture file should exist"

            let result = roundtrip workflowWithExternalRunYaml

            match result with
            | Result.Ok encoded ->
                Expect.isTrue
                    (encoded.Contains "run: tools/echo.cwl")
                    "Workflow roundtrip should preserve relative external run path"
            | Result.Error message -> failtest message
        }

        test "command line tool fixture with requirements and hints roundtrips" {
            let result = roundtrip toolWithRequirementsAndHintsYaml

            match result with
            | Result.Ok encoded ->
                Expect.isTrue
                    (encoded.Contains "InlineJavascriptRequirement")
                    "Roundtrip should preserve representative requirement content"

                Expect.isTrue
                    (encoded.Contains "DockerRequirement")
                    "Roundtrip should preserve representative hint content"
            | Result.Error message -> failtest message
        }
    ]

let editorTests =
    testList "Editor" [
        test "createNew CommandLineTool produces valid state" {
            let state = createNew CommandLineTool
            Expect.equal state.Version 0 "Initial version should be 0"
            Expect.isFalse state.IsDirty "Should not be dirty"
            Expect.isNone state.FilePath "Should have no file path"

            match state.ProcessingUnit with
            | CWLProcessingUnit.CommandLineTool _ -> ()
            | _ -> failtest "Expected CommandLineTool"
        }

        test "createNew Workflow produces valid state" {
            let state = createNew Workflow

            match state.ProcessingUnit with
            | CWLProcessingUnit.Workflow _ -> ()
            | _ -> failtest "Expected Workflow"
        }

        test "createNew ExpressionTool produces valid state" {
            let state = createNew ExpressionTool

            match state.ProcessingUnit with
            | CWLProcessingUnit.ExpressionTool _ -> ()
            | _ -> failtest "Expected ExpressionTool"
        }

        test "touch increments version and marks dirty" {
            let state = createNew CommandLineTool
            let touched = touch state
            Expect.equal touched.Version 1 "Version should be 1"
            Expect.isTrue touched.IsDirty "Should be dirty"
        }

        test "fromLoaded sets file path and version" {
            let pu = Decode.decodeCWLProcessingUnit minimalToolYaml
            let state = fromLoaded pu "test.cwl"
            Expect.equal state.FilePath (Some "test.cwl") "Should set file path"
            Expect.equal state.CwlVersion DefaultCwlVersion "Should extract version"
        }

        test "createNew serializes with cwlVersion by default" {
            let toolYaml = createNew CommandLineTool |> saveFromEditor
            let workflowYaml = createNew Workflow |> saveFromEditor
            let expressionYaml = createNew ExpressionTool |> saveFromEditor

            let expected = sprintf "cwlVersion: %s" DefaultCwlVersion
            Expect.isTrue (toolYaml.Contains expected) "Tool should include default cwlVersion"
            Expect.isTrue (workflowYaml.Contains expected) "Workflow should include default cwlVersion"
            Expect.isTrue (expressionYaml.Contains expected) "ExpressionTool should include default cwlVersion"
        }

    ]

let validationTests =
    testList "Validation" [
        test "Empty tool passes validation" {
            let td = CWLToolDescription(ResizeArray())
            let result = validateProcessingUnit (CWLProcessingUnit.CommandLineTool td) OnSave
            Expect.isTrue result.IsValid "Should be valid (only warnings)"
        }

        test "Empty workflow gives warning about no steps" {
            let wd = CWLWorkflowDescription(ResizeArray(), ResizeArray(), ResizeArray())
            let result = validateProcessingUnit (CWLProcessingUnit.Workflow wd) OnSave
            Expect.isTrue result.IsValid "Should be valid"

            Expect.isTrue
                (result.Issues |> List.exists (fun i -> i.Message.Contains "no steps"))
                "Should warn about no steps"
        }

        test "ExpressionTool with empty expression fails" {
            let et = CWLExpressionToolDescription(ResizeArray(), "")
            let result = validateProcessingUnit (CWLProcessingUnit.ExpressionTool et) OnSave
            Expect.isFalse result.IsValid "Should be invalid"
        }
    ]

let mutationTests =
    testList "Decode-Mutate-Encode" [
        test "Decode tool, add input, re-encode preserves new input" {
            // Decode
            let pu = Decode.decodeCWLProcessingUnit minimalToolYaml

            match pu with
            | CWLProcessingUnit.CommandLineTool td ->
                // Mutate: add a new input
                let newInput = CWLInput("extra_arg")
                newInput.SetProperty("type", CWLType.Int)

                let inputs =
                    match td.Inputs with
                    | Some existing -> existing
                    | None ->
                        let ra = ResizeArray() in
                        td.Inputs <- Some ra
                        ra

                inputs.Add(newInput)
                // Re-encode
                let yaml = Encode.encodeProcessingUnit pu
                Expect.isTrue (yaml.Contains "extra_arg") "Re-encoded YAML should contain the new input name"
                Expect.isTrue (yaml.Contains "echo") "Re-encoded YAML should still contain baseCommand"
            | _ -> failtest "Expected CommandLineTool"
        }

        test "Decode workflow, add step output, re-encode preserves mutation" {
            let pu = Decode.decodeCWLProcessingUnit minimalWorkflowYaml

            match pu with
            | CWLProcessingUnit.Workflow wd ->
                // Verify we can inspect the decoded workflow
                Expect.isGreaterThan wd.Steps.Count 0 "Should have at least one step"
                // Mutate: add a new output to step1
                let step = wd.Steps.[0]
                step.Out.Add(StepOutput.StepOutputString "extra_output")
                // Re-encode
                let yaml = Encode.encodeProcessingUnit pu
                Expect.isTrue (yaml.Contains "Workflow") "Re-encoded YAML should contain class"
                Expect.isTrue (yaml.Contains "step1") "Re-encoded YAML should contain step"
                Expect.isTrue (yaml.Contains "extra_output") "Re-encoded YAML should contain the new output"
            | _ -> failtest "Expected Workflow"
        }

        test "Decode tool, mutate via EditorState touch, version increments" {
            let result = tryLoadToEditor minimalToolYaml "test.cwl"

            match result with
            | Result.Ok state ->
                Expect.equal state.Version 0 "Initial version"
                let s2 = touch state
                Expect.equal s2.Version 1 "After touch"
                Expect.isTrue s2.IsDirty "Should be dirty"
                // Encode should still work
                let yaml = saveFromEditor s2
                Expect.isTrue (yaml.Contains "echo") "Encoded YAML still valid"
            | Result.Error msg -> failtest msg
        }

        test "saveFromEditorForPath preserves original workflow step run string for same file" {
            let rawWorkflowYaml =
                """
cwlVersion: v1.2
class: Workflow
inputs: {}
outputs:
    final:
        type: string
        outputSource: step1/out
steps:
    step1:
        run: tools/echo.cwl
        in: {}
        out: [out]
"""

            let resolvedWorkflowYaml =
                """
cwlVersion: v1.2
class: Workflow
inputs: {}
outputs:
    final:
        type: string
        outputSource: step1/out
steps:
    step1:
        run:
            cwlVersion: v1.2
            class: CommandLineTool
            baseCommand: echo
            inputs: {}
            outputs:
                out:
                    type: string
        in: {}
        out: [out]
"""

            let workflowPath = Path.Combine("C:\\", "projects", "wf", "main.cwl")

            match tryLoadToEditorWithResolved rawWorkflowYaml (Some resolvedWorkflowYaml) workflowPath with
            | Result.Error message -> failtestf "Expected load success but got %s" message
            | Result.Ok state ->
                let savedYaml = saveFromEditorForPath state workflowPath

                Expect.isTrue
                    (savedYaml.Contains "run: tools/echo.cwl")
                    "Save should keep original run string when saving same file"

                Expect.isFalse
                    (savedYaml.Contains "class: CommandLineTool")
                    "Save should not inline resolved run object"
        }

        test "saveFromEditorForPath rewrites workflow step run string relative to save target" {
            let rawWorkflowYaml =
                """
cwlVersion: v1.2
class: Workflow
inputs: {}
outputs:
    final:
        type: string
        outputSource: step1/out
steps:
    step1:
        run: tools/echo.cwl
        in: {}
        out: [out]
"""

            let resolvedWorkflowYaml =
                """
cwlVersion: v1.2
class: Workflow
inputs: {}
outputs:
    final:
        type: string
        outputSource: step1/out
steps:
    step1:
        run:
            cwlVersion: v1.2
            class: CommandLineTool
            baseCommand: echo
            inputs: {}
            outputs:
                out:
                    type: string
        in: {}
        out: [out]
"""

            let sourceWorkflowPath = Path.Combine("C:\\", "projects", "wf", "main.cwl")
            let copyWorkflowPath = Path.Combine("C:\\", "projects", "copies", "main-copy.cwl")

            match tryLoadToEditorWithResolved rawWorkflowYaml (Some resolvedWorkflowYaml) sourceWorkflowPath with
            | Result.Error message -> failtestf "Expected load success but got %s" message
            | Result.Ok state ->
                let savedYaml = saveFromEditorForPath state copyWorkflowPath

                Expect.isTrue
                    (savedYaml.Contains "run: ../wf/tools/echo.cwl")
                    "Save-as copy should rewrite run path relative to new workflow location"

                Expect.isFalse
                    (savedYaml.Contains "class: CommandLineTool")
                    "Save-as copy should not inline resolved run object"
        }

    ]

let editorMutationHelperTests =
    testList "Editor mutation helpers" [
        test "removeAtAndSelectNext removes selected index and returns next valid index" {
            let items = ResizeArray [ "a"; "b"; "c" ]
            let next = removeAtAndSelectNext (Some 1) items
            Expect.sequenceEqual items [ "a"; "c" ] "Item should be removed"
            Expect.equal next (Some 1) "Selection should stay at same index when possible"
        }

        test "moveUp swaps current item with previous item" {
            let items = ResizeArray [ "a"; "b"; "c" ]
            let next = moveUp (Some 2) items
            Expect.sequenceEqual items [ "a"; "c"; "b" ] "Item should move up"
            Expect.equal next (Some 1) "Selection should follow moved item"
        }

        test "moveDown swaps current item with next item" {
            let items = ResizeArray [ "a"; "b"; "c" ]
            let next = moveDown (Some 0) items
            Expect.sequenceEqual items [ "b"; "a"; "c" ] "Item should move down"
            Expect.equal next (Some 1) "Selection should follow moved item"
        }

        test "cloneInputWithName preserves fields and custom properties" {
            let source = CWLInput("original")
            source.Type_ <- Some CWLType.String
            source.Optional <- Some true
            source.InputBinding <- Some(InputBinding.create (prefix = "--in", position = 1))
            source.SetProperty("doc", "input docs")

            let cloned = cloneInputWithName source "renamed"

            Expect.equal cloned.Name "renamed" "Clone should have requested new name"
            Expect.equal cloned.Type_ source.Type_ "Clone should preserve type"
            Expect.equal cloned.Optional source.Optional "Clone should preserve optional flag"
            Expect.equal cloned.InputBinding source.InputBinding "Clone should preserve input binding"

            Expect.equal
                (cloned.GetPropertyValue("doc").ToString())
                "input docs"
                "Clone should preserve custom properties"

            Expect.notEqual cloned.Name source.Name "Clone rename must not mutate source"
        }

        test "cloneInputWithName ignores dynamic name shadow property" {
            let source = CWLInput("original")
            source.SetProperty("name", "shadow-name")
            source.SetProperty("doc", "input docs")

            let cloned = cloneInputWithName source "renamed"

            let hasShadowName =
                cloned.GetProperties(false)
                |> Seq.exists (fun kvp -> String.Equals(kvp.Key, "name", StringComparison.OrdinalIgnoreCase))

            Expect.equal cloned.Name "renamed" "Clone should keep constructor-based renamed input key"
            Expect.isFalse hasShadowName "Clone should not carry a dynamic name shadow property"

            Expect.equal
                (cloned.GetPropertyValue("doc").ToString())
                "input docs"
                "Clone should still preserve non-name custom properties"
        }

        test "cloneOutputWithName preserves fields and custom properties" {
            let source = CWLOutput("original")
            source.Type_ <- Some(CWLType.file ())
            source.OutputSource <- Some(OutputSource.Single "step/out")
            source.OutputBinding <- Some(OutputBinding.create (glob = "*.txt"))
            source.SetProperty("label", "report output")

            let cloned = cloneOutputWithName source "renamed"

            Expect.equal cloned.Name "renamed" "Clone should have requested new name"
            Expect.equal cloned.Type_ source.Type_ "Clone should preserve type"
            Expect.equal cloned.OutputSource source.OutputSource "Clone should preserve outputSource"
            Expect.equal cloned.OutputBinding source.OutputBinding "Clone should preserve outputBinding"

            Expect.equal
                (cloned.GetPropertyValue("label").ToString())
                "report output"
                "Clone should preserve custom properties"

            Expect.notEqual cloned.Name source.Name "Clone rename must not mutate source"
        }

        test "cloneOutputWithName ignores dynamic name shadow property" {
            let source = CWLOutput("original")
            source.SetProperty("name", "shadow-name")
            source.SetProperty("label", "report output")

            let cloned = cloneOutputWithName source "renamed"

            let hasShadowName =
                cloned.GetProperties(false)
                |> Seq.exists (fun kvp -> String.Equals(kvp.Key, "name", StringComparison.OrdinalIgnoreCase))

            Expect.equal cloned.Name "renamed" "Clone should keep constructor-based renamed output key"
            Expect.isFalse hasShadowName "Clone should not carry a dynamic name shadow property"

            Expect.equal
                (cloned.GetPropertyValue("label").ToString())
                "report output"
                "Clone should still preserve non-name custom properties"
        }
    ]

let commandLineToolMutationTests =
    testList "CommandLineTool mutations" [
        test "addInput and renameInputAt create editable input entries" {
            let tool = CWLToolDescription(ResizeArray())
            let index = addInput tool
            let inputs = tool.Inputs |> Option.defaultValue (ResizeArray())
            Expect.equal index 0 "First added input should be at index 0"
            Expect.equal inputs.Count 1 "One input should exist"

            renameInputAt inputs 0 "renamed_input"
            Expect.equal inputs.[0].Name "renamed_input" "Input should be renamed"
        }

        test "setBaseCommand clears when text is whitespace" {
            let tool = CWLToolDescription(ResizeArray())
            setBaseCommand tool "echo"
            Expect.isSome tool.BaseCommand "BaseCommand should be set"

            setBaseCommand tool "   "
            Expect.isNone tool.BaseCommand "Whitespace should clear baseCommand"
        }

        test "toggle requirement mutates requirement collection deterministically" {
            let tool = CWLToolDescription(ResizeArray())
            setRequirementEnabled tool "docker" true
            let reqs = tool.Requirements |> Option.defaultValue (ResizeArray())

            let dockerCount =
                reqs |> Seq.filter (fun req -> requirementKey req = Some "docker") |> Seq.length

            Expect.equal dockerCount 1 "Docker requirement should be present once"

            setRequirementEnabled tool "docker" false
            let afterDisable = tool.Requirements |> Option.defaultValue (ResizeArray())

            let dockerAfterDisable =
                afterDisable
                |> Seq.filter (fun req -> requirementKey req = Some "docker")
                |> Seq.length

            Expect.equal dockerAfterDisable 0 "Docker requirement should be removed"
        }

        test "setRequirementField updates payload-backed requirements" {
            let tool = CWLToolDescription(ResizeArray())

            setRequirementEnabled tool "inline-javascript" true
            setRequirementField tool "inline-javascript" "expressionLib" "lib/a.js\nlib/b.js"

            let inlineJavascript =
                tool.Requirements
                |> Option.defaultValue (ResizeArray())
                |> Seq.tryPick (
                    function
                    | Requirement.InlineJavascriptRequirement value -> Some value
                    | _ -> None
                )
                |> Option.defaultWith (fun () -> failwith "InlineJavascriptRequirement should exist")

            let expressionLib =
                inlineJavascript.ExpressionLib |> Option.defaultValue (ResizeArray())

            Expect.sequenceEqual
                expressionLib
                [ "lib/a.js"; "lib/b.js" ]
                "Expression library entries should be parsed and stored"

            setRequirementEnabled tool "load-listing" true
            setRequirementField tool "load-listing" "loadListing" "deep_listing"

            let loadListing =
                tool.Requirements
                |> Option.defaultValue (ResizeArray())
                |> Seq.tryPick (
                    function
                    | Requirement.LoadListingRequirement value -> Some value.LoadListing
                    | _ -> None
                )
                |> Option.defaultWith (fun () -> failwith "LoadListingRequirement should exist")

            Expect.equal loadListing LoadListingEnum.DeepListing "loadListing should be updated via enum value"

            setRequirementEnabled tool "tool-time-limit" true
            setRequirementField tool "tool-time-limit" "timelimitMode" "expression"
            setRequirementField tool "tool-time-limit" "timelimitValue" "$(inputs.timeout)"

            let expressionTimeLimit =
                tool.Requirements
                |> Option.defaultValue (ResizeArray())
                |> Seq.tryPick (
                    function
                    | Requirement.ToolTimeLimitRequirement(ToolTimeLimitExpression expression) -> Some expression
                    | _ -> None
                )

            Expect.equal expressionTimeLimit (Some "$(inputs.timeout)") "ToolTimeLimit should switch to expression mode"

            setRequirementField tool "tool-time-limit" "timelimitMode" "seconds"
            setRequirementField tool "tool-time-limit" "timelimitValue" "42"

            let secondsTimeLimit =
                tool.Requirements
                |> Option.defaultValue (ResizeArray())
                |> Seq.tryPick (
                    function
                    | Requirement.ToolTimeLimitRequirement(ToolTimeLimitSeconds seconds) -> Some seconds
                    | _ -> None
                )

            Expect.equal secondsTimeLimit (Some 42L) "ToolTimeLimit should switch back to numeric seconds"

            setRequirementEnabled tool "resource" true
            setRequirementField tool "resource" "coresMin" "2"
            setRequirementField tool "resource" "coresMax" "$(inputs.max_cores)"

            let resource =
                tool.Requirements
                |> Option.defaultValue (ResizeArray())
                |> Seq.tryPick (
                    function
                    | Requirement.ResourceRequirement value -> Some value
                    | _ -> None
                )
                |> Option.defaultWith (fun () -> failwith "ResourceRequirement should exist")

            Expect.equal (resource.TryGetInt64("coresMin")) (Some 2L) "Resource coresMin should parse as integer"

            Expect.equal
                (resource.TryGetExpression("coresMax"))
                (Some "$(inputs.max_cores)")
                "Resource coresMax should preserve expression value"

            setRequirementEnabled tool "schema-def" true
            setRequirementField tool "schema-def" "schema.add" ""
            setRequirementField tool "schema-def" "schema.name:0" "SampleType"
            setRequirementField tool "schema-def" "schema.type:0" "int"

            let schemaTypes =
                tool.Requirements
                |> Option.defaultValue (ResizeArray())
                |> Seq.tryPick (
                    function
                    | Requirement.SchemaDefRequirement values -> Some values
                    | _ -> None
                )
                |> Option.defaultWith (fun () -> failwith "SchemaDefRequirement should exist")

            Expect.equal schemaTypes.Count 1 "SchemaDef should contain one type entry"
            Expect.equal schemaTypes.[0].Name "SampleType" "Schema type name should be editable"

            match schemaTypes.[0].Type_ with
            | CWLType.Int -> ()
            | _ -> failtest "Schema type should update to int"

            setRequirementEnabled tool "software" true
            setRequirementField tool "software" "software.add" ""
            setRequirementField tool "software" "software.package:0" "samtools"
            setRequirementField tool "software" "software.version:0" "1.18\n1.19"
            setRequirementField tool "software" "software.specs:0" "https://example.org/spec"

            let softwarePackages =
                tool.Requirements
                |> Option.defaultValue (ResizeArray())
                |> Seq.tryPick (
                    function
                    | Requirement.SoftwareRequirement values -> Some values
                    | _ -> None
                )
                |> Option.defaultWith (fun () -> failwith "SoftwareRequirement should exist")

            Expect.equal softwarePackages.Count 1 "SoftwareRequirement should contain one package entry"
            Expect.equal softwarePackages.[0].Package "samtools" "Software package name should be editable"

            Expect.sequenceEqual
                (softwarePackages.[0].Version |> Option.defaultValue (ResizeArray()))
                [ "1.18"; "1.19" ]
                "Software versions should parse from textarea input"

            Expect.sequenceEqual
                (softwarePackages.[0].Specs |> Option.defaultValue (ResizeArray()))
                [ "https://example.org/spec" ]
                "Software specs should parse from textarea input"

            setRequirementEnabled tool "env-vars" true
            setRequirementField tool "env-vars" "env.add" ""
            setRequirementField tool "env-vars" "env.name:0" "OMP_NUM_THREADS"
            setRequirementField tool "env-vars" "env.value:0" "8"

            let envVars =
                tool.Requirements
                |> Option.defaultValue (ResizeArray())
                |> Seq.tryPick (
                    function
                    | Requirement.EnvVarRequirement values -> Some values
                    | _ -> None
                )
                |> Option.defaultWith (fun () -> failwith "EnvVarRequirement should exist")

            Expect.equal envVars.Count 1 "EnvVarRequirement should contain one env var entry"
            Expect.equal envVars.[0].EnvName "OMP_NUM_THREADS" "Env var name should be editable"
            Expect.equal envVars.[0].EnvValue "8" "Env var value should be editable"

            setRequirementEnabled tool "docker" true
            setRequirementField tool "docker" "dockerFile" "docker/Dockerfile"
            setRequirementField tool "docker" "dockerFileMode" "include"
            setRequirementField tool "docker" "dockerRunOptions" "--rm\n--network=host"

            setRequirementEnabled tool "initial-workdir" true
            setRequirementField tool "initial-workdir" "iwd.addString" ""
            setRequirementField tool "initial-workdir" "iwd.addDirent" ""
            setRequirementField tool "initial-workdir" "iwd.addFile" ""
            setRequirementField tool "initial-workdir" "iwd.addDirectory" ""
            setRequirementField tool "initial-workdir" "iwd.value:0" "config.txt"
            setRequirementField tool "initial-workdir" "iwd.entryMode:1" "import"
            setRequirementField tool "initial-workdir" "iwd.value:1" "config/template.json"
            setRequirementField tool "initial-workdir" "iwd.entryname:1" "rendered.json"
            setRequirementField tool "initial-workdir" "iwd.entrynameMode:1" "include"
            setRequirementField tool "initial-workdir" "iwd.writable:1" "true"
            setRequirementField tool "initial-workdir" "iwd.value:2" "file:///tmp/data.txt"

            let listing =
                tool.Requirements
                |> Option.defaultValue (ResizeArray())
                |> Seq.tryPick (
                    function
                    | Requirement.InitialWorkDirRequirement values -> Some values
                    | _ -> None
                )
                |> Option.defaultWith (fun () -> failwith "InitialWorkDirRequirement should exist")

            Expect.equal listing.Count 4 "InitialWorkDir should contain four entries"

            match listing.[0] with
            | StringEntry(SchemaSaladString.Literal text) ->
                Expect.equal text "config.txt" "String entry should be editable"
            | _ -> failtest "Expected first InitialWorkDir entry to be a string literal"

            match listing.[1] with
            | DirentEntry dirent ->
                match dirent.Entry with
                | SchemaSaladString.Import "config/template.json" -> ()
                | _ -> failtest "Dirent entry should preserve schema-salad mode"

                match dirent.Entryname with
                | Some(SchemaSaladString.Include "rendered.json") -> ()
                | _ -> failtest "Dirent entryname should preserve schema-salad mode"

                Expect.equal dirent.Writable (Some true) "Dirent writable should be editable"
            | _ -> failtest "Expected second InitialWorkDir entry to be a Dirent entry"

            match listing.[2] with
            | FileEntry file ->
                Expect.equal
                    (string (file.GetPropertyValue("location")))
                    "file:///tmp/data.txt"
                    "File entry should store location"
            | _ -> failtest "Expected third InitialWorkDir entry to be a File entry"

            let dockerRequirement =
                tool.Requirements
                |> Option.defaultValue (ResizeArray())
                |> Seq.tryPick (
                    function
                    | Requirement.DockerRequirement value -> Some value
                    | _ -> None
                )
                |> Option.defaultWith (fun () -> failwith "DockerRequirement should exist")

            match dockerRequirement.DockerFile with
            | Some(SchemaSaladString.Include "docker/Dockerfile") -> ()
            | _ -> failtest "Docker dockerFile mode should be editable"

            let dockerRunOptions =
                dockerRequirement.DockerRunOptions |> Option.defaultValue (ResizeArray())

            Expect.sequenceEqual
                dockerRunOptions
                [ "--rm"; "--network=host" ]
                "Docker dockerRunOptions should be editable"
        }

        test "setHintField updates known hint payloads" {
            let tool = CWLToolDescription(ResizeArray())
            setHintEnabled tool "load-listing" true
            setHintField tool "load-listing" "loadListing" "shallow_listing"

            let hintLoadListing =
                tool.Hints
                |> Option.defaultValue (ResizeArray())
                |> Seq.tryPick (
                    function
                    | KnownHint(Requirement.LoadListingRequirement value) -> Some value.LoadListing
                    | _ -> None
                )

            Expect.equal hintLoadListing (Some LoadListingEnum.ShallowListing) "Known hint payload should be editable"
        }

        test "multiple input feature requirement can be toggled for workflow" {
            let workflow = CWLWorkflowDescription(ResizeArray(), ResizeArray(), ResizeArray())
            setWorkflowRequirementEnabled workflow "multiple-input-feature" true

            let hasMultipleInputRequirement =
                workflow.Requirements
                |> Option.defaultValue (ResizeArray())
                |> Seq.exists (fun req -> requirementKey req = Some "multiple-input-feature")

            Expect.isTrue hasMultipleInputRequirement "MultipleInputFeatureRequirement should be addable"

            setWorkflowRequirementEnabled workflow "multiple-input-feature" false

            let hasRequirementAfterDisable =
                workflow.Requirements
                |> Option.defaultValue (ResizeArray())
                |> Seq.exists (fun req -> requirementKey req = Some "multiple-input-feature")

            Expect.isFalse hasRequirementAfterDisable "MultipleInputFeatureRequirement should be removable"
        }
    ]

let workflowMutationTests =
    testList "Workflow mutations" [
        test "workflow step helpers update id, run, inputs, and outputs" {
            let workflow = CWLWorkflowDescription(ResizeArray(), ResizeArray(), ResizeArray())
            let stepIndex = addWorkflowStep workflow

            setWorkflowStepIdAt workflow.Steps stepIndex "quality_check"
            setWorkflowStepRunAt workflow.Steps stepIndex "qc.cwl"

            let stepInputIndex =
                match addWorkflowStepInputAt workflow.Steps stepIndex with
                | Some index -> index
                | None -> failtest "Expected step input to be added"

            setWorkflowStepInputIdAt workflow.Steps stepIndex stepInputIndex "reads"
            setWorkflowStepInputSourceAt workflow.Steps stepIndex stepInputIndex "input_reads"

            let stepOutputIndex =
                match addWorkflowStepOutputAt workflow.Steps stepIndex with
                | Some index -> index
                | None -> failtest "Expected step output to be added"

            setWorkflowStepOutputIdAt workflow.Steps stepIndex stepOutputIndex "qc_report"

            let step = workflow.Steps.[stepIndex]
            Expect.equal step.Id "quality_check" "Step id should be updated"

            match step.Run with
            | WorkflowStepRun.RunString runTarget -> Expect.equal runTarget "qc.cwl" "Step run should be updated"
            | _ -> failtest "Expected run target to stay a run string"

            let inputSource =
                step.In.[stepInputIndex].Source |> Option.defaultValue (ResizeArray())

            Expect.sequenceEqual inputSource [ "input_reads" ] "Step input source should be updated"
            Expect.equal (step.Out.[stepOutputIndex] |> stepOutputId) "qc_report" "Step output id should be updated"
        }

        test "workflow requirements toggle deterministically" {
            let workflow = CWLWorkflowDescription(ResizeArray(), ResizeArray(), ResizeArray())
            setWorkflowRequirementEnabled workflow "docker" true

            let reqKeys =
                workflow.Requirements
                |> Option.defaultValue (ResizeArray())
                |> Seq.choose requirementKey
                |> Set.ofSeq

            Expect.isTrue (reqKeys.Contains "docker") "Docker requirement should be present"

            setWorkflowRequirementEnabled workflow "docker" false

            let reqKeysAfterDisable =
                workflow.Requirements
                |> Option.defaultValue (ResizeArray())
                |> Seq.choose requirementKey
                |> Set.ofSeq

            Expect.isFalse (reqKeysAfterDisable.Contains "docker") "Docker requirement should be removed"
        }

        test "step id update ignores whitespace and invalid indexes no-op safely" {
            let workflow = CWLWorkflowDescription(ResizeArray(), ResizeArray(), ResizeArray())
            let stepIndex = addWorkflowStep workflow
            setWorkflowStepIdAt workflow.Steps stepIndex "initial_id"

            setWorkflowStepIdAt workflow.Steps stepIndex "   "
            setWorkflowStepIdAt workflow.Steps -1 "bad_index"
            setWorkflowStepIdAt workflow.Steps 999 "bad_index"

            Expect.equal
                workflow.Steps.[stepIndex].Id
                "initial_id"
                "Whitespace and invalid-index updates should not change step id"
        }

        test "step move operations clamp at list boundaries" {
            let workflow = CWLWorkflowDescription(ResizeArray(), ResizeArray(), ResizeArray())
            let firstIndex = addWorkflowStep workflow
            let secondIndex = addWorkflowStep workflow
            setWorkflowStepIdAt workflow.Steps firstIndex "first"
            setWorkflowStepIdAt workflow.Steps secondIndex "second"

            let moveFirstUp = moveWorkflowStepUp (Some 0) workflow.Steps
            Expect.equal moveFirstUp (Some 0) "Moving first step up should keep index"

            let moveLastDown =
                moveWorkflowStepDown (Some(workflow.Steps.Count - 1)) workflow.Steps

            Expect.equal moveLastDown (Some(workflow.Steps.Count - 1)) "Moving last step down should keep index"
        }

        test "step input source parsing trims entries and removes empty tokens" {
            let workflow = CWLWorkflowDescription(ResizeArray(), ResizeArray(), ResizeArray())
            let stepIndex = addWorkflowStep workflow

            let stepInputIndex =
                match addWorkflowStepInputAt workflow.Steps stepIndex with
                | Some index -> index
                | None -> failtest "Expected step input to be added"

            setWorkflowStepInputSourceAt workflow.Steps stepIndex stepInputIndex "  reads , , step1/out , "

            let parsed =
                workflow.Steps.[stepIndex].In.[stepInputIndex].Source
                |> Option.defaultValue (ResizeArray())

            Expect.sequenceEqual parsed [ "reads"; "step1/out" ] "Source parser should trim and drop empty items"
        }

        test "inline run target is preserved when user attempts text overwrite" {
            let inlineTool = CWLToolDescription(ResizeArray())

            let step =
                WorkflowStep("step_inline", ResizeArray(), ResizeArray(), WorkflowStepRunOps.fromTool inlineTool)

            let workflow =
                CWLWorkflowDescription(ResizeArray [| step |], ResizeArray(), ResizeArray())

            setWorkflowStepRunAt workflow.Steps 0 "should_not_replace_inline"

            match workflow.Steps.[0].Run with
            | WorkflowStepRun.RunCommandLineTool _ -> ()
            | _ -> failtest "Inline run should remain inline and not be overwritten by text setter"
        }

        test "step run-kind mutation can switch between string and inline variants" {
            let step =
                WorkflowStep.fromRunPath ("step1", ResizeArray(), ResizeArray(), "tool.cwl")

            let workflow =
                CWLWorkflowDescription(ResizeArray [| step |], ResizeArray(), ResizeArray())

            setWorkflowStepRunKindAt workflow.Steps 0 RunWorkflowKind

            match workflow.Steps.[0].Run with
            | WorkflowStepRun.RunWorkflow _ -> ()
            | _ -> failtest "Run kind should switch to inline workflow"

            setWorkflowStepRunKindAt workflow.Steps 0 RunOperationKind

            match workflow.Steps.[0].Run with
            | WorkflowStepRun.RunOperation _ -> ()
            | _ -> failtest "Run kind should switch to inline operation"

            setWorkflowStepRunKindAt workflow.Steps 0 RunStringKind

            match workflow.Steps.[0].Run with
            | WorkflowStepRun.RunString runTarget ->
                Expect.equal
                    runTarget
                    "tool.cwl"
                    "Switching back to string should preserve existing run target when available"
            | _ -> failtest "Run kind should switch back to run-string"
        }
    ]

let expressionToolMutationTests =
    testList "ExpressionTool mutations" [
        test "expression helpers mutate inputs, outputs, expression, and requirements" {
            let expressionTool = CWLExpressionToolDescription(ResizeArray(), "$(inputs)")
            let inputIndex = addExpressionInput expressionTool
            let inputs = CWLExpressionToolDescription.getInputsOrEmpty expressionTool
            renameInputAt inputs inputIndex "sample"

            let outputIndex = addExpressionOutput expressionTool
            setOutputGlobAt expressionTool.Outputs outputIndex "*.txt"

            setExpressionText expressionTool "${ return { out: inputs.sample }; }"
            setExpressionRequirementEnabled expressionTool "inline-javascript" true

            let yaml =
                expressionTool
                |> CWLProcessingUnit.ExpressionTool
                |> Encode.encodeProcessingUnit

            Expect.isTrue (yaml.Contains "sample") "Encoded yaml should include renamed input"

            Expect.isTrue
                (yaml.Contains "InlineJavascriptRequirement")
                "Encoded yaml should include InlineJavascriptRequirement"

            Expect.isTrue (yaml.Contains "expression") "Encoded yaml should include expression field"
        }
    ]

let workflowCanvasAdapterTests =
    testList "Workflow canvas adapter" [
        test "sourcePorts and targetPorts expose workflow endpoints for canvas selection" {
            let stepInput = StepInput.create ("reads")

            let step =
                WorkflowStep.fromRunPath (
                    "qc",
                    ResizeArray [| stepInput |],
                    ResizeArray [| StepOutput.StepOutputString "report" |],
                    "qc.cwl"
                )

            let workflow =
                CWLWorkflowDescription(
                    ResizeArray [| step |],
                    ResizeArray [| CWLInput("reads") |],
                    ResizeArray [| CWLOutput("final") |]
                )

            let sources = sourcePorts workflow |> Seq.map (fun port -> port.Label) |> Set.ofSeq
            let targets = targetPorts workflow |> Seq.map (fun port -> port.Label) |> Set.ofSeq

            Expect.isTrue (sources.Contains "input/reads") "Workflow input should be available as source"
            Expect.isTrue (sources.Contains "qc/report") "Step output should be available as source"
            Expect.isTrue (targets.Contains "qc/reads") "Step input should be available as target"
            Expect.isTrue (targets.Contains "output/final") "Workflow output should be available as target"
        }

        test "tryCreateConnectionEdge infers edge kind from endpoint node classes" {
            let inputToStep =
                tryCreateConnectionEdge WorkflowInputSourceNodeId "reads" "step:qc" "reads"

            let stepToOutput =
                tryCreateConnectionEdge "step:qc" "report" WorkflowOutputSinkNodeId "final"

            let invalid =
                tryCreateConnectionEdge WorkflowOutputSinkNodeId "final" "step:qc" "reads"

            match inputToStep with
            | Some edge -> Expect.equal edge.Kind InputToStep "Input->step should be InputToStep"
            | None -> failtest "Expected valid input->step edge"

            match stepToOutput with
            | Some edge -> Expect.equal edge.Kind StepToOutput "Step->output should be StepToOutput"
            | None -> failtest "Expected valid step->output edge"

            Expect.isNone invalid "Output->step should be rejected"
        }

        test "addConnection and removeConnection mutate graph edge set safely" {
            let workflow = CWLWorkflowDescription(ResizeArray(), ResizeArray(), ResizeArray())
            workflow.Inputs.Add(CWLInput("reads"))

            let step =
                WorkflowStep.fromRunPath (
                    "qc",
                    ResizeArray [| StepInput.create ("reads") |],
                    ResizeArray [| StepOutput.StepOutputString "report" |],
                    "qc.cwl"
                )

            workflow.Steps.Add(step)
            workflow.Outputs.Add(CWLOutput("final"))

            let graph = toCanvasGraph workflow

            let firstAdd =
                addConnection graph WorkflowInputSourceNodeId "reads" "step:qc" "reads"

            let duplicateAdd =
                addConnection graph WorkflowInputSourceNodeId "reads" "step:qc" "reads"

            Expect.isTrue firstAdd "Valid edge should be addable"
            Expect.isFalse duplicateAdd "Duplicate edge should be ignored"

            let createdEdge =
                tryCreateConnectionEdge WorkflowInputSourceNodeId "reads" "step:qc" "reads"
                |> Option.defaultWith (fun () -> failwith "Expected deterministic edge id")

            let removed = removeConnection graph createdEdge.Id
            let removedAgain = removeConnection graph createdEdge.Id

            Expect.isTrue removed "Existing edge should be removable"
            Expect.isFalse removedAgain "Removing already-removed edge should no-op"
        }

        test "toCanvasGraph maps workflow inputs, steps, outputs and links" {
            let stepInput = StepInput.create ("message", source = ResizeArray [| "message" |])

            let step =
                WorkflowStep.fromRunPath (
                    "step1",
                    ResizeArray [| stepInput |],
                    ResizeArray [| StepOutput.StepOutputString "out" |],
                    "tool.cwl"
                )

            let wfOutput = CWLOutput("final", outputSource = OutputSource.Single "step1/out")

            let workflow =
                CWLWorkflowDescription(
                    ResizeArray [| step |],
                    ResizeArray [| CWLInput("message") |],
                    ResizeArray [| wfOutput |]
                )

            let graph = toCanvasGraph workflow

            Expect.isTrue
                (graph.Nodes |> Seq.exists (fun n -> n.Id = WorkflowInputSourceNodeId))
                "Input node should exist"

            Expect.isTrue (graph.Nodes |> Seq.exists (fun n -> n.Id = "step:step1")) "Step node should exist"

            Expect.isTrue
                (graph.Nodes |> Seq.exists (fun n -> n.Id = WorkflowOutputSinkNodeId))
                "Workflow output sink node should exist"

            Expect.isTrue
                (graph.Edges
                 |> Seq.exists (fun e -> e.Kind = InputToStep && e.TargetNodeId = "step:step1"))
                "Input -> step edge should exist"

            Expect.isTrue
                (graph.Edges
                 |> Seq.exists (fun e -> e.Kind = StepToOutput && e.TargetNodeId = WorkflowOutputSinkNodeId))
                "Step -> output edge should exist"

            let outIds = step.Out |> Seq.map stepOutputId |> ResizeArray
            Expect.sequenceEqual outIds [ "out" ] "Step output ids should be extractable from step.Out"
        }

        test "toCanvasGraph uses a single workflow output sink node for multiple outputs" {
            let step =
                WorkflowStep.fromRunPath (
                    "step1",
                    ResizeArray(),
                    ResizeArray [| StepOutput.StepOutputString "out" |],
                    "tool.cwl"
                )

            let wfOutputA = CWLOutput("finalA", outputSource = OutputSource.Single "step1/out")
            let wfOutputB = CWLOutput("finalB", outputSource = OutputSource.Single "step1/out")

            let workflow =
                CWLWorkflowDescription(ResizeArray [| step |], ResizeArray(), ResizeArray [| wfOutputA; wfOutputB |])

            let graph = toCanvasGraph workflow

            let outputNodeCount =
                graph.Nodes
                |> Seq.filter (fun node -> node.Kind = WorkflowOutputNode)
                |> Seq.length

            let sinkEdgeTargets =
                graph.Edges
                |> Seq.filter (fun edge -> edge.TargetNodeId = WorkflowOutputSinkNodeId)
                |> Seq.length

            Expect.equal outputNodeCount 1 "Exactly one output sink node should be rendered"
            Expect.equal sinkEdgeTargets 2 "Each workflow output should connect to the shared sink node"
        }

        test "applyConnections writes edge changes back into workflow" {
            let stepInput = StepInput.create ("message", source = ResizeArray [| "message" |])

            let step =
                WorkflowStep.fromRunPath (
                    "step1",
                    ResizeArray [| stepInput |],
                    ResizeArray [| StepOutput.StepOutputString "out" |],
                    "tool.cwl"
                )

            let wfOutput = CWLOutput("final", outputSource = OutputSource.Single "step1/out")

            let workflow =
                CWLWorkflowDescription(
                    ResizeArray [| step |],
                    ResizeArray [| CWLInput("message") |],
                    ResizeArray [| wfOutput |]
                )

            let graph = toCanvasGraph workflow
            graph.Edges.Clear()

            graph.Edges.Add(
                {
                    Id = $"edge:{WorkflowInputSourceNodeId}/message->{WorkflowOutputSinkNodeId}/final"
                    Kind = InputToOutput
                    SourceNodeId = WorkflowInputSourceNodeId
                    SourcePortId = "message"
                    TargetNodeId = WorkflowOutputSinkNodeId
                    TargetPortId = "final"
                    SourceReference = None
                }
            )

            applyConnections graph workflow

            Expect.equal
                workflow.Outputs.[0].OutputSource
                (Some(OutputSource.Single "message"))
                "OutputSource should be updated from graph edge"
        }

        test "applyConnections preserves source reference formatting for unchanged graph edges" {
            let stepInput = StepInput.create ("message", source = ResizeArray [| "#message" |])

            let step =
                WorkflowStep.fromRunPath (
                    "step1",
                    ResizeArray [| stepInput |],
                    ResizeArray [| StepOutput.StepOutputString "out" |],
                    "tool.cwl"
                )

            let wfOutput = CWLOutput("final", outputSource = OutputSource.Single "#step1/out")

            let workflow =
                CWLWorkflowDescription(
                    ResizeArray [| step |],
                    ResizeArray [| CWLInput("message") |],
                    ResizeArray [| wfOutput |]
                )

            let graph = toCanvasGraph workflow
            applyConnections graph workflow

            let normalizedInputSources =
                workflow.Steps.[0].In.[0].Source |> Option.defaultValue (ResizeArray())

            Expect.sequenceEqual
                normalizedInputSources
                [ "#message" ]
                "Step input source formatting should be preserved"

            Expect.equal
                workflow.Outputs.[0].OutputSource
                (Some(OutputSource.Single "#step1/out"))
                "Workflow output source formatting should be preserved"
        }

        test "applyConnections infers workflow output type from connected step output" {
            let tool = CWLToolDescription(ResizeArray())
            let dbFolderOutput = CWLOutput("dbFolder")
            dbFolderOutput.Type_ <- Some(CWLType.directory ())
            tool.Outputs <- ResizeArray [| dbFolderOutput |]

            let step =
                WorkflowStep(
                    "PeptideDB",
                    ResizeArray(),
                    ResizeArray [| StepOutput.StepOutputString "dbFolder" |],
                    WorkflowStepRunOps.fromTool tool
                )

            let workflow =
                CWLWorkflowDescription(ResizeArray [| step |], ResizeArray(), ResizeArray())

            let outputIndex = addWorkflowOutput workflow
            let outputName = workflow.Outputs.[outputIndex].Name

            let graph = toCanvasGraph workflow

            let connectionAdded =
                addConnection graph "step:PeptideDB" "dbFolder" WorkflowOutputSinkNodeId outputName

            Expect.isTrue connectionAdded "Step output should connect to workflow output"

            applyConnections graph workflow

            match workflow.Outputs.[outputIndex].Type_ with
            | Some(CWLType.Directory _) -> ()
            | _ -> failtest "Connected workflow output should inherit Directory type from source step output"
        }

        test "toCanvasGraph/applyConnections preserves OutputSource.Multiple" {
            let stepA =
                WorkflowStep.fromRunPath (
                    "stepA",
                    ResizeArray(),
                    ResizeArray [| StepOutput.StepOutputString "outA" |],
                    "a.cwl"
                )

            let stepB =
                WorkflowStep.fromRunPath (
                    "stepB",
                    ResizeArray(),
                    ResizeArray [| StepOutput.StepOutputString "outB" |],
                    "b.cwl"
                )

            let multipleSources = ResizeArray [| "stepA/outA"; "stepB/outB" |]

            let wfOutput =
                CWLOutput("final", outputSource = OutputSource.Multiple multipleSources)

            let workflow =
                CWLWorkflowDescription(ResizeArray [| stepA; stepB |], ResizeArray(), ResizeArray [| wfOutput |])

            let graph = toCanvasGraph workflow
            applyConnections graph workflow

            match workflow.Outputs.[0].OutputSource with
            | Some(OutputSource.Multiple values) ->
                Expect.sequenceEqual
                    values
                    [ "stepA/outA"; "stepB/outB" ]
                    "Multiple outputSource values should roundtrip through graph adapter"
            | _ -> failtest "Expected OutputSource.Multiple after graph roundtrip"
        }

        test "buildWorkflowGraphReadModel has no diagnostics for valid workflow wiring" {
            let stepInput = StepInput.create ("message", source = ResizeArray [| "message" |])

            let step =
                WorkflowStep.fromRunPath (
                    "step1",
                    ResizeArray [| stepInput |],
                    ResizeArray [| StepOutput.StepOutputString "out" |],
                    "tool.cwl"
                )

            let wfOutput = CWLOutput("final", outputSource = OutputSource.Single "step1/out")

            let workflow =
                CWLWorkflowDescription(
                    ResizeArray [| step |],
                    ResizeArray [| CWLInput("message") |],
                    ResizeArray [| wfOutput |]
                )

            let readModel = buildWorkflowGraphReadModel workflow None None

            Expect.equal readModel.Diagnostics.Count 0 "Valid workflow graph should not produce diagnostics"
            Expect.isGreaterThan readModel.NodeCount 0 "Read model should include nodes"
            Expect.isGreaterThan readModel.EdgeCount 0 "Read model should include edges"
        }

        test "buildWorkflowGraphReadModel reports missing-reference diagnostics for invalid source links" {
            let brokenInput =
                StepInput.create ("reads", source = ResizeArray [| "missingStep/out" |])

            let step =
                WorkflowStep.fromRunPath (
                    "qc",
                    ResizeArray [| brokenInput |],
                    ResizeArray [| StepOutput.StepOutputString "report" |],
                    "qc.cwl"
                )

            let workflow =
                CWLWorkflowDescription(ResizeArray [| step |], ResizeArray [| CWLInput("reads") |], ResizeArray())

            let readModel = buildWorkflowGraphReadModel workflow None None

            let hasMissingReference =
                readModel.Diagnostics
                |> Seq.exists (fun issue -> issue.Kind = "missing-reference")

            Expect.isTrue hasMissingReference "Broken source wiring should emit missing-reference diagnostics"
        }
    ]

let allTests =
    testList "Cwl" [
        roundtripTests
        goldenFixtureTests
        editorTests
        validationTests
        mutationTests
        editorMutationHelperTests
        commandLineToolMutationTests
        workflowMutationTests
        expressionToolMutationTests
        workflowCanvasAdapterTests
    ]
