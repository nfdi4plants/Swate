/// Editor-side mutation helpers for ARCtrl mutable collections and types.
/// Keeps array/object mutation logic out of renderer components.
module CWLBuilder.Domain.EditorMutations

open System
open DynamicObj
open ARCtrl.CWL

let nonEmptyOrNone (value: string) =
    if String.IsNullOrWhiteSpace value then None else Some value

let nextName (prefix: string) (existing: seq<string>) =
    let existingSet = existing |> Set.ofSeq
    let mutable index = 1
    let mutable candidate = sprintf "%s_%d" prefix index
    while existingSet.Contains candidate do
        index <- index + 1
        candidate <- sprintf "%s_%d" prefix index
    candidate

let removeAtAndSelectNext (index: int option) (items: ResizeArray<'T>) =
    match index with
    | Some i when i >= 0 && i < items.Count ->
        items.RemoveAt i
        if items.Count = 0 then None
        elif i >= items.Count then Some (items.Count - 1)
        else Some i
    | _ ->
        index

let moveUp (index: int option) (items: ResizeArray<'T>) =
    match index with
    | Some i when i > 0 && i < items.Count ->
        let previous = items.[i - 1]
        items.[i - 1] <- items.[i]
        items.[i] <- previous
        Some (i - 1)
    | Some i when i >= 0 && i < items.Count ->
        Some i
    | _ ->
        None

let moveDown (index: int option) (items: ResizeArray<'T>) =
    match index with
    | Some i when i >= 0 && i < items.Count - 1 ->
        let next = items.[i + 1]
        items.[i + 1] <- items.[i]
        items.[i] <- next
        Some (i + 1)
    | Some i when i >= 0 && i < items.Count ->
        Some i
    | _ ->
        None

let private copyDynamicPropertiesForRenamedPort (source: DynamicObj) (target: DynamicObj) =
    source.GetProperties(false)
    |> Seq.iter (fun kvp ->
        // `name`/`Name` identifies the map key in CWL and must come from constructor arg.
        // Copying a shadow dynamic property here can revert renames in JS runtimes.
        if String.Equals(kvp.Key, "name", StringComparison.OrdinalIgnoreCase) |> not then
            target.SetProperty(kvp.Key, kvp.Value)
    )

let cloneInputWithName (source: CWLInput) (name: string) =
    let replacement =
        CWLInput(
            name,
            ?type_ = source.Type_,
            ?inputBinding = source.InputBinding,
            ?optional = source.Optional
        )

    copyDynamicPropertiesForRenamedPort source replacement
    replacement

let cloneOutputWithName (source: CWLOutput) (name: string) =
    let replacement =
        CWLOutput(
            name,
            ?type_ = source.Type_,
            ?outputBinding = source.OutputBinding,
            ?outputSource = source.OutputSource
        )

    copyDynamicPropertiesForRenamedPort source replacement
    replacement
