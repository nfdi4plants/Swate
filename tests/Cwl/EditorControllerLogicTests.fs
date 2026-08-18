module CWLBuilder.Renderer.Tests.EditorControllerLogicTests

open Expecto
open ARCtrl.CWL
open CWLBuilder.Domain.EditorTypes
open CWLBuilder.Domain.CommandLineToolMutations
open CWLBuilder.Domain.WorkflowMutations
open CWLBuilder.Domain.ExpressionToolMutations
open CWLBuilder.Domain.Tests.TestFixtures
open CWLBuilder.Electron.Shared.IPCTypes.DTOs
open CWLBuilder.Electron.Renderer.EditorControllerLogic

let private mkLoadResponse success yaml filePath error =
    {
        Success = success
        Yaml = yaml
        ResolvedYaml = None
        FilePath = filePath
        Error = error
    }

let loadEditSaveTests =
    testList "Renderer load/edit/save flow" [
        test "load -> mutate -> save yaml stays coherent" {
            let response = mkLoadResponse true (Some minimalToolYaml) "example.cwl" None

            let state =
                match tryCreateLoadedState response with
                | Ok loaded -> loaded
                | Error message -> failtestf "Expected load success but got %s" message

            match state.ProcessingUnit with
            | CWLProcessingUnit.CommandLineTool tool ->
                let newInputIndex = addInput tool
                let inputs = tool.Inputs |> Option.defaultValue (ResizeArray())
                renameInputAt inputs newInputIndex "extra_arg"
                setBaseCommand tool "cat"
            | _ ->
                failtest "Expected CommandLineTool state"

            let touched = touch state
            match ensureCanSave touched with
            | Error message -> failtestf "Save should be allowed, got %s" message
            | Ok () ->
                let yaml = createSaveYamlForPath touched "example.cwl"
                Expect.isTrue (yaml.Contains "cat") "Updated baseCommand should be encoded"
                Expect.isTrue (yaml.Contains "extra_arg") "New input should be encoded"
        }

        test "workflow load -> mutate -> save yaml stays coherent" {
            let response = mkLoadResponse true (Some minimalWorkflowYaml) "workflow.cwl" None

            let state =
                match tryCreateLoadedState response with
                | Ok loaded -> loaded
                | Error message -> failtestf "Expected workflow load success but got %s" message

            match state.ProcessingUnit with
            | CWLProcessingUnit.Workflow workflow ->
                let newStepIndex = addWorkflowStep workflow
                setWorkflowStepIdAt workflow.Steps newStepIndex "stage_2"
                setWorkflowStepRunAt workflow.Steps newStepIndex "stage2.cwl"
            | _ ->
                failtest "Expected Workflow state"

            let touched = touch state
            match ensureCanSave touched with
            | Error message -> failtestf "Workflow save should be allowed, got %s" message
            | Ok () ->
                let yaml = createSaveYamlForPath touched "workflow.cwl"
                Expect.isTrue (yaml.Contains "stage_2") "New workflow step id should be encoded"
                Expect.isTrue (yaml.Contains "stage2.cwl") "Workflow run target should be encoded"
        }

        test "expression tool load -> mutate -> save yaml stays coherent" {
            let response = mkLoadResponse true (Some minimalExpressionToolYaml) "expr.cwl" None

            let state =
                match tryCreateLoadedState response with
                | Ok loaded -> loaded
                | Error message -> failtestf "Expected expression tool load success but got %s" message

            match state.ProcessingUnit with
            | CWLProcessingUnit.ExpressionTool expressionTool ->
                let newInputIndex = addExpressionInput expressionTool
                let inputs = CWLExpressionToolDescription.getInputsOrEmpty expressionTool
                renameInputAt inputs newInputIndex "delta"
                setExpressionText expressionTool "${ return {'output_val': inputs.delta}; }"
            | _ ->
                failtest "Expected ExpressionTool state"

            let touched = touch state
            match ensureCanSave touched with
            | Error message -> failtestf "ExpressionTool save should be allowed, got %s" message
            | Ok () ->
                let yaml = createSaveYamlForPath touched "expr.cwl"
                Expect.isTrue (yaml.Contains "delta") "New expression input should be encoded"
                Expect.isTrue (yaml.Contains "output_val") "Expression output key should be encoded"
        }

        test "operation load -> mutate -> save yaml stays coherent" {
            let response = mkLoadResponse true (Some minimalOperationYaml) "operation.cwl" None

            let state =
                match tryCreateLoadedState response with
                | Ok loaded -> loaded
                | Error message -> failtestf "Expected operation load success but got %s" message

            match state.ProcessingUnit with
            | CWLProcessingUnit.Operation operation ->
                operation.Intent <- parseIntentText "service"
            | _ ->
                failtest "Expected Operation state"

            let touched = touch state
            match ensureCanSave touched with
            | Error message -> failtestf "Operation save should be allowed, got %s" message
            | Ok () ->
                let yaml = createSaveYamlForPath touched "operation.cwl"
                Expect.isTrue (yaml.Contains "Operation") "Operation class should be encoded"
                Expect.isTrue (yaml.Contains "service") "Updated operation intent should be encoded"
        }
    ]

