namespace Swate.Components.ARCObjectExplorer

open Swate.Components

[<RequireQualifiedAccess>]
module KindFilter =

    let ArcObjectExplorerOptions: SelectItem<string>[] = [|
        {| label = "Study"; item = "Study" |}
        {| label = "Assay"; item = "Assay" |}
        {| label = "Workflow"; item = "Workflow" |}
        {| label = "Run"; item = "Run" |}
        {| label = "Table"; item = "Table" |}
        {| label = "DataMap"; item = "DataMap" |}
        {| label = "Note"; item = "Note" |}
        {| label = "Sample"; item = "Sample" |}
    |]

    let GraphObjectExplorerOptions: SelectItem<string>[] = [|
        {| label = "Study"; item = "Study" |}
        {| label = "Assay"; item = "Assay" |}
        {| label = "Workflow"; item = "Workflow" |}
        {| label = "Run"; item = "Run" |}
        {| label = "Table"; item = "Table" |}
        {| label = "DataMap"; item = "DataMap" |}
        {| label = "Sample"; item = "Sample" |}
    |]

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
