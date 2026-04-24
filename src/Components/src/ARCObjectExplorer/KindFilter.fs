module Swate.Components.ARCObjectExplorer.KindFilter

open Swate.Components
open Swate.Components.ARCObjectExplorer.GraphExplorer.Model

let private createOption(label: string) : SelectItem<string> = {|
    label = label
    item = label
|}

let arcObjectExplorerOptions: SelectItem<string>[] =
    [|
        "Study"
        "Assay"
        "Workflow"
        "Run"
        "Table"
        "DataMap"
        "Note"
        "Sample"
    |]
    |> Array.map createOption

let graphObjectExplorerOptions: SelectItem<string>[] =
    GraphSemanticKind.allInFilterOrder
    |> List.map GraphSemanticKind.label
    |> List.toArray
    |> Array.map createOption

let defaultSelectedIndices (options: SelectItem<string>[]) =
    options
    |> Array.mapi (fun index _ -> index)
    |> Set.ofArray

let selectedLabels (options: SelectItem<string>[]) (selectedKindIndices: Set<int>) =
    selectedKindIndices
    |> Seq.sort
    |> Seq.choose (fun index ->
        options
        |> Array.tryItem index
        |> Option.map (fun option -> option.item))
    |> Set.ofSeq
