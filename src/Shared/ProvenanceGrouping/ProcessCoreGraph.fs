module internal Swate.Components.Shared.ProvenanceGrouping.ProcessCoreGraph

open System.Text
open ProcessCore
open Swate.Components.Shared.ProvenanceGrouping.Types
open Swate.Components.Shared.ProvenanceGrouping.ProcessCoreAdapterTypes

type DatasetEntry = { Path: string list; Dataset: Dataset }

/// Length-prefixed encoding for an optional string field so concatenated
/// fingerprint segments cannot collide across different field boundaries.
let private field (value: string option) =
    match value with
    | None -> "-1:"
    | Some text -> string text.Length + ":" + text

let datasetEntries (arc: ARC) : DatasetEntry list =
    let rec walk (path: string list) (dataset: Dataset) : DatasetEntry list =
        let currentPath = path @ [ dataset.Identifier ]

        {
            Path = currentPath
            Dataset = dataset
        }
        :: (dataset.HasPart |> Seq.toList |> List.collect (walk currentPath))

    walk [] (arc :> Dataset)

let resolveDatasetMatches (path: string list) (arc: ARC) : Dataset list =
    datasetEntries arc
    |> List.filter (fun entry -> entry.Path = path)
    |> List.map (fun entry -> entry.Dataset)

let tryResolveDataset (path: string list) (arc: ARC) : Dataset option =
    match resolveDatasetMatches path arc with
    | [ dataset ] -> Some dataset
    | _ -> None

let tryDatasetPath (dataset: Dataset) (arc: ARC) : string list option =
    datasetEntries arc
    |> List.tryFind (fun entry -> obj.ReferenceEquals(entry.Dataset, dataset))
    |> Option.map (fun entry -> entry.Path)

let processLocation (datasetPath: string list) (index: int) (proc: Process) : ProcessCoreProcessLocation = {
    DatasetPath = datasetPath
    ProcessIndex = index
    ExpectedName = proc.Name
}

let tryResolveProcess (location: ProcessCoreProcessLocation) (arc: ARC) : Process option =
    tryResolveDataset location.DatasetPath arc
    |> Option.bind (fun dataset ->
        if location.ProcessIndex >= 0 && location.ProcessIndex < dataset.Processes.Count then
            let proc = dataset.Processes.[location.ProcessIndex]

            if proc.Name = location.ExpectedName then
                Some proc
            else
                None
        else
            None
    )

let annotationFingerprint (annotation: Annotation) : ProcessCoreAnnotationFingerprint = {
    Name = annotation.Name
    Value = annotation.Value
    Unit = annotation.Unit
    NameTAN = annotation.NameTAN
    ValueTAN = annotation.ValueTAN
    UnitTAN = annotation.UnitTAN
    AdditionalType = annotation.AdditionalType
}

/// Mirrors ProcessCore's own `Annotation.Equals` (Name, Value, Unit, NameTAN).
/// Used only to detect public-API deduplication collisions, never as a
/// substitute for the full fingerprint when deciding round-trip identity.
let annotationsEqualByProcessCoreKey (left: Annotation) (right: Annotation) : bool =
    left.Name = right.Name
    && left.Value = right.Value
    && left.Unit = right.Unit
    && left.NameTAN = right.NameTAN

let private appendAnnotation (sb: StringBuilder) (annotation: Annotation) =
    sb.Append(field (Some annotation.Name)) |> ignore
    sb.Append(field annotation.Value) |> ignore
    sb.Append(field annotation.Unit) |> ignore
    sb.Append(field annotation.NameTAN) |> ignore
    sb.Append(field annotation.ValueTAN) |> ignore
    sb.Append(field annotation.UnitTAN) |> ignore
    sb.Append(field annotation.AdditionalType) |> ignore

let private nodeAdditionalType (node: IONode) =
    match node with
    | SampleNode sample -> sample.AdditionalType
    | DataNode data -> data.AdditionalType

let private nodeAdditionalProperties (node: IONode) : Annotation seq =
    match node with
    | SampleNode sample -> sample.AdditionalProperty :> seq<Annotation>
    | DataNode data -> data.AdditionalProperty :> seq<Annotation>

/// Canonical, Fable-friendly, length-prefixed encoding of the reachable
/// graph state used for round-trip and staleness detection. Deliberately not
/// `GetHashCode()`, which is unstable across runs and does not distinguish
/// content changes from unrelated objects.
let graphFingerprint (arc: ARC) : string =
    let sb = StringBuilder()

    for entry in datasetEntries arc do
        sb.Append(field (Some(String.concat "/" entry.Path))) |> ignore

        for index in 0 .. entry.Dataset.Processes.Count - 1 do
            let proc = entry.Dataset.Processes.[index]
            sb.Append(field (Some(string index))) |> ignore
            sb.Append(field (Some proc.Name)) |> ignore
            sb.Append(field proc.AdditionalType) |> ignore

            for node in Seq.append proc.Inputs proc.Outputs do
                let kind =
                    match node with
                    | SampleNode _ -> "S"
                    | DataNode _ -> "D"

                sb.Append(field (Some kind)) |> ignore
                sb.Append(field (Some(node.Key()))) |> ignore
                sb.Append(field (nodeAdditionalType node)) |> ignore

                for annotation in nodeAdditionalProperties node do
                    appendAnnotation sb annotation

            for parameterValue in proc.ParameterValue do
                appendAnnotation sb parameterValue

            match proc.ExecutesProtocol with
            | Some recipe ->
                sb.Append(field recipe.Name) |> ignore
                sb.Append(field recipe.Version) |> ignore
                sb.Append(field recipe.Description) |> ignore
                sb.Append(field recipe.Url) |> ignore

                for recipeComponent in recipe.Components do
                    appendAnnotation sb recipeComponent
            | None -> sb.Append("-1:") |> ignore

    sb.ToString()

let nodeLocation (node: IONode) : ProcessCoreNodeLocation =
    match node with
    | SampleNode _ -> {
        Kind = ProcessCoreNodeKind.Sample
        Key = node.Key()
      }
    | DataNode _ -> {
        Kind = ProcessCoreNodeKind.Data
        Key = node.Key()
      }

let nodeDisplayName (node: IONode) =
    match node with
    | SampleNode sample -> sample.Name
    | DataNode data -> data.Name

let sourceRef (location: ProcessCoreTableLocation) : ProvenanceSourceRef = {
    Id = String.concat "/" (location.DatasetPath @ [ location.TableName ])
    Name = location.TableName
}

let processId (location: ProcessCoreProcessLocation) : ProvenanceProcessId =
    String.concat "/" (location.DatasetPath @ [ string location.ProcessIndex; location.ExpectedName ])
