module Swate.Components.Shared.Cwl.Features.InputsFeature

open System
open Swate.Components.Shared.Cwl.Documents.Common
open Swate.Components.Shared.Cwl.Documents.Mutations
open Swate.Components.Shared.Cwl.Documents.Types

let private nextName (prefix: string) (existing: seq<string>) =
    let existingSet = existing |> Set.ofSeq
    let mutable index = 1
    let mutable candidate = sprintf "%s_%d" prefix index

    while existingSet.Contains candidate do
        index <- index + 1
        candidate <- sprintf "%s_%d" prefix index

    candidate

let private parsePosition (value: string) =
    match Int32.TryParse value with
    | true, parsed -> Some parsed
    | _ -> None

let private normalizeBinding (binding: InputBindingModel) =
    if binding.Prefix.IsNone && binding.Position.IsNone then
        None
    else
        Some binding

let private updateBinding update (input: InputModel) =
    let current =
        input.InputBinding |> Option.defaultValue { Prefix = None; Position = None }

    {
        input with
            InputBinding = current |> update |> normalizeBinding
    }

let private documentInputs document =
    match document with
    | CommandLineToolDoc model -> model.Inputs
    | WorkflowDoc model -> model.Inputs
    | ExpressionToolDoc model -> model.Inputs
    | OperationDoc model -> model.Inputs

let private updateInputs update document =
    match document with
    | CommandLineToolDoc model ->
        CommandLineToolDoc {
            model with
                Inputs = update model.Inputs
        }
    | WorkflowDoc model ->
        WorkflowDoc {
            model with
                Inputs = update model.Inputs
        }
    | ExpressionToolDoc model ->
        ExpressionToolDoc {
            model with
                Inputs = update model.Inputs
        }
    | OperationDoc model ->
        OperationDoc {
            model with
                Inputs = update model.Inputs
        }

let private moveInputUpInList (inputId: InputId) (inputs: InputModel list) =
    match inputs |> List.tryFindIndex (fun input -> input.Id = inputId) with
    | Some index when index > 0 ->
        let items = inputs |> List.toArray
        let previous = items.[index - 1]
        items.[index - 1] <- items.[index]
        items.[index] <- previous
        items |> Array.toList
    | _ -> inputs

let private moveInputDownInList (inputId: InputId) (inputs: InputModel list) =
    match inputs |> List.tryFindIndex (fun input -> input.Id = inputId) with
    | Some index when index >= 0 && index < inputs.Length - 1 ->
        let items = inputs |> List.toArray
        let next = items.[index + 1]
        items.[index + 1] <- items.[index]
        items.[index] <- next
        items |> Array.toList
    | _ -> inputs

let renameInput (inputId: InputId) (name: string) (document: EditorDocument) =
    let trimmed = name.Trim()

    if String.IsNullOrWhiteSpace trimmed then
        document
    else
        document
        |> updateInputs (updateInput inputId (fun input -> { input with Name = trimmed }))

let setInputType (inputId: InputId) (cwlType: string option) (document: EditorDocument) =
    let normalized =
        cwlType
        |> Option.bind (fun value -> if String.IsNullOrWhiteSpace value then None else Some value)

    document
    |> updateInputs (updateInput inputId (fun input -> { input with CwlType = normalized }))

let setInputPrefix (inputId: InputId) (prefix: string) (document: EditorDocument) =
    let normalized =
        if String.IsNullOrWhiteSpace prefix then
            None
        else
            Some prefix

    document
    |> updateInputs (updateInput inputId (updateBinding (fun binding -> { binding with Prefix = normalized })))

let setInputPosition (inputId: InputId) (position: string) (document: EditorDocument) =
    document
    |> updateInputs (
        updateInput
            inputId
            (updateBinding (fun binding -> {
                binding with
                    Position = parsePosition position
            }))
    )

let setInputOptional (inputId: InputId) (isOptional: bool) (document: EditorDocument) =
    document
    |> updateInputs (updateInput inputId (fun input -> { input with Optional = isOptional }))

let addInput (document: EditorDocument) =
    let currentInputs = documentInputs document

    let input = {
        createInput (nextName "input" (currentInputs |> List.map (fun item -> item.Name))) with
            CwlType = Some "string"
    }

    document |> updateInputs (addInput input)

let removeInput (inputId: InputId) (document: EditorDocument) =
    document |> updateInputs (removeInput inputId)

let moveInputUp (inputId: InputId) (document: EditorDocument) =
    document |> updateInputs (moveInputUpInList inputId)

let moveInputDown (inputId: InputId) (document: EditorDocument) =
    document |> updateInputs (moveInputDownInList inputId)
