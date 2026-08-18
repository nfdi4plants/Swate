/// ExpressionTool-focused mutation helpers.
/// Reuses generic input/output mutation operations from CommandLineTool helpers.
module Swate.Components.Shared.Cwl.ExpressionToolMutations

open ARCtrl.CWL
open Swate.Components.Shared.Cwl.CommandLineToolMutations
open Swate.Components.Shared.Cwl.RequirementMutations
open Swate.Components.Shared.Cwl.EditorMutations

let setExpressionRequirementEnabled (tool: CWLExpressionToolDescription) (key: string) (enabled: bool) =
    tool.Requirements <- toggleRequirement key enabled tool.Requirements

let setExpressionHintEnabled (tool: CWLExpressionToolDescription) (key: string) (enabled: bool) =
    tool.Hints <- toggleHint key enabled tool.Hints

let setExpressionRequirementField
    (tool: CWLExpressionToolDescription)
    (key: string)
    (fieldKey: string)
    (value: string)
    =
    setRequirementFieldByKey tool.Requirements key fieldKey value

let setExpressionHintField (tool: CWLExpressionToolDescription) (key: string) (fieldKey: string) (value: string) =
    setHintFieldByKey tool.Hints key fieldKey value

let setExpressionRequirementDockerField
    (tool: CWLExpressionToolDescription)
    (key: string)
    (fieldKey: string)
    (value: string)
    =
    setExpressionRequirementField tool key fieldKey value

let setExpressionHintDockerField (tool: CWLExpressionToolDescription) (key: string) (fieldKey: string) (value: string) =
    setExpressionHintField tool key fieldKey value

let setExpressionText (tool: CWLExpressionToolDescription) (expression: string) = tool.Expression <- expression

let addExpressionInput (tool: CWLExpressionToolDescription) =
    let inputs = CWLExpressionToolDescription.getOrCreateInputs tool
    let name = nextName "input" (inputs |> Seq.map (fun input -> input.Name))
    let input = CWLInput(name)
    input.Type_ <- Some CWLType.String
    inputs.Add(input)
    inputs.Count - 1

let addExpressionOutput (tool: CWLExpressionToolDescription) =
    CommandLineToolMutations.addOutput tool.Outputs
