/// CommandLineTool-focused mutation helpers.
/// Keeps field/list mutation logic out of renderer components.
module Swate.Components.Shared.Cwl.CommandLineToolMutations

open System
open ARCtrl.CWL
open Swate.Components.Shared.Cwl.EditorMutations
open Swate.Components.Shared.Cwl.RequirementMutations

let private parseIntOrNone (value: string) =
    match Int32.TryParse value with
    | true, parsed -> Some parsed
    | _ -> None

let parseIntentText (value: string) =
    value.Split([| ',' |], StringSplitOptions.RemoveEmptyEntries)
    |> Array.map (fun token -> token.Trim())
    |> Array.filter (String.IsNullOrWhiteSpace >> not)
    |> ResizeArray
    |> fun values -> if values.Count = 0 then None else Some values

let intentText (value: ResizeArray<string> option) =
    value |> Option.defaultValue (ResizeArray()) |> Seq.toList |> String.concat ", "

let private normalizeInputBinding (binding: InputBinding) =
    if
        binding.Prefix.IsNone
        && binding.Position.IsNone
        && binding.ItemSeparator.IsNone
        && binding.Separate.IsNone
    then
        None
    else
        Some binding

let private normalizeOutputBinding (binding: OutputBinding) =
    if binding.Glob.IsNone then None else Some binding

let setProcessingUnitVersion (newVersion: string) (processingUnit: CWLProcessingUnit) =
    match processingUnit with
    | CWLProcessingUnit.CommandLineTool td -> td.CWLVersion <- newVersion
    | CWLProcessingUnit.Workflow wd -> wd.CWLVersion <- newVersion
    | CWLProcessingUnit.ExpressionTool et -> et.CWLVersion <- newVersion
    | CWLProcessingUnit.Operation op -> op.CWLVersion <- newVersion

let setBaseCommand (tool: CWLToolDescription) (command: string) =
    match nonEmptyOrNone command with
    | Some value -> tool.BaseCommand <- Some(ResizeArray [| value |])
    | None -> tool.BaseCommand <- None

let setRequirementEnabled (tool: CWLToolDescription) (key: string) (enabled: bool) =
    tool.Requirements <- toggleRequirement key enabled tool.Requirements

let setHintEnabled (tool: CWLToolDescription) (key: string) (enabled: bool) =
    tool.Hints <- toggleHint key enabled tool.Hints

let setRequirementField (tool: CWLToolDescription) (key: string) (fieldKey: string) (value: string) =
    setRequirementFieldByKey tool.Requirements key fieldKey value

let setHintField (tool: CWLToolDescription) (key: string) (fieldKey: string) (value: string) =
    setHintFieldByKey tool.Hints key fieldKey value

let setRequirementDockerField (tool: CWLToolDescription) (key: string) (fieldKey: string) (value: string) =
    setRequirementField tool key fieldKey value

let setHintDockerField (tool: CWLToolDescription) (key: string) (fieldKey: string) (value: string) =
    setHintField tool key fieldKey value

let addInput (tool: CWLToolDescription) =
    let inputs = CWLToolDescription.getOrCreateInputs tool
    let name = nextName "input" (inputs |> Seq.map (fun item -> item.Name))
    let input = CWLInput(name)
    input.Type_ <- Some CWLType.String
    inputs.Add(input)
    inputs.Count - 1

let renameInputAt (inputs: ResizeArray<CWLInput>) (index: int) (newName: string) =
    if index >= 0 && index < inputs.Count then
        let trimmed = newName.Trim()

        if String.IsNullOrWhiteSpace trimmed |> not then
            let replacement = cloneInputWithName inputs.[index] trimmed
            inputs.[index] <- replacement

let setInputTypeAt (inputs: ResizeArray<CWLInput>) (index: int) (cwlType: CWLType option) =
    if index >= 0 && index < inputs.Count then
        inputs.[index].Type_ <- cwlType

let setInputPrefixAt (inputs: ResizeArray<CWLInput>) (index: int) (prefix: string) =
    if index >= 0 && index < inputs.Count then
        let input = inputs.[index]
        let binding = input.InputBinding |> Option.defaultValue (InputBinding.create ())

        let nextBinding = {
            binding with
                Prefix = nonEmptyOrNone prefix
        }

        input.InputBinding <- normalizeInputBinding nextBinding

let setInputPositionAt (inputs: ResizeArray<CWLInput>) (index: int) (position: string) =
    if index >= 0 && index < inputs.Count then
        let input = inputs.[index]
        let binding = input.InputBinding |> Option.defaultValue (InputBinding.create ())

        let nextBinding = {
            binding with
                Position = parseIntOrNone position
        }

        input.InputBinding <- normalizeInputBinding nextBinding

let setInputOptionalAt (inputs: ResizeArray<CWLInput>) (index: int) (isOptional: bool) =
    if index >= 0 && index < inputs.Count then
        inputs.[index].Optional <- Some isOptional

let removeInput (activeIndex: int option) (inputs: ResizeArray<CWLInput>) =
    removeAtAndSelectNext activeIndex inputs

let moveInputUp (activeIndex: int option) (inputs: ResizeArray<CWLInput>) = moveUp activeIndex inputs

let moveInputDown (activeIndex: int option) (inputs: ResizeArray<CWLInput>) = moveDown activeIndex inputs

let addOutput (outputs: ResizeArray<CWLOutput>) =
    let name = nextName "output" (outputs |> Seq.map (fun item -> item.Name))
    let output = CWLOutput(name)
    output.Type_ <- Some(CWLType.file ())
    outputs.Add(output)
    outputs.Count - 1

let renameOutputAt (outputs: ResizeArray<CWLOutput>) (index: int) (newName: string) =
    if index >= 0 && index < outputs.Count then
        let trimmed = newName.Trim()

        if String.IsNullOrWhiteSpace trimmed |> not then
            let replacement = cloneOutputWithName outputs.[index] trimmed
            outputs.[index] <- replacement

let setOutputTypeAt (outputs: ResizeArray<CWLOutput>) (index: int) (cwlType: CWLType option) =
    if index >= 0 && index < outputs.Count then
        outputs.[index].Type_ <- cwlType

let setOutputGlobAt (outputs: ResizeArray<CWLOutput>) (index: int) (glob: string) =
    if index >= 0 && index < outputs.Count then
        let output = outputs.[index]
        let binding = output.OutputBinding |> Option.defaultValue (OutputBinding.create ())

        let nextBinding = {
            binding with
                Glob = nonEmptyOrNone glob
        }

        output.OutputBinding <- normalizeOutputBinding nextBinding

let removeOutput (activeIndex: int option) (outputs: ResizeArray<CWLOutput>) =
    removeAtAndSelectNext activeIndex outputs

let moveOutputUp (activeIndex: int option) (outputs: ResizeArray<CWLOutput>) = moveUp activeIndex outputs

let moveOutputDown (activeIndex: int option) (outputs: ResizeArray<CWLOutput>) = moveDown activeIndex outputs
