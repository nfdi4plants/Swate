namespace Swate.Components.ARCObjectExplorer

open Swate.Components

[<RequireQualifiedAccess>]
module KindFilter =

    let private createOption(label: string) : SelectItem<string> = {|
        label = label
        item = label
    |}

    let ArcObjectExplorerOptions: SelectItem<string>[] =
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

    let GraphObjectExplorerOptions: SelectItem<string>[] =
        ArcObjectExplorerOptions
        |> Array.filter (fun option -> option.item <> "Note")

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