let failurePathTests =
    testList "Renderer failure paths" [
        test "load failure response is surfaced as error" {
            let response = mkLoadResponse false None "missing.cwl" (Some "permission denied")
            let result = tryCreateLoadedState response
            Expect.isError result "Failed load response should be an error"
        }

        test "successful load without yaml is rejected" {
            let response = mkLoadResponse true None "broken.cwl" None
            let result = tryCreateLoadedState response
            Expect.isError result "Missing yaml payload should be rejected"
        }

        test "empty yaml load surfaces explicit empty-document error without duplicate prefix" {
            let response = mkLoadResponse true (Some "") "empty.cwl" None
            let result = tryCreateLoadedState response
            Expect.equal result (Error "Failed to decode CWL: CWL document is empty.") "Error should be explicit and have a single decode prefix"
        }

        test "save is blocked when validation has errors" {
            let state = createNew CommandLineTool
            match state.ProcessingUnit with
            | CWLProcessingUnit.CommandLineTool tool ->
                tool.Inputs <- Some (ResizeArray [| CWLInput("") |])
            | _ -> failtest "Expected CommandLineTool"

            let invalidState = touch state
            let result = ensureCanSave invalidState
            Expect.isError result "Validation errors should block save"
        }
    ]

let saveMergeBehaviorTests =
    testList "Renderer save merge behavior" [
        test "successful save clears dirty state and returns saved notification when no newer edits exist" {
            let stateAtSaveClick = createNew CommandLineTool |> touch

            let result = mergeSuccessfulSave stateAtSaveClick (Some stateAtSaveClick) "saved.cwl"

            Expect.equal result.InfoMessage "Saved to saved.cwl" "Save notification should confirm the saved file path"

            match result.NextState with
            | Some nextState ->
                Expect.equal nextState.FilePath (Some "saved.cwl") "Merged state should carry the saved file path"
                Expect.isFalse nextState.IsDirty "Merged state should be clean when no newer edits exist"
            | None ->
                failtest "Expected merged state when save result matches the active editor session"
        }

        test "successful save preserves dirty state and returns snapshot notification when newer edits already exist" {
            let stateAtSaveClick = createNew CommandLineTool |> touch
            let latestState = touch stateAtSaveClick

            let result = mergeSuccessfulSave stateAtSaveClick (Some latestState) "saved.cwl"

            Expect.equal result.InfoMessage "Saved snapshot to saved.cwl. New edits remain unsaved." "Notification should explain that a newer dirty revision still exists"

            match result.NextState with
            | Some nextState ->
                Expect.equal nextState.FilePath (Some "saved.cwl") "Merged state should still update the saved file path"
                Expect.isTrue nextState.IsDirty "Newer edits should remain dirty after the earlier save completes"
                Expect.equal nextState.Version latestState.Version "Merge should not roll back the latest revision counter"
            | None ->
                failtest "Expected merged state when save result matches the active editor session"
        }
    ]

[<Tests>]
let allTests =
    testList "RendererControllerLogic" [
        loadEditSaveTests
        failurePathTests
        saveMergeBehaviorTests
    ]
