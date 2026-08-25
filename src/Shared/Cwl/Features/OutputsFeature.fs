module Swate.Components.Shared.Cwl.Features.OutputsFeature

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

let private normalizeBinding (binding: OutputBindingModel) =
    if binding.Glob.IsNone then None else Some binding

let private updateBinding update (output: OutputModel) =
    let current = output.OutputBinding |> Option.defaultValue { Glob = None }

    {
        output with
            OutputBinding = current |> update |> normalizeBinding
    }

let private moveOutputUpInList (outputId: OutputId) (outputs: OutputModel list) =
    match outputs |> List.tryFindIndex (fun output -> output.Id = outputId) with
    | Some index when index > 0 ->
        let items = outputs |> List.toArray
        let previous = items.[index - 1]
        items.[index - 1] <- items.[index]
        items.[index] <- previous
        items |> Array.toList
    | _ -> outputs

let private moveOutputDownInList (outputId: OutputId) (outputs: OutputModel list) =
    match outputs |> List.tryFindIndex (fun output -> output.Id = outputId) with
    | Some index when index >= 0 && index < outputs.Length - 1 ->
        let items = outputs |> List.toArray
        let next = items.[index + 1]
        items.[index + 1] <- items.[index]
        items.[index] <- next
        items |> Array.toList
    | _ -> outputs

let renameOutput (outputId: OutputId) (name: string) (document: EditorDocument) =
    let trimmed = name.Trim()

    if String.IsNullOrWhiteSpace trimmed then
        document
    else
        document
        |> updateDocumentOutputs (updateOutput outputId (fun output -> { output with Name = trimmed }))

let setOutputType (outputId: OutputId) (cwlType: string option) (document: EditorDocument) =
    let normalized =
        cwlType
        |> Option.bind (fun value -> if String.IsNullOrWhiteSpace value then None else Some value)

    document
    |> updateDocumentOutputs (updateOutput outputId (fun output -> { output with CwlType = normalized }))

let setOutputGlob (outputId: OutputId) (glob: string) (document: EditorDocument) =
    let normalized = if String.IsNullOrWhiteSpace glob then None else Some glob

    document
    |> updateDocumentOutputs (updateOutput outputId (updateBinding (fun binding -> { binding with Glob = normalized })))

let addOutput (document: EditorDocument) =
    let currentOutputs =
        match document with
        | CommandLineToolDoc model -> model.Outputs
        | WorkflowDoc model -> model.Outputs
        | ExpressionToolDoc model -> model.Outputs
        | OperationDoc model -> model.Outputs

    let output = {
        createOutput (nextName "output" (currentOutputs |> List.map (fun item -> item.Name))) with
            CwlType = Some "file"
    }

    document |> updateDocumentOutputs (addOutput output)

let removeOutput (outputId: OutputId) (document: EditorDocument) =
    document |> updateDocumentOutputs (removeOutput outputId)

let moveOutputUp (outputId: OutputId) (document: EditorDocument) =
    document |> updateDocumentOutputs (moveOutputUpInList outputId)

let moveOutputDown (outputId: OutputId) (document: EditorDocument) =
    document |> updateDocumentOutputs (moveOutputDownInList outputId)
