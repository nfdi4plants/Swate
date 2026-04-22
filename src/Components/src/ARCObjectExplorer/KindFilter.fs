module Swate.Components.ARCObjectExplorer.KindFilter

open Swate.Components

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
    [|
        "Datasets"
        "Protocols"
        "FormalParameters"
        "Processes"
        "Materials"
        "Data"
    |]
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
